using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TsOpUndo.Operations
{
    /// <summary>
    /// 複数のオペレーションを一纏めにし、
    /// 1つのオペレーションとして扱えるようにするためのオペレーション
    /// </summary>
    public class CompositeOperation : AbstractOperation
    {
        private readonly ICollection<IOperation> _operations = new List<IOperation>();

        public IEnumerable<IOperation> Operations => _operations.ToArray();

        public CompositeOperation(params IOperation[] operations)
        {
            if (operations is null) throw new NullReferenceException(nameof(operations));

            Add(operations);
        }


        protected override void DoRollback()
        {
            foreach (var operation in _operations.Reverse())
                operation.Rollback();
        }

        protected override void DoRollForward()
        {
            foreach (var operation in _operations)
                operation.RollForward();
        }

        /// <summary>
        /// オペレーションを追加します
        /// </summary>
        public CompositeOperation Add(IOperation operation)
        {
            if (operation is null) throw new NullReferenceException(nameof(operation));

            _operations.Add(operation);
            return this;
        }

        /// <summary>
        /// オペレーションを追加します
        /// </summary>
        public CompositeOperation Add(params IOperation[] operations)
        {
            if (operations is null) throw new NullReferenceException(nameof(operations));

            foreach (var operation in operations)
                _operations.Add(operation);
            return this;
        }
    }

    public static class CompositeOperationExt {
        /// <summary>
        /// 複数のオペレーションをグループ化して１つのオペレーションに変換する
        /// </summary>
        public static CompositeOperation ToCompositeOperation(this IEnumerable<IOperation> operations)
        {
            return new CompositeOperation(operations.ToArray());
        }
    }
}
