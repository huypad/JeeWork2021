using APIModel.DTO;
using DPSinfra.ConnectionCache;
using DPSinfra.Kafka;
using DPSinfra.Notifier;
using DpsLibs.Data;
using JeeWork_Core2021.Classes;
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

namespace API_JeeWork2021.Classes
{
    public class NhacNho
    {
        private readonly IHostingEnvironment _hostingEnvironment;
        private IConfiguration _configuration;
        private IConnectionCache ConnectionCache;
        private IProducer _producer;
        private INotifier _notifier;
        public NhacNho(IConnectionCache _cache, IConfiguration configuration, INotifier notifier, IProducer producer)
        {
            //10p chạy 1 lần 600000
            Timer10Minute = new System.Timers.Timer(600000);
            Timer10Minute.Elapsed += new System.Timers.ElapsedEventHandler(Timer10Minute_Elapsed);
            _configuration = configuration;
            ConnectionCache = _cache;
            _producer = producer;
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
        System.Timers.Timer Timer10Minute;

        public void Start()
        {
            Timer10Minute.Start();
        }
        public void Stop()
        {
            Timer10Minute.Start();
        }
        protected void Timer10Minute_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            string ham = "Timer10Minute_Elapsed"; string idkh = "0";
            string ConnectionString = "";
            try
            {
                List<long> DanhSachCustomer = WeworkLiteController.GetDanhSachCustomerID(_configuration);
                foreach (var item in DanhSachCustomer) // có danh sách Customer foreach lấy danh sách tài khoản
                {
                    try
                    {
                        ConnectionString = WeworkLiteController.getConnectionString(ConnectionCache, item, _configuration);
                        ham = "DataAccount"; idkh = item.ToString();
                        List<AccUsernameModel> DataAccount = WeworkLiteController.GetDanhSachAccountFromCustomerID(_configuration, item);
                        if (DataAccount != null && !string.IsNullOrEmpty(ConnectionString))
                        {
                            foreach (var account in DataAccount)
                            {
                                ham = "SLCongviecUser"; idkh = item.ToString();
                                SLCongviecUser(account.UserId, account.CustomerID, ConnectionString, _configuration, _producer);
                                ham = "SLCongviecQuaHan"; idkh = item.ToString();
                                SLCongviecQuaHan(account.UserId, account.CustomerID, ConnectionString, _configuration, _producer);
                                ham = "SLCongViecHetHanTrongNgay"; idkh = item.ToString();
                                SLCongViecHetHanTrongNgay(account.UserId, account.CustomerID, ConnectionString, _configuration, _producer);
                                ham = "SLDuAnQuaHan"; idkh = item.ToString();
                                SLDuAnQuaHan(account.UserId, account.CustomerID, ConnectionString, _configuration, _producer);
                            }
                        }
                        using (DpsConnection cnn = new DpsConnection(ConnectionString))
                        {
                            if (cnn.LastError != null)
                            {
                                string content = " TimerSendReminder_Elapsed. Lỗi Database: " + cnn.LastError.Message;
                                string error_message = "";
                                string CustemerID1 = "0";
                                //Gửi thông báo khi phát sinh lỗi
                                SendMail.SendWithConnection("huypaddaica@gmail.com", "[JeeWork] " + DateTime.Now.ToString("dd/MM/yyyy HH:mm") + " Lỗi chạy tự động. Lỗi Database: ", new MailAddressCollection(), content, CustemerID1, "", false, out error_message, cnn, ConnectionString);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        using (DpsConnection cnn = new DpsConnection(ConnectionString))
                        {
                            if (cnn.LastError != null)
                            {
                                string content = " TimerSendReminder_Elapsed. Lỗi Database: " + ex.Message;
                                string error_message = "";
                                string CustemerID1 = "0";
                                //Gửi thông báo khi phát sinh lỗi
                                SendMail.SendWithConnection("huypaddaica@gmail.com", "[JeeWork] " + DateTime.Now.ToString("dd/MM/yyyy HH:mm") + " Lỗi chạy tự động. Lỗi Database: ", new MailAddressCollection(), content, CustemerID1, "", false, out error_message, cnn, ConnectionString);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                string error = ex.Message;
                string content = " TimerSendReminder_Elapsed: " + ex.Message + ". Customer " + idkh + " funcion " + ham;
                string error_message = "";
                string CustemerID1 = "0";
                using (DpsConnection cnn = new DpsConnection(ConnectionString))
                {
                    //Gửi thông báo khi phát sinh lỗi
                    SendMail.SendWithConnection("huypaddaica@gmail.com", "[JeeWork] " + DateTime.Now.ToString("dd/MM/yyyy HH:mm") + " Lỗi chạy tự động. Lỗi Database: ", new MailAddressCollection(), content, CustemerID1, "", false, out error_message, cnn, ConnectionString);
                }
            }

        }
       
        /// <summary>
        /// Update số lượng công việc  +1 hoặc -1 với những tài kkhoan quy định mà không nhắc nhở theo định kì 
        /// </summary>
        public static void UpdateSoluongCV(long UserID, long CustomerID, string value, IConfiguration _configuration, IProducer _producer)
        {
            UpdateSoluongCongviecUser(UserID, CustomerID, value, _configuration, _producer);
        }
        public static void UpdateSoluongCVHetHan(long UserID, long CustomerID, string value, IConfiguration _configuration, IProducer _producer)
        {
            UpdateSoluongCongviecHetHanUser(UserID, CustomerID, value, _configuration, _producer);
        }
        public static void UpdateCVHoanthanh(long UserID, long CustomerID, bool hasDeadline, IConfiguration _configuration, IProducer _producer)
        {
            UpdateSoluongCongviecUser(UserID, CustomerID, "-", _configuration, _producer);
            if (hasDeadline)
            {
                UpdateSoluongCongviecHetHanUser(UserID, CustomerID, "-", _configuration, _producer);
            }
        }
        public static void UpdateSoluongDuan(long UserID, long CustomerID, string ConnectionString, IConfiguration _configuration, IProducer _producer)
        {
            SLDuAnQuaHan(UserID, CustomerID, ConnectionString, _configuration, _producer);
        }
        /// <summary>
        /// Cập nhập số lượng công việc đang làm cần nhắc nhở
        /// </summary>
        /// <param name="UserID"></param>
        /// <param name="CustomerID"></param>
        /// <param name="ConnectionString"></param>
        /// <param name="_configuration"></param>
        /// <param name="_producer"></param>
        public static void UpdateSoluongCongviecUser(long UserID, long CustomerID, string value, IConfiguration _configuration, IProducer _producer)
        {
            var demo = new Remider()
            {
                PhanLoaiID = 503,
                SoLuong = 1,
                UserID = UserID,
                CustomerID = CustomerID,
                DataField = "Sophu1",
                FieldChange = value,
            };
            SendTestReminder(_configuration, _producer, demo);
        }

        public static void UpdateSoluongCongviecHetHanUser(long UserID, long CustomerID, string value, IConfiguration _configuration, IProducer _producer)
        {
            var demo = new Remider()
            {
                PhanLoaiID = 802,
                SoLuong = 1,
                UserID = UserID,
                CustomerID = CustomerID,
                DataField = "Sophu1",
                FieldChange = value,
            };
            SendTestReminder(_configuration, _producer, demo);
        }

        public static long SLCongviecUser(long UserID, long CustomerID, string ConnectionString, IConfiguration _configuration, IProducer _producer)
        {
            using (DpsConnection cnn = new DpsConnection(ConnectionString))
            {
                SqlConditions cond = new SqlConditions();
                cond.Add("UserID", UserID);
                string sqlq = @$"select w.* from v_wework_new w 
                                where disabled = 0 and (w.createdBy = @UserID or w.id_nv = @UserID) 
                                and status not in (select id_row from we_status where isfinal = 1)";
                DataTable dt = new DataTable();
                dt = cnn.CreateDataTable(sqlq, cond);
                if (cnn.LastError != null || dt.Rows.Count < 0)
                    return 0;
                var demo = new Remider()
                {
                    PhanLoaiID = 503,
                    SoLuong = dt.Rows.Count,
                    UserID = UserID,
                    CustomerID = CustomerID,
                    DataField = "Sophu1",
                };
                SendTestReminder(_configuration, _producer, demo);
                return dt.Rows.Count;
            }
        }
        public static long SLCongViecHetHanTrongNgay(long UserID, long CustomerID, string ConnectionString, IConfiguration _configuration, IProducer _producer)
        {
            using (DpsConnection cnn = new DpsConnection(ConnectionString))
            {
                DateTime today = DateTime.Now;
                DateTime currentTime = today.Date.Add(new TimeSpan(0, 0, 0));
                SqlConditions cond = new SqlConditions();
                cond.Add("UserID", UserID);
                string sqlq = @$"select w.* from v_wework_new w 
                                where disabled = 0 
                                and (w.CreatedBy = @UserID or w.Id_NV = @UserID) 
                                and deadline is not null
                                and status not in (select id_row from we_status where isfinal = 1 and disabled = 0)";
                sqlq += " and deadline >= '" + currentTime + "' and deadline < '" + currentTime.AddDays(1) + "'";
                DataTable dt = new DataTable();
                dt = cnn.CreateDataTable(sqlq, cond);
                if (cnn.LastError != null || dt.Rows.Count < 0)
                    return 0;
                var demo = new Remider()
                {
                    PhanLoaiID = 803,
                    SoLuong = dt.Rows.Count,
                    UserID = UserID,
                    CustomerID = CustomerID,
                    DataField = "Sophu1",
                };
                SendTestReminder(_configuration, _producer, demo);
                return dt.Rows.Count;
            }
        }
        public static long SLCongviecQuaHan(long UserID, long CustomerID, string ConnectionString, IConfiguration _configuration, IProducer _producer)
        {
            using (DpsConnection cnn = new DpsConnection(ConnectionString))
            {
                SqlConditions cond = new SqlConditions();
                cond.Add("UserID", UserID);
                string sqlq = @$"select w.* 
                            from v_wework_new w 
                            where disabled = 0 
                            and (w.CreatedBy = @UserID or w.Id_NV = @UserID) 
                            and deadline < dateadd (minute, +10, getdate())
                            and deadline > dateadd (minute, -10, getdate())
                            and status not in (select id_row from we_status where isfinal = 1 and Disabled = 0)";
                DataTable dt = new DataTable();
                dt = cnn.CreateDataTable(sqlq, cond);
                if (cnn.LastError != null || dt.Rows.Count < 0)
                    return 0;
                var demo = new Remider()
                {
                    PhanLoaiID = 802,
                    SoLuong = dt.Rows.Count,
                    UserID = UserID,
                    CustomerID = CustomerID,
                    DataField = "Sophu1",
                };
                SendTestReminder(_configuration, _producer, demo);
                return dt.Rows.Count;
            }
        }

        public static long SLDuAnQuaHan(long UserID, long IdKH, string ConnectionString, IConfiguration _configuration, IProducer _producer)
        {
            using (DpsConnection cnn = new DpsConnection(ConnectionString))
            {
                SqlConditions cond = new SqlConditions();
                cond.Add("UserID", UserID);
                cond.Add("IdKH", IdKH);
                string sqlq = @$"select p.*
                                from we_project_team p
                                join we_department d on d.id_row = p.id_department
                                join we_project_team_user u on u.id_project_team = p.id_row
                                where u.disabled = 0 
                                and id_user = @UserID 
                                and end_date < GETDATE()
                                and p.disabled = 0 
                                and d.disabled = 0 
                                and IdKH=@IdKH";
                DataTable dt = new DataTable();
                dt = cnn.CreateDataTable(sqlq, cond);
                if (cnn.LastError != null || dt.Rows.Count < 0)
                    return 0;
                var demo = new Remider()
                {
                    PhanLoaiID = 804,
                    SoLuong = dt.Rows.Count,
                    UserID = UserID,
                    CustomerID = IdKH,
                    DataField = "Sophu1",
                };
                SendTestReminder(_configuration, _producer, demo);
                return dt.Rows.Count;
            }
        }
        public static void SendTestReminder(IConfiguration _configuration, IProducer _producer, Remider remider)
        {
            string TopicCus = _configuration.GetValue<string>("KafkaConfig:TopicProduce:JeeFlowUpdateReminder");
            string obj = Newtonsoft.Json.JsonConvert.SerializeObject(remider);
            _producer.PublishAsync(TopicCus, obj);
        }
        public class Remider
        {
            public long PhanLoaiID { get; set; }
            public int SoLuong { get; set; }
            public long UserID { get; set; }
            public long CustomerID { get; set; }
            public string DataField { get; set; }
            public string FieldChange { get; set; } = "";
        }
    }
}
