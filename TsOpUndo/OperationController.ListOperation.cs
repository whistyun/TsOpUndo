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
    public partial class OperationController
    {
        public void ExecuteLegacyAdd(IList list, object value)
        {
            var operation = new ListInsertOperation(list, value);

            Execute(operation);
        }

        public void ExecuteLegacyInsert(IList list, object value, int index)
        {
            var operation = new ListInsertOperation(list, value, index);

            Execute(operation);
        }

        public void ExecuteLegacyAddRange(IList list, IEnumerable value)
        {
            var operation = value.Cast<object>()
                                 .Select(x => new ListInsertOperation(list, x))
                                 .ToCompositeOperation();

            Execute(operation);
        }

        public void ExecuteLegacyRemove(IList list, object value)
        {
            var operation = new ListRemoveOperation(list, value, list.IndexOf(value));

            Execute(operation);
        }

        public void ExecuteLegacyRemoveAt(IList list, int index)
        {
            var operation = new ListRemoveOperation(list, index);

            Execute(operation);
        }

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

        public void ExecuteLegacyClearList(IList list)
        {
            var operation = new ListClearOperation(list);

            Execute(operation);
        }


        public void ExecuteAdd<T>(IList<T> list, T value)
        {
            var operation = new ListInsertOperation<T>(list, value);

            Execute(operation);
        }

        public void ExecuteInsert<T>(IList<T> list, T value, int index)
        {
            var operation = new ListInsertOperation<T>(list, value, index);

            Execute(operation);
        }

        public void ExecuteAddRange<T>(IList<T> list, IEnumerable<T> value)
        {
            var operation = value.Select(x => new ListInsertOperation<T>(list, x))
                                 .ToCompositeOperation();

            Execute(operation);
        }

        public void ExecuteRemove<T>(IList<T> list, T value)
        {
            var operation = new ListRemoveOperation<T>(list, value, list.IndexOf(value));

            Execute(operation);
        }

        public void ExecuteRemoveAt<T>(IList<T> list, int index)
        {
            var operation = new ListRemoveOperation<T>(list, index);

            Execute(operation);
        }

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

        public void ExecuteClearList<T>(IList<T> list)
        {
            var operation = new ListClearOperation<T>(list);

            Execute(operation);
        }
    }
}
