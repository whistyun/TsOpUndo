using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TsOpUndo.Operations
{
    /// <summary>
    /// 実行/前進回帰と、巻き戻し処理をデリゲートとして定義できるオペレーション
    /// </summary>
    public class DelegateOperation : AbstractOperation
    {
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

        public DelegateOperation(Action execute, Action rollback)
        {
            if (execute is null) throw new NullReferenceException(nameof(execute));
            if (rollback is null) throw new NullReferenceException(nameof(rollback));

            _execute = execute;
            _rollback = rollback;
        }

        protected override void DoRollForward()
        {
            _execute.Invoke();
        }

        protected override void DoRollback()
        {
            _rollback.Invoke();
        }
    }
}
