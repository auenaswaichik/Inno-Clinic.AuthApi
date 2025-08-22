using AuthApi.DbContexts;
using AuthApi.Interfaces.IRepositories;
using AuthApi.Interfaces.IServices;
using AuthApi.Repositories;
using AuthApi.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using DotNetEnv;
using AuthApi.Options;
using AuthApi.Extensions;
using MassTransit;
using AuthApi.Messages.UserCreatedMessages;

Env.Load();

var builder = WebApplication.CreateBuilder(args);

builder.Configuration.AddEnvironmentVariables();

builder.Services.Configure<KeycloakOptions>(builder.Configuration.GetSection("Keycloak"));

builder.Services
    .AddDbContext<AuthApiDbContext>
    (
        options =>
            options.UseSqlServer
            (
                builder.Configuration.GetConnectionString("sqlConnection")
            )
    );

builder.Services.AddHttpClient();

builder.Services.AddHttpClient("KeycloakClient", client =>
{
    var keycloakBaseUrl = builder.Configuration["Keycloak:BaseUrl"];
    client.BaseAddress = new Uri($"{keycloakBaseUrl}");
});

builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IAuthService, AuthService>();

builder.Services.AddControllers();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        var keycloakBaseUrl = builder.Configuration["Keycloak:BaseUrl"];
        var realm = builder.Configuration["Keycloak:Realm"];

        options.Authority = $"http://{keycloakBaseUrl}/realms/{realm}";
        options.Audience = builder.Configuration["Keycloak:ClientId"];
        options.RequireHttpsMetadata = false; 
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateAudience = true,
            ValidateIssuer = true,
            RoleClaimType = "roles"
        };
    });

builder.Services.AddAuthorization();

builder.Services.AddMassTransit(x =>
{
    x.AddConsumer<UserCreatedConsumer>();

    x.UsingRabbitMq((context, cfg) =>
    {
        cfg.Host(builder.Configuration["RabbitMQ:Host"], "/", h =>
        {
            h.Username(builder.Configuration["RabbitMQ:Username"]);
            h.Password(builder.Configuration["RabbitMQ:Password"]);
        });

        cfg.ReceiveEndpoint("user-created", e =>
        {
            e.PrefetchCount = 32;
            e.Durable = true;
            e.AutoDelete = false;

            e.UseInMemoryOutbox();
            e.UseMessageRetry(r => r.Interval(5, TimeSpan.FromSeconds(10)));

            e.ConfigureConsumer<UserCreatedConsumer>(context);
        });
    });
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.EnsureDatabaseMigration();

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
