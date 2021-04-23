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

    }
   
}