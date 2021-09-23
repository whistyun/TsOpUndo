using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using TsOperationHistory.Extensions;

namespace TsOperationHistory
{
    public class OperationRecorder
    {
        private readonly IOperationController _root;
        private readonly Stack<IOperationController> _stack = new Stack<IOperationController>();

        public IOperationController Current
        {
            get
            {
                if (_stack.Any())
                    return _stack.Peek();
                return _root;
            }
        }

        public OperationRecorder()
            : this(new OperationController(1024))
        {

        }

        public OperationRecorder(IOperationController controller)
        {
            _root = controller;
        }

        public void BeginRecode([CallerMemberName] string callerMemberName = "")
        {
            string className = Reflections.NameOfCallingClass();
            Trace.WriteLine($"{className}.{callerMemberName}() => BeginRecode()");
            _stack.Push(new OperationController(1024));
        }

        public void EndRecode([CallerMemberName] string callerMemberName = "")
        {
            string className = Reflections.NameOfCallingClass();
            string message = $"{className}.{callerMemberName}() => EndRecode()";
            Trace.WriteLine(message);
            var controller = _stack.Pop();
            var operation = controller.Operations.ToCompositeOperation();
            operation.Message = message;
            Current.Push(operation);
        }

        public DisposableRecorder Begin([CallerMemberName] string callerMemberName = "")
        {
            return new DisposableRecorder(this);
        }

        public class DisposableRecorder : IDisposable
        {
            private bool IsDisposed = false;
            OperationRecorder Owner { get; }

            public IOperationController Current
            {
                get => Owner.Current;
            }

            public DisposableRecorder(OperationRecorder owner)
            {
                Owner = owner;
                Owner.BeginRecode();
            }

            ~DisposableRecorder() => Dispose();

            public void Dispose()
            {
                if (IsDisposed) return;

                IsDisposed = true;
                Owner.EndRecode();
            }
        }
    }
}