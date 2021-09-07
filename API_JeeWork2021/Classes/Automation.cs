using DPSinfra.Kafka;
using DPSinfra.Notifier;
using JeeWork_Core2021.Controllers.Wework;
using JeeWork_Core2021.Models;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace API_JeeWork2021.Classes
{
    public class Automation
    {
        public INotifier _notifier;
        public Automation(INotifier notifier)
        {
            _notifier = notifier;
        }

        public async static void SendAutomation(Post_Automation_Model post, IConfiguration _config, IProducer _producer)
        {
            //if (WeworkLiteController.IsNotify(_config))
            //{
                string topic = _config.GetValue<string>("KafkaConfig:TopicProduce:JeeWorkAutomationService");
                string obj = Newtonsoft.Json.JsonConvert.SerializeObject(post);
                await _producer.PublishAsync(topic, obj);
            //}
                
        }
    }
}
