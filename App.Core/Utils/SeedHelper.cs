using App.Core.Base;
using System;
using System.Linq.Expressions;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace App.Core.Utils
{
    public static class HSeed
    {
        public static string SeedId<T>(this T obj, Expression<Func<T, string>> identifyFieldExpr)
        {
            if (obj == null)
                throw new InvalidArgumentException(
                    paramName: nameof(obj),
                    message: "Đối tượng không được null."
                );

            var typeName = typeof(T).Name;
            if (!(identifyFieldExpr.Body is MemberExpression member))
                throw new InvalidArgumentException(
                    paramName: nameof(identifyFieldExpr),
                    message: "Biểu thức phải là một MemberExpression."
                );

            var propName = member.Member.Name;
            var getter = identifyFieldExpr.Compile();
            var rawValue = getter(obj);
            var valuePart = !string.IsNullOrEmpty(rawValue)
                ? FormatIdentifyValue(rawValue)
                : HashObjectToValuePart(obj);

            return $"[Seed][{typeName}][{valuePart}]";
        }

        private static string FormatIdentifyValue(string identifyValue)
        {
            var noSpaces = identifyValue.Replace(" ", "_");
            return noSpaces.Length <= 14
                ? noSpaces
                : noSpaces.Substring(0, 14);
        }

        private static string HashObjectToValuePart<T>(T obj)
        {
            var json = JsonSerializer.Serialize(obj);
            using var md5 = MD5.Create();
            var hashBytes = md5.ComputeHash(Encoding.UTF8.GetBytes(json));
            var hex = BitConverter.ToString(hashBytes).Replace("-", "");
            return hex.Substring(0, Math.Min(14, hex.Length));
        }
    }
}
