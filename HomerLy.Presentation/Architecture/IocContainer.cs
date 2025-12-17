using Homerly.Business.Interfaces;
using Homerly.Business.Services;
using HomerLy.Business.Interfaces;
using HomerLy.Business.Service;
using HomerLy.DataAccess;
using HomerLy.DataAccess.Commons;
using HomerLy.DataAccess.Interfaces;
using HomerLy.DataAccess.Repository;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;
using System.Text;

namespace Homerly.Presentation.Architecture
{
    public static class IocContainer
    {
        public static IServiceCollection SetupIocContainer(this IServiceCollection services)
        {
            services.SetupDbContext();

            //Add generic repositories
            services.AddScoped(typeof(IGenericRepository<>), typeof(GenericRepository<>));

            //Add business services
            services.SetupBusinessServicesLayer();


            services.SetupJwt();
            return services;
        }

        private static IServiceCollection SetupDbContext(this IServiceCollection services)
        {
            IConfiguration configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", true, true)
                .AddEnvironmentVariables()
                .Build();

            // Get the connection string from "DefaultConnection"
            var connectionString = configuration.GetConnectionString("DefaultConnection");

            // ??ng ký DbContext v?i Npgsql - Postgres
            services.AddDbContext<HomerLyDbContext>(options =>
                options.UseNpgsql(connectionString,
                    sql => sql.MigrationsAssembly(typeof(HomerLyDbContext).Assembly.FullName)
                )
            );

            return services;
        }

        public static IServiceCollection SetupBusinessServicesLayer(this IServiceCollection services)
        {
            // Inject service vào DI container
            services.AddScoped<ICurrentTime, CurrentTime>();
            services.AddScoped<IClaimsService, ClaimsService>();
            services.AddScoped<IUnitOfWork, UnitOfWork>();
            services.AddHttpContextAccessor();

            // Register business services
            services.AddScoped<IAuthService, AuthService>();
            services.AddScoped<IAccountService, AccountService>();
            services.AddScoped<IPropertyService, PropertyService>();
            services.AddScoped<IUtilityReadingService, UtilityReadingService>();
            services.AddScoped<ITenancyService, TenancyService>();
            services.AddScoped<IInvoiceService, InvoiceService>();
            services.AddScoped<IPropertyReportService, PropertyReportService>();
            services.AddScoped<IChatService, ChatService>();
            services.AddScoped<IPaymentService, PaymentService>();

            return services;
        }

        private static IServiceCollection SetupJwt(this IServiceCollection services)
        {
            IConfiguration configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", true, true)
                .AddEnvironmentVariables()
                .Build();

            services
                .AddAuthentication(options =>
                {
                    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
                    options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
                })
                .AddJwtBearer(x =>
                {
                    x.SaveToken = true;
                    x.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuer = true,   // B?t ki?m tra Issuer
                        ValidateAudience = true, // B?t ki?m tra Audience
                        ValidateLifetime = true,
                        ValidIssuer = configuration["JWT:Issuer"],
                        ValidAudience = configuration["JWT:Audience"],
                        IssuerSigningKey =
                            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(configuration["JWT:SecretKey"] ??
                                                                            throw new InvalidOperationException())),
                        ClockSkew = TimeSpan.Zero,
                        NameClaimType = ClaimTypes.Name,
                        RoleClaimType = ClaimTypes.Role
                    };
                    x.Events = new JwtBearerEvents
                    {
                        OnMessageReceived = context =>
                        {
                            // Read from Session
                            var token = context.HttpContext.Session.GetString("AuthToken");

                            // For SignalR: read token from query string
                            var accessToken = context.Request.Query["access_token"];
                            var path = context.HttpContext.Request.Path;

                            if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/chatHub"))
                            {
                                context.Token = accessToken;
                            }
                            else if (!string.IsNullOrEmpty(token))
                            {
                                context.Token = token;
                            }

                            return Task.CompletedTask;
                        }
                    };
                });
            services.AddAuthorization(options =>
            {
                options.AddPolicy("UserPolicy", policy =>
                    policy.RequireRole("User"));

                options.AddPolicy("OwnerPolicy", policy =>
                    policy.RequireRole("Owner"));

                options.AddPolicy("AdminPolicy", policy =>
                    policy.RequireRole("Admin"));
            });

            return services;
        }
    }

}
