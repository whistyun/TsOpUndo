using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TsOpUndo.Operations
{
    /// <summary>
    /// 操作を表現するためのインターフェース
    /// </summary>
    public interface IOperation
    {
        /// <summary>
        /// 操作内容を要約した文言
        /// </summary>
        /// <remarks>
        /// このプロパティはユーザーに操作内容を説明するために使用することが可能です。
        /// ライブラリからはこの値をもとに何らかの操作する事はありません。
        /// </remarks>
        string Message { get; set; }

        /// <summary>
        /// 操作の実行
        /// </summary>
        void RollForward();

        /// <summary>
        /// 操作の取消
        /// </summary>
        void Rollback();

        /// <summary>
        /// 操作の実行/取消前に呼び出されます
        /// </summary>
        event Action PreEvent;

        /// <summary>
        /// 操作の実行/取消後に呼び出されます
        /// </summary>
        event Action PostEvent;
    }

    /// <summary>
    /// IOperationの実装を簡略化するためのクラス
    /// </summary>
    public abstract class AbstractOperation : IOperation
    {
        /// <inheritdoc/>
        public string Message { get; set; }

        /// <inheritdoc/>
        public event Action PreEvent;
        /// <inheritdoc/>
        public event Action PostEvent;

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
                PreEvent?.Invoke();
                DoRollback();
            }
            finally
            {
                PostEvent?.Invoke();
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
                PreEvent?.Invoke();
                DoRollForward();
            }
            finally
            {
                PostEvent?.Invoke();
            }
        }

        /// <summary>
        /// 操作の取消
        /// </summary>
        protected abstract void DoRollback();

        /// <summary>
        /// 操作の実行
        /// </summary>
        protected abstract void DoRollForward();
    }
}
