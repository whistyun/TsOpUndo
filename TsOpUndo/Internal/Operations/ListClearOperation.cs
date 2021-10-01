using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TsOpUndo.Operations;

namespace TsOpUndo.Internal.Operations
{
    internal class ListClearOperation : AbstractOperation
    {
        private readonly IList _list;
        private object[] _prevData;

        public ListClearOperation(IList list) : this(list, list.Cast<object>().ToArray())
        {
        }

        public ListClearOperation(IList list, object[] prevDatas)
        {
            Debug.Assert(list != null);

            _list = list;
            _prevData = prevDatas;
        }

        protected override void DoRollForward()
        {
            _list.Clear();
        }

        protected override void DoRollback()
        {
            Debug.Assert(_prevData != null);

            foreach (var data in _prevData)
                _list.Add(data);
        }
    }


    internal class ListClearOperation<T> : AbstractOperation
    {
        private readonly IList<T> _list;
        private T[] _prevData;

        public ListClearOperation(IList<T> list) : this(list, list.ToArray())
        {
        }

        public ListClearOperation(IList<T> list, T[] prevDatas)
        {
            Debug.Assert(list != null);

            _list = list;
            _prevData = prevDatas;
        }

        protected override void DoRollForward()
        {
            _list.Clear();
        }

        protected override void DoRollback()
        {
            Debug.Assert(_prevData != null);

            foreach (var data in _prevData)
                _list.Add(data);
        }
    }
}
