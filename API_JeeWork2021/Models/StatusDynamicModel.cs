using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace JeeWork_Core2021.Models
{

    public class StatusDynamicModel
    {
        public long Id_row { get; set; }
        public string StatusName { get; set; }
        public string Description { get; set; }
        public long Id_project_team { get; set; }
        public string Type { get; set; }
        public bool? IsDefault { get; set; }
        public string Color { get; set; }
        public long Position { get; set; } = 1;
        public long Follower { get; set; }
        public long id_department { get; set; }
        public bool IsFinal { get; set; }
        public bool IsDeadline { get; set; }
        public bool IsToDo { get; set; }
        public long StatusID { get; set; } // Status tham chiếu tới
    }
    public class PositionModel //
    {
        public long id_row_from { get; set; }
        public long id_row_to { get; set; }
        public long position_from { get; set; }
        public long position_to { get; set; }
        public string columnname { get; set; }
        public long id_columnname { get; set; }
    }
    public class PositionTemplateModel
    {
        public long id_row_from { get; set; }
        public long id_row_to { get; set; }
        public long position_from { get; set; }
        public long position_to { get; set; }
        public long templateid { get; set; }
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
        public long position { get; set; } = 1;
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
    }
}