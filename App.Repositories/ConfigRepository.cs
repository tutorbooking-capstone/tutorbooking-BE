using App.Core.Config;
using App.Repositories.Context;
using App.Repositories.Models.User;
using App.Repositories.States;
using App.Repositories.UoW;
using FluentValidation;
using FluentValidation.AspNetCore;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace App.Repositories
{
    public static class ConfigRepository
    {
        public static IServiceCollection AddAppRepositoriesConfig(
            this IServiceCollection services,
            IConfiguration configuration,
            string connectionStringName = "DeployConnection")
        {
            services.AddAppDbContext(configuration, connectionStringName);
            services.AddModelsValidation();

            services.AddAppIdentityConfig<AppUser, IdentityRole>()
                .AddEntityFrameworkStores<AppDbContext>()
                .AddDefaultTokenProviders();

            services.AddScoped<IUnitOfWork, UnitOfWork>();
            services.AddStateManagers();
            return services;
        }

        #region Add Sub Services
        public static IServiceCollection AddAppDbContext(
            this IServiceCollection services,
            IConfiguration configuration,
            string connectionStringName)
        {
            var connectionString = configuration.GetConnectionString(connectionStringName);
            if (string.IsNullOrEmpty(connectionString))
                throw new InvalidOperationException($"Connection string '{connectionStringName}' not found in configuration.");
                
            // services.AddDbContext<AppDbContext>(options =>
            //     options.UseSqlServer(connectionString));
            services.AddDbContext<AppDbContext>(options => 
                options.UseNpgsql(
                    connectionString,
                    npgsqlOptions => npgsqlOptions.MigrationsHistoryTable("__efmigrations_history", "public")
                ));
                
            return services;
        }

        public static IServiceCollection AddModelsValidation(this IServiceCollection services)
        {
            services.AddFluentValidationAutoValidation();
            services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());
            return services;
        }

        public static IServiceCollection AddStateManagers(this IServiceCollection services)
        {
            services.AddScoped<ApplicationRevisionStateManager>();
            services.AddScoped<TutorApplicationStateManager>();
            services.AddScoped<TutorStateManager>();

            services.AddScoped<StateOrchestrator>();

            return services;
        }
        #endregion
    }
}