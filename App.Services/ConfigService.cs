using App.Core.Jsetting;
using App.Core.Provider;
using App.Repositories.Models;
using App.Services.Events;
using App.Services.Hangfire;
using App.Services.Infras;
using App.Services.Interfaces;
using App.Services.Interfaces.User;
using App.Services.Services;
using App.Services.Services.User;
using Hangfire;
using Hangfire.PostgreSql;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.Net.Http.Headers;

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
            services.AddScoped<IManagerService, ManagerService>();
            services.AddScoped<IAdminService, AdminService>();
            
            services.AddScoped<ITutorService, TutorService>();
            services.AddScoped<ILearnerService, LearnerService>();
            #endregion

            #region Another Services
            //services.AddScoped<ISeedService, SeedService>();
            services.AddScoped<IScheduleService, ScheduleService>();
            services.AddScoped<ILearnerBookingService, LearnerBookingService>();
            services.AddScoped<ITutorBookingService, TutorBookingService>();
            services.AddScoped<IBookingService, BookingService>();

            services.AddScoped<IBlogService, BlogService>();
            services.AddScoped<IHashtagService, HashtagService>();
            services.AddScoped<IDocumentService, DocumentService>();
            services.AddScoped<ILessonService, LessonService>();

            services.AddScoped<ITutorApplicationService, TutorApplicationService>();
			services.AddScoped<IChatService, ChatService>();
            services.AddScoped<ITutorApplicationStaffService, TutorApplicationStaffService>();
            services.AddScoped<IBookingSlotRatingService, BookingSlotRatingService>();
            services.AddScoped<INotificationService, NotificationService>();
            services.AddScoped<ILegalDocumentService, LegalDocumentService>();
            services.AddScoped<ILegalDocumentAcceptanceService, LegalDocumentAcceptanceService>();
            services.AddSingleton<IOtpService, OtpService>();
            services.AddHostedService<OtpCleanupService>();
            services.AddScoped<ITutorIntroductionVideoService, TutorIntroductionVideoService>();
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
            services.AddScoped<IWithdrawalService, WithdrawalService>();
            services.AddScoped<IBankAccountService, BankAccountService>();
            services.AddScoped<IDisputeService, DisputeService>();
            services.AddScoped<IFeeService, FeeService>();
            services.AddPayosServices();
            #endregion

            #region Hangfire Services
            services.AddHangfireServices(configuration);
            services.AddScoped<OfferExpirationService>();
            services.AddScoped<BookingHeldFundService>();
            #endregion

            #region Notification Events
            services.AddSingleton<NotificationEvents>();
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
            services.AddHangfire(config =>
            {
                config.UsePostgreSqlStorage(options =>
                {
                    options.UseNpgsqlConnection(configuration.GetConnectionString("DeployConnection"));
                });
                //config.UseMAMQSqlExtension();
                config.UseFilter(new AutomaticRetryAttribute 
                { 
                    Attempts = 3,
                    OnAttemptsExceeded = AttemptsExceededAction.Delete 
                });
            });
            
            services.AddHangfireServer(options =>
            {
                options.WorkerCount = 1; 
                options.Queues = new[] { "tutorbooking_jobs", "default" }; 
                options.ServerName = "TutorBookingServer";
            });

            return services;
        }
        #endregion
    }
}
