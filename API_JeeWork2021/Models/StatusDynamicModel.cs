using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace JeeWork_Core2021.Models
{
    public class FilterWorkModel
    {
        public long id_row { get; set; }
        public string title { get; set; }
        public string color { get; set; }
        public List<FilterDetailModel> details { get; set; }
    }
    public class FilterDetailModel
    {
        public long id_row { get; set; }
        public int id_key { get; set; }
        public string @operator { get; set; }
        public string value { get; set; }

    }
    public class StatusListModel
    {
        public long id_row { get; set; }
        public string StatusName { get; set; }
        public string Description { get; set; }
        public string color { get; set; }
        public long Type { get; set; }
        public bool IsDefault { get; set; }
        public bool IsFinal { get; set; }
        public bool IsDeadline { get; set; }
        public bool IsToDo { get; set; }
    }
    public class TemplateStatusModel
    {
        public long id_row { get; set; }
        public string StatusName { get; set; }
        public string Description { get; set; }
        public string color { get; set; }
        public long Type { get; set; }
        public bool IsDefault { get; set; }
        public bool IsFinal { get; set; }
        public bool IsDeadline { get; set; }
        public bool IsToDo { get; set; }
        public long id_department { get; set; }
        public long StatusID { get; set; }
        public long TemplateID { get; set; }
        public long CustomerID { get; set; }
    }
    public class Different_Statuses
    {
        //public long Id_row { get; set; }
        public long id_project_team { get; set; }
        public bool IsMapAll { get; set; }
        public long TemplateID_New { get; set; }
        //public long TemplateID_Old { get; set; }
        public List<MapModel> Map_Detail { get; set; }
    }
    public class MapModel
    {
        public long old_status { get; set; }
        public long new_status { get; set; }
        //public string Type { get; set; }
        //public bool IsDefault { get; set; }
        //public string Color { get; set; }
        //public long Position { get; set; }
        //public long Follower { get; set; }
        //public string StatusName { get; set; }
        //public string Description { get; set; }
    }
}