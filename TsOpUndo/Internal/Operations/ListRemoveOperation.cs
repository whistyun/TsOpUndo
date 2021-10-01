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
    /// 削除オペレーション
    /// RollBack時に削除位置も復元する
    /// </summary>
    internal class ListRemoveOperation : AbstractOperation
    {
        private readonly IList _list;
        private readonly object _property;
        private int _insertIndex = -1;

        public ListRemoveOperation(IList list, int index) : this(list, list[index], index)
        {
        }

        public ListRemoveOperation(IList list, object removedValue, int valuePosition)
        {
            Debug.Assert(list != null);

            _list = list;
            _property = removedValue;
            _insertIndex = valuePosition;
        }

        protected override void DoRollForward()
        {
            if (_insertIndex == -1)
                _insertIndex = _list.IndexOf(_property);

            _list.RemoveAt(_insertIndex);
        }

        protected override void DoRollback()
        {
            Debug.Assert(_insertIndex != -1);

            _list.Insert(_insertIndex, _property);
        }
    }

    /// <summary>
    /// 削除オペレーション
    /// RollBack時に削除位置も復元する
    /// </summary>
    internal class ListRemoveOperation<T> : AbstractOperation
    {
        private readonly IList<T> _list;
        private readonly T _property;
        private int _insertIndex = -1;

        public ListRemoveOperation(IList<T> list, int index) : this(list, list[index], index)
        {
        }

        public ListRemoveOperation(IList<T> list, T removedValue, int valuePosition)
        {
            Debug.Assert(list != null);

            _list = list;
            _property = removedValue;
            _insertIndex = valuePosition;
        }

        protected override void DoRollForward()
        {
            if (_insertIndex == -1)
                _insertIndex = _list.IndexOf(_property);

            _list.RemoveAt(_insertIndex);
        }

        protected override void DoRollback()
        {
            Debug.Assert(_insertIndex != -1);

            _list.Insert(_insertIndex, _property);
        }
    }
}
