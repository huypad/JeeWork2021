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
    }
}

