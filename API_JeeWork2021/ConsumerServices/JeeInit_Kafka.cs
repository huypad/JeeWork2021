using JeeWork_Core2021.Models;
using DPSinfra.ConnectionCache;
using DPSinfra.Kafka;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using DPSinfra.Logger;
using JeeWork_Core2021.Controllers.Wework;
using JeeWork_Core2021.Classes;

namespace JeeWork_Core2021.ConsumerServices
{
    public class JeeInit_Kafka : IHostedService
    {
        private IConfiguration _config;
        private Consumer initJeeAdminConsumer, updateAdminCosumer;
        private string mess;
        IProducer _producer;
        private readonly ILogger<JeeInit_Kafka> _logger;
        private IConnectionCache _cache;
        public JeeInit_Kafka(IConfiguration config, IProducer producer, IConnectionCache connectionCache, ILogger<JeeInit_Kafka> logger)
        {
            _config = config;
            _producer = producer;
            var groupid1 = _config.GetValue<string>("KafkaConfig:Consumer:JeeWorkGroupInit");
            initJeeAdminConsumer = new Consumer(_config, groupid1);
            var groupid2 = _config.GetValue<string>("KafkaConfig:Consumer:JeeWorkGroupUpdateAdmin");
            updateAdminCosumer = new Consumer(_config, groupid2);
            _cache = connectionCache;
            _logger = logger;
        }
        public Task StartAsync(CancellationToken cancellationToken)
        {
           var topicProduceByAccount1 = _config.GetValue<string>("KafkaConfig:TopicConsume:JeeplatformInitialization");
            var topicProduceByAccount2 = _config.GetValue<string>("KafkaConfig:TopicConsume:JeeplatformUpdateAdmin");
            _ = Task.Run(() =>
            {
                //ko có thì khi topic có mess, consumer sẽ ko thể lấy được mess
                //c1:
                //initJeeAdminConsumer.SubscribeAsync(topicProduceByAccount, delegate (string s) { getMess(s); });
                //c2:
                initJeeAdminConsumer.SubscribeAsync(topicProduceByAccount1, getMessInit);
                updateAdminCosumer.SubscribeAsync(topicProduceByAccount2, getMessUpdateAdmin);
                //Write: in ra mess, WriteLine: in ra mess và xuống dòng
                //viết delegate để lấy được gt của mess
            }, cancellationToken);
            return Task.CompletedTask;
        }
        public async Task StopAsync(CancellationToken cancellationToken)
        {
            await initJeeAdminConsumer.closeAsync();
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
                var topic = _config.GetValue<string>("KafkaConfig:TopicProduce:JeeplatformInitializationAppupdate");
                string roles = "";
                Console.WriteLine(message);
                if (kq.AppCode.Contains("WORK"))
                {
                    string conn = JeeWorkLiteController.getConnectionString(_cache, kq.CustomerID, _config); ;
                    List<string> roles_admin = ConsumerHelper.getRoles(conn);
                    Console.WriteLine("New customer has in app");
                    Console.WriteLine(Common.GetDateTime());
                    Console.WriteLine(roles);
                    if (kq.IsInitial) //cấp luôn roles Admin
                    {
                        roles = string.Join(",", roles_admin);
                        int idnhom = ConsumerHelper.createNhom(conn, kq.CustomerID, roles_admin, 1);
                        if (idnhom > 0)
                        {
                            if (ConsumerHelper.insertUsertoGroup(conn, kq.Username, idnhom) != 1)
                            {
                                return; //insert thất bại
                            }
                        }
                        ConsumerHelper.publishUpdateCustom(_producer, topic, kq.UserID, roles);
                        return;
                    }
                    if (kq.IsAdmin)
                    {
                        roles = string.Join(",", roles_admin);
                        int idnhom = ConsumerHelper.createNhom(conn, kq.CustomerID, roles_admin, 1);
                        if (idnhom > 0)
                        {
                            if (ConsumerHelper.insertUsertoGroup(conn, kq.Username, idnhom) != 1)
                            {
                                return;
                            }
                        }
                    }
                    else
                    {
                        roles = "3501,3502,3503,3610"; //quyền mặc định 
                        List<string> rol_df = new List<string>() { "3502","3502","3503","3610" }; 
                        int idnhom = ConsumerHelper.createNhom(conn, kq.CustomerID, rol_df);
                        if (idnhom > 0)
                        {
                            if (ConsumerHelper.insertUsertoGroup(conn, kq.Username, idnhom) != 1)
                            {
                                return;
                            }
                        }
                    }
                    ConsumerHelper.publishUpdateCustom(_producer, topic, kq.UserID, roles);
                    return;
                }
            }
            catch (Exception ex)
            {
                return;
            }
        }
        public void getMessUpdateAdmin(string message)
        {
            try
            { 
                var d1 = new GeneralLog()
                {
                    name = "jee-work",
                    data = message,
                    message = "jeeplatform.updateadmin"
                };
                _logger.LogTrace(JsonConvert.SerializeObject(d1));
                var kq = JsonConvert.DeserializeObject<UpdateAdminMessage>(message);
                var topic = _config.GetValue<string>("KafkaConfig:TopicProduce:JeeplatformInitializationAppupdate");
                string roles = "";
                if (kq.AppCode == _config.GetValue<string>("AppConfig:AppCode"))
                {
                    string conn = _cache.GetConnectionString(kq.CustomerID);
                    List<string> roles_ad = ConsumerHelper.getRoles(conn);
                    if (kq.Action == "remove")
                    {
                        roles = "0";
                        int idnhom = ConsumerHelper.getIdGroup(conn, kq.CustomerID, 1);
                        if (ConsumerHelper.removeUserfromGroup(conn, kq.Username, idnhom) != 1)
                        {
                            return;
                        }
                        idnhom = ConsumerHelper.getIdGroup(conn, kq.CustomerID);
                        if (ConsumerHelper.insertUsertoGroup(conn, kq.Username, idnhom) != 1)
                        {
                            return;
                        }
                    }
                    else
                    {
                        roles = string.Join(",", roles_ad);
                        int idnhom = ConsumerHelper.getIdGroup(conn, kq.CustomerID, 1);
                        if (ConsumerHelper.insertUsertoGroup(conn, kq.Username, idnhom) != 1)
                        {
                            return;
                        }
                    }
                    var dataRoles = new
                    {
                        roles = roles
                    };
                    UpdateMessage updateMess = new UpdateMessage();
                    var topicUpdateAccount = topic;
                    updateMess.userID = kq.UserID;
                    updateMess.updateField = _config.GetValue<string>("KafkaConfig:ProjectName");
                    updateMess.fieldValue = dataRoles;
                    var mess_send = JsonConvert.SerializeObject(updateMess);
                    _producer.PublishAsync(topicUpdateAccount, mess_send);
                    Console.WriteLine("====================");
                    Console.WriteLine(roles);
                    var d2 = new GeneralLog()
                    {
                        name = "jee-work",
                        data = mess_send,
                        message = topicUpdateAccount
                    };
                    _logger.LogTrace(JsonConvert.SerializeObject(d2));
                    ConsumerHelper.publishUpdateCustom(_producer, topic, kq.UserID, roles);
                    return;
                }
            }
            catch
            {
                return;
            }
        }
        public void getValue(string value)
        {
            Console.WriteLine(value);
        }
    }
}
