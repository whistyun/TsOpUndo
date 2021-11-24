using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using TsOpUndo.Internal.Operations;
using TsOpUndo.Operations;

namespace TsOpUndo.Internal.Listeners
{
    internal class FastObjectListListener : ICancellable
    {
        private OperationController _controller;
        private IList _list;
        private object[] _targetBackup;
        private List<ICancellable> _targetListener;
        private bool _enableRecord;

        private PathInfo _listItemPathInfo;

        public FastObjectListListener(OperationController controller, IList list, PathInfo listItemInfo, bool onlyScan)
        {
            _controller = controller;
            _list = list;
            _targetBackup = list.Cast<object>().ToArray();
            _targetListener = new List<ICancellable>();
            _listItemPathInfo = listItemInfo;
            _enableRecord = !onlyScan;

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
            if (e.OldItems != null)
            {
                foreach (var idx in Enumerable.Range(e.OldStartingIndex, e.OldItems.Count).Reverse())
                {
                    _targetListener[idx]?.Cancel();
                    _targetListener.RemoveAt(idx);
                }
            }

            if (e.NewItems != null)
            {
                foreach (var tpl in e.NewItems.Cast<object>().Select((item, index) => new { item, index }))
                {
                    ScanItem(tpl.item, tpl.index + e.NewStartingIndex);
                }
            }

            if (!_controller.IsOperating && _enableRecord)
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

                _controller.Push(operation);
            }
        }

        private void ScanItem(object item, int index = -1)
        {
            ICancellable listener;

            if (item is INotifyPropertyChanged2 notifyObj2)
            {
                listener = new FastObjectListener(_controller, notifyObj2);
            }
            else
            {
                var foListeners =
                _listItemPathInfo.PropChange2ChildPaths
                                 .Select(p => GetOrNull<INotifyPropertyChanged2>(p))
                                 .OfType<INotifyPropertyChanged2>()
                                 .Select(p => new FastObjectListener(_controller, p));

                var folstListeners =
                _listItemPathInfo.ListOfPropChange2ChildPaths
                                 .Select(p => new
                                 {
                                     Path = p.Path,
                                     Info = p.Info,
                                     Value = GetOrNull<INotifyCollectionChanged>(p.Path)
                                 })
                                 .Where(p => p.Value != null)
                                 .Select(p => new FastObjectListListener(
                                     _controller,
                                     new ListWrapper(p.Value),
                                     p.Info,
                                     _listItemPathInfo.IgnorePropertyPaths.Contains(p.Path)));

                var lstListeners =
                _listItemPathInfo.ListChangeChildPaths
                                 .Select(p => GetOrNull<INotifyCollectionChanged>(p))
                                 .OfType<INotifyCollectionChanged>()
                                 .Select(p => new ListListener(_controller, (p as IList) ?? new ListWrapper(p)));

                T GetOrNull<T>(string propPath)
                    => FastReflection.TryGetValue(item, propPath, out T child) ? child : default(T);

                listener = foListeners.Union<ICancellable>(folstListeners).Union(lstListeners).Collect();
            }

            if (index == -1 || index == _targetListener.Count)
                _targetListener.Add(listener);
            else
                _targetListener.Insert(index, listener);
        }

        public void Cancel()
        {
            foreach (var c in _targetListener)
                c?.Cancel();
        }
    }
}
