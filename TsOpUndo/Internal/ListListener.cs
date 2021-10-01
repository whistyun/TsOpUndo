using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TsOpUndo.Internal.Operations;
using TsOpUndo.Operations;

namespace TsOpUndo.Internal
{
    internal class ListListener : ICancellable
    {
        private bool IsAlive = true;

        private OperationController Controller;
        private object[] TargetBackup;
        private IList TargetList;
        private string TargetProperty;
        private Func<IList> Getter;
        private bool callFromOperation = false;
        private string PropNm;

        public ListListener(OperationController controller, IList ownerList, string propNm, Func<IList> getter)
        {
            Controller = controller;
            Getter = getter;
            SetTarget(ownerList);

            PropNm = propNm;
        }

        public void UpdateTarget() => SetTarget(Getter.Invoke());

        private void SetTarget(IList newOwner)
        {
            if (TargetList != null)
                ((INotifyCollectionChanged)TargetList).CollectionChanged -= CollectionChanged;

            TargetList = newOwner;
            TargetBackup = newOwner.Cast<object>().ToArray();
            ((INotifyCollectionChanged)TargetList).CollectionChanged += CollectionChanged;
        }

        public void Cancel() => IsAlive = false;

        public void PropertyChanged(object sender, PropertyChangedEventArgs args)
        {
            if (!IsAlive) return;

            if (args.PropertyName == PropNm)
            {
                UpdateTarget();
            }
        }

        public void CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (!IsAlive) return;

            if (callFromOperation)
            {
                TargetBackup = TargetList.Cast<object>().ToArray();
                return;
            }

            IOperation operation;
            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    operation = (e.NewStartingIndex + e.NewItems.Count == TargetList.Count) ?
                        e.NewItems.Cast<object>()
                                  .Select(c => new ListInsertOperation(TargetList, c))
                                  .ToCompositeOperation() :
                        e.NewItems.Cast<object>()
                                  .Select((c, i) => new ListInsertOperation(TargetList, c, e.NewStartingIndex + i))
                                  .ToCompositeOperation();
                    break;

                case NotifyCollectionChangedAction.Remove:
                    operation =
                        e.OldItems.Cast<object>()
                                  .Select((c, i) => new ListRemoveOperation(TargetList, c, e.OldStartingIndex + i))
                                  .Reverse()
                                  .ToCompositeOperation();
                    break;

                case NotifyCollectionChangedAction.Replace:
                case NotifyCollectionChangedAction.Move:
                    operation = new[]{
                            e.OldItems.Cast<object>()
                                      .Select((c,i)=> new ListRemoveOperation(TargetList,c,e.OldStartingIndex+i ))
                                      .Reverse()
                                      .ToCompositeOperation(),
                            e.NewItems.Cast<object>()
                                      .Select((c, i) => new ListInsertOperation(TargetList, c, e.NewStartingIndex + i))
                                      .ToCompositeOperation()
                        }.ToCompositeOperation();
                    break;

                case NotifyCollectionChangedAction.Reset:
                    operation = new ListClearOperation(TargetList, TargetBackup);
                    break;

                default:
                    throw new InvalidOperationException();
            }

            operation.PreEvent += () => callFromOperation = true;
            operation.PostEvent += () => callFromOperation = false;

            Controller.Push(operation);
            TargetBackup = TargetList.Cast<object>().ToArray();
        }
    }
}
