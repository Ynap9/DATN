using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using NLog;
using NLog.Web;
using OpenIddict.Abstractions;
using Scalar.AspNetCore;
using sh.be.api.Swagger;
using sh.be.api.Worker;
using sh.be.applications.Base;
using sh.be.applications.Module.Auth.Implements;
using sh.be.applications.Module.Auth.Interfaces;
using sh.be.applications.Module.Core.Implements;
using sh.be.applications.Module.Core.Interfaces;
using sh.be.domain.Module.Auth;
using sh.be.infrastructure.data.Persistence.Db;
using sh.be.infrastructure.data.Persistence.Seeder;
using sh.be.infrastructure.external.Mail.Implements;
using sh.be.infrastructure.external.Mail.Interfaces;
using sh.be.shared.Constants.Auth;
using sh.be.shared.Constants.Swagger;
using sh.be.shared.Settings;
using System.Net.WebSockets;
using System.Reflection.Metadata;
using System.Text;

var logger = LogManager.Setup().LoadConfigurationFromAppSettings().GetCurrentClassLogger();
logger.Info("Starting application...");
var builder = WebApplication.CreateBuilder(args);

Console.WriteLine("EnvironmentName => " + builder.Environment.EnvironmentName);


builder.Logging.ClearProviders();
builder.Host.UseNLog();

#region db
string connectionString = builder.Configuration.GetConnectionString("SO_HOA_DB")
    ?? throw new InvalidOperationException("Không tìm thấy connection string \"SO_HOA_DB\" trong appsettings.json");



builder.Services.AddDbContext<ShDbContext>(options =>
{
    options.UseSqlServer(connectionString, options =>
    {
        //options.MigrationsAssembly(typeof(Program).Namespace);
        //options.MigrationsHistoryTable(DbSchemas.TableMigrationsHistory, DbSchemas.Core);
        options.CommandTimeout(600);
    });
    //options.UseOpenIddict(); // Register OpenIddict entities
}, ServiceLifetime.Scoped);
#endregion

#region cors
string allowOrigins = builder.Configuration.GetSection("AllowedHosts")!.Value!;
//File.WriteAllText("cors.now.txt", $"CORS: {allowOrigins}");;/'\,
Console.WriteLine($"CORS: {allowOrigins}");
var origins = allowOrigins
    .Split(';')
    .Where(s => !string.IsNullOrWhiteSpace(s))
    .ToArray();
builder.Services.AddCors(options =>
{
    options.AddPolicy(
        ProgramExtensions.CorsPolicy,
        builder =>
        {
            builder
                .WithOrigins(origins)
                .AllowAnyMethod()
                .AllowAnyHeader()
                //.AllowCredentials()
                .WithExposedHeaders("Content-Disposition");
        }
    );
    options.AddPolicy("SignalRPolicy", builder =>
    {
        builder
            .SetIsOriginAllowed(_ => true)
            .AllowAnyMethod()
            .AllowAnyHeader()
            .AllowCredentials();
    });
});
#endregion
#region identity
// 2. Add Identity
builder.Services.AddDataProtection();
builder.Services.AddIdentityCore<AppUser>()

    .AddEntityFrameworkStores<ShDbContext>()
    .AddDefaultTokenProviders();
#endregion

#region mapper
// Build mapper configuration
builder.Services.AddAutoMapper(cfg => { }, typeof(MappingProfile));
#endregion

#region auth
string secretKey = builder.Configuration.GetSection("AuthServer:SecretKey").Value!;

builder.Services.Configure<AuthServerSettings>(builder.Configuration.GetSection("AuthServer"));
builder.Services.Configure<EmailSettings>(builder.Configuration.GetSection("Email"));
//builder.Services.Configure<S3Settings>(builder.Configuration.GetSection("S3"));

builder.Services.AddOpenIddict()
    .AddCore(opt =>
    {
        opt.UseEntityFrameworkCore()
           .UseDbContext<ShDbContext>();
    })
    // Register the OpenIddict server components.
    .AddServer(options =>
    {
        // Enable the token endpoint.
        options.SetTokenEndpointUris("connect/token")
            .SetAuthorizationEndpointUris("/connect/authorize")
        ;

        // Enable the client credentials flow.
        options.AllowClientCredentialsFlow()
                .AllowPasswordFlow()
                .AllowRefreshTokenFlow()
                .AllowAuthorizationCodeFlow()
                .RequireProofKeyForCodeExchange()
                ;

        options.AcceptAnonymousClients();
        options.DisableAccessTokenEncryption();

        options.RegisterScopes(OpenIddictConstants.Scopes.OpenId, OpenIddictConstants.Scopes.OfflineAccess, OpenIddictConstants.Scopes.Profile);

        // Register the signing and encryption credentials.
        //options.AddDevelopmentEncryptionCertificate()
        //       .AddDevelopmentSigningCertificate();

        // Development: ephemeral keys (or dev certs if you want)
        options.AddEphemeralEncryptionKey()
               .AddEphemeralSigningKey();

        // ?? Symmetric signing key
        var secret = Encoding.UTF8.GetBytes(secretKey);
        options.AddEncryptionKey(new SymmetricSecurityKey(secret));
        options.AddSigningKey(new SymmetricSecurityKey(secret));

        // Register the ASP.NET Core host and configure the ASP.NET Core options.
        options.UseAspNetCore()
                .EnableAuthorizationEndpointPassthrough()
               .EnableTokenEndpointPassthrough()
               .DisableTransportSecurityRequirement();

    });

builder.Services.AddAuthentication(options =>
{
    //options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultAuthenticateScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    options.DefaultSignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
})
    .AddJwtBearer(
        options =>
        {
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateLifetime = true,        // ? only check exp & nbf
                ClockSkew = TimeSpan.Zero, // no extra leeway

                ValidateIssuer = false,
                ValidateAudience = false,
                ValidateIssuerSigningKey = true,
                // Symmetric key (HMAC) example
                IssuerSigningKey = new SymmetricSecurityKey(
                    Encoding.UTF8.GetBytes(secretKey)
                ),

                // ?? Accept both "JWT" and "at+jwt" as token types
                ValidTypes = new[] { "JWT", "at+jwt" }
            };
            options.RequireHttpsMetadata = false;
        }
    )
    .AddCookie(CookieAuthenticationDefaults.AuthenticationScheme);



builder.Services.AddAuthorization();


builder.Services.AddHostedService<AuthWorker>();
#endregion

#region services
// Add other services here
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IRoleService, RoleService>();
builder.Services.AddScoped<IMailService, MailService>();
builder.Services.AddScoped<IPermissionService, PermissionService>();
builder.Services.AddScoped<IAuditLogService, AuditLogService>();
#endregion


builder.Services.AddHttpClient();
builder.Services.AddHttpContextAccessor();
builder.Services.AddControllers(o => o.Conventions.Add(new ModuleGroupConvention()));
builder.Services.AddHealthChecks();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen(c =>
{
    //c.SwaggerDoc("v1", new() { Title = "My API", Version = "v1" });

    //add config swagger cho từng module
    c.SwaggerDoc(SwaggerConstants.ALL, new() { Title = "Sh API - ALL", Version = "v1" });
    foreach(var module in SwaggerConstants.MODULES)
    {
        c.SwaggerDoc(module, new() { Title = $"Sh API - {module.ToLower()}", Version = "v1" });
    }

    c.DocInclusionPredicate((docName, apiDesc) => docName == SwaggerConstants.ALL || apiDesc.GroupName == docName);
    //c.OperationFilter<ModuleTagOperationFiltor>();

    c.DocumentFilter<ModuleTagGroupDocumentFilter>();

    // 🔑 Add Bearer JWT Security Definition
    c.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.ApiKey,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Description = "Enter 'Bearer' [space] and then your valid token.\r\n\r\nExample: \"Bearer eyJhbGciOiJIUzI1NiIs...\"",
    });

    // 🔐 Add Security Requirement (apply globally to all endpoints)
    c.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
    {
        {
            new Microsoft.OpenApi.Models.OpenApiSecurityScheme
            {
                Reference = new Microsoft.OpenApi.Models.OpenApiReference
                {
                    Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            new string[] {}
        }
    });
});

var app = builder.Build();


#region Seed data
// Run seeding inside scope
using (var scope = app.Services.CreateScope())
{
    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();
    var shDbContext = scope.ServiceProvider.GetRequiredService<ShDbContext>();

    await SeedRole.SeedAsync(shDbContext);
    await SeedPermission.SeedAsync(shDbContext);
    await SeedUser.SeedAsync(shDbContext,userManager);
    await SeedRolePermission.SeedAsync(shDbContext);
}
#endregion


// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint($"/swagger/{SwaggerConstants.ALL}/swagger.json", SwaggerConstants.ALL.ToUpper());
        foreach(var module in SwaggerConstants.MODULES)
        {
            c.SwaggerEndpoint($"/swagger/{module}/swagger.json", module.ToUpper());
        }
    });

    app.MapScalarApiReference(options =>
    {
    options.AddDocument(SwaggerConstants.ALL, SwaggerConstants.ALL.ToUpper(),
        $"/swagger/{SwaggerConstants.ALL}/swagger.json", isDefault: true);
        foreach (var module in SwaggerConstants.MODULES){
            options.AddDocument(module, module.ToUpper(),
                $"/swagger/{module}/swagger.json");
        }
        options.AddPreferredSecuritySchemes("Bearer");

    });
}

app.UseCors(ProgramExtensions.CorsPolicy);
app.UseStaticFiles();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();


app.MapHealthChecks("/health");
app.Run();


