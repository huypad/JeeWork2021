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
using System.Globalization;
using System.Text;
using DPSinfra.ConnectionCache;
using Microsoft.Extensions.Configuration;
using DPSinfra.Notifier;
using Microsoft.Extensions.Logging;
using API_JeeWork2021.Classes;
using DPSinfra.Kafka;
using JeeWork_Core2021.Controller;
using Google.Apis.Calendar.v3;
using Google.Apis.Auth.OAuth2;
using System.IO;
using Google.Apis.Services;
using Google.Apis.Calendar.v3.Data;
using Microsoft.AspNetCore.Http;
using DPSinfra.Logger;
using Newtonsoft.Json;

namespace JeeWork_Core2021.Controllers.Wework
{
    [ApiController]
    [Route("api/tp-tasks")]
    [EnableCors("JeeWorkPolicy")]
    /// <summary>
    /// api quản lý work (click up)
    /// </summary>
    // [EnableCors(origins: "*", headers: "*", methods: "*")]
    public class TaskController : ControllerBase
    {
        private readonly IHostingEnvironment _hostingEnvironment;
        private JeeWorkConfig _config;
        public static DataImportModel data_import = new DataImportModel();
        public List<AccUsernameModel> DataAccount;
        private IConnectionCache ConnectionCache;
        private IConfiguration _configuration;
        private INotifier _notifier;
        private IProducer _producer;
        private readonly ILogger<TaskController> _logger;
        static string sql_isquahan = " w.deadline < GETUTCDATE() and w.deadline is not null and w.end_date is null ";
        static string sql_dangthuchien = "((w.deadline >= GETUTCDATE() and deadline is not null) or deadline is null ) and w.end_date is null";
        static string sqlhoanthanhdunghan = " w.end_date is not null and (w.deadline >= w.end_date or w.deadline is null) ";
        static string sqlhoanthanhquahan = " w.end_date is not null and w.deadline < w.end_date";
        static string sqlhoanthanh = " w.end_date is not null ";
        // kiểm tra điều kiện hoàn thành
        string queryhoanthanh = " and w.end_date is not null ";
        string querydangthuchien = " and w.end_date is null ";
        public TaskController(IOptions<JeeWorkConfig> config, IHostingEnvironment hostingEnvironment, IConnectionCache _cache, IConfiguration configuration, INotifier notifier, ILogger<TaskController> logger, IProducer producer)
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
        /// <summary>
        /// Trang danh sách công việc chính
        /// </summary>
        /// <param name="query"></param>
        /// <returns></returns>
        [Route("list-task")]
        [HttpGet]
        public object ListTask([FromQuery] QueryParams query)
        {
            UserJWT loginData = Ulities.GetUserByHeader(HttpContext.Request.Headers);
            if (loginData == null)
                return JsonResultCommon.DangNhap();
            if (query == null)
                query = new QueryParams();
            bool Visible = true;
            PageModel pageModel = new PageModel();
            try
            {
                string domain = _configuration.GetValue<string>("Host:JeeWork_API") + "/";
                string ConnectionString = JeeWorkLiteController.getConnectionString(ConnectionCache, loginData.CustomerID, _configuration);
                using (DpsConnection cnn = new DpsConnection(ConnectionString))
                {
                    #region Lấy dữ liệu account từ JeeAccount
                    DataAccount = JeeWorkLiteController.GetAccountFromJeeAccount(HttpContext.Request.Headers, _configuration);
                    if (DataAccount == null)
                        return JsonResultCommon.Custom("Lỗi lấy danh sách nhân viên từ hệ thống quản lý tài khoản");
                    #endregion
                    DataTable dt_Fields = JeeWorkLiteController.ListField(int.Parse(query.filter["id_project_team"]), 3, cnn);
                    var data = GetTaskByProjects(Request.Headers, query.filter["id_project_team"], dt_Fields, ConnectionString, query, loginData, DataAccount);
                    return JsonResultCommon.ThanhCong(data, pageModel, Visible);
                }
            }
            catch (Exception ex)
            {
                return JsonResultCommon.Exception(_logger, ex, _config, loginData);
            }
        }
        /// <summary>
        /// Danh sách công việc theo Space, Folder, Space
        /// </summary>
        /// <param name="query"></param>
        /// <returns></returns>
        [Route("work-filter")]
        [HttpGet]
        public object WorkFilter([FromQuery] QueryParams query)
        {
            UserJWT loginData = Ulities.GetUserByHeader(HttpContext.Request.Headers);
            if (loginData == null)
                return JsonResultCommon.DangNhap();
            if (query == null)
                query = new QueryParams();
            bool Visible = true;
            PageModel pageModel = new PageModel();
            try
            {
                string domain = _configuration.GetValue<string>("Host:JeeWork_API") + "/";
                string ConnectionString = JeeWorkLiteController.getConnectionString(ConnectionCache, loginData.CustomerID, _configuration);
                using (DpsConnection cnn = new DpsConnection(ConnectionString))
                {
                    #region Lấy dữ liệu account từ JeeAccount
                    DataAccount = JeeWorkLiteController.GetAccountFromJeeAccount(HttpContext.Request.Headers, _configuration);
                    if (DataAccount == null)
                        return JsonResultCommon.Custom("Lỗi lấy danh sách nhân viên từ hệ thống quản lý tài khoản");
                    #endregion
                    long spaceid = 0; long folderid = 0;
                    if (!string.IsNullOrEmpty(query.filter["spaceid"]))
                        spaceid = int.Parse(query.filter["spaceid"]);
                    if (!string.IsNullOrEmpty(query.filter["folderid"]))
                        folderid = int.Parse(query.filter["folderid"]);
                    DataTable workspace = new DataTable();
                    workspace.Columns.Add("id_row"); // id_dự án
                    workspace.Columns.Add("title"); // có thể là tên phòng ban hay thư mục
                    workspace.Columns.Add("projectname"); // tên dự án
                    workspace.Columns.Add("color"); // màu sắc
                    workspace.Columns.Add("parentid"); // id
                    workspace.Columns.Add("id_department"); // link bảng we_project_team ==> we_department
                    DataRow _r = workspace.NewRow();
                    SqlConditions Conds = new SqlConditions();
                    Conds.Add("disabled", 0);
                    long type = 3; long id = 0;

                    DataSet ds_workspace = new DataSet();
                    DataTable dt = new DataTable();
                    DataTable dt_list = new DataTable();
                    if (spaceid > 0)
                    {
                        id = spaceid;
                        type = 1;
                    }
                    if (folderid > 0)
                    {
                        type = 2;
                        id = folderid;
                    }
                    Common common = new Common(ConnectionString);
                    ds_workspace = Common.GetWorkSpace(loginData, id, type, ConnectionString);
                    string listDA = string.Join(",", ds_workspace.Tables[2].AsEnumerable().Select(x => x["id_row"]).ToList());
                    if (string.IsNullOrEmpty(listDA))
                    {
                        listDA = "0";
                    }
                    string sql_list = "";
                    if (spaceid > 0)
                    {
                        Conds.Add("parentid", spaceid); // khi xem công việc của phòng ban
                        DataRow[] dr_space = ds_workspace.Tables[0].Select("id_row = " + spaceid);
                        if (dr_space.Length > 0)
                        {
                            dt = dr_space.CopyToDataTable();
                            foreach (DataRow fd in dt.Rows)
                            {
                                Conds = new SqlConditions();
                                Conds.Add("disabled", 0);
                                Conds.Add("id_department", fd["id_row"]);
                                sql_list = $"select id_row, id_department, title, color from we_project_team where (where) and id_row in ({listDA})";
                                dt_list = cnn.CreateDataTable(sql_list, "(where)", Conds);
                                if (dt_list.Rows.Count > 0)
                                {
                                    foreach (DataRow _list in dt_list.Rows)
                                    {
                                        _r = workspace.NewRow();
                                        _r["id_row"] = _list["id_row"];
                                        _r["title"] = fd["title"];
                                        _r["projectname"] = _list["title"];
                                        _r["color"] = _list["color"];
                                        _r["parentid"] = fd["parentid"];
                                        _r["id_department"] = _list["id_department"];
                                        workspace.Rows.Add(_r);
                                    }
                                }
                            }
                        }
                    }
                    DataRow[] dr_fd = ds_workspace.Tables[1].Select("parentid = " + spaceid);
                    if (type == 2)
                        dr_fd = ds_workspace.Tables[1].Select("id_row = " + folderid);
                    if (dr_fd.Length > 0)
                    {
                        dt = dr_fd.CopyToDataTable();
                        foreach (DataRow fd in dt.Rows)
                        {
                            Conds = new SqlConditions();
                            Conds.Add("disabled", 0);
                            Conds.Add("id_department", fd["id_row"]);
                            sql_list = $"select id_row, id_department, title, color from we_project_team where (where) and id_row in ({listDA})";
                            dt_list = cnn.CreateDataTable(sql_list, "(where)", Conds);
                            if (dt_list.Rows.Count > 0)
                            {
                                foreach (DataRow _list in dt_list.Rows)
                                {
                                    _r = workspace.NewRow();
                                    _r["id_row"] = _list["id_row"];
                                    _r["title"] = fd["title"];
                                    _r["projectname"] = _list["title"];
                                    _r["color"] = _list["color"];
                                    _r["parentid"] = fd["parentid"];
                                    _r["id_department"] = _list["id_department"];
                                    workspace.Rows.Add(_r);
                                }
                            }
                        }
                    }
                    DataTable dt_fields = JeeWorkLiteController.ListField(id, type, cnn);
                    var data = from r in workspace.AsEnumerable()
                               select new
                               {
                                   id_department = r["id_department"],
                                   projectname = r["projectname"],
                                   title = r["title"],
                                   color = r["color"],
                                   id_row = r["id_row"],
                                   project_data = GetWorkByProjects(Request.Headers, r["id_row"].ToString(), dt_fields, ConnectionString, query, loginData, DataAccount)
                               };
                    return JsonResultCommon.ThanhCong(data, pageModel, Visible);
                }
            }
            catch (Exception ex)
            {
                return JsonResultCommon.Exception(_logger, ex, _config, loginData);
            }
        }
        /// <summary>
        /// Danh sách công việc cá nhân (Tôi làm/ Tôi đang theo dõi)
        /// </summary>
        /// <param name="query"></param>
        /// <returns></returns>
        [Route("my-list-mobile")]
        [HttpGet]
        public object MyListMobile([FromQuery] QueryParams query)
        {
            UserJWT loginData = Ulities.GetUserByHeader(HttpContext.Request.Headers);
            if (loginData == null)
                return JsonResultCommon.DangNhap();
            if (query == null)
                query = new QueryParams();
            bool Visible = true;
            string ConnectionString = "";
            PageModel pageModel = new PageModel();
            try
            {
                #region Lấy dữ liệu account từ JeeAccount
                DataAccount = JeeWorkLiteController.GetAccountFromJeeAccount(HttpContext.Request.Headers, _configuration);
                if (DataAccount == null)
                    return JsonResultCommon.Custom("Lỗi lấy danh sách nhân viên từ hệ thống quản lý tài khoản");
                #endregion
                string domain = _configuration.GetValue<string>("Host:JeeWork_API") + "/";
                ConnectionString = JeeWorkLiteController.getConnectionString(ConnectionCache, loginData.CustomerID, _configuration);
                using (DpsConnection cnn = new DpsConnection(ConnectionString))
                {
                    long IDNV = loginData.UserID;
                    #region filter thời gian, keyword
                    DateTime from = Common.GetDateTime();
                    DateTime to = Common.GetDateTime();
                    string strW = "";
                    if (!string.IsNullOrEmpty(query.filter["TuNgay"]))
                    {
                        bool from1 = DateTime.TryParseExact(query.filter["TuNgay"], "dd/MM/yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out from);
                        if (!from1)
                            return JsonResultCommon.Custom("Thời gian bắt đầu không hợp lệ");
                    }
                    if (!string.IsNullOrEmpty(query.filter["DenNgay"]))
                    {
                        bool to1 = DateTime.TryParseExact(query.filter["DenNgay"], "dd/MM/yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out to);
                        if (!to1)
                            return JsonResultCommon.Custom("Thời gian kết thúc không hợp lệ");
                    }
                    #endregion
                    string order_by = "";
                    SqlConditions conds = new SqlConditions();
                    conds.Add("w_user.disabled", 0);
                    string select_user = $@"select distinct w_user.id_user,'' as hoten,'' as email,'' as image, id_work,w_user.loai,w_user.CreatedBy
                                        from we_work_user w_user 
                                        join we_work on we_work.id_row = w_user.id_work where (where)";
                    DataTable dt_Users = cnn.CreateDataTable(select_user, "(where)", conds);
                    #region Lấy công việc user theo filter
                    string strW_parent = "";
                    if (!string.IsNullOrEmpty(query.filter["id_nv"]))
                        IDNV = long.Parse(query.filter["id_nv"]);
                    strW = " and (w.id_nv=@iduser or w.createdby=@iduser or w.nguoigiao=@iduser or w.id_row in (select id_work from we_work_user where loai = 2 and disabled=0 and id_user = @iduser)  (parent))"; // w.nguoigiao=@iduser or w.createdby=@iduser -- w.NguoiGiao = @iduser or
                    if (!string.IsNullOrEmpty(query.filter["filter"]))
                    {
                        if (query.filter["filter"] == "1")//được giao
                            strW = " and (w.id_nv=@iduser (parent)) ";
                        if (query.filter["filter"] == "2")//giao đi
                            strW = " and (w.nguoigiao=@iduser (parent))";
                        if (query.filter["filter"] == "3")// theo dõi
                            strW = $" and (w.id_row in (select id_work from we_work_user where loai = 2 and disabled=0 and id_user = @iduser ) (parent))";
                    }
                    if (!string.IsNullOrEmpty(query.filter["sort"]))
                    {
                        order_by = query.filter["sort"];
                    }
                    if (!string.IsNullOrEmpty(query.filter["isclose"]))
                    {
                        strW = " and w.closed = " + query.filter["isclose"] + ""; // đóng truyền 0, không đóng truyền 1
                    }
                    string displayChild = "0";//hiển thị con: 0-không hiển thị, 1- 1 cấp con, 2- nhiều cấp con
                    displayChild = query.filter["displayChild"];
                    if (displayChild.ToString() == "0")
                    {
                        strW = strW.Replace("(parent)", " ");
                    }
                    else
                    {
                        string querysub = "";
                        if (string.IsNullOrEmpty(query.filter["subtask_done"]) || query.filter["subtask_done"] == "0")// sub task
                            querysub += $" and ww.end_date is null";
                        strW_parent = $"or w.id_row in (select id_parent from we_work ww join we_work_user wu on ww.id_row = wu.id_work where ww.disabled = 0 and wu.disabled = 0 and id_parent is not null and id_user = @iduser {querysub} )";
                        if (query.filter["filter"] == "1")//được giao
                            strW_parent = $"or w.id_row in (select id_parent from we_work ww join we_work_user wu on ww.id_row = wu.id_work where ww.disabled = 0 and wu.disabled = 0 and id_parent is not null and wu.loai = 1 and id_user = @iduser {querysub})";
                        if (query.filter["filter"] == "2")//giao đi
                            strW_parent = $"or w.id_row in (select id_parent from we_work ww join we_work_user wu on ww.id_row = wu.id_work where ww.disabled = 0 and wu.disabled = 0 and id_parent is not null and wu.CreatedBy = @iduser {querysub})";
                        if (query.filter["filter"] == "3")// theo dõi
                            strW_parent = $"or w.id_row in (select id_parent from we_work ww join we_work_user wu on ww.id_row = wu.id_work where ww.disabled = 0 and wu.disabled = 0 and id_parent is not null and wu.loai = 2 and id_user = @iduser {querysub})";
                        strW = strW.Replace("(parent)", strW_parent);
                    }
                    if (query.filter["subtask_done"] == "0")// sub task 
                        strW += $" and w.id_row not in (select id_row from we_work where end_date is not null and id_parent is not null) ";
                    if (query.filter["task_done"] == "0")// task
                        strW += $" and  w.id_row not in (select id_row from we_work where end_date is not null and id_parent is null) ";
                    #endregion
                    string columnName = "id_project_team";
                    DataSet ds = GetWorkByEmployee(Request.Headers, cnn, query, IDNV, DataAccount, strW, order_by);
                    if (cnn.LastError != null || ds == null)
                        return JsonResultCommon.Exception(_logger, cnn.LastError, _config, loginData, ControllerContext);
                    var temp = filterTasks(ds.Tables[0].AsEnumerable().Where(x => x["id_parent"] == DBNull.Value), query.filter);//k bao gồm con
                    var tags = ds.Tables[1].AsEnumerable();
                    // Phân trang
                    int total = temp.Count();
                    if (total == 0)
                        return JsonResultCommon.ThanhCong(new List<string>());
                    if (query.more)
                    {
                        query.page = 1;
                        query.record = total;
                    }
                    pageModel.TotalCount = total;
                    pageModel.AllPage = (int)Math.Ceiling(total / (decimal)query.record);
                    pageModel.Size = query.record;
                    pageModel.Page = query.page;
                    var dtNew = temp.Skip((query.page - 1) * query.record).Take(query.record);
                    var dtChild = ds.Tables[0].AsEnumerable().Where(x => x["id_parent"] != DBNull.Value).AsEnumerable();
                    if (dtChild.Any())
                        dtNew = dtNew.Concat(dtChild);
                    dtNew = temp.Skip((query.page - 1) * query.record).Take(query.record);
                    Func<DateTime, int> weekProjector = d => CultureInfo.CurrentCulture.Calendar.GetWeekOfYear(d, CalendarWeekRule.FirstFourDayWeek, DayOfWeek.Monday);
                    var data = from r in dtNew.CopyToDataTable().AsEnumerable()
                               select new
                               {
                                   id_parent = r["id_parent"],
                                   id_row = r["id_row"],
                                   title = r["title"],
                                   description = r["description"],
                                   id_project_team = r["id_project_team"],
                                   project_team = r["project_team"],
                                   deadline = r["deadline"],
                                   end_date = r["end_date"],
                                   urgent = r["urgent"],
                                   important = r["important"],
                                   start_date = r["start_date"],
                                   prioritize = r["prioritize"],
                                   favourite = r["favourite"],
                                   status = r["status"],
                                   id_milestone = r["id_milestone"],
                                   milestone = r["milestone"],
                                   trehan = r["TreHan"],
                                   estimates = r["estimates"],
                                   hoanthanh = r["done"],
                                   danglam = r["Doing"],
                                   closed = r["closed"],
                                   closed_work_date = r["closed_work_date"],
                                   closed_work_by = r["closed_work_by"],
                                   accepted_date = r["accepted_date"] == DBNull.Value ? "" : r["accepted_date"],
                                   activated_by = r["activated_by"].Equals(DBNull.Value) ? new { } : JeeWorkLiteController.Get_InfoUsers(r["activated_by"].ToString(), DataAccount),
                                   activated_date = r["activated_date"] == DBNull.Value ? "" : r["activated_date"],
                                   closed_by = r["closed_by"].Equals(DBNull.Value) ? new { } : JeeWorkLiteController.Get_InfoUsers(r["closed_by"].ToString(), DataAccount),
                                   closed_date = r["closed_date"] == DBNull.Value ? "" : r["closed_date"],
                                   state_change_date = r["state_change_date"] == DBNull.Value ? "" : r["state_change_date"],
                                   createddate = r["CreatedDate"],
                                   createdby = r["CreatedBy"].Equals(DBNull.Value) ? new { } : JeeWorkLiteController.Get_InfoUsers(r["CreatedBy"].ToString(), DataAccount),
                                   nguoitao = r["NguoiTao"],
                                   updateddate = r["UpdatedDate"] == DBNull.Value ? "" : r["UpdatedDate"],
                                   updatedby = r["UpdatedBy"].Equals(DBNull.Value) ? new { } : JeeWorkLiteController.Get_InfoUsers(r["UpdatedBy"].ToString(), DataAccount),
                                   nguoisua = r["NguoiSua"],
                                   clickup_prioritize = r["clickup_prioritize"],
                                   activity_date = r["ActivityDate"],
                                   comments = SoluongComment(r["id_row"].ToString(), ConnectionString),  // SL bình luận
                                   status_info = JeeWorkLiteController.get_info_status(r["status"].ToString(), ConnectionString),
                                   //DataStatus = list_status_user(r["id_row"].ToString(), r["id_project_team"].ToString(), loginData, ConnectionString, DataAccount),
                                   User = from us in dt_Users.AsEnumerable()
                                          where r["id_row"].Equals(us["id_work"]) && long.Parse(us["loai"].ToString()).Equals(1)
                                          select new
                                          {
                                              id_nv = us["id_user"],
                                              hoten = us["hoten"],
                                              image = us["image"],
                                              email = us["email"],
                                              loai = us["loai"],
                                          },
                                   NguoiGiao = from us in dt_Users.AsEnumerable()
                                               where r["id_row"].ToString().Equals(us["id_work"].ToString()) && long.Parse(us["loai"].ToString()).Equals(1)
                                               select us["CreatedBy"].Equals(DBNull.Value) ? new { } : JeeWorkLiteController.Get_InfoUsers(us["CreatedBy"].ToString(), DataAccount),
                                   UsersInfo = from us in dt_Users.AsEnumerable()
                                               where r["id_row"].Equals(us["id_work"]) && long.Parse(us["loai"].ToString()).Equals(1)
                                               select us["id_user"].Equals(DBNull.Value) ? new { } : JeeWorkLiteController.Get_InfoUsers(us["id_user"].ToString(), DataAccount),
                                   Follower = from us in dt_Users.AsEnumerable()
                                              where r["id_row"].Equals(us["id_work"]) && long.Parse(us["loai"].ToString()).Equals(2)
                                              select new
                                              {
                                                  id_nv = us["id_user"],
                                                  hoten = us["hoten"],
                                                  image = us["image"],
                                                  email = us["email"],
                                                  loai = us["loai"],
                                              },
                                   FollowerInfo = from us in dt_Users.AsEnumerable()
                                                  where r["id_row"].Equals(us["id_work"]) && long.Parse(us["loai"].ToString()).Equals(2)
                                                  select new
                                                  {
                                                      data = us["id_user"].Equals(DBNull.Value) ? new { } : JeeWorkLiteController.Get_InfoUsers(us["id_user"].ToString(), DataAccount),
                                                  },
                                   Tags = from t in tags
                                          where r["id_row"].Equals(t["id_work"])
                                          select new
                                          {
                                              id_row = t["id_tag"],
                                              title = t["title"],
                                              color = t["color"]
                                          },

                                   Childs = displayChild == "0" ? new List<string>() : getChild(domain, loginData.CustomerID, columnName, displayChild == "1" ? "0" : "2", 0, dtNew.CopyToDataTable().AsEnumerable(), tags, DataAccount, loginData, ConnectionString, r["id_row"])
                               };
                    return JsonResultCommon.ThanhCong(data, pageModel, Visible);
                }
            }
            catch (Exception ex)
            {
                return JsonResultCommon.Exception(_logger, ex, _config, loginData);
            }
        }
        /// <summary>
        /// Danh sách công việc hiển thị trên widget 
        /// Type "41" công việc tôi làm
        /// Type "42" công việc tôi giao
        /// </summary>
        /// <param name="query"></param>
        /// <returns></returns>
        [Route("list-work-by-widget")]
        [HttpGet]
        public object List_Work_By_Widget([FromQuery] QueryParams query)
        {
            UserJWT loginData = Ulities.GetUserByHeader(HttpContext.Request.Headers);
            if (loginData == null)
                return JsonResultCommon.DangNhap();
            if (query == null)
                query = new QueryParams();
            bool Visible = true;
            PageModel pageModel = new PageModel();
            try
            {
                #region Lấy dữ liệu account từ JeeAccount
                DataAccount = JeeWorkLiteController.GetAccountFromJeeAccount(HttpContext.Request.Headers, _configuration);
                if (DataAccount == null)
                    return JsonResultCommon.Custom("Lỗi lấy danh sách nhân viên từ hệ thống quản lý tài khoản");
                string error = "";
                string listID = JeeWorkLiteController.ListAccount(HttpContext.Request.Headers, out error, _configuration);
                if (error != "")
                    return JsonResultCommon.Custom(error);
                #endregion
                string domain = _configuration.GetValue<string>("Host:JeeWork_API") + "/";
                string ConnectionString = JeeWorkLiteController.getConnectionString(ConnectionCache, loginData.CustomerID, _configuration);
                using (DpsConnection cnn = new DpsConnection(ConnectionString))
                {
                    long IDNV = loginData.UserID;
                    #region filter thời gian, keyword
                    DateTime from = Common.GetDateTime();
                    DateTime to = Common.GetDateTime();
                    int nam = DateTime.Today.Year;
                    int thang = DateTime.Today.Month;
                    var lastDayOfMonth = DateTime.DaysInMonth(nam, thang);
                    string strW = "";
                    if (!string.IsNullOrEmpty(query.filter["Thang"]))
                    {
                        thang = int.Parse(query.filter["Thang"]);
                    }
                    if (!string.IsNullOrEmpty(query.filter["Nam"]))
                    {
                        nam = int.Parse(query.filter["Nam"]);
                    }
                    from = new DateTime(nam, thang, 1, 0, 0, 1);
                    to = new DateTime(nam, thang, lastDayOfMonth, 23, 59, 59);
                    #endregion
                    if (!string.IsNullOrEmpty(query.filter["id_nv"]))
                        IDNV = long.Parse(query.filter["id_nv"]);
                    #region Lấy công việc user theo filter
                    string strW_parent = "";
                    if (!string.IsNullOrEmpty(query.filter["id_nv"]))
                        IDNV = long.Parse(query.filter["id_nv"]);
                    strW = " and (w.id_nv=@iduser or w.createdby=@iduser or w.nguoigiao=@iduser or w.id_row in (select id_work from we_work_user where loai = 2 and disabled=0 and id_user = @iduser)  (parent))"; // w.nguoigiao=@iduser or w.createdby=@iduser -- w.NguoiGiao = @iduser or
                    if (!string.IsNullOrEmpty(query.filter["filter"]))
                    {
                        if (query.filter["filter"] == "1")//được giao
                            strW = " and (w.id_nv=@iduser (parent)) ";
                        if (query.filter["filter"] == "2")//giao đi
                            strW = " and (w.nguoigiao=@iduser (parent))";
                        if (query.filter["filter"] == "3")// theo dõi
                            strW = $" and (w.id_row in (select id_work from we_work_user where loai = 2 and disabled=0 and id_user = @iduser ) (parent))";
                    }
                    string displayChild = "0";//hiển thị con: 0-không hiển thị, 1- 1 cấp con, 2- nhiều cấp con
                    if (!string.IsNullOrEmpty(query.filter["displayChild"]))
                    {
                        displayChild = query.filter["displayChild"];
                        strW = strW.Replace("(parent)", " ");
                    }
                    else
                    {
                        strW_parent = "or w.id_row in (select id_parent from we_work ww join we_work_user wu on ww.id_row = wu.id_work where ww.disabled = 0 and wu.disabled = 0 and id_parent is not null and id_user = @iduser)";
                        strW = strW.Replace("(parent)", strW_parent);
                    }
                    #endregion
                    strW += " and w.deadline is not null and (w.deadline >= '" + from + "' and w.deadline <= '" + to + "')";
                    DataSet ds = GetWorkByEmployee(Request.Headers, cnn, query, IDNV, DataAccount, strW);
                    if (cnn.LastError != null || ds == null)
                        return JsonResultCommon.Exception(_logger, cnn.LastError, _config, loginData, ControllerContext);
                    // Phân trang
                    var dt_data = ds.Tables[0].Rows.Count;
                    DataTable dt = JeeWorkLiteController.project_by_user(loginData.UserID, loginData.CustomerID, cnn, "");
                    if (cnn.LastError != null || dt == null)
                        return JsonResultCommon.Exception(_logger, cnn.LastError, _config, loginData, ControllerContext);
                    Func<DateTime, int> weekProjector = d => CultureInfo.CurrentCulture.Calendar.GetWeekOfYear(d, CalendarWeekRule.FirstFourDayWeek, DayOfWeek.Monday);
                    var Children = from r in ds.Tables[0].AsEnumerable()
                                   where r["deadline"].ToString() != ""
                                   select new
                                   {
                                       id_row = r["id_row"],
                                       title = r["title"],
                                       id_project_team = r["id_project_team"],
                                       deadline = r["deadline"],
                                       status = r["status"],
                                       start_date = r["start_date"],
                                       createddate = string.Format("{0:dd/MM/yyyy HH:mm}", r["createddate"]),
                                       createdby = r["createdby"],
                                       id_nguoigiao = r["nguoigiao"],
                                       hoten_nguoigiao = r["hoten_nguoigiao"],
                                       hoten_assign = r["hoten"],
                                       id_nv_assign = r["id_nv"],
                                       prioritize = r["clickup_prioritize"],
                                       typeid = "".Equals(r["nguoigiao"].ToString()) ? 0 : (r["nguoigiao"].Equals(loginData.UserID) ? 32 : (r["id_nv"].Equals(loginData.UserID) ? 31 : 0)),
                                       project_info = from t in dt.AsEnumerable()
                                                      where r["id_project_team"].Equals(t["id_row"])
                                                      select new
                                                      {
                                                          id_row = t["id_row"],
                                                          title = t["title"],
                                                          isproject = t["is_project"],
                                                          start_date = t["start_date"] == DBNull.Value ? "" : string.Format("{0:dd/MM/yyyy HH:mm}", t["start_date"]),
                                                          end_date = t["end_date"] == DBNull.Value ? "" : string.Format("{0:dd/MM/yyyy HH:mm}", t["end_date"]),
                                                          color = t["color"],
                                                          status = t["status"],
                                                          locked = t["locked"],
                                                      },
                                   };
                    return JsonResultCommon.ThanhCong(Children, pageModel, Visible);
                }
            }
            catch (Exception ex)
            {
                return JsonResultCommon.Exception(_logger, ex, _config, loginData);
            }
        }
        /// <summary>
        /// Bộ lọc tùy chỉnh
        /// </summary>
        /// <param name="query"></param>
        /// <returns></returns>
        [Route("list-work-by-filter")]
        [HttpGet]
        public object ListWorkByFilter([FromQuery] QueryParams query)
        {
            UserJWT loginData = Ulities.GetUserByHeader(HttpContext.Request.Headers);
            if (loginData == null)
                return JsonResultCommon.DangNhap();
            if (query == null)
                query = new QueryParams();
            bool Visible = true;
            PageModel pageModel = new PageModel();
            try
            {
                string domain = _configuration.GetValue<string>("Host:JeeWork_API") + "/";
                string ConnectionString = JeeWorkLiteController.getConnectionString(ConnectionCache, loginData.CustomerID, _configuration);
                using (DpsConnection cnn = new DpsConnection(ConnectionString))
                {
                    #region Lấy dữ liệu account từ JeeAccount
                    DataAccount = JeeWorkLiteController.GetAccountFromJeeAccount(HttpContext.Request.Headers, _configuration);
                    if (DataAccount == null)
                        return JsonResultCommon.Custom("Lỗi lấy danh sách nhân viên từ hệ thống quản lý tài khoản");

                    string error = "";
                    string listID = JeeWorkLiteController.ListAccount(HttpContext.Request.Headers, out error, _configuration);
                    if (error != "")
                        return JsonResultCommon.Custom(error);
                    #endregion
                    #region filter thời gian , keyword
                    DateTime from = Common.GetDateTime();
                    DateTime to = Common.GetDateTime();
                    if (!string.IsNullOrEmpty(query.filter["TuNgay"]))
                    {
                        bool from1 = DateTime.TryParseExact(query.filter["TuNgay"], "dd/MM/yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out from);
                        if (!from1)
                            return JsonResultCommon.Custom("Thời gian bắt đầu không hợp lệ");
                    }
                    if (!string.IsNullOrEmpty(query.filter["DenNgay"]))
                    {
                        bool to1 = DateTime.TryParseExact(query.filter["DenNgay"], "dd/MM/yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out to);
                        if (!to1)
                            return JsonResultCommon.Custom("Thời gian kết thúc không hợp lệ");
                    }
                    #endregion
                    string displayChild = "0";//hiển thị con: 0-không hiển thị, 1- 1 cấp con, 2- nhiều cấp con
                    if (!string.IsNullOrEmpty(query.filter["displayChild"]))
                        displayChild = query.filter["displayChild"];
                    string columnName = "id_project_team";
                    string strW = " and (w.id_nv=@iduser or w.nguoigiao=@iduser or w.createdby=@iduser)";
                    #region group
                    string strG = @"select distinct p.id_row, p.title from we_project_team_user u
                                    join we_project_team p on p.id_row=u.id_project_team 
                                    where u.disabled=0 and p.Disabled=0 and id_user=" + query.filter["id_nv"];
                    #endregion
                    DataTable dtG = cnn.CreateDataTable(strG);
                    if (dtG.Rows.Count == 0)
                        return JsonResultCommon.ThanhCong(new List<string>(), null, Visible);
                    strW += FilterWorkController.genStringWhere(cnn, loginData.UserID, query.filter["id_filter"], DataAccount);
                    DataSet ds = GetWorkByEmployee(Request.Headers, cnn, query, long.Parse(query.filter["id_nv"]), DataAccount, strW);
                    if (cnn.LastError != null || ds == null)
                        return JsonResultCommon.Exception(_logger, cnn.LastError, _config, loginData, ControllerContext);
                    var temp = filterWork(ds.Tables[0].AsEnumerable().Where(x => x["id_parent"] == DBNull.Value), query.filter);//k bao gồm con
                    var tags = ds.Tables[1].AsEnumerable();
                    // Phân trang
                    int total = temp.Count();
                    if (total == 0)
                        return JsonResultCommon.ThanhCong(new List<string>());
                    if (query.more)
                    {
                        query.page = 1;
                        query.record = total;
                    }
                    pageModel.TotalCount = total;
                    pageModel.AllPage = (int)Math.Ceiling(total / (decimal)query.record);
                    pageModel.Size = query.record;
                    pageModel.Page = query.page;
                    var dtNew = temp.Skip((query.page - 1) * query.record).Take(query.record);
                    var dtChild = ds.Tables[0].AsEnumerable().Where(x => x["id_parent"] != DBNull.Value).AsEnumerable();
                    dtNew = dtNew.Concat(dtChild);
                    Func<DateTime, int> weekProjector = d => CultureInfo.CurrentCulture.Calendar.GetWeekOfYear(d, CalendarWeekRule.FirstFourDayWeek, DayOfWeek.Monday);
                    var Children = from rr in dtG.AsEnumerable()
                                   select new
                                   {
                                       id = rr["id_row"],
                                       title = rr["title"],
                                       data = getChild(domain, loginData.CustomerID, columnName, displayChild, rr["id_row"], dtNew.CopyToDataTable().AsEnumerable(), tags, DataAccount, loginData, ConnectionString)
                                       //data = getChild(domain, loginData.CustomerID, columnName, displayChild, rr["id_row"], dtNew.CopyToDataTable().AsEnumerable(), tags)

                                   };
                    return JsonResultCommon.ThanhCong(Children, pageModel, Visible);
                }
            }
            catch (Exception ex)
            {
                return JsonResultCommon.Exception(_logger, ex, _config, loginData);
            }
        }
        /// <summary>
        /// Load danh sách công việc của user theo id quản lý (Click up)
        /// </summary>
        /// <param name="query"></param>
        /// <returns></returns>
        [Route("list-work-user-by-manager")]
        [HttpGet]
        public object ListWorkUserByManager([FromQuery] QueryParams query)
        {

            UserJWT loginData = Ulities.GetUserByHeader(HttpContext.Request.Headers);
            if (loginData == null)
                return JsonResultCommon.DangNhap();
            if (query == null)
                query = new QueryParams();
            bool Visible = true;
            PageModel pageModel = new PageModel();
            try
            {
                string domain = _configuration.GetValue<string>("Host:JeeWork_API") + "/";
                string ConnectionString = JeeWorkLiteController.getConnectionString(ConnectionCache, loginData.CustomerID, _configuration);
                using (DpsConnection cnn = new DpsConnection(ConnectionString))
                {
                    #region Lấy dữ liệu account từ JeeAccount
                    DataAccount = JeeWorkLiteController.GetAccountFromJeeAccount(HttpContext.Request.Headers, _configuration);
                    if (DataAccount == null)
                        return JsonResultCommon.Custom("Lỗi lấy danh sách nhân viên từ hệ thống quản lý tài khoản");

                    List<AccUsernameModel> DataStaff = JeeWorkLiteController.GetMyStaff(HttpContext.Request.Headers, _configuration, loginData);
                    if (DataStaff == null)
                        return JsonResultCommon.ThanhCong(new List<string>());
                    List<string> nvs = DataStaff.Select(x => x.UserId.ToString()).ToList();
                    string listIDNV = string.Join(",", nvs);

                    string error = "";
                    string listID = JeeWorkLiteController.ListAccount(HttpContext.Request.Headers, out error, _configuration);
                    if (error != "")
                        return JsonResultCommon.Custom(error);
                    #endregion
                    if (string.IsNullOrEmpty(query.filter["id_nv"]))
                        return JsonResultCommon.Custom("Thành viên");
                    #region filter thời gian , keyword
                    DateTime from = Common.GetDateTime();
                    DateTime to = Common.GetDateTime();
                    if (!string.IsNullOrEmpty(query.filter["TuNgay"]))
                    {
                        bool from1 = DateTime.TryParseExact(query.filter["TuNgay"], "dd/MM/yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out from);
                        if (!from1)
                            return JsonResultCommon.Custom("Thời gian bắt đầu không hợp lệ");
                    }
                    if (!string.IsNullOrEmpty(query.filter["DenNgay"]))
                    {
                        bool to1 = DateTime.TryParseExact(query.filter["DenNgay"], "dd/MM/yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out to);
                        if (!to1)
                            return JsonResultCommon.Custom("Thời gian kết thúc không hợp lệ");
                    }
                    #endregion
                    string strW = $" and (w.id_nv in ({listIDNV}) or w.createdby in ({listIDNV}))";
                    string displayChild = "0";//hiển thị con: 0-không hiển thị, 1- 1 cấp con, 2- nhiều cấp con
                    if (!string.IsNullOrEmpty(query.filter["displayChild"]))
                        displayChild = query.filter["displayChild"];
                    #region group
                    string groupby = "";
                    string columnName = "";
                    string query_group = "";
                    DataTable dtG = new DataTable();
                    dtG.Columns.Add("id_row", typeof(object));
                    dtG.Columns.Add("title", typeof(string));
                    dtG.Columns.Add("image", typeof(string));
                    if (!string.IsNullOrEmpty(query.filter["groupby"]))
                    {
                        groupby = query.filter["groupby"];
                        // project,member
                        if ("project".Equals(groupby))
                        {
                            columnName = "id_project_team";
                            query_group = @"select distinct p.id_row, p.title,'' as image  from we_project_team_user u
                                    join we_project_team p on p.id_row=u.id_project_team 
                                    where u.disabled=0 and p.Disabled=0 and id_user=" + query.filter["id_nv"];
                            dtG = cnn.CreateDataTable(query_group);
                        }
                        if ("member".Equals(groupby))
                        {
                            dtG = new DataTable();
                            dtG.Columns.Add("id_row", typeof(object));
                            dtG.Columns.Add("title", typeof(string));
                            dtG.Columns.Add("image", typeof(string));
                            columnName = "Id_NV";
                            //using (DpsConnection cnnHR = new DpsConnection(_config.HRConnectionString))
                            //{
                            //    dt_data_group = Common.GetListByManager(loginData.UserID.ToString(), cnnHR);//id_nv, hoten...
                            //}
                            foreach (var item in DataStaff)
                            {
                                dtG.Rows.Add(new object[] { item.UserId, item.FullName, item.AvartarImgURL });
                            }
                        }
                    }
                    #endregion
                    if (dtG.Rows.Count == 0)
                        return JsonResultCommon.ThanhCong(new List<string>(), null, Visible);
                    //DataSet ds = getWork_IDNV(cnn, query, loginData.UserID, DataAccount, strW);
                    DataSet ds = getWork_IDNV(Request.Headers, cnn, query, loginData.UserID, DataAccount, strW);
                    if (cnn.LastError != null || ds == null)
                        return JsonResultCommon.Exception(_logger, cnn.LastError, _config, loginData, ControllerContext);
                    var temp = filterWork(ds.Tables[0].AsEnumerable().Where(x => x["id_parent"] == DBNull.Value), query.filter);//k bao gồm con
                    var tags = ds.Tables[1].AsEnumerable();
                    int total = temp.Count();
                    if (total == 0)
                        return JsonResultCommon.ThanhCong(new List<string>());
                    if (query.more)
                    {
                        query.page = 1;
                        query.record = total;
                    }
                    pageModel.TotalCount = total;
                    pageModel.AllPage = (int)Math.Ceiling(total / (decimal)query.record);
                    pageModel.Size = query.record;
                    pageModel.Page = query.page;
                    // Phân trang
                    var dtNew = temp.Skip((query.page - 1) * query.record).Take(query.record);
                    var dtChild = ds.Tables[0].AsEnumerable().Where(x => x["id_parent"] != DBNull.Value).AsEnumerable();
                    //DataTable dt_test = ds.Tables[0].AsEnumerable().Where(x => x["id_parent"] != DBNull.Value);
                    //DataTable dt_test = ds.Tables[0].AsEnumerable().Where(x => x["id_parent"] != DBNull.Value).CopyToDataTable();
                    dtNew = dtNew.Concat(dtChild);

                    var Children = from rr in dtG.AsEnumerable()
                                   select new
                                   {
                                       id = rr["id_row"],
                                       title = rr["title"],
                                       image = rr["image"],
                                       data = getChild(domain, loginData.CustomerID, columnName, displayChild, rr["id_row"], dtNew.CopyToDataTable().AsEnumerable(), tags, DataAccount, loginData, ConnectionString)
                                   };
                    return JsonResultCommon.ThanhCong(Children, pageModel, Visible);
                }
            }
            catch (Exception ex)
            {
                return JsonResultCommon.Exception(_logger, ex, _config, loginData);
            }
        }
        /// <summary>
        /// Ghi lại lịch sử công việc
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [Route("log-detail")]
        [HttpGet]
        public object LogDetail(long id)
        {
            UserJWT loginData = Ulities.GetUserByHeader(HttpContext.Request.Headers);
            if (loginData == null)
                return JsonResultCommon.DangNhap();
            try
            {
                string domain = _configuration.GetValue<string>("Host:JeeWork_API") + "/";
                string ConnectionString = JeeWorkLiteController.getConnectionString(ConnectionCache, loginData.CustomerID, _configuration);
                using (DpsConnection cnn = new DpsConnection(ConnectionString))
                {
                    #region Lấy dữ liệu account từ JeeAccount
                    DataAccount = JeeWorkLiteController.GetAccountFromJeeAccount(HttpContext.Request.Headers, _configuration);
                    if (DataAccount == null)
                        return JsonResultCommon.Custom("Lỗi lấy danh sách nhân viên từ hệ thống quản lý tài khoản");

                    string error = "";
                    string listID = JeeWorkLiteController.ListAccount(HttpContext.Request.Headers, out error, _configuration);
                    if (error != "")
                        return JsonResultCommon.Custom(error);
                    #endregion
                    string sql = @$"select l.*, act.action, act.action_en, act.format, act.sql,w.title,
                                 l.CreatedBy as Id_NV, '' AS Hoten, '' as Mobile, '' as Username, '' as Email, '' as image,
                                '' as CocauID, '' as CoCauToChuc,  '' as Id_Chucdanh, '' AS Tenchucdanh
                                from we_log l join we_log_action act on l.id_action = act.id_row
                                join we_work w on w.id_row = l.object_id
                                where act.object_type = 1 and view_detail=1 and l.CreatedBy in ({listID}) and l.id_row = " + id;
                    DataTable dt = cnn.CreateDataTable(sql);
                    if (dt == null || dt.Rows.Count == 0)
                        return JsonResultCommon.KhongTonTai("Hoạt động");
                    #region Map info account từ JeeAccount
                    foreach (DataRow item in dt.Rows)
                    {
                        var info = DataAccount.Where(x => item["id_nv"].ToString().Contains(x.UserId.ToString())).FirstOrDefault();

                        if (info != null)
                        {
                            item["hoten"] = info.FullName;
                            item["mobile"] = info.PhoneNumber;
                            item["Username"] = info.Username;
                            item["Email"] = info.Email;
                            item["image"] = info.AvartarImgURL;
                            item["Tenchucdanh"] = info.Jobtitle;
                        }
                    }
                    #endregion
                    if (dt.Rows[0]["sql"] != DBNull.Value)
                    {
                        DataTable temp = cnn.CreateDataTable(dt.Rows[0]["sql"].ToString(), new SqlConditions() { { "old", dt.Rows[0]["oldvalue"] }, { "new", dt.Rows[0]["newvalue"] } });
                        if (temp == null)
                            return JsonResultCommon.Exception(_logger, cnn.LastError, _config, loginData, ControllerContext);
                        dt.Rows[0]["oldvalue"] = temp.AsEnumerable().Where(x => x[0].ToString() == dt.Rows[0]["oldvalue"].ToString()).Select(x => x[1]).FirstOrDefault();
                        dt.Rows[0]["newvalue"] = temp.AsEnumerable().Where(x => x[0].ToString() == dt.Rows[0]["newvalue"].ToString()).Select(x => x[1]).FirstOrDefault();
                    }
                    if (dt.Rows[0]["format"] != DBNull.Value)
                    {
                        string f = "{0:" + dt.Rows[0]["format"].ToString() + "}";
                        if (dt.Rows[0]["oldvalue"] != DBNull.Value)
                            dt.Rows[0]["oldvalue"] = string.Format(f, DateTime.Parse(dt.Rows[0]["oldvalue"].ToString()));
                        if (dt.Rows[0]["newvalue"] != DBNull.Value)
                            dt.Rows[0]["newvalue"] = string.Format(f, DateTime.Parse(dt.Rows[0]["newvalue"].ToString()));
                    }
                    var data = (from r in dt.AsEnumerable()
                                select new
                                {
                                    id_row = r["id_row"],
                                    action = r["action"],
                                    action_en = r["action_en"],
                                    object_id = r["object_id"],
                                    title = r["title"],
                                    oldvalue = r["oldvalue"],
                                    newvalue = r["newvalue"],
                                    CreatedDate = string.Format("{0:dd/MM/yyyy HH:mm}", r["CreatedDate"]),
                                    NguoiTao = new
                                    {
                                        id_nv = r["id_nv"],
                                        hoten = r["hoten"],
                                        username = r["username"],
                                        tenchucdanh = r["tenchucdanh"],
                                        mobile = r["mobile"],
                                        image = r["image"]
                                        //image = JeeWorkLiteController.genLinkImage(domain, loginData.CustomerID, r["id_nv"].ToString(), _hostingEnvironment.ContentRootPath)
                                    }
                                }).FirstOrDefault();
                    return JsonResultCommon.ThanhCong(data);
                }
            }
            catch (Exception ex)
            {
                return JsonResultCommon.Exception(_logger, ex, _config, loginData);
            }
        }
        /// <summary>
        /// Import dữ liệu công việc từ excel
        /// </summary>
        /// <returns></returns>
        [Route("ImportData")]
        [HttpPost]
        public object ImportData(ImportWorkModel data)
        {
            UserJWT loginData = Ulities.GetUserByHeader(HttpContext.Request.Headers);
            if (loginData == null)
                return JsonResultCommon.DangNhap();
            try
            {
                string ConnectionString = JeeWorkLiteController.getConnectionString(ConnectionCache, loginData.CustomerID, _configuration);
                if (data.id_project_team <= 0)
                    return JsonResultCommon.BatBuoc("Dự án/phòng ban");
                if (data.Review || (!data.Review && data_import.dtW == null))
                {
                    #region Lấy dữ liệu account từ JeeAccount
                    DataAccount = JeeWorkLiteController.GetAccountFromJeeAccount(HttpContext.Request.Headers, _configuration);
                    if (DataAccount == null)
                        return JsonResultCommon.Custom("Lỗi lấy danh sách nhân viên từ hệ thống quản lý tài khoản");

                    string error = "";
                    string listID = JeeWorkLiteController.ListAccount(HttpContext.Request.Headers, out error, _configuration);
                    if (error != "")
                        return JsonResultCommon.Custom(error);
                    #endregion
                    string CustemerID = loginData.CustomerID.ToString();
                    string filesave = "", filename = "";
                    if (!UploadHelper.UploadFile(data.File, data.FileName, "/AttWork/", _hostingEnvironment.ContentRootPath, ref filename, _configuration))
                        return JsonResultCommon.Custom("Upload file thất bại");
                    //BLayer.General g = new BLayer.General();

                    //if (!g.SaveFile(data.File, data.FileName, loginData.CustomerID.ToString(), "/AttWork/", out filesave, out filename))
                    //    return JsonResultCommon.Custom("Upload file thất bại");
                    string loi = "";
                    //filename = string.Format("{0}{1}", AppDomain.CurrentDomain.BaseDirectory, "/AttWork/");
                    DataTable dt = Common.ReaddataFromXLSFile(_hostingEnvironment.ContentRootPath + "/dulieu/" + filename, data.Sheet, out loi);
                    if (!loi.Equals(""))
                        return JsonResultCommon.Custom("File không hợp lệ");
                    //List<string> headers = new List<string>() { "STT","Tên công việc","Người giao","Người thực hiện","Người theo dõi","Ưu tiên(urgent)","Tags","Ngày bắt đầu","Deadline","Hoàn thành thực tế","Mô tả công việc","Trạng thái","Kết quả công việc","Mục tiêu" };
                    if (!dt.Columns.Contains("STT"))
                        return JsonResultCommon.Custom("Dữ liệu không đúng định dạng");
                    DataSet ds;
                    using (DpsConnection cnn = new DpsConnection(ConnectionString))
                    {
                        string sql = "select id_row, title from we_group where disabled=0 and id_project_team=@id";
                        sql += @$";select id_user, '' as hoten, '' as Username from we_project_team_user u
where disabled = 0 and u.id_user in ({listID}) and id_project_team = @id";
                        sql += ";select id_row, title from we_tag where disabled=0 and id_project_team=@id";
                        sql += ";select id_row, title from we_milestone where disabled=0 and id_project_team=@id";
                        sql += ";select * from we_work where 1=0";//lấy cấu trúc
                        ds = cnn.CreateDataSet(sql, new SqlConditions() { { "id", data.id_project_team } });
                        #region Map info account từ JeeAccount
                        foreach (DataRow item in ds.Tables[1].Rows)
                        {
                            var info = DataAccount.Where(x => item["id_user"].ToString().Contains(x.UserId.ToString())).FirstOrDefault();
                            if (info != null)
                            {
                                item["hoten"] = info.FullName;
                                item["Username"] = info.Username;
                            }
                        }
                        #endregion
                        cnn.Disconnect();
                        DataTable dtPK = new DataTable();
                        dtPK.Columns.Add(new DataColumn() { ColumnName = "id_row" });
                        dtPK.Columns.Add(new DataColumn() { ColumnName = "title" });
                        dtPK.Columns.Add(new DataColumn() { ColumnName = "table" });//T:tag, M:milestone
                        dtPK.Columns.Add(new DataColumn() { ColumnName = "stt" });//stt tạm
                        dtPK.Clear();

                        DataTable dtMapU = new DataTable();
                        dtMapU.Columns.Add(new DataColumn() { ColumnName = "id_work" });
                        dtMapU.Columns.Add(new DataColumn() { ColumnName = "id_user" });
                        dtMapU.Columns.Add(new DataColumn() { ColumnName = "loai" });
                        dtMapU.Columns.Add(new DataColumn() { ColumnName = "createdby" });
                        dtMapU.Clear();

                        DataTable dtMapT = new DataTable();
                        dtMapT.Columns.Add(new DataColumn() { ColumnName = "id_work" });
                        dtMapT.Columns.Add(new DataColumn() { ColumnName = "id_tag" });
                        dtMapT.Clear();

                        var asG = ds.Tables[0].AsEnumerable();
                        var asU = ds.Tables[1].AsEnumerable();
                        var asT = ds.Tables[2].AsEnumerable();
                        var asM = ds.Tables[3].AsEnumerable();
                        DataTable dtW = ds.Tables[4].Clone();
                        dtW.Columns["id_milestone"].DataType = typeof(String);
                        dtW.Columns["id_group"].DataType = typeof(String);
                        List<ReviewModel> review = new List<ReviewModel>();
                        if (dt.Rows.Count > 0)
                        {
                            DataTable dtP = dt.Clone();
                            dtP.Columns.Add(new DataColumn() { ColumnName = "milestone" });
                            dtP.Columns.Add(new DataColumn() { ColumnName = "id_tags" });
                            dtP.Columns.Add(new DataColumn() { ColumnName = "note" });
                            dtP.Columns.Add(new DataColumn() { ColumnName = "error" });
                            dtP.Columns.Add(new DataColumn() { ColumnName = "id_parent" });
                            string s = "#";
                            List<string> parents = new List<string>() { };
                            var group = new
                            {
                                id_row = "",
                                title = "",
                            };
                            bool firstR = false;
                            for (int i = 0; i < dt.Rows.Count; i++)
                            {
                                DataRow drW = dtW.NewRow();
                                DataRow dr = dt.Rows[i];
                                if (dr["STT"].ToString() == "")//group
                                {
                                    //var find = asG.Where(x => x["title"].ToString() == dr[1].ToString()).FirstOrDefault();
                                    //if (find == null)
                                    //{
                                    //    int stt = dtPK.Rows.Count + 1;
                                    //    dtPK.Rows.Add(new object[] { 0, dr[1].ToString(), "we_group", stt });
                                    //    group = new
                                    //    {
                                    //        id_row = "stt_" + stt,
                                    //        title = dr[1].ToString(),
                                    //    };
                                    //}
                                    //else
                                    //{
                                    //    group = new
                                    //    {
                                    //        id_row = find["id_row"].ToString(),
                                    //        title = dr[1].ToString(),
                                    //    };
                                    //}
                                    firstR = true;
                                    continue;
                                }
                                if (firstR && dr[1].ToString().Contains("#"))
                                    return JsonResultCommon.Custom("Công việc đầu tiên không được là công việc con");
                                firstR = false;
                                var arr = dr.ItemArray.ToList();
                                string id_milestone = "";
                                string id_tags = "";
                                #region check data
                                string n = "";
                                if (dr["Mục tiêu"].ToString() != "")
                                {
                                    var f = asM.Where(x => x["title"].ToString() == dr["Mục tiêu"].ToString()).FirstOrDefault();
                                    if (f != null)
                                    {
                                        id_milestone = f["id_row"].ToString();
                                        drW["id_milestone"] = id_milestone;
                                    }
                                    else
                                    {
                                        n += "Mục tiêu sẽ được thêm mới; ";
                                        f = dtPK.AsEnumerable().Where(x => x["table"].ToString() == "we_milestone" && x["title"].ToString() == dr["Mục tiêu"].ToString()).FirstOrDefault();
                                        if (f == null)
                                        {
                                            int stt = dtPK.Rows.Count + 1;
                                            dtPK.Rows.Add(new object[] { 0, dr["Mục tiêu"].ToString(), "we_milestone", stt });
                                            drW["id_milestone"] = "stt_" + stt;
                                        }
                                        else
                                        {
                                            drW["id_milestone"] = "stt_" + f["stt"].ToString();
                                        }
                                    }
                                }


                                if (dr["Tags"].ToString() != "")
                                {
                                    var splt = dr["Tags"].ToString().Split(',');
                                    string tttt = "";
                                    foreach (string tag in splt)
                                    {
                                        var f = asT.Where(x => x["title"].ToString() == tag).FirstOrDefault();
                                        if (f != null)
                                        {
                                            id_tags += f["id_row"].ToString();
                                            dtMapT.Rows.Add(new object[] { dr["STT"].ToString(), f["id_row"] });
                                        }
                                        else
                                        {
                                            tttt += tag + ",";
                                            f = dtPK.AsEnumerable().Where(x => x["table"].ToString() == "we_tag" && x["title"].ToString() == tag).FirstOrDefault();
                                            if (f == null)
                                            {
                                                int stt = dtPK.Rows.Count + 1;
                                                dtPK.Rows.Add(new object[] { 0, tag, "we_tag", stt });
                                                dtMapT.Rows.Add(new object[] { dr["STT"].ToString(), stt });
                                            }
                                        }
                                    }
                                    if (tttt != "")
                                        n += "Tag " + tttt.Remove(tttt.Length - 1) + " sẽ được thêm mới; ";
                                }
                                string e = "";
                                if (dr["Tên công việc"].ToString() == "")
                                    e += "Tên công việc là bắt buộc;";
                                else
                                    drW["title"] = dr["Tên công việc"].ToString();
                                if (dr["Mô tả công việc"].ToString() != "")
                                    drW["description"] = dr["Mô tả công việc"].ToString();
                                if (dr["Ưu tiên (urgent)"].ToString() != "")
                                    drW["urgent"] = dr["Ưu tiên (urgent)"].ToString() == "Yes";
                                else
                                    drW["urgent"] = false;
                                //if ((dr["Người giao"].ToString() != "" && dr["Người thực hiện"].ToString() == "") || (dr["Người giao"].ToString() == "" && dr["Người thực hiện"].ToString() != ""))
                                //{
                                //    e +="Có người giao phải có người thực hie; ";
                                //}    
                                long nguoigiao = loginData.UserID;
                                if (dr["Người giao"].ToString() != "")
                                {
                                    var f = asU.Where(x => x["username"].ToString() == dr["Người giao"].ToString()).FirstOrDefault();
                                    if (f == null)
                                        e += "Người giao không đúng; ";
                                    else
                                        nguoigiao = long.Parse(f["id_user"].ToString());
                                }
                                else
                                {
                                    if (dr["Người thực hiện"].ToString() != "")
                                    {
                                        //dr["Người giao"] = loginData.UserName;
                                        n += "Người import sẽ là người giao;";
                                    }
                                }
                                if (dr["Người thực hiện"].ToString() != "")
                                {
                                    var f = asU.Where(x => x["username"].ToString() == dr["Người thực hiện"].ToString()).FirstOrDefault();
                                    if (f == null)
                                        e += "Người thực hiện không đúng; ";
                                    else
                                    {
                                        dtMapU.Rows.Add(new object[] { dr["STT"].ToString(), f["id_user"], 1, nguoigiao });
                                    }
                                }
                                if (dr["Người theo dõi"].ToString() != "")
                                {
                                    var count = dr["Tags"].ToString().Split(',').Length;
                                    var f = asU.Where(x => dr["Người theo dõi"].ToString().Contains(x["username"].ToString()));
                                    if (f == null || f.Count() != count)
                                        e += "Người theo dõi không đúng; ";
                                    else
                                    {
                                        foreach (var u in f)
                                        {
                                            dtMapU.Rows.Add(new object[] { dr["STT"].ToString(), u["id_user"], 2, nguoigiao });
                                        }
                                    }
                                }
                                if (dr["Ngày bắt đầu"].ToString() != "")
                                {
                                    DateTime d = DateTime.MinValue;
                                    bool from1 = DateTime.TryParseExact(dr["Ngày bắt đầu"].ToString(), "dd/MM/yyyy HH:mm", CultureInfo.InvariantCulture, DateTimeStyles.None, out d);
                                    if (!from1)
                                        e += "Ngày bắt đầu không hợp lệ; ";
                                    else
                                        drW["start_date"] = d;
                                }
                                if (dr["Hoàn thành thực tế"].ToString() != "")
                                {
                                    DateTime d = DateTime.MinValue;
                                    bool from1 = DateTime.TryParseExact(dr["Hoàn thành thực tế"].ToString(), "dd/MM/yyyy HH:mm", CultureInfo.InvariantCulture, DateTimeStyles.None, out d);
                                    if (!from1)
                                        e += "Ngày hoàn thành thực tế không hợp lệ; ";
                                    else
                                        drW["end_date"] = d;
                                }
                                if (dr["Deadline"].ToString() != "")
                                {
                                    DateTime d = DateTime.MinValue;
                                    bool from1 = DateTime.TryParseExact(dr["Deadline"].ToString(), "dd/MM/yyyy HH:mm", CultureInfo.InvariantCulture, DateTimeStyles.None, out d);
                                    if (!from1)
                                        e += "Deadline không hợp lệ; ";
                                    else
                                        drW["deadline"] = d;
                                }

                                if (dr["Trạng thái"].ToString() != "")
                                {

                                }
                                else
                                {
                                    DataTable table = JeeWorkLiteController.StatusDynamic(data.id_project_team, new List<AccUsernameModel>(), cnn);
                                    if (dt.Rows.Count > 0)
                                    {
                                        DataRow[] RowStatus = table.Select("IsDefault = 1 and IsFinal = 0");
                                        if (RowStatus.Length > 0)
                                        {
                                            drW["status"] = RowStatus[0]["id_row"];
                                        }
                                        else
                                        {
                                            continue;
                                        }
                                    }
                                }
                                #endregion
                                arr.Add(id_milestone);//nếu id_milestone="" thì thêm mới
                                arr.Add(id_tags);
                                arr.Add(n);
                                arr.Add(e);
                                if (!dr[1].ToString().StartsWith("#"))//cha
                                {
                                    arr.Add(DBNull.Value);
                                    dtP.Rows.Add(arr.ToArray());
                                    s = "#";
                                    parents = new List<string>() { };
                                }
                                else
                                {
                                    drW["title"] = drW["title"].ToString().Replace(s, "");
                                    if (parents.Count == 0)
                                        parents.Add(dtP.Rows[dtP.Rows.Count - 1]["STT"].ToString());
                                    arr.Add(parents.Last());
                                    dtP.Rows.Add(arr.ToArray());
                                    drW["id_parent"] = parents.Last();
                                    if (i + 1 < dt.Rows.Count)
                                    {
                                        string str = dt.Rows[i + 1][1].ToString();
                                        if (str.StartsWith(s + "#"))//tiếp theo là con của dòng hiện tại
                                        {
                                            parents.Add(dr["STT"].ToString());
                                            s += "#";
                                        }
                                        else
                                        {
                                            if (!str.StartsWith(s))//tiếp theo không cùng cấp=>cha
                                            {
                                                while (s.Length > 1 && !str.StartsWith(s + "#"))
                                                {
                                                    s = s.Remove(s.Length - 1);
                                                    parents.RemoveAt(parents.Count - 1);
                                                }
                                            }
                                        }
                                    }
                                }
                                //drW["id_group"] = group.id_row;
                                if (e != "")
                                    drW["disabled"] = true;
                                dtW.Rows.Add(drW);
                                if (i + 1 == dt.Rows.Count || dt.Rows[i + 1]["STT"].ToString() == "")//group
                                {
                                    var w = dtP.Copy();
                                    dtP.Clear();
                                    var aaa = new ReviewModel()
                                    {
                                        id_row = group.id_row,
                                        title = group.title == "" ? "Chưa phân loại" : group.title,
                                        note = (group.id_row == "" && group.title != "") ? "Nhóm làm việc sẽ được thêm mới" : "",
                                        dtW = w
                                    };
                                    review.Add(aaa);
                                }
                            }
                        }

                        data_import.dtPK = dtPK;
                        data_import.dtW = dtW;
                        data_import.dtUser = dtMapU;
                        data_import.dtTag = dtMapT;
                        if (data.Review)
                        {
                            return JsonResultCommon.ThanhCong(review);
                        }
                    }
                }
                using (DpsConnection cnn = new DpsConnection(ConnectionString))
                {
                    int dem = 0;
                    int total = data_import.dtW.Rows.Count;
                    cnn.BeginTransaction();
                    //Hashtable val = new Hashtable();
                    //val["CreatedDate"] = Common.GetDateTime();
                    //val["CreatedBy"] = loginData.UserID;
                    //val["id_project_team"] = data.id_project_team;
                    #region insert tag mới, milestone mới, group
                    foreach (DataRow dr in data_import.dtPK.Rows)
                    {
                        Hashtable vl = new Hashtable();
                        vl["CreatedDate"] = Common.GetDateTime();
                        vl["CreatedBy"] = loginData.UserID;
                        vl["id_project_team"] = data.id_project_team;
                        vl["title"] = dr["title"];
                        if (dr["table"].ToString() != "we_tag")
                            vl["description"] = "";
                        if (dr["table"].ToString() == "we_milestone")
                        {
                            vl["deadline"] = Common.GetDateTime();
                            vl["person_in_charge"] = loginData.UserID;
                        }
                        if (cnn.Insert(vl, dr["table"].ToString()) <= 0)
                        {
                            cnn.RollbackTransaction();
                            return JsonResultCommon.Exception(_logger, cnn.LastError, _config, loginData, ControllerContext);
                        }
                        dr["id_row"] = long.Parse(cnn.ExecuteScalar("select IDENT_CURRENT('" + dr["table"].ToString() + "') ").ToString());
                    }
                    #endregion

                    Hashtable valW = new Hashtable();
                    valW["CreatedDate"] = Common.GetDateTime();
                    valW["CreatedBy"] = loginData.UserID;
                    valW["id_project_team"] = data.id_project_team;
                    DataTable dt = JeeWorkLiteController.StatusDynamic(data.id_project_team, new List<AccUsernameModel>(), cnn);
                    if (dt.Rows.Count > 0)
                    {
                        DataRow[] RowStatus = dt.Select("IsDefault = 1 and IsFinal = 0");
                        if (RowStatus.Length > 0)
                        {
                            valW["status"] = RowStatus[0]["id_row"];
                        }
                    }
                    for (int i = 0; i < data_import.dtW.Rows.Count; i++)
                    {
                        DataRow dr = data_import.dtW.Rows[i];
                        if (dr["disabled"] == DBNull.Value || !(bool)dr["disabled"])
                        {
                            valW["title"] = dr["title"];
                            valW["description"] = dr["description"];
                            valW["result"] = dr["result"];
                            valW["urgent"] = dr["urgent"] == DBNull.Value ? false : dr["urgent"];
                            if (dr["id_milestone"].ToString() != "")
                            {
                                string s = dr["id_milestone"].ToString();
                                if (s.Contains("stt_"))
                                {
                                    s = data_import.dtPK.AsEnumerable().Where(x => "stt_" + x["stt"].ToString() == s).First()["id_row"].ToString();
                                }
                                valW["id_milestone"] = s;
                            }
                            if (dr["id_group"].ToString() != "")
                            {
                                string s = dr["id_group"].ToString();
                                if (s.Contains("stt_"))
                                {
                                    s = data_import.dtPK.AsEnumerable().Where(x => "stt_" + x["stt"].ToString() == s).First()["id_row"].ToString();
                                }
                                valW["id_group"] = s;
                            }
                            valW["start_date"] = dr["start_date"];
                            valW["end_date"] = dr["end_date"];
                            valW["Deadline"] = dr["Deadline"];
                            if (dr["id_parent"] != DBNull.Value)
                            {
                                int index = int.Parse(dr["id_parent"].ToString());
                                valW["id_parent"] = data_import.dtW.Rows[index]["id_row"];
                            }
                            if (cnn.Insert(valW, "we_work") < 0)
                            {
                                cnn.RollbackTransaction();
                                return JsonResultCommon.Exception(_logger, cnn.LastError, _config, loginData, ControllerContext);
                            }
                            long idW = long.Parse(cnn.ExecuteScalar("select IDENT_CURRENT('we_work') ").ToString());
                            dr["id_row"] = idW;
                            var filter = data_import.dtUser.AsEnumerable().Where(x => int.Parse(x["id_work"].ToString()) == i + 1);
                            foreach (DataRow rr in filter)
                            {
                                Hashtable val = new Hashtable();
                                val["createddate"] = Common.GetDateTime();
                                val["createdby"] = rr["createdby"];
                                val["id_work"] = idW;
                                val["id_user"] = rr["id_user"];
                                val["loai"] = rr["loai"];
                                if (cnn.Insert(val, "we_work_user") < 0)
                                {
                                    cnn.RollbackTransaction();
                                    return JsonResultCommon.Exception(_logger, cnn.LastError, _config, loginData, ControllerContext);
                                }
                            }
                            filter = data_import.dtTag.AsEnumerable().Where(x => int.Parse(x["id_work"].ToString()) == i + 1);
                            foreach (DataRow rr in filter)
                            {
                                Hashtable val = new Hashtable();
                                val["createddate"] = Common.GetDateTime();
                                val["createdby"] = loginData.UserID;
                                val["id_work"] = idW;

                                string s = rr["id_tag"].ToString();
                                if (s.Contains("stt_"))
                                {
                                    s = data_import.dtPK.AsEnumerable().Where(x => "stt_" + x["stt"].ToString() == s).First()["id_row"].ToString();
                                }
                                val["id_tag"] = s;
                                if (cnn.Insert(val, "we_work_tag") < 0)
                                {
                                    cnn.RollbackTransaction();
                                    return JsonResultCommon.Exception(_logger, cnn.LastError, _config, loginData, ControllerContext);
                                }
                            }
                            //Insert người follow cho từng tình trạng của công việc
                            DataTable dt_status = JeeWorkLiteController.StatusDynamic(data.id_project_team, new List<AccUsernameModel>(), cnn);
                            if (dt_status.Rows.Count > 0)
                            {
                                foreach (DataRow item in dt_status.Rows)
                                {
                                    Hashtable val = new Hashtable();
                                    val.Add("id_project_team", data.id_project_team);
                                    val.Add("workid", idW);
                                    val.Add("statusid", item["id_row"]);
                                    if (string.IsNullOrEmpty(item["follower"].ToString()))
                                        val.Add("checker", DBNull.Value);
                                    else
                                        val.Add("checker", item["follower"]);
                                    val.Add("createddate", Common.GetDateTime());
                                    val.Add("createdby", loginData.UserID);
                                    if (cnn.Insert(val, "we_work_process") != 1)
                                    {
                                        cnn.RollbackTransaction();
                                        return JsonResultCommon.Exception(_logger, cnn.LastError, _config, loginData, ControllerContext);
                                    }
                                    long processid = long.Parse(cnn.ExecuteScalar("select IDENT_CURRENT('we_work_process')").ToString());
                                    val = new Hashtable();
                                    val.Add("processid", processid);
                                    if (string.IsNullOrEmpty(item["follower"].ToString()))
                                    {
                                        val.Add("new_checker", DBNull.Value);
                                    }
                                    else
                                    {
                                        val.Add("new_checker", item["follower"]);
                                        var info = DataAccount.Where(x => item["follower"].ToString().Contains(x.UserId.ToString())).FirstOrDefault();
                                        if (info != null)
                                        {
                                            val.Add("content_note", loginData.customdata.personalInfo.Fullname + " thêm " + info.FullName + " vào theo dõi");
                                        }
                                    }
                                    val.Add("createddate", Common.GetDateTime());
                                    val.Add("createdby", loginData.UserID);
                                    if (cnn.Insert(val, "we_work_process_log") != 1)
                                    {
                                        cnn.RollbackTransaction();
                                        return JsonResultCommon.Exception(_logger, cnn.LastError, _config, loginData, ControllerContext);
                                    }
                                }
                            }
                            dem++;
                        }
                    }

                    cnn.EndTransaction();
                    var re = new
                    {
                        total = total,
                        success = dem
                    };
                    return JsonResultCommon.ThanhCong(re);
                }
            }
            catch (Exception ex)
            {
                return JsonResultCommon.Exception(_logger, ex, _config, loginData);
            }
        }
        /// <summary>
        /// Update giá trị các cột mới của công việc
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        [Route("update_new-field")]
        [HttpPost]
        public object UpdateNewField(UpdateWorkModel data)
        {
            UserJWT loginData = Ulities.GetUserByHeader(HttpContext.Request.Headers);
            if (loginData == null)
                return JsonResultCommon.DangNhap();
            try
            {
                string log_content = "";
                string ConnectionString = JeeWorkLiteController.getConnectionString(ConnectionCache, loginData.CustomerID, _configuration);
                using (DpsConnection cnn = new DpsConnection(ConnectionString))
                {
                    SqlConditions sqlcond = new SqlConditions();
                    sqlcond.Add("id_row", data.WorkID);
                    sqlcond.Add("disabled", 0);
                    string s = "select title as tencongviec_old, * from v_wework_new where (where)";
                    DataTable old = cnn.CreateDataTable(s, "(where)", sqlcond);
                    if (old == null || old.Rows.Count == 0)
                        return JsonResultCommon.KhongTonTai("Công việc");
                    SqlConditions cond_status = new SqlConditions();
                    long iduser = loginData.UserID;
                    long idk = loginData.CustomerID;
                    string workname = "";
                    long id_project_team = 0;
                    long StatusPresent = 0;
                    DataTable dt_infowork = cnn.CreateDataTable("select title, id_project_team, status " +
                        "from we_work " +
                        "where id_row = @id_row", new SqlConditions() { { "id_row", data.WorkID } });
                    if (dt_infowork.Rows.Count > 0)
                    {
                        workname = dt_infowork.Rows[0]["title"].ToString();
                        id_project_team = long.Parse(dt_infowork.Rows[0]["id_project_team"].ToString());
                        StatusPresent = long.Parse(dt_infowork.Rows[0]["status"].ToString());
                    }
                    Hashtable has = new Hashtable();
                    has.Add("ID_project_team", id_project_team);
                    has.Add("FieldID", data.FieldID);
                    has.Add("WorkID", data.WorkID);
                    has.Add("TypeID", data.TypeID);
                    has.Add("Value", data.Value);
                    DataTable dt = cnn.CreateDataTable(s, "(where)", sqlcond);
                    sqlcond = new SqlConditions();
                    sqlcond.Add("ID_project_team", id_project_team);
                    sqlcond.Add("FieldID", data.FieldID);
                    sqlcond.Add("WorkID", data.WorkID);
                    sqlcond.Add("TypeID", data.TypeID);
                    string sa = "select * from we_newfileds_values where (where)";
                    DataTable dt_values = cnn.CreateDataTable(sa, "(where)", sqlcond);
                    cnn.BeginTransaction();
                    if (dt_values.Rows.Count <= 0)
                    {
                        if (cnn.Insert(has, "we_newfileds_values") != 1)
                        {
                            cnn.RollbackTransaction();
                            return JsonResultCommon.Exception(_logger, cnn.LastError, _config, loginData, ControllerContext);
                        }
                    }
                    else
                    {
                        if (cnn.Update(has, sqlcond, "we_newfileds_values") != 1)
                        {
                            cnn.RollbackTransaction();
                            return JsonResultCommon.Exception(_logger, cnn.LastError, _config, loginData, ControllerContext);
                        }
                    }
                    bool re = true;
                    re = JeeWorkLiteController.log(_logger, loginData.Username, cnn, 1, data.id_row, iduser, log_content);
                    if (!re)
                    {
                        cnn.RollbackTransaction();
                        return JsonResultCommon.Exception(_logger, cnn.LastError, _config, loginData, ControllerContext);
                    }
                    #region Ghi log trong project
                    string LogContent = "", LogEditContent = "";
                    LogEditContent = Common.GetEditLogContent(old, dt);
                    if (!LogEditContent.Equals(""))
                    {
                        LogEditContent = "Chỉnh sửa dữ liệu (" + data.id_row + ") : " + LogEditContent;
                        LogContent = "Chỉnh sửa dữ liệu UpdateNewField (" + data.id_row + "), Chi tiết xem trong log chỉnh sửa chức năng";
                    }
                    Common.Ghilogfile(loginData.CustomerID.ToString(), LogEditContent, LogContent, loginData.Username, ControllerContext);
                    #endregion
                    #region Ghi log lên CDN
                    var d2 = new ActivityLog()
                    {
                        username = loginData.Username,
                        category = LogEditContent,
                        action = loginData.customdata.personalInfo.Fullname + " thao tác",
                        data = JsonConvert.SerializeObject(data)
                    };
                    _logger.LogInformation(JsonConvert.SerializeObject(d2));
                    #endregion

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
        /// Lịch sử chi tiết thao tác công việc
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [Route("log-detail-by-work")]
        [HttpGet]
        public object LogDetailByWork(long id)
        {
            UserJWT loginData = Ulities.GetUserByHeader(HttpContext.Request.Headers);
            if (loginData == null)
                return JsonResultCommon.DangNhap();
            try
            {
                string domain = _configuration.GetValue<string>("Host:JeeWork_API") + "/";
                string ConnectionString = JeeWorkLiteController.getConnectionString(ConnectionCache, loginData.CustomerID, _configuration);
                using (DpsConnection cnn = new DpsConnection(ConnectionString))
                {
                    #region Lấy dữ liệu account từ JeeAccount
                    DataAccount = JeeWorkLiteController.GetAccountFromJeeAccount(HttpContext.Request.Headers, _configuration);
                    if (DataAccount == null)
                        return JsonResultCommon.Custom("Lỗi lấy danh sách nhân viên từ hệ thống quản lý tài khoản");

                    string error = "";
                    string listID = JeeWorkLiteController.ListAccount(HttpContext.Request.Headers, out error, _configuration);
                    if (error != "")
                        return JsonResultCommon.Custom(error);
                    #endregion
                    string sql = @$" select l.*, act.action, act.action_en, act.format, act.sql,w.title, 
                                l.CreatedBy as Id_NV, '' AS Hoten, '' as Mobile, '' as Username, '' as Email, '' as image,
                                '' as CocauID, '' as CoCauToChuc,  '' as Id_Chucdanh, '' AS Tenchucdanh, '' as ColorStatus_Old, '' as ColorStatus_New 
                                from we_log l join we_log_action act on l.id_action = act.id_row
                                join we_work w on w.id_row = l.object_id
                                where act.object_type = 1 and view_detail=1 and l.object_id = " + id + " order by l.CreatedDate";
                    DataTable dt = new DataTable();
                    dt = cnn.CreateDataTable(sql);
                    if (dt == null || cnn.LastError != null)
                        return JsonResultCommon.KhongTonTai("Công việc");
                    foreach (DataRow item in dt.Rows)
                    {
                        if (item["sql"] != DBNull.Value)
                        {
                            DataTable temp = cnn.CreateDataTable(item["sql"].ToString(), new SqlConditions() { { "old", item["oldvalue"] }, { "new", item["newvalue"] } });
                            if (temp == null)
                                return JsonResultCommon.Exception(_logger, cnn.LastError, _config, loginData, ControllerContext);
                            if (int.Parse(item["id_action"].ToString()) == 44) // Đối với status trả về thêm Color
                            {
                                item["ColorStatus_Old"] = temp.AsEnumerable().Where(x => x[0].ToString() == item["oldvalue"].ToString()).Select(x => x[2]).FirstOrDefault();
                                item["ColorStatus_New"] = temp.AsEnumerable().Where(x => x[0].ToString() == item["newvalue"].ToString()).Select(x => x[2]).FirstOrDefault();
                            }
                            if (int.Parse(item["id_action"].ToString()) == 9 || int.Parse(item["id_action"].ToString()) == 5 || int.Parse(item["id_action"].ToString()) == 6) // Đối với tag gắn title
                            {
                                if (temp.Rows.Count > 0)
                                    item["action"] = item["action"].ToString().Replace("{0}", temp.Rows[0]["title"].ToString());
                                else
                                    item["action"] = item["action"].ToString().Replace("{0}", "");
                            }
                            item["oldvalue"] = temp.AsEnumerable().Where(x => x[0].ToString() == item["oldvalue"].ToString()).Select(x => x[1]).FirstOrDefault();
                            item["newvalue"] = temp.AsEnumerable().Where(x => x[0].ToString() == item["newvalue"].ToString()).Select(x => x[1]).FirstOrDefault();
                        }
                        if (item["format"] != DBNull.Value)
                        {
                            string f = "{0:" + item["format"].ToString() + "}";
                            if (item["oldvalue"] != DBNull.Value)
                                item["oldvalue"] = string.Format(f, DateTime.Parse(item["oldvalue"].ToString()));
                            if (item["newvalue"] != DBNull.Value)
                                item["newvalue"] = string.Format(f, DateTime.Parse(item["newvalue"].ToString()));
                        }

                        #region Map info account từ JeeAccount 
                        var info = DataAccount.Where(x => item["id_nv"].ToString().Contains(x.UserId.ToString())).FirstOrDefault();

                        if (info != null)
                        {
                            item["hoten"] = info.FullName;
                            item["mobile"] = info.PhoneNumber;
                            item["Username"] = info.Username;
                            item["Email"] = info.Email;
                            item["image"] = info.AvartarImgURL;
                            item["Tenchucdanh"] = info.Jobtitle;
                        }
                        // giao việc, bỏ giao việc cho user
                        if (item["id_action"].ToString().Equals("15") || item["id_action"].ToString().Equals("55") || item["id_action"].ToString().Equals("56") || item["id_action"].ToString().Equals("57"))
                        {
                            var infoUser = DataAccount.Where(x => item["newvalue"].ToString().Contains(x.UserId.ToString())).FirstOrDefault();
                            if (infoUser != null)
                            {
                                item["action"] = item["action"].ToString().Replace("{0}", infoUser.FullName);
                                item["action_en"] = item["action_en"].ToString().Replace("{0}", infoUser.FullName);
                            }
                            item["oldvalue"] = DBNull.Value;
                            item["newvalue"] = DBNull.Value;

                        }
                        #endregion
                    }
                    var data = (from r in dt.AsEnumerable()
                                select new
                                {
                                    id_row = r["id_row"],
                                    action = r["action"],
                                    action_en = r["action_en"],
                                    object_id = r["object_id"],
                                    title = r["title"],
                                    oldvalue = r["oldvalue"],
                                    newvalue = r["newvalue"],
                                    id_action = r["id_action"],
                                    colornew = r["ColorStatus_New"],
                                    colorold = r["ColorStatus_Old"],
                                    CreatedDate = string.Format("{0:dd/MM/yyyy HH:mm}", r["CreatedDate"]),
                                    NguoiTao = new
                                    {
                                        id_nv = r["id_nv"],
                                        hoten = r["hoten"],
                                        username = r["username"],
                                        tenchucdanh = r["tenchucdanh"],
                                        mobile = r["mobile"],
                                        image = r["image"],
                                    }
                                });
                    return JsonResultCommon.ThanhCong(data);
                }
            }
            catch (Exception ex)
            {
                return JsonResultCommon.Exception(_logger, ex, _config, loginData);
            }
        }
        /// <summary>
        /// Xuất excel công việc
        /// </summary>
        /// <param name="query"></param>
        /// <returns></returns>
        [Route("ExportExcel")]
        [HttpGet]
        public async Task<IActionResult> ExportExcel([FromQuery] QueryParams query)
        {
            UserJWT loginData = Ulities.GetUserByHeader(HttpContext.Request.Headers);
            if (loginData == null)
                return Unauthorized();
            try
            {
                if (string.IsNullOrEmpty(query.filter["id_project_team"]))
                    return BadRequest();
                #region Lấy dữ liệu account từ JeeAccount
                DataAccount = JeeWorkLiteController.GetAccountFromJeeAccount(HttpContext.Request.Headers, _configuration);
                if (DataAccount == null)
                    return BadRequest();

                string error = "";
                string listID = JeeWorkLiteController.ListAccount(HttpContext.Request.Headers, out error, _configuration);
                if (error != "")
                    return BadRequest();
                #endregion
                #region filter thời gian , keyword
                DateTime from = Common.GetDateTime();
                DateTime to = Common.GetDateTime();
                if (!string.IsNullOrEmpty(query.filter["TuNgay"]))
                {
                    bool from1 = DateTime.TryParseExact(query.filter["TuNgay"], "dd/MM/yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out from);
                    if (!from1)
                        return BadRequest();
                }
                if (!string.IsNullOrEmpty(query.filter["DenNgay"]))
                {
                    bool to1 = DateTime.TryParseExact(query.filter["DenNgay"], "dd/MM/yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out to);
                    if (!to1)
                        return BadRequest();
                }
                #endregion
                string displayChild = "0";//hiển thị con: 0-không hiển thị, 1- 1 cấp con, 2- nhiều cấp con
                if (!string.IsNullOrEmpty(query.filter["displayChild"]))
                    displayChild = query.filter["displayChild"];
                string[] header = { "Tên công việc", "Người giao", "Người thực hiện", "Người theo dõi", "Ưu tiên (urgent)", "Tags", "Ngày bắt đầu", "Deadline", "Hoàn thành thực tế", "Mô tả công việc", "Trạng thái", "Kết quả công việc", "Mục tiêu", "Ngày tạo", "Mã công việc (ID)" };
                DataTable dt = new DataTable();
                var temp = (from c in header
                            select new DataColumn() { ColumnName = c }).ToList();
                temp.Add(new DataColumn() { ColumnName = "merge_row" });
                temp.Add(new DataColumn() { ColumnName = "merge_title" });
                DataColumn[] cols = temp.ToArray();
                dt.Columns.AddRange(cols);
                string ConnectionString = JeeWorkLiteController.getConnectionString(ConnectionCache, loginData.CustomerID, _configuration);
                using (DpsConnection cnn = new DpsConnection(ConnectionString))
                {
                    string strG = @"select null as id_row, N'Chưa phân loại' as title union
select id_row, title from we_group g where disabled=0 and id_project_team=" + query.filter["id_project_team"];
                    DataTable dtG = cnn.CreateDataTable(strG);
                    DataSet ds = GetWorkByEmployee(Request.Headers, cnn, query, loginData.UserID, DataAccount);
                    var tags = ds.Tables[1].AsEnumerable();
                    var followers = ds.Tables[2].AsEnumerable();
                    if (cnn.LastError != null || ds == null)
                        return BadRequest();
                    DataRow _new;
                    foreach (DataRow drG in dtG.Rows)
                    {
                        _new = dt.NewRow();
                        _new["merge_row"] = true;
                        _new["merge_title"] = drG["title"] + ":";
                        dt.Rows.Add(_new);
                        var a = ds.Tables[0].AsEnumerable().Where(x => x["id_group"].Equals(drG["id_row"]));
                        if (displayChild == "0")
                            a = a.Where(x => x["id_parent"] == DBNull.Value);
                        if (a.Count() == 0)
                            continue;
                        DataTable dtW = a.CopyToDataTable();
                        genDr(dtW, followers, tags, DBNull.Value, "", ref dt);
                    }
                }
                //Xuất excel
                string[] width = { "315", "140", "140", "140", "140", "140", "140", "140", "140", "280", "140", "280", "210", "140", "140" };
                Hashtable format = new Hashtable();
                string rowheight = "18.5";
                string s = "Danh sách công việc";
                if (displayChild == "0")
                    s += "(Không bao gồm công việc con)";
                string excel = ExportExcelHelper.ExportToExcel(dt, s, header, width, rowheight, rowheight, format);
                string fileName = "Danhsachcongviec_" + string.Format("{0:ddMMyyyy}", DateTime.Today) + ".xls";
                var bytearr = Encoding.UTF8.GetBytes(excel);
                this.Response.Headers.Add("X-Filename", fileName);
                this.Response.Headers.Add("Access-Control-Expose-Headers", "X-Filename");
                return new FileContentResult(bytearr, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");
            }
            catch (Exception ex)
            {
                return BadRequest(JsonResultCommon.Exception(_logger, ex, _config, loginData));
            }
        }
        /// <summary>
        /// Update/Xóa các trường dữ liệu người dùng tự tạo
        /// </summary>
        /// <param name="id"></param>
        /// <param name="hidden">cập nhật hidden</param>
        /// <param name="isdeleted">Xóa</param>
        /// <returns></returns>
        [Route("update-hidden")]
        [HttpGet]
        public BaseModel<object> update_hidden(long id, long hidden, bool isdeleted = false)
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
                    string sqlq = "select ISNULL((select count(*) from we_fields_project_team where disabled=0 and id_row = " + id + "),0)";
                    if (long.Parse(cnn.ExecuteScalar(sqlq).ToString()) != 1)
                        return JsonResultCommon.KhongTonTai("Công việc");
                    if (isdeleted)
                        sqlq = "update we_fields_project_team set disabled=1, UpdatedDate=GETUTCDATE(), UpdatedBy=" + iduser + " where id_row = " + id + "";
                    else
                        sqlq = "update we_fields_project_team set IsHidden=" + hidden + ", UpdatedDate=GETUTCDATE(), UpdatedBy=" + iduser + " where id_row = " + id + "";
                    SqlConditions sqlcond = new SqlConditions();
                    sqlcond.Add("id_row", id);
                    sqlcond.Add("disabled", 0);
                    cnn.BeginTransaction();
                    if (cnn.ExecuteNonQuery(sqlq) < 1)
                    {
                        cnn.RollbackTransaction();
                        return JsonResultCommon.Exception(_logger, cnn.LastError, _config, loginData, ControllerContext);
                    }
                    #region Ghi log trong project
                    string LogContent = "update_hidden (" + id + ")";
                    Common.Ghilogfile(loginData.CustomerID.ToString(), "", LogContent, loginData.Username, ControllerContext);
                    #endregion
                    #region Ghi log lên CDN
                    var d2 = new ActivityLog()
                    {
                        username = loginData.Username,
                        category = LogContent,
                        action = loginData.customdata.personalInfo.Fullname + " thao tác",
                        data = ""
                    };
                    _logger.LogInformation(JsonConvert.SerializeObject(d2));
                    #endregion
                    cnn.EndTransaction();
                    return JsonResultCommon.ThanhCong();
                }
            }
            catch (Exception ex)
            {
                return JsonResultCommon.Exception(_logger, ex, _config, loginData);
            }
        }
        /// <summary>
        /// Chi tiết công việc
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [Route("Detail")]
        [HttpGet]
        public object Detail(long id)
        {
            UserJWT loginData = Ulities.GetUserByHeader(HttpContext.Request.Headers);
            if (loginData == null)
                return JsonResultCommon.DangNhap();
            try
            {
                string domain = _configuration.GetValue<string>("Host:JeeWork_API") + "/";
                string ConnectionString = JeeWorkLiteController.getConnectionString(ConnectionCache, loginData.CustomerID, _configuration);
                using (DpsConnection cnn = new DpsConnection(ConnectionString))
                {
                    if (!JeeWorkLiteController.CheckCustomerID(id, "we_work", loginData, cnn))
                    {
                        return JsonResultCommon.Custom("Công việc không tồn tại");
                    }
                    #region Lấy dữ liệu account từ JeeAccount
                    DataAccount = JeeWorkLiteController.GetAccountFromJeeAccount(HttpContext.Request.Headers, _configuration);
                    if (DataAccount == null)
                        return JsonResultCommon.Custom("Lỗi lấy danh sách nhân viên từ hệ thống quản lý tài khoản");
                    string error = "";
                    string listID = JeeWorkLiteController.ListAccount(HttpContext.Request.Headers, out error, _configuration);
                    if (error != "")
                        return JsonResultCommon.Custom(error);
                    #endregion
                    string sql = $@"";
                    #region Trả dữ liệu về backend để hiển thị lên giao diện
                    string sqlq = @$"select distinct w.id_row,w.title,w.description,w.id_project_team,w.id_group,w.deadline,w.id_milestone,w.milestone,estimates,w.id_nv,
                                w.id_parent,w.start_date,w.end_date,w.urgent,w.important,w.prioritize,w.status,w.result,w.CreatedDate,w.CreatedBy,
                                w.UpdatedDate,w.UpdatedBy,w.NguoiGiao, w.project_team,w.id_department,w.clickup_prioritize 
                                , '' as hoten_nguoigiao, Iif(fa.id_row is null ,0,1) as favourite,
                                iIf(w.Status=2 and w.end_date>w.deadline,1,0) as is_htquahan,
                                iIf(w.Status = 2 and w.end_date <= w.deadline, 1, 0) as is_htdunghan ,
                                iIf(w.Status = 1 and  w.start_date is not null, 1, 0) as is_danglam,
                                iIf(w.Status = 1 and GETUTCDATE() > w.deadline, 1, 0) as is_quahan,
                                iif(convert(varchar, w.deadline,103) like convert(varchar, GETUTCDATE(),103),1,0) as duetoday,
                                iif(w.status=1 and w.start_date is null,1,0) as require,
                                '' as NguoiTao,'' as NguoiSua from v_wework_new w 
                                left join we_work_favourite fa on fa.id_work=w.id_row and fa.createdby=6 and fa.disabled=0
                                where w.id_row= " + id + " or id_parent=" + id;
                    //tag
                    sqlq += @";select a.title, a.id_row, a.color 
                    from we_tag a join we_work_tag b on a.id_row=b.id_tag 
                    where a.disabled=0 and b.disabled = 0 and id_work = " + id;
                    //người theo dõi
                    sqlq += @$";select id_work,id_user as id_nv,'' as hoten,''as mobile,''as username,''as email,''as image,''as tenchucdanh from we_work_user u 
where u.disabled = 0 and u.loai = 2 and id_work=" + id;
                    //attachment
                    sqlq += @$";select a.*, '' as username from we_attachment a
where Disabled=0 and object_type in (1,11) and object_id=" + id;
                    // Quá trình xử lý
                    sqlq += @$";select process.*, '' as hoten, statusname, we_status.Position, we_status.color
                                from we_work_process process
                                join we_status on we_status.id_row = process.statusid
                                where we_status.disabled=0 and workid=" + id + " order by position";
                    DataSet ds = cnn.CreateDataSet(sqlq);
                    if (cnn.LastError != null || ds == null)
                    {
                        return JsonResultCommon.Exception(_logger, cnn.LastError, _config, loginData, ControllerContext);
                    }
                    if (ds.Tables[0] == null || ds.Tables[0].Rows.Count == 0)
                        return JsonResultCommon.KhongTonTai();
                    #region Map info account từ JeeAccount
                    foreach (DataRow item in ds.Tables[0].Rows)
                    {
                        var infoNguoiGiao = DataAccount.Where(x => item["nguoigiao"].ToString().Contains(x.UserId.ToString())).FirstOrDefault();
                        var infoNguoiTao = DataAccount.Where(x => item["CreatedBy"].ToString().Contains(x.UserId.ToString())).FirstOrDefault();
                        var infoNguoiSua = DataAccount.Where(x => item["UpdatedBy"].ToString().Contains(x.UserId.ToString())).FirstOrDefault();
                        //if (infoNguoiGiao != null)
                        //{
                        //    item["hoten_nguoigiao"] = infoNguoiGiao.FullName;
                        //}
                        //if (infoNguoiTao != null)
                        //{
                        //    item["NguoiTao"] = infoNguoiTao.Username;
                        //}
                        //if (infoNguoiSua != null)
                        //{
                        //    item["NguoiSua"] = infoNguoiSua.Username;
                        //}
                    }
                    foreach (DataRow item in ds.Tables[2].Rows)
                    {
                        var info = DataAccount.Where(x => item["id_nv"].ToString().Contains(x.UserId.ToString())).FirstOrDefault();
                        if (info != null)
                        {
                            item["hoten"] = info.FullName;
                            item["mobile"] = info.PhoneNumber;
                            item["username"] = info.Username;
                            item["email"] = info.Email;
                            item["tenchucdanh"] = info.Jobtitle;
                            item["image"] = info.AvartarImgURL;
                        }
                    }
                    foreach (DataRow item in ds.Tables[3].Rows)
                    {
                        var info = DataAccount.Where(x => item["createdby"].ToString().Contains(x.UserId.ToString())).FirstOrDefault();
                        if (info != null)
                        {
                            item["username"] = info.Username;
                        }
                    }
                    foreach (DataRow item in ds.Tables[4].Rows)
                    {
                        var info = DataAccount.Where(x => item["Checker"].ToString().Contains(x.UserId.ToString())).FirstOrDefault();
                        if (info != null)
                        {
                            item["hoten"] = info.FullName;
                        }
                    }
                    #endregion
                    sqlq = @$"exec GetObjectActivitiesNew 1,{id}";
                    //, null as hoten,null as mobile,null as username,null as email,null as Tenchucdanh,null as image
                    DataTable dtLog = cnn.CreateDataTable(sqlq);
                    dtLog.Columns.Add("hoten");
                    dtLog.Columns.Add("mobile");
                    dtLog.Columns.Add("username");
                    dtLog.Columns.Add("email");
                    dtLog.Columns.Add("Tenchucdanh");
                    dtLog.Columns.Add("image");
                    #region Map info account từ JeeAccount
                    foreach (DataRow item in dtLog.Rows)
                    {
                        var info = DataAccount.Where(x => item["id_nv"].ToString().Contains(x.UserId.ToString())).FirstOrDefault();
                        if (info != null)
                        {
                            item["hoten"] = info.FullName;
                            item["mobile"] = info.PhoneNumber;
                            item["username"] = info.Username;
                            item["email"] = info.Email;
                            item["tenchucdanh"] = info.Jobtitle;
                            item["image"] = info.AvartarImgURL;
                        }
                    }
                    #endregion
                    DataTable User = new DataTable();
                    DataTable UserChild = new DataTable();
                    DataTable ProcessWork = new DataTable();
                    SqlConditions conds0 = new SqlConditions();
                    conds0 = new SqlConditions();
                    conds0.Add("w_user.Disabled", 0);
                    SqlConditions conds1 = new SqlConditions();
                    conds1 = new SqlConditions();
                    conds1.Add("w_user.Disabled", 0);
                    conds1.Add("loai", 1);
                    string select_user = $@"select  distinct w_user.id_user,'' as hoten,'' as image,'' as username,'' as tenchucdanh,'' as mobile, id_work
                                                    from we_work_user w_user join we_work on we_work.id_row = w_user.id_work 
                                                    where (where)";
                    User = cnn.CreateDataTable(select_user, "(where)", conds1);
                    #region Map info account từ JeeAccount
                    foreach (DataRow item in User.Rows)
                    {
                        var info = DataAccount.Where(x => item["id_user"].ToString().Contains(x.UserId.ToString())).FirstOrDefault();
                        if (info != null)
                        {
                            item["hoten"] = info.FullName;
                            item["image"] = info.AvartarImgURL;
                            item["username"] = info.Username;
                            item["tenchucdanh"] = info.Jobtitle;
                            item["mobile"] = info.PhoneNumber;
                        }
                    }
                    #endregion
                    DataTable dt_projects = cnn.CreateDataTable($@"select id_row, icon, title, id_department
                                                                , loai, start_date, end_date, color, status 
                                                                , is_project, Locked, Disabled 
                                                                from we_project_team where Disabled = 0");
                    bool rs = Common.CheckRoleByProject(ds.Tables[0].Rows[0]["id_project_team"].ToString(), loginData, cnn, ConnectionString);

                    var data = (from r in ds.Tables[0].AsEnumerable()
                                    //where r["id_parent"] == DBNull.Value
                                where r["id_row"].ToString() == id.ToString()
                                select new
                                {
                                    id_parent = r["id_parent"],
                                    id_group = r["id_group"],
                                    id_row = r["id_row"],
                                    title = r["title"],
                                    description = r["description"],
                                    id_project_team = r["id_project_team"],
                                    project_team = r["project_team"],
                                    deadline = r["deadline"],
                                    start_date = r["start_date"],
                                    end_date = r["end_date"],
                                    //deadline = string.Format("{0:dd/MM/yyyy HH:mm}", r["deadline"]),
                                    //start_date = string.Format("{0:dd/MM/yyyy HH:mm}", r["start_date"]),
                                    //end_date = string.Format("{0:dd/MM/yyyy HH:mm}", r["end_date"]),
                                    urgent = r["urgent"],
                                    important = r["important"],
                                    prioritize = r["prioritize"],
                                    favourite = r["favourite"],
                                    //require = r["require"],
                                    status = r["status"],
                                    //id_milestone = r["id_milestone"],
                                    milestone = r["milestone"],
                                    //is_htquahan = r["is_htquahan"],
                                    //is_htdunghan = r["is_htdunghan"],
                                    //is_danglam = r["is_danglam"],
                                    //is_quahan = r["is_quahan"],
                                    //duetoday = r["duetoday"],
                                    CreatedDate = string.Format("{0:dd/MM/yyyy HH:mm}", r["CreatedDate"]),
                                    CreatedBy = r["CreatedBy"],
                                    //NguoiTao = r["NguoiTao"],
                                    UpdatedDate = r["UpdatedDate"] == DBNull.Value ? "" : string.Format("{0:dd/MM/yyyy HH:mm}", r["UpdatedDate"]),
                                    UpdatedBy = r["UpdatedBy"],
                                    // NguoiSua = r["NguoiSua"],
                                    //NguoiGiao = r["NguoiGiao"],
                                    clickup_prioritize = r["clickup_prioritize"],
                                    result = r["result"],
                                    estimates = r["estimates"],
                                    DataStatus = list_status_user(r["id_row"].ToString(), r["id_project_team"].ToString(), loginData, ConnectionString, DataAccount),
                                    User = from us in User.AsEnumerable()
                                           where r["id_row"].Equals(us["id_work"])
                                           select new
                                           {
                                               id_nv = us["id_user"],
                                               hoten = us["hoten"],
                                               username = us["username"],
                                               tenchucdanh = us["tenchucdanh"],
                                               mobile = us["mobile"],
                                               image = us["image"],
                                               //image = JeeWorkLiteController.genLinkImage(domain, loginData.CustomerID, us["id_user"].ToString(), _hostingEnvironment.ContentRootPath),
                                           },
                                    Followers = from f in ds.Tables[2].AsEnumerable()
                                                select new
                                                {
                                                    id_nv = f["id_nv"],
                                                    hoten = f["hoten"],
                                                    username = f["username"],
                                                    tenchucdanh = f["tenchucdanh"],
                                                    mobile = f["mobile"],
                                                    image = f["image"],
                                                    //image = JeeWorkLiteController.genLinkImage(domain, loginData.CustomerID, f["id_nv"].ToString(), _hostingEnvironment.ContentRootPath)
                                                },
                                    Tags = from t in ds.Tables[1].AsEnumerable()
                                           select new
                                           {
                                               id_row = t["id_row"],
                                               title = t["title"],
                                               color = t["color"]
                                           },
                                    Attachments = from dr in ds.Tables[3].AsEnumerable()
                                                  where dr["object_type"].ToString() == "1"
                                                  select new
                                                  {
                                                      id_row = dr["id_row"],
                                                      path = JeeWorkLiteController.genLinkAttachment(_configuration, dr["path"]),
                                                      filename = dr["filename"],
                                                      type = dr["type"],
                                                      isImage = UploadHelper.IsImage(dr["type"].ToString()),
                                                      icon = UploadHelper.GetIcon(dr["type"].ToString()),
                                                      size = dr["size"],
                                                      NguoiTao = dr["username"],
                                                      Object_Type = dr["object_type"],
                                                      CreatedBy = dr["CreatedBy"],
                                                      CreatedDate = string.Format("{0:dd/MM/yyyy HH:mm}", dr["CreatedDate"])
                                                  },
                                    Attachments_Result = from dr in ds.Tables[3].AsEnumerable()
                                                         where dr["object_type"].ToString() == "11"
                                                         select new
                                                         {
                                                             id_row = dr["id_row"],
                                                             path = JeeWorkLiteController.genLinkAttachment(_configuration, dr["path"]),
                                                             filename = dr["filename"],
                                                             type = dr["type"],
                                                             isImage = UploadHelper.IsImage(dr["type"].ToString()),
                                                             icon = UploadHelper.GetIcon(dr["type"].ToString()),
                                                             size = dr["size"],
                                                             NguoiTao = dr["username"],
                                                             Object_Type = dr["object_type"],
                                                             CreatedBy = dr["CreatedBy"],
                                                             CreatedDate = string.Format("{0:dd/MM/yyyy HH:mm}", dr["CreatedDate"])
                                                         },
                                    Process = from t in ds.Tables[4].AsEnumerable()
                                              select new
                                              {
                                                  id_row = t["id_row"],
                                                  workid = t["workid"],
                                                  statusname = t["statusName"],
                                                  color = t["color"],
                                                  checker = t["checker"],
                                                  change_note = t["change_note"],
                                                  position = t["Position"],
                                                  statusid = t["statusid"],
                                              },
                                    Project_Info = from pr in dt_projects.AsEnumerable()
                                                   where r["id_project_team"].Equals(pr["id_row"])
                                                   select new
                                                   {
                                                       id_row = pr["id_row"],
                                                       title = pr["title"],
                                                       color = pr["color"],
                                                       id_department = pr["id_department"],
                                                       status = pr["status"],
                                                       start_date = pr["start_date"] == DBNull.Value ? "" : string.Format("{0:dd/MM/yyyy}", pr["start_date"]),
                                                       end_date = pr["end_date"] == DBNull.Value ? "" : string.Format("{0:dd/MM/yyyy}", pr["end_date"]),
                                                       is_project = pr["is_project"],
                                                       locked = pr["Locked"],
                                                       icon = pr["icon"],
                                                   },
                                    Activities = from u in dtLog.AsEnumerable()
                                                 select new
                                                 {
                                                     id_row = u["id_row"],
                                                     action = u["action"],
                                                     action_en = u["action_en"],
                                                     view_detail = u["view_detail"],
                                                     log_content = u["log_content"],
                                                     CreatedDate = string.Format("{0:dd/MM/yyyy HH:mm}", u["CreatedDate"]),
                                                     NguoiTao = new
                                                     {
                                                         id_nv = u["id_nv"],
                                                         hoten = u["hoten"],
                                                         username = u["username"],
                                                         tenchucdanh = u["tenchucdanh"],
                                                         mobile = u["mobile"],
                                                         image = u["image"],
                                                         //image = JeeWorkLiteController.genLinkImage(domain, loginData.CustomerID, u["id_nv"].ToString(), _hostingEnvironment.ContentRootPath)
                                                     }
                                                 },
                                    Childs = from rr in ds.Tables[0].AsEnumerable()
                                             where rr["id_parent"].Equals(r["id_row"]) && (rs || (!rs && (rr["id_nv"].ToString() == loginData.UserID.ToString() || rr["CreatedBy"].ToString() == loginData.UserID.ToString() || r["id_nv"].ToString() == loginData.UserID.ToString())))
                                             select new
                                             {
                                                 id_parent = rr["id_parent"],
                                                 id_row = rr["id_row"],
                                                 title = rr["title"],
                                                 description = rr["description"],
                                                 id_project_team = rr["id_project_team"],
                                                 project_team = rr["project_team"],
                                                 deadline = string.Format("{0:dd/MM/yyyy HH:mm}", rr["deadline"]),
                                                 start_date = string.Format("{0:dd/MM/yyyy HH:mm}", rr["start_date"]),
                                                 end_date = string.Format("{0:dd/MM/yyyy HH:mm}", rr["end_date"]),
                                                 urgent = rr["urgent"],
                                                 important = rr["important"],
                                                 prioritize = rr["prioritize"],
                                                 favourite = rr["favourite"],
                                                 //require = rr["require"],
                                                 status = rr["status"],
                                                 //milestone = rr["milestone"],
                                                 //is_htquahan = rr["is_htquahan"],
                                                 //is_htdunghan = rr["is_htdunghan"],
                                                 //is_danglam = rr["is_danglam"],
                                                 //is_quahan = rr["is_quahan"],
                                                 //duetoday = rr["duetoday"],
                                                 CreatedDate = string.Format("{0:dd/MM/yyyy HH:mm}", rr["CreatedDate"]),
                                                 CreatedBy = rr["CreatedBy"],
                                                 //NguoiTao = rr["NguoiTao"],
                                                 UpdatedDate = rr["UpdatedDate"] == DBNull.Value ? "" : string.Format("{0:dd/MM/yyyy HH:mm}", rr["UpdatedDate"]),
                                                 UpdatedBy = rr["UpdatedBy"],
                                                 //NguoiSua = rr["NguoiSua"],
                                                 //NguoiGiao = rr["NguoiGiao"],
                                                 User = from u in User.AsEnumerable()
                                                        where rr["id_row"].Equals(u["id_work"])
                                                        select new
                                                        {
                                                            id_nv = u["id_user"],
                                                            hoten = u["hoten"],
                                                            //username = us["username"],
                                                            //tenchucdanh = us["tenchucdanh"],
                                                            //mobile = us["mobile"],
                                                            image = u["image"],
                                                            //image = JeeWorkLiteController.genLinkImage(domain, loginData.CustomerID, u["id_user"].ToString(), _hostingEnvironment.ContentRootPath)
                                                        },
                                             }
                                }).FirstOrDefault();
                    return JsonResultCommon.ThanhCong(data);
                }
                #endregion
            }
            catch (Exception ex)
            {
                return JsonResultCommon.Exception(_logger, ex, _config, loginData);
            }
        }
        /// <summary>
        /// Chi tiết công việc
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [Route("detail-task")]
        [HttpGet]
        public object DetailTask(long id)
        {
            UserJWT loginData = Ulities.GetUserByHeader(HttpContext.Request.Headers);
            if (loginData == null)
                return JsonResultCommon.DangNhap();
            try
            {
                bool isview = false;
                string domain = _configuration.GetValue<string>("Host:JeeWork_API") + "/";
                string ConnectionString = JeeWorkLiteController.getConnectionString(ConnectionCache, loginData.CustomerID, _configuration);
                using (DpsConnection cnn = new DpsConnection(ConnectionString))
                {
                    long id_project_team = 0;
                    string sql = $@"";
                    #region Trả dữ liệu về backend để hiển thị lên giao diện
                    string sqlq = @$"select distinct w.id_row,w.title,w.description
                                    ,w.id_project_team,w.id_group,w.deadline
                                    ,w.id_milestone,w.milestone, estimates, w.id_nv
                                    ,w.id_parent,w.start_date
                                    ,w.end_date,w.urgent,w.important
                                    ,w.prioritize,w.status,w.result,w.CreatedDate,w.CreatedBy
                                    ,w.UpdatedDate,w.UpdatedBy
                                    ,w.NguoiGiao, w.project_team,w.department
                                    ,w.id_department,w.clickup_prioritize 
                                    ,Iif(fa.id_row is null ,0,1) as favourite
                                    , num_comment, estimates, accepted_date, activated_date
                                    , closed_date, state_change_date, activated_by, closed_by
                                    , islate, closed, closed_work_date, closed_work_by
                                    from v_wework_new w 
                                    left join we_work_favourite fa on fa.id_work=w.id_row 
                                    and fa.createdby=" + loginData.UserID + " " +
                                    "and fa.disabled=0 " +
                                    "where w.id_row= " + id + " or id_parent=" + id;
                    //tag
                    sqlq += @";select a.title, a.id_row, a.color 
                    from we_tag a join we_work_tag b on a.id_row=b.id_tag 
                    where a.disabled=0 and b.disabled = 0 and id_work = " + id;
                    // thành viên task & subtask (loại 1 assign, loại 2 người theo dõi)
                    sqlq += @$";select id_row, id_work, id_user, loai, id_child
                                , CreatedDate, CreatedBy, UpdatedDate, UpdatedBy
                                 from we_work_user where disabled = 0 
                                or id_work in (select id_row from we_work where Disabled = 0 and id_parent = " + id + ") " +
                                "and id_work=" + id;
                    //attachment
                    sqlq += @$";select a.* from we_attachment a
                    where Disabled=0 and object_type in (1,11) and object_id=" + id;
                    // Quá trình xử lý
                    sqlq += @$";select process.*, '' as hoten, statusname, we_status.Position, we_status.color
                                from we_work_process process
                                join we_status on we_status.id_row = process.statusid
                                where we_status.disabled=0 and workid=" + id + " order by position";
                    DataSet ds = cnn.CreateDataSet(sqlq);
                    if (cnn.LastError != null || ds == null)
                    {
                        return JsonResultCommon.Exception(_logger, cnn.LastError, _config, loginData, ControllerContext);
                    }
                    if (ds.Tables[0] == null || ds.Tables[0].Rows.Count == 0)
                        return JsonResultCommon.KhongTonTai("Công việc");
                    id_project_team = long.Parse(ds.Tables[0].Rows[0]["id_project_team"].ToString());
                    if (!JeeWorkLiteController.CheckCustomerID(id, "we_work", loginData, cnn))
                    {
                        return JsonResultCommon.Custom("Công việc không tồn tại");
                    }
                    int id_role = 3; // quyền xem công việc của người khác
                    DataTable dt_role = new DataTable();
                    string sql_role = "";
                    sql_role = "select * from we_role where disabled = 0 and id_row = " + id_role;
                    dt_role = cnn.CreateDataTable(sql_role);
                    if (dt_role.Rows.Count > 0)
                    {
                        isview = Common.CheckIsUpdatedTask(id_project_team.ToString(), id_role, loginData, cnn, ConnectionString);
                        if (!isview)
                        {
                            isview = Common.CheckIsViewTask(id_project_team.ToString(), loginData.UserID, id, loginData, cnn, ConnectionString);
                            if (!isview)
                            {
                                return JsonResultCommon.Custom("Bạn không có quyền " + dt_role.Rows[0]["title"].ToString());
                            }
                        }
                    }
                    #region Lấy dữ liệu account từ JeeAccount
                    DataAccount = JeeWorkLiteController.GetAccountFromJeeAccount(HttpContext.Request.Headers, _configuration);
                    if (DataAccount == null)
                        return JsonResultCommon.Custom("Lỗi lấy danh sách nhân viên từ hệ thống quản lý tài khoản");
                    string error = "";
                    string listID = JeeWorkLiteController.ListAccount(HttpContext.Request.Headers, out error, _configuration);
                    if (error != "")
                        return JsonResultCommon.Custom(error);
                    #endregion
                    #region Map info account từ JeeAccount
                    ds.Tables[2].Columns.Add("hoten");
                    ds.Tables[2].Columns.Add("mobile");
                    ds.Tables[2].Columns.Add("username");
                    ds.Tables[2].Columns.Add("email");
                    ds.Tables[2].Columns.Add("tenchucdanh");
                    ds.Tables[2].Columns.Add("image");
                    foreach (DataRow item in ds.Tables[2].Rows)
                    {
                        var info = DataAccount.Where(x => item["id_user"].ToString().Contains(x.UserId.ToString())).FirstOrDefault();
                        if (info != null)
                        {
                            item["hoten"] = info.FullName;
                            item["mobile"] = info.PhoneNumber;
                            item["username"] = info.Username;
                            item["email"] = info.Email;
                            item["tenchucdanh"] = info.Jobtitle;
                            item["image"] = info.AvartarImgURL;
                        }
                    }
                    #endregion
                    SqlConditions cond = new SqlConditions();
                    cond.Add("disabled", 0);
                    cond.Add("id_row", id_project_team);
                    string sql_proj = "";
                    sql_proj = "select id_row, icon, title, id_department, loai" +
                        ", start_date, end_date, color, status" +
                        ", is_project, Locked, Disabled " +
                        "from we_project_team where (where)";
                    DataTable dt_proj = cnn.CreateDataTable(sql_proj, "(where)", cond);
                    bool rs = Common.CheckRoleByProject(ds.Tables[0].Rows[0]["id_project_team"].ToString(), loginData, cnn, ConnectionString);
                    var data = (from r in ds.Tables[0].AsEnumerable()
                                where r["id_row"].ToString() == id.ToString()
                                select new
                                {
                                    id_parent = r["id_parent"],
                                    id_group = r["id_group"],
                                    id_row = r["id_row"],
                                    title = r["title"],
                                    description = r["description"],
                                    id_project_team = r["id_project_team"],
                                    project_team = r["project_team"],
                                    department = r["department"],
                                    deadline = r["deadline"],
                                    start_date = r["start_date"],
                                    end_date = r["end_date"],
                                    favourite = r["favourite"],
                                    status = r["status"],
                                    id_milestone = r["id_milestone"],
                                    milestone = r["milestone"],
                                    createddate = string.Format("{0:dd/MM/yyyy HH:mm}", r["CreatedDate"]),
                                    nguoigiao = r["NguoiGiao"].Equals(DBNull.Value) ? new { } : JeeWorkLiteController.Get_InfoUsers(r["NguoiGiao"].ToString(), DataAccount),
                                    updateddate = r["UpdatedDate"] == DBNull.Value ? "" : r["UpdatedDate"],
                                    updatedby = r["UpdatedBy"].Equals(DBNull.Value) ? new { } : JeeWorkLiteController.Get_InfoUsers(r["UpdatedBy"].ToString(), DataAccount),
                                    createdby = r["CreatedBy"].Equals(DBNull.Value) ? new { } : JeeWorkLiteController.Get_InfoUsers(r["CreatedBy"].ToString(), DataAccount),
                                    clickup_prioritize = r["clickup_prioritize"],
                                    result = r["result"],
                                    estimates = r["estimates"],
                                    closed = r["closed"],
                                    isviewtask = isview,
                                    num_comment = r["num_comment"],
                                    closed_work_date = r["closed_work_date"],
                                    closed_work_by = r["closed_work_by"],
                                    accepted_date = r["accepted_date"] == DBNull.Value ? "" : r["accepted_date"],
                                    activated_by = r["activated_by"].Equals(DBNull.Value) ? new { } : JeeWorkLiteController.Get_InfoUsers(r["activated_by"].ToString(), DataAccount),
                                    activated_date = r["activated_date"] == DBNull.Value ? "" : r["activated_date"],
                                    closed_by = r["closed_by"].Equals(DBNull.Value) ? new { } : JeeWorkLiteController.Get_InfoUsers(r["closed_by"].ToString(), DataAccount),
                                    closed_date = r["closed_date"] == DBNull.Value ? "" : r["closed_date"],
                                    state_change_date = r["state_change_date"] == DBNull.Value ? "" : r["state_change_date"],
                                    DataStatus = list_status_user(r["id_row"].ToString(), r["id_project_team"].ToString(), loginData, ConnectionString, DataAccount),
                                    Users = from us in ds.Tables[2].AsEnumerable()
                                            where r["id_row"].Equals(us["id_work"]) && us["loai"].ToString().Equals("1")
                                            select new
                                            {
                                                id_nv = us["id_user"],
                                                hoten = us["hoten"],
                                                username = us["username"],
                                                tenchucdanh = us["tenchucdanh"],
                                                mobile = us["mobile"],
                                                image = us["image"],
                                                createddate = string.Format("{0:dd/MM/yyyy HH:mm}", r["CreatedDate"]),
                                                updateddate = r["UpdatedDate"] == DBNull.Value ? "" : r["UpdatedDate"],
                                                updatedby = r["UpdatedBy"].Equals(DBNull.Value) ? new { } : JeeWorkLiteController.Get_InfoUsers(r["UpdatedBy"].ToString(), DataAccount),
                                                createdby = r["CreatedBy"].Equals(DBNull.Value) ? new { } : JeeWorkLiteController.Get_InfoUsers(r["CreatedBy"].ToString(), DataAccount),
                                            },
                                    Followers = from us in ds.Tables[2].AsEnumerable()
                                                where r["id_row"].Equals(us["id_work"]) && us["loai"].ToString().Equals("2")
                                                select new
                                                {
                                                    id_nv = us["id_user"],
                                                    hoten = us["hoten"],
                                                    username = us["username"],
                                                    tenchucdanh = us["tenchucdanh"],
                                                    mobile = us["mobile"],
                                                    image = us["image"],
                                                    createddate = string.Format("{0:dd/MM/yyyy HH:mm}", r["CreatedDate"]),
                                                    updateddate = r["UpdatedDate"] == DBNull.Value ? "" : r["UpdatedDate"],
                                                    updatedby = r["UpdatedBy"].Equals(DBNull.Value) ? new { } : JeeWorkLiteController.Get_InfoUsers(r["UpdatedBy"].ToString(), DataAccount),
                                                    createdby = r["CreatedBy"].Equals(DBNull.Value) ? new { } : JeeWorkLiteController.Get_InfoUsers(r["CreatedBy"].ToString(), DataAccount),
                                                },
                                    Tags = from t in ds.Tables[1].AsEnumerable()
                                           select new
                                           {
                                               id_row = t["id_row"],
                                               title = t["title"],
                                               color = t["color"]
                                           },
                                    Attachments = from dr in ds.Tables[3].AsEnumerable()
                                                  where dr["object_type"].ToString() == "1"
                                                  select new
                                                  {
                                                      id_row = dr["id_row"],
                                                      path = JeeWorkLiteController.genLinkAttachment(_configuration, dr["path"]),
                                                      filename = dr["filename"],
                                                      type = dr["type"],
                                                      isImage = UploadHelper.IsImage(dr["type"].ToString()),
                                                      icon = UploadHelper.GetIcon(dr["type"].ToString()),
                                                      size = dr["size"],
                                                      // NguoiTao = dr["username"],
                                                      Object_Type = dr["object_type"],
                                                      CreatedBy = dr["CreatedBy"],
                                                      CreatedDate = string.Format("{0:dd/MM/yyyy HH:mm}", dr["CreatedDate"])
                                                  },
                                    Attachments_Result = from dr in ds.Tables[3].AsEnumerable()
                                                         where dr["object_type"].ToString() == "11"
                                                         select new
                                                         {
                                                             id_row = dr["id_row"],
                                                             path = JeeWorkLiteController.genLinkAttachment(_configuration, dr["path"]),
                                                             filename = dr["filename"],
                                                             type = dr["type"],
                                                             isImage = UploadHelper.IsImage(dr["type"].ToString()),
                                                             icon = UploadHelper.GetIcon(dr["type"].ToString()),
                                                             size = dr["size"],
                                                             /// NguoiTao = dr["username"],
                                                             Object_Type = dr["object_type"],
                                                             CreatedBy = dr["CreatedBy"],
                                                             CreatedDate = string.Format("{0:dd/MM/yyyy HH:mm}", dr["CreatedDate"])
                                                         },
                                    Process = from t in ds.Tables[4].AsEnumerable()
                                              select new
                                              {
                                                  id_row = t["id_row"],
                                                  workid = t["workid"],
                                                  statusname = t["statusName"],
                                                  color = t["color"],
                                                  checker = t["checker"],
                                                  change_note = t["change_note"],
                                                  position = t["Position"],
                                                  statusid = t["statusid"],
                                              },
                                    Project_Info = from pr in dt_proj.AsEnumerable()
                                                   select new
                                                   {
                                                       id_row = pr["id_row"],
                                                       title = pr["title"],
                                                       color = pr["color"],
                                                       id_department = pr["id_department"],
                                                       status = pr["status"],
                                                       start_date = pr["start_date"] == DBNull.Value ? "" : string.Format("{0:dd/MM/yyyy}", pr["start_date"]),
                                                       end_date = pr["end_date"] == DBNull.Value ? "" : string.Format("{0:dd/MM/yyyy}", pr["end_date"]),
                                                       is_project = pr["is_project"],
                                                       locked = pr["Locked"],
                                                       icon = pr["icon"],
                                                   },
                                    //Child = displayChild == "0" ? new List<string>() : GetChildTask(domain, IdKHDPS, columnName, displayChild == "1" ? "0" : "2", id, temp, tags, DataAccount, loginData, ConnectionString, dt_Users, r["id_row"]),
                                    Childs = from rr in ds.Tables[0].AsEnumerable()
                                             where rr["id_parent"].Equals(r["id_row"]) && (rs || (!rs && (rr["id_nv"].ToString() == loginData.UserID.ToString() || rr["CreatedBy"].ToString() == loginData.UserID.ToString() || r["id_nv"].ToString() == loginData.UserID.ToString())))
                                             select new
                                             {
                                                 id_parent = rr["id_parent"],
                                                 id_row = rr["id_row"],
                                                 title = rr["title"],
                                                 description = rr["description"],
                                                 id_project_team = rr["id_project_team"],
                                                 project_team = rr["project_team"],
                                                 deadline = string.Format("{0:dd/MM/yyyy HH:mm}", rr["deadline"]),
                                                 start_date = string.Format("{0:dd/MM/yyyy HH:mm}", rr["start_date"]),
                                                 end_date = string.Format("{0:dd/MM/yyyy HH:mm}", rr["end_date"]),
                                                 closed = rr["closed"],
                                                 clickup_prioritize = rr["clickup_prioritize"],
                                                 favourite = rr["favourite"],
                                                 status = rr["status"],
                                                 milestone = rr["milestone"],
                                                 createddate = string.Format("{0:dd/MM/yyyy HH:mm}", rr["CreatedDate"]),
                                                 nguoigiao = rr["NguoiGiao"].Equals(DBNull.Value) ? new { } : JeeWorkLiteController.Get_InfoUsers(rr["NguoiGiao"].ToString(), DataAccount),
                                                 updateddate = rr["UpdatedDate"] == DBNull.Value ? "" : rr["UpdatedDate"],
                                                 updatedby = rr["UpdatedBy"].Equals(DBNull.Value) ? new { } : JeeWorkLiteController.Get_InfoUsers(rr["UpdatedBy"].ToString(), DataAccount),
                                                 createdby = rr["CreatedBy"].Equals(DBNull.Value) ? new { } : JeeWorkLiteController.Get_InfoUsers(rr["CreatedBy"].ToString(), DataAccount),
                                                 Users = from us in ds.Tables[2].AsEnumerable()
                                                         where rr["id_row"].Equals(us["id_work"])
                                                         select new
                                                         {
                                                             id_nv = us["id_user"],
                                                             hoten = us["hoten"],
                                                             username = us["username"],
                                                             tenchucdanh = us["tenchucdanh"],
                                                             mobile = us["mobile"],
                                                             image = us["image"],
                                                             createddate = string.Format("{0:dd/MM/yyyy HH:mm}", r["CreatedDate"]),
                                                             updateddate = r["UpdatedDate"] == DBNull.Value ? "" : r["UpdatedDate"],
                                                             updatedby = r["UpdatedBy"].Equals(DBNull.Value) ? new { } : JeeWorkLiteController.Get_InfoUsers(r["UpdatedBy"].ToString(), DataAccount),
                                                             createdby = r["CreatedBy"].Equals(DBNull.Value) ? new { } : JeeWorkLiteController.Get_InfoUsers(r["CreatedBy"].ToString(), DataAccount),
                                                         },
                                                 Followers = from us in ds.Tables[2].AsEnumerable()
                                                             where rr["id_row"].Equals(us["id_work"]) && us["loai"].ToString().Equals("2")
                                                             select new
                                                             {
                                                                 id_nv = us["id_user"],
                                                                 hoten = us["hoten"],
                                                                 username = us["username"],
                                                                 tenchucdanh = us["tenchucdanh"],
                                                                 mobile = us["mobile"],
                                                                 image = us["image"],
                                                                 createddate = string.Format("{0:dd/MM/yyyy HH:mm}", rr["CreatedDate"]),
                                                                 updateddate = rr["UpdatedDate"] == DBNull.Value ? "" : rr["UpdatedDate"],
                                                                 updatedby = rr["UpdatedBy"].Equals(DBNull.Value) ? new { } : JeeWorkLiteController.Get_InfoUsers(rr["UpdatedBy"].ToString(), DataAccount),
                                                                 createdby = rr["CreatedBy"].Equals(DBNull.Value) ? new { } : JeeWorkLiteController.Get_InfoUsers(rr["CreatedBy"].ToString(), DataAccount),
                                                             },
                                                 Tags = from t in ds.Tables[1].AsEnumerable()
                                                        select new
                                                        {
                                                            id_row = t["id_row"],
                                                            title = t["title"],
                                                            color = t["color"]
                                                        },
                                                 Attachments = from dr in ds.Tables[3].AsEnumerable()
                                                               where dr["object_type"].ToString() == "1"
                                                               select new
                                                               {
                                                                   id_row = dr["id_row"],
                                                                   path = JeeWorkLiteController.genLinkAttachment(_configuration, dr["path"]),
                                                                   filename = dr["filename"],
                                                                   type = dr["type"],
                                                                   isImage = UploadHelper.IsImage(dr["type"].ToString()),
                                                                   icon = UploadHelper.GetIcon(dr["type"].ToString()),
                                                                   size = dr["size"],
                                                                   // NguoiTao = dr["username"],
                                                                   Object_Type = dr["object_type"],
                                                                   CreatedBy = dr["CreatedBy"],
                                                                   CreatedDate = string.Format("{0:dd/MM/yyyy HH:mm}", dr["CreatedDate"])
                                                               },
                                                 Attachments_Result = from dr in ds.Tables[3].AsEnumerable()
                                                                      where dr["object_type"].ToString() == "11"
                                                                      select new
                                                                      {
                                                                          id_row = dr["id_row"],
                                                                          path = JeeWorkLiteController.genLinkAttachment(_configuration, dr["path"]),
                                                                          filename = dr["filename"],
                                                                          type = dr["type"],
                                                                          isImage = UploadHelper.IsImage(dr["type"].ToString()),
                                                                          icon = UploadHelper.GetIcon(dr["type"].ToString()),
                                                                          size = dr["size"],
                                                                          /// NguoiTao = dr["username"],
                                                                          Object_Type = dr["object_type"],
                                                                          CreatedBy = dr["CreatedBy"],
                                                                          CreatedDate = string.Format("{0:dd/MM/yyyy HH:mm}", dr["CreatedDate"])
                                                                      },
                                                 Process = from t in ds.Tables[4].AsEnumerable()
                                                           select new
                                                           {
                                                               id_row = t["id_row"],
                                                               workid = t["workid"],
                                                               statusname = t["statusName"],
                                                               color = t["color"],
                                                               checker = t["checker"],
                                                               change_note = t["change_note"],
                                                               position = t["Position"],
                                                               statusid = t["statusid"],
                                                           },
                                             }
                                }).FirstOrDefault();
                    return JsonResultCommon.ThanhCong(data);
                }
                #endregion
            }
            catch (Exception ex)
            {
                return JsonResultCommon.Exception(_logger, ex, _config, loginData);
            }
        }
        /// <summary>
        /// Gantt view
        /// </summary>
        /// <param name="query"></param>
        /// <returns></returns>
        [Route("gantt-view")]
        [HttpGet]
        public async Task<object> Ganttview([FromQuery] QueryParams query)
        {

            UserJWT loginData = Ulities.GetUserByHeader(HttpContext.Request.Headers);
            if (loginData == null)
                return JsonResultCommon.DangNhap();
            if (query == null)
                query = new QueryParams();
            bool Visible = true;
            PageModel pageModel = new PageModel();
            try
            {
                string domain = _configuration.GetValue<string>("Host:JeeWork_API") + "/";
                string ConnectionString = JeeWorkLiteController.getConnectionString(ConnectionCache, loginData.CustomerID, _configuration);
                using (DpsConnection cnn = new DpsConnection(ConnectionString))
                {
                    string listDept = JeeWorkLiteController.getListDepartment_GetData(loginData, cnn, HttpContext.Request.Headers, _configuration, ConnectionString);
                    #region Lấy dữ liệu account từ JeeAccount
                    DataAccount = JeeWorkLiteController.GetAccountFromJeeAccount(HttpContext.Request.Headers, _configuration);
                    if (DataAccount == null)
                        return JsonResultCommon.Custom("Lỗi lấy danh sách nhân viên từ hệ thống quản lý tài khoản");

                    string error = "";
                    string listID = JeeWorkLiteController.ListAccount(HttpContext.Request.Headers, out error, _configuration);
                    if (error != "")
                        return JsonResultCommon.Custom(error);
                    #endregion
                    if (string.IsNullOrEmpty(query.filter["id_project_team"]))
                        return JsonResultCommon.Custom("Dự án/phòng ban bắt buộc nhập");
                    #region filter thời gian, keyword
                    DateTime from = Common.GetDateTime();
                    DateTime to = Common.GetDateTime();
                    if (!string.IsNullOrEmpty(query.filter["TuNgay"]))
                    {
                        bool from1 = DateTime.TryParseExact(query.filter["TuNgay"], "dd/MM/yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out from);
                        if (!from1)
                            return JsonResultCommon.Custom("Thời gian bắt đầu không hợp lệ");
                    }
                    if (!string.IsNullOrEmpty(query.filter["DenNgay"]))
                    {
                        bool to1 = DateTime.TryParseExact(query.filter["DenNgay"], "dd/MM/yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out to);
                        if (!to1)
                            return JsonResultCommon.Custom("Thời gian kết thúc không hợp lệ");
                    }
                    #endregion
                    string displayChild = "0";//hiển thị con: 0-không hiển thị, 1- 1 cấp con, 2- nhiều cấp con
                    if (!string.IsNullOrEmpty(query.filter["displayChild"]))
                        displayChild = query.filter["displayChild"];
                    string strG = @"select 0 as id_row, N'Chưa phân loại' as title union
                    select id_row, title from we_group g where disabled=0 
                    and id_project_team=" + query.filter["id_project_team"];
                    string sql_status = @"select id_row, statusname as title, color, position, type 
                                    from we_status 
                                    where disabled = 0 and id_project_team=" + query.filter["id_project_team"] + " " +
                                    "order by position";
                    DataTable dt_st = cnn.CreateDataTable(sql_status);
                    //DataTable dt_st = cnn.CreateDataTable(strG);
                    DataSet ds = await GetWork_ClickUp(Request.Headers, cnn, query, loginData.UserID, DataAccount, listDept);
                    DataTable dt_stt = cnn.CreateDataTable($"select * from we_status where id_project_team=" + query.filter["id_project_team"]);
                    if (cnn.LastError != null || ds == null)
                        return JsonResultCommon.Exception(_logger, cnn.LastError, _config, loginData, ControllerContext);
                    var temp = filterWork(ds.Tables[0].AsEnumerable().Where(x => x["id_parent"] == DBNull.Value), query.filter);//k bao gồm con
                    var tags = ds.Tables[1].AsEnumerable();

                    // Phân trang
                    int total = temp.Count();
                    if (total == 0)
                        return JsonResultCommon.ThanhCong(new
                        {
                            rows = new List<string>(),
                            items = new List<string>()
                        });
                    if (query.more)
                    {
                        query.page = 1;
                        query.record = total;
                    }

                    pageModel.TotalCount = total;
                    pageModel.AllPage = (int)Math.Ceiling(total / (decimal)query.record);
                    pageModel.Size = query.record;
                    pageModel.Page = query.page;
                    var dtNew = temp.Skip((query.page - 1) * query.record).Take(query.record);
                    var dtChild = ds.Tables[0].AsEnumerable().Where(x => x["id_parent"] != DBNull.Value).AsEnumerable();
                    var rows = (from rr in dt_st.AsEnumerable()
                                select new
                                {
                                    id = "G" + rr["id_row"],
                                    label = rr["title"],
                                    expanded = true,
                                    parentId = "",
                                    start_date = "",
                                    end_date = "",
                                    deadline = "",
                                    status = "",
                                    color = "",
                                }).AsEnumerable();
                    rows = rows.Concat(from rr in dtNew.AsEnumerable()
                                       select new
                                       {
                                           id = "W" + rr["id_row"],
                                           label = rr["title"],
                                           expanded = true,
                                           parentId = rr["status"] == DBNull.Value ? "G0" : ("G" + rr["status"]),
                                           start_date = rr["start_date"] == DBNull.Value ? "--" : string.Format("{0:dd/MM}", rr["start_date"]),
                                           end_date = rr["end_date"] == DBNull.Value ? "--" : string.Format("{0:dd/MM}", rr["end_date"]),
                                           deadline = rr["deadline"] == DBNull.Value ? "--" : string.Format("{0:dd/MM}", rr["deadline"]),
                                           status = rr["status"] == DBNull.Value ? "DOING" : (dt_stt.AsEnumerable().Where(x => rr["status"].ToString().Contains(x["id_row"].ToString())).FirstOrDefault())["StatusName"].ToString(),
                                           color = rr["status"] == DBNull.Value ? "DOING" : (dt_stt.AsEnumerable().Where(x => rr["status"].ToString().Contains(x["id_row"].ToString())).FirstOrDefault())["color"].ToString(),
                                       });
                    rows = rows.Concat(from rr in dtChild
                                       select new
                                       {
                                           id = "W" + rr["id_row"],
                                           label = rr["title"],
                                           expanded = true,
                                           parentId = "W" + rr["id_parent"],
                                           start_date = rr["start_date"] == DBNull.Value ? "--" : string.Format("{0:dd/MM}", rr["start_date"]),
                                           end_date = rr["end_date"] == DBNull.Value ? "--" : string.Format("{0:dd/MM}", rr["end_date"]),
                                           deadline = rr["deadline"] == DBNull.Value ? "--" : string.Format("{0:dd/MM}", rr["deadline"]),
                                           status = rr["status"] == DBNull.Value ? "DOING" : (dt_stt.AsEnumerable().Where(x => rr["status"].ToString().Contains(x["id_row"].ToString())).FirstOrDefault())["StatusName"].ToString(),
                                           color = rr["status"] == DBNull.Value ? "DOING" : (dt_stt.AsEnumerable().Where(x => rr["status"].ToString().Contains(x["id_row"].ToString())).FirstOrDefault())["color"].ToString(),
                                       });
                    double ms = 0;
                    var items = (from rr in dtNew.AsEnumerable()
                                 select new
                                 {
                                     id = rr["id_row"],
                                     rowId = "W" + rr["id_row"],
                                     label = rr["title"],
                                     style = getStyle(rr["status"].ToString(), dt_stt),
                                     time = new
                                     {
                                         end = ms = getMs(rr["deadline"]),
                                         start = rr["start_date"] == DBNull.Value ? ms : getMs(rr["start_date"]),
                                     }
                                 }).AsEnumerable();
                    items = items.Concat(from rr in dtChild
                                         select new
                                         {
                                             id = rr["id_row"],
                                             rowId = "W" + rr["id_row"],
                                             label = rr["title"],
                                             style = getStyle(rr["status"].ToString(), dt_stt),
                                             time = new
                                             {
                                                 end = ms = getMs(rr["deadline"]),
                                                 start = rr["start_date"] == DBNull.Value ? ms : getMs(rr["start_date"]),
                                             }
                                         });
                    var data = new
                    {
                        rows = rows,
                        items = items
                    };
                    return JsonResultCommon.ThanhCong(data, pageModel, Visible);
                }
            }
            catch (Exception ex)
            {
                return JsonResultCommon.Exception(_logger, ex, _config, loginData);
            }
        }
        /// <summary>
        /// Gantt Editor
        /// </summary>
        /// <param name="query"></param>
        /// <returns></returns>
        [Route("gantt-editor")]
        [HttpGet]
        public async Task<object> GanttEditor([FromQuery] QueryParams query)
        {
            UserJWT loginData = Ulities.GetUserByHeader(HttpContext.Request.Headers);
            if (loginData == null)
                return JsonResultCommon.DangNhap();
            if (query == null)
                query = new QueryParams();
            bool Visible = true;
            PageModel pageModel = new PageModel();
            try
            {
                string domain = _configuration.GetValue<string>("Host:JeeWork_API") + "/";
                string ConnectionString = JeeWorkLiteController.getConnectionString(ConnectionCache, loginData.CustomerID, _configuration);
                using (DpsConnection cnn = new DpsConnection(ConnectionString))
                {
                    string listDept = JeeWorkLiteController.getListDepartment_GetData(loginData, cnn, HttpContext.Request.Headers, _configuration, ConnectionString);
                    #region Lấy dữ liệu account từ JeeAccount
                    DataAccount = JeeWorkLiteController.GetAccountFromJeeAccount(HttpContext.Request.Headers, _configuration);
                    if (DataAccount == null)
                        return JsonResultCommon.Custom("Lỗi lấy danh sách nhân viên từ hệ thống quản lý tài khoản");
                    string error = "";
                    #endregion
                    if (string.IsNullOrEmpty(query.filter["id_project_team"]))
                        return JsonResultCommon.Custom("Dự án/phòng ban bắt buộc nhập");

                    #region filter thời gian , keyword
                    DateTime from = Common.GetDateTime();
                    DateTime to = Common.GetDateTime();
                    if (!string.IsNullOrEmpty(query.filter["TuNgay"]))
                    {
                        bool from1 = DateTime.TryParseExact(query.filter["TuNgay"], "dd/MM/yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out from);
                        if (!from1)
                            return JsonResultCommon.Custom("Thời gian bắt đầu không hợp lệ");
                    }
                    if (!string.IsNullOrEmpty(query.filter["DenNgay"]))
                    {
                        bool to1 = DateTime.TryParseExact(query.filter["DenNgay"], "dd/MM/yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out to);
                        if (!to1)
                            return JsonResultCommon.Custom("Thời gian kết thúc không hợp lệ");
                    }
                    #endregion
                    string displayChild = "0";//hiển thị con: 0-không hiển thị, 1- 1 cấp con, 2- nhiều cấp con
                    if (!string.IsNullOrEmpty(query.filter["displayChild"]))
                        displayChild = query.filter["displayChild"];
                    string strG = @"select 0 as id_row, N'Chưa phân loại' as title union
                    select id_row, title from we_group g where disabled=0 
                    and id_project_team=" + query.filter["id_project_team"];
                    string sql_status = @"select id_row, statusname as title, color, position, type 
                                    from we_status 
                                    where disabled = 0 and id_project_team=" + query.filter["id_project_team"] + " " +
                                    "order by position";
                    DataTable dt_st = cnn.CreateDataTable(sql_status);
                    //DataTable dt_st = cnn.CreateDataTable(strG);
                    DataSet ds = await GetWork_ClickUp(Request.Headers, cnn, query, loginData.UserID, DataAccount, listDept);
                    DataTable dt_stt = cnn.CreateDataTable($"select * from we_status where id_project_team=" + query.filter["id_project_team"]);
                    if (cnn.LastError != null || ds == null)
                        return JsonResultCommon.Exception(_logger, cnn.LastError, _config, loginData, ControllerContext);
                    var temp = filterWork(ds.Tables[0].AsEnumerable().Where(x => x["id_parent"] == DBNull.Value), query.filter);//k bao gồm con
                    var tags = ds.Tables[1].AsEnumerable();

                    // Phân trang
                    int total = temp.Count();
                    if (total == 0)
                        return JsonResultCommon.ThanhCong(new
                        {
                            rows = new List<string>(),
                            items = new List<string>()
                        });
                    if (query.more)
                    {
                        query.page = 1;
                        query.record = total;
                    }

                    pageModel.TotalCount = total;
                    pageModel.AllPage = (int)Math.Ceiling(total / (decimal)query.record);
                    pageModel.Size = query.record;
                    pageModel.Page = query.page;
                    var dtNew = temp.Skip((query.page - 1) * query.record).Take(query.record);
                    var dtChild = ds.Tables[0].AsEnumerable().Where(x => x["id_parent"] != DBNull.Value).AsEnumerable();
                    //var rows = (from rr in dt_st.AsEnumerable()
                    //            select new
                    //            {
                    //                pID = int.Parse(rr["id_row"].ToString()),
                    //                pName = rr["title"],
                    //                pStart = "",
                    //                pEnd = "",
                    //                pClass = "ggroupblack",
                    //                pLink = "",
                    //                pMile = 0,
                    //                pRes = "",
                    //                pComp = 0,
                    //                pGroup = 1,
                    //                pParent = 0,
                    //                pOpen = 1,
                    //                pDepend = "",
                    //                pCaption = "",
                    //                pNotes = "",
                    //            }).AsEnumerable();
                    var rows = (from rr in dtNew.AsEnumerable()
                                select new
                                {
                                    pID = int.Parse(rr["id_row"].ToString()),
                                    pName = rr["title"],
                                    pStart = rr["start_date"] == DBNull.Value ? "" : string.Format("{0:yyyy-MM-dd}", rr["start_date"]),
                                    pEnd = rr["deadline"] == DBNull.Value ? "" : string.Format("{0:yyyy-MM-dd}", rr["deadline"]),
                                    //pClass = (dt_stt.AsEnumerable().Where(x => rr["status"].ToString().Contains(x["id_row"].ToString())).FirstOrDefault())["color"].ToString(),
                                    pClass = "gtaskyellow",
                                    pLink = "",
                                    pMile = 0,
                                    pRes = "",
                                    pComp = 0,
                                    pGroup = 1,
                                    pParent = 0,
                                    pOpen = 1,
                                    pDepend = "",
                                    pCaption = "",
                                    pNotes = "",
                                });
                    rows = rows.Concat(from rr in dtChild
                                       select new
                                       {
                                           pID = int.Parse(rr["id_row"].ToString()),
                                           pName = rr["title"],
                                           pStart = rr["start_date"] == DBNull.Value ? "" : string.Format("{0:yyyy-MM-dd}", rr["start_date"]),
                                           pEnd = rr["deadline"] == DBNull.Value ? "" : string.Format("{0:yyyy-MM-dd}", rr["deadline"]),
                                           //pClass = (dt_stt.AsEnumerable().Where(x => rr["status"].ToString().Contains(x["id_row"].ToString())).FirstOrDefault())["color"].ToString(),
                                           pClass = "gtaskblue",
                                           pLink = "",
                                           pMile = 0,
                                           pRes = "",
                                           pComp = 0,
                                           pGroup = 0,
                                           pParent = int.Parse(rr["id_parent"].ToString()),
                                           pOpen = 1,
                                           pDepend = "",
                                           pCaption = "",
                                           pNotes = "",
                                       });
                    var data = new
                    {
                        rows = rows
                        //items = items
                    };
                    return JsonResultCommon.ThanhCong(data, pageModel, Visible);
                }
            }
            catch (Exception ex)
            {
                return JsonResultCommon.Exception(_logger, ex, _config, loginData);
            }
        }
        /// <summary>
        /// Get Style
        /// </summary>
        /// <param name="v"></param>
        /// <returns></returns>
        private object getStyle(string id, DataTable dt)
        {
            string b = "#4298F4";
            if (dt.Rows.Count > 0)
            {
                var stt = dt.AsEnumerable().Where(x => id.ToString().Contains(x["id_row"].ToString())).FirstOrDefault();
                b = stt["color"].ToString();
            }
            return new { background = b };
        }
        private double getMs(object obj)
        {
            if (obj == DBNull.Value)
                return 0;
            DateTime dateTime = (DateTime)(obj);
            var ttt = dateTime.ToUniversalTime().Subtract(
new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)
).TotalMilliseconds;// / TimeSpan.TicksPerMillisecond;
            return Math.Round(ttt);
        }
        /// <summary>
        /// Thêm mới công việc
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        [Route("Insert")]
        [HttpPost]
        public object Insert(WorkModel data)
        {
            UserJWT loginData = Ulities.GetUserByHeader(HttpContext.Request.Headers);
            if (loginData == null)
                return JsonResultCommon.DangNhap();
            try
            {
                DataTable dt_status = new DataTable();
                string strRe = "";
                if (string.IsNullOrEmpty(data.title))
                    strRe += (strRe == "" ? "" : ",") + "tên công việc";
                if (data.id_project_team <= 0)
                    strRe += (strRe == "" ? "" : ",") + "dự án/phòng ban";
                if (strRe != "")
                    return JsonResultCommon.BatBuoc(strRe);
                string ConnectionString = JeeWorkLiteController.getConnectionString(ConnectionCache, loginData.CustomerID, _configuration);
                using (DpsConnection cnn = new DpsConnection(ConnectionString))
                {
                    if (Common.IsProjectClosed(data.id_project_team.ToString(), cnn))
                    {
                        return JsonResultCommon.Custom("Dự án đã đóng không thể xóa công việc");
                    }
                    Common comm = new Common(ConnectionString);
                    // quyền 1 : tạo mới công việc
                    bool rs = Common.CheckIsUpdatedTask(data.id_project_team.ToString(), 1, loginData, cnn, ConnectionString);
                    if (!rs)
                    {
                        return JsonResultCommon.Custom("Bạn không có quyền tạo mới công việc");
                    }
                    #region Lấy dữ liệu account từ JeeAccount
                    DataAccount = JeeWorkLiteController.GetAccountFromJeeAccount(HttpContext.Request.Headers, _configuration);
                    if (DataAccount == null)
                        return JsonResultCommon.Custom("Lỗi lấy danh sách nhân viên từ hệ thống quản lý tài khoản");
                    #endregion
                    long iduser = loginData.UserID;
                    long idk = loginData.CustomerID;
                    Hashtable val = new Hashtable();
                    if (data.id_parent > 0)
                        val.Add("id_parent", data.id_parent);
                    val.Add("title", data.title);
                    if (string.IsNullOrEmpty(data.description))
                        val.Add("description", "");
                    else
                        val.Add("description", data.description);
                    val.Add("id_project_team", data.id_project_team);
                    if (data.status > 0)
                        val.Add("status", data.status);
                    else // Trường hợp người dùng không chọn status thì lấy status mặc định của project team
                    {
                        DataTable dt = JeeWorkLiteController.StatusDynamic(data.id_project_team, new List<AccUsernameModel>(), cnn);
                        if (dt.Rows.Count > 0)
                        {
                            DataRow[] RowStatus = dt.Select("IsDefault = 1 and IsFinal = 0");
                            if (RowStatus.Length > 0)
                            {
                                val.Add("status", RowStatus[0]["id_row"]);
                                data.status = long.Parse(RowStatus[0]["id_row"].ToString());
                            }
                        }
                    }
                    int estimates = 0;
                    if ((!"".Equals(data.estimates)) && int.TryParse(data.estimates, out estimates))
                    {
                        val.Add("estimates", estimates);
                    }
                    else
                        val.Add("estimates", DBNull.Value);
                    if (data.start_date > DateTime.MinValue)
                    {
                        val.Add("start_date", data.start_date);
                    }
                    if (data.deadline > DateTime.MinValue)
                        val.Add("deadline", data.deadline);
                    if (data.id_group > 0)
                        val.Add("id_group", data.id_group);
                    if (data.id_milestone > 0)
                        val.Add("id_milestone", data.id_milestone);
                    val.Add("CreatedDate", Common.GetDateTime());
                    val.Add("CreatedBy", iduser);
                    val.Add("clickup_prioritize", data.urgent);
                    cnn.BeginTransaction();
                    if (cnn.Insert(val, "we_work") != 1)
                    {
                        cnn.RollbackTransaction();
                        return JsonResultCommon.Exception(_logger, cnn.LastError, _config, loginData, ControllerContext);
                    }
                    long idc = long.Parse(cnn.ExecuteScalar("select IDENT_CURRENT('we_work')").ToString());
                    //Insert người follow cho từng tình trạng của công việc
                    dt_status = JeeWorkLiteController.StatusDynamic(data.id_project_team, new List<AccUsernameModel>(), cnn);
                    if (dt_status.Rows.Count > 0)
                    {
                        foreach (DataRow item in dt_status.Rows)
                        {
                            val = new Hashtable();
                            val.Add("id_project_team", data.id_project_team);
                            val.Add("workid", idc);
                            val.Add("statusid", item["id_row"]);
                            if (string.IsNullOrEmpty(item["follower"].ToString()))
                                val.Add("checker", DBNull.Value);
                            else
                                val.Add("checker", item["follower"]);
                            val.Add("createddate", Common.GetDateTime());
                            val.Add("createdby", iduser);
                            if (cnn.Insert(val, "we_work_process") != 1)
                            {
                                cnn.RollbackTransaction();
                                return JsonResultCommon.Exception(_logger, cnn.LastError, _config, loginData, ControllerContext);
                            }
                            long processid = long.Parse(cnn.ExecuteScalar("select IDENT_CURRENT('we_work_process')").ToString());
                            val = new Hashtable();
                            val.Add("processid", processid);
                            if (string.IsNullOrEmpty(item["follower"].ToString()))
                            {
                                val.Add("new_checker", DBNull.Value);
                            }
                            else
                            {
                                val.Add("new_checker", item["follower"]);
                                var info = DataAccount.Where(x => item["follower"].ToString().Contains(x.UserId.ToString())).FirstOrDefault();
                                if (info != null)
                                {
                                    val.Add("content_note", loginData.customdata.personalInfo.Fullname + " thêm " + info.FullName + " vào theo dõi");
                                }
                            }
                            val.Add("createddate", Common.GetDateTime());
                            val.Add("createdby", iduser);
                            if (cnn.Insert(val, "we_work_process_log") != 1)
                            {
                                cnn.RollbackTransaction();
                                return JsonResultCommon.Exception(_logger, cnn.LastError, _config, loginData, ControllerContext);
                            }
                        }
                    }
                    if (data.Users != null)
                    {
                        Hashtable val1 = new Hashtable();
                        val1["id_work"] = idc;
                        val1["CreatedDate"] = Common.GetDateTime();
                        val1["CreatedBy"] = iduser;
                        foreach (var user in data.Users)
                        {
                            val1["id_user"] = user.id_user;
                            val1["loai"] = user.loai;
                            if (cnn.Insert(val1, "we_work_user") != 1)
                            {
                                cnn.RollbackTransaction();
                                return JsonResultCommon.Exception(_logger, cnn.LastError, _config, loginData, ControllerContext);
                            }
                        }
                    }
                    if (data.Tags != null)
                    {
                        Hashtable val2 = new Hashtable();
                        val2["id_work"] = idc;
                        val2["CreatedDate"] = Common.GetDateTime();
                        val2["CreatedBy"] = iduser;
                        foreach (var tag in data.Tags)
                        {
                            val2["id_tag"] = tag.id_tag;
                            if (cnn.Insert(val2, "we_work_tag") != 1)
                            {
                                cnn.RollbackTransaction();
                                return JsonResultCommon.Exception(_logger, cnn.LastError, _config, loginData, ControllerContext);
                            }
                        }
                    }
                    if (data.Attachments != null)
                    {
                        foreach (var item in data.Attachments)
                        {
                            var temp = new AttachmentModel()
                            {
                                item = item,
                                object_type = 1,
                                object_id = idc,
                                id_user = loginData.UserID
                            };
                            if (!AttachmentController.upload(temp, cnn, _hostingEnvironment.ContentRootPath, _configuration))
                            {
                                cnn.RollbackTransaction();
                                return JsonResultCommon.Exception(_logger, cnn.LastError, _config, loginData, ControllerContext);
                            }
                        }
                    }
                    if (!JeeWorkLiteController.log(_logger, loginData.Username, cnn, 1, idc, iduser, data.title))
                    {
                        cnn.RollbackTransaction();
                        return JsonResultCommon.Exception(_logger, cnn.LastError, _config, loginData, ControllerContext);
                    }
                    cnn.EndTransaction();
                    #region Ghi log trong project
                    string LogContent = "", LogEditContent = "";
                    LogContent = LogEditContent = "Thêm mới dữ liệu công việc: title=" + data.title + ", id_project_team=" + data.id_project_team;
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
                    // thông báo nhắc nhở
                    foreach (var user in data.Users)
                    {
                        if (user.loai == 1)
                        {
                            NhacNho.UpdateQuantityTask_Users(user.id_user, loginData.CustomerID, "+", _configuration, _producer);
                        }
                    }
                    #region gửi event insert công việc mới
                    DataTable dtwork = cnn.CreateDataTable("select * from v_wework_new where Disabled = 0 and id_row = " + idc);
                    if (dtwork.Rows.Count > 0)
                    {
                        Post_Automation_Model postauto = new Post_Automation_Model();
                        postauto.taskid = idc; // id task mới tạo
                        postauto.userid = loginData.UserID;
                        postauto.listid = data.id_project_team; // id project team
                        postauto.customerid = loginData.CustomerID;
                        postauto.eventid = 7;
                        postauto.departmentid = long.Parse(dtwork.Rows[0]["id_department"].ToString()); // id_department
                        Automation.SendAutomation(postauto, _configuration, _producer);
                    }
                    #endregion
                    data.id_row = idc;
                    var users_loai1 = JeeWorkLiteController.GetUserSendNotify(loginData, idc, 1, 1, ConnectionString, DataAccount, cnn);
                    var users_loai2 = JeeWorkLiteController.GetUserSendNotify(loginData, idc, 1, 2, ConnectionString, DataAccount, cnn);
                    #region Lấy thông tin để thông báo
                    int templateguimail = 10;
                    SendNotifyModel noti = new SendNotifyModel();
                    noti = JeeWorkLiteController.GetInfoNotify(templateguimail, ConnectionString);
                    string workname = "";
                    workname = $"\"{data.title}\"";
                    string TitleLanguageKey = "ww_themmoicongviec";
                    #endregion
                    SqlConditions cond_user = new SqlConditions();
                    cond_user.Add("id_work", data.id_row);
                    cond_user.Add("disabled", 0);
                    cond_user.Add("loai", 1);
                    string sql_user = "";
                    sql_user = "select id_user from we_work_user where loai = @loai and disabled = @disabled and id_work=@id_work";
                    object _user = cnn.ExecuteScalar(sql_user, cond_user);
                    if (_user == null)
                        _user = "0";
                    if (users_loai1.Count > 0)
                    {
                        NotifyModel notify_model = new NotifyModel();
                        Hashtable has_replace = new Hashtable();
                        has_replace.Add("nguoigui", loginData.Username);
                        has_replace.Add("tencongviec", workname);
                        notify_model.AppCode = "WORK";
                        notify_model.From_IDNV = loginData.UserID.ToString();
                        notify_model.TitleLanguageKey = LocalizationUtility.GetBackendMessage(TitleLanguageKey, "", "vi");
                        notify_model.TitleLanguageKey = notify_model.TitleLanguageKey.Replace("$nguoigui$", loginData.customdata.personalInfo.Fullname)
                            .Replace("$tencongviec$", workname);
                        notify_model.ReplaceData = has_replace;
                        notify_model.To_Link_MobileApp = noti.link_mobileapp.Replace("$id$", data.id_row.ToString());
                        notify_model.To_Link_WebApp = noti.link.Replace("$id$", data.id_row.ToString());
                        string user_assign = " cho bạn";
                        for (int i = 0; i < users_loai1.Count; i++)
                        {
                            notify_model.To_IDNV = users_loai1[i].ToString();
                            var info = DataAccount.Where(x => notify_model.To_IDNV.ToString().Contains(x.UserId.ToString())).FirstOrDefault();
                            //if (!_user.ToString().Equals(notify_model.To_IDNV))
                            //{
                            //    user_assign = info.FullName;
                            //}
                            user_assign = dtwork.Rows[0]["project_team"].ToString() + " (Phòng ban: " + dtwork.Rows[0]["department"].ToString() + ")";
                            notify_model.TitleLanguageKey = notify_model.TitleLanguageKey.Replace("$forme$", " trong dự án: " + user_assign + "");
                            if (info is not null)
                            {
                                bool kq_noti = JeeWorkLiteController.SendNotify(loginData, info.Username, notify_model, _notifier, _configuration);
                            }
                        }
                    }
                    JeeWorkLiteController.SendEmail(data.id_row, users_loai2, templateguimail, loginData, ConnectionString, _notifier, _configuration);
                    //#region Lấy thông tin để thông báo
                    //SendNotifyModel noti = JeeWorkLiteController.GetInfoNotify(10, ConnectionString);
                    //#endregion
                    //JeeWorkLiteController.SendEmail(idc, data.Users.Select(x => x.id_user).ToList(), 10, loginData, ConnectionString, _notifier, _configuration);
                    //#region Notify thêm mới công việc
                    //Hashtable has_replace = new Hashtable();
                    //for (int i = 0; i < data.Users.Count; i++)
                    //{
                    //    NotifyModel notify_model = new NotifyModel();
                    //    has_replace = new Hashtable();
                    //    has_replace.Add("nguoigui", loginData.Username);
                    //    has_replace.Add("tencongviec", data.title);
                    //    notify_model.AppCode = "WORK";
                    //    notify_model.From_IDNV = loginData.UserID.ToString();
                    //    notify_model.To_IDNV = data.Users[i].id_user.ToString();
                    //    notify_model.TitleLanguageKey = LocalizationUtility.GetBackendMessage("ww_themmoicongviec", "", "vi");
                    //    notify_model.TitleLanguageKey = notify_model.TitleLanguageKey.Replace("$nguoigui$", loginData.customdata.personalInfo.Fullname);
                    //    notify_model.TitleLanguageKey = notify_model.TitleLanguageKey.Replace("$tencongviec$", data.title);
                    //    notify_model.ReplaceData = has_replace;
                    //    notify_model.To_Link_MobileApp = noti.link_mobileapp.Replace("$id$", data.id_row.ToString());
                    //    notify_model.To_Link_WebApp = noti.link.Replace("$id$", data.id_row.ToString());
                    //    var info = DataAccount.Where(x => notify_model.To_IDNV.ToString().Contains(x.UserId.ToString())).FirstOrDefault();
                    //    if (info is not null)
                    //    {
                    //        bool kq_noti = JeeWorkLiteController.SendNotify(loginData, info.Username, notify_model, _notifier, _configuration);
                    //    }
                    //}
                    //#endregion
                    return JsonResultCommon.ThanhCong(data);
                }
            }
            catch (Exception ex)
            {
                return JsonResultCommon.Exception(_logger, ex, _config, loginData);
            }
        }
        /// <summary>
        /// Update thông tin công việc
        /// <param name="data"></param>
        /// <returns></returns>
        [Route("Update")]
        [HttpPost]
        public async Task<BaseModel<object>> Update(WorkModel data)
        {
            UserJWT loginData = Ulities.GetUserByHeader(HttpContext.Request.Headers);
            if (loginData == null)
                return JsonResultCommon.DangNhap();
            try
            {
                string ConnectionString = JeeWorkLiteController.getConnectionString(ConnectionCache, loginData.CustomerID, _configuration);
                using (DpsConnection cnn = new DpsConnection(ConnectionString))
                {
                    #region Lấy dữ liệu account từ JeeAccount
                    DataAccount = JeeWorkLiteController.GetAccountFromJeeAccount(HttpContext.Request.Headers, _configuration);
                    if (DataAccount == null)
                        return JsonResultCommon.Custom("Lỗi lấy danh sách nhân viên từ hệ thống quản lý tài khoản");
                    #endregion
                    SqlConditions sqlcond = new SqlConditions();
                    sqlcond.Add("id_row", data.id_row);
                    sqlcond.Add("disabled", 0);
                    string s = "select * from we_work where (where)";
                    DataTable old = cnn.CreateDataTable(s, "(where)", sqlcond);
                    if (old == null || old.Rows.Count == 0)
                        return JsonResultCommon.KhongTonTai("Công việc");
                    long iduser = loginData.UserID;
                    long idk = loginData.CustomerID;
                    Hashtable val = new Hashtable();
                    val.Add("title", data.title);
                    if (string.IsNullOrEmpty(data.description))
                        val.Add("description", "");
                    else
                        val.Add("description", data.description);
                    if (data.id_group > 0)
                        val.Add("id_group", data.id_group);
                    else
                        val.Add("id_group", DBNull.Value);
                    val.Add("prioritize", data.prioritize);
                    val.Add("urgent", data.urgent);
                    int estimates = 0;
                    if ((!"".Equals(data.estimates)) && int.TryParse(data.estimates, out estimates))
                    {
                        val.Add("estimates", estimates);
                    }
                    else
                        val.Add("estimates", DBNull.Value);
                    val.Add("UpdatedDate", Common.GetDateTime());
                    val.Add("UpdatedBy", iduser);
                    val.Add("status", data.status);
                    cnn.BeginTransaction();
                    if (cnn.Update(val, sqlcond, "we_work") != 1)
                    {
                        cnn.RollbackTransaction();
                        return JsonResultCommon.Exception(_logger, cnn.LastError, _config, loginData, ControllerContext);
                    }
                    #region Chỉnh sửa (Không cho sửa User)
                    //string ids = string.Join(",", data.Users.Where(x => x.loai == 1 && x.id_row > 0).Select(x => x.id_row));
                    //if (ids != "")
                    //{
                    //    string strDel = "Update we_work_user set Disabled=1, UpdatedDate=GETUTCDATE(), UpdatedBy=" + iduser + " where Disabled=0 and loai=1 and id_work=" + data.id_row + " and id_row not in (" + ids + ")";
                    //    if (cnn.ExecuteNonQuery(strDel) < 0)
                    //    {
                    //        cnn.RollbackTransaction();
                    //        return JsonResultCommon.Exception(cnn.LastError, _config, loginData.CustomerID,ControllerContext);
                    //    }
                    //}
                    //foreach (var user in data.Users)
                    //{
                    //    if (user.id_row == 0)
                    //    {
                    //        Hashtable val1 = new Hashtable();
                    //        val1["id_work"] = data.id_row;
                    //        val1["CreatedDate"] = Common.GetDateTime();
                    //        val1["CreatedBy"] = iduser;
                    //        val1["id_user"] = user.id_user;
                    //        val1["loai"] = 1;
                    //        if (cnn.Insert(val1, "we_work_user") != 1)
                    //        {
                    //            cnn.RollbackTransaction();
                    //            return JsonResultCommon.Exception(cnn.LastError, _config, loginData.CustomerID,ControllerContext);
                    //        }
                    //    }
                    //}
                    #endregion
                    DataTable dt = cnn.CreateDataTable(s, "(where)", sqlcond);
                    //string LogContent = "", LogEditContent = "";
                    //string LogEditContentTemp = DpsPage.GetEditLogContent(old, dt);
                    //if (!LogEditContentTemp.Equals(""))
                    //{
                    //    LogEditContent = "Chỉnh sửa dữ liệu (" + data.id_row + ") : " + LogEditContentTemp;
                    //    LogContent = "Chỉnh sửa dữ liệu work (" + data.id_row + "), Chi tiết xem trong log chỉnh sửa chức năng";
                    //}
                    //DpsPage.Ghilogfile(loginData.CustomerID.ToString(), LogEditContent, LogContent, loginData.UserName);

                    var keys = new List<string> { "title", "description", "id_group" };
                    var vals = JeeWorkLiteController.CheckKeyChange(keys, old, dt);
                    if (vals[0])
                    {
                        if (!JeeWorkLiteController.log(_logger, loginData.Username, cnn, 17, data.id_row, iduser, "", old.Rows[0]["title"], data.title))
                        {
                            cnn.RollbackTransaction();
                            return JsonResultCommon.Exception(_logger, cnn.LastError, _config, loginData, ControllerContext);
                        }
                    }
                    if (vals[1])
                    {
                        if (!JeeWorkLiteController.log(_logger, loginData.Username, cnn, 16, data.id_row, iduser, "", old.Rows[0]["description"], data.description))
                        {
                            cnn.RollbackTransaction();
                            return JsonResultCommon.Exception(_logger, cnn.LastError, _config, loginData, ControllerContext);
                        }
                    }
                    if (vals[2])
                    {
                        if (!JeeWorkLiteController.log(_logger, loginData.Username, cnn, 12, data.id_row, iduser, "", old.Rows[0]["id_group"], data.id_group))
                        {
                            cnn.RollbackTransaction();
                            return JsonResultCommon.Exception(_logger, cnn.LastError, _config, loginData, ControllerContext);
                        }
                    }
                    #region Lấy thông tin để thông báo
                    SendNotifyModel noti = JeeWorkLiteController.GetInfoNotify(10, ConnectionString);
                    #endregion
                    JeeWorkLiteController.SendEmail(data.id_row, data.Users.Select(x => x.id_user).ToList(), 10, loginData, ConnectionString, _notifier, _configuration);
                    #region Ghi log trong project
                    string LogContent = "", LogEditContent = "";
                    LogEditContent = Common.GetEditLogContent(old, dt);
                    if (!LogEditContent.Equals(""))
                    {
                        LogEditContent = "Chỉnh sửa dữ liệu (" + data.id_row + ") : " + LogEditContent;
                        LogContent = "Chỉnh sửa dữ liệu Công việc (" + data.id_row + "), Chi tiết xem trong log chỉnh sửa chức năng";
                    }
                    Common.Ghilogfile(loginData.CustomerID.ToString(), LogEditContent, LogContent, loginData.Username, ControllerContext);
                    #endregion
                    #region Ghi log lên CDN
                    var d2 = new ActivityLog()
                    {
                        username = loginData.Username,
                        category = LogEditContent,
                        action = loginData.customdata.personalInfo.Fullname + " thao tác",
                        data = JsonConvert.SerializeObject(data)
                    };
                    _logger.LogInformation(JsonConvert.SerializeObject(d2));
                    #endregion
                    cnn.EndTransaction();
                    #region Notify chỉnh sửa công việc
                    Hashtable has_replace = new Hashtable();
                    for (int i = 0; i < data.Users.Count; i++)
                    {
                        NotifyModel notify_model = new NotifyModel();
                        has_replace = new Hashtable();
                        has_replace.Add("nguoigui", loginData.Username);
                        has_replace.Add("tencongviec", data.title);
                        notify_model.AppCode = "WORK";
                        notify_model.From_IDNV = loginData.UserID.ToString();
                        notify_model.To_IDNV = data.Users[i].id_user.ToString();
                        notify_model.TitleLanguageKey = LocalizationUtility.GetBackendMessage("ww_chinhsuacongviec", "", "vi");
                        notify_model.TitleLanguageKey = notify_model.TitleLanguageKey.Replace("$nguoigui$", loginData.customdata.personalInfo.Fullname);
                        notify_model.TitleLanguageKey = notify_model.TitleLanguageKey.Replace("$tencongviec$", data.title);
                        notify_model.ReplaceData = has_replace;
                        notify_model.To_Link_MobileApp = noti.link_mobileapp.Replace("$id$", data.id_row.ToString());
                        notify_model.To_Link_WebApp = noti.link.Replace("$id$", data.id_row.ToString());

                        //try
                        //{
                        //    if (notify_model != null)
                        //    {
                        //        Knoti = new APIModel.Models.Notify();
                        //        bool kq = Knoti.PushNotify(notify_model.From_IDNV, notify_model.To_IDNV, notify_model.AppCode, notify_model.TitleLanguageKey, notify_model.ReplaceData, notify_model.To_Link_WebApp, notify_model.To_Link_MobileApp, notify_model.ComponentName, notify_model.Component);
                        //    }
                        //}
                        //catch
                        //{ }

                        var info = DataAccount.Where(x => notify_model.To_IDNV.ToString().Contains(x.UserId.ToString())).FirstOrDefault();
                        if (info is not null)
                        {
                            bool kq_noti = JeeWorkLiteController.SendNotify(loginData, info.Username, notify_model, _notifier, _configuration);
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
        /// <summary>
        /// Update cột ứng với từng project
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        [Route("update-column-work")]
        [HttpPost]
        public async Task<BaseModel<object>> UpdateColumnWork(ColumnWorkModel data)
        {

            UserJWT loginData = Ulities.GetUserByHeader(HttpContext.Request.Headers);
            if (loginData == null)
                return JsonResultCommon.DangNhap();
            try
            {
                long iduser = loginData.UserID;
                long idk = loginData.CustomerID;
                DataTable dt = new DataTable();
                string column_name = "id_project_team"; long WorkSpaceID = 0;
                string ConnectionString = JeeWorkLiteController.getConnectionString(ConnectionCache, loginData.CustomerID, _configuration);
                using (DpsConnection cnn = new DpsConnection(ConnectionString))
                {
                    SqlConditions sqlcond = new SqlConditions();
                    sqlcond.Add("fieldname", data.columnname);
                    sqlcond.Add("disabled", 0);
                    if (data.id_project_team > 0)
                    {
                        sqlcond.Add("id_project_team", data.id_project_team);
                        column_name = "id_project_team";
                        WorkSpaceID = data.id_project_team;
                    }
                    else
                    {
                        sqlcond.Add("departmentid", data.id_department);
                        column_name = "departmentid";
                        WorkSpaceID = data.id_department;
                    }
                    string sqlq = "";
                    sqlcond.Add("fieldname", data.columnname);
                    sqlcond.Add("disabled", 0);
                    sqlq = "select * from we_fields_project_team where (where)";
                    dt = cnn.CreateDataTable(sqlq, "(where)", sqlcond);
                    if (dt.Rows.Count == 0 || data.isnewfield)
                    {
                        Hashtable val = new Hashtable();
                        val.Add("createddate", Common.GetDateTime());
                        val.Add("createdby", iduser);
                        val.Add("fieldname", data.columnname);
                        string sql_position = "";
                        val.Add(column_name, WorkSpaceID);
                        sql_position = "select ISNULL((select max(position) from we_fields_project_team where " + column_name + " = " + WorkSpaceID + "),0)";
                        long position = 0;
                        position = long.Parse(cnn.ExecuteScalar(sql_position).ToString());
                        val.Add("position", position + 1);
                        val.Add("objectid", 1);
                        val.Add("disabled", 0);
                        if (data.isnewfield)
                        {
                            string strRe = "";
                            if (string.IsNullOrEmpty(data.Title))
                                strRe += (strRe == "" ? "" : ",") + "tên cột";
                            if (strRe != "")
                                return JsonResultCommon.BatBuoc(strRe);
                            val.Add("title", data.Title);
                            val.Add("isnewfield", 1);
                        }
                        else
                            val.Add("isnewfield", 0);
                        cnn.BeginTransaction();
                        if (cnn.Insert(val, "we_fields_project_team") != 1)
                        {
                            cnn.RollbackTransaction();
                            return JsonResultCommon.Exception(_logger, cnn.LastError, _config, loginData, ControllerContext);
                        }
                        long FieldID = long.Parse(cnn.ExecuteScalar("select IDENT_CURRENT('we_fields_project_team')").ToString());
                        if (data.isnewfield)
                        {
                            foreach (var op in data.Options)
                            {
                                val = new Hashtable();
                                if (data.id_project_team > 0)
                                {
                                    val["id_project_team"] = WorkSpaceID;
                                }
                                else
                                {
                                    val["id_department"] = WorkSpaceID;
                                }
                                val["typeid"] = op.TypeID;
                                val["fieldid"] = FieldID;
                                val["value"] = op.Value;
                                val["color"] = op.Color;
                                if (!string.IsNullOrEmpty(op.Note))
                                    val["note"] = op.Note;
                                else
                                    val["note"] = DBNull.Value;
                                if (cnn.Insert(val, "we_newfields_options") != 1)
                                {
                                    cnn.RollbackTransaction();
                                    return JsonResultCommon.Exception(_logger, cnn.LastError, _config, loginData, ControllerContext);
                                }
                            }
                        }
                        cnn.EndTransaction();
                        return JsonResultCommon.ThanhCong(data);
                    }
                    else
                    {
                        cnn.ExecuteNonQuery("delete we_fields_project_team where " + column_name + "=" + WorkSpaceID + " and fieldname = '" + data.columnname + "'");
                        return JsonResultCommon.ThanhCong(data);
                    }
                }
            }
            catch (Exception ex)
            {
                return JsonResultCommon.Exception(_logger, ex, _config, loginData);
            }
        }
        /// <summary>
        /// Detail of custom filter by id
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [Route("detail-column-new-field")]
        [HttpGet]
        public object Detail_column_new_field(long field, long type)
        {

            UserJWT loginData = Ulities.GetUserByHeader(HttpContext.Request.Headers);
            if (loginData == null)
                return JsonResultCommon.DangNhap();
            try
            {
                string ConnectionString = JeeWorkLiteController.getConnectionString(ConnectionCache, loginData.CustomerID, _configuration);
                using (DpsConnection cnn = new DpsConnection(ConnectionString))
                {
                    string sqlq = "", columname = "", tablename = "", tablename_opts = "";
                    if (type < 3)
                    {
                        columname = "departmentid";
                    }
                    else
                    {
                        columname = "id_project_team";
                    }
                    sqlq = @"select * from we_fields_project_team where disabled=0 and id_row =" + field;
                    sqlq += @";select * from we_newfields_options where fieldid = " + field;
                    #region Trả dữ liệu về backend để hiển thị lên giao diện

                    DataSet ds = cnn.CreateDataSet(sqlq);
                    if (cnn.LastError != null || ds == null)
                    {
                        return JsonResultCommon.Exception(_logger, cnn.LastError, _config, loginData, ControllerContext);
                    }
                    if (ds.Tables[0] == null || ds.Tables[0].Rows.Count == 0)
                        return JsonResultCommon.KhongTonTai("Cột");
                    var data = (from r in ds.Tables[0].AsEnumerable()
                                select new
                                {
                                    id_row = r["id_row"],
                                    fieldname = r["fieldname"],
                                    title = r["Title"],
                                    ishidden = r["IsHidden"],
                                    id_project_team = type == 3 ? r["id_project_team"] : "0",
                                    id_department = type < 3 ? r["departmentid"] : "0",
                                    options = from op in ds.Tables[1].AsEnumerable()
                                              select new
                                              {
                                                  rowid = op["RowID"],
                                                  FieldID = op["FieldID"],
                                                  TypeID = op["TypeID"],
                                                  ID_project_team = type == 3 ? r["id_project_team"] : "0",
                                                  id_department = type < 3 ? r["departmentid"] : "0",
                                                  Value = op["Value"],
                                                  Color = op["Color"],
                                                  Note = op["Note"]
                                              }
                                }).FirstOrDefault();
                    return JsonResultCommon.ThanhCong(data);
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
        [Route("update-column-new-field")]
        [HttpPost]
        public async Task<BaseModel<object>> UpdateColumnNewField(ColumnWorkModel data)
        {

            UserJWT loginData = Ulities.GetUserByHeader(HttpContext.Request.Headers);
            if (loginData == null)
                return JsonResultCommon.DangNhap();
            try
            {
                string strRe = "";
                if (string.IsNullOrEmpty(data.Title))
                    strRe += (strRe == "" ? "" : ",") + "tên cột";
                if (data.columnname == "dropdown" && (data.Options == null || data.Options.Count == 0))
                    strRe += (strRe == "" ? "" : ",") + "trường thông tin chi tiết";
                if (strRe != "")
                    return JsonResultCommon.BatBuoc(strRe);
                string ConnectionString = JeeWorkLiteController.getConnectionString(ConnectionCache, loginData.CustomerID, _configuration);
                using (DpsConnection cnn = new DpsConnection(ConnectionString))
                {
                    long iduser = loginData.UserID;
                    long idk = loginData.CustomerID;
                    SqlConditions sqlcond = new SqlConditions();
                    sqlcond.Add("id_row", data.id_row);
                    string s = "select * from we_fields_project_team where disabled=0 and id_row=@id_row";//createdby=@CreatedBy and
                    DataTable old = cnn.CreateDataTable(s, sqlcond);
                    if (cnn.LastError != null || old == null)
                        return JsonResultCommon.Exception(_logger, cnn.LastError, _config, loginData, ControllerContext);
                    if (old.Rows.Count == 0)
                        return JsonResultCommon.KhongTonTai("Column");
                    #region Trường hợp cột này được thêm từ phòng ban nhưng chọn thêm option từ dự án => cập nhật lại Id cho phòng ban
                    if (!string.IsNullOrEmpty(old.Rows[0]["departmentid"].ToString()))
                    {
                        data.id_department = long.Parse(old.Rows[0]["departmentid"].ToString());
                        data.id_project_team = 0;
                    }
                    #endregion
                    Hashtable val = new Hashtable();
                    val.Add("title", data.Title);
                    val.Add("UpdatedDate", Common.GetDateTime());
                    val.Add("UpdatedBy", iduser);
                    string strCheck = "select count(*) from we_fields_project_team where disabled =0 and title=@name and id_row<>@id_row and IsNewField = 1";// and (CreatedBy=@id_user) 
                    if (int.Parse(cnn.ExecuteScalar(strCheck, new SqlConditions() { { "name", data.Title }, { "id_row", data.id_row } }).ToString()) > 0)
                    {
                        return JsonResultCommon.Custom("Cột đã tồn tại");
                    }
                    cnn.BeginTransaction();
                    if (cnn.Update(val, sqlcond, "we_fields_project_team") != 1)
                    {
                        cnn.RollbackTransaction();
                        return JsonResultCommon.Exception(_logger, cnn.LastError, _config, loginData, ControllerContext);

                    }
                    string ids = string.Join(",", data.Options.Where(x => x.rowid > 0).Select(x => x.rowid));
                    if (ids != "")//xóa
                    {
                        string strDel = "delete we_newfields_options where rowid=" + data.id_row + " and rowid not in (" + ids + ")";
                        if (cnn.ExecuteNonQuery(strDel) < 0)
                        {
                            cnn.RollbackTransaction();
                            return JsonResultCommon.Exception(_logger, cnn.LastError, _config, loginData, ControllerContext);
                        }
                    }
                    foreach (var key in data.Options)
                    {
                        val = new Hashtable();
                        if (data.id_project_team > 0)
                        {
                            val["id_project_team"] = data.id_project_team;
                        }
                        else
                        {
                            val["id_department"] = data.id_department;
                        }
                        val["TypeID"] = key.TypeID;
                        val["FieldID"] = key.FieldID;
                        val["Value"] = key.Value;
                        val["Color"] = key.Color;
                        if (!string.IsNullOrEmpty(key.Note))
                            val["Note"] = key.Note;
                        else
                            val["Note"] = DBNull.Value;
                        if (key.rowid == 0)
                        {
                            if (cnn.Insert(val, "we_newfields_options") != 1)
                            {
                                cnn.RollbackTransaction();
                                return JsonResultCommon.Exception(_logger, cnn.LastError, _config, loginData, ControllerContext);
                            }
                        }
                        else
                        {
                            sqlcond = new SqlConditions();
                            sqlcond.Add("rowid", key.rowid);
                            if (cnn.Update(val, sqlcond, "we_newfields_options") != 1)
                            {
                                cnn.RollbackTransaction();
                                return JsonResultCommon.Exception(_logger, cnn.LastError, _config, loginData, ControllerContext);
                            }
                        }
                    }
                    DataTable dt = cnn.CreateDataTable(s, sqlcond);
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
        /// Update cột ứng với từng project
        /// // 1 - Kéo từ list - sang list (Chung project) thì thay đổi vị trí, 2- kéo từ check list - list, 3 - Kéo từ status - status, 4 - kéo từ list - sang list, 5 - kéo thay đổi vị trí cột
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        [Route("drap-drop-item")]
        [HttpPost]
        public async Task<BaseModel<object>> DragDropItemWork(DragDropModel data)
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
                    SqlConditions sqlcond = new SqlConditions();
                    sqlcond.Add("id_row", data.id_row);

                    //string s = "select  id_project_team, fieldname, CreatedDate, CreatedBy, Disabled, ObjectID, position from we_fields_project_team where (where)";
                    //DataTable dt = cnn.CreateDataTable(s, "(where)", sqlcond);
                    //if (dt == null || dt.Rows.Count == 0)
                    //{
                    //    return JsonResultCommon.KhongTonTai("Công việc");
                    //}
                    string tablename = "we_work";
                    string key = "";
                    bool IsUpdateDirect = false;
                    Hashtable val = new Hashtable();
                    SqlConditions Conds = new SqlConditions();
                    string sql_update = "";
                    switch (data.typedrop)
                    {
                        case 1: // Kéo từ list - sang list (Chung project) thì thay đổi vị trí
                            {
                                IsUpdateDirect = true;
                                int priority_to = 0;
                                int priority_from = 0;
                                long id_parent = 0;

                                DataTable dt_data = cnn.CreateDataTable($@"select id_parent, priority, id_row 
                                                                        from we_work 
                                                                        where id_row={data.id_to} and Disabled = 0");
                                if (dt_data.Rows.Count == 0)
                                {
                                    return JsonResultCommon.KhongCoDuLieu(false);
                                }
                                //id_parent = dt_data.Rows[0]["id_parent"].ToString();
                                //if (string.IsNullOrEmpty(id_parent))
                                //{
                                //    id_parent = dt_data.Rows[0]["id_row"].ToString();
                                //}
                                id_parent = data.id_parent;
                                int.TryParse(dt_data.Rows[0]["priority"].ToString(), out priority_to);
                                if (id_parent == data.id_from)
                                    return JsonResultCommon.KhongCoDuLieu(false);
                                if (data.status_from != data.status_to)
                                    val.Add("status", data.status_to);
                                if (data.IsAbove)
                                {
                                    priority_from = priority_to; // cập nhật vị trí from = vị trí to

                                    string sql_position = "";
                                    sql_position = $@"select priority from we_work 
                                                                    where id_row<{data.id_to} 
                                                                    and priority={priority_to} 
                                                                    and Disabled = 0";
                                    if (id_parent > 0)
                                        sql_position += $" and id_parent={id_parent}";
                                    else
                                        sql_position += " and id_parent is null";
                                    DataTable dt_ = cnn.CreateDataTable(sql_position);
                                    if (cnn.LastError != null)
                                        return JsonResultCommon.Exception(_logger, cnn.LastError, _config, loginData, ControllerContext);
                                    if (dt_.Rows.Count > 0) priority_from = priority_to + 1;
                                    if (id_parent > 0)
                                        val.Add("id_parent", id_parent);
                                    else
                                        val.Add("id_parent", DBNull.Value);
                                    val.Add("UpdatedDate", Common.GetDateTime());
                                    val.Add("UpdatedBy", loginData.UserID);
                                    val.Add("priority", priority_from);
                                    Conds = new SqlConditions();
                                    Conds.Add("id_row", data.id_from);
                                    int result = cnn.Update(val, Conds, "we_work");
                                    if (result <= 0)
                                    {
                                        return JsonResultCommon.ThatBai("Lỗi kéo vị trí ở trên");
                                    }
                                    sql_update = $"update we_work set priority=priority+{(priority_to == priority_from ? 1 : 2)} " +
                                    $"where (priority>={priority_from}) and id_row <> {data.id_from} and id_row <> {data.id_to} and Disabled = 0 ";
                                    if (id_parent > 0)
                                        sql_update += $" and id_parent = {id_parent}";
                                    cnn.ExecuteNonQuery(sql_update);
                                }
                                else
                                {
                                    priority_from = priority_to + 1;
                                    if (id_parent > 0)
                                        val.Add("id_parent", id_parent);
                                    else
                                        val.Add("id_parent", DBNull.Value);
                                    val.Add("UpdatedDate", Common.GetDateTime());
                                    val.Add("UpdatedBy", loginData.UserID);
                                    val.Add("priority", priority_from);
                                    Conds = new SqlConditions();
                                    Conds.Add("id_row", data.id_from);
                                    int result = cnn.Update(val, Conds, "we_work");
                                    if (result <= 0)
                                    {
                                        return JsonResultCommon.ThatBai("Lỗi kéo vị trí ở dưới");
                                    }
                                    sql_update = $"update we_work set priority=priority+1 where priority>{priority_to} and id_row>{data.id_to} and id_row <> {data.id_from} and Disabled = 0 and id_parent={dt_data.Rows[0]["id_parent"]}";
                                    cnn.ExecuteNonQuery(sql_update);
                                    //  cnn.ExecuteNonQuery($"update we_work set priority=priority+2 where priority={priority_to} and id_row>{data.id_to} and id_row <> {data.id_from} and Disabled = 0 and id_parent={dt_data.Rows[0]["id_parent"]}");
                                }
                            }
                            break;
                        case 2: // Chuyển task -> subtask
                            {
                                IsUpdateDirect = false;
                                val = new Hashtable();
                                val.Add("id_parent", data.id_to);
                                val.Add("UpdatedDate", Common.GetDateTime());
                                val.Add("UpdatedBy", loginData.UserID);
                                val.Add("priority", data.priority_from);
                                sqlcond = new SqlConditions();
                                sqlcond.Add("id_row", data.id_from);
                                tablename = "we_work";
                            }
                            break;
                        case 3:
                            {
                                IsUpdateDirect = false;
                                val = new Hashtable();
                                val.Add("UpdatedDate", Common.GetDateTime());
                                val.Add("UpdatedBy", loginData.UserID);
                                val.Add("status", data.status_to);
                                sqlcond = new SqlConditions();
                                sqlcond.Add("id_row", data.id_from);
                                tablename = "we_work";
                            }
                            break;
                        //case 4:
                        //    {

                        //    }
                        //    break;
                        case 5:
                            {

                            }
                            break;
                    }
                    if (!IsUpdateDirect)
                    {
                        if (cnn.Update(val, sqlcond, tablename) != 1)
                        {
                            cnn.RollbackTransaction();
                            return JsonResultCommon.Exception(_logger, cnn.LastError, _config, loginData, ControllerContext);
                        }
                    }
                    cnn.EndTransaction();
                    return JsonResultCommon.ThanhCong(data);
                    //sqlcond.Add("fieldname", data.columnname);
                    val.Add("disabled", 0);
                    val.Add("UpdatedDate", Common.GetDateTime());
                    val.Add("UpdatedBy", iduser);
                    //val.Add("fieldname", data.columnname);
                    val.Add("id_project_team", data.id_project_team);
                    val.Add("ObjectID", 1);
                    double so = 0;
                    object position = cnn.ExecuteScalar("select Max(position) from we_fields_project_team where id_project_team =" + data.id_project_team + "").ToString();
                    if (position == null)
                        position = so;
                    so = double.Parse(position.ToString());
                    val.Add("position", so + 1);
                    cnn.BeginTransaction();

                }
            }
            catch (Exception ex)
            {
                return JsonResultCommon.Exception(_logger, ex, _config, loginData);
            }
        }
        /// <summary>
        /// Update nhanh các trường thông tin trong 1 công việc
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        [Route("Update-by-key")]
        [HttpPost]
        public async Task<BaseModel<object>> UpdateByKey(UpdateWorkModel data)
        {
            UserJWT loginData = Ulities.GetUserByHeader(HttpContext.Request.Headers);
            if (loginData == null)
                return JsonResultCommon.DangNhap();
            try
            {
                int id_log_action = data.id_log_action;
                string log_content = "";
                bool is_delete_assign = false;
                string ConnectionString = JeeWorkLiteController.getConnectionString(ConnectionCache, loginData.CustomerID, _configuration);
                using (DpsConnection cnn = new DpsConnection(ConnectionString))
                {
                    #region Lấy dữ liệu account từ JeeAccount
                    DataAccount = JeeWorkLiteController.GetAccountFromJeeAccount(HttpContext.Request.Headers, _configuration);
                    if (DataAccount == null)
                        return JsonResultCommon.Custom("Lỗi lấy danh sách nhân viên từ hệ thống quản lý tài khoản");
                    #endregion
                    SqlConditions sqlcond = new SqlConditions();
                    sqlcond.Add("id_row", data.id_row);
                    sqlcond.Add("disabled", 0);
                    string s = "select title as tencongviec_old, * from v_wework_new where (where)";
                    DataTable old = cnn.CreateDataTable(s, "(where)", sqlcond);
                    if (old == null || old.Rows.Count == 0)
                        return JsonResultCommon.KhongTonTai("Công việc");
                    SqlConditions cond_status = new SqlConditions();
                    long iduser = loginData.UserID;
                    long idk = loginData.CustomerID;
                    string workname = "";
                    long id_project_team = 0;
                    long StatusPresent = 0;
                    Post_Automation_Model postauto = new Post_Automation_Model();
                    postauto.taskid = data.id_row; // id task
                    postauto.userid = loginData.UserID;
                    postauto.listid = long.Parse(old.Rows[0]["id_project_team"].ToString()); // id project team
                    postauto.departmentid = long.Parse(old.Rows[0]["id_department"].ToString()); // id_department
                    postauto.customerid = loginData.CustomerID;
                    Common comm = new Common(ConnectionString);
                    if (Common.IsProjectClosed(old.Rows[0]["id_project_team"].ToString(), cnn))
                    {
                        return JsonResultCommon.Custom("Dự án đã đóng không thể cập nhật");
                    }
                    if (Common.IsTaskClosed(data.id_row, cnn))
                    {
                        return JsonResultCommon.Custom("Công việc đã đóng không thể cập nhật");
                    }
                    if (data.id_role > 0)
                    {
                        DataTable dt_role = new DataTable();
                        string sql_role = "select * from we_role where disabled = 0 and id_row = " + data.id_role;
                        dt_role = cnn.CreateDataTable(sql_role);
                        if (dt_role.Rows.Count > 0)
                        {
                            bool rs = Common.CheckIsUpdatedTask(old.Rows[0]["id_project_team"].ToString(), data.id_role, loginData, cnn, ConnectionString);
                            if (!rs)
                            {
                                return JsonResultCommon.Custom("Bạn không có quyền " + dt_role.Rows[0]["title"].ToString());
                            }
                        }
                    }
                    bool iscomplete = false;
                    DataTable dt_infowork = cnn.CreateDataTable("select title, id_project_team, status, start_date, deadline  " +
                        "from we_work " +
                        "where id_row = @id_row", new SqlConditions() { { "id_row", data.id_row } });
                    if (dt_infowork.Rows.Count > 0)
                    {
                        workname = dt_infowork.Rows[0]["title"].ToString();
                        id_project_team = long.Parse(dt_infowork.Rows[0]["id_project_team"].ToString());
                        StatusPresent = long.Parse(dt_infowork.Rows[0]["status"].ToString());
                    }
                    DataTable dt_user1 = cnn.CreateDataTable("select distinct id_nv, title, id_row from v_wework_new where id_nv is not null and (where)", "(where)", sqlcond);
                    List<long> danhsachU = new List<long>();
                    if (cnn.LastError is null && dt_user1.Rows.Count > 0)
                    {
                        danhsachU = dt_user1.AsEnumerable().Select(x => long.Parse(x["id_nv"].ToString())).ToList();
                    }
                    string sql_status = "select id_row, isdeadline, istodo, isfinal, isdefault " +
                    "from we_status where disabled = 0 and id_project_team = @id_project_team";
                    DataTable dt_StatusID = new DataTable();
                    dt_StatusID = cnn.CreateDataTable(sql_status, new SqlConditions() { { "id_project_team", id_project_team.ToString() } });
                    int templateguimail = 0;
                    string TitleLanguageKey = "", key_special = "";
                    bool is_assign = false;
                    long loai = 1;
                    Hashtable has_replace = new Hashtable();
                    SendNotifyModel noti = new SendNotifyModel();
                    var users_loai1 = JeeWorkLiteController.GetUserSendNotify(loginData, data.id_row, id_log_action, 1, ConnectionString, DataAccount, cnn);
                    var users_loai2 = JeeWorkLiteController.GetUserSendNotify(loginData, data.id_row, id_log_action, 2, ConnectionString, DataAccount, cnn);
                    if (data.key != "Tags" && data.key != "Attachments" && data.key != "Attachments_result" && data.key != "assign" && data.key != "follower")
                    {
                        Hashtable val = new Hashtable();
                        val.Add("updateddate", Common.GetDateTime());
                        val.Add("updatedby", iduser);
                        cnn.BeginTransaction();
                        if (data.value != null)
                        {
                            // Xử lý riêng cho update status
                            if ("status".Equals(data.key))
                            {
                                bool isTodo = long.Parse(cnn.ExecuteScalar("select count(*) from we_status where id_row = " + data.value + " and isTodo = 1").ToString()) > 0;
                                bool isFinal = long.Parse(cnn.ExecuteScalar("select count(*) from we_status where id_row = " + data.value + " and IsFinal = 1").ToString()) > 0;
                                string StatusID = "";
                                if ("complete".Equals(data.status_type))
                                {
                                    sql_status += " and isfinal = 1 ";
                                    StatusID = cnn.ExecuteScalar(sql_status, new SqlConditions() { { "id_project_team", id_project_team.ToString() } }).ToString();
                                    if (StatusID != null)
                                    {
                                        data.value = StatusID;
                                        val.Add("end_date", Common.GetDateTime());
                                        val.Add("closed_date", Common.GetDateTime()); // date update isFilnal = 1
                                        val.Add("closed_by", loginData.UserID); // user update isFilnal = 1
                                        data.value = StatusID;
                                        iscomplete = true;
                                        if (StatusID.Equals(StatusPresent.ToString()))
                                            return JsonResultCommon.ThanhCong(data);
                                    }
                                }
                                else // Xử lý trường hợp người dùng next status
                                {
                                    if ("next".Equals(data.status_type)) // Lấy status tiếp theo
                                    {
                                        DataTable dt_final = new DataTable();
                                        sql_status += " and Position > (select Position from we_status where disabled = 0 and id_project_team = @id_project_team and id_row = " + data.value + ") order by IsFinal,id_row";
                                        dt_StatusID = cnn.CreateDataTable(sql_status, new SqlConditions() { { "id_project_team", id_project_team.ToString() } });
                                        if (dt_StatusID.Rows.Count > 0)
                                            data.value = dt_StatusID.Rows[0][0].ToString();
                                        else
                                        {
                                            string status_final = "select id_row from we_status where disabled = 0 and id_project_team = @id_project_team and isfinal = 1 order by Position";
                                            SqlConditions conds = new SqlConditions();
                                            conds.Add("id_project_team", dt_infowork.Rows[0]["id_project_team"].ToString());
                                            dt_final = cnn.CreateDataTable(status_final, conds);
                                            if (dt_final.Rows.Count > 0)
                                            {
                                                StatusID = dt_final.Rows[0]["id_row"].ToString();
                                                data.value = StatusID;
                                                if (StatusID.Equals(StatusPresent.ToString()))
                                                    return JsonResultCommon.ThanhCong(data);
                                            }
                                            else
                                                StatusID = "0";
                                        }
                                        if (!string.IsNullOrEmpty(StatusID))
                                        {
                                            data.value = StatusID;
                                            if (long.Parse(cnn.ExecuteScalar("select count(*) from we_status where id_row = " + StatusID + " and IsFinal = 1").ToString()) > 0)
                                            {
                                                val.Add("end_date", Common.GetDateTime());
                                                //val.Add("closed_date", Common.GetDateTime()); // date update isFilnal = 1
                                                //val.Add("closed_by", loginData.UserID); // user update isFilnal = 1
                                                iscomplete = true;
                                            }
                                            else
                                            {
                                                val.Add("end_date", DBNull.Value);
                                                if (long.Parse(cnn.ExecuteScalar("select count(*) from we_status where id_row = " + StatusID + " and isTodo = 1").ToString()) > 0)
                                                {
                                                    val.Add("start_date", Common.GetDateTime()); // start_date isTodo = 1
                                                    val.Add("activated_date", Common.GetDateTime()); // date update isTodo = 1
                                                    val.Add("activated_by", loginData.UserID); // user update isTodo = 1
                                                }
                                            }
                                        }
                                    }
                                    else
                                    {
                                        if (isFinal)
                                        {
                                            val.Add("end_date", Common.GetDateTime());
                                            iscomplete = true;
                                            //val.Add("closed_date", Common.GetDateTime()); // date update isFilnal = 1
                                            //val.Add("closed_by", loginData.UserID); // user update isFilnal = 1
                                        }
                                        else
                                        {
                                            val.Add("end_date", DBNull.Value);
                                            if (isTodo)
                                            {
                                                val.Add("start_date", Common.GetDateTime()); // start_date isTodo = 1
                                                val.Add("activated_date", Common.GetDateTime()); // date update isTodo = 1
                                                val.Add("activated_by", loginData.UserID); // user update isTodo = 1
                                            }
                                        }
                                    }
                                }
                                val.Add("state_change_date", Common.GetDateTime()); // Ngày thay đổi trạng thái (Bất kỳ cập nhật trạng thái là thay đổi)
                            }
                            if (("deadline".Equals(data.key) || "start_date".Equals(data.key)))
                            {
                                DateTime values = Common.GetDateTime();
                                if (DateTime.TryParse(data.value.ToString(), out values))
                                {
                                    if ("deadline".Equals(data.key))
                                    {
                                        DateTime dt1 = values;
                                        DateTime dt_utc = values.ToUniversalTime();
                                        if (!string.IsNullOrEmpty(dt_infowork.Rows[0]["start_date"].ToString()))
                                        {
                                            if (values < DateTime.Parse(dt_infowork.Rows[0]["start_date"].ToString()))
                                            {
                                                return JsonResultCommon.Custom("Hạn chót phải lớn hơn ngày bắt đầu");
                                            }
                                        }
                                        #region kiểm tra công việc hoàn thành hay chưa
                                        DataTable dtstt = cnn.CreateDataTable(@"select * from we_status s 
                                                                            join we_work w on s.id_row = w.status
                                                                            where w.id_row = " + data.id_row + " " +
                                                                                "and (s.isfinal = 1 or end_date is not null)");
                                        if (dtstt.Rows.Count == 0)
                                        {
                                            #region Kiểm tra, cập nhật trạng thái tương ứng
                                            DateTime deadline = Common.GetDateTime();
                                            if (DateTime.TryParse(data.value.ToString(), out deadline))
                                            {
                                                string bieuthuc = "-";
                                                if (deadline > Common.GetDateTime())
                                                {
                                                    val.Add("islate", DBNull.Value);
                                                }
                                                else
                                                {
                                                    val.Add("islate", 1);
                                                    bieuthuc = "+";
                                                }
                                                foreach (long idUser in danhsachU)
                                                {
                                                    NhacNho.UpdateSoluongCVHetHan(idUser, loginData.CustomerID, bieuthuc, _configuration, _producer);
                                                }
                                            }
                                            #endregion
                                        }
                                        #endregion
                                    }
                                    else
                                    {
                                        if (!string.IsNullOrEmpty(dt_infowork.Rows[0]["deadline"].ToString()))
                                        {
                                            var end = DateTime.Parse(dt_infowork.Rows[0]["deadline"].ToString());
                                            var start = DateTime.Parse(values.ToString());
                                            if (values > DateTime.Parse(dt_infowork.Rows[0]["deadline"].ToString()))
                                            {
                                                return JsonResultCommon.Custom("Ngày bắt đầu phải nhỏ hơn hạn chót");
                                            }
                                        }
                                    }
                                }
                            }
                            val.Add(data.key, data.value);
                        }
                        else
                            val.Add(data.key, DBNull.Value);
                        if (cnn.Update(val, sqlcond, "we_work") != 1)
                        {
                            cnn.RollbackTransaction();
                            return JsonResultCommon.Exception(_logger, cnn.LastError, _config, loginData, ControllerContext);
                        }
                        if ("status".Equals(data.key) && iscomplete)
                        {
                            DataTable dt_user = cnn.CreateDataTable("select id_nv, title, id_row from v_wework_new where (where) and id_nv is not null", "(where)", sqlcond);
                            if (dt_user.Rows.Count > 0)
                                NhacNho.UpdateQuantityTask_Users(long.Parse(dt_user.Rows[0]["id_nv"].ToString()), loginData.CustomerID, "-", _configuration, _producer);
                        }
                        switch (data.key)
                        {
                            #region Hạn chót
                            case "deadline":
                                TitleLanguageKey = "ww_chinhsuadeadline";
                                key_special = "ww_xoadeadline";
                                if (data.value != null)
                                    templateguimail = 12;
                                else
                                    templateguimail = 33;
                                postauto.eventid = 3;
                                Automation.SendAutomation(postauto, _configuration, _producer);
                                break;
                            #endregion
                            #region Ngày bắt đầu
                            case "start_date":
                                if (data.value != null)
                                {
                                    templateguimail = 27;
                                    TitleLanguageKey = "ww_chinhsuathoigianbatdau";
                                    postauto.eventid = 4;
                                    Automation.SendAutomation(postauto, _configuration, _producer);
                                }
                                else
                                {
                                    templateguimail = 32;
                                    TitleLanguageKey = "ww_xoathoigianbatdau";
                                }
                                break;
                            #endregion
                            #region Mô tả
                            case "description":
                                templateguimail = 28;
                                TitleLanguageKey = "ww_chinhsuamota";
                                break;
                            #endregion
                            #region Mô tả
                            case "id_group":
                                templateguimail = 0;
                                TitleLanguageKey = "work_dichuyendennhom";
                                break;
                            #endregion
                            #region Tên công việc
                            case "title":
                                templateguimail = 11;
                                TitleLanguageKey = "ww_chinhsuacongviec";
                                break;
                            #endregion
                            #region Trạng thái
                            case "status":
                                templateguimail = 21;
                                TitleLanguageKey = "ww_capnhattrangthaicongviec";
                                DataTable dts = cnn.CreateDataTable("select * from we_status where id_row = " + data.value);
                                if (dts.Rows.Count > 0)
                                {
                                    key_special = dts.Rows[0]["StatusName"].ToString();
                                }
                                postauto.eventid = 1;
                                if (string.IsNullOrEmpty(old.Rows[0]["status"].ToString()))
                                {
                                    postauto.data_input = "any";
                                }
                                else
                                {
                                    postauto.data_input = old.Rows[0]["status"].ToString(); // giá trị cũ
                                }
                                postauto.data_input += "," + data.value; // giá trị mới
                                                                         //postauto.data_input = datapost;
                                Automation.SendAutomation(postauto, _configuration, _producer);
                                break;
                            #endregion
                            #region Kết quả công việc
                            case "result":
                                templateguimail = 31;
                                TitleLanguageKey = "ww_capnhatketquacongviec";
                                break;
                            #endregion
                            #region Thời gian dự kiến
                            case "estimates":
                                templateguimail = 26;
                                TitleLanguageKey = "ww_capnhatthoigianuoctinh";
                                break;
                            #endregion
                            #region Độ ưu tiên
                            case "clickup_prioritize":
                                templateguimail = 25;
                                TitleLanguageKey = "ww_thaydoidouutiencongviec";
                                postauto.eventid = 2;
                                if (string.IsNullOrEmpty(old.Rows[0]["clickup_prioritize"].ToString()))
                                {
                                    postauto.data_input = "any"; // giá trị cũ
                                }
                                else
                                {
                                    postauto.data_input = old.Rows[0]["clickup_prioritize"].ToString(); // giá trị cũ
                                }
                                postauto.data_input += "," + data.value; // giá trị mới
                                                                         //postauto.data_input = datapost;
                                Automation.SendAutomation(postauto, _configuration, _producer);
                                break;
                                #endregion
                        }
                    }
                    else
                    {
                        if ("assign".Equals(data.key) || "follower".Equals(data.key))//assign , follower cho 1 người mới hoặc xóa
                        {
                            is_assign = true;
                            if ("follower".Equals(data.key))
                            {
                                loai = 2;
                                templateguimail = 24;
                                TitleLanguageKey = "ww_xoafollower";
                                id_log_action = 57;
                            }
                            if ("assign".Equals(data.key))
                            {
                                loai = 1;
                                templateguimail = 22;
                                TitleLanguageKey = "ww_xoaassign";
                                postauto.eventid = 5;
                            }
                            if (data.value != null)
                            {
                                postauto.eventid = 6;
                                SqlConditions cond_user = new SqlConditions();
                                cond_user.Add("id_work", data.id_row);
                                cond_user.Add("loai", loai);
                                cond_user.Add("disabled", 0);
                                string sql_user = "";
                                sql_user = "select id_user from we_work_user where loai = @loai and disabled = @disabled and id_work=@id_work";
                                if (loai > 1)
                                {
                                    cond_user.Add("id_user", data.value);
                                    sql_user += " and id_user = @id_user";
                                }
                                object _user = cnn.ExecuteScalar(sql_user, cond_user);
                                if (_user != null)
                                {
                                    Hashtable val = new Hashtable();
                                    val["UpdatedDate"] = Common.GetDateTime();
                                    val["UpdatedBy"] = iduser;
                                    val["disabled"] = 1;
                                    if (cnn.Update(val, cond_user, "we_work_user") != 1)
                                    {
                                        cnn.RollbackTransaction();
                                        return JsonResultCommon.Exception(_logger, cnn.LastError, _config, loginData, ControllerContext);
                                    }
                                    if (loai == 1) // loai = 1 assign đếm lại nhắc nhở xóa người -1
                                    {
                                        NhacNho.UpdateQuantityTask_Users(long.Parse(data.value.ToString()), loginData.CustomerID, "-", _configuration, _producer);
                                    }
                                    is_delete_assign = true;
                                    #region Lấy thông tin để thông báo
                                    noti = JeeWorkLiteController.GetInfoNotify(templateguimail, ConnectionString);
                                    #endregion
                                    if (users_loai1.Count > 0)
                                    {
                                        NotifyModel notify_model = new NotifyModel();
                                        has_replace = new Hashtable();
                                        has_replace.Add("nguoigui", loginData.Username);
                                        has_replace.Add("tencongviec", workname);
                                        notify_model.AppCode = "WORK";
                                        notify_model.From_IDNV = loginData.UserID.ToString();
                                        notify_model.TitleLanguageKey = LocalizationUtility.GetBackendMessage(TitleLanguageKey, "", "vi");
                                        notify_model.TitleLanguageKey = notify_model.TitleLanguageKey.Replace("$nguoigui$", loginData.customdata.personalInfo.Fullname)
                                            .Replace("$tencongviec$", workname);
                                        notify_model.ReplaceData = has_replace;
                                        notify_model.To_Link_MobileApp = noti.link_mobileapp.Replace("$id$", data.id_row.ToString());
                                        notify_model.To_Link_WebApp = noti.link.Replace("$id$", data.id_row.ToString());
                                        for (int i = 0; i < users_loai1.Count; i++)
                                        {
                                            notify_model.To_IDNV = users_loai1[i].ToString();
                                            var info = DataAccount.Where(x => notify_model.To_IDNV.ToString().Contains(x.UserId.ToString())).FirstOrDefault();
                                            if (info is not null)
                                            {
                                                bool kq_noti = JeeWorkLiteController.SendNotify(loginData, info.Username, notify_model, _notifier, _configuration);
                                            }
                                        }
                                    }
                                    JeeWorkLiteController.SendEmail(data.id_row, users_loai2, templateguimail, loginData, ConnectionString, _notifier, _configuration, old, Convert.ToInt32(_user.ToString()));
                                }
                                if (_user == null || !_user.ToString().Equals(data.value.ToString())) // thêm người mới và trường hợp hợp người mới từ người cũ đã xóa
                                {
                                    is_delete_assign = false;
                                    Hashtable val = new Hashtable();
                                    val["id_work"] = data.id_row;
                                    val["createddate"] = Common.GetDateTime();
                                    val["createdby"] = iduser;
                                    if (string.IsNullOrEmpty(data.value.ToString()))
                                        val["id_user"] = DBNull.Value;
                                    else
                                        val["id_user"] = data.value;
                                    val["loai"] = loai;
                                    if (cnn.Insert(val, "we_work_user") != 1)
                                    {
                                        cnn.RollbackTransaction();
                                        return JsonResultCommon.Exception(_logger, cnn.LastError, _config, loginData, ControllerContext);
                                    }
                                    users_loai1 = JeeWorkLiteController.GetUserSendNotify(loginData, data.id_row, id_log_action, 1, ConnectionString, DataAccount, cnn);
                                    users_loai2 = JeeWorkLiteController.GetUserSendNotify(loginData, data.id_row, id_log_action, 2, ConnectionString, DataAccount, cnn);
                                    if (loai == 1) // loai = 1 assign đếm lại nhắc nhở thêm người +1
                                    {
                                        NhacNho.UpdateQuantityTask_Users(long.Parse(data.value.ToString()), loginData.CustomerID, "+", _configuration, _producer);
                                    }
                                    #region gửi event post automation - giao việc
                                    postauto.data_input += data.value;
                                    #endregion
                                    if ("assign".Equals(data.key))
                                    {
                                        templateguimail = 10;
                                        TitleLanguageKey = "ww_assign";
                                        Automation.SendAutomation(postauto, _configuration, _producer);
                                    }
                                    else
                                    {
                                        templateguimail = 23;
                                        TitleLanguageKey = "ww_follower";
                                    }
                                }
                            }
                        }
                        if ("Tags".Equals(data.key))//thêm 1 tag mới
                        {
                            var f = cnn.ExecuteScalar("select count(*) from we_work_tag where disabled=0 and id_work=" + data.id_row + " and id_tag=" + data.value);
                            Hashtable val2 = new Hashtable();
                            if (int.Parse(f.ToString()) > 0) // Tag đã có => Delete
                            {
                                val2 = new Hashtable();
                                val2["UpdatedDate"] = Common.GetDateTime();
                                val2["UpdatedBy"] = iduser;
                                val2["Disabled"] = 1;
                                SqlConditions cond = new SqlConditions();
                                cond.Add("id_work", data.id_row);
                                cond.Add("id_tag", data.value);
                                if (cnn.Update(val2, cond, "we_work_tag") <= 0)
                                {
                                    cnn.RollbackTransaction();
                                    return JsonResultCommon.Exception(_logger, cnn.LastError, _config, loginData, ControllerContext);
                                }
                            }
                            else
                            {
                                val2 = new Hashtable();
                                val2["id_work"] = data.id_row;
                                val2["CreatedDate"] = Common.GetDateTime();
                                val2["CreatedBy"] = iduser;
                                val2["id_tag"] = data.value;
                                if (cnn.Insert(val2, "we_work_tag") != 1)
                                {
                                    cnn.RollbackTransaction();
                                    return JsonResultCommon.Exception(_logger, cnn.LastError, _config, loginData, ControllerContext);
                                }
                            }
                            templateguimail = 34;
                            TitleLanguageKey = "ww_capnhattag";
                        }
                        if ("Attachments".Equals(data.key) || "Attachments_result".Equals(data.key))//upload files mới
                        {
                            int object_type = data.key == "Attachments" ? 1 : 11;
                            List<FileUploadModel> Attachments = (from r in data.values select Newtonsoft.Json.JsonConvert.DeserializeObject<FileUploadModel>(r.ToString())).ToList();
                            if (Attachments != null)
                            {
                                foreach (var item in Attachments)
                                {
                                    log_content += item.filename + ";";
                                    var temp = new AttachmentModel()
                                    {
                                        item = item,
                                        object_type = object_type,
                                        object_id = data.id_row,
                                        id_user = loginData.UserID
                                    };
                                    if (!AttachmentController.upload(temp, cnn, _hostingEnvironment.ContentRootPath, _configuration))
                                    {
                                        cnn.RollbackTransaction();
                                        return JsonResultCommon.Exception(_logger, cnn.LastError, _config, loginData, ControllerContext);
                                    }
                                }
                            }
                            DataTable dt_user = cnn.CreateDataTable("select id_nv, title, id_row from v_wework_new where (where) and id_nv is not null", "(where)", sqlcond);
                            if (dt_user.Rows.Count > 0)
                            {
                                var users = new List<long> { long.Parse(dt_user.Rows[0]["id_nv"].ToString()) };
                                cnn.EndTransaction();
                                if ("Attachments".Equals(data.key))
                                {
                                    //idtemplateguimail = 29;
                                    templateguimail = 29;
                                    TitleLanguageKey = "ww_themtailieucongviec";
                                }
                                else
                                {
                                    templateguimail = 34;
                                    TitleLanguageKey = "ww_themtailieucapnhatketquacongviec";
                                    //idtemplateguimail = 30;
                                }
                            }
                        }
                    }
                    if (!is_delete_assign)
                    {
                        #region Lấy thông tin để thông báo
                        if (templateguimail > 0)
                            noti = JeeWorkLiteController.GetInfoNotify(templateguimail, ConnectionString);
                        #endregion
                        SqlConditions cond_user = new SqlConditions();
                        cond_user.Add("id_work", data.id_row);
                        cond_user.Add("disabled", 0);
                        cond_user.Add("loai", loai);
                        string sql_user = "";
                        sql_user = "select id_user from we_work_user where loai = @loai and disabled = @disabled and id_work=@id_work";
                        if (loai > 1)
                        {
                            cond_user.Add("id_user", data.value);
                            sql_user += " and id_user = @id_user";
                        }
                        object _user = cnn.ExecuteScalar(sql_user, cond_user);
                        if (_user == null)
                            _user = "0";
                        string user_assign = " cho bạn";
                        if (users_loai1.Count > 0)
                        {
                            NotifyModel notify_model = new NotifyModel();
                            has_replace = new Hashtable();
                            has_replace.Add("nguoigui", loginData.Username);
                            has_replace.Add("tencongviec", workname);
                            notify_model.AppCode = "WORK";
                            if (is_assign)
                            {
                                notify_model.From_IDNV = _user.ToString();
                            }
                            else
                            {
                                notify_model.From_IDNV = loginData.UserID.ToString();
                            }
                            notify_model.TitleLanguageKey = LocalizationUtility.GetBackendMessage(TitleLanguageKey, "", "vi");
                            if (data.value == null)
                            {
                                notify_model.TitleLanguageKey = LocalizationUtility.GetBackendMessage(key_special, "", "vi");
                                data.value = "";
                            }
                            if ("Attachments".Equals(data.key) || "Attachments_result".Equals(data.key))//upload files mới
                            {
                                notify_model.TitleLanguageKey = LocalizationUtility.GetBackendMessage(TitleLanguageKey, "", "vi");
                            }
                            notify_model.TitleLanguageKey = notify_model.TitleLanguageKey.Replace("$nguoigui$", loginData.customdata.personalInfo.Fullname);
                            notify_model.TitleLanguageKey = notify_model.TitleLanguageKey.Replace("$tencongviec$", workname);
                            notify_model.TitleLanguageKey = notify_model.TitleLanguageKey.Replace("$value$", data.value.ToString());
                            if ("status".Equals(data.key))
                                notify_model.TitleLanguageKey = notify_model.TitleLanguageKey.Replace("$trangthai$", key_special);
                            notify_model.ReplaceData = has_replace;
                            if (templateguimail == 0 && "id_group".Equals(data.key))
                            {
                                noti.link_mobileapp = "CongViecCaNhan/ChiTietCVCaNhan/$id$";
                                noti.link = "/tasks(auxName:aux/detail/$id$)";
                            }
                            notify_model.To_Link_MobileApp = noti.link_mobileapp.Replace("$id$", data.id_row.ToString());
                            notify_model.To_Link_WebApp = noti.link.Replace("$id$", data.id_row.ToString());
                            for (int i = 0; i < users_loai1.Count; i++)
                            {
                                notify_model.To_IDNV = users_loai1[i].ToString();
                                var info = DataAccount.Where(x => notify_model.To_IDNV.ToString().Contains(x.UserId.ToString())).FirstOrDefault();
                                if (!_user.ToString().Equals(notify_model.To_IDNV))
                                {
                                    user_assign = info.FullName;
                                }
                                notify_model.TitleLanguageKey = notify_model.TitleLanguageKey.Replace("$forme$", user_assign);
                                if (info is not null)
                                {
                                    bool kq_noti = JeeWorkLiteController.SendNotify(loginData, info.Username, notify_model, _notifier, _configuration);
                                }
                            }
                        }
                        if (templateguimail > 0)
                            JeeWorkLiteController.SendEmail(data.id_row, users_loai2, templateguimail, loginData, ConnectionString, _notifier, _configuration, old, Convert.ToInt32(_user.ToString()));
                    }
                    DataTable dt = cnn.CreateDataTable(s, "(where)", sqlcond);
                    if (id_log_action > 0)
                    {
                        bool re = true;
                        if (data.key != "Tags" && data.key != "Attachments" && data.key != "Attachments_result")
                        {
                            string temp = data.key;
                            if (temp == "assign" || temp == "follower")
                                temp = "id_nv";
                            re = JeeWorkLiteController.log(_logger, loginData.Username, cnn, id_log_action, data.id_row, iduser, log_content, old.Rows[0][temp], data.value);
                        }
                        else
                            re = JeeWorkLiteController.log(_logger, loginData.Username, cnn, id_log_action, data.id_row, iduser, log_content, null, data.value);
                        if (!re)
                        {
                            cnn.RollbackTransaction();
                            return JsonResultCommon.Exception(_logger, cnn.LastError, _config, loginData, ControllerContext);
                        }
                    }
                    #region Ghi log trong project
                    //string LogContent = "", LogEditContent = "";
                    //LogEditContent = Common.GetEditLogContent(old, dt);
                    //if (!LogEditContent.Equals(""))
                    //{
                    //    LogEditContent = "Chỉnh sửa dữ liệu (ID: " + data.id_row + ": " + data.key + "=" + data.value + ") : " + LogEditContent;
                    //    LogContent = "Chỉnh sửa dữ liệu công việc (" + data.id_row + "), Chi tiết xem trong log chỉnh sửa chức năng";
                    //}
                    //Common.Ghilogfile(loginData.CustomerID.ToString(), LogEditContent, LogContent, loginData.Username, ControllerContext);
                    #endregion
                    cnn.EndTransaction();
                    return JsonResultCommon.ThanhCong(data);
                }
            }
            catch (Exception ex)
            {
                return JsonResultCommon.Exception(_logger, ex, _config, loginData);
            }
        }
        static string[] Scopes = { CalendarService.Scope.CalendarReadonly };
        ServiceAccountCredential credential;
        /// <summary>
        /// Đồng bộ dữ liệu công việc lên google
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        [Route("google-calender")]
        [HttpPost]
        public async Task<BaseModel<object>> Google_Calender(GoogleCalendarModel data)
        {

            UserJWT loginData = Ulities.GetUserByHeader(HttpContext.Request.Headers);
            if (loginData == null)
                return JsonResultCommon.DangNhap();
            try
            {
                string ConnectionString = JeeWorkLiteController.getConnectionString(ConnectionCache, loginData.CustomerID, _configuration);
                using (DpsConnection cnn = new DpsConnection(ConnectionString))
                {
                    #region Lấy dữ liệu account từ JeeAccount
                    DataAccount = JeeWorkLiteController.GetAccountFromJeeAccount(HttpContext.Request.Headers, _configuration);
                    if (DataAccount == null)
                        return JsonResultCommon.Custom("Lỗi lấy danh sách nhân viên từ hệ thống quản lý tài khoản");
                    #endregion
                    SqlConditions sqlcond = new SqlConditions();
                    sqlcond.Add("id_row", data.id_row);
                    sqlcond.Add("disabled", 0);
                    string s = "select title as tencongviec_old, * from v_wework_new where (where)";
                    DataTable old = cnn.CreateDataTable(s, "(where)", sqlcond);
                    if (old == null || old.Rows.Count == 0)
                        return JsonResultCommon.KhongTonTai("Công việc");
                    SqlConditions cond_status = new SqlConditions();
                    long iduser = loginData.UserID;
                    long idk = loginData.CustomerID;
                    string workname = "";
                    long id_project_team = 0;
                    long StatusPresent = 0;
                    Post_Automation_Model postauto = new Post_Automation_Model();
                    postauto.taskid = data.id_row; // id task
                    postauto.userid = loginData.UserID;
                    postauto.listid = long.Parse(old.Rows[0]["id_project_team"].ToString()); // id project team
                    postauto.departmentid = long.Parse(old.Rows[0]["id_department"].ToString()); // id_department
                    postauto.customerid = loginData.CustomerID;
                    //Data datapost = new Data();
                    DataTable dt_infowork = cnn.CreateDataTable("select title, id_project_team, status, start_date, deadline  " +
                        "from we_work " +
                        "where id_row = @id_row", new SqlConditions() { { "id_row", data.id_row } });
                    if (dt_infowork.Rows.Count > 0)
                    {
                        workname = dt_infowork.Rows[0]["title"].ToString();
                        id_project_team = long.Parse(dt_infowork.Rows[0]["id_project_team"].ToString());
                        StatusPresent = long.Parse(dt_infowork.Rows[0]["status"].ToString());
                    }
                    DataTable dt_user1 = cnn.CreateDataTable("select distinct id_nv, title, id_row from v_wework_new where id_nv is not null and (where)", "(where)", sqlcond);
                    List<long> danhsachU = new List<long>();
                    if (cnn.LastError is null && dt_user1.Rows.Count > 0)
                    {
                        danhsachU = dt_user1.AsEnumerable().Select(x => long.Parse(x["id_nv"].ToString())).ToList();
                    }
                    //CalendarService _service = this.GetCalendarService("PATHTOJSONFILE\\gsuite-migration-123456-a4556s8df.json");
                    //CreateEvent(_service);
                    //GetEvents(_service);
                    var service = new CalendarService(new BaseClientService.Initializer()
                    {
                        HttpClientInitializer = credential,
                        ApplicationName = "Calendar API Sample",
                    });
                    var ev = new Event();
                    EventDateTime start = new EventDateTime();
                    start.DateTime = new DateTime(2019, 3, 11, 10, 0, 0);
                    EventDateTime end = new EventDateTime();
                    end.DateTime = new DateTime(2019, 3, 11, 10, 30, 0);
                    ev.Start = start;
                    ev.End = end;
                    ev.Summary = "New Event";
                    ev.Description = "Description...";
                    var calendarId = "primary";
                    Event recurringEvent = service.Events.Insert(ev, calendarId).Execute();
                    //Console.WriteLine("Event created: %s\n", "");
                    cnn.EndTransaction();
                    return JsonResultCommon.ThanhCong(data);
                }
            }
            catch (Exception ex)
            {
                return JsonResultCommon.Exception(_logger, ex, _config, loginData);
            }
        }
        private CalendarService GetCalendarService(string keyfilepath)
        {
            try
            {
                string[] Scopes = {
   CalendarService.Scope.Calendar,
   CalendarService.Scope.CalendarEvents,
   CalendarService.Scope.CalendarEventsReadonly
  };

                GoogleCredential credential;
                using (var stream = new FileStream(keyfilepath, FileMode.Open, FileAccess.Read))
                {
                    // As we are using admin SDK, we need to still impersonate user who has admin access    
                    //  https://developers.google.com/admin-sdk/directory/v1/guides/delegation    
                    credential = GoogleCredential.FromStream(stream)
                     .CreateScoped(Scopes).CreateWithUser("huytranvan1404@gmail.com");
                }

                // Create Calendar API service.    
                var service = new CalendarService(new BaseClientService.Initializer()
                {
                    HttpClientInitializer = credential,
                    ApplicationName = "Calendar Sample",
                });
                return service;
            }
            catch (Exception ex)
            {
                throw;
            }
        }
        private void CreateEvent(CalendarService _service)
        {
            Event body = new Event();
            EventAttendee a = new EventAttendee();
            a.Email = "huytranvan1404@gmail.com";
            EventAttendee b = new EventAttendee();
            b.Email = "huypaddaica@gmail.com";
            List<EventAttendee> attendes = new List<EventAttendee>();
            attendes.Add(a);
            attendes.Add(b);
            body.Attendees = attendes;
            EventDateTime start = new EventDateTime();
            start.DateTime = Convert.ToDateTime("2019-08-28T09:00:00+0530");
            EventDateTime end = new EventDateTime();
            end.DateTime = Convert.ToDateTime("2019-08-28T11:00:00+0530");
            body.Start = start;
            body.End = end;
            body.Location = "Avengers Mansion";
            body.Summary = "Discussion about new Spidey suit";
            EventsResource.InsertRequest request = new EventsResource.InsertRequest(_service, body, "huytv@dps.com.vn");
            Event response = request.Execute();
        }
        private void GetEvents(CalendarService _service)
        {
            // Define parameters of request.    
            EventsResource.ListRequest request = _service.Events.List("primary");
            request.TimeMin = Common.GetDateTime();
            request.ShowDeleted = false;
            request.SingleEvents = true;
            request.MaxResults = 10;
            request.OrderBy = EventsResource.ListRequest.OrderByEnum.StartTime;

            string eventsValue = "";
            // List events.    
            Events events = request.Execute();
            eventsValue = "Upcoming events:\n";
            if (events.Items != null && events.Items.Count > 0)
            {
                foreach (var eventItem in events.Items)
                {
                    string when = eventItem.Start.DateTime.ToString();
                    if (String.IsNullOrEmpty(when))
                    {
                        when = eventItem.Start.Date;
                    }
                    eventsValue += string.Format("{0} ({1})", eventItem.Summary, when) + "\n";
                }
                //MessageBox.Show(eventsValue);
            }
            else
            {
                //MessageBox.Show("No upcoming events found.");
            }
        }
        /// <summary>
        /// Nhân bản công việc
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        [Route("Duplicate")]
        [HttpPost]
        public async Task<object> Duplicate(WorkDuplicateModel data)
        {

            UserJWT loginData = Ulities.GetUserByHeader(HttpContext.Request.Headers);
            if (loginData == null)
                return JsonResultCommon.DangNhap();
            try
            {
                string strRe = "";
                if (data.id <= 0)
                    strRe += (strRe == "" ? "" : ",") + "công việc";
                if (data.type == 2)
                {
                    if (data.id_project_team == 0)//nhân bản trong dự án/phòng ban
                        strRe += (strRe == "" ? "" : ",") + "dự án/phòng ban";
                }
                else
                {
                    if (string.IsNullOrEmpty(data.title))
                        strRe += (strRe == "" ? "" : ",") + "tiêu đề";
                }
                if (strRe != "")
                {
                    return JsonResultCommon.BatBuoc(strRe);
                }
                string ConnectionString = JeeWorkLiteController.getConnectionString(ConnectionCache, loginData.CustomerID, _configuration);
                using (DpsConnection cnn = new DpsConnection(ConnectionString))
                {
                    long iduser = loginData.UserID;
                    long idk = loginData.CustomerID;
                    Hashtable val = new Hashtable();
                    val.Add("id", data.id);
                    val.Add("type", data.type);
                    val.Add("duplicate_child", data.duplicate_child);
                    val.Add("urgent", data.urgent);
                    val.Add("required_result", data.required_result);
                    if (data.type == 2)
                    {
                        val.Add("title", data.title);
                        val.Add("id_project_team", data.id_project_team);
                    }
                    else
                    {
                        val.Add("title", data.title);
                        if (data.id_parent > 0)//nhân bản subtask làm subtask
                            val.Add("id_parent", data.id_parent);
                    }
                    if (!string.IsNullOrEmpty(data.description))
                        val.Add("description", data.description);
                    if (data.deadline > DateTime.MinValue)
                        val.Add("deadline", data.deadline);
                    if (data.assign > 0)
                        val.Add("assign", data.assign);
                    if (data.start_date > DateTime.MinValue)
                        val.Add("start_date", data.start_date);
                    if (data.followers != null && data.followers.Count > 0)
                        val.Add("followers", string.Join(",", data.followers));
                    val.Add("CreatedDate", Common.GetDateTime());
                    val.Add("CreatedBy", iduser);
                    cnn.BeginTransaction();
                    if (cnn.Insert(val, "we_work_duplicate") != 1)
                    {
                        cnn.RollbackTransaction();
                        return JsonResultCommon.Exception(_logger, cnn.LastError, _config, loginData, ControllerContext);
                    }
                    long idc = long.Parse(cnn.ExecuteScalar("select IDENT_CURRENT('we_work_duplicate')").ToString());
                    string sql = "exec DuplicateWork " + idc;

                    DataTable dt = cnn.CreateDataTable(sql);
                    if (cnn.LastError != null || dt == null || dt.Rows.Count == 0)
                    {
                        cnn.RollbackTransaction();
                        return JsonResultCommon.Exception(_logger, cnn.LastError, _config, loginData, ControllerContext);
                    }
                    // update lại tình trạng cho công việc mới khởi tạo
                    long workID_New = long.Parse(cnn.ExecuteScalar("select max(id_row) from we_work where disabled = 0").ToString());
                    string sqlq = "select ISNULL((select id_row from we_status where disabled=0 and Position = 1 and id_project_team = " + data.id_project_team + "),0)";
                    var statusID = long.Parse(cnn.ExecuteScalar(sqlq).ToString());
                    //Insert người follow cho từng tình trạng của công việc
                    DataTable dt_status = JeeWorkLiteController.StatusDynamic(data.id_project_team, new List<AccUsernameModel>(), cnn);
                    foreach (DataRow dr in dt.Rows)
                    {
                        if (dt_status.Rows.Count > 0)
                        {
                            foreach (DataRow item in dt_status.Rows)
                            {
                                val = new Hashtable();
                                val.Add("id_project_team", data.id_project_team);
                                val.Add("workid", dr["id_row"]);
                                val.Add("statusid", item["id_row"]);
                                if (string.IsNullOrEmpty(item["follower"].ToString()))
                                    val.Add("checker", DBNull.Value);
                                else
                                    val.Add("checker", item["follower"]);
                                val.Add("createddate", Common.GetDateTime());
                                val.Add("createdby", iduser);
                                if (cnn.Insert(val, "we_work_process") != 1)
                                {
                                    cnn.RollbackTransaction();
                                    return JsonResultCommon.Exception(_logger, cnn.LastError, _config, loginData, ControllerContext);
                                }
                                long processid = long.Parse(cnn.ExecuteScalar("select IDENT_CURRENT('we_work_process')").ToString());
                                val = new Hashtable();
                                val.Add("processid", processid);
                                if (string.IsNullOrEmpty(item["follower"].ToString()))
                                {
                                    val.Add("new_checker", DBNull.Value);
                                }
                                else
                                {
                                    val.Add("new_checker", item["follower"]);
                                    var info = DataAccount.Where(x => item["follower"].ToString().Contains(x.UserId.ToString())).FirstOrDefault();
                                    if (info != null)
                                    {
                                        val.Add("content_note", loginData.customdata.personalInfo.Fullname + " thêm " + info.FullName + " vào theo dõi");
                                    }
                                }
                                val.Add("createddate", Common.GetDateTime());
                                val.Add("createdby", iduser);
                                if (cnn.Insert(val, "we_work_process_log") != 1)
                                {
                                    cnn.RollbackTransaction();
                                    return JsonResultCommon.Exception(_logger, cnn.LastError, _config, loginData, ControllerContext);
                                }
                            }
                        }
                        if (data.type == 2)
                        {
                            val = new Hashtable();
                            val.Add("status", statusID);
                            SqlConditions cond = new SqlConditions();
                            cond.Add("id_row", dr["id_row"]);
                            //cnn.BeginTransaction();
                            if (cnn.Update(val, cond, "we_work") <= 0)
                            {
                                cnn.RollbackTransaction();
                                return JsonResultCommon.Exception(_logger, cnn.LastError, _config, loginData, ControllerContext);
                            }
                        }
                    }
                    #region Ghi log trong project
                    string LogContent = "", LogEditContent = "";
                    LogContent = LogEditContent = "Nhân bản công việc: title=" + data.title + ", id_project_team=" + data.id_project_team;
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
                    cnn.EndTransaction();

                    foreach (DataRow dr in dt.Rows)
                    {
                        #region Lấy thông tin để thông báo
                        SendNotifyModel noti = JeeWorkLiteController.GetInfoNotify(10, ConnectionString);
                        #endregion
                        DataTable tblU = cnn.CreateDataTable("select * from we_work_user where disabled = 0 and id_work = " + dr["id_row"]);
                        List<long> dataUser = tblU.AsEnumerable().Select(x => long.Parse(x["id_user"].ToString())).ToList();
                        JeeWorkLiteController.SendEmail(idc, dataUser, 10, loginData, ConnectionString, _notifier, _configuration);
                        #region Notify thêm mới công việc
                        Hashtable has_replace = new Hashtable();
                        for (int i = 0; i < dataUser.Count; i++)
                        {
                            NotifyModel notify_model = new NotifyModel();
                            has_replace = new Hashtable();
                            has_replace.Add("nguoigui", loginData.Username);
                            has_replace.Add("tencongviec", dr["title"]);
                            notify_model.AppCode = "WORK";
                            notify_model.From_IDNV = loginData.UserID.ToString();
                            notify_model.To_IDNV = dataUser[i].ToString();
                            notify_model.TitleLanguageKey = LocalizationUtility.GetBackendMessage("ww_themmoicongviec", "", "vi");
                            notify_model.TitleLanguageKey = notify_model.TitleLanguageKey.Replace("$nguoigui$", loginData.customdata.personalInfo.Fullname);
                            notify_model.TitleLanguageKey = notify_model.TitleLanguageKey.Replace("$tencongviec$", dr["title"].ToString());
                            notify_model.ReplaceData = has_replace;
                            notify_model.To_Link_MobileApp = noti.link_mobileapp.Replace("$id$", dr["id_row"].ToString());
                            notify_model.To_Link_WebApp = noti.link.Replace("$id$", dr["id_row"].ToString());
                            var info = DataAccount.Where(x => notify_model.To_IDNV.ToString().Contains(x.UserId.ToString())).FirstOrDefault();
                            if (info is not null)
                            {
                                bool kq_noti = JeeWorkLiteController.SendNotify(loginData, info.Username, notify_model, _notifier, _configuration);
                            }
                        }
                        #endregion
                    }
                    return JsonResultCommon.ThanhCong(dt.AsEnumerable().FirstOrDefault());
                }
            }
            catch (Exception ex)
            {
                return JsonResultCommon.Exception(_logger, ex, _config, loginData);
            }
        }
        /// <summary>
        /// Xóa công việc
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
                    #region Lấy dữ liệu account từ JeeAccount
                    DataAccount = JeeWorkLiteController.GetAccountFromJeeAccount(HttpContext.Request.Headers, _configuration);
                    if (DataAccount == null)
                        return JsonResultCommon.Custom("Lỗi lấy danh sách nhân viên từ hệ thống quản lý tài khoản");
                    #endregion
                    SqlConditions sqlcondQ = new SqlConditions();
                    sqlcondQ.Add("id_row", id);
                    sqlcondQ.Add("disabled", 0);
                    string s = "select title as tencongviec_old, * from v_wework_new where (where)";
                    DataTable old = cnn.CreateDataTable(s, "(where)", sqlcondQ);
                    if (old == null || old.Rows.Count == 0)
                        return JsonResultCommon.KhongTonTai("Công việc");
                    bool _delete = Common.CheckIsUpdatedTask(old.Rows[0]["id_project_team"].ToString(), 13, loginData, cnn, ConnectionString);
                    if (!_delete)
                        return JsonResultCommon.Custom("Bạn không có quyền xóa công việc");
                    if (Common.IsTaskClosed(id, cnn))
                    {
                        return JsonResultCommon.Custom("Công việc đã đóng không thể xóa công việc");
                    }
                    if (Common.IsProjectClosed(old.Rows[0]["id_project_team"].ToString(), cnn))
                    {
                        return JsonResultCommon.Custom("Dự án đã đóng không thể xóa công việc");
                    }
                    string xoacon = " in (select id_row from v_wework_clickup_new where (id_row = " + id + " or id_parent = " + id + ") and  disabled = 0 ) ";
                    string sqlq = "update we_work set disabled=1, UpdatedDate=GETUTCDATE(), UpdatedBy=" + iduser + " where id_row " + xoacon;
                    SqlConditions sqlcond = new SqlConditions();
                    sqlcond.Add("id_row", id);
                    sqlcond.Add("disabled", 0);
                    DataTable dt_user = cnn.CreateDataTable("select id_nv, title, id_row from v_wework_new where (where) and Id_NV is not null", "(where)", sqlcond);
                    DataTable dt_user1 = cnn.CreateDataTable("select id_nv, title, id_row from v_wework_new where id_nv is not null and (where) and Id_NV is not null", "(where)", sqlcond);
                    // get users để gửi mail thông báo
                    //var users = getUserTask(cnn, id);
                    cnn.BeginTransaction();
                    var users_loai1 = JeeWorkLiteController.GetUserSendNotify(loginData, id, 18, 1, ConnectionString, DataAccount, cnn);
                    var users_loai2 = JeeWorkLiteController.GetUserSendNotify(loginData, id, 18, 2, ConnectionString, DataAccount, cnn);
                    if (cnn.ExecuteNonQuery(sqlq) < 1)
                    {
                        cnn.RollbackTransaction();
                        return JsonResultCommon.Exception(_logger, cnn.LastError, _config, loginData, ControllerContext);
                    }
                    bool rs = JeeWorkLiteController.Delete_TableReference(id, "we_work", loginData, cnn);
                    if (!rs)
                    {
                        cnn.RollbackTransaction();
                        return JsonResultCommon.Exception(_logger, cnn.LastError, _config, loginData, ControllerContext);
                    }
                    if (!JeeWorkLiteController.log(_logger, loginData.Username, cnn, 18, id, iduser))
                    {
                        cnn.RollbackTransaction();
                        return JsonResultCommon.Exception(_logger, cnn.LastError, _config, loginData, ControllerContext);
                    }
                    #region Ghi log trong project
                    string LogContent = "Xóa dữ liệu công việc (" + id + ")";
                    Common.Ghilogfile(loginData.CustomerID.ToString(), "", LogContent, loginData.Username, ControllerContext);
                    #endregion
                    cnn.EndTransaction();
                    #region Lấy thông tin để thông báo
                    int templateguimail = 15;
                    SendNotifyModel noti = new SendNotifyModel();
                    noti = JeeWorkLiteController.GetInfoNotify(templateguimail, ConnectionString);
                    string workname = old.Rows[0]["tencongviec_old"].ToString();
                    string TitleLanguageKey = "ww_xoacongviec";
                    #endregion
                    string user_assign = "0";
                    if (users_loai1.Count > 0)
                    {
                        NotifyModel notify_model = new NotifyModel();
                        Hashtable has_replace = new Hashtable();
                        has_replace.Add("nguoigui", loginData.Username);
                        has_replace.Add("tencongviec", workname);
                        notify_model.AppCode = "WORK";
                        if (!string.IsNullOrEmpty(old.Rows[0]["id_nv"].ToString()))
                        {
                            user_assign = old.Rows[0]["id_nv"].ToString();
                        }
                        notify_model.From_IDNV = user_assign;
                        //notify_model.From_IDNV = loginData.UserID.ToString();
                        notify_model.TitleLanguageKey = LocalizationUtility.GetBackendMessage(TitleLanguageKey, "", "vi");
                        notify_model.TitleLanguageKey = notify_model.TitleLanguageKey.Replace("$nguoigui$", loginData.customdata.personalInfo.Fullname)
                            .Replace("$tencongviec$", workname);
                        notify_model.ReplaceData = has_replace;
                        notify_model.To_Link_MobileApp = noti.link_mobileapp.Replace("$id$", id.ToString());
                        notify_model.To_Link_WebApp = noti.link.Replace("$id$", id.ToString());
                        for (int i = 0; i < users_loai1.Count; i++)
                        {
                            notify_model.To_IDNV = users_loai1[i].ToString();
                            var info = DataAccount.Where(x => notify_model.To_IDNV.ToString().Contains(x.UserId.ToString())).FirstOrDefault();
                            if (info is not null)
                            {
                                bool kq_noti = JeeWorkLiteController.SendNotify(loginData, info.Username, notify_model, _notifier, _configuration);
                            }
                        }
                    }
                    JeeWorkLiteController.SendEmail(id, users_loai2, templateguimail, loginData, ConnectionString, _notifier, _configuration, old, long.Parse(user_assign));
                    //#region Check dự án đó có gửi gửi mail khi xóa không
                    //long id_project = long.Parse(cnn.ExecuteScalar("select id_project_team from we_work where id_row = " + id).ToString());
                    //if (JeeWorkLiteController.CheckNotify_ByConditions(id_project, "email_delete_work", false, ConnectionString))
                    //{
                    //    JeeWorkLiteController.SendEmail(id, users, 15, loginData, ConnectionString, _notifier, _configuration);
                    //    if (users.Count > 0) // dt_user.Rows.Count > 0
                    //    {
                    //        // var users = new List<long> { long.Parse(dt_user.Rows[0]["id_nv"].ToString()) };
                    //        // JeeWorkLiteController.SendEmail(id, users, 15, loginData, ConnectionString, _notifier, _configuration);
                    //        #region Lấy thông tin để thông báo
                    //        SendNotifyModel noti = JeeWorkLiteController.GetInfoNotify(15, ConnectionString);
                    //        #endregion
                    //        object workname = cnn.ExecuteScalar("select title from we_work where disabled = 1 and id_row = @id_row", new SqlConditions() { { "id_row", id } });
                    //        if (workname != null)
                    //            workname = workname.ToString();
                    //        #region Notify assign
                    //        Hashtable has_replace = new Hashtable();
                    //        for (int i = 0; i < users.Count; i++)
                    //        {
                    //            NotifyModel notify_model = new NotifyModel();
                    //            has_replace = new Hashtable();
                    //            has_replace.Add("nguoigui", loginData.Username);
                    //            has_replace.Add("tencongviec", workname);
                    //            notify_model.AppCode = "WORK";
                    //            notify_model.From_IDNV = loginData.UserID.ToString();
                    //            notify_model.To_IDNV = users[i].ToString();
                    //            notify_model.TitleLanguageKey = LocalizationUtility.GetBackendMessage("ww_xoacongviec", "", "vi");
                    //            notify_model.TitleLanguageKey = notify_model.TitleLanguageKey.Replace("$nguoigui$", loginData.customdata.personalInfo.Fullname);
                    //            notify_model.TitleLanguageKey = notify_model.TitleLanguageKey.Replace("$tencongviec$", workname.ToString());
                    //            notify_model.ReplaceData = has_replace;
                    //            notify_model.To_Link_MobileApp = noti.link_mobileapp.Replace("$id$", id.ToString());
                    //            notify_model.To_Link_WebApp = noti.link.Replace("$id$", id.ToString());
                    //            var info = DataAccount.Where(x => notify_model.To_IDNV.ToString().Contains(x.UserId.ToString())).FirstOrDefault();
                    //            if (info is not null)
                    //            {
                    //                bool kq_noti = JeeWorkLiteController.SendNotify(loginData, info.Username, notify_model, _notifier, _configuration);
                    //            }
                    //        }
                    //        #endregion
                    //    }
                    //}
                    //#endregion
                    if (cnn.LastError is null && dt_user1.Rows.Count > 0)
                    {
                        List<long> danhsachU = dt_user1.AsEnumerable().Select(x => long.Parse(x["id_nv"].ToString())).ToList();
                        foreach (long idUser in danhsachU)
                        {
                            NhacNho.UpdateQuantityTask_Users(idUser, loginData.CustomerID, "-", _configuration, _producer);
                        }
                    }
                    return JsonResultCommon.ThanhCong();
                }
            }
            catch (Exception ex)
            {
                return JsonResultCommon.Exception(_logger, ex, _config, loginData);
            }
        }

        [Route("update-work-process")]
        [HttpPost]
        public async Task<BaseModel<object>> UpdateWorkProcess(WorkProcessModel data)
        {

            UserJWT loginData = Ulities.GetUserByHeader(HttpContext.Request.Headers);
            if (loginData == null)
                return JsonResultCommon.DangNhap();
            try
            {
                string ConnectionString = JeeWorkLiteController.getConnectionString(ConnectionCache, loginData.CustomerID, _configuration);
                using (DpsConnection cnn = new DpsConnection(ConnectionString))
                {
                    #region Lấy dữ liệu account từ JeeAccount
                    DataAccount = JeeWorkLiteController.GetAccountFromJeeAccount(HttpContext.Request.Headers, _configuration);
                    if (DataAccount == null)
                        return JsonResultCommon.Custom("Lỗi lấy danh sách nhân viên từ hệ thống quản lý tài khoản");
                    #endregion
                    Common comm = new Common(ConnectionString);
                    if (Common.IsTaskClosed(data.workid, cnn))
                    {
                        return JsonResultCommon.Custom("Công việc đã đóng không thể cập nhật");
                    }
                    SqlConditions sqlcond = new SqlConditions();
                    sqlcond.Add("id_row", data.id_row);
                    sqlcond.Add("disabled", 0);
                    string s = "select * from we_work_process where (where)";
                    DataTable old = cnn.CreateDataTable(s, "(where)", sqlcond);
                    if (old == null || old.Rows.Count == 0)
                        return JsonResultCommon.KhongTonTai("Quá trình xử lý công việc");
                    long iduser = loginData.UserID;
                    long idk = loginData.CustomerID;
                    Hashtable val = new Hashtable();
                    if (!string.IsNullOrEmpty(data.checker) || data.checker == "0")
                        val.Add("checker", data.checker);
                    else
                        val.Add("checker", DBNull.Value);
                    val.Add("updateddate", Common.GetDateTime());
                    val.Add("updatedby", iduser);
                    val.Add("change_note", data.change_note);
                    if (string.IsNullOrEmpty(data.change_note))
                    {
                        string kiemtrachecker = $"select * from we_work_process where id_row = {data.id_row} and disabled = 0 and Checker is not null";
                        if (cnn.CreateDataTable(kiemtrachecker).Rows.Count > 0)
                        {
                            return JsonResultCommon.Custom("Bạn chưa nhập lý do thay đổi người theo dõi công việc");
                        }
                    }
                    cnn.BeginTransaction();
                    if (cnn.Update(val, sqlcond, "we_work_process") != 1)
                    {
                        cnn.RollbackTransaction();
                        return JsonResultCommon.Exception(_logger, cnn.LastError, _config, loginData, ControllerContext);
                    }
                    DataTable dt = cnn.CreateDataTable(s, "(where)", sqlcond);
                    val = new Hashtable();
                    string value_new = "";
                    string value_old = "";
                    val.Add("processid", data.id_row);
                    if (string.IsNullOrEmpty(old.Rows[0]["checker"].ToString()) || "0".Equals(old.Rows[0]["checker"].ToString()))
                    {
                        val.Add("old_checker", DBNull.Value);
                    }
                    else
                    {
                        val.Add("old_checker", old.Rows[0]["checker"].ToString());
                        var info_old = DataAccount.Where(x => old.Rows[0]["checker"].ToString().Contains(x.UserId.ToString())).FirstOrDefault();
                        if (info_old != null)
                        {
                            value_old = info_old.FullName;
                        }
                    }
                    if (string.IsNullOrEmpty(data.checker.ToString()) || data.checker == "0")
                    {
                        val.Add("new_checker", DBNull.Value);
                    }
                    else
                    {
                        val.Add("new_checker", data.checker);
                        var info = DataAccount.Where(x => data.checker.Contains(x.UserId.ToString())).FirstOrDefault();
                        if (info != null)
                        {
                            value_new = info.FullName;
                        }
                    }
                    val.Add("content_note", loginData.customdata.personalInfo.Fullname + " đã chỉnh sửa người theo dõi từ " + value_old + " --> " + value_new);
                    val.Add("createddate", Common.GetDateTime());
                    val.Add("createdby", iduser);
                    val.Add("note", data.change_note);
                    if (cnn.Insert(val, "we_work_process_log") != 1)
                    {
                        cnn.RollbackTransaction();
                        return JsonResultCommon.Exception(_logger, cnn.LastError, _config, loginData, ControllerContext);
                    }
                    #region Ghi log trong project
                    string LogContent = "", LogEditContent = "";
                    LogContent = LogEditContent = "we_work_process: title=" + data.workid + ", id_project_team=" + data.id_project_team;
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
                    cnn.EndTransaction();

                    SqlConditions sqlcondcv = new SqlConditions();
                    sqlcondcv.Add("id_row", data.workid);
                    sqlcondcv.Add("disabled", 0);
                    string cvold = "select * from v_wework_new where (where)";
                    DataTable dtcvold = cnn.CreateDataTable(cvold, "(where)", sqlcondcv);
                    // gửi thông báo cho user mới
                    if (!string.IsNullOrEmpty(data.checker))
                    {
                        if (dtcvold.Rows.Count > 0)
                        {
                            var users = new List<long> { long.Parse(data.checker) };
                            JeeWorkLiteController.SendEmail(data.workid, users, 18, loginData, ConnectionString, _notifier, _configuration);
                            if (users.Count > 0)
                            {
                                #region Lấy thông tin để thông báo
                                SendNotifyModel noti = JeeWorkLiteController.GetInfoNotify(11, ConnectionString);
                                #endregion
                                #region Notify chỉnh sửa công việc
                                Hashtable has_replace = new Hashtable();
                                for (int i = 0; i < users.Count; i++)
                                {
                                    NotifyModel notify_model = new NotifyModel();
                                    has_replace = new Hashtable();
                                    has_replace.Add("nguoigui", loginData.Username);
                                    has_replace.Add("tencongviec", dtcvold.Rows[0]["title"].ToString());
                                    notify_model.AppCode = "WORK";
                                    notify_model.From_IDNV = loginData.UserID.ToString();
                                    notify_model.To_IDNV = users[i].ToString();

                                    notify_model.TitleLanguageKey = LocalizationUtility.GetBackendMessage("ww_thongbaoxulycongviec", "", "vi");
                                    notify_model.TitleLanguageKey = notify_model.TitleLanguageKey.Replace("$nguoigui$", loginData.customdata.personalInfo.Fullname);
                                    notify_model.TitleLanguageKey = notify_model.TitleLanguageKey.Replace("$tencongviec$", dtcvold.Rows[0]["title"].ToString());
                                    notify_model.TitleLanguageKey = notify_model.TitleLanguageKey.Replace("$step$", data.statusid.ToString());
                                    notify_model.ReplaceData = has_replace;
                                    notify_model.To_Link_MobileApp = noti.link_mobileapp.Replace("$id$", data.workid.ToString());
                                    notify_model.To_Link_WebApp = noti.link.Replace("$id$", data.workid.ToString());

                                    var info = DataAccount.Where(x => notify_model.To_IDNV.ToString().Contains(x.UserId.ToString())).FirstOrDefault();
                                    if (info is not null)
                                    {
                                        bool kq_noti = JeeWorkLiteController.SendNotify(loginData, info.Username, notify_model, _notifier, _configuration);
                                    }
                                }
                                #endregion
                            }
                        }
                    }

                    // gửi thông báo cho user cũ hủy bỏ theo dõi
                    if (string.IsNullOrEmpty(old.Rows[0]["checker"].ToString()) || "0".Equals(old.Rows[0]["checker"].ToString()))
                    {
                    }
                    else
                    { // gửi thông tin bị bỏ quyền 
                        var users = new List<long> { long.Parse(old.Rows[0]["checker"].ToString()) };
                        JeeWorkLiteController.SendEmail(data.workid, users, 46, loginData, ConnectionString, _notifier, _configuration);
                        if (users.Count > 0)
                        {
                            #region Lấy thông tin để thông báo
                            SendNotifyModel noti = JeeWorkLiteController.GetInfoNotify(46, ConnectionString);
                            #endregion
                            #region Notify chỉnh sửa công việc
                            Hashtable has_replace = new Hashtable();
                            for (int i = 0; i < users.Count; i++)
                            {
                                NotifyModel notify_model = new NotifyModel();
                                has_replace = new Hashtable();
                                has_replace.Add("nguoigui", loginData.Username);
                                has_replace.Add("tencongviec", dtcvold.Rows[0]["title"].ToString());
                                notify_model.AppCode = "WORK";
                                notify_model.From_IDNV = loginData.UserID.ToString();
                                notify_model.To_IDNV = users[i].ToString();

                                notify_model.TitleLanguageKey = LocalizationUtility.GetBackendMessage("ww_thongbaoboxulycongviec", "", "vi");
                                notify_model.TitleLanguageKey = notify_model.TitleLanguageKey.Replace("$nguoigui$", loginData.customdata.personalInfo.Fullname);
                                notify_model.TitleLanguageKey = notify_model.TitleLanguageKey.Replace("$tencongviec$", dtcvold.Rows[0]["title"].ToString());
                                notify_model.TitleLanguageKey = notify_model.TitleLanguageKey.Replace("$step$", data.statusid.ToString());
                                notify_model.ReplaceData = has_replace;
                                notify_model.To_Link_MobileApp = noti.link_mobileapp.Replace("$id$", data.workid.ToString());
                                notify_model.To_Link_WebApp = noti.link.Replace("$id$", data.workid.ToString());

                                var info = DataAccount.Where(x => notify_model.To_IDNV.ToString().Contains(x.UserId.ToString())).FirstOrDefault();
                                if (info is not null)
                                {
                                    bool kq_noti = JeeWorkLiteController.SendNotify(loginData, info.Username, notify_model, _notifier, _configuration);
                                }
                            }
                            #endregion
                        }
                    }

                    return JsonResultCommon.ThanhCong(data);
                }
            }
            catch (Exception ex)
            {
                return JsonResultCommon.Exception(_logger, ex, _config, loginData);
            }
        }
        #region calendar
        [Route("list-event")]
        [HttpGet]
        public object ListEvent([FromQuery] QueryParams query)
        {
            UserJWT loginData = Ulities.GetUserByHeader(HttpContext.Request.Headers);
            if (loginData == null)
                return JsonResultCommon.DangNhap();
            if (query == null)
                query = new QueryParams();
            bool Visible = true;
            PageModel pageModel = new PageModel();
            try
            {

                string domain = _configuration.GetValue<string>("Host:JeeWork_API") + "/";
                string ConnectionString = JeeWorkLiteController.getConnectionString(ConnectionCache, loginData.CustomerID, _configuration);
                using (DpsConnection cnn = new DpsConnection(ConnectionString))
                {
                    #region Lấy dữ liệu account từ JeeAccount
                    DataAccount = JeeWorkLiteController.GetAccountFromJeeAccount(HttpContext.Request.Headers, _configuration);
                    if (DataAccount == null)
                        return JsonResultCommon.Custom("Lỗi lấy danh sách nhân viên từ hệ thống quản lý tài khoản");
                    string error = "";
                    string listID = JeeWorkLiteController.ListAccount(HttpContext.Request.Headers, out error, _configuration);
                    if (error != "")
                        return JsonResultCommon.Custom(error);
                    #endregion
                    #region filter thời gian, keyword
                    DateTime from = Common.GetDateTime();
                    DateTime to = Common.GetDateTime();
                    //if (!string.IsNullOrEmpty(query.filter["TuNgay"]))
                    //{
                    //    bool from1 = DateTime.TryParseExact(query.filter["TuNgay"], "dd/MM/yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out from);
                    //    if (!from1)
                    //        return JsonResultCommon.Custom("Thời gian bắt đầu không hợp lệ");
                    //}
                    //if (!string.IsNullOrEmpty(query.filter["DenNgay"]))
                    //{
                    //    bool to1 = DateTime.TryParseExact(query.filter["DenNgay"], "dd/MM/yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out to);
                    //    if (!to1)
                    //        return JsonResultCommon.Custom("Thời gian bắt đầu không hợp lệ");
                    //}
                    //int nam = DateTime.Today.Year;
                    //int thang = DateTime.Today.Month;
                    //var lastDayOfMonth = DateTime.DaysInMonth(nam, thang);
                    //if (!string.IsNullOrEmpty(query.filter["Thang"]))
                    //{
                    //    thang = int.Parse(query.filter["Thang"]);
                    //}
                    //if (!string.IsNullOrEmpty(query.filter["Nam"]))
                    //{
                    //    nam = int.Parse(query.filter["Nam"]);
                    //}
                    //from = new DateTime(nam, thang, 1, 0, 0, 1);
                    //to = GetEndDateInMonth(thang, nam);
                    Dictionary<string, string> collect = new Dictionary<string, string>
                        {
                            { "CreatedDate", "CreatedDate"},
                            { "Deadline", "deadline"},
                            { "StartDate", "start_date"}
                        };
                    string collect_by = "CreatedDate";
                    if (!string.IsNullOrEmpty(query.filter["collect_by"]))
                        collect_by = collect[query.filter["collect_by"]];
                    #endregion
                    string strW_parent = "";
                    string strW = " ";
                    string forme = "", assign = "", following = "";
                    forme = "w.id_nv=@iduser";
                    assign = "w.nguoigiao=@iduser";
                    if (!string.IsNullOrEmpty(query.filter["forme"]) && !string.IsNullOrEmpty(query.filter["assign"]) && !string.IsNullOrEmpty(query.filter["following"]))
                    {
                        strW = " and (w.id_nv=@iduser or w.createdby=@iduser or w.nguoigiao=@iduser or w.id_row in (select id_work from we_work_user where loai = 2 and disabled=0 and id_user = @iduser) (parent))"; // w.nguoigiao=@iduser or w.createdby=@iduser -- w.NguoiGiao = @iduser or
                    }
                    if (string.IsNullOrEmpty(query.filter["forme"]) && string.IsNullOrEmpty(query.filter["assign"]) && string.IsNullOrEmpty(query.filter["following"]))
                    {
                        strW = " and (w.id_nv=@iduser or w.createdby=@iduser or w.nguoigiao=@iduser or w.id_row in (select id_work from we_work_user where loai = 2 and disabled=0 and id_user = @iduser) (parent))"; // w.nguoigiao=@iduser or w.createdby=@iduser -- w.NguoiGiao = @iduser or
                    }
                    if (!string.IsNullOrEmpty(query.filter["forme"]))//được giao - 41
                    {
                        strW += " and " + forme + " (parent) ";
                    }
                    if (!string.IsNullOrEmpty(query.filter["assign"]))//giao đi - 42
                    {
                        strW += " and " + assign + " (parent) ";
                    }
                    if (!string.IsNullOrEmpty(query.filter["following"]))// theo dõi - 43
                    {
                        strW = $" and w.id_row in (select id_work from we_work_user where loai = 2 and disabled=0 and id_user = @iduser) (parent)";
                    }
                    string displayChild = "1";//hiển thị con: 0-không hiển thị, 1- 1 cấp con, 2- nhiều cấp con
                    if (!string.IsNullOrEmpty(query.filter["displayChild"]))
                    {
                        displayChild = query.filter["displayChild"];
                        strW = strW.Replace("(parent)", " ");
                    }
                    else
                    {
                        strW_parent = "or w.id_row in (select id_parent from we_work ww join we_work_user wu on ww.id_row = wu.id_work where ww.disabled = 0 and wu.disabled = 0 and id_parent is not null and id_user = @iduser)";
                        strW = strW.Replace("(parent)", strW_parent);
                    }
                    //#endregion
                    DataSet ds = GetWorkByEmployee(Request.Headers, cnn, query, loginData.UserID, DataAccount, strW);//" and w.id_nv=@iduser"
                    if (cnn.LastError != null || ds == null)
                        return JsonResultCommon.Exception(_logger, cnn.LastError, _config, loginData, ControllerContext);
                    //var temp = filterWork(ds.Tables[0].AsEnumerable(), query.filter);//k bao gồm con
                    var temp = ds.Tables[0].AsEnumerable();
                    // Phân trang
                    int total = temp.Count();
                    if (total == 0)
                        return JsonResultCommon.ThanhCong(new List<string>());
                    //var dtChild = ds.Tables[0].AsEnumerable().Where(x => x["id_parent"] != DBNull.Value).AsEnumerable();
                    var Children = from rr in temp
                                   where !"".Equals(rr["start_date"].ToString())
                                   select new
                                   {
                                       id_row = rr["id_row"],
                                       start = rr["start_date"] != DBNull.Value ? string.Format("{0:yyyy-MM-ddTHH:mm}", rr["start_date"]) : "",
                                       end = rr["end_date"] != DBNull.Value ? string.Format("{0:yyyy-MM-ddTHH:mm}", rr["end_date"]) : "",
                                       deadline = rr["deadline"] != DBNull.Value ? string.Format("{0:yyyy-MM-ddTHH:mm}", rr["deadline"]) : "",
                                       title = rr["title"],
                                       classNames = "",
                                       id_nv = rr["id_nv"],
                                       phanloai = calendar_getphanloai(rr["id_row"].ToString(), loginData, ConnectionString)
                                   };
                    return JsonResultCommon.ThanhCong(Children, pageModel, Visible);
                }
            }
            catch (Exception ex)
            {
                return JsonResultCommon.Exception(_logger, ex, _config, loginData);
            }
        }
        [Route("list-event-by-project")]
        [HttpGet]
        public object ListEventByProject([FromQuery] QueryParams query)
        {

            UserJWT loginData = Ulities.GetUserByHeader(HttpContext.Request.Headers);
            if (loginData == null)
                return JsonResultCommon.DangNhap();
            if (query == null)
                query = new QueryParams();
            bool Visible = true;
            PageModel pageModel = new PageModel();
            try
            {
                string domain = _configuration.GetValue<string>("Host:JeeWork_API") + "/";
                string ConnectionString = JeeWorkLiteController.getConnectionString(ConnectionCache, loginData.CustomerID, _configuration);
                using (DpsConnection cnn = new DpsConnection(ConnectionString))
                {
                    #region Lấy dữ liệu account từ JeeAccount
                    DataAccount = JeeWorkLiteController.GetAccountFromJeeAccount(HttpContext.Request.Headers, _configuration);
                    if (DataAccount == null)
                        return JsonResultCommon.Custom("Lỗi lấy danh sách nhân viên từ hệ thống quản lý tài khoản");

                    string error = "";
                    string listID = JeeWorkLiteController.ListAccount(HttpContext.Request.Headers, out error, _configuration);
                    if (error != "")
                        return JsonResultCommon.Custom(error);
                    #endregion

                    if (string.IsNullOrEmpty(query.filter["id_project_team"]))
                        return JsonResultCommon.Custom("Dự án/phòng ban bắt buộc nhập");
                    #region filter thời gian , keyword
                    DateTime from = Common.GetDateTime();
                    DateTime to = Common.GetDateTime();
                    if (!string.IsNullOrEmpty(query.filter["TuNgay"]))
                    {
                        bool from1 = DateTime.TryParseExact(query.filter["TuNgay"], "dd/MM/yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out from);
                        if (!from1)
                            return JsonResultCommon.Custom("Thời gian bắt đầu không hợp lệ");

                    }
                    if (!string.IsNullOrEmpty(query.filter["DenNgay"]))
                    {
                        bool to1 = DateTime.TryParseExact(query.filter["DenNgay"], "dd/MM/yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out to);
                        if (!to1)
                            return JsonResultCommon.Custom("Thời gian bắt đầu không hợp lệ");
                    }
                    #endregion
                    string displayChild = "0";//hiển thị con: 0-không hiển thị, 1- 1 cấp con, 2- nhiều cấp con
                    if (!string.IsNullOrEmpty(query.filter["displayChild"]))
                        displayChild = query.filter["displayChild"];
                    DataSet ds = GetWorkByEmployee(Request.Headers, cnn, query, loginData.UserID, DataAccount);
                    if (cnn.LastError != null || ds == null)
                        return JsonResultCommon.Exception(_logger, cnn.LastError, _config, loginData, ControllerContext);
                    var temp = filterWork(ds.Tables[0].AsEnumerable().Where(x => x["id_parent"] == DBNull.Value), query.filter);//k bao gồm con
                    var tags = ds.Tables[1].AsEnumerable();
                    // Phân trang
                    int total = temp.Count();
                    if (total == 0)
                        return JsonResultCommon.ThanhCong(new List<string>());
                    if (query.more)
                    {
                        query.page = 1;
                        query.record = total;
                    }
                    pageModel.TotalCount = total;
                    pageModel.AllPage = (int)Math.Ceiling(total / (decimal)query.record);
                    pageModel.Size = query.record;
                    pageModel.Page = query.page;
                    var dtNew = temp.Skip((query.page - 1) * query.record).Take(query.record);
                    var dtChild = ds.Tables[0].AsEnumerable().Where(x => x["id_parent"] != DBNull.Value).AsEnumerable();
                    var Children = from rr in dtNew
                                   select new
                                   {
                                       id_row = rr["id_row"],
                                       start = rr["start_date"] != DBNull.Value ? string.Format("{0:yyyy-MM-ddTHH:mm}", rr["start_date"]) : "",
                                       end = rr["end_date"] != DBNull.Value ? string.Format("{0:yyyy-MM-ddTHH:mm}", rr["end_date"]) : "",
                                       title = rr["title"],
                                       //classNames = new List<string>() { rr["status"].ToString() == "2" ? "success" : "", rr["is_quahan"].ToString() == "1" ? "overdue" : "" },
                                       //imageurl = JeeWorkLiteController.genLinkImage(domain, loginData.UserID, rr["id_nv"].ToString(), _hostingEnvironment.ContentRootPath)
                                       //Children = getChild(domain, loginData.CustomerID, "", displayChild, g.Key, g.Concat(dtChild).CopyToDataTable().AsEnumerable(), tags)
                                   };
                    return JsonResultCommon.ThanhCong(Children, pageModel, Visible);
                }
            }
            catch (Exception ex)
            {
                return JsonResultCommon.Exception(_logger, ex, _config, loginData);
            }
        }
        #endregion
        [Route("ExportExcelByUsers")]
        [HttpGet]
        public async Task<IActionResult> ExportExcelByUsers([FromQuery] QueryParams query)
        {
            UserJWT loginData = Ulities.GetUserByHeader(HttpContext.Request.Headers);
            if (loginData == null)
                return Unauthorized();
            try
            {
                #region Lấy dữ liệu account từ JeeAccount
                DataAccount = JeeWorkLiteController.GetAccountFromJeeAccount(HttpContext.Request.Headers, _configuration);
                if (DataAccount == null)
                    return BadRequest("Lỗi lấy danh sách nhân viên từ hệ thống quản lý tài khoản");
                string error = "";
                string listID = JeeWorkLiteController.ListAccount(HttpContext.Request.Headers, out error, _configuration);
                if (error != "")
                    return BadRequest(error);
                #endregion

                if (string.IsNullOrEmpty(query.filter["id_nv"]))
                    return BadRequest();

                #region filter thời gian , keyword
                DateTime from = Common.GetDateTime();
                DateTime to = Common.GetDateTime();
                if (!string.IsNullOrEmpty(query.filter["TuNgay"]))
                {
                    bool from1 = DateTime.TryParseExact(query.filter["TuNgay"], "dd/MM/yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out from);
                    if (!from1)
                        return BadRequest();
                }
                if (!string.IsNullOrEmpty(query.filter["DenNgay"]))
                {
                    bool to1 = DateTime.TryParseExact(query.filter["DenNgay"], "dd/MM/yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out to);
                    if (!to1)
                        return BadRequest();
                }
                #endregion
                string displayChild = "0";//hiển thị con: 0-không hiển thị, 1- 1 cấp con, 2- nhiều cấp con
                if (!string.IsNullOrEmpty(query.filter["displayChild"]))
                    displayChild = query.filter["displayChild"];
                string[] header = { "Tên công việc", "Người giao", "Người thực hiện", "Người theo dõi", "Ưu tiên (urgent)", "Tags", "Ngày bắt đầu", "Deadline", "Hoàn thành thực tế", "Mô tả công việc", "Trạng thái", "Kết quả công việc", "Mục tiêu", "Ngày tạo", "Mã công việc (ID)" };
                DataTable dt = new DataTable();
                var temp = (from c in header
                            select new DataColumn() { ColumnName = c }).ToList();
                temp.Add(new DataColumn() { ColumnName = "merge_row" });
                temp.Add(new DataColumn() { ColumnName = "merge_title" });
                DataColumn[] cols = temp.ToArray();
                dt.Columns.AddRange(cols);
                string ConnectionString = JeeWorkLiteController.getConnectionString(ConnectionCache, loginData.CustomerID, _configuration);
                using (DpsConnection cnn = new DpsConnection(ConnectionString))
                {
                    //                    string strG = @"select p.id_row, p.title from we_project_team_user u
                    //join we_project_team p on p.id_row=u.id_project_team where u.disabled=0 and p.Disabled=0 and id_user=" + query.filter["id_nv"];
                    string strG = @$"select distinct p.id_row, p.title from we_project_team_user u
join we_project_team p on p.id_row=u.id_project_team 
join we_department d on d.id_row = p.id_department
where u.disabled=0 and p.Disabled=0 and d.Disabled = 0 and id_user = { query.filter["id_nv"] } and d.IdKH = { loginData.CustomerID}";
                    DataTable dtG = cnn.CreateDataTable(strG);
                    //DataSet ds = getWork(cnn, query, loginData.UserID);
                    string strW = " and (w.id_nv=@iduser or w.createdby=@iduser or w.id_row in (select id_work from we_work_user where id_user = @iduser union all select id_parent from we_work ww join we_work_user wu on ww.id_row = wu.id_work where ww.Disabled = 0 and wu.Disabled = 0  and id_user = @iduser ) )"; // w.nguoigiao=@iduser or w.createdby=@iduser -- w.NguoiGiao = @iduser or
                    DataSet ds = GetWorkByEmployee(Request.Headers, cnn, query, long.Parse(query.filter["id_nv"].ToString()), DataAccount, strW);
                    var tags = ds.Tables[1].AsEnumerable();
                    var followers = ds.Tables[2].AsEnumerable();
                    if (cnn.LastError != null || ds == null)
                        return BadRequest();
                    DataRow _new;
                    foreach (DataRow drG in dtG.Rows)
                    {
                        _new = dt.NewRow();
                        _new["merge_row"] = true;
                        _new["merge_title"] = drG["title"] + ":";
                        dt.Rows.Add(_new);
                        var a = ds.Tables[0].AsEnumerable().Where(x => x["id_project_team"].Equals(drG["id_row"]));
                        if (displayChild == "0")
                            a = a.Where(x => x["id_parent"] == DBNull.Value);
                        if (a.Count() == 0)
                            continue;
                        DataTable dtW = a.CopyToDataTable();
                        genDr(dtW, followers, tags, DBNull.Value, "", ref dt);
                    }
                }
                //Xuất excel
                string[] width = { "315", "140", "140", "140", "140", "140", "140", "140", "140", "280", "140", "280", "210", "140", "140" };
                Hashtable format = new Hashtable();
                string rowheight = "18.5";
                string s = "Danh sách công việc";
                if (displayChild == "0")
                    s += "(Không bao gồm công việc con)";
                string excel = ExportExcelHelper.ExportToExcel(dt, s, header, width, rowheight, rowheight, format);
                string fileName = "Danhsachcongviec_" + query.filter["id_nv"] + "_" + string.Format("{0:ddMMyyyy}", DateTime.Today) + ".xls";
                var bytearr = Encoding.UTF8.GetBytes(excel);
                this.Response.Headers.Add("X-Filename", fileName);
                this.Response.Headers.Add("Access-Control-Expose-Headers", "X-Filename");
                return new FileContentResult(bytearr, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");
            }
            catch (Exception ex)
            {
                return BadRequest(JsonResultCommon.Exception(_logger, ex, _config, loginData));
            }
        }
        /// <summary>
        /// Chức năng đóng công việc dành cho mobile
        /// </summary>
        /// <param name="id">id task</param>
        /// <param name="closed"></param>
        /// <returns></returns>
        [Route("Close")]
        [HttpGet]
        public async Task<object> Close(long id, bool closed = true)
        {
            UserJWT loginData = Ulities.GetUserByHeader(HttpContext.Request.Headers);
            if (loginData == null)
                return JsonResultCommon.DangNhap();
            try
            {
                string ConnectionString = JeeWorkLiteController.getConnectionString(ConnectionCache, loginData.CustomerID, _configuration);
                using (DpsConnection cnn = new DpsConnection(ConnectionString))
                {
                    SqlConditions sqlcond = new SqlConditions();
                    int id_log_action = closed ? 59 : 60;
                    sqlcond.Add("id_row", id);
                    sqlcond.Add("disabled", 0);
                    string s = "select title as tencongviec_old, * from v_wework_new where (where)";
                    DataTable old = cnn.CreateDataTable(s, "(where)", sqlcond);
                    if (old == null || old.Rows.Count == 0)
                        return JsonResultCommon.KhongTonTai("Công việc");
                    bool checkrole = Common.CheckIsUpdatedTask(old.Rows[0]["id_project_team"].ToString(), 22, loginData, cnn, ConnectionString);
                    if (!checkrole)
                        return JsonResultCommon.Custom("Bạn không có quyền đóng/mở công việc");
                    if (old.Rows[0]["closed"] != DBNull.Value && (bool)old.Rows[0]["closed"] == closed)
                        return JsonResultCommon.Custom("Công việc này vẫn chưa " + (closed ? "mở" : "đóng"));
                    long iduser = loginData.UserID;
                    long idk = loginData.CustomerID;
                    Hashtable val = new Hashtable();
                    val.Add("closed", closed);
                    val.Add("closed_work_date", Common.GetDateTime());
                    val.Add("closed_work_by", iduser);
                    cnn.BeginTransaction();
                    if (cnn.Update(val, sqlcond, "we_work") != 1)
                    {
                        cnn.RollbackTransaction();
                        return JsonResultCommon.Exception(_logger, cnn.LastError, _config, loginData, ControllerContext);
                    }
                    DataTable dt = cnn.CreateDataTable(s, "(where)", sqlcond);
                    if (!JeeWorkLiteController.log(_logger, loginData.Username, cnn, id_log_action, id, iduser, "", null))
                    {
                        cnn.RollbackTransaction();
                        return JsonResultCommon.Exception(_logger, cnn.LastError, _config, loginData, ControllerContext);
                    }
                    #region Ghi log trong project
                    string LogContent = "Đóng công việc (" + id + ")";
                    Common.Ghilogfile(loginData.CustomerID.ToString(), "", LogContent, loginData.Username, ControllerContext);
                    #endregion
                    #region Ghi log lên CDN
                    var d2 = new ActivityLog()
                    {
                        username = loginData.Username,
                        category = LogContent,
                        action = loginData.customdata.personalInfo.Fullname + " thao tác",
                        data = ""
                    };
                    _logger.LogInformation(JsonConvert.SerializeObject(d2));
                    #endregion
                    cnn.EndTransaction();
                    #region Lấy dữ liệu account từ JeeAccount
                    DataAccount = JeeWorkLiteController.GetAccountFromJeeAccount(HttpContext.Request.Headers, _configuration);
                    if (DataAccount == null)
                        return JsonResultCommon.Custom("Lỗi lấy danh sách nhân viên từ hệ thống quản lý tài khoản");
                    #endregion
                    var users_loai1 = JeeWorkLiteController.GetUserSendNotify(loginData, id, id_log_action, 1, ConnectionString, DataAccount, cnn);
                    var users_loai2 = JeeWorkLiteController.GetUserSendNotify(loginData, id, id_log_action, 2, ConnectionString, DataAccount, cnn);
                    //#region Check dự án đó có gửi gửi mail khi chỉnh sửa công việc hay không
                    //int TemplateId = closed ? 43 : 44;
                    //string txtnoti = closed ? "ww_dongcongviec" : "ww_mocongviec";
                    //if (JeeWorkLiteController.CheckNotify_ByConditions(long.Parse(old.Rows[0]["id_project_team"].ToString()), "email_update_work", false, ConnectionString))
                    //{
                    //    DataTable dt_user = cnn.CreateDataTable("select id_nv, title, id_row from v_wework_new where (where) and id_nv is not null", "(where)", sqlcond);
                    //    if (dt_user.Rows.Count > 0)
                    //    {
                    //        var users = new List<long> { long.Parse(dt_user.Rows[0]["id_nv"].ToString()) };
                    //        cnn.EndTransaction();
                    //        JeeWorkLiteController.SendEmail(id, users, TemplateId, loginData, ConnectionString, _notifier, _configuration, old);
                    //        #region Lấy thông tin để thông báo
                    //        SendNotifyModel noti = JeeWorkLiteController.GetInfoNotify(11, ConnectionString);
                    //        #endregion
                    //        #region Notify chỉnh sửa công việc
                    //        Hashtable has_replace = new Hashtable();
                    //        for (int i = 0; i < users.Count; i++)
                    //        {
                    //            NotifyModel notify_model = new NotifyModel();
                    //            has_replace = new Hashtable();
                    //            has_replace.Add("nguoigui", loginData.Username);
                    //            has_replace.Add("tencongviec", old.Rows[0]["title"].ToString());
                    //            notify_model.AppCode = "WORK";
                    //            notify_model.From_IDNV = loginData.UserID.ToString();
                    //            notify_model.To_IDNV = users[i].ToString();
                    //            notify_model.TitleLanguageKey = LocalizationUtility.GetBackendMessage(txtnoti, "", "vi");
                    //            notify_model.TitleLanguageKey = notify_model.TitleLanguageKey.Replace("$nguoigui$", loginData.customdata.personalInfo.Fullname);
                    //            notify_model.TitleLanguageKey = notify_model.TitleLanguageKey.Replace("$tencongviec$", old.Rows[0]["title"].ToString());
                    //            notify_model.ReplaceData = has_replace;
                    //            notify_model.To_Link_MobileApp = noti.link_mobileapp.Replace("$id$", id.ToString());
                    //            notify_model.To_Link_WebApp = noti.link.Replace("$id$", id.ToString());
                    //            DataAccount = JeeWorkLiteController.GetAccountFromJeeAccount(HttpContext.Request.Headers, _configuration);
                    //            if (DataAccount == null)
                    //                return JsonResultCommon.Custom("Lỗi lấy danh sách nhân viên từ hệ thống quản lý tài khoản");
                    //            var info = DataAccount.Where(x => notify_model.To_IDNV.ToString().Contains(x.UserId.ToString())).FirstOrDefault();
                    //            if (info is not null)
                    //            {
                    //                bool kq_noti = JeeWorkLiteController.SendNotify(loginData, info.Username, notify_model, _notifier, _configuration);
                    //            }
                    //        }
                    //        #endregion
                    //    }
                    //}
                    //#endregion
                    #region Lấy thông tin để thông báo
                    int templateguimail = closed ? 43 : 44;
                    SendNotifyModel noti = new SendNotifyModel();
                    noti = JeeWorkLiteController.GetInfoNotify(templateguimail, ConnectionString);
                    string workname = old.Rows[0]["tencongviec_old"].ToString();
                    string TitleLanguageKey = closed ? "ww_dongcongviec" : "ww_mocongviec";
                    #endregion
                    string user_assign = "0";
                    if (users_loai1.Count > 0)
                    {
                        NotifyModel notify_model = new NotifyModel();
                        Hashtable has_replace = new Hashtable();
                        has_replace.Add("nguoigui", loginData.Username);
                        has_replace.Add("tencongviec", workname);
                        notify_model.AppCode = "WORK";
                        if (!string.IsNullOrEmpty(old.Rows[0]["id_nv"].ToString()))
                        {
                            user_assign = old.Rows[0]["id_nv"].ToString();
                        }
                        notify_model.From_IDNV = user_assign;
                        notify_model.TitleLanguageKey = LocalizationUtility.GetBackendMessage(TitleLanguageKey, "", "vi");
                        notify_model.TitleLanguageKey = notify_model.TitleLanguageKey.Replace("$nguoigui$", loginData.customdata.personalInfo.Fullname)
                            .Replace("$tencongviec$", workname);
                        notify_model.ReplaceData = has_replace;
                        notify_model.To_Link_MobileApp = noti.link_mobileapp.Replace("$id$", id.ToString());
                        notify_model.To_Link_WebApp = noti.link.Replace("$id$", id.ToString());
                        for (int i = 0; i < users_loai1.Count; i++)
                        {
                            notify_model.To_IDNV = users_loai1[i].ToString();
                            var info = DataAccount.Where(x => notify_model.To_IDNV.ToString().Contains(x.UserId.ToString())).FirstOrDefault();
                            if (info is not null)
                            {
                                bool kq_noti = JeeWorkLiteController.SendNotify(loginData, info.Username, notify_model, _notifier, _configuration);
                            }
                        }
                    }
                    JeeWorkLiteController.SendEmail(id, users_loai2, templateguimail, loginData, ConnectionString, _notifier, _configuration, old, long.Parse(user_assign));
                    return JsonResultCommon.ThanhCong(closed);
                }
            }
            catch (Exception ex)
            {
                return JsonResultCommon.Exception(_logger, ex, _config, loginData);
            }
        }

        /// <summary>
        /// Danh sách công việc cá nhân (Tôi làm/ Tôi đang theo dõi)
        /// </summary>
        /// <param name="query"></param>
        /// <returns></returns>
        [Route("my-list")]
        [HttpGet]
        public object MyList([FromQuery] QueryParams query)
        {
            UserJWT loginData = Ulities.GetUserByHeader(HttpContext.Request.Headers);
            if (loginData == null)
                return JsonResultCommon.DangNhap();
            if (query == null)
                query = new QueryParams();
            bool Visible = true;
            PageModel pageModel = new PageModel();
            try
            {
                #region Lấy dữ liệu account từ JeeAccount
                DataAccount = JeeWorkLiteController.GetAccountFromJeeAccount(HttpContext.Request.Headers, _configuration);
                if (DataAccount == null)
                    return JsonResultCommon.Custom("Lỗi lấy danh sách nhân viên từ hệ thống quản lý tài khoản");
                #endregion
                string domain = _configuration.GetValue<string>("Host:JeeWork_API") + "/";
                string ConnectionString = JeeWorkLiteController.getConnectionString(ConnectionCache, loginData.CustomerID, _configuration);
                using (DpsConnection cnn = new DpsConnection(ConnectionString))
                {
                    long IDNV = loginData.UserID;
                    #region filter thời gian, keyword
                    DateTime from = Common.GetDateTime();
                    DateTime to = Common.GetDateTime();
                    if (!string.IsNullOrEmpty(query.filter["TuNgay"]))
                    {
                        bool from1 = DateTime.TryParseExact(query.filter["TuNgay"], "dd/MM/yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out from);
                        if (!from1)
                            return JsonResultCommon.Custom("Thời gian bắt đầu không hợp lệ");
                    }
                    if (!string.IsNullOrEmpty(query.filter["DenNgay"]))
                    {
                        bool to1 = DateTime.TryParseExact(query.filter["DenNgay"], "dd/MM/yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out to);
                        if (!to1)
                            return JsonResultCommon.Custom("Thời gian kết thúc không hợp lệ");
                    }
                    #endregion
                    #region Lấy công việc user theo filter
                    string strW_parent = "";
                    string strW = "";
                    if (!string.IsNullOrEmpty(query.filter["id_nv"]))
                        IDNV = long.Parse(query.filter["id_nv"]);
                    strW = " and (w.id_nv=@iduser or w.createdby=@iduser or w.nguoigiao=@iduser or w.id_row in (select id_work from we_work_user where loai = 2 and disabled=0 and id_user = @iduser)  (parent))"; // w.nguoigiao=@iduser or w.createdby=@iduser -- w.NguoiGiao = @iduser or
                    if (!string.IsNullOrEmpty(query.filter["filter"]))
                    {
                        if (query.filter["filter"] == "1")//được giao
                            strW = " and (w.id_nv=@iduser (parent)) ";
                        if (query.filter["filter"] == "2")//giao đi
                            strW = " and (w.nguoigiao=@iduser (parent))";
                        if (query.filter["filter"] == "3")// theo dõi
                            strW = $" and (w.id_row in (select id_work from we_work_user where loai = 2 and disabled=0 and id_user = @iduser ) (parent))";
                    }
                    string displayChild = "0";//hiển thị con: 0-không hiển thị, 1- 1 cấp con, 2- nhiều cấp con
                    displayChild = query.filter["displayChild"];
                    if (displayChild.ToString() == "0")
                    {
                        strW = strW.Replace("(parent)", " ");
                    }
                    else
                    {
                        string querysub = "";
                        if (string.IsNullOrEmpty(query.filter["subtask_done"]) || query.filter["subtask_done"] == "0")// sub task
                            querysub += $" and ww.end_date is null";
                        strW_parent = $"or w.id_row in (select id_parent from we_work ww join we_work_user wu on ww.id_row = wu.id_work where ww.disabled = 0 and wu.disabled = 0 and id_parent is not null and id_user = @iduser {querysub} )";
                        if (query.filter["filter"] == "1")//được giao
                            strW_parent = $"or w.id_row in (select id_parent from we_work ww join we_work_user wu on ww.id_row = wu.id_work where ww.disabled = 0 and wu.disabled = 0 and id_parent is not null and wu.loai = 1 and id_user = @iduser {querysub})";
                        if (query.filter["filter"] == "2")//giao đi
                            strW_parent = $"or w.id_row in (select id_parent from we_work ww join we_work_user wu on ww.id_row = wu.id_work where ww.disabled = 0 and wu.disabled = 0 and id_parent is not null and wu.CreatedBy = @iduser {querysub})";
                        if (query.filter["filter"] == "3")// theo dõi
                            strW_parent = $"or w.id_row in (select id_parent from we_work ww join we_work_user wu on ww.id_row = wu.id_work where ww.disabled = 0 and wu.disabled = 0 and id_parent is not null and wu.loai = 2 and id_user = @iduser {querysub})";
                        strW = strW.Replace("(parent)", strW_parent);
                    }
                    if (query.filter["subtask_done"] == "0")// sub task 
                        strW += $" and w.id_row not in (select id_row from we_work where end_date is not null and id_parent is not null) ";
                    if (query.filter["task_done"] == "0")// task
                        strW += $" and  w.id_row not in (select id_row from we_work where end_date is not null and id_parent is null) ";
                    #endregion
                    string columnName = "id_project_team";
                    #region group
                    string strG = @$"select distinct p.id_row, p.title 
                                    from we_project_team_user u
                                    join we_project_team p on p.id_row=u.id_project_team 
                                    join we_department d on d.id_row = p.id_department
                                    where u.disabled=0 and p.disabled=0 and d.disabled = 0 
                                    and id_user = { IDNV } and d.idkh = { loginData.CustomerID}";
                    #endregion
                    DataTable dtG = cnn.CreateDataTable(strG);
                    if (dtG.Rows.Count == 0)
                        return JsonResultCommon.ThanhCong(new List<string>(), null, Visible);
                    DataSet ds = GetWorkByEmployee(Request.Headers, cnn, query, IDNV, DataAccount, strW);
                    if (cnn.LastError != null || ds == null)
                        return JsonResultCommon.Exception(_logger, cnn.LastError, _config, loginData, ControllerContext);
                    //var temp = filterWork(ds.Tables[0].AsEnumerable().Where(x => x["id_parent"] == DBNull.Value), query.filter);//k bao gồm con
                    var tags = ds.Tables[1].AsEnumerable();
                    // Phân trang
                    DataTable dt_work = ds.Tables[0];
                    dt_work.DefaultView.Sort = "ActivityDate desc";
                    Func<DateTime, int> weekProjector = d => CultureInfo.CurrentCulture.Calendar.GetWeekOfYear(d, CalendarWeekRule.FirstFourDayWeek, DayOfWeek.Monday);
                    var Children = from rr in dtG.AsEnumerable()
                                   select new
                                   {
                                       id = rr["id_row"],
                                       title = rr["title"],
                                       data = getChild(domain, loginData.CustomerID, columnName, displayChild, rr["id_row"], dt_work.AsEnumerable(), tags, DataAccount, loginData, ConnectionString)
                                   };
                    return JsonResultCommon.ThanhCong(Children, pageModel, Visible);
                }
            }
            catch (Exception ex)
            {
                return JsonResultCommon.Exception(_logger, ex, _config, loginData);
            }
        }
        private void genDr(DataTable dtW, EnumerableRowCollection<DataRow> followers, EnumerableRowCollection<DataRow> tags, object id_parent, string level, ref DataTable dt)
        {
            var a = dtW.AsEnumerable().Where(x => x["id_parent"].Equals(id_parent));
            if (a.Count() == 0)
                return;
            DataTable filter = a.CopyToDataTable();
            foreach (DataRow dr in filter.Rows)
            {
                DataRow _new = dt.NewRow();
                _new[0] = level + dr["title"];
                _new[1] = dr["hoten_nguoigiao"];
                _new[2] = dr["hoten"];
                _new[3] = string.Join(",", (from t in followers
                                            where dr["id_row"].Equals(t["id_work"])
                                            select t["hoten"]));
                _new[4] = (bool)dr["urgent"] ? "Khẩn" : "";
                _new[5] = string.Join(",", (from t in tags
                                            where dr["id_row"].Equals(t["id_work"])
                                            select t["title"]));
                _new[6] = dr["start_date"] == DBNull.Value ? "" : string.Format("{0:dd/MM/yyyy HH:mm}", dr["start_date"]);
                _new[7] = dr["deadline"] == DBNull.Value ? "" : string.Format("{0:dd/MM/yyyy HH:mm}", dr["deadline"]);
                _new[8] = dr["end_date"] == DBNull.Value ? "" : string.Format("{0:dd/MM/yyyy HH:mm}", dr["end_date"]);
                _new[9] = dr["description"];
                string s = "";
                _new[10] = s;//s == "1" ? "Đang làm" : (s == "2" ? "Hoàn thành" : "Chờ review");
                _new[11] = dr["result"];
                _new[12] = dr["milestone"];
                _new[13] = dr["createddate"] == DBNull.Value ? "" : string.Format("{0:dd/MM/yyyy HH:mm}", dr["createddate"]);
                _new[14] = dr["id_row"];
                dt.Rows.Add(_new);
                genDr(dtW, followers, tags, dr["id_row"], level + "#", ref dt);
            }
        }
        public static DataTable list_status_user(string workid, string id_project_team, UserJWT loginData, string ConnectionString, List<AccUsernameModel> DataAccount)
        {
            using (DpsConnection cnn = new DpsConnection(ConnectionString))
            {
                try
                {
                    //cnn = new DpsConnection();
                    DataTable dt = new DataTable();
                    string a = "select status from we_work where id_row = " + workid + "";
                    object status_hientai = cnn.ExecuteScalar("select status from we_work where id_row = " + workid + "");
                    if (status_hientai == null)
                    {
                        string test = a;
                    }
                    if (cnn.CreateDataTable("select position from we_status where id_row = " + status_hientai + " and id_project_team =" + id_project_team).Rows.Count == 0)
                    {
                        string sqlq = "select ISNULL((select id_row from we_status where disabled=0 and Position = 1 and id_project_team = " + id_project_team + "),0)";
                        var statusID = long.Parse(cnn.ExecuteScalar(sqlq).ToString());
                        Hashtable val = new Hashtable();
                        val.Add("status", statusID);
                        SqlConditions cond = new SqlConditions();
                        cond.Add("id_row", workid);
                        //cnn.BeginTransaction();
                        if (cnn.Update(val, cond, "we_work") <= 0)
                        {
                            cnn.RollbackTransaction();
                            return new DataTable();
                        }
                        status_hientai = cnn.ExecuteScalar("select status from we_work where id_row = " + workid + "");
                    }
                    long position = long.Parse(cnn.ExecuteScalar("select position from we_status where id_row = " + status_hientai + " and id_project_team =" + id_project_team).ToString());
                    string sql = @"";
                    sql = @$"select process.id_project_team, workid, process.statusid, process.id_row as processid
                        , process.checker, process.change_note , statusname, color, position, _status.id_row
                        , _status.type, isdefault, isfinal, isdeadline, istodo, '' as hoten_follower
                        from we_work_process process right join we_status _status
                        on _status.id_row = process.statusid and workid = " + workid + " " +
                        "and process.disabled = 0 " +
                        "where _status.id_project_team = " + id_project_team + " " +
                        "and _status.disabled = 0 order by position ";
                    bool admin_project = false;
                    object project_team = cnn.ExecuteScalar("select admin from we_project_team_user where id_project_team = " + id_project_team + " and Disabled = 0 and id_user =" + loginData.UserID);
                    if (project_team != null)
                        admin_project = bool.TrueString.Equals(project_team.ToString());
                    bool admin_system = MenuController.CheckGroupAdministrator(loginData.Username, cnn, loginData.CustomerID);
                    bool is_stop = false;
                    dt = cnn.CreateDataTable(sql);
                    dt.Columns.Add("allow_update", typeof(bool));
                    if (dt.Rows.Count > 0)
                    {
                        foreach (DataRow item in dt.Rows)
                        {
                            if (admin_project || admin_system)
                            {
                                item["allow_update"] = true;
                            }
                            else
                            {
                                long value_table = long.Parse(item["position"].ToString());
                                string checker = item["checker"].ToString();
                                if (Math.Abs(value_table - position) == 0) // dòng hiện tại
                                {
                                    item["allow_update"] = true;
                                }
                                if (Math.Abs(value_table - position) > 0) // dòng trên và dòng dưới
                                {
                                    if (Math.Abs(value_table - position) == 1) // dòng trên + 1 và dòng dưới +1
                                    {
                                        item["allow_update"] = true;
                                        if (!string.IsNullOrEmpty(checker) && !checker.Equals(loginData.UserID.ToString()))
                                        {
                                            is_stop = true;
                                        }
                                    }
                                    if (is_stop) // Nếu dòng n-1 và n+1 có người check và không thuộc user đăng nhập thì dừng hẳn
                                    {
                                        if (Math.Abs(value_table - position) > 1) // dòng trên + 1 và dòng dưới +1
                                        {
                                            item["allow_update"] = false;
                                        }
                                    }
                                    else
                                    {
                                        if (Math.Abs(value_table - position) > 1) // dòng trên + 1 và dòng dưới +1
                                        {
                                            if (string.IsNullOrEmpty(checker) || (checker == loginData.UserID.ToString()))
                                            {
                                                item["allow_update"] = true;
                                            }
                                            else
                                                item["allow_update"] = false;
                                        }
                                    }
                                }
                            }
                            var info = DataAccount.Where(x => item["checker"].ToString().Contains(x.UserId.ToString())).FirstOrDefault();
                            if (info != null)
                            {
                                item["hoten_follower"] = info.FullName;
                            }
                        }
                        return dt;
                    }
                    else
                        return new DataTable();
                }
                catch
                {
                    return new DataTable();
                }
            }
        }
        public static async Task<DataSet> GetWork_ClickUp(IHeaderDictionary _header, DpsConnection cnn, QueryParams query, long curUser, List<AccUsernameModel> DataAccount, string listDept, string dieukien_where = "")
        {
            List<string> nvs = DataAccount.Select(x => x.UserId.ToString()).ToList();
            string ListID = string.Join(",", nvs);
            SqlConditions Conds = new SqlConditions();
            Conds.Add("iduser", curUser);
            string dieukienSort = "id_row";
            if (!string.IsNullOrEmpty(query.filter["id_project_team"]))
            {
                dieukien_where += " and id_project_team=@id_project_team";
                Conds.Add("id_project_team", query.filter["id_project_team"]);
            }
            if (!string.IsNullOrEmpty(query.filter["id_nv"]))
            {
                dieukien_where += " and w.id_nv=@id_nv";
                Conds.Add("id_nv", query.filter["id_nv"]);
            }
            #region danh sách department, list status hoàn thành, trễ,đang làm
            string list_Complete = "";
            list_Complete = ReportController.GetListStatusDynamic(listDept, cnn, "IsFinal");
            string list_Deadline = "";
            list_Deadline = ReportController.GetListStatusDynamic(listDept, cnn, "IsDeadline");
            string list_Todo = "";
            list_Todo = ReportController.GetListStatusDynamic(listDept, cnn, "IsTodo");
            #endregion
            #region filter thời gian , keyword
            DateTime from = Common.GetDateTime();
            DateTime to = Common.GetDateTime();
            if (!string.IsNullOrEmpty(query.filter["TuNgay"]))
            {
                DateTime.TryParseExact(query.filter["TuNgay"], "dd/MM/yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out from);
                dieukien_where += " and w.CreatedDate>=@from";
                Conds.Add("from", JeeWorkLiteController.GetUTCTime(_header, from.ToString()));
            }
            if (!string.IsNullOrEmpty(query.filter["DenNgay"]))
            {
                DateTime.TryParseExact(query.filter["DenNgay"], "dd/MM/yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out to);
                to = to.AddDays(1);
                dieukien_where += " and w.CreatedDate<@to";
                Conds.Add("to", JeeWorkLiteController.GetUTCTime(_header, to.ToString()));
            }
            if (!string.IsNullOrEmpty(query.filter["keyword"]))
            {
                dieukien_where += " and (w.title like N'%@keyword%' or w.description like N'%@keyword%')";
                dieukien_where = dieukien_where.Replace("@keyword", query.filter["keyword"]);
            }
            #endregion
            #region Sort data theo các dữ liệu bên dưới
            Dictionary<string, string> sortableFields = new Dictionary<string, string>
                        {
                            { "id_row", "id_row"},
                            { "title", "title"},
                            { "CreatedDate", "CreatedDate"},
                            {"UpdatedDate","UpdatedDate" },
                            {"deadline","deadline" },
                            {"end_date","end_date" },
                            {"important","important" },
                            {"prioritize","prioritize" },
                            {"urgent","urgent" }
                        };
            #endregion
            if (!string.IsNullOrEmpty(query.sortField) && sortableFields.ContainsKey(query.sortField))
                dieukienSort = sortableFields[query.sortField] + ("desc".Equals(query.sortOrder) ? " desc" : " asc");
            #region Trả dữ liệu về backend để hiển thị lên giao diện
            string sqlq = @$"select Distinct w.*, '' as hoten_nguoigiao
, Iif(fa.id_row is null ,0,1) as favourite 
,coalesce( f.count,0) as num_file,coalesce( com.count,0) as num_com,
iIf(w.Status in (" + list_Complete + @") and w.end_date>w.deadline,1,0) as is_htquahan,
iIf(w.Status  in (" + list_Complete + @") and (w.end_date <= w.deadline or w.end_date is null or w.deadline is null),1,0) as is_htdunghan,
iIf(w.Status not in (" + list_Complete + "," + list_Deadline + @") , 1, 0) as is_danglam, 
iIf(w.Status in (" + list_Deadline + @$") , 1, 0) as is_quahan,
iif(convert(varchar, w.deadline,103) like convert(varchar, GETUTCDATE(),103),1,0) as duetoday,
iif(w.status=1 and w.start_date is null,1,0) as require,
'' as NguoiTao, '' as NguoiSua from v_wework_clickup_new w 
left join (select count(*) as count,object_id from we_attachment where object_type=1 group by object_id) f on f.object_id=w.id_row
left join (select count(*) as count,object_id from we_comment where object_type=1 group by object_id) com on com.object_id=w.id_row
left join we_work_favourite fa 
on fa.id_work=w.id_row and fa.createdby=@iduser and fa.disabled=0
where 1=1 and w.CreatedBy in ({ListID}) " + dieukien_where + "  order by " + dieukienSort;
            sqlq += ";select id_work, id_tag,color, title " +
                "from we_work_tag wt join we_tag t on wt.id_tag=t.id_row where wt.Disabled=0";
            if (!string.IsNullOrEmpty(query.filter["id_project_team"]))
                sqlq += " and id_project_team=" + query.filter["id_project_team"];
            //người theo dõi
            sqlq += @$";select id_work,u.id_user ,'' as hoten from we_work_user u 
where u.disabled = 0 and u.id_user in ({ListID}) and u.loai = 2";
            DataSet ds = cnn.CreateDataSet(sqlq, Conds);

            #region Map info account từ JeeAccount
            foreach (DataRow item in ds.Tables[0].Rows)
            {
                var infoNguoiGiao = DataAccount.Where(x => item["nguoigiao"].ToString().Contains(x.UserId.ToString())).FirstOrDefault();
                var infoNguoiTao = DataAccount.Where(x => item["CreatedBy"].ToString().Contains(x.UserId.ToString())).FirstOrDefault();
                var infoNguoiSua = DataAccount.Where(x => item["UpdatedBy"].ToString().Contains(x.UserId.ToString())).FirstOrDefault();

                if (infoNguoiGiao != null)
                {
                    item["hoten_nguoigiao"] = infoNguoiGiao.FullName;
                }
                if (infoNguoiTao != null)
                {
                    item["NguoiTao"] = infoNguoiTao.Username;
                }
                if (infoNguoiSua != null)
                {
                    item["NguoiSua"] = infoNguoiSua.Username;
                }
            }
            foreach (DataRow item in ds.Tables[2].Rows)
            {
                var info = DataAccount.Where(x => item["id_user"].ToString().Contains(x.UserId.ToString())).FirstOrDefault();

                if (info != null)
                {
                    item["hoten"] = info.FullName;
                }
            }
            #endregion
            return ds;
            #endregion
        }
        public static DataSet GetWorkByEmployee(IHeaderDictionary _header, DpsConnection cnn, QueryParams query, long curUser, List<AccUsernameModel> DataAccount, string dieukien_where = "", string order_by = "")
        {
            //List<string> nvs = DataAccount.Select(x => x.UserId.ToString()).ToList();
            SqlConditions Conds = new SqlConditions();
            Conds.Add("iduser", curUser);
            #region Code filter
            string dieukienSort = "w.createddate";
            switch (order_by)
            {
                case "CreatedDate_Giam":
                    {
                        dieukienSort = "w.createddate desc";
                        break;
                    }
                case "CreatedDate_Tang":
                    {
                        dieukienSort = "w.createddate";
                        break;
                    }
                case "Prioritize_Cao":
                    {
                        dieukienSort = "w.clickup_prioritize";
                        break;
                    }
                case "Prioritize_Thap":
                    {
                        dieukienSort = "w.clickup_prioritize";
                        break;
                    }
                case "Deadline_Tang":
                    {
                        dieukienSort = "w.deadline";
                        break;
                    }
                case "Deadline_Giam":
                    {
                        dieukienSort = "w.deadline desc";
                        break;
                    }
                default:
                    {
                        dieukienSort = "w.createddate";
                        break;
                    }
            }
            if (!string.IsNullOrEmpty(query.filter["id_project_team"]))
            {
                dieukien_where += " and id_project_team=@id_project_team";
                Conds.Add("id_project_team", query.filter["id_project_team"]);
            }
            if (!string.IsNullOrEmpty(query.filter["id_nv"]))
            {
                Conds.Add("id_nv", query.filter["id_nv"]);
            }
            DateTime from = Common.GetDateTime();
            DateTime to = Common.GetDateTime();
            Dictionary<string, string> collect = new Dictionary<string, string>
                        {
                            { "CreatedDate", "CreatedDate"},
                            { "Deadline", "deadline"},
                            { "StartDate", "start_date"}
                        };
            string collect_by = "CreatedDate";
            if (!string.IsNullOrEmpty(query.filter["collect_by"]))
                collect_by = collect[query.filter["collect_by"]];
            if (!string.IsNullOrEmpty(query.filter["TuNgay"]))
            {
                DateTime.TryParseExact(query.filter["TuNgay"], "dd/MM/yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out from);
                dieukien_where += " and w." + collect_by + ">=@from";
                Conds.Add("from", JeeWorkLiteController.GetUTCTime(_header, from.ToString()));
            }
            if (!string.IsNullOrEmpty(query.filter["DenNgay"]))
            {
                DateTime.TryParseExact(query.filter["DenNgay"], "dd/MM/yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out to);
                to = to.AddDays(1);
                dieukien_where += " and w." + collect_by + "<@to";
                Conds.Add("to", JeeWorkLiteController.GetUTCTime(_header, to.ToString()));
            }
            int nam = DateTime.Today.Year;
            int thang = DateTime.Today.Month;
            var lastDayOfMonth = DateTime.DaysInMonth(nam, thang);
            if (!string.IsNullOrEmpty(query.filter["Thang"]))
            {
                thang = int.Parse(query.filter["Thang"]);
            }
            if (!string.IsNullOrEmpty(query.filter["Nam"]))
            {
                nam = int.Parse(query.filter["Nam"]);
            }
            if (!string.IsNullOrEmpty(query.filter["Thang"]) && !string.IsNullOrEmpty(query.filter["Nam"]))
            {
                from = new DateTime(nam, thang, 1, 0, 0, 1);
                to = GetEndDateInMonth(thang, nam);
                dieukien_where += " and w." + collect_by + ">=@from";
                Conds.Add("from", JeeWorkLiteController.GetUTCTime(_header, from.ToString()));
                dieukien_where += " and w." + collect_by + "<@to";
                Conds.Add("to", JeeWorkLiteController.GetUTCTime(_header, to.ToString()));
            }
            if (!string.IsNullOrEmpty(query.filter["keyword"]))
            {
                dieukien_where += " and (w.title like N'%@keyword%' or w.description like N'%@keyword%')";
                dieukien_where = dieukien_where.Replace("@keyword", query.filter["keyword"]);
            }
            #endregion
            #region Sort data follow data below
            Dictionary<string, string> sortableFields = new Dictionary<string, string>
                        {
                            { "id_row", "id_row"},
                            { "title", "title"},
                            { "CreatedDate", "CreatedDate"},
                            {"UpdatedDate","UpdatedDate" },
                            {"deadline","deadline" },
                            {"end_date","end_date" },
                            {"important","important" },
                            {"prioritize","prioritize" },
                            {"urgent","urgent" }
                        };
            #endregion
            if (!string.IsNullOrEmpty(query.sortField) && sortableFields.ContainsKey(query.sortField))
                dieukienSort = sortableFields[query.sortField] + ("desc".Equals(query.sortOrder) ? " desc" : " asc");
            #region Return data to backend to display on the interface
            string sqlq = @$"select  distinct w.id_row,w.title,w.description,w.id_project_team,w.id_group,w.estimates
                            ,w.deadline,w.id_milestone, w.milestone,
                            w.id_parent,w.start_date,w.end_date,w.urgent,w.important,w.prioritize
                            ,w.status,w.result,w.createddate,w.createdby,
                            w.UpdatedDate,w.UpdatedBy, w.project_team,w.id_department
                            , w.clickup_prioritize, w.nguoigiao,'' as hoten_nguoigiao, Id_NV,''as hoten
                            , Iif(fa.id_row is null ,0,1) as favourite 
                            ,coalesce( f.count,0) as num_file, coalesce( com.count,0) as num_com
                            ,'' as NguoiTao, '' as NguoiSua 
                            , w.accepted_date, w.activated_date, w.closed_date, w.state_change_date,
                            w.activated_by, w.closed_by, w.closed, w.closed_work_date, w.closed_work_by
                            ,iIf(w.deadline < GETUTCDATE() and w.deadline is not null and w.end_date is null  ,1,0) as TreHan -- Trễ hạn: Ngày kết thúc is null và deadline is not null và deadline < GETUTCDATE()
                            ,iIf(w.end_date is not null ,1,0) as Done --Hoàn thành: Ngày kết thúc is not null và deadline is not null và deadline < GETUTCDATE()
                            ,iIf(((deadline >= GETUTCDATE() and deadline is not null) or deadline is null) and w.end_date is null ,1,0) as Doing -- Đang làm: Ngày kết thúc is null và deadline is not null và deadline => GETUTCDATE()
                            from v_wework_new w 
                            left join (select count(*) as count,object_id 
                            from we_attachment where object_type=1 group by object_id) f on f.object_id=w.id_row
                            left join (select count(*) as count,object_id
                            from we_comment where object_type=1 group by object_id) com on com.object_id=w.id_row
                            left join we_work_favourite fa 
                            on fa.id_work=w.id_row and fa.createdby=@iduser and fa.disabled=0
                            where 1=1 " + dieukien_where + "  order by " + dieukienSort;
            sqlq += ";select id_work, id_tag,color, title " +
                "from we_work_tag wt join we_tag t on wt.id_tag=t.id_row where wt.disabled=0 and t.disabled=0";
            string where_string = "";
            if (!string.IsNullOrEmpty(query.filter["id_project_team"]))
            {
                where_string = " and id_project_team=" + query.filter["id_project_team"];
            }
            if (!string.IsNullOrEmpty(query.filter["status"]))
            {
                dieukien_where += " and status=" + query.filter["status"];
            }
            sqlq += where_string;
            //người theo dõi
            sqlq += @$";select id_work,id_user,'' as hoten from we_work_user u 
                        where u.disabled = 0 and u.loai = 2";
            DataSet ds = cnn.CreateDataSet(sqlq, Conds);
            ds.Tables[0].Columns.Add("PhanLoai");
            ds.Tables[0].Columns.Add("ActivityDate", typeof(DateTime));
            string sql_activity = @"SELECT b.maxdate, a.*
                                            from we_work a
                                            join
                                                (
                                                    select object_id, MAX(CreatedDate) maxdate
                                                    from we_log
                                                    where id_action in (select id_row from we_log_action where object_type = 1)
                                                    group by object_id
                                                ) b on a.id_row = b.object_id
                                            where a.disabled = 0 " + where_string + " " +
                                    "order by maxdate desc";
            DataTable dt_activity = cnn.CreateDataTable(sql_activity);
            #endregion
            #region Map info account từ JeeAccount
            foreach (DataRow item in ds.Tables[0].Rows)
            {
                var infoNguoiTao = DataAccount.Where(x => item["CreatedBy"].ToString().Contains(x.UserId.ToString())).FirstOrDefault();
                var infoNguoiSua = DataAccount.Where(x => item["UpdatedBy"].ToString().Contains(x.UserId.ToString())).FirstOrDefault();
                var infonguoigiao = DataAccount.Where(x => item["nguoigiao"].ToString().Contains(x.UserId.ToString())).FirstOrDefault();
                var infoId_NV = DataAccount.Where(x => item["Id_NV"].ToString().Contains(x.UserId.ToString())).FirstOrDefault();
                if (infoNguoiTao != null)
                {
                    item["NguoiTao"] = infoNguoiTao.Username;
                }
                if (infoNguoiSua != null)
                {
                    item["NguoiSua"] = infoNguoiSua.Username;
                }
                if (infonguoigiao != null)
                {
                    item["hoten_nguoigiao"] = infonguoigiao.FullName;
                }
                if (infoId_NV != null)
                {
                    item["hoten"] = infoId_NV.FullName;
                }
                if (!string.IsNullOrEmpty(query.filter["sort_activity"]))
                {
                    if (dt_activity.Rows.Count > 0)
                    {
                        DataRow[] dr = dt_activity.Select("id_row =" + item["id_row"].ToString());
                        if (dr.Length > 0)
                        {
                            item["ActivityDate"] = Convert.ToDateTime(dr[0]["maxdate"]);
                        }
                    }
                }
                else
                    item["ActivityDate"] = Convert.ToDateTime(item["createddate"]);
            }
            foreach (DataRow item in ds.Tables[2].Rows)
            {
                var info = DataAccount.Where(x => item["id_user"].ToString().Contains(x.UserId.ToString())).FirstOrDefault();
                if (info != null)
                {
                    item["hoten"] = info.FullName;
                }
            }
            #endregion
            return ds;
        }
        private DataSet getWork_IDNV(IHeaderDictionary _header, DpsConnection cnn, QueryParams query, long curUser, List<AccUsernameModel> DataAccount, string dieukien_where = "")
        {
            List<string> nvs = DataAccount.Select(x => x.UserId.ToString()).ToList();
            string ListID = string.Join(",", nvs);

            SqlConditions Conds = new SqlConditions();
            Conds.Add("iduser", curUser);
            string dieukienSort = "id_row";
            if (!string.IsNullOrEmpty(query.filter["id_project_team"]))
            {
                dieukien_where += " and id_project_team=@id_project_team";
                Conds.Add("id_project_team", query.filter["id_project_team"]);
            }
            if (!string.IsNullOrEmpty(query.filter["id_nv"]))
            {
                //dieukien_where += " and w.id_nv=@id_nv";
                //dieukien_where += dieukien_where;
                Conds.Add("id_nv", query.filter["id_nv"]);
            }

            #region filter thời gian, keyword
            DateTime from = Common.GetDateTime();
            DateTime to = Common.GetDateTime();
            if (!string.IsNullOrEmpty(query.filter["TuNgay"]))
            {
                DateTime.TryParseExact(query.filter["TuNgay"], "dd/MM/yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out from);
                dieukien_where += " and w.CreatedDate>=@from";
                Conds.Add("from", JeeWorkLiteController.GetUTCTime(_header, from.ToString()));
            }
            if (!string.IsNullOrEmpty(query.filter["DenNgay"]))
            {
                DateTime.TryParseExact(query.filter["DenNgay"], "dd/MM/yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out to);
                to = to.AddDays(1);
                dieukien_where += " and w.CreatedDate<@to";
                Conds.Add("to", JeeWorkLiteController.GetUTCTime(_header, to.ToString()));
            }
            if (!string.IsNullOrEmpty(query.filter["keyword"]))
            {
                dieukien_where += " and (w.title like N'%@keyword%' or w.description like N'%@keyword%' )";
                dieukien_where = dieukien_where.Replace("@keyword", query.filter["keyword"]);
            }
            #endregion
            #region Sort data theo các dữ liệu bên dưới
            Dictionary<string, string> sortableFields = new Dictionary<string, string>
                        {
                            { "id_row", "id_row"},
                            { "title", "title"},
                            { "CreatedDate", "CreatedDate"},
                            {"UpdatedDate","UpdatedDate" },
                            {"deadline","deadline" },
                            {"end_date","end_date" },
                            {"important","important" },
                            {"prioritize","prioritize" },
                            {"urgent","urgent" }
                        };
            #endregion
            if (!string.IsNullOrEmpty(query.sortField) && sortableFields.ContainsKey(query.sortField))
                dieukienSort = sortableFields[query.sortField] + ("desc".Equals(query.sortOrder) ? " desc" : " asc");
            #region Trả dữ liệu về backend để hiển thị lên giao diện
            //, nv.holot+' '+nv.ten as hoten_nguoigiao -- w.NguoiGiao,
            string sqlq = @$"select  distinct w.id_row,w.title,w.description,w.id_project_team,w.id_group,w.deadline,w.id_milestone,w.milestone,w.Id_NV,w.estimates,
w.id_parent,w.start_date,w.end_date,w.urgent,w.important,w.prioritize,w.status,w.result,w.CreatedDate,w.CreatedBy,
w.UpdatedDate,w.UpdatedBy, w.project_team,w.id_department,w.clickup_prioritize 
, Iif(fa.id_row is null ,0,1) as favourite 
,coalesce( f.count,0) as num_file,coalesce( com.count,0) as num_com,
'' as NguoiTao, '' as NguoiSua 
, w.accepted_date, w.activated_date, w.closed_date, w.state_change_date,
w.activated_by, w.closed_by, closed, closed_work_date, closed_work_by
,isnull((select count(*) from we_work v where deadline < GETUTCDATE() and deadline is not null and w.id_row = v.id_row and v.end_date is null),0) as TreHan-- Trễ hạn: Ngày kết thúc is null và deadline is not null và deadline < GETUTCDATE()
,isnull((select count(*) from we_work v where deadline < GETUTCDATE() and deadline is not null and w.id_row = v.id_row and v.end_date is not null),0) as done --Hoàn thành: Ngày kết thúc is not null và deadline is not null và deadline < GETUTCDATE()
,isnull((select count(*) from we_work v where ((deadline >= GETUTCDATE() and deadline is not null) or deadline is null) and w.id_row = v.id_row and v.end_date is null),0) as Doing--Đang làm: Ngày kết thúc is null và deadline is not null và deadline => GETUTCDATE()
from v_wework_new w 
left join (select count(*) as count,object_id 
from we_attachment where object_type=1 group by object_id) f on f.object_id=w.id_row
left join (select count(*) as count,object_id 
from we_comment where object_type=1 group by object_id) com on com.object_id=w.id_row
left join we_work_favourite fa 
on fa.id_work=w.id_row and fa.createdby=@iduser and fa.disabled=0
where 1=1 " + dieukien_where + "  order by " + dieukienSort;
            sqlq += ";select id_work, id_tag,color, title " +
                "from we_work_tag wt join we_tag t on wt.id_tag=t.id_row where wt.Disabled=0";
            string where_id_project_team = "";
            if (!string.IsNullOrEmpty(query.filter["id_project_team"]))
            {
                where_id_project_team = " and id_project_team=" + query.filter["id_project_team"];
            }
            sqlq += where_id_project_team;
            //người theo dõi
            sqlq += @$";select id_work,'' as hoten,id_user from we_work_user u 
where u.disabled = 0 and u.loai = 2";

            DataSet ds = cnn.CreateDataSet(sqlq, Conds);
            ds.Tables[0].Columns.Add("ActivityDate");
            string sql_activity = @"SELECT b.maxdate, a.*
                                            from we_work a
                                            join
                                                (
                                                    select object_id, MAX(CreatedDate) maxdate
                                                    from we_log
                                                    where id_action in (select id_row from we_log_action where object_type = 1)
                                                    group by object_id
                                                ) b on a.id_row = b.object_id
                                            where a.disabled = 0 " + where_id_project_team + " " +
                                    "order by maxdate desc";
            DataTable dt_activity = cnn.CreateDataTable(sql_activity);
            #region Map info account từ JeeAccount
            foreach (DataRow item in ds.Tables[0].Rows)
            {
                var infoNguoiTao = DataAccount.Where(x => item["CreatedBy"].ToString().Contains(x.UserId.ToString())).FirstOrDefault();
                var infoNguoiSua = DataAccount.Where(x => item["UpdatedBy"].ToString().Contains(x.UserId.ToString())).FirstOrDefault();
                if (infoNguoiTao != null)
                {
                    item["NguoiTao"] = infoNguoiTao.Username;
                }
                if (infoNguoiSua != null)
                {
                    item["NguoiSua"] = infoNguoiSua.Username;
                }
                if (!string.IsNullOrEmpty(query.filter["sort_activity"]))
                {
                    if (dt_activity.Rows.Count > 0)
                    {
                        DataRow[] dr = dt_activity.Select("id_row =" + item["id_row"].ToString());
                        if (dr.Length > 0)
                        {
                            item["ActivityDate"] = dr[0]["maxdate"].ToString();
                        }
                    }
                }
                else
                    item["ActivityDate"] = item["CreatedDate"];
            }
            foreach (DataRow item in ds.Tables[2].Rows)
            {
                var info = DataAccount.Where(x => item["id_user"].ToString().Contains(x.UserId.ToString())).FirstOrDefault();

                if (info != null)
                {
                    item["hoten"] = info.FullName;
                }
            }

            #endregion
            return ds;
            #endregion
        }
        public static object getChild(string domain, long IdKHDPS, string columnName, string displayChild, object id, EnumerableRowCollection<DataRow> temp, EnumerableRowCollection<DataRow> tags, List<AccUsernameModel> DataAccount, UserJWT loginData, string ConnectString, object parent = null)
        {
            object a = "";
            if (parent == null)
                parent = DBNull.Value;
            else
            {
                a = parent;
            }
            // get user Id 
            DataTable dt_Users = new DataTable();
            DataTable dt_User2 = new DataTable();
            DataTable dt_status = new DataTable();
            using (DpsConnection cnn = new DpsConnection(ConnectString))
            {
                SqlConditions conds = new SqlConditions();
                conds.Add("w_user.disabled", 0);
                string select_user = $@"select distinct w_user.id_user,'' as hoten,'' as email,'' as image, id_work,w_user.loai,w_user.CreatedBy
                                        from we_work_user w_user 
                                        join we_work on we_work.id_row = w_user.id_work where (where)";
                if ("id_project_team".Equals(columnName))
                {
                    select_user += " and id_work in (select id_row from we_work where id_project_team = " + id + ")";
                }
                dt_status = JeeWorkLiteController.StatusDynamic(long.Parse(id.ToString()), DataAccount, cnn);
                dt_Users = cnn.CreateDataTable(select_user, "(where)", conds);
                #region Map info account từ JeeAccount
                if (dt_Users.Rows.Count > 0)
                {
                    foreach (DataRow item in dt_Users.Rows)
                    {
                        var info = DataAccount.Where(x => item["id_user"].ToString().Contains(x.UserId.ToString())).FirstOrDefault();
                        if (info != null)
                        {
                            item["hoten"] = info.FullName;
                            item["image"] = info.AvartarImgURL;
                            item["email"] = info.Email;
                        }
                    }
                }
                #endregion
                if (columnName.ToLower() == "id_nv")
                {
                    DataRow[] dr = dt_Users.Select("id_user=" + id);
                    if (dr.Length > 0)
                    {
                        dt_User2 = dr.CopyToDataTable();
                    }
                }
            }
            //columnName="" : không group
            // update lại data khi sửa từ wiget wiget thì bỏ đi phần này : ----- && (columnName == "" || (columnName != "" && r[columnName].Equals(id)))
            // k có phần này thì workclickup lấy dữ liệu không map theo id dự án
            var re = from r in temp
                     where r["id_parent"].Equals(parent) && (columnName == "" || (columnName != "" && r[columnName].ToString().Equals(id.ToString()))) //(parent == null &&  r[columnName].Equals(id)) || (r["id_parent"].Equals(parent) && parent != null)
                     orderby r["ActivityDate"] descending
                     select new
                     {
                         id_parent = r["id_parent"],
                         id_row = r["id_row"],
                         title = r["title"],
                         description = r["description"],
                         id_project_team = r["id_project_team"],
                         project_team = r["project_team"],
                         deadline = r["deadline"],
                         end_date = r["end_date"],
                         urgent = r["urgent"],
                         important = r["important"],
                         start_date = r["start_date"],
                         prioritize = r["prioritize"],
                         favourite = r["favourite"],
                         status = r["status"],
                         id_milestone = r["id_milestone"],
                         milestone = r["milestone"],
                         trehan = r["TreHan"],
                         estimates = r["estimates"],
                         hoanthanh = r["done"],
                         danglam = r["Doing"],
                         closed = r["closed"],
                         closed_work_date = r["closed_work_date"],
                         closed_work_by = r["closed_work_by"],
                         accepted_date = r["accepted_date"] == DBNull.Value ? "" : r["accepted_date"],
                         activated_by = r["activated_by"].Equals(DBNull.Value) ? new { } : JeeWorkLiteController.Get_InfoUsers(r["activated_by"].ToString(), DataAccount),
                         activated_date = r["activated_date"] == DBNull.Value ? "" : r["activated_date"],
                         closed_by = r["closed_by"].Equals(DBNull.Value) ? new { } : JeeWorkLiteController.Get_InfoUsers(r["closed_by"].ToString(), DataAccount),
                         closed_date = r["closed_date"] == DBNull.Value ? "" : r["closed_date"],
                         state_change_date = r["state_change_date"] == DBNull.Value ? "" : r["state_change_date"],
                         createddate = r["CreatedDate"],
                         createdby = r["CreatedBy"].Equals(DBNull.Value) ? new { } : JeeWorkLiteController.Get_InfoUsers(r["CreatedBy"].ToString(), DataAccount),
                         nguoitao = r["NguoiTao"],
                         updateddate = r["UpdatedDate"] == DBNull.Value ? "" : r["UpdatedDate"],
                         updatedby = r["UpdatedBy"].Equals(DBNull.Value) ? new { } : JeeWorkLiteController.Get_InfoUsers(r["UpdatedBy"].ToString(), DataAccount),
                         nguoisua = r["NguoiSua"],
                         clickup_prioritize = r["clickup_prioritize"],
                         activity_date = r["ActivityDate"],
                         comments = SoluongComment(r["id_row"].ToString(), ConnectString),  // SL bình luận
                         status_info = JeeWorkLiteController.get_info_status(r["status"].ToString(), ConnectString),
                         DataStatus = list_status_user(r["id_row"].ToString(), r["id_project_team"].ToString(), loginData, ConnectString, DataAccount),
                         User = from us in dt_Users.AsEnumerable()
                                where r["id_row"].Equals(us["id_work"]) && long.Parse(us["loai"].ToString()).Equals(1)
                                select new
                                {
                                    id_nv = us["id_user"],
                                    hoten = us["hoten"],
                                    image = us["image"],
                                    email = us["email"],
                                    loai = us["loai"],
                                },
                         NguoiGiao = from us in dt_Users.AsEnumerable()
                                     where r["id_row"].ToString().Equals(us["id_work"].ToString()) && long.Parse(us["loai"].ToString()).Equals(1)
                                     select us["CreatedBy"].Equals(DBNull.Value) ? new { } : JeeWorkLiteController.Get_InfoUsers(us["CreatedBy"].ToString(), DataAccount),
                         UsersInfo = from us in dt_Users.AsEnumerable()
                                     where r["id_row"].Equals(us["id_work"]) && long.Parse(us["loai"].ToString()).Equals(1)
                                     select us["id_user"].Equals(DBNull.Value) ? new { } : JeeWorkLiteController.Get_InfoUsers(us["id_user"].ToString(), DataAccount),
                         Follower = from us in dt_Users.AsEnumerable()
                                    where r["id_row"].Equals(us["id_work"]) && long.Parse(us["loai"].ToString()).Equals(2)
                                    select new
                                    {
                                        id_nv = us["id_user"],
                                        hoten = us["hoten"],
                                        image = us["image"],
                                        email = us["email"],
                                        loai = us["loai"],
                                    },
                         FollowerInfo = from us in dt_Users.AsEnumerable()
                                        where r["id_row"].Equals(us["id_work"]) && long.Parse(us["loai"].ToString()).Equals(2)
                                        select new
                                        {
                                            data = us["id_user"].Equals(DBNull.Value) ? new { } : JeeWorkLiteController.Get_InfoUsers(us["id_user"].ToString(), DataAccount),
                                        },
                         Tags = from t in tags
                                where r["id_row"].Equals(t["id_work"])
                                select new
                                {
                                    id_row = t["id_tag"],
                                    title = t["title"],
                                    color = t["color"]
                                },
                         Status = from s in dt_status.AsEnumerable()
                                  select new
                                  {
                                      id_row = s["id_row"],
                                      statusname = s["StatusName"],
                                      description = s["description"],
                                      id_project_team = s["id_project_team"],
                                      typeid = s["type"],
                                      color = s["color"],
                                      position = s["position"],
                                      IsFinal = s["isfinal"],
                                      IsDeadline = s["isdeadline"],
                                      IsDefault = s["Isdefault"],
                                      IsToDo = s["istodo"]
                                  },
                         Childs = displayChild == "0" ? new List<string>() : getChild(domain, IdKHDPS, columnName, displayChild == "1" ? "0" : "2", id, temp, tags, DataAccount, loginData, ConnectString, r["id_row"])
                     };
            return re;
        }
        public static object GetChildTask(string domain, long IdKHDPS, string columnName, string displayChild, object id, EnumerableRowCollection<DataRow> temp, EnumerableRowCollection<DataRow> tags, List<AccUsernameModel> DataAccount, UserJWT loginData, string ConnectionString, DataTable dt_Users, object parent = null)
        {
            long new_parent = 0;
            if (parent == null)
                parent = DBNull.Value;
            else
            {
                new_parent = long.Parse(parent.ToString());
            }
            using (DpsConnection cnn = new DpsConnection(ConnectionString))
            {
                var re = from r in temp
                         where (r["id_parent"].ToString().Equals(parent.ToString())) && (new_parent > 0 ? new_parent > 0 : (r[columnName].ToString().Equals(id.ToString())))
                         select new
                         {
                             id_parent = r["id_parent"],
                             id_row = r["id_row"],
                             title = r["title"],
                             description = r["description"],
                             id_project_team = r["id_project_team"],
                             project_team = r["project_team"],
                             deadline = r["deadline"],
                             end_date = r["end_date"],
                             urgent = r["urgent"],
                             important = r["important"],
                             start_date = r["start_date"],
                             prioritize = r["prioritize"],
                             id_group = r["id_group"],
                             status = r["status"],
                             id_milestone = r["id_milestone"],
                             milestone = r["milestone"],
                             num_comment = r["num_comment"],
                             estimates = r["estimates"],
                             closed = r["closed"],
                             closed_work_date = r["closed_work_date"],
                             closed_work_by = r["closed_work_by"],
                             activated_by = r["activated_by"].Equals(DBNull.Value) ? new { } : JeeWorkLiteController.Get_InfoUsers(r["activated_by"].ToString(), DataAccount),
                             accepted_date = r["accepted_date"] == DBNull.Value ? "" : r["accepted_date"],
                             id_nv = r["id_nv"].Equals(DBNull.Value) ? new { } : JeeWorkLiteController.Get_InfoUsers(r["id_nv"].ToString(), DataAccount),
                             activated_date = r["activated_date"] == DBNull.Value ? "" : r["activated_date"],
                             closed_by = r["closed_by"].Equals(DBNull.Value) ? new { } : JeeWorkLiteController.Get_InfoUsers(r["closed_by"].ToString(), DataAccount),
                             closed_date = r["closed_date"] == DBNull.Value ? "" : r["closed_date"],
                             state_change_date = r["state_change_date"] == DBNull.Value ? "" : r["state_change_date"],
                             createddate = r["CreatedDate"],
                             createdby = r["CreatedBy"].Equals(DBNull.Value) ? new { } : JeeWorkLiteController.Get_InfoUsers(r["CreatedBy"].ToString(), DataAccount),
                             updateddate = r["UpdatedDate"] == DBNull.Value ? "" : r["UpdatedDate"],
                             updatedby = r["UpdatedBy"].Equals(DBNull.Value) ? new { } : JeeWorkLiteController.Get_InfoUsers(r["UpdatedBy"].ToString(), DataAccount),
                             clickup_prioritize = r["clickup_prioritize"],
                             comments = SoluongComment(r["id_row"].ToString(), ConnectionString),
                             children = displayChild == "0" ? new List<string>() : GetChildTask(domain, IdKHDPS, columnName, displayChild == "1" ? "0" : "2", id, temp, tags, DataAccount, loginData, ConnectionString, dt_Users, r["id_row"]),
                             Users = from us in dt_Users.AsEnumerable()
                                     where r["id_row"].ToString().Equals(us["id_work"].ToString())
                                     select us["id_user"].Equals(DBNull.Value) ? new { } : JeeWorkLiteController.Get_InfoUsers(us["id_user"].ToString(), DataAccount),
                             User = from us in dt_Users.AsEnumerable()
                                    where r["id_row"].Equals(us["id_work"]) && long.Parse(us["loai"].ToString()).Equals(1)
                                    select new
                                    {
                                        id_nv = us["id_user"],
                                        hoten = us["hoten"],
                                        image = us["image"],
                                        email = us["email"],
                                        loai = us["loai"],
                                    },
                             Tags = from t in tags
                                    where r["id_row"].ToString().Equals(t["id_work"].ToString())
                                    select new
                                    {
                                        id_row = t["id_tag"],
                                        title = t["title"],
                                        color = t["color"]
                                    },
                             DataStatus = list_status_user(r["id_row"].ToString(), r["id_project_team"].ToString(), loginData, ConnectionString, DataAccount),
                         };
                return re.Distinct().ToList(); // .OrderByDescending(x => x.createddate);
            }
        }
        public static long SoluongComment(string idwork, string ConnectionString)
        {
            using (DpsConnection cnn = new DpsConnection(ConnectionString))
            {
                long comment = long.Parse(cnn.ExecuteScalar("select iif(num_comment is null , 0 , num_comment) from we_work WHERE id_row =  " + idwork).ToString());
                return comment;
            }
        }
        public static EnumerableRowCollection<DataRow> filterWork(EnumerableRowCollection<DataRow> enumerableRowCollections, FilterModel filter)
        {
            var temp = enumerableRowCollections;
            #region filter
            if (!string.IsNullOrEmpty(filter["status"]))
                temp = temp.Where(x => x["status"].ToString() == filter["status"]);
            if (!string.IsNullOrEmpty(filter["is_quahan"]))//quá hạn
                temp = temp.Where(x => x["is_quahan"].ToString() == filter["is_quahan"]);
            if (!string.IsNullOrEmpty(filter["is_htquahan"]))//hoàn thành quá hạn
                temp = temp.Where(x => x["is_htquahan"].ToString() == filter["is_htquahan"]);
            if (!string.IsNullOrEmpty(filter["assign"]))//giao cho tôi thì truyền id của user hiện tại
                temp = temp.Where(x => x["id_nv"].ToString() == filter["assign"]);
            if (!string.IsNullOrEmpty(filter["nguoigiao"]))//filter theo người giao
                temp = temp.Where(x => x["nguoigiao"].ToString() == filter["nguoigiao"]);
            if (!string.IsNullOrEmpty(filter["important"]))//quan trọng
                temp = temp.Where(x => x["important"].ToString().Contains(filter["important"]));
            if (!string.IsNullOrEmpty(filter["urgent"]))//khẩn cấp
                temp = temp.Where(x => x["urgent"].ToString().Contains(filter["urgent"]));
            if (!string.IsNullOrEmpty(filter["prioritize"]))//ưu tiên
                temp = temp.Where(x => x["prioritize"].ToString().Contains(filter["prioritize"]));
            if (!string.IsNullOrEmpty(filter["favourite"]))//gắn dấu *
                temp = temp.Where(x => x["favourite"].ToString().Contains(filter["favourite"]));
            if (!string.IsNullOrEmpty(filter["require"]))//phải làm
                temp = temp.Where(x => x["require"].ToString().Contains(filter["require"]));
            if (!string.IsNullOrEmpty(filter["is_danglam"]))//đang làm
                temp = temp.Where(x => x["is_danglam"].ToString().Contains(filter["is_danglam"]));
            #endregion
            return temp;
        }
        public static EnumerableRowCollection<DataRow> filterTasks(EnumerableRowCollection<DataRow> enumerableRowCollections, FilterModel filter)
        {
            var temp = enumerableRowCollections;
            #region filter
            if (!string.IsNullOrEmpty(filter["New"]))// Task new
                temp = temp.Where(x => x["New"].ToString() == filter["New"]);
            if (!string.IsNullOrEmpty(filter["Done"]))// Task hoàn thành
                temp = temp.Where(x => x["Done"].ToString() == filter["Done"]);
            if (!string.IsNullOrEmpty(filter["Doing"]))// Task đang làm
                temp = temp.Where(x => x["Doing"].ToString().Contains(filter["Doing"]));
            #endregion
            return temp;
        }
        private object GetWorkByProjects(IHeaderDictionary _header, string id_project_team, DataTable dt_fields, string ConnectionString, QueryParams query, UserJWT loginData, List<AccUsernameModel> DataAccount)
        {
            try
            {
                using (DpsConnection cnn = new DpsConnection(ConnectionString))
                {
                    #region filter thời gian, keyword, group by
                    DateTime from = Common.GetDateTime();
                    DateTime to = Common.GetDateTime();
                    string groupby = "status";
                    string tableName = "";
                    string querySQL = "";
                    string data_newfield = "";
                    DataTable dt_filter = new DataTable();
                    DataTable dt_filter_tmp = new DataTable();
                    DataTable dt_new_fields = new DataTable();
                    dt_filter.Columns.Add("id_row", typeof(String));
                    dt_filter.Columns.Add("statusname", typeof(String));
                    dt_filter.Columns.Add("color", typeof(String));
                    dt_filter.Columns.Add("Follower", typeof(String));
                    dt_filter.Columns.Add("description", typeof(String));
                    SqlConditions sqlcondQ = new SqlConditions();
                    sqlcondQ.Add("id_row", id_project_team);
                    sqlcondQ.Add("disabled", 0);
                    string s = "select * from we_project_team where (where)";
                    DataTable old = cnn.CreateDataTable(s, "(where)", sqlcondQ);
                    if (old == null || old.Rows.Count == 0)
                        return JsonResultCommon.KhongTonTai("Dự án");
                    bool rs = Common.CheckRoleByProject(id_project_team, loginData, cnn, ConnectionString);

                    data_newfield = "select * from we_newfileds_values where id_project_team = " + id_project_team + "";
                    #endregion
                    string strW = "";
                    SqlConditions Conds = new SqlConditions();
                    dt_new_fields = cnn.CreateDataTable(data_newfield);
                    Dictionary<string, string> collect = new Dictionary<string, string>
                        {
                            { "CreatedDate", "CreatedDate"},
                            { "Deadline", "deadline"},
                            { "StartDate", "start_date"}
                        };
                    string collect_by = "CreatedDate";
                    if (!string.IsNullOrEmpty(query.filter["collect_by"]))
                        collect_by = collect[query.filter["collect_by"]];
                    if (!string.IsNullOrEmpty(query.filter["TuNgay"]))
                    {
                        DateTime.TryParseExact(query.filter["TuNgay"], "dd/MM/yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out from);
                        strW += " and w." + collect_by + ">=@from";
                        Conds.Add("from", JeeWorkLiteController.GetUTCTime(_header, from.ToString()));
                    }
                    if (!string.IsNullOrEmpty(query.filter["DenNgay"]))
                    {
                        DateTime.TryParseExact(query.filter["DenNgay"], "dd/MM/yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out to);
                        to = to.AddDays(1);
                        strW += " and w." + collect_by + "<@to";
                        Conds.Add("to", JeeWorkLiteController.GetUTCTime(_header, to.ToString()));
                    }
                    if (!string.IsNullOrEmpty(query.filter["keyword"]))
                    {
                        strW += " and (w.title like N'%@keyword%' or w.description like N'%@keyword%')";
                        strW = strW.Replace("@keyword", query.filter["keyword"]);
                    }
                    strW += " and (w.id_project_team = @id_project_team)";
                    strW = strW.Replace("@id_project_team", id_project_team.ToString());
                    SqlConditions cond = new SqlConditions();
                    cond.Add("id_project_team", id_project_team);
                    cond.Add("w_u.disabled", 0);
                    string sql_user = $@"select w_u.id_work, w_u.id_user, w_u.loai, id_child
                                        , w_u.disabled, '' as hoten, id_project_team,'' as image, w.id_parent
                                        from we_work_user w_u 
                                        join we_work w on w.id_row = w_u.id_work 
                                        where (where)";
                    DataTable dt_users = cnn.CreateDataTable(sql_user, "(where)", cond);
                    #region Map info account từ JeeAccount
                    foreach (DataRow item in dt_users.Rows)
                    {
                        var info = DataAccount.Where(x => item["id_user"].ToString().Contains(x.UserId.ToString())).FirstOrDefault();
                        if (info != null)
                        {
                            item["hoten"] = info.FullName;
                            item["image"] = info.AvartarImgURL;
                        }
                    }
                    #endregion
                    if (!string.IsNullOrEmpty(query.filter["groupby"]))
                    {
                        groupby = query.filter["groupby"];
                        switch (groupby)
                        {
                            case "priority":
                                {
                                    dt_filter = new DataTable();
                                    dt_filter.Rows.Add(new object[] { "1", "Urgent", "#fd397a" });
                                    dt_filter.Rows.Add(new object[] { "2", "High", "#ffb822" });
                                    dt_filter.Rows.Add(new object[] { "3", "Normal", "#5578eb" });
                                    dt_filter.Rows.Add(new object[] { "4", "Low", "#74788d " });
                                    break;
                                }
                            case "status":
                                {
                                    tableName = "we_status";
                                    querySQL = "select id_row, statusname, color, follower, description, type, position from " + tableName + " " +
                                        "where disabled = 0 and id_project_team  = " + int.Parse(id_project_team) + " " +
                                        "order by position";
                                    dt_filter = cnn.CreateDataTable(querySQL);
                                    break;
                                }
                            case "groupwork":
                                {
                                    tableName = "we_group";
                                    querySQL = "select id_row, title as statusname,'' as color, '' as follower, '' as description from " + tableName + " " +
                                        "where disabled = 0 and id_project_team  = " + int.Parse(id_project_team) + "";
                                    dt_filter = cnn.CreateDataTable(querySQL);
                                    DataRow newRow = dt_filter.NewRow();
                                    newRow[0] = 0;
                                    newRow[1] = "Chưa phân loại";
                                    dt_filter.Rows.InsertAt(newRow, 0);
                                    foreach (DataRow row in dt_filter.Rows)
                                    {
                                        string word = "Ă";
                                        if (!string.IsNullOrEmpty(row[1].ToString()))
                                        {
                                            char[] array = row[1].ToString().Take(1).ToArray();
                                            word = array[0].ToString();
                                        }
                                        row["color"] = JeeWorkLiteController.GetColorName(word);
                                    }
                                    break;
                                }
                            case "assignee":
                                {
                                    dt_filter = cnn.CreateDataTable("select id_user as id_row, '' as statusname,'' as color, '' as follower, '' as description " +
                                        "from we_project_team_user where disabled = 0 and id_project_team = " + id_project_team);
                                    DataRow newRow = dt_filter.NewRow();
                                    foreach (DataRow item in dt_filter.Rows)
                                    {
                                        newRow = dt_filter.NewRow();
                                        var info = DataAccount.Where(x => item["id_row"].ToString().Contains(x.UserId.ToString())).FirstOrDefault();
                                        if (info != null)
                                        {
                                            item["statusname"] = info.FullName;
                                            item["color"] = info.BgColor;
                                            item["Follower"] = "";
                                            item["Description"] = "";
                                        }
                                    }
                                    newRow = dt_filter.NewRow();
                                    newRow[0] = "0";
                                    newRow[1] = "Công việc nhiều người làm";
                                    dt_filter.Rows.InsertAt(newRow, 0);
                                    dt_filter.Rows.InsertAt(dt_filter.NewRow(), 0);
                                    break;
                                }
                            default: break;
                        }
                    }
                    DataTable dt_tag = new DataTable();
                    SqlConditions conds = new SqlConditions();
                    string FieldsSelected = "";
                    List<object> FieldName_New = dt_fields.AsEnumerable().Where(x => !(bool)x["isnewfield"]).Select(x => x["FieldName"]).ToList();
                    foreach (var _item in FieldName_New)
                    {
                        string fieldname = _item.ToString();
                        if ("tag".Equals(fieldname))
                        {
                            conds = new SqlConditions();
                            conds.Add("id_project_team", id_project_team);
                            conds.Add("w_tag.Disabled", 0);
                            conds.Add("tag.Disabled", 0);
                            string select_tag = "select tag.title, tag.color, w_tag.id_row, w_tag.id_tag, w_tag.id_work " +
                                "from we_work_tag w_tag join we_tag tag on tag.id_row = w_tag.id_tag " +
                                "where (where)";
                            dt_tag = cnn.CreateDataTable(select_tag, "(where)", conds);
                        }
                        if ("id_row".Equals(fieldname))
                            fieldname = "cast(id_row as varchar) as id_row";
                        if ("status".Equals(fieldname))
                            fieldname = "cast(status as varchar) as status";
                        if ("comments".Equals(fieldname))
                            fieldname = "num_comment";
                        FieldsSelected += "," + fieldname.ToLower();
                    }
                    if (!FieldsSelected.Equals(""))
                        FieldsSelected = FieldsSelected.Substring(1);
                    if (!rs)
                    {
                        long userid = loginData.UserID;
                        strW += @$" and w.id_row in (select id_parent from we_work ww join we_work_user wu on ww.id_row = wu.id_work where ww.disabled = 0 and wu.disabled = 0 and id_parent is not null and id_user = {userid}
 union all select id_row from v_wework_new where id_nv = {userid} or createdby = {userid} )";
                    }
                    string sql = "select iIf(id_group is null,0,id_group) as id_group, createddate, work_group, closed, closed_work_date, closed_work_by, " + FieldsSelected;
                    sql += $@" from v_wework_clickup_new w where w.disabled = 0 " + strW + "";
                    DataTable result = new DataTable();
                    result = cnn.CreateDataTable(sql, Conds);
                    DataTable tmp = new DataTable();
                    string queryTag = @"select a.id_row,a.title,a.color,b.id_work from we_tag a 
                                        join we_work_tag b on a.id_row=b.id_tag 
                                        where a.disabled=0 and b.disabled = 0 
                                        and a.id_project_team = " + id_project_team + "" +
                                        " and id_work = ";
                    result.Columns.Add("Tags", typeof(DataTable));
                    result.Columns.Add("User", typeof(DataTable));
                    result.Columns.Add("UserSubtask", typeof(DataTable));
                    result.Columns.Add("Follower", typeof(DataTable));
                    DataColumnCollection columns = result.Columns;
                    if (!columns.Contains("id_nv"))
                    {
                        result.Columns.Add("id_nv", typeof(string));
                    }
                    result.Columns.Add("DataStatus", typeof(DataTable));
                    result.Columns.Add("DataChildren", typeof(DataTable));
                    if (result.Rows.Count > 0)
                    {
                        foreach (DataRow dr in result.Rows)
                        {
                            dr["Tags"] = cnn.CreateDataTable(queryTag + dr["id_row"]);
                            var row_user = dt_users.Select("id_parent is null and loai = 1 and id_work = " + dr["id_row"]);
                            if (row_user.Any())
                                dr["User"] = row_user.CopyToDataTable();
                            row_user = dt_users.Select("id_parent is null and loai = 2 and id_work = " + dr["id_row"]);
                            if (row_user.Any())
                                dr["Follower"] = row_user.CopyToDataTable();
                            DataRow[] user_child = dt_users.Select("id_parent is not null and id_parent = " + dr["id_row"]);
                            dr["UserSubtask"] = cnn.CreateDataTable(sql_user + " and id_work in (select id_row from we_work where id_parent = " + dr["id_row"] + ")", "(where)", cond);
                            dr["DataStatus"] = list_status_user(dr["id_row"].ToString(), id_project_team, loginData, ConnectionString, DataAccount);
                            result.AcceptChanges();
                            DataRow[] r_parent = result.Select("id_parent is not null and id_parent =" + dr["id_row"]);
                            DataTable dt_parent = new DataTable();
                            if (string.IsNullOrEmpty(dr["id_parent"].ToString()) && r_parent.Length > 0)
                            {
                                #region Lấy thông tin subtask tương ứng
                                dt_parent = r_parent.CopyToDataTable();
                                for (int i = 0; i < dt_parent.Rows.Count; i++)
                                {
                                    dt_parent.Rows[i]["Tags"] = cnn.CreateDataTable(queryTag + dt_parent.Rows[i]["id_row"]);
                                    row_user = dt_users.Select("id_parent is not null and loai = 1 and id_work = " + dt_parent.Rows[i]["id_row"]);
                                    if (row_user.Any())
                                        dt_parent.Rows[i]["User"] = row_user.CopyToDataTable();
                                    row_user = dt_users.Select("id_parent is not null and loai = 2 and id_work = " + dt_parent.Rows[i]["id_row"]);
                                    if (row_user.Any())
                                        dt_parent.Rows[i]["Follower"] = row_user.CopyToDataTable();
                                    dt_parent.Rows[i]["UserSubtask"] = new DataTable();
                                    dt_parent.Rows[i]["DataStatus"] = list_status_user(dt_parent.Rows[i]["id_row"].ToString(), id_project_team, loginData, ConnectionString, DataAccount);
                                    dt_parent.AcceptChanges();
                                    //dr["DataChildren"] = dt_parent;
                                }
                                #endregion
                                dr["DataChildren"] = dt_parent;
                            }
                            else
                                dr["DataChildren"] = dt_parent;
                        }
                        var filterTeam = " id_parent is null and id_project_team = " + id_project_team;
                        var rows = result.Select(filterTeam);
                        if (rows.Any())
                            tmp = rows.CopyToDataTable();
                    }
                    var data = new
                    {
                        datawork = tmp,
                        TenCot = dt_fields,
                        Tag = dt_tag,
                        Filter = dt_filter,
                        User = dt_users,
                        DataWork_NewField = dt_new_fields
                    };
                    return data;
                }
            }
            catch (Exception ex)
            {
                return JsonResultCommon.Exception(_logger, ex, _config, loginData);
            }
        }
        private object GetTaskByProjects(IHeaderDictionary _header, string id_project_team, DataTable dt_fields, string ConnectionString, QueryParams query, UserJWT loginData, List<AccUsernameModel> DataAccount)
        {
            using (DpsConnection cnn = new DpsConnection(ConnectionString))
            {
                #region filter thời gian, keyword, group by
                DateTime from = Common.GetDateTime();
                DateTime to = Common.GetDateTime();
                string groupby = "status";
                string tableName = "";
                string querySQL = "";
                string data_newfield = "";
                DataTable dt_filter = new DataTable();
                DataTable dt_filter_tmp = new DataTable();
                DataTable dt_new_fields = new DataTable();
                dt_filter.Columns.Add("id_row", typeof(String));
                dt_filter.Columns.Add("statusname", typeof(String));
                dt_filter.Columns.Add("color", typeof(String));
                dt_filter.Columns.Add("Follower", typeof(String));
                dt_filter.Columns.Add("description", typeof(String));
                SqlConditions sqlcondQ = new SqlConditions();
                sqlcondQ.Add("id_row", id_project_team);
                sqlcondQ.Add("disabled", 0);
                string s = "select * from we_project_team where (where)";
                DataTable old = cnn.CreateDataTable(s, "(where)", sqlcondQ);
                if (old == null || old.Rows.Count == 0)
                    return JsonResultCommon.KhongTonTai("Dự án");
                bool rs = Common.CheckRoleByProject(id_project_team, loginData, cnn, ConnectionString);
                data_newfield = "select * from we_newfileds_values where id_project_team = " + id_project_team + "";
                #endregion
                string strW = "";
                SqlConditions Conds = new SqlConditions();
                dt_new_fields = cnn.CreateDataTable(data_newfield);
                Dictionary<string, string> collect = new Dictionary<string, string>
                        {
                            { "CreatedDate", "CreatedDate"},
                            { "Deadline", "deadline"},
                            { "StartDate", "start_date"}
                        };
                string collect_by = "CreatedDate";
                if (!string.IsNullOrEmpty(query.filter["collect_by"]))
                    collect_by = collect[query.filter["collect_by"]];
                if (!string.IsNullOrEmpty(query.filter["TuNgay"]))
                {
                    DateTime.TryParseExact(query.filter["TuNgay"], "dd/MM/yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out from);
                    strW += " and w." + collect_by + ">=@from";
                    Conds.Add("from", JeeWorkLiteController.GetUTCTime(_header, from.ToString()));
                }
                if (!string.IsNullOrEmpty(query.filter["DenNgay"]))
                {
                    DateTime.TryParseExact(query.filter["DenNgay"], "dd/MM/yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out to);
                    to = to.AddDays(1);
                    strW += " and w." + collect_by + "<@to";
                    Conds.Add("to", JeeWorkLiteController.GetUTCTime(_header, to.ToString()));
                }
                if (!string.IsNullOrEmpty(query.filter["keyword"]))
                {
                    strW += " and (w.title like N'%@keyword%' or w.description like N'%@keyword%')";
                    strW = strW.Replace("@keyword", query.filter["keyword"]);
                }
                if (string.IsNullOrEmpty(query.filter["task_done"]))
                {
                    strW += " and  w.id_row not in ( select id_row from we_work where end_date is not null and id_parent is null )";
                }
                if (string.IsNullOrEmpty(query.filter["subtask_done"]))
                {
                    strW += " and w.id_row not in ( select id_row from we_work where end_date is not null and id_parent is not null) ";
                }
                if (!string.IsNullOrEmpty(query.filter["forme"]))
                {
                    strW += " and (w.id_nv=@iduser or w.id_row in (select id_parent from we_work ww join we_work_user wu on ww.id_row = wu.id_work where ww.disabled = 0 and wu.disabled = 0 and id_parent is not null and wu.loai = 1 and id_user = @iduser))";
                    strW = strW.Replace("@iduser", loginData.UserID.ToString());
                }
                else
                    strW += "";
                strW += " and (w.id_project_team = @id_project_team)";
                strW = strW.Replace("@id_project_team", id_project_team.ToString());
                SqlConditions cond = new SqlConditions();
                cond.Add("id_project_team", id_project_team);
                cond.Add("w_u.disabled", 0);
                cond.Add("w_u.loai", 1);
                string sql_user = $@"select w_u.id_work, w_u.id_user, w_u.loai, id_child
                                        , w_u.disabled, '' as hoten, '' as color
                                        , id_project_team,'' as image, w.id_parent
                                        from we_work_user w_u 
                                        join we_work w on w.id_row = w_u.id_work 
                                        where (where)";
                DataTable dt_users = cnn.CreateDataTable(sql_user, "(where)", cond);
                #region Map info account từ JeeAccount
                foreach (DataRow item in dt_users.Rows)
                {
                    var info = DataAccount.Where(x => item["id_user"].ToString().Contains(x.UserId.ToString())).FirstOrDefault();
                    if (info != null)
                    {
                        item["hoten"] = info.FullName;
                        item["image"] = info.AvartarImgURL;
                        item["color"] = info.BgColor;
                    }
                }
                #endregion
                DataTable dt_value = new DataTable();
                string column = "status";
                string field_custom = "", field_type = "";
                if (!string.IsNullOrEmpty(query.filter["groupby"]))
                {
                    groupby = query.filter["groupby"];
                    switch (groupby)
                    {
                        case "priority":
                            {
                                column = "clickup_prioritize";
                                dt_filter.Rows.Add(new object[] { "1", "Khẩn cấp", "#fd397a", 1, "" });
                                dt_filter.Rows.Add(new object[] { "2", "Cao", "#ffb822", 2, "" });
                                dt_filter.Rows.Add(new object[] { "3", "Bình thường", "#5578eb", 3, "" });
                                dt_filter.Rows.Add(new object[] { "4", "Thấp", "#74788d", 4, "" });
                                DataRow newRow = dt_filter.NewRow();
                                newRow = dt_filter.NewRow();
                                newRow[0] = 0;
                                newRow[1] = "Chưa có độ ưu tiên";
                                newRow[2] = "#21BD1C";
                                newRow[3] = 5;
                                dt_filter.Rows.InsertAt(newRow, 0);
                                dt_filter.DefaultView.Sort = "Follower";
                                dt_filter = dt_filter.DefaultView.ToTable();
                                break;
                            }
                        case "status":
                            {
                                column = "status";
                                tableName = "we_status";
                                querySQL = "select id_row, statusname, color, follower, description, type, position from " + tableName + " " +
                                    "where disabled = 0 and id_project_team  = " + int.Parse(id_project_team) + " " +
                                    "order by position";
                                dt_filter = cnn.CreateDataTable(querySQL);
                                break;
                            }
                        case "groupwork":
                            {
                                column = "id_group";
                                tableName = "we_group";
                                querySQL = "select id_row, title as statusname,'' as color, '' as follower, '' as description from " + tableName + " " +
                                    "where disabled = 0 and id_project_team  = " + int.Parse(id_project_team) + "";
                                dt_filter = cnn.CreateDataTable(querySQL);
                                DataRow newRow = dt_filter.NewRow();
                                newRow[0] = 0;
                                newRow[1] = "Chưa phân loại";
                                dt_filter.Rows.InsertAt(newRow, 0);
                                foreach (DataRow row in dt_filter.Rows)
                                {
                                    string word = "";
                                    if (!string.IsNullOrEmpty(row[1].ToString()))
                                    {
                                        char[] array = row[1].ToString().Take(1).ToArray();
                                        word = array[0].ToString();
                                    }
                                    row["color"] = JeeWorkLiteController.GetColorName(word);
                                }
                                break;
                            }
                        //case "tags":
                        //    {
                        //        column = "id_group";
                        //        tableName = "we_group";
                        //        querySQL = "select id_row, title as statusname,'' as color, '' as follower, '' as description from " + tableName + " " +
                        //            "where disabled = 0 and id_project_team  = " + int.Parse(id_project_team) + "";
                        //        dt_filter = cnn.CreateDataTable(querySQL);
                        //        DataRow newRow = dt_filter.NewRow();
                        //        newRow[0] = 0;
                        //        newRow[1] = "Chưa có nhãn";
                        //        dt_filter.Rows.InsertAt(newRow, 0);
                        //        foreach (DataRow row in dt_filter.Rows)
                        //        {
                        //            string word = "";
                        //            if (!string.IsNullOrEmpty(row[1].ToString()))
                        //            {
                        //                char[] array = row[1].ToString().Take(1).ToArray();
                        //                word = array[0].ToString();
                        //            }
                        //            row["color"] = JeeWorkLiteController.GetColorName(word);
                        //        }
                        //        break;
                        //    }
                        //case "deadline":
                        //    {
                        //        column = "id_group";
                        //        tableName = "we_group";
                        //        querySQL = "select id_row, title as statusname,'' as color, '' as follower, '' as description from " + tableName + " " +
                        //            "where disabled = 0 and id_project_team  = " + int.Parse(id_project_team) + "";
                        //        dt_filter = cnn.CreateDataTable(querySQL);
                        //        DataRow newRow = dt_filter.NewRow();
                        //        newRow[0] = 0;
                        //        newRow[1] = "Chưa gắn hạn chót";
                        //        dt_filter.Rows.InsertAt(newRow, 0);
                        //        foreach (DataRow row in dt_filter.Rows)
                        //        {
                        //            string word = "";
                        //            if (!string.IsNullOrEmpty(row[1].ToString()))
                        //            {
                        //                char[] array = row[1].ToString().Take(1).ToArray();
                        //                word = array[0].ToString();
                        //            }
                        //            row["color"] = JeeWorkLiteController.GetColorName(word);
                        //        }
                        //        break;
                        //    }
                        case "assignee":
                            {
                                column = "id_nv";
                                dt_filter = cnn.CreateDataTable("select id_user as id_row, '' as statusname,'' as color, '' as follower, '' as description " +
                                    "from we_project_team_user where disabled = 0 and id_project_team = " + id_project_team);
                                DataRow newRow = dt_filter.NewRow();
                                newRow = dt_filter.NewRow();
                                newRow[0] = DBNull.Value;
                                newRow[1] = "Chưa giao việc";
                                dt_filter.Rows.InsertAt(newRow, 0);
                                //dt_filter.Rows.InsertAt(dt_filter.NewRow(), 0);
                                foreach (DataRow item in dt_filter.Rows)
                                {
                                    //newRow = dt_filter.NewRow();
                                    var info = DataAccount.Where(x => item["id_row"].ToString().Contains(x.UserId.ToString())).FirstOrDefault();
                                    if (info != null)
                                    {
                                        item["statusname"] = info.FullName;
                                        item["color"] = info.BgColor;
                                        item["Follower"] = "";
                                        item["Description"] = "";
                                    }
                                    else
                                    {
                                        item["color"] = JeeWorkLiteController.GetColorName("Chưa giao việc");
                                    }
                                }
                                break;
                            }
                        case "custom":
                            {
                                if (!string.IsNullOrEmpty(query.filter["field_custom"]))
                                {
                                    field_custom = query.filter["field_custom"];
                                }
                                if (!string.IsNullOrEmpty(query.filter["field_type"]))
                                {
                                    field_type = query.filter["field_type"].ToString().ToLower();
                                }
                                if ("date".Equals(field_type) || "checkbox".Equals(field_type))
                                {
                                    tableName = "we_newfileds_values";
                                    querySQL = "select value as id_row, value as statusname, '#21BD1C' as color, '' as follower, '' as description " +
                                  "from " + tableName + " " +
                                  "where id_project_team  = " + id_project_team + " and FieldID = " + field_custom;
                                }
                                if ("labels".Equals(field_type) || "dropdown".Equals(field_type))
                                {
                                    tableName = "we_newfields_options";
                                    querySQL = "select rowid as id_row, value as statusname, Color, '' as follower, '' as description " +
                                  "from " + tableName + " " +
                                  "where id_project_team  = " + id_project_team + " and FieldID = " + field_custom;
                                }
                                column = "custom";
                                dt_filter = cnn.CreateDataTable(querySQL);
                                DataRow newRow = dt_filter.NewRow();
                                newRow[0] = 0;
                                newRow[1] = "Chưa phân loại";
                                string word = "";
                                if (!string.IsNullOrEmpty(newRow[1].ToString()))
                                {
                                    char[] array = newRow[1].ToString().Take(1).ToArray();
                                    word = array[0].ToString();
                                }
                                newRow[2] = JeeWorkLiteController.GetColorName(word);
                                dt_filter.Rows.InsertAt(newRow, 0);
                                dt_value = cnn.CreateDataTable("select fieldid, workid, typeid, value, id_project_team " +
                                    "from we_newfileds_values where FieldID = " + field_custom + " and id_project_team =" + id_project_team);
                                break;
                            }
                        default: break;
                    }
                }
                SqlConditions conds = new SqlConditions();
                if (!rs)
                {
                    long userid = loginData.UserID;
                    strW += @$" and w.id_row in (select id_parent from we_work ww 
                                    join we_work_user wu on ww.id_row = wu.id_work 
                                    where ww.disabled = 0 and wu.disabled = 0 
                                    and id_parent is not null and id_user = {userid}
                                     union all select id_row 
                                    from v_wework_new where id_nv = {userid} or createdby = {userid} )";
                }
                string displayChild = "0";//hiển thị con: 0-không hiển thị, 1- 1 cấp con, 2- nhiều cấp con
                if (!string.IsNullOrEmpty(query.filter["displayChild"]))
                    displayChild = query.filter["displayChild"];
                string sql = "select iIf(id_group is null,0,id_group) as id_group, '0' as custom" +
                    ", cast(id_row as varchar) as id_row, cast(status as varchar) as status, * ";
                sql += $@" from v_wework_new w where w.disabled = 0 " + strW + "";
                DataTable result = new DataTable();
                result = cnn.CreateDataTable(sql, Conds);
                DataTable tmp = new DataTable();
                string queryTag = @"select a.id_row,a.title,a.color,b.id_work from we_tag a 
                                        join we_work_tag b on a.id_row=b.id_tag 
                                        where a.disabled=0 and b.disabled = 0 
                                        and a.id_project_team = " + id_project_team + "" +
                                    " and id_work = ";
                DataColumnCollection columns = result.Columns;
                if (!columns.Contains("id_nv"))
                {
                    result.Columns.Add("id_nv", typeof(string));
                }
                if (dt_value.Rows.Count > 0)
                {
                    foreach (DataRow item in result.Rows)
                    {
                        DataRow[] row = dt_value.Select("WorkID=" + item["id_row"].ToString());
                        if (row.Length > 0)
                        {
                            item["custom"] = row[0]["value"].ToString();
                        }
                        else
                        {
                            item["custom"] = "0";
                        }
                    }
                }
                string domain = _configuration.GetValue<string>("Host:JeeWork_API") + "/";
                if (result.Rows.Count > 0)
                {
                    var filterTeam = " id_parent is null and id_project_team = " + id_project_team;
                    var rows = result.Select(filterTeam);
                    if (rows.Any())
                        tmp = rows.CopyToDataTable();
                }
                DataTable dt_tag = new DataTable();
                conds = new SqlConditions();
                conds.Add("id_project_team", id_project_team);
                conds.Add("w_tag.disabled", 0);
                conds.Add("tag.disabled", 0);
                string select_tag = "select tag.title, tag.color, w_tag.id_row, w_tag.id_tag, w_tag.id_work " +
                    "from we_work_tag w_tag join we_tag tag on tag.id_row = w_tag.id_tag " +
                    "where (where)";
                dt_tag = cnn.CreateDataTable(select_tag, "(where)", conds);
                var data = from p in dt_filter.AsEnumerable()
                           select new
                           {
                               id_row = p["id_row"],
                               statusname = p["statusname"],
                               color = p["color"],
                               follower = p["follower"],
                               description = p["description"],
                               datawork = GetChildTask(domain, loginData.CustomerID, column, displayChild, p["id_row"], result.AsEnumerable(), dt_tag.AsEnumerable(), DataAccount, loginData, ConnectionString, dt_users)
                           };
                return data;
            }
        }
        public static DataTable DataUpDateColumn(long type, long id, string ConnectionString)
        {
            using (DpsConnection cnn = new DpsConnection(ConnectionString))
            {
                DataTable result = new DataTable();
                result.Clear();
                result.Columns.Add("TableName");
                result.Columns.Add("ID");
                result.Columns.Add("CoumnName");
                result.Columns.Add("TableOption");
                DataRow _row = result.NewRow();
                if (type < 3)
                {
                    _row["TableName"] = "we_fields_department";
                    _row["ID"] = id.ToString();
                    _row["CoumnName"] = "id_department";
                    _row["TableOption"] = "we_newfields_options_dpm";
                    result.Rows.Add(_row);
                    #region Lấy tất cả id_project team dựa vào id_department
                    DataTable dt_project = cnn.CreateDataTable(@"select id_row from we_project_team where disabled = 0 and (id_department = " + id + " " +
                           "or id_department in (select id_row from we_department where ParentID = " + id + "))");
                    if (dt_project.Rows.Count > 0)
                    {
                        foreach (DataRow item in dt_project.Rows)
                        {
                            _row = result.NewRow();
                            _row["TableName"] = "we_fields_project_team";
                            _row["ID"] = item["id_row"].ToString();
                            _row["CoumnName"] = "id_project_team";
                            _row["TableOption"] = "we_newfields_options";
                            result.Rows.Add(_row);
                        }
                    }
                    #endregion
                }
                else
                {
                    _row = result.NewRow();
                    _row["TableName"] = "we_fields_project_team";
                    _row["ID"] = id.ToString();
                    _row["CoumnName"] = "id_project_team";
                    _row["TableOption"] = "we_newfields_options";
                    result.Rows.Add(_row);
                }
                return result;
            }
        }

        /// <summary>
        /// lấy thông tin người để gửi thông báo : Người được giao việc, người giao việc, người theo dõi
        /// </summary>
        /// <param name="cnn"></param>
        /// <param name="id_work"></param>
        /// <returns></returns>
        public static List<long> getUserTask(DpsConnection cnn, long id_work)
        {
            SqlConditions conds = new SqlConditions();
            conds.Add("id_work", id_work);
            string sql = @"select wu.id_user from we_work_user wu  
 where  wu.Disabled = 0 and wu.id_work = @id_work 
 union all
 select CreatedBy as id_user from we_work_user wu  
 where  wu.Disabled = 0 and wu.id_work = @id_work ";
            DataTable dtuser = cnn.CreateDataTable(sql, conds);
            if (dtuser.Rows.Count == 0)
            {
                return new List<long>();
            }
            List<long> listUser = dtuser.AsEnumerable().Select(x => long.Parse(x["id_user"].ToString())).Distinct().ToList();
            return listUser;
        }
        private object calendar_getphanloai(string id_row, UserJWT loginData, string ConnectionString)
        {
            DataTable dt = new DataTable();
            List<int> ListPhanLoai = new List<int>();
            using (DpsConnection cnn = new DpsConnection(ConnectionString))
            {
                SqlConditions conds = new SqlConditions();
                DataTable dt_follow = new DataTable();
                conds.Add("w.id_work", id_row);
                conds.Add("w.disabled", 0);
                int assign = 0, forme = 0, following = 0, loai = 0; string sql = "";
                sql = @"select id_user, createdby, loai from we_work_user w where (where)";
                dt = cnn.CreateDataTable(sql, "(where)", conds);
                if (dt.Rows.Count > 0)
                {
                    foreach (DataRow item in dt.Rows)
                    {
                        loai = int.Parse(item["loai"].ToString());
                        forme = int.Parse(item["id_user"].ToString()); // type 41
                        assign = int.Parse(item["createdby"].ToString()); // type 42
                        following = int.Parse(item["id_user"].ToString()); // type 43
                        if (loai == 1 && forme == loginData.UserID)
                        {
                            ListPhanLoai.Add(41);
                        }
                        if (loai == 2 && following == loginData.UserID)
                        {
                            ListPhanLoai.Add(43);
                        }
                        if (assign == loginData.UserID)
                        {
                            ListPhanLoai.Add(42);
                        }
                    }
                    ListPhanLoai.Sort();
                }
            }
            var data = new
            {
                ListID = ListPhanLoai.ToArray()
            };
            return data;
        }
        public static DateTime GetEndDateInMonth(int thang, int nam)
        {
            int songaycuathang = DateTime.DaysInMonth(nam, thang);
            IFormatProvider fm = new CultureInfo("en-US", true);
            string d = songaycuathang.ToString() + "/" + thang.ToString() + "/" + nam.ToString();
            DateTime result = new DateTime();
            DateTime.TryParseExact(d, "d/M/yyyy", fm, DateTimeStyles.NoCurrentDateDefault, out result);
            return result;
        }
    }
}

