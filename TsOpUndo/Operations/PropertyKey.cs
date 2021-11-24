using System;

namespace TsOpUndo.Operations
{
    /// <summary>
    /// プロパティ変更の操作のためのマージ用キー
    /// </summary>
    class PropertyKey : IEquatable<PropertyKey>
    {
        object _object;
        string _property;

        public PropertyKey(object o, string property)
        {
            _object = o;
            _property = property;
        }

        public override bool Equals(object obj)
            => Equals(obj as PropertyKey);

        public bool Equals(PropertyKey other)
        {
            if (other is null) return false;

            return Object.Equals(other._object, _object)
                && other._property == _property;
        }

        public override int GetHashCode()
        {
            return unchecked(_object.GetHashCode() + _property.GetHashCode());
        }
    }
}
