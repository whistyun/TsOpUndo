using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TsOpUndo.Internal;
using TsOpUndo.Operations;

namespace TsOpUndo
{
    /// <summary>
    /// オペレーションを実行するコントローラ
    /// </summary>
    public partial class OperationController
    {
        public static TimeSpan DefaultMergeSpan = TimeSpan.FromSeconds(0);

        private readonly UndoStack<IOperation> _undoStack;
        private readonly Stack<UndoStack<IOperation>> _compositeBuilder;
        private DateTime _lastPushed = DateTime.Now;

        private TimeSpan? _mergeSpan;
        public TimeSpan MergeSpan
        {
            get => _mergeSpan.HasValue ? _mergeSpan.Value : DefaultMergeSpan;
            set => _mergeSpan = value;
        }


        public OperationController()
            : this(1024)
        {
        }

        public OperationController(int capacity)
        {
            if (capacity < 0) throw new ArgumentException($"{nameof(capacity)} must be positive.");

            _undoStack = new UndoStack<IOperation>(capacity);
            _compositeBuilder = new Stack<UndoStack<IOperation>>();
        }

        /// <summary>
        /// 前の処理(戻せる処理)が残っているか？
        /// </summary>
        public bool HasUndo => _undoStack.HasUndo;

        /// <summary>
        /// 次の処理(やり直せる処理)が残っているか？
        /// </summary>
        public bool HasRedo => _undoStack.HasRedo;

        /// <summary>
        /// 実行された操作一覧を取得する
        /// </summary>
        public IEnumerable<IOperation> Undos => _undoStack;

        /// <summary>
        /// ロールフォワード対象を取得する
        /// </summary>
        public IEnumerable<IOperation> Redos => _undoStack.RedoStack.Reverse();

        /// <summary>
        /// UndoもしくはRedoの処理中か否かを示します。
        /// </summary>
        public bool IsOperating { private set; get; }

        /// <summary>
        /// スタックが更新されたときにイベントが発生する
        /// </summary>
        public event EventHandler<OperationStackChangedEventArgs> StackChanged;

        /// <summary>
        /// 先頭のオペレーションをロールバックする
        /// </summary>
        public void Undo()
        {
            if (!HasUndo) return;

            PreStackChanged();
            _undoStack.Undo().Rollback();
            OnStackChanged(OperationStackChangedEvent.Undo);
        }

        /// <summary>
        /// ロールバックされたオペレーションをロールフォワードする
        /// </summary>
        public void Redo()
        {
            if (!HasRedo) return;

            PreStackChanged();
            _undoStack.Redo().RollForward();
            OnStackChanged(OperationStackChangedEvent.Redo);
        }

        /// <summary>
        /// スタックをクリアする
        /// </summary>
        public void Clear()
        {
            PreStackChanged();

            if (_compositeBuilder.Count > 0)
                _compositeBuilder.Peek().Clear();
            else
                _undoStack.Clear();

            OnStackChanged(OperationStackChangedEvent.Clear);
        }

        /// <summary>
        /// スタックからデータを取り出さずにデータを取得する
        /// </summary>
        public IOperation Peek()
        {
            return _compositeBuilder.Count > 0 ?
                _compositeBuilder.Peek().Peek() :
                _undoStack.Peek();
        }

        /// <summary>
        /// スタックからデータを取り出す
        /// </summary>
        public IOperation Pop()
        {
            PreStackChanged();

            var result = _compositeBuilder.Count > 0 ?
                _compositeBuilder.Peek().Pop() :
                _undoStack.Pop();

            OnStackChanged(OperationStackChangedEvent.Pop);

            return result;
        }

        public IOperation Push(IOperation operation)
        {
            if (operation is null) throw new ArgumentNullException(nameof(operation));

            PreStackChanged();

            var stack = _compositeBuilder.Count > 0 ? _compositeBuilder.Peek() : _undoStack;

            if (DateTime.Now - _lastPushed < MergeSpan
                && operation is IMergeableOperation mop
                && stack.Count > 0
                && stack.Peek() is IMergeableOperation prevMop
                && prevMop.CanMerge(mop))
            {
                prevMop.Merge(mop);
            }
            else
            {
                stack.Push(operation);
                _lastPushed = DateTime.Now;
            }

            OnStackChanged(OperationStackChangedEvent.Push);

            return operation;
        }

        /// <summary>
        /// 実行しないでスタックにデータを積む
        /// </summary>
        public IOperation PushWithoutMerge(IOperation operation)
        {
            if (operation is null) throw new ArgumentNullException(nameof(operation));

            PreStackChanged();

            var stack = _compositeBuilder.Count > 0 ? _compositeBuilder.Peek() : _undoStack;

            stack.Push(operation);

            _lastPushed = DateTime.Now;
            OnStackChanged(OperationStackChangedEvent.Push);

            return operation;
        }

        /// <summary>
        /// 操作を実行し、スタックに積む
        /// </summary>
        public IOperation Execute(IOperation operation)
        {
            if (operation is null) throw new ArgumentNullException(nameof(operation));

            Push(operation).RollForward();

            return operation;
        }

        public IOperation ExecuteDispose<T>(T disposer, Action restorePropertyAction) where T : IDisposable, IRestoreable
        {
            var operation = new DelegateOperation(
                    () => disposer.Dispose(),
                    () => disposer.Restore(restorePropertyAction));

            Push(operation).RollForward();

            return operation;
        }

        private void PreStackChanged()
        {
            Debug.Assert(!IsOperating, "PreStackChanged dupplication call");

            IsOperating = true;
        }

        private void OnStackChanged(OperationStackChangedEvent eventType)
        {
            Debug.Assert(IsOperating, "OnStackChanged dupplication call");

            IsOperating = false;
            StackChanged?.Invoke(this, new OperationStackChangedEventArgs() { EventType = eventType });
        }
    }
}
