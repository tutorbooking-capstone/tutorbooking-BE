using FluentValidation;
using FluentValidation.AspNetCore;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace App.DTOs
{
    public static class DTOsConfig
    {
        public static IServiceCollection AddAppDTOsConfig(this IServiceCollection services)
        {
            services.AddDTOsValidation();

            return services;
        }

        #region Add Sub Services
        public static IServiceCollection AddDTOsValidation(this IServiceCollection services)
        {
            services.AddFluentValidationAutoValidation();

            // Đăng ký tất cả validator trong assembly
            services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());

            // Hoặc đăng ký từng validator riêng lẻ
            //services.AddScoped<IValidator<TutorLanguageDTO>, TutorLanguageDTOValidator>();
            //services.AddTransient<IValidator<TutorLanguageDTO>, TutorLanguageDTOValidator>();
            return services;
        }
        #endregion
    }
}
