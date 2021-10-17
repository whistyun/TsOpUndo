using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TsOpUndo.Internal;
using TsOpUndo.Internal.Accessors;

namespace TsOpUndo.Operations
{
    public class StaticPropertyOperation : AbstractMergeableOperation
    {
        private IAccessor _accessor;
        private object _nextValue;
        private object _prevValue;

        public StaticPropertyOperation(Type @type, string propertyName, object nextValue) : base(new PropertyKey(@type, propertyName))
        {
            _accessor = FastReflection.GetStaticAccessor(@type, propertyName);
            _nextValue = nextValue;
            _prevValue = _accessor.GetValue();
        }

        public override bool CanMerge(IMergeableOperation operation)
        {
            if (operation is StaticPropertyOperation pop)
            {
                return Object.Equals(pop.GetMergeKey(), GetMergeKey());
            }

            return false;
        }

        protected override void DoMerge(IMergeableOperation nextOperation)
        {
            var spop = (StaticPropertyOperation)nextOperation;
            _nextValue = spop._nextValue;
        }

        protected override void DoRollback()
        {
            _accessor.SetValue(_prevValue);
        }

        protected override void DoRollForward()
        {
            _accessor.SetValue(_nextValue);
        }
    }
}
