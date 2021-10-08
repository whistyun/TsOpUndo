using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using TsOpUndo.Internal.Operations;
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

                ScanProperty(_object, _object.GetType().GetProperty(e.PropertyName));
            }

            if (!_controller.IsOperating)
            {
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

    internal class ObjectListListener : ICancellable
    {
        private OperationController _controller;
        private IList _list;
        private object[] _targetBackup;
        private List<ICancellable> _targetListener;
        private List<ICancellable> _siblingListener;


        public ObjectListListener(OperationController controller, IList list)
        {
            _controller = controller;
            _list = list;
            _targetBackup = list.Cast<object>().ToArray();
            _targetListener = new List<ICancellable>();

            if (list is INotifyCollectionChanged notify)
            {
                notify.CollectionChanged += Notify_CollectionChanged;
            }
            else throw new InvalidCastException($"{nameof(list)} is not INotifyCollectionChanged");

            foreach (object item in _targetBackup)
                ScanItem(item);
        }

        private void Notify_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            foreach (var idx in Enumerable.Range(e.OldStartingIndex, e.OldItems.Count).Reverse())
            {
                _targetListener[idx]?.Cancel();
                _targetListener.RemoveAt(idx);
            }

            foreach (var tpl in e.NewItems.Cast<object>().Select((item, index) => new { item, index }))
            {
                ScanItem(tpl.item, tpl.index + e.NewStartingIndex);
            }

            if (!_controller.IsOperating)
            {
                IOperation operation;
                switch (e.Action)
                {
                    case NotifyCollectionChangedAction.Add:
                        operation = (e.NewStartingIndex + e.NewItems.Count == _list.Count) ?
                            e.NewItems.Cast<object>()
                                      .Select(c => new ListInsertOperation(_list, c))
                                      .ToCompositeOperation() :
                            e.NewItems.Cast<object>()
                                      .Select((c, i) => new ListInsertOperation(_list, c, e.NewStartingIndex + i))
                                      .ToCompositeOperation();
                        break;

                    case NotifyCollectionChangedAction.Remove:
                        operation =
                            e.OldItems.Cast<object>()
                                      .Select((c, i) => new ListRemoveOperation(_list, c, e.OldStartingIndex + i))
                                      .Reverse()
                                      .ToCompositeOperation();
                        break;

                    case NotifyCollectionChangedAction.Replace:
                    case NotifyCollectionChangedAction.Move:
                        operation = Enumerable.Union<IOperation>(
                            e.OldItems.Cast<object>()
                                      .Select((c, i) => new ListRemoveOperation(_list, c, e.OldStartingIndex + i))
                                      .Reverse(),
                            e.NewItems.Cast<object>()
                                      .Select((c, i) => new ListInsertOperation(_list, c, e.NewStartingIndex + i))
                        ).ToCompositeOperation();
                        break;

                    case NotifyCollectionChangedAction.Reset:
                        operation = new ListClearOperation(_list, _targetBackup);
                        break;

                    default:
                        throw new InvalidOperationException();
                }
            }
        }

        private void ScanItem(object item, int index = -1)
        {
            ICancellable listener = ObjectListener.EvaluateListener(_controller, item);

            if (index == -1 || index == _targetListener.Count)
                _targetListener.Add(listener);
            else
                _targetListener[index] = listener;
        }

        public void Cancel()
        {
            foreach (var c in _targetListener)
                c?.Cancel();
        }
    }

}
