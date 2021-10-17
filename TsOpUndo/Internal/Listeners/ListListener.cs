using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using TsOpUndo.Internal.Operations;
using TsOpUndo.Operations;

namespace TsOpUndo.Internal.Listeners
{
    internal class ListListener : ICancellable
    {
        private OperationController _controller;
        private object[] _targetBackup;
        private IList _targetList;

        public ListListener(OperationController controller, IList ownerList)
        {
            _controller = controller;

            _targetList = ownerList;
            _targetBackup = ownerList.Cast<object>().ToArray();
            ((INotifyCollectionChanged)_targetList).CollectionChanged += CollectionChanged;
        }

        public void Cancel()
        {
            ((INotifyCollectionChanged)_targetList).CollectionChanged -= CollectionChanged;
        }

        public void CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (_controller.IsOperating)
            {
                _targetBackup = _targetList.Cast<object>().ToArray();
                return;
            }

            IOperation operation;
            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    operation = (e.NewStartingIndex + e.NewItems.Count == _targetList.Count) ?
                        e.NewItems.Cast<object>()
                                  .Select(c => new ListInsertOperation(_targetList, c))
                                  .ToCompositeOperation() :
                        e.NewItems.Cast<object>()
                                  .Select((c, i) => new ListInsertOperation(_targetList, c, e.NewStartingIndex + i))
                                  .ToCompositeOperation();
                    break;

                case NotifyCollectionChangedAction.Remove:
                    operation =
                        e.OldItems.Cast<object>()
                                  .Select((c, i) => new ListRemoveOperation(_targetList, c, e.OldStartingIndex + i))
                                  .Reverse()
                                  .ToCompositeOperation();
                    break;

                case NotifyCollectionChangedAction.Replace:
                case NotifyCollectionChangedAction.Move:
                    operation = Enumerable.Union<IOperation>(
                        e.OldItems.Cast<object>()
                                  .Select((c, i) => new ListRemoveOperation(_targetList, c, e.OldStartingIndex + i))
                                  .Reverse(),
                        e.NewItems.Cast<object>()
                                  .Select((c, i) => new ListInsertOperation(_targetList, c, e.NewStartingIndex + i))
                    ).ToCompositeOperation();
                    break;

                case NotifyCollectionChangedAction.Reset:
                    operation = new ListClearOperation(_targetList, _targetBackup);
                    break;

                default:
                    throw new InvalidOperationException();
            }

            _controller.Push(operation);
            _targetBackup = _targetList.Cast<object>().ToArray();
        }
    }
}
