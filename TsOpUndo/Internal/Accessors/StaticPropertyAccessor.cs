using System;

namespace TsOpUndo.Internal.Accessors
{
    internal sealed class StaticPropertyAccessor<TTarget, TProperty> : IAccessor
    {
        private readonly Func<TProperty> _getter;
        private readonly Action<TProperty> _setter;

        public StaticPropertyAccessor(Func<TProperty> getter, Action<TProperty> setter)
        {
            _getter = getter;
            _setter = setter;
        }

        public object GetValue(object target)
        {
            throw new NotSupportedException();
        }

        public object GetValue(object target, int index)
        {
            throw new NotSupportedException();
        }

        public object GetValue()
        {
            return _getter();
        }

        public void SetValue(object target, object value)
        {
            throw new NotSupportedException();
        }

        public void SetValue(object target, int index, object value)
        {
            throw new NotSupportedException();
        }

        public void SetValue(object value)
        {
            _setter((TProperty)value);
        }

        public bool HasGetter => (_getter != null);
        public bool HasSetter => (_setter != null);

        public Type PropertyType { get; } = typeof(TProperty);
    }
}
