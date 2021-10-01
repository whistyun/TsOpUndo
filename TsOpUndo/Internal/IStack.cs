using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TsOpUndo.Internal
{
    internal interface IStack<T> : IEnumerable<T>
    {
        /// <summary>
        /// スタックに値を積みます
        /// </summary>
        /// <param name="item">登録する値</param>
        /// <returns>登録する値</returns>
        T Push(T item);

        /// <summary>
        /// スタックに値を積まれた最後の(間近に登録した)値を取得します。
        /// このメソッドは、Popと異なり、スタックから値を削除しません。
        /// </summary>
        /// <returns>スタックに値を積まれた最後の(間近に登録した)値</returns>
        T Peek();

        /// <summary>
        /// スタックに値を積まれた最後の(間近に登録した)値を取り出します。
        /// </summary>
        /// <returns>スタックに値を積まれた最後の(間近に登録した)値</returns>
        T Pop();

        /// <summary>
        /// スタックに登録された全ての値を削除します。
        /// </summary>
        void Clear();
    }
}
