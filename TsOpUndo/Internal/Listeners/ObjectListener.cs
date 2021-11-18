using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Reflection;
using TsOpUndo.Operations;

namespace TsOpUndo.Internal.Listeners
{
    internal class ObjectListener : ICancellable
    {
        public static ICancellable EvaluateListener(OperationController controller, object target)
        {
            if (target is null) return null;
            if (target is string) return null;

            Type targetType = target.GetType();

            if (targetType.IsValueType) return null;

            if (target is INotifyPropertyChanged2 notify2)
            {
                return new ObjectListener(controller, notify2);
            }
            if (typeof(INotifyCollectionChanged).IsAssignableFrom(targetType))
            {
                if (targetType.HasInterface(typeof(IList<>)))
                {
                    var list = new ListWrapper(target);
                    return new ObjectListListener(controller, list);
                }
                else if (typeof(IList).IsAssignableFrom(targetType))
                {
                    var list = (IList)target;
                    return new ObjectListListener(controller, list);
                }
            }
            if (target is IEnumerable enumerable)
            {
                return enumerable.Cast<object>()
                                 .Select(itm => EvaluateListener(controller, itm))
                                 .Where(listener => listener != null)
                                 .CollectOrNull();
            }

            return targetType.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                             .Select(pinf => EvaluateListener(controller, pinf.GetValue(target)))
                             .Where(listener => listener != null)
                             .CollectOrNull();
        }




        private OperationController _controller;
        private INotifyPropertyChanged2 _object;
        private Dictionary<string, List<ICancellable>> _children;

        public ObjectListener(OperationController controller, INotifyPropertyChanged2 vm)
        {
            _controller = controller;
            _object = vm;
            _children = new Dictionary<string, List<ICancellable>>();

            vm.PropertyChanged2 += PropertyChanged2;

            ScanObject(_object);
        }

        private void ScanObject(object targetObj)
        {
            if (targetObj is null) return;

            foreach (var propInfo in targetObj.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                ScanProperty(targetObj, propInfo);
            }
        }

        private void ScanProperty(object targetObject, PropertyInfo propInfo)
        {
            object propVal = propInfo.GetValue(targetObject);
            ICancellable listener = ObjectListener.EvaluateListener(_controller, propVal);
            if (listener is null) return;

            RegisterListener(propInfo.Name, listener);
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

            ScanProperty(_object, _object.GetType().GetProperty(e.PropertyName));

            if (!_controller.IsOperating)
            {
                if (e.IsChained) return;

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
}
