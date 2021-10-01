using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TsOpUndo
{
    public interface IRestoreable
    {
        void Restore(Action restorePropertiesAction);
    }
}
