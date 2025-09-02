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
        
        // Thêm biến để theo dõi memory usage
        private static readonly Timer _memoryMonitorTimer;
        private const int MEMORY_CHECK_INTERVAL_MS = 60000; // 1 phút
        private const int MEMORY_THRESHOLD_MB = 450; // 450MB (90% của 512MB Heroku dyno)
        
        static Startup()
        {
            // Khởi tạo timer để theo dõi memory usage
            _memoryMonitorTimer = new Timer(MonitorMemoryUsage, null, MEMORY_CHECK_INTERVAL_MS, MEMORY_CHECK_INTERVAL_MS);
        }
        
        private static void MonitorMemoryUsage(object state)
        {
            var currentMemory = GC.GetTotalMemory(false) / (1024 * 1024); // MB
            if (currentMemory > MEMORY_THRESHOLD_MB)
            {
                // Log và force GC khi memory usage cao
                Console.WriteLine($"[MEMORY WARNING] High memory usage detected: {currentMemory}MB - forcing GC");
                GC.Collect(2, GCCollectionMode.Forced, true, true);
            }
        }

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
                MaxAutomaticRedirections = 5, // Giảm từ 10 xuống 5
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
                        credentialPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString() + ".json");
                        File.WriteAllText(credentialPath, firebaseCredJson);
                        
                        // Đăng ký cleanup khi app shutdown
                        AppDomain.CurrentDomain.ProcessExit += (s, e) => {
                            if (File.Exists(credentialPath)) File.Delete(credentialPath);
                        };
                    }
                    else
                    {
                        throw new FileNotFoundException("Firebase credentials file not found and FIREBASE_CREDENTIALS environment variable is not set.");
                    }
                }
                
                if (FirebaseApp.DefaultInstance == null)
                {
                    FirebaseApp.Create(new AppOptions
                    {
                        Credential = GoogleCredential.FromFile(credentialPath),
                    });
                }
            }
            catch (Exception ex)
            {
                // Log lỗi nhưng không làm crash ứng dụng
                ILogger<Startup> logger;
                try {
                    logger = services.BuildServiceProvider(validateScopes: true).GetService<ILogger<Startup>>();
                } catch {
                    logger = null;
                }
                logger?.LogError(ex, "Error initializing Firebase. Some features may not work properly.");
            }
            #endregion

            services.AddSingleton<IUserIdProvider, CustomUserIdProvider>();
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            // Thêm middleware giám sát memory
            app.Use(async (context, next) =>
            {
                // Check memory usage mỗi 100 requests
                if (Random.Shared.Next(100) == 0)
                {
                    var currentMemory = GC.GetTotalMemory(false) / (1024 * 1024); // MB
                    var logger = context.RequestServices.GetService<ILogger<Startup>>();
                    
                    if (currentMemory > MEMORY_THRESHOLD_MB)
                    {
                        logger?.LogWarning("Memory usage high: {MemoryMB}MB - forcing GC", currentMemory);
                        GC.Collect(2, GCCollectionMode.Forced, true);
                    }
                }
                
                await next();
            });

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
            if (env.IsDevelopment())
                app.UseMiniProfiler();

            app.UseSwagger();
            app.UseSwaggerUI();
            
            app.UseHangfireDashboard("/hangfire", new DashboardOptions
            {
                Authorization = env.IsDevelopment() ? Array.Empty<IDashboardAuthorizationFilter>() : new[] { new HangfireAuthorizationFilter() },
                IsReadOnlyFunc = (context) => true
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

                    options.ApplicationMaxBufferSize = 32 * 1024; // Giảm từ 64KB xuống 32KB
                    options.TransportMaxBufferSize = 32 * 1024;   // Giảm từ 64KB xuống 32KB
                    options.AllowStatefulReconnects = false;
                    
                    // Thêm giới hạn thời gian
                    // Thiết lập thời gian chờ tối đa cho Long Polling là 7 giây
                    // Điều này giúp giảm tải cho server bằng cách giới hạn thời gian mỗi request long polling có thể giữ connection mở
                    // Sau 7 giây, request sẽ tự động kết thúc và client cần gửi request mới để tiếp tục nhận dữ liệu
                    options.LongPolling.PollTimeout = TimeSpan.FromSeconds(7);
                });

                endpoints.MapHub<NotificationHub>("/notification-hub", options =>
                {
                    options.Transports =
                        Microsoft.AspNetCore.Http.Connections.HttpTransportType.WebSockets |
                        Microsoft.AspNetCore.Http.Connections.HttpTransportType.LongPolling;

                    options.ApplicationMaxBufferSize = 32 * 1024; // Giảm từ 64KB xuống 32KB
                    options.TransportMaxBufferSize = 32 * 1024;   // Giảm từ 64KB xuống 32KB
                    options.AllowStatefulReconnects = false;
                    
                    // Thêm giới hạn thời gian
                    options.LongPolling.PollTimeout = TimeSpan.FromSeconds(7);
                });
            });
        }
    }
}
