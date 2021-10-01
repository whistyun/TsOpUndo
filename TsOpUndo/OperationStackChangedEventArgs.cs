using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TsOpUndo
{
    public class OperationStackChangedEventArgs : EventArgs
    {
        public OperationStackChangedEvent EventType { get; set; }
    }
}
