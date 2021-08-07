using APIModel.DTO;
using DPSinfra.ConnectionCache;
using DPSinfra.Notifier;
using DpsLibs.Data;
using JeeWork_Core2021.Controller;
using JeeWork_Core2021.Controllers.Wework;
using JeeWork_Core2021.Models;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Net.Mail;
using System.Threading.Tasks;

namespace JeeWork_Core2021.Classes
{
    public class AutoSendMail
    {
        private readonly IHostingEnvironment _hostingEnvironment;
        private IConfiguration _configuration;
        private IConnectionCache ConnectionCache;
        private string ConnectionString;
        private INotifier _notifier;
        public AutoSendMail(IConnectionCache _cache, IConfiguration configuration, INotifier notifier)
        {
            //
            // TODO: Add constructor logic here
            //
            //1p chạy 1 lần(chỉ áp dụng những chức năng cần chạy sớm và phải chạy nhanh) - 60000
            Timer1Minute = new System.Timers.Timer(60000);
            Timer1Minute.Elapsed += new System.Timers.ElapsedEventHandler(Timer1Minute_Elapsed);
            //5p chạy 1 lần - 300000
            Timer5Minute = new System.Timers.Timer(300000);
            Timer5Minute.Elapsed += new System.Timers.ElapsedEventHandler(Timer5Minute_Elapsed);
            //10p chạy 1 lần - 600000
            TimerSendNotify = new System.Timers.Timer(60000);
            TimerSendNotify.Elapsed += new System.Timers.ElapsedEventHandler(TimerSendNotify_Elapsed);
            //60p chạy 1 lần
            TimerAutoUpdate = new System.Timers.Timer(3600000);
            TimerAutoUpdate.Elapsed += new System.Timers.ElapsedEventHandler(TimerAutoUpdate_Elapsed);
            _configuration = configuration;
            ConnectionCache = _cache;
            //ConnectionString = WeworkLiteController.getConnectionString(ConnectionCache, 1119, _configuration); // #update customerID
            _notifier = notifier;
        }
        public string MsgError;
        private string _basePath;
        public string BasePath
        {
            get
            {
                return _basePath;
            }
            set
            {
                _basePath = value;
            }
        }
        System.Timers.Timer TimerAutoUpdate;
        System.Timers.Timer TimerSendNotify;
        System.Timers.Timer Timer1Minute;
        System.Timers.Timer Timer5Minute;

        string CurrentMachineID = "";
        public Hashtable logtowrite = new Hashtable();
        string listerror = "";
        public static int dadocduoc = 0;
        public string NotSendmail;
        public void Start()
        {
            TimerAutoUpdate.Start();
            TimerSendNotify.Start();
            Timer1Minute.Start();
            Timer5Minute.Start();
        }
        public void Stop()
        {
            TimerAutoUpdate.Stop();
            TimerSendNotify.Stop();
            Timer1Minute.Stop();
            Timer1Minute.Stop();
        }

        protected void Timer1Minute_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            try
            {
            }
            catch (Exception ex)
            {
            }
        }
        private void Timer5Minute_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            try
            {
            }
            catch (Exception ex)
            {
            }
        }
        protected void TimerSendNotify_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            try
            {
                #region danh sách tài khoản customer
                List<long> DanhSachCustomer = WeworkLiteController.GetDanhSachCustomerID(_configuration);
                if (DanhSachCustomer != null)
                {
                    //long CustomerID = 1119; // để test
                    foreach (long CustomerID in DanhSachCustomer)
                    {
                        try
                        {
                            string _connection = WeworkLiteController.getConnectionString(ConnectionCache, CustomerID, _configuration); // #update customerID
                            using (DpsConnection cnnWW = new DpsConnection(_connection))
                            {
                                ThongBaoSapHetHan(cnnWW, CustomerID.ToString());
                                ThongBaoHetHan(cnnWW, CustomerID.ToString());
                                EveryDay_UpdateLate(cnnWW, CustomerID.ToString());
                                EveryDayForceRun(cnnWW, CustomerID.ToString());
                            }
                        }
                        catch (Exception ex)
                        {
                            //continue;
                        }
                    }
                }
                #endregion
            }
            catch (Exception ex)
            {
            }
        }
        void TimerAutoUpdate_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            try
            {
            }
            catch (Exception ex)
            {
            }
        }
        public class PushNotifyModel
        {
            public string[] Tokens { get; set; }
            public string Title { get; set; }
            public string Content { get; set; }
            public string Data { get; set; }
            public int Loai { get; set; }
        }
        private void EveryDayForceRun(DpsConnection cnn, string CustemerID)
        {
            PushNotifyModel notify = new PushNotifyModel();
            APIModel.Models.Notify Knoti;
            string select = "select id_row, title, frequency, repeated_day, id_project_team, id_group" +
                            ",deadline, start_date, end_date" +
                            ",description, assign, locked, createdDate, createdby" +
                            ",disabled, updatedDate, updatedBy, last_run, run_by " +
                            "from we_repeated where disabled = 0 and locked = 0 and customerid = " + CustemerID + "";
            DataTable dt = cnn.CreateDataTable(select);
            bool IsCreateAuto = false;
            if (dt.Rows.Count > 0)
            {
                foreach (DataRow _item in dt.Rows)
                {
                    try
                    {
                        DayOfWeek date = new DayOfWeek();
                        DateTime time = DateTime.UtcNow;
                        double loai = double.Parse(_item["frequency"].ToString());
                        string[] repeated_day = _item["repeated_day"].ToString().Split(',');
                        foreach (string day in repeated_day)
                        {
                            try
                            {
                                DateTime WeekdayCurrent = new DateTime();
                                if (loai == 1) // Nếu báo cáo theo hàng tuần
                                {
                                    date = Common.GetDayOfWeekDay(day);
                                    WeekdayCurrent = Common.StartOfWeek(time, date); // Lấy ra ngày (Param: thứ) của tuần hiện tại
                                    if (WeekdayCurrent < DateTime.UtcNow) // Nếu WeekdayCurrent nhỏ hơn ngày hiện tại, thì tạo dữ liệu cho thứ của tuần tiếp theo
                                    {
                                        try
                                        {
                                            WeekdayCurrent = WeekdayCurrent.AddDays(7);
                                        }
                                        catch (Exception ex)
                                        {
                                        }
                                    }
                                }
                                if (loai == 2) // Báo cáo theo tháng
                                {
                                    // Ngày lặp lại = ngày truyền vào của tháng và năm đó
                                    WeekdayCurrent = new DateTime(DateTime.Now.Year, DateTime.Now.Month, int.Parse(day));
                                    if (WeekdayCurrent < DateTime.UtcNow) // Nếu WeekdayCurrent nhỏ hơn ngày hiện tại, thì tạo dữ liệu cho thứ của tuần tiếp theo
                                    {
                                        try
                                        {
                                            WeekdayCurrent = WeekdayCurrent.AddMonths(1);
                                        }
                                        catch (Exception ex)
                                        {
                                        }
                                    }
                                }
                                DateTime ngaybd = (DateTime)_item["start_date"];
                                DateTime ngaykt = (DateTime)_item["end_date"];
                                // Kiểm tra ngày lấy được có nằm trong khoảng time lặp lại không
                                try
                                {
                                    if (WeekdayCurrent >= ngaybd && WeekdayCurrent <= ngaykt)
                                    {
                                        // Kiểm tra đã có dữ liệu repeated chưa
                                        SqlConditions cond = new SqlConditions();
                                        cond.Add("start_date", WeekdayCurrent);
                                        cond.Add("id_repeated", _item["id_row"]);
                                        cond.Add("Disabled", 0);
                                        DataTable dt_repeated_work = cnn.CreateDataTable("select * from we_work where (where)", "(where)", cond);
                                        if (dt_repeated_work.Rows.Count == 0)
                                            IsCreateAuto = true;
                                    }
                                }
                                catch (Exception ex)
                                {
                                }
                                // Tạo công việc
                                if (IsCreateAuto)
                                {
                                    bool result = RepeatedController.WeWork_CreateTaskAuto(dt, cnn, "0", WeekdayCurrent);
                                    if (result) // Gửi mail & notify
                                    {
                                        // Update lại thông tin ForceRun
                                        Hashtable has = new Hashtable();
                                        SqlConditions conds = new SqlConditions();
                                        conds.Add("id_row", _item["id_row"].ToString());
                                        has.Add("last_run", DateTime.Now);
                                        has.Add("run_by", 0);
                                        cnn.Update(has, conds, "we_repeated");
                                        string sql_new = "select we_work_user.*, we_work.title as tencongviec " +
                                            "from we_work join we_work_user on we_work_user.id_work = we_work.id_row " +
                                            "where we_work.disabled = 0 and id_repeated is not null";
                                        DataTable dt_New_Data = cnn.CreateDataTable(sql_new);
                                        if (dt_New_Data.Rows.Count > 0)
                                        {
                                            foreach (DataRow row in dt_New_Data.Rows)
                                            {
                                                var users = new List<long> { long.Parse(row["id_user"].ToString()) };
                                                UserJWT loginData = new UserJWT();
                                                loginData.CustomerID = int.Parse(CustemerID);
                                                loginData.LastName = "Hệ thống";
                                                loginData.UserID = 0;
                                                WeworkLiteController.mailthongbao(int.Parse(row["id_work"].ToString()), users, 10, loginData, ConnectionString, _notifier);
                                                #region Notify thêm mới công việc
                                                Hashtable has_replace = new Hashtable();
                                                for (int i = 0; i < users.Count; i++)
                                                {
                                                    NotifyModel notify_model = new NotifyModel();
                                                    has_replace = new Hashtable();
                                                    has_replace.Add("nguoigui", "Hệ thống");
                                                    has_replace.Add("tencongviec", row["tencongviec"]);
                                                    notify_model.AppCode = "WW";
                                                    notify_model.From_IDNV = "";
                                                    notify_model.To_IDNV = users[i].ToString();
                                                    notify_model.TitleLanguageKey = LocalizationUtility.GetBackendMessage("ww_themmoicongviec", "", "vi");
                                                    notify_model.TitleLanguageKey = notify_model.TitleLanguageKey.Replace("$nguoigui$", loginData.customdata.personalInfo.Fullname);
                                                    notify_model.TitleLanguageKey = notify_model.TitleLanguageKey.Replace("$tencongviec$", row["tencongviec"].ToString());
                                                    notify_model.ReplaceData = has_replace;
                                                    notify_model.To_Link_MobileApp = "";
                                                    notify_model.To_Link_WebApp = "/tasks/detail/" + int.Parse(row["id_work"].ToString()) + "";
                                                    try
                                                    {
                                                        if (notify_model != null)
                                                        {
                                                            Knoti = new APIModel.Models.Notify();
                                                            bool kq = Knoti.PushNotify(notify_model.From_IDNV, notify_model.To_IDNV, notify_model.AppCode, notify_model.TitleLanguageKey, notify_model.ReplaceData, notify_model.To_Link_WebApp, notify_model.To_Link_MobileApp, notify_model.ComponentName, notify_model.Component);
                                                        }
                                                    }
                                                    catch (Exception ex)
                                                    {
                                                    }
                                                }
                                                #endregion
                                            }
                                        }
                                    }
                                }
                            }
                            catch (Exception ex)
                            {
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                    }
                }
            }

        }
        // Update tình trạng và gửi email của những công việc trễ hạn
        private void EveryDay_UpdateLate(DpsConnection cnn, string CustemerID)
        {
            PushNotifyModel notify = new PushNotifyModel();
            APIModel.Models.Notify Knoti;
            Hashtable has = new Hashtable();
            SqlConditions conds = new SqlConditions();

            string select = "select * from v_wework where disabled = 0 and deadline is not null and deadline < GETDATE() and id_nv is not null";
            DataTable dt = cnn.CreateDataTable(select);
            if (dt.Rows.Count > 0)
            {
                foreach (DataRow _item in dt.Rows)
                {
                    DateTime time = DateTime.UtcNow;
                    has = new Hashtable();
                    conds = new SqlConditions();
                    conds.Add("id_row", _item["id_row"].ToString()); // id_row table we_work - get id_project_team -for lấy status theo project
                    conds.Add("id_project_team", _item["id_project_team"].ToString());
                    DataTable dt_status_late = cnn.CreateDataTable("select id_row,StatusName,IsDeadline,Follower,IsFinal,id_project_team " +
                                                       "from we_status where Disabled = 0 and IsDeadline = 1 and id_project_team = @id_project_team", conds);
                    long hoanthanh = long.Parse(cnn.ExecuteScalar("select id_row from we_status where id_project_team = @id_project_team and IsFinal = 1", conds).ToString());

                    if (hoanthanh != long.Parse(_item["status"].ToString()))
                    {
                        has.Add("status", dt_status_late.Rows[0]["id_row"].ToString());
                        cnn.Update(has, conds, "we_work");
                        var users = new List<long> { long.Parse(_item["id_nv"].ToString()) };
                        UserJWT loginData = new UserJWT();
                        loginData.CustomerID = int.Parse(CustemerID);
                        loginData.LastName = "Hệ thống";
                        loginData.UserID = 0;
                        #region Notify nhắc nhở công việc hết hạn
                        Hashtable has_replace = new Hashtable();
                        for (int i = 0; i < users.Count; i++)
                        {
                            NotifyModel notify_model = new NotifyModel();
                            has_replace = new Hashtable();
                            has_replace.Add("nguoigui", "Hệ thống");
                            has_replace.Add("tencongviec", _item["title"]);
                            notify_model.AppCode = "WW";
                            notify_model.From_IDNV = "";
                            notify_model.To_IDNV = users[i].ToString();
                            notify_model.TitleLanguageKey = LocalizationUtility.GetBackendMessage("ww_themmoicongviec", "", "vi");
                            notify_model.TitleLanguageKey = notify_model.TitleLanguageKey.Replace("$nguoigui$", loginData.customdata.personalInfo.Fullname);
                            notify_model.TitleLanguageKey = notify_model.TitleLanguageKey.Replace("$tencongviec$", _item["title"].ToString());
                            notify_model.ReplaceData = has_replace;
                            notify_model.To_Link_MobileApp = "";
                            notify_model.To_Link_WebApp = "/tasks";
                            try
                            {
                                if (notify_model != null)
                                {
                                    Knoti = new APIModel.Models.Notify();
                                    bool kq = Knoti.PushNotify(notify_model.From_IDNV, notify_model.To_IDNV, notify_model.AppCode, notify_model.TitleLanguageKey, notify_model.ReplaceData, notify_model.To_Link_WebApp, notify_model.To_Link_MobileApp, notify_model.ComponentName, notify_model.Component);
                                }
                            }
                            catch
                            { }
                        }
                        #endregion
                    }
                }
            }
        }

        private void ThongBaoSapHetHan(DpsConnection cnn, string CustemerID)
        {
            string baotruoc_deadline = Common.GetThamSo(cnn, CustemerID, 3);
            APIModel.Models.Notify Knoti;
            Hashtable has = new Hashtable();
            SqlConditions conds = new SqlConditions();
            string select = @"select (SELECT DATEDIFF(hour , GETDATE(), deadline)) as thoigianconlai, w.* 
from v_wework w where disabled = 0 and deadline is not null and deadline > (GETDATE()) 
and deadline< (GETDATE() +CONVERT(INT, ("+baotruoc_deadline+"))) and id_nv is not null";
            DataTable dt = cnn.CreateDataTable(select);

            List<long> users = new List<long>();

            UserJWT loginData = new UserJWT();
            loginData.CustomerID = int.Parse(CustemerID);
            loginData.LastName = "Hệ thống";
            loginData.UserID = 0;
            foreach (DataRow dr in dt.Rows)
            {
                //users.Add(long.Parse(dr["Id_NV"].ToString()));
                WeworkLiteController.mailthongbao(long.Parse(dr["id_row"].ToString()), new List<long> { long.Parse(dr["Id_NV"].ToString()) }, 17, loginData, ConnectionString, _notifier);//thiết lập vai trò admin
            }
        }

        private void ThongBaoHetHan(DpsConnection cnn, string CustemerID)
        {
            PushNotifyModel notify = new PushNotifyModel();
            APIModel.Models.Notify Knoti;
            Hashtable has = new Hashtable();
            SqlConditions conds = new SqlConditions();
            string select = @"select w.* from v_wework w where disabled = 0 and deadline is not null and deadline < (GETDATE()) 
and id_nv is not null and exists (select id_row from we_status where IsFinal <> 1 )";
            DataTable dt = cnn.CreateDataTable(select);

            List<long> users = new List<long>();

            UserJWT loginData = new UserJWT();
            loginData.CustomerID = int.Parse(CustemerID);
            loginData.LastName = "Hệ thống";
            loginData.UserID = 0;
            foreach (DataRow dr in dt.Rows)
            {
                //users.Add(long.Parse(dr["Id_NV"].ToString()));
                WeworkLiteController.mailthongbao(long.Parse(dr["id_row"].ToString()), new List<long> { long.Parse(dr["Id_NV"].ToString()) }, 17, loginData, ConnectionString, _notifier);//thiết lập vai trò admin
            }
        }
        public static void SendErrorReport(string custemerid, string errormsg, JeeWorkConfig config, string ConnectionString)
        {
            try
            {
                string mailto = config.Error_MailTo;
                string mcc = config.Error_MailCC;
                if (!string.IsNullOrEmpty(mailto))
                {
                    // #update #mail
                    using (DpsConnection cnn = new DpsConnection(ConnectionString))
                    {
                        MailInfo MInfo = new MailInfo(custemerid, cnn);
                        MailAddressCollection cc = new MailAddressCollection();
                        if (!string.IsNullOrEmpty(mcc))
                            cc.Add(mcc);
                        SendMail.Send(mailto, "Lỗi JeeWork", cc, "Nội dung lỗi: " + errormsg, custemerid, "", false, out errormsg, MInfo, ConnectionString);
                    }
                }
            }
            catch (Exception ex)
            {

            }
        }
    }
}
