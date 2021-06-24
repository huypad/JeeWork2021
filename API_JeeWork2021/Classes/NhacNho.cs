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
        private string ConnectionString;
        private INotifier _notifier;
        public NhacNho(IOptions<JeeWorkConfig> config, IHostingEnvironment hostingEnvironment, IConnectionCache _cache, IConfiguration configuration, INotifier notifier)
        {
            //
            // TODO: Add constructor logic here
            //
            //1p chạy 1 lần (chỉ áp dụng những chức năng cần chạy sớm và phải chạy nhanh)
            Timer1Minute = new System.Timers.Timer(60000);
            Timer1Minute.Elapsed += new System.Timers.ElapsedEventHandler(Timer1Minute_Elapsed);
            //5p chạy 1 lần
            Timer5Minute = new System.Timers.Timer(300000);
            Timer5Minute.Elapsed += new System.Timers.ElapsedEventHandler(Timer5Minute_Elapsed);
            //10p chạy 1 lần
            TimerSendNotify = new System.Timers.Timer(600000);
            TimerSendNotify.Elapsed += new System.Timers.ElapsedEventHandler(TimerSendNotify_Elapsed);
            //60p chạy 1 lần
            TimerAutoUpdate = new System.Timers.Timer(3600000);
            TimerAutoUpdate.Elapsed += new System.Timers.ElapsedEventHandler(TimerAutoUpdate_Elapsed);
            _config = config.Value;
            _configuration = configuration;
            ConnectionCache = _cache;
            ConnectionString = WeworkLiteController.getConnectionString(ConnectionCache, 1119, _configuration); // #update customerID
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
                //using (DpsConnection cnn = new DpsConnection(_config.HRConnectionString))
                //{
                //    DataTable tmp = cnn.CreateDataTable("select ngay,custemerid from tbl_ngaythongbao_custemer where id_row=4");
                //    DataTable dt = cnn.CreateDataTable("select rowid from tbl_custemers");
                //    for (int i = 0; i < dt.Rows.Count; i++)
                //    {
                //        string CustemerID = dt.Rows[i]["rowid"].ToString();
                //        //MailInfo MInfo = new MailInfo(CustemerID, cnn);
                //        //generate task from repeated
                //        using (DpsConnection cnnWW = new DpsConnection(ConnectionString))
                //        {
                //            //WeworkLiteController.Insert_Template(cnnWW, CustemerID);
                //            //EveryDayForceRun(cnnWW, CustemerID);
                //            //EveryDay_UpdateLate(cnnWW, CustemerID);
                //            //ThongBaoSapHetHan(cnnWW, CustemerID);
                //            //ThongBaoHetHan(cnnWW, CustemerID);
                //        }
                //    }
                //}
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
        public static void SendErrorReport(string custemerid, string errormsg, JeeWorkConfig config, string ConnectionString)
        {
            try
            {
               
            }
            catch (Exception ex)
            {

            }
        }

        public static void UpdateSoluongCV(long UserID, long CustomerID, string ConnectionString, IConfiguration _configuration, IProducer _producer)
        {
            GetSoluongCongviecUser(UserID, CustomerID, ConnectionString, _configuration, _producer);
            GetSoluongCongviecQuaHan(UserID, CustomerID, ConnectionString, _configuration, _producer);
        }
        
        public static void UpdateSoluongDuan(long UserID, long CustomerID, string ConnectionString, IConfiguration _configuration, IProducer _producer)
        {
            GetSoluongDuAnQuaHan(UserID, CustomerID, ConnectionString, _configuration, _producer);
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
                var field = new List<DataField>()
                        {
                            new DataField() { ID = "Sophu1", Value = int.Parse(Tongcv.ToString())},
                        };

                var demo = new Remider()
                {
                    PhanLoaiID = 503,
                    SoLuong = int.Parse(Tongcv.ToString()),
                    UserID = UserID,
                    CustomerID = CustomerID,
                    DataField = field,
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
                var field = new List<DataField>()
                        {
                            new DataField() { ID = "Sophu1", Value = int.Parse(Tongcv.ToString())},
                        };

                var demo = new Remider()
                {
                    PhanLoaiID = 803,
                    SoLuong = int.Parse(Tongcv.ToString()),
                    UserID = UserID,
                    CustomerID = CustomerID,
                    DataField = field,
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
                var field = new List<DataField>()
                        {
                            new DataField() { ID = "Sophu1", Value = int.Parse(Tongcv.ToString())},
                        };

                var demo = new Remider()
                {
                    PhanLoaiID = 802,
                    SoLuong = int.Parse(Tongcv.ToString()),
                    UserID = UserID,
                    CustomerID = CustomerID,
                    DataField = field,
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
                var field = new List<DataField>()
                        {
                            new DataField() { ID = "Sophu1", Value = int.Parse(Tongda.ToString())},
                        };

                var demo = new Remider()
                {
                    PhanLoaiID = 804,
                    SoLuong = int.Parse(Tongda.ToString()),
                    UserID = UserID,
                    CustomerID = IdKH,
                    DataField = field,
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
            public List<DataField> DataField{ get; set; }
        }
        public class DataField
        {
            public object ID{ get; set; }
            public int Value { get; set; }
        }


    }
}
