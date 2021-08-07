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
    [Route("api/status-dynamic")]
    [EnableCors("JeeWorkPolicy")]
    /// <summary>
    /// api quản lý we_authorize
    /// </summary>
    // [EnableCors(origins: "*", headers: "*", methods: "*")]
    public class StatusDynamicController : ControllerBase
    {
        private readonly IHostingEnvironment _hostingEnvironment;
        private JeeWorkConfig _config;
        private IConnectionCache ConnectionCache;
        private IConfiguration _configuration;
        private readonly ILogger<StatusDynamicController> _logger;

        public StatusDynamicController(IOptions<JeeWorkConfig> config, IHostingEnvironment hostingEnvironment, IConnectionCache _cache, IConfiguration configuration, ILogger<StatusDynamicController> logger)
        {
            _hostingEnvironment = hostingEnvironment;
            _config = config.Value;
            ConnectionCache = _cache;
            _configuration = configuration;
            _logger = logger;
        }
        [Route("Insert")]
        [HttpPost]
        public async Task<object> Insert(StatusDynamicModel data)
        {
            string Token = Common.GetHeader(Request);
            UserJWT loginData = Ulities.GetUserByHeader(HttpContext.Request.Headers);
            if (loginData == null)
                return JsonResultCommon.DangNhap();
            try
            {
                string strRe = "";
                if (string.IsNullOrEmpty(data.StatusName))
                    strRe += (strRe == "" ? "" : ",") + "tên status";
                if (data.Id_project_team <= 0)
                    strRe += (strRe == "" ? "" : ",") + "trường thông tin dự án/phòng ban";
                if (strRe != "")
                    return JsonResultCommon.BatBuoc(strRe);
                string ConnectionString = WeworkLiteController.getConnectionString(ConnectionCache, loginData.CustomerID, _configuration);
                using (DpsConnection cnn = new DpsConnection(ConnectionString))
                {
                    long iduser = loginData.UserID;
                    long idk = loginData.CustomerID;
                    Hashtable val = new Hashtable();
                    val.Add("StatusName", data.StatusName);
                    val.Add("id_project_team", data.Id_project_team);
                    val.Add("Disabled", 0);
                    val.Add("IsDefault", 0);
                    val.Add("IsFinal", 0);
                    val.Add("Type", data.Type);
                    if (data.Follower > 0)
                        val.Add("Follower", data.Follower);
                    else
                        val.Add("Follower", DBNull.Value);
                    if (string.IsNullOrEmpty(data.Color))
                        val.Add("color", "");
                    else
                        val.Add("color", data.Color);
                    if (string.IsNullOrEmpty(data.Description))
                        val.Add("Description", "");
                    else
                        val.Add("Description", data.Description);

                    val.Add("CreatedDate", DateTime.Now);
                    val.Add("CreatedBy", iduser);
                    // insert position
                    int statusfinal = int.Parse(cnn.ExecuteScalar("select Position from we_status where id_project_team = " + data.Id_project_team + "").ToString());
                    // lấy ra ID tiếp theo nhưng phải nhỏ hơn
                    string strCheck = "select count(*) from we_status where Disabled=0 and (id_project_team=@id_project_team) and StatusName=@name";
                    if (int.Parse(cnn.ExecuteScalar(strCheck, new SqlConditions() { { "name", data.StatusName }, { "id_project_team", data.Id_project_team } }).ToString()) > 0)
                    {
                        return JsonResultCommon.Custom("Status đã tồn tại");
                    }
                    cnn.BeginTransaction();
                    if (cnn.Insert(val, "we_status") != 1)
                    {
                        cnn.RollbackTransaction();
                        return JsonResultCommon.Exception(_logger,cnn.LastError, _config, loginData , ControllerContext);
                    }
                    string idc = cnn.ExecuteScalar("select IDENT_CURRENT('we_status')").ToString();
                    // Insert người follow cho công việc (We_Status_Process)
                    val = new Hashtable();
                    val.Add("id_project_team", data.Id_project_team);
                    val.Add("StatusID", idc);
                    if (data.Follower > 0)
                        val.Add("Checker", data.Follower);
                    else
                        val.Add("Checker", DBNull.Value);
                    val.Add("CheckedDate", DateTime.Now);
                    val.Add("CreatedBy", iduser);
                    val.Add("CreatedDate", DateTime.Now);
                    cnn.BeginTransaction();
                    //if (cnn.Insert(val, "We_Work_Process") != 1)
                    //{
                    //    cnn.RollbackTransaction();
                    //    return JsonResultCommon.Exception(_logger,cnn.LastError, _config, loginData , ControllerContext);
                    //}
                    cnn.EndTransaction();
                    data.Id_row = int.Parse(idc);
                    return JsonResultCommon.ThanhCong(data);
                }
            }
            catch (Exception ex)
            {
                return JsonResultCommon.Exception(_logger,ex, _config, loginData);
            }
        }
        [Route("Update")]
        [HttpPost]
        public async Task<object> Update(StatusDynamicModel data)
        {
            string Token = Common.GetHeader(Request);
            UserJWT loginData = Ulities.GetUserByHeader(HttpContext.Request.Headers);
            if (loginData == null)
                return JsonResultCommon.DangNhap();
            try
            {
                string strRe = "";
                if (string.IsNullOrEmpty(data.StatusName))
                    strRe += (strRe == "" ? "" : ",") + "tên status";
                if (data.Id_project_team <= 0)
                    strRe += (strRe == "" ? "" : ",") + "trường thông tin dự án/phòng ban";
                if (strRe != "")
                    return JsonResultCommon.BatBuoc(strRe);

                string ConnectionString = WeworkLiteController.getConnectionString(ConnectionCache, loginData.CustomerID, _configuration);
                using (DpsConnection cnn = new DpsConnection(ConnectionString))
                {
                    long iduser = loginData.UserID;
                    long idk = loginData.CustomerID;
                    SqlConditions sqlcond = new SqlConditions();
                    sqlcond.Add("id_row", data.Id_row);
                    string s = "select * from we_status where disabled=0 and id_row=@id_row";
                    DataTable old = cnn.CreateDataTable(s, sqlcond);
                    if (cnn.LastError != null || old == null)
                        return JsonResultCommon.Exception(_logger,cnn.LastError, _config, loginData , ControllerContext);
                    if (old.Rows.Count == 0)
                        return JsonResultCommon.KhongTonTai("Filter");
                    Hashtable val = new Hashtable();
                    val.Add("StatusName", data.StatusName);
                    val.Add("id_project_team", data.Id_project_team);
                    val.Add("Disabled", 0);
                    val.Add("IsDefault", 0);
                    if (!string.IsNullOrEmpty(data.Type))
                    {
                        val.Add("Type", data.Type);
                    }
                    if (string.IsNullOrEmpty(data.Color))
                        val.Add("color", "");
                    else
                        val.Add("color", data.Color);
                    if (string.IsNullOrEmpty(data.Description))
                        val.Add("Description", "");
                    else
                        val.Add("Description", data.Description);
                    if (data.Follower > 0)
                        val.Add("Follower", data.Follower);
                    else
                        val.Add("Follower", DBNull.Value);
                    val.Add("UpdatedDate", DateTime.Now);
                    val.Add("UpdatedBy", iduser);
                    string strCheck = "select count(*) from we_status where Disabled=0 and id_project_team=@id_project_team and StatusName=@name  and id_row != @id_row";
                    if (int.Parse(cnn.ExecuteScalar(strCheck, new SqlConditions() { { "name", data.StatusName }, { "id_project_team", data.Id_project_team }, { "id_row", data.Id_row } }).ToString()) > 0)
                    {
                        return JsonResultCommon.Custom("Status đã tồn tại");
                    }
                    cnn.BeginTransaction();
                    if (cnn.Update(val, sqlcond, "we_status") != 1)
                    {
                        cnn.RollbackTransaction();
                        return JsonResultCommon.Exception(_logger,cnn.LastError, _config, loginData , ControllerContext);
                    }
                    cnn.EndTransaction();
                    return JsonResultCommon.ThanhCong(data);
                }
            }
            catch (Exception ex)
            {
                return JsonResultCommon.Exception(_logger,ex, _config, loginData);
            }
        }
        [Route("Delete")]
        [HttpGet]
        public BaseModel<object> Delete(long id)
        {
            string Token = Common.GetHeader(Request);
            UserJWT loginData = Ulities.GetUserByHeader(HttpContext.Request.Headers);
            if (loginData == null)
                return JsonResultCommon.DangNhap();
            try
            {
                long iduser = loginData.UserID;
                long idk = loginData.CustomerID;
                string ConnectionString = WeworkLiteController.getConnectionString(ConnectionCache, loginData.CustomerID, _configuration);
                using (DpsConnection cnn = new DpsConnection(ConnectionString))
                {
                    string sqlq = "select ISNULL((select count(*) from we_status where disabled=0 and id_row = " + id + "),0)";
                    if (long.Parse(cnn.ExecuteScalar(sqlq).ToString()) != 1)
                        return JsonResultCommon.KhongTonTai("Filter");
                    sqlq = "update we_status set Disabled=1, UpdatedDate=getdate(), UpdatedBy=" + iduser + " where id_row = " + id;
                    cnn.BeginTransaction();
                    if (cnn.ExecuteNonQuery(sqlq) != 1)
                    {
                        cnn.RollbackTransaction();
                        return JsonResultCommon.Exception(_logger,cnn.LastError, _config, loginData , ControllerContext);
                    }
                    cnn.EndTransaction();
                    return JsonResultCommon.ThanhCong();
                }
            }
            catch (Exception ex)
            {
                return JsonResultCommon.Exception(_logger,ex, _config, loginData);
            }
        }

        [Route("different-statuses")]
        [HttpPost]
        public async Task<object> Different_Statuses(Different_Statuses data)
        {
            string Token = Common.GetHeader(Request);
            string sqlq_insert = "";
            UserJWT loginData = Ulities.GetUserByHeader(HttpContext.Request.Headers);
            if (loginData == null)
                return JsonResultCommon.DangNhap();
            try
            {
                string strRe = "";
                if (data.id_project_team <= 0)
                    strRe += (strRe == "" ? "" : ",") + "trường thông tin dự án/phòng ban";
                if (data.TemplateID_New <= 0)
                    strRe += (strRe == "" ? "" : ",") + "trường thông tin mẫu mới";
                if (data.TemplateID_New <= 0)
                    strRe += (strRe == "" ? "" : ",") + "trường thông tin mẫu cũ";
                if (strRe != "")
                    return JsonResultCommon.BatBuoc(strRe);
                string ConnectionString = WeworkLiteController.getConnectionString(ConnectionCache, loginData.CustomerID, _configuration);
                using (DpsConnection cnn = new DpsConnection(ConnectionString))
                {
                    long iduser = loginData.UserID;
                    long idk = loginData.CustomerID;
                    #region Update những status hiện tại về disabled = 1
                    sqlq_insert = "update we_status set Disabled=1, UpdatedDate=getdate(), UpdatedBy=" + iduser + " where id_project_team = " + data.id_project_team;
                    cnn.BeginTransaction();
                    if (cnn.ExecuteNonQuery(sqlq_insert) < 0)
                    {
                        cnn.RollbackTransaction();
                        return JsonResultCommon.Exception(_logger,cnn.LastError, _config, loginData , ControllerContext);
                    }
                    //DataTable dt_status_old = cnn.CreateDataTable("select * from we_status where Disabled = 1 and id_project_team =" + data.id_project_team + "");
                    #endregion
                    #region update lại template mới
                    string sql_update_template = $@"update we_project_team set id_template = {data.TemplateID_New} , UpdatedDate=getdate(), UpdatedBy=" + iduser + " where id_row = " + data.id_project_team;
                    if (cnn.ExecuteNonQuery(sql_update_template) < 0)
                    {
                        cnn.RollbackTransaction();
                        return JsonResultCommon.Exception(_logger,cnn.LastError, _config, loginData , ControllerContext);
                    }
                    #endregion
                    #region Insert những status của template mới
                    sqlq_insert = $@"insert into we_status (StatusName, description, Disabled, id_project_team, Type, IsDefault, color, Position, IsFinal, Follower, IsDeadline, IsToDo, StatusID_Reference, CreatedBy, CreatedDate)
                        select StatusName, description, Disabled, " + data.id_project_team + ", Type, IsDefault, color, Position, IsFinal, Follower, IsDeadline, IsTodo, Id_row," + loginData.UserID + ", getdate() from we_template_status where Disabled = 0 and TemplateID = " + data.TemplateID_New + "";
                    int rs = cnn.ExecuteNonQuery(sqlq_insert);
                    if (rs < 0)
                    {
                        cnn.RollbackTransaction();
                        return JsonResultCommon.Exception(_logger,cnn.LastError, _config, loginData , ControllerContext);
                    }
                    // insert người
                    #endregion
                    int status_todo = int.Parse(cnn.ExecuteScalar("select id_row from we_status where Disabled = 0 and id_project_team = " + data.id_project_team + " and istodo = 1").ToString());

                    if (data.IsMapAll)
                    {
                        string sql_fl = "select iIf(Follower is not null,Follower,0) from we_status where Disabled = 0 and IsToDo = 1 and id_project_team = " + data.id_project_team;
                        int fl = int.Parse(cnn.ExecuteScalar(sql_fl).ToString());
                        // map người follow công việc tương ứng
                        sqlq_insert = $"update we_status set Follower = {(fl > 0 ? fl.ToString() : " null ")} " +
                            "where id_project_team = " + data.id_project_team + " and disabled = 0";
                        sqlq_insert += ";update we_work set status = " + status_todo + " where id_project_team = " + data.id_project_team;
                        rs = cnn.ExecuteNonQuery(sqlq_insert);
                        if (rs < 0)
                        {
                            cnn.RollbackTransaction();
                            return JsonResultCommon.Exception(_logger,cnn.LastError, _config, loginData , ControllerContext);
                        }
                    }
                    else
                    {
                        if (data.Map_Detail != null)
                        {
                            foreach (var _map in data.Map_Detail)
                            {
                                string sql_update = ""; string where = "";
                                // update người follower
                                where = "where StatusID_Reference = " + _map.new_status + " and id_project_team =" + data.id_project_team + " and disabled = 0";
                                sql_update = $@"update we_status set Follower = (select Follower from we_status where id_row = " + _map.old_status + ") ";
                                sql_update += where;
                                sql_update += $@";update we_work set status_old = " + _map.old_status + " where status = "+ _map.old_status + " and id_project_team = " + data.id_project_team + "";
                                long newStatusID = long.Parse(cnn.ExecuteScalar("select id_row from we_status " + where).ToString());
                                sql_update += $@";update we_work set status = " + newStatusID + " where status = " + _map.old_status + " and id_project_team = " + data.id_project_team + "";
                                rs = cnn.ExecuteNonQuery(sql_update);
                                if (rs < 0)
                                {
                                    cnn.RollbackTransaction();
                                    return JsonResultCommon.Exception(_logger,cnn.LastError, _config, loginData , ControllerContext);
                                }
                            }
                        }
                    }
                    cnn.EndTransaction();
                    return JsonResultCommon.ThanhCong(data);
                }
            }
            catch (Exception ex)
            {
                return JsonResultCommon.Exception(_logger,ex, _config, loginData);
            }
        }
    }
}
