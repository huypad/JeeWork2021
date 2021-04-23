using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace JeeWork_Core2021.Models
{
    public class NotifyModel
    {
        public string From_IDNV { get; set; }
        public string To_IDNV { get; set; }
        public string AppCode { get; set; } = "WW";
        public string TitleLanguageKey { get; set; }
        public Hashtable ReplaceData { get; set; }
        public string To_Link_WebApp { get; set; }
        public string To_Link_MobileApp { get; set; }
        public string ComponentName { get; set; }
        public string Component { get; set; }
    }
}