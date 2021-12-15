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
using System.Globalization;
using DPSinfra.Logger;
using Newtonsoft.Json;

namespace JeeWork_Core2021.Controllers.Wework
{
    [ApiController]
    [Route("api/automation")]
    [EnableCors("JeeWorkPolicy")]
    /// <summary>
    /// api quản lý autonmation
    /// </summary>
    public class AutomationController : ControllerBase
    {
        private readonly IHostingEnvironment _hostingEnvironment;
        private JeeWorkConfig _config;
        private IConnectionCache ConnectionCache;
        private IConfiguration _configuration;
        private readonly ILogger<AutomationController> _logger;
        public List<AccUsernameModel> DataAccount;
        public AutomationController(IOptions<JeeWorkConfig> config, IHostingEnvironment hostingEnvironment, IConnectionCache _cache, IConfiguration configuration, ILogger<AutomationController> logger)
        {
            _hostingEnvironment = hostingEnvironment;
            _config = config.Value;
            ConnectionCache = _cache;
            _configuration = configuration;
            _logger = logger;
        }
        /// <summary>
        /// GetActionList
        /// </summary>
        /// <param name="query"></param>
        /// <returns></returns>
        [Route("get-action-list")]
        [HttpGet]
        public object GetListActionList([FromQuery] QueryParams query)
        {
            UserJWT loginData = Ulities.GetUserByHeader(HttpContext.Request.Headers);
            if (loginData == null)
                return JsonResultCommon.DangNhap();
            try
            {
                if (query == null)
                    query = new QueryParams();
                string ConnectionString = JeeWorkLiteController.getConnectionString(ConnectionCache, loginData.CustomerID, _configuration);
                using (DpsConnection cnn = new DpsConnection(ConnectionString))
                {
                    Dictionary<string, string> collect = new Dictionary<string, string>
                        {
                            { "id_row", "id_row"},
                            { "subactionname", "subactionname"},
                        };
                    SqlConditions cond = new SqlConditions();
                    string where_string = "";
                    #region Filter
                    if (!string.IsNullOrEmpty(query.filter["keyword"]))
                    {
                        where_string += " and (subactionname like N'%@keyword%') ";
                        where_string = where_string.Replace("@keyword", query.filter["keyword"]);
                    }
                    if (!string.IsNullOrEmpty(query.filter["actionid"]))
                    {
                        where_string += $" and actionid = " + query.filter["actionid"] + "";
                    }
                    #endregion
                    #region Trả dữ liệu về backend để hiển thị lên giao diện
                    string sqlq = @"select rowid, actionname, description, disabled
                                    from automation_actionList where disabled = 0 order by actionname";
                    sqlq += @";select rowid, actionid, subactionname, tablename, primarykeylist, description, disabled
                                    from automation_subactionlist
                                    where disabled = 0 " + where_string + " order by subactionname";
                    DataSet ds = cnn.CreateDataSet(sqlq, cond);
                    if (cnn.LastError != null || ds == null)
                        return JsonResultCommon.Exception(_logger, cnn.LastError, _config, loginData, ControllerContext);
                    var data = from r in ds.Tables[0].AsEnumerable()
                               select new
                               {
                                   rowid = r["rowid"],
                                   actionname = r["actionname"],
                                   description = r["description"],
                                   disabled = r["disabled"],
                                   data_subactionlist = from s in ds.Tables[1].AsEnumerable()
                                                        where s["actionid"].ToString() == r["rowid"].ToString()
                                                        select new
                                                        {
                                                            rowid = s["rowid"],
                                                            actionid = s["actionid"],
                                                            subactionname = s["subactionname"],
                                                            tablename = s["tablename"],
                                                            primarykeylist = s["primarykeylist"],
                                                            description = s["description"],
                                                            disabled = s["disabled"],
                                                        },
                               };
                    return JsonResultCommon.ThanhCong(data);
                    #endregion
                }
            }
            catch (Exception ex)
            {
                return JsonResultCommon.Exception(_logger, ex, _config, loginData);
            }
        }
        /// <summary>
        /// get automation list
        /// </summary>
        /// <returns></returns>
        [Route("get-automation-list")]
        [HttpGet]
        public object Get_AutomationList([FromQuery] QueryParams query)
        {
            UserJWT loginData = Ulities.GetUserByHeader(HttpContext.Request.Headers);
            if (loginData == null)
                return JsonResultCommon.DangNhap();
            try
            {
                string ConnectionString = JeeWorkLiteController.getConnectionString(ConnectionCache, loginData.CustomerID, _configuration);
                using (DpsConnection cnn = new DpsConnection(ConnectionString))
                {
                    string sqlq = $@"";
                    #region Lấy dữ liệu account từ JeeAccount
                    DataAccount = JeeWorkLiteController.GetAccountFromJeeAccount(HttpContext.Request.Headers, _configuration);
                    if (DataAccount == null)
                        return JsonResultCommon.Custom("Lỗi lấy danh sách nhân viên từ hệ thống quản lý tài khoản");
                    #endregion
                    if (query.filter != null && query.filter.keys != null)
                    {
                        sqlq += @"select list.rowid, list.title, actionname, event.title as event_name
                                , list.description, list.createddate, list.createdby, '' as nguoitao_fullname
                                , list.listid, list.status, list.eventid, list.condition, list.actionid
                                , list.data, list.departmentid
                                from automationlist list 
                                join automation_eventlist event
                                on event.rowid = list.eventid
                                join automation_actionlist action
                                on action.rowid = list.actionid
                                where list.disabled = 0 ";
                        if ( !string.IsNullOrEmpty(query.filter["departmentid"]))
                            sqlq += $" and ( departmentid in (select id_row from we_department where Disabled = 0 and (id_row = {query.filter["departmentid"]} or id_row in (select ParentID from we_department where Disabled = 0 and id_row = {query.filter["departmentid"]})))";
                        if (!string.IsNullOrEmpty(query.filter["listid"]))
                        {
                            sqlq += " or listid=" + query.filter["listid"]+")";
                        }
                        else
                        {
                            sqlq += ")";
                        }
                        sqlq += " order by list.rowid";
                    }
                    DataTable dt = new DataTable();
                    if (sqlq != "")
                    {
                        dt = cnn.CreateDataTable(sqlq);
                        if (cnn.LastError != null || dt == null)
                            return JsonResultCommon.Exception(_logger, cnn.LastError, _config, loginData, ControllerContext);
                        #region Map info account từ JeeAccount
                        if (dt.Rows.Count > 0)
                        {
                            foreach (DataRow item in dt.Rows)
                            {
                                var infoNguoiTao = DataAccount.Where(x => item["CreatedBy"].ToString().Contains(x.UserId.ToString())).FirstOrDefault();
                                if (infoNguoiTao != null)
                                {
                                    item["nguoitao_fullname"] = infoNguoiTao.FullName;
                                }
                            }
                        }
                        #endregion
                    }
                    var data = from r in dt.AsEnumerable()
                               select new
                               {
                                   rowid = r["rowid"],
                                   title = r["title"],
                                   description = r["description"],
                                   nguoitao_fullname = r["nguoitao_fullname"],
                                   createddate = r["createddate"],
                                   createdby = r["createdby"],
                                   listid = r["listid"],
                                   event_name = r["event_name"],
                                   actionname = r["actionname"],
                                   status = r["status"],
                                   eventid = r["eventid"],
                                   condition = auto_get_condition_by_event(long.Parse(r["rowid"].ToString()), r["condition"].ToString(), loginData.CustomerID),
                                   actionid = r["actionid"],
                                   data = r["data"],
                                   departmentid = r["departmentid"],
                                   data_actions = auto_get_action(long.Parse(r["rowid"].ToString()), loginData, DataAccount)
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
        /// 
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        [Route("add-automation")]
        [HttpPost]
        public async Task<object> Add_Automation(List<AutomationListModel> data)
        {
            UserJWT loginData = Ulities.GetUserByHeader(HttpContext.Request.Headers);
            if (loginData == null)
                return JsonResultCommon.DangNhap();
            try
            {

                string strRe = "";
                string error = "";
                //if (string.IsNullOrEmpty(data.title))
                //    strRe += (strRe == "" ? "" : ",") + "tên chức năng thực thi tự động";
                if (strRe != "")
                    return JsonResultCommon.BatBuoc(strRe);
                string ConnectionString = JeeWorkLiteController.getConnectionString(ConnectionCache, loginData.CustomerID, _configuration);
                using (DpsConnection cnn = new DpsConnection(ConnectionString))
                {
                    cnn.BeginTransaction();

                    foreach (var item in data)
                    {
                        bool isInsert = insertAutomation(item, loginData, cnn, out error);
                        if(!isInsert || !string.IsNullOrEmpty(error))
                        {
                            return JsonResultCommon.Custom(error);
                        }
                    }
                    
                    cnn.EndTransaction();

                    foreach (var item in data)
                    {
                        #region Ghi log trong project
                        string LogContent = "", LogEditContent = "";
                        LogContent = LogEditContent = $"Thêm automation {item.title} vào : {item.departmentid} ";
                        Common.Ghilogfile(loginData.CustomerID.ToString(), LogEditContent, LogContent, loginData.Username, ControllerContext);
                        #endregion 
                        #region Ghi log lên CDN
                        var d2 = new ActivityLog()
                        {
                            username = loginData.Username,
                            category = LogContent,
                            action = loginData.customdata.personalInfo.Fullname + " thao tác",
                            data = JsonConvert.SerializeObject(item)
                        };
                        _logger.LogInformation(JsonConvert.SerializeObject(d2));
                        #endregion
                    }


                    return JsonResultCommon.ThanhCong(data);
                }
            }
            catch (Exception ex)
            {
                return JsonResultCommon.Exception(_logger, ex, _config, loginData);
            }
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        [Route("delete/{id}")]
        [HttpGet]
        public async Task<object> DeleteAutomation(long id)
        {
            UserJWT loginData = Ulities.GetUserByHeader(HttpContext.Request.Headers);
            if (loginData == null)
                return JsonResultCommon.DangNhap();
            try
            { 
                string ConnectionString = JeeWorkLiteController.getConnectionString(ConnectionCache, loginData.CustomerID, _configuration);
                using (DpsConnection cnn = new DpsConnection(ConnectionString))
                {
                    cnn.BeginTransaction();

                    string sqlu = "update AutomationList set Disabled = 1 where RowID = " + id;
                    if(cnn.ExecuteNonQuery(sqlu) != 1)
                    {
                        cnn.RollbackTransaction();
                        return JsonResultCommon.Exception(_logger, cnn.LastError, _config, loginData, ControllerContext);
                    }
                    
                    
                    cnn.EndTransaction();
                    #region Ghi log trong project
                    string LogContent = "", LogEditContent = "";
                    LogContent = LogEditContent = $" xóa automation {id}. ";
                    Common.Ghilogfile(loginData.CustomerID.ToString(), LogEditContent, LogContent, loginData.Username, ControllerContext);
                    #endregion
                    #region Ghi log lên CDN
                    var d2 = new ActivityLog()
                    {
                        username = loginData.Username,
                        category = LogContent,
                        action = loginData.customdata.personalInfo.Fullname + " thao tác",
                        data = JsonConvert.SerializeObject(new { id = id})
                    };
                    _logger.LogInformation(JsonConvert.SerializeObject(d2));
                    #endregion
                    return JsonResultCommon.ThanhCong(id);
                }
            }
            catch (Exception ex)
            {
                return JsonResultCommon.Exception(_logger, ex, _config, loginData);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        [Route("update-automation")]
        [HttpPost]
        public async Task<object> UpdateAutomation(AutomationListModel data)
        {
            UserJWT loginData = Ulities.GetUserByHeader(HttpContext.Request.Headers);
            if (loginData == null)
                return JsonResultCommon.DangNhap();
            try
            {
                string ConnectionString = JeeWorkLiteController.getConnectionString(ConnectionCache, loginData.CustomerID, _configuration);
                using (DpsConnection cnn = new DpsConnection(ConnectionString))
                {
                    SqlConditions cond = new SqlConditions();
                    cond.Add("rowid",data.rowid);
                    cond.Add("Disabled", 0);
                    string sqlq = @"select * from AutomationList where RowID = @rowid and Disabled = @Disabled";
                    DataTable dta = cnn.CreateDataTable(sqlq,cond);
                    if(cnn.LastError!=null || dta.Rows.Count == 0)
                    {
                        return JsonResultCommon.KhongTonTai("chức năng tự động");
                    }
                    cnn.BeginTransaction();

                    long iduser = loginData.UserID;
                    long idk = loginData.CustomerID;
                    Hashtable val = new Hashtable();
                    val.Add("title", data.title);
                    if (!string.IsNullOrEmpty(data.description))
                        val.Add("description", data.description);

                    if (!string.IsNullOrEmpty(data.departmentid))
                        val.Add("departmentid", data.departmentid);

                    if (!string.IsNullOrEmpty(data.listid))
                        val.Add("listid", data.listid);

                    //val.Add("UpdatedDate", Common.GetDateTime());
                    //val.Add("UpdatedBy", iduser);
                    val.Add("status", data.status);
                    // event
                    val.Add("eventid", data.eventid);
                    if (!string.IsNullOrEmpty(data.condition))
                        val.Add("condition", data.condition);
                    // action
                    val.Add("actionid", data.actionid);
                    if (!string.IsNullOrEmpty(data.data))
                        val.Add("data", data.data);
                    //string strCheck = "select count(*) from automationlist where disabled=0 and title=@name";
                    //if (int.Parse(cnn.ExecuteScalar(strCheck, new SqlConditions() { { "name", data.title } }).ToString()) > 0)
                    //{
                    //    return JsonResultCommon.Custom("Chức năng tự động đã tồn tại trong danh sách");
                    //}

                    if (cnn.Update(val,cond, "automationlist") != 1)
                    {
                        cnn.RollbackTransaction();
                        return JsonResultCommon.Exception(_logger, cnn.LastError, _config, loginData, ControllerContext);
                    }
                    //long max_auto = long.Parse(cnn.ExecuteScalar("select IDENT_CURRENT('automationlist')").ToString());
                    cnn.ExecuteNonQuery("delete Automation_SubAction where AutoID = "+data.rowid);
                    if (data.subaction != null)
                    {
                        Hashtable has = new Hashtable();
                        foreach (var sub in data.subaction)
                        {
                            has["autoid"] = data.rowid;
                            has["subactionid"] = sub.subactionid;
                            has["value"] = sub.value;
                            if (cnn.Insert(has, "automation_subaction") != 1)
                            {
                                cnn.RollbackTransaction();
                                return JsonResultCommon.Exception(_logger, cnn.LastError, _config, loginData, ControllerContext);
                            }
                        }
                    }
                    //cnn.ExecuteNonQuery("delete Automation_SubAction where AutoID = " + data.rowid);
                    if (data.task != null)
                    {
                        Hashtable has = new Hashtable();
                        has["autoid"] = data.rowid;
                        foreach (var _t in data.task)
                        {
                            has["title"] = _t.title;
                            if (!string.IsNullOrEmpty(_t.description))
                                has["description"] = _t.description;
                            has["id_project_team"] = _t.id_project_team;
                            has["id_group"] = _t.id_group;
                            has["deadline"] = _t.deadline;
                            if (_t.id_parent > 0)
                                has["id_parent"] = _t.id_parent;
                            //else
                            //    has["id_parent"] = DBNull.Value;
                            has["start_date"] = _t.start_date;
                            //if (_t.startdate_type == "3")
                            //{
                            //    has["start_date"] = _t.start_date;
                            //}
                            //if (_t.deadline_type == "3")
                            //{
                            //    has["deadline"] = _t.deadline;
                            //}
                            has["status"] = _t.status;
                            has["startdate_type"] = _t.startdate_type;
                            has["deadline_type"] = _t.deadline_type;
                            has["priority"] = _t.priority;
                            if (cnn.Insert(has, "automation_task") != 1)
                            {
                                cnn.RollbackTransaction();
                                return JsonResultCommon.Exception(_logger, cnn.LastError, _config, loginData, ControllerContext);
                            }
                            long max_task = long.Parse(cnn.ExecuteScalar("select IDENT_CURRENT('automation_task')").ToString());
                            if (_t.users != null)
                            {
                                Hashtable val1 = new Hashtable();
                                val1["taskid"] = max_task;
                                foreach (var user in _t.users)
                                {
                                    val1["id_user"] = user.id_user;
                                    val1["loai"] = user.loai;
                                    if (cnn.Insert(val1, "automation_task_user") != 1)
                                    {
                                        cnn.RollbackTransaction();
                                        return JsonResultCommon.Exception(_logger, cnn.LastError, _config, loginData, ControllerContext);
                                    }
                                }
                            }
                            if (_t.tags != null)
                            {
                                Hashtable val1 = new Hashtable();
                                val1["taskid"] = max_task;
                                val1.Add("createddate", Common.GetDateTime());
                                foreach (var _tag in _t.tags)
                                {
                                    val1["tagid"] = _tag.id_tag;
                                    if (cnn.Insert(val1, "automation_task_tag") != 1)
                                    {
                                        cnn.RollbackTransaction();
                                        return JsonResultCommon.Exception(_logger, cnn.LastError, _config, loginData, ControllerContext);
                                    }
                                }
                            }
                        }
                    }
                    if (!JeeWorkLiteController.log(_logger, loginData.Username, cnn, 52, data.rowid, iduser, data.title))
                    {
                        cnn.RollbackTransaction();
                        return JsonResultCommon.Exception(_logger, cnn.LastError, _config, loginData, ControllerContext);
                    }
                    cnn.EndTransaction();

                    #region Ghi log trong project
                    string LogContent = "", LogEditContent = "";
                    LogContent = LogEditContent = $"Chỉnh sửa automation {data.title} của : {data.departmentid} ";
                    Common.Ghilogfile(loginData.CustomerID.ToString(), LogEditContent, LogContent, loginData.Username, ControllerContext);
                    #endregion
                    #region Ghi log lên CDN
                    var d2 = new ActivityLog()
                    {
                        username = loginData.Username,
                        category = LogContent,
                        action = loginData.customdata.personalInfo.Fullname + " thao tác",
                        data = JsonConvert.SerializeObject(data)
                    };
                    _logger.LogInformation(JsonConvert.SerializeObject(d2));
                    #endregion
                    return JsonResultCommon.ThanhCong(data);
                }
            }
            catch (Exception ex)
            {
                return JsonResultCommon.Exception(_logger, ex, _config, loginData);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        [Route("updatestatus-automation")]
        [HttpGet]
        public async Task<object> UpdateStatusAutomation(long rowid)
        {
            UserJWT loginData = Ulities.GetUserByHeader(HttpContext.Request.Headers);
            if (loginData == null)
                return JsonResultCommon.DangNhap();
            try
            {
                string ConnectionString = JeeWorkLiteController.getConnectionString(ConnectionCache, loginData.CustomerID, _configuration);
                using (DpsConnection cnn = new DpsConnection(ConnectionString))
                {
                    SqlConditions cond = new SqlConditions();
                    cond.Add("rowid",rowid);
                    cond.Add("Disabled", 0);
                    string sqlq = @"select * from AutomationList where RowID = @rowid and Disabled = @Disabled";
                    DataTable dta = cnn.CreateDataTable(sqlq,cond);
                    if(cnn.LastError!=null || dta.Rows.Count == 0)
                    {
                        return JsonResultCommon.KhongTonTai("chức năng tự động");
                    }
                    cnn.BeginTransaction();
                    var status = dta.Rows[0]["status"].ToString();
                    Hashtable val = new Hashtable();
                    
                    if(status == "1")
                    {
                        val.Add("status", 0);
                    }
                    else
                    {
                        val.Add("status", 1);
                    }

                    if (cnn.Update(val, cond, "automationlist") != 1)
                    {
                        cnn.RollbackTransaction();
                        return JsonResultCommon.Exception(_logger, cnn.LastError, _config, loginData, ControllerContext);
                    }

                    if (!JeeWorkLiteController.log(_logger, loginData.Username, cnn, 52, rowid, loginData.UserID))
                    {
                        cnn.RollbackTransaction();
                        return JsonResultCommon.Exception(_logger, cnn.LastError, _config, loginData, ControllerContext);
                    }
                    cnn.EndTransaction();
                    #region Ghi log trong project
                    string LogContent = "", LogEditContent = "";
                    LogContent = LogEditContent = $"Chỉnh sửa status automation {rowid} ";
                    Common.Ghilogfile(loginData.CustomerID.ToString(), LogEditContent, LogContent, loginData.Username, ControllerContext);
                    #endregion
                    #region Ghi log lên CDN
                    var d2 = new ActivityLog()
                    {
                        username = loginData.Username,
                        category = LogContent,
                        action = loginData.customdata.personalInfo.Fullname + " thao tác",
                        data = JsonConvert.SerializeObject(new { rowid= rowid })
                    };
                    _logger.LogInformation(JsonConvert.SerializeObject(d2));
                    #endregion
                    return JsonResultCommon.ThanhCong(rowid);
                }
            }
            catch (Exception ex)
            {
                return JsonResultCommon.Exception(_logger, ex, _config, loginData);
            }
        }
        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        [Route("list-task-parent")]
        [HttpGet]
        public object List_Task_Auto(long id,bool isDeparment)
        {
            UserJWT loginData = Ulities.GetUserByHeader(HttpContext.Request.Headers);
            if (loginData == null)
                return JsonResultCommon.DangNhap();
            try
            {
                #region Lấy dữ liệu account từ JeeAccount
                DataAccount = JeeWorkLiteController.GetAccountFromJeeAccount(HttpContext.Request.Headers, _configuration);
                if (DataAccount == null)
                    return JsonResultCommon.Custom("Lỗi lấy danh sách nhân viên từ hệ thống quản lý tài khoản");
                string err = "";
                if (id < 1)
                {
                    if (isDeparment)
                    {
                        err = "lấy dữ liệu phòng ban";
                    }
                    else
                    {
                        err = "lấy dữ liệu dự án";
                    }
                }
                string listID = JeeWorkLiteController.ListAccount(HttpContext.Request.Headers, out err, _configuration);
                if (err != "")
                    return JsonResultCommon.Custom(err);
                #endregion
                string ConnectionString = JeeWorkLiteController.getConnectionString(ConnectionCache, loginData.CustomerID, _configuration);
                using (DpsConnection cnn = new DpsConnection(ConnectionString))
                {
                    long IDdepartment = 0;
                    if (isDeparment)
                    {
                        IDdepartment = long.Parse(cnn.ExecuteScalar($@"select top 1 id_row from we_department where Disabled = 0 and ParentID is null and 
(id_row = {id} or ( id_row = (select ParentID from we_department where Disabled = 0 and ParentID is not null and id_row = {id})) )").ToString());
                    }
                    else
                    {
                        IDdepartment = long.Parse(cnn.ExecuteScalar($@"select top 1 id_row from we_department where Disabled = 0 and ParentID is null and 
(id_row = (select id_department from we_project_team where id_row = {id})
 or ( id_row = (select ParentID from we_department where Disabled = 0 and ParentID is not null 
 and id_row = (select id_department from we_project_team where id_row = {id}))))").ToString());
                    }
                    
                    string sql_space = "", sql_project = "", sql_folder = "", where_department = "",sql_task="";
                    //if (v_module.ToLower().Equals("module = 'wework'"))
                    //{
                    where_department = @$" disabled = 0 and CreatedBy in ({listID}) 
                                        and IdKH = {loginData.CustomerID} and (id_row in (select id_department from we_project_team 
                                        where (id_row in (select id_project_team from we_project_team_user where id_user = { loginData.UserID}
                                        and Disabled = 0) or (CreatedBy = { loginData.UserID})) and disabled = 0) or (CreatedBy = { loginData.UserID}));";
                    sql_space = @$"select id_row, title, id_cocau, IdKH, priority, disabled, ParentID
                                        from we_department
                                        where ParentID is null and id_row = {IDdepartment} and " + where_department + "";
                    sql_project = "select p.id_row, p.icon, p.title, p.detail, p.id_department" +
                        ", p.loai, p.start_date, p.end_date, p.color, p.template, p.status, p.is_project" +
                        ", p.priority, p.CreatedDate, p.CreatedBy, p.Locked, p.Disabled, default_view " +
                        "from we_project_team p where" +
                        $" p.Disabled = 0 and id_department in (select id_row from we_department where Disabled = 0 and id_row = {IDdepartment} or ParentID = {IDdepartment}) and p.CreatedBy in ({listID})";
                    //}
                    sql_folder = @$"select id_row, title, id_cocau, IdKH, priority, disabled, ParentID 
                                        from we_department
                                        where ParentID is not null and ParentID = {IDdepartment} and " + where_department + "";
                    sql_task = @$"select w.*,st.color,st.StatusName from v_wework_clickup_new w
left join we_status st on w.status = st.id_row 
where w.Disabled = 0 and id_parent is null and  w.id_department in (select id_row from we_department where Disabled = 0 and id_row = {IDdepartment} or ParentID = {IDdepartment})";
                    DataTable dt_space = cnn.CreateDataTable(sql_space);
                    DataTable dt_project = cnn.CreateDataTable(sql_project);
                    DataTable dt_folder = cnn.CreateDataTable(sql_folder);
                    DataTable dt_task = cnn.CreateDataTable(sql_task);
                    if (cnn.LastError != null)
                        return JsonResultCommon.Exception(_logger, cnn.LastError, _config, loginData, ControllerContext);
                    var data = from r in dt_space.AsEnumerable()
                               select new
                               {
                                   id_row = r["id_row"],
                                   title = r["title"],
                                   type = 1,
                                   folder = from f in dt_folder.AsEnumerable()
                                            where int.Parse(r["id_row"].ToString()) == int.Parse(f["ParentID"].ToString())
                                            select new
                                            {
                                                id_row = f["id_row"],
                                                title = f["title"],
                                                type = 2,
                                                project = from p in dt_project.AsEnumerable()
                                                          where int.Parse(f["id_row"].ToString()) == int.Parse(p["id_department"].ToString())
                                                          select new
                                                          {
                                                              id_row = p["id_row"],
                                                              title = p["title"],
                                                              type = 3,
                                                              dataTask = from t in dt_task.AsEnumerable()
                                                                         where int.Parse(p["id_row"].ToString()) == int.Parse(t["id_project_team"].ToString())
                                                                         select new
                                                                         {
                                                                             id_row = t["id_row"],
                                                                             title = t["title"],
                                                                             description = t["description"],
                                                                             status = t["status"],
                                                                             color = t["color"],
                                                                             StatusName = t["StatusName"],
                                                                             type = 4,
                                                                         }
                                                          }
                                            },
                                   project = from p in dt_project.AsEnumerable()
                                             where int.Parse(r["id_row"].ToString()) == int.Parse(p["id_department"].ToString())
                                             select new
                                             {
                                                 id_row = p["id_row"],
                                                 title = p["title"],
                                                 type = 3,
                                                 dataTask = from t in dt_task.AsEnumerable()
                                                            where int.Parse(p["id_row"].ToString()) == int.Parse(t["id_project_team"].ToString())
                                                            select new
                                                            {
                                                                id_row = t["id_row"],
                                                                title = t["title"],
                                                                description = t["description"],
                                                                status = t["status"],
                                                                color = t["color"],
                                                                StatusName = t["StatusName"],
                                                                type = 4,
                                                            }
                                             }
                               };
                    return JsonResultCommon.ThanhCong(data);
                }
            }
            catch (Exception ex)
            {
                return JsonResultCommon.Exception(_logger, ex, _config, loginData);
            }
        }
        private object auto_get_action(long autoid, UserJWT loginData, List<AccUsernameModel> DataAccount)
        {
            List<object> result = new List<object>();
            if (autoid == 0)
                return null;
            string ConnectionString = JeeWorkLiteController.getConnectionString(ConnectionCache, loginData.CustomerID, _configuration);
            using (DpsConnection cnn = new DpsConnection(ConnectionString))
            {
                DataTable dt = new DataTable();
                long actionid = 0;
                DataTable dt_detail = new DataTable();
                string sqlq_automation = "", sql_data = "";
                sqlq_automation = "select rowid, title, description, listid, status, eventid" +
                                    ", condition, actionid, data, departmentid " +
                                    "from AutomationList where disabled = 0 and rowid = " + autoid;
                dt_detail = cnn.CreateDataTable(sqlq_automation);
                if (cnn.LastError != null || dt_detail == null)
                    return null;
                actionid = long.Parse(dt_detail.Rows[0]["actionid"].ToString());
                sql_data = "select rowid, autoid, subactionid, value " +
                            "from automation_subaction " +
                            "where autoid = " + autoid + "";
                dt = cnn.CreateDataTable(sql_data);
                if (cnn.LastError != null || dt == null)
                    return null;
                switch (actionid)
                {
                    case 1:
                    case 7:
                        foreach (DataRow item in dt.Rows)
                        {
                            result.Add(new { id = item["rowid"], value = item["Value"], actionid = item["subactionid"] });
                        }
                        break;
                    case 4:
                    case 8:
                    case 9:
                    case 10:
                    case 11:
                    case 12:
                    case 13:
                    case 14:
                        result.Add(new { actionid = actionid, value = dt_detail.Rows[0]["data"].ToString() });
                        break;
                    case 5:
                    case 6:
                        break;
                    case 2:
                    case 3:
                        string where_str = " where autoid = " + autoid + "";
                        sql_data = "select rowid, title, description, id_project_team, id_group" +
                            ", deadline, id_parent, start_date, status, priority" +
                            ", startdate_type, deadline_type, autoid " +
                            "from automation_task " + where_str + "  order by RowID desc";
                        sql_data += ";select id_row, taskid, id_user, loai, '' as hoten from automation_task_user " +
                            "where taskid in (select rowid from automation_task " + where_str + ")";
                        sql_data += ";select tagid, taskid from automation_task_tag " +
                                    "where taskid in (select rowid from automation_task " + where_str + ")";
                        DataSet ds_task = cnn.CreateDataSet(sql_data);
                        if (cnn.LastError != null || ds_task == null)
                        {
                            return JsonResultCommon.Exception(_logger, cnn.LastError, _config, loginData, ControllerContext);
                        }
                        if (ds_task.Tables[0] == null || ds_task.Tables[0].Rows.Count == 0)
                            return JsonResultCommon.KhongTonTai();
                        if (ds_task.Tables.Count == 3)
                        {
                            foreach (DataRow item in ds_task.Tables[1].Rows)
                            {
                                var info_users = DataAccount.Where(x => item["id_user"].ToString().Contains(x.UserId.ToString())).FirstOrDefault();
                                if (info_users != null)
                                {
                                    item["hoten"] = info_users.FullName;
                                }
                            }
                            var data_task = (from r in ds_task.Tables[0].AsEnumerable()
                                            //where r["autoid"].ToString() == autoid.ToString()
                                        select new
                                        {
                                            rowid = r["rowid"],
                                            id_group = r["id_group"],
                                            title = r["title"],
                                            description = r["description"],
                                            id_project_team = r["id_project_team"],
                                            priority = r["priority"],
                                            status = r["status"],
                                            startdate_type = r["startdate_type"],
                                            deadline_type = r["deadline_type"],
                                            id_parent = r["id_parent"],
                                            //deadline = r["deadline"] == DBNull.Value ? "" : string.Format("{0:dd/MM/yyyy HH:mm}", r["deadline"]),
                                            //start_date = r["start_date"] == DBNull.Value ? "" : string.Format("{0:dd/MM/yyyy HH:mm}", r["start_date"]),
                                            deadline = r["deadline"],
                                            start_date = r["start_date"],
                                            users = from f in ds_task.Tables[1].AsEnumerable()
                                                    select new
                                                    {
                                                        id_user = f["id_user"],
                                                        hoten = f["hoten"],
                                                        taskid = f["taskid"],
                                                    },
                                            tags = from t in ds_task.Tables[2].AsEnumerable()
                                                   select new
                                                   {
                                                       taskid = t["taskid"],
                                                       tagid = t["tagid"]
                                                   },
                                        }).FirstOrDefault();
                            result.Add(new { data_task = data_task });
                        }
                        else
                            result = new List<object>();
                        break;
                    default:
                        break;
                }
                cnn.Disconnect();
                return result;
            }
        }
        private object auto_get_condition_by_event(long autoid, string condition, long CustomerID)
        {
            List<object> result = new List<object>();

            if (autoid == 0)
                return null;
            string ConnectionString = JeeWorkLiteController.getConnectionString(ConnectionCache, CustomerID, _configuration);
            using (DpsConnection cnn = new DpsConnection(ConnectionString))
            {
                DataTable dt = new DataTable();
                long eventid = 0;
                DataTable dt_detail = new DataTable();
                string sqlq_automation = "", sql_data = "";
                sqlq_automation = "select rowid, title, description, listid, status, eventid" +
                                    ", condition, actionid, data, departmentid " +
                                    "from automationlist where disabled = 0 and rowid =" + autoid;
                dt_detail = cnn.CreateDataTable(sqlq_automation);
                if (cnn.LastError != null || dt_detail == null)
                    return null;
                eventid = long.Parse(dt_detail.Rows[0]["eventid"].ToString());
                sql_data = "select rowid, autoid, subactionid, value " +
                            "from automation_subaction " +
                            "where autoid = " + autoid + "";
                dt = cnn.CreateDataTable(sql_data);
                if (cnn.LastError != null || dt == null)
                    return null;
                switch (eventid)
                {
                    case 1: // Lưu Condition theo định dang From:x,y;To:z,k. Trong đó x,y,z,k là statusid (để trống là any)
                    case 2:
                        {
                            string[] conditions = condition.Split(';');
                            if (conditions.Length == 2)
                            {
                                string from = conditions[0].Replace("From:", "");
                                string to = conditions[1].Replace("To:", "");
                                result.Add(new { from = from.Trim(), to = to.Trim() });
                            }
                            //sql_data = "";
                        }
                        break;
                    case 3: // không truyền gì hết
                    case 4:
                    case 7:
                    case 10:
                    case 11:
                        break;
                    case 5:
                    case 6:
                    case 8:
                        result.Add(new { list = condition }); // 56610,16110
                        break;
                    case 12:
                    case 13:
                        result.Add(new { list = condition });
                        break;
                    default:
                        break;
                }
                cnn.Disconnect();
                return result;
            }
        }
        private bool insertAutomation(AutomationListModel data, UserJWT loginData, DpsConnection cnn, out string error)
        {
            error = "";
            long iduser = loginData.UserID;
            long idk = loginData.CustomerID;
            Hashtable val = new Hashtable();
            val.Add("title", data.title);
            if (!string.IsNullOrEmpty(data.description))
                val.Add("description", data.description);
            else
                val.Add("description", DBNull.Value);

            if (!string.IsNullOrEmpty(data.departmentid))
                val.Add("departmentid", data.departmentid);
            else
                val.Add("departmentid", DBNull.Value);

            if (!string.IsNullOrEmpty(data.listid))
                val.Add("listid", data.listid);
            else
                val.Add("listid", DBNull.Value);
            val.Add("createddate", Common.GetDateTime());
            val.Add("createdby", iduser);
            val.Add("status", 1);
            // event
            val.Add("eventid", data.eventid);
            if (!string.IsNullOrEmpty(data.condition))
                val.Add("condition", data.condition);
            else
                val.Add("condition", DBNull.Value);
            // action
            val.Add("actionid", data.actionid);
            if (!string.IsNullOrEmpty(data.data))
                val.Add("data", data.data);
            else
                val.Add("data", DBNull.Value);
            //string strCheck = "select count(*) from automationlist where disabled=0 and title=@name";
            //if (int.Parse(cnn.ExecuteScalar(strCheck, new SqlConditions() { { "name", data.title } }).ToString()) > 0)
            //{
            //    error = "Chức năng tự động đã tồn tại trong danh sách";
            //    return false;
            //}
            if (cnn.Insert(val, "automationlist") != 1)
            {
                cnn.RollbackTransaction();
                error = "Lỗi truy xuất dữ liệu";
                return false;
            }
            long max_auto = long.Parse(cnn.ExecuteScalar("select IDENT_CURRENT('automationlist')").ToString());
            if (data.subaction != null)
            {
                Hashtable has = new Hashtable();
                foreach (var sub in data.subaction)
                {
                    has["autoid"] = max_auto;
                    has["subactionid"] = sub.subactionid;
                    has["value"] = sub.value;
                    if (cnn.Insert(has, "automation_subaction") != 1)
                    {
                        cnn.RollbackTransaction();
                        error = "Lỗi truy xuất dữ liệu";
                        return false;
                    }
                }
            }
            if (data.task != null)
            {
                Hashtable has = new Hashtable();
                has["autoid"] = max_auto;
                foreach (var _t in data.task)
                {
                    has["title"] = _t.title;
                    if (!string.IsNullOrEmpty(_t.description))
                        has["description"] = _t.description;
                    has["id_project_team"] = _t.id_project_team;
                    has["id_group"] = _t.id_group;
                    if (_t.id_parent > 0)
                        has["id_parent"] = _t.id_parent;
                    //else
                    //    has["id_parent"] = DBNull.Value;
                    if (!string.IsNullOrEmpty(_t.start_date))
                    {
                        has["start_date"] = _t.start_date;
                    }
                    if (!string.IsNullOrEmpty(_t.deadline))
                    {
                        has["deadline"] = _t.deadline;
                    }
                    has["status"] = _t.status;
                    has["startdate_type"] = _t.startdate_type;
                    has["deadline_type"] = _t.deadline_type;
                    has["priority"] = _t.priority;
                    if (cnn.Insert(has, "automation_task") != 1)
                    {
                        cnn.RollbackTransaction();
                        error = "Lỗi truy xuất dữ liệu";
                        return false;
                    }
                    long max_task = long.Parse(cnn.ExecuteScalar("select IDENT_CURRENT('automation_task')").ToString());
                    if (_t.users != null)
                    {
                        Hashtable val1 = new Hashtable();
                        val1["taskid"] = max_task;
                        foreach (var user in _t.users)
                        {
                            val1["id_user"] = user.id_user;
                            val1["loai"] = user.loai;
                            if (cnn.Insert(val1, "automation_task_user") != 1)
                            {
                                cnn.RollbackTransaction();
                                error = "Lỗi truy xuất dữ liệu";
                                return false;
                            }
                        }
                    }
                    if (_t.tags != null)
                    {
                        Hashtable val1 = new Hashtable();
                        val1["taskid"] = max_task;
                        val1.Add("createddate", Common.GetDateTime());
                        foreach (var _tag in _t.tags)
                        {
                            val1["tagid"] = _tag.id_tag;
                            if (cnn.Insert(val1, "automation_task_tag") != 1)
                            {
                                cnn.RollbackTransaction();
                                error = cnn.LastError.Message;
                                return false;
                            }
                        }
                    }
                }
            }
            if (!JeeWorkLiteController.log(_logger, loginData.Username, cnn, 52, max_auto, iduser, data.title))
            {
                cnn.RollbackTransaction();
                error = "Lỗi truy xuất dữ liệu";
                return false;
            }
            return true;
        }

    }
}