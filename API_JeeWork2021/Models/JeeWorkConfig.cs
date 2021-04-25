using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace JeeWork_Core2021.Models
{
    public class JeeWorkConfig
    {
        public string SysName { get; set; }
        public string LinkAPI { get; set; }
        public string ConnectionString { get; set; }
        public string HRCatalog { get; set; }
        public string API_Account { get; set; }
        public string Error_MailTo { get; set; }
        public string Error_MailCC { get; set; }
        public string HRConnectionString
        {
            get
            {
                return ConnectionString.Replace("jeework", HRCatalog);
            }
        }
        public string LinkBackend { get; set; }
        public string SecretKey { get; set; }
    }

    public class TimerAutoRunConfig
    {
        public string AutoCreateDNXKFromKHSX { get; set; } = "";
        public string AutoNotifyWarningRunKHGH { get; set; } = "";
        public string AutoCreateDNXKFromLXHB { get; set; } = "";
    }
}
