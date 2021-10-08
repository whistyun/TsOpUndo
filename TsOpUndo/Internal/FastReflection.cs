using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using TsOpUndo.Internal.Accessors;

namespace TsOpUndo.Internal
{
    internal static class FastReflection
    {
        private static readonly ConcurrentDictionary<Type, ConcurrentDictionary<string, IAccessor>> Cache
            = new ConcurrentDictionary<Type, ConcurrentDictionary<string, IAccessor>>();

        public static bool PublicOnly { get; set; } = true;

        private static IMultiLayerAccessor MakeAccessor(object _object, string propertyName)
        {
            propertyName = propertyName.Replace("[", ".[");
            List<IAccessor> list = new List<IAccessor>();
            IAccessor accessor = null;
            object obj = _object;
            foreach (var propertyNameSplit in propertyName.Split('.'))
            {
                var p = propertyNameSplit;
                if (p.First() == '[')
                {
                    p = "Item";
                    var index = int.Parse(propertyNameSplit.Replace("[", "").Replace("]", ""));
                    accessor = CreateIAccessorWithIndex(obj, p);
                    obj = accessor.GetValue(obj, index);
                }
                else if (_object is Type)
                {
                    accessor = CreateIAccessorWithType(obj, p);
                    obj = accessor.GetValue();
                }
                else
                {
                    accessor = CreateIAccessor(obj, p);
                    obj = accessor.GetValue(obj);
                }
                list.Add(accessor);
            }
            return new MultiPropertyAccessor(list);
        }

        private static IAccessor CreateIAccessor(object _object, string propertyNameSplit)
        {
            var propertyInfo = _object.GetType().GetProperty(propertyNameSplit,
                                BindingFlags.NonPublic |
                                BindingFlags.Public |
                                BindingFlags.Instance);

            if (propertyInfo == null)
                return null;

            if (_object.GetType().IsClass == false)
            {
                return new StructAccessor(propertyInfo, PublicOnly);
            }

            var getInfo = propertyInfo.GetGetMethod(PublicOnly is false);
            var setInfo = propertyInfo.GetSetMethod(PublicOnly is false);

            var getterDelegateType = typeof(Func<,>).MakeGenericType(propertyInfo.DeclaringType, propertyInfo.PropertyType);
            var getter = getInfo != null ? Delegate.CreateDelegate(getterDelegateType, getInfo) : null;

            var setterDelegateType = typeof(Action<,>).MakeGenericType(propertyInfo.DeclaringType, propertyInfo.PropertyType);
            var setter = setInfo != null ? Delegate.CreateDelegate(setterDelegateType, setInfo) : null;

            var accessorType = typeof(PropertyAccessor<,>).MakeGenericType(propertyInfo.DeclaringType, propertyInfo.PropertyType);

            return (IAccessor)Activator.CreateInstance(accessorType, getter, setter);
        }

        private static IAccessor CreateIAccessorWithType(object _object, string propertyNameSplit)
        {
            var propertyInfo = (_object as Type).GetProperty(propertyNameSplit,
                                BindingFlags.NonPublic |
                                BindingFlags.Public |
                                BindingFlags.Static);

            if (propertyInfo == null)
                return null;

            if ((_object as Type).IsClass == false)
            {
                return new StructAccessor(propertyInfo, PublicOnly);
            }

            var getInfo = propertyInfo.GetGetMethod(PublicOnly is false);
            var setInfo = propertyInfo.GetSetMethod(PublicOnly is false);

            var getterDelegateType = typeof(Func<>).MakeGenericType(propertyInfo.PropertyType);
            var getter = getInfo != null ? Delegate.CreateDelegate(getterDelegateType, getInfo) : null;

            var setterDelegateType = typeof(Action<>).MakeGenericType(propertyInfo.PropertyType);
            var setter = setInfo != null ? Delegate.CreateDelegate(setterDelegateType, setInfo) : null;

            var accessorType = typeof(StaticPropertyAccessor<,>).MakeGenericType(propertyInfo.DeclaringType, propertyInfo.PropertyType);

            return (IAccessor)Activator.CreateInstance(accessorType, getter, setter);
        }

        private static IAccessor CreateIAccessorWithIndex(object _object, string propertyNameSplit)
        {
            var propertyInfo = _object.GetType().GetProperty(propertyNameSplit,
                                BindingFlags.NonPublic |
                                BindingFlags.Public |
                                BindingFlags.Instance);

            if (propertyInfo == null)
                return null;

            if (_object.GetType().IsClass == false)
            {
                return new StructAccessor(propertyInfo, PublicOnly);
            }

            var getInfo = propertyInfo.GetGetMethod(PublicOnly is false);
            var setInfo = propertyInfo.GetSetMethod(PublicOnly is false);

            var getterDelegateType = typeof(Func<,,>).MakeGenericType(propertyInfo.DeclaringType, typeof(int), propertyInfo.PropertyType);
            var getter = getInfo != null ? Delegate.CreateDelegate(getterDelegateType, getInfo) : null;

            var setterDelegateType = typeof(Action<,,>).MakeGenericType(propertyInfo.DeclaringType, typeof(int), propertyInfo.PropertyType);
            var setter = setInfo != null ? Delegate.CreateDelegate(setterDelegateType, setInfo) : null;

            var accessorType = typeof(IndexerAccessor<,>).MakeGenericType(propertyInfo.DeclaringType, propertyInfo.PropertyType);

            return (IAccessor)Activator.CreateInstance(accessorType, getter, setter);
        }

        public static IAccessor GetAccessor(object _object, string propertyName)
        {
            var accessors = Cache.GetOrAdd(_object.GetType(), x => new ConcurrentDictionary<string, IAccessor>());
            return accessors.GetOrAdd(propertyName, x => MakeAccessor(_object, propertyName));
        }

        public static IAccessor GetStaticAccessor(Type @class, string propertyName)
        {
            var accessors = Cache.GetOrAdd(@class, x => new ConcurrentDictionary<string, IAccessor>());
            return accessors.GetOrAdd(propertyName, x => MakeAccessor(@class, propertyName));
        }

        public static T GetProperty<T>(object _object, string propertyName)
        {
            IAccessor accessor = GetAccessor(_object, propertyName);
            return (T)accessor.GetValue(_object);
        }

        public static bool HasInterface<T>(this Type type) => HasInterface(type, typeof(T));

        public static bool HasInterface<T>(this Type type, out Type filteredType) => HasInterface(type, typeof(T), out filteredType);

        public static bool HasInterface(this Type type, Type checkType) => HasInterface(type, checkType, out var _);

        public static bool HasInterface(this Type type, Type checkType, out Type filteredType)
        {
            TypeFilter filter;
            if (checkType.IsGenericType)
                filter = (t, c) => t.IsGenericType && t.GetGenericTypeDefinition() == checkType;
            else
                filter = (t, c) => t == checkType;

            filteredType = type.FindInterfaces(filter, null).FirstOrDefault();

            return filteredType != null;
        }
    }
}
