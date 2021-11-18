using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace TsOpUndo
{
    public interface INotifyPropertyChanged2 : INotifyPropertyChanged
    {
        /// <summary>
        /// プロパティの値変更時に呼び出されます
        /// </summary>
        event PropertyChangedEventHandler2 PropertyChanged2;
    }

    public delegate void PropertyChangedEventHandler2(object sender, PropertyChangedEvent2Args e);

    public class PropertyChangedEvent2Args : PropertyChangedEventArgs
    {
        public object OldValue { get; }
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

    public class GenericNotifyPropertyChanged2 : AbstractNotifyPropertyChanged2
    {
        protected Dictionary<string, object> ValueStore = new Dictionary<string, object>();

        protected bool SetValue<V>(V newValue, [CallerMemberName] string propertyName = null)
        {
            V oldValue = GetValue<V>(propertyName);

            if (EqualityComparer<V>.Default.Equals(oldValue, newValue)) return false;

            ValueStore[propertyName] = newValue;

            Raise(new PropertyChangedEvent2Args(propertyName, oldValue, newValue));

            return true;
        }

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

        private void RaiseFrom(PropertyChangedEvent2Args @base, string propertyName = null)
        {
            var ev = new PropertyChangedEvent2Args(propertyName, @base.OldValue, @base.NewValue, true);
            Raise(ev);
        }

    }
}
