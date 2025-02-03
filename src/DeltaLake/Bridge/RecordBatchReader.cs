using System.Collections.Generic;
using System.Threading.Tasks;
using Apache.Arrow;
using Apache.Arrow.Ipc;

namespace DeltaLake.Bridge
{
    internal sealed class RecordBatchReader : IArrowArrayStream
    {
        private readonly IEnumerator<RecordBatch> _enumerator;

        public RecordBatchReader(IEnumerable<RecordBatch> recordBatches, Schema schema)
        {
            _enumerator = recordBatches.GetEnumerator();
            Schema = schema;
        }

        public Schema Schema { get; }

        public void Dispose()
        {
            _enumerator.Dispose();
        }

        public ValueTask<RecordBatch> ReadNextRecordBatchAsync(System.Threading.CancellationToken cancellationToken = default)
        {
            if (_enumerator.MoveNext())
            {
#if NETCOREAPP
                return ValueTask.FromResult(_enumerator.Current);
#else
                return new ValueTask<RecordBatch>(_enumerator.Current);
#endif
            }

#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
#if NETCOREAPP
            return ValueTask.FromResult<RecordBatch>(default);
#else
            return new ValueTask<RecordBatch>(default(RecordBatch));
#endif
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.
        }
    }
}