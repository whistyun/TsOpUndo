using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using TsOperationHistory.Internal;

namespace TsOperationHistory.Extensions
{
    public static class OperationControllerServiceExtensions
    {
        public static void ExecuteAdd<T>(this IOperationController controller, IList<T> list, T value)
        {
            var operation = list.ToAddOperation(value);
            controller.Execute(operation);
        }

        public static void ExecuteInsert<T>(this IOperationController controller, IList<T> list, T value, int index)
        {
            var operation = new InsertOperation<T>(@list, value, index);
            controller.Execute(operation);
        }

        public static void ExecuteAddRange<T>(this IOperationController controller, IList<T> list, IEnumerable<T> value)
        {
            var operation = list.ToAddRangeOperation(value);
            controller.Execute(operation);
        }

        public static void ExecuteRemove<T>(this IOperationController controller, IList<T> list, T value)
        {
            var operation = list.ToRemoveOperation(value);
            controller.Execute(operation);
        }

        public static void ExecuteRemoveAt<T>(this IOperationController controller, IList<T> list, int index)
        {
            if (list is IList iList)
            {
                var operation = iList.ToRemoveAtOperation(index);
                controller.Execute(operation);
            }
            else
            {
                var target = list[index];
                var operation = list.ToRemoveOperation(target);
                controller.Execute(operation);
            }
        }

        public static void ExecuteRemoveItems<T>(this IOperationController controller, IList<T> list, IEnumerable<T> value)
        {
            var operation = list.ToRemoveRangeOperation(value);
            controller.Execute(operation);
        }

        public static void ExecuteSetProperty<T, TProperty>(this IOperationController controller, T owner, string propertyName, TProperty value)
        {
            var operation = owner
                .GenerateSetPropertyOperation(propertyName, value)
                .Merge(controller);

            controller.Execute(operation);
        }

        public static void ExecuteSetPropertyWithEnforcePropertyType<T, TProperty>(this IOperationController controller, T owner, string propertyName, object value)
        {
            var operation = owner
                .GenerateSetPropertyOperation<TProperty>(propertyName, (TProperty)value)
                .Merge(controller);

            controller.Execute(operation);
        }

        public static void ExecuteSetStaticProperty<TProperty>(this IOperationController controller, Type @class, string propertyName, TProperty value)
        {
            var operation = @class.GenerateSetStaticPropertyOperation(propertyName, value)
                                 .Merge(controller);

            controller.Execute(operation);
        }

        public static void ExecuteDispose<T>(this IOperationController controller, T disposing, Action regenerateAction) where T : IDisposable
        {
            var operation = disposing.ExecuteDispose(regenerateAction);

            controller.Execute(operation);
        }

        public static IDisposable BindListPropertyChanged<T, C>(this IOperationController controller, INotifyPropertyChanged owner, string propertyName, bool autoMerge = true, bool disposable = true)
                where T : IList<C>, INotifyCollectionChanged
        {
            INotifyPropertyChanged obj;
            T prevVal;
            string propNm;

            if (propertyName.Contains("."))
            {
                DescendentPropertyNameChain(owner, propertyName, out obj, out prevVal, out propNm);
            }
            else
            {
                obj = owner;
                prevVal = FastReflection.GetProperty<T>(owner, propertyName);
                propNm = propertyName;

            }

            Func<T> getter = () => FastReflection.GetProperty<T>(obj, propNm);
            var watcher = new ListListner<T, C>(controller, prevVal, propNm, getter);

            obj.PropertyChanged += watcher.PropertyChanged;

            return disposable ?
                new Disposer(() => watcher.Kill()) :
                new Disposer(() => { });

        }

        public static IDisposable BindPropertyChanged<T>(this IOperationController controller, INotifyPropertyChanged owner, string propertyName, bool autoMerge = true, bool disposable = true)
        {
            INotifyPropertyChanged obj;
            T prevVal;
            string propNm;

            if (propertyName.Contains("."))
            {
                DescendentPropertyNameChain(owner, propertyName, out obj, out prevVal, out propNm);
            }
            else
            {
                obj = owner;
                prevVal = FastReflection.GetProperty<T>(owner, propertyName);
                propNm = propertyName;
            }

            var callFromOperation = false;
            obj.PropertyChanged += PropertyChanged;

            return disposable ?
                new Disposer(() => obj.PropertyChanged -= PropertyChanged) :
                new Disposer(() => { });

            // local function
            void PropertyChanged(object sender, PropertyChangedEventArgs args)
            {
                if (args.PropertyName == propNm)
                {
                    T newValue = FastReflection.GetProperty<T>(owner, propertyName);

                    if (callFromOperation)
                    {
                        prevVal = newValue;
                        return;
                    }

                    callFromOperation = true;
                    var operation = owner
                        .GenerateAutoMergeOperation(
                            propertyName, newValue, prevVal,
                            new PropertyBindKey(sender, propertyName),
                            Operation.DefaultMergeSpan);

                    if (autoMerge)
                    {
                        operation = operation.Merge(controller);
                    }

                    prevVal = newValue;

                    controller.Push(operation);
                    callFromOperation = false;
                }
            }
        }

        private static void DescendentPropertyNameChain<T>(INotifyPropertyChanged owner, string propertyName, out INotifyPropertyChanged intermediateValue, out T prevValue, out string bottomLayerPropertyName)
        {
            bottomLayerPropertyName = propertyName.Substring(propertyName.IndexOf(".") + 1);
            intermediateValue = FastReflection.GetProperty<INotifyPropertyChanged>(owner, propertyName.Substring(0, propertyName.IndexOf(".")));
            prevValue = FastReflection.GetProperty<T>(intermediateValue, bottomLayerPropertyName);
            if (bottomLayerPropertyName.IndexOf(".") != -1)
            {
                DescendentPropertyNameChain<T>(intermediateValue, bottomLayerPropertyName, out intermediateValue, out prevValue, out bottomLayerPropertyName);
            }
        }
    }

    class ListListner<T, C> where T : IList<C>, INotifyCollectionChanged
    {
        private bool IsAlive = true;

        private IOperationController Controller;
        private C[] TargetBackup;
        private T TargetList;
        private string TargetProperty;
        private Func<T> Getter;
        private bool callFromOperation = false;
        private string PropNm;

        public ListListner(IOperationController controller, T ownerList, string propNm, Func<T> getter)
        {
            Controller = controller;
            Getter = getter;
            SetTarget(ownerList);

            PropNm = propNm;
        }

        public void UpdateTarget() => SetTarget(Getter.Invoke());

        public void Kill() => IsAlive = false;

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
                TargetBackup = TargetList.ToArray();
                return;
            }

            IOperation operation;
            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    operation = (e.NewStartingIndex + e.NewItems.Count == TargetList.Count) ?
                        TargetList.ToAddRangeOperation(e.NewItems.Cast<C>()) :
                        TargetList.ToInsertRangeOperation(e.NewItems.Cast<C>(), e.NewStartingIndex);
                    break;

                case NotifyCollectionChangedAction.Remove:
                    operation = e.OldItems.Cast<C>()
                                          .Select((x, idx) => new RemoveOperation<C>(TargetList, x, e.OldStartingIndex + idx))
                                          .ToCompositeOperation();
                    break;

                case NotifyCollectionChangedAction.Replace:
                case NotifyCollectionChangedAction.Move:
                    operation = new[]{
                            e.OldItems.Cast<C>()
                                      .Select((x,idx)=> new RemoveOperation<C>(TargetList,x,e.OldStartingIndex+idx ))
                                      .ToCompositeOperation(),
                            TargetList.ToInsertRangeOperation(e.NewItems.Cast<C>(), e.NewStartingIndex)
                        }.ToCompositeOperation();
                    break;

                case NotifyCollectionChangedAction.Reset:
                    operation = new ClearOperation<C>(TargetList, TargetBackup);
                    break;

                default:
                    throw new InvalidOperationException();
            }

            operation = operation
                .AddPreEvent(() => callFromOperation = true)
                .AddPostEvent(() => callFromOperation = false);

            Controller.Push(operation);
            TargetBackup = TargetList.ToArray();
        }

        private void SetTarget(T newOwner)
        {
            if (TargetList != null)
                TargetList.CollectionChanged -= CollectionChanged;

            TargetList = newOwner;
            TargetBackup = newOwner.ToArray();
            TargetList.CollectionChanged += CollectionChanged;
        }
    }
}