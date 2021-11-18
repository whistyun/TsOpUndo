using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TsOpUndo
{
    /// <summary>
    /// OperationControllerのスタック変化が何によって起こったか示すための列挙
    /// </summary>
    public enum OperationStackChangedEvent
    {
        /// <summary>
        /// Undo処理によってスタックに変更が発生したことを示す
        /// </summary>
        Undo,
        /// <summary>
        /// Redo処理によってスタックに変更が発生したことを示す
        /// </summary>
        Redo,
        /// <summary>
        /// Push呼び出しによってスタックに変更が発生したことを示す
        /// </summary>
        Push,
        /// <summary>
        /// Pop呼び出しによってスタックに変更が発生したことを示す
        /// </summary>
        Pop,
        /// <summary>
        /// スタックのクリアによってスタックに変更が発生したことを示す
        /// </summary>
        Clear,
    }
}
