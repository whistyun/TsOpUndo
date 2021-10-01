using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TsOpUndo
{
    public enum OperationStackChangedEvent
    {
        Undo,
        Redo,
        Push,
        Pop,
        Clear,
    }
}
