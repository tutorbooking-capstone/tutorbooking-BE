using App.Core.Base;
using App.Services.Interfaces;
using App.Services.Interfaces.User;
using App.Services.Services;
using App.Services.Services.User;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace App.Services
{
    public static class ConfigService
    {
        public static IServiceCollection AddAppServicesConfig(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            services.AddConfigWithAppSettings(configuration);

            #region User Services
            services.AddScoped<IAuthService, AuthService>();
            services.AddScoped<IEmailService, EmailService>();
            services.AddScoped<ITokenService, TokenService>();
            services.AddScoped<IUserService, UserService>();
            #endregion

            #region Another Services
            services.AddScoped<IBlogService, BlogService>();
            #endregion

            services.AddHttpContextAccessor();

            return services;
        }

        #region Add Sub Services
        public static IServiceCollection AddConfigWithAppSettings(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            services.Configure<EmailSettings>(configuration.GetSection("EmailSettings"));
            
            services.AddJwtSettingsConfig(configuration);
            
            return services;
        }
        
        public static IServiceCollection AddJwtSettingsConfig(
            this IServiceCollection services, 
            IConfiguration configuration)
        {
            services.AddSingleton(option =>
            {
                JwtSettings jwtSettings = new JwtSettings
                {
                    SecretKey = configuration.GetValue<string>("JwtSettings:SecretKey"),
                    Issuer = configuration.GetValue<string>("JwtSettings:Issuer"),
                    Audience = configuration.GetValue<string>("JwtSettings:Audience"),
                    AccessTokenExpirationMinutes = configuration.GetValue<int>("JwtSettings:AccessTokenExpirationMinutes"),
                    RefreshTokenExpirationDays = configuration.GetValue<int>("JwtSettings:RefreshTokenExpirationDays")
                };
                jwtSettings.IsValid();
                return jwtSettings;
            });
            
            return services;
        }
        #endregion
    }
}
