using System;

namespace TsOpUndo.Internal.Accessors
{
    internal interface IAccessor
    {
        object GetValue(object target);

        object GetValue(object target, int index);

        object GetValue();

        void SetValue(object target, object value);

        void SetValue(object target, int index, object value);

        void SetValue(object value);

        bool HasGetter { get; }
        bool HasSetter { get; }

        Type PropertyType { get; }
    }
}
