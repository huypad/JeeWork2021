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
            TimerSendNotify = new System.Timers.Timer(600000);
            TimerSendNotify.Elapsed += new System.Timers.ElapsedEventHandler(Timer10Minute_Elapsed);
            //60p chạy 1 lần - 3600000
            TimerAutoUpdate = new System.Timers.Timer(3600000);
            TimerAutoUpdate.Elapsed += new System.Timers.ElapsedEventHandler(Timer60Minute_Elapsed);
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
        public bool Time1IsRun;
        public bool Time5IsRun;
        public bool Time10IsRun;
        public bool Time60IsRun;
        public Hashtable logtowrite = new Hashtable();
        public static int dadocduoc = 0;
        public string NotSendmail;
        public void Start()
        {
            Time10IsRun = false;
            Time1IsRun = false;
            Time5IsRun = false;
            Time60IsRun = false;
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
            Time10IsRun = false;
            Time1IsRun = false;
            Time5IsRun = false;
            Time60IsRun = false;
        }

        protected void Timer1Minute_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            try
            {
                string _connection = WeworkLiteController.getConnectionString(ConnectionCache, 1119, _configuration); // #update customerID
                using (DpsConnection cnn = new DpsConnection(_connection))
                {

                }
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
        protected void Timer10Minute_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            //Time10IsRun = true; 
            //string _connection = ""; string ham = "CongViecHetHanTrongNgay"; string idkh = "0";
            //try
            //{
            //    #region danh sách customer
            //    List<long> list_customer = WeworkLiteController.GetDanhSachCustomerID(_configuration);
            //    if (list_customer != null)
            //    {
            //        //long CustomerID = 1119; // để test
            //        foreach (long CustomerID in list_customer)
            //        {
            //            _connection = WeworkLiteController.getConnectionString(ConnectionCache, CustomerID, _configuration); // #update customerID
            //            using (DpsConnection cnn = new DpsConnection(_connection))
            //            {
            //                CongViecHetHanTrongNgay(cnn, CustomerID.ToString(), ConnectionString);
            //                if (cnn.LastError != null)
            //                {
            //                    string content = " Timer10minute. Lỗi Database: " + cnn.LastError.Message;
            //                    string error_message = "";
            //                    string CustemerID1 = "0";
            //                    //Gửi thông báo khi phát sinh lỗi
            //                    SendMail.SendWithConnection("huypaddaica@gmail.com", "[JeeWork] " + DateTime.Now.ToString("dd/MM/yyyy HH:mm") + " Lỗi chạy tự động. Lỗi Database: ", new MailAddressCollection(), content, CustemerID1, "", false, out error_message, cnn, ConnectionString);
            //                }
            //            }
            //        }
            //    }
            //    #endregion
            //}
            //catch (Exception ex)
            //{
            //    string error = ex.Message;
            //    string content = " Timer10minute: " + ex.Message + ". Customer " + idkh + " funcion " + ham;
            //    string error_message = "";
            //    string CustemerID1 = "0";
            //    using (DpsConnection cnn = new DpsConnection())
            //    {
            //        //Gửi thông báo khi phát sinh lỗi
            //        SendMail.SendWithConnection("huypaddaica@gmail.com", "[JeeWork] " + DateTime.Now.ToString("dd/MM/yyyy HH:mm") + " Lỗi chạy tự động. Lỗi Database: ", new MailAddressCollection(), content, CustemerID1, "", false, out error_message, cnn, ConnectionString);
            //    }
            //}
            //Time10IsRun = false;
        }
        void Timer60Minute_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            if (Time60IsRun) return;
            Time60IsRun = true;
            string _connection = ""; string ham = "ThongBaoHetHan"; string idkh = "0";
            try
            {
                #region danh sách customer
                List<long> list_customer = WeworkLiteController.GetDanhSachCustomerID(_configuration);
                if (list_customer != null)
                {
                    foreach (long CustomerID in list_customer)
                    {
                        _connection = WeworkLiteController.getConnectionString(ConnectionCache, CustomerID, _configuration); // #update customerID
                        using (DpsConnection cnn = new DpsConnection(_connection))
                        {
                            ham = "EveryDayReminder"; idkh = CustomerID.ToString();
                            EveryDayReminder(cnn, CustomerID, _connection);
                            if (cnn.LastError != null)
                            {
                                string content = " Timer60minute. Lỗi Database: " + cnn.LastError.Message;
                                string error_message = "";
                                string CustemerID1 = "0";
                                //Gửi thông báo khi phát sinh lỗi
                                SendMail.SendWithConnection("huypaddaica@gmail.com", "[JeeWork] " + DateTime.Now.ToString("dd/MM/yyyy HH:mm") + " Lỗi chạy tự động. Lỗi Database: ", new MailAddressCollection(), content, CustemerID1, "", false, out error_message, cnn, ConnectionString);
                            }
                        }
                    }
                }
                #endregion
            }
            catch (Exception ex)
            {
                string error = ex.Message;
                string content = " Timer60minute: " + ex.Message + ". Customer " + idkh + " funcion " + ham;
                string error_message = "";
                string CustemerID1 = "0";
                using (DpsConnection cnn = new DpsConnection())
                {
                    //Gửi thông báo khi phát sinh lỗi
                    SendMail.SendWithConnection("huypaddaica@gmail.com", "[JeeWork] " + DateTime.Now.ToString("dd/MM/yyyy HH:mm") + " Lỗi chạy tự động. Lỗi Database: ", new MailAddressCollection(), content, CustemerID1, "", false, out error_message, cnn, ConnectionString);
                }
            }
            Time60IsRun = false;
        }
        public class PushNotifyModel
        {
            public string[] Tokens { get; set; }
            public string Title { get; set; }
            public string Content { get; set; }
            public string Data { get; set; }
            public int Loai { get; set; }
        }
        private void TaoCongViecTuDong(DpsConnection cnn, string CustemerID, string connectionString)
        {
            PushNotifyModel notify = new PushNotifyModel();
            APIModel.Models.Notify Knoti;
            string select = @"select id_row, title, frequency, repeated_day, id_project_team, id_group
                            ,deadline, start_date, end_date
                            ,description, assign, locked, createdDate, createdby
                            ,disabled, updatedDate, updatedBy, last_run, run_by 
                            from we_repeated where disabled = 0 and locked = 0 and customerid = " + CustemerID + "";
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
                                                WeworkLiteController.mailthongbao(int.Parse(row["id_work"].ToString()), users, 10, loginData, ConnectionString, _notifier, _configuration);
                                                #region Lấy thông tin để thông báo
                                                SendNotifyModel noti = WeworkLiteController.GetInfoNotify(10, ConnectionString);
                                                #endregion
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
                                                    notify_model.TitleLanguageKey = notify_model.TitleLanguageKey.Replace("$nguoigui$", loginData.LastName);
                                                    notify_model.TitleLanguageKey = notify_model.TitleLanguageKey.Replace("$tencongviec$", row["tencongviec"].ToString());
                                                    notify_model.ReplaceData = has_replace;
                                                    notify_model.To_Link_MobileApp = noti.link_mobileapp.Replace("$id$", row["id_work"].ToString());
                                                    notify_model.To_Link_WebApp = noti.link.Replace("$id$", row["id_work"].ToString());
                                                    //notify_model.To_Link_WebApp = "/tasks/detail/" + int.Parse(row["id_work"].ToString()) + "";
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
        private void CapNhatCongViecTreHan(DpsConnection cnn, string CustemerID, string connectionString)
        {
            PushNotifyModel notify = new PushNotifyModel();
            APIModel.Models.Notify Knoti;
            Hashtable has = new Hashtable();
            SqlConditions conds = new SqlConditions();
            // lấy công việc trễ hạn có trạng thái khác deadline và hoàn thành
            string select = @"select * from v_wework_new w where disabled = 0 
                            and deadline is not null and deadline < GETDATE() -- and id_nv is not null
                              and exists (select * from we_status s 
                              where w.status = s.id_row and IsFinal <> 1 and IsDeadline <> 1)";
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
                    DataTable dt_status_late = cnn.CreateDataTable("select id_row, StatusName, IsDeadline,Follower,IsFinal,id_project_team " +
                                                       "from we_status where Disabled = 0 and IsDeadline = 1 and id_project_team = @id_project_team", conds);
                    long hoanthanh = long.Parse(cnn.ExecuteScalar("select id_row from we_status where id_project_team = @id_project_team and IsFinal = 1", conds).ToString());

                    if (hoanthanh != long.Parse(_item["status"].ToString()))
                    {
                        has.Add("status", dt_status_late.Rows[0]["id_row"].ToString());
                        cnn.Update(has, conds, "we_work");
                        // nếu có người mới gửi thông báo
                        if (_item["id_nv"] != DBNull.Value)
                        {
                            var users = new List<long> { long.Parse(_item["id_nv"].ToString()) };
                            UserJWT loginData = new UserJWT();
                            loginData.CustomerID = int.Parse(CustemerID);
                            loginData.LastName = "Hệ thống";
                            loginData.UserID = 0;
                            #region Lấy thông tin để thông báo
                            SendNotifyModel noti = WeworkLiteController.GetInfoNotify(17, ConnectionString);
                            #endregion
                            #region Notify nhắc nhở công việc hết hạn
                            WeworkLiteController.mailthongbao(long.Parse(_item["id_row"].ToString()), users, 17, loginData, ConnectionString, _notifier, _configuration);
                            Hashtable has_replace = new Hashtable();
                            for (int i = 0; i < users.Count; i++)
                            {
                                NotifyModel notify_model = new NotifyModel();
                                has_replace = new Hashtable();
                                has_replace.Add("nguoigui", "Hệ thống");
                                has_replace.Add("tencongviec", _item["title"]);
                                notify_model.AppCode = "WORK";
                                notify_model.From_IDNV = "";
                                notify_model.To_IDNV = users[i].ToString();
                                notify_model.TitleLanguageKey = LocalizationUtility.GetBackendMessage("ww_thongbaocvtrehan", "", "vi");
                                notify_model.TitleLanguageKey = notify_model.TitleLanguageKey.Replace("$nguoigui$", loginData.LastName);
                                notify_model.TitleLanguageKey = notify_model.TitleLanguageKey.Replace("$tencongviec$", _item["title"].ToString());
                                notify_model.ReplaceData = has_replace;
                                notify_model.To_Link_MobileApp = noti.link_mobileapp.Replace("$id$", _item["id_row"].ToString());
                                notify_model.To_Link_WebApp = noti.link.Replace("$id$", _item["id_row"].ToString());

                                List<AccUsernameModel> DataAccount = WeworkLiteController.GetDanhSachAccountFromCustomerID(_configuration, long.Parse(CustemerID));
                                var info = DataAccount.Where(x => notify_model.To_IDNV.ToString().Contains(x.UserId.ToString())).FirstOrDefault();
                                if (info is not null)
                                {
                                    bool kq_noti = WeworkLiteController.SendNotify(loginData.Username, info.Username, notify_model, _notifier, _configuration);
                                }
                            }
                            #endregion
                        }
                    }
                }
            }
        }
        private void CapNhatDuAnTreHan(DpsConnection cnn, string CustemerID, string connectionString)
        {
            PushNotifyModel notify = new PushNotifyModel();
            APIModel.Models.Notify Knoti;
            Hashtable has = new Hashtable();
            SqlConditions conds = new SqlConditions();
            // lấy dự án có trạng thái đúng tiến độ và deadline trễ
            string select = @"select * from we_project_team p where disabled = 0 and 
                              end_date is not null and locked = 0 
                              and end_date < GETDATE() and status = 1";
            DataTable dt = cnn.CreateDataTable(select);
            if (dt.Rows.Count > 0)
            {
                foreach (DataRow _item in dt.Rows)
                {
                    DateTime time = DateTime.UtcNow;
                    has = new Hashtable();
                    conds = new SqlConditions();
                    conds.Add("id_row", _item["id_row"].ToString()); // id_row table we_work - get id_project_team -for lấy status theo project

                    has.Add("status", 2);
                    cnn.Update(has, conds, "we_project_team");
                    // lấy danh sách thành viên có trong dự án
                    string sqltv = @"select * from we_project_team_user where Disabled = 0 and id_project_team = " + _item["id_row"].ToString();
                    DataTable dttv = cnn.CreateDataTable(sqltv);
                    if (dttv.Rows.Count > 0)
                    {
                        // nếu có người mới gửi thông báo
                        foreach (DataRow user in dttv.Rows)
                        {
                            var users = new List<long> { long.Parse(user["id_user"].ToString()) };
                            UserJWT loginData = new UserJWT();
                            loginData.CustomerID = int.Parse(CustemerID);
                            loginData.LastName = "Hệ thống";
                            loginData.UserID = 0;
                            #region Lấy thông tin để thông báo
                            SendNotifyModel noti = WeworkLiteController.GetInfoNotify(39, ConnectionString);
                            #endregion
                            #region Notify nhắc nhở dự án hết hạn
                            WeworkLiteController.mailthongbao(long.Parse(_item["id_row"].ToString()), users, 39, loginData, ConnectionString, _notifier, _configuration);
                            Hashtable has_replace = new Hashtable();
                            for (int i = 0; i < users.Count; i++)
                            {
                                NotifyModel notify_model = new NotifyModel();
                                has_replace = new Hashtable();
                                has_replace.Add("nguoigui", "Hệ thống");
                                has_replace.Add("duan", _item["title"]);
                                notify_model.AppCode = "WORK";
                                notify_model.From_IDNV = "";
                                notify_model.To_IDNV = users[i].ToString();
                                notify_model.TitleLanguageKey = LocalizationUtility.GetBackendMessage("ww_thongbaoduantrehan", "", "vi");
                                notify_model.TitleLanguageKey = notify_model.TitleLanguageKey.Replace("$nguoigui$", loginData.LastName);
                                notify_model.TitleLanguageKey = notify_model.TitleLanguageKey.Replace("$duan$", _item["title"].ToString());
                                notify_model.ReplaceData = has_replace;
                                notify_model.To_Link_MobileApp = noti.link_mobileapp.Replace("$id$", _item["id_row"].ToString());
                                notify_model.To_Link_WebApp = noti.link.Replace("$id$", _item["id_row"].ToString());

                                List<AccUsernameModel> DataAccount = WeworkLiteController.GetDanhSachAccountFromCustomerID(_configuration, long.Parse(CustemerID));
                                var info = DataAccount.Where(x => notify_model.To_IDNV.ToString().Contains(x.UserId.ToString())).FirstOrDefault();
                                if (info is not null)
                                {
                                    bool kq_noti = WeworkLiteController.SendNotify(loginData.Username, info.Username, notify_model, _notifier, _configuration);
                                }
                            }
                            #endregion
                        }
                    }
                }
            }
        }
        private void CongViecHetHanTrongNgay(DpsConnection cnn, string CustemerID, string ConnectionString)
        {
            PushNotifyModel notify = new PushNotifyModel();
            Hashtable has = new Hashtable();
            SqlConditions conds = new SqlConditions();
            DateTime today = DateTime.Now;
            DateTime currentTime = today.Date.Add(new TimeSpan(0, 0, 0));
            string select = @"select (SELECT datediff(hour , GETDATE(), deadline)) as thoigianconlai, w.* 
            from v_wework_new w where disabled = 0 
            and deadline is not null and deadline <= Getdate()
            and deadline >= '" + currentTime + "' " +
            "and deadline < '" + currentTime.AddDays(1) + "'";
            DataTable dt = cnn.CreateDataTable(select);
            UserJWT loginData = new UserJWT();
            loginData.CustomerID = int.Parse(CustemerID);
            loginData.LastName = "Hệ thống";
            loginData.UserID = 0;
            foreach (DataRow dr in dt.Rows)
            {
                WeworkLiteController.mailthongbao(long.Parse(dr["id_row"].ToString()), new List<long> { long.Parse(dr["Id_NV"].ToString()) }, 41, loginData, ConnectionString, _notifier, _configuration);//thông báo trễ hạn
                #region Lấy thông tin để thông báo
                SendNotifyModel noti = WeworkLiteController.GetInfoNotify(41, ConnectionString);
                #endregion
                #region Notify
                Hashtable has_replace = new Hashtable();
                NotifyModel notify_model = new NotifyModel();
                has_replace = new Hashtable();
                has_replace.Add("nguoigui", loginData.Username);
                has_replace.Add("tencongviec", dr["id_row"].ToString());
                notify_model.AppCode = "WORK";
                notify_model.From_IDNV = loginData.UserID.ToString();
                notify_model.To_IDNV = dr["id_row"].ToString().ToString();
                notify_model.TitleLanguageKey = LocalizationUtility.GetBackendMessage("ww_thongbaocvtrehan", "", "vi");
                notify_model.TitleLanguageKey = notify_model.TitleLanguageKey.Replace("$nguoigui$", loginData.LastName);
                notify_model.TitleLanguageKey = notify_model.TitleLanguageKey.Replace("$tencongviec$", dr["id_row"].ToString());
                notify_model.ReplaceData = has_replace;
                notify_model.To_Link_MobileApp = noti.link_mobileapp.Replace("$id$", dr["id_row"].ToString());
                notify_model.To_Link_WebApp = noti.link.Replace("$id$", dr["id_row"].ToString());

                List<AccUsernameModel> DataAccount = WeworkLiteController.GetDanhSachAccountFromCustomerID(_configuration, long.Parse(CustemerID));
                var info = DataAccount.Where(x => notify_model.To_IDNV.ToString().Contains(x.UserId.ToString())).FirstOrDefault();
                if (info is not null)
                {
                    bool kq_noti = WeworkLiteController.SendNotify(loginData.Username, info.Username, notify_model, _notifier, _configuration);
                }
                #endregion
            }
        }
        private void CongViecHetHan(DpsConnection cnn, string CustemerID, string ConnectionString)
        {
            PushNotifyModel notify = new PushNotifyModel();
            Hashtable has = new Hashtable();
            SqlConditions conds = new SqlConditions();
            string select = @"select (SELECT DATEDIFF(hour , GETDATE(), deadline)) as thoigianconlai, w.* 
            from v_wework_new w 
            where disabled = 0 and deadline is not null 
            and deadline > GETDATE() and w.end_date is null
            and id_nv is not null";
            DataTable dt = cnn.CreateDataTable(select);
            UserJWT loginData = new UserJWT();
            loginData.CustomerID = int.Parse(CustemerID);
            loginData.LastName = "Hệ thống";
            loginData.UserID = 0;
            foreach (DataRow dr in dt.Rows)
            {
                WeworkLiteController.mailthongbao(long.Parse(dr["id_row"].ToString()), new List<long> { long.Parse(dr["Id_NV"].ToString()) }, 17, loginData, ConnectionString, _notifier, _configuration);//thông báo trễ hạn
                #region Lấy thông tin để thông báo
                SendNotifyModel noti = WeworkLiteController.GetInfoNotify(17, ConnectionString);
                #endregion
                #region Notify thêm mới công việc
                Hashtable has_replace = new Hashtable();
                NotifyModel notify_model = new NotifyModel();
                has_replace = new Hashtable();
                has_replace.Add("nguoigui", loginData.Username);
                has_replace.Add("tencongviec", dr["id_row"].ToString());
                notify_model.AppCode = "WORK";
                notify_model.From_IDNV = loginData.UserID.ToString();
                notify_model.To_IDNV = dr["id_row"].ToString().ToString();
                notify_model.TitleLanguageKey = LocalizationUtility.GetBackendMessage("ww_thongbaocvtrehan", "", "vi");
                notify_model.TitleLanguageKey = notify_model.TitleLanguageKey.Replace("$nguoigui$", loginData.LastName);
                notify_model.TitleLanguageKey = notify_model.TitleLanguageKey.Replace("$tencongviec$", dr["id_row"].ToString());
                notify_model.ReplaceData = has_replace;
                notify_model.To_Link_MobileApp = noti.link_mobileapp.Replace("$id$", dr["id_row"].ToString());
                notify_model.To_Link_WebApp = noti.link.Replace("$id$", dr["id_row"].ToString());

                List<AccUsernameModel> DataAccount = WeworkLiteController.GetDanhSachAccountFromCustomerID(_configuration, long.Parse(CustemerID));
                var info = DataAccount.Where(x => notify_model.To_IDNV.ToString().Contains(x.UserId.ToString())).FirstOrDefault();
                if (info is not null)
                {
                    bool kq_noti = WeworkLiteController.SendNotify(loginData.Username, info.Username, notify_model, _notifier, _configuration);
                }
                #endregion
            }
        }
        private void DuAnHetHan(DpsConnection cnn, string CustemerID, string ConnectionString)
        {
            PushNotifyModel notify = new PushNotifyModel();
            Hashtable has = new Hashtable();
            SqlConditions conds = new SqlConditions();
            string select = @"select * from we_project_team p 
                            where disabled = 0 
                            and end_date is not null 
                            and end_date < GETDATE() 
                            and locked = 0";
            DataTable dt = cnn.CreateDataTable(select);
            if (dt.Rows.Count > 0)
            {
                foreach (DataRow _item in dt.Rows)
                {
                    // lấy danh sách người quản trị có trong dự án
                    string sqltv = @"select * from we_project_team_user 
                                    where admin = 1 and disabled = 0 
                                    and id_project_team = " + _item["id_row"].ToString();
                    DataTable dttv = cnn.CreateDataTable(sqltv);
                    if (dttv.Rows.Count > 0)
                    {
                        // nếu có người mới gửi thông báo
                        foreach (DataRow user in dttv.Rows)
                        {
                            var users = new List<long> { long.Parse(user["id_user"].ToString()) };
                            UserJWT loginData = new UserJWT();
                            loginData.CustomerID = int.Parse(CustemerID);
                            loginData.LastName = "Hệ thống";
                            loginData.UserID = 0;
                            #region Lấy thông tin để thông báo
                            SendNotifyModel noti = WeworkLiteController.GetInfoNotify(39, ConnectionString);
                            #endregion
                            #region Notify nhắc nhở dự án hết hạn
                            WeworkLiteController.mailthongbao(long.Parse(_item["id_row"].ToString()), users, 39, loginData, ConnectionString, _notifier, _configuration);
                            Hashtable has_replace = new Hashtable();
                            for (int i = 0; i < users.Count; i++)
                            {
                                NotifyModel notify_model = new NotifyModel();
                                has_replace = new Hashtable();
                                has_replace.Add("nguoigui", "Hệ thống");
                                has_replace.Add("duan", _item["title"]);
                                notify_model.AppCode = "WORK";
                                notify_model.From_IDNV = "";
                                notify_model.To_IDNV = users[i].ToString();
                                notify_model.TitleLanguageKey = LocalizationUtility.GetBackendMessage("ww_thongbaoduantrehan", "", "vi");
                                notify_model.TitleLanguageKey = notify_model.TitleLanguageKey.Replace("$nguoigui$", loginData.LastName);
                                notify_model.TitleLanguageKey = notify_model.TitleLanguageKey.Replace("$duan$", _item["title"].ToString());
                                notify_model.ReplaceData = has_replace;
                                notify_model.To_Link_MobileApp = noti.link_mobileapp.Replace("$id$", _item["id_row"].ToString());
                                notify_model.To_Link_WebApp = noti.link.Replace("$id$", _item["id_row"].ToString());
                                List<AccUsernameModel> DataAccount = WeworkLiteController.GetDanhSachAccountFromCustomerID(_configuration, long.Parse(CustemerID));
                                var info = DataAccount.Where(x => notify_model.To_IDNV.ToString().Contains(x.UserId.ToString())).FirstOrDefault();
                                if (info is not null)
                                {
                                    bool kq_noti = WeworkLiteController.SendNotify(loginData.Username, info.Username, notify_model, _notifier, _configuration);
                                }
                            }
                            #endregion
                        }
                    }
                }
            }
        }
        private void DuAnSapHetHan(DpsConnection cnn, string CustemerID, string ConnectionString)
        {
            Hashtable has = new Hashtable();
            SqlConditions conds = new SqlConditions();
            string select = @"select (SELECT DATEDIFF(hour , GETDATE(), end_date)) as thoigianconlai, pu.* 
                            from we_project_team p join we_project_team_user pu 
                            on p.id_row = pu.id_project_team
                            where p.disabled = 0 and pu.disabled = 0 and p.locked = 0
                            and end_date is not null 
                            and end_date > (GETDATE()) 
                            and DATEDIFF(day, end_date, GETDATE()) < 2";
            DataTable dt = cnn.CreateDataTable(select);
            if (cnn.LastError != null || dt.Rows.Count == 0)
            {
                return;
            }
            UserJWT loginData = new UserJWT();
            loginData.CustomerID = int.Parse(CustemerID);
            loginData.LastName = "Hệ thống";
            loginData.UserID = 0;
            foreach (DataRow dr in dt.Rows)
            {
                WeworkLiteController.mailthongbao(long.Parse(dr["id_project_team"].ToString()), new List<long> { long.Parse(dr["id_user"].ToString()) }, 20, loginData, ConnectionString, _notifier, _configuration);//thông báo sắp hết hạn dự án
            }
        }
        private void EveryDayReminder(DpsConnection cnn, long CustomerID, string ConnectionString)
        {
            string ham = "EveryDayReminder";
            try
            {
                TimeSpan thoigiannhacnho = new TimeSpan(1, 0, 0);
                SqlConditions cond = new SqlConditions();
                cond.Add("Id_row", 1);
                cond.Add("CustemerID", CustomerID);
                string select = "select giatri from tbl_thamso where (where)";
                DataTable dt = cnn.CreateDataTable(select, "(where)", cond);
                if (dt.Rows.Count > 0)
                {
                    bool IsNhacnho = false;
                    DateTime Gionhacnho = new DateTime();
                    if ("".Equals(dt.Rows[0][0].ToString())) IsNhacnho = true;
                    else
                    {
                        if (DateTime.TryParse(dt.Rows[0][0].ToString(), out Gionhacnho))
                        {
                            if (Gionhacnho <= DateTime.Now)
                                IsNhacnho = true;
                        }
                    }
                    if (IsNhacnho)
                    {
                        DateTime Gionhactieptheo = Gionhacnho.AddDays(1);
                        if (Gionhactieptheo < DateTime.Now)
                            Gionhactieptheo = DateTime.Today.AddDays(1).Add(thoigiannhacnho);
                        //Cập nhật lại giờ nhắc tiếp theo
                        Hashtable val = new Hashtable();
                        val.Add("giatri", Gionhactieptheo.ToString());
                        cnn.Update(val, cond, "tbl_thamso");
                        #region Chạy các hàm nhắc nhở
                        ham = "CongViecHetHanTrongNgay";
                        CongViecHetHanTrongNgay(cnn, CustomerID.ToString(), ConnectionString);
                        ham = "CongViecHetHan";
                        CongViecHetHan(cnn, CustomerID.ToString(), ConnectionString);
                        ham = "DuAnSapHetHan";
                        DuAnSapHetHan(cnn, CustomerID.ToString(), ConnectionString);
                        ham = "DuAnHetHan";
                        DuAnHetHan(cnn, CustomerID.ToString(), ConnectionString);
                        ham = "TaoCongViecTuDong";
                        TaoCongViecTuDong(cnn, CustomerID.ToString(), ConnectionString);
                        ham = "CapNhatCongViecTreHan";
                        CapNhatCongViecTreHan(cnn, CustomerID.ToString(), ConnectionString);
                        ham = "CapNhatDuAnTreHan";
                        CapNhatDuAnTreHan(cnn, CustomerID.ToString(), ConnectionString);
                        #endregion
                    }
                    if (cnn.LastError != null)
                    {
                        string content = " EveryDayReminder. Lỗi Database: " + cnn.LastError.Message;
                        string error_message = "";
                        string CustemerID1 = "0";
                        //Gửi thông báo khi phát sinh lỗi
                        SendMail.SendWithConnection("huypaddaica@gmail.com", "[JeeWork] " + DateTime.Now.ToString("dd/MM/yyyy HH:mm") + " Lỗi chạy tự động. Lỗi Database: ", new MailAddressCollection(), content, CustemerID1, "", false, out error_message, cnn, ConnectionString);
                    }
                }
                else
                {
                    Hashtable val = new Hashtable();
                    val.Add("Id_row", 1);
                    val.Add("giatri", DateTime.Now.AddDays(-1));
                    val.Add("mota", "Thời gian nhắc nhở tiếp theo (Theo ngày)");
                    val.Add("nhom", "other");
                    val.Add("id_nhom", 0);
                    val.Add("CustemerID", CustomerID);
                    val.Add("Allowedit", 0);
                    cnn.Insert(val, "tbl_thamso");
                }
            }
            catch (Exception ex)
            {
                string error = ex.Message;
                string content = " EveryDayReminder: " + ex.Message + ". Customer " + CustomerID + " funcion " + ham;
                string error_message = "";
                string CustemerID1 = "0";
                //Gửi thông báo khi phát sinh lỗi
                SendMail.SendWithConnection("huypaddaica@gmail.com", "[JeeWork] " + DateTime.Now.ToString("dd/MM/yyyy HH:mm") + " Lỗi chạy tự động. Lỗi Database: ", new MailAddressCollection(), content, CustemerID1, "", false, out error_message, cnn, ConnectionString);
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
                        string error_message = "";
                        SendMail.SendWithConnection("huypaddaica@gmail.com", "[JeeWork] " + DateTime.Now.ToString("dd/MM/yyyy HH:mm") + " Lỗi từ API: ", new MailAddressCollection(), errormsg, "", "", false, out error_message, cnn, ConnectionString);
                    }
                }
            }
            catch (Exception ex)
            {

            }
        }
    }
}
