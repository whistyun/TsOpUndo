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
        private Type faceType;

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
            this.faceType = list.GetType()
                                .FindInterfaces((t, c) => t.IsGenericType && t.GetGenericTypeDefinition() == typeof(IList<>), null)
                                .First();

            indexerProp = faceType.GetProperties().Where(p => p.GetIndexParameters().Length == 1).First();

            isReadOnlyProp = faceType.GetProperty(nameof(IList<object>.IsReadOnly));
            countProp = faceType.GetProperty(nameof(IList<object>.Count));

            addMethod = faceType.GetMethod(nameof(List<int>.Add));
            clearMethod = faceType.GetMethod(nameof(List<int>.Clear));
            containsMethod = faceType.GetMethod(nameof(List<int>.Contains));
            getEnumeratorMethod = faceType.GetMethod(nameof(List<int>.GetEnumerator));
            indexOfMethod = faceType.GetMethod(nameof(List<int>.IndexOf));
            insertMethod = faceType.GetMethod(nameof(List<int>.Insert));
            removeMethod = faceType.GetMethod(nameof(List<int>.Remove));
            removeAtMethod = faceType.GetMethod(nameof(List<int>.RemoveAt));
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
