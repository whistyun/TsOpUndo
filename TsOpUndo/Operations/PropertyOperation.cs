using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TsOpUndo.Internal;
using TsOpUndo.Internal.Accessors;

namespace TsOpUndo.Operations
{
    public class PropertyOperation : AbstractMergeableOperation
    {
        private IAccessor _accessor;
        private object _owner;
        private object _nextValue;
        private object _prevValue;

        public PropertyOperation(object owner, string propertyName, object nextValue) : base(new PropertyKey(owner, propertyName))
        {
            _owner = owner;
            _accessor = FastReflection.GetAccessor(owner, propertyName);
            _nextValue = nextValue;
            _prevValue = _accessor.GetValue(owner);
        }

        public PropertyOperation(object owner, string propertyName, object prevValue, object nextValue) : base(new PropertyKey(owner, propertyName))
        {
            _owner = owner;
            _accessor = FastReflection.GetAccessor(owner, propertyName);
            _prevValue = prevValue;
            _nextValue = nextValue;
        }

        public override bool CanMerge(IMergeableOperation operation)
        {
            if (operation is PropertyOperation pop)
            {
                return Object.Equals(pop.GetMergeKey(), GetMergeKey());
            }

            return false;
        }

        protected override void DoMerge(IMergeableOperation nextOperation)
        {
            var pop = (PropertyOperation)nextOperation;
            _nextValue = pop._nextValue;
        }

        protected override void DoRollback()
        {
            _accessor.SetValue(_owner, _prevValue);
        }

        protected override void DoRollForward()
        {
            _accessor.SetValue(_owner, _nextValue);
        }
    }
}
