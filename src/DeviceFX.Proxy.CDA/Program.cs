using System.Net.Http.Headers;
using DeviceFX.Proxy.CDA;
using Yarp.ReverseProxy.Transforms;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"))
    .AddTransforms(transformBuilderContext =>
    {
        transformBuilderContext.AddRequestTransform(async transformContext =>
        {
            var tokenService = transformBuilderContext.Services.GetRequiredService<ITokenService>();
            var token = await tokenService.GetAccessTokenAsync();
            transformContext.ProxyRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        });
    });

builder.Services.AddMemoryCache();
builder.Services.AddSingleton<ITokenService, TokenService>();
builder.Services.AddOptions<CiscoOptions>().Bind(builder.Configuration).ValidateDataAnnotations().ValidateOnStart();
builder.Services.AddHttpClient();
builder.Services.AddAuthentication(WebexAuthenticationScheme.AuthenticationScheme)
    .AddWebex()
    .AddCookie(options =>
    {
        options.Events.OnRedirectToLogin = ctx =>
        {
            ctx.Response.StatusCode = StatusCodes.Status401Unauthorized;
            return Task.CompletedTask;
        };
        options.Events.OnRedirectToAccessDenied = ctx =>
        {
            ctx.Response.StatusCode = StatusCodes.Status403Forbidden;
            return Task.CompletedTask;
        };
        options.ExpireTimeSpan = TimeSpan.FromMinutes(10); // Session duration
        options.SlidingExpiration = true;
        options.Cookie.Name = builder.Environment.ApplicationName;
        options.Cookie.HttpOnly = true;
        options.Cookie.SecurePolicy = CookieSecurePolicy.Always; // Enforce HTTPS
    });

var app = builder.Build();
app.UseHttpsRedirection();
app.UseAuthentication();
app.UseRouting();
app.UseAuthorization();
app.MapReverseProxy().RequireAuthorization();
app.Run();