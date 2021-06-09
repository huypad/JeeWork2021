using DPSinfra.Kafka;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace JeeWork_Core2021.ConsumerServices
{
    public class testkafka : IHostedService
    {
        private IConfiguration _config;
        private Consumer testkafkaConsumer;
        public testkafka(IConfiguration config)
        {
            _config = config;

            //get group 
            var groupid = _config.GetValue<string>("KafkaConfig:groupTestJA");
            testkafkaConsumer = new Consumer(_config, groupid);
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            var topicProduceByJA = _config.GetValue<string>("KafkaConfig:topicProduceByJA");
            _ = Task.Run(() =>
            {
                testkafkaConsumer.SubscribeAsync(topicProduceByJA, Console.WriteLine);
            }, cancellationToken);
            return Task.CompletedTask;
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            await testkafkaConsumer.closeAsync();
        }
    }
}
