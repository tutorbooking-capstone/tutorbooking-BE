using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Mvc;
using App.Core.Config;
using App.Core.Utils;
using StackExchange.Profiling.SqlFormatters;
using StackExchange.Profiling;

namespace App.Core
{
    public static class ConfigCore
    {
        public static IMvcBuilder ConfigureValidation(this IMvcBuilder builder)
        {
            builder.Services.Configure<ApiBehaviorOptions>(options => {
                options.SuppressModelStateInvalidFilter = true;
            });

            builder.AddMvcOptions(options => {
                options.Filters.Add<ValidationFilter>();
            });     
            
            return builder;
        }

        public static IServiceCollection AddAppCoreConfig(this IServiceCollection services)
        {
            services.AddScoped<DatabaseQueryTracker>();
            return services;
        }

        public static IServiceCollection AddMiniProfilerConfig(this IServiceCollection services)
        {
            services.AddMiniProfiler(options =>
            {
                // Base path cho UI (truy cập qua /profiler)
                options.RouteBasePath = "/profiler";
                
                // Formatter cho SQL (chọn 1 trong các options dưới)
                //options.SqlFormatter = new InlineFormatter(); // Format SQL ngắn gọn
                options.SqlFormatter = new VerboseSqlServerFormatter(); // Format chi tiết
                // options.SqlFormatter = new SqlServerFormatter(); // Format mặc định
                
                // Theo dõi mở/đóng kết nối database
                options.TrackConnectionOpenClose = true;
                
                // Hiển thị thời gian server trong header response
                options.EnableServerTimingHeader = true;
                
                // Bật theo dõi các child actions
                options.EnableMvcFilterProfiling = true;
                
                // Bật theo dõi các view rendering
                options.EnableMvcViewProfiling = true;
                
                // Bật debug mode
                options.EnableDebugMode = true;
                
                // Bật hiển thị thời gian cho các custom steps
                options.ShouldProfile = request => true; // Profile mọi request
                
                // Tùy chỉnh màu sắc UI
                options.PopupRenderPosition = RenderPosition.Right; // Vị trí popup
                options.PopupShowTimeWithChildren = true;
                
                // Bỏ qua các path cụ thể
                options.IgnoredPaths.Add("/swagger");
                //options.IgnoredPaths.Add("/healthcheck");
            })
            .AddEntityFramework(); // Theo dõi Entity Framework queries

            
            return services;
        }
    }
}
