
using System;

namespace TsOpUndo.Internal
{
    internal class Disposer : IDisposable
    {
        private bool _disposed;
        private readonly Action _action;

        public Disposer(Action action)
        {
            _action = action;
        }

        ~Disposer()
        {
            Dispose();
        }


        public void Dispose()
        {
            if (_disposed) return;

            _action();
            _disposed = true;
        }
    }
}