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
    public class TemplateCenterModel
    {
        public long id_row { get; set; } //
        public string title { get; set; } //
        public long templateid { get; set; } //
        public long customerid { get; set; } //
        public long types { get; set; } // 1 - space, 2 - folder, 3 - list (Project)
        public long ObjectTypesID { get; set; } // Truyền id department hoặc id list
        public long ParentID { get; set; } //2 - folder (Truyền ID Department chọn), 3 - list (Project) Truyền id_Folder chọn
        public long levels { get; set; } // 1 - Beginner, 2 - Intermediate, 3 - Advanced
        public long viewid { get; set; } // 
        public long group_statusid { get; set; } //
        public long template_typeid { get; set; } //
        public long img_temp { get; set; } //
        public long field_id { get; set; } //
        public bool is_customitems { get; set; } = false;
        public bool is_projectdates { get; set; } = false;
        public bool is_task { get; set; } = false;
        public bool is_views { get; set; } = false;
        public List<ListFieldModel> customitems { get; set; }
        public List<ProjectDatesModel> projectdates { get; set; }
    }
    public class ListFieldModel
    {
        public long id_field { get; set; }
        public string fieldname { get; set; }
        public string title { get; set; }
        public bool isvisible { get; set; }
        public string note { get; set; }
        public string type { get; set; }
        public long position { get; set; }
        public bool isnewfield { get; set; }
        public bool isdefault { get; set; }
        public long typeid { get; set; }
    }
    public class ProjectDatesModel
    {
        public DateTime start_date { get; set; }
        public DateTime end_date { get; set; }
    }
}