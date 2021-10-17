using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace TsOpUndo.Internal
{
    public class ListWrapper : IList, INotifyCollectionChanged
    {
        private object list;
        private INotifyCollectionChanged listNotify;

        private PropertyInfo indexerProp;
        private PropertyInfo isReadOnlyProp;
        private PropertyInfo countProp;

        private MethodInfo addMethod;
        private MethodInfo clearMethod;
        private MethodInfo containsMethod;
        private MethodInfo getEnumeratorMethod;
        private MethodInfo indexOfMethod;
        private MethodInfo insertMethod;
        private MethodInfo removeMethod;
        private MethodInfo removeAtMethod;


        public ListWrapper(object list)
        {
            this.list = list;
            this.listNotify = (INotifyCollectionChanged)list;

            var type = list.GetType();

            if (!type.HasInterface(typeof(IList<>), out var listType)
            || !type.HasInterface(typeof(ICollection<>), out var collectionType)
            || !type.HasInterface(typeof(IEnumerable), out var enumerableType))
            {
                throw new InvalidCastException($"{nameof(list)} should be IList<>");
            }

            // IList<>
            indexerProp = listType.GetProperties().Where(p => p.GetIndexParameters().Length == 1).First();
            indexOfMethod = listType.GetMethod(nameof(List<int>.IndexOf));
            insertMethod = listType.GetMethod(nameof(List<int>.Insert));
            removeAtMethod = listType.GetMethod(nameof(List<int>.RemoveAt));

            // ICollection<>
            addMethod = collectionType.GetMethod(nameof(List<int>.Add));
            clearMethod = collectionType.GetMethod(nameof(List<int>.Clear));
            containsMethod = collectionType.GetMethod(nameof(List<int>.Contains));
            removeMethod = collectionType.GetMethod(nameof(List<int>.Remove));
            countProp = collectionType.GetProperty(nameof(IList<object>.Count));
            isReadOnlyProp = collectionType.GetProperty(nameof(IList<object>.IsReadOnly));

            // IEnumerable
            getEnumeratorMethod = enumerableType.GetMethod(nameof(List<int>.GetEnumerator));
        }

        public object this[int index]
        {
            get => indexerProp.GetValue(list, new object[] { index });
            set => indexerProp.SetValue(list, new object[] { index, value });
        }

        public bool IsReadOnly => (bool)isReadOnlyProp.GetValue(list);

        public bool IsFixedSize => false;

        public int Count => (int)countProp.GetValue(list);

        public object SyncRoot => this;

        public bool IsSynchronized => false;

        public event NotifyCollectionChangedEventHandler CollectionChanged
        {
            add { listNotify.CollectionChanged += value; }
            remove { listNotify.CollectionChanged -= value; }
        }

        public int Add(object value)
        {
            addMethod.Invoke(list, new[] { value });
            return -1;
        }

        public void Clear() => clearMethod.Invoke(list, new object[0]);

        public bool Contains(object value) => (bool)containsMethod.Invoke(list, new object[] { value });

        public void CopyTo(Array array, int index)
        {
            foreach (object o in this)
            {
                array.SetValue(o, index++);
            }
        }

        public IEnumerator GetEnumerator() => (IEnumerator)getEnumeratorMethod.Invoke(list, new object[0]);

        public int IndexOf(object value) => (int)indexOfMethod.Invoke(list, new object[] { value });

        public void Insert(int index, object value) => insertMethod.Invoke(list, new[] { index, value });

        public void Remove(object value) => removeMethod.Invoke(list, new object[] { value });

        public void RemoveAt(int index) => removeAtMethod.Invoke(list, new object[] { index });
    }
}
