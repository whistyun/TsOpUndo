using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TsOpUndo.Operations
{
    /// <summary>
    /// マージ可能な操作
    /// </summary>
    /// <remarks>
    /// ここでは、複数の操作を1つの操作に置き換えることをマージとします。
    /// 
    /// 例えば、同一のオブジェクトの同じプロパティに対して値をA→B、B→Cと設定する2つの操作を、
    /// A→Cと1つの操作に纏めます。
    /// </remarks>
    public interface IMergeableOperation : IOperation
    {
        /// <summary>
        /// マージ可能か判断します。
        /// </summary>
        /// <param name="operation"></param>
        /// <returns>マージ可能ならtrue、不可ならfalse</returns>
        bool CanMerge(IMergeableOperation operation);

        /// <summary>
        /// マージします。
        /// </summary>
        /// <param name="nextOperation">次の操作</param>
        void Merge(IMergeableOperation nextOperation);

        /// <summary>
        /// マージのためのKeyを取得。
        /// </summary>
        /// <remarks>
        /// 操作がマージ可能か判断するために使用します。
        /// このキーの値が同値の場合、マージ可能と判断されます。
        /// </remarks>
        object GetMergeKey();

        /// <summary>
        /// PreEventに登録されたAction一覧
        /// </summary>
        Action[] RegisteredPreEvents { get; }

        /// <summary>
        /// PostEventに登録されたAction一覧
        /// </summary>
        Action[] RegisteredPostEvents { get; }
    }

    /// <summary>
    /// IMergeableOperationの実装を簡略化するためのクラス
    /// </summary>
    public abstract class AbstractMergeableOperation : IMergeableOperation
    {
        private object _mergeKey;
        private List<Action> _RegisteredPreEvents = new List<Action>();
        private List<Action> _RegisteredPostEvents = new List<Action>();

        /// <inheritdoc/>
        public string Message { get; set; }

        /// <inheritdoc/>
        public event Action PreEvent
        {
            add => _RegisteredPreEvents.Add(value);
            remove => _RegisteredPreEvents.Remove(value);
        }

        /// <inheritdoc/>
        public event Action PostEvent
        {
            add => _RegisteredPostEvents.Add(value);
            remove => _RegisteredPostEvents.Remove(value);
        }

        /// <inheritdoc/>
        public Action[] RegisteredPreEvents
        {
            get => _RegisteredPreEvents.ToArray();
        }

        /// <inheritdoc/>
        public Action[] RegisteredPostEvents
        {
            get => _RegisteredPostEvents.ToArray();
        }

        /// <summary>
        /// マージのためのキーを指定してインスタンスを生成
        /// </summary>
        /// <param name="mergeKey">マージのためのキー</param>
        public AbstractMergeableOperation(object mergeKey)
        {
            _mergeKey = mergeKey;
        }

        /// <summary>
        /// 操作の取消
        /// </summary>
        /// <remarks>
        /// このメソッドはAbstractOperationにより実装されます。
        /// 継承クラスはDoRollbackを実装してください。
        /// </remarks>
        public void Rollback()
        {
            try
            {
                InvokePreEvent();
                DoRollback();
            }
            finally
            {
                InvokePostEvent();
            }
        }

        /// <summary>
        /// 操作の実行
        /// </summary>
        /// <remarks>
        /// このメソッドはAbstractOperationにより実装されます。
        /// 継承クラスはDoRollForwardを実装してください。
        /// </remarks>
        public void RollForward()
        {
            try
            {
                InvokePreEvent();
                DoRollForward();
            }
            finally
            {
                InvokePostEvent();
            }
        }

        /// <inheritdoc/>
        public object GetMergeKey() => _mergeKey;

        private void InvokePreEvent() => _RegisteredPreEvents.ForEach(e => e.Invoke());
        private void InvokePostEvent() => _RegisteredPostEvents.ForEach(e => e.Invoke());

        /// <summary>
        /// 操作の取消
        /// </summary>
        protected abstract void DoRollback();
        /// <summary>
        /// 操作の実行
        /// </summary>
        protected abstract void DoRollForward();
        /// <summary>
        /// マージします。
        /// </summary>
        /// <param name="nextOperation">次の操作</param>
        protected abstract void DoMerge(IMergeableOperation nextOperation);

        /// <inheritdoc/>
        public abstract bool CanMerge(IMergeableOperation operation);

        /// <inheritdoc/>
        public virtual void Merge(IMergeableOperation nextOperation)
        {
            if (!CanMerge(nextOperation))
                throw new ArgumentException($"{nextOperation} can not be merged");

            _RegisteredPreEvents.AddRange(nextOperation.RegisteredPreEvents);
            _RegisteredPostEvents.InsertRange(0, nextOperation.RegisteredPostEvents);

            DoMerge(nextOperation);
        }
    }
}
