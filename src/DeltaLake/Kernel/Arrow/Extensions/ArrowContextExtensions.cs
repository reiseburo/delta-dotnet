// -----------------------------------------------------------------------------
// <summary>
// Extension methods for transforming Arrow Context.
// </summary>
//
// <copyright company="The Delta Lake Project Authors">
// Copyright (2024) The Delta Lake Project Authors.  All rights reserved.
// Licensed under the Apache license. See LICENSE file in the project root for full license information.
// </copyright>
// -----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using Apache.Arrow;
using Apache.Arrow.C;
using Apache.Arrow.Types;
using DeltaLake.Extensions;
using DeltaLake.Kernel.Interop;
using DeltaLake.Kernel.State;

namespace DeltaLake.Kernel.Arrow.Extensions
{
    /// <summary>
    /// Static class containing extension methods for Arrow Context.
    /// </summary>
    internal static class ArrowContextExtensions
    {

        #region Extension Methods

        /// <summary>
        /// Converts an Arrow Context to <see cref="Apache.Arrow.Table"/>
        /// </summary>
        /// <param name="context">The Arrow Context to convert.</param>
        /// <returns>The converted Arrow Table.</returns>
        internal static unsafe Apache.Arrow.Table ToTable(this ArrowContext context)
        {
            (Schema schema, List<RecordBatch> recordBatches) = context.ToSchematizedBatches();
            return Apache.Arrow.Table.TableFromRecordBatches(schema, recordBatches);
        }

        /// <summary>
        /// Converts an Arrow Context to a single <see cref="Apache.Arrow.RecordBatch"/>"/>
        /// </summary>
        /// <remarks>
        /// Inspired from https://github.com/apache/arrow/issues/35371, this is
        /// the only documented way to convert a list of <see cref="RecordBatch"/>es 
        /// reliably to a single <see cref="RecordBatch"/>.
        /// </remarks>
        /// <param name="context">The Arrow Context to convert.</param>
        /// <returns>The converted Record Batch.</returns>
        internal static unsafe RecordBatch ToRecordBatch(this ArrowContext context)
        {
            (Schema schema, List<RecordBatch> recordBatches) = context.ToSchematizedBatches();
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
            return new RecordBatch(schema, concatenatedColumns, concatenatedColumns[0].Length);
        }

        /// <summary>
        /// Converts an Arrow Context to a list of <see cref="Apache.Arrow.RecordBatch"/>"/>
        /// and <see cref="Apache.Arrow.Schema"/>.
        /// </summary>
        /// <param name="context">The Arrow Context to convert.</param>
        /// <returns>The converted tuple of Arrow Schema and Record Batches.</returns>
        internal static (Schema, List<RecordBatch>) ToSchematizedBatches(this ArrowContext context)
        {
            context.ValidateContext();

            List<RecordBatch> recordBatches = new(context.NumBatches);
            List<Schema> schemas = new(context.NumBatches);

            for (int i = 0; i < context.NumBatches; i++)
            {
                unsafe
                {
                    ArrowFFIData* arrowStructPtr = context.ArrowStructs[i];
                    ParquetStringPartitions* partitionsPtr = context.Partitions[i];

                    Schema schemaWithoutPartition = CArrowSchemaImporter.ImportSchema((CArrowSchema*)&arrowStructPtr->schema);
                    RecordBatch batchWithoutPartition = CArrowArrayImporter.ImportRecordBatch((CArrowArray*)&arrowStructPtr->array, schemaWithoutPartition);

                    Schema schemaWithPartition = AddPartitionColumnsToSchema(schemaWithoutPartition, partitionsPtr);
                    RecordBatch batchWithPartition = AddPartitionColumnsToRecordBatch(batchWithoutPartition, partitionsPtr);

                    recordBatches.Add(batchWithPartition);
                    schemas.Add(schemaWithPartition);
                }
            }

            for (int i = 1; i < schemas.Count; i++)
            {
                if (schemas[i] == schemas[0]) throw new InvalidOperationException($"All schemas must be the same in - got {i}th {schemas[i]} != 0th {schemas[0]}");
            }

            return (schemas[0], recordBatches);
        }

        #endregion Extension Methods

        #region Private Methods

        private static void ValidateContext(this ArrowContext context)
        {
            if (context.NumBatches == 0)
            {
                throw new InvalidOperationException("Arrow Context must contain at least one RecordBatch");
            }
        }

#pragma warning disable CA1859, IDE0060 // Although we're not using partitionValue right now, it will be used when Kernel supports reporting Arrow Schema
        private static unsafe IArrowType DeterminePartitionColumnType(string colName, string colValue)
        {
            // Currently, there's no way to determine the type of the partition,
            // because the Kernel always represents partition values as strings in CStringMap.
            //
            // We have a request with Kernel team here to get back the Arrow Schema from
            // the Delta Transaction Log:
            //
            // >>> https://delta-users.slack.com/archives/C04TRPG3LHZ/p1728178727958499
            // >>> https://delta-users.slack.com/archives/C04TRPG3LHZ/p1728001059452499?thread_ts=1727999835.930339&cid=C04TRPG3LHZ
            //
            return StringType.Default;
        }
#pragma warning restore CA1859, IDE0060

        private static unsafe Schema AddPartitionColumnsToSchema(Apache.Arrow.Schema originalSchema, ParquetStringPartitions* partitionsPtr)
        {
            Apache.Arrow.Schema.Builder schemaBuilder = new();
            foreach (Field field in originalSchema.FieldsList)
            {
                schemaBuilder = schemaBuilder.Field(field);
            }

            for (int i = 0; i < partitionsPtr->Len; i++)
            {
#pragma warning disable CS8600, CS8604 // If Kernel sends us back null pointers, we are in trouble anyway
                string colName = MarshalExtensions.PtrToStringUTF8((IntPtr)partitionsPtr->ColNames[i]);
                string colValue = MarshalExtensions.PtrToStringUTF8((IntPtr)partitionsPtr->ColValues[i]);
                IArrowType dataType = DeterminePartitionColumnType(colName, colValue);
#pragma warning restore CS8600, CS8604

                Field field = new(colName, dataType, nullable: true);
                schemaBuilder = schemaBuilder.Field(field);
            }

            return schemaBuilder.Build();
        }

        private static unsafe RecordBatch AddPartitionColumnsToRecordBatch(RecordBatch recordBatch, ParquetStringPartitions* partitionsPtr)
        {
            Apache.Arrow.Schema.Builder schemaBuilder = new();
            var columnList = new List<IArrowArray>();

            // Append the original fieldList to the schema builder.
            //
            foreach (Field field in recordBatch.Schema.FieldsList) schemaBuilder = schemaBuilder.Field(field);

            // Append the original columns to the column list.
            //
            for (int i = 0; i < recordBatch.Schema.FieldsList.Count; i++) columnList.Add(recordBatch.Column(i));

            // Convert each of the partition column metadata structs into actual
            // columns, then add it to the schema builder and column list.
            //
            for (int i = 0; i < partitionsPtr->Len; i++)
            {
                StringArray.Builder columnBuilder = new();

#pragma warning disable CS8600
                string colName = MarshalExtensions.PtrToStringUTF8((IntPtr)partitionsPtr->ColNames[i]);
                string colValue = MarshalExtensions.PtrToStringUTF8((IntPtr)partitionsPtr->ColValues[i]);
#pragma warning restore CS8600

                Field field = new(colName, StringType.Default, nullable: true);
                schemaBuilder = schemaBuilder.Field(field);

                for (int j = 0; j < recordBatch.Length; j++)
                {
                    columnBuilder = columnBuilder.Append(colValue ?? "");
                }
                columnList.Add(columnBuilder.Build());
            }
            return new RecordBatch(schemaBuilder.Build(), columnList, recordBatch.Length);
        }

        #endregion Private Methods
    }
}
