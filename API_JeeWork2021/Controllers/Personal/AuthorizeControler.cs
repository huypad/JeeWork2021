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
using DPSinfra.ConnectionCache;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace JeeWork_Core2021.Controllers.Wework
{
    [ApiController]
    [Route("api/authorizecontroler")]
    [EnableCors("JeeWorkPolicy")]
    /// <summary>
    /// api quản lý we_authorize
    /// </summary>
    // [EnableCors(origins: "*", headers: "*", methods: "*")]
    public class AuthorizeControler : ControllerBase
    {
        private JeeWorkConfig _config;
        private IConnectionCache ConnectionCache;
        private IConfiguration _configuration;
        private readonly ILogger<AuthorizeControler> _logger;

        public AuthorizeControler(IOptions<JeeWorkConfig> config, IConnectionCache _cache, IConfiguration configuration, ILogger<AuthorizeControler> logger)
        {
            _config = config.Value; 
            ConnectionCache = _cache;
            _configuration = configuration;
            _logger = logger;
        }

        /// <param name="data"></param>
        /// <returns></returns>
        [Route("Insert")]
        [HttpPost]
        public async Task<object> Insert([FromBody] AuthorizeModel data)
        {
            UserJWT loginData = Ulities.GetUserByHeader(HttpContext.Request.Headers);
            if (loginData == null)
                return JsonResultCommon.DangNhap();
            try
            {
                string strRe = "";
                if (data.id_user < 0)
                    strRe += (strRe == "" ? "" : ",") + "thông tin người ủy quyền";
                if (strRe != "")
                    return JsonResultCommon.BatBuoc(strRe);

                string ConnectionString = JeeWorkLiteController.getConnectionString(ConnectionCache, loginData.CustomerID, _configuration);
                using (DpsConnection cnn = new DpsConnection(ConnectionString))
                {
                    long iduser = loginData.UserID;
                    long idk = loginData.CustomerID;
                    Hashtable val = new Hashtable();
                    val.Add("id_user", data.id_user);
                    val.Add("CreatedDate", Common.GetDateTime());
                    val.Add("CreatedBy", iduser);
                    cnn.BeginTransaction();
                    if (cnn.Insert(val, "we_authorize") != 1)
                    {
                        cnn.RollbackTransaction();
                        if (cnn.LastError != null)
                            cnn.LastError.Data.Add("customerid", loginData.CustomerID);
                        return JsonResultCommon.Exception(_logger,cnn.LastError, _config, loginData,ControllerContext);
                    }
                    string idc = cnn.ExecuteScalar("select IDENT_CURRENT('we_authorize')").ToString();
                    cnn.EndTransaction();
                    data.id_row = int.Parse(idc);
                    //clientdata<DemoModel> data = new clientdata<DemoModel>();

                    //data.data = new DemoModel();
                    //data.data.tmp = "this is message of notify";
                    //data.status = 1;
                    //Producer<DemoModel> producer = Producer<DemoModel>.getInstance(_config);
                    //producer.PublishAsync("NFC_Notification", data);
                    return JsonResultCommon.ThanhCong(data);
                }
            }
            catch (Exception ex)
            {
                return JsonResultCommon.Exception(_logger,ex, _config, loginData);
            }
        }

        /// <param name="data"></param>
        /// <returns></returns>
        [Route("Update")]
        [HttpPost]
        public async Task<BaseModel<object>> Update([FromBody] AuthorizeModel data)
        {
            UserJWT loginData = Ulities.GetUserByHeader(HttpContext.Request.Headers);
            if (loginData == null)
                return JsonResultCommon.DangNhap();
            try
            {
                string strRe = "";
                if (data.id_user < 0)
                    strRe += (strRe == "" ? "" : ",") + "thông tin người ủy quyền";
                if (strRe != "")
                    return JsonResultCommon.BatBuoc(strRe);
                string ConnectionString = JeeWorkLiteController.getConnectionString(ConnectionCache, loginData.CustomerID, _configuration);
                using (DpsConnection cnn = new DpsConnection(ConnectionString))
                {
                    SqlConditions sqlcond = new SqlConditions();
                    sqlcond.Add("id_row", data.id_row);
                    sqlcond.Add("disabled", 0);
                    string s = "select * from we_authorize where (where)";
                    DataTable old = cnn.CreateDataTable(s, "(where)", sqlcond);
                    if (old == null || old.Rows.Count == 0)
                        return JsonResultCommon.KhongTonTai("Thông tin ủy quyền");
                    long iduser = loginData.UserID;
                    long idk = loginData.CustomerID;
                    Hashtable val = new Hashtable();
                    val.Add("is_all_project", data.is_all_project);
                    val.Add("list_project", data.list_project);
                    val.Add("start_date", data.start_date);
                    val.Add("end_date", data.end_date);
                    val.Add("id_user", data.id_user);
                    val.Add("UpdatedDate", Common.GetDateTime());
                    val.Add("UpdatedBy", iduser);
                    cnn.BeginTransaction();
                    if (cnn.Update(val, sqlcond, "we_authorize") != 1)
                    {
                        cnn.RollbackTransaction();
                        if (cnn.LastError != null)
                            cnn.LastError.Data.Add("customerid", loginData.CustomerID);
                        return JsonResultCommon.Exception(_logger,cnn.LastError, _config, loginData,ControllerContext);
                    }
                    DataTable dt = cnn.CreateDataTable(s, "(where)", sqlcond);
                    cnn.EndTransaction();
                    return JsonResultCommon.ThanhCong(data);
                }
            }
            catch (Exception ex)
            {
                return JsonResultCommon.Exception(_logger,ex, _config, loginData);
            }
        }

        /// <param name="id"></param>
        /// <returns></returns>
        [Route("Delete")]
        [HttpGet]
        public BaseModel<object> Delete(long id)
        {
            UserJWT loginData = Ulities.GetUserByHeader(HttpContext.Request.Headers);
            if (loginData == null)
                return JsonResultCommon.DangNhap();
            try
            {
                long iduser = loginData.UserID;
                long idk = loginData.CustomerID;
                string ConnectionString = JeeWorkLiteController.getConnectionString(ConnectionCache, loginData.CustomerID, _configuration);
                using (DpsConnection cnn = new DpsConnection(ConnectionString))
                {
                    string sqlq = "select ISNULL((select count(*) from we_authorize where Disabled=0 and id_row = " + id + "),0)";
                    if (long.Parse(cnn.ExecuteScalar(sqlq).ToString()) != 1)
                        return JsonResultCommon.KhongTonTai("Nhóm làm việc");
                    //if (Common.TestDuplicate("", id.ToString(), "-1", "we_work", "id_group", "", "-1", cnn, "", true) == false)
                    //{
                    //    return JsonResultCommon.Custom("Đang có công việc thuộc nhóm công việc này nên không thể xóa");
                    //}
                    sqlq = "update we_authorize set Disabled=1, UpdatedDate=GETUTCDATE(), UpdatedBy=" + iduser + " where id_row = " + id;
                    cnn.BeginTransaction();
                    if (cnn.ExecuteNonQuery(sqlq) != 1)
                    {
                        cnn.RollbackTransaction();
                        return JsonResultCommon.Exception(_logger,cnn.LastError, _config, loginData,ControllerContext);
                    }
                    string LogContent = "Xóa dữ liệu authorize (" + id + ")";
                    cnn.EndTransaction();
                    return JsonResultCommon.ThanhCong();
                }
            }
            catch (Exception ex)
            {
                return JsonResultCommon.Exception(_logger,ex, _config, loginData);
            }
        }
    }
}