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
using DPSinfra.Notifier;

namespace JeeWork_Core2021.Controllers.Wework
{
    [ApiController]
    [Route("api/third-party")]
    [EnableCors("JeeWorkPolicy")]
    /// <summary>
    /// Quản lý các api cung cấp cho bên thứ 3
    /// </summary>
    // [EnableCors(origins: "*", headers: "*", methods: "*")]
    public class ThirdPartyController : ControllerBase
    {
        private readonly IHostingEnvironment _hostingEnvironment;
        private JeeWorkConfig _config;
        private INotifier _notifier;
        private IConnectionCache ConnectionCache;
        private IConfiguration _configuration;
        private readonly ILogger<ConfigNotifyController> _logger;
        public List<AccUsernameModel> DataAccount;
        public ThirdPartyController(IOptions<JeeWorkConfig> config, IHostingEnvironment hostingEnvironment, IConnectionCache _cache, IConfiguration configuration, ILogger<ConfigNotifyController> logger)
        {
            _hostingEnvironment = hostingEnvironment;
            _config = config.Value;
            ConnectionCache = _cache;
            _configuration = configuration;
            _logger = logger;
        }
        /// <summary>
        /// Lấy danh sách work space
        /// </summary>
        /// <returns></returns>
        [Route("get-work-space")]
        [HttpGet]
        public object GetListWorkSpace()
        {
            DataSet ds_workspace = new DataSet();
            UserJWT loginData = Ulities.GetUserByHeader(HttpContext.Request.Headers);
            if (loginData == null)
                return JsonResultCommon.DangNhap();
            try
            {
                string ConnectionString = WeworkLiteController.getConnectionString(ConnectionCache, loginData.CustomerID, _configuration);
                using (DpsConnection Conn = new DpsConnection(ConnectionString))
                {
                    #region Lấy dữ liệu account từ JeeAccount
                    DataAccount = WeworkLiteController.GetAccountFromJeeAccount(HttpContext.Request.Headers, _configuration);
                    if (DataAccount == null)
                        return JsonResultCommon.Custom("Lỗi lấy danh sách nhân viên từ hệ thống quản lý tài khoản");
                    string err = "";
                    string listID = WeworkLiteController.ListAccount(HttpContext.Request.Headers, out err, _configuration);
                    if (err != "")
                        return JsonResultCommon.Custom(err);
                    #endregion
                    Common permit = new Common(ConnectionString);
                    ds_workspace = Common.GetWorkSpace(loginData, 0, 0, ConnectionString);
                }
                if (ds_workspace != null)
                {
                    var workspace = from r in ds_workspace.Tables[0].AsEnumerable()
                                    orderby r["title"]
                                    select new
                                    {
                                        id_row = r["id_row"],
                                        title = r["title"],
                                        icon = "flaticon-signs-1",
                                        priority = r["priority"],
                                        isfolder = false,
                                        type = 1,
                                        parentowner = r["parentowner"],
                                        owner = r["owner"],
                                        list = from r2 in ds_workspace.Tables[2].AsEnumerable()
                                               where r2["id_department"].ToString() == r["id_row"].ToString()
                                               select new
                                               {
                                                   id_row = r2["id_row"],
                                                   title = r2["Title"],
                                                   locked = r2["Locked"],
                                                   color = r2["color"],
                                                   status = r2["Status"],
                                                   default_view = r2["default_view"],
                                                   is_project = r2["is_project"],
                                                   type = 3,
                                                   parentowner = r["parentowner"],
                                                   owner = r["owner"],
                                                   admin_project = r2["admin_project"],
                                               },
                                        folder = from r3 in ds_workspace.Tables[1].AsEnumerable()
                                                 where r3["ParentID"].ToString() == r["id_row"].ToString()
                                                 orderby r3["Title"]
                                                 select new
                                                 {
                                                     id_row = r3["id_row"],
                                                     title = r3["title"],
                                                     icon = "flaticon-folder",
                                                     priority = r3["priority"],
                                                     type = 2,
                                                     isfolder = true,
                                                     owner = r3["owner"],
                                                     parentowner = r3["parentowner"],
                                                     list = from r4 in ds_workspace.Tables[2].AsEnumerable()
                                                            where r4["id_department"].ToString() == r3["id_row"].ToString()
                                                            select new
                                                            {
                                                                id_row = r4["id_row"],
                                                                title = r4["title"],
                                                                locked = r4["locked"],
                                                                color = r4["color"],
                                                                status = r4["status"],
                                                                default_view = r4["Default_View"],
                                                                type = 3,
                                                                is_project = r4["is_project"],
                                                                owner = r3["owner"],
                                                                parentowner = r3["parentowner"],
                                                                admin_project = r4["admin_project"],
                                                            },
                                                 },
                                    };
                    return JsonResultCommon.ThanhCong(workspace);
                }
                else
                {
                    return JsonResultCommon.KhongHopLe("Dữ liệu workspace không đúng chuẩn");
                }
            }
            catch (Exception ex)
            {
                return JsonResultCommon.Exception(_logger, ex, _config, loginData);
            }
        }

        /// <summary>
        /// Cung cấp API Tạo dự án từ JeeMeeting
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        /// 
        [Route("generate-projects-auto")]
        [HttpPost]
        public async Task<object> Generate_Projects([FromBody] GenerateProjectAutoModel data)
        {
            UserJWT loginData = Ulities.GetUserByHeader(HttpContext.Request.Headers);
            if (loginData == null)
                return JsonResultCommon.DangNhap();
            try
            {
                string ConnectionString = WeworkLiteController.getConnectionString(ConnectionCache, loginData.CustomerID, _configuration);
                using (DpsConnection cnn = new DpsConnection(ConnectionString))
                {
                    #region Lấy dữ liệu account từ JeeAccount
                    DataAccount = WeworkLiteController.GetAccountFromJeeAccount(HttpContext.Request.Headers, _configuration);
                    if (DataAccount == null)
                        return JsonResultCommon.Custom("Lỗi lấy danh sách nhân viên từ hệ thống quản lý tài khoản");
                    #endregion
                    //string strCheck = @$"select id_row, id_department, id_template, meetingid 
                    //                        from we_project_team where (where)";
                    SqlConditions conds = new SqlConditions();
                    //conds.Add("meetingid", data.meetingid);
                    //conds.Add("disabled", 0);
                    string strCheck = @$"select id_row, idkh, phanloaiid from we_department where (where)";
                    strCheck = @"select * from we_project_team where meetingid is not null 
                                and id_department in (select id_row from we_department where idkh = " + loginData.CustomerID + ")";
                    conds.Add("idkh", loginData.CustomerID);
                    //conds.Add("id_meeting", data.meetingid);
                    DataTable dt_check = cnn.CreateDataTable(strCheck);
                    if (cnn.LastError != null || dt_check == null)
                    {
                        cnn.RollbackTransaction();
                        return JsonResultCommon.Exception(_logger, cnn.LastError, _config, loginData, ControllerContext);
                    }
                    string error = "", departmentid = "0";
                    if (dt_check.Rows.Count == 0) // Chưa có dự án trong cuộc họp ==> Khởi tạo phòng ban => dự án
                    {
                        if (WeworkLiteController.init_space(cnn, loginData, data, out error))
                        {
                            departmentid = cnn.ExecuteScalar("select IDENT_CURRENT('we_department')").ToString();
                        }
                    }
                    else
                        departmentid = dt_check.Rows[0]["id_department"].ToString();
                    data.id_department = long.Parse(departmentid);
                    Hashtable val = new Hashtable();
                    val.Add("title", data.title);
                    val.Add("id_department", departmentid);
                    val.Add("loai", data.loai);
                    val.Add("meetingid", data.meetingid);
                    val.Add("CreatedDate", Common.GetDateTime());
                    val.Add("CreatedBy", loginData.UserID);
                    strCheck = "select count(*) from we_project_team where Disabled=0 and (id_department=@id_department) and title=@name";
                    if (int.Parse(cnn.ExecuteScalar(strCheck, new SqlConditions() { { "id_department", data.id_department }, { "name", data.title } }).ToString()) > 0)
                    {
                        return JsonResultCommon.Trung("Dự án/phòng ban");
                    }
                    cnn.BeginTransaction();
                    if (cnn.Insert(val, "we_project_team") != 1)
                    {
                        cnn.RollbackTransaction();
                        return JsonResultCommon.Exception(_logger, cnn.LastError, _config, loginData, ControllerContext);
                    }
                    long idc = long.Parse(cnn.ExecuteScalar("select IDENT_CURRENT('we_project_team')").ToString());
                    data.listid = idc;
                    //DataTable dt_member = new DataTable();
                    // insert thành viên
                    val = new Hashtable();
                    val["id_project_team"] = idc;
                    val["createddate"] = Common.GetDateTime();
                    val["createdby"] = loginData.UserID;
                    foreach (var owner in data.Users)
                    {
                        val["id_user"] = owner.id_user;
                        val["admin"] = owner.admin;
                        if (cnn.Insert(val, "we_project_team_user") != 1)
                        {
                            cnn.RollbackTransaction();
                            return JsonResultCommon.Exception(_logger, cnn.LastError, _config, loginData, ControllerContext);
                        }
                    }
                    string sql_insert = "";
                    var list_roles = new List<long> { 1, 11 };
                    if (!WeworkLiteController.Init_RoleDefault(idc, list_roles, cnn))
                    {
                        cnn.RollbackTransaction();
                        return JsonResultCommon.Exception(_logger, cnn.LastError, _config, loginData, ControllerContext);
                    }
                    #region Khởi tạo view mặc định
                    if (!WeworkLiteController.Init_DefaultView_Project(idc, cnn))
                    {
                        cnn.RollbackTransaction();
                        return JsonResultCommon.Exception(_logger, cnn.LastError, _config, loginData, ControllerContext);
                    }
                    #endregion
                    // Tạo status mặc định cho project này dựa vào id_department
                    long soluongstatus = long.Parse(cnn.ExecuteScalar("select count(id_row) from we_status where disabled = 0 and id_department = " + departmentid).ToString());
                    if (soluongstatus > 0)
                    {
                        string insertSTT = $@"insert into we_status (StatusName, description,id_project_team, id_department, CreatedDate, CreatedBy, Disabled,   Type, IsDefault, color, Position, IsFinal, Follower, IsDeadline, IsToDo, StatusID_Reference)
                        select StatusName, description,{idc}, 0, GETUTCDATE(), { loginData.UserID}, Disabled,   Type, IsDefault, color, Position, IsFinal, Follower, IsDeadline, IsToDo, StatusID_Reference from we_status where Disabled = 0 and id_department = " + departmentid + "";
                        cnn.ExecuteNonQuery(insertSTT);
                        if (cnn.LastError != null)
                        {
                            cnn.RollbackTransaction();
                            return JsonResultCommon.Exception(_logger, cnn.LastError, _config, loginData, ControllerContext);
                        }
                    }
                    else
                    {
                        long TemplateID = long.Parse(cnn.ExecuteScalar(@$"select iIf(TemplateID is not null,TemplateID,0) from we_department where id_row = (select id_department from we_project_team where id_row = {idc})").ToString());
                        if (TemplateID > 0)
                        {
                            sql_insert = $@"update we_project_team set id_template = " + TemplateID + " where id_row = " + idc;
                            sql_insert += $@";insert into we_status (StatusName, description, id_project_team, CreatedDate, CreatedBy, Disabled, UpdatedDate, UpdatedBy, Type, IsDefault, color, Position, IsFinal, Follower, IsDeadline, IsToDo, StatusID_Reference)
                        select StatusName, description, " + idc + ", CreatedDate, CreatedBy, Disabled, UpdatedDate, UpdatedBy, Type, IsDefault, color, Position, IsFinal, Follower, IsDeadline, IsToDo, id_row from we_template_status where Disabled = 0 and TemplateID = " + TemplateID + "";
                            cnn.ExecuteNonQuery(sql_insert);
                            if (cnn.LastError != null)
                            {
                                cnn.RollbackTransaction();
                                return JsonResultCommon.Exception(_logger, cnn.LastError, _config, loginData, ControllerContext);
                            }
                            WeworkLiteController.update_position_status(idc, cnn, "id_project_team");
                        }
                        else
                        {
                            // Tạo status mặc định cho project này nếu department không có templateID thì lấy mẫu template đầu tiên của customerid đó
                            conds = new SqlConditions(); string sql = "";
                            conds.Add("Disabled", 0);
                            conds.Add("is_template_center", 0);
                            conds.Add("CustomerID", loginData.CustomerID);
                            sql = "select id_row, Title, Description, IsDefault, Color, id_department, TemplateID, CustomerID " +
                                "from we_template_customer " +
                                "where (where) ";
                            DataTable dt_template = cnn.CreateDataTable(sql, "(where)", conds);
                            if (cnn.LastError == null && dt_template.Rows.Count > 0)
                            {
                                TemplateID = long.Parse(dt_template.Rows[0]["id_row"].ToString());
                                sql_insert = "";
                                sql_insert = $@"update we_project_team set id_template = " + TemplateID + " where id_row = " + idc;
                                sql_insert += $@";insert into we_status (StatusName, description, id_project_team, CreatedDate, CreatedBy, Disabled, UpdatedDate, UpdatedBy, Type, IsDefault, color, Position, IsFinal, Follower, IsDeadline, IsToDo, StatusID_Reference)
                        select StatusName, description, " + idc + ", CreatedDate, CreatedBy, Disabled, UpdatedDate, UpdatedBy, Type, IsDefault, color, Position, IsFinal, Follower, IsDeadline, IsToDo, id_row from we_template_status where Disabled = 0 and TemplateID = " + TemplateID + "";
                                cnn.ExecuteNonQuery(sql_insert);
                                if (cnn.LastError != null)
                                {
                                    cnn.RollbackTransaction();
                                    return JsonResultCommon.Exception(_logger, cnn.LastError, _config, loginData, ControllerContext);
                                }
                                WeworkLiteController.update_position_status(idc, cnn, "id_project_team");
                            }
                        }
                    }
                    WeworkLiteController.update_position_status(idc, cnn, "id_project_team");
                    #region Khởi tạo các cột hiển thị mặc định cho công việc
                    if (!WeworkLiteController.Insert_field_project_team(idc, cnn))
                    {
                        cnn.RollbackTransaction();
                        return JsonResultCommon.Exception(_logger, cnn.LastError, _config, loginData, ControllerContext);
                    }
                    #endregion
                    if (!WeworkLiteController.log(_logger, loginData.Username, cnn, 31, idc, loginData.UserID, data.title))
                    {
                        cnn.RollbackTransaction();
                        return JsonResultCommon.Exception(_logger, cnn.LastError, _config, loginData, ControllerContext);
                    }
                    cnn.EndTransaction();
                    data.listid = idc;
                    Hashtable has_replace = new Hashtable();
                    var users_admin = new List<long> { loginData.UserID };
                    #region Lấy thông tin để thông báo
                    SendNotifyModel noti = WeworkLiteController.GetInfoNotify(6, ConnectionString);
                    #endregion
                    WeworkLiteController.mailthongbao(idc, users_admin, 6, loginData, ConnectionString, _notifier, _configuration);//thiết lập vai trò admin
                    #region Notify thiết lập vai trò admin
                    for (int i = 0; i < users_admin.Count; i++)
                    {
                        NotifyModel notify_model = new NotifyModel();
                        has_replace = new Hashtable();
                        has_replace.Add("nguoigui", loginData.Username);
                        has_replace.Add("project_team", "Cuộc họp");
                        notify_model.AppCode = "WORK";
                        notify_model.From_IDNV = loginData.UserID.ToString();
                        notify_model.To_IDNV = users_admin[i].ToString();
                        notify_model.TitleLanguageKey = LocalizationUtility.GetBackendMessage("ww_thietlapvaitroadmin", "", "vi");
                        notify_model.TitleLanguageKey = notify_model.TitleLanguageKey.Replace("$nguoigui$", loginData.customdata.personalInfo.Fullname);
                        notify_model.TitleLanguageKey = notify_model.TitleLanguageKey.Replace("$project_team$", "Cuộc họp");
                        notify_model.TitleLanguageKey = notify_model.TitleLanguageKey.Replace("$loai$", "dự án");
                        notify_model.ReplaceData = has_replace;
                        notify_model.To_Link_MobileApp = noti.link_mobileapp.Replace("$id$", data.listid.ToString());
                        notify_model.To_Link_WebApp = noti.link.Replace("$id$", data.listid.ToString());
                        var info = DataAccount.Where(x => notify_model.To_IDNV.ToString().Contains(x.UserId.ToString())).FirstOrDefault();
                        if (info is not null)
                        {
                            bool kq_noti = WeworkLiteController.SendNotify(loginData.Username, info.Username, notify_model, _notifier, _configuration);
                        }
                    }
                    #endregion
                    #region Thông báo cho member
                    noti = WeworkLiteController.GetInfoNotify(5, ConnectionString);
                    List<long> users_member = data.Users.Where(x => x.id_row == 0 && !x.admin).Select(x => x.id_user).ToList();
                    #endregion
                    WeworkLiteController.mailthongbao(idc, users_member, 5, loginData, ConnectionString, _notifier, _configuration);//thêm vào dự án
                    #region Notify thiết lập vai trò member
                    for (int i = 0; i < users_member.Count; i++)
                    {
                        NotifyModel notify_model = new NotifyModel();
                        has_replace = new Hashtable();
                        has_replace.Add("nguoigui", loginData.Username);
                        has_replace.Add("project_team", data.title);
                        notify_model.AppCode = "WORK";
                        notify_model.From_IDNV = loginData.UserID.ToString();
                        notify_model.To_IDNV = data.Users[i].id_user.ToString();
                        notify_model.TitleLanguageKey = "ww_thietlapvaitrothanhvien";
                        notify_model.TitleLanguageKey = LocalizationUtility.GetBackendMessage("ww_thietlapvaitrothanhvien", "", "vi");
                        notify_model.TitleLanguageKey = notify_model.TitleLanguageKey.Replace("$nguoigui$", loginData.customdata.personalInfo.Fullname);
                        notify_model.TitleLanguageKey = notify_model.TitleLanguageKey.Replace("$project_team$", data.title);
                        notify_model.TitleLanguageKey = notify_model.TitleLanguageKey.Replace("$loai$", "dự án");
                        notify_model.ReplaceData = has_replace;
                        notify_model.To_Link_MobileApp = noti.link_mobileapp.Replace("$id$", data.listid.ToString());
                        notify_model.To_Link_WebApp = noti.link.Replace("$id$", data.listid.ToString());
                        var info = DataAccount.Where(x => data.Users[i].id_user.ToString().Contains(x.UserId.ToString())).FirstOrDefault();
                        if (info is not null)
                        {
                            bool kq_noti = WeworkLiteController.SendNotify(loginData.Username, info.Username, notify_model, _notifier, _configuration);
                        }
                    }
                    #endregion
                    return JsonResultCommon.ThanhCong(data);
                }
            }
            catch (Exception ex)
            {
                return JsonResultCommon.Exception(_logger, ex, _config, loginData);
            }
        }


        #region Các API liên quan đến Template center (Cung cấp cho WorkFlow)
        /// <summary>
        /// GetListTemplateCenter
        /// </summary>
        /// <param name="query"></param>
        /// <returns></returns>
        [Route("get-list-template-center")]
        [HttpGet]
        public object GetListTemplateCenter([FromQuery] QueryParams query)
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
                            { "title", "title"},
                        };
                    SqlConditions cond = new SqlConditions();
                    string where_template = "";
                    string where_type = "";
                    string types = "", levels = "", template_typeid = "", collect_by = "";
                    #region Filter
                    if (!string.IsNullOrEmpty(query.filter["keyword"]))
                    {
                        where_template += " and (title like N'%@keyword%') ";
                        where_template = where_template.Replace("@keyword", query.filter["keyword"]);
                    }
                    if (!string.IsNullOrEmpty(query.filter["template_typeid"]))//API: WeworkLiteController.lite_template_types
                    {
                        template_typeid = query.filter["template_typeid"];
                        where_template += $" and list.template_typeid in ({template_typeid})";
                        where_type += $" and id_row in ({template_typeid})";
                    }
                    if (!string.IsNullOrEmpty(query.filter["types"]))//1 - space, 2 - folder, 3 - list (Project)
                    {
                        types = query.filter["types"];
                        where_template += $" and list.types in ({types})"; // 1,2,3 || 1 || 1,3
                    }
                    if (!string.IsNullOrEmpty(query.filter["levels"]))//1 - Beginner, 2 - Intermediate, 3 - Advanced
                    {
                        levels = query.filter["levels"];
                        where_template += $" and list.levels in ({levels})"; // 1,2,3 || 1 || 1,3
                    }
                    if (!string.IsNullOrEmpty(query.filter["collect_by"]))//Người tạo (Table: we_template_customer)
                    {
                        collect_by = query.filter["collect_by"];
                        where_template += $" and list.createdby in ({collect_by})";
                    }
                    #endregion
                    #region Trả dữ liệu về backend để hiển thị lên giao diện
                    string sqlq = @"select id_row, title, isdefault, types
                                    from we_template_types 
                                    where disabled = 0  " + where_type;
                    sqlq += @$";select id_row, title, description, isdefault, color
                                    , is_template_center, types, levels, img_temp, share_with, sample_id
                                    , viewid, group_statusid, template_typeid, field_id
                                    from we_template_customer list
                                    where disabled = 0 and is_template_center = 1 and id_row in (select id_template from we_template_library where id_user = {loginData.UserID})" + where_template;
                    sqlq += @";select id_row, title, description, isdefault, color
                                    , is_template_center, types, levels, img_temp, share_with, sample_id
                                    , viewid, group_statusid, template_typeid, field_id
                                    from we_template_list list
                                    where disabled = 0 and is_template_center = 1 " + where_template;
                    DataSet ds = cnn.CreateDataSet(sqlq, cond);
                    if (cnn.LastError != null || ds == null)
                        return JsonResultCommon.Exception(_logger, cnn.LastError, _config, loginData, ControllerContext);
                    var data = from r in ds.Tables[0].AsEnumerable()
                               select new
                               {
                                   id_row = r["id_row"],
                                   title = r["title"],
                                   isdefault = r["isdefault"],
                                   types = r["types"],
                                   data_template = from s in ds.Tables[1].AsEnumerable()
                                                   where s["template_typeid"].ToString() == r["id_row"].ToString()
                                                   select new
                                                   {
                                                       id_row = s["id_row"],
                                                       title = s["title"],
                                                       description = s["description"],
                                                       isdefault = s["isdefault"],
                                                       color = s["color"],
                                                       is_template_center = s["is_template_center"],
                                                       types = s["types"],
                                                       levels = s["levels"],
                                                       viewid = s["viewid"],
                                                       group_statusid = s["group_statusid"],
                                                       template_typeid = s["template_typeid"],
                                                       img_temp = s["img_temp"],
                                                       field_id = s["field_id"],
                                                       share_with = s["share_with"],
                                                       sample_id = s["sample_id"]
                                                   },
                                   data_template_default = from d in ds.Tables[2].AsEnumerable()
                                                           where d["template_typeid"].ToString() == r["id_row"].ToString()
                                                           select new
                                                           {
                                                               id_row = d["id_row"],
                                                               title = d["title"],
                                                               description = d["description"],
                                                               isdefault = d["isdefault"],
                                                               color = d["color"],
                                                               is_template_center = d["is_template_center"],
                                                               types = d["types"],
                                                               levels = d["levels"],
                                                               viewid = d["viewid"],
                                                               group_statusid = d["group_statusid"],
                                                               template_typeid = d["template_typeid"],
                                                               img_temp = d["img_temp"],
                                                               field_id = d["field_id"],
                                                               share_with = d["share_with"],
                                                               sample_id = d["sample_id"]
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
        /// GetListTemplateCenter
        /// </summary>
        /// <param name="query"></param>
        /// <returns></returns>
        [Route("get-template-by-user")]
        [HttpGet]
        public object GetTemplate_ByUser([FromQuery] QueryParams query)
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
                            { "title", "title"},
                        };
                    SqlConditions cond = new SqlConditions();
                    string where_template = "";
                    string collect_by = "";
                    #region Filter
                    if (!string.IsNullOrEmpty(query.filter["keyword"]))
                    {
                        where_template += " and (title like N'%@keyword%') ";
                        where_template = where_template.Replace("@keyword", query.filter["keyword"]);
                    }
                    if (string.IsNullOrEmpty(query.filter["collect_by"]))// Nếu không truyền mặc định lấy người tạo OR list_share có người tạo
                    {
                        collect_by = loginData.UserID.ToString();
                        //where_template += $" and (list.createdby = " + loginData.UserID + " " +
                        //    "or (list_share like '" + loginData.UserID + "%' " +
                        //    "or list_share like '%" + loginData.UserID + "')) ";
                    }
                    else
                    {
                        collect_by = query.filter["collect_by"];
                    }
                    where_template += $" and (list.id_row in (select id_template from we_template_library where disabled = 0 and id_user =" + collect_by + "))";

                    #endregion
                    #region Trả dữ liệu về backend để hiển thị lên giao diện
                    string sqlq = "";
                    sqlq += @"select id_row, title, description, isdefault, color
                                    , is_template_center, types, levels, img_temp
                                    , viewid, group_statusid, template_typeid
                                    , field_id, sample_id, share_with
                                    from we_template_customer list
                                    where disabled = 0 
                                    and is_template_center = 1 " + where_template + " " +
                                    "order by title";
                    string sqlListShare = "select * from we_template_library where disabled = 0";
                    DataTable dtShare = cnn.CreateDataTable(sqlListShare, cond);
                    DataTable dt = cnn.CreateDataTable(sqlq, cond);

                    if (cnn.LastError != null || dt == null)
                        return JsonResultCommon.Exception(_logger, cnn.LastError, _config, loginData, ControllerContext);
                    if (dt.Rows.Count == 0)
                        return JsonResultCommon.ThanhCong(new List<string>());
                    var data = from r in dt.AsEnumerable()
                               select new
                               {
                                   id_row = r["id_row"],
                                   title = r["title"],
                                   description = r["description"],
                                   isdefault = r["isdefault"],
                                   color = r["color"],
                                   is_template_center = r["is_template_center"],
                                   types = r["types"],
                                   levels = r["levels"],
                                   img_temp = r["img_temp"],
                                   viewid = r["viewid"],
                                   field_id = r["field_id"],
                                   group_statusid = r["group_statusid"],
                                   template_typeid = r["template_typeid"],
                                   share_with = r["share_with"],
                                   sample_id = r["sample_id"],
                                   list_share = from s in dtShare.AsEnumerable()
                                                where r["id_row"].ToString().Equals(s["id_template"].ToString())
                                                select new
                                                {
                                                    id_user = s["id_user"],
                                                    id_row = s["id_row"],
                                                    id_template = s["id_template"],
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
        /// Detail (id của template trong bảng we_template_customer)
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [Route("detail")]
        [HttpGet]
        public object Detail(long id, bool istemplatelist = false)
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
                    string sqlq = @$"select id_row, title, description, createddate, createdby,addtolibrary,save_as_id
                                    , isdefault, color, id_department, templateid, customerid
                                    , is_template_center, types, levels, viewid, group_statusid 
                                    , template_typeid, img_temp, field_id, share_with, sample_id
                                    from we_template_customer 
                                    where is_template_center = 1 and id_row=" + id;
                    if (istemplatelist)
                    {
                        sqlq = @$"select id_row, title, description, createddate, createdby,'' as addtolibrary,save_as_id
                                    , isdefault, color, '' as id_department, '' as templateid, '{loginData.CustomerID}' as customerid
                                    , is_template_center, types, levels, viewid, group_statusid 
                                    , template_typeid, img_temp, field_id, share_with, sample_id
                                    from we_template_list 
                                    where is_template_center = 1 and id_row=" + id;
                    }
                    string list_viewid = "", group_statusid = "", field_id = "";
                    DataTable dt_Detail = new DataTable();
                    dt_Detail = cnn.CreateDataTable(sqlq);
                    if (dt_Detail.Rows.Count > 0)
                    {
                        list_viewid = dt_Detail.Rows[0]["viewid"].ToString();
                        group_statusid = dt_Detail.Rows[0]["group_statusid"].ToString();
                        field_id = dt_Detail.Rows[0]["field_id"].ToString();
                    }
                    if (string.IsNullOrEmpty(list_viewid)) list_viewid = "null";
                    if (string.IsNullOrEmpty(group_statusid)) group_statusid = "null";
                    if (string.IsNullOrEmpty(field_id)) field_id = "null";
                    sqlq += @$";select id_row, view_name, description, is_default, icon, link, image, templateid 
                                from  we_default_views 
                                where id_row in (" + list_viewid + ") " +
                                "order by is_default desc";
                    sqlq += @$";select id_field, fieldname, title, type, position, isdefault, typeid
                            from we_fields 
                            where IsDel = 0 and id_field in (" + field_id + ") " +
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
                                    addtolibrary = r["addtolibrary"],
                                    save_as_id = r["save_as_id"],
                                    istemplatelist = istemplatelist,
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
                                                     email = share["Email"],
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
                    #endregion
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
        [Route("save-as-template")]
        [HttpPost]
        public async Task<object> SaveAsTemplate(TemplateCenterModel data)
        {
            UserJWT loginData = Ulities.GetUserByHeader(HttpContext.Request.Headers);
            if (loginData == null)
                return JsonResultCommon.DangNhap();
            try
            {
                string strRe = "";
                string error = "";
                if (string.IsNullOrEmpty(data.title))
                    strRe += (strRe == "" ? "" : ",") + "tên mẫu";
                if (strRe != "")
                    return JsonResultCommon.BatBuoc(strRe);
                string ConnectionString = WeworkLiteController.getConnectionString(ConnectionCache, loginData.CustomerID, _configuration);
                using (DpsConnection cnn = new DpsConnection(ConnectionString))
                {
                    cnn.BeginTransaction();
                    if (!WeworkLiteController.init_save_as_new_template(cnn, data, loginData, out error))
                    {
                        cnn.RollbackTransaction();
                        return JsonResultCommon.Custom(error);
                    }
                    long sampleid = long.Parse(cnn.ExecuteScalar("select max(id_row) from we_sample_data where parentid is null").ToString());
                    long group_id = long.Parse(cnn.ExecuteScalar("select max(id_row) from we_status_group").ToString());
                    long iduser = loginData.UserID;
                    long idk = loginData.CustomerID;
                    string x = string.Join(",", data.list_field_name.Select(x => x.id_field));
                    if (!string.IsNullOrEmpty(x))
                    {
                        data.field_id = x;
                    }
                    Hashtable val = new Hashtable();
                    val.Add("title", data.title);
                    if (!string.IsNullOrEmpty(data.description))
                        val.Add("description", data.description);
                    else
                        val.Add("description", DBNull.Value);
                    val.Add("customerid", loginData.CustomerID);
                    val.Add("createddate", Common.GetDateTime());
                    val.Add("createdby", iduser);
                    val.Add("isdefault", 0);
                    val.Add("is_template_center", 1);
                    val.Add("types", data.types);
                    val.Add("levels", data.levels);
                    //val.Add("viewid", data.viewid);
                    val.Add("group_statusid", group_id);
                    val.Add("template_typeid", 1); // lấy mặc định trong we_template_types
                    if (!string.IsNullOrEmpty(data.img_temp))
                        val.Add("img_temp", data.img_temp);
                    else
                        val.Add("img_temp", DBNull.Value);
                    if (!string.IsNullOrEmpty(data.viewid))
                        val.Add("viewid", data.viewid);
                    else
                        val.Add("viewid", DBNull.Value);
                    if (!string.IsNullOrEmpty(data.field_id))
                        val.Add("field_id", data.field_id);
                    else
                        val.Add("field_id", DBNull.Value);
                    val.Add("share_with", data.share_with);
                    val.Add("sample_id", sampleid);
                    val.Add("save_as_id", data.save_as_id);
                    string strCheck = "select count(*) from we_template_customer where disabled=0 and (CustomerID=@customerid) and title=@name";
                    if (int.Parse(cnn.ExecuteScalar(strCheck, new SqlConditions() { { "customerid", data.customerid }, { "name", data.title } }).ToString()) > 0)
                    {
                        return JsonResultCommon.Custom("Mẫu đã tồn tại trong danh sách");
                    }
                    if (cnn.Insert(val, "we_template_customer") != 1)
                    {
                        cnn.RollbackTransaction();
                        return JsonResultCommon.Exception(_logger, cnn.LastError, _config, loginData, ControllerContext);
                    }
                    long idc = long.Parse(cnn.ExecuteScalar("select IDENT_CURRENT('we_template_customer')").ToString());
                    #region insert we_template_library
                    val = new Hashtable();
                    val.Add("id_template", idc);
                    val.Add("createddate", Common.GetDateTime());
                    val.Add("createdby", iduser);
                    val.Add("disabled", 0);
                    val.Add("id_user", loginData.UserID);
                    if (cnn.Insert(val, "we_template_library") != 1)
                    {
                        cnn.RollbackTransaction();
                        return JsonResultCommon.Exception(_logger, cnn.LastError, _config, loginData, ControllerContext);
                    }
                    #endregion
                    if (!WeworkLiteController.log(_logger, loginData.Username, cnn, 45, idc, iduser, data.title))
                    {
                        cnn.RollbackTransaction();
                        return JsonResultCommon.Exception(_logger, cnn.LastError, _config, loginData, ControllerContext);
                    }
                    #region insert data vào bảng tạm
                    if (!InsertDataToTemp(idc, data.types, long.Parse(data.save_as_id), loginData.UserID, cnn, out error))
                    {
                        return JsonResultCommon.Custom(error);
                    }
                    #endregion
                    data.id_row = idc;
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
        /// 
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        [Route("use-template")]
        [HttpPost]
        public async Task<object> SudungTemplate(TemplateCenterModel data, bool istemplatelist)
        {
            UserJWT loginData = Ulities.GetUserByHeader(HttpContext.Request.Headers);
            if (loginData == null)
                return JsonResultCommon.DangNhap();
            try
            {
                string strRe = "";
                string error = "";
                if (string.IsNullOrEmpty(data.title))
                    strRe += (strRe == "" ? "" : ",") + "tên mẫu";
                if (strRe != "")
                    return JsonResultCommon.BatBuoc(strRe);
                string ConnectionString = WeworkLiteController.getConnectionString(ConnectionCache, loginData.CustomerID, _configuration);
                using (DpsConnection cnn = new DpsConnection(ConnectionString))
                {

                    long iduser = loginData.UserID;
                    long idk = loginData.CustomerID;
                    string x = string.Join(",", data.list_field_name.Select(x => x.id_field));
                    if (!string.IsNullOrEmpty(x))
                    {
                        data.field_id = x;
                    }
                    Hashtable val = new Hashtable();
                    val.Add("title", data.title);
                    val.Add("customerid", loginData.CustomerID);
                    val.Add("createddate", Common.GetDateTime());
                    val.Add("createdby", iduser);
                    val.Add("isdefault", 0);
                    val.Add("is_template_center", 1);
                    val.Add("types", data.types);
                    val.Add("levels", data.levels);
                    val.Add("template_typeid", 1); // lấy mặc định trong we_template_types
                    if (!string.IsNullOrEmpty(data.viewid))
                        val.Add("viewid", data.viewid);
                    else
                        val.Add("viewid", DBNull.Value);
                    if (!string.IsNullOrEmpty(data.field_id))
                        val.Add("field_id", data.field_id);
                    else
                        val.Add("field_id", DBNull.Value);
                    // flow -> insert vào mẫu tạm xong lấy dữ liệu trong mẫu tạm đi insert
                    cnn.BeginTransaction();
                    if (cnn.Insert(val, "we_template_customer_temp") != 1)
                    {
                        cnn.RollbackTransaction();
                        return JsonResultCommon.Exception(_logger, cnn.LastError, _config, loginData, ControllerContext);
                    }
                    long idc = long.Parse(cnn.ExecuteScalar("select IDENT_CURRENT('we_template_customer_temp')").ToString());
                    //select save_as_id from we_template_customer where id_row =
                    #region insert Bảng tạm về data
                    if (!InsertTempToData(idc, data, loginData, istemplatelist, cnn, out error))
                    {
                        return JsonResultCommon.Custom(error);
                    }
                    #endregion
                    data.id_row = idc;
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
                        string x = string.Join(",", data.list_field_name.Select(x => x.id_field));
                        if (!string.IsNullOrEmpty(x))
                        {
                            data.field_id = x;
                        }
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
                        if (!string.IsNullOrEmpty(data.field_id)) val.Add("field_id", data.field_id);
                        val.Add("share_with", data.share_with);
                        val.Add("updatedby", iduser);
                        val.Add("updateddate", Common.GetDateTime());
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
                        val1["createddate"] = Common.GetDateTime();
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
        /// add template library
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        [Route("add-template-library")]
        [HttpPost]
        public async Task<BaseModel<object>> add_template_library(add_template_library_Model data)
        {
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
                        sql_insert = $@"insert into we_template_customer (title, description, createdDate, createdby, disabled, isdefault, color, id_department, save_as_id, customerid,is_template_center,template_typeid,types,levels,viewid,group_statusid,field_id,share_with,sample_id,addtolibrary, is_custom)
                        select title, description, getdate(), " + loginData.UserID + ", 0, isdefault, color, 0, id_row, " + loginData.CustomerID + " as CustomerID,1,template_typeid,types,levels,viewid,group_statusid,field_id,share_with,sample_id,1, is_custom from we_template_list where disabled = 0 and id_row = " + data.templateid + "";
                        cnn.ExecuteNonQuery(sql_insert);
                        if (cnn.LastError != null)
                        {
                            cnn.RollbackTransaction();
                            return JsonResultCommon.Exception(_logger, cnn.LastError, _config, loginData, ControllerContext);
                        }
                        long idc = long.Parse(cnn.ExecuteScalar("select IDENT_CURRENT('we_template_customer')").ToString());
                        sqlcond = new SqlConditions();
                        sqlcond.Add("id_row", idc);
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
                            string strDel = "update we_template_library set disabled=1, updateddate=getdate(), updatedby=" + iduser + " where disabled=0 and id_template=" + idc + " and id_user not in (" + ids + ")";
                            if (cnn.ExecuteNonQuery(strDel) < 0)
                            {
                                cnn.RollbackTransaction();
                                return JsonResultCommon.Exception(_logger, cnn.LastError, _config, loginData, ControllerContext);
                            }
                        }
                        Hashtable val1 = new Hashtable();
                        val1["id_template"] = idc;
                        val1["createddate"] = Common.GetDateTime();
                        val1["createdby"] = iduser;
                        foreach (var owner in data.list_share)
                        {
                            if (owner.id_row == 0)
                            {
                                bool HasItem = long.Parse(cnn.ExecuteScalar($"select count(*) from we_template_library where disabled = 0 and id_template = {idc} and id_user = {owner.id_user}").ToString()) > 0;
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
                        if (!WeworkLiteController.log(_logger, loginData.Username, cnn, 46, idc, iduser))
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
        /// delete template library
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        [Route("delete-library/{id}")]
        [HttpGet]
        public async Task<BaseModel<object>> delete_library(long id)
        {

            UserJWT loginData = Ulities.GetUserByHeader(HttpContext.Request.Headers);
            if (loginData == null)
                return JsonResultCommon.DangNhap();
            try
            {
                Hashtable val = new Hashtable();
                SqlConditions sqlcond = new SqlConditions();
                long iduser = loginData.UserID;
                long idk = loginData.CustomerID;
                string ConnectionString = WeworkLiteController.getConnectionString(ConnectionCache, loginData.CustomerID, _configuration);
                using (DpsConnection cnn = new DpsConnection(ConnectionString))
                {
                    cnn.BeginTransaction();
                    string sqlu = "update we_template_customer set disabled = 1 where id_row = " + id;
                    if (cnn.ExecuteNonQuery(sqlu) != 1)
                    {
                        cnn.RollbackTransaction();
                        return JsonResultCommon.Exception(_logger, cnn.LastError, _config, loginData, ControllerContext);
                    }
                    if (!WeworkLiteController.log(_logger, loginData.Username, cnn, 46, id, iduser))
                    {
                        cnn.RollbackTransaction();
                        return JsonResultCommon.Exception(_logger, cnn.LastError, _config, loginData, ControllerContext);
                    }
                    cnn.EndTransaction();
                    return JsonResultCommon.ThanhCong(id);
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

        /// <summary>
        /// insert data chính thức vào bảng tạm
        /// </summary>
        /// <param name="id_temp_center"></param>
        /// <param name="type"></param>
        /// <param name="idmau"></param>
        /// <param name="UserID"></param>
        /// <param name="cnn"></param>
        /// <param name="error"></param>
        /// <returns></returns>
        private bool InsertDataToTemp(long id_temp_center, long type, long idmau, long UserID, DpsConnection cnn, out string error)
        {
            error = "";
            // idmau là id của dự án phòng ban muốn đem đi nhân bản vào bảng tạm
            if (type == 1) // phòng ban
            {
                long idc = InserDepartmentToTemp(id_temp_center, idmau, UserID, 0, cnn, out error);
                if (idc <= 0)
                {
                    return false;
                }
                // xong bước 1 kiểm tra có folder hay không nếu có thì lưu folder
                string sqlf = "select * from we_department where  Disabled = 0 and  ParentID = " + idmau;
                DataTable datafolder = cnn.CreateDataTable(sqlf);
                if (datafolder.Rows.Count > 0)
                {
                    foreach (DataRow dr in datafolder.Rows)
                    {
                        long idfolder = long.Parse(dr["id_row"].ToString());
                        long idfoldertemp = InserDepartmentToTemp(id_temp_center, idfolder, UserID, idc, cnn, out error);
                        if (idfolder <= 0)
                        {
                            return false;
                        }
                    }
                }
            }
            else if (type == 2) // thư mục -- idfolder cũng là idparent 
            {
                long idc = InserDepartmentToTemp(id_temp_center, idmau, UserID, idmau, cnn, out error);
                if (idc <= 0)
                {
                    return false;
                }
            }
            else if (type == 3) // phòng ban -- id_project cũng là id_department
            {
                if (!InsertProjectToTemp(id_temp_center, idmau, UserID, idmau, cnn, out error))
                {
                    return false;
                }
            }
            return true;
        }
        private long InserDepartmentToTemp(long id_temp_center, long idmau, long UserID, long idparent, DpsConnection cnn, out string error)
        {
            // insert department/folder kiểm tra trong department/folder có dự án thì tạo dự án luôn
            error = "";
            string sql = "exec [DuplicateDepartmentToTemp] @id_temp_center,@id ,@UserID,@idparent";
            SqlConditions conds = new SqlConditions();
            conds.Add("id_temp_center", id_temp_center);
            conds.Add("id", idmau);
            conds.Add("UserID", UserID);
            conds.Add("idparent", idparent);
            DataTable dt = cnn.CreateDataTable(sql, conds);
            if (cnn.LastError != null || dt == null || dt.Rows.Count == 0)
            {
                cnn.RollbackTransaction();
                error = cnn.LastError.Message.ToString();
                return 0;
            }
            long idc = long.Parse(dt.Rows[0]["id_row"].ToString());
            string sqlp = "select * from we_project_team where  Disabled = 0 and id_department = " + idmau;
            DataTable dataprojectteam = cnn.CreateDataTable(sqlp);
            if (dataprojectteam.Rows.Count > 0)
            {
                foreach (DataRow dr in dataprojectteam.Rows)
                {
                    long idproject = long.Parse(dr["id_row"].ToString());
                    if (!InsertProjectToTemp(id_temp_center, idproject, UserID, idc, cnn, out error))
                    {
                        return 0;
                    }
                }
            }
            return idc;
        }
        private bool InsertProjectToTemp(long id_temp_center, long idprojectteam, long UserID, long id_department, DpsConnection cnn, out string error)
        {
            error = "";
            string sql = "exec [SaveAsTemplate] @id_temp_center,@id ,@UserID,@id_department";
            SqlConditions conds = new SqlConditions();
            conds.Add("id_temp_center", id_temp_center);
            conds.Add("id", idprojectteam);
            conds.Add("UserID", UserID);
            conds.Add("id_department", id_department);
            DataTable dt = cnn.CreateDataTable(sql, conds);
            if (cnn.LastError != null || dt == null || dt.Rows.Count == 0)
            {
                cnn.RollbackTransaction();
                error = cnn.LastError.Message.ToString();
                return false;
            }
            return true;
        }

        /// <summary>
        /// insert data bảng tạm về chính thức
        /// </summary>
        /// <param name="id_temp"></param>
        /// <param name="type"></param>
        /// <param name="idmau"></param>
        /// <param name="UserID"></param>
        /// <param name="cnn"></param>
        /// <param name="error"></param>
        /// <returns></returns>
        private bool InsertTempToData(long id_temp, TemplateCenterModel data, UserJWT loginData, bool istemplatelist, DpsConnection cnn, out string error)
        {
            error = "";
            long idmau = 0;
            string field = "id_temp_center";
            long datatemplatecemter = data.id_row;
            if (istemplatelist)
            {
                field = "id_template_list";
            }
            // idmau là id của dự án phòng ban muốn đem đi nhân bản vào bảng tạm
            if (data.types == 1) // phòng ban
            {
                idmau = long.Parse(cnn.ExecuteScalar($"select id_row from we_department_temp where {field} = {datatemplatecemter} and parentid is null").ToString());
                long idc = InsertTepmToDepartment(id_temp, idmau, data.title, data.ParentID, data.field_id, loginData, cnn, out error);
                if (idc <= 0)
                {
                    return false;
                }
                // xong bước 1 kiểm tra có folder hay không nếu có thì lưu folder
                string sqlf = "select * from we_department_temp where  Disabled = 0 and  ParentID = " + idmau;
                DataTable datafolder = cnn.CreateDataTable(sqlf);
                if (datafolder.Rows.Count > 0)
                {
                    foreach (DataRow dr in datafolder.Rows)
                    {
                        long idfolder = long.Parse(dr["id_row"].ToString());
                        string titlef = dr["title"].ToString();
                        long idfoldertemp = InsertTepmToDepartment(id_temp, idfolder, titlef, idc, data.field_id, loginData, cnn, out error);
                        if (idfoldertemp <= 0)
                        {
                            return false;
                        }
                    }
                }
            }
            else if (data.types == 2) // thư mục -- idfolder  
            {
                idmau = long.Parse(cnn.ExecuteScalar($"select top 1 id_row from we_department_temp where {field} = {datatemplatecemter}").ToString());
                long idc = InsertTepmToDepartment(id_temp, idmau, data.title, data.ParentID, data.field_id, loginData, cnn, out error);
                if (idc <= 0)
                {
                    return false;
                }
            }
            else if (data.types == 3) // phòng ban  
            {
                idmau = long.Parse(cnn.ExecuteScalar($"select top 1 id_row from we_project_team_temp where {field} = {datatemplatecemter}").ToString());
                if (!InsertTempToProject(id_temp, idmau, data.title, data.ParentID, cnn, out error))
                {
                    return false;
                }
            }
            return true;
        }
        private long InsertTepmToDepartment(long id_temp, long idmau, string title, long idparent, string field_id, UserJWT loginData, DpsConnection cnn, out string error)
        {
            // insert department/folder kiểm tra trong department/folder có dự án thì tạo dự án luôn
            error = "";
            string sql = "exec [DuplicateTempToDepartment] @id_temp,@id ,@title,@idparent, @CreatedBy, @CreatedDate,@IDKH";
            SqlConditions conds = new SqlConditions();
            conds.Add("id_temp", id_temp);
            conds.Add("id", idmau);
            conds.Add("title", title);
            conds.Add("idparent", idparent);
            conds.Add("CreatedBy", loginData.UserID);
            conds.Add("CreatedDate", Common.GetDateTime());
            conds.Add("IDKH", loginData.CustomerID);
            DataTable dt = cnn.CreateDataTable(sql, conds);
            if (cnn.LastError != null || dt == null || dt.Rows.Count == 0)
            {
                cnn.RollbackTransaction();
                error = cnn.LastError.Message.ToString();
                return 0;
            }
            long idc = long.Parse(dt.Rows[0]["id_row"].ToString());
            string sqlp = "select * from we_project_team_temp where disabled = 0 and id_department = " + idmau;
            DataTable dataprojectteam = cnn.CreateDataTable(sqlp);
            if (dataprojectteam.Rows.Count > 0)
            {
                foreach (DataRow dr in dataprojectteam.Rows)
                {
                    long idproject = long.Parse(dr["id_row"].ToString());
                    string titlep = dr["title"].ToString();
                    if (!InsertTempToProject(id_temp, idproject, titlep, idc, cnn, out error))
                    {
                        return 0;
                    }
                }
            }
            return idc;
        }
        private bool InsertTempToProject(long id_temp, long idprojectteam, string title, long id_department, DpsConnection cnn, out string error)
        {
            error = "";
            string sql = "exec [DuplicateTempToProjectTeam] @id_temp,@id ,@title,@id_department ";
            SqlConditions conds = new SqlConditions();
            conds.Add("id_temp", id_temp);
            conds.Add("id", idprojectteam);
            conds.Add("title", title);
            conds.Add("id_department", id_department);
            DataTable dt = cnn.CreateDataTable(sql, conds);
            if (cnn.LastError != null || dt == null || dt.Rows.Count == 0)
            {
                cnn.RollbackTransaction();
                error = cnn.LastError.Message.ToString();
                return false;
            }
            if (!WeworkLiteController.insert_processwork(cnn))
            {
                return false;
            }
            return true;
        }
        /// <summary>
        /// add user vào template library
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        [Route("save-image")]
        [HttpPost]
        public async Task<BaseModel<object>> SaveImage(FileUploadModel data, bool istemplatelist = false)
        {
            UserJWT loginData = Ulities.GetUserByHeader(HttpContext.Request.Headers);
            if (loginData == null)
                return JsonResultCommon.DangNhap();
            try
            {
                long iduser = loginData.UserID;
                long idk = loginData.CustomerID;
                string tablename = "we_template_list";
                if (!istemplatelist)
                    tablename = "we_template_customer";
                string ConnectionString = WeworkLiteController.getConnectionString(ConnectionCache, loginData.CustomerID, _configuration);
                using (DpsConnection cnn = new DpsConnection(ConnectionString))
                {
                    if (data != null)
                    {
                        string x = "";
                        string folder = "/logo/";
                        if (!UploadHelper.UploadFile(data.strBase64, data.filename, folder, _hostingEnvironment.ContentRootPath, ref x, _configuration))
                            return JsonResultCommon.Custom("Không thể cập nhật hình ảnh");
                        string link = WeworkLiteController.genLinkAttachment(_configuration, x);
                        string sqlu = $"update {tablename} set img_temp =N'{link}' where id_row = {data.IdRow} ";
                        cnn.BeginTransaction();
                        if (cnn.ExecuteNonQuery(sqlu) != 1)
                        {
                            cnn.RollbackTransaction();
                            return JsonResultCommon.ThatBai("Cập nhật hình ảnh");
                        }
                        cnn.EndTransaction();
                    }
                }
                return JsonResultCommon.ThanhCong(data);
            }
            catch (Exception ex)
            {
                return JsonResultCommon.Exception(_logger, ex, _config, loginData);
            }
        }
        #endregion
    }
}

