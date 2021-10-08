using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TsOpUndo.Operations
{
    /// <summary>
    /// マージ可能なオペレーションを示します。
    /// </summary>
    /// <remarks>
    /// ここでは、複数のオペレーションを1つのオペレーションに置き換えることをマージとします。
    /// 
    /// 例えば、同一のオブジェクトの同じプロパティに対して値をA→B、B→Cと設定する2つのオペレーションを、
    /// A→Cと1つのオペレーションに纏めます。
    /// </remarks>
    /// 
    public interface IMergeableOperation : IOperation
    {
        /// <summary>
        /// マージ可能か判断します。
        /// </summary>
        /// <param name="operation"></param>
        /// <returns></returns>
        bool CanMerge(IMergeableOperation operation);

        /// <summary>
        /// オペレーションをマージします。
        /// </summary>
        /// <param name="prevMergeJudge">次のオペレーション</param>
        /// <returns></returns>
        void Merge(IMergeableOperation nextOperation);

        /// <summary>
        /// マージのためのKeyを取得。
        /// </summary>
        /// <remarks>
        /// オペレーションがマージ可能か判断するために使用します。
        /// このキーの値が同値の場合、マージ可能と判断されます。
        /// </remarks>
        object GetMergeKey();

        Action[] RegisteredPreEvents { get; }
        Action[] RegisteredPostEvents { get; }
    }

    public abstract class AbstractMergeableOperation : IMergeableOperation
    {
        private object _mergeKey;
        private List<Action> _RegisteredPreEvents = new List<Action>();
        private List<Action> _RegisteredPostEvents = new List<Action>();

        public string Message { get; set; }

        public event Action PreEvent
        {
            add => _RegisteredPreEvents.Add(value);
            remove => _RegisteredPreEvents.Remove(value);
        }

        public event Action PostEvent
        {
            add => _RegisteredPostEvents.Add(value);
            remove => _RegisteredPostEvents.Remove(value);
        }

        public Action[] RegisteredPreEvents
        {
            get => _RegisteredPreEvents.ToArray();
        }

        public Action[] RegisteredPostEvents
        {
            get => _RegisteredPostEvents.ToArray();
        }


        public AbstractMergeableOperation(object mergeKey)
        {
            _mergeKey = mergeKey;
        }

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

        public object GetMergeKey() => _mergeKey;

        private void InvokePreEvent() => _RegisteredPreEvents.ForEach(e => e.Invoke());
        private void InvokePostEvent() => _RegisteredPostEvents.ForEach(e => e.Invoke());

        protected abstract void DoRollback();
        protected abstract void DoRollForward();
        protected abstract void DoMerge(IMergeableOperation nextOperation);

        public abstract bool CanMerge(IMergeableOperation operation);
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
