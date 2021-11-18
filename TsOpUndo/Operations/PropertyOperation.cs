using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TsOpUndo.Internal;
using TsOpUndo.Internal.Accessors;

namespace TsOpUndo.Operations
{
    /// <summary>
    /// プロパティの値変更を操作として定義するためのクラス
    /// </summary>
    public class PropertyOperation : AbstractMergeableOperation
    {
        private IAccessor _accessor;
        private object _owner;
        private object _nextValue;
        private object _prevValue;

        /// <summary>
        /// 引数で指定されたプロパティに対しての値変更を操作としてインスタンス生成。
        /// </summary>
        /// <param name="owner">対象のオブジェクト</param>
        /// <param name="propertyName">対象のプロパティ</param>
        /// <param name="nextValue">変更後の値</param>
        public PropertyOperation(object owner, string propertyName, object nextValue) : base(new PropertyKey(owner, propertyName))
        {
            _owner = owner;
            _accessor = FastReflection.GetAccessor(owner, propertyName);
            _nextValue = nextValue;
            _prevValue = _accessor.GetValue(owner);
        }

        /// <summary>
        /// 引数で指定されたプロパティに対しての値変更を操作としてインスタンス生成。
        /// </summary>
        /// <param name="owner">対象のオブジェクト</param>
        /// <param name="propertyName">対象のプロパティ</param>
        /// <param name="prevValue">変更前の値</param>
        /// <param name="nextValue">変更後の値</param>
        public PropertyOperation(object owner, string propertyName, object prevValue, object nextValue) : base(new PropertyKey(owner, propertyName))
        {
            _owner = owner;
            _accessor = FastReflection.GetAccessor(owner, propertyName);
            _prevValue = prevValue;
            _nextValue = nextValue;
        }

        /// <inheritdoc/>
        public override bool CanMerge(IMergeableOperation operation)
        {
            if (operation is PropertyOperation pop)
            {
                return Object.Equals(pop.GetMergeKey(), GetMergeKey());
            }

            return false;
        }

        /// <inheritdoc/>
        protected override void DoMerge(IMergeableOperation nextOperation)
        {
            var pop = (PropertyOperation)nextOperation;
            _nextValue = pop._nextValue;
        }

        /// <inheritdoc/>
        protected override void DoRollback()
        {
            _accessor.SetValue(_owner, _prevValue);
        }

        /// <inheritdoc/>
        protected override void DoRollForward()
        {
            _accessor.SetValue(_owner, _nextValue);
        }
    }
}
