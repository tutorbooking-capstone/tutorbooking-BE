using App.Core;
using App.DTOs;
using App.Repositories;
using App.Services;
using FirebaseAdmin;
using Google.Apis.Auth.OAuth2;
using Hangfire;
using Hangfire.Dashboard;
using TutorBooking.APIService.Hubs;
using TutorBooking.APIService.Hubs.ChatHubs;
using TutorBooking.APIService.Hubs.NotificationHubs;
using TutorBooking.APIService.Middleware;

namespace TutorBooking.APIService
{
    public class Startup
    {
        public IConfiguration Configuration { get; }

        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public void ConfigureServices(IServiceCollection services)
        {
            services.ConfigJsonOptions();
            services.AddEndpointsApiExplorer();
            services.ConfigHttpClient();

            #region Add App Libraries Config
            services.AddAppCoreConfig();
            services.AddAppRepositoriesConfig(Configuration);
            services.AddAppServicesConfig(Configuration);
            services.AddAppDTOsConfig();
            services.AddAppAPIConfig(Configuration);
            #endregion

            #region 3rd Party Libraries Config
            services.AddMiniProfilerConfig();
            #endregion

            #region Add Cors
            services.AddCors(options =>
            {
                options.AddPolicy("AllowFrontend", builder =>
                {
                    builder
                        .WithOrigins(
                            "http://localhost:5173", // Local development
                            "https://ngoai-ngu-ngay.vercel.app" // Deployed frontend
                        )
                        .AllowAnyMethod()
                        .AllowAnyHeader()
                        .AllowCredentials();
                });
            });
            #endregion

            #region Add PayOS
            services.AddHttpClient("PayOS", client =>
            {
                client.Timeout = TimeSpan.FromSeconds(30);
            })
            .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
            {
                // Cho phép chuyển hướng
                AllowAutoRedirect = true,
                // Tăng số lượng chuyển hướng tối đa
                MaxAutomaticRedirections = 10,
                // Cấu hình SSL/TLS
                ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
            });
            #endregion

            #region Firebase
            FirebaseApp.Create(new AppOptions
            {
                Credential = GoogleCredential.FromFile("ngoaingungay-firebase-adminsdk-fbsvc-0855eb8d07.json"),
            });
            #endregion

            services.Configure<ConnectionServiceOptions>(options =>
            {
                options.MaxConnectionsPerUser = 5;
                options.MaxTotalConnections = 1000;
            });

            services.AddSingleton<ConnectionService>();
            // Đăng ký ConnectionCleanupService để dọn dẹp kết nối không hoạt động
            services.AddHostedService<ConnectionCleanupService>();
        }
        //testd sadf
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            // Middleware handle logging, better for debug engineering :>>>
            app.UseMiddleware<RequestLogSeparatorMiddleware>();

            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();

                app.Use(async (context, next) =>
                {
                    if (context.Request.Path == "/")
                    {

                        context.Response.Redirect("/profiler/results");
                        return;
                    }
                    await next();
                });
            }

            #region 3rd Party Libraries
            app.UseMiniProfiler();

            app.UseSwagger();
            app.UseSwaggerUI();
            
            // Thêm Hangfire Dashboard
            app.UseHangfireDashboard("/hangfire", new DashboardOptions
            {
                Authorization = env.IsDevelopment() ? Array.Empty<IDashboardAuthorizationFilter>() : new[] { new HangfireAuthorizationFilter() },
                IsReadOnlyFunc = (context) => false
            });
            #endregion

            app.UseHttpsRedirection();
            app.UseRouting();
            app.UseCors("AllowFrontend");

            // Exception handling middleware should come first to catch exceptions from subsequent middleware.
            app.UseMiddleware<ExceptionMiddleware>(); // Correct: Placed early to handle exceptions globally.
            // Authentication must come before Authorization and custom permission checks.
            app.UseAuthentication(); // Correct: Establishes user identity.
            
            // Authorization checks if the authenticated user has permission based on standard policies/roles.
            app.UseAuthorization(); // Correct: Must follow UseAuthentication.
            // Custom Permission middleware performs additional checks, relying on the authenticated user.
            app.UseMiddleware<PermissionMiddleware>(); // Correct: Placed after Authentication and Authorization as it likely depends on the established user identity and roles.


            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
				endpoints.MapHub<ChatHub>("/chathub", options =>
				{
					options.Transports =
						Microsoft.AspNetCore.Http.Connections.HttpTransportType.WebSockets |
						Microsoft.AspNetCore.Http.Connections.HttpTransportType.LongPolling;

					options.ApplicationMaxBufferSize = 64 * 1024; // 64KB
					options.TransportMaxBufferSize = 64 * 1024;   // 64KB
					options.AllowStatefulReconnects = true;
				});

				endpoints.MapHub<NotificationHub>("/notification-hub", options =>
				{
					options.Transports =
						Microsoft.AspNetCore.Http.Connections.HttpTransportType.WebSockets |
						Microsoft.AspNetCore.Http.Connections.HttpTransportType.LongPolling;

					options.ApplicationMaxBufferSize = 64 * 1024; // 64KB
					options.TransportMaxBufferSize = 64 * 1024;   // 64KB
				});
			});
        }
    }

}
