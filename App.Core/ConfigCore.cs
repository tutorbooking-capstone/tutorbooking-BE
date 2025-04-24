using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Mvc;
using App.Core.Config;
using App.Core.Utils;

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
    }
}
