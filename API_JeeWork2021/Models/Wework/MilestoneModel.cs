﻿using DocumentFormat.OpenXml.Office.CoverPageProps;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace JeeWork_Core2021.Models
{
    public class MilestoneModel
    {
        public long id_row { get; set; }
        public string title { get; set; }
        public string description { get; set; }
        public long id_project_team { get; set; }
        public long person_in_charge { get; set; }
        public DateTime deadline { get; set; }
    }
}