﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TsOpUndo.Internal;
using TsOpUndo.Internal.Operations;
using TsOpUndo.Operations;

namespace TsOpUndo
{
    public partial class OperationController
    {
        public IDisposable BeginRecord()
        {
            _compositeBuilder.Push(new UndoStack<IOperation>(1024));

            return new RecordCloser(this, _compositeBuilder.Count);
        }

        class RecordCloser : IDisposable
        {
            OperationController Owner { get; }
            public int Level { get; }

            public RecordCloser(OperationController owner, int level)
            {
                Owner = owner;
                Level = level;
            }

            public void Dispose()
            {
                if (Owner._compositeBuilder.Count != Level)
                    throw new InvalidOperationException();

                var operation = Owner._compositeBuilder.Pop().ToCompositeOperation();
                Owner.Push(operation);
            }
        }
    }

}
