using JeeWork_Core2021.Classes;
using JeeWork_Core2021.Models;
using API_JeeWork2021.Classes;
using DPSinfra.ConnectionCache;
using DPSinfra.Kafka;
using DPSinfra.Logger;
using DPSinfra.Notifier;
using DpsLibs.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace JeeWork_Core2021.Controllers
{
    [Route("api/test")]
    [ApiController]
    public class TestKafkaController : ControllerBase
    {
        private readonly ILogger<TestKafkaController> _logger;
        private Notification notify;
        private IProducer _producer;
        private IConfiguration _config;
        private INotifier _notifier;
        private IConnectionCache _cache;
        public TestKafkaController(INotifier notifier, IProducer producer, IConfiguration Configuration, IConnectionCache connection, ILogger<TestKafkaController> logger)
        {
            notify = new Notification(notifier);
            _producer = producer;
            _config = Configuration;
            _notifier = notifier;
            _cache = connection;
            _logger = logger;
        }
        [HttpGet]
        [Route("testkafka")]
        public object Testkafka()
        {
            var demo = new
            {
                AppCode = new List<string>() { "OFFICE", "WORK" },
                CustomerID = 1174,
                IsAdmin = true,
                IsInitial = true,
                UserID = 76810,
                Username = "phongtest32.admin"
            };
            _producer.PublishAsync("jeeplatform.initialization", Newtonsoft.Json.JsonConvert.SerializeObject(demo));
            return "OK";
        }
        [HttpGet]
        [Route("testNotify")]
        public string testNotify()
        {
            //notify.notification("congtytest.sanh","test abc xyz");
            //notify.notification("congtytest.admin", "test abc xyz");
            return "Oke";
        }
        [HttpGet]
        [Route("testNotifyMail")]
        public async void testNotifyMail()
        {
            //UserJWT loginData = Ulities.GetUserByHeader(HttpContext.Request.Headers);
            //notify.notificationemail(loginData.access_token);
            //return "Oke";
            //UserJWT loginData = Ulities.GetUserByHeader(HttpContext.Request.Headers);
            emailMessage asyncnotice = new emailMessage()
            {
                CustomerID = 1119,
                from = "derhades1998@gmail.com",
                to = "thanhthang1798@gmail.com",
                subject = "Mail test",
                html = "<h1>Hello World</h1>" //nội dung html
            };
            await _notifier.sendEmail(asyncnotice);
        }
        [HttpGet]
        [Route("MaHoa")]
        public object MaHoa(string userID, string pass, string key)
        {
        var mahoa = new
        {
                userID = DpsLibs.Common.EncDec.Encrypt(userID, key),
                passWord = DpsLibs.Common.EncDec.Encrypt(pass, key)
        };
            return mahoa;
        }
        [HttpGet]
        [Route("testLog")]
        public object testLog()
        {
            var d2 = new ActivityLog()
            {
                username = "usertest",
                category = "Lỗi",
                action = "Duyệt",
                data = "null"
            };
            _logger.LogDebug(JsonConvert.SerializeObject(d2));
            return d2;
        }
        [HttpGet]
        [Route("testAuto")]
        public string testAuto()
        {
            string topic = _config.GetValue<string>("KafkaConfig:TopicProduce:JeeWorkAutomationService");
            _producer.PublishAsync(topic, "{\"CustomerID\":31,\"AppCode\":[\"HR\",\"ADMIN\",\"Land\",\"REQ\",\"WF\",\"jee-doc\",\"OFFICE\",\"WW\",\"WMS\",\"TEST\",\"AMS\",\"ACC\"],\"UserID\":76745,\"Username\":\"powerplus.admin\"}");
            return "Oke";
        }
        [HttpGet("XinChao")]
        public object TestCode()
        {
            try
            {
                DataTable dt = new DataTable();
                SqlConditions Conds = new SqlConditions();
                string sql = @"select * from AccountList";
                using (DpsConnection cnn = new DpsConnection("Data Source=.\\SQLEXPRESS;Initial Catalog=JeeCustomers;User ID=jeerequest;Password=Je3reQu3ts$Dew"))
                {
                    dt = cnn.CreateDataTable(sql);
                    var result = dt.AsEnumerable();
                    return result;
                }
            }
            catch (Exception ex)
            {
                return ex;
            }
        }
        [HttpGet("testgetvalue")]
        public async Task<IActionResult> testgetvalue()
        {
            try
            {
                var event1 = new AutomaticModel
                {
                    EventID = 1,
                    TaskID = 1,
                    ListID = 1,
                    data = new Data
                    {
                        X = 10,
                        Y = 12,
                    }
                };

                // chỗ này anh parse lại json raw để bắn, em tạm thời đóng code này
                // await _producer.PublishAsync("topioc ở đây", Newtonsoft.Json.JsonConvert.SerializeObject(event1));

                // hàm dưới này là test chỗ getvalue thôi, em parse lại raw json xong em parse lại object để làm tiếp
                var getvalueEvent1 = Newtonsoft.Json.JsonConvert.SerializeObject(event1);
                var objEvent1 = Newtonsoft.Json.JsonConvert.DeserializeObject<AutomaticModel>(getvalueEvent1);

                if (objEvent1.EventID == 1)
                {
                    // cách lấy X, Y trong data
                    long statusid_Old = Int32.Parse(objEvent1.data.X.ToString());
                    long statusid_New = Int32.Parse(objEvent1.data.Y.ToString());
                }

                var event2 = new AutomaticModel
                {
                    EventID = 2,
                    TaskID = 1,
                    ListID = 1,
                    data = new Data
                    {
                        X = 3,
                        Y = 4,
                    }
                };

                var getvalueEvent2 = Newtonsoft.Json.JsonConvert.SerializeObject(event2);
                var objEvent2 = Newtonsoft.Json.JsonConvert.DeserializeObject<AutomaticModel>(getvalueEvent2);

                if (objEvent2.EventID == 2)
                {
                    // cách lấy X, Y trong data
                    long priorityID_old = Int32.Parse(objEvent1.data.X.ToString());
                    long priorityID_new = Int32.Parse(objEvent1.data.Y.ToString());
                }

                var event3 = new AutomaticModel
                {
                    EventID = 3,
                    TaskID = 1,
                    ListID = 1
                };

                var getvalueEvent3 = Newtonsoft.Json.JsonConvert.SerializeObject(event3);
                var objEvent3 = Newtonsoft.Json.JsonConvert.DeserializeObject<AutomaticModel>(getvalueEvent3);

                if (objEvent3.EventID == 3)
                {
                    // chỗ này do eventid = 3 nên em không tạo data phía trên nhưng anh tò mò nó có giá trị gì thì anh debug xem object chỗ này nha
                    var data = objEvent3.data;
                }

                // mấy cái khác tương tự nha a
                return Ok();
            }
            catch (Exception ex)
            {
                return null;
                //return BadRequest(MessageReturnHelper.Exception(ex));
            }
        }

        [HttpGet]
        [Route("test-utc-time")]
        public string TestUTCTime()
        {
            string _k = "09/10/2021 15:05:29";
            string _p = "2021-09-10T15:05:29.000";
            DateTime dt1 = DateTime.Parse(_k);
            DateTime dt2 = DateTime.Parse(_p);
            DateTime dt3 = DateTime.Parse(_k).ToUniversalTime();
            var _header = Request.Headers;
            string _time = _header["CurrentTime"].ToString();
            int _timeZone = int.Parse(_header["TimeZone"].ToString());
            _timeZone /= -60;
            DateTime dt = DateTime.Parse(_time);
            int _currentTZ = TimeZoneInfo.Local.GetUtcOffset(DateTime.UtcNow).Hours;
            int _chl = Math.Abs(_currentTZ - _timeZone);
            var _currentLocalTime = dt.AddHours(_chl);
            return _currentLocalTime.ToString();
        }

        public class AutomaticModel
        {
            public long EventID { get; set; }
            public long TaskID { get; set; }
            public long ListID { get; set; }
            public Data data { get; set; }
        }

        public class Post_Automation_Model
        {
            public long eventid { get; set; }
            public int taskid { get; set; }
            public long userid { get; set; }
            public long listid { get; set; }
            public Data data { get; set; }
        }
        public class Data
        {
            public object X { get; set; }
            public object Y { get; set; }
        }
    }
}
