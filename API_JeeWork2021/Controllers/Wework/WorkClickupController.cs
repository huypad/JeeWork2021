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

namespace JeeWork_Core2021.Controllers.Wework
{
    [ApiController]
    [Route("api/work-click-up")]
    [EnableCors("JeeWorkPolicy")]
    /// <summary>
    /// api quản lý work (click up)
    /// </summary>
    // [EnableCors(origins: "*", headers: "*", methods: "*")]
    public class WorkClickupController : ControllerBase
    {
        private readonly IHostingEnvironment _hostingEnvironment;
        private JeeWorkConfig _config;
        string HRCatalog = JeeWorkConstant.getConfig("JeeWorkConfig:HRCatalog");
        public static DataImportModel data_import = new DataImportModel();
        public List<AccUsernameModel> DataAccount;
        private IConnectionCache ConnectionCache;
        private IConfiguration _configuration;
        private INotifier _notifier;
        private IProducer _producer;
        private readonly ILogger<WorkClickupController> _logger;
        public WorkClickupController(IOptions<JeeWorkConfig> config, IHostingEnvironment hostingEnvironment, IConnectionCache _cache, IConfiguration configuration, INotifier notifier, ILogger<WorkClickupController> logger, IProducer producer)
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
        [Route("list-work")]
        [HttpGet]
        public async Task<object> ListWorkClickUp([FromQuery] QueryParams query)
        {
            string Token = Common.GetHeader(Request);
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
                string ConnectionString = WeworkLiteController.getConnectionString(ConnectionCache, loginData.CustomerID, _configuration);
                using (DpsConnection cnn = new DpsConnection(ConnectionString))
                {
                    #region Lấy dữ liệu account từ JeeAccount
                    DataAccount = WeworkLiteController.GetAccountFromJeeAccount(HttpContext.Request.Headers, _configuration);
                    if (DataAccount == null)
                        return JsonResultCommon.Custom("Lỗi lấy danh sách nhân viên từ hệ thống quản lý tài khoản");

                    //List<string> nvs = DataAccount.Select(x => x.UserId.ToString()).ToList();
                    //string ids = string.Join(",", nvs);
                    string error = "";
                    string listID = WeworkLiteController.ListAccount(HttpContext.Request.Headers, out error, _configuration);
                    if (error != "")
                        return JsonResultCommon.Custom(error);
                    #endregion
                    string listDept = WeworkLiteController.getListDepartment_GetData(loginData, cnn, HttpContext.Request.Headers, _configuration, ConnectionString);
                    #region filter thời gian, keyword, group by
                    DateTime from = DateTime.Now;
                    DateTime to = DateTime.Now;
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
                    data_newfield = "select * from we_newfileds_values where id_project_team = " + int.Parse(query.filter["id_project_team"]) + "";
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
                    string strW = "", strW1 = "";
                    SqlConditions Conds = new SqlConditions();
                    DataTable dt_Fields = WeworkLiteController.GetListField(int.Parse(query.filter["id_project_team"]), ConnectionString);
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
                        strW1 += " and w." + collect_by + ">=@from";
                        Conds.Add("from", from);
                    }
                    if (!string.IsNullOrEmpty(query.filter["DenNgay"]))
                    {
                        DateTime.TryParseExact(query.filter["DenNgay"], "dd/MM/yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out to);
                        to = to.AddDays(1);
                        strW1 += " and w." + collect_by + "<@to";
                        Conds.Add("to", to);
                    }
                    if (!string.IsNullOrEmpty(query.filter["keyword"]))
                    {
                        strW = " and (w.title like N'%@keyword%' or w.description like N'%@keyword%')";
                        strW = strW.Replace("@keyword", query.filter["keyword"]);
                    }
                    if (!string.IsNullOrEmpty(query.filter["id_project_team"]))
                    {
                        strW = " and (w.id_project_team = @id_project_team)";
                        strW = strW.Replace("@id_project_team", query.filter["id_project_team"]);
                    }
                    if (!string.IsNullOrEmpty(query.filter["groupby"]))
                    {
                        groupby = query.filter["groupby"];
                        switch (groupby)
                        {
                            case "priority":
                                {
                                    //dt_filter_tmp = new DataTable();
                                    dt_filter_tmp = new DataTable();
                                    dt_filter_tmp.Columns.Add("id_row", typeof(String));
                                    dt_filter_tmp.Columns.Add("statusname", typeof(String));
                                    dt_filter_tmp.Columns.Add("color", typeof(String));
                                    dt_filter_tmp.Columns.Add("Follower", typeof(String));
                                    dt_filter_tmp.Rows.Add(new object[] { "1", "Urgent", "#fd397a" });
                                    dt_filter_tmp.Rows.Add(new object[] { "2", "High", "#ffb822" });
                                    dt_filter_tmp.Rows.Add(new object[] { "3", "Normal", "#5578eb" });
                                    dt_filter_tmp.Rows.Add(new object[] { "4", "Low", "#74788d " });
                                    break;
                                }
                            case "status":
                                {
                                    tableName = "we_status";
                                    querySQL = "select id_row, statusname, color, Follower, Description from " + tableName + " " +
                                        "where disabled = 0 and id_project_team  = " + int.Parse(query.filter["id_project_team"]) + "";
                                    dt_filter_tmp = cnn.CreateDataTable(querySQL);
                                    break;
                                }
                            case "groupwork":
                                {
                                    tableName = "we_group";
                                    querySQL = "select  id_row, title,'' as color, '' as Follower, '' as Description  from " + tableName + " " +
                                        "where disabled = 0 and id_project_team  = " + int.Parse(query.filter["id_project_team"]) + "";
                                    dt_filter_tmp = cnn.CreateDataTable(querySQL);
                                    DataRow newRow = dt_filter_tmp.NewRow();
                                    newRow[0] = 0;
                                    newRow[1] = "Chưa phân loại";
                                    dt_filter_tmp.Rows.InsertAt(newRow, 0);
                                    break;
                                }
                            case "assignee":
                                {
                                    tableName = "v_wework_clickup_new";
                                    querySQL = "select distinct id_nv, hoten, '' as color, '' as Follower, '' as Description from " + tableName + " " +
                                        "where id_project_team  = " + int.Parse(query.filter["id_project_team"]) + "";
                                    SqlConditions conds1 = new SqlConditions();
                                    conds1.Add("id_project_team", query.filter["id_project_team"]);
                                    conds1.Add("w_user.Disabled", 0);
                                    string select_user = $@"select  distinct w_user.id_user,'' as hoten,'' as color, '' as Follower,'' as Description
                                                    from we_work_user w_user join we_work on we_work.id_row = w_user.id_work 
                                                    where (where)";
                                    dt_filter_tmp = cnn.CreateDataTable(select_user, "(where)", conds1);
                                    #region Map info account từ JeeAccount

                                    foreach (DataRow item in dt_filter_tmp.Rows)
                                    {
                                        var info = DataAccount.Where(x => item["id_user"].ToString().Contains(x.UserId.ToString())).FirstOrDefault();
                                        if (info != null)
                                        {
                                            item["hoten"] = info.FullName;
                                        }
                                    }
                                    #endregion
                                    DataRow newRow = dt_filter_tmp.NewRow();
                                    newRow[0] = "0";
                                    newRow[1] = "Công việc nhiều người làm";
                                    dt_filter_tmp.Rows.InsertAt(newRow, 0);
                                    dt_filter_tmp.Rows.InsertAt(dt_filter_tmp.NewRow(), 0);
                                    break;
                                }
                            default: break;
                        }
                    }
                    foreach (DataRow row in dt_filter_tmp.Rows)
                    {
                        string word = "Ă";
                        if (!string.IsNullOrEmpty(row[1].ToString()))
                        {
                            char[] array = row[1].ToString().Take(1).ToArray();
                            word = array[0].ToString();
                        }
                        dt_filter.Rows.Add(new object[] { row[0].ToString(), row[1].ToString(), row[2].ToString() == "" ? getColorByName(word) : row[2].ToString(), row[3].ToString() == "" ? "0" : row[3].ToString(), row[4].ToString() });
                    }
                    DataTable dt_tag = new DataTable();
                    DataTable dt_user = new DataTable();
                    SqlConditions conds = new SqlConditions();
                    string FieldsSelected = "";
                    List<object> FieldName_New = dt_Fields.AsEnumerable().Where(x => !(bool)x["isnewfield"]).Select(x => x["FieldName"]).ToList();
                    foreach (var _item in FieldName_New)
                    {
                        string fieldname = _item.ToString();
                        if ("tag".Equals(fieldname))
                        {
                            conds = new SqlConditions();
                            conds.Add("id_project_team", query.filter["id_project_team"]);
                            conds.Add("w_tag.Disabled", 0);
                            conds.Add("tag.Disabled", 0);
                            string select_tag = "select tag.title, tag.color, w_tag.id_row, w_tag.id_tag, w_tag.id_work " +
                                "from we_work_tag w_tag join we_tag tag on tag.id_row = w_tag.id_tag " +
                                "where (where)";
                            dt_tag = cnn.CreateDataTable(select_tag, "(where)", conds);
                        }
                        if ("id_nv".Equals(fieldname))
                        {
                            conds = new SqlConditions();
                            conds.Add("id_project_team", query.filter["id_project_team"]);
                            conds.Add("w_user.Disabled", 0);
                            string select_user = $@"select w_user.id_work, w_user.id_user, w_user.loai, id_child, w_user.Disabled, '' as hoten, id_project_team
                                                    from we_work_user w_user join we_work on we_work.id_row = w_user.id_work 
                                                   where (where)";
                            dt_user = cnn.CreateDataTable(select_user, "(where)", conds);
                            #region Map info account từ JeeAccount
                            foreach (DataRow item in dt_user.Rows)
                            {
                                var info = DataAccount.Where(x => item["id_user"].ToString().Contains(x.UserId.ToString())).FirstOrDefault();
                                if (info != null)
                                {
                                    item["hoten"] = info.FullName;
                                }
                            }
                            #endregion
                        }
                        else
                        {
                            if ("id_row".Equals(fieldname))
                                fieldname = "cast(id_row as varchar) as id_row";
                            if ("status".Equals(fieldname))
                                fieldname = "cast(status as varchar) as status";
                            if ("comments".Equals(fieldname))
                                fieldname = "'' as comments";
                            FieldsSelected += "," + fieldname.ToLower();
                        }
                    }
                    if (!FieldsSelected.Equals(""))
                        FieldsSelected = FieldsSelected.Substring(1);
                    string sql = "Select  iIf(id_group is null,0,id_group) as id_group ,work_group, " + FieldsSelected;
                    sql += $@" from v_wework_clickup_new w where w.Disabled = 0 " + strW1 + strW;
                    DataTable result = new DataTable();
                    result = cnn.CreateDataTable(sql, Conds);
                    DataTable dt_comments = cnn.CreateDataTable("select id_row, object_type, object_id, comment, id_parent, Disabled " +
                       "from we_comment where disabled = 0 and object_type = 1");
                    string queryTag = @"select a.id_row,a.title,a.color,b.id_work from we_tag a 
                                        join we_work_tag b on a.id_row=b.id_tag 
                                        where a.disabled=0 and b.disabled = 0 
                                        and a.id_project_team = " + query.filter["id_project_team"] + "" +
                                        " and id_work = ";
                    string queryUser = $@"select w_user.id_work, w_user.id_user, w_user.loai, id_child, w_user.Disabled, '' as hoten, id_project_team
                                                    from we_work_user w_user join we_work on we_work.id_row = w_user.id_work 
                                                    where w_user.Disabled = 0 and we_work.id_project_team = " + query.filter["id_project_team"] + "and id_work = ";
                    result.Columns.Add("Tags", typeof(DataTable));
                    result.Columns.Add("User", typeof(DataTable));
                    result.Columns.Add("Follower", typeof(DataTable));
                    result.Columns.Add("id_nv", typeof(string));
                    if (result.Rows.Count > 0)
                    {
                        foreach (DataRow dr in result.Rows)
                        {
                            dr["comments"] = dt_comments.Compute("count(id_row)", "object_id =" + dr["id_row"].ToString() + "").ToString();
                            dr["Tags"] = cnn.CreateDataTable(queryTag + dr["id_row"]);
                            DataTable dt_u = cnn.CreateDataTable(queryUser + dr["id_row"] + "  and loai = 1");
                            DataTable dt_f = cnn.CreateDataTable(queryUser + dr["id_row"] + "  and loai = 2");
                            #region Map info account từ JeeAccount
                            foreach (DataRow item in dt_u.Rows)
                            {
                                var info = DataAccount.Where(x => item["id_user"].ToString().Contains(x.UserId.ToString())).FirstOrDefault();
                                if (info != null)
                                {
                                    item["hoten"] = info.FullName;
                                }
                            }
                            #endregion
                            dr["User"] = dt_u;
                            dr["Follower"] = dt_f;
                        }
                    }
                    DataTable dt_data = new DataTable();
                    DataTable tmp = new DataTable();
                    result.Columns.Add("DataChildren", typeof(DataTable));
                    if (result.Rows.Count > 0)
                    {
                        var filterTeam = " id_parent is null and id_project_team = " + (query.filter["id_project_team"]);
                        var rows = result.Select(filterTeam);
                        if (rows.Any())
                            tmp = rows.CopyToDataTable();
                        foreach (DataRow item in tmp.Rows)
                        {
                            item["DataChildren"] = dtChildren(item["id_row"].ToString(), result, cnn, dt_Fields, query.filter["id_project_team"], DataAccount);
                        }
                        //DataSet ds = await GetWork_ClickUp(cnn, query, loginData.UserID, DataAccount, listDept, strW);
                        //if (cnn.LastError != null || ds == null)
                        //    return JsonResultCommon.Exception(_logger, cnn.LastError, _config, loginData, ControllerContext);
                        //dt_data = ds.Tables[0];
                        //var tags = ds.Tables[1].AsEnumerable();
                    }
                    var data = new
                    {
                        datawork = tmp,
                        TenCot = dt_Fields,
                        Tag = dt_tag,
                        Filter = dt_filter,
                        User = dt_user,
                        DataWork_NewField = dt_new_fields
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
        /// Load danh sách công việc theo user - Cá nhân (Click up)
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
                DataAccount = WeworkLiteController.GetAccountFromJeeAccount(HttpContext.Request.Headers, _configuration);
                if (DataAccount == null)
                    return JsonResultCommon.Custom("Lỗi lấy danh sách nhân viên từ hệ thống quản lý tài khoản");
                string error = "";
                string listID = WeworkLiteController.ListAccount(HttpContext.Request.Headers, out error, _configuration);
                if (error != "")
                    return JsonResultCommon.Custom(error);
                #endregion

                string domain = _configuration.GetValue<string>("Host:JeeWork_API") + "/";
                string ConnectionString = WeworkLiteController.getConnectionString(ConnectionCache, loginData.CustomerID, _configuration);
                using (DpsConnection cnn = new DpsConnection(ConnectionString))
                {
                    long IDNV = loginData.UserID;
                    #region filter thời gian , keyword
                    DateTime from = DateTime.Now;
                    DateTime to = DateTime.Now;
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
                    if (!string.IsNullOrEmpty(query.filter["id_nv"]))
                        IDNV = long.Parse(query.filter["id_nv"]);
                    string displayChild = "0";//hiển thị con: 0-không hiển thị, 1- 1 cấp con, 2- nhiều cấp con
                    if (!string.IsNullOrEmpty(query.filter["displayChild"]))
                        displayChild = query.filter["displayChild"];
                    string columnName = "id_project_team";
                    string strW = " and (w.id_nv=@iduser or w.createdby=@iduser)"; // w.nguoigiao=@iduser or w.createdby=@iduser -- w.NguoiGiao = @iduser or
                    if (!string.IsNullOrEmpty(query.filter["workother"]) && bool.Parse(query.filter["workother"]))
                    {
                        strW = " and ( ( (w.createdby=@iduser or w.NguoiGiao = @iduser )and w.Id_NV not in (@iduser) and w.Id_NV is not null))";
                    }
                    if (!string.IsNullOrEmpty(query.filter["following"]) && bool.Parse(query.filter["following"]))
                    {
                        strW = $" and (w.id_row in ( select id_work from we_work_user where loai = 2 and disabled=0 and id_user = { loginData.UserID }))";
                    }
                    #region group
                    string strG = @$"select distinct p.id_row, p.title 
                                    from we_project_team_user u
                                    join we_project_team p on p.id_row=u.id_project_team 
                                    join we_department d on d.id_row = p.id_department
                                    where u.disabled=0 and p.Disabled=0 and d.Disabled = 0 
                                    and id_user = { IDNV } and d.IdKH = { loginData.CustomerID}";
                    #endregion
                    DataTable dtG = cnn.CreateDataTable(strG);
                    if (dtG.Rows.Count == 0)
                        return JsonResultCommon.ThanhCong(new List<string>(), null, Visible);
                    DataSet ds = getWork(cnn, query, IDNV, DataAccount, strW);
                    if (cnn.LastError != null || ds == null)
                        return JsonResultCommon.Exception(_logger, cnn.LastError, _config, loginData, ControllerContext);
                    //var temp = filterWork(ds.Tables[0].AsEnumerable().Where(x => x["id_parent"] == DBNull.Value), query.filter);//k bao gồm con
                    var tags = ds.Tables[1].AsEnumerable();
                    // Phân trang
                    var dt_data = ds.Tables[0].Rows.Count;
                    //int total = temp.Count();
                    //if (total == 0)
                    //return JsonResultCommon.ThanhCong(new List<string>());
                    //if (query.more)
                    //{
                    //    query.page = 1;
                    //    query.record = total;
                    //}
                    //pageModel.TotalCount = total;
                    //pageModel.AllPage = (int)Math.Ceiling(total / (decimal)query.record);
                    //pageModel.Size = query.record;
                    //pageModel.Page = query.page;
                    //var dtNew = temp.Skip((query.page - 1) * query.record).Take(query.record);
                    //var dtChild = ds.Tables[0].AsEnumerable().Where(x => x["id_parent"] != DBNull.Value).AsEnumerable();
                    //dtNew = dtNew.Concat(dtChild);
                    Func<DateTime, int> weekProjector = d => CultureInfo.CurrentCulture.Calendar.GetWeekOfYear(d, CalendarWeekRule.FirstFourDayWeek, DayOfWeek.Monday);
                    var Children = from rr in dtG.AsEnumerable()
                                   select new
                                   {
                                       id = rr["id_row"],
                                       title = rr["title"],
                                       data = getChild(domain, loginData.CustomerID, columnName, displayChild, rr["id_row"], ds.Tables[0].AsEnumerable(), tags, DataAccount, ConnectionString)
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
        /// Bộ lọc tùy chỉnh
        /// </summary>
        /// <param name="query"></param>
        /// <returns></returns>
        [Route("list-work-by-filter")]
        [HttpGet]
        public object ListWorkByFilter([FromQuery] QueryParams query)
        {
            string Token = Common.GetHeader(Request);
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
                string ConnectionString = WeworkLiteController.getConnectionString(ConnectionCache, loginData.CustomerID, _configuration);
                using (DpsConnection cnn = new DpsConnection(ConnectionString))
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

                    #region filter thời gian , keyword
                    DateTime from = DateTime.Now;
                    DateTime to = DateTime.Now;
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
                    strW += FilterWorkController.genStringWhere(cnn, loginData.UserID, query.filter["id_filter"]);

                    DataSet ds = getWork(cnn, query, long.Parse(query.filter["id_nv"]), DataAccount, strW);
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
                                       data = getChild(domain, loginData.CustomerID, columnName, displayChild, rr["id_row"], dtNew.CopyToDataTable().AsEnumerable(), tags, DataAccount, ConnectionString)
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
            string Token = Common.GetHeader(Request);
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
                string ConnectionString = WeworkLiteController.getConnectionString(ConnectionCache, loginData.CustomerID, _configuration);
                using (DpsConnection cnn = new DpsConnection(ConnectionString))
                {
                    #region Lấy dữ liệu account từ JeeAccount
                    DataAccount = WeworkLiteController.GetAccountFromJeeAccount(HttpContext.Request.Headers, _configuration);
                    if (DataAccount == null)
                        return JsonResultCommon.Custom("Lỗi lấy danh sách nhân viên từ hệ thống quản lý tài khoản");

                    List<AccUsernameModel> DataStaff = WeworkLiteController.GetMyStaff(HttpContext.Request.Headers, _configuration, loginData);
                    if (DataStaff == null)
                        return JsonResultCommon.ThanhCong(new List<string>());
                    List<string> nvs = DataStaff.Select(x => x.UserId.ToString()).ToList();
                    string listIDNV = string.Join(",", nvs);

                    string error = "";
                    string listID = WeworkLiteController.ListAccount(HttpContext.Request.Headers, out error, _configuration);
                    if (error != "")
                        return JsonResultCommon.Custom(error);
                    #endregion

                    if (string.IsNullOrEmpty(query.filter["id_nv"]))
                        return JsonResultCommon.Custom("Thành viên");
                    #region filter thời gian , keyword
                    DateTime from = DateTime.Now;
                    DateTime to = DateTime.Now;
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
                    if (!string.IsNullOrEmpty(query.filter["groupby"]))
                    {
                        groupby = query.filter["groupby"];
                        // project,member
                        if ("project".Equals(groupby))
                        {
                            columnName = "id_project_team";
                            query_group = @"select distinct p.id_row, p.title from we_project_team_user u
                                    join we_project_team p on p.id_row=u.id_project_team 
                                    where u.disabled=0 and p.Disabled=0 and id_user=" + query.filter["id_nv"];
                            dtG = cnn.CreateDataTable(query_group);
                        }
                        if ("member".Equals(groupby))
                        {
                            dtG = new DataTable();
                            dtG.Columns.Add("id_row", typeof(object));
                            dtG.Columns.Add("title", typeof(string));
                            columnName = "Id_NV";
                            //using (DpsConnection cnnHR = new DpsConnection(_config.HRConnectionString))
                            //{
                            //    dt_data_group = Common.GetListByManager(loginData.UserID.ToString(), cnnHR);//id_nv, hoten...
                            //}
                            foreach (var item in DataStaff)
                            {
                                dtG.Rows.Add(new object[] { item.UserId, item.FullName });
                            }
                        }
                    }
                    #endregion
                    if (dtG.Rows.Count == 0)
                        return JsonResultCommon.ThanhCong(new List<string>(), null, Visible);
                    DataSet ds = getWork_IDNV(cnn, query, loginData.UserID, DataAccount, strW);
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
                    DataTable dt_test = ds.Tables[0].AsEnumerable().Where(x => x["id_parent"] != DBNull.Value).CopyToDataTable();
                    dtNew = dtNew.Concat(dtChild);
                    var Children = from rr in dtG.AsEnumerable()
                                   select new
                                   {
                                       id = rr["id_row"],
                                       title = rr["title"],
                                       data = getChild(domain, loginData.CustomerID, columnName, displayChild, rr["id_row"], dtNew.CopyToDataTable().AsEnumerable(), tags, DataAccount, ConnectionString)
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
            string Token = Common.GetHeader(Request);
            UserJWT loginData = Ulities.GetUserByHeader(HttpContext.Request.Headers);
            if (loginData == null)
                return JsonResultCommon.DangNhap();
            try
            {
                string domain = _configuration.GetValue<string>("Host:JeeWork_API") + "/";
                string ConnectionString = WeworkLiteController.getConnectionString(ConnectionCache, loginData.CustomerID, _configuration);
                using (DpsConnection cnn = new DpsConnection(ConnectionString))
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
                                        //image = WeworkLiteController.genLinkImage(domain, loginData.CustomerID, r["id_nv"].ToString(), _hostingEnvironment.ContentRootPath)
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
            string Token = Common.GetHeader(Request);
            UserJWT loginData = Ulities.GetUserByHeader(HttpContext.Request.Headers);
            if (loginData == null)
                return JsonResultCommon.DangNhap();

            try
            {
                string ConnectionString = WeworkLiteController.getConnectionString(ConnectionCache, loginData.CustomerID, _configuration);
                if (data.id_project_team <= 0)
                    return JsonResultCommon.BatBuoc("Dự án/phòng ban");
                if (data.Review || (!data.Review && data_import.dtW == null))
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
                    }
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
                                DataTable table = WeworkLiteController.StatusDynamic(data.id_project_team, new List<AccUsernameModel>(), ConnectionString);
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

                using (DpsConnection cnn = new DpsConnection(ConnectionString))
                {
                    int dem = 0;
                    int total = data_import.dtW.Rows.Count;
                    cnn.BeginTransaction();
                    //Hashtable val = new Hashtable();
                    //val["CreatedDate"] = DateTime.Now;
                    //val["CreatedBy"] = loginData.UserID;
                    //val["id_project_team"] = data.id_project_team;
                    #region insert tag mới, milestone mới, group
                    foreach (DataRow dr in data_import.dtPK.Rows)
                    {
                        Hashtable vl = new Hashtable();
                        vl["CreatedDate"] = DateTime.Now;
                        vl["CreatedBy"] = loginData.UserID;
                        vl["id_project_team"] = data.id_project_team;
                        vl["title"] = dr["title"];
                        if (dr["table"].ToString() != "we_tag")
                            vl["description"] = "";
                        if (dr["table"].ToString() == "we_milestone")
                        {
                            vl["deadline"] = DateTime.Now;
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
                    valW["CreatedDate"] = DateTime.Now;
                    valW["CreatedBy"] = loginData.UserID;
                    valW["id_project_team"] = data.id_project_team;
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
                                val["createddate"] = DateTime.Now;
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
                                val["createddate"] = DateTime.Now;
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
        public async Task<BaseModel<object>> UpdateNewField(UpdateWorkModel data)
        {
            string Token = Common.GetHeader(Request);
            UserJWT loginData = Ulities.GetUserByHeader(HttpContext.Request.Headers);
            if (loginData == null)
                return JsonResultCommon.DangNhap();
            try
            {
                string log_content = "";
                string ConnectionString = WeworkLiteController.getConnectionString(ConnectionCache, loginData.CustomerID, _configuration);
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
                    re = WeworkLiteController.log(_logger, loginData.Username, cnn, 1, data.id_row, iduser, log_content);
                    if (!re)
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
        /// Lịch sử chi tiết thao tác công việc
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [Route("log-detail-by-work")]
        [HttpGet]
        public object LogDetailByWork(long id)
        {
            string Token = Common.GetHeader(Request);
            UserJWT loginData = Ulities.GetUserByHeader(HttpContext.Request.Headers);
            if (loginData == null)
                return JsonResultCommon.DangNhap();
            try
            {
                string domain = _configuration.GetValue<string>("Host:JeeWork_API") + "/";
                string ConnectionString = WeworkLiteController.getConnectionString(ConnectionCache, loginData.CustomerID, _configuration);
                using (DpsConnection cnn = new DpsConnection(ConnectionString))
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
                    string sql = @$" select l.*, act.action, act.action_en, act.format, act.sql,w.title, 
l.CreatedBy as Id_NV, '' AS Hoten, '' as Mobile, '' as Username, '' as Email, '' as image,
'' as CocauID, '' as CoCauToChuc,  '' as Id_Chucdanh, '' AS Tenchucdanh, '' as ColorStatus_Old, '' as ColorStatus_New 
from we_log l join we_log_action act on l.id_action = act.id_row
join we_work w on w.id_row = l.object_id
where act.object_type = 1 and view_detail=1  and l.CreatedBy in ({listID}) and l.object_id = " + id + " order by l.CreatedDate";
                    DataTable dt = new DataTable();
                    dt = cnn.CreateDataTable(sql);
                    if (dt == null || cnn.LastError != null)
                        return JsonResultCommon.KhongTonTai("Công việc");
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
            string Token = Common.GetHeader(Request);
            UserJWT loginData = Ulities.GetUserByHeader(HttpContext.Request.Headers);
            if (loginData == null)
                return Unauthorized();
            try
            {
                if (string.IsNullOrEmpty(query.filter["id_project_team"]))
                    return BadRequest();
                #region Lấy dữ liệu account từ JeeAccount
                DataAccount = WeworkLiteController.GetAccountFromJeeAccount(HttpContext.Request.Headers, _configuration);
                if (DataAccount == null)
                    return BadRequest();

                string error = "";
                string listID = WeworkLiteController.ListAccount(HttpContext.Request.Headers, out error, _configuration);
                if (error != "")
                    return BadRequest();
                #endregion
                #region filter thời gian , keyword
                DateTime from = DateTime.Now;
                DateTime to = DateTime.Now;
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
                string ConnectionString = WeworkLiteController.getConnectionString(ConnectionCache, loginData.CustomerID, _configuration);
                using (DpsConnection cnn = new DpsConnection(ConnectionString))
                {
                    string strG = @"select null as id_row, N'Chưa phân loại' as title union
select id_row, title from we_group g where disabled=0 and id_project_team=" + query.filter["id_project_team"];
                    DataTable dtG = cnn.CreateDataTable(strG);
                    DataSet ds = getWork(cnn, query, loginData.UserID, DataAccount);
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
                    string sqlq = "select ISNULL((select count(*) from we_fields_project_team where disabled=0 and id_row = " + id + "),0)";
                    if (long.Parse(cnn.ExecuteScalar(sqlq).ToString()) != 1)
                        return JsonResultCommon.KhongTonTai("Công việc");
                    if (isdeleted)
                        sqlq = "update we_fields_project_team set disabled=1, UpdatedDate=getdate(), UpdatedBy=" + iduser + " where id_row = " + id + "";
                    else
                        sqlq = "update we_fields_project_team set IsHidden=" + hidden + ", UpdatedDate=getdate(), UpdatedBy=" + iduser + " where id_row = " + id + "";
                    SqlConditions sqlcond = new SqlConditions();
                    sqlcond.Add("id_row", id);
                    sqlcond.Add("disabled", 0);
                    cnn.BeginTransaction();
                    if (cnn.ExecuteNonQuery(sqlq) < 1)
                    {
                        cnn.RollbackTransaction();
                        return JsonResultCommon.Exception(_logger, cnn.LastError, _config, loginData, ControllerContext);
                    }
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
            string Token = Common.GetHeader(Request);
            UserJWT loginData = Ulities.GetUserByHeader(HttpContext.Request.Headers);
            if (loginData == null)
                return JsonResultCommon.DangNhap();
            try
            {
                string domain = _configuration.GetValue<string>("Host:JeeWork_API") + "/";
                string ConnectionString = WeworkLiteController.getConnectionString(ConnectionCache, loginData.CustomerID, _configuration);
                using (DpsConnection cnn = new DpsConnection(ConnectionString))
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
                    string sql = $@"";
                    #region Trả dữ liệu về backend để hiển thị lên giao diện
                    string sqlq = @$"select distinct w.id_row,w.title,w.description,w.id_project_team,w.id_group,w.deadline,w.id_milestone,w.milestone,
w.id_parent,w.start_date,w.end_date,w.urgent,w.important,w.prioritize,w.status,w.result,w.CreatedDate,w.CreatedBy,
w.UpdatedDate,w.UpdatedBy,w.NguoiGiao, w.project_team,w.id_department,w.clickup_prioritize 
, '' as hoten_nguoigiao, Iif(fa.id_row is null ,0,1) as favourite,
iIf(w.Status=2 and w.end_date>w.deadline,1,0) as is_htquahan,
iIf(w.Status = 2 and w.end_date <= w.deadline, 1, 0) as is_htdunghan ,
iIf(w.Status = 1 and  w.start_date is not null, 1, 0) as is_danglam,
iIf(w.Status = 1 and getdate() > w.deadline, 1, 0) as is_quahan,
iif(convert(varchar, w.deadline,103) like convert(varchar, GETDATE(),103),1,0) as duetoday,
iif(w.status=1 and w.start_date is null,1,0) as require,
'' as NguoiTao,'' as NguoiSua from v_wework_new w 
left join we_work_favourite fa on fa.id_work=w.id_row and fa.createdby=6 and fa.disabled=0
where w.CreatedBy in ({listID}) and w.id_row= " + id + " or id_parent=" + id;
                    //tag
                    sqlq += @";select a.title, a.id_row, a.color 
                    from we_tag a join we_work_tag b on a.id_row=b.id_tag 
                    where a.disabled=0 and b.disabled = 0 and id_work = " + id;
                    //người theo dõi
                    sqlq += @$";select id_work,id_user as id_nv,'' as hoten,''as mobile,''as username,''as email,''as image,''as tenchucdanh from we_work_user u 
where u.disabled = 0 and u.id_user in ({listID}) and u.loai = 2 and id_work=" + id;
                    //attachment
                    sqlq += @$";select a.*, '' as username from we_attachment a
where Disabled=0 and object_type in (1,11) and a.CreatedBy in ({listID}) and object_id=" + id;
                    // Quá trình xử lý
                    sqlq += @$";select process.*, '' as hoten, StatusName, we_status.Position, we_status.color
from we_work_process process
join we_status on we_status.id_row = process.StatusID
where we_status.disabled=0 and WorkID=" + id + " order by Position";
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
                    string select_user = $@"select  distinct w_user.id_user,'' as hoten,'' as image, id_work
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
                        }
                    }
                    #endregion
                    DataTable dt_projects = cnn.CreateDataTable($@"select id_row, icon, title, id_department
                                                                , loai, start_date, end_date, color, status 
                                                                , is_project, Locked, Disabled 
                                                                from we_project_team where Disabled = 0");
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
                                    deadline = string.Format("{0:dd/MM/yyyy HH:mm}", r["deadline"]),
                                    start_date = string.Format("{0:dd/MM/yyyy HH:mm}", r["start_date"]),
                                    end_date = string.Format("{0:dd/MM/yyyy HH:mm}", r["end_date"]),
                                    urgent = r["urgent"],
                                    important = r["important"],
                                    prioritize = r["prioritize"],
                                    favourite = r["favourite"],
                                    require = r["require"],
                                    status = r["status"],
                                    id_milestone = r["id_milestone"],
                                    milestone = r["milestone"],
                                    is_htquahan = r["is_htquahan"],
                                    is_htdunghan = r["is_htdunghan"],
                                    is_danglam = r["is_danglam"],
                                    is_quahan = r["is_quahan"],
                                    duetoday = r["duetoday"],
                                    CreatedDate = string.Format("{0:dd/MM/yyyy HH:mm}", r["CreatedDate"]),
                                    CreatedBy = r["CreatedBy"],
                                    NguoiTao = r["NguoiTao"],
                                    UpdatedDate = r["UpdatedDate"] == DBNull.Value ? "" : string.Format("{0:dd/MM/yyyy HH:mm}", r["UpdatedDate"]),
                                    UpdatedBy = r["UpdatedBy"],
                                    NguoiSua = r["NguoiSua"],
                                    NguoiGiao = r["NguoiGiao"],
                                    clickup_prioritize = r["clickup_prioritize"],
                                    User = from us in User.AsEnumerable()
                                           where r["id_row"].Equals(us["id_work"])
                                           select new
                                           {
                                               id_nv = us["id_user"],
                                               hoten = us["hoten"],
                                               //username = us["username"],
                                               //tenchucdanh = us["tenchucdanh"],
                                               //mobile = us["mobile"],
                                               image = us["image"],
                                               //image = WeworkLiteController.genLinkImage(domain, loginData.CustomerID, us["id_user"].ToString(), _hostingEnvironment.ContentRootPath),
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
                                                    //image = WeworkLiteController.genLinkImage(domain, loginData.CustomerID, f["id_nv"].ToString(), _hostingEnvironment.ContentRootPath)
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
                                                      path = WeworkLiteController.genLinkAttachment(_configuration, dr["path"]),
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
                                                             path = WeworkLiteController.genLinkAttachment(_configuration, dr["path"]),
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
                                              where r["status"].Equals(t["statusid"])
                                              select new
                                              {
                                                  workid = t["workid"],
                                                  statusname = t["StatusName"],
                                                  color = t["color"],
                                                  hoten = t["hoten"],
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
                                                         //image = WeworkLiteController.genLinkImage(domain, loginData.CustomerID, u["id_nv"].ToString(), _hostingEnvironment.ContentRootPath)
                                                     }
                                                 },
                                    Childs = from rr in ds.Tables[0].AsEnumerable()
                                             where rr["id_parent"].Equals(r["id_row"])
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
                                                 require = rr["require"],
                                                 status = rr["status"],
                                                 milestone = rr["milestone"],
                                                 is_htquahan = rr["is_htquahan"],
                                                 is_htdunghan = rr["is_htdunghan"],
                                                 is_danglam = rr["is_danglam"],
                                                 is_quahan = rr["is_quahan"],
                                                 duetoday = rr["duetoday"],
                                                 CreatedDate = string.Format("{0:dd/MM/yyyy HH:mm}", rr["CreatedDate"]),
                                                 CreatedBy = rr["CreatedBy"],
                                                 NguoiTao = rr["NguoiTao"],
                                                 UpdatedDate = rr["UpdatedDate"] == DBNull.Value ? "" : string.Format("{0:dd/MM/yyyy HH:mm}", rr["UpdatedDate"]),
                                                 UpdatedBy = rr["UpdatedBy"],
                                                 NguoiSua = rr["NguoiSua"],
                                                 NguoiGiao = rr["NguoiGiao"],
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
                                                            //image = WeworkLiteController.genLinkImage(domain, loginData.CustomerID, u["id_user"].ToString(), _hostingEnvironment.ContentRootPath)
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
            string Token = Common.GetHeader(Request);
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
                string ConnectionString = WeworkLiteController.getConnectionString(ConnectionCache, loginData.CustomerID, _configuration);
                using (DpsConnection cnn = new DpsConnection(ConnectionString))
                {
                    string listDept = WeworkLiteController.getListDepartment_GetData(loginData, cnn, HttpContext.Request.Headers, _configuration, ConnectionString);
                    #region Lấy dữ liệu account từ JeeAccount
                    DataAccount = WeworkLiteController.GetAccountFromJeeAccount(HttpContext.Request.Headers, _configuration);
                    if (DataAccount == null)
                        return JsonResultCommon.Custom("Lỗi lấy danh sách nhân viên từ hệ thống quản lý tài khoản");

                    string error = "";
                    string listID = WeworkLiteController.ListAccount(HttpContext.Request.Headers, out error, _configuration);
                    if (error != "")
                        return JsonResultCommon.Custom(error);
                    #endregion
                    if (string.IsNullOrEmpty(query.filter["id_project_team"]))
                        return JsonResultCommon.Custom("Dự án/phòng ban bắt buộc nhập");

                    #region filter thời gian , keyword
                    DateTime from = DateTime.Now;
                    DateTime to = DateTime.Now;
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
select id_row, title from we_group g where disabled=0 and id_project_team=" + query.filter["id_project_team"];
                    DataTable dtG = cnn.CreateDataTable(strG);
                    DataSet ds = await GetWork_ClickUp(cnn, query, loginData.UserID, DataAccount, listDept);
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
                    var rows = (from rr in dtG.AsEnumerable()
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
                                           parentId = rr["id_group"] == DBNull.Value ? "G0" : ("G" + rr["id_group"]),
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
        public async Task<object> Insert(WorkModel data)
        {
            string Token = Common.GetHeader(Request);
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

                string ConnectionString = WeworkLiteController.getConnectionString(ConnectionCache, loginData.CustomerID, _configuration);
                using (DpsConnection cnn = new DpsConnection(ConnectionString))
                {
                    #region Lấy dữ liệu account từ JeeAccount
                    DataAccount = WeworkLiteController.GetAccountFromJeeAccount(HttpContext.Request.Headers, _configuration);
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
                    else // Trường hợp người dùng không chọn status thì lấy status mặc định của ProjectTeam
                    {
                        DataTable dt = WeworkLiteController.StatusDynamic(data.id_project_team, new List<AccUsernameModel>(), ConnectionString);
                        if (dt.Rows.Count > 0)
                        {
                            DataRow[] RowStatus = dt.Select("IsDefault = 1 and IsFinal = 0");
                            if (RowStatus.Length > 0)
                            {
                                val.Add("status", RowStatus[0]["id_row"]);
                            }
                        }
                    }
                    if (data.start_date > DateTime.MinValue)
                    {
                        string return_date = "";
                        DateTime ngaykiemtra = WeworkLiteController.Check_ConditionDate("we_project_team", "end_date", data.id_project_team, out return_date, cnn);
                        if (ngaykiemtra > DateTime.MinValue)
                        {
                            if (data.start_date > ngaykiemtra)
                                return JsonResultCommon.Custom("Ngày bắt đầu phải nhỏ hơn ngày kết thúc của dự án" + return_date + "");
                        }
                        ngaykiemtra = WeworkLiteController.Check_ConditionDate("we_project_team", "start_date", data.id_project_team, out return_date, cnn);
                        if (ngaykiemtra > DateTime.MinValue)
                        {
                            if (ngaykiemtra > data.start_date)
                                return JsonResultCommon.Custom("Ngày bắt đầu phải lớn hơn hoặc bằng ngày bắt đầu của dự án" + return_date + "");
                        }
                        val.Add("start_date", data.start_date);
                    }
                    if (data.deadline > DateTime.MinValue)
                        val.Add("deadline", data.deadline);
                    if (data.id_group > 0)
                        val.Add("id_group", data.id_group);
                    if (data.id_milestone > 0)
                        val.Add("id_milestone", data.id_milestone);
                    val.Add("CreatedDate", DateTime.Now);
                    val.Add("CreatedBy", iduser);
                    val.Add("clickup_prioritize", data.urgent);
                    cnn.BeginTransaction();
                    if (cnn.Insert(val, "we_work") != 1)
                    {
                        cnn.RollbackTransaction();
                        return JsonResultCommon.Exception(_logger, cnn.LastError, _config, loginData, ControllerContext);
                    }
                    long idc = long.Parse(cnn.ExecuteScalar("select IDENT_CURRENT('we_work')").ToString());
                    
                    // Insert người follow cho từng tình trạng của công việc
                    dt_status = WeworkLiteController.StatusDynamic(data.id_project_team, new List<AccUsernameModel>(), ConnectionString);
                    if (dt_status.Rows.Count > 0)
                    {
                        foreach (DataRow item in dt_status.Rows)
                        {
                            val = new Hashtable();
                            val.Add("id_project_team", data.id_project_team);
                            val.Add("WorkID", idc);
                            val.Add("StatusID", item["id_row"]);
                            if (string.IsNullOrEmpty(item["Follower"].ToString()))
                                val.Add("Checker", DBNull.Value);
                            else
                                val.Add("Checker", item["Follower"]);
                            val.Add("CreatedDate", DateTime.Now);
                            val.Add("CreatedBy", iduser);
                            if (cnn.Insert(val, "We_Work_Process") != 1)
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
                        val1["CreatedDate"] = DateTime.Now;
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
                        val2["CreatedDate"] = DateTime.Now;
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
                    //string LogContent = "", LogEditContent = "";
                    //LogContent = LogEditContent = "Thêm mới dữ liệu work: title=" + data.title + ", id_project_team=" + data.id_project_team;
                    //DpsPage.Ghilogfile(loginData.CustomerID.ToString(), LogEditContent, LogContent, loginData.UserName);
                    if (!WeworkLiteController.log(_logger, loginData.Username, cnn, 1, idc, iduser, data.title))
                    {
                        cnn.RollbackTransaction();
                        return JsonResultCommon.Exception(_logger, cnn.LastError, _config, loginData, ControllerContext);
                    }
                    cnn.EndTransaction();

                    // thông báo nhắc nhở
                    foreach (var user in data.Users)
                    {
                        if (user.loai == 1)
                        {
                            NhacNho.UpdateSoluongCV(user.id_user, loginData.CustomerID, "+", _configuration, _producer);
                        }
                    }
                    #region gửi event insert công việc mới
                    DataTable dtwork = cnn.CreateDataTable("select * from v_wework_new where Disabled = 0 and id_row = " + idc);
                    Post_Automation_Model postauto = new Post_Automation_Model();
                    postauto.taskid = idc; // id task mới tạo
                    postauto.userid = loginData.UserID;
                    postauto.listid = data.id_project_team; // id project team
                    postauto.customerid = loginData.CustomerID;
                    postauto.eventid = 7;
                    postauto.departmentid = long.Parse(dtwork.Rows[0]["id_department"].ToString()); // id_department
                    Automation.SendAutomation(postauto, _configuration, _producer);
                    #endregion
                    data.id_row = idc;
                    WeworkLiteController.mailthongbao(idc, data.Users.Select(x => x.id_user).ToList(), 10, loginData, ConnectionString, _notifier);
                    #region Notify thêm mới công việc
                    Hashtable has_replace = new Hashtable();
                    for (int i = 0; i < data.Users.Count; i++)
                    {
                        NotifyModel notify_model = new NotifyModel();
                        has_replace = new Hashtable();
                        has_replace.Add("nguoigui", loginData.Username);
                        has_replace.Add("tencongviec", data.title);
                        notify_model.AppCode = "WW";
                        notify_model.From_IDNV = loginData.UserID.ToString();
                        notify_model.To_IDNV = data.Users[i].id_user.ToString();
                        notify_model.TitleLanguageKey = "ww_themmoicongviec";
                        notify_model.ReplaceData = has_replace;
                        notify_model.To_Link_MobileApp = "";
                        notify_model.To_Link_WebApp = "/tasks?detail=" + data.id_row + "";
                        try
                        {
                            if (notify_model != null)
                            {
                                Knoti = new APIModel.Models.Notify();
                                bool kq = Knoti.PushNotify(notify_model.From_IDNV, notify_model.To_IDNV, notify_model.AppCode, notify_model.TitleLanguageKey, notify_model.ReplaceData, notify_model.To_Link_WebApp, notify_model.To_Link_MobileApp, notify_model.ComponentName, notify_model.Component);
                            }
                        }
                        catch
                        { }

                        var info = DataAccount.Where(x => notify_model.To_IDNV.ToString().Contains(x.UserId.ToString())).FirstOrDefault();
                        if (info is not null)
                        {
                            bool kq_noti = WeworkLiteController.SendNotify(loginData.Username, info.Username, notify_model);
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
        /// Update thông tin công việc
        /// <param name="data"></param>
        /// <returns></returns>
        [Route("Update")]
        [HttpPost]
        public async Task<BaseModel<object>> Update(WorkModel data)
        {
            string Token = Common.GetHeader(Request);
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

                    val.Add("UpdatedDate", DateTime.Now);
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
                    //    string strDel = "Update we_work_user set Disabled=1, UpdatedDate=getdate(), UpdatedBy=" + iduser + " where Disabled=0 and loai=1 and id_work=" + data.id_row + " and id_row not in (" + ids + ")";
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
                    //        val1["CreatedDate"] = DateTime.Now;
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
                    var vals = WeworkLiteController.CheckKeyChange(keys, old, dt);
                    if (vals[0])
                    {
                        if (!WeworkLiteController.log(_logger, loginData.Username, cnn, 17, data.id_row, iduser, "", old.Rows[0]["title"], data.title))
                        {
                            cnn.RollbackTransaction();
                            return JsonResultCommon.Exception(_logger, cnn.LastError, _config, loginData, ControllerContext);
                        }
                    }
                    if (vals[1])
                    {
                        if (!WeworkLiteController.log(_logger, loginData.Username, cnn, 16, data.id_row, iduser, "", old.Rows[0]["description"], data.description))
                        {
                            cnn.RollbackTransaction();
                            return JsonResultCommon.Exception(_logger, cnn.LastError, _config, loginData, ControllerContext);
                        }
                    }
                    if (vals[2])
                    {
                        if (!WeworkLiteController.log(_logger, loginData.Username, cnn, 12, data.id_row, iduser, "", old.Rows[0]["id_group"], data.id_group))
                        {
                            cnn.RollbackTransaction();
                            return JsonResultCommon.Exception(_logger, cnn.LastError, _config, loginData, ControllerContext);
                        }
                    }
                    WeworkLiteController.mailthongbao(data.id_row, data.Users.Select(x => x.id_user).ToList(), 10, loginData, ConnectionString, _notifier);
                    cnn.EndTransaction();
                    #region Notify chỉnh sửa công việc
                    Hashtable has_replace = new Hashtable();
                    for (int i = 0; i < data.Users.Count; i++)
                    {
                        NotifyModel notify_model = new NotifyModel();
                        has_replace = new Hashtable();
                        has_replace.Add("nguoigui", loginData.Username);
                        has_replace.Add("tencongviec", data.title);
                        notify_model.AppCode = "WW";
                        notify_model.From_IDNV = loginData.UserID.ToString();
                        notify_model.To_IDNV = data.Users[i].id_user.ToString();
                        notify_model.TitleLanguageKey = "ww_chinhsuacongviec";
                        notify_model.ReplaceData = has_replace;
                        notify_model.To_Link_MobileApp = "";
                        notify_model.To_Link_WebApp = "/tasks/detail/" + data.id_row + "";
                        try
                        {
                            if (notify_model != null)
                            {
                                Knoti = new APIModel.Models.Notify();
                                bool kq = Knoti.PushNotify(notify_model.From_IDNV, notify_model.To_IDNV, notify_model.AppCode, notify_model.TitleLanguageKey, notify_model.ReplaceData, notify_model.To_Link_WebApp, notify_model.To_Link_MobileApp, notify_model.ComponentName, notify_model.Component);
                            }
                        }
                        catch
                        { }

                        var info = DataAccount.Where(x => notify_model.To_IDNV.ToString().Contains(x.UserId.ToString())).FirstOrDefault();
                        if (info is not null)
                        {
                            bool kq_noti = WeworkLiteController.SendNotify(loginData.Username, info.Username, notify_model);
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
                    SqlConditions sqlcond = new SqlConditions();
                    sqlcond.Add("fieldname", data.columnname);
                    sqlcond.Add("id_project_team", data.id_project_team);
                    sqlcond.Add("disabled", 0);
                    string s = "select  id_project_team, fieldname, CreatedDate, CreatedBy, Disabled, ObjectID, position from we_fields_project_team where (where)";
                    DataTable dt = cnn.CreateDataTable(s, "(where)", sqlcond);
                    if (dt == null || dt.Rows.Count == 0 || data.isnewfield)
                    {
                        Hashtable val = new Hashtable();
                        val.Add("CreatedDate", DateTime.Now);
                        val.Add("CreatedBy", iduser);
                        val.Add("fieldname", data.columnname);
                        val.Add("id_project_team", data.id_project_team);
                        val.Add("ObjectID", 1);
                        val.Add("Disabled", 0);
                        double so = 0;
                        string position = cnn.ExecuteScalar("select Max(position) from we_fields_project_team where id_project_team =" + data.id_project_team + "").ToString();
                        if (position == "")
                            position = so.ToString();
                        so = double.Parse(position.ToString());
                        val.Add("position", so + 1);
                        if (data.isnewfield)
                        {
                            string strRe = "";
                            if (string.IsNullOrEmpty(data.Title))
                                strRe += (strRe == "" ? "" : ",") + "tên công việc";
                            if (strRe != "")
                                return JsonResultCommon.BatBuoc(strRe);
                            val.Add("Title", data.Title);
                            val.Add("IsNewField", 1);
                        }
                        else
                            val.Add("IsNewField", 0);
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
                                val["ID_project_team"] = op.ID_project_team;
                                val["TypeID"] = op.TypeID;
                                val["FieldID"] = FieldID;
                                val["Value"] = op.Value;
                                val["Color"] = op.Color;
                                if (!string.IsNullOrEmpty(op.Note))
                                    val["Note"] = op.Note;
                                else
                                    val["Note"] = DBNull.Value;
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
                        cnn.ExecuteNonQuery("delete we_fields_project_team where id_project_team =" + data.id_project_team + " and fieldname = '" + data.columnname + "'");
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
        public object Detail_column_new_field(long field)
        {
            string Token = Common.GetHeader(Request);
            UserJWT loginData = Ulities.GetUserByHeader(HttpContext.Request.Headers);
            if (loginData == null)
                return JsonResultCommon.DangNhap();
            try
            {
                string ConnectionString = WeworkLiteController.getConnectionString(ConnectionCache, loginData.CustomerID, _configuration);
                using (DpsConnection cnn = new DpsConnection(ConnectionString))
                {
                    #region Trả dữ liệu về backend để hiển thị lên giao diện
                    string sqlq = @"select id_row, fieldname,id_project_team,Title, IsHidden 
                                    from we_fields_project_team
                                    where disabled=0 and id_row =" + field;
                    sqlq += @";select RowID, FieldID, TypeID, ID_project_team, Value, Color, Note 
                                from we_newfields_options where FieldID = " + field;
                    DataSet ds = cnn.CreateDataSet(sqlq);
                    if (cnn.LastError != null || ds == null)
                    {
                        return JsonResultCommon.Exception(_logger, cnn.LastError, _config, loginData, ControllerContext);
                    }
                    if (ds.Tables[0] == null || ds.Tables[0].Rows.Count == 0)
                        return JsonResultCommon.KhongTonTai();
                    var data = (from r in ds.Tables[0].AsEnumerable()
                                select new
                                {
                                    id_row = r["id_row"],
                                    fieldname = r["fieldname"],
                                    id_project_team = r["id_project_team"],
                                    title = r["Title"],
                                    ishidden = r["IsHidden"],
                                    options = from op in ds.Tables[1].AsEnumerable()
                                              select new
                                              {
                                                  rowid = op["RowID"],
                                                  FieldID = op["FieldID"],
                                                  TypeID = op["TypeID"],
                                                  ID_project_team = op["ID_project_team"],
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
        /// Update cột ứng với từng project
        /// // 1 - Kéo từ list - sang list (Chung project) thì thay đổi vị trí, 2- kéo từ check list - list, 3 - Kéo từ status - status, 4 - kéo từ list - sang list, 5 - kéo thay đổi vị trí cột
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        [Route("drap-drop-item")]
        [HttpPost]
        public async Task<BaseModel<object>> DragDropItemWork(DragDropModel data)
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
                                    val.Add("UpdatedDate", DateTime.Now);
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
                                    val.Add("UpdatedDate", DateTime.Now);
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
                                val.Add("UpdatedDate", DateTime.Now);
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
                                val.Add("UpdatedDate", DateTime.Now);
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
                    val.Add("UpdatedDate", DateTime.Now);
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
            string Token = Common.GetHeader(Request);
            UserJWT loginData = Ulities.GetUserByHeader(HttpContext.Request.Headers);
            if (loginData == null)
                return JsonResultCommon.DangNhap();
            try
            {
                string log_content = "";
                string ConnectionString = WeworkLiteController.getConnectionString(ConnectionCache, loginData.CustomerID, _configuration);
                using (DpsConnection cnn = new DpsConnection(ConnectionString))
                {
                    #region Lấy dữ liệu account từ JeeAccount
                    DataAccount = WeworkLiteController.GetAccountFromJeeAccount(HttpContext.Request.Headers, _configuration);
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
                    string sql_status = "select id_row, IsDeadline, IsToDo, IsFinal,IsDefault " +
                    "from we_status where disabled = 0 and id_project_team = @id_project_team";
                    DataTable dt_StatusID = new DataTable();
                    dt_StatusID = cnn.CreateDataTable(sql_status, new SqlConditions() { { "id_project_team", id_project_team.ToString() } });
                    if (data.key != "Tags" && data.key != "Attachments" && data.key != "assign" && data.key != "follower")
                    {
                        Hashtable val = new Hashtable();
                        val.Add("UpdatedDate", DateTime.Now);
                        val.Add("UpdatedBy", iduser);
                        cnn.BeginTransaction();
                        // Xử lý riêng cho update status
                        if ("status".Equals(data.key))
                        {
                            bool isFinal = long.Parse(cnn.ExecuteScalar("select count(*) from we_status where id_row = " + data.value + " and IsFinal = 1").ToString()) > 0;
                            bool hasDeadline = long.Parse(cnn.ExecuteScalar("select count(*) from we_status where id_row = (select status from v_wework_clickup_new where id_row = "+data.id_row+") and IsDeadline = 1").ToString()) > 0;
                            bool isDeadline = long.Parse(cnn.ExecuteScalar("select count(*) from we_status where id_row = " + data.value + " and IsDeadline = 1").ToString()) > 0;
                            string StatusID = "";
                            if ("complete".Equals(data.status_type))
                            {
                                sql_status += " and isfinal = 1 ";
                                StatusID = cnn.ExecuteScalar(sql_status, new SqlConditions() { { "id_project_team", id_project_team.ToString() } }).ToString();
                                if (StatusID != null)
                                {
                                    val.Add(data.key, StatusID);
                                    data.value = StatusID;
                                }
                            }
                            else // Xử lý trường hợp người dùng next status
                            {
                                if ("next".Equals(data.status_type)) // Lấy status tiếp theo
                                {
                                    sql_status += " and id_row > (select id_row from we_status where disabled = 0 and id_project_team = @id_project_team and id_row = " + data.value + ") order by IsFinal,id_row";
                                    dt_StatusID = cnn.CreateDataTable(sql_status, new SqlConditions() { { "id_project_team", id_project_team.ToString() } });
                                    if (dt_StatusID.Rows.Count > 0)
                                        StatusID = dt_StatusID.Rows[0][0].ToString();
                                    else // Nếu đã next vị trí cuối rồi, cho closed luôn
                                        StatusID = cnn.ExecuteScalar("select id_row from we_status where disabled = 0 and id_project_team = @id_project_team and isfinal = 1 order by Position", new SqlConditions() { { "id_project_team", dt_infowork.Rows[0]["id_project_team"].ToString() } }).ToString();
                                    if (StatusID != null)
                                    {
                                        val.Add(data.key, StatusID);
                                        data.value = StatusID;
                                        if (long.Parse(cnn.ExecuteScalar("select count(*) from we_status where id_row = " + StatusID + " and IsFinal = 1").ToString()) > 0)
                                        {
                                            val.Add("end_date", DateTime.Now);
                                        }
                                        else
                                        {
                                            val.Add("end_date", DBNull.Value);
                                        }
                                    }
                                }
                                else
                                {
                                    val.Add("status", data.value);
                                    if (isFinal)
                                    {
                                        val.Add("end_date", DateTime.Now);
                                    }
                                    else
                                    {
                                        val.Add("end_date", DBNull.Value);
                                    }
                                }
                            }
                            #region gửi thông báo nhắc nhở
                            if (isDeadline)
                            {
                                foreach (long idUser in danhsachU)
                                {
                                    NhacNho.UpdateSoluongCVHetHan(idUser, loginData.CustomerID, "+", _configuration, _producer);
                                }
                            }
                            if (isFinal)
                            {
                                foreach (long idUser in danhsachU)
                                {
                                    NhacNho.UpdateCVHoanthanh(idUser, loginData.CustomerID, hasDeadline, _configuration, _producer);
                                }
                            }
                            #endregion
                        }
                        else
                        {
                            if (data.value == null)
                                val.Add(data.key, DBNull.Value);
                            else
                                val.Add(data.key, data.value);
                            #region Nếu key là deadline thì kiểm tra rồi cập nhật status tương ứng (Nếu đã trễ thì update IsDeadline, ngược lại update IsTodo)
                            if ("deadline".Equals(data.key))
                            {
                                DateTime values = Convert.ToDateTime(data.value.ToString());
                                string return_date = "";
                                DateTime ngaykiemtra = WeworkLiteController.Check_ConditionDate("we_project_team", "end_date", id_project_team, out return_date, cnn);
                                if (ngaykiemtra > DateTime.MinValue)
                                {
                                    if (values > ngaykiemtra)
                                        return JsonResultCommon.Custom("Hạn chót phải nhỏ hơn ngày kết thúc của dự án " + string.Format("{0:MM/dd/yyyy}", return_date) + "");
                                }
                                ngaykiemtra = WeworkLiteController.Check_ConditionDate("we_project_team", "start_date", id_project_team, out return_date, cnn);
                                if (ngaykiemtra > DateTime.MinValue)
                                {
                                    if (ngaykiemtra > values)
                                        return JsonResultCommon.Custom("Hạn chót phải lớn hơn hoặc bằng ngày bắt đầu của dự án " + string.Format("{0:MM/dd/yyyy}", return_date) + "");
                                }
                                DateTime deadline = DateTime.Now;
                                if (DateTime.TryParse(data.value.ToString(), out deadline))
                                {
                                    if (deadline > DateTime.Now)
                                    {
                                        #region Kiểm tra Nếu công việc trước đó có IsDeadline = 1
                                        var statusNew = cnn.ExecuteScalar("select * from we_status where disabled=0 " +
                                            "and id_project_team = " + id_project_team + " and id_row = " + StatusPresent + " and (IsDeadline = 1)");
                                        #endregion
                                        if (statusNew != null)
                                        {
                                            #region Trường hợp công việc từ deadline giãn ngày ra
                                            DataRow[] RowStatus = dt_StatusID.Select("IsToDo = 1");
                                            if (RowStatus.Length > 0)
                                            {
                                                StatusPresent = long.Parse(RowStatus[0]["id_row"].ToString());
                                            }
                                            #endregion
                                            val.Add("status", StatusPresent);

                                            foreach (long idUser in danhsachU)
                                            {
                                                NhacNho.UpdateSoluongCVHetHan(idUser, loginData.CustomerID, "-", _configuration, _producer);
                                            }
                                        }
                                    }
                                    else
                                    {
                                        DataRow[] RowStatus = dt_StatusID.Select("IsDeadline = 1");
                                        if (RowStatus.Length > 0)
                                        {
                                            StatusPresent = long.Parse(RowStatus[0]["id_row"].ToString());
                                        }
                                        val.Add("status", StatusPresent);

                                        foreach (long idUser in danhsachU)
                                        {
                                            NhacNho.UpdateSoluongCVHetHan(idUser, loginData.CustomerID, "+", _configuration, _producer);
                                        }
                                    }
                                }

                                
                            }
                            #endregion
                        }
                        if ("deadline".Equals(data.key) || "start_date".Equals(data.key))
                        {
                            DateTime values = DateTime.Now;
                            if (DateTime.TryParse(data.value.ToString(), out values))
                            {
                                if ("deadline".Equals(data.key))
                                {
                                    if (!string.IsNullOrEmpty(dt_infowork.Rows[0]["start_date"].ToString()))
                                    {
                                        if (values < DateTime.Parse(dt_infowork.Rows[0]["start_date"].ToString()))
                                        {
                                            return JsonResultCommon.Custom("Hạn chót phải lớn hơn ngày bắt đầu");
                                        }
                                    }
                                }
                                else
                                {
                                    if (!string.IsNullOrEmpty(dt_infowork.Rows[0]["deadline"].ToString()))
                                    {
                                        if (values > DateTime.Parse(dt_infowork.Rows[0]["deadline"].ToString()))
                                        {
                                            return JsonResultCommon.Custom("Ngày bắt đầu nhỏ hơn hạn chót");
                                        }
                                    }
                                }
                            }

                        }
                        
                        if (cnn.Update(val, sqlcond, "we_work") != 1)
                        {
                            cnn.RollbackTransaction();
                            return JsonResultCommon.Exception(_logger, cnn.LastError, _config, loginData, ControllerContext);
                        }
                        // gửi event sau khi update thành công
                        #region gửi event đổi độ ưu tiên công việc
                        if ("clickup_prioritize".Equals(data.key))
                        {
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
                        }
                        #endregion
                        #region gửi event post automation -- deadline or start_date
                        if ("deadline".Equals(data.key)) // key deadline
                        {
                            postauto.eventid = 3;
                            Automation.SendAutomation(postauto, _configuration, _producer);
                        }
                        else if ("start_date".Equals(data.key))// key start_date
                        {
                            postauto.eventid = 4;
                            Automation.SendAutomation(postauto, _configuration, _producer);
                        }
                        #endregion
                        if (data.key == "title")
                        {
                            #region Check dự án đó có gửi gửi mail khi chỉnh sửa công việc hay không
                            if (WeworkLiteController.CheckNotify_ByConditions(data.id_row, "email_update_work", false, ConnectionString))
                            {
                                DataTable dt_user = cnn.CreateDataTable("select id_nv, title, id_row from v_wework_new where (where)", "(where)", sqlcond);
                                if (dt_user.Rows.Count > 0)
                                {
                                    if (data.IsStaff)
                                    {
                                        var users = new List<long> { long.Parse(dt_user.Rows[0]["id_nv"].ToString()) };
                                        cnn.EndTransaction();
                                        WeworkLiteController.mailthongbao(data.id_row, users, 11, loginData, ConnectionString, _notifier, old);
                                        #region Notify chỉnh sửa công việc
                                        Hashtable has_replace = new Hashtable();
                                        for (int i = 0; i < users.Count; i++)
                                        {
                                            NotifyModel notify_model = new NotifyModel();
                                            has_replace = new Hashtable();
                                            has_replace.Add("nguoigui", loginData.Username);
                                            has_replace.Add("tencongviec", workname);
                                            notify_model.AppCode = "WW";
                                            notify_model.From_IDNV = loginData.UserID.ToString();
                                            notify_model.To_IDNV = users[i].ToString();
                                            notify_model.TitleLanguageKey = "ww_chinhsuacongviec";
                                            notify_model.ReplaceData = has_replace;
                                            notify_model.To_Link_MobileApp = "";
                                            notify_model.To_Link_WebApp = "/tasks/detail/" + data.id_row + "";
                                            try
                                            {
                                                if (notify_model != null)
                                                {
                                                    Knoti = new APIModel.Models.Notify();
                                                    bool kq = Knoti.PushNotify(notify_model.From_IDNV, notify_model.To_IDNV, notify_model.AppCode, notify_model.TitleLanguageKey, notify_model.ReplaceData, notify_model.To_Link_WebApp, notify_model.To_Link_MobileApp, notify_model.ComponentName, notify_model.Component);
                                                }
                                            }
                                            catch
                                            { }

                                            var info = DataAccount.Where(x => notify_model.To_IDNV.ToString().Contains(x.UserId.ToString())).FirstOrDefault();
                                            if (info is not null)
                                            {
                                                bool kq_noti = WeworkLiteController.SendNotify(loginData.Username, info.Username, notify_model);
                                            }
                                        }
                                        #endregion
                                    }
                                }
                            }
                            #endregion
                        }
                        if (data.key == "deadline")
                        {
                            #region Check dự án đó có gửi gửi mail khi chỉnh sửa công việc hay không
                            if (WeworkLiteController.CheckNotify_ByConditions(data.id_row, "email_update_work", false, ConnectionString))
                            {
                                DataTable dt_user = cnn.CreateDataTable("select id_nv, title, id_row from v_wework_new where (where)", "(where)", sqlcond);
                                if (dt_user.Rows.Count > 0)
                                {
                                    if (data.IsStaff)
                                    {
                                        var users = new List<long> { long.Parse(dt_user.Rows[0]["id_nv"].ToString()) };
                                        cnn.EndTransaction();
                                        WeworkLiteController.mailthongbao(data.id_row, users, 12, loginData, ConnectionString, _notifier, old);
                                        #region Notify chỉnh sửa công việc
                                        Hashtable has_replace = new Hashtable();
                                        for (int i = 0; i < users.Count; i++)
                                        {
                                            NotifyModel notify_model = new NotifyModel();
                                            has_replace = new Hashtable();
                                            has_replace.Add("nguoigui", loginData.Username);
                                            has_replace.Add("tencongviec", workname);
                                            notify_model.AppCode = "WW";
                                            notify_model.From_IDNV = loginData.UserID.ToString();
                                            notify_model.To_IDNV = users[i].ToString();
                                            notify_model.TitleLanguageKey = "ww_chinhsuadeadline";
                                            notify_model.ReplaceData = has_replace;
                                            notify_model.To_Link_MobileApp = "";
                                            notify_model.To_Link_WebApp = "/tasks/detail/" + data.id_row + "";
                                            try
                                            {
                                                if (notify_model != null)
                                                {
                                                    Knoti = new APIModel.Models.Notify();
                                                    bool kq = Knoti.PushNotify(notify_model.From_IDNV, notify_model.To_IDNV, notify_model.AppCode, notify_model.TitleLanguageKey, notify_model.ReplaceData, notify_model.To_Link_WebApp, notify_model.To_Link_MobileApp, notify_model.ComponentName, notify_model.Component);
                                                }
                                            }
                                            catch
                                            { }

                                            var info = DataAccount.Where(x => notify_model.To_IDNV.ToString().Contains(x.UserId.ToString())).FirstOrDefault();
                                            if (info is not null)
                                            {
                                                bool kq_noti = WeworkLiteController.SendNotify(loginData.Username, info.Username, notify_model);
                                            }
                                        }
                                        #endregion
                                    }
                                }
                            }
                            #endregion
                        }
                        if (data.key == "status")
                        {
                            #region gửi event post automation
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
                            #endregion
                            WeworkLiteController.ProcessWork(data.id_row, long.Parse(data.value.ToString()), loginData, _config, ConnectionString, _notifier);
                            DataTable dt_user = cnn.CreateDataTable("select id_nv, title, id_row from v_wework_new where (where)", "(where)", sqlcond);
                            if (dt_user.Rows.Count > 0)
                            {
                                #region Check dự án đó có gửi gửi mail khi cập nhật tình trạng hay không
                                if (WeworkLiteController.CheckNotify_ByConditions(data.id_row, "email_update_status", false, ConnectionString))
                                {
                                    if (data.IsStaff)
                                    {
                                        var users = new List<long> { long.Parse(dt_user.Rows[0]["id_nv"].ToString()) };
                                        if (int.Parse(data.value.ToString()) == 2)
                                        {
                                            WeworkLiteController.mailthongbao(data.id_row, users, 13, loginData, ConnectionString, _notifier);
                                            #region Notify cập nhật trạng thái công việc hoàn thành
                                            Hashtable has_replace = new Hashtable();
                                            for (int i = 0; i < users.Count; i++)
                                            {
                                                NotifyModel notify_model = new NotifyModel();
                                                has_replace = new Hashtable();
                                                has_replace.Add("nguoigui", loginData.Username);
                                                has_replace.Add("tencongviec", workname);
                                                notify_model.AppCode = "WW";
                                                notify_model.From_IDNV = loginData.UserID.ToString();
                                                notify_model.To_IDNV = users[i].ToString();
                                                notify_model.TitleLanguageKey = "ww_capnhattrangthaicongviec_done";
                                                notify_model.ReplaceData = has_replace;
                                                notify_model.To_Link_MobileApp = "";
                                                notify_model.To_Link_WebApp = "/tasks/detail/" + data.id_row + "";
                                                try
                                                {
                                                    if (notify_model != null)
                                                    {
                                                        Knoti = new APIModel.Models.Notify();
                                                        bool kq = Knoti.PushNotify(notify_model.From_IDNV, notify_model.To_IDNV, notify_model.AppCode, notify_model.TitleLanguageKey, notify_model.ReplaceData, notify_model.To_Link_WebApp, notify_model.To_Link_MobileApp, notify_model.ComponentName, notify_model.Component);
                                                    }
                                                }
                                                catch
                                                { }

                                                var info = DataAccount.Where(x => notify_model.To_IDNV.ToString().Contains(x.UserId.ToString())).FirstOrDefault();
                                                if (info is not null)
                                                {
                                                    bool kq_noti = WeworkLiteController.SendNotify(loginData.Username, info.Username, notify_model);
                                                }
                                            }
                                            #endregion
                                        }
                                        if (int.Parse(data.value.ToString()) == 1)
                                        {
                                            WeworkLiteController.mailthongbao(data.id_row, users, 14, loginData, ConnectionString, _notifier);
                                            #region Notify cập nhật trạng thái công việc đang làm
                                            Hashtable has_replace = new Hashtable();
                                            for (int i = 0; i < users.Count; i++)
                                            {
                                                NotifyModel notify_model = new NotifyModel();
                                                has_replace = new Hashtable();
                                                has_replace.Add("nguoigui", loginData.Username);
                                                has_replace.Add("tencongviec", workname);
                                                notify_model.AppCode = "WW";
                                                notify_model.From_IDNV = loginData.UserID.ToString();
                                                notify_model.To_IDNV = users[i].ToString();
                                                notify_model.TitleLanguageKey = "ww_capnhattrangthaicongviec_todo";
                                                notify_model.ReplaceData = has_replace;
                                                notify_model.To_Link_MobileApp = "";
                                                notify_model.To_Link_WebApp = "/tasks/detail/" + data.id_row + "";
                                                try
                                                {
                                                    if (notify_model != null)
                                                    {
                                                        Knoti = new APIModel.Models.Notify();
                                                        bool kq = Knoti.PushNotify(notify_model.From_IDNV, notify_model.To_IDNV, notify_model.AppCode, notify_model.TitleLanguageKey, notify_model.ReplaceData, notify_model.To_Link_WebApp, notify_model.To_Link_MobileApp, notify_model.ComponentName, notify_model.Component);
                                                    }
                                                }
                                                catch
                                                { }

                                                var info = DataAccount.Where(x => notify_model.To_IDNV.ToString().Contains(x.UserId.ToString())).FirstOrDefault();
                                                if (info is not null)
                                                {
                                                    bool kq_noti = WeworkLiteController.SendNotify(loginData.Username, info.Username, notify_model);
                                                }
                                            }
                                            #endregion
                                        }
                                    }
                                }
                                #endregion
                            }
                        }
                    }
                    else
                    {
                        if (data.key == "assign" || data.key == "follower")//assign , follower cho 1 người mới hoặc xóa
                        {
                            if (data.value != null)
                            {
                                bool isAssign = false; // = true: thêm người, = false: xóa người
                                Hashtable val1 = new Hashtable();
                                val1["id_work"] = data.id_row;
                                val1["CreatedDate"] = DateTime.Now;
                                val1["CreatedBy"] = iduser;
                                if (string.IsNullOrEmpty(data.value.ToString()))
                                    val1["id_user"] = DBNull.Value;
                                else
                                    val1["id_user"] = data.value;
                                long loai = 1;
                                if (data.key == "follower")
                                    loai = 2;
                                val1["loai"] = loai;

                                SqlConditions sqlcond123 = new SqlConditions();
                                sqlcond123.Add("id_work", data.id_row);
                                sqlcond123.Add("id_user", data.value);
                                sqlcond123.Add("loai", loai);
                                var sql = @"select * from we_work_user where id_work = @id_work and id_user = @id_user and loai = @loai";
                                DataTable dtG = cnn.CreateDataTable(sql, sqlcond123);
                                if (dtG.Rows.Count > 0)
                                {
                                    if (cnn.Delete(sqlcond123, "we_work_user") != 1)
                                    {
                                        cnn.RollbackTransaction();
                                        return JsonResultCommon.Exception(_logger, cnn.LastError, _config, loginData, ControllerContext);
                                    }

                                    if (loai == 1) // loai = 1 Assign đếm lại nhắc nhở xóa người -1
                                    {
                                        NhacNho.UpdateSoluongCV(long.Parse(data.value.ToString()), loginData.CustomerID, "-", _configuration, _producer);
                                    }
                                }
                                else
                                {
                                    if (cnn.Insert(val1, "we_work_user") != 1)
                                    {
                                        cnn.RollbackTransaction();
                                        return JsonResultCommon.Exception(_logger, cnn.LastError, _config, loginData, ControllerContext);
                                    }
                                    isAssign = true;
                                    if (loai == 1) // loai = 1 Assign đếm lại nhắc nhở thêm người +1
                                    {
                                        NhacNho.UpdateSoluongCV(long.Parse(data.value.ToString()), loginData.CustomerID, "+", _configuration, _producer);
                                    }
                                }

                                #region Check dự án đó có gửi gửi mail khi chỉnh sửa công việc hay không
                                if (WeworkLiteController.CheckNotify_ByConditions(data.id_row, "email_update_work", false, ConnectionString))
                                {
                                    if (data.IsStaff) // Trường hợp Case update về giá trị NULL thì không gửi email (Người nhận = null nên gửi không được)
                                    {
                                        var users = new List<long> { long.Parse(data.value.ToString()) };
                                        WeworkLiteController.mailthongbao(data.id_row, users, 10, loginData, ConnectionString, _notifier);
                                        #region Notify assign
                                        Hashtable has_replace = new Hashtable();
                                        for (int i = 0; i < users.Count; i++)
                                        {
                                            NotifyModel notify_model = new NotifyModel();
                                            has_replace = new Hashtable();
                                            has_replace.Add("nguoigui", loginData.Username);
                                            has_replace.Add("tencongviec", workname);
                                            notify_model.AppCode = "WW";
                                            notify_model.From_IDNV = loginData.UserID.ToString();
                                            notify_model.To_IDNV = users[i].ToString();
                                            notify_model.TitleLanguageKey = "ww_assign";
                                            notify_model.ReplaceData = has_replace;
                                            notify_model.To_Link_MobileApp = "";
                                            notify_model.To_Link_WebApp = "/tasks/detail/" + data.id_row + "";
                                            try
                                            {
                                                if (notify_model != null)
                                                {
                                                    Knoti = new APIModel.Models.Notify();
                                                    bool kq = Knoti.PushNotify(notify_model.From_IDNV, notify_model.To_IDNV, notify_model.AppCode, notify_model.TitleLanguageKey, notify_model.ReplaceData, notify_model.To_Link_WebApp, notify_model.To_Link_MobileApp, notify_model.ComponentName, notify_model.Component);
                                                }
                                            }
                                            catch
                                            { }

                                            var info = DataAccount.Where(x => notify_model.To_IDNV.ToString().Contains(x.UserId.ToString())).FirstOrDefault();
                                            if (info is not null)
                                            {
                                                bool kq_noti = WeworkLiteController.SendNotify(loginData.Username, info.Username, notify_model);
                                            }
                                        }
                                        #endregion
                                    }
                                }
                                #endregion

                                #region gửi event post automation - giao việc
                                if (isAssign) // nếu được giao event = 5 else event = 6
                                {
                                    postauto.eventid = 5;
                                }
                                else
                                {
                                    postauto.eventid = 6;
                                }
                                //postauto.data_input = "any";
                                postauto.data_input += data.value; // giá trị mới
                                //postauto.data_input = datapost;
                                Automation.SendAutomation(postauto, _configuration, _producer);
                                #endregion
                            } //data.key == "assign"

                        }
                        if (data.key == "Tags")//thêm 1 tag mới
                        {
                            var f = cnn.ExecuteScalar("select count(*) from we_work_tag where disabled=0 and id_work=" + data.id_row + " and id_tag=" + data.value);
                            Hashtable val2 = new Hashtable();
                            if (int.Parse(f.ToString()) > 0) // Tag đã có => Delete
                            {
                                val2 = new Hashtable();
                                val2["UpdatedDate"] = DateTime.Now;
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
                                val2["CreatedDate"] = DateTime.Now;
                                val2["CreatedBy"] = iduser;
                                val2["id_tag"] = data.value;
                                if (cnn.Insert(val2, "we_work_tag") != 1)
                                {
                                    cnn.RollbackTransaction();
                                    return JsonResultCommon.Exception(_logger, cnn.LastError, _config, loginData, ControllerContext);
                                }
                            }
                        }
                        if (data.key == "Attachments" || data.key == "Attachments_result")//upload files mới
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
                        }
                    }

                    DataTable dt = cnn.CreateDataTable(s, "(where)", sqlcond);
                    if (data.id_log_action > 0)
                    {
                        bool re = true;
                        if (data.key != "Tags" && data.key != "Attachments")
                        {
                            string temp = data.key;
                            if (temp == "assign")
                                temp = "id_nv";
                            re = WeworkLiteController.log(_logger, loginData.Username, cnn, data.id_log_action, data.id_row, iduser, log_content, old.Rows[0][temp], data.value);
                        }
                        else
                            re = WeworkLiteController.log(_logger, loginData.Username, cnn, data.id_log_action, data.id_row, iduser, log_content);
                        if (!re)
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
        /// Nhân bản công việc
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        [Route("Duplicate")]
        [HttpPost]
        public async Task<object> Duplicate(WorkDuplicateModel data)
        {
            string Token = Common.GetHeader(Request);
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
                string ConnectionString = WeworkLiteController.getConnectionString(ConnectionCache, loginData.CustomerID, _configuration);
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
                    val.Add("CreatedDate", DateTime.Now);
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
                    val = new Hashtable();
                    val.Add("status", statusID);
                    SqlConditions cond = new SqlConditions();
                    cond.Add("id_row", workID_New);
                    cnn.BeginTransaction();
                    if (cnn.Update(val, cond, "we_work") <= 0)
                    {
                        cnn.RollbackTransaction();
                        return JsonResultCommon.Exception(_logger, cnn.LastError, _config, loginData, ControllerContext);
                    }
                    cnn.EndTransaction();
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
                    #region Lấy dữ liệu account từ JeeAccount
                    DataAccount = WeworkLiteController.GetAccountFromJeeAccount(HttpContext.Request.Headers, _configuration);
                    if (DataAccount == null)
                        return JsonResultCommon.Custom("Lỗi lấy danh sách nhân viên từ hệ thống quản lý tài khoản");
                    #endregion
                    string sqlq = "select ISNULL((select count(*) from we_work where disabled=0 and id_row = " + id + "),0)";
                    if (long.Parse(cnn.ExecuteScalar(sqlq).ToString()) != 1)
                        return JsonResultCommon.KhongTonTai("Công việc");
                    //code xóa cv con
                    string xoacon = " in ( select id_row from v_wework_clickup_new where (id_row = " + id + " or id_parent = " + id + ") and  disabled = 0 ) ";
                    //
                    sqlq = "update we_work set Disabled=1, UpdatedDate=getdate(), UpdatedBy=" + iduser + " where id_row " + xoacon;
                    SqlConditions sqlcond = new SqlConditions();
                    sqlcond.Add("id_row", id);
                    sqlcond.Add("disabled", 0);
                    DataTable dt_user = cnn.CreateDataTable("select id_nv, title, id_row from v_wework_new where (where)", "(where)", sqlcond);
                    DataTable dt_user1 = cnn.CreateDataTable("select id_nv, title, id_row from v_wework_new where id_nv is not null and (where)", "(where)", sqlcond);
                    cnn.BeginTransaction();
                    if (cnn.ExecuteNonQuery(sqlq) < 1)
                    {
                        cnn.RollbackTransaction();
                        return JsonResultCommon.Exception(_logger, cnn.LastError, _config, loginData, ControllerContext);
                    }
                    if (!WeworkLiteController.log(_logger, loginData.Username, cnn, 18, id, iduser))
                    {
                        cnn.RollbackTransaction();
                        return JsonResultCommon.Exception(_logger, cnn.LastError, _config, loginData, ControllerContext);
                    }
                    cnn.EndTransaction();
                    #region Check dự án đó có gửi gửi mail khi xóa không
                    if (WeworkLiteController.CheckNotify_ByConditions(id, "email_delete_work", false, ConnectionString))
                    {
                        if (dt_user.Rows.Count > 0)
                        {
                            var users = new List<long> { long.Parse(dt_user.Rows[0]["id_nv"].ToString()) };
                            WeworkLiteController.mailthongbao(id, users, 15, loginData, ConnectionString, _notifier);
                            object workname = cnn.ExecuteScalar("select title from we_work where Disabled = 1 and id_row = @id_row", new SqlConditions() { { "id_row", id } });
                            if (workname != null)
                                workname = workname.ToString();
                            #region Notify assign
                            Hashtable has_replace = new Hashtable();
                            for (int i = 0; i < users.Count; i++)
                            {
                                NotifyModel notify_model = new NotifyModel();
                                has_replace = new Hashtable();
                                has_replace.Add("nguoigui", loginData.Username);
                                has_replace.Add("tencongviec", workname);
                                notify_model.AppCode = "WW";
                                notify_model.From_IDNV = loginData.UserID.ToString();
                                notify_model.To_IDNV = users[i].ToString();
                                notify_model.TitleLanguageKey = "ww_xoacongviec";
                                notify_model.ReplaceData = has_replace;
                                notify_model.To_Link_MobileApp = "";
                                notify_model.To_Link_WebApp = "/tasks";
                                try
                                {
                                    if (notify_model != null)
                                    {
                                        Knoti = new APIModel.Models.Notify();
                                        bool kq = Knoti.PushNotify(notify_model.From_IDNV, notify_model.To_IDNV, notify_model.AppCode, notify_model.TitleLanguageKey, notify_model.ReplaceData, notify_model.To_Link_WebApp, notify_model.To_Link_MobileApp, notify_model.ComponentName, notify_model.Component);
                                    }
                                }
                                catch
                                { }

                                var info = DataAccount.Where(x => notify_model.To_IDNV.ToString().Contains(x.UserId.ToString())).FirstOrDefault();
                                if (info is not null)
                                {
                                    bool kq_noti = WeworkLiteController.SendNotify(loginData.Username, info.Username, notify_model);
                                }
                            }
                            #endregion
                        }
                    }
                    #endregion
                    if (cnn.LastError is null && dt_user1.Rows.Count > 0)
                    {
                        List<long> danhsachU = dt_user1.AsEnumerable().Select(x => long.Parse(x["id_nv"].ToString())).ToList();
                        foreach (long idUser in danhsachU)
                        {
                            NhacNho.UpdateSoluongCV(idUser, loginData.CustomerID, "-", _configuration, _producer);
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

        #region calendar
        [Route("list-event")]
        [HttpGet]
        public object ListEvent([FromQuery] QueryParams query)
        {
            string Token = Common.GetHeader(Request);
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
                string ConnectionString = WeworkLiteController.getConnectionString(ConnectionCache, loginData.CustomerID, _configuration);
                using (DpsConnection cnn = new DpsConnection(ConnectionString))
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

                    #region filter thời gian , keyword
                    DateTime from = DateTime.Now;
                    DateTime to = DateTime.Now;
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
                    DataSet ds = getWork(cnn, query, loginData.UserID, DataAccount, " and w.id_nv=@iduser");
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
                                       classNames = new List<string>() { rr["status"].ToString() == "2" ? "success" : "", rr["is_quahan"].ToString() == "1" ? "overdue" : "" },
                                       //imageurl = WeworkLiteController.genLinkImage(domain, loginData.UserID, rr["id_nv"].ToString(), _hostingEnvironment.ContentRootPath)
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

        [Route("list-event-by-project")]
        [HttpGet]
        public object ListEventByProject([FromQuery] QueryParams query)
        {
            string Token = Common.GetHeader(Request);
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
                string ConnectionString = WeworkLiteController.getConnectionString(ConnectionCache, loginData.CustomerID, _configuration);
                using (DpsConnection cnn = new DpsConnection(ConnectionString))
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

                    if (string.IsNullOrEmpty(query.filter["id_project_team"]))
                        return JsonResultCommon.Custom("Dự án/phòng ban bắt buộc nhập");
                    #region filter thời gian , keyword
                    DateTime from = DateTime.Now;
                    DateTime to = DateTime.Now;
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
                    DataSet ds = getWork(cnn, query, loginData.UserID, DataAccount);
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
                                       classNames = new List<string>() { rr["status"].ToString() == "2" ? "success" : "", rr["is_quahan"].ToString() == "1" ? "overdue" : "" },
                                       //imageurl = WeworkLiteController.genLinkImage(domain, loginData.UserID, rr["id_nv"].ToString(), _hostingEnvironment.ContentRootPath)
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
            string Token = Common.GetHeader(Request);
            UserJWT loginData = Ulities.GetUserByHeader(HttpContext.Request.Headers);
            if (loginData == null)
                return Unauthorized();
            try
            {
                #region Lấy dữ liệu account từ JeeAccount
                DataAccount = WeworkLiteController.GetAccountFromJeeAccount(HttpContext.Request.Headers, _configuration);
                if (DataAccount == null)
                    return BadRequest("Lỗi lấy danh sách nhân viên từ hệ thống quản lý tài khoản");
                string error = "";
                string listID = WeworkLiteController.ListAccount(HttpContext.Request.Headers, out error, _configuration);
                if (error != "")
                    return BadRequest(error);
                #endregion

                if (string.IsNullOrEmpty(query.filter["id_nv"]))
                    return BadRequest();

                #region filter thời gian , keyword
                DateTime from = DateTime.Now;
                DateTime to = DateTime.Now;
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
                string ConnectionString = WeworkLiteController.getConnectionString(ConnectionCache, loginData.CustomerID, _configuration);
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
                    string strW = " and (w.id_nv=@iduser or w.createdby=@iduser)"; // w.nguoigiao=@iduser or w.createdby=@iduser -- w.NguoiGiao = @iduser or
                    DataSet ds = getWork(cnn, query, long.Parse(query.filter["id_nv"].ToString()), DataAccount, strW);
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
                if (dr["is_htquahan"].ToString() == "1")
                    s = "Hoàn thành quá hạn";
                if (dr["is_htdunghan"].ToString() == "1")
                    s = "Hoàn thành";
                if (dr["is_danglam"].ToString() == "1")
                    s = "Đang làm";
                if (dr["is_quahan"].ToString() == "1")
                    s = "Quá hạn";
                _new[10] = s;//s == "1" ? "Đang làm" : (s == "2" ? "Hoàn thành" : "Chờ review");
                _new[11] = dr["result"];
                _new[12] = dr["milestone"];
                _new[13] = dr["createddate"] == DBNull.Value ? "" : string.Format("{0:dd/MM/yyyy HH:mm}", dr["createddate"]);
                _new[14] = dr["id_row"];
                dt.Rows.Add(_new);
                genDr(dtW, followers, tags, dr["id_row"], level + "#", ref dt);
            }
        }
        public static string getColorByName(string name)
        {
            string result = "";

            switch (name)
            {
                case "A":
                    result = "rgb(51 152 219)";
                    break;
                case "Ă":
                    result = "rgb(241, 196, 15)";
                    break;
                case "Â":
                    result = "rgb(142, 68, 173)";
                    break;
                case "B":
                    result = "rgb(142, 68, 173)";
                    break;
                case "C":
                    result = "rgb(211, 84, 0)";
                    break;
                case "D":
                    result = "rgb(44, 62, 80)";
                    break;
                case "Đ":
                    result = "rgb(127, 140, 141)";
                    break;
                case "E":
                    result = "rgb(26, 188, 156)";
                    break;
                case "Ê":
                    result = "rgb(51 152 219)";
                    break;
                case "G":
                    result = "rgb(241, 196, 15)";
                    break;
                case "H":
                    result = "rgb(142, 68, 173)";
                    break;
                case "I":
                    result = "rgb(142, 68, 173)";
                    break;
                case "K":
                    result = "rgb(211, 84, 0)";
                    break;
                case "L":
                    result = "rgb(44, 62, 80)";
                    break;
                case "M":
                    result = "rgb(127, 140, 141)";
                    break;
                case "N":
                    result = "rgb(51 152 219)";
                    break;
                case "O":
                    result = "rgb(51 152 219)";
                    break;
                case "Ô":
                    result = "rgb(241, 196, 15)";
                    break;
                case "Ơ":
                    result = "rgb(142, 68, 173)";
                    break;
                case "P":
                    result = "rgb(142, 68, 173)";
                    break;
                case "Q":
                    result = "rgb(211, 84, 0)";
                    break;
                case "R":
                    result = "rgb(44, 62, 80)";
                    break;
                case "S":
                    result = "rgb(127, 140, 141)";
                    break;
                case "T":
                    result = "rgb(51 152 219)";
                    break;
                case "U":
                    result = "rgb(51 152 219)";
                    break;
                case "Ư":
                    result = "rgb(241, 196, 15)";
                    break;
                case "V":
                    result = "rgb(211, 84, 0)";
                    break;
                case "X":
                    result = "rgb(142, 68, 173)";
                    break;
                case "W":
                    result = "rgb(211, 84, 0)";
                    break;
            }
            return result;
        }
        public static DataTable dtChildren(string id_parent, DataTable data, DpsConnection cnn, DataTable dataField, string id_project_team, List<AccUsernameModel> DataAccount)
        {
            DataTable result = new DataTable();
            foreach (DataRow item in dataField.Rows)
            {
                DataColumnCollection columns = result.Columns;
                if (!columns.Contains(item["fieldName"].ToString()))
                {
                    result.Columns.Add(item["fieldName"].ToString());
                }
            }
            string queryTag = @"select a.id_row,a.title,a.color,b.id_work 
                                from we_tag a join we_work_tag b 
                                on a.id_row=b.id_tag 
                                where a.disabled=0 and b.disabled = 0 
                                and a.id_project_team = " + id_project_team + " " +
                                "and id_work = ";
            //DataTable dt_Tags = cnn.CreateDataTable(queryTag);
            string queryUser = $@"select w_user.id_work, w_user.id_user, w_user.loai
                                , id_child, w_user.disabled, '' as hoten, id_project_team
                                from we_work_user w_user 
                                join we_work 
                                on we_work.id_row = w_user.id_work 
                                where w_user.Disabled = 0 
                                and we_work.id_project_team = " + id_project_team + " and id_work = ";
            result.Columns.Add("Tags", typeof(DataTable));
            result.Columns.Add("User", typeof(DataTable));
            //result.Columns.Add("comments", typeof(string));


            result.Columns.Add("DataChildren", typeof(DataTable));
            DataRow[] row = data.Select("id_parent in (" + id_parent + ")");
            foreach (DataRow dr in row)
            {
                DataRow drow = result.NewRow();
                foreach (DataRow field in dataField.Rows)
                {
                    if (!(bool)field["isnewfield"])
                        drow[field["fieldName"].ToString()] = dr[field["fieldName"].ToString()];
                }
                drow["DataChildren"] = dtChildren(dr["id_row"].ToString(), data, cnn, dataField, id_project_team, DataAccount);
                result.Rows.Add(drow);
            }
            if (result.Rows.Count > 0)
            {
                foreach (DataRow dr in result.Rows)
                {
                    dr["Tags"] = cnn.CreateDataTable(queryTag + dr["id_row"]);
                    DataTable user = cnn.CreateDataTable(queryUser + dr["id_row"]);
                    #region Map info account từ JeeAccount
                    foreach (DataRow item in user.Rows)
                    {
                        var info = DataAccount.Where(x => item["id_user"].ToString().Contains(x.UserId.ToString())).FirstOrDefault();
                        if (info != null)
                        {
                            item["hoten"] = info.FullName;
                        }
                    }
                    dr["User"] = user;
                    #endregion
                }
            }
            return result;
        }
        public static async Task<DataSet> GetWork_ClickUp(DpsConnection cnn, QueryParams query, long curUser, List<AccUsernameModel> DataAccount, string listDept, string dieukien_where = "")
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
            DateTime from = DateTime.Now;
            DateTime to = DateTime.Now;
            if (!string.IsNullOrEmpty(query.filter["TuNgay"]))
            {
                DateTime.TryParseExact(query.filter["TuNgay"], "dd/MM/yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out from);
                dieukien_where += " and w.CreatedDate>=@from";
                Conds.Add("from", from);
            }
            if (!string.IsNullOrEmpty(query.filter["DenNgay"]))
            {
                DateTime.TryParseExact(query.filter["DenNgay"], "dd/MM/yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out to);
                to = to.AddDays(1);
                dieukien_where += " and w.CreatedDate<@to";
                Conds.Add("to", to);
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
iif(convert(varchar, w.deadline,103) like convert(varchar, GETDATE(),103),1,0) as duetoday,
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
        public static DataSet getWork(DpsConnection cnn, QueryParams query, long curUser, List<AccUsernameModel> DataAccount, string dieukien_where = "")
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
            DateTime from = DateTime.Now;
            DateTime to = DateTime.Now;

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
                Conds.Add("from", from);
            }
            if (!string.IsNullOrEmpty(query.filter["DenNgay"]))
            {
                DateTime.TryParseExact(query.filter["DenNgay"], "dd/MM/yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out to);
                to = to.AddDays(1);
                dieukien_where += " and w." + collect_by + "<@to";
                Conds.Add("to", to);
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
            //, nv.holot+' '+nv.ten as hoten_nguoigiao -- w.NguoiGiao,
            string sqlq = @$"select  distinct w.id_row,w.title,w.description,w.id_project_team,w.id_group,w.deadline,w.id_milestone,w.milestone,
                            w.id_parent,w.start_date,w.end_date,w.urgent,w.important,w.prioritize,w.status,w.result,w.CreatedDate,w.CreatedBy,
                            w.UpdatedDate,w.UpdatedBy, w.project_team,w.id_department,w.clickup_prioritize , w.nguoigiao,'' as hoten_nguoigiao,w.Id_NV,''as hoten
                            , Iif(fa.id_row is null ,0,1) as favourite 
                            ,coalesce( f.count,0) as num_file,coalesce( com.count,0) as num_com,
                            iIf(w.Status=2 and w.end_date>w.deadline,1,0) as is_htquahan,
                            iIf(w.Status = 2 and w.end_date <= w.deadline, 1, 0) as is_htdunghan ,
                            iIf(w.Status = 1 and  w.start_date is not null, 1, 0) as is_danglam,
                            iIf(w.Status = 1 and getdate() > w.deadline, 1, 0) as is_quahan,
                            iif(convert(varchar, w.deadline,103) like convert(varchar, GETDATE(),103),1,0) as duetoday,
                            iif(w.status=1 and w.start_date is null,1,0) as require,
                            '' as NguoiTao, '' as NguoiSua from v_wework_new w 
                            left join (select count(*) as count,object_id 
                            from we_attachment where object_type=1 group by object_id) f on f.object_id=w.id_row
                            left join (select count(*) as count,object_id 
                            from we_comment where object_type=1 group by object_id) com on com.object_id=w.id_row
                            left join we_work_favourite fa 
                            on fa.id_work=w.id_row and fa.createdby=@iduser and fa.disabled=0
                            where 1=1 " + dieukien_where + "  order by " + dieukienSort;
            sqlq += ";select id_work, id_tag,color, title " +
                "from we_work_tag wt join we_tag t on wt.id_tag=t.id_row where wt.Disabled=0";
            if (!string.IsNullOrEmpty(query.filter["id_project_team"]))
                sqlq += " and id_project_team=" + query.filter["id_project_team"];
            //người theo dõi
            sqlq += @$";select id_work,id_user,'' as hoten from we_work_user u 
                        where u.disabled = 0 and u.id_user in ({ListID}) and u.loai = 2";

            DataSet ds = cnn.CreateDataSet(sqlq, Conds);
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
        private DataSet getWork_IDNV(DpsConnection cnn, QueryParams query, long curUser, List<AccUsernameModel> DataAccount, string dieukien_where = "")
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
            DateTime from = DateTime.Now;
            DateTime to = DateTime.Now;
            if (!string.IsNullOrEmpty(query.filter["TuNgay"]))
            {
                DateTime.TryParseExact(query.filter["TuNgay"], "dd/MM/yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out from);
                dieukien_where += " and w.CreatedDate>=@from";
                Conds.Add("from", from);
            }
            if (!string.IsNullOrEmpty(query.filter["DenNgay"]))
            {
                DateTime.TryParseExact(query.filter["DenNgay"], "dd/MM/yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out to);
                to = to.AddDays(1);
                dieukien_where += " and w.CreatedDate<@to";
                Conds.Add("to", to);
            }
            if (!string.IsNullOrEmpty(query.filter["keyword"]))
            {
                dieukien_where += " and (w.title like N'%@keyword%' or w.description like N'%@keyword%' or tao.Username like N'%@keyword%' or sua.Username like N'%@keyword%')";
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
            string sqlq = @$"select  distinct w.id_row,w.title,w.description,w.id_project_team,w.id_group,w.deadline,w.id_milestone,w.milestone,w.Id_NV,
w.id_parent,w.start_date,w.end_date,w.urgent,w.important,w.prioritize,w.status,w.result,w.CreatedDate,w.CreatedBy,
w.UpdatedDate,w.UpdatedBy, w.project_team,w.id_department,w.clickup_prioritize 
, Iif(fa.id_row is null ,0,1) as favourite 
,coalesce( f.count,0) as num_file,coalesce( com.count,0) as num_com,
iIf(w.Status=2 and w.end_date>w.deadline,1,0) as is_htquahan,
iIf(w.Status = 2 and w.end_date <= w.deadline, 1, 0) as is_htdunghan ,
iIf(w.Status = 1 and  w.start_date is not null, 1, 0) as is_danglam,
iIf(w.Status = 1 and getdate() > w.deadline, 1, 0) as is_quahan,
iif(convert(varchar, w.deadline,103) like convert(varchar, GETDATE(),103),1,0) as duetoday,
iif(w.status=1 and w.start_date is null,1,0) as require,
'' as NguoiTao, '' as NguoiSua from v_wework_new w 
left join (select count(*) as count,object_id 
from we_attachment where object_type=1 group by object_id) f on f.object_id=w.id_row
left join (select count(*) as count,object_id 
from we_comment where object_type=1 group by object_id) com on com.object_id=w.id_row
left join we_work_favourite fa 
on fa.id_work=w.id_row and fa.createdby=@iduser and fa.disabled=0
where 1=1 and  w.CreatedBy in ({ListID}) " + dieukien_where + "  order by " + dieukienSort;
            sqlq += ";select id_work, id_tag,color, title " +
                "from we_work_tag wt join we_tag t on wt.id_tag=t.id_row where wt.Disabled=0";
            if (!string.IsNullOrEmpty(query.filter["id_project_team"]))
                sqlq += " and id_project_team=" + query.filter["id_project_team"];
            //người theo dõi
            sqlq += @$";select id_work,'' as hoten,id_user from we_work_user u 
where u.disabled = 0 and u.id_user in ({ListID}) and u.loai = 2";

            DataSet ds = cnn.CreateDataSet(sqlq, Conds);
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
        public static object getChild(string domain, long IdKHDPS, string columnName, string displayChild, object id, EnumerableRowCollection<DataRow> temp, EnumerableRowCollection<DataRow> tags, List<AccUsernameModel> DataAccount, string ConnectString, object parent = null)
        {
            object a = "";
            if (parent == null)
                parent = DBNull.Value;
            else
            {
                a = parent;
            }
            // get user Id 
            DataTable User = new DataTable();
            DataTable User2 = new DataTable();
            DataTable dt_status = new DataTable();
            using (DpsConnection cnn = new DpsConnection(ConnectString))
            {
                SqlConditions conds1 = new SqlConditions();
                conds1 = new SqlConditions();
                conds1.Add("w_user.Disabled", 0);
                string select_user = $@"select  distinct w_user.id_user,'' as hoten,'' as image, id_work,w_user.loai
                                        from we_work_user w_user 
                                        join we_work on we_work.id_row = w_user.id_work where (where)";
                if ("id_project_team".Equals(columnName))
                {
                    select_user += " and id_work in (select id_row from we_work where id_project_team = " + id + ")";
                }
                dt_status = WeworkLiteController.StatusDynamic(long.Parse(id.ToString()), DataAccount, ConnectString);
                User = cnn.CreateDataTable(select_user, "(where)", conds1);
                int _a = User.Rows.Count;
                #region Map info account từ JeeAccount
                if (User.Rows.Count > 0)
                {
                    foreach (DataRow item in User.Rows)
                    {
                        var info = DataAccount.Where(x => item["id_user"].ToString().Contains(x.UserId.ToString())).FirstOrDefault();
                        if (info != null)
                        {
                            item["hoten"] = info.FullName;
                            item["image"] = info.AvartarImgURL;
                        }
                    }
                }
                #endregion
                if (columnName == "Id_NV")
                {
                    SqlConditions conds2 = new SqlConditions();
                    conds2.Add("w_user.Disabled", 0);
                    conds2.Add("w_user.id_user", id);
                    User2 = cnn.CreateDataTable(select_user, "(where)", conds2);
                    #region Map info account từ JeeAccount
                    foreach (DataRow item in User2.Rows)
                    {
                        var info = DataAccount.Where(x => item["id_user"].ToString().Contains(x.UserId.ToString())).FirstOrDefault();
                        if (info != null)
                        {
                            item["hoten"] = info.FullName;
                        }
                    }
                    #endregion
                }
                int b = User2.Rows.Count;
            }
            //columnName="" : không group
            // update lại data khi sửa từ wiget wiget thì bỏ đi phần này : ----- && (columnName == "" || (columnName != "" && r[columnName].Equals(id)))
            // k có phần này thì workclickup lấy dữ liệu không map theo id dự án
            var re = from r in temp
                     where r["id_parent"].Equals(parent) && (columnName == "" || (columnName != "" && r[columnName].ToString().Equals(id.ToString()))) //(parent == null &&  r[columnName].Equals(id)) || (r["id_parent"].Equals(parent) && parent != null)
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
                         require = r["require"],
                         status = r["status"],
                         id_milestone = r["id_milestone"],
                         milestone = r["milestone"],
                         is_htquahan = r["is_htquahan"],
                         is_htdunghan = r["is_htdunghan"],
                         is_danglam = r["is_danglam"],
                         is_quahan = r["is_quahan"],
                         duetoday = r["duetoday"],
                         num_file = r["num_file"],
                         num_com = r["num_com"],
                         createddate = r["CreatedDate"],
                         createdby = r["CreatedBy"],
                         nguoitao = r["NguoiTao"],
                         updateddate = r["UpdatedDate"] == DBNull.Value ? "" : r["UpdatedDate"],
                         updatedby = r["UpdatedBy"],
                         nguoisua = r["NguoiSua"],
                         //nguoigiao = r["NguoiGiao"],
                         clickup_prioritize = r["clickup_prioritize"],
                         //id_nv = r["id_nv"],
                         //assign = r["id_nv"] == DBNull.Value ? null : new
                         //{
                         //    id_nv = r["id_nv"],
                         //    hoten = r["hoten"],
                         //    username = r["username"],
                         //    tenchucdanh = r["tenchucdanh"],
                         //    mobile = r["mobile"],
                         //    image = WeworkLiteController.genLinkImage(domain, IdKHDPS, r["id_nv"].ToString(), _hostingEnvironment.ContentRootPath)
                         //},
                         User = from us in User.AsEnumerable()
                                where r["id_row"].Equals(us["id_work"]) && long.Parse(us["loai"].ToString()).Equals(1)
                                select new
                                {
                                    id_nv = us["id_user"],
                                    hoten = us["hoten"],
                                    //username = us["username"],
                                    //tenchucdanh = us["tenchucdanh"],
                                    //mobile = us["mobile"],
                                    image = us["image"],
                                    loai = us["loai"],
                                    //image = WeworkLiteController.genLinkImage(domain, IdKHDPS, us["id_user"].ToString(), _hostingEnvironment.ContentRootPath),
                                },
                         Follower = from us in User.AsEnumerable()
                                    where r["id_row"].Equals(us["id_work"]) && long.Parse(us["loai"].ToString()).Equals(2)
                                    select new
                                    {
                                        id_nv = us["id_user"],
                                        hoten = us["hoten"],
                                        image = us["image"],
                                        loai = us["loai"],
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
                                      typeid = s["Type"],
                                      color = s["color"],
                                      position = s["Position"],
                                      IsFinal = s["IsFinal"],
                                      IsDeadline = s["IsDeadline"],
                                      IsDefault = s["IsDefault"],
                                      IsToDo = s["IsToDo"]
                                  },
                         Childs = displayChild == "0" ? new List<string>() : getChild(domain, IdKHDPS, columnName, displayChild == "1" ? "0" : "2", id, temp, tags, DataAccount, ConnectString, r["id_row"])
                     };
            return re.Distinct().ToList();
        }
        public static EnumerableRowCollection<DataRow> filterWork(EnumerableRowCollection<DataRow> enumerableRowCollections, FilterModel filter)
        {
            var temp = enumerableRowCollections;
            #region filter
            if (!string.IsNullOrEmpty(filter["status"]))//1: đang thực hiên(đang làm & phải làm)||2: đã xong||3: đang đánh giá
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
    }
}
