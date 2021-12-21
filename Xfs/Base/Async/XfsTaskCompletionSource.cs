﻿using System;
using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;

namespace Xfs
{
    public class XfsTaskCompletionSource : IXfsAwaiter
    {
        private const int Pending = 0;
        private const int Succeeded = 1;
        private const int Faulted = 2;
        private const int Canceled = 3;

        private int state;
        private ExceptionDispatchInfo? exception;
        private Action? continuation;                                                             /// action or list

        XfsAwaiterStatus IXfsAwaiter.Status => (XfsAwaiterStatus)state;

        bool IXfsAwaiter.IsCompleted => state != Pending;

        public XfsTask Task => new XfsTask(this);

        void IXfsAwaiter.GetResult()
        {
            switch (this.state)
            {
                case Succeeded:
                    return;
                case Faulted:
                    this.exception?.Throw();
                    this.exception = null;
                    return;
                case Canceled:
                    {
                        this.exception?.Throw(); // guranteed operation canceled exception.
                        this.exception = null;
                        throw new OperationCanceledException();
                    }
                default:
                    throw new NotSupportedException("ETTask does not allow call GetResult directly when task not completed. Please use 'await'.");
            }
        }

        void ICriticalNotifyCompletion.UnsafeOnCompleted(Action action)
        {
            this.continuation = action;
            if (state != Pending)
            {
                TryInvokeContinuation();
            }
        }

        private void TryInvokeContinuation()
        {
            this.continuation?.Invoke();
            this.continuation = null;
        }

        public void SetResult()
        {
            if (this.TrySetResult())
            {
                return;
            }

            throw new InvalidOperationException("TaskT_TransitionToFinal_AlreadyCompleted");
        }
        public void SetException(Exception e)
        {
            if (this.TrySetException(e))
            {
                return;
            }

            throw new InvalidOperationException("TaskT_TransitionToFinal_AlreadyCompleted");
        }
        public bool TrySetResult()
        {
            if (this.state != Pending)
            {
                return false;
            }

            this.state = Succeeded;

            this.TryInvokeContinuation();
            return true;

        }
        public bool TrySetException(Exception e)
        {
            if (this.state != Pending)
            {
                return false;
            }

            this.state = Faulted;

            this.exception = ExceptionDispatchInfo.Capture(e);
            this.TryInvokeContinuation();
            return true;

        }
        public bool TrySetCanceled()
        {
            if (this.state != Pending)
            {
                return false;
            }

            this.state = Canceled;

            this.TryInvokeContinuation();
            return true;
        }
        public bool TrySetCanceled(OperationCanceledException e)
        {
            if (this.state != Pending)
            {
                return false;
            }

            this.state = Canceled;

            this.exception = ExceptionDispatchInfo.Capture(e);
            this.TryInvokeContinuation();
            return true;
        }
        void INotifyCompletion.OnCompleted(Action action)
        {
            ((ICriticalNotifyCompletion)this).UnsafeOnCompleted(action);
        }

    }
    public class XfsTaskCompletionSource<T> : IXfsAwaiter<T>
    {
        //State(= AwaiterStatus)

        private const int Pending = 0;
        private const int Succeeded = 1;
        private const int Faulted = 2;
        private const int Canceled = 3;

        private int state;
        private T? value;
        private ExceptionDispatchInfo? exception;
        private Action? continuation; // action or list

        bool IXfsAwaiter.IsCompleted => state != Pending;

        public XfsTask<T> Task => new XfsTask<T>(this);

        XfsAwaiterStatus IXfsAwaiter.Status => (XfsAwaiterStatus)state;

        T IXfsAwaiter<T>.GetResult()
        {
            switch (this.state)
            {
                case Succeeded:
                    return this.value;
                case Faulted:
                    this.exception?.Throw();
                    this.exception = null;
                    return default;
                case Canceled:
                    {
                        this.exception?.Throw(); // guranteed operation canceled exception.
                        this.exception = null;
                        throw new OperationCanceledException();
                    }
                default:
                    throw new NotSupportedException("ETTask does not allow call GetResult directly when task not completed. Please use 'await'.");
            }
        }

        void ICriticalNotifyCompletion.UnsafeOnCompleted(Action action)
        {
            this.continuation = action;
            if (state != Pending)
            {
                TryInvokeContinuation();
            }
        }

        private void TryInvokeContinuation()
        {
            this.continuation?.Invoke();
            this.continuation = null;
        }

        public void SetResult(T result)
        {
            if (this.TrySetResult(result))
            {
                return;
            }

            throw new InvalidOperationException("TaskT_TransitionToFinal_AlreadyCompleted");
        }

        public void SetException(Exception e)
        {
            if (this.TrySetException(e))
            {
                return;
            }

            throw new InvalidOperationException("TaskT_TransitionToFinal_AlreadyCompleted");
        }

        public bool TrySetResult(T result)
        {
            if (this.state != Pending)
            {
                return false;
            }

            this.state = Succeeded;

            this.value = result;
            this.TryInvokeContinuation();
            return true;

        }

        public bool TrySetException(Exception e)
        {
            if (this.state != Pending)
            {
                return false;
            }

            this.state = Faulted;

            this.exception = ExceptionDispatchInfo.Capture(e);
            this.TryInvokeContinuation();
            return true;

        }

        public bool TrySetCanceled()
        {
            if (this.state != Pending)
            {
                return false;
            }

            this.state = Canceled;

            this.TryInvokeContinuation();
            return true;

        }

        public bool TrySetCanceled(OperationCanceledException e)
        {
            if (this.state != Pending)
            {
                return false;
            }

            this.state = Canceled;

            this.exception = ExceptionDispatchInfo.Capture(e);
            this.TryInvokeContinuation();
            return true;

        }

        void IXfsAwaiter.GetResult()
        {
            ((IXfsAwaiter<T>)this).GetResult();
        }

        void INotifyCompletion.OnCompleted(Action action)
        {
            ((ICriticalNotifyCompletion)this).UnsafeOnCompleted(action);
        }
    }



}

