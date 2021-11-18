using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TsOpUndo.Operations
{
    /// <summary>
    /// 実行/前進回帰と、巻き戻し処理をデリゲートとして定義できるクラス
    /// </summary>
    public class DelegateOperation : AbstractOperation
    {
        /// <summary>
        /// 値の変更を操作として定義します。
        /// </summary>
        /// <typeparam name="T">値の型</typeparam>
        /// <param name="setter">値の変更処理</param>
        /// <param name="newValue">変更後の値</param>
        /// <param name="prevValue">変更前の値</param>
        /// <returns>作成された操作</returns>
        public static DelegateOperation CreateFrom<T>(Action<T> setter, T newValue, T prevValue)
        {
            if (setter is null) throw new NullReferenceException(nameof(setter));

            return new DelegateOperation(
                () => setter(newValue),
                () => setter(prevValue)
            );
        }


        private readonly Action _execute;
        private readonly Action _rollback;

        /// <summary>
        /// 実行/前進回帰と、巻き戻し処理をもとにインスタンスを生成
        /// </summary>
        /// <param name="execute">実行/前進回帰</param>
        /// <param name="rollback">巻き戻し処理</param>
        public DelegateOperation(Action execute, Action rollback)
        {
            if (execute is null) throw new NullReferenceException(nameof(execute));
            if (rollback is null) throw new NullReferenceException(nameof(rollback));

            _execute = execute;
            _rollback = rollback;
        }

        /// <inheritdoc/>
        protected override void DoRollForward()
        {
            _execute.Invoke();
        }

        /// <inheritdoc/>
        protected override void DoRollback()
        {
            _rollback.Invoke();
        }
    }
}
