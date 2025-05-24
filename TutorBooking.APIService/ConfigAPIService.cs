using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Reflection;
using System.Text.Json.Serialization;
using System.Text;
using App.Core;
using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;
using TutorBooking.APIService.Middleware;

namespace TutorBooking.APIService
{
    public static class ConfigAPIService
    {
        public static IServiceCollection AddAppAPIConfig(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            services.ConfigRoute();
            services.AddAuthenJwt(configuration);
            services.ConfigSwagger();
            services.ConfigureValidation();
			services.ConfigureSignalR();
            services.ConfigureControllers();

            return services;
        }

        #region Add Sub Services
        public static IServiceCollection ConfigRoute(this IServiceCollection services)
        {
            services.Configure<RouteOptions>(options =>
            {
                options.LowercaseUrls = true;
            });

            return services;
        }

        public static IServiceCollection AddAuthenJwt(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            var jwtSettings = configuration.GetSection("JwtSettings");
            services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            }).AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = jwtSettings["Issuer"],
                    ValidateAudience = true,
                    ValidAudience = jwtSettings["Audience"],
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings["SecretKey"] ?? string.Empty)),
                    NameClaimType = JwtRegisteredClaimNames.Sub,
                    RoleClaimType = ClaimTypes.Role
                };
                options.SaveToken = true;
                options.RequireHttpsMetadata = true;
                options.Events = new JwtBearerEvents
                {
                    OnAuthenticationFailed = context =>
                    {
                        var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Startup>>();
                        logger.LogError(context.Exception, "[AUTH FAILED] Authentication failed for request path {Path}", context.Request.Path);
                        Console.WriteLine($"\n\n\n[Authentication failed]\n {context.Exception.Message}\n\n\n");
                        return Task.CompletedTask;
                    },
                    OnTokenValidated = context =>
                    {
                        var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Startup>>();
                        var claims = context.Principal?.Claims.Select(c => $"{c.Type}: {c.Value}") ?? Enumerable.Empty<string>();
                        var roles = context.Principal?.FindAll(ClaimTypes.Role).Select(c => c.Value) ?? Enumerable.Empty<string>();
                        var userId = context.Principal?.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "N/A";

                        logger.LogInformation("[TOKEN VALIDATED] User ID: {UserId}, Path: {Path}", userId, context.Request.Path);
                        logger.LogDebug("[TOKEN VALIDATED] Claims: [{Claims}]", string.Join(", ", claims));
                        logger.LogDebug("[TOKEN VALIDATED] Roles found in token: [{Roles}]", string.Join(", ", roles));

                        return Task.CompletedTask;
                    },
					OnMessageReceived = context =>
					{
						var authHeader = context.Request.Headers.Authorization.ToString();

						if (!string.IsNullOrEmpty(authHeader) && authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
							context.Token = authHeader.Substring("Bearer ".Length).Trim();
                            
						return Task.CompletedTask;
					}
				};
            });

            return services;
        }

        public static IServiceCollection ConfigSwagger(this IServiceCollection services)
        {
            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo
                {
                    Version = "v1",
                    Title = "API"
                });

                c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
                {
                    In = ParameterLocation.Header,
                    Description = "JWT Authorization header sử dụng scheme Bearer.",
                    Type = SecuritySchemeType.Http,
                    Name = "Authorization",
                    Scheme = "Bearer"
                });

                c.AddSecurityRequirement(new OpenApiSecurityRequirement
                {
                    {
                        new OpenApiSecurityScheme
                        {
                            Reference = new OpenApiReference
                            {
                                Type = ReferenceType.SecurityScheme,
                                Id = "Bearer"
                            }
                        },
                        new string[] { }
                    }
                });

                var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
                var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
                if (File.Exists(xmlPath))
                {
                    c.IncludeXmlComments(xmlPath);
                }
            });

            services.AddAuthorization(options =>
            {
                options.AddPolicy("RequireAdministratorRole", policy => policy.RequireRole("Administrator"));
            });

            return services;
        }

        public static IServiceCollection ConfigJsonOptions(this IServiceCollection services)
        {
            services.AddControllers()
                .AddJsonOptions(options =>
                {
                    options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
                    options.JsonSerializerOptions.WriteIndented = true;
                });

            return services;
        }

        public static IServiceCollection ConfigHttpClient(this IServiceCollection services)
        {
            services.AddHttpClient();
            return services;
        }

        public static IServiceCollection ConfigureValidation(this IServiceCollection services)
        {
            services.AddControllers()
                .ConfigureValidation();

            return services;
        }

        public static IServiceCollection ConfigureFilters(this IServiceCollection services)
        {
            services.AddControllers(options =>
            {
                options.Filters.Add<AuthorizationLogFilter>();
            });

            return services;
        }

        public static IServiceCollection ConfigureControllers(this IServiceCollection services)
        {
            services.AddControllers(options =>
            {
                options.Filters.Add<AuthorizationLogFilter>();
            })
            .ConfigureValidation()
            .AddJsonOptions(options =>
            {
                options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
                options.JsonSerializerOptions.WriteIndented = true;
            });

            return services;
        }

		public static IServiceCollection ConfigureSignalR(this IServiceCollection services)
		{
			// Add SignalR with optional configuration
			services.AddSignalR(options =>
			{
				options.EnableDetailedErrors = true;
				options.MaximumReceiveMessageSize = 102400; // 100 KB
				options.ClientTimeoutInterval = TimeSpan.FromSeconds(60);
				options.KeepAliveInterval = TimeSpan.FromSeconds(30);
				options.StatefulReconnectBufferSize = 1000;
			});
			return services;
		}
        #endregion
    }
}
