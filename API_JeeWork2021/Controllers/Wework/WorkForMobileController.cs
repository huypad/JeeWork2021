using DpsLibs.Data;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using JeeWork_Core2021.Classes;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Hosting;
using JeeWork_Core2021.Models;
using Microsoft.Extensions.Options;
using Microsoft.AspNetCore.Authorization;
using System.Globalization;
using System.Text;
using DPSinfra.ConnectionCache;
using Microsoft.Extensions.Configuration;
using DPSinfra.Notifier;
using Microsoft.Extensions.Logging;
using API_JeeWork2021.Classes;
using DPSinfra.Kafka;
using JeeWork_Core2021.Controller;

namespace JeeWork_Core2021.Controllers.Wework
{
    [ApiController]
    [Route("api/work-for-mobile")]
    [EnableCors("JeeWorkPolicy")]
    /// <summary>
    /// api quản lý work (click up)
    /// </summary>
    // [EnableCors(origins: "*", headers: "*", methods: "*")]
    public class WorkForMobileController : ControllerBase
    {
        private readonly IHostingEnvironment _hostingEnvironment;
        private JeeWorkConfig _config;
        public static DataImportModel data_import = new DataImportModel();
        public List<AccUsernameModel> DataAccount;
        private IConnectionCache ConnectionCache;
        private IConfiguration _configuration;
        private INotifier _notifier;
        private IProducer _producer;
        private readonly ILogger<WorkClickupController> _logger;
        public WorkForMobileController(IOptions<JeeWorkConfig> config, IHostingEnvironment hostingEnvironment, IConnectionCache _cache, IConfiguration configuration, INotifier notifier, ILogger<WorkClickupController> logger, IProducer producer)
        {
            _hostingEnvironment = hostingEnvironment;
            _config = config.Value;
            ConnectionCache = _cache;
            _configuration = configuration;
            _notifier = notifier;
            _logger = logger;
            _producer = producer;

        }
        APIModel.Models.Notify Knoti;
    }
}
