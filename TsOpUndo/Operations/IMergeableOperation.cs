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
    }

    public abstract class AbstractMergeableOperation : IMergeableOperation
    {
        public string Message { get; set; }

        public event Action PreEvent;
        public event Action PostEvent;

        private object _mergeKey;

        public AbstractMergeableOperation(object mergeKey)
        {
            _mergeKey = mergeKey;
        }

        public void Rollback()
        {
            try
            {
                PreEvent?.Invoke();
                DoRollback();
            }
            finally
            {
                PostEvent?.Invoke();
            }
        }

        public void RollForward()
        {
            try
            {
                PreEvent?.Invoke();
                DoRollForward();
            }
            finally
            {
                PostEvent?.Invoke();
            }
        }

        public object GetMergeKey() => _mergeKey;

        protected abstract void DoRollback();
        protected abstract void DoRollForward();

        public abstract bool CanMerge(IMergeableOperation operation);
        public abstract void Merge(IMergeableOperation nextOperation);
    }
}
