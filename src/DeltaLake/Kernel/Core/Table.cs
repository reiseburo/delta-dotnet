// -----------------------------------------------------------------------------
// <summary>
// The Kernel representation of a Delta Table, contains the memory management
// and lifecycle of the table on both sides (C# and Kernel).
// </summary>
//
// <copyright company="The Delta Lake Project Authors">
// Copyright (2024) The Delta Lake Project Authors.  All rights reserved.
// Licensed under the Apache license. See LICENSE file in the project root for full license information.
// </copyright>
// -----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Apache.Arrow;
using DeltaLake.Extensions;
using DeltaLake.Kernel.Arrow.Extensions;
using DeltaLake.Kernel.Callbacks.Allocators;
using DeltaLake.Kernel.Callbacks.Errors;
using DeltaLake.Kernel.Callbacks.Visit;
using DeltaLake.Kernel.Interop;
using DeltaLake.Kernel.State;
using DeltaLake.Table;
using Microsoft.Data.Analysis;
using static DeltaLake.Kernel.Callbacks.Visit.VisitCallbacks;
using DeltaRustBridge = DeltaLake.Bridge;
using ICancellationToken = System.Threading.CancellationToken;

namespace DeltaLake.Kernel.Core
{
    /// <summary>
    /// Reference to unmanaged delta table.
    ///
    /// The idea is, we prioritize the Kernel FFI implementations, and
    /// operations not supported by the FFI yet falls back to Delta RS Runtime
    /// implementation.
    /// </summary>
    internal class Table : DeltaRustBridge.Table
    {
        /// <summary>
        /// Behavioral flags.
        /// </summary>
        private bool isKernelSupported;
        private readonly bool isKernelAllocated;

        /// <summary>
        /// Regular managed objects.
        /// </summary>
        private readonly TableStorageOptions tableStorageOptions;

        /// <summary>
        /// Kernel InterOp objects.
        /// </summary>
        private readonly KernelStringSlice tableLocationSlice;
        private readonly KernelStringSlice[] storageOptionsKeySlices;
        private readonly KernelStringSlice[] storageOptionsValueSlices;
        private readonly ExternResultHandleSharedExternEngine sharedExternEngine;

        /// <summary>
        /// Disposable managed objects.
        /// </summary>
        /// <remarks>
        /// It is our responsibility to dispose of these alongside this <see cref="Table"/> class.
        /// </remarks>
        private readonly ISafeState state;

        /// <summary>
        /// Pointers **WE** manage alongside this <see cref="Table"/> class.
        /// </summary>
        /// <remarks>
        /// It is our responsibility to release these pointers via the paired GC
        /// handles via <see cref="GCHandle.Free()"/>.
        /// </remarks>
        private readonly unsafe sbyte* gcPinnedTableLocationPtr;
        private readonly unsafe sbyte** gcPinnedStorageOptionsKeyPtrs;
        private readonly unsafe sbyte** gcPinnedStorageOptionsValuePtrs;

        private readonly GCHandle tableLocationHandle;
        private readonly GCHandle[] storageOptionsKeyHandles;
        private readonly GCHandle[] storageOptionsValueHandles;

        /// <summary>
        /// Pointers **KERNEL** manages related to this <see cref="Table"/> class.
        /// </summary>
        /// <remarks>
        /// It is our responsibility to ask Kernel to release these pointers
        /// when <see cref="Table"/> class is disposed.
        /// </remarks>
        private readonly unsafe EngineBuilder* kernelOwnedEngineBuilderPtr;
        private readonly unsafe SharedExternEngine* kernelOwnedSharedExternEnginePtr;

        /// <summary>
        /// Initializes a new instance of the <see cref="Table"/> class.
        /// </summary>
        /// <remarks>
        /// If the user passes in a version at load time, Kernel cannot be used.
        /// </remarks>
        /// <param name="table">The Delta Rust table.</param>
        /// <param name="options">The table options.</param>
        internal unsafe Table(DeltaRustBridge.Table table, TableOptions options)
            : this(table, options, options.IsKernelSupported()) { }

        /// <summary>
        /// Initializes a new instance of the <see cref="Table"/> class.
        /// </summary>
        /// <param name="table">The Delta Rust table.</param>
        /// <param name="tableStorageOptions">The table storage options.</param>
        /// <param name="useKernel">Whether to use the Kernel or not.</param>
        internal unsafe Table(DeltaRustBridge.Table table, TableStorageOptions tableStorageOptions, Boolean useKernel = true)
            : base(table._runtime, table._ptr)
        {
            this.tableStorageOptions = tableStorageOptions;
            this.isKernelSupported = useKernel && tableStorageOptions.IsKernelSupported();

            if (this.isKernelSupported)
            {
                // Kernel String Slice is used to communicate the table location.
                //
                (GCHandle handle, IntPtr ptr) = tableStorageOptions.TableLocation.ToPinnedSBytePointer();
                this.tableLocationHandle = handle;
                this.gcPinnedTableLocationPtr = (sbyte*)ptr.ToPointer();
                this.tableLocationSlice = new KernelStringSlice { ptr = this.gcPinnedTableLocationPtr, len = (nuint)tableStorageOptions.TableLocation.Length };

                // Shared engine is the core runtime at the Kernel, tied to this table,
                // it is managed by the Kernel, but our responsibility to release it.
                //
                ExternResultEngineBuilder engineBuilder = Methods.get_engine_builder(this.tableLocationSlice, Marshal.GetFunctionPointerForDelegate<AllocateErrorFn>(AllocateErrorCallbacks.ThrowAllocationError));
                if (engineBuilder.tag != ExternResultEngineBuilder_Tag.OkEngineBuilder)
                {
                    throw new InvalidOperationException("Could not initiate engine builder from Delta Kernel");
                }
                this.kernelOwnedEngineBuilderPtr = engineBuilder.Anonymous.Anonymous1.ok;

                // The joys of unmanaged code, this is all to pass some Key:Value string pairs
                // to the Kernel's Engine Builder (e.g. Storage Account/S3 Keys etc.).
                //
                int index = 0;
                int count = tableStorageOptions.StorageOptions.Count;
                this.storageOptionsKeyHandles = new GCHandle[count];
                this.storageOptionsValueHandles = new GCHandle[count];
                this.gcPinnedStorageOptionsKeyPtrs = (sbyte**)Marshal.AllocHGlobal(count * sizeof(sbyte*));
                this.gcPinnedStorageOptionsValuePtrs = (sbyte**)Marshal.AllocHGlobal(count * sizeof(sbyte*));
                this.storageOptionsKeySlices = new KernelStringSlice[count];
                this.storageOptionsValueSlices = new KernelStringSlice[count];

                foreach (KeyValuePair<string, string> kvp in tableStorageOptions.StorageOptions)
                {
                    (GCHandle keyHandle, IntPtr keyPtr) = kvp.Key.ToPinnedSBytePointer();
                    (GCHandle valueHandle, IntPtr valuePtr) = kvp.Value.ToPinnedSBytePointer();

                    this.storageOptionsKeyHandles[index] = keyHandle;
                    this.storageOptionsValueHandles[index] = valueHandle;
                    this.gcPinnedStorageOptionsKeyPtrs[index] = (sbyte*)keyPtr.ToPointer();
                    this.gcPinnedStorageOptionsValuePtrs[index] = (sbyte*)valuePtr.ToPointer();
                    this.storageOptionsKeySlices[index] = new KernelStringSlice { ptr = this.gcPinnedStorageOptionsKeyPtrs[index], len = (nuint)kvp.Key.Length };
                    this.storageOptionsValueSlices[index] = new KernelStringSlice { ptr = this.gcPinnedStorageOptionsValuePtrs[index], len = (nuint)kvp.Value.Length };

                    Methods.set_builder_option(this.kernelOwnedEngineBuilderPtr, this.storageOptionsKeySlices[index], this.storageOptionsValueSlices[index]);

                    index++;
                }

                this.sharedExternEngine = Methods.builder_build(this.kernelOwnedEngineBuilderPtr);
                if (this.sharedExternEngine.tag != ExternResultHandleSharedExternEngine_Tag.OkHandleSharedExternEngine)
                {
                    throw new InvalidOperationException("Could not build engine from the engine builder sent to Delta Kernel.");
                }
                this.kernelOwnedSharedExternEnginePtr = this.sharedExternEngine.Anonymous.Anonymous1.ok;
                this.state = new ManagedTableState(this.tableLocationSlice, this.kernelOwnedSharedExternEnginePtr);
                this.isKernelAllocated = true;
            }
        }

        #region Delta Kernel table operations

        internal (Apache.Arrow.Schema, List<RecordBatch>) ReadAsRecordBatchesAndSchema()
        {
            if (!this.isKernelAllocated || !this.isKernelSupported)
            {
                // There's currently no direct equivalent to this in delta-rs,
                // so we throw if the Kernel is not being used.
                //
                throw new InvalidOperationException("Direct read operation is not supported without using the Delta Kernel.");
            }

            unsafe
            {
                // Refresh the necessary Kernel state together before initiating
                // the scan.
                //
                SharedSnapshot* managedSnapshotPtr = this.state.Snapshot(true);
                SharedSchema* managedSchemaPtr = this.state.Schema(true);
                PartitionList* managedPartitionListPtr = this.state.PartitionList(true);
                SharedScan* managedScanPtr = this.state.Scan(true);
                SharedGlobalScanState* managedGlobalScanStatePtr = this.state.GlobalScanState(true);

                // Memory scoped to this ReadAsArrowTable operation
                //
                SharedScanDataIterator* kernelOwnedScanDataIteratorPtr = null;
                IntPtr tableRootPtr = (IntPtr)Methods.snapshot_table_root(managedSnapshotPtr, Marshal.GetFunctionPointerForDelegate<AllocateStringFn>(StringAllocatorCallbacks.AllocateString));

                // We don't explicitly pin the memory for these complex objects,
                // since they will not be GC-ed until the vars go out of scope
                // of this method, which encapsulates the complete scan
                // operation.
                //
                ArrowContext methodScopedArrowContext = new();
                ArrowContext* arrowContextPtr = &methodScopedArrowContext;

                EngineContext methodScopedEngineContext = new()
                {
                    GlobalScanState = managedGlobalScanStatePtr,
                    Schema = managedSchemaPtr,
                    TableRoot = (char*)tableRootPtr,
                    Engine = this.kernelOwnedSharedExternEnginePtr,
                    PartitionList = managedPartitionListPtr,
                    PartitionValues = null,
                    ArrowContext = arrowContextPtr
                };
                EngineContext* engineContextPtr = &methodScopedEngineContext;

                try
                {
                    ExternResultHandleSharedScanDataIterator dataIteratorHandle = Methods.kernel_scan_data_init(this.kernelOwnedSharedExternEnginePtr, managedScanPtr);
                    if (dataIteratorHandle.tag != ExternResultHandleSharedScanDataIterator_Tag.OkHandleSharedScanDataIterator)
                    {
                        throw new InvalidOperationException("Failed to construct kernel scan data iterator.");
                    }
                    kernelOwnedScanDataIteratorPtr = dataIteratorHandle.Anonymous.Anonymous1.ok;
                    for (; ; )
                    {
                        ExternResultbool isScanOk = Methods.kernel_scan_data_next(kernelOwnedScanDataIteratorPtr, engineContextPtr, Marshal.GetFunctionPointerForDelegate<VisitScanDataDelegate>(VisitCallbacks.VisitScanData));
                        if (isScanOk.tag != ExternResultbool_Tag.Okbool) throw new InvalidOperationException("Failed to iterate on table scan data.");
                        else if (!isScanOk.Anonymous.Anonymous1.ok) break;
                    }
                    return (methodScopedArrowContext.Schema, methodScopedArrowContext.ToRecordBatches());
                }
                finally
                {
                    if (kernelOwnedScanDataIteratorPtr != null) Methods.free_kernel_scan_data(kernelOwnedScanDataIteratorPtr);
                    if (tableRootPtr != IntPtr.Zero) Marshal.FreeHGlobal(tableRootPtr);
                    methodScopedArrowContext.Dispose();
                }
            }
        }

        internal Apache.Arrow.Table ReadAsArrowTable()
        {
            (Schema schema, List<RecordBatch> recordBatches) = this.ReadAsRecordBatchesAndSchema();
            return Apache.Arrow.Table.TableFromRecordBatches(schema, recordBatches);
        }

        /// <remarks>
        /// Inspired from https://github.com/apache/arrow/issues/35371, this is
        /// the only documented way to convert a list of <see
        /// cref="RecordBatch"/>es reliably to a single <see cref="DataFrame"/>.
        /// </remarks>
        internal DataFrame ReadAsDataFrame()
        {
            (_, List<RecordBatch> recordBatches) = this.ReadAsRecordBatchesAndSchema();
            if (recordBatches == null || recordBatches.Count == 0)
            {
                throw new ArgumentException("Cannot read as DataFrame, the list of Arrow Record Batches from Delta Kernel is null or empty.");
            }
            Schema schema = recordBatches[0].Schema;
            List<IArrowArray> concatenatedColumns = new();

            foreach (Field field in schema.FieldsList)
            {
                List<IArrowArray> columnArrays = new();
                foreach (RecordBatch recordBatch in recordBatches)
                {
                    IArrowArray column = recordBatch.Column(field.Name);
                    columnArrays.Add(column);
                }
                IArrowArray concatenatedColumn = ArrowArrayConcatenator.Concatenate(columnArrays);
                concatenatedColumns.Add(concatenatedColumn);
            }
            return DataFrame.FromArrowRecordBatch(new RecordBatch(schema, concatenatedColumns, concatenatedColumns[0].Length));
        }

        internal override long Version()
        {
            if (this.isKernelAllocated && this.isKernelSupported)
            {
                unsafe
                {
                    return unchecked((long)Methods.version(this.state.Snapshot(true)));
                }
            }
            return base.Version();
        }

        internal override string Uri()
        {
            if (this.isKernelAllocated && this.isKernelSupported)
            {
                unsafe
                {
                    IntPtr tableRootPtr = IntPtr.Zero;
                    try
                    {
                        tableRootPtr = (IntPtr)Methods.snapshot_table_root(this.state.Snapshot(true), Marshal.GetFunctionPointerForDelegate<AllocateStringFn>(StringAllocatorCallbacks.AllocateString));

                        // Kernel returns an extra "/", delta-rs does not
                        //
                        return Marshal.PtrToStringAnsi(tableRootPtr)?.TrimEnd('/') ?? string.Empty;
                    }
                    finally
                    {
                        if (tableRootPtr != IntPtr.Zero) Marshal.FreeHGlobal(tableRootPtr);
                    }
                }
            }
            return base.Uri();
        }

        /// <remarks>
        /// Kernel does not support "loading". The moment the user invokes
        /// this, we run into a state inconsistency, because further calls to "Version()" via Kernel
        /// will return the latest version, as opposed to the version the user loaded.
        ///
        /// So - we un-support Kernel and invoke "delta-rs" going forward.
        /// </remarks>
        internal override async Task LoadVersionAsync(ulong version, ICancellationToken cancellationToken)
        {
            this.isKernelSupported = false;
            await base.LoadVersionAsync(version, cancellationToken).ConfigureAwait(false);
        }

        /// <remarks>
        /// <see cref="LoadVersionAsync"/> remarks.
        /// </remarks>
        internal override async Task LoadTimestampAsync(long timestampMilliseconds, ICancellationToken cancellationToken)
        {
            this.isKernelSupported = false;
            await base.LoadTimestampAsync(timestampMilliseconds, cancellationToken).ConfigureAwait(false);
        }

        /// <remarks>
        /// Kernel does not support all Metadata - so we get what we can from
        /// delta-rs, and override with Kernel values when supported. The idea
        /// is, as Kernel exposes more metadata that delta-rs does not, we can
        /// continue to expose the Kernel view to end users.
        /// </remarks>
        internal override TableMetadata Metadata()
        {
            TableMetadata metadata = base.Metadata();
            if (this.isKernelAllocated && this.isKernelSupported)
            {
                unsafe
                {
                    metadata.PartitionColumns = PartitionColumns();
                }
            }
            return metadata;
        }

        #endregion Delta Kernel table operations

        #region SafeHandle implementation

        protected override unsafe bool ReleaseHandle()
        {
            if (this.isKernelAllocated)
            {
                this.state.Dispose();

                if (this.tableLocationHandle.IsAllocated) this.tableLocationHandle.Free();
                foreach (GCHandle handle in this.storageOptionsKeyHandles) if (handle.IsAllocated) handle.Free();
                foreach (GCHandle handle in this.storageOptionsValueHandles) if (handle.IsAllocated) handle.Free();
                Marshal.FreeHGlobal((IntPtr)this.gcPinnedStorageOptionsKeyPtrs);
                Marshal.FreeHGlobal((IntPtr)this.gcPinnedStorageOptionsValuePtrs);

                // EngineBuilder* does not need to be deallocated
                //
                // >>> https://delta-users.slack.com/archives/C04TRPG3LHZ/p1727978348653369
                //
                Methods.free_engine(kernelOwnedSharedExternEnginePtr);
            }

            return base.ReleaseHandle();
        }

        #endregion SafeHandle implementation

        #region Private methods

        private List<string> PartitionColumns()
        {
            List<string> partitionColumns = new();
            unsafe
            {
                PartitionList* managedPartitionListPtr = this.state.PartitionList(true);
                int numPartitions = managedPartitionListPtr->Len;
                if (numPartitions > 0)
                {
                    for (int i = 0; i < numPartitions; i++)
                    {
                        partitionColumns.Add(
                            Marshal.PtrToStringAnsi((IntPtr)managedPartitionListPtr->Cols[i])
                                ?? throw new InvalidOperationException(
                                    $"Delta Kernel returned a null partition column name despite reporting {numPartitions} > 0 partition(s) exist."
                                )
                        );
                    }
                }

                return partitionColumns;
            }
        }

        #endregion Private methods
    }
}
