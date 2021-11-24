using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using TsOpUndo.Operations;

namespace TsOpUndo.Internal.Listeners
{
    internal class FastObjectListener : ICancellable
    {
        private OperationController _controller;
        private INotifyPropertyChanged2 _object;
        private PathInfo _objectInfo;

        private Dictionary<string, List<ICancellable>> _children;

        public FastObjectListener(OperationController controller, INotifyPropertyChanged2 vm)
        {
            _controller = controller;
            _object = vm;
            _objectInfo = PathInfo.GetOrInit(vm.GetType());
            _children = new Dictionary<string, List<ICancellable>>();

            Scan();

            vm.PropertyChanged2 += PropertyChanged2;
        }

        private void Scan(string targetProperty = null)
        {
            foreach (var propPath in _objectInfo.PropChange2ChildPaths)
            {
                TreatProperty(propPath,
                    delegate (string basePath, INotifyPropertyChanged2 obj)
                    {
                        RegisterListener(
                            basePath,
                            new FastObjectListener(_controller, obj));
                    }
                );
            }

            foreach (var nmPInf in _objectInfo.ListOfPropChange2ChildPaths)
            {
                var onlyScan = _objectInfo.IgnorePropertyPaths.Contains(nmPInf.Path);

                TreatProperty(nmPInf.Path,
                    delegate (string basePath, INotifyCollectionChanged list)
                    {
                        RegisterListener(
                            basePath,
                            new FastObjectListListener(
                                _controller,
                                new ListWrapper(list),
                                nmPInf.Info,
                                onlyScan));
                    }
                );
            }

            foreach (var propPath in _objectInfo.ListChangeChildPaths)
            {
                if (_objectInfo.IgnorePropertyPaths.Contains(propPath))
                    continue;

                TreatProperty(propPath,
                    delegate (string basePath, INotifyCollectionChanged list)
                    {
                        RegisterListener(
                             basePath,
                             new ListListener(
                                 _controller,
                                 (list as IList) ?? new ListWrapper(list)));
                    }
                );
            }

            void TreatProperty<T>(string propPath, Action<string, T> consumer)
            {
                var baseProp = propPath.Split('.')[0];

                // プロパティ名による絞り込み、
                // 例えば、'Name'で絞り込みする場合は、
                // 'Name'や、'Name.Account'などを抽出し、'Age'などをはじく
                var match = targetProperty is null
                         || targetProperty == baseProp;

                if (!match) return;

                if (FastReflection.TryGetValue(_object, propPath, out T child))
                {
                    consumer(baseProp, child);
                }
            }
        }

        public void Cancel()
        {
            _object.PropertyChanged2 -= PropertyChanged2;

            foreach (var list in _children.Values)
                foreach (var child in list)
                    child.Cancel();

            _children.Clear();
        }

        private void PropertyChanged2(object sender, PropertyChangedEvent2Args e)
        {
            if (_children.TryGetValue(e.PropertyName, out var list))
            {
                foreach (var child in list)
                    child.Cancel();

                list.Clear();
            }

            Scan(e.PropertyName);

            if (!_controller.IsOperating)
            {
                if (e.IsChained) return;

                if (_objectInfo.IgnorePropertyPaths.Contains(e.PropertyName))
                    return;

                var operation = new PropertyOperation(_object, e.PropertyName, e.OldValue, e.NewValue);
                _controller.Push(operation);
            }
        }

        private void RegisterListener(string name, ICancellable listener)
        {
            List<ICancellable> list;
            if (!_children.TryGetValue(name, out list))
            {
                list = new List<ICancellable>();
                _children[name] = list;
            }
            list.Add(listener);
        }
    }

    /// <summary>
    /// ある型が持つプロパティを一覧化するためのクラス
    /// </summary>
    internal class PathInfo
    {
        private static IDictionary<Type, PathInfo> Cache = new Dictionary<Type, PathInfo>();

        /// <summary>
        /// 対象の型
        /// </summary>
        public Type Type { get; }
        /// <summary>
        /// 対象の型がINotifyPropertyChanged2を実装しているか？
        /// </summary>
        public bool IsINotifyPropertyChanged2 { get; }

        /// <summary>
        /// NoBindHistory属性がつけられたプロパティパス一覧
        /// </summary>
        public HashSet<string> IgnorePropertyPaths { get; }
        /// <summary>
        /// INotifyPropertyChanged2を持つプロパティパス一覧
        /// </summary>
        public List<string> PropChange2ChildPaths { get; }
        /// <summary>
        /// INotifyPropertyChanged2のリストを持つプロパティパス一覧
        /// </summary>
        public List<NamedPathInfo> ListOfPropChange2ChildPaths { get; }
        /// <summary>
        /// INotifyCollectionChangedを持つプロパティパス一覧
        /// </summary>
        public List<string> ListChangeChildPaths { get; } = new List<string>();

        public bool HasVariable
        {
            get => IsINotifyPropertyChanged2
                || PropChange2ChildPaths.Count > 0
                || ListOfPropChange2ChildPaths.Count > 0
                || ListChangeChildPaths.Count > 0;
        }


        private PathInfo(Type type, bool isINotifyPropertyChanged2)
        {
            Type = type;
            IsINotifyPropertyChanged2 = isINotifyPropertyChanged2;

            IgnorePropertyPaths = new HashSet<string>();
            PropChange2ChildPaths = new List<string>();
            ListOfPropChange2ChildPaths = new List<NamedPathInfo>();
        }

        private void Init() => ScanOf(null, Type);

        private void ScanOf(string basePath, Type type, Stack<Type> propPathTypes = null)
        {
            if (propPathTypes is null)
                propPathTypes = new Stack<Type>();

            propPathTypes.Push(type);

            var propInfos = type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                            .Where(pinf => pinf.CanWrite && pinf.CanRead)
                            .Where(pinf => pinf.GetGetMethod().GetParameters().Count() == 0);

            foreach (var propInfo in propInfos)
            {
                var propPath = basePath + (basePath is null ? "" : ".") + propInfo.Name;

                var nobindAttr = propInfo.GetCustomAttribute<NoBindHistoryAttribute>();
                if (nobindAttr != null)
                {
                    IgnorePropertyPaths.Add(propPath);

                    if (!nobindAttr.AllowBindChild) continue;
                }

                Type propType = propInfo.PropertyType;
                if (propType.IsValueType) continue;
                if (propType == typeof(string)) continue;

                if (typeof(INotifyPropertyChanged2).IsAssignableFrom(propType))
                {
                    PropChange2ChildPaths.Add(propPath);
                    PathInfo.GetOrInit(propType);
                }
                else if (typeof(INotifyCollectionChanged).IsAssignableFrom(propType))
                {
                    if (propType.HasInterface(typeof(IList<>), out Type propListType))
                    {
                        var componentType = propListType.GetGenericArguments().First();
                        var componentInfo = PathInfo.GetOrInit(componentType);
                        if (componentInfo.HasVariable)
                        {
                            var item = new NamedPathInfo(propPath, componentInfo);
                            ListOfPropChange2ChildPaths.Add(item);
                        }
                        else
                        {
                            ListChangeChildPaths.Add(propPath);
                        }
                    }
                    else
                    {
                        ListChangeChildPaths.Add(propPath);
                    }
                }
                else if (!propPathTypes.Contains(propType))
                {
                    propPathTypes.Push(propType);
                    ScanOf(propPath, propType, propPathTypes);
                    propPathTypes.Pop();
                }
            }
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        public static PathInfo GetOrInit(Type type)
        {
            if (Cache.TryGetValue(type, out var info))
            {
                return info;
            }
            else
            {
                info = new PathInfo(type, typeof(INotifyPropertyChanged2).IsAssignableFrom(type));
                Cache[type] = info;
                info.Init();

                return info;
            }
        }
    }

    internal class NamedPathInfo
    {
        public string Path { get; }
        public PathInfo Info { get; }

        public NamedPathInfo(string path, PathInfo info)
        {
            Path = path;
            Info = info;
        }
    }
}
