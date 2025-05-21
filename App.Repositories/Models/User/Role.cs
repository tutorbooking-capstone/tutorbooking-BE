using Microsoft.AspNetCore.Authorization;
using System;

namespace App.Repositories.Models.User
{
    public enum Role
    {
        Admin,
        Tutor,
        Learner,
        Staff
    }

    #region Extensions
    public static class RoleExtensions
    {
        public static string ToStringRole(this Role role)
            => Enum.GetName(typeof(Role), role) 
                ?? throw new ArgumentOutOfRangeException(nameof(role), $"Unexpected role value: {role}");

        public static Role ToRoleEnum(this string roleString)
        {
            if (Enum.TryParse(typeof(Role), roleString, out var result))
                return (Role)result!;
            throw new ArgumentOutOfRangeException(nameof(roleString), $"Unexpected role string: {roleString}");
        }

        public static string ToRolesString(this IEnumerable<Role> roles)
        {
            if (roles == null || !roles.Any())
                return string.Empty;

            return string.Join(",", roles.Select(r => r.ToString()));
        }
    }

    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = false)]
    public class AuthorizeRolesAttribute : AuthorizeAttribute
    {
        public AuthorizeRolesAttribute(params Role[] roles)
        {
            if (roles == null || roles.Length == 0)
                throw new ArgumentNullException(nameof(roles));

            Roles = string.Join(",", roles.Select(r => Enum.GetName(typeof(Role), r)));
        }
    }
    #endregion
}

