using DocumentFormat.OpenXml.Office.CoverPageProps;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace JeeWork_Core2021.Models
{
    public class AuthorizeModel
    {
        public long id_row { get; set; }
        public long id_user { get; set; }
        public bool is_all_project { get; set; }
        public string list_project { get; set; }
        public DateTime start_date { get; set; }
        public DateTime end_date { get; set; }
        public long CreatedBy { get; set; }
    }
}