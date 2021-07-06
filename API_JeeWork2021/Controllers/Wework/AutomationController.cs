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
        private readonly ILogger<TemplateController> _logger;
        public List<AccUsernameModel> DataAccount;
        public AutomationController(IOptions<JeeWorkConfig> config, IHostingEnvironment hostingEnvironment, IConnectionCache _cache, IConfiguration configuration, ILogger<TemplateController> logger)
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
                string ConnectionString = WeworkLiteController.getConnectionString(ConnectionCache, loginData.CustomerID, _configuration);
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
        public object Get_AutomationList([FromQuery] FilterModel filter)
        {

            UserJWT loginData = Ulities.GetUserByHeader(HttpContext.Request.Headers);
            if (loginData == null)
                return JsonResultCommon.DangNhap();
            try
            {
                string ConnectionString = WeworkLiteController.getConnectionString(ConnectionCache, loginData.CustomerID, _configuration);
                using (DpsConnection cnn = new DpsConnection(ConnectionString))
                {
                    string sqlq = $@"";
                    #region Lấy dữ liệu account từ JeeAccount
                    DataAccount = WeworkLiteController.GetAccountFromJeeAccount(HttpContext.Request.Headers, _configuration);
                    if (DataAccount == null)
                        return JsonResultCommon.Custom("Lỗi lấy danh sách nhân viên từ hệ thống quản lý tài khoản");
                    #endregion
                    if (filter != null && filter.keys != null)
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
                        if (filter.keys.Contains("listid") && !string.IsNullOrEmpty(filter["listid"]))
                            sqlq += " and listid=" + filter["listid"];
                        if (filter.keys.Contains("departmentid") && !string.IsNullOrEmpty(filter["departmentid"]))
                            sqlq += " and departmentid=" + filter["departmentid"];
                        sqlq += " order by list.title";
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
        /// Detail (id của template trong bảng we_template_customer)
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [Route("Detail")]
        [HttpGet]
        public object Detail(long id)
        {
            string Token = Common.GetHeader(Request);
            UserJWT loginData = Ulities.GetUserByHeader(HttpContext.Request.Headers);
            if (loginData == null)
                return JsonResultCommon.DangNhap();
            try
            {
                #region Lấy dữ liệu account từ JeeAccount
                DataAccount = WeworkLiteController.GetAccountFromJeeAccount(HttpContext.Request.Headers, _configuration);
                if (DataAccount == null)
                    return JsonResultCommon.Custom("Lỗi lấy danh sách nhân viên từ hệ thống quản lý tài khoản");

                string error = "";
                string listID = WeworkLiteController.ListAccount(HttpContext.Request.Headers, out error, _configuration);
                if (error != "")
                    return JsonResultCommon.Custom(error);
                #endregion
                PageModel pageModel = new PageModel();
                string domain = _configuration.GetValue<string>("Host:JeeWork_API") + "/";
                string ConnectionString = WeworkLiteController.getConnectionString(ConnectionCache, loginData.CustomerID, _configuration);
                //bool Visible = Common.CheckRoleByToken(Token, "3403", ConnectionString, DataAccount);
                bool Visible = true;
                using (DpsConnection cnn = new DpsConnection(ConnectionString))
                {
                    #region Trả dữ liệu về backend để hiển thị lên giao diện
                    string sqlq = @$"select id_row, title, description, createddate, createdby
                                    , isdefault, color, id_department, templateid, customerid
                                    , is_template_center, types, levels, viewid, group_statusid 
                                    , template_typeid, img_temp, field_id, share_with, sample_id
                                    from we_template_customer 
                                    where is_template_center = 1 and id_row=" + id;
                    string list_viewid = "", group_statusid = "", field_id = "";
                    DataTable dt_Detail = new DataTable();
                    dt_Detail = cnn.CreateDataTable(sqlq);
                    list_viewid = dt_Detail.Rows[0]["viewid"].ToString();
                    group_statusid = dt_Detail.Rows[0]["group_statusid"].ToString();
                    field_id = dt_Detail.Rows[0]["field_id"].ToString();
                    sqlq += @$";select id_row, view_name, description, is_default, icon, link, image, templateid 
                                from  we_default_views 
                                where id_row in (" + list_viewid + ") " +
                                "order by is_default desc";
                    sqlq += @$";select id_field, fieldname, title, type, position, isdefault, typeid
                            from we_fields 
                            where isNewField = 1 and IsDel = 0 and isvisible = 0 
                            and id_field in (" + field_id + ") " +
                            "order by position, title";
                    sqlq += @$";select id_row, title, description, locked, array_status 
                                from we_status_group 
                                where id_row in (" + group_statusid + ") " +
                                "order by title";
                    sqlq += @$";select distinct id_user, '' as hoten, '' as id_user,'' as image,'' as username,'' as mobile,'' as Email,'' as tenchucdanh
from we_template_library where disabled = 0 and id_template = " + id;
                    DataSet ds = cnn.CreateDataSet(sqlq);
                    if (cnn.LastError != null || ds == null)
                    {
                        return JsonResultCommon.Exception(_logger, cnn.LastError, _config, loginData, ControllerContext);
                    }
                    #region Map info account từ JeeAccount

                    foreach (DataRow item in ds.Tables[4].Rows)
                    {
                        var info = DataAccount.Where(x => item["id_user"].ToString().Contains(x.UserId.ToString())).FirstOrDefault();
                        if (info != null)
                        {
                            item["hoten"] = info.FullName;
                            item["image"] = info.AvartarImgURL;
                            item["username"] = info.Username;
                            item["mobile"] = info.PhoneNumber;
                            item["Email"] = info.Email;
                            item["tenchucdanh"] = info.Jobtitle;
                        }
                    }
                    #endregion
                    if (ds.Tables[0] == null || ds.Tables[0].Rows.Count == 0)
                        return JsonResultCommon.KhongTonTai();
                    ds.Tables[3].Columns.Add("DataStatus", typeof(DataTable));
                    if (ds.Tables[3].Rows.Count > 0)
                    {
                        foreach (DataRow dr in ds.Tables[3].Rows)
                        {
                            dr["DataStatus"] = dtChildren(dr["array_status"].ToString(), cnn);
                        }
                    }
                    var data = (from r in ds.Tables[0].AsEnumerable()
                                select new
                                {
                                    id_row = r["id_row"],
                                    title = r["title"],
                                    description = r["description"],
                                    isdefault = r["isdefault"],
                                    createddate = string.Format("{0:dd/MM/yyyy HH:mm}", r["createddate"]),
                                    createdby = r["createdby"],
                                    color = r["color"],
                                    templateid = r["templateid"],
                                    customerid = r["customerid"],
                                    is_template_center = r["is_template_center"],
                                    types = r["types"],
                                    levels = r["levels"],
                                    viewid = r["viewid"],
                                    group_statusid = r["group_statusid"],
                                    template_typeid = r["template_typeid"],
                                    img_temp = r["img_temp"],
                                    field_id = r["field_id"],
                                    share_with = r["share_with"],
                                    sample_id = r["sample_id"],
                                    data_views = from rr in ds.Tables[1].AsEnumerable()
                                                 select new
                                                 {
                                                     id_row = rr["id_row"],
                                                     view_name = rr["view_name"],
                                                     description = rr["description"],
                                                     is_default = rr["is_default"],
                                                     icon = rr["icon"],
                                                     link = rr["link"],
                                                     image = rr["image"],
                                                 },
                                    data_fields = from field in ds.Tables[2].AsEnumerable()
                                                  select new
                                                  {
                                                      id_field = field["id_field"],
                                                      fieldname = field["fieldname"],
                                                      title = field["title"],
                                                      type = field["type"],
                                                      position = field["position"],
                                                      isdefault = field["isdefault"],
                                                      typeid = field["typeid"],
                                                  },
                                    list_share = from share in ds.Tables[4].AsEnumerable()
                                                 select new
                                                 {
                                                     id_nv = share["id_user"],
                                                     hoten = share["hoten"],
                                                     image = share["image"],
                                                     username = share["username"],
                                                     mobile = share["mobile"],
                                                     Email = share["Email"],
                                                 },
                                    data_status = from status in ds.Tables[3].AsEnumerable()
                                                  select new
                                                  {
                                                      id_row = status["id_row"],
                                                      title = status["title"],
                                                      description = status["description"],
                                                      locked = status["locked"],
                                                      status_list = (((DataTable)status["DataStatus"]) != null
                                                      && ((DataTable)status["DataStatus"]).Rows.Count > 0) ?
                                                      from _status in ((DataTable)status["DataStatus"]).AsEnumerable()
                                                      select new
                                                      {
                                                          id_row = _status["id_row"],
                                                          statusname = _status["statusname"],
                                                          isdefault = _status["isdefault"],
                                                          color = _status["color"],
                                                          position = _status["position"],
                                                          isfinal = _status["isfinal"],
                                                          isdeadline = _status["isdeadline"],
                                                          istodo = _status["istodo"],
                                                      } : null,
                                                  }
                                }).FirstOrDefault();

                    return JsonResultCommon.ThanhCong(data, pageModel, Visible);
                }
                #endregion
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
        public async Task<object> Add_Automation(AutomationListModel data)
        {
            string Token = Common.GetHeader(Request);
            UserJWT loginData = Ulities.GetUserByHeader(HttpContext.Request.Headers);
            if (loginData == null)
                return JsonResultCommon.DangNhap();
            try
            {
                string strRe = "";
                string error = "";
                if (string.IsNullOrEmpty(data.title))
                    strRe += (strRe == "" ? "" : ",") + "tên chức năng thực thi tự động";
                if (strRe != "")
                    return JsonResultCommon.BatBuoc(strRe);
                string ConnectionString = WeworkLiteController.getConnectionString(ConnectionCache, loginData.CustomerID, _configuration);
                using (DpsConnection cnn = new DpsConnection(ConnectionString))
                {
                    cnn.BeginTransaction();

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
                    val.Add("createddate", DateTime.Now);
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
                    string strCheck = "select count(*) from automationlist where disabled=0 and title=@name";
                    if (int.Parse(cnn.ExecuteScalar(strCheck, new SqlConditions() { { "name", data.title } }).ToString()) > 0)
                    {
                        return JsonResultCommon.Custom("Chức năng tự động đã tồn tại trong danh sách");
                    }
                    if (cnn.Insert(val, "automationlist") != 1)
                    {
                        cnn.RollbackTransaction();
                        return JsonResultCommon.Exception(_logger, cnn.LastError, _config, loginData, ControllerContext);
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
                                return JsonResultCommon.Exception(_logger, cnn.LastError, _config, loginData, ControllerContext);
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
                            has["description"] = _t.description;
                            has["id_project_team"] = _t.id_project_team;
                            has["id_group"] = _t.id_group;
                            has["deadline"] = _t.deadline;
                            if (_t.id_parent > 0)
                                has["id_parent"] = _t.id_parent;
                            else
                                has["id_parent"] = DBNull.Value;
                            has["start_date"] = _t.start_date;
                            has["status"] = _t.status;
                            has["startdate_type"] = _t.startdate_type;
                            has["deadline_type"] = _t.deadline_type;
                            if (cnn.Insert(has, "automation_task") != 1)
                            {
                                cnn.RollbackTransaction();
                                return JsonResultCommon.Exception(_logger, cnn.LastError, _config, loginData, ControllerContext);
                            }
                            long max_task = long.Parse(cnn.ExecuteScalar("select IDENT_CURRENT('we_work')").ToString());
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
                                val.Add("createddate", DateTime.Now);
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
                    if (!WeworkLiteController.log(_logger, loginData.Username, cnn, 52, max_auto, iduser, data.title))
                    {
                        cnn.RollbackTransaction();
                        return JsonResultCommon.Exception(_logger, cnn.LastError, _config, loginData, ControllerContext);
                    }
                    cnn.EndTransaction();
                    return JsonResultCommon.ThanhCong(data);
                }
            }
            catch (Exception ex)
            {
                return JsonResultCommon.Exception(_logger, ex, _config, loginData);
            }
        }
        /// <summary>
        /// Cập nhật template center
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        [Route("update-template-center")]
        [HttpPost]
        public async Task<BaseModel<object>> update_template_center(TemplateCenterModel data)
        {
            string Token = Common.GetHeader(Request);
            UserJWT loginData = Ulities.GetUserByHeader(HttpContext.Request.Headers);
            if (loginData == null)
                return JsonResultCommon.DangNhap();
            try
            {
                Hashtable val = new Hashtable();
                SqlConditions sqlcond = new SqlConditions();
                long iduser = loginData.UserID;
                long idk = loginData.CustomerID;
                string strRe = "";
                if (string.IsNullOrEmpty(data.title))
                    strRe += (strRe == "" ? "" : ",") + " tên mẫu";
                if (strRe != "")
                    return JsonResultCommon.BatBuoc(strRe);
                string ConnectionString = WeworkLiteController.getConnectionString(ConnectionCache, loginData.CustomerID, _configuration);
                using (DpsConnection cnn = new DpsConnection(ConnectionString))
                {
                    string s = "";
                    if (data.id_row > 0)
                    {
                        sqlcond = new SqlConditions();
                        sqlcond.Add("id_row", data.id_row);
                        sqlcond.Add("disabled", 0);
                        sqlcond.Add("is_template_center", 1);
                        s = "select * from we_template_customer where (where)";
                        DataTable old = cnn.CreateDataTable(s, "(where)", sqlcond);
                        if (old == null || old.Rows.Count == 0)
                            return JsonResultCommon.KhongTonTai("Template");
                        val.Add("title", data.title);
                        if (!string.IsNullOrEmpty(data.description)) val.Add("description", data.description);
                        val.Add("field_id", data.field_id);
                        val.Add("share_with", data.share_with);
                        val.Add("updatedby", iduser);
                        val.Add("updateddate", DateTime.Now);
                        cnn.BeginTransaction();
                        if (cnn.Update(val, sqlcond, "we_template_customer") != 1)
                        {
                            cnn.RollbackTransaction();
                            return JsonResultCommon.Exception(_logger, cnn.LastError, _config, loginData, ControllerContext);
                        }
                        string ids = string.Join(",", data.list_share.Select(x => x.id_user));
                        // xóa tv k có trong danhsach => ktra tv có chưa > chưa thì insert
                        if (ids != "")//xóa thành viên
                        {
                            string strDel = "update we_template_library set disabled=1, updateddate=getdate(), updatedby=" + iduser + " where disabled=0 and id_template=" + data.id_row + " and id_user not in (" + ids + ")";
                            if (cnn.ExecuteNonQuery(strDel) < 0)
                            {
                                cnn.RollbackTransaction();
                                return JsonResultCommon.Exception(_logger, cnn.LastError, _config, loginData, ControllerContext);
                            }
                        }
                        Hashtable val1 = new Hashtable();
                        val1["id_template"] = data.id_row;
                        val1["createddate"] = DateTime.Now;
                        val1["createdby"] = iduser;
                        foreach (var owner in data.list_share)
                        {
                            if (owner.id_row == 0)
                            {
                                bool HasItem = long.Parse(cnn.ExecuteScalar($"select count(*) from we_template_library where disabled = 0 and id_template = {data.id_row} and id_user = {owner.id_user}").ToString()) > 0;
                                if (!HasItem)
                                {
                                    val1["id_user"] = owner.id_user;
                                    if (cnn.Insert(val1, "we_template_library") != 1)
                                    {
                                        cnn.RollbackTransaction();
                                        return JsonResultCommon.Exception(_logger, cnn.LastError, _config, loginData, ControllerContext);
                                    }
                                }
                            }
                        }
                        if (!WeworkLiteController.log(_logger, loginData.Username, cnn, 46, data.id_row, iduser))
                        {
                            cnn.RollbackTransaction();
                            return JsonResultCommon.Exception(_logger, cnn.LastError, _config, loginData, ControllerContext);
                        }
                    }
                    cnn.EndTransaction();
                    return JsonResultCommon.ThanhCong(data);
                }
            }
            catch (Exception ex)
            {
                return JsonResultCommon.Exception(_logger, ex, _config, loginData);
            }
        }
        /// <summary>
        /// DS loại template
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
                DataAccount = WeworkLiteController.GetAccountFromJeeAccount(HttpContext.Request.Headers, _configuration);
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
                string listID = WeworkLiteController.ListAccount(HttpContext.Request.Headers, out err, _configuration);
                if (err != "")
                    return JsonResultCommon.Custom(err);
                #endregion
                string ConnectionString = WeworkLiteController.getConnectionString(ConnectionCache, loginData.CustomerID, _configuration);
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
 and id_row = (select id_department from we_project_team where id_row = {id}))) )").ToString());
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
where w.Disabled = 0 and id_parent is null and  id_department in (select id_row from we_department where Disabled = 0 and id_row = {IDdepartment} or ParentID = {IDdepartment})";
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
        /// <summary>
        /// add user vào template library
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        [Route("add-template-library")]
        [HttpPost]
        public async Task<BaseModel<object>> add_template_library(add_template_library_Model data)
        {
            string Token = Common.GetHeader(Request);
            UserJWT loginData = Ulities.GetUserByHeader(HttpContext.Request.Headers);
            if (loginData == null)
                return JsonResultCommon.DangNhap();
            try
            {
                Hashtable val = new Hashtable();
                SqlConditions sqlcond = new SqlConditions();
                long iduser = loginData.UserID;
                long idk = loginData.CustomerID;
                string strRe = "";
                if (data.templateid <= 0)
                    strRe += (strRe == "" ? "" : ",") + " mẫu";
                if (strRe != "")
                    return JsonResultCommon.KhongTonTai(strRe);
                string ConnectionString = WeworkLiteController.getConnectionString(ConnectionCache, loginData.CustomerID, _configuration);
                using (DpsConnection cnn = new DpsConnection(ConnectionString))
                {
                    string s = "";
                    if (data.templateid > 0)
                    {
                        string sql_insert = "";
                        sql_insert = $@"insert into we_template_customer (title, description, createdDate, createdby, disabled, isdefault, color, id_department, templateID, customerid)
                        select title, description, getdate(), " + loginData.UserID + ", 0, isdefault, color, 0, id_row, " + loginData.CustomerID + " as CustomerID from we_template_list where Disabled = 0 and id_row = " + data.templateid + "";
                        cnn.ExecuteNonQuery(sql_insert);
                        if (cnn.LastError != null)
                        {
                            cnn.RollbackTransaction();
                            return JsonResultCommon.Exception(_logger, cnn.LastError, _config, loginData, ControllerContext);
                        }
                        sqlcond = new SqlConditions();
                        sqlcond.Add("id_row", data.templateid);
                        sqlcond.Add("disabled", 0);
                        sqlcond.Add("is_template_center", 1);
                        s = "select * from we_template_customer where (where)";
                        DataTable old = cnn.CreateDataTable(s, "(where)", sqlcond);
                        if (old == null || old.Rows.Count == 0)
                            return JsonResultCommon.KhongTonTai("Template");
                        cnn.BeginTransaction();
                        string ids = string.Join(",", data.list_share.Select(x => x.id_user));
                        // xóa tv k có trong danhsach => ktra tv có chưa > chưa thì insert
                        if (ids != "")//xóa thành viên
                        {
                            string strDel = "update we_template_library set disabled=1, updateddate=getdate(), updatedby=" + iduser + " where disabled=0 and id_template=" + data.templateid + " and id_user not in (" + ids + ")";
                            if (cnn.ExecuteNonQuery(strDel) < 0)
                            {
                                cnn.RollbackTransaction();
                                return JsonResultCommon.Exception(_logger, cnn.LastError, _config, loginData, ControllerContext);
                            }
                        }
                        Hashtable val1 = new Hashtable();
                        val1["id_template"] = data.templateid;
                        val1["createddate"] = DateTime.Now;
                        val1["createdby"] = iduser;
                        foreach (var owner in data.list_share)
                        {
                            if (owner.id_row == 0)
                            {
                                bool HasItem = long.Parse(cnn.ExecuteScalar($"select count(*) from we_template_library where disabled = 0 and id_template = {data.templateid} and id_user = {owner.id_user}").ToString()) > 0;
                                if (!HasItem)
                                {
                                    val1["id_user"] = owner.id_user;
                                    if (cnn.Insert(val1, "we_template_library") != 1)
                                    {
                                        cnn.RollbackTransaction();
                                        return JsonResultCommon.Exception(_logger, cnn.LastError, _config, loginData, ControllerContext);
                                    }
                                }
                            }
                        }
                        if (!WeworkLiteController.log(_logger, loginData.Username, cnn, 46, data.templateid, iduser))
                        {
                            cnn.RollbackTransaction();
                            return JsonResultCommon.Exception(_logger, cnn.LastError, _config, loginData, ControllerContext);
                        }
                    }
                    cnn.EndTransaction();
                    return JsonResultCommon.ThanhCong(data);
                }
            }
            catch (Exception ex)
            {
                return JsonResultCommon.Exception(_logger, ex, _config, loginData);
            }
        }
        private DataTable dtChildren(string array_status, DpsConnection cnn)
        {
            DataTable result = new DataTable();
            string sqlq = "select id_row, statusname, isdefault, color, position, isfinal, isdeadline, istodo " +
                "from we_status_list " +
                "where Disabled = 0 and isdefault = 1 and id_row in (" + array_status + ")" +
                "order by position, statusname";
            result = cnn.CreateDataTable(sqlq);
            if (result.Rows.Count > 0)

                return result;
            else
                return new DataTable();
        }
        private object auto_get_action(long autoid, UserJWT loginData, List<AccUsernameModel> DataAccount)
        {
            List<object> result = new List<object>();
            if (autoid == 0)
                return null;
            string ConnectionString = WeworkLiteController.getConnectionString(ConnectionCache, loginData.CustomerID, _configuration);
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
                            result.Add(new { id = item["rowid"], value = item["Value"], actionid = actionid });
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
                            "from automation_task " + where_str + "";
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
                                            deadline = r["deadline"] == DBNull.Value ? "" : string.Format("{0:dd/MM/yyyy HH:mm}", r["deadline"]),
                                            start_date = r["start_date"] == DBNull.Value ? "" : string.Format("{0:dd/MM/yyyy HH:mm}", r["start_date"]),
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
            string ConnectionString = WeworkLiteController.getConnectionString(ConnectionCache, CustomerID, _configuration);
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
    }
}