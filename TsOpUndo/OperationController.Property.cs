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
using TsOpUndo.Internal.Listeners;
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

            return new PropertyListeners(this, obj, propNm, prevVal, autoMerge);
        }

        public ICancellable BindPropertyChanged2(INotifyPropertyChanged2 owner)
        {
            return new ObjectListener(this, owner);
        }

        public ICancellable BindPropertyChanged2Fast(INotifyPropertyChanged2 owner)
        {
            return new FastObjectListener(this, owner);
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
            else if (valAccessor.PropertyType.HasInterface(typeof(IList<>)))
            {
                // is ICollection<>
                prevVal = new ListWrapper(prevVal);
                getter = () => new ListWrapper(valAccessor.GetValue(obj));
            }
            else
            {
                throw new ArgumentException("${propertyName} is neither IList nor IList<>.");
            }

            return new PropertyListListener(this, owner, (IList)prevVal, propNm, getter);
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
