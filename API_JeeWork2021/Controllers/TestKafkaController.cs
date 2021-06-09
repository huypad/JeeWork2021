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
            //emailMessage asyncnotice = new emailMessage()
            //{
            //    access_token = loginData.access_token,
            //    from = "derhades1998@gmail.com",
            //    to = "ngocrong193@gmail.com",
            //    subject = "Mail test",
            //    html = "<h1>Hello World</h1>" //nội dung html
            //};
            //await _notifier.sendEmail(asyncnotice);
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

    }
}
