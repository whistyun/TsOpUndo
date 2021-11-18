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
    /*
     * プロパティの変更・監視に関するメソッドを定義しています
     */

    public partial class OperationController
    {
        /// <summary>
        /// プロパティの値変更を操作として実行・記録します。
        /// </summary>
        /// <typeparam name="T">オブジェクトの型</typeparam>
        /// <typeparam name="V">プロパティの型</typeparam>
        /// <param name="owner">オブジェクト</param>
        /// <param name="selector">プロパティへのアクセス</param>
        /// <param name="value">設定する値</param>
        public void ExecuteSetProperty<T, V>(T owner, Expression<Func<T, V>> selector, V value)
        {
            var propertyName = ((MemberExpression)selector.Body).Member.Name;

            ExecuteSetProperty(owner, propertyName, value);
        }

        /// <summary>
        /// プロパティの値変更を操作として実行・記録します。
        /// </summary>
        /// <typeparam name="V">プロパティの型</typeparam>
        /// <param name="owner">オブジェクト</param>
        /// <param name="propertyName">プロパティ名</param>
        /// <param name="value">設定する値</param>
        public void ExecuteSetProperty<V>(object owner, string propertyName, V value)
        {
            var operation = new PropertyOperation(owner, propertyName, value);

            Execute(operation);
        }

        /// <summary>
        /// クラス変数の値変更を操作として実行・記録します。
        /// </summary>
        /// <typeparam name="V">変更値の型</typeparam>
        /// <param name="class">クラス</param>
        /// <param name="propertyName">クラス変数名</param>
        /// <param name="value">変更後の値</param>
        public void ExecuteSetStaticProperty<V>(Type @class, string propertyName, V value)
        {
            var operation = new StaticPropertyOperation(@class, propertyName, value);

            Execute(operation);
        }

        /// <summary>
        /// プロパティの監視を行います
        /// </summary>
        /// <param name="owner">監視対象のオブジェクト</param>
        /// <param name="propertyName">監視するプロパティの名前</param>
        /// <param name="autoMerge">値変更の操作をマージ可能とするか</param>
        /// <returns>監視を終了する場合Disposeを呼び出してください</returns>
        public IDisposable BindPropertyChangedDisposable(
            INotifyPropertyChanged owner,
            string propertyName,
            bool autoMerge = true)
        {
            var cancellable = BindPropertyChanged(owner, propertyName, autoMerge);
            return new Disposer(() => cancellable.Cancel());
        }

        /// <summary>
        /// リスト変更の監視を行います
        /// </summary>
        /// <param name="owner">監視対象のオブジェクト</param>
        /// <param name="propertyName">監視するプロパティの名前</param>
        /// <param name="autoMerge">値変更の操作をマージ可能とするか</param>
        /// <returns>監視を終了する場合Disposeを呼び出してください</returns>
        public IDisposable BindListPropertyChangedDisposable(
                        INotifyPropertyChanged owner,
                        string propertyName,
                        bool autoMerge = true)
        {
            var cancellable = BindListPropertyChanged(owner, propertyName, autoMerge);
            return new Disposer(() => cancellable.Cancel());
        }

        /// <summary>
        /// プロパティの監視を行います
        /// </summary>
        /// <param name="owner">監視対象のオブジェクト</param>
        /// <param name="propertyName">監視するプロパティの名前</param>
        /// <param name="autoMerge">値変更の操作をマージ可能とするか</param>
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

        /// <summary>
        /// オブジェクトが持つ全てのプロパティを監視します
        /// </summary>
        /// <param name="owner">監視対象のオブジェクト</param>
        /// <returns>監視を終了する場合Cancelを呼び出してください</returns>
        public ICancellable BindPropertyChanged2(INotifyPropertyChanged2 owner)
        {
            return new ObjectListener(this, owner);
        }

        /// <summary>
        /// オブジェクトが持つ全てのプロパティを監視します
        /// </summary>
        /// <param name="owner">監視対象のオブジェクト</param>
        /// <returns>監視を終了する場合Cancelを呼び出してください</returns>
        public ICancellable BindPropertyChanged2Fast(INotifyPropertyChanged2 owner)
        {
            return new FastObjectListener(this, owner);
        }

        /// <summary>
        /// リスト変更の監視を行います
        /// </summary>
        /// <param name="owner">監視対象のオブジェクト</param>
        /// <param name="propertyName">監視するプロパティの名前</param>
        /// <param name="autoMerge">値変更の操作をマージ可能とするか</param>
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
