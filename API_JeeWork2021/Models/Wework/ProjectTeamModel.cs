using JeeWork_Core2021.Classes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace JeeWork_Core2021.Models
{
    public class ProjectTeamModel
    {
        public long id_row { get; set; } = 0;
        public FileUploadModel icon { get; set; }
        public string title { get; set; }
        public string description { get; set; }
        public string detail { get; set; }
        public long id_department { get; set; }
        /// <summary>
        /// 1:dự án nội bộ, 2: dự án làm việc vs khách hàng
        /// </summary>
        public int loai { get; set; }
        public bool is_project { get; set; }
        public DateTime start_date { get; set; }
        public DateTime end_date { get; set; }
        public int status { get; set; } = 1;
        public bool locked { get; set; }
        public string stage_description { get; set; }
        public string color { get; set; }
        public long template { get; set; }
        public bool allow_percent_done { get; set; }
        public bool require_evaluate { get; set; }
        public bool evaluate_by_assignner { get; set; }
        public bool allow_estimate_time { get; set; }
        public List<ProjectTeamUserModel> Users { get; set; }
        public long id_template { get; set; } = 0;
        public TemplateCenterModel templatecenter { get; set; }

    }
    public class ProjectTeamUserModel
    {
        public long id_row { get; set; } = 0;
        public long id_project_team { get; set; }
        public long id_user { get; set; }
        public bool admin { get; set; }
    }
    public class ProjectTeamCloseModel
    {
        public long id_row { get; set; }
        public int close_status { get; set; }
        public string note { get; set; }
        public bool stop_reminder { get; set; }
    }
    public class AddUserToProjectTeamModel
    {
        public long id_row { get; set; }
        public List<ProjectTeamUserModel> Users { get; set; }
    }

    public class ProjectTeamDuplicateModel
    {
        public long id_row { get; set; }
        public long id { get; set; }
        public string title { get; set; }
        /// <summary>
        /// Nhân bản công việc<param/>
        /// 0: không nhân bản cv, nhóm, 1: nhân bản nhom, 2: nhân bản nhóm & cv
        /// </summary>
        public int type { get; set; }
        /// <summary>
        /// Giữ lại thông tin người giao
        /// </summary>
        public bool keep_creater { get; set; }
        /// <summary>
        /// Giữ lại thông tin người nhận
        /// </summary>
        public bool keep_checker { get; set; }
        /// <summary>
        /// Giữ lại thông tin người theo dõi
        /// </summary>
        public bool keep_follower { get; set; }
        /// <summary>
        /// Giữ lại thông tin hạn hoàn thành: 0: không, 1: có, 2:giữ lại và thay đổi (Điền số giờ điều chỉnh - hạn hoàn thành mới bằng hạn hoàn thành cũ cộng thêm giờ điều chỉnh)
        /// </summary>
        public int keep_deadline { get; set; }
        public int hour_adjusted { get; set; }
        public bool keep_checklist { get; set; }
        public bool keep_child { get; set; }
        public bool keep_milestone { get; set; }
        public bool keep_tag { get; set; }
        public bool keep_admin { get; set; }
        public bool keep_member { get; set; }
        public bool keep_role { get; set; }
    }
    public class ProjectViewsModel
    {
        public long id_row { get; set; }
        public long viewid { get; set; } // View chọn
        public long id_project_team { get; set; }
        public string view_name_new { get; set; } // tên view mới
        public string link { get; set; }
        public bool? default_everyone { get; set; }
        public bool? default_for_me { get; set; }
        public bool? pin_view { get; set; }
        public bool? personal_view { get; set; }
        public bool? favourite { get; set; }
        public long id_department { get; set; }
    }
    public class GenerateProjectAutoModel
    {
        public long listid { get; set; } = 0;
        public long id_department { get; set; } = 0;
        public string title { get; set; } // Tiêu đề của đối tượng
        // 1:dự án nội bộ, 2: dự án làm việc vs khách hàng, 3 - dự án tạo từ cuộc họp
        public long loai { get; set; } = 3;
        public long meetingid { get; set; }
    }
}
