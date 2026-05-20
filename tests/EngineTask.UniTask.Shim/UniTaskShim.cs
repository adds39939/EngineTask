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
// UniTask mirror to compile and run end-to-end, AND to faithfully
// model UniTask's no-allocation property on the synchronous async path.
//
// UniTask<T> and UniTask are readonly structs. AsyncUniTaskMethodBuilder
// is a struct builder — for a synchronously-completing `async UniTask<int>
// M() => 5;`, the entire builder + state machine + result stays on the
// stack. The build encodes a deliberate constraint: AwaitOnCompleted and
// AwaitUnsafeOnCompleted throw NotSupportedException. We are not trying
// to faithfully model UniTask's asynchronous pooling machinery; we are
// modelling the zero-alloc synchronous path that the Phase 5 / Phase 7
// allocation tests assert against.

namespace Cysharp.Threading.Tasks;

using System;
using System.Runtime.CompilerServices;

[AsyncMethodBuilder(typeof(AsyncUniTaskMethodBuilder))]
public readonly struct UniTask
{
    public Awaiter GetAwaiter() => default;

    public static UniTask<T> FromResult<T>(T value) => new(value);
    public static UniTask CompletedTask => default;

    public readonly struct Awaiter : INotifyCompletion
    {
        public bool IsCompleted => true;
        public void GetResult() { }
        public void OnCompleted(Action continuation) => continuation();
    }
}

[AsyncMethodBuilder(typeof(AsyncUniTaskMethodBuilder<>))]
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

// Struct method builder for `async UniTask M() { ... }`.
// Models the synchronous-completion path with zero heap allocation.
public struct AsyncUniTaskMethodBuilder
{
    private Exception? _exception;

    public static AsyncUniTaskMethodBuilder Create() => default;

    public void Start<TStateMachine>(ref TStateMachine stateMachine)
        where TStateMachine : IAsyncStateMachine
        => stateMachine.MoveNext();

    public void SetStateMachine(IAsyncStateMachine stateMachine) { }
    public void SetException(Exception exception) => _exception = exception;
    public void SetResult() { }

    public void AwaitOnCompleted<TAwaiter, TStateMachine>(ref TAwaiter awaiter, ref TStateMachine stateMachine)
        where TAwaiter : INotifyCompletion
        where TStateMachine : IAsyncStateMachine
        => throw new NotSupportedException(
            "This UniTask shim supports synchronously-completing async methods only. Use the real Cysharp.Threading.Tasks.UniTask for asynchronous paths.");

    public void AwaitUnsafeOnCompleted<TAwaiter, TStateMachine>(ref TAwaiter awaiter, ref TStateMachine stateMachine)
        where TAwaiter : ICriticalNotifyCompletion
        where TStateMachine : IAsyncStateMachine
        => throw new NotSupportedException(
            "This UniTask shim supports synchronously-completing async methods only. Use the real Cysharp.Threading.Tasks.UniTask for asynchronous paths.");

    public UniTask Task
    {
        get
        {
            if (_exception is not null) throw _exception;
            return default;
        }
    }
}

// Struct method builder for `async UniTask<T> M() { ... return value; }`.
// Models the synchronous-completion path with zero heap allocation —
// SetResult stores the T inline; the returned UniTask<T> wraps it.
public struct AsyncUniTaskMethodBuilder<T>
{
    private T _result;
    private Exception? _exception;

    public static AsyncUniTaskMethodBuilder<T> Create() => default;

    public void Start<TStateMachine>(ref TStateMachine stateMachine)
        where TStateMachine : IAsyncStateMachine
        => stateMachine.MoveNext();

    public void SetStateMachine(IAsyncStateMachine stateMachine) { }
    public void SetException(Exception exception) => _exception = exception;
    public void SetResult(T result) => _result = result;

    public void AwaitOnCompleted<TAwaiter, TStateMachine>(ref TAwaiter awaiter, ref TStateMachine stateMachine)
        where TAwaiter : INotifyCompletion
        where TStateMachine : IAsyncStateMachine
        => throw new NotSupportedException(
            "This UniTask shim supports synchronously-completing async methods only. Use the real Cysharp.Threading.Tasks.UniTask for asynchronous paths.");

    public void AwaitUnsafeOnCompleted<TAwaiter, TStateMachine>(ref TAwaiter awaiter, ref TStateMachine stateMachine)
        where TAwaiter : ICriticalNotifyCompletion
        where TStateMachine : IAsyncStateMachine
        => throw new NotSupportedException(
            "This UniTask shim supports synchronously-completing async methods only. Use the real Cysharp.Threading.Tasks.UniTask for asynchronous paths.");

    public UniTask<T> Task
    {
        get
        {
            if (_exception is not null) throw _exception;
            return new UniTask<T>(_result);
        }
    }
}
