using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace JeeWork_Core2021.Models
{
    public class DepartmentModel
    {
        public long id_row { get; set; } = 0;
        public string title { get; set; }
        public long id_cocau { get; set; } = 0;
        public bool IsDataStaff_HR { get; set; }
        public long TemplateID { get; set; }
        public List<DepartmentOwnerModel> Owners { get; set; }
        public bool ReUpdated { get; set; } // áp dụng cho trường hợp sửa. Khi muốn cập nhật lại dữ liệu thành viên
        public List<DepartmentViewModel> DefaultView { get; set; }
        public bool IsFolder { get; set; } = false;
        public long ParentID { get; set; }
    }
    public class DepartmentOwnerModel
    {
        public long id_row { get; set; } = 0;
        public long id_department { get; set; }
        public long id_user { get; set; }
        public long type { get; set; }
    }
    public class DepartmentViewModel
    {
        public long id_row { get; set; } = 0;
        public long id_department { get; set; }
        public long viewid { get; set; }
        public bool is_default { get; set; }
    }
}
