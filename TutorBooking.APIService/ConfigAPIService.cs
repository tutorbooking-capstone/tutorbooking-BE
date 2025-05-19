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
using Microsoft.AspNetCore.Identity;
using App.Repositories.Models.User;
using App.Core.Utils;
using Microsoft.Extensions.Options;
using Microsoft.CodeAnalysis;
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Interfaces;

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
                    }
                };
            })

			// firebase
			.AddJwtBearer("Firebase ", options =>
			{
				var projectId = "nnn-capstone";
				var client = new HttpClient();
				var keys = client
					.GetStringAsync(
						"https://www.googleapis.com/robot/v1/metadata/x509/securetoken@system.gserviceaccount.com")
					.Result;
				var originalKeys = new JsonWebKeySet(keys).GetSigningKeys();
				var additionalkeys = client
					.GetStringAsync(
						"https://www.googleapis.com/service_accounts/v1/jwk/securetoken@system.gserviceaccount.com")
					.Result;
				var morekeys = new JsonWebKeySet(additionalkeys).GetSigningKeys();
				var totalkeys = originalKeys.Concat(morekeys);


				options.IncludeErrorDetails = true;
				options.Authority = $"https://securetoken.google.com/{projectId}";
				options.TokenValidationParameters = new TokenValidationParameters
				{
					ValidateIssuer = true,
					ValidIssuer = $"https://securetoken.google.com/{projectId}",
					ValidateAudience = true,
					ValidAudience = projectId,
					ValidateLifetime = true,
					ValidateIssuerSigningKey = true,
					IssuerSigningKeys = totalkeys
				};

				options.Events = new JwtBearerEvents
				{
					OnTokenValidated = async context =>
					{
						// Receive the JWT token that firebase has provided
						var firebaseToken = context.SecurityToken as Microsoft.IdentityModel.JsonWebTokens.JsonWebToken;
						// Get the Firebase UID of this user
						var firebaseUid = firebaseToken?.Claims.FirstOrDefault(c => c.Type == "user_id")?.Value;
						if (!string.IsNullOrEmpty(firebaseUid))
						{
							// Use the Firebase UID to find or create the user in your Identity system
							var userManager = context.HttpContext.RequestServices
								.GetRequiredService<UserManager<AppUser>>();
							var user = await userManager.FindByNameAsync(firebaseUid);

							if (user == null)
							{
								var newUser = new AppUser
								{
									Id = Guid.NewGuid().ToString("N"),
									FirebaseUserId = firebaseUid,
									Email = firebaseToken.Claims.FirstOrDefault(c => c.Type == "email")?.Value,
									UserName = firebaseUid,
									NormalizedEmail = userManager.KeyNormalizer.NormalizeEmail(firebaseToken.Claims.FirstOrDefault(c => c.Type == "email")?.Value),
									NormalizedUserName = userManager.KeyNormalizer.NormalizeName(firebaseToken.Claims.FirstOrDefault(c => c.Type == "email")?.Value),
									FullName = firebaseToken.Claims.FirstOrDefault(c => c.Type == "name")?.Value ??
										   $"Firebase {firebaseUid}",
									SecurityStamp = Guid.NewGuid().ToString(),
									PasswordHash = "",
									PhoneNumberConfirmed = true,
									EmailConfirmed = true,
									CreatedTime = DateTime.UtcNow,
									CodeGeneratedTime = DateTime.UtcNow
								};
								await userManager.CreateAsync(user);
							}
						}
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

				//Firebase
				c.AddSecurityDefinition("Firebase", new OpenApiSecurityScheme
				{
					Type = SecuritySchemeType.OAuth2,

					Flows = new OpenApiOAuthFlows
					{
						Password = new OpenApiOAuthFlow
						{
							TokenUrl = new Uri("/api/auth/login-google", UriKind.Relative),
							Extensions = new Dictionary<string, IOpenApiExtension>
				{
					{ "returnSecureToken", new OpenApiBoolean(true) },
							},
						}
					}
				});

				c.AddSecurityRequirement(new OpenApiSecurityRequirement
				{
					{
						new OpenApiSecurityScheme
						{
							Reference = new OpenApiReference
							{
								Type = ReferenceType.SecurityScheme,
								Id = JwtBearerDefaults.AuthenticationScheme
							},
							Scheme = "oauth2",
							Name = JwtBearerDefaults.AuthenticationScheme,
							In = ParameterLocation.Header,
						},
						new List<string> { "openid", "email", "profile" }
					}
				});
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
        #endregion
    }
}
