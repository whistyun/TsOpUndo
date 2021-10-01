using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using TsOpUndo.Internal;
using TsOpUndo.Internal.Accessors;
using TsOpUndo.Internal.Operations;
using TsOpUndo.Operations;

namespace TsOpUndo
{
    public partial class OperationController
    {
        public void ExecuteSetProperty<T, V>(T owner, Expression<Func<T, V>> selector, V value)
        {
            var propertyName = ((MemberExpression)selector.Body).Member.Name;

            ExecuteSetProperty(owner, propertyName, value);
        }

        public void ExecuteSetProperty<V>(object owner, string propertyName, V value)
        {
            var operation = new PropertyOperation(owner, propertyName, value);

            Execute(operation);
        }

        public void ExecuteSetStaticProperty<V>(Type @class, string propertyName, V value)
        {
            var operation = new StaticPropertyOperation(@class, propertyName, value);

            Execute(operation);
        }

        public IDisposable BindPropertyChangedDisposable(
            INotifyPropertyChanged owner,
            string propertyName,
            bool autoMerge = true)
        {
            var cancellable = BindPropertyChanged(owner, propertyName, autoMerge);
            return new Disposer(() => cancellable.Cancel());
        }

        public IDisposable BindListPropertyChangedDisposable(
                        INotifyPropertyChanged owner,
                        string propertyName,
                        bool autoMerge = true)
        {
            var cancellable = BindListPropertyChanged(owner, propertyName, autoMerge);
            return new Disposer(() => cancellable.Cancel());
        }

        public ICancellable BindPropertyChanged(
            INotifyPropertyChanged owner,
            string propertyName,
            bool autoMerge = true)
        {
            INotifyPropertyChanged obj;
            object prevVal;
            string propNm;

            if (propertyName.Contains("."))
            {
                DescendentPropertyNameChain(owner, propertyName, out obj, out prevVal, out propNm, out var _);
            }
            else
            {
                obj = owner;
                prevVal = FastReflection.GetProperty<object>(owner, propertyName);
                propNm = propertyName;
            }

            var callFromOperation = false;
            obj.PropertyChanged += PropertyChanged;

            return new Cancellable(() => obj.PropertyChanged -= PropertyChanged);

            // local function
            void PropertyChanged(object sender, PropertyChangedEventArgs args)
            {
                if (args.PropertyName == propNm)
                {
                    object newValue = FastReflection.GetProperty<object>(obj, propNm);

                    if (callFromOperation)
                    {
                        prevVal = newValue;
                        return;
                    }

                    var operation = new PropertyOperation(obj, propNm, prevVal, newValue);
                    operation.PreEvent += () => callFromOperation = true; ;
                    operation.PostEvent += () => callFromOperation = false;

                    prevVal = newValue;

                    if (autoMerge)
                    {
                        Push(operation);
                    }
                    else
                    {
                        PushWithoutMerge(operation);
                    }
                }
            }
        }

        public ICancellable BindListPropertyChanged(
                INotifyPropertyChanged owner,
                string propertyName,
                bool autoMerge = true)
        {
            INotifyPropertyChanged obj;
            object prevVal;
            IAccessor valAccessor;
            string propNm;

            if (propertyName.Contains("."))
            {
                DescendentPropertyNameChain(owner, propertyName, out obj, out prevVal, out propNm, out valAccessor);
            }
            else
            {
                obj = owner;
                valAccessor = FastReflection.GetAccessor(owner, propertyName);
                prevVal = valAccessor.GetValue(owner); ;
                propNm = propertyName;
            }

            if (!typeof(INotifyCollectionChanged).IsAssignableFrom(valAccessor.PropertyType))
            {
                throw new ArgumentException($"{propertyName} is not INotifyCollectionChanged.");
            }

            Func<IList> getter;


            if (typeof(IList).IsAssignableFrom(valAccessor.PropertyType))
            {
                // is ICollection
                getter = () => (IList)valAccessor.GetValue(obj);
            }
            else if (valAccessor.PropertyType.FindInterfaces(
                     (t, c) => t.IsGenericType && t.GetGenericTypeDefinition() == typeof(IList<>),
                     null).Length > 0)
            {
                // is ICollection<>
                prevVal = new ListWrapper(prevVal);
                getter = () => new ListWrapper(valAccessor.GetValue(obj));
            }
            else
            {
                throw new ArgumentException("${propertyName} is neither IList nor IList<>.");
            }


            var watcher = new ListListener(this, (IList)prevVal, propNm, getter);

            obj.PropertyChanged += watcher.PropertyChanged;

            return watcher;

        }

        private static void DescendentPropertyNameChain<T>(
            INotifyPropertyChanged owner,
            string propertyName,
            out INotifyPropertyChanged intermediateValue,
            out T prevValue,
            out string bottomLayerPropertyName,
            out IAccessor bottomLayerAccessor)
        {
            string intermediateValueExpr = propertyName.Substring(0, propertyName.LastIndexOf("."));

            bottomLayerPropertyName = propertyName.Substring(intermediateValueExpr.Length + 1);

            intermediateValue = FastReflection.GetProperty<INotifyPropertyChanged>(owner, intermediateValueExpr);

            bottomLayerAccessor = FastReflection.GetAccessor(intermediateValue, bottomLayerPropertyName);
            prevValue = (T)bottomLayerAccessor.GetValue(intermediateValue);
        }
    }
}
