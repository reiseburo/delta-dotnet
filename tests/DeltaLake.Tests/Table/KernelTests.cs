using System.Text.RegularExpressions;
using Apache.Arrow;
using Apache.Arrow.Memory;
using Apache.Arrow.Types;
using DeltaLake.Extensions;
using DeltaLake.Interfaces;
using DeltaLake.Table;
using Microsoft.Data.Analysis;
using Polly;

namespace DeltaLake.Tests.Table;

public class KernelTests
{
    private static readonly string stringColumnName = "colStringTest";
    private static readonly string intColumnName = "colIntegerTest";
    private static readonly string partitionStringColumnName = "colPartitionStringTest";
    private static readonly string partitionIntegerColumnName = "colPartitionIntegerTest";
    private const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
    private const int numRetriesOnThrow = 10;
    private static readonly string[] unsafeRetriableErrorsWeMustNotRetryOn = new[]
    {
        @"The metadata of your Delta table couldn't be recovered",
    };
    private static readonly string[] safeRetriableErrorsDeltaRustDoesNotRetryOn = new[]
    {
        @"Failed to read delta log object.*Unable to open file.*\.json.*Access is denied\.",
        @"Failed to commit transaction"
    };

    /// <remarks>
    ///
    /// Re: "Parallelism":
    ///
    /// The test attempts to simulate real-life concurrency situations (parallel writers across processes),
    /// such as performing writes into Delta Delta Partitions and ensure these core scenarios do not regress.
    ///
    /// The test DOES NOT attempt to test concurrency of a singleton <see cref="ITable"/> client etc.,
    /// because both the <see cref="Bridge.Table"/> and the <see cref="Kernel.Core.Table"/> does not have
    /// the locking mechanisms in place (yet) to guarantee safe concurrent access to underlying state and pointers.
    ///
    /// Once we get there, a single <see cref="ITable"/> will be able to perform both concurrent reads and writes
    /// by protecting the underling delta-rs and delta-kernel-rs state with necessary locks.
    ///
    /// </remarks>
    [Fact]
    public async Task Multi_Partitioned_Table_Parallelized_Bridge_Write_Can_Be_Read_By_Kernel()
    {
        // Setup
        //
        int numRowsPerPartition = 10;

        int numPartitions = 3;
        int numWritesPerStringPartition = 3;
        int numWritesPerIntegerPartition = 3;
        int numConcurrentWriters = numPartitions * numWritesPerStringPartition * numWritesPerIntegerPartition;

        int numSerialReadsPerReader = 10;
        int numConcurrentReaders = 5;

        int numRows = numRowsPerPartition * numConcurrentWriters;

        var tempDir = DirectoryHelpers.CreateTempSubdirectory();
        using IEngine engine = new DeltaEngine(EngineOptions.Default);
        var builder = new Apache.Arrow.Schema.Builder()
                                .Field(fb => { fb.Name(stringColumnName); fb.DataType(StringType.Default); fb.Nullable(false); })
                                .Field(fb => { fb.Name(partitionStringColumnName); fb.DataType(StringType.Default); fb.Nullable(false); })
                                .Field(fb => { fb.Name(partitionIntegerColumnName); fb.DataType(Int32Type.Default); fb.Nullable(false); })
                                .Field(fb => { fb.Name(intColumnName); fb.DataType(Int32Type.Default); fb.Nullable(false); });
        var schema = builder.Build();
        int numColumns = schema.FieldsList.Count;
        var tableCreateOptions = new TableCreateOptions(tempDir.FullName, schema)
        {
            Configuration = new Dictionary<string, string> { ["delta.dataSkippingNumIndexedCols"] = "32" },
            PartitionBy = new[] { partitionStringColumnName, partitionIntegerColumnName },
        };
        var tableLoadOptions = new TableOptions() { TableLocation = tempDir.FullName };
        var tableWriteOptions = new InsertOptions { SaveMode = SaveMode.Append };
        var allocator = new NativeMemoryAllocator();
        var randomValueGenerator = new Random();
        var hostNamePrefix = Environment.MachineName;
        AsyncPolicy policy = Policy
            .Handle<Exception>(ex =>
            {
                foreach (var pattern in unsafeRetriableErrorsWeMustNotRetryOn) if (Regex.IsMatch(ex.Message, pattern)) return false;
                foreach (var pattern in safeRetriableErrorsDeltaRustDoesNotRetryOn) if (Regex.IsMatch(ex.Message, pattern)) return true;
                return false;
            })
            .WaitAndRetryAsync(
                numRetriesOnThrow,
                retryAttempt => TimeSpan.FromSeconds(retryAttempt),
                (exception, timeSpan, retryCount, context) => { }
            );

        try
        {
            // Exercise: Parallelized reads via Kernel
            //
            // >>> sharedTable: Simulates the original author of the table, doesn't need to support concurrent creates
            // >>> threadIsolatedTable: Simulates partitioned writers to the table, needs concurrent writes
            //
            using ITable sharedTable = await engine.CreateTableAsync(tableCreateOptions, CancellationToken.None);
            var writeTasks = new List<Task>();
            for (int i = 0; i < numPartitions; i++)
            {
                for (int j = 0; j < numWritesPerStringPartition; j++)
                {
                    for (int k = 0; k < numWritesPerIntegerPartition; k++)
                    {
                        int localI = i;
                        int localJ = j;
                        int localK = k;

                        writeTasks.Add(Task.Run(async () =>
                        {
                            await policy.ExecuteAsync(async () =>
                            {
                                using ITable threadIsolatedTable = await engine.LoadTableAsync(tableLoadOptions, CancellationToken.None);
                                var partition = $"{hostNamePrefix}_{localI}_{localJ}_{localK}";
                                var recordBatchBuilder = new RecordBatch.Builder(allocator)
                                    .Append(stringColumnName, false, col => col.String(arr => arr.AppendRange(Enumerable.Range(0, numRowsPerPartition).Select(_ => GenerateRandomString(randomValueGenerator)))))
                                    .Append(partitionStringColumnName, false, col => col.String(arr => arr.AppendRange(Enumerable.Range(0, numRowsPerPartition).Select(_ => partition))))
                                    .Append(partitionIntegerColumnName, false, col => col.Int32(arr => arr.AppendRange(Enumerable.Range(0, numRowsPerPartition).Select(_ => localI + localJ + localK))))
                                    .Append(intColumnName, false, col => col.Int32(arr => arr.AppendRange(Enumerable.Range(0, numRowsPerPartition).Select(_ => randomValueGenerator.Next()))));
                                await threadIsolatedTable.InsertAsync(new[] { recordBatchBuilder.Build() }, schema, tableWriteOptions, CancellationToken.None);
                            });
                        }));
                    }
                }
            }

            await Task.WhenAll(writeTasks);

            // Exercise: Parallelized writes via Kernel
            //
            // >>> sharedTable: Not used
            // >>> threadIsolatedTable: Simulates parallel readers to the table, needs concurrent reads
            //
            var readTasks = new List<Task>();
            for (int i = 0; i < numConcurrentReaders; i++)
            {
                readTasks.Add(Task.Run(async () =>
                {
                    using ITable threadIsolatedTable = await engine.LoadTableAsync(tableLoadOptions, CancellationToken.None);

                    // Multiple passes here ensures Kernel scan state is reset per read request
                    //
                    for (int j = 0; j < numSerialReadsPerReader; j++)
                    {
                        // Exercise: Reads via Kernel
                        //
                        Apache.Arrow.Table arrowTable = await threadIsolatedTable.ReadAsArrowTableAsync(default);
                        DataFrame dataFrame = await threadIsolatedTable.ReadAsDataFrameAsync(default);
                        string stringResult = dataFrame.ToMarkdown();

                        // Validate: Data Integrity
                        //
                        Assert.Equal(numRows, arrowTable.RowCount);
                        Assert.Equal(numRows, dataFrame.Rows.Count);
                        Assert.Equal(numRows, Regex.Matches(stringResult, hostNamePrefix).Count);
                        Assert.Equal(numColumns, arrowTable.ColumnCount);
                        Assert.Equal(numColumns, dataFrame.Columns.Count);
                        Assert.Equal(numConcurrentWriters, dataFrame[partitionStringColumnName].Cast<string>().Distinct().Count());

                        var writerSchemaFieldMap = schema.FieldsList.ToDictionary(field => field.Name);
                        var kernelSchemaFieldMap = arrowTable.Schema.FieldsList.ToDictionary(field => field.Name);
                        var bridgeSchemaFieldMap = threadIsolatedTable.Schema().FieldsList.ToDictionary(field => field.Name);

                        // Validate: Schema Integrity
                        //
                        Assert.Equal(writerSchemaFieldMap.Count, kernelSchemaFieldMap.Count);
                        Assert.Equal(writerSchemaFieldMap.Count, bridgeSchemaFieldMap.Count);
                        Assert.Equal(writerSchemaFieldMap.Count, numColumns);

                        foreach (var kvp in writerSchemaFieldMap)
                        {
                            Assert.True(bridgeSchemaFieldMap.ContainsKey(kvp.Key));
                            Assert.Equal(kvp.Value.DataType, bridgeSchemaFieldMap[kvp.Key].DataType);
                        }

                        foreach (var kvp in writerSchemaFieldMap)
                        {
                            Assert.True(kernelSchemaFieldMap.ContainsKey(kvp.Key));
                            if (kvp.Key == partitionIntegerColumnName)
                            {
                                // Kernel has a limitation where it can only report back String as the Partition
                                // values:
                                //
                                // >>> https://delta-users.slack.com/archives/C04TRPG3LHZ/p1728178727958499
                                //
                                Assert.Equal(StringType.Default, kernelSchemaFieldMap[kvp.Key].DataType);
                                Assert.Equal(Int32Type.Default, writerSchemaFieldMap[kvp.Key].DataType);
                                continue;
                            }
                            else
                            {
                                Assert.Equal(kvp.Value.DataType, kernelSchemaFieldMap[kvp.Key].DataType);
                            }
                        }
                    }
                }));
            }
            await Task.WhenAll(readTasks);
        }
        finally
        {
            tempDir.Delete(true);
        }
    }

    private static string GenerateRandomString(Random random, int length = 10) => new string(Enumerable.Repeat(chars, length).Select(s => s[random.Next(s.Length)]).ToArray());
}
