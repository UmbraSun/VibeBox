using Api.Services;
using Application.Common.Interfaces;
using Application.Common.Settings;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.SignalR;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using System.Text;

namespace Api.Extensions;

public static class DependencyInjectionExtensions
{
    public static IServiceCollection AddApiServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddControllers();
        services.AddCorsConfig();
        services.AddSwagger();
        services.AddAuthorization(configuration);
        services.AddHttpContextAccessor();
        services.AddServices();
        services.AddSignalRConf();

        return services;
    }

    private static void AddCorsConfig(this IServiceCollection services)
    {
        services.AddCors(options =>
        {
            options.AddPolicy("ReactClient", policy =>
            {
                policy
                    .WithOrigins("http://localhost:5173")
                    .AllowAnyHeader()
                    .AllowAnyMethod()
                    .AllowCredentials();
            });
        });
    }

    private static void AddSwagger(this IServiceCollection services)
    {
        services.AddEndpointsApiExplorer();

        services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = "VibeBox API",
                Version = "v1",
                Description = "VibeBox backend API"
            });

            options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            {
                Name = "Authorization",
                Type = SecuritySchemeType.Http,
                Scheme = "bearer",
                BearerFormat = "JWT",
                In = ParameterLocation.Header,
                Description = "Enter the JWT access token."
            });

            options.AddSecurityRequirement(document =>
                new OpenApiSecurityRequirement
                {
                    [new OpenApiSecuritySchemeReference("Bearer", document)] = []
                });
        });
    }

    private static void AddAuthorization(this IServiceCollection services, IConfiguration configuration)
    {
        var jwtSettings = configuration
            .GetSection(JwtSettings.SectionName)
            .Get<JwtSettings>()
            ?? throw new InvalidOperationException("JWT settings were not configured.");

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = jwtSettings.Issuer,

                    ValidateAudience = true,
                    ValidAudience = jwtSettings.Audience,

                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(
                        Encoding.UTF8.GetBytes(jwtSettings.SecretKey)),

                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.Zero
                };
            });

        services.AddAuthorization();
    }

    private static void AddServices(this IServiceCollection services)
    {
        services.AddScoped<ICurrentUserService, CurrentUserService>();
    }

    private static void AddSignalRConf(this IServiceCollection services)
    {
        services.AddSignalR(options =>
        {
            options.EnableDetailedErrors = true;
            options.MaximumReceiveMessageSize = 1024 * 1024; // 1 MB
        });

        services.AddSingleton<IUserIdProvider, SignalRUserIdProvider>();

        services.PostConfigure<JwtBearerOptions>(
            JwtBearerDefaults.AuthenticationScheme,
            options =>
            {
                var previousHandler = options.Events.OnMessageReceived;

                options.Events.OnMessageReceived = async context =>
                {
                    if (previousHandler is not null)
                    {
                        await previousHandler(context);
                    }

                    if (string.IsNullOrEmpty(context.Token) &&
                        context.Request.Path.StartsWithSegments(
                            "/hubs/call-signaling"))
                    {
                        var accessToken =
                            context.Request.Query["access_token"];

                        if (!string.IsNullOrEmpty(accessToken))
                        {
                            context.Token = accessToken;
                        }
                    }
                };
            });

        services.AddSingleton<RoomConnectionTracker>();
    }
}