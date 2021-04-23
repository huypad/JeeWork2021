using DocumentFormat.OpenXml.Office.CoverPageProps;
using JeeWork_Core2021.Classes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace JeeWork_Core2021.Models
{
    public class TopicModel
    {
        public long id_row { get; set; }
        public string title { get; set; }
        public string description { get; set; }
        public long id_project_team { get; set; }
        public bool email { get; set; }
        public List<TopicUserModel> Users { get; set; }
        public List<FileUploadModel> Attachments { get; set; }
        
    }
    public class TopicUserModel
    {
        public long id_row { get; set; }
        public long id_topic { get; set; }
        public long id_user { get; set; }
        public bool favourite { get; set; }

    }
}