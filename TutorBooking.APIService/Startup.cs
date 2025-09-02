using App.Core;
using App.DTOs;
using App.Repositories;
using App.Services;
using FirebaseAdmin;
using Google.Apis.Auth.OAuth2;
using Hangfire;
using Hangfire.Dashboard;
using Microsoft.AspNetCore.SignalR;
using TutorBooking.APIService.Hubs;
using TutorBooking.APIService.Hubs.ChatHubs;
using TutorBooking.APIService.Hubs.NotificationHubs;
using TutorBooking.APIService.Middleware;
using App.Services.Infras;

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
                            "https://localhost:5173", // Local development with HTTPS
                            "https://ngoai-ngu-ngay.vercel.app", // Deployed frontend
                            "https://tutorbooking-dev-065fe6ad4a6a.herokuapp.com" // Heroku
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
            try
            {
                string credentialPath;
                if (File.Exists("ngoaingungay-firebase-adminsdk-fbsvc-0855eb8d07.json"))
                {
                    credentialPath = "ngoaingungay-firebase-adminsdk-fbsvc-0855eb8d07.json";
                }
                else
                {
                    var firebaseCredJson = Environment.GetEnvironmentVariable("FIREBASE_CREDENTIALS");
                    if (!string.IsNullOrEmpty(firebaseCredJson))
                    {
                        credentialPath = Path.Combine(Path.GetTempPath(), "firebase-credentials.json");
                        File.WriteAllText(credentialPath, firebaseCredJson);
                    }
                    else
                    {
                        throw new FileNotFoundException("Firebase credentials file not found and FIREBASE_CREDENTIALS environment variable is not set.");
                    }
                }
                
                FirebaseApp.Create(new AppOptions
                {
                    Credential = GoogleCredential.FromFile(credentialPath),
                });
            }
            catch (Exception ex)
            {
                // Log lỗi nhưng không làm crash ứng dụng
                var logger = services.BuildServiceProvider().GetService<ILogger<Startup>>();
                logger?.LogError(ex, "Error initializing Firebase. Some features may not work properly.");
            }
            #endregion

            services.AddSingleton<IUserIdProvider, CustomUserIdProvider>();

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
            
            app.UseHangfireDashboard("/hangfire", new DashboardOptions
            {
                Authorization = env.IsDevelopment() ? Array.Empty<IDashboardAuthorizationFilter>() : new[] { new HangfireAuthorizationFilter() },
                IsReadOnlyFunc = (context) => false
            });
            HangfireConfig.ConfigureRecurringJobs();
            #endregion

            if (env.IsDevelopment())
                app.UseHttpsRedirection(); // Chỉ dùng HTTPS Redirection trong Development
            // else 
            // {
            //     // Trong Production, không dùng HTTPS Redirection vì Heroku đã xử lý
            //     // Heroku sẽ tự xử lý SSL termination
            // }

            app.UseRouting();
            app.UseCors("AllowFrontend");

            app.UseMiddleware<ExceptionMiddleware>(); 
            app.UseAuthentication(); 
            app.UseAuthorization(); 
            app.UseMiddleware<PermissionMiddleware>(); 

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
                    options.AllowStatefulReconnects = true;
                });
			});

        }
    }

}
