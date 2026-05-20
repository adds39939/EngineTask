// A deliberately-minimal stand-in for Cysharp.Threading.Tasks.UniTask.
//
// Real UniTask ships as part of the Cysharp/UniTask Unity package and
// has a runtime dependency on UnityEngine. Pulling it into a plain
// net8.0 xUnit project either fails (the package is not on NuGet in a
// form usable outside Unity) or drags in UnityEngine.dll, which is not
// available here. The Plan calls Unity integration in CI a known pain
// and tells us to skip it.
//
// What we need from this shim is just enough surface area for the
// generated UniTask mirror — public UniTask<int> AddAsync(...) =>
// global::Cysharp.Threading.Tasks.UniTask.FromResult(a + b) — to
// compile and run end-to-end. That is: an awaitable UniTask<T> and a
// static UniTask.FromResult<T>. Nothing else is exercised yet.
//
// Phase 5 (allocation verification) is where this stub is expected to
// either be replaced by the real library or by a behaviourally-accurate
// fake; the Phase 4 contract is just "the generator emits code that
// compiles and runs against Cysharp.Threading.Tasks.UniTask".

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
