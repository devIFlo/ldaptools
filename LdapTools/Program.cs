using AspNetCoreHero.ToastNotification;
using LdapTools.Data;
using LdapTools.Models;
using LdapTools.Repositories.Implementations;
using LdapTools.Repositories.Interfaces;
using LdapTools.Services;
using LdapTools.Services.Implementations;
using LdapTools.Services.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Serilog.Sinks.PostgreSQL;
using Serilog;
using Microsoft.AspNetCore.DataProtection;
using System.Runtime.InteropServices;

var builder = WebApplication.CreateBuilder(args);

var connectionString = Environment.GetEnvironmentVariable("CSLdapTools");

// Add Serilog
Log.Logger = new LoggerConfiguration()
    .WriteTo.PostgreSQL(
        connectionString: connectionString,
        tableName: "\"Logs\"",
        columnOptions: new Dictionary<string, ColumnWriterBase>
        {
            { "\"Message\"", new RenderedMessageColumnWriter() },
            { "\"Level\"", new LevelColumnWriter() },
            { "\"Timestamp\"", new TimestampColumnWriter() },
            { "\"Exception\"", new ExceptionColumnWriter() },
            { "\"Properties\"", new PropertiesColumnWriter() }
        })
    .Enrich.FromLogContext()
    .CreateLogger();

// Add services to the container.
builder.Services.AddDbContext<ApplicationDbContext>(options =>
        options.UseNpgsql(connectionString));

builder.Services.AddIdentity<ApplicationUser, IdentityRole>()
        .AddEntityFrameworkStores<ApplicationDbContext>()
        .AddDefaultTokenProviders();

var keyPath = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? @"C:\keys" : "/var/keys";
if (!Directory.Exists(keyPath))
{
    Directory.CreateDirectory(keyPath);
}
builder.Services.AddDataProtection().PersistKeysToFileSystem(new DirectoryInfo(keyPath));

builder.Services.AddControllersWithViews();

builder.Services.AddTransient<IEmailSender, EmailSender>();

builder.Services.AddScoped<ILdapService, LdapService>();
builder.Services.AddScoped<ILdapSettingsRepository, LdapSettingsRepository>();
builder.Services.AddScoped<IEmailSettingsRepository, EmailSettingsRepository>();
builder.Services.AddScoped<IPasswordResetTokenRepository, PasswordResetTokenRepository>();
builder.Services.AddScoped<TokenService>();
builder.Services.AddScoped<ISeedUserRoleInitial, SeedUserRoleInitial>();

//Adiciona a dependência do Toastify
builder.Services.AddNotyf(config =>
{
    config.DurationInSeconds = 10;
    config.IsDismissable = true;
    config.Position = NotyfPosition.TopRight;
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

await CreateUserProfilesAsync(app);

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Account}/{action=Login}/{id?}");

app.Run();

async Task CreateUserProfilesAsync(WebApplication app)
{
    var scopedFactory = app.Services.GetService<IServiceScopeFactory>();

    if (scopedFactory != null)
    {
        using (var scope = scopedFactory.CreateScope())
        {
            var userRoleInitial = scope.ServiceProvider.GetService<ISeedUserRoleInitial>();

            if (userRoleInitial != null)
            {
                await userRoleInitial.SeedRolesAsync();
                await userRoleInitial.SeedUsersAsync();
            }
        }
    }
}