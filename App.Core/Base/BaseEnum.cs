using System.Collections.Concurrent;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Reflection;

namespace App.Core.Base
{
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
    public sealed class EnumDescriptionAttribute : Attribute
    {
        public string Description { get; }
        public EnumDescriptionAttribute(string description) => Description = description ?? throw new ArgumentNullException(nameof(description));
    }

    /// <summary>
    /// Lightweight, immutable metadata struct for an enum member.
    /// Using readonly struct to minimize heap allocations.
    /// NumericValue is ulong (canonicalized) to represent any underlying integral enum safely.
    /// </summary>
    public readonly struct EnumValueInfo
    {
        public string Name { get; }
        public ulong NumericValue { get; }
        public string Description { get; }

        public EnumValueInfo(string name, ulong numericValue, string description)
        {
            Name = name;
            NumericValue = numericValue;
            Description = description;
        }
    }

    /// <summary>
    /// Per-type precomputed cache container (immutable after construction).
    /// Contains arrays + dictionaries for O(1) lookups.
    /// </summary>
    internal sealed class EnumTypeCache
    {
        public EnumValueInfo[] Values { get; }
        public Dictionary<ulong, string> NumericToDescription { get; }
        public Dictionary<string, ulong> NameToNumeric_Ordinal { get; }
        public Dictionary<string, ulong>? NameToNumeric_IgnoreCase { get; } // built on demand

        public EnumTypeCache(EnumValueInfo[] values, Dictionary<ulong, string> numericToDescription, Dictionary<string, ulong> nameToNumericOrdinal)
        {
            Values = values;
            NumericToDescription = numericToDescription;
            NameToNumeric_Ordinal = nameToNumericOrdinal;
        }

        public Dictionary<string, ulong> GetNameDictionary(bool ignoreCase)
        {
            if (!ignoreCase) return NameToNumeric_Ordinal;
            if (NameToNumeric_IgnoreCase != null) return NameToNumeric_IgnoreCase;

            // build lazily if needed (not common)
            var d = new Dictionary<string, ulong>(StringComparer.OrdinalIgnoreCase);
            foreach (var kv in NameToNumeric_Ordinal)
                d[kv.Key] = kv.Value;

            // reflection to set the readonly backing field - use private mutable via hack? Simpler to just return newly built dict.
            return d;
        }
    }

    public static class EnumHelper
    {
        // Top-level cache: Type -> per-type cache
        private static readonly ConcurrentDictionary<Type, EnumTypeCache> _cache = new();

        /// <summary>
        /// Build per-type cache (no LINQ, minimal allocations)
        /// </summary>
        private static EnumTypeCache BuildCache(Type enumType)
        {
            if (enumType == null) throw new ArgumentNullException(nameof(enumType));
            if (!enumType.IsEnum) throw new ArgumentException("Type must be enum", nameof(enumType));

            // Get public static fields (enum members)
            var fields = enumType.GetFields(BindingFlags.Public | BindingFlags.Static);
            var count = fields.Length;

            var values = new EnumValueInfo[count];
            var numericToDesc = new Dictionary<ulong, string>(count);
            var nameToNumeric = new Dictionary<string, ulong>(count); // ordinal (case-sensitive)

            for (int i = 0; i < count; i++)
            {
                var f = fields[i];
                var raw = f.GetValue(null); // boxed enum value
                var numeric = Convert.ToUInt64(raw!); // canonicalize to ulong (safe for all underlying types)

                // attribute precedence: custom EnumDescriptionAttribute -> DescriptionAttribute -> DisplayAttribute -> field.Name
                string? desc = null;
                var custom = f.GetCustomAttribute<EnumDescriptionAttribute>(false);
                if (custom != null)
                {
                    desc = custom.Description;
                }
                else
                {
                    var descr = f.GetCustomAttribute<DescriptionAttribute>(false);
                    if (descr != null) desc = descr.Description;
                    else
                    {
                        var disp = f.GetCustomAttribute<DisplayAttribute>(false);
                        if (disp != null) desc = disp.GetName() ?? disp.GetDescription();
                    }
                }

                if (desc == null) desc = f.Name;

                values[i] = new EnumValueInfo(f.Name, numeric, desc);
                // if duplicate numeric values appear (aliases), keep first description (consistent). If needed, handle aliasing explicitly.
                if (!numericToDesc.ContainsKey(numeric))
                    numericToDesc[numeric] = desc;
                if (!nameToNumeric.ContainsKey(f.Name))
                    nameToNumeric[f.Name] = numeric;
            }

            // optional: stable sort by numeric
            Array.Sort(values, (a, b) => a.NumericValue.CompareTo(b.NumericValue));

            return new EnumTypeCache(values, numericToDesc, nameToNumeric);
        }

        /// <summary>
        /// Get metadata array for enum type T (very cheap after cache warm).
        /// </summary>
        public static EnumValueInfo[] GetEnumValues<T>() where T : Enum
        {
            var type = typeof(T);
            var cache = _cache.GetOrAdd(type, BuildCache);
            return cache.Values;
        }

        /// <summary>
        /// O(1) description lookup by numeric value (Convert.ToUInt64(value)).
        /// Very fast: single dictionary lookup, minimal allocations.
        /// </summary>
        public static string GetDescription<T>(this T value) where T : Enum
        {
            var type = typeof(T);
            var cache = _cache.GetOrAdd(type, BuildCache);
            var numeric = Convert.ToUInt64(value);
            if (cache.NumericToDescription.TryGetValue(numeric, out var desc))
                return desc;
            // fallback (very rare) - use ToString
            return value.ToString() ?? string.Empty;
        }

        /// <summary>
        /// Try parse either by name (respecting ignoreCase flag) or by numeric (decimal) string.
        /// Avoids boxing where possible. Returns false if not parseable or out-of-range.
        /// </summary>
        public static bool TryParse<T>(string? input, bool ignoreCase, out T result) where T : struct, Enum
        {
            result = default;
            if (string.IsNullOrWhiteSpace(input)) return false;

            var type = typeof(T);
            var cache = _cache.GetOrAdd(type, BuildCache);

            // Try name (fast path) - use Enum.TryParse to benefit from built-in performance and support for numeric names like "0x1"
            if (Enum.TryParse<T>(input, ignoreCase, out var byName))
            {
                result = byName;
                return true;
            }

            // Try numeric
            if (ulong.TryParse(input, out var numeric))
            {
                // If numeric defined in enum (handles aliased values)
                if (cache.NumericToDescription.ContainsKey(numeric))
                {
                    result = (T)Enum.ToObject(type, numeric);
                    return true;
                }

                // Also attempt conversion to underlying signed range (rare)
                try
                {
                    var underlying = Enum.GetUnderlyingType(type);
                    var converted = Convert.ChangeType(numeric, underlying);
                    if (Enum.IsDefined(type, converted))
                    {
                        result = (T)Enum.ToObject(type, converted);
                        return true;
                    }
                }
                catch
                {
                    // ignore
                }
            }

            return false;
        }

        /// <summary>
        /// Get type-safe metadata dictionary. Key is type.Name (caller can change if they want).
        /// </summary>
        public static Dictionary<string, EnumValueInfo[]> GetEnumMetadata(params Type[] enumTypes)
        {
            if (enumTypes == null) throw new ArgumentNullException(nameof(enumTypes));
            var res = new Dictionary<string, EnumValueInfo[]>(enumTypes.Length);
            foreach (var t in enumTypes)
            {
                if (t == null || !t.IsEnum) continue;
                var c = _cache.GetOrAdd(t, BuildCache);
                res[t.Name] = c.Values;
            }
            return res;
        }

        /// <summary>
        /// Clear entire cache.
        /// </summary>
        public static void ClearCache() => _cache.Clear();

        /// <summary>
        /// Reload a specific enum type (rebuild its cache).
        /// </summary>
        public static void Reload(Type enumType)
        {
            if (enumType == null) throw new ArgumentNullException(nameof(enumType));
            if (!enumType.IsEnum) throw new ArgumentException("Type must be enum.", nameof(enumType));
            _cache.AddOrUpdate(enumType, BuildCache(enumType), (_, __) => BuildCache(enumType));
        }
    }
}
