using System.Net.Http.Headers;
using System.Text;
using System.Threading.RateLimiting;
using DeviceFX.Proxy.CDA;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.HttpLogging;
using Microsoft.IdentityModel.Tokens;
using Yarp.ReverseProxy.Transforms;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOptions<CiscoOptions>().Bind(builder.Configuration).ValidateDataAnnotations().ValidateOnStart();
var ciscoOptions = builder.Configuration.Get<CiscoOptions>() ?? throw new InvalidOperationException("CiscoOptions configuration is required");

builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"))
    .AddTransforms(transformBuilderContext =>
    {
        transformBuilderContext.AddRequestTransform(async transformContext =>
        {
            var tokenService = transformBuilderContext.Services.GetRequiredService<ICdaTokenService>();
            var token = await tokenService.GetAccessTokenAsync();
            transformContext.ProxyRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        });
    });

builder.Services.AddRateLimiter(options =>
{
    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(partitionKey: httpContext.User.Identity?.Name
                                                               ?? httpContext.Request.Headers.Host.ToString(),
            factory: partition => new FixedWindowRateLimiterOptions
            {
                AutoReplenishment = true,
                PermitLimit = ciscoOptions.PermitLimit,
                QueueLimit = 0,
                Window = TimeSpan.FromSeconds(ciscoOptions.WindowSizeSeconds)
            }
        ));
});

builder.Services.AddControllers();
builder.Services.AddMemoryCache();
builder.Services.AddSingleton<ICdaTokenService, CdaTokenService>();
builder.Services.AddSingleton<AppleAttestService>();
builder.Services.AddSingleton<GoogleAttestService>();
builder.Services.AddSingleton<IJwtTokenService, JwtTokenService>();
builder.Services.AddHttpClient();

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            NameClaimType = "name",
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = ciscoOptions.Issuer,
            ValidAudience = ciscoOptions.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(ciscoOptions.SigningKey))
        };
    });

builder.Services.AddHttpLogging();
builder.Services.Configure<HttpLoggingOptions>(options =>
{
    var httpLoggingSection = builder.Configuration.GetSection(nameof(HttpLoggingOptions));
    httpLoggingSection.Bind(options);
});

var app = builder.Build();
app.UseHttpLogging();
app.UseHttpsRedirection();
app.UseAuthentication();
app.UseRouting();
app.UseAuthorization();
app.UseRateLimiter();
app.MapControllers();
app.MapReverseProxy().RequireAuthorization();
app.Run();