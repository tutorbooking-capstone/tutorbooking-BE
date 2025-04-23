using App.Repositories;
using App.Services;
using App.DTOs;
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
            services.AddAppRepositoriesConfig(Configuration);
            services.AddAppServicesConfig(Configuration);
            services.AddAppDTOsConfig();
            services.AddAppAPIConfig(Configuration);
            #endregion

            #region Add Cors
            services.AddCors(options =>
            {
                options.AddPolicy("AllowAll", builder =>
                {
                    builder.AllowAnyOrigin()
                            .AllowAnyMethod()
                            .AllowAnyHeader();
                });
            });
            #endregion

        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            // Middleware handle logging, better for debug engineering :>>>
            app.UseMiddleware<RequestLogSeparatorMiddleware>();

            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseHttpsRedirection();
            app.UseRouting();
            app.UseCors("AllowAll");

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
            });
        }
    }

}
