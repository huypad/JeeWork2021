using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using JeeWork_Core2021.Models;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi.Models;
using System.IO;
using Microsoft.Extensions.FileProviders;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Newtonsoft.Json.Serialization;
using JeeWork_Core2021.Classes;
using VaultSharp;
using DPSinfra.Vault;
using VaultSharp.V1.Commons;
using DPSinfra.Kafka;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using DPSinfra.Logger;
using DPSinfra.Notifier;

namespace JeeWork_Core2021
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            // add Vault and get Vault for secret in another services
            var vaultClient = ConfigVault(services);
            Secret<SecretData> kafkaSecret = vaultClient.V1.Secrets.KeyValue.V2.ReadSecretAsync(path: "jwt", mountPoint: "kv").Result;
            IDictionary<string, object> kafkaDataSecret = kafkaSecret.Data.Data;
            string access_secret = kafkaDataSecret["access_secret"].ToString();
            Configuration["Jwt:access_secret"] = access_secret; 
            ///////// kafka
            Secret<SecretData> kafka = vaultClient.V1.Secrets.KeyValue.V2.ReadSecretAsync(path: "kafka", mountPoint: "kv").Result;
            IDictionary<string, object> kafkaData = kafka.Data.Data;
            string KafkaUser = kafkaData["username"].ToString();
            string KafkaPassword = kafkaData["password"].ToString();

            Configuration["KafkaConfig:username"] = KafkaUser;
            Configuration["KafkaConfig:password"] = KafkaPassword;
            // add Kafka
            services.addKafkaService();
            services.AddHttpClient();
            services.addNotificationService();

            services.AddCors(o => o.AddPolicy("JeeWorkPolicy", builder =>
            {
                builder.AllowAnyOrigin()
                       .AllowAnyMethod()
                       .AllowAnyHeader();
            }));
            services.AddControllers().AddNewtonsoftJson();
            services.AddControllersWithViews().AddNewtonsoftJson();
            services.AddRazorPages().AddNewtonsoftJson();
            services.AddControllers().AddNewtonsoftJson(options => { options.SerializerSettings.ContractResolver = new DefaultContractResolver(); });
            services.AddAuthentication(x =>
            {
                x.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                x.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            }).AddJwtBearer(options =>
            {
                //options.RequireHttpsMetadata = false;
                //options.SaveToken = false;
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = false,
                    ValidateAudience = false,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    //IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(access_secret.ToString())),
                };
            });
            //Swagger
            services.AddSwaggerGen(c =>
            {
                var securityScheme = new OpenApiSecurityScheme
                {
                    Name = "Authorization",
                    Description = "Please enter into field the word 'Bearer' following by space and JWT",
                    In = ParameterLocation.Header,
                    Type = SecuritySchemeType.ApiKey,
                    //Scheme = "bearer", // must be lower case
                    Reference = new OpenApiReference
                    {
                        Id = "Bearer",
                        Type = ReferenceType.SecurityScheme
                    }
                };
                c.AddSecurityDefinition(securityScheme.Reference.Id, securityScheme);
                c.AddSecurityRequirement(new OpenApiSecurityRequirement
                                        {
                                            {securityScheme, new string[] { }}
                                        });
            });
            services.AddMvc().ConfigureApiBehaviorOptions(options =>
            {
                options.SuppressModelStateInvalidFilter = true;
            });
            services.AddOptions();
            //Config appsetting
            services.Configure<JeeWorkConfig>(options => Configuration.GetSection("JeeWorkConfig").Bind(options));
            //kafka
            services.AddLogging(builder => {
                builder.addAsyncLogger<AsyncLoggerProvider>(p => new AsyncLoggerProvider(p.GetService<IProducer>()));
            });
        }
        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                app.UseSwagger();
                app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "JeeWork v1"));
            }
            //app.UseHttpsRedirection();
            app.UseRouting();
            app.UseCors("JeeWorkPolicy");
            app.UseAuthentication();
            app.UseAuthorization();
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });


            app.UseExceptionHandler("/error");
            app.UseStaticFiles();
            app.UseStaticFiles(new StaticFileOptions
            {
                FileProvider = new PhysicalFileProvider(
                   Path.Combine(Directory.GetCurrentDirectory(), "dulieu")),
                RequestPath = "/dulieu"
            });

            app.UseHttpsRedirection();
        }
        private VaultClient ConfigVault(IServiceCollection services)
        {
            var serviceConfig = Configuration.GetVaultConfig();
            return services.addVaultService(serviceConfig);
        }
    }
}
