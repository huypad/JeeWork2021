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
        public long Position { get; set; }
        public long Follower { get; set; }
        public long id_department { get; set; }
    }
    public class PositionModel
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
}