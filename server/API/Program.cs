using Microsoft.OpenApi.Models;
using RxBox.API.Extensions;
using RxBox.API.Middleware;
using Serilog;

// Logger de arranque: captura fallos que ocurren antes de construir el host.
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(new ConfigurationBuilder()
        .AddJsonFile("appsettings.json")
        .AddJsonFile($"appsettings.{Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production"}.json", optional: true)
        .Build())
    .CreateLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);
    builder.Configuration.AddEnvironmentVariables(prefix: "RXBOX__");

    builder.Host.UseSerilog((context, services, configuration) => configuration
        .ReadFrom.Configuration(context.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext());

    builder.Services.AddControllers();
    builder.Services.AddDatabase(builder.Configuration);
    builder.Services.AddRepositories();
    builder.Services.AddAppServices();
    builder.Services.AddCorsPolicy(builder.Configuration);
    builder.Services.AddJwtAuth(builder.Configuration);
    builder.Services.AddDevelopmentSeed(builder.Configuration);

    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen(c =>
    {
        c.SwaggerDoc("v1", new() { Title = "RxBox API", Version = "v1" });

        var bearer = new OpenApiSecurityScheme
        {
            Name = "Authorization",
            In = ParameterLocation.Header,
            Type = SecuritySchemeType.Http,
            Scheme = "bearer",
            BearerFormat = "JWT",
            Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
        };
        c.AddSecurityDefinition("Bearer", bearer);
        c.AddSecurityRequirement(new OpenApiSecurityRequirement { [bearer] = [] });
    });
    builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
    builder.Services.AddProblemDetails();

    var app = builder.Build();

    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "RxBox API v1"));
    }

    app.UseSerilogRequestLogging();
    app.UseExceptionHandler();
    app.UseCors("RxBoxPolicy");

    app.UseAuthentication();
    app.UseAuthorization();

    app.MapControllers();

    await app.MigrateAsync();
    if (app.Environment.IsDevelopment())
        await app.SeedDevelopmentAsync();

    app.Run();
}
catch (Exception ex) when (ex is not HostAbortedException) // lo lanza a propósito `dotnet ef` al inspeccionar el host
{
    Log.Fatal(ex, "La aplicación no pudo arrancar.");
    Environment.ExitCode = 1;
}
finally
{
    Log.CloseAndFlush();
}
