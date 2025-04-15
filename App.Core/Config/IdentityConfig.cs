using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;

namespace App.Core.Config
{
    public static class IdentityConfig
    {
        public static IdentityBuilder AddAppIdentityConfig<TUser, TRole>(
            this IServiceCollection services)
            where TUser : class
            where TRole : class
        {
            return services.AddIdentity<TUser, TRole>(options =>
            {
                options.Password.RequireDigit = true;
                options.Password.RequireLowercase = true;
                options.Password.RequireUppercase = true;
                options.Password.RequireNonAlphanumeric = true;
                options.Password.RequiredLength = 8;

                options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(30);
                options.Lockout.MaxFailedAccessAttempts = 5;
                options.Lockout.AllowedForNewUsers = true;

                options.User.RequireUniqueEmail = true;
                options.User.AllowedUserNameCharacters = 
                    "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789-._@+";

                options.SignIn.RequireConfirmedEmail = true;
            });
        }
        
        #region Add App With Specific Identity Config
        public static IdentityBuilder AddAppIdentityConfig<TUser, TRole>(
            this IServiceCollection services,
            Action<IdentityOptions> configureOptions)
            where TUser : class
            where TRole : class
        {
            return services.AddIdentity<TUser, TRole>(configureOptions);
        }
        #endregion
    }
}
