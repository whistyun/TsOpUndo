using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace TsOpUndo
{
    /// <summary>
    /// 変更前の値を含めてプロパティの変更を通知します。
    /// </summary>
    public interface INotifyPropertyChanged2 : INotifyPropertyChanged
    {
        /// <summary>
        /// プロパティの値が変更されたことを通知します。
        /// </summary>
        event PropertyChangedEventHandler2 PropertyChanged2;
    }

    /// <summary>
    /// INotifyPropertyChanged2.PropertyChanged2をハンドルするためのメソッド
    /// </summary>
    /// <param name="sender">プロパティ変更が起こったオブジェクト</param>
    /// <param name="e">プロパティの値変更に関する情報を含むイベントデータ</param>
    public delegate void PropertyChangedEventHandler2(object sender, PropertyChangedEvent2Args e);

    /// <summary>
    /// プロパティの値変更に関する情報
    /// </summary>
    public class PropertyChangedEvent2Args : PropertyChangedEventArgs
    {
        /// <summary>変更前の値</summary>
        public object OldValue { get; }
        /// <summary>変更後の値</summary>
        public object NewValue { get; }

        /// <summary>他の値変更に関連して発生したイベントであることを示します</summary>
        public bool IsChained { get; }

        /// <summary>
        /// インスタンス生成
        /// </summary>
        /// <param name="propertyName">変更が発生したプロパティの名前</param>
        /// <param name="oldV">変更前の値</param>
        /// <param name="newV">変更後の値</param>
        public PropertyChangedEvent2Args(string propertyName, object oldV, object newV) :
            this(propertyName, oldV, newV, false)
        {
        }

        /// <summary>
        /// 関連したイベントとしてインスタンス生成
        /// </summary>
        /// <param name="propertyName">変更が発生したプロパティの名前</param>
        /// <param name="baseEv">元とするイベント</param>
        public PropertyChangedEvent2Args(string propertyName, PropertyChangedEvent2Args baseEv) :
            this(propertyName, baseEv.OldValue, baseEv.NewValue, true)
        {
        }

        /// <summary>
        /// インスタンス生成
        /// </summary>
        /// <param name="propertyName">変更が発生したプロパティの名前</param>
        /// <param name="oldV">変更前の値</param>
        /// <param name="newV">変更後の値</param>
        /// <param name="isChained">関連したイベントか？</param>
        public PropertyChangedEvent2Args(string propertyName, object oldV, object newV, bool isChained) :
            base(propertyName)
        {
            OldValue = oldV;
            NewValue = newV;
            IsChained = isChained;
        }
    }

    public abstract class AbstractNotifyPropertyChanged2 : INotifyPropertyChanged2
    {
        /// <inheritdoc/>
        public event PropertyChangedEventHandler PropertyChanged;

        /// <inheritdoc/>
        public event PropertyChangedEventHandler2 PropertyChanged2;

        /// <summary>
        /// PropertyChangedイベントとPropertyChanged2イベントを発火します
        /// </summary>
        /// <param name="ev">イベントデータ</param>
        internal void Raise(PropertyChangedEvent2Args ev)
        {
            PropertyChanged2?.Invoke(this, ev);
            PropertyChanged?.Invoke(this, ev);
        }
    }

    /// <summary>
    /// INotifyPropertyChanged2の実装を簡略化するためのクラス
    /// </summary>
    /// <remarks>
    /// このクラスはプロパティの定義をバッキングフィールドを含めて簡略化する事を目的としています。
    /// <code>
    /// class ViewModel: GenericNotifyPropertyChanged2
    /// {
    ///     public string Name
    ///     {
    ///         set => SetValue(value);
    ///         get => GetValue&lt;string&gt;();
    ///     }
    /// }
    /// </code>
    /// </remarks>
    public class GenericNotifyPropertyChanged2 : AbstractNotifyPropertyChanged2
    {
        /// <summary>
        /// バッキングフィールドの代用
        /// </summary>
        protected Dictionary<string, object> ValueStore = new Dictionary<string, object>();

        /// <summary>
        /// 対象のプロパティの値を変更します。
        /// 値に変更があった場合、変更通知を行います。
        /// </summary>
        /// <typeparam name="V">プロパティの型</typeparam>
        /// <param name="newValue">変更後の値</param>
        /// <param name="propertyName">プロパティの名称</param>
        /// <returns>値の変更があった場合はtrue、無い場合はfalse</returns>
        protected bool SetValue<V>(V newValue, [CallerMemberName] string propertyName = null)
        {
            V oldValue = GetValue<V>(propertyName);

            if (EqualityComparer<V>.Default.Equals(oldValue, newValue)) return false;

            ValueStore[propertyName] = newValue;

            Raise(new PropertyChangedEvent2Args(propertyName, oldValue, newValue));

            return true;
        }

        /// <summary>
        /// プロパティの値を取得します
        /// </summary>
        /// <typeparam name="V">プロパティの型</typeparam>
        /// <param name="propertyName">プロパティの名前</param>
        /// <returns>取得したプロパティの値</returns>
        protected V GetValue<V>([CallerMemberName] string propertyName = null)
        {
            if (ValueStore.TryGetValue(propertyName, out var v))
            {
                return (V)v;
            }
            else
            {
                return default(V);
            }
        }
    }

    /// <summary>
    /// INotifyPropertyChanged2の実装を簡略化するためのクラス
    /// </summary>
    public class NotifyPropertyChanged2 : AbstractNotifyPropertyChanged2
    {
        /// <summary>
        /// プロパティの値が変更されたことを通知します
        /// </summary>
        protected bool RaiseIfChanged<V>(Action<V> setter, V oldV, V newV, [CallerMemberName] string propertyName = null)
        {
            if (EqualityComparer<V>.Default.Equals(oldV, newV)) return false;

            var ev = new PropertyChangedEvent2Args(propertyName, oldV, newV);
            setter(newV);
            Raise(ev);

            return true;
        }

        /// <summary>
        /// プロパティの値が変更されたことを通知します
        /// </summary>
        protected bool RaiseIfChanged<V>(ref V property, V newV, [CallerMemberName] string propertyName = null)
        {
            if (EqualityComparer<V>.Default.Equals(property, newV)) return false;

            var ev = new PropertyChangedEvent2Args(propertyName, property, newV);
            property = newV;
            Raise(ev);

            return true;
        }

        /// <summary>
        /// プロパティ変更を連動させます。
        /// 例えば、`ChainFrom(obj, "Prp1", "Prp2")`とした場合、
        /// objのPrp1プロパティの変更が起こった際にPrp2の変更が発生したことを通知します。
        /// </summary>
        /// <param name="source">イベント発生元</param>
        /// <param name="sourcePropertyName">発生元の対象プロパティ名</param>
        /// <param name="propertyName">連動して値変更を通知するプロパティ名</param>
        [Obsolete]
        protected void ChainFrom(INotifyPropertyChanged2 source, string sourcePropertyName, string propertyName)
        {
            source.PropertyChanged2 += (s, e) =>
            {
                if (e.PropertyName == sourcePropertyName)
                {
                    RaiseFrom(e, propertyName);
                }
            };
        }

        /// <summary>
        /// 指定のイベントデータを使用してプロパティ値変更を通知します。
        /// </summary>
        /// <param name="base">イベントデータ</param>
        /// <param name="propertyName">プロパティ名</param>
        private void RaiseFrom(PropertyChangedEvent2Args @base, string propertyName)
        {
            var ev = new PropertyChangedEvent2Args(propertyName, @base.OldValue, @base.NewValue, true);
            Raise(ev);
        }
    }
}
