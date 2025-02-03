// -----------------------------------------------------------------------------
// <summary>
// Arrow InterOp handler for Delta Kernel FFI that converts Parquet files to
// Arrow in-memory with zero-copy.
// </summary>
//
// <copyright company="The Delta Lake Project Authors">
// Copyright (2024) The Delta Lake Project Authors.  All rights reserved.
// Licensed under the Apache license. See LICENSE file in the project root for full license information.
// </copyright>
// -----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using DeltaLake.Extensions;
using DeltaLake.Kernel.Callbacks.Allocators;
using DeltaLake.Kernel.Callbacks.Errors;
using DeltaLake.Kernel.Callbacks.Visit;
using DeltaLake.Kernel.Interop;
using DeltaLake.Kernel.State;

namespace DeltaLake.Kernel.Arrow.Handlers
{
    /// <summary>
    /// Arrow InterOp handler for Delta Kernel FFI, performs zero-copy
    /// operations.
    /// </summary>
    internal class ArrowFFIInterOpHandler : IArrowInteropHandler
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ArrowFFIInterOpHandler"/> class.
        /// </summary>
        public ArrowFFIInterOpHandler() { }

        #region IArrowInteropHandler implementation

        /// <inheritdoc/>
        /// <remarks>
        /// Caller is responsible for freeing the allocated memory when disposing
        /// <see cref="ArrowContext"/>.
        /// </remarks>
        public unsafe void StoreArrowInContext(
            ArrowContext* context,
            ArrowFFIData* arrowData,
            PartitionList* partitionCols,
            CStringMap* partitionValue
        )
        {
            // Allocate memory for the new set of arriving data.
            //
            ArrowFFIData* thisArrowStructPtr = (ArrowFFIData*)Marshal.AllocHGlobal(sizeof(ArrowFFIData));
            ParquetStringPartitions* thisPartitionsPtr = (ParquetStringPartitions*)Marshal.AllocHGlobal(sizeof(ParquetStringPartitions));

            // Allocate memory for the pointers array that will replace the old
            // pointers array.
            //
            ArrowFFIData** newArrowStructsArrayPtr = (ArrowFFIData**)Marshal.AllocHGlobal(sizeof(ArrowFFIData*) * (context->NumBatches + 1));
            ParquetStringPartitions** newPartitionsArrayPtr = (ParquetStringPartitions**)Marshal.AllocHGlobal(sizeof(ParquetStringPartitions*) * (context->NumBatches + 1));

            // Copy incoming values into the newly globally allocated memory.
            //
            *thisArrowStructPtr = *arrowData;
            *thisPartitionsPtr = ArrowFFIInterOpHandler.ParseParquetStringPartitions(partitionCols, partitionValue);

            // Copy old pre-allocated pointers from the ArrowContext, if exists.
            // This is a zero-copy operation, since we're just copying the
            // pointers around rapidly.
            //
            if (context->NumBatches > 0)
            {
                for (int i = 0; i < context->NumBatches; i++)
                {
                    newArrowStructsArrayPtr[i] = context->ArrowStructs[i];
                    newPartitionsArrayPtr[i] = context->Partitions[i];
                }
            }

            // Append the new set that just got read from the Kernel to
            // the end, and set the final size from this operation.
            //
            newArrowStructsArrayPtr[context->NumBatches] = thisArrowStructPtr;
            newPartitionsArrayPtr[context->NumBatches] = thisPartitionsPtr;

            context->ArrowStructs = newArrowStructsArrayPtr;
            context->Partitions = newPartitionsArrayPtr;

            context->NumBatches++;
        }

        /// <inheritdoc/>
        public unsafe void ReadParquetAsArrow(
            EngineContext* context,
            KernelStringSlice path,
            KernelBoolSlice selectionVector
        )
        {
#pragma warning disable CS8600
            string tableRoot = MarshalExtensions.PtrToStringUTF8((IntPtr)context->TableRoot)?.TrimEnd('/');
            string parquetAbsolutePath = $"{tableRoot}/{MarshalExtensions.PtrToStringUTF8((IntPtr)path.ptr, (int)path.len)}";
#pragma warning restore CS8600

            (GCHandle parquetAbsolutePathHandle, IntPtr gcPinnedParquetAbsolutePathPtr) = parquetAbsolutePath.ToPinnedSBytePointer();
            KernelStringSlice parquetAbsolutePathSlice = new() { ptr = (sbyte*)gcPinnedParquetAbsolutePathPtr, len = (nuint)parquetAbsolutePath.Length };
            FileMeta parquetMeta = new() { path = parquetAbsolutePathSlice };

            try
            {
                ExternResultHandleExclusiveFileReadResultIterator isParquetFileReadOk = Methods.read_parquet_file(context->Engine, &parquetMeta, context->Schema);
                if (isParquetFileReadOk.tag != ExternResultHandleExclusiveFileReadResultIterator_Tag.OkHandleExclusiveFileReadResultIterator)
                {
                    throw new InvalidOperationException($"Kernel failed to read parquet file at: {parquetAbsolutePath}");
                }

                ExclusiveFileReadResultIterator* arrowReadIterator = isParquetFileReadOk.Anonymous.Anonymous1.ok;
                for (; ; )
                {
                    ExternResultbool isArrowResultReadOk = Methods.read_result_next(arrowReadIterator, context, Marshal.GetFunctionPointerForDelegate(VisitCallbacks.IngestArrowData));
                    if (isArrowResultReadOk.tag != ExternResultbool_Tag.Okbool)
                    {
                        KernelReadError* arrowReadError = (KernelReadError*)isArrowResultReadOk.Anonymous.Anonymous2.err;
                        throw new InvalidOperationException($"Failed to iterate on reading arrow data from parquet: {arrowReadError->Message}");
                    }
                    else if (!isArrowResultReadOk.Anonymous.Anonymous1.ok) break;
                }
                Methods.free_read_result_iter(arrowReadIterator);
            }
            finally
            {
                if (parquetAbsolutePathHandle.IsAllocated) parquetAbsolutePathHandle.Free();
            }
        }

        #endregion IArrowInteropHandler implementation

        #region Private Methods

        private static unsafe ParquetStringPartitions ParseParquetStringPartitions(
            PartitionList* partitionCols,
            CStringMap* partitionKeyValueMap
        )
        {
            List<string> colNames = new();
            List<string> colValues = new();

            for (int i = 0; i < partitionCols->Len; i++)
            {
#pragma warning disable CS8600
                string colName = MarshalExtensions.PtrToStringUTF8((IntPtr)partitionCols->Cols[i]);
#pragma warning restore CS8600

                // The Kernel can currently only report String values back as
                // partition values, even if it's a different type (like
                // Integer, DateTime etc). This is a limitation of the Kernel
                // today, more information here:
                //
                // >>> https://delta-users.slack.com/archives/C04TRPG3LHZ/p1728178727958499
                //
#pragma warning disable CS1024, CS8629, CS8600 // If Kernel sends us back null pointers, we are in trouble anyway
                void* colValPtr = Methods.get_from_map(
                    partitionKeyValueMap,
                    new KernelStringSlice
                    {
                        ptr = (sbyte*)partitionCols->Cols[i],
                        len = (ulong)colName?.Length
                    },
                    Marshal.GetFunctionPointerForDelegate<AllocateStringFn>(StringAllocatorCallbacks.AllocateString)
                );
                string colVal = colValPtr != null ? MarshalExtensions.PtrToStringUTF8((IntPtr)colValPtr) : String.Empty;
#pragma warning restore CS1024, CS8629, CS8600

                if (!string.IsNullOrEmpty(colName) && !string.IsNullOrEmpty(colVal))
                {
                    colNames.Add(colName);
                    colValues.Add(colVal!);
                }
                else
                {
                    throw new InvalidOperationException($"Failed to parse partition columns (got {colName}) and values (got {colVal}) from the Kernel");
                }
            }

            byte** colNamesPtr = (byte**)Marshal.AllocHGlobal(colNames.Count * sizeof(byte*));
            byte** colValuesPtr = (byte**)Marshal.AllocHGlobal(colValues.Count * sizeof(byte*));

            for (int i = 0; i < colNames.Count; i++)
            {
                colNamesPtr[i] = (byte*)MarshalExtensions.StringToCoTaskMemUTF8(colNames[i]);
                colValuesPtr[i] = (byte*)MarshalExtensions.StringToCoTaskMemUTF8(colValues[i]);
            }

            return new ParquetStringPartitions
            {
                Len = colNames.Count,
                ColNames = colNamesPtr,
                ColValues = colValuesPtr
            };
        }

        #endregion Private Methods
    }
}
