using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace JeeWork_Core2021.Models
{
    public class TagModel
    {
        public long id_row { get; set; }
        public long id_project_team { get; set; }
        public string title { get; set; }
        public string color { get; set; }
    }
}