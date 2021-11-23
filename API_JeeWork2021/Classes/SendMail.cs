using System;
using System.Data;
using System.Configuration;
using System.Web;
using System.Collections;
using System.Net.Mail;
using System.IO;
using System.Net;
using DpsLibs.Data;
using System.Threading.Tasks;
using JeeWork_Core2021.Classes;
using JeeWork_Core2021.Models;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Options;
using DPSinfra.Notifier;
using System.Linq;

namespace JeeWork_Core2021.Classes
{
    /// <summary>
    /// Summary description for SendMail
    /// </summary>
    public class SendMail
    {
        private JeeWorkConfig _config;
        private readonly IHostingEnvironment _hostingEnvironment;
        public SendMail(IOptions<JeeWorkConfig> config, IHostingEnvironment hostingEnvironment)
        {
            _config = config.Value;
            //
            // TODO: Add constructor logic here
            //
        }
        public static bool Send(string templatefile, Hashtable Replace, string mailTo, string title, MailAddressCollection cc, string CustemerID, string AttacheFile, bool SaveCannotSend, out string ErrorMessage, string ConnectionString)
        {
            if ((mailTo == null) || ("".Equals(mailTo.Trim())))
            {
                ErrorMessage = "Email người  nhận không đúng";
                return true;
            }
            string contents = "";
            try
            {
                contents = File.ReadAllText(templatefile, System.Text.UTF8Encoding.UTF8);
            }
            catch (Exception ex)
            {
                ErrorMessage = ex.Message;
                return false;
            }
            foreach (DictionaryEntry Ent in Replace)
            {
                contents = contents.Replace(Ent.Key.ToString(), Ent.Value.ToString());
            }
            return Send(mailTo, title, cc, contents, CustemerID, "", SaveCannotSend, out ErrorMessage, ConnectionString);
        }
        public static bool Send(string templatefile, Hashtable Replace, string mailTo, string title, MailAddressCollection cc, string CustemerID, string AttacheFile, bool SaveCannotSend, out string ErrorMessage, MailInfo MInfo, string ConnectionString)
        {
            if ((mailTo == null) || ("".Equals(mailTo.Trim())))
            {
                ErrorMessage = "Email người  nhận không đúng";
                return true;
            }
            string contents = "";
            try
            {
                contents = File.ReadAllText(templatefile, System.Text.UTF8Encoding.UTF8);
            }
            catch (Exception ex)
            {
                ErrorMessage = ex.Message;
                return false;
            }
            foreach (DictionaryEntry Ent in Replace)
            {
                contents = contents.Replace(Ent.Key.ToString(), Ent.Value.ToString());
            }
            MailAddress email = new MailAddress(mailTo);
            MailAddressCollection To = new MailAddressCollection();
            To.Add(email);
            return Send(To, title, cc, contents, CustemerID, "", SaveCannotSend, out ErrorMessage, MInfo, ConnectionString);
        }
        public static bool Send(MailAddressCollection mailTo, string title, MailAddressCollection cc, string contents, string CustemerID, string AttacheFile, bool SaveCannotSend, out string ErrorMessage, string ConnectionString)
        {
            GetDateTime UTCdate = new GetDateTime();
            if (mailTo.Count <= 0)
            {
                ErrorMessage = "Email không hợp lệ";
                return true;
            }
            DataTable dt = new DataTable();
            using (DpsConnection cnn = new DpsConnection(ConnectionString))
            {
                SqlConditions cond = new SqlConditions();
                cond.Add("RowID", CustemerID);
                dt = cnn.CreateDataTable("select SmtpClient, Port, Email, Password, EnableSSL, Username from tbl_custemers where (where)", "(where)", cond);
            }
            if (dt.Rows.Count <= 0)
            {
                if (SaveCannotSend)
                    SaveMailCannotSend(title, mailTo, contents, "Không tìm thấy khách hàng", cc, CustemerID, ConnectionString);
                ErrorMessage = "Không tìm thấy cấu hình mailserver";
                return false;
            }
            else
            {
                int port = 0;
                if (!int.TryParse(dt.Rows[0]["Port"].ToString(), out port))
                {
                    if (SaveCannotSend)
                        SaveMailCannotSend(title, mailTo, contents, "Chưa cấu hình email", cc, CustemerID, ConnectionString);
                    ErrorMessage = "Thông tin port trong cấu hình mail server không hợp lệ";
                    return false;
                }
                //Task.Factory.StartNew(() =>
                //{

                string email = dt.Rows[0]["email"].ToString();
                string username = dt.Rows[0]["username"].ToString();
                string SmtpClient = dt.Rows[0]["SmtpClient"].ToString();
                string password = "";
                try
                {
                    password = DpsLibs.Common.EncDec.Decrypt(dt.Rows[0]["Password"].ToString(), "JeeHR_DPSSecurity435");
                }
                catch { }
                SmtpClient s = new SmtpClient(SmtpClient, port);
                s.UseDefaultCredentials = false;
                if (bool.TrueString.Equals(dt.Rows[0]["EnableSSL"].ToString()))
                    s.EnableSsl = true;
                else s.EnableSsl = false;
                s.Credentials = new NetworkCredential(username, password);
                s.DeliveryMethod = SmtpDeliveryMethod.Network;
                MailMessage m = new MailMessage();
                string guiden = "", guikem = "";
                for (int i = 0; i < mailTo.Count; i++)
                {
                    m.To.Add(mailTo[i]);
                    guiden += "," + mailTo[i];
                }
                m.From = new MailAddress(email);
                if ((!"".Equals(AttacheFile)) && (File.Exists(AttacheFile)))
                {
                    Attachment att = new Attachment(AttacheFile);
                    m.Attachments.Add(att);
                }
                for (int i = 0; i < cc.Count; i++)
                {
                    m.CC.Add(cc[i]);
                    guikem += "," + cc[i];
                }
                m.IsBodyHtml = true;
                m.Subject = title;
                m.Body = contents;
                if (!"".Equals(guiden)) guiden = guiden.Substring(1);
                DpsConnection cnn1 = new DpsConnection(ConnectionString);
                try
                {
                    s.Send(m);
                    //Lưu lại email đã gửi
                    Hashtable val = new Hashtable();
                    val.Add("MailTo", guiden);
                    val.Add("Title", title);
                    if (!"".Equals(guikem)) guikem = guikem.Substring(1);
                    val.Add("Cc", guikem);
                    val.Add("Contents", contents);
                    val.Add("SendTime", UTCdate.Date);
                    val.Add("SendDate", DateTime.Today);
                    val.Add("SendFrom", email);
                    val.Add("CustemerID", CustemerID);
                    cnn1.Insert(val, "Sys_SendMail");
                    cnn1.Disconnect();
                }
                catch (Exception ex)
                {
                    if (SaveCannotSend)
                    {
                        Hashtable val = new Hashtable();
                        val.Add("Title", title);
                        val.Add("Email", guiden);
                        val.Add("Contents", contents);
                        val.Add("LastSend", UTCdate.Date);
                        val.Add("Lan", 1);
                        val.Add("Error", ex.Message);
                        val.Add("CustemerID", CustemerID);
                        if (!"".Equals(guikem)) guikem = guikem.Substring(1);
                        val.Add("cc", guikem);
                        cnn1.Insert(val, "Tbl_emailchuaguiduoc");
                        cnn1.Disconnect();
                    }
                    //SaveMailCannotSend(title, mailTo, contents, ex.Message, cc, CustemerID);
                }
                //});
            }

            ErrorMessage = "";
            return true;
        }
        public static bool Send(MailAddressCollection mailTo, string title, MailAddressCollection cc, string TempateFile, Hashtable Replace, string CustemerID, string AttacheFile, bool SaveCannotSend, out string ErrorMessage, string ConnectionString)
        {
            string contents = "";
            try
            {
                contents = File.ReadAllText(TempateFile, System.Text.UTF8Encoding.UTF8);
            }
            catch (Exception ex)
            {
                ErrorMessage = ex.Message;
                return false;
            }
            foreach (DictionaryEntry Ent in Replace)
            {
                contents = contents.Replace(Ent.Key.ToString(), Ent.Value.ToString());
            }
            return Send(mailTo, title, cc, contents, CustemerID, "", SaveCannotSend, out ErrorMessage, ConnectionString);
        }
        public static bool Send(string mailTo, string title, MailAddressCollection cc, string contents, string CustemerID, string AttacheFile, bool SaveCannotSend, out string ErrorMessage, string ConnectionString)
        {
            MailAddress email = new MailAddress(mailTo);
            MailAddressCollection to = new MailAddressCollection();
            to.Add(email);
            return Send(to, title, cc, contents, CustemerID, AttacheFile, SaveCannotSend, out ErrorMessage, ConnectionString);
        }
        public static bool SendWithConnection(MailAddressCollection mailTo, string title, MailAddressCollection cc, string contents, string CustemerID, string AttacheFile, bool SaveCannotSend, out string ErrorMessage, DpsConnection cnn, string ConnectionString)
        {
            GetDateTime UTCdate = new GetDateTime();
            string email = "hrm@dps.com.vn";
            string username = "hrm@dps.com.vn";
            string SmtpClient = "smtp.gmail.com";
            bool EnableSSL = true;
            int port = 587;
            string password = "3mailHRm@dps";
            SmtpClient s = new SmtpClient(SmtpClient, port);
            s.UseDefaultCredentials = false;
            s.EnableSsl = true;
            s.Credentials = new NetworkCredential(username, password);
            s.DeliveryMethod = SmtpDeliveryMethod.Network;
            MailMessage m = new MailMessage();
            string guiden = "", guikem = "";
            for (int i = 0; i < mailTo.Count; i++)
            {
                m.To.Add(mailTo[i]);
                guiden += "," + mailTo[i];
            }
            m.From = new MailAddress(email);
            if ((!"".Equals(AttacheFile)) && (File.Exists(AttacheFile)))
            {
                Attachment att = new Attachment(AttacheFile);
                m.Attachments.Add(att);
            }
            for (int i = 0; i < cc.Count; i++)
            {
                m.CC.Add(cc[i]);
                guikem += "," + cc[i];
            }
            m.IsBodyHtml = true;
            m.Subject = title;
            m.Body = contents;
            if (!"".Equals(guiden)) guiden = guiden.Substring(1);
            try
            {
                s.Send(m);
                //Lưu lại email đã gửi
                Hashtable val = new Hashtable();
                val.Add("MailTo", guiden);
                val.Add("Title", title);
                if (!"".Equals(guikem)) guikem = guikem.Substring(1);
                val.Add("Cc", guikem);
                val.Add("Contents", contents);
                val.Add("SendTime", UTCdate.Date);
                val.Add("SendDate", DateTime.Today);
                val.Add("SendFrom", email);
                val.Add("CustemerID", CustemerID);
                cnn.Insert(val, "Sys_SendMail");
            }
            catch (Exception ex)
            {
                if (SaveCannotSend)
                {
                    Hashtable val = new Hashtable();
                    val.Add("Title", title);
                    val.Add("Email", guiden);
                    val.Add("Contents", contents);
                    val.Add("LastSend", UTCdate.Date);
                    val.Add("Lan", 1);
                    val.Add("Error", ex.Message);
                    val.Add("CustemerID", CustemerID);
                    if (!"".Equals(guikem)) guikem = guikem.Substring(1);
                    val.Add("cc", guikem);
                    cnn.Insert(val, "Tbl_emailchuaguiduoc");
                }
                //SaveMailCannotSend(title, mailTo, contents, ex.Message, cc, CustemerID);
            }
            //});
            ErrorMessage = "";
            return true;
        }
        public static bool SendWithConnection(MailAddressCollection mailTo, string title, MailAddressCollection cc, string TempateFile, Hashtable Replace, string CustemerID, string AttacheFile, bool SaveCannotSend, out string ErrorMessage, DpsConnection cnn, string ConnectionString)
        {
            string contents = "";
            try
            {
                contents = File.ReadAllText(TempateFile, System.Text.UTF8Encoding.UTF8);
            }
            catch (Exception ex)
            {
                ErrorMessage = ex.Message;
                return false;
            }
            foreach (DictionaryEntry Ent in Replace)
            {
                contents = contents.Replace(Ent.Key.ToString(), Ent.Value.ToString());
            }
            return SendWithConnection(mailTo, title, cc, contents, CustemerID, "", SaveCannotSend, out ErrorMessage, cnn, ConnectionString);
        }
        public static bool SendWithConnection(string mailTo, string title, MailAddressCollection cc, string contents, string CustemerID, string AttacheFile, bool SaveCannotSend, out string ErrorMessage, DpsConnection cnn, string ConnectionString)
        {
            MailAddress email = new MailAddress(mailTo);
            MailAddressCollection to = new MailAddressCollection();
            to.Add(email);
            return SendWithConnection(to, title, cc, contents, CustemerID, AttacheFile, SaveCannotSend, out ErrorMessage, cnn, ConnectionString);
        }
        public static bool SendWithConnection(string templatefile, Hashtable Replace, string mailTo, string title, MailAddressCollection cc, string CustemerID, string AttacheFile, bool SaveCannotSend, out string ErrorMessage, DpsConnection cnn, string ConnectionString)
        {
            if ((mailTo == null) || ("".Equals(mailTo.Trim())))
            {
                ErrorMessage = "Email người nhận không đúng";
                return true;
            }
            string contents = "";
            try
            {
                contents = File.ReadAllText(templatefile, System.Text.UTF8Encoding.UTF8);
            }
            catch (Exception ex)
            {
                ErrorMessage = ex.Message;
                return false;
            }
            foreach (DictionaryEntry Ent in Replace)
            {
                contents = contents.Replace(Ent.Key.ToString(), Ent.Value.ToString());
            }
            return SendWithConnection(mailTo, title, cc, contents, CustemerID, "", SaveCannotSend, out ErrorMessage, cnn, ConnectionString);
        }
        private static void SaveMailCannotSend(string title, MailAddressCollection mailTo, string contents, string error, MailAddressCollection cc, string CustemerID, string ConnectionString)
        {
            GetDateTime UTCdate = new GetDateTime();
            Hashtable val = new Hashtable();
            val.Add("Title", title);
            val.Add("Email", mailTo);
            val.Add("Contents", contents);
            val.Add("LastSend", UTCdate.Date);
            val.Add("Lan", 1);
            val.Add("Error", error);
            val.Add("CustemerID", CustemerID);
            string guikem = "";
            for (int i = 0; i < cc.Count; i++)
            {
                guikem += "," + cc[i].Address;
            }
            if (!"".Equals(guikem)) guikem = guikem.Substring(1);
            val.Add("cc", guikem);
            using (DpsConnection cnn = new DpsConnection(ConnectionString))
            {
                cnn.Insert(val, "Tbl_emailchuaguiduoc");
            }
        }
        public static bool Send(MailAddressCollection mailTo, string title, MailAddressCollection cc, string TempateFile, Hashtable Replace, string CustemerID, string AttacheFile, bool SaveCannotSend, out string ErrorMessage, MailInfo MInfo, string ConnectionString)
        {
            string contents = "";
            try
            {
                contents = File.ReadAllText(TempateFile, System.Text.UTF8Encoding.UTF8);
            }
            catch (Exception ex)
            {
                ErrorMessage = ex.Message;
                return false;
            }
            foreach (DictionaryEntry Ent in Replace)
            {
                contents = contents.Replace(Ent.Key.ToString(), Ent.Value.ToString());
            }
            return Send(mailTo, title, cc, contents, CustemerID, "", SaveCannotSend, out ErrorMessage, MInfo, ConnectionString);
        }
        public static bool Send(MailAddressCollection mailTo, string title, MailAddressCollection cc, string contents, string CustemerID, string AttacheFile, bool SaveCannotSend, out string ErrorMessage, MailInfo MInfo, string ConnectionString)
        {
            GetDateTime UTCdate = new GetDateTime();
            if (mailTo.Count <= 0)
            {
                ErrorMessage = "Email không hợp lệ";
                return true;
            }
            if (string.IsNullOrEmpty(MInfo.Email))
            {
                if (SaveCannotSend)
                    SaveMailCannotSend(title, mailTo, contents, "Không tìm thấy khách hàng", cc, CustemerID, ConnectionString);
                ErrorMessage = "Không tìm thấy cấu hình mailserver";
                return false;
            }
            else
            {
                bool enablessl = MInfo.EnableSSL;
                string smptclient = MInfo.SmptClient;
                int port = MInfo.Port;
                string username = MInfo.UserName;
                string password = MInfo.Password;
                string email = MInfo.Email.ToString();
                if ("".Equals(email))
                {
                    ErrorMessage = "Chưa cấu hình email";
                    return false;
                }
                Task.Factory.StartNew(() =>
                {
                    SmtpClient s = new SmtpClient(smptclient, port);
                    s.UseDefaultCredentials = false;
                    s.EnableSsl = enablessl;
                    s.Credentials = new NetworkCredential(username, password);
                    s.DeliveryMethod = SmtpDeliveryMethod.Network;
                    MailMessage m = new MailMessage();
                    string guiden = "", guikem = "";
                    for (int i = 0; i < mailTo.Count; i++)
                    {
                        m.To.Add(mailTo[i]);
                        guiden += "," + mailTo[i];
                    }
                    m.From = new MailAddress(email);
                    if ((!"".Equals(AttacheFile)) && (File.Exists(AttacheFile)))
                    {
                        Attachment att = new Attachment(AttacheFile);
                        m.Attachments.Add(att);
                    }
                    for (int i = 0; i < cc.Count; i++)
                    {
                        m.CC.Add(cc[i]);
                        guikem += "," + cc[i];
                    }
                    m.IsBodyHtml = true;
                    m.Subject = title;
                    m.Body = contents;
                    if (!"".Equals(guiden)) guiden = guiden.Substring(1);
                    DpsConnection cnn1 = new DpsConnection(ConnectionString);
                    try
                    {
                        s.Send(m);
                        //Lưu lại email đã gửi
                        Hashtable val = new Hashtable();
                        val.Add("MailTo", guiden);
                        val.Add("Title", title);
                        if (!"".Equals(guikem)) guikem = guikem.Substring(1);
                        val.Add("Cc", guikem);
                        val.Add("Contents", contents);
                        val.Add("SendTime", UTCdate.Date);
                        val.Add("SendDate", DateTime.Today);
                        val.Add("SendFrom", email);
                        val.Add("CustemerID", CustemerID);
                        cnn1.Insert(val, "Sys_SendMail");
                        cnn1.Disconnect();
                    }
                    catch (Exception ex)
                    {
                        if (SaveCannotSend)
                        {
                            Hashtable val = new Hashtable();
                            val.Add("Title", title);
                            val.Add("Email", guiden);
                            val.Add("Contents", contents);
                            val.Add("LastSend", UTCdate.Date);
                            val.Add("Lan", 1);
                            val.Add("Error", ex.Message);
                            val.Add("CustemerID", CustemerID);
                            if (!"".Equals(guikem)) guikem = guikem.Substring(1);
                            val.Add("cc", guikem);
                            cnn1.Insert(val, "Tbl_emailchuaguiduoc");
                            cnn1.Disconnect();
                        }
                    }
                });
            }
            ErrorMessage = "";
            return true;
        }
        public static bool Send(string mailTo, string title, MailAddressCollection cc, string contents, string CustemerID, string AttacheFile, bool SaveCannotSend, out string ErrorMessage, MailInfo MInfo, string ConnectionString)
        {
            MailAddress email = new MailAddress(mailTo);
            MailAddressCollection to = new MailAddressCollection();
            to.Add(email);
            return Send(to, title, cc, contents, CustemerID, AttacheFile, SaveCannotSend, out ErrorMessage, MInfo, ConnectionString);
        }
        public static bool Send_Synchronized(string mailTo, string title, MailAddressCollection cc, string contents, string CustemerID, string AttacheFile, bool SaveCannotSend, out string ErrorMessage, MailInfo MInfo, string ConnectionString, INotifier _notifier)
        {
            MailAddress email = new MailAddress(mailTo);
            MailAddressCollection to = new MailAddressCollection();
            to.Add(email);
            return Send_Synchronized(to, title, cc, contents, CustemerID, AttacheFile, SaveCannotSend, out ErrorMessage, MInfo, ConnectionString, _notifier);
        }
        public static bool Send_Synchronized(MailAddressCollection mailTo, string title, MailAddressCollection cc, string contents, string CustemerID, string AttacheFile, bool SaveCannotSend, out string ErrorMessage, MailInfo MInfo, string ConnectionString, INotifier _notifier)
        {
            GetDateTime UTCdate = new GetDateTime();
            if (mailTo.Count <= 0)
            {
                ErrorMessage = "Email không hợp lệ";
                return true;
            }
            if ("".Equals(MInfo.Email))
            {
                if (SaveCannotSend)
                    SaveMailCannotSend(title, mailTo, contents, "Không tìm thấy khách hàng", cc, CustemerID, ConnectionString);
                ErrorMessage = "Không tìm thấy cấu hình mailserver";
                return false;
            }
            else
            {
                bool enablessl = MInfo.EnableSSL;
                string smptclient = MInfo.SmptClient;
                int port = MInfo.Port;
                string username = MInfo.UserName;
                string password = MInfo.Password;
                string email = MInfo.Email;
                if ("".Equals(email))
                {
                    ErrorMessage = "Chưa cấu hình email";
                    return false;
                }
                Task.Factory.StartNew(() =>
                {
                    SmtpClient s = new SmtpClient(smptclient, port);
                    s.UseDefaultCredentials = false;
                    s.EnableSsl = enablessl;
                    s.Credentials = new NetworkCredential(username, password);
                    s.DeliveryMethod = SmtpDeliveryMethod.Network;
                    MailMessage m = new MailMessage();
                    string guiden = "", mailcc = "";
                    for (int i = 0; i < mailTo.Count; i++)
                    {
                        m.To.Add("huytranvan1404@gmail.com");
                        guiden += "," + mailTo[i];
                    }
                    m.From = new MailAddress(email);
                    if ((!"".Equals(AttacheFile)) && (File.Exists(AttacheFile)))
                    {
                        Attachment att = new Attachment(AttacheFile);
                        m.Attachments.Add(att);
                    }
                    for (int i = 0; i < cc.Count; i++)
                    {
                        //m.CC.Add(cc[i]);
                        mailcc += "," + cc[i];
                    }
                    m.IsBodyHtml = true;
                    m.Subject = title;
                    m.Body = contents + guiden + mailcc;
                    if (!"".Equals(guiden)) guiden = guiden.Substring(1);
                    DpsConnection cnn1 = new DpsConnection(ConnectionString);
                    try
                    {
                        s.Send(m);
                        emailMessage asyncnotice = new emailMessage()
                        {
                            CustomerID = long.Parse(CustemerID),
                            access_token = "",
                            to = guiden,
                            cc = string.Join(",", mailcc.Split(',').Where(x => !string.IsNullOrEmpty(x))),
                            subject = title,
                            html = contents //nội dung html
                        };
                        _notifier.sendEmail(asyncnotice);
                        //Lưu lại email đã gửi
                        Hashtable val = new Hashtable();
                        val.Add("MailTo", guiden);
                        val.Add("Title", title);
                        if (!"".Equals(mailcc)) mailcc = mailcc.Substring(1);
                        val.Add("cc", mailcc);
                        val.Add("Contents", contents);
                        val.Add("SendTime", UTCdate.Date);
                        val.Add("SendDate", DateTime.Today);
                        val.Add("SendFrom", email);
                        val.Add("CustemerID", CustemerID);
                        cnn1.Insert(val, "Sys_SendMail");
                        cnn1.Disconnect();
                    }
                    catch (Exception ex)
                    {
                        if (SaveCannotSend)
                        {
                            Hashtable val = new Hashtable();
                            val.Add("title", title);
                            val.Add("Email", guiden);
                            val.Add("Contents", contents);
                            val.Add("LastSend", UTCdate.Date);
                            val.Add("Lan", 1);
                            val.Add("Error", ex.Message);
                            val.Add("CustemerID", CustemerID);
                            if (!"".Equals(mailcc)) mailcc = mailcc.Substring(1);
                            val.Add("cc", mailcc);
                            cnn1.Insert(val, "Tbl_emailchuaguiduoc");
                            cnn1.Disconnect();
                        }
                    }
                });
            }
            ErrorMessage = "";
            return true;
        }
        public class MailInfo
        {
            public MailInfo(string CustemerID, DpsConnection cnn)
            {
            }
            public MailInfo()
            {
                InfoMailTest();
            }
            public MailInfo(string CustemerID, string ConnectionString)
            {
                using (DpsConnection cnn = new DpsConnection(ConnectionString))
                {
                }
            }
            private void InfoMailTest()
            {
                Email = "hrm@dps.com.vn";
                UserName = "hrm@dps.com.vn";
                SmptClient = "smtp.gmail.com";
                EnableSSL = true;
                Port = 587;
                Password = "3mailHRm@dps";
            }
            public string Email;
            public string UserName;
            public string SmptClient;
            public string Password;
            public bool EnableSSL;
            public int Port;
        }
    }
}