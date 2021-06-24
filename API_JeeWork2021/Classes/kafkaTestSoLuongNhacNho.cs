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
using DPSinfra.ConnectionCache;
using Microsoft.Extensions.Logging;
using DPSinfra.Logger;
using System.Text;

namespace API_JeeWork2021.Classes
{
    public class kafkaTestSoLuongNhacNho : IHostedService
    {
        private IConfiguration _config;
        private Consumer testSoLuong;
        private IProducer _producer;
        private IConnectionCache _cache;
        private readonly ILogger<kafkaTestSoLuongNhacNho> _logger;
        public kafkaTestSoLuongNhacNho(IConfiguration config, IProducer producer, IConnectionCache connectionCache, ILogger<kafkaTestSoLuongNhacNho> logger)
        {
            _cache = connectionCache;
            _producer = producer;
            _config = config;
            testSoLuong = new Consumer(_config, "test-sls");
            _logger = logger;
        }
        public async Task StartAsync(CancellationToken cancellationToken)
        {
            _ = Task.Run(() =>
            {
                testSoLuong.SubscribeAsync("jeeflow.update.reminder", getMessTestSL);
            }, cancellationToken);
            await Task.CompletedTask;
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            await testSoLuong.closeAsync();
        }
        public void getMessTestSL(string msg)
        {
            Console.OutputEncoding = Encoding.UTF8;
            Console.WriteLine("========SL nhắc nhở==============================================================");
            Console.WriteLine(msg);
            Console.WriteLine("==========End SL nhắc nhở============================================================");
        }
    }

}