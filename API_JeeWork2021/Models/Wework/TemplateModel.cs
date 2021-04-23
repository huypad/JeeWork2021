using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace JeeWork_Core2021.Models
{
    public class TemplateModel
    {
        public long id_row { get; set; }
        public string title { get; set; }
        public string description { get; set; }
        public string color { get; set; }
        public bool isdefault { get; set; } = true;
        public long customerid { get; set; }
        public List<TemplateStatusModel> Status { get; set; }
    }
    public class UpdateQuickModel
    {
        public long id_row { get; set; }
        public string columname { get; set; } // color, title, statusname
        public string values { get; set; }
        public bool istemplate { get; set; } = true;
        public long customerid { get; set; }
        public long id_template { get; set; } // áp dụng khi thêm status mới cho template

    }
}