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
        event PropertyChangedEventHandler2 PropertyChanged2;
    }

    public delegate void PropertyChangedEventHandler2(object sender, PropertyChangedEvent2Args e);

    public class PropertyChangedEvent2Args : PropertyChangedEventArgs
    {
        public object OldValue { get; }
        public object NewValue { get; }

        public PropertyChangedEvent2Args(string propertyName, object oldV, object newV) : base(propertyName)
        {
            OldValue = oldV;
            NewValue = newV;
        }

    }

    public class GenericNotifyPropertyChanged2 : INotifyPropertyChanged2
    {
        public event PropertyChangedEventHandler2 PropertyChanged2;
        public event PropertyChangedEventHandler PropertyChanged;

        protected Dictionary<string, object> ValueStore = new Dictionary<string, object>();

        protected bool SetValue<V>(V newValue, [CallerMemberName] string propertyName = null)
        {
            V oldValue = GetValue<V>(propertyName);

            if (EqualityComparer<V>.Default.Equals(oldValue, newValue)) return false;


            ValueStore[propertyName] = newValue;

            var ev = new PropertyChangedEvent2Args(propertyName, oldValue, newValue);
            PropertyChanged2?.Invoke(this, ev);
            PropertyChanged?.Invoke(this, ev);

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

    public class NotifyPropertyChanged2 : INotifyPropertyChanged2
    {
        /// <inheritdoc/>
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// プロパティの値変更時に呼び出されます
        /// </summary>
        public event PropertyChangedEventHandler2 PropertyChanged2;

        protected void RaiseFrom(PropertyChangedEvent2Args @base, [CallerMemberName] string propertyName = null)
        {
            var ev = new PropertyChangedEvent2Args(propertyName, @base.OldValue, @base.NewValue);
            PropertyChanged2?.Invoke(this, ev);
            PropertyChanged.Invoke(this, ev);
        }

        /// <summary>
        /// プロパティの値が変更されたことを通知します
        /// </summary>
        protected bool RaiseIfChanged<V>(Action<V> setter, V oldV, V newV, [CallerMemberName] string propertyName = null)
        {
            if (EqualityComparer<V>.Default.Equals(oldV, newV)) return false;

            var ev = new PropertyChangedEvent2Args(propertyName, oldV, newV);
            setter(newV);
            PropertyChanged2?.Invoke(this, ev);
            PropertyChanged.Invoke(this, ev);

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
            PropertyChanged2?.Invoke(this, ev);
            PropertyChanged.Invoke(this, ev);

            return true;
        }

        protected void ChainFrom(INotifyPropertyChanged2 source, string sourceProperty, string propertyName)
        {
            source.PropertyChanged2 += (s, e) =>
            {
                if (String.IsNullOrEmpty(e.PropertyName) || e.PropertyName == sourceProperty)
                {
                    RaiseFrom(e, propertyName);
                }
            };
        }

    }
}
