using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using RxBox.Core.Entities;
using RxBox.Core.Interfaces;
using RxBox.Core.Interfaces.Repositories;
using RxBox.Core.Options;
using RxBox.Infrastructure.Auth;
using RxBox.Infrastructure.Data;
using RxBox.Infrastructure.Data.Interceptors;
using RxBox.Infrastructure.Data.Seed;
using RxBox.Infrastructure.Repositories;

namespace RxBox.API.Extensions
{
    public static class ServiceExtensions
    {
        public static IServiceCollection AddDatabase(this IServiceCollection services, IConfiguration config)
        {
            services.AddHttpContextAccessor();
            services.AddScoped<ICurrentUserService, CurrentUserService>();
            services.AddScoped<AuditoriaSaveChangesInterceptor>();

            services.AddDbContext<AppDbContext>((sp, opt) =>
                opt.UseNpgsql(
                        config.GetConnectionString("Postgres"),
                        npgsql => npgsql.MigrationsAssembly("RxBox.Infrastructure"))
                   .AddInterceptors(sp.GetRequiredService<AuditoriaSaveChangesInterceptor>()));

            return services;
        }

        public static IServiceCollection AddRepositories(this IServiceCollection services)
        {
            services.AddScoped<IUnitOfWork, UnitOfWork>();
            return services;
        }

        /// <summary>
        /// Registra aquí cada servicio de aplicación nuevo:
        ///   services.AddScoped&lt;ICategoriaService, CategoriaService&gt;();
        /// </summary>
        public static IServiceCollection AddAppServices(this IServiceCollection services)
        {
            services.AddScoped<IAuthService, AuthService>();
            services.AddScoped<ICatalogoSeedService, CatalogoSeedService>();
            return services;
        }

        public static IServiceCollection AddDevelopmentSeed(this IServiceCollection services, IConfiguration config)
        {
            services.AddOptions<SeedOptions>().Bind(config.GetSection("Seed"));
            services.AddScoped<DevelopmentSeeder>();
            return services;
        }

        public static IServiceCollection AddCorsPolicy(this IServiceCollection services, IConfiguration config)
        {
            var origins = config.GetSection("Cors:Origins").Get<string[]>() ?? [];

            services.AddCors(options => options.AddPolicy("RxBoxPolicy", policy =>
                policy.WithOrigins(origins)
                      .AllowAnyHeader()
                      .AllowAnyMethod()
                      .AllowCredentials()));

            return services;
        }

        /// <summary>
        /// JWT propio. Las opciones se resuelven al arrancar el host: sin un
        /// "Jwt:Secret" de al menos 32 bytes la API no levanta.
        /// </summary>
        public static IServiceCollection AddJwtAuth(this IServiceCollection services, IConfiguration config)
        {
            services.AddOptions<JwtOptions>()
                .Bind(config.GetSection("Jwt"))
                .Validate(o => Encoding.UTF8.GetByteCount(o.Secret) >= 32, "'Jwt:Secret' debe tener al menos 32 bytes.")
                .ValidateOnStart();

            services.AddSingleton<JwtTokenService>();
            services.AddSingleton<IPasswordHasher<Usuario>, PasswordHasher<Usuario>>();

            services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer();
            services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
                .Configure<JwtTokenService, IOptions<JwtOptions>>((opt, tokens, jwt) =>
                {
                    opt.MapInboundClaims = false; // conserva los nombres de claim tal como se emiten (sub, box_id, rol)
                    opt.TokenValidationParameters = tokens.ParametrosValidacion(jwt.Value.Audience);
                    opt.TokenValidationParameters.NameClaimType = "sub";
                    opt.TokenValidationParameters.RoleClaimType = JwtTokenService.ClaimRol;

                    opt.Events = new JwtBearerEvents
                    {
                        OnAuthenticationFailed = ctx =>
                        {
                            ctx.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>()
                                .LogWarning("JWT rechazado: {Motivo}", ctx.Exception.GetType().Name);
                            return Task.CompletedTask;
                        }
                    };
                });

            services.AddAuthorization();
            return services;
        }
    }
}
