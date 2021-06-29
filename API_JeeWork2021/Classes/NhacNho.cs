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
        private JeeWorkConfig _config;
        private readonly IHostingEnvironment _hostingEnvironment;
        private IConfiguration _configuration;
        private IConnectionCache ConnectionCache;
        private IProducer _producer;
        private INotifier _notifier;
        public NhacNho(IOptions<JeeWorkConfig> config, IHostingEnvironment hostingEnvironment, IConnectionCache _cache, IConfiguration configuration, INotifier notifier, IProducer producer)
        {
            //
            // TODO: Add constructor logic here
            //
            //1p chạy 1 lần (chỉ áp dụng những chức năng cần chạy sớm và phải chạy nhanh)
            Timer1Minute = new System.Timers.Timer(60000);
            Timer1Minute.Elapsed += new System.Timers.ElapsedEventHandler(Timer1Minute_Elapsed);
            ////5p chạy 1 lần
            //Timer5Minute = new System.Timers.Timer(300000);
            //Timer5Minute.Elapsed += new System.Timers.ElapsedEventHandler(Timer5Minute_Elapsed);
            ////10p chạy 1 lần
            //TimerSendNotify = new System.Timers.Timer(600000);
            //TimerSendNotify.Elapsed += new System.Timers.ElapsedEventHandler(TimerSendReminder_Elapsed);
            ////60p chạy 1 lần
            //TimerAutoUpdate = new System.Timers.Timer(3600000);
            //TimerAutoUpdate.Elapsed += new System.Timers.ElapsedEventHandler(TimerAutoUpdate_Elapsed);
            _config = config.Value;
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
        //System.Timers.Timer TimerAutoUpdate;
        //System.Timers.Timer TimerSendNotify;
        System.Timers.Timer Timer1Minute;
        //System.Timers.Timer Timer5Minute;

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
        protected void TimerSendReminder_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            try
            {
                List<long> DanhSachCustomer = WeworkLiteController.GetDanhSachCustomerID(_configuration);
                foreach(var item in DanhSachCustomer) // có danh sách Customer foreach lấy danh sách tài khoản
                {
                    string ConnectionString = WeworkLiteController.getConnectionString(ConnectionCache, item, _configuration);
                    List<AccUsernameModel> DataAccount = WeworkLiteController.GetDanhSachAccountFromCustomerID(_configuration,item);
                    // Chạy tự động từ danh sach Account
                    /*
                     var data = from r in DataAccount
                               select new
                               {
                                   UserID = r.UserId,
                                   Fullname = r.FullName,
                                   congviecphutrach = NhacNho.GetSoluongCongviecUser(r.UserId, r.CustomerID, ConnectionString, _configuration, _producer),
                                   congviecquahan = NhacNho.GetSoluongCongviecQuaHan(r.UserId, r.CustomerID, ConnectionString, _configuration, _producer),
                                   congviechethantrongngay = NhacNho.GetSoluongCongviecHethanTrongngay(r.UserId, r.CustomerID, ConnectionString, _configuration, _producer),
                                   duanquahan = NhacNho.GetSoluongDuAnQuaHan(r.UserId,r.CustomerID, ConnectionString, _configuration, _producer),
                               };
                     */

                    foreach(var account in DataAccount)
                    {
                        GetSoluongCongviecUser(account.UserId, account.CustomerID, ConnectionString, _configuration, _producer);
                        GetSoluongCongviecQuaHan(account.UserId, account.CustomerID, ConnectionString, _configuration, _producer);
                        GetSoluongCongviecHethanTrongngay(account.UserId, account.CustomerID, ConnectionString, _configuration, _producer);
                        GetSoluongDuAnQuaHan(account.UserId, account.CustomerID, ConnectionString, _configuration, _producer);
                    }

                }
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

        private void EveryDayForceRun(DpsConnection cnn, string CustemerID)
        {

        }
        // Update tình trạng và gửi email của những công việc trễ hạn
        private void EveryDay_UpdateLate(DpsConnection cnn, string CustemerID)
        {
                   
        }

        private void ThongBaoSapHetHan(DpsConnection cnn, string CustemerID)
        {

        }

        private void ThongBaoHetHan(DpsConnection cnn, string CustemerID)
        {
            

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
        public static void UpdateCVHoanthanh(long UserID, long CustomerID,bool hasDeadline, IConfiguration _configuration, IProducer _producer)
        {
            UpdateSoluongCongviecUser(UserID, CustomerID, "-", _configuration, _producer);
            if (hasDeadline)
            {
                UpdateSoluongCongviecHetHanUser(UserID, CustomerID, "-", _configuration, _producer);
            }
        }
        
        public static void UpdateSoluongDuan(long UserID, long CustomerID, string ConnectionString, IConfiguration _configuration, IProducer _producer)
        {
            GetSoluongDuAnQuaHan(UserID, CustomerID, ConnectionString, _configuration, _producer);
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
        
        public static long GetSoluongCongviecUser(long UserID, long CustomerID, string ConnectionString, IConfiguration _configuration, IProducer _producer)
        {
            using (DpsConnection cnn = new DpsConnection(ConnectionString))
            {
                SqlConditions cond = new SqlConditions();
                cond.Add("UserID", UserID);
                string sqlq = @$"select count(distinct id_row) from v_wework_new w 
    where Disabled = 0 and ( w.CreatedBy = @UserID or w.Id_NV = @UserID) and status not in (select id_row from we_status where IsFinal = 1 and Disabled = 0)";
                long Tongcv = long.Parse(cnn.ExecuteScalar(sqlq, cond).ToString());
                if (cnn.LastError is not null)
                    return 0;

                var demo = new Remider()
                {
                    PhanLoaiID = 503,
                    SoLuong = int.Parse(Tongcv.ToString()),
                    UserID = UserID,
                    CustomerID = CustomerID,
                    DataField = "Sophu1",
                };
                SendTestReminder(_configuration, _producer, demo);
                return Tongcv;
            }
        }
        public static long GetSoluongCongviecHethanTrongngay(long UserID, long CustomerID, string ConnectionString, IConfiguration _configuration, IProducer _producer)
        {
            //SELECT CAST(CAST(GETDATE() AS DATE) AS DATETIME) as homnay
            //SELECT CAST(CAST(GETDATE()+1 AS DATE) AS DATETIME) as ngaymai
            using (DpsConnection cnn = new DpsConnection(ConnectionString))
            {
                SqlConditions cond = new SqlConditions();
                cond.Add("UserID", UserID);
                string sqlq = @$"select count(distinct id_row) from v_wework_new w 
    where Disabled = 0 and ( w.CreatedBy = @UserID or w.Id_NV = @UserID) and status not in (select id_row from we_status where IsFinal = 1 and Disabled = 0)";
                sqlq += "and (deadline BETWEEN (SELECT CAST(CAST(GETDATE() AS DATE) AS DATETIME)) AND (SELECT CAST(CAST(GETDATE()+ 1 AS DATE) AS DATETIME)) )";
                long Tongcv = long.Parse(cnn.ExecuteScalar(sqlq, cond).ToString());
                if (cnn.LastError is not null)
                    return 0;

                var demo = new Remider()
                {
                    PhanLoaiID = 803,
                    SoLuong = int.Parse(Tongcv.ToString()),
                    UserID = UserID,
                    CustomerID = CustomerID,
                    DataField = "Sophu1",
                };
                SendTestReminder(_configuration, _producer, demo);
                return Tongcv;
            }
        }
        public static long GetSoluongCongviecQuaHan(long UserID, long CustomerID, string ConnectionString, IConfiguration _configuration, IProducer _producer)
        {
            //SELECT CAST(CAST(GETDATE() AS DATE) AS DATETIME) as homnay
            //SELECT CAST(CAST(GETDATE()+1 AS DATE) AS DATETIME) as ngaymai
            using (DpsConnection cnn = new DpsConnection(ConnectionString))
            {
                SqlConditions cond = new SqlConditions();
                cond.Add("UserID", UserID);
                string sqlq = @$"select count(distinct id_row) from v_wework_new w 
    where Disabled = 0 and ( w.CreatedBy = @UserID or w.Id_NV = @UserID) and status not in (select id_row from we_status where IsFinal = 1 and Disabled = 0)";
                sqlq += "and (deadline < GETDATE() or status in (select id_row from we_status where IsDeadline = 1 and Disabled = 0) )";
                long Tongcv = long.Parse(cnn.ExecuteScalar(sqlq, cond).ToString());
                if (cnn.LastError is not null)
                    return 0;

                var demo = new Remider()
                {
                    PhanLoaiID = 802,
                    SoLuong = int.Parse(Tongcv.ToString()),
                    UserID = UserID,
                    CustomerID = CustomerID,
                    DataField = "Sophu1",
                };
                SendTestReminder(_configuration, _producer, demo);
                return Tongcv;
            }
        }
        
        public static long GetSoluongDuAnQuaHan(long UserID,long IdKH, string ConnectionString, IConfiguration _configuration, IProducer _producer)
        {
            //SELECT CAST(CAST(GETDATE() AS DATE) AS DATETIME) as homnay
            //SELECT CAST(CAST(GETDATE()+1 AS DATE) AS DATETIME) as ngaymai
            using (DpsConnection cnn = new DpsConnection(ConnectionString))
            {
                SqlConditions cond = new SqlConditions();
                cond.Add("UserID", UserID);
                cond.Add("IdKH", IdKH);
                string sqlq = @$"select count(distinct p.id_row)
from we_project_team p
join we_department d on d.id_row = p.id_department
join we_project_team_user u on u.id_project_team = p.id_row
where u.Disabled = 0 and id_user = @UserID  and end_date < GETDATE()
and p.Disabled = 0  and d.Disabled = 0 and IdKH=@IdKH ";
                long Tongda = long.Parse(cnn.ExecuteScalar(sqlq, cond).ToString());
                if (cnn.LastError is not null)
                    return 0;

                var demo = new Remider()
                {
                    PhanLoaiID = 804,
                    SoLuong = int.Parse(Tongda.ToString()),
                    UserID = UserID,
                    CustomerID = IdKH,
                    DataField = "Sophu1",
                };
                SendTestReminder(_configuration, _producer, demo);
                return Tongda;
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
            public long PhanLoaiID{ get; set; }
            public int SoLuong { get; set; }
            public long UserID { get; set; }
            public long CustomerID { get; set; }
            public string DataField{ get; set; }
            public string FieldChange { get; set; } = "";
        }


    }
}
