using DocumentFormat.OpenXml.Office.CoverPageProps;
using JeeWork_Core2021.Classes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
namespace JeeWork_Core2021.Models
{
    public class RepeatedModel
    {
        public long id_row { get; set; }
        public string title { get; set; }
        public int frequency { get; set; } = 1;//1: tuần, 2:tháng
        public string repeated_day { get; set; }//thứ hoặc ngày lặp lại T2,CN hoặc 1,2,31
        public float deadline { get; set; }//số giờ
        public DateTime start_date { get; set; }
        public DateTime end_date { get; set; }
        public string description { get; set; }
        public string id_project_team { get; set; }
        public long id_group { get; set; }
        public bool Locked { get; set; }
        public long assign { get; set; }
        public List<RepeatedUserModel> Users { get; set; }
        public List<RepeatedTaskModel> Tasks { get; set; }
    }
    public class RepeatedUserModel
    {
        public long id_row { get; set; }
        public long id_repeated { get; set; }
        public long id_user { get; set; }
        public int loai { get; set; }//1: assign, 2 follow

    }
    public class RepeatedTaskUserModel
    {
        public long id_row { get; set; }
        public long id_repeated_task { get; set; }
        public long id_user { get; set; }
    }
    public class RepeatedTaskModel
    {
        public long id_row { get; set; }
        public long id_repeated { get; set; }
        public string Title { get; set; }
        public bool IsTodo { get; set; }
        public long UserID { get; set; }
        public long Deadline { get; set; }


    }
}