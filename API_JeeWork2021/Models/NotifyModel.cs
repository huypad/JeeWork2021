using DpsLibs.Data;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Web;

namespace JeeWork_Core2021.Models
{
    public class NotifyModel
    {
        public string From_IDNV { get; set; }
        public string To_IDNV { get; set; }
        public string AppCode { get; set; } = "WORK";
        public string TitleLanguageKey { get; set; }
        public Hashtable ReplaceData { get; set; }
        public string To_Link_WebApp { get; set; }
        public string To_Link_MobileApp { get; set; }
        public string ComponentName { get; set; } = "";
        public string Component { get; set; } = "";
    }
    public class NotificationMess
    {
        public string Content { get; set; }
        public string Img { get; set; }  //kèm avatar người gửi
        public string Icon { get; set; } //kèm icon thông báo
        public string AppCode { get; set; } //gửi từ app nào
        public string Link { get; set; } //link chuyển nếu có
        public string Domain { get; set; } //link chuyển nếu có
        public string oslink { get; set; } //link chuyển nếu có
        public int Loai { get; set; }
    }
    public class ConfigNotify
    {
        private static string connectionString;
        public ConfigNotify()
        {
        }
        public ConfigNotify(string CustemerID)
        {
            using (DpsConnection cnn = new DpsConnection(connectionString))
            {
                InitialData(CustemerID, "project", cnn);
            }
        }
        public ConfigNotify(string CustemerID, string object_type, DpsConnection cnn)
        {
            InitialData(CustemerID, "project", cnn);
        }
        private void InitialData(string id, string object_type, DpsConnection cnn)
        {
            //string select = "select config_notify, config_email from we_project_team where (where)";
            //SqlConditions cond = new SqlConditions();
            //cond.Add("id_row", id);
            //cond.Add("disabled", 0);
            //DataTable dt = cnn.CreateDataTable(select, "(where)", cond);
            //DataTable dt_notify = cnn.CreateDataTable("select * from we_log_action order by object_type");
            //if (dt.Rows.Count == 1)
            //{
            //    DataRow row = dt.Rows[0];
            //    string notify = dt.Rows[0][0].ToString();
            //    //ghép dữ liệu cho đủ 100 ký tự
            //    for (int i = notify.Length; i <= dt_notify.Rows.Count; i++)
            //    {
            //        notify += "0";
            //    }
            //    string _email = dt.Rows[0][1].ToString();
            //    //ghép dữ liệu cho đủ 100 ký tự
            //    for (int i = _email.Length; i <= dt_notify.Rows.Count; i++)
            //    {
            //        _email += "0";
            //    }
            //    string[] arrnotify = ConvertToArray(notify);
            //    int vitri_notify = 0;
            //    foreach (DataRow item in dt_notify.Rows)
            //    {
            //        item["id_row"] = "1".Equals(arrnotify[vitri_notify]);
            //        vitri_notify++;
            //    }
            //    string[] arremail = ConvertToArray(_email);
            //    int vitri_email = 0;
            //    foreach (DataRow item in dt_notify.Rows)
            //    {
            //        item["id_row"] = "1".Equals(arrnotify[vitri_email]);
            //        vitri_email++;
            //    }
            //}
            //custemerID = id;
        }
        public bool SaveConfig()
        {
            string s = "";
            //ghép dữ liệu cho đủ 100 ký tự
            for (int i = 1; i <= 100; i++)
            {
                s += "0";
            }
            string[] arr = ConvertToArray(s);
            string snotify = "";
            //ghép dữ liệu cho đủ 100 ký tự
            for (int i = 1; i <= 100; i++)
            {
                snotify += "0";
            }
            string[] Narr = ConvertToArray(s);
            string znotify = "";
            //ghép dữ liệu cho đủ 100 ký tự
            for (int i = 1; i <= 100; i++)
            {
                znotify += "0";
            }
            string[] Zarr = ConvertToArray(znotify);
            string dznotify = "";
            //ghép dữ liệu cho đủ 100 ký tự
            for (int i = 1; i <= 100; i++)
            {
                dznotify += "0";
            }
            string[] DZarr = ConvertToArray(dznotify);
            string sResult = ConvertToString(arr);
            string NsResult = ConvertToString(Narr);
            using (DpsConnection cnn = new DpsConnection(connectionString))
            {
                Hashtable val = new Hashtable();
                val.Add("config_notify", sResult);
                val.Add("config_email", NsResult);
                SqlConditions cond = new SqlConditions();
                //cond.Add("RowID", custemerID);
                int rs = cnn.Update(val, cond, "we_project_team");
                if (rs <= 0) return false;
            }
            return true;
        }
        public string[] ConvertToArray(string inputdata)
        {
            string[] result = new string[inputdata.Length];
            for (int i = 0; i < inputdata.Length; i++)
            {
                result[i] = inputdata.Substring(i, 1);
            }
            return result;
        }
        public string ConvertToString(string[] inputdata)
        {
            string result = "";
            for (int i = 0; i < inputdata.Length; i++)
            {
                result += inputdata[i];
            }
            return result;
        }
        public string ConvertToString(string[] inputdata, string ch)
        {
            string result = "";
            for (int i = 0; i < inputdata.Length; i++)
            {
                result += ch + inputdata[i];
            }
            if (result != "") result = result.Substring(1);
            return result;
        }
        public class ConfigNotifyModel
        {
            public long id_row { get; set; }
            public long id_project_team { get; set; }
            public bool values { get; set; }
            public bool isnotify { get; set; }
            public bool isemail { get; set; }
        }
    }
}