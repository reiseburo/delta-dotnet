using System;
using System.Collections.Generic;
using System.Linq;
using DeltaLake.Extensions;

namespace DeltaLake.Table
{
    /// <summary>
    /// Metadata for the table
    /// </summary>
    public record TableMetadata
    {
        private static readonly Dictionary<string, string> EmptySettings = new();

        /// <summary>
        /// Table id
        /// </summary>
        public string Id { get; init; } = string.Empty;

        /// <summary>
        /// Optional table name
        /// </summary>
        public string? Name { get; init; }

        /// <summary>
        /// Optional table description
        /// </summary>
        public string? Description { get; init; }

        /// <summary>
        /// Table format provider
        /// </summary>
        public string FormatProvider { get; init; } = string.Empty;

        /// <summary>
        /// Dictionary of formatting options
        /// </summary>
#pragma warning disable CS8619
        public IReadOnlyDictionary<string, string?> FormatOptions { get; init; } = EmptySettings;
#pragma warning restore CS8619

        /// <summary>
        /// Serialized table schema
        /// </summary>
        public string SchemaString { get; init; } = String.Empty;

        /// <summary>
        /// List of partition columns
        /// </summary>
        public IReadOnlyList<string> PartitionColumns { get; internal set; } = Array.Empty<string>();

        /// <summary>
        /// Created time
        /// </summary>
        public DateTimeOffset CreatedTime { get; init; }

        /// <summary>
        /// Configuration map
        /// </summary>
        public IReadOnlyDictionary<string, string> Configuration { get; init; } = EmptySettings;

        internal unsafe static TableMetadata FromUnmanaged(Bridge.Interop.TableMetadata* metadata)
        {
            var partitionColumns = StringArrayFromPointer(metadata->partition_columns, (int)metadata->partition_columns_count);
            return new DeltaLake.Table.TableMetadata
            {
                Id = MarshalExtensions.PtrToStringUTF8(new IntPtr(metadata->id)) ?? string.Empty,
                Name = MarshalExtensions.PtrToStringUTF8(new IntPtr(metadata->name)),
                Description = MarshalExtensions.PtrToStringUTF8(new IntPtr(metadata->description)),
                FormatProvider = MarshalExtensions.PtrToStringUTF8(new IntPtr(metadata->format_provider)) ?? string.Empty,
                SchemaString = MarshalExtensions.PtrToStringUTF8(new IntPtr(metadata->schema_string)) ?? string.Empty,
                CreatedTime = DateTimeOffset.FromUnixTimeMilliseconds(metadata->created_time),
                FormatOptions = KeyValueToDictionaryNullable(metadata->format_options),
                PartitionColumns = partitionColumns,
                Configuration = KeyValueToDictionary(metadata->configuration),
            };
        }

        private unsafe static string[] StringArrayFromPointer(sbyte** pointer, int length)
        {
            if (pointer == null)
            {
                return Array.Empty<string>();
            }

            var result = new string[length];
            for (var i = 0; i < length; i++)
            {
                var entry = *(pointer + i);
                result[i] = MarshalExtensions.PtrToStringUTF8(new IntPtr(entry)) ?? string.Empty;
            }

            return result;
        }

        private unsafe static Dictionary<string, string?> KeyValueToDictionaryNullable(Bridge.Interop.Dictionary kvs)
        {
            var dictionary = new Dictionary<string, string?>();
            if (kvs.length.ToUInt32() <= 0)
            {
                return dictionary;
            }

            for (var i = 0; i < (int)kvs.length; i++)
            {
                var entry = *(kvs.values + i);
                dictionary.TryAdd(
                    Bridge.ByteArrayRef.StrictUTF8.GetString(entry->key, (int)entry->key_length),
                    entry->value == null ? null : Bridge.ByteArrayRef.StrictUTF8.GetString(entry->value, (int)entry->value_length)
                );
            }

            return dictionary;
        }

        private unsafe static Dictionary<string, string> KeyValueToDictionary(Bridge.Interop.Dictionary kvs)
        {
            var toReturn = new Dictionary<string, string>();
            foreach (var (key, value) in KeyValueToDictionaryNullable(kvs).Where(x => x.Value != null))
            {
                if (value != null)
                {
                    toReturn[key] = value;
                }
            }

            return toReturn;
        }
    }
}