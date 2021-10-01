using System;

namespace TsOpUndo
{
    public interface ICancellable
    {
        void Cancel();
    }

    public class Cancellable : ICancellable
    {
        private bool _unregistered;
        private Action _action;

        public Cancellable(Action act)
        {
            _action = act;
        }

        public void Cancel()
        {
            if (_unregistered) return;

            _action();
            _unregistered = true;
        }
    }
}
