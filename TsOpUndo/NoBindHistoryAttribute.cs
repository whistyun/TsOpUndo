using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TsOpUndo
{
    /// <summary>
    /// 対象のプロパティを監視対象から外します
    /// </summary>
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
    public class NoBindHistoryAttribute : Attribute
    {
        /// <summary>
        /// プロパティのプロパティに対して監視対象とするか設定します。
        /// trueの場合は監視対象とし、falseの場合は監視対象外とします。
        /// </summary>
        public bool AllowBindChild { get; set; }
    }
}
