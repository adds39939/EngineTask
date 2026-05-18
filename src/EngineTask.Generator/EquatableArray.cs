using System;
using System.Collections;
using System.Collections.Generic;

namespace EngineTask.Generator;

internal readonly struct EquatableArray<T> : IEquatable<EquatableArray<T>>, IEnumerable<T>
    where T : IEquatable<T>
{
    private readonly T[]? _items;

    public EquatableArray(T[]? items) => _items = items;

    public static EquatableArray<T> Empty { get; } = new(Array.Empty<T>());

    public int Length => _items?.Length ?? 0;

    public T this[int index] => _items![index];

    public bool Equals(EquatableArray<T> other)
    {
        var left = _items;
        var right = other._items;
        if (ReferenceEquals(left, right)) return true;
        if (left is null || right is null) return false;
        if (left.Length != right.Length) return false;
        for (var i = 0; i < left.Length; i++)
            if (!left[i].Equals(right[i])) return false;
        return true;
    }

    public override bool Equals(object? obj) => obj is EquatableArray<T> ea && Equals(ea);

    public override int GetHashCode()
    {
        if (_items is null) return 0;
        unchecked
        {
            var hash = 17;
            foreach (var item in _items)
                hash = (hash * 31) + (item?.GetHashCode() ?? 0);
            return hash;
        }
    }

    public IEnumerator<T> GetEnumerator() =>
        ((IEnumerable<T>)(_items ?? Array.Empty<T>())).GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
