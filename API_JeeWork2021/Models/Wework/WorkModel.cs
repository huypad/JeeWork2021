using JeeWork_Core2021.Classes;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Web;

namespace JeeWork_Core2021.Models
{
    public class WorkModel
    {
        public long id_row { get; set; }
        public string title { get; set; }
        public DateTime deadline { get; set; }
        public DateTime start_date { get; set; }
        public DateTime end_date { get; set; }
        public string description { get; set; }
        public long id_project_team { get; set; }
        public long id_milestone { get; set; }
        public long id_group { get; set; }
        public bool prioritize { get; set; } = false;
        public long urgent { get; set; }
        public long id_parent { get; set; }
        public long status { get; set; } // thang viet
        /// <summary>
        /// assign và follower
        /// </summary>
        public List<WorkUserModel> Users { get; set; }
        public List<FileUploadModel> Attachments { get; set; }
        public List<WorkTagModel> Tags { get; set; }
        public long clickup_prioritize { get; set; }


    }
    public class WorkDuplicateModel
    {
        public long id_row { get; set; }
        public int type { get; set; }//1: nhân bản vào dự án phòng ban cũ, 2: vào dự án phòng ban mới
        public long id { get; set; }
        public string title { get; set; }
        public DateTime deadline { get; set; }
        public DateTime start_date { get; set; }
        public string description { get; set; }
        public long id_parent { get; set; }
        public long id_project_team { get; set; }
        public long id_group { get; set; }
        public bool duplicate_child { get; set; }
        public long assign { get; set; }
        public List<long> followers { get; set; }
        public bool urgent { get; set; }
        public bool required_result { get; set; }
    }

    public class UpdateWorkModel
    {
        public long id_row { get; set; }
        public string key { get; set; }
        public object value { get; set; }
        public bool IsStaff { get; set; }
        public List<object> values { get; set; }
        public int id_log_action
        {
            get
            {
                switch (key)
                {
                    //case "status": return value.ToString() == "2" ? 2 : 3;//1: đang làm, 2: hoàn thành, 3: chờ review
                    case "deadline": return 4;
                    case "urgent": return 7;
                    case "important": return 8;
                    case "Tags": return 9;
                    case "Attachments": return 10;
                    case "start_date": return 11;
                    case "id_group": return 12;
                    case "Attachments_result": return 13;
                    case "result": return 14;
                    case "assign": return 15;
                    case "follower": return 56;
                    case "deleteassign": return 55;
                    case "deletefollower": return 57;
                    case "description": return 16;
                    case "title": return 17;
                    case "subtasks": return 40;
                    case "moved": return 41;
                    case "dublicate": return 42;
                    case "favorites": return 43;
                    case "status": return 44;
                    case "new_field": return 1;
                    default: return 0;
                }
            }
        }
        public string status_type { get; set; }
        public long FieldID { get; set; } // dùng cho new field (id_row của we_fields_project_team)
        public string Value { get; set; } // dùng cho new field (nếu có options thì truyền ID không truyền text)
        public string WorkID { get; set; } // dùng cho new field // giống id_row
        public string TypeID { get; set; } // dùng cho new field //




    }
    public class ColumnWorkModel
    {
        public long id_row { get; set; } = 0;
        public long id_project_team { get; set; }
        public string columnname { get; set; }
        public bool isnewfield { get; set; }
        public long TypeID { get; set; }
        public List<OptionsModel> Options { get; set; }
        public string Title { get; set; } // áp dụng cho trường hợp IsNewField = 1

    }
    public class DragDropModel
    {
        public long id_row { get; set; }
        public int typedrop { get; set; } // 1 - Kéo từ list - sang list (Chung project) thì thay đổi vị trí, 2- kéo từ check list - list, 3 - Kéo từ status - status, 4 - kéo từ list - sang list, 5 - kéo thay đổi vị trí cột
        public long id_project_team { get; set; }
        public long id_parent { get; set; }
        public long status { get; set; }
        public long status_to { get; set; }
        public long status_from { get; set; }
        public long id_to { get; set; }
        public long id_from { get; set; }
        public string fieldname { get; set; }
        public long priority_from { get; set; } // áp dụng case = 2, cập nhật vị trí mới (Vị trí cao nhất) cho item đến (không cần care các subtask đang ở vị trí nào) 
        public bool IsAbove { get; set; } // true: Kéo cùng cấp trên, false: kéo cùng cập dưới (Thì cập nhật lại vị trí cho các item ở phía trên)

    }
    public class WorkTagModel
    {
        public long id_row { get; set; }
        public long id_work { get; set; }
        public long id_tag { get; set; }
    }

    public class WorkUserModel
    {
        public long id_row { get; set; }
        public long id_work { get; set; }
        public long id_user { get; set; }
        public int loai { get; set; }//1: assign, 2 follow

    }
    public class ImportWorkModel
    {
        public string File { get; set; }
        public string FileName { get; set; }
        public string Sheet { get; set; }
        public bool Review { get; set; } = true;
        public long id_project_team { get; set; }
    }
    public class ReviewModel
    {
        public string id_row { get; set; }
        public string title { get; set; }
        public string note { get; set; }
        public DataTable dtW { get; set; }
    }
    public class DataImportModel
    {
        public DataTable dtW { get; set; }
        public DataTable dtPK { get; set; }//Tag và milestone mới và group
        public DataTable dtUser { get; set; }
        public DataTable dtTag { get; set; }
    }
    public class OptionsModel
    {
        public long rowid { get; set; }
        public long FieldID { get; set; }
        public long TypeID { get; set; }
        public long ID_project_team { get; set; }
        public string Value { get; set; }
        public string Color { get; set; }
        public string Note { get; set; }



    }

}