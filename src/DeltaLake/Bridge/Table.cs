using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Apache.Arrow;
using Apache.Arrow.C;
using Apache.Arrow.Ipc;
using DeltaLake.Bridge.Interop;
using DeltaLake.Errors;
using DeltaLake.Table;
using ICancellationToken = System.Threading.CancellationToken;

namespace DeltaLake.Bridge
{
    /// <summary>
    /// Reference to unmanaged delta table from Delta Rust.
    /// </summary>
    internal class Table : SafeHandle
    {
#if !NETCOREAPP
        private unsafe delegate void MetadataRelease(Interop.TableMetadata* metadata);
#endif

        internal static readonly ByteArrayRef SaveModeAppend = ByteArrayRef.FromUTF8("append");

        internal static readonly ByteArrayRef SaveModeOverwrite = ByteArrayRef.FromUTF8("overwrite");

        internal static readonly ByteArrayRef SaveModeError = ByteArrayRef.FromUTF8("error");

        internal static readonly ByteArrayRef SaveModeIfgnore = ByteArrayRef.FromUTF8("ignore");

        internal readonly unsafe Interop.RawDeltaTable* _ptr;

        internal readonly Runtime _runtime;

        /// <summary>
        /// Initializes a new instance of the <see cref="Table"/> class.
        /// </summary>
        /// <param name="runtime">The Delta RS Runtime.</param>
        /// <param name="inner">The inner table pointer.</param>
        internal unsafe Table(Runtime runtime, Interop.RawDeltaTable* inner)
            : base(IntPtr.Zero, true)
        {
            _ptr = inner;
            _runtime = runtime;
            SetHandle((IntPtr)_ptr);
        }

        #region Delta Rust table operations

        internal virtual async Task LoadVersionAsync(ulong version, ICancellationToken cancellationToken)
        {
            var tsc = new TaskCompletionSource<bool>();
            using (var scope = new Scope())
            {
                unsafe
                {
                    Interop.Methods.table_load_version(
                        _runtime.Ptr,
                         _ptr,
                          unchecked((long)version),
                          scope.CancellationToken(cancellationToken),
                          scope.FunctionPointer<Interop.TableEmptyCallback>((fail) =>
                    {
                        if (cancellationToken.IsCancellationRequested)
                        {
                            tsc.TrySetCanceled(cancellationToken);
                            return;
                        }

                        if (fail != null)
                        {
                            tsc.TrySetException(DeltaRuntimeException.FromDeltaTableError(_runtime.Ptr, fail));
                        }
                        else
                        {
                            _ = Task.Run(() => tsc.TrySetResult(true));
                        }
                    }));
                }

                await tsc.Task.ConfigureAwait(false);
            }
        }

        internal virtual async Task LoadTimestampAsync(long timestampMilliseconds, ICancellationToken cancellationToken)
        {
            var tsc = new TaskCompletionSource<bool>();
            using (var scope = new Scope())
            {
                unsafe
                {
                    Interop.Methods.table_load_with_datetime(
                        _runtime.Ptr,
                         _ptr,
                         timestampMilliseconds,
                          scope.CancellationToken(cancellationToken),
                          scope.FunctionPointer<Interop.TableEmptyCallback>((fail) =>
                    {
                        if (cancellationToken.IsCancellationRequested)
                        {
                            tsc.TrySetCanceled(cancellationToken);
                            return;
                        }

                        if (fail != null)
                        {
                            tsc.TrySetException(DeltaRuntimeException.FromDeltaTableError(_runtime.Ptr, fail));
                        }
                        else
                        {
                            _ = Task.Run(() => tsc.TrySetResult(true));
                        }
                    }));
                }

                await tsc.Task.ConfigureAwait(false);
            }
        }

        internal virtual long Version()
        {
            unsafe
            {
                return Interop.Methods.table_version(_ptr);
            }
        }

        internal virtual string Uri()
        {
            unsafe
            {
                var uri = Interop.Methods.table_uri(_ptr);
                try
                {
                    if (uri == null)
                    {
                        return string.Empty;
                    }


                    return ByteArrayRef.StrictUTF8.GetString(uri->data, (int)uri->size);
                }
                finally
                {
                    Interop.Methods.byte_array_free(_runtime.Ptr, uri);
                }
            }
        }

        internal virtual string[] FileUris()
        {
            unsafe
            {
                return GetStringArray(Interop.Methods.table_file_uris(_runtime.Ptr, _ptr, null));
            }
        }

        internal virtual string[] Files()
        {
            unsafe
            {
                return GetStringArray(Interop.Methods.table_files(_runtime.Ptr, _ptr, null));
            }
        }

        internal virtual ProtocolInfo ProtocolVersions()
        {
            unsafe
            {
                var response = Methods.table_protocol_versions(_runtime.Ptr, _ptr);
                return new ProtocolInfo
                {
                    MinimumReaderVersion = response.min_reader_version,
                    MinimumWriterVersion = response.min_writer_version,
                };
            }
        }

        internal virtual Schema Schema()
        {
            unsafe
            {
                var result = Methods.table_schema(_runtime.Ptr, _ptr);
                if (result.error != null)
                {
                    throw DeltaRuntimeException.FromDeltaTableError(_runtime.Ptr, result.error);
                }

                var schemaPointer = (CArrowSchema*)result.bytes;
                try
                {
                    return CArrowSchemaImporter.ImportSchema(schemaPointer);
                }
                finally
                {
                    CArrowSchema.Free(schemaPointer);
                }
            }
        }

        internal virtual async Task<string> InsertAsync(
            IReadOnlyCollection<RecordBatch> records,
            Schema schema,
            InsertOptions options,
            ICancellationToken cancellationToken)
        {
            if (records.Count == 0)
            {
                return string.Empty;
            }

            using var stream = new RecordBatchReader(records, schema);
            return await InsertAsync(stream, options, cancellationToken).ConfigureAwait(false);
        }

        internal virtual async Task<string> InsertAsync(
            IArrowArrayStream stream,
            InsertOptions options,
            ICancellationToken cancellationToken)
        {
            var tsc = new TaskCompletionSource<string>();
            using (var scope = new Scope())
            {
                unsafe
                {
                    var ffiStream = CArrowArrayStream.Create();
                    try
                    {
                        CArrowArrayStreamExporter.ExportArrayStream(stream, ffiStream);
                        Interop.Methods.table_insert(
                            _runtime.Ptr,
                             _ptr,
                             ffiStream,
                             scope.Pointer(scope.ByteArray(options.Predicate)),
                             scope.Pointer(ConvertSaveMode(options.SaveMode).Ref),
                             new UIntPtr(options.MaxRowsPerGroup),
                             (byte)(options.OverwriteSchema ? 1 : 0),
                            scope.CancellationToken(cancellationToken),
                              scope.FunctionPointer<Interop.GenericErrorCallback>((success, fail) =>
                        {
                            if (cancellationToken.IsCancellationRequested)
                            {
                                tsc.TrySetCanceled(cancellationToken);
                                return;
                            }

                            if (fail != null)
                            {
                                tsc.TrySetException(DeltaRuntimeException.FromDeltaTableError(_runtime.Ptr, fail));
                            }
                            else
                            {
                                _ = Task.Run(() => tsc.TrySetResult("{}"));
                            }
                        }));
                    }
                    finally
                    {
                        CArrowArrayStream.Free(ffiStream);
                    }
                }

                return await tsc.Task.ConfigureAwait(false);
            }
        }

        internal virtual async Task<string> MergeAsync(
            string query,
            IReadOnlyCollection<RecordBatch> records,
            Schema schema,
            ICancellationToken cancellationToken)
        {
            if (records.Count == 0)
            {
                return string.Empty;
            }

            var tsc = new TaskCompletionSource<string>();
            using (var scope = new Scope())
            {
                using (var stream = new RecordBatchReader(records, schema))
                {
                    unsafe
                    {
                        var ffiStream = CArrowArrayStream.Create();
                        try
                        {
                            CArrowArrayStreamExporter.ExportArrayStream(stream, ffiStream);
                            Interop.Methods.table_merge(
                                _runtime.Ptr,
                                 _ptr,
                                 scope.Pointer(scope.ByteArray(query)),
                                 ffiStream,
                                scope.CancellationToken(cancellationToken),
                                  scope.FunctionPointer<Interop.GenericErrorCallback>((success, fail) =>
                            {

                                if (cancellationToken.IsCancellationRequested)
                                {
                                    tsc.TrySetCanceled(cancellationToken);
                                }
                                else if (fail != null)
                                {
                                    tsc.TrySetException(DeltaRuntimeException.FromDeltaTableError(_runtime.Ptr, fail));
                                }
                                else
                                {
                                    using var content = new ByteArray(_runtime, (Interop.ByteArray*)success);
                                    var value = content.ToUTF8();
                                    _ = Task.Run(() => tsc.TrySetResult(value));
                                }

                            }));
                        }
                        finally
                        {
                            CArrowArrayStream.Free(ffiStream);
                        }
                    }

                    return await tsc.Task.ConfigureAwait(false);
                }
            }
        }

        internal virtual async Task<IArrowArrayStream> QueryAsync(
            string query,
            string? tableName,
            ICancellationToken cancellationToken)
        {
            var tsc = new TaskCompletionSource<IArrowArrayStream>();
            using (var scope = new Scope())
            {
                unsafe
                {
                    Methods.table_query(
                        _runtime.Ptr,
                         _ptr,
                         scope.Pointer(scope.ByteArray(query)),
                         scope.Pointer(scope.ByteArray(tableName)),
                        scope.CancellationToken(cancellationToken),
                          scope.FunctionPointer<GenericErrorCallback>((success, fail) =>
                    {
                        if (cancellationToken.IsCancellationRequested)
                        {
                            tsc.TrySetCanceled(cancellationToken);
                            return;
                        }

                        if (fail != null)
                        {
                            tsc.TrySetException(DeltaRuntimeException.FromDeltaTableError(_runtime.Ptr, fail));
                        }
                        else
                        {
                            var stream = CArrowArrayStreamImporter.ImportArrayStream((CArrowArrayStream*)success);
                            _ = Task.Run(() =>
                            {
                                if (!tsc.TrySetResult(stream))
                                {
                                    stream.Dispose();
                                }
                            });
                        }
                    }));
                }

                return await tsc.Task.ConfigureAwait(false);
            }
        }

        internal virtual async Task<string> DeleteAsync(string predicate, ICancellationToken cancellationToken)
        {
            var tsc = new TaskCompletionSource<string>();
            using (var scope = new Scope())
            {
                unsafe
                {
                    Methods.table_delete(
                        _runtime.Ptr,
                        _ptr,
                        scope.Pointer(scope.ByteArray(predicate)),
                        scope.CancellationToken(cancellationToken),
                        scope.FunctionPointer<Interop.GenericErrorCallback>((success, fail) =>
                        {
                            if (cancellationToken.IsCancellationRequested)
                            {
                                tsc.TrySetCanceled(cancellationToken);
                            }
                            else if (fail != null)
                            {
                                tsc.TrySetException(DeltaRuntimeException.FromDeltaTableError(_runtime.Ptr, fail));
                            }
                            else
                            {
                                using var content = new ByteArray(_runtime, (Interop.ByteArray*)success);
                                var value = content.ToUTF8();
                                _ = Task.Run(() => tsc.TrySetResult(value));
                            }
                        }));

                }

                return await tsc.Task.ConfigureAwait(false);
            }
        }

        internal virtual async Task<string> UpdateAsync(string query, ICancellationToken cancellationToken)
        {
            var tsc = new TaskCompletionSource<string>();
            using (var scope = new Scope())
            {
                unsafe
                {
                    Methods.table_update(
                        _runtime.Ptr,
                        _ptr,
                        scope.Pointer(scope.ByteArray(query)),
                        scope.CancellationToken(cancellationToken),
                        scope.FunctionPointer<Interop.GenericErrorCallback>((success, fail) =>
                        {
                            if (cancellationToken.IsCancellationRequested)
                            {
                                tsc.TrySetCanceled(cancellationToken);
                            }
                            else if (fail != null)
                            {
                                tsc.TrySetException(DeltaRuntimeException.FromDeltaTableError(_runtime.Ptr, fail));
                            }
                            else
                            {
                                using var content = new ByteArray(_runtime, (Interop.ByteArray*)success);
                                var result = content.ToUTF8();
                                _ = Task.Run(() => tsc.TrySetResult(result));
                            }
                        }));

                }

                return await tsc.Task.ConfigureAwait(false);
            }
        }

        internal virtual async Task<byte[]> HistoryAsync(ulong limit, ICancellationToken cancellationToken)
        {
            var tsc = new TaskCompletionSource<byte[]>();
            using (var scope = new Scope())
            {
                unsafe
                {
                    Methods.history(
                        _runtime.Ptr,
                        _ptr,
                        new UIntPtr(limit),
                        scope.CancellationToken(cancellationToken),
                        scope.FunctionPointer<Interop.GenericErrorCallback>((success, fail) =>
                        {
                            if (cancellationToken.IsCancellationRequested)
                            {
                                tsc.TrySetCanceled(cancellationToken);
                            }
                            else if (fail != null)
                            {
                                tsc.TrySetException(DeltaRuntimeException.FromDeltaTableError(_runtime.Ptr, fail));
                            }
                            else
                            {
                                using var content = new ByteArray(_runtime, (Interop.ByteArray*)success);
                                var bytes = content.ToByteArray();
                                _ = Task.Run(() => tsc.TrySetResult(bytes)); ;
                            }
                        }));

                }

                return await tsc.Task.ConfigureAwait(false);
            }
        }

        internal virtual async Task AddConstraintAsync(IReadOnlyDictionary<string, string> constraints, IReadOnlyDictionary<string, string>? customMetadata, ICancellationToken cancellationToken)
        {
            if (constraints.Count == 0)
            {
                return;
            }

            var tsc = new TaskCompletionSource<bool>();
            using (var scope = new Scope())
            {
                unsafe
                {
                    Methods.table_add_constraints(
                        _runtime.Ptr,
                        _ptr,
                        scope.Dictionary(_runtime, constraints),
                        customMetadata == null ? null : scope.Dictionary(_runtime, customMetadata),
                        scope.CancellationToken(cancellationToken),
                        scope.FunctionPointer<Interop.TableEmptyCallback>((fail) =>
                        {
                            if (cancellationToken.IsCancellationRequested)
                            {
                                tsc.TrySetCanceled(cancellationToken);
                            }
                            else if (fail != null)
                            {
                                tsc.TrySetException(DeltaRuntimeException.FromDeltaTableError(_runtime.Ptr, fail));
                            }
                            else
                            {
                                _ = Task.Run(() => tsc.TrySetResult(true));
                            }
                        }));
                }

                await tsc.Task.ConfigureAwait(false);
            }
        }

        internal virtual async Task UpdateIncrementalAsync(long? maxVersion, ICancellationToken cancellationToken)
        {
            var tsc = new TaskCompletionSource<bool>();
            using (var scope = new Scope())
            {
                unsafe
                {
                    Methods.table_update_incremental(
                        _runtime.Ptr,
                        _ptr,
                        maxVersion ?? -1L,
                        scope.CancellationToken(cancellationToken),
                        scope.FunctionPointer<Interop.TableEmptyCallback>((fail) =>
                        {
                            if (cancellationToken.IsCancellationRequested)
                            {
                                tsc.TrySetCanceled(cancellationToken);
                            }
                            else if (fail != null)
                            {
                                tsc.TrySetException(DeltaRuntimeException.FromDeltaTableError(_runtime.Ptr, fail));
                            }
                            else
                            {
                                _ = Task.Run(() => tsc.TrySetResult(true));
                            }
                        }));

                }

                await tsc.Task.ConfigureAwait(false);
            }
        }

        internal virtual DeltaLake.Table.TableMetadata Metadata()
        {
            unsafe
            {
                var result = Methods.table_metadata(_runtime.Ptr, _ptr);
                if (result.error != null)
                {
                    throw DeltaRuntimeException.FromDeltaTableError(_runtime.Ptr, result.error);
                }

                try
                {
                    return DeltaLake.Table.TableMetadata.FromUnmanaged(result.metadata);
                }
                finally
                {
#if NETCOREAPP
                    var release = (delegate* unmanaged<Interop.TableMetadata*, void>)result.metadata->release;
#else
                    var release = Marshal.GetDelegateForFunctionPointer<MetadataRelease>(result.metadata->release);
#endif
                    release(result.metadata);
                }
            }
        }

        internal virtual async Task RestoreAsync(RestoreOptions options, ICancellationToken cancellationToken)
        {
            var tsc = new TaskCompletionSource<bool>();
            using (var scope = new Scope())
            {
                unsafe
                {
                    Methods.table_restore(
                        _runtime.Ptr,
                        _ptr,
                        options.Timestamp?.ToUnixTimeMilliseconds() ?? (long?)options.Version ?? 0,
                        BoolAsByte(options.Timestamp.HasValue),
                        BoolAsByte(options.IgnoreMissingFiles),
                        BoolAsByte(options.ProtocolDowngradeAllowed),
                        scope.Dictionary(_runtime, options.CustomMetadata),
                        scope.CancellationToken(cancellationToken),
                        scope.FunctionPointer<Interop.TableEmptyCallback>((fail) =>
                        {
                            if (cancellationToken.IsCancellationRequested)
                            {
                                tsc.TrySetCanceled(cancellationToken);
                            }
                            else if (fail != null)
                            {
                                tsc.TrySetException(DeltaRuntimeException.FromDeltaTableError(_runtime.Ptr, fail));
                            }
                            else
                            {
                                _ = Task.Run(() => tsc.TrySetResult(true));
                            }
                        }));
                }

                await tsc.Task.ConfigureAwait(false);
            }
        }

        #endregion Delta Rust table operations

        #region SafeHandle implementation

        /// <inheritdoc />
        public override bool IsInvalid => false;

        /// <inheritdoc />
        protected override unsafe bool ReleaseHandle()
        {
            Interop.Methods.table_free(_ptr);
            return true;
        }

        #endregion SafeHandle implementation

        #region Helper methods

        internal static ByteArrayRef ConvertSaveMode(SaveMode saveMode) =>
            saveMode switch
            {
                SaveMode.Append => SaveModeAppend,
                SaveMode.Overwrite => SaveModeOverwrite,
                SaveMode.ErrorIfExists => SaveModeError,
                SaveMode.Ignore => SaveModeIfgnore,
                _ => throw new ArgumentOutOfRangeException(nameof(saveMode)),
            };

        private static byte BoolAsByte(bool input) =>
            input switch
            {
                true => 0,
                false => 1,
            };

        private unsafe string[] GetStringArray(GenericOrError genericOrError)
        {
            if (genericOrError.error != null)
            {
                throw DeltaRuntimeException.FromDeltaTableError(_runtime.Ptr, genericOrError.error);
            }

            try
            {
                if (genericOrError.bytes == null)
                {
                    return System.Array.Empty<string>();
                }

                var dynamicArray = (DynamicArray*)genericOrError.bytes;
                var uris = new string[(int)dynamicArray->size];
                for (var i = 0; i < uris.Length; i++)
                {
                    var instance = dynamicArray->data + i;
                    uris[i] = ByteArrayRef.StrictUTF8.GetString(instance->data, (int)instance->size);
                }

                return uris;
            }
            finally
            {
                Interop.Methods.dynamic_array_free(_runtime.Ptr, (DynamicArray*)genericOrError.bytes);
            }
        }

        #endregion Helper methods
    }
}