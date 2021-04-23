using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace JeeWork_Core2021.Models
{
    public class ChecklistModel
    {
        public long id_row { get; set; }
        public long id_work { get; set; }
        public string title { get; set; }
    }
    public class ChecklistItemModel
    {
        public long id_row { get; set; }
        public long id_checklist { get; set; }
        public string title { get; set; }
        public long checker { get; set; }
        public long priority { get; set; }
    }
}