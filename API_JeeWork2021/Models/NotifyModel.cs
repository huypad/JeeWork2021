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
        public string AppCode { get; set; } = "WORK";
        public string TitleLanguageKey { get; set; }
        public Hashtable ReplaceData { get; set; }
        public string To_Link_WebApp { get; set; }
        public string To_Link_MobileApp { get; set; }
        public string ComponentName { get; set; } = "";
        public string Component { get; set; } = "";
    }

    public class NotificationMess
    {
        public string Content { get; set; }
        public string Img { get; set; }  //kèm avatar người gửi
        public string Icon { get; set; } //kèm icon thông báo
        public string AppCode { get; set; } //gửi từ app nào
        public string Link { get; set; } //link chuyển nếu có
        public string Domain { get; set; } //link chuyển nếu có
        public string oslink { get; set; } //link chuyển nếu có
        public int Loai { get; set; }

    }
}