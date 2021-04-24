using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace JeeWork_Core2021.Helpers
{
    public static class ConfigurationManager_JeeWork
    {
        public static IConfiguration ConfigAppSetting { get; }
        public static AppSettings AppSettings { get; set; }
        public static ConnectionStrings ConnDps { get; set; }
        static ConfigurationManager_JeeWork()
        {
            ConfigAppSetting = new ConfigurationBuilder()
                    .SetBasePath(Directory.GetCurrentDirectory())
                    .AddJsonFile("appsettings.json", optional: false)
                    .Build();

            AppSettings = ConfigAppSetting.GetSection("AppSettings").Get<AppSettings>();
            ConnDps = ConfigAppSetting.GetSection("ConnectionStrings").Get<ConnectionStrings>();
        }

    }


    public class HostingEnviroment_JeeWork
    {
        private IWebHostEnvironment Environment;

        public static string Server_MapPath { get; set; }
        public static string WWW_Server_MapPath { get; set; }

        public HostingEnviroment_JeeWork(IWebHostEnvironment env)
        {
            Environment = env;
            Server_MapPath = Environment.ContentRootPath;
            WWW_Server_MapPath = Environment.WebRootPath;
        }
    }

}
