// -----------------------------------------------------------------------------
// <copyright company="The Delta Lake Project Authors">
// Copyright (2024) The Delta Lake Project Authors.  All rights reserved.
// Licensed under the Apache license. See LICENSE file in the project root for full license information.
// </copyright>
// -----------------------------------------------------------------------------

using System;
using System.Runtime.InteropServices;

namespace DeltaLake.Kernel.Interop
{
    internal enum KernelError
    {
        UnknownError,
        FFIError,
        ArrowError,
        EngineDataTypeError,
        ExtractError,
        GenericError,
        IOErrorError,
        ParquetError,
        ObjectStoreError,
        ObjectStorePathError,
        ReqwestError,
        FileNotFoundError,
        MissingColumnError,
        UnexpectedColumnTypeError,
        MissingDataError,
        MissingVersionError,
        DeletionVectorError,
        InvalidUrlError,
        MalformedJsonError,
        MissingMetadataError,
        MissingProtocolError,
        MissingMetadataAndProtocolError,
        ParseError,
        JoinFailureError,
        Utf8Error,
        ParseIntError,
        InvalidColumnMappingModeError,
        InvalidTableLocationError,
        InvalidDecimalError,
        InvalidStructDataError,
        InternalError,
        InvalidExpression,
    }

    internal partial struct CStringMap
    {
    }

    internal partial struct DvInfo
    {
    }

    internal partial struct EngineBuilder
    {
    }

    internal partial struct ExclusiveEngineData
    {
    }

    internal partial struct ExclusiveFileReadResultIterator
    {
    }

    internal partial struct KernelExpressionVisitorState
    {
    }

    internal partial struct SharedExternEngine
    {
    }

    internal partial struct SharedGlobalScanState
    {
    }

    internal partial struct SharedScan
    {
    }

    internal partial struct SharedScanDataIterator
    {
    }

    internal partial struct SharedSchema
    {
    }

    internal partial struct SharedSnapshot
    {
    }

    internal partial struct StringSliceIterator
    {
    }

    internal unsafe partial struct KernelBoolSlice
    {
        [NativeTypeName("bool*")]
        public byte* ptr;

        [NativeTypeName("uintptr_t")]
        public ulong len;
    }

    internal unsafe partial struct KernelRowIndexArray
    {
        [NativeTypeName("uint64_t *")]
        public ulong* ptr;

        [NativeTypeName("uintptr_t")]
        public ulong len;
    }

    internal partial struct EngineError
    {
        [NativeTypeName("enum KernelError")]
        public KernelError etype;
    }

    internal enum ExternResultEngineBuilder_Tag
    {
        OkEngineBuilder,
        ErrEngineBuilder,
    }

    internal unsafe partial struct ExternResultEngineBuilder
    {
        public ExternResultEngineBuilder_Tag tag;

        [NativeTypeName("__AnonymousRecord_delta_kernel_ffi_L170_C3")]
        public _Anonymous_e__Union Anonymous;

        [StructLayout(LayoutKind.Explicit)]
        internal unsafe partial struct _Anonymous_e__Union
        {
            [FieldOffset(0)]
            [NativeTypeName("__AnonymousRecord_delta_kernel_ffi_L171_C5")]
            public _Anonymous1_e__Struct Anonymous1;

            [FieldOffset(0)]
            [NativeTypeName("__AnonymousRecord_delta_kernel_ffi_L174_C5")]
            public _Anonymous2_e__Struct Anonymous2;

            internal unsafe partial struct _Anonymous1_e__Struct
            {
                [NativeTypeName("struct EngineBuilder *")]
                public EngineBuilder* ok;
            }

            internal unsafe partial struct _Anonymous2_e__Struct
            {
                [NativeTypeName("struct EngineError *")]
                public EngineError* err;
            }
        }
    }

    internal unsafe partial struct KernelStringSlice
    {
        [NativeTypeName("const char *")]
        public sbyte* ptr;

        [NativeTypeName("uintptr_t")]
        public ulong len;
    }

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    [return: NativeTypeName("struct EngineError *")]
    internal unsafe delegate EngineError* AllocateErrorFn([NativeTypeName("enum KernelError")] KernelError etype, [NativeTypeName("struct KernelStringSlice")] KernelStringSlice msg);

    internal enum ExternResultHandleSharedExternEngine_Tag
    {
        OkHandleSharedExternEngine,
        ErrHandleSharedExternEngine,
    }

    internal unsafe partial struct ExternResultHandleSharedExternEngine
    {
        public ExternResultHandleSharedExternEngine_Tag tag;

        [NativeTypeName("__AnonymousRecord_delta_kernel_ffi_L257_C3")]
        public _Anonymous_e__Union Anonymous;

        [StructLayout(LayoutKind.Explicit)]
        internal unsafe partial struct _Anonymous_e__Union
        {
            [FieldOffset(0)]
            [NativeTypeName("__AnonymousRecord_delta_kernel_ffi_L258_C5")]
            public _Anonymous1_e__Struct Anonymous1;

            [FieldOffset(0)]
            [NativeTypeName("__AnonymousRecord_delta_kernel_ffi_L261_C5")]
            public _Anonymous2_e__Struct Anonymous2;

            internal unsafe partial struct _Anonymous1_e__Struct
            {
                [NativeTypeName("HandleSharedExternEngine")]
                public SharedExternEngine* ok;
            }

            internal unsafe partial struct _Anonymous2_e__Struct
            {
                [NativeTypeName("struct EngineError *")]
                public EngineError* err;
            }
        }
    }

    internal enum ExternResultHandleSharedSnapshot_Tag
    {
        OkHandleSharedSnapshot,
        ErrHandleSharedSnapshot,
    }

    internal unsafe partial struct ExternResultHandleSharedSnapshot
    {
        public ExternResultHandleSharedSnapshot_Tag tag;

        [NativeTypeName("__AnonymousRecord_delta_kernel_ffi_L314_C3")]
        public _Anonymous_e__Union Anonymous;

        [StructLayout(LayoutKind.Explicit)]
        internal unsafe partial struct _Anonymous_e__Union
        {
            [FieldOffset(0)]
            [NativeTypeName("__AnonymousRecord_delta_kernel_ffi_L315_C5")]
            public _Anonymous1_e__Struct Anonymous1;

            [FieldOffset(0)]
            [NativeTypeName("__AnonymousRecord_delta_kernel_ffi_L318_C5")]
            public _Anonymous2_e__Struct Anonymous2;

            internal unsafe partial struct _Anonymous1_e__Struct
            {
                [NativeTypeName("HandleSharedSnapshot")]
                public SharedSnapshot* ok;
            }

            internal unsafe partial struct _Anonymous2_e__Struct
            {
                [NativeTypeName("struct EngineError *")]
                public EngineError* err;
            }
        }
    }

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    [return: NativeTypeName("NullableCvoid")]
    internal unsafe delegate void* AllocateStringFn([NativeTypeName("struct KernelStringSlice")] KernelStringSlice kernel_str);

    internal unsafe partial struct EngineSchemaVisitor
    {
        public void* data;

        [NativeTypeName("uintptr_t (*)(void *, uintptr_t)")]
        public IntPtr make_field_list;

        [NativeTypeName("void (*)(void *, uintptr_t, struct KernelStringSlice, uintptr_t)")]
        public IntPtr visit_struct;

        [NativeTypeName("void (*)(void *, uintptr_t, struct KernelStringSlice, bool, uintptr_t)")]
        public IntPtr visit_array;

        [NativeTypeName("void (*)(void *, uintptr_t, struct KernelStringSlice, bool, uintptr_t)")]
        public IntPtr visit_map;

        [NativeTypeName("void (*)(void *, uintptr_t, struct KernelStringSlice, uint8_t, uint8_t)")]
        public IntPtr visit_decimal;

        [NativeTypeName("void (*)(void *, uintptr_t, struct KernelStringSlice)")]
        public IntPtr visit_string;

        [NativeTypeName("void (*)(void *, uintptr_t, struct KernelStringSlice)")]
        public IntPtr visit_long;

        [NativeTypeName("void (*)(void *, uintptr_t, struct KernelStringSlice)")]
        public IntPtr visit_integer;

        [NativeTypeName("void (*)(void *, uintptr_t, struct KernelStringSlice)")]
        public IntPtr visit_short;

        [NativeTypeName("void (*)(void *, uintptr_t, struct KernelStringSlice)")]
        public IntPtr visit_byte;

        [NativeTypeName("void (*)(void *, uintptr_t, struct KernelStringSlice)")]
        public IntPtr visit_float;

        [NativeTypeName("void (*)(void *, uintptr_t, struct KernelStringSlice)")]
        public IntPtr visit_double;

        [NativeTypeName("void (*)(void *, uintptr_t, struct KernelStringSlice)")]
        public IntPtr visit_boolean;

        [NativeTypeName("void (*)(void *, uintptr_t, struct KernelStringSlice)")]
        public IntPtr visit_binary;

        [NativeTypeName("void (*)(void *, uintptr_t, struct KernelStringSlice)")]
        public IntPtr visit_date;

        [NativeTypeName("void (*)(void *, uintptr_t, struct KernelStringSlice)")]
        public IntPtr visit_timestamp;

        [NativeTypeName("void (*)(void *, uintptr_t, struct KernelStringSlice)")]
        public IntPtr visit_timestamp_ntz;
    }

    internal unsafe partial struct EngineIterator
    {
        public void* data;

        [NativeTypeName("const void *(*)(void *)")]
        public IntPtr get_next;
    }

    internal enum ExternResultusize_Tag
    {
        Okusize,
        Errusize,
    }

    internal unsafe partial struct ExternResultusize
    {
        public ExternResultusize_Tag tag;

        [NativeTypeName("__AnonymousRecord_delta_kernel_ffi_L514_C3")]
        public _Anonymous_e__Union Anonymous;

        [StructLayout(LayoutKind.Explicit)]
        internal unsafe partial struct _Anonymous_e__Union
        {
            [FieldOffset(0)]
            [NativeTypeName("__AnonymousRecord_delta_kernel_ffi_L515_C5")]
            public _Anonymous1_e__Struct Anonymous1;

            [FieldOffset(0)]
            [NativeTypeName("__AnonymousRecord_delta_kernel_ffi_L518_C5")]
            public _Anonymous2_e__Struct Anonymous2;

            internal partial struct _Anonymous1_e__Struct
            {
                [NativeTypeName("uintptr_t")]
                public ulong ok;
            }

            internal unsafe partial struct _Anonymous2_e__Struct
            {
                [NativeTypeName("struct EngineError *")]
                public EngineError* err;
            }
        }
    }

    internal enum ExternResultbool_Tag
    {
        Okbool,
        Errbool,
    }

    internal unsafe partial struct ExternResultbool
    {
        public ExternResultbool_Tag tag;

        [NativeTypeName("__AnonymousRecord_delta_kernel_ffi_L535_C3")]
        public _Anonymous_e__Union Anonymous;

        [StructLayout(LayoutKind.Explicit)]
        internal unsafe partial struct _Anonymous_e__Union
        {
            [FieldOffset(0)]
            [NativeTypeName("__AnonymousRecord_delta_kernel_ffi_L536_C5")]
            public _Anonymous1_e__Struct Anonymous1;

            [FieldOffset(0)]
            [NativeTypeName("__AnonymousRecord_delta_kernel_ffi_L539_C5")]
            public _Anonymous2_e__Struct Anonymous2;

            internal partial struct _Anonymous1_e__Struct
            {
#if NETCOREAPP
                public bool ok;
#else
                // Desktop framework apparently doesn't consider bool to be blittable
                private int intOk;

                public bool ok
                {
                    get => intOk != 0;
                }
#endif
            }

            internal unsafe partial struct _Anonymous2_e__Struct
            {
                [NativeTypeName("struct EngineError *")]
                public EngineError* err;
            }
        }
    }

    internal enum ExternResultHandleExclusiveFileReadResultIterator_Tag
    {
        OkHandleExclusiveFileReadResultIterator,
        ErrHandleExclusiveFileReadResultIterator,
    }

    internal unsafe partial struct ExternResultHandleExclusiveFileReadResultIterator
    {
        public ExternResultHandleExclusiveFileReadResultIterator_Tag tag;

        [NativeTypeName("__AnonymousRecord_delta_kernel_ffi_L592_C3")]
        public _Anonymous_e__Union Anonymous;

        [StructLayout(LayoutKind.Explicit)]
        internal unsafe partial struct _Anonymous_e__Union
        {
            [FieldOffset(0)]
            [NativeTypeName("__AnonymousRecord_delta_kernel_ffi_L593_C5")]
            public _Anonymous1_e__Struct Anonymous1;

            [FieldOffset(0)]
            [NativeTypeName("__AnonymousRecord_delta_kernel_ffi_L596_C5")]
            public _Anonymous2_e__Struct Anonymous2;

            internal unsafe partial struct _Anonymous1_e__Struct
            {
                [NativeTypeName("HandleExclusiveFileReadResultIterator")]
                public ExclusiveFileReadResultIterator* ok;
            }

            internal unsafe partial struct _Anonymous2_e__Struct
            {
                [NativeTypeName("struct EngineError *")]
                public EngineError* err;
            }
        }
    }

    internal partial struct FileMeta
    {
        [NativeTypeName("struct KernelStringSlice")]
        public KernelStringSlice path;

        [NativeTypeName("int64_t")]
        public long last_modified;

        [NativeTypeName("uintptr_t")]
        public ulong size;
    }

    internal unsafe partial struct FFI_ArrowArray
    {
        [NativeTypeName("int64_t")]
        public long length;

        [NativeTypeName("int64_t")]
        public long null_count;

        [NativeTypeName("int64_t")]
        public long offset;

        [NativeTypeName("int64_t")]
        public long n_buffers;

        [NativeTypeName("int64_t")]
        public long n_children;

        [NativeTypeName("const void **")]
        public void** buffers;

        [NativeTypeName("struct FFI_ArrowArray **")]
        public FFI_ArrowArray** children;

        [NativeTypeName("struct FFI_ArrowArray *")]
        public FFI_ArrowArray* dictionary;

        [NativeTypeName("void (*)(struct FFI_ArrowArray *)")]
        public IntPtr release;

        public void* private_data;
    }

    internal unsafe partial struct FFI_ArrowSchema
    {
        [NativeTypeName("const char *")]
        public sbyte* format;

        [NativeTypeName("const char *")]
        public sbyte* name;

        [NativeTypeName("const char *")]
        public sbyte* metadata;

        [NativeTypeName("int64_t")]
        public long flags;

        [NativeTypeName("int64_t")]
        public long n_children;

        [NativeTypeName("struct FFI_ArrowSchema **")]
        public FFI_ArrowSchema** children;

        [NativeTypeName("struct FFI_ArrowSchema *")]
        public FFI_ArrowSchema* dictionary;

        [NativeTypeName("void (*)(struct FFI_ArrowSchema *)")]
        public IntPtr release;

        public void* private_data;
    }

    internal partial struct ArrowFFIData
    {
        [NativeTypeName("struct FFI_ArrowArray")]
        public FFI_ArrowArray array;

        [NativeTypeName("struct FFI_ArrowSchema")]
        public FFI_ArrowSchema schema;
    }

    internal enum ExternResultArrowFFIData_Tag
    {
        OkArrowFFIData,
        ErrArrowFFIData,
    }

    internal unsafe partial struct ExternResultArrowFFIData
    {
        public ExternResultArrowFFIData_Tag tag;

        [NativeTypeName("__AnonymousRecord_delta_kernel_ffi_L717_C3")]
        public _Anonymous_e__Union Anonymous;

        [StructLayout(LayoutKind.Explicit)]
        internal unsafe partial struct _Anonymous_e__Union
        {
            [FieldOffset(0)]
            [NativeTypeName("__AnonymousRecord_delta_kernel_ffi_L718_C5")]
            public _Anonymous1_e__Struct Anonymous1;

            [FieldOffset(0)]
            [NativeTypeName("__AnonymousRecord_delta_kernel_ffi_L721_C5")]
            public _Anonymous2_e__Struct Anonymous2;

            internal unsafe partial struct _Anonymous1_e__Struct
            {
                [NativeTypeName("struct ArrowFFIData *")]
                public ArrowFFIData* ok;
            }

            internal unsafe partial struct _Anonymous2_e__Struct
            {
                [NativeTypeName("struct EngineError *")]
                public EngineError* err;
            }
        }
    }

    internal enum ExternResultHandleSharedScan_Tag
    {
        OkHandleSharedScan,
        ErrHandleSharedScan,
    }

    internal unsafe partial struct ExternResultHandleSharedScan
    {
        public ExternResultHandleSharedScan_Tag tag;

        [NativeTypeName("__AnonymousRecord_delta_kernel_ffi_L774_C3")]
        public _Anonymous_e__Union Anonymous;

        [StructLayout(LayoutKind.Explicit)]
        internal unsafe partial struct _Anonymous_e__Union
        {
            [FieldOffset(0)]
            [NativeTypeName("__AnonymousRecord_delta_kernel_ffi_L775_C5")]
            public _Anonymous1_e__Struct Anonymous1;

            [FieldOffset(0)]
            [NativeTypeName("__AnonymousRecord_delta_kernel_ffi_L778_C5")]
            public _Anonymous2_e__Struct Anonymous2;

            internal unsafe partial struct _Anonymous1_e__Struct
            {
                [NativeTypeName("HandleSharedScan")]
                public SharedScan* ok;
            }

            internal unsafe partial struct _Anonymous2_e__Struct
            {
                [NativeTypeName("struct EngineError *")]
                public EngineError* err;
            }
        }
    }

    internal unsafe partial struct EnginePredicate
    {
        public void* predicate;

        [NativeTypeName("uintptr_t (*)(void *, struct KernelExpressionVisitorState *)")]
        public IntPtr visitor;
    }

    internal enum ExternResultHandleSharedScanDataIterator_Tag
    {
        OkHandleSharedScanDataIterator,
        ErrHandleSharedScanDataIterator,
    }

    internal unsafe partial struct ExternResultHandleSharedScanDataIterator
    {
        public ExternResultHandleSharedScanDataIterator_Tag tag;

        [NativeTypeName("__AnonymousRecord_delta_kernel_ffi_L884_C3")]
        public _Anonymous_e__Union Anonymous;

        [StructLayout(LayoutKind.Explicit)]
        internal unsafe partial struct _Anonymous_e__Union
        {
            [FieldOffset(0)]
            [NativeTypeName("__AnonymousRecord_delta_kernel_ffi_L885_C5")]
            public _Anonymous1_e__Struct Anonymous1;

            [FieldOffset(0)]
            [NativeTypeName("__AnonymousRecord_delta_kernel_ffi_L888_C5")]
            public _Anonymous2_e__Struct Anonymous2;

            internal unsafe partial struct _Anonymous1_e__Struct
            {
                [NativeTypeName("HandleSharedScanDataIterator")]
                public SharedScanDataIterator* ok;
            }

            internal unsafe partial struct _Anonymous2_e__Struct
            {
                [NativeTypeName("struct EngineError *")]
                public EngineError* err;
            }
        }
    }

    internal enum ExternResultKernelBoolSlice_Tag
    {
        OkKernelBoolSlice,
        ErrKernelBoolSlice,
    }

    internal unsafe partial struct ExternResultKernelBoolSlice
    {
        public ExternResultKernelBoolSlice_Tag tag;

        [NativeTypeName("__AnonymousRecord_delta_kernel_ffi_L905_C3")]
        public _Anonymous_e__Union Anonymous;

        [StructLayout(LayoutKind.Explicit)]
        internal unsafe partial struct _Anonymous_e__Union
        {
            [FieldOffset(0)]
            [NativeTypeName("__AnonymousRecord_delta_kernel_ffi_L906_C5")]
            public _Anonymous1_e__Struct Anonymous1;

            [FieldOffset(0)]
            [NativeTypeName("__AnonymousRecord_delta_kernel_ffi_L909_C5")]
            public _Anonymous2_e__Struct Anonymous2;

            internal partial struct _Anonymous1_e__Struct
            {
                [NativeTypeName("struct KernelBoolSlice")]
                public KernelBoolSlice ok;
            }

            internal unsafe partial struct _Anonymous2_e__Struct
            {
                [NativeTypeName("struct EngineError *")]
                public EngineError* err;
            }
        }
    }

    internal enum ExternResultKernelRowIndexArray_Tag
    {
        OkKernelRowIndexArray,
        ErrKernelRowIndexArray,
    }

    internal unsafe partial struct ExternResultKernelRowIndexArray
    {
        public ExternResultKernelRowIndexArray_Tag tag;

        [NativeTypeName("__AnonymousRecord_delta_kernel_ffi_L926_C3")]
        public _Anonymous_e__Union Anonymous;

        [StructLayout(LayoutKind.Explicit)]
        internal unsafe partial struct _Anonymous_e__Union
        {
            [FieldOffset(0)]
            [NativeTypeName("__AnonymousRecord_delta_kernel_ffi_L927_C5")]
            public _Anonymous1_e__Struct Anonymous1;

            [FieldOffset(0)]
            [NativeTypeName("__AnonymousRecord_delta_kernel_ffi_L930_C5")]
            public _Anonymous2_e__Struct Anonymous2;

            internal partial struct _Anonymous1_e__Struct
            {
                [NativeTypeName("struct KernelRowIndexArray")]
                public KernelRowIndexArray ok;
            }

            internal unsafe partial struct _Anonymous2_e__Struct
            {
                [NativeTypeName("struct EngineError *")]
                public EngineError* err;
            }
        }
    }

    internal partial struct Stats
    {
        [NativeTypeName("uint64_t")]
        public ulong num_records;
    }

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal unsafe delegate void CScanCallback([NativeTypeName("NullableCvoid")] void* engine_context, [NativeTypeName("struct KernelStringSlice")] KernelStringSlice path, [NativeTypeName("int64_t")] long size, [NativeTypeName("const struct Stats *")] Stats* stats, [NativeTypeName("const struct DvInfo *")] DvInfo* dv_info, [NativeTypeName("const struct CStringMap *")] CStringMap* partition_map);

    internal static unsafe partial class Methods
    {
        [DllImport("delta_kernel_ffi", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void free_bool_slice([NativeTypeName("struct KernelBoolSlice")] KernelBoolSlice slice);

        [DllImport("delta_kernel_ffi", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void free_row_indexes([NativeTypeName("struct KernelRowIndexArray")] KernelRowIndexArray slice);

        [DllImport("delta_kernel_ffi", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void free_engine_data([NativeTypeName("HandleExclusiveEngineData")] ExclusiveEngineData* engine_data);

        [DllImport("delta_kernel_ffi", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("struct ExternResultEngineBuilder")]
        public static extern ExternResultEngineBuilder get_engine_builder([NativeTypeName("struct KernelStringSlice")] KernelStringSlice path, [NativeTypeName("AllocateErrorFn")] IntPtr allocate_error);

        [DllImport("delta_kernel_ffi", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void set_builder_option([NativeTypeName("struct EngineBuilder *")] EngineBuilder* builder, [NativeTypeName("struct KernelStringSlice")] KernelStringSlice key, [NativeTypeName("struct KernelStringSlice")] KernelStringSlice value);

        [DllImport("delta_kernel_ffi", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("struct ExternResultHandleSharedExternEngine")]
        public static extern ExternResultHandleSharedExternEngine builder_build([NativeTypeName("struct EngineBuilder *")] EngineBuilder* builder);

        [DllImport("delta_kernel_ffi", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("struct ExternResultHandleSharedExternEngine")]
        public static extern ExternResultHandleSharedExternEngine get_default_engine([NativeTypeName("struct KernelStringSlice")] KernelStringSlice path, [NativeTypeName("AllocateErrorFn")] IntPtr allocate_error);

        [DllImport("delta_kernel_ffi", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("struct ExternResultHandleSharedExternEngine")]
        public static extern ExternResultHandleSharedExternEngine get_sync_engine([NativeTypeName("AllocateErrorFn")] IntPtr allocate_error);

        [DllImport("delta_kernel_ffi", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void free_engine([NativeTypeName("HandleSharedExternEngine")] SharedExternEngine* engine);

        [DllImport("delta_kernel_ffi", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("struct ExternResultHandleSharedSnapshot")]
        public static extern ExternResultHandleSharedSnapshot snapshot([NativeTypeName("struct KernelStringSlice")] KernelStringSlice path, [NativeTypeName("HandleSharedExternEngine")] SharedExternEngine* engine);

        [DllImport("delta_kernel_ffi", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void free_snapshot([NativeTypeName("HandleSharedSnapshot")] SharedSnapshot* snapshot);

        [DllImport("delta_kernel_ffi", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("uint64_t")]
        public static extern ulong version([NativeTypeName("HandleSharedSnapshot")] SharedSnapshot* snapshot);

        [DllImport("delta_kernel_ffi", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("NullableCvoid")]
        public static extern void* snapshot_table_root([NativeTypeName("HandleSharedSnapshot")] SharedSnapshot* snapshot, [NativeTypeName("AllocateStringFn")] IntPtr allocate_fn);

        [DllImport("delta_kernel_ffi", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern bool string_slice_next([NativeTypeName("HandleStringSliceIterator")] StringSliceIterator* data, [NativeTypeName("NullableCvoid")] void* engine_context, [NativeTypeName("void (*)(NullableCvoid, struct KernelStringSlice)")] IntPtr engine_visitor);

        [DllImport("delta_kernel_ffi", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void free_string_slice_data([NativeTypeName("HandleStringSliceIterator")] StringSliceIterator* data);

        [DllImport("delta_kernel_ffi", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("uintptr_t")]
        public static extern ulong visit_schema([NativeTypeName("HandleSharedSnapshot")] SharedSnapshot* snapshot, [NativeTypeName("struct EngineSchemaVisitor *")] EngineSchemaVisitor* visitor);

        [DllImport("delta_kernel_ffi", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("uintptr_t")]
        public static extern ulong visit_expression_and([NativeTypeName("struct KernelExpressionVisitorState *")] KernelExpressionVisitorState* state, [NativeTypeName("struct EngineIterator *")] EngineIterator* children);

        [DllImport("delta_kernel_ffi", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("uintptr_t")]
        public static extern ulong visit_expression_lt([NativeTypeName("struct KernelExpressionVisitorState *")] KernelExpressionVisitorState* state, [NativeTypeName("uintptr_t")] ulong a, [NativeTypeName("uintptr_t")] ulong b);

        [DllImport("delta_kernel_ffi", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("uintptr_t")]
        public static extern ulong visit_expression_le([NativeTypeName("struct KernelExpressionVisitorState *")] KernelExpressionVisitorState* state, [NativeTypeName("uintptr_t")] ulong a, [NativeTypeName("uintptr_t")] ulong b);

        [DllImport("delta_kernel_ffi", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("uintptr_t")]
        public static extern ulong visit_expression_gt([NativeTypeName("struct KernelExpressionVisitorState *")] KernelExpressionVisitorState* state, [NativeTypeName("uintptr_t")] ulong a, [NativeTypeName("uintptr_t")] ulong b);

        [DllImport("delta_kernel_ffi", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("uintptr_t")]
        public static extern ulong visit_expression_ge([NativeTypeName("struct KernelExpressionVisitorState *")] KernelExpressionVisitorState* state, [NativeTypeName("uintptr_t")] ulong a, [NativeTypeName("uintptr_t")] ulong b);

        [DllImport("delta_kernel_ffi", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("uintptr_t")]
        public static extern ulong visit_expression_eq([NativeTypeName("struct KernelExpressionVisitorState *")] KernelExpressionVisitorState* state, [NativeTypeName("uintptr_t")] ulong a, [NativeTypeName("uintptr_t")] ulong b);

        [DllImport("delta_kernel_ffi", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("struct ExternResultusize")]
        public static extern ExternResultusize visit_expression_column([NativeTypeName("struct KernelExpressionVisitorState *")] KernelExpressionVisitorState* state, [NativeTypeName("struct KernelStringSlice")] KernelStringSlice name, [NativeTypeName("AllocateErrorFn")] IntPtr allocate_error);

        [DllImport("delta_kernel_ffi", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("uintptr_t")]
        public static extern ulong visit_expression_not([NativeTypeName("struct KernelExpressionVisitorState *")] KernelExpressionVisitorState* state, [NativeTypeName("uintptr_t")] ulong inner_expr);

        [DllImport("delta_kernel_ffi", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("uintptr_t")]
        public static extern ulong visit_expression_is_null([NativeTypeName("struct KernelExpressionVisitorState *")] KernelExpressionVisitorState* state, [NativeTypeName("uintptr_t")] ulong inner_expr);

        [DllImport("delta_kernel_ffi", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("struct ExternResultusize")]
        public static extern ExternResultusize visit_expression_literal_string([NativeTypeName("struct KernelExpressionVisitorState *")] KernelExpressionVisitorState* state, [NativeTypeName("struct KernelStringSlice")] KernelStringSlice value, [NativeTypeName("AllocateErrorFn")] IntPtr allocate_error);

        [DllImport("delta_kernel_ffi", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("uintptr_t")]
        public static extern ulong visit_expression_literal_int([NativeTypeName("struct KernelExpressionVisitorState *")] KernelExpressionVisitorState* state, [NativeTypeName("int32_t")] int value);

        [DllImport("delta_kernel_ffi", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("uintptr_t")]
        public static extern ulong visit_expression_literal_long([NativeTypeName("struct KernelExpressionVisitorState *")] KernelExpressionVisitorState* state, [NativeTypeName("int64_t")] long value);

        [DllImport("delta_kernel_ffi", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("uintptr_t")]
        public static extern ulong visit_expression_literal_short([NativeTypeName("struct KernelExpressionVisitorState *")] KernelExpressionVisitorState* state, [NativeTypeName("int16_t")] short value);

        [DllImport("delta_kernel_ffi", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("uintptr_t")]
        public static extern ulong visit_expression_literal_byte([NativeTypeName("struct KernelExpressionVisitorState *")] KernelExpressionVisitorState* state, [NativeTypeName("int8_t")] sbyte value);

        [DllImport("delta_kernel_ffi", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("uintptr_t")]
        public static extern ulong visit_expression_literal_float([NativeTypeName("struct KernelExpressionVisitorState *")] KernelExpressionVisitorState* state, float value);

        [DllImport("delta_kernel_ffi", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("uintptr_t")]
        public static extern ulong visit_expression_literal_double([NativeTypeName("struct KernelExpressionVisitorState *")] KernelExpressionVisitorState* state, double value);

        [DllImport("delta_kernel_ffi", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("uintptr_t")]
        public static extern ulong visit_expression_literal_bool([NativeTypeName("struct KernelExpressionVisitorState *")] KernelExpressionVisitorState* state, bool value);

        [DllImport("delta_kernel_ffi", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("struct ExternResultbool")]
        public static extern ExternResultbool read_result_next([NativeTypeName("HandleExclusiveFileReadResultIterator")] ExclusiveFileReadResultIterator* data, [NativeTypeName("NullableCvoid")] void* engine_context, [NativeTypeName("void (*)(NullableCvoid, HandleExclusiveEngineData)")] IntPtr engine_visitor);

        [DllImport("delta_kernel_ffi", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void free_read_result_iter([NativeTypeName("HandleExclusiveFileReadResultIterator")] ExclusiveFileReadResultIterator* data);

        [DllImport("delta_kernel_ffi", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("struct ExternResultHandleExclusiveFileReadResultIterator")]
        public static extern ExternResultHandleExclusiveFileReadResultIterator read_parquet_file([NativeTypeName("HandleSharedExternEngine")] SharedExternEngine* engine, [NativeTypeName("const struct FileMeta *")] FileMeta* file, [NativeTypeName("HandleSharedSchema")] SharedSchema* physical_schema);

        [DllImport("delta_kernel_ffi", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("uintptr_t")]
        public static extern ulong engine_data_length([NativeTypeName("HandleExclusiveEngineData *")] ExclusiveEngineData** data);

        [DllImport("delta_kernel_ffi", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void* get_raw_engine_data([NativeTypeName("HandleExclusiveEngineData")] ExclusiveEngineData* data);

        [DllImport("delta_kernel_ffi", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("struct ExternResultArrowFFIData")]
        public static extern ExternResultArrowFFIData get_raw_arrow_data([NativeTypeName("HandleExclusiveEngineData")] ExclusiveEngineData* data, [NativeTypeName("HandleSharedExternEngine")] SharedExternEngine* engine);

        [DllImport("delta_kernel_ffi", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void free_scan([NativeTypeName("HandleSharedScan")] SharedScan* scan);

        [DllImport("delta_kernel_ffi", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("struct ExternResultHandleSharedScan")]
        public static extern ExternResultHandleSharedScan scan([NativeTypeName("HandleSharedSnapshot")] SharedSnapshot* snapshot, [NativeTypeName("HandleSharedExternEngine")] SharedExternEngine* engine, [NativeTypeName("struct EnginePredicate *")] EnginePredicate* predicate);

        [DllImport("delta_kernel_ffi", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("HandleSharedGlobalScanState")]
        public static extern SharedGlobalScanState* get_global_scan_state([NativeTypeName("HandleSharedScan")] SharedScan* scan);

        [DllImport("delta_kernel_ffi", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("HandleSharedSchema")]
        public static extern SharedSchema* get_global_read_schema([NativeTypeName("HandleSharedGlobalScanState")] SharedGlobalScanState* state);

        [DllImport("delta_kernel_ffi", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void free_global_read_schema([NativeTypeName("HandleSharedSchema")] SharedSchema* schema);

        [DllImport("delta_kernel_ffi", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("uintptr_t")]
        public static extern ulong get_partition_column_count([NativeTypeName("HandleSharedGlobalScanState")] SharedGlobalScanState* state);

        [DllImport("delta_kernel_ffi", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("HandleStringSliceIterator")]
        public static extern StringSliceIterator* get_partition_columns([NativeTypeName("HandleSharedGlobalScanState")] SharedGlobalScanState* state);

        [DllImport("delta_kernel_ffi", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void free_global_scan_state([NativeTypeName("HandleSharedGlobalScanState")] SharedGlobalScanState* state);

        [DllImport("delta_kernel_ffi", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("struct ExternResultHandleSharedScanDataIterator")]
        public static extern ExternResultHandleSharedScanDataIterator kernel_scan_data_init([NativeTypeName("HandleSharedExternEngine")] SharedExternEngine* engine, [NativeTypeName("HandleSharedScan")] SharedScan* scan);

        [DllImport("delta_kernel_ffi", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("struct ExternResultbool")]
        public static extern ExternResultbool kernel_scan_data_next([NativeTypeName("HandleSharedScanDataIterator")] SharedScanDataIterator* data, [NativeTypeName("NullableCvoid")] void* engine_context, [NativeTypeName("void (*)(NullableCvoid, HandleExclusiveEngineData, struct KernelBoolSlice)")] IntPtr engine_visitor);

        [DllImport("delta_kernel_ffi", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void free_kernel_scan_data([NativeTypeName("HandleSharedScanDataIterator")] SharedScanDataIterator* data);

        [DllImport("delta_kernel_ffi", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("NullableCvoid")]
        public static extern void* get_from_map([NativeTypeName("const struct CStringMap *")] CStringMap* map, [NativeTypeName("struct KernelStringSlice")] KernelStringSlice key, [NativeTypeName("AllocateStringFn")] IntPtr allocate_fn);

        [DllImport("delta_kernel_ffi", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("struct ExternResultKernelBoolSlice")]
        public static extern ExternResultKernelBoolSlice selection_vector_from_dv([NativeTypeName("const struct DvInfo *")] DvInfo* dv_info, [NativeTypeName("HandleSharedExternEngine")] SharedExternEngine* engine, [NativeTypeName("HandleSharedGlobalScanState")] SharedGlobalScanState* state);

        [DllImport("delta_kernel_ffi", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("struct ExternResultKernelRowIndexArray")]
        public static extern ExternResultKernelRowIndexArray row_indexes_from_dv([NativeTypeName("const struct DvInfo *")] DvInfo* dv_info, [NativeTypeName("HandleSharedExternEngine")] SharedExternEngine* engine, [NativeTypeName("HandleSharedGlobalScanState")] SharedGlobalScanState* state);

        [DllImport("delta_kernel_ffi", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void visit_scan_data([NativeTypeName("HandleExclusiveEngineData")] ExclusiveEngineData* data, [NativeTypeName("struct KernelBoolSlice")] KernelBoolSlice selection_vec, [NativeTypeName("NullableCvoid")] void* engine_context, [NativeTypeName("CScanCallback")] IntPtr callback);
    }
}

