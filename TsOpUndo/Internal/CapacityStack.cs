using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace TsOpUndo.Internal
{
    /// <summary>
    /// 保有可能数に上限のあるスタックを定義します。
    /// </summary>
    internal class CapacityStack<T> : IStack<T>
    {
        private readonly LinkedList<T> _collection = new LinkedList<T>();

        public CapacityStack(int capacity) { Capacity = capacity; }

        public int Capacity { get; }

        /// <summary>
        /// スタックに値を積みます。保有上限を超える場合は一番下の(最初に登録した)値を削除します
        /// </summary>
        /// <param name="item">登録する値</param>
        /// <returns>登録する値</returns>
        public T Push(T item)
        {
            _collection.AddLast(item);
            if (_collection.Count > Capacity)
                _collection.RemoveFirst();
            return item;
        }

        /// <inheritdoc/>
        public T Peek() => _collection.Last();

        /// <inheritdoc/>
        public T Pop()
        {
            var item = _collection.Last();
            _collection.RemoveLast();
            return item;
        }

        /// <inheritdoc/>
        public void Clear() => _collection.Clear();

        /// <inheritdoc/>
        public IEnumerator<T> GetEnumerator() => _collection.GetEnumerator();

        /// <inheritdoc/>
        IEnumerator IEnumerable.GetEnumerator() => _collection.GetEnumerator();
    }
}
