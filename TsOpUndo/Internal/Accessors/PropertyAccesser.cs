using System;

namespace TsOpUndo.Internal.Accessors
{
    internal sealed class PropertyAccessor<TTarget, TProperty> : IAccessor
    {
        private readonly Func<TTarget, TProperty> _getter;
        private readonly Action<TTarget, TProperty> _setter;

        public PropertyAccessor(Func<TTarget, TProperty> getter, Action<TTarget, TProperty> setter)
        {
            _getter = getter;
            _setter = setter;
        }

        public object GetValue(object target)
        {
            if (_getter != null)
                return _getter((TTarget)target);

            return default;
        }

        public object GetValue(object target, int index)
        {
            throw new NotSupportedException();
        }

        public object GetValue()
        {
            throw new NotSupportedException();
        }

        public void SetValue(object target, object value)
        {
            _setter?.Invoke((TTarget)target, (TProperty)value);
        }

        public void SetValue(object target, int index, object value)
        {
            throw new NotSupportedException();
        }

        public void SetValue(object value)
        {
            throw new NotSupportedException();
        }

        public bool HasGetter => (_getter != null);
        public bool HasSetter => (_setter != null);

        public Type PropertyType { get; } = typeof(TProperty);
    }
}
