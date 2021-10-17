using System;
using System.Collections.Generic;
using System.Linq;

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

    public static class CancellableExt
    {
        public static ICancellable Collect(this IEnumerable<ICancellable> cancellables)
        {
            return new CancellableCollector(cancellables);
        }

        public static ICancellable CollectOrNull(this IEnumerable<ICancellable> cancellables)
        {
            var array = cancellables.ToArray();

            return array.Length != 0 ?
                new CancellableCollector(array) :
                null;
        }
    }

    internal class CancellableCollector : ICancellable
    {
        private ICancellable[] _cancellables;

        internal CancellableCollector(ICancellable[] cancellables)
        {
            _cancellables = cancellables;
        }
        public CancellableCollector(IEnumerable<ICancellable> cancellables) : this(cancellables.ToArray())
        {
        }

        public void Cancel()
        {
            foreach (var c in _cancellables)
                c.Cancel();
        }
    }
}
