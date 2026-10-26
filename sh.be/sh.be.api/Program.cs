using Asp.Versioning;
using Asp.Versioning.ApiExplorer;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Cors.Infrastructure;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using NLog;
using NLog.Web;
using OpenIddict.Abstractions;
using Scalar.AspNetCore;
using sh.be.api.Services;
using sh.be.api.Swagger;
using sh.be.api.Worker;
using sh.be.applications.Base;
using sh.be.applications.Base.Interfaces.External.DataReader;
using sh.be.applications.Base.Interfaces.External.DigitalSignature;
using sh.be.applications.Base.Interfaces.Api;
using sh.be.applications.Base.Interfaces.External.BackgroundJob;
using sh.be.applications.Base.Interfaces.External.Mail;
using sh.be.applications.Base.Interfaces.External.SignalR;
using sh.be.applications.Base.Interfaces.External.Storage;
using sh.be.applications.Module.Auth.Implements;
using sh.be.applications.Module.Auth.Interfaces.Services;
using sh.be.applications.Module.Auth.Interfaces.Queries;
using sh.be.applications.Module.Core.Implements;
using sh.be.applications.Module.Core.Interfaces.Services;
using sh.be.applications.Module.Core.Interfaces.Queries;
using sh.be.applications.Module.Construction.Implements;
using sh.be.applications.Module.Construction.Interfaces.Queries;
using sh.be.applications.Module.Construction.Interfaces.Services;
using sh.be.applications.Module.Construction.Jobs;
using sh.be.applications.Module.DigitalSignature.Implements;
using sh.be.applications.Module.DigitalSignature.Interfaces.Queries;
using sh.be.applications.Module.DigitalSignature.Interfaces.Services;
using sh.be.applications.Module.Packaging.Implements;
using sh.be.applications.Module.Packaging.Interfaces.Queries;
using sh.be.applications.Module.Packaging.Interfaces.Services;
using sh.be.applications.Module.Operation.Implements;
using sh.be.applications.Module.Operation.Interfaces.Queries;
using sh.be.applications.Module.Operation.Interfaces.Services;
using sh.be.domain.Base;
using sh.be.domain.Module.Auth.Entities;
using sh.be.domain.Module.Auth.Repositories;
using sh.be.domain.Module.Auth.Services;
using sh.be.domain.Module.Core.Repositories;
using sh.be.domain.Module.Construction.DocumentFields.Repositories;
using sh.be.domain.Module.Construction.DocumentFields.Services;
using sh.be.domain.Module.Construction.DocumentTypes.Repositories;
using sh.be.domain.Module.Construction.DocumentTypes.Services;
using sh.be.domain.Module.Construction.Projects.Repositories;
using sh.be.domain.Module.Construction.Projects.Services;
using sh.be.domain.Module.Construction.QaAssignments.Repositories;
using sh.be.domain.Module.Construction.QaAssignments.Services;
using sh.be.domain.Module.Construction.Submissions.Repositories;
using sh.be.domain.Module.Construction.Submissions.Services;
using sh.be.domain.Module.DigitalSignature.Repositories;
using sh.be.domain.Module.Operation.StaffGroups.Repositories;
using sh.be.domain.Module.Operation.StaffGroups.Services;
using sh.be.infrastructure.data.Base;
using sh.be.infrastructure.data.Module.Auth.Queries;
using sh.be.infrastructure.data.Module.Auth.Repositories;
using sh.be.infrastructure.data.Module.Core.Queries;
using sh.be.infrastructure.data.Module.Core.Repositories;
using sh.be.infrastructure.data.Module.Construction.Queries;
using sh.be.infrastructure.data.Module.Construction.Repositories;
using sh.be.infrastructure.data.Module.DigitalSignature.Queries;
using sh.be.infrastructure.data.Module.DigitalSignature.Repositories;
using sh.be.infrastructure.data.Module.Packaging.Queries;
using sh.be.infrastructure.data.Module.Operation.Queries;
using sh.be.infrastructure.data.Module.Operation.Repositories;
using sh.be.infrastructure.data.Persistence.Db;
using sh.be.infrastructure.data.Persistence.Seeder;
using sh.be.infrastructure.external.BackgroundJob.Base;
using sh.be.infrastructure.external.BackgroundJob.Implements;
using sh.be.infrastructure.external.DataReader.Implements;
using sh.be.infrastructure.external.DigitalSignature.Implements;
using sh.be.infrastructure.external.Mail.Implements;
using sh.be.infrastructure.external.SignalR.Base;
using sh.be.infrastructure.external.SignalR.Implements;
using sh.be.infrastructure.external.Storage.Implements;
using sh.be.shared.Constants.Auth;
using sh.be.shared.Constants.BackgroundJob;
using sh.be.shared.Constants.DigitalSignature;
using sh.be.shared.Constants.SignalR;
using sh.be.shared.Constants.Swagger;
using sh.be.shared.Constants.Versioning;
using sh.be.shared.Settings;
using System.Globalization;
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

#region hangfire
var backgroundJobSettings = builder.Configuration.GetSection(BackgroundJobConst.CONFIG_SECTION).Get<BackgroundJobSettings>() ?? new();
builder.Services.Configure<BackgroundJobSettings>(builder.Configuration.GetSection(BackgroundJobConst.CONFIG_SECTION));
builder.Services.AddHangfireBackgroundJob(connectionString, backgroundJobSettings);
#endregion

#region signalr
builder.Services.AddSignalRNotification();
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
    options.AddPolicy(SignalRConst.CORS_POLICY, builder =>
    {
        builder
            .SetIsOriginAllowed(origin => origins.Contains(CorsConstants.AnyOrigin) || origins.Contains(origin, StringComparer.OrdinalIgnoreCase))
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
builder.Services.Configure<GoogleSheetSettings>(builder.Configuration.GetSection("GoogleSheet"));
builder.Services.Configure<S3Settings>(builder.Configuration.GetSection("S3"));
builder.Services.Configure<PluginSettings>(builder.Configuration.GetSection(PluginConst.CONFIG_SECTION));

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
            options.Events = new JwtBearerEvents
            {
                OnMessageReceived = context =>
                {
                    var accessToken = context.Request.Query[SignalRConst.ACCESS_TOKEN_QUERY];
                    if (!string.IsNullOrEmpty(accessToken) && context.Request.Path.StartsWithSegments(SignalRConst.HUB_PATH))
                    {
                        context.Token = accessToken;
                    }
                    return Task.CompletedTask;
                }
            };
        }
    )
    .AddCookie(CookieAuthenticationDefaults.AuthenticationScheme);



builder.Services.AddAuthorization();


builder.Services.AddHostedService<AuthWorker>();
builder.Services.AddHostedService<RecurringJobRegistrar>();
#endregion

#region services
// Add other services here
// common
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddScoped<IRequestContext, HttpRequestContext>();
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
builder.Services.AddScoped<IAuthorizationStateProtector, AuthorizationStateProtector>();

// repositories
builder.Services.AddScoped<IRoleRepository, RoleRepository>();
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IPermissionRepository, PermissionRepository>();
builder.Services.AddScoped<IAuditLogRepository, AuditLogRepository>();
builder.Services.AddScoped<IDocumentTypeRepository, DocumentTypeRepository>();
builder.Services.AddScoped<IDocumentFieldRepository, DocumentFieldRepository>();
builder.Services.AddScoped<IProjectRepository, ProjectRepository>();
builder.Services.AddScoped<IProjectConfigRepository, ProjectConfigRepository>();
builder.Services.AddScoped<IProjectDeadlineNotificationRepository, ProjectDeadlineNotificationRepository>();
builder.Services.AddScoped<ISubmissionRepository, SubmissionRepository>();
builder.Services.AddScoped<IQaSamplingConfigRepository, QaSamplingConfigRepository>();
builder.Services.AddScoped<IQaReviewerModeRepository, QaReviewerModeRepository>();
builder.Services.AddScoped<IQaTicketRepository, QaTicketRepository>();
builder.Services.AddScoped<ITemplateRepository, TemplateRepository>();
builder.Services.AddScoped<ISigningBatchRepository, SigningBatchRepository>();
builder.Services.AddScoped<IStaffGroupRepository, StaffGroupRepository>();

// queries
builder.Services.AddScoped<IRoleQuery, RoleQuery>();
builder.Services.AddScoped<IUserQuery, UserQuery>();
builder.Services.AddScoped<IAuditLogQuery, AuditLogQuery>();
builder.Services.AddScoped<IDocumentTypeQuery, DocumentTypeQuery>();
builder.Services.AddScoped<IDocumentFieldQuery, DocumentFieldQuery>();
builder.Services.AddScoped<IProjectQuery, ProjectQuery>();
builder.Services.AddScoped<IProjectConfigQuery, ProjectConfigQuery>();
builder.Services.AddScoped<IProjectDeadlineQuery, ProjectDeadlineQuery>();
builder.Services.AddScoped<ISubmissionQuery, SubmissionQuery>();
builder.Services.AddScoped<IQaAssignmentQuery, QaAssignmentQuery>();
builder.Services.AddScoped<ITemplateQuery, TemplateQuery>();
builder.Services.AddScoped<ISigningBatchQuery, SigningBatchQuery>();
builder.Services.AddScoped<IPackageConfigQuery, PackageConfigQuery>();
builder.Services.AddScoped<IStaffGroupQuery, StaffGroupQuery>();

// domain services
builder.Services.AddScoped<RoleDomainService>();
builder.Services.AddScoped<UserDomainService>();
builder.Services.AddScoped<DocumentTypeDomainService>();
builder.Services.AddScoped<DocumentFieldDomainService>();
builder.Services.AddScoped<ProjectDomainService>();
builder.Services.AddScoped<ProjectConfigDomainService>();
builder.Services.AddScoped<SubmissionDomainService>();
builder.Services.AddScoped<QaAssignmentDomainService>();
builder.Services.AddScoped<StaffGroupDomainService>();
builder.Services.AddSingleton<IQaStrategyValidator, FormatOnlyQaStrategyValidator>();

// application services
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IRoleService, RoleService>();
builder.Services.AddScoped<IPermissionService, PermissionService>();
builder.Services.AddScoped<IAuditLogService, AuditLogService>();
builder.Services.AddScoped<IBackgroundJobService, BackgroundJobService>();
builder.Services.AddScoped<ISignalRService, SignalRService>();
builder.Services.AddScoped<IDocumentTypeService, DocumentTypeService>();
builder.Services.AddScoped<IDocumentFieldService, DocumentFieldService>();
builder.Services.AddScoped<IProjectService, ProjectService>();
builder.Services.AddScoped<IProjectConfigService, ProjectConfigService>();
builder.Services.AddScoped<IProjectDeadlineService, ProjectDeadlineService>();
builder.Services.AddScoped<ISubmissionService, SubmissionService>();
builder.Services.AddScoped<ISubmissionFileService, SubmissionFileService>();
builder.Services.AddScoped<IQaAssignmentService, QaAssignmentService>();
builder.Services.AddScoped<ITemplateService, TemplateService>();
builder.Services.AddScoped<ISigningBatchService, SigningBatchService>();
builder.Services.AddScoped<IPluginService, PluginService>();
builder.Services.AddScoped<IPackageConfigService, PackageConfigService>();
builder.Services.AddScoped<IStaffGroupService, StaffGroupService>();
builder.Services.AddSingleton<ISigningRunner, SigningRunner>();

// jobs
builder.Services.AddScoped<ProjectDeadlineJob>();
builder.Services.AddScoped<SubmissionExportJob>();
builder.Services.AddScoped<SubmissionExportCleanupJob>();

// third-party services
builder.Services.AddScoped<IMailExternal, MailExternal>();
builder.Services.AddScoped<IDataReaderExternal, GoogleSheetReaderExternal>();
builder.Services.AddScoped<IExcelExternal, ExcelExternal>();
builder.Services.AddScoped<IGoogleSheetExternal, GoogleSheetExternal>();
builder.Services.AddScoped<IFileDownloadExternal, HttpFileDownloadExternal>();
builder.Services.AddScoped<IFileStorageExternal, S3FileStorageExternal>();
builder.Services.AddScoped<ISampleFileExternal, SampleFileExternal>();
builder.Services.AddScoped<IPluginSetupExternal, PluginSetupExternal>();
builder.Services.AddSingleton<ICertificateValidatorExternal, CertificateValidatorExternal>();
builder.Services.AddSingleton<ISigningQueueExternal, SigningQueueExternal>();
builder.Services.AddSingleton<IPdfSignerExternal, PdfSignerExternal>();
builder.Services.AddSingleton<ITimestampExternal, TimestampExternal>();
builder.Services.AddScoped<IBackgroundJobExternal, HangfireBackgroundJobExternal>();
builder.Services.AddSingleton<ISignalRExternal, SignalRExternal>();
#endregion


builder.Services.AddHttpClient();
builder.Services.AddHttpClient(TimestampConst.HTTP_CLIENT_NAME, client => client.Timeout = TimeSpan.FromSeconds(TimestampConst.TIMEOUT_SECONDS));
builder.Services.AddHttpContextAccessor();
builder.Services.AddControllers(o => o.Conventions.Add(new ModuleGroupConvention()));
builder.Services.AddHealthChecks();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();

#region api version
var apiVersionSettings = builder.Configuration.GetSection(ApiVersionConstants.CONFIG_SECTION).Get<ApiVersionSettings>() ?? new();
builder.Services.AddApiVersioning(options =>
{
    options.DefaultApiVersion = ApiVersionParser.Default.Parse(ApiVersionConstants.DEFAULT_VERSION);
    options.ReportApiVersions = true;
    options.ApiVersionReader = new UrlSegmentApiVersionReader();
    foreach (var sunset in apiVersionSettings.Sunsets)
    {
        var sunsetPolicy = options.Policies.Sunset(ApiVersionParser.Default.Parse(sunset.Version)).Effective(sunset.Date);
        if (!string.IsNullOrWhiteSpace(sunset.Link))
        {
            sunsetPolicy.Link(sunset.Link);
        }
    }
})
.AddMvc()
.AddApiExplorer(options =>
{
    options.GroupNameFormat = ApiVersionConstants.GROUP_NAME_FORMAT;
    options.FormatGroupName = (module, apiVersionGroupName) => module;
    options.SubstituteApiVersionInUrl = true;
});
builder.Services.ConfigureOptions<ConfigureSwaggerOptions>();
#endregion

builder.Services.AddSwaggerGen(c =>
{
    //c.SwaggerDoc("v1", new() { Title = "My API", Version = "v1" });

    c.DocInclusionPredicate((docName, apiDesc) =>
    {
        if (apiDesc.GetApiVersion() is not ApiVersion apiVersion)
        {
            return false;
        }
        var apiVersionGroupName = SwaggerDocumentName.BuildApiVersionGroupName(apiVersion);
        return docName == SwaggerDocumentName.Build(apiVersionGroupName, SwaggerConstants.ALL)
            || docName == SwaggerDocumentName.Build(apiVersionGroupName, apiDesc.GroupName);
    });
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
    await SeedPackaging.SeedAsync(shDbContext);
}
await app.Services.GetRequiredService<ISigningRunner>().RecoverAsync();
#endregion


// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    var apiVersionGroupNames = app.Services.GetRequiredService<IApiVersionDescriptionProvider>().ApiVersionDescriptions
        .Select(apiVersionDescription => apiVersionDescription.ApiVersion)
        .Distinct()
        .OrderByDescending(apiVersion => apiVersion)
        .Select(SwaggerDocumentName.BuildApiVersionGroupName)
        .ToList();
    var defaultDocumentName = SwaggerDocumentName.Build(apiVersionGroupNames.First(), SwaggerConstants.ALL);

    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        foreach (var apiVersionGroupName in apiVersionGroupNames)
        {
            foreach (var module in SwaggerConstants.DOCUMENTS)
            {
                var documentName = SwaggerDocumentName.Build(apiVersionGroupName, module);
                c.SwaggerEndpoint($"/swagger/{documentName}/swagger.json", documentName.ToUpper());
            }
        }
    });

    app.MapScalarApiReference(options =>
    {
        foreach (var apiVersionGroupName in apiVersionGroupNames)
        {
            foreach (var module in SwaggerConstants.DOCUMENTS)
            {
                var documentName = SwaggerDocumentName.Build(apiVersionGroupName, module);
                options.AddDocument(documentName, documentName.ToUpper(),
                    $"/swagger/{documentName}/swagger.json", isDefault: documentName == defaultDocumentName);
            }
        }
        options.AddPreferredSecuritySchemes("Bearer");

    });
}

app.UseRequestLocalization(new RequestLocalizationOptions()
    .SetDefaultCulture(CultureInfo.InvariantCulture.Name)
    .AddSupportedCultures(CultureInfo.InvariantCulture.Name)
    .AddSupportedUICultures(CultureInfo.InvariantCulture.Name));

app.UseCors(ProgramExtensions.CorsPolicy);
app.UseStaticFiles();

app.UseAuthentication();
app.UseAuthorization();

app.UseHangfireBackgroundJobDashboard(!app.Environment.IsDevelopment());

app.MapControllers();
app.MapSignalRNotificationHub(JwtBearerDefaults.AuthenticationScheme);


app.MapHealthChecks("/health");
app.Run();


