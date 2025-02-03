// -----------------------------------------------------------------------------
// <summary>
// Memory allocation error callback hooks.
// </summary>
//
// <copyright company="The Delta Lake Project Authors">
// Copyright (2024) The Delta Lake Project Authors.  All rights reserved.
// Licensed under the Apache license. See LICENSE file in the project root for full license information.
// </copyright>
// -----------------------------------------------------------------------------

using System;
using DeltaLake.Extensions;
using DeltaLake.Kernel.Interop;

namespace DeltaLake.Kernel.Callbacks.Errors
{
    /// <summary>
    /// The Kernel Memory Allocation Error Callback methods.
    /// </summary>
    internal class AllocateErrorCallbacks
    {
        /// <summary>
        /// Warns and throws an allocation error.
        /// </summary>
        /// <param name="etype">The type of kernel error.</param>
        /// <param name="msg">The error message.</param>
        /// <returns>An EngineError pointer.</returns>
        internal static unsafe EngineError* ThrowAllocationError(
            KernelError etype,
            KernelStringSlice msg
        )
        {
            string message = MarshalExtensions.PtrToStringUTF8((IntPtr)msg.ptr) ?? string.Empty;
            throw new InvalidOperationException(
                $"Kernel engine error of type {etype} occurred with message: {message}"
            );
        }
    }
}
