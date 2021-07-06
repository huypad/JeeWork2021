using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace JeeWork_Core2021.Models
{
    public class AutomationListModel
    {
        public long rowid { get; set; }
        public string title { get; set; }
        public string description { get; set; }
        public string listid { get; set; }
        public string departmentid { get; set; }
        public string eventid { get; set; } // 1 event - nhiều hành động
        public string condition { get; set; }
        public long actionid { get; set; }
        public string data { get; set; } // Lưu data của action: comment, listid đối với duplicate task,
        public string status { get; set; } // 0: Inactive, 1: Active
        public List<Automation_SubAction_Model> subaction { get; set; }
        public List<Auto_Task_Model> task { get; set; }

        //public List<Automation_Events_Model> events { get; set; }
    }
    public class Automation_SubAction_Model
    {
        public long rowid { get; set; }
        public string autoid { get; set; }
        public long subactionid { get; set; }
        public string value { get; set; }
    }
    //public class Automation_Events_Model
    //{
    //    public string from { get; set; }
    //    public string to { get; set; }
    //    public string conditions { get; set; } // Các trường hơi
    //}
    public class Auto_Task_Model
    {
        public long id_row { get; set; }
        public string title { get; set; }
        public DateTime deadline { get; set; }
        public DateTime start_date { get; set; }
        public string description { get; set; }
        public long id_project_team { get; set; }
        public long id_group { get; set; }
        public long id_parent { get; set; }
        public long status { get; set; }
        /// <summary>
        /// assign và follower
        /// </summary>
        public List<WorkUserModel> users { get; set; }
        public List<WorkTagModel> tags { get; set; }
        public long priority { get; set; }
        public string startdate_type { get; set; }
        public string deadline_type { get; set; }
        public long autoid { get; set; }
    }
    public class Auto_Task_TagModel
    {
        public long id_row { get; set; }
        public long id_work { get; set; }
        public long id_tag { get; set; }
    }
    public class Auto_Task_UserModel
    {
        public long id_row { get; set; }
        public long id_work { get; set; }
        public long id_user { get; set; }
        public int loai { get; set; }//1: assign, 2 follow
    }
    public class Post_Automation_Model
    {
        public long eventid { get; set; }
        public long customerid { get; set; }
        public long taskid { get; set; }
        public long userid { get; set; }
        public long listid { get; set; }
        public long departmentid { get; set; }
        public string data_input { get; set; }
    }
    //public class Data
    //{
    //    public object X { get; set; }
    //    public object Y { get; set; }
    //}

}