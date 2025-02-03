using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Apache.Arrow.C;
using DeltaLake.Errors;

namespace DeltaLake.Bridge
{
    /// <summary>
    /// Core-owned Delta Rust runtime provisions physical Delta Tables at the
    /// storage level and delta-rs owned pointers to the table.
    /// </summary>
    internal class Runtime : SafeHandle
    {
        /// <summary>
        /// Gets the pointer to the bridged delta-rs runtime.
        /// </summary>
        internal unsafe Interop.Runtime* Ptr { get; private init; }

        /// <summary>
        /// Initializes a new instance of the <see cref="Runtime"/> class.
        /// </summary>
        /// <param name="options">Engine options.</param>
        /// <exception cref="InvalidOperationException">Any internal core error.</exception>
        internal Runtime(DeltaLake.Table.EngineOptions options)
            : base(IntPtr.Zero, true)
        {
            unsafe
            {
                var res = Interop.Methods.runtime_new(null);
                // If it failed, copy byte array, free runtime and byte array. Otherwise just
                // return runtime.
                if (res.fail != null)
                {
                    var message = ByteArrayRef.StrictUTF8.GetString(
                        res.fail->data,
                        (int)res.fail->size);
                    Interop.Methods.byte_array_free(res.runtime, res.fail);
                    Interop.Methods.runtime_free(res.runtime);
                    throw new InvalidOperationException(message);
                }
                Ptr = res.runtime;
                SetHandle((IntPtr)Ptr);
            }
        }

        internal virtual async Task<IntPtr> LoadTablePtrAsync(
            DeltaLake.Table.TableOptions options,
            System.Threading.CancellationToken cancellationToken)
        {
            var buffer = ArrayPool<byte>.Shared.Rent(System.Text.Encoding.UTF8.GetByteCount(options.TableLocation));
#if NETCOREAPP
            var encodedLength = System.Text.Encoding.UTF8.GetBytes(options.TableLocation, buffer);
#else
            var encodedLength = System.Text.Encoding.UTF8.GetBytes(options.TableLocation, 0, options.TableLocation.Length, buffer, 0);
#endif
            try
            {
                return await LoadTablePtrAsync(buffer.AsMemory(0, encodedLength), options, cancellationToken).ConfigureAwait(false);
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(buffer);
            }
        }

        internal virtual async Task<IntPtr> LoadTablePtrAsync(
            Memory<byte> tableUri,
            DeltaLake.Table.TableOptions options,
            System.Threading.CancellationToken cancellationToken)
        {
            var tsc = new TaskCompletionSource<IntPtr>();
            using (var scope = new Scope())
            {
                unsafe
                {
                    var nativeOptions = new Interop.TableOptions()
                    {
                        version = options.Version.HasValue ? unchecked((long)options.Version.Value) : -1L,
                        without_files = (byte)(options.WithoutFiles ? 1 : 0),
                        log_buffer_size = options.LogBufferSize ?? (nuint)0,
                        storage_options = options.StorageOptions != null ? scope.Dictionary(this, options.StorageOptions) : null,
                    };
                    Interop.Methods.table_new(
                        Ptr,
                        scope.Pointer(scope.ByteArray(tableUri)),
                        scope.Pointer(nativeOptions),
                        scope.CancellationToken(cancellationToken),
                        scope.FunctionPointer<Interop.TableNewCallback>((success, fail) =>
                    {
                        if (cancellationToken.IsCancellationRequested)
                        {
                            _ = Task.Run(() => tsc.TrySetCanceled(cancellationToken)); ;
                            return;
                        }

                        if (fail != null)
                        {
                            _ = Task.Run(() => tsc.TrySetException(DeltaRuntimeException.FromDeltaTableError(Ptr, fail)));
                        }
                        else
                        {
                            _ = Task.Run(() => tsc.TrySetResult((IntPtr)success));
                        }
                    }));
                }

                return await tsc.Task.ConfigureAwait(false);
            }
        }

        internal virtual async Task<IntPtr> CreateTablePtrAsync(
            DeltaLake.Table.TableCreateOptions options,
            System.Threading.CancellationToken cancellationToken)
        {
            var tsc = new TaskCompletionSource<IntPtr>();
            using (var scope = new Scope())
            {
                unsafe
                {
                    var nativeSchema = CArrowSchema.Create();
                    try
                    {
                        CArrowSchemaExporter.ExportSchema(options.Schema, nativeSchema);
                        var saveMode = Table.ConvertSaveMode(options.SaveMode);
                        var nativeOptions = new Interop.TableCreatOptions()
                        {
                            table_uri = scope.ByteArray(options.TableLocation),
                            schema = nativeSchema,
                            partition_by = scope.ArrayPointer(options.PartitionBy.Select(x => scope.ByteArray(x)).ToArray()),
                            partition_count = (nuint)options.PartitionBy.Count,
                            mode = saveMode.Ref,
                            name = scope.ByteArray(options.Name),
                            description = scope.ByteArray(options.Description),
                            configuration = scope.Dictionary(this, options.Configuration ?? new Dictionary<string, string>()),
                            custom_metadata = scope.Dictionary(this, options.CustomMetadata ?? new Dictionary<string, string>()),
                            storage_options = scope.Dictionary(this, options.StorageOptions ?? new Dictionary<string, string>()),
                        };
                        Interop.Methods.create_deltalake(
                            Ptr,
                            scope.Pointer(nativeOptions),
                            scope.CancellationToken(cancellationToken),
                            scope.FunctionPointer<Interop.TableNewCallback>((success, fail) =>
                            {
                                if (cancellationToken.IsCancellationRequested)
                                {
                                    _ = Task.Run(() => tsc.TrySetCanceled(cancellationToken)); ;
                                    return;
                                }

                                if (fail != null)
                                {
                                    _ = Task.Run(() => tsc.TrySetException(DeltaRuntimeException.FromDeltaTableError(Ptr, fail)));
                                }
                                else
                                {
                                    _ = Task.Run(() => tsc.TrySetResult((IntPtr)success));
                                }
                            }));
                    }
                    finally
                    {
                        CArrowSchema.Free(nativeSchema);
                    }
                }

                return await tsc.Task.ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Free a byte array.
        /// </summary>
        /// <param name="byteArray">Byte array to free.</param>
        internal unsafe void FreeByteArray(Interop.ByteArray* byteArray)
        {
            Interop.Methods.byte_array_free(Ptr, byteArray);
        }

        #region SafeHandle implementation

        /// <inheritdoc />
        public override unsafe bool IsInvalid => false;

        /// <inheritdoc />
        protected override unsafe bool ReleaseHandle()
        {
            Interop.Methods.runtime_free(Ptr);
            return true;
        }

        #endregion SafeHandle implementation
    }
}