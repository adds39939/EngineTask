using System;
using System.Collections.Generic;
using System.Text;

namespace EngineTask.Generator.CustomFlavours;

// A deliberately-minimal recursive-descent JSON parser.
//
// Why hand-rolled: System.Text.Json is fine in normal code but
// shipping it inside a source-generator NuGet package risks
// assembly-loading conflicts in the Roslyn host process. The
// enginetask.json schema only uses objects, arrays, and strings, so
// 80 lines is enough — and keeping it in-tree means no extra ship-time
// dependencies.
//
// Returns nested `object?` with these runtime types:
//   - JSON object → Dictionary<string, object?>
//   - JSON array  → List<object?>
//   - JSON string → string
//   - JSON null   → null
//   - JSON true/false → bool (parsed but not used by the schema)
//   - JSON number → double (parsed but not used by the schema)
//
// Throws MiniJsonException on malformed input; the generator catches
// this at the AdditionalFiles boundary and converts to a diagnostic.
internal static class MiniJson
{
    public static object? Parse(string text)
    {
        var ctx = new Cursor(text);
        SkipWs(ctx);
        var result = ParseValue(ctx);
        SkipWs(ctx);
        if (ctx.Pos < ctx.Text.Length)
            throw new MiniJsonException($"Unexpected trailing input at position {ctx.Pos}");
        return result;
    }

    private sealed class Cursor
    {
        public readonly string Text;
        public int Pos;
        public Cursor(string text) { Text = text; Pos = 0; }
    }

    private static object? ParseValue(Cursor c)
    {
        SkipWs(c);
        if (c.Pos >= c.Text.Length)
            throw new MiniJsonException("Unexpected end of input");
        var ch = c.Text[c.Pos];
        if (ch == '{') return ParseObject(c);
        if (ch == '[') return ParseArray(c);
        if (ch == '"') return ParseString(c);
        if (ch == 't' || ch == 'f') return ParseBool(c);
        if (ch == 'n') return ParseNull(c);
        if (ch == '-' || (ch >= '0' && ch <= '9')) return ParseNumber(c);
        throw new MiniJsonException($"Unexpected character '{ch}' at position {c.Pos}");
    }

    private static Dictionary<string, object?> ParseObject(Cursor c)
    {
        Expect(c, '{');
        var result = new Dictionary<string, object?>(StringComparer.Ordinal);
        SkipWs(c);
        if (Peek(c) == '}') { c.Pos++; return result; }
        while (true)
        {
            SkipWs(c);
            var key = ParseString(c);
            SkipWs(c);
            Expect(c, ':');
            var value = ParseValue(c);
            result[key] = value;
            SkipWs(c);
            var next = Peek(c);
            if (next == ',') { c.Pos++; continue; }
            if (next == '}') { c.Pos++; return result; }
            throw new MiniJsonException($"Expected ',' or '}}' at position {c.Pos}");
        }
    }

    private static List<object?> ParseArray(Cursor c)
    {
        Expect(c, '[');
        var result = new List<object?>();
        SkipWs(c);
        if (Peek(c) == ']') { c.Pos++; return result; }
        while (true)
        {
            result.Add(ParseValue(c));
            SkipWs(c);
            var next = Peek(c);
            if (next == ',') { c.Pos++; continue; }
            if (next == ']') { c.Pos++; return result; }
            throw new MiniJsonException($"Expected ',' or ']' at position {c.Pos}");
        }
    }

    private static string ParseString(Cursor c)
    {
        Expect(c, '"');
        var sb = new StringBuilder();
        while (c.Pos < c.Text.Length)
        {
            var ch = c.Text[c.Pos++];
            if (ch == '"') return sb.ToString();
            if (ch != '\\') { sb.Append(ch); continue; }
            if (c.Pos >= c.Text.Length)
                throw new MiniJsonException("Unterminated escape");
            var esc = c.Text[c.Pos++];
            switch (esc)
            {
                case '"': sb.Append('"'); break;
                case '\\': sb.Append('\\'); break;
                case '/': sb.Append('/'); break;
                case 'b': sb.Append('\b'); break;
                case 'f': sb.Append('\f'); break;
                case 'n': sb.Append('\n'); break;
                case 'r': sb.Append('\r'); break;
                case 't': sb.Append('\t'); break;
                case 'u':
                    if (c.Pos + 4 > c.Text.Length)
                        throw new MiniJsonException("Truncated \\u escape");
                    var hex = c.Text.Substring(c.Pos, 4);
                    if (!ushort.TryParse(hex, System.Globalization.NumberStyles.HexNumber,
                            System.Globalization.CultureInfo.InvariantCulture, out var code))
                        throw new MiniJsonException($"Invalid \\u escape '{hex}'");
                    sb.Append((char)code);
                    c.Pos += 4;
                    break;
                default:
                    throw new MiniJsonException($"Invalid escape '\\{esc}'");
            }
        }
        throw new MiniJsonException("Unterminated string");
    }

    private static bool ParseBool(Cursor c)
    {
        if (c.Text.Length - c.Pos >= 4 && c.Text.Substring(c.Pos, 4) == "true")
        { c.Pos += 4; return true; }
        if (c.Text.Length - c.Pos >= 5 && c.Text.Substring(c.Pos, 5) == "false")
        { c.Pos += 5; return false; }
        throw new MiniJsonException($"Expected true/false at position {c.Pos}");
    }

    private static object? ParseNull(Cursor c)
    {
        if (c.Text.Length - c.Pos >= 4 && c.Text.Substring(c.Pos, 4) == "null")
        { c.Pos += 4; return null; }
        throw new MiniJsonException($"Expected null at position {c.Pos}");
    }

    private static double ParseNumber(Cursor c)
    {
        var start = c.Pos;
        if (Peek(c) == '-') c.Pos++;
        while (c.Pos < c.Text.Length && (
            (c.Text[c.Pos] >= '0' && c.Text[c.Pos] <= '9') ||
            c.Text[c.Pos] == '.' ||
            c.Text[c.Pos] == 'e' || c.Text[c.Pos] == 'E' ||
            c.Text[c.Pos] == '+' || c.Text[c.Pos] == '-'))
            c.Pos++;
        var slice = c.Text.Substring(start, c.Pos - start);
        if (!double.TryParse(slice, System.Globalization.NumberStyles.Float,
                System.Globalization.CultureInfo.InvariantCulture, out var value))
            throw new MiniJsonException($"Invalid number '{slice}' at position {start}");
        return value;
    }

    private static void SkipWs(Cursor c)
    {
        while (c.Pos < c.Text.Length)
        {
            var ch = c.Text[c.Pos];
            if (ch == ' ' || ch == '\t' || ch == '\r' || ch == '\n') c.Pos++;
            else break;
        }
    }

    private static char Peek(Cursor c) =>
        c.Pos < c.Text.Length ? c.Text[c.Pos] : '\0';

    private static void Expect(Cursor c, char ch)
    {
        if (c.Pos >= c.Text.Length || c.Text[c.Pos] != ch)
            throw new MiniJsonException($"Expected '{ch}' at position {c.Pos}");
        c.Pos++;
    }
}

internal sealed class MiniJsonException : Exception
{
    public MiniJsonException(string message) : base(message) { }
}
