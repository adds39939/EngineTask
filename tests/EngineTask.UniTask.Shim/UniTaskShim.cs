// A deliberately-minimal stand-in for Cysharp.Threading.Tasks.UniTask.
//
// Real UniTask ships as part of the Cysharp/UniTask Unity package and
// has a runtime dependency on UnityEngine. Pulling it into a plain
// net8.0 project either fails (the package is not on NuGet in a form
// usable outside Unity) or drags in UnityEngine.dll, which is not
// available here. The Plan calls Unity integration in CI a known pain
// and tells us to skip it.
//
// What we need from this shim is enough surface area for the generated
// UniTask mirror — `public UniTask<int> AddAsync(...) =>
// global::Cysharp.Threading.Tasks.UniTask.FromResult(a + b)` — to
// compile and run end-to-end, AND to faithfully model UniTask's
// no-allocation property on the synchronous path. UniTask<T> is a
// readonly struct holding T inline, and FromResult is a static factory
// returning that struct — no heap allocation occurs anywhere in the
// chain. This matches the real package's allocation profile on the
// synchronously-completed path.
//
// The async-state-machine path (an `async UniTask<int>` method) is NOT
// supported by this shim — there is no AsyncUniTaskMethodBuilder.
// Adding one would inevitably either (a) wrap an AsyncTaskMethodBuilder
// internally, which would allocate and mislead the benchmarks, or (b)
// reimplement UniTask's pooled state-source machinery, which is large
// and out of Phase 5's scope. The shim therefore covers the Task ↔
// UniTask static-factory translation correctly, and the async-path
// claim is exercised through GDTask (which we consume from the real
// NuGet package) instead.

namespace Cysharp.Threading.Tasks;

using System;
using System.Runtime.CompilerServices;

public readonly struct UniTask
{
    public static UniTask<T> FromResult<T>(T value) => new(value);
}

public readonly struct UniTask<T>
{
    private readonly T _value;
    public UniTask(T value) => _value = value;
    public Awaiter GetAwaiter() => new(_value);

    public readonly struct Awaiter : INotifyCompletion
    {
        private readonly T _value;
        public Awaiter(T value) => _value = value;
        public bool IsCompleted => true;
        public T GetResult() => _value;
        public void OnCompleted(Action continuation) => continuation();
    }
}
