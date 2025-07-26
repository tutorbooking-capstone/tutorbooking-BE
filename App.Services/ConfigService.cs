using System.Net.Http.Headers;
using App.Core.Jsetting;
using App.Core.Provider;
using App.Repositories.Models;
using App.Services.Infras;
using App.Services.Interfaces;
using App.Services.Interfaces.User;
using App.Services.Services;
using App.Services.Services.User;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Hangfire.PostgreSql;
using Hangfire;

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
            //services.AddScoped<ISeedService, SeedService>();
            services.AddScoped<IScheduleService, ScheduleService>();
            services.AddScoped<ILearnerBookingService, LearnerBookingService>();
            services.AddScoped<ITutorBookingService, TutorBookingService>();

            services.AddScoped<IBlogService, BlogService>();
            services.AddScoped<IHashtagService, HashtagService>();
            services.AddScoped<IDocumentService, DocumentService>();
            services.AddScoped<ILessonService, LessonService>();

            services.AddScoped<ITutorApplicationService, TutorApplicationService>();
			services.AddScoped<IChatService, ChatService>();
            services.AddScoped<ITutorApplicationStaffService, TutorApplicationStaffService>();
            services.AddScoped<IBookingSlotRatingService, BookingSlotRatingService>();
            services.AddScoped<INotificationService, NotificationService>();
            #endregion

            #region Provider Services
            services.AddScoped<ICurrentUserProvider, CurrentUserProvider>();
            services.AddScoped<ICloudinaryProvider, CloudinaryProvider>();
            services.AddScoped<IDatabaseService, DatabaseService>();
            #endregion

            #region Payment Services
            services.AddScoped<IPaymentProcessingService, PaymentProcessingService>();
            services.AddScoped<IWalletService, WalletService>();
            services.AddScoped<IDepositService, DepositService>();
            services.AddPayosServices();
            #endregion

            #region Hangfire Services
            services.AddHangfireServices(configuration);
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
            services.Configure<PayosSettings>(configuration.GetSection("PayOS"));
            services.Configure<BookingSettings>(configuration.GetSection("BookingSettings"));

            services.AddJwtSettingsConfig(configuration);
            
            return services;
        }
        
        public static IServiceCollection AddPayosServices(this IServiceCollection services)
        {
            // Đăng ký HttpClient cho PayOS
            services.AddHttpClient("PayOS", client =>
            {
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            });

            services.AddScoped<IPayosService, PayosService>();
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

        public static IServiceCollection AddHangfireServices(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            // Cấu hình Hangfire với PostgreSQL
            services.AddHangfire(config =>
            {
                config.UsePostgreSqlStorage(options =>
                {
                    options.UseNpgsqlConnection(configuration.GetConnectionString("DeployConnection"));
                });
                
                // Bỏ qua các công việc đã thất bại sau 3 lần thử lại
                config.UseFilter(new AutomaticRetryAttribute { Attempts = 3 });
            });
            
            // Đăng ký BackgroundJobClient để sử dụng với DI
            services.AddHangfireServer(options =>
            {
                options.WorkerCount = 1; // Số lượng worker, có thể điều chỉnh tùy theo tài nguyên
                options.Queues = new[] { "default" }; // Queue mặc định
            });

            return services;
        }
        #endregion
    }
}
