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
    /// クラス変数の値変更を操作として定義するためのクラス
    /// </summary>
    public class StaticPropertyOperation : AbstractMergeableOperation
    {
        private IAccessor _accessor;
        private object _nextValue;
        private object _prevValue;

        /// <summary>
        /// 引数で指定されたクラス変数に対しての値変更を操作としてインスタンス生成。
        /// </summary>
        /// <param name="type">対象のクラス</param>
        /// <param name="propertyName">対象のクラス変数名</param>
        /// <param name="nextValue">変更後の値</param>
        public StaticPropertyOperation(Type @type, string propertyName, object nextValue) : base(new PropertyKey(@type, propertyName))
        {
            _accessor = FastReflection.GetStaticAccessor(@type, propertyName);
            _nextValue = nextValue;
            _prevValue = _accessor.GetValue();
        }

        /// <inheritdoc/>
        public override bool CanMerge(IMergeableOperation operation)
        {
            if (operation is StaticPropertyOperation pop)
            {
                return Object.Equals(pop.GetMergeKey(), GetMergeKey());
            }

            return false;
        }

        /// <inheritdoc/>
        protected override void DoMerge(IMergeableOperation nextOperation)
        {
            var spop = (StaticPropertyOperation)nextOperation;
            _nextValue = spop._nextValue;
        }

        /// <inheritdoc/>
        protected override void DoRollback()
        {
            _accessor.SetValue(_prevValue);
        }

        /// <inheritdoc/>
        protected override void DoRollForward()
        {
            _accessor.SetValue(_nextValue);
        }
    }
}
