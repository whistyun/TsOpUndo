using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TsOpUndo.Operations
{
    /// <summary>
    /// 履歴として保存する1アクション
    /// </summary>
    public interface IOperation
    {
        /// <summary>
        /// メッセージ
        /// </summary>
        string Message { get; set; }

        /// <summary>
        /// 実行 / 前進回帰
        /// </summary>
        void RollForward();

        /// <summary>
        /// ロールバック
        /// </summary>
        void Rollback();

        event Action PreEvent;

        event Action PostEvent;
    }

    public abstract class AbstractOperation : IOperation
    {
        public string Message { get; set; }

        public event Action PreEvent;
        public event Action PostEvent;

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

        protected abstract void DoRollback();
        protected abstract void DoRollForward();
    }
}
