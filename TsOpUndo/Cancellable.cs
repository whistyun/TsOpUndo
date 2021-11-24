using System;
using System.Collections.Generic;
using System.Linq;

namespace TsOpUndo
{
    /// <summary>
    /// 取り消し可能であることを示す
    /// </summary>
    public interface ICancellable
    {
        /// <summary>
        /// 取り消しを行う
        /// </summary>
        void Cancel();
    }

    /// <summary>
    /// ICancellableの実装を簡略化するためのクラス
    /// </summary>
    public class Cancellable : ICancellable
    {
        private bool _unregistered;
        private Action _action;

        /// <summary>
        /// 指定されたActionを取り消し処理とするインスタンスを生成
        /// </summary>
        /// <param name="act">取り消し処理呼び出し時に実行されるAction</param>
        public Cancellable(Action act)
        {
            _action = act;
        }

        /// <inheritdoc/>
        public void Cancel()
        {
            if (_unregistered) return;

            _action();
            _unregistered = true;
        }
    }

    /// <summary>
    /// ICancellableに関する拡張メソッドをまとめるクラス
    /// </summary>
    public static class CancellableExt
    {
        /// <summary>
        /// 複数のICancellableを一つにまとめます。
        /// この拡張メソッドは(たとえ0個のICancellableを渡したとしても)nullを返しません。
        /// </summary>
        /// <param name="cancellables">まとめる対象</param>
        /// <returns>1つに纏められたICancellable</returns>
        public static ICancellable Collect(this IEnumerable<ICancellable> cancellables)
        {
            return new CancellableCollector(cancellables);
        }

        /// <summary>
        /// 複数のICancellableを一つにまとめます。
        /// この拡張メソッドは、0個のICancellableを渡した場合はnullを返します。
        /// </summary>
        /// <param name="cancellables">まとめる対象</param>
        /// <returns>1つに纏められたICancellable。纏める対象が0個の場合はnull</returns>
        public static ICancellable CollectOrNull(this IEnumerable<ICancellable> cancellables)
        {
            var array = cancellables.ToArray();

            return array.Length != 0 ?
                new CancellableCollector(array) :
                null;
        }
    }

    internal class CancellableCollector : ICancellable
    {
        private ICancellable[] _cancellables;

        internal CancellableCollector(ICancellable[] cancellables)
        {
            _cancellables = cancellables;
        }
        public CancellableCollector(IEnumerable<ICancellable> cancellables) : this(cancellables.ToArray())
        {
        }

        public void Cancel()
        {
            foreach (var c in _cancellables)
                c.Cancel();
        }
    }
}
