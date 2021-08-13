using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace JeeWork_Core2021.Models
{
    public class SendNotifyModel
    {
        public long id_row { get; set; }
        public string name { get; set; }
        public string title { get; set; }
        public string template { get; set; }
        public string keys { get; set; }
        public string link { get; set; } //Link dành cho web app
        public string lang { get; set; }
        public string link_mobileapp { get; set; } // deep link dành cho mobile
        public bool exclude_sender { get; set; } = true;
    }
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
        public string description { get; set; }
        public long templateid { get; set; } //
        public long customerid { get; set; } //
        public long types { get; set; } // 1 - space, 2 - folder, 3 - list (Project)
        public long ObjectTypesID { get; set; } // Truyền id department hoặc id list
        public long ParentID { get; set; } //2 - folder (Truyền ID Department chọn), 3 - list (Project) Truyền id_Folder chọn
        public long levels { get; set; } // 1 - Beginner, 2 - Intermediate, 3 - Advanced
        public string viewid { get; set; } // 
        public string group_statusid { get; set; } //
        public List<StatusListModel> list_status { get; set; } // Dùng khi save as
        public long template_typeid { get; set; } //
        public string img_temp { get; set; } //
        public string field_id { get; set; } // truyền is new field, anh tự lấy trong DB
        public bool is_customitems { get; set; }
        public bool is_projectdates { get; set; }
        public bool is_task { get; set; }
        public bool is_views { get; set; }
        public long share_with { get; set; } // 1 - Only Me, 2 - Everyone (including guests), 3 - All Members, 4 - Select people
        public List<TempalteUserModel> list_share { get; set; } // Nếu share_with = 4 thì nhập thêm cột list_share (Danh sách các member)
        public List<ListFieldModel> list_field_name { get; set; }
        public DateTime start_date { get; set; }
        public DateTime end_date { get; set; }
        public string sample_id { get; set; } // Link với table we_sample_data để lấy bộ dữ liệu mẫu tương ứng với template (áp dụng is_template_center = 1)
        public string save_as_id { get; set; } // Dùng để lưu ngược lại từ space/folder/list về lại template, id link với các table phụ thuộc vào types
        public long id_reference { get; set; } // levels = 1,2 lấy id_row trong we_department, level = 3 we_project_team, level = 4 we_work

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
    public class TempalteUserModel
    {
        public long id_row { get; set; }
        public long id_template { get; set; }
        public long id_user { get; set; }
    }
    public class add_template_library_Model
    {
        public long templateid { get; set; } = 0; // link từ we_template_customer
        public List<TempalteUserModel> list_share { get; set; } // Nếu share_with = 4 thì nhập thêm cột list_share (Danh sách các member)
       
    }
}