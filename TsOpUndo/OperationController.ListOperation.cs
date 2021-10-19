using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TsOpUndo.Internal.Operations;
using TsOpUndo.Operations;

namespace TsOpUndo
{
    /*
     * リストの変更に関するメソッドを定義しています
     */

    public partial class OperationController
    {
        /// <summary>
        /// リストへの値追加を操作として記録・実行します。
        /// </summary>
        /// <param name="list">追加処理を行うリスト</param>
        /// <param name="value">リストへ追加するデータ</param>
        public void ExecuteLegacyAdd(IList list, object value)
        {
            var operation = new ListInsertOperation(list, value);

            Execute(operation);
        }

        /// <summary>
        /// リストへの値追加を操作として記録・実行します。
        /// </summary>
        /// <param name="list">追加処理を行うリスト</param>
        /// <param name="index">追加位置</param>
        /// <param name="value">リストへ追加するデータ</param>
        public void ExecuteLegacyInsert(IList list, object value, int index)
        {
            var operation = new ListInsertOperation(list, value, index);

            Execute(operation);
        }

        /// <summary>
        /// リストへの値追加(複数)を操作として記録・実行します。
        /// </summary>
        /// <param name="list">追加処理を行うリスト</param>
        /// <param name="value">リストへ追加するデータ</param>
        public void ExecuteLegacyAddRange(IList list, IEnumerable value)
        {
            var operation = value.Cast<object>()
                                 .Select(x => new ListInsertOperation(list, x))
                                 .ToCompositeOperation();

            Execute(operation);
        }

        /// <summary>
        /// リストへの値削除を操作として記録・実行します。
        /// </summary>
        /// <param name="list">削除処理を行うリスト</param>
        /// <param name="value">リストから削除するデータ</param>
        public void ExecuteLegacyRemove(IList list, object value)
        {
            var operation = new ListRemoveOperation(list, value, list.IndexOf(value));

            Execute(operation);
        }

        /// <summary>
        /// リストへの値削除を操作として記録・実行します。
        /// </summary>
        /// <param name="list">削除処理を行うリスト</param>
        /// <param name="index">削除する要素の位置</param>
        public void ExecuteLegacyRemoveAt(IList list, int index)
        {
            var operation = new ListRemoveOperation(list, index);

            Execute(operation);
        }

        /// <summary>
        /// リストへの値削除(複数)を操作として記録・実行します。
        /// </summary>
        /// <param name="list">削除処理を行うリスト</param>
        /// <param name="value">リストから削除する値一覧</param>
        public void ExecuteLegacyRemoveItems(IList list, IEnumerable value)
        {
            using (this.BeginRecord())
            {
                foreach (var v in value)
                {
                    var operation = new ListRemoveOperation(list, v, list.IndexOf(v));
                    Execute(operation);
                }
            }
        }

        /// <summary>
        /// リストのクリアを操作として記録・実行します。
        /// </summary>
        /// <param name="list">クリア処理を行うリスト</param>
        public void ExecuteLegacyClearList(IList list)
        {
            var operation = new ListClearOperation(list);

            Execute(operation);
        }

        /// <summary>
        /// リストへの値追加を操作として記録・実行します。
        /// </summary>
        /// <param name="list">追加処理を行うリスト</param>
        /// <param name="value">リストへ追加するデータ</param>
        /// <typeparam name="T">要素の型</typeparam>
        public void ExecuteAdd<T>(IList<T> list, T value)
        {
            var operation = new ListInsertOperation<T>(list, value);

            Execute(operation);
        }

        /// <summary>
        /// リストへの値追加を操作として記録・実行します。
        /// </summary>
        /// <param name="list">追加処理を行うリスト</param>
        /// <param name="index">追加位置</param>
        /// <param name="value">リストへ追加するデータ</param>
        /// <typeparam name="T">要素の型</typeparam>
        public void ExecuteInsert<T>(IList<T> list, T value, int index)
        {
            var operation = new ListInsertOperation<T>(list, value, index);

            Execute(operation);
        }

        /// <summary>
        /// リストへの値追加(複数)を操作として記録・実行します。
        /// </summary>
        /// <param name="list">追加処理を行うリスト</param>
        /// <param name="value">リストへ追加するデータ</param>
        /// <typeparam name="T">要素の型</typeparam>
        public void ExecuteAddRange<T>(IList<T> list, IEnumerable<T> value)
        {
            var operation = value.Select(x => new ListInsertOperation<T>(list, x))
                                 .ToCompositeOperation();

            Execute(operation);
        }

        /// <summary>
        /// リストへの値削除を操作として記録・実行します。
        /// </summary>
        /// <param name="list">削除処理を行うリスト</param>
        /// <param name="value">リストから削除するデータ</param>
        /// <typeparam name="T">要素の型</typeparam>
        public void ExecuteRemove<T>(IList<T> list, T value)
        {
            var operation = new ListRemoveOperation<T>(list, value, list.IndexOf(value));

            Execute(operation);
        }

        /// <summary>
        /// リストへの値削除を操作として記録・実行します。
        /// </summary>
        /// <param name="list">削除処理を行うリスト</param>
        /// <param name="index">削除する要素の位置</param>
        /// <typeparam name="T">要素の型</typeparam>
        public void ExecuteRemoveAt<T>(IList<T> list, int index)
        {
            var operation = new ListRemoveOperation<T>(list, index);

            Execute(operation);
        }

        /// <summary>
        /// リストへの値削除(複数)を操作として記録・実行します。
        /// </summary>
        /// <param name="list">削除処理を行うリスト</param>
        /// <param name="value">リストから削除する値一覧</param>
        /// <typeparam name="T">要素の型</typeparam>
        public void ExecuteRemoveItems<T>(IList<T> list, IEnumerable<T> value)
        {
            using (this.BeginRecord())
            {
                foreach (var v in value)
                {
                    var operation = new ListRemoveOperation<T>(list, v, list.IndexOf(v));
                    Execute(operation);
                }
            }
        }

        /// <summary>
        /// リストのクリアを操作として記録・実行します。
        /// </summary>
        /// <param name="list">クリア処理を行うリスト</param>
        /// <typeparam name="T">要素の型</typeparam>
        public void ExecuteClearList<T>(IList<T> list)
        {
            var operation = new ListClearOperation<T>(list);

            Execute(operation);
        }
    }
}
