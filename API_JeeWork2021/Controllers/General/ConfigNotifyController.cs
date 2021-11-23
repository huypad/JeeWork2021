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
using DPSinfra.ConnectionCache;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using JeeWork_Core2021.Controller;
using static JeeWork_Core2021.Models.ConfigNotify;

namespace JeeWork_Core2021.Controllers.Wework
{
    [ApiController]
    [Route("api/config-notify")]
    [EnableCors("JeeWorkPolicy")]
    /// <summary>
    /// api quản lý config-notify
    /// </summary>
    // [EnableCors(origins: "*", headers: "*", methods: "*")]
    public class ConfigNotifyController : ControllerBase
    {
        private readonly IHostingEnvironment _hostingEnvironment;
        private JeeWorkConfig _config;
        private IConnectionCache ConnectionCache;
        private IConfiguration _configuration;
        private readonly ILogger<ConfigNotifyController> _logger;

        public ConfigNotifyController(IOptions<JeeWorkConfig> config, IHostingEnvironment hostingEnvironment, IConnectionCache _cache, IConfiguration configuration, ILogger<ConfigNotifyController> logger)
        {
            _hostingEnvironment = hostingEnvironment;
            _config = config.Value;
            ConnectionCache = _cache;
            _configuration = configuration;
            _logger = logger;
        }
        [Route("get-list-config")]
        [HttpGet]
        public BaseModel<object> GetListConfig(long id, string langcode)
        {
            UserJWT loginData = Ulities.GetUserByHeader(HttpContext.Request.Headers);
            if (loginData == null)
                return JsonResultCommon.DangNhap();
            string[] arrConfig = new string[100];
            string[] arrnotify = new string[100];
            string[] arremail = new string[100];
            try
            {
                string ConnectionString = JeeWorkLiteController.getConnectionString(ConnectionCache, loginData.CustomerID, _configuration);
                using (DpsConnection cnn = new DpsConnection(ConnectionString))
                {
                    string sqlq = "select id_row, config_notify, config_email " +
                        "from we_project_team where disabled=0 and id_row = " + id + "";
                    DataTable dt = cnn.CreateDataTable(sqlq);
                    if (cnn.LastError != null || dt == null || dt.Rows.Count == 0)
                        return JsonResultCommon.KhongTonTai("Dự án/phòng ban");
                    DataTable dt_notify = new DataTable();
                    ConfigNotify _config = new ConfigNotify();
                    dt_notify = JeeWorkLiteController.dt_notify(cnn);
                    if (dt.Rows.Count == 1)
                    {
                        DataRow row = dt.Rows[0];
                        string notify = dt.Rows[0]["config_notify"].ToString();
                        if ("".Equals(notify))
                        {
                            for (int i = 1; i <= 100; i++)
                            {
                                notify += "0";
                            }
                        }
                        arrnotify = _config.ConvertToArray(notify);
                        string _email = dt.Rows[0]["config_email"].ToString();
                        if ("".Equals(_email))
                        {
                            for (int i = 1; i <= 100; i++)
                            {
                                _email += "0";
                            }
                        }
                        arremail = _config.ConvertToArray(_email);
                    }
                    var data = from r in dt_notify.AsEnumerable()
                               select new
                               {
                                   id_row = r["id_row"],
                                   action = r["action"],
                                   object_type = r["object_type"],
                                   show_in_activities = r["show_in_activities"],
                                   view_detail = r["view_detail"],
                                   icon = r["icon"],
                                   isnotify = arrnotify[int.Parse(r["id_row"].ToString())].Equals("1") ? true : false,
                                   isemail = arremail[int.Parse(r["id_row"].ToString())].Equals("1") ? true : false,
                                   id_project_team = id,
                                   langkey = LocalizationUtility.GetBackendMessage(r["langkey"].ToString(), "", langcode),
                                   id_template_mail = r["id_template_mail"]
                               };
                    return JsonResultCommon.ThanhCong(data);
                }
            }
            catch (Exception ex)
            {
                return JsonResultCommon.Exception(_logger, ex, _config, loginData);
            }
        }
        /// <summary>
        /// Lưu on/off thông báo
        /// </summary>
        /// <param name="arr_data"></param>
        /// <returns></returns>
        [Route("save_notify")]
        [HttpPost]
        public BaseModel<object> Save_Notify(ConfigNotifyModel data)
        {
            BaseModel<object> model = new BaseModel<object>();
            SqlConditions Conds = new SqlConditions();
            UserJWT loginData = Ulities.GetUserByHeader(HttpContext.Request.Headers);
            try
            {
                string[] arrConfig = new string[100];
                ConfigNotify _cf = new ConfigNotify();
                if (loginData != null)
                {
                    #region Khai bao sử dụng ConnectionString
                    string connectionString = JeeWorkLiteController.getConnectionString(ConnectionCache, loginData.CustomerID, _configuration);
                    #endregion
                    using (DpsConnection cnn = new DpsConnection(connectionString))
                    {
                        string sqlq = "select id_row, config_notify, config_email " +
                        "from we_project_team where disabled=0 and id_row = " + data.id_project_team + "";
                        string where_customer = "";
                        //where_customer = " and id_department in (select id_row from we_department where disabled = 0 " +
                        //    "and idkh = " + loginData.CustomerID+")";
                        DataTable dt = cnn.CreateDataTable(sqlq + where_customer);
                        if (!JeeWorkLiteController.CheckCustomerID(data.id_project_team, "we_project_team", loginData, cnn))
                        {
                            return JsonResultCommon.Custom("Dự án không tồn tại");
                        }
                        if (dt.Rows.Count == 1)
                        {
                            string notifydb = "";
                            if (data.isnotify)
                                notifydb = dt.Rows[0]["config_notify"].ToString();
                            else
                                notifydb = dt.Rows[0]["config_email"].ToString();
                            if ("".Equals(notifydb))
                            {
                                for (int i = 1; i <= 100; i++)
                                {
                                    notifydb += "0";
                                }
                            }
                            arrConfig = _cf.ConvertToArray(notifydb);
                            arrConfig[int.Parse(data.id_row.ToString())] = data.values ? "1" : "0";
                            string result = _cf.ConvertToString(arrConfig);
                            Hashtable val = new Hashtable();
                            if (data.isnotify)
                                val.Add("config_notify", result);
                            else
                                val.Add("config_email", result);
                            SqlConditions cond = new SqlConditions();
                            cond.Add("id_row", data.id_project_team);
                            int rs = cnn.Update(val, cond, "we_project_team");
                            if (rs <= 0)
                            {
                                cnn.RollbackTransaction();
                                return JsonResultCommon.Exception(_logger, cnn.LastError, _config, loginData, ControllerContext);
                            }
                        }
                    }
                    model.status = 1;
                    model.data = true;
                    return model;
                }
                else
                {
                    return JsonResultCommon.DangNhap();
                }
            }
            catch (Exception ex)
            {
                return JsonResultCommon.Exception(_logger, ex, _config, loginData);
            }
        }
    }
}

