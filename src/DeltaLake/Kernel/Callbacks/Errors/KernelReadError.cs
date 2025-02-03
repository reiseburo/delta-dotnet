// -----------------------------------------------------------------------------
// <summary>
// Error handling for Kernel read operations.
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
    /// Represents an error that occurred during reading operations via the Kernel.
    /// </summary>
    internal struct KernelReadError
    {
        public EngineError etype;
        public IntPtr msg;

#pragma warning disable CS8603, IDE0251 // Possible pointer null reference return is possible when we work with Kernel if the Kernel has a bug
        public string Message
        {
            get => MarshalExtensions.PtrToStringUTF8(msg);
            set => msg = MarshalExtensions.StringToCoTaskMemUTF8(value);
        }
#pragma warning restore CS8603, IDE0251
    }
}
