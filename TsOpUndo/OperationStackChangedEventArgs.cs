using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TsOpUndo
{
    /// <summary>
    /// スタック変更イベントのデータ
    /// </summary>
    public class OperationStackChangedEventArgs : EventArgs
    {
        /// <summary>
        /// OperationControllerのスタック変化が何によって起こったか示すための列挙
        /// </summary>
        public OperationStackChangedEvent EventType { get; set; }
    }
}
