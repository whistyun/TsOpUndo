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
    /// 操作を履歴として管理し、Undo / Redoを実行するクラス
    /// </summary>
    public partial class OperationController
    {
        /// <summary>
        /// 前回操作と今回操作をマージするか決める閾時間(デフォルト値)
        /// </summary>
        public static TimeSpan DefaultMergeSpan = TimeSpan.FromSeconds(0);

        private readonly UndoStack<IOperation> _undoStack;
        private readonly Stack<UndoStack<IOperation>> _compositeBuilder;
        private DateTime _lastPushed = DateTime.Now;

        private TimeSpan? _mergeSpan;

        /// <summary>
        /// 前回操作と今回操作をマージするか決める閾時間
        /// </summary>
        public TimeSpan MergeSpan
        {
            get => _mergeSpan.HasValue ? _mergeSpan.Value : DefaultMergeSpan;
            set => _mergeSpan = value;
        }

        /// <summary>
        /// 記録する操作数を1024としてインスタンスを作成
        /// </summary>
        public OperationController()
            : this(1024)
        {
        }

        /// <summary>
        /// 記録する操作数を指定してインスタンスを作成
        /// </summary>
        /// <param name="capacity">記録する操作数上限</param>
        public OperationController(int capacity)
        {
            if (capacity < 0) throw new ArgumentException($"{nameof(capacity)} must be positive.");

            _undoStack = new UndoStack<IOperation>(capacity);
            _compositeBuilder = new Stack<UndoStack<IOperation>>();
        }

        /// <summary>
        /// 前の操作(戻せる操作)が残っているか？
        /// </summary>
        public bool HasUndo => _undoStack.HasUndo;

        /// <summary>
        /// 次の操作(やり直せる操作)が残っているか？
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
        /// 最後に記録された操作を取り消します。
        /// </summary>
        public void Undo()
        {
            if (!HasUndo) return;

            PreStackChanged();
            _undoStack.Undo().Rollback();
            OnStackChanged(OperationStackChangedEvent.Undo);
        }

        /// <summary>
        /// 最後に取り消された操作を再実行します。
        /// </summary>
        public void Redo()
        {
            if (!HasRedo) return;

            PreStackChanged();
            _undoStack.Redo().RollForward();
            OnStackChanged(OperationStackChangedEvent.Redo);
        }

        /// <summary>
        /// 記録された操作を全て削除します。
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
        /// 最後に記録された操作を取得します。
        /// </summary>
        /// <returns>最後に記録された操作</returns>
        public IOperation Peek()
        {
            return _compositeBuilder.Count > 0 ?
                _compositeBuilder.Peek().Peek() :
                _undoStack.Peek();
        }

        /// <summary>
        /// 最後に記録された操作を取得し、記録から削除します。
        /// </summary>
        /// <returns>最後に記録された操作。操作は記録から削除されます。</returns>
        public IOperation Pop()
        {
            PreStackChanged();

            var result = _compositeBuilder.Count > 0 ?
                _compositeBuilder.Peek().Pop() :
                _undoStack.Pop();

            OnStackChanged(OperationStackChangedEvent.Pop);

            return result;
        }

        /// <summary>
        /// 操作を記録します。
        /// </summary>
        /// <param name="operation">記録する操作</param>
        /// <returns>記録した操作</returns>
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
        /// 操作を記録します。操作は前回の操作とマージしません。
        /// </summary>
        /// <param name="operation">記録する操作</param>
        /// <returns>記録した操作</returns>
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
        /// 操作を実行し、記録します。
        /// </summary>
        /// <param name="operation">記録する操作</param>
        /// <returns>記録した操作</returns>
        public IOperation Execute(IOperation operation)
        {
            if (operation is null) throw new ArgumentNullException(nameof(operation));

            Push(operation).RollForward();

            return operation;
        }

        /// <summary>
        /// Dispose処理を操作として記録します。
        /// </summary>
        /// <typeparam name="T">Dispose処理をするデータ型</typeparam>
        /// <param name="disposer">Dispose処理をするデータ</param>
        /// <param name="restorePropertyAction">復元処理</param>
        /// <returns>記録した操作</returns>
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
