using System;

namespace TsOpUndo.Internal.Accessors
{
    internal sealed class IndexerAccessor<TTarget, TProperty> : IAccessor
    {
        private readonly Func<TTarget, int, TProperty> _getter;
        private readonly Action<TTarget, int, TProperty> _setter;

        public IndexerAccessor(Func<TTarget, int, TProperty> getter, Action<TTarget, int, TProperty> setter)
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
            if (_getter != null)
                return _getter((TTarget)target, index);

            return default;
        }

        public object GetValue()
        {
            throw new NotSupportedException();
        }

        public void SetValue(object target, object value)
        {
            throw new NotSupportedException();
        }

        public void SetValue(object target, int index, object value)
        {
            _setter?.Invoke((TTarget)target, index, (TProperty)value);
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