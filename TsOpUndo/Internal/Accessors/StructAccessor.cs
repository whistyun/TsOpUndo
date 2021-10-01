using System;
using System.Reflection;

namespace TsOpUndo.Internal.Accessors
{
    internal sealed class StructAccessor : IAccessor
    {
        private readonly PropertyInfo _propertyInfo;
        public StructAccessor(PropertyInfo propertyInfo, bool publicOnly)
        {
            _propertyInfo = propertyInfo;
            HasGetter = propertyInfo.GetGetMethod(publicOnly is false) != null;
            HasSetter = propertyInfo.GetSetMethod(publicOnly is false) != null;
            PropertyType = _propertyInfo.PropertyType;
        }

        public object GetValue(object target)
        {
            return _propertyInfo.GetValue(target);
        }

        public void SetValue(object target, object value)
        {
            _propertyInfo.SetValue(target, value);
        }

        public object GetValue(object target, int index)
        {
            throw new NotSupportedException();
        }

        public object GetValue()
        {
            throw new NotSupportedException();
        }

        public void SetValue(object target, int index, object value)
        {
            throw new NotSupportedException();
        }

        public void SetValue(object value)
        {
            throw new NotSupportedException();
        }

        public bool HasGetter { get; }

        public bool HasSetter { get; }

        public Type PropertyType { get; }
    }
}