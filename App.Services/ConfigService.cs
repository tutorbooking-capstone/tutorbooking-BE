using App.Core.Jsetting;
using App.Core.Provider;
using App.Services.Infras;
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
            services.AddScoped<IProfileService, ProfileService>();
            
            services.AddScoped<ITutorService, TutorService>();
            services.AddScoped<ILearnerService, LearnerService>();
            #endregion

            #region Another Services
            services.AddScoped<ISeedService, SeedService>();
            services.AddScoped<IScheduleService, ScheduleService>();

            services.AddScoped<IBlogService, BlogService>();
            services.AddScoped<IHashtagService, HashtagService>();
            services.AddScoped<IDocumentService, DocumentService>();

            services.AddScoped<ITutorApplicationService, TutorApplicationService>();
            #endregion

            #region Provider Services
            services.AddScoped<ICurrentUserProvider, CurrentUserProvider>();
            services.AddScoped<ICloudinaryProvider, CloudinaryProvider>();
            services.AddScoped<IDatabaseService, DatabaseService>();
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
            services.Configure<CloudinarySettings>(configuration.GetSection("CloudinarySettings"));


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
