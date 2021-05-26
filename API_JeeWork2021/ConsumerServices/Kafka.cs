using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DPSinfra.Kafka;
using Newtonsoft.Json;
using DPSinfra.Utils;
using JeeWork_Core2021.Models;
using DPSinfra.ConnectionCache;

namespace JeeWork_Core2021.ConsumerServices
{
    public class Kafka : IHostedService
    {
        private IConfiguration _config;
        private Consumer Init_jeeWorkConsumer, Update_jeeWorkConsumer;
        private IProducer _producer;
        private string mess;
        private IConnectionCache _cache;
        public Kafka(IConfiguration config, IProducer producer, IConnectionCache connectionCache)
        {
            //_producer = producer;
            //_config = config;
            //var group = _config.GetValue<string>("KafkaConfig:ProjectName") + _config.GetValue<string>("KafkaConsumer:ConsumerAddNewCustomer");
            //jeeRequestkafkaConsumer = new Consumer(_config, group);
            _config = config;
            _producer = producer;
            var groupid1 = _config.GetValue<string>("AsyncService:groupInit");
            Init_jeeWorkConsumer = new Consumer(_config, groupid1);
            var groupid2 = _config.GetValue<string>("AsyncService:groupUpdate");
            Update_jeeWorkConsumer = new Consumer(_config, groupid2);
            _cache = connectionCache;
        }
        public Task StartAsync(CancellationToken cancellationToken)
        {
            //var topicProduceByAccount = _config.GetValue<string>("AsyncService:topicProduceByAccount");
            //_ = Task.Run(() =>
            //{
            //    jeeRequestkafkaConsumer.SubscribeAsync(topicProduceByAccount, getMsg);
            //}, cancellationToken);
            //return Task.CompletedTask;
            var topicProduceByAccount1 = _config.GetValue<string>("AsyncService:topicProduceByAccount");
            var topicProduceByAccount2 = _config.GetValue<string>("AsyncService:topicUpdateAdmin");
            _ = Task.Run(() =>
            {
                //ko có thì khi topic có mess, consumer sẽ ko thể lấy được mess
                //c1:
                //initJeeAdminConsumer.SubscribeAsync(topicProduceByAccount, delegate (string s) { getMess(s); });
                //c2:
                Init_jeeWorkConsumer.SubscribeAsync(topicProduceByAccount1, getMessInit);
                Update_jeeWorkConsumer.SubscribeAsync(topicProduceByAccount2, getMessUpdateAdmin);
                //Write: in ra mess, WriteLine: in ra mess và xuống dòng
                //viết delegate để lấy được gt của mess
            }, cancellationToken);
            return Task.CompletedTask;
        }
        public async Task StopAsync(CancellationToken cancellationToken)
        {
            await Init_jeeWorkConsumer.closeAsync();
        }

        public Action<string> getMess()
        {
            Action<string> messageTarget;
            messageTarget = s => Console.WriteLine(s);
            //messageTarget = delegate(string s) { Console.WriteLine(s); };
            messageTarget("Has message !!"); //nếu có dòng này mà chưa có mess sẽ in ra dòng này 
            return messageTarget;
        }
        public void getMessInit(string message)
        {
            try
            {
                mess = message; //test để biết có nhận message từ topic ko
                var kq = JsonConvert.DeserializeObject<InitMessage>(message);
                var topic = _config.GetValue<string>("AsyncService:topicUpdateAccount");
                string roles = "";
                Console.WriteLine(message);
                if (kq.AppCode.Contains("WW"))
                {
                    string conn = _cache.GetConnectionString(kq.CustomerID);

                    List<string> roles_ad = ConsumerHelper.getRolesAdmin(conn);
                    Console.WriteLine("New customer has in app !!");
                    Console.WriteLine(DateTime.Now);
                    Console.WriteLine(roles);
                    if (kq.IsInitial) //cấp luôn roles Admin
                    {
                        roles = string.Join(",", roles_ad);
                        if (ConsumerHelper.insertUsertoAdmin(conn, kq.Username, 1) != 1)
                        {
                            return; //insert thất bại
                        }
                        ConsumerHelper.publishUpdateCustom(_producer, topic, kq.UserID, roles);
                        return;
                    }
                    if (kq.IsAdmin)
                    {
                        roles = string.Join(",", roles_ad);
                        if (ConsumerHelper.insertUsertoAdmin(conn, kq.Username, 1) != 1)
                        {
                            return; //insert thất bại
                        }
                    }
                    else
                    {
                        roles = "3400,3500"; //quyền mặc định: xem dự án & phòng ban đang có
                        if (ConsumerHelper.insertUsertoAdmin(conn, kq.Username, 2) != 1)
                        {
                            return; //insert thất bại
                        }
                    }
                    ConsumerHelper.publishUpdateCustom(_producer, topic, kq.UserID, roles);
                    return;
                }
            }
            catch
            {
                return;
            }
        }

        public void getMessUpdateAdmin(string message)
        {
            try
            {
                var kq = JsonConvert.DeserializeObject<UpdateAdminMessage>(message);
                var topic = _config.GetValue<string>("AsyncService:topicUpdateAccount");
                string roles = "";

                List<string> roles_ad = ConsumerHelper.getRolesAdmin(_cache.GetConnectionString(kq.CustomerID));
                if (kq.AppCode == "WW")
                {
                    string conn = _cache.GetConnectionString(kq.CustomerID);
                    if (kq.Action == "remove")
                    {
                        roles = "3400,3500"; //quyền mặc định: xem dự án & phòng ban đang có
                        if (ConsumerHelper.insertUsertoAdmin(conn, kq.Username, 2) != 1)
                        {
                            return; //insert thất bại
                        }
                        if (ConsumerHelper.removeUserfromAdmin(conn, kq.Username) != 1)
                        {
                            return; //remove thất bại
                        }
                    }
                    else
                    {
                        roles = string.Join(",", roles_ad);
                        if (ConsumerHelper.insertUsertoAdmin(conn, kq.Username, 1) != 1)
                        {
                            return; //insert thất bại
                        }
                    }
                    ConsumerHelper.publishUpdateCustom(_producer, topic, kq.UserID, roles);
                    return;
                }
            }
            catch
            {
                return;
            }

        }
    }
    public class messageJeeAcount
    {
        public long CustomerID { get; set; }
        public List<string> AppCode { get; set; }
        public long UserID { get; set; }
        public string Username { get; set; }
        public bool IsInitial { get; set; }
        public bool IsAdmin { get; set; }
    }
}
