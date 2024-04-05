namespace Sage.Web;

using Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.SqlClient;
using Spryer.AspNetCore.Identity;
using Spryer.AspNetCore.Identity.SqlServer;

static class Program
{
    static Program()
    {
        DapperSupport.Initialize();
    }

    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
        builder.Services.AddSingleton(sp => SqlClientFactory.Instance.CreateDataSource(connectionString));

        builder.Services
            .AddIdentity<IdentityUser, IdentityRole>(options => options.SignIn.RequireConfirmedAccount = true)
            .AddDapperStores(options =>
            {
                options.UseSqlServer();
            })
            .AddDefaultUI()
            .AddDefaultTokenProviders();

        builder.Services.AddRazorPages();
        var app = builder.Build();

        // Configure the HTTP request pipeline.
        if (!app.Environment.IsDevelopment())
        {
            app.UseExceptionHandler("/Error");
            // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
            app.UseHsts();
        }

        app.UseHttpsRedirection();
        app.UseStaticFiles();

        app.UseRouting();

        app.UseAuthorization();

        app.MapRazorPages();

        app.Run();
    }
}
