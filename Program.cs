using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using web_groupware.Data;
using NLog;
using NLog.Web;
using System;
using Microsoft.AspNetCore.DataProtection;
#pragma warning disable CS8600,CS8601,CS8602,CS8604,CS8618,CS8629
var logger = NLog.LogManager.Setup().LoadConfigurationFromAppSettings().GetCurrentClassLogger();
logger.Debug("init main");

try
{
    var builder = WebApplication.CreateBuilder(args);
    builder.Services.AddDbContext<web_groupwareContext>(options =>
        options.UseSqlServer(builder.Configuration.GetConnectionString("web_groupwareContext") ?? throw new InvalidOperationException("Connection string 'web_groupwareContext' not found.")));

    // Add services to the container.
    builder.Services.AddControllersWithViews();

    builder.Logging.ClearProviders();
    builder.Host.UseNLog();
    
    //string subdirectory = builder.Configuration.GetValue<string>("Subdirectory");

    builder.Services.AddRazorPages().AddRazorRuntimeCompilation();

    DirectoryInfo di = new DirectoryInfo(Environment.CurrentDirectory);
    string di_parent = di.Parent.ToString();
    builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
                .AddCookie(options =>
                {
                    double ExpireTimeSpan = Double.Parse(builder.Configuration.GetSection("CommonInfo").GetValue<string>("ExpireTimeSpan"));
                    options.ExpireTimeSpan = TimeSpan.FromDays(ExpireTimeSpan);
                    options.LoginPath = "/Login/Index";
                    options.Cookie.Name = "Astcookies";
                    options.Cookie.Path = "/";
                    options.DataProtectionProvider = DataProtectionProvider.Create(new DirectoryInfo(builder.Configuration.GetSection("CommonInfo").GetValue<string>("Path_DPKeys")));
                });
    builder.Services.AddSingleton<Microsoft.AspNetCore.Http.IHttpContextAccessor, Microsoft.AspNetCore.Http.HttpContextAccessor>();

    var app = builder.Build();

    // Configure the HTTP request pipeline.
    if (!app.Environment.IsDevelopment())
    {
        app.UseExceptionHandler("/Login/Logout");
        // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
        app.UseHsts();
    }

    app.UseHttpsRedirection();
    app.UseStaticFiles();

    app.UseRouting();
    app.UseAuthentication();
    app.UseAuthorization();

    app.MapControllerRoute(
        name: "default",
        pattern: "{controller=Login}/{action=Index}/{id?}");

    app.Run();
}
catch (Exception exception)
{
    logger.Error(exception, "Stopped program because of exception");
    throw;
}
finally
{
    NLog.LogManager.Shutdown();
}

