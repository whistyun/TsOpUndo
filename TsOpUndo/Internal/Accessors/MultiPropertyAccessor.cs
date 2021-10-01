using System;
using System.Collections.Generic;
using System.Linq;

namespace TsOpUndo.Internal.Accessors
{
    internal interface IMultiLayerAccessor : IAccessor
    {
        List<IAccessor> AccessorChain { get; }
    }

    internal sealed class MultiPropertyAccessor : IMultiLayerAccessor
    {
        public List<IAccessor> AccessorChain { get; } = new List<IAccessor>();

        public bool HasGetter => AccessorChain.All(x => x.HasGetter);

        public bool HasSetter => AccessorChain.All(x => x.HasSetter);

        public Type PropertyType => AccessorChain.Last().PropertyType;

        public MultiPropertyAccessor(IEnumerable<IAccessor> accessors)
        {
            AccessorChain.AddRange(accessors);
        }

        public object GetValue(object target)
        {
            object obj = target;
            foreach (var chain in AccessorChain)
            {
                obj = chain.GetValue(obj);
            }
            return obj;
        }

        public object GetValue(object target, int index)
        {
            object obj = target;
            foreach (var chain in AccessorChain)
            {
                if (chain != AccessorChain.Last())
                {
                    obj = chain.GetValue(obj);
                }
                else
                {
                    obj = chain.GetValue(obj, index);
                }
            }
            return obj;
        }

        public object GetValue()
        {
            return AccessorChain.First().GetValue();
        }

        public void SetValue(object target, object value)
        {
            object obj = target;
            IAccessor accessor = null;
            foreach (var chain in AccessorChain)
            {
                var temp = chain.GetValue(obj);
                if (chain != AccessorChain.Last())
                {
                    obj = temp;
                }
                accessor = chain;
            }
            accessor.SetValue(obj, value);
        }

        public void SetValue(object target, int index, object value)
        {
            object obj = target;
            IAccessor accessor = null;
            foreach (var chain in AccessorChain)
            {
                if (chain != AccessorChain.Last())
                {
                    obj = chain.GetValue(obj);
                }
                accessor = chain;
            }
            accessor.SetValue(obj, index, value);
        }

        public void SetValue(object value)
        {
            AccessorChain.First().SetValue(value);
        }
    }
}
