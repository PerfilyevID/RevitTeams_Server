using AspNetCore.Yandex.ObjectStorage;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using MongoDB.Driver;
using PDS_Server.Elements;
using PDS_Server.Elements.Communications;
using PDS_Server.Elements.Plugins;
using PDS_Server.Elements.Revit;
using PDS_Server.Repositories;
using PDS_Server.Services;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;

namespace PDS_Server
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddSession(options => {
                options.IdleTimeout = TimeSpan.FromMinutes(60);
                options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest; 
                options.Cookie.SameSite = SameSiteMode.Strict; 
                options.Cookie.HttpOnly = false;
                options.Cookie.IsEssential = true;
            });

            services.AddYandexObjectStorage(Configuration);

            services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddJwtBearer(options =>
                {
                    options.SaveToken = true;
                    options.RequireHttpsMetadata = true;
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuer = true,
                        ValidIssuer = AuthOptions.ISSUER,
                        ValidateAudience = true,
                        ValidAudience = AuthOptions.AUDIENCE,
                        ValidateLifetime = true,
                        IssuerSigningKey = AuthOptions.GetSymmetricSecurityKey(),
                        ValidateIssuerSigningKey = true
                    };
                });


            var clientSettings = MongoClientSettings.FromUrl(new MongoUrl(Configuration.GetConnectionString("YandexDb")));
            clientSettings.SslSettings = new SslSettings();
            clientSettings.UseTls = true;
            clientSettings.SslSettings.ClientCertificates = new List<X509Certificate>() { new X509Certificate("CA.pem") };
            clientSettings.SslSettings.EnabledSslProtocols = SslProtocols.Tls13 | SslProtocols.Tls12 | SslProtocols.Tls11 | SslProtocols.Tls;
            clientSettings.SslSettings.ClientCertificateSelectionCallback = (sender, host, certificates, certificate, issuers) => clientSettings.SslSettings.ClientCertificates.First();
            clientSettings.SslSettings.ServerCertificateValidationCallback = (sender, certificate, chain, errors) => true;

            services.AddSingleton<IMongoClient, MongoClient>(sp => new MongoClient(clientSettings));

            //services.AddSingleton<IMongoClient, MongoClient>(sp => new MongoClient(Configuration.GetConnectionString("YandexCluster")));
            services.AddTransient<IMongoRepository<DbAccount>, MongoRepository<DbAccount>>();
            services.AddTransient<IMongoRepository<DbPlugin>, MongoRepository<DbPlugin>>();
            services.AddTransient<IMongoRepository<DbApplication>, MongoRepository<DbApplication>>();
            services.AddTransient<IMongoRepository<DbTeam>, MongoRepository<DbTeam>>();
            services.AddTransient<IMongoRepository<DbReport>, MongoRepository<DbReport>>();
            services.AddTransient<IMongoRepository<DbException>, MongoRepository<DbException>>();

            services.AddTransient<IEmailSender, YandexSender>();

            services.AddTransient<IMongoRepository<DbDepartment>, MongoRepository<DbDepartment>>();
            services.AddTransient<IMongoRepository<DbChat>, MongoRepository<DbChat>>();
            services.AddTransient<IMongoRepository<DbClashResult>, MongoRepository<DbClashResult>>();
            services.AddTransient<IMongoRepository<DbProject>, MongoRepository<DbProject>>();
            services.AddTransient<IMongoRepository<DbDocument>, MongoRepository<DbDocument>>();
            services.AddTransient<IMongoRepository<DbTask>, MongoRepository<DbTask>>();
            services.AddTransient<IMongoRepository<DbCheckResult>, MongoRepository<DbCheckResult>>();
            services.AddTransient<IMongoRepository<DbGroupOfClashes>, MongoRepository<DbGroupOfClashes>>();

            services.AddTransient<IMongoRepository<DbFamily>, MongoRepository<DbFamily>>();
            services.AddTransient<IMongoRepository<DbFamilyCategory>, MongoRepository<DbFamilyCategory>>();

            services.AddTransient<IMongoRepository<DbNews>, MongoRepository<DbNews>>();

            services.AddTransient<IBot, TelegramBot>();

            services.AddControllersWithViews();

            services.AddHsts(options =>
            {
                options.Preload = true;
                options.IncludeSubDomains = true;
                options.MaxAge = TimeSpan.FromDays(60);
            });
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            app.UseDeveloperExceptionPage();

            app.Use(async (context, next) =>
            {
                if (context.Request.Path.StartsWithSegments("/robots.txt"))
                {
                    var robotsTxtPath = Path.Combine(env.ContentRootPath, "robots.txt");
                    string output = "User-agent: *  \nAllow: /";
                    if (File.Exists(robotsTxtPath))
                    {
                        output = await File.ReadAllTextAsync(robotsTxtPath);
                    }
                    context.Response.ContentType = "text/plain";
                    await context.Response.WriteAsync(output);
                }
                else await next();
            });

            app.UseHsts();

            app.UseStaticFiles();

            app.UseRouting();

            app.UseSession();

            app.Use(async (context, next) =>
            {
                var JWToken = context.Session.GetString("JWToken");
                if (!string.IsNullOrEmpty(JWToken))
                {
                    if (!context.Request.Headers.Keys.Contains("Authorization"))
                        context.Request.Headers.Add("Authorization", "Bearer " + JWToken);
                }
                await next();
            });

            app.UseAuthentication();

            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }
    }
}
