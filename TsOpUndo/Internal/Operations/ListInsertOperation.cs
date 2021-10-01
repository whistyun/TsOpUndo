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
    /// <summary>
    /// 追加オペレーション
    /// </summary>
    internal class ListInsertOperation : AbstractOperation
    {
        private readonly IList _list;
        private readonly object _property;
        private readonly int _insertIndex;

        public ListInsertOperation(IList list, object insertValue, int insertIndex = -1)
        {
            Debug.Assert(list != null);

            _list = list;
            _property = insertValue;
            _insertIndex = insertIndex;
        }

        protected override void DoRollForward()
        {
            if (_insertIndex < 0)
                _list.Add(_property);
            else
                _list.Insert(_insertIndex, _property);
        }

        protected override void DoRollback()
        {
            _list.RemoveAt(_insertIndex < 0 ? _list.Count - 1 : _insertIndex);
        }
    }

    /// <summary>
    /// 追加オペレーション
    /// </summary>
    internal class ListInsertOperation<T> : AbstractOperation
    {
        private readonly IList<T> _list;
        private readonly T _property;
        private readonly int _insertIndex;

        public ListInsertOperation(IList<T> list, T insertValue, int insertIndex = -1)
        {
            Debug.Assert(list != null);

            _list = list;
            _property = insertValue;
            _insertIndex = insertIndex;
        }

        protected override void DoRollForward()
        {
            if (_insertIndex < 0)
                _list.Add(_property);
            else
                _list.Insert(_insertIndex, _property);
        }

        protected override void DoRollback()
        {
            _list.RemoveAt(_insertIndex < 0 ? _list.Count - 1 : _insertIndex);
        }
    }
}
