using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace JeeWork_Core2021.Helpers
{
    public class AppSettings
    {
        public string Secret { get; set; }
        public string EXTENSION_UPLOAD_FILE { get; set; }
        public string MAX_SIZE_ANH { get; set; }
    }

    public class ConnectionStrings
    {
        public string ConnectSource { get; set; }
    }

}
