using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Mvc;
using App.Core.Utils;

namespace App.Core
{
    public static class ConfigCore
    {
        public static IMvcBuilder ConfigureValidation(this IMvcBuilder builder)
        {
            builder.AddMvcOptions(options => {
                options.Filters.Add<ValidationFilter>();
            });

            builder.Services.Configure<ApiBehaviorOptions>(options => {
                options.SuppressModelStateInvalidFilter = true;
            });

            return builder;
        }
    }
}
