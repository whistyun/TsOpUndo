using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TsOpUndo
{
    /// <summary>
    /// 復元可能であることを示す
    /// </summary>
    public interface IRestoreable
    {
        /// <summary>
        /// 復元処理を実行します
        /// </summary>
        /// <param name="restorePropertiesAction">復元処理実行時に呼び出す処理</param>
        void Restore(Action restorePropertiesAction);
    }
}
