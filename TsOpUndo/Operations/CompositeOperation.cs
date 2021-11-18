using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TsOpUndo.Operations
{
    /// <summary>
    /// 複数の操作を一纏めにし、
    /// 1つの操作として扱えるようにするためのクラス
    /// </summary>
    public class CompositeOperation : AbstractOperation
    {
        private readonly ICollection<IOperation> _operations = new List<IOperation>();

        /// <summary>
        /// 纏められた操作の一覧
        /// </summary>
        public IEnumerable<IOperation> Operations => _operations.ToArray();

        /// <summary>
        /// 複数の操作を一纏めにします
        /// </summary>
        /// <param name="operations">纏める操作</param>
        public CompositeOperation(params IOperation[] operations)
        {
            if (operations is null) throw new NullReferenceException(nameof(operations));

            Add(operations);
        }

        /// <inheritdoc/>
        protected override void DoRollback()
        {
            foreach (var operation in _operations.Reverse())
                operation.Rollback();
        }

        /// <inheritdoc/>
        protected override void DoRollForward()
        {
            foreach (var operation in _operations)
                operation.RollForward();
        }

        /// <summary>
        /// 操作を纏める対象として追加します
        /// </summary>
        public CompositeOperation Add(IOperation operation)
        {
            if (operation is null) throw new NullReferenceException(nameof(operation));

            _operations.Add(operation);
            return this;
        }

        /// <summary>
        /// 操作を纏める対象として追加します
        /// </summary>
        public CompositeOperation Add(params IOperation[] operations)
        {
            if (operations is null) throw new NullReferenceException(nameof(operations));

            foreach (var operation in operations)
                _operations.Add(operation);
            return this;
        }
    }

    /// <summary>
    /// CompositeOperationに関する拡張メソッドを纏めたクラス
    /// </summary>
    public static class CompositeOperationExt
    {
        /// <summary>
        /// 複数のオペレーションをグループ化して１つのオペレーションに変換する
        /// </summary>
        public static CompositeOperation ToCompositeOperation(this IEnumerable<IOperation> operations)
        {
            return new CompositeOperation(operations.ToArray());
        }
    }
}
