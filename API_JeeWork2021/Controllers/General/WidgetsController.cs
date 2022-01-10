using DpsLibs.Data;
using JeeWork_Core2021.Classes;
using JeeWork_Core2021.Models;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Mail;
using Microsoft.AspNetCore.Http;
using Microsoft.Net.Http.Headers;
using RestSharp;
using Newtonsoft.Json;
using DPSinfra.ConnectionCache;
using Microsoft.Extensions.Configuration;
using DPSinfra.Notifier;
using Microsoft.Extensions.Logging;

namespace JeeWork_Core2021.Controllers.Wework
{
    [ApiController]
    [Route("api/widgets")]
    [EnableCors("JeeWorkPolicy")]
    /// <summary>
    /// các ds widget dùng cho Landing Page
    /// </summary>
    public class WidgetsController : ControllerBase
    {
        private JeeWorkConfig _config;
        public List<AccUsernameModel> DataAccount;
        private IConnectionCache ConnectionCache;
        private IConfiguration _configuration;
        private readonly ILogger<WidgetsController> _logger;

        static string sql_isquahan = " w.deadline < GETUTCDATE() and w.deadline is not null and w.end_date is null ";
        static string sql_dangthuchien = "((w.deadline >= GETUTCDATE() and deadline is not null) or deadline is null ) and w.end_date is null";
        static string sqlhoanthanhdunghan = " w.end_date is not null and (w.deadline >= w.end_date or w.deadline is null) ";
        static string sqlhoanthanhquahan = " w.end_date is not null and w.deadline < w.end_date";
        //static string sqlhoanthanh = " w.end_date is not null ";
        // kiểm tra điều kiện hoành thành
        string queryhoanthanh = " and w.end_date is not null ";
        //string querydangthuchien = " and w.end_date is null ";

        public WidgetsController(IOptions<JeeWorkConfig> config, IConnectionCache _cache, IConfiguration configuration, ILogger<WidgetsController> logger)
        {
            _config = config.Value;
            ConnectionCache = _cache;
            _configuration = configuration;
            _logger = logger;
        }
        [Route("my-projects")]
        [HttpGet]
        public object MyProjects([FromQuery] QueryParams query)
        {
            UserJWT loginData = Ulities.GetUserByHeader(HttpContext.Request.Headers);
            if (loginData == null)
                return JsonResultCommon.DangNhap();
            if (query == null)
                query = new QueryParams();

            bool Visible = false;
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
                    SqlConditions Conds = new SqlConditions();
                    string dieukienSort = "tong desc", dieukien_where = " ";
                    if (!string.IsNullOrEmpty(query.filter["keyword"]))
                    {
                        dieukien_where += " and (p.title like '%@keyword%' or p.description like '%@keyword%')";
                        dieukien_where = dieukien_where.Replace("@keyword", query.filter["keyword"]);
                    }
                    if (!string.IsNullOrEmpty(query.filter["status"]))
                    {
                        dieukien_where += " and p.status=@status";
                        Conds.Add("status", query.filter["status"]);
                    }
                    if (!string.IsNullOrEmpty(query.filter["locked"]))
                    {
                        dieukien_where += " and p.locked=@locked";
                        Conds.Add("locked", query.filter["locked"]);
                    }
                    #region Sort data theo các dữ liệu bên dưới
                    Dictionary<string, string> sortableFields = new Dictionary<string, string>
                        {
                            { "priority", "priority"},
                            { "title", "title"},
                            { "description", "description"},
                            { "CreatedBy", "NguoiTao"},
                            { "CreatedDate", "CreatedDate"},
                            { "UpdatedBy", "NguoiSua"},
                            {"UpdatedDate","UpdatedDate" }
                        };
                    #endregion
                    if (!string.IsNullOrEmpty(query.sortField) && sortableFields.ContainsKey(query.sortField))
                        dieukienSort = sortableFields[query.sortField] + ("desc".Equals(query.sortOrder) ? " desc" : " asc");
                    #region get list trạng thái status 
                    List<string> lstHoanthanh = cnn.CreateDataTable("select id_row from we_status where IsFinal = 1").AsEnumerable().Select(x => x["id_row"].ToString()).ToList();
                    List<string> lstQuahan = cnn.CreateDataTable("select id_row from we_status where isDeadline = 1").AsEnumerable().Select(x => x["id_row"].ToString()).ToList();
                    string strhoanthanh = string.Join(",", lstHoanthanh);
                    string strquahan = string.Join(",", lstQuahan);
                    if (string.IsNullOrEmpty(strhoanthanh))
                    {
                        strhoanthanh = "0";
                    }
                    if (string.IsNullOrEmpty(strquahan))
                    {
                        strquahan = "0";
                    }
                    #endregion
                    #region Trả dữ liệu về backend để hiển thị lên giao diện
                    string sqlq = @$"select p.*, de.title as department,coalesce(w.tong,0) as tong
                                    ,coalesce( w.ht,0) as ht, coalesce(w.quahan,0) as quahan
                                    , '' as NguoiTao, '' as NguoiSua from we_project_team p 
                                    left join we_department de on de.id_row=p.id_department
                                    left join (select count(*) as tong, 
                                    COUNT(CASE WHEN w.status in (" + strhoanthanh + @") THEN 1 END) as ht
                                    , COUNT(CASE WHEN w.status in (" + strquahan + @")THEN 1 END) as quahan
                                    ,w.id_project_team from we_work w where w.Disabled=0 group by w.id_project_team) w 
                                    on p.id_row=w.id_project_team 
                                    join we_project_team_user project_user 
                                    on project_user.id_project_team = p.id_row and admin = 1 
									and project_user.id_user = " + loginData.UserID + " " +
                                    "where p.Disabled=0 and de.Disabled = 0 " +
                                    "" + dieukien_where + " order by " + dieukienSort;
                    sqlq += @$";select u.*,admin,'' as hoten,'' as username, '' as tenchucdanh
                                            ,'' as mobile,'' as image
                                            from we_project_team_user u 
                                            join we_project_team p on p.id_row=u.id_project_team 
                                            where u.disabled=0 and u.Id_user in (" + listID + " )";
                    DataSet ds = cnn.CreateDataSet(sqlq, Conds);
                    if (cnn.LastError != null || ds == null)
                        return JsonResultCommon.Exception(_logger, cnn.LastError, _config, loginData, ControllerContext);
                    DataTable dt = ds.Tables[0];
                    if (dt.Rows.Count == 0)
                        return JsonResultCommon.ThanhCong(new List<string>(), pageModel, Visible);
                    #region Map info account từ JeeAccount
                    foreach (DataRow item in ds.Tables[1].Rows)
                    {
                        var info = DataAccount.Where(x => item["id_user"].ToString().Contains(x.UserId.ToString())).FirstOrDefault();
                        if (info != null)
                        {
                            item["hoten"] = info.FullName;
                            item["username"] = info.Username;
                            item["tenchucdanh"] = info.Jobtitle;
                            item["mobile"] = info.PhoneNumber;
                            item["image"] = info.AvartarImgURL;
                        }
                    }
                    #endregion
                    var temp = dt.AsEnumerable();
                    dt = temp.CopyToDataTable();
                    int total = dt.Rows.Count;
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
                    dt = dt.AsEnumerable().Skip((query.page - 1) * query.record).Take(query.record).CopyToDataTable();
                    var data = from r in dt.AsEnumerable()
                               select new
                               { //
                                   id_row = r["id_row"],
                                   title = r["title"],
                                   description = r["description"],
                                   id_department = r["id_department"],
                                   department = r["department"],
                                   CreatedDate = string.Format("{0:dd/MM/yyyy HH:mm}", r["CreatedDate"]),
                                   CreatedBy = r["CreatedBy"],
                                   NguoiTao = r["NguoiTao"],
                                   UpdatedDate = r["UpdatedDate"] == DBNull.Value ? "" : string.Format("{0:dd/MM/yyyy HH:mm}", r["UpdatedDate"]),
                                   UpdatedBy = r["UpdatedBy"],
                                   NguoiSua = r["NguoiSua"],
                                   status = r["status"],
                                   locked = r["locked"],
                                   start_date = string.Format("{0:dd/MM/yyyy}", r["start_date"]),
                                   end_date = string.Format("{0:dd/MM/yyyy}", r["end_date"]),
                                   users = from u in ds.Tables[1].AsEnumerable()
                                           where u["id_project_team"].ToString() == r["id_row"].ToString()
                                           select new
                                           {
                                               id_row = u["id_row"],
                                               id_project_team = u["id_project_team"],
                                               id_user = u["id_user"],
                                               admin = u["admin"],
                                               hoten = u["hoten"],
                                               username = u["username"],
                                               tenchucdanh = u["tenchucdanh"],
                                               mobile = u["mobile"],
                                               image = u["image"],
                                               favourite = u["favourite"],
                                           },
                                   Count = new
                                   {
                                       tong = r["tong"],
                                       ht = r["ht"],
                                       quahan = r["quahan"],
                                       percentage = JeeWorkLiteController.calPercentage(r["tong"], r["ht"])
                                   }
                               };
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
        /// Báo cáo chi tiết theo thành viên (Không cần tuyền params)
        /// </summary>
        /// <param name="query"></param>
        /// <returns></returns>
        [Route("members")]
        [HttpGet]
        public object Members([FromQuery] QueryParams query)
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
                    string listDept = JeeWorkLiteController.getListDepartment_GetData(loginData, cnn, HttpContext.Request.Headers, _configuration, ConnectionString);
                    string domain = _configuration.GetValue<string>("Host:JeeWork_API") + "/";
                    Dictionary<string, string> collect = new Dictionary<string, string>
                        {
                            { "CreatedDate", "CreatedDate"},
                            { "Deadline", "Deadline"},
                            { "StartDate", "StartDate"}
                        };
                    string collect_by = "CreatedDate";
                    if (!string.IsNullOrEmpty(query.filter["collect_by"]))
                        collect_by = collect[query.filter["collect_by"]];
                    SqlConditions cond = new SqlConditions();
                    string strW = "";
                    DateTime from = Common.GetDateTime();
                    DateTime to = Common.GetDateTime();
                    if (string.IsNullOrEmpty(query.filter["TuNgay"]) || string.IsNullOrEmpty(query.filter["DenNgay"]))
                        return JsonResultCommon.Custom("Khoảng thời gian không hợp lệ");
                    bool from1 = DateTime.TryParseExact(query.filter["TuNgay"], "dd/MM/yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out from);
                    if (!from1)
                        return JsonResultCommon.Custom("Thời gian bắt đầu không hợp lệ");
                    strW += " and w." + collect_by + ">=@from";
                    cond.Add("from", JeeWorkLiteController.GetUTCTime(Request.Headers, from.ToString()));
                    bool to1 = DateTime.TryParseExact(query.filter["DenNgay"], "dd/MM/yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out to);
                    if (!to1)
                        return JsonResultCommon.Custom("Thời gian kết thúc không hợp lệ");
                    strW += " and w." + collect_by + "<@to";
                    cond.Add("to", JeeWorkLiteController.GetUTCTime(Request.Headers, to.ToString()));

                    if (!string.IsNullOrEmpty(query.filter["id_department"]))
                    {
                        listDept = query.filter["id_department"];
                        //strW += " and id_department=@id_department";
                        //cond.Add("id_department", query.filter["id_department"]);
                    }
                    //if (!string.IsNullOrEmpty(query.filter["status"]))//1: đang thực hiện (đang làm & phải làm)||2: đã xong
                    //{
                    //    strW += " and status=@status";
                    //    cond.Add("status", query.filter["status"]);
                    //}
                    string displayChild = "0";//hiển thị con: 0-không hiển thị, 1- 1 cấp con, 2- nhiều cấp con
                    if (!string.IsNullOrEmpty(query.filter["displayChild"]))
                        displayChild = query.filter["displayChild"];

                    #region danh sách department, list status hoàn thành, trễ,đang làm
                    if (listDept != "")
                    {

                        strW += " and id_department in (" + listDept + ") ";
                    }
                    string list_Complete = "";
                    list_Complete = ReportController.GetListStatusDynamic(listDept, cnn, " IsFinal ");
                    string list_Deadline = "";
                    list_Deadline = ReportController.GetListStatusDynamic(listDept, cnn, " IsDeadline ");
                    string list_Todo = "";
                    list_Todo = ReportController.GetListStatusDynamic(listDept, cnn, " IsTodo ");
                    #endregion

                    #region Lấy dữ liệu account từ JeeAccount
                    DataAccount = JeeWorkLiteController.GetAccountFromJeeAccount(HttpContext.Request.Headers, _configuration);
                    if (DataAccount == null)
                        return JsonResultCommon.Custom("Lỗi lấy danh sách nhân viên từ hệ thống quản lý tài khoản");

                    string error = "";
                    string listID = JeeWorkLiteController.ListAccount(HttpContext.Request.Headers, out error, _configuration);
                    if (error != "")
                        return JsonResultCommon.Custom(error);
                    #endregion
                    DataTable dt = new DataTable();
                    dt.Columns.Add("id_nv");
                    dt.Columns.Add("hoten");
                    dt.Columns.Add("mobile");
                    dt.Columns.Add("Username");
                    dt.Columns.Add("Email");
                    dt.Columns.Add("Tenchucdanh");
                    dt.Columns.Add("tong");
                    dt.Columns.Add("ht");
                    dt.Columns.Add("ht_quahan");
                    dt.Columns.Add("quahan");
                    dt.Columns.Add("danglam");
                    dt.Columns.Add("dangdanhgia");
                    dt.Columns.Add("image");
                    foreach (var item in DataAccount)
                    {
                        dt.Rows.Add(item.UserId, item.FullName, item.PhoneNumber, item.Username, item.Email, item.Jobtitle, 0, 0, 0, 0, 0, 0, item.AvartarImgURL);
                    }
                    List<string> nvs = dt.AsEnumerable().Select(x => x["id_nv"].ToString()).ToList();
                    if (nvs.Count == 0)
                        return JsonResultCommon.ThanhCong(nvs);
                    string ids = string.Join(",", nvs);
                    string sqlq = @"select count(distinct p.id_row) as dem, id_user from we_project_team p 
                                    join we_project_team_user u 
                                    on p.id_row=u.id_project_team 
                                    where p.disabled=0 and u.disabled=0 
                                    and u.id_user in (" + ids + ") " +
                                    "group by u.id_user";
                    sqlq += @$";select id_row, id_nv, status,
                                     iIf( {sqlhoanthanhquahan} ,1,0) as is_htquahan,
                                    iIf({sqlhoanthanhdunghan} ,1,0) as is_ht,
                                    iIf( {sql_dangthuchien} , 1, 0) as dangthuchien, 
                                    iIf( {sql_isquahan} , 1, 0) as is_quahan
                                    from v_wework_new w 
                                    where id_nv in (" + ids + ") " + strW + " (parent)";


                    if (displayChild == "0")
                        sqlq = sqlq.Replace("(parent)", " and id_parent is null");
                    else
                        sqlq = sqlq.Replace("(parent)", " ");

                    DataSet ds = cnn.CreateDataSet(sqlq, cond);
                    var asP = ds.Tables[0].AsEnumerable();
                    DataTable dtW = ds.Tables[1];
                    bool hasValue = dtW.Rows.Count > 0;
                    int total = 0, success = 0;

                    foreach (DataRow dr in dt.Rows)
                    {
                        DataRow[] row = dtW.Select("id_nv=" + dr["id_nv"].ToString());
                        if (row.Length > 0)
                        {
                            dr["tong"] = total = (hasValue ? (int)dtW.Compute("count(id_nv)", "id_nv=" + dr["id_nv"].ToString()) : 0);
                            dr["ht"] = (hasValue ? (int)dtW.Compute("count(id_nv)", " is_ht=1 and id_nv=" + dr["id_nv"].ToString()) : 0);
                            dr["ht_quahan"] = hasValue ? dtW.Compute("count(id_nv)", " is_htquahan=1 and id_nv=" + dr["id_nv"].ToString()) : 0;
                            dr["quahan"] = hasValue ? dtW.Compute("count(id_nv)", " is_quahan=1 and id_nv=" + dr["id_nv"].ToString()) : 0;
                            dr["danglam"] = hasValue ? dtW.Compute("count(id_nv)", " dangthuchien=1 and id_nv=" + dr["id_nv"].ToString()) : 0;
                            dr["dangdanhgia"] = 0;
                            //dr["dangdanhgia"] = hasValue ? dtW.Compute("count(id_nv)", " status=3 and id_nv=" + dr["id_nv"].ToString()) : 0;
                        }
                        else
                        {
                            dr["tong"] = dr["ht"] = dr["ht_quahan"] = dr["quahan"] = dr["danglam"] = dr["dangdanhgia"] = 0;
                        }
                    }
                    for (int i = dt.Rows.Count - 1; i >= 0; i--)
                    {
                        DataRow dr = dt.Rows[i];
                        total = int.Parse(dr["tong"].ToString());
                        if ((total) <= 0)
                        {
                            dr.Delete();
                        }
                    }
                    dt.AcceptChanges();
                    var data = from r in dt.AsEnumerable()
                               select new
                               {
                                   id_nv = r["id_nv"],
                                   hoten = r["hoten"],
                                   tenchucdanh = r["tenchucdanh"],
                                   image = r["image"],
                                   num_project = asP.Where(x => x["id_user"].Equals(r["id_nv"])).Select(x => x["dem"]).DefaultIfEmpty(0).First(),
                                   num_work = total = (hasValue ? (int)dtW.Compute("count(id_nv)", "id_nv=" + r["id_nv"].ToString()) : 0),
                                   danglam = hasValue ? dtW.Compute("count(id_nv)", " dangthuchien=1  and id_nv=" + r["id_nv"].ToString()) : 0,
                                   hoanthanh = success = (hasValue ? (int)dtW.Compute("count(id_nv)", " is_ht=1 and id_nv=" + r["id_nv"].ToString()) : 0),
                                   dangdanhgia = 0,
                                   ht_quahan = hasValue ? dtW.Compute("count(id_nv)", " is_htquahan=1 and id_nv=" + r["id_nv"].ToString()) : 0,
                                   quahan = hasValue ? dtW.Compute("count(id_nv)", " is_quahan=1 and id_nv=" + r["id_nv"].ToString()) : 0,
                                   percentage = total == 0 ? 0 : (success * 100 / total)
                               };
                    if ("desc".Equals(query.sortOrder))
                        data = data.OrderByDescending(x => x.num_work);
                    else
                        data = data.OrderBy(x => x.num_work);
                    return JsonResultCommon.ThanhCong(data);
                }
            }
            catch (Exception ex)
            {
                return JsonResultCommon.Exception(_logger, ex, _config, loginData);
            }
        }
        /// <summary>
        /// Biểu đô tròn trạng thái công việc
        /// <para/>và<para/>
        /// Công việc không đúng hạn
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [Route("trang-thai-cv")]
        [HttpGet]
        public object TrangThaiCongViec([FromQuery] QueryParams query)
        {
            UserJWT loginData = Ulities.GetUserByHeader(HttpContext.Request.Headers);
            if (loginData == null)
                return JsonResultCommon.DangNhap();
            try
            {
                if (query == null)
                    query = new QueryParams();

                Dictionary<string, string> collect = new Dictionary<string, string>
                        {
                            { "CreatedDate", "CreatedDate"},
                            { "Deadline", "Deadline"},
                            { "StartDate", "StartDate"}
                        };
                string collect_by = "CreatedDate";
                if (!string.IsNullOrEmpty(query.filter["collect_by"]))
                    collect_by = collect[query.filter["collect_by"]];
                SqlConditions cond = new SqlConditions();
                string strW = "";
                DateTime from = Common.GetDateTime();
                DateTime to = Common.GetDateTime();
                string ConnectionString = JeeWorkLiteController.getConnectionString(ConnectionCache, loginData.CustomerID, _configuration);
                using (DpsConnection cnn = new DpsConnection(ConnectionString))
                {
                    if (!string.IsNullOrEmpty(query.filter["TuNgay"]))
                    {
                        bool from1 = DateTime.TryParseExact(query.filter["TuNgay"], "dd/MM/yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out from);
                        if (!from1)
                            return JsonResultCommon.Custom("Thời gian bắt đầu không hợp lệ");
                        strW += " and w." + collect_by + ">=@from";
                        cond.Add("from", JeeWorkLiteController.GetUTCTime(Request.Headers, from.ToString()));
                    }
                    if (!string.IsNullOrEmpty(query.filter["DenNgay"]))
                    {
                        bool to1 = DateTime.TryParseExact(query.filter["DenNgay"], "dd/MM/yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out to);
                        if (!to1)
                            return JsonResultCommon.Custom("Thời gian kết thúc không hợp lệ");
                        strW += " and w." + collect_by + "<@to";
                        cond.Add("to", JeeWorkLiteController.GetUTCTime(Request.Headers, to.ToString()));
                    }
                    string id_project_team = "0";
                    string hoanthanh = "";
                    string quahan = "";
                    string todo = "";
                    if (!string.IsNullOrEmpty(query.filter["id_projectteam"]))
                    {
                        id_project_team = query.filter["id_projectteam"].ToString();
                        strW += " and id_project_team=@id_projectteam";
                        cond.Add("id_projectteam", query.filter["id_projectteam"]);
                    }
                    else
                    {
                        string listDept = JeeWorkLiteController.getListDepartment_GetData(loginData, cnn, HttpContext.Request.Headers, _configuration, ConnectionString);
                        hoanthanh = ReportController.GetListStatusDynamic(listDept, cnn, " IsFinal "); // IsFinal
                        quahan = ReportController.GetListStatusDynamic(listDept, cnn, "IsDeadline"); // IsDeadline
                        todo = ReportController.GetListStatusDynamic(listDept, cnn, "IsTodo"); //IsTodo
                        strW += $" and id_department in ({listDept})";
                    }
                    if (!string.IsNullOrEmpty(query.filter["status"]))//1: đang thực hiên(đang làm & phải làm)||2: đã xong
                    {
                        strW += " and status=@status";
                        cond.Add("status", query.filter["status"]);
                    }
                    string displayChild = "0";//hiển thị con: 0-không hiển thị, 1- 1 cấp con, 2- nhiều cấp con
                    if (!string.IsNullOrEmpty(query.filter["displayChild"]))
                        displayChild = query.filter["displayChild"];
                    string sqlq = "";
                    sqlq = @$"select id_row, id_nv, status, deadline, end_date from v_wework_clickup_new w where disabled = 0 " + strW;
                    if (displayChild == "0")
                        sqlq += " and id_parent is null";
                    DataSet ds = cnn.CreateDataSet(sqlq, cond);
                    if (cnn.LastError != null || ds == null)
                    {
                        return JsonResultCommon.Exception(_logger, cnn.LastError, _config, loginData, ControllerContext);
                    }
                    DataTable dtW = ds.Tables[0];
                    List<object> data = new List<object>();
                    cond = new SqlConditions();
                    cond.Add("disabled", 0);
                    cond.Add("id_project_team", id_project_team);
                    List<int> list_count = new List<int>();
                    List<string> statusnameList = new List<string>();
                    List<string> Color = new List<string>();
                    DataTable dt_status = cnn.CreateDataTable("select id_row, statusname, color, type from we_status where (where) order by type, statusname", "(where)", cond);
                    if (dt_status.Rows.Count > 0)
                    {
                        foreach (DataRow item in dt_status.Rows)
                        {
                            statusnameList.Add(item["statusname"].ToString());
                            Color.Add(item["color"].ToString());
                            list_count.Add((int)dtW.Compute("count(id_row)", "status=" + item["id_row"].ToString()));
                            data.Add(new
                            {
                                trangthai = item["statusname"].ToString(),
                                value = (int)dtW.Compute("count(id_row)", "status=" + item["id_row"].ToString()),
                                color = item["color"].ToString()
                            });
                        }
                    }
                    #region Trả dữ liệu về backend để hiển thị lên giao diện
                    return JsonResultCommon.ThanhCong(new
                    {
                        TrangThaiCongViec = statusnameList,
                        ColorList = Color,
                        DataTrangThaiCongViec = list_count,
                        DataBarChart = data,
                    });
                    #endregion
                }
            }
            catch (Exception ex)
            {
                return JsonResultCommon.Exception(_logger, ex, _config, loginData);
            }
        }
        public static string calPercentage(object tong, object v)
        {
            try
            {
                if (tong == null || v == null)
                    return "";
                double re = 0;
                double sum = double.Parse(tong.ToString());
                if (sum == 0)
                    return "0.00";
                double val = double.Parse(v.ToString());
                re = (val * 100) / sum;
                return string.Format("{0:N2}", re);
            }
            catch (Exception ex)
            {
                return "";
            }
        }
        /// <summary>
        ///  my-list-wiget
        /// </summary>
        /// <param name="query"></param>
        /// <returns></returns>
        [Route("my-list-wiget")]
        [HttpGet]
        public object MyListWiget([FromQuery] QueryParams query)
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
                    string strW = " and (w.id_nv=@iduser or (w.nguoigiao=@iduser or w.createdby=@iduser) and (w.id_nv is null  or w.id_parent > 0 ))";
                    #region
                    if (!string.IsNullOrEmpty(query.filter["tinhtrang"]))
                    {
                        string tinhtrang = query.filter["tinhtrang"];
                        string hoanthanh = ReportController.GetListStatusDynamic(listDept, cnn, " IsFinal "); // IsFinal
                        string quahan = ReportController.GetListStatusDynamic(listDept, cnn, "IsDeadline"); // IsDeadline
                        string todo = ReportController.GetListStatusDynamic(listDept, cnn, "IsTodo"); //IsTodo
                                                                                                      //if (tinhtrang == "todo")
                                                                                                      //{
                                                                                                      //    strW += $" and w.status not in ({quahan},{hoanthanh}) ";
                                                                                                      //}
                                                                                                      //else if (tinhtrang == "deadline")
                                                                                                      //{
                                                                                                      //    strW += $" and w.status in ({quahan}) ";
                                                                                                      //}
                                                                                                      //else
                                                                                                      //{
                                                                                                      //    strW += $" and w.status in ({hoanthanh}) ";
                                                                                                      //};
                        if (tinhtrang == "todo")
                        {
                            strW += " and " + sql_dangthuchien; ;
                        }
                        else if (tinhtrang == "deadline")
                        {
                            strW += " and " + sql_isquahan;
                        }
                        else
                        {
                            strW += queryhoanthanh;
                        };
                    }
                    #endregion
                    #region group
                    string strG = @"select distinct p.id_row, p.title from we_project_team_user u
                                    join we_project_team p on p.id_row=u.id_project_team 
                                    where u.disabled=0 and p.Disabled=0 ";
                    if (!string.IsNullOrEmpty(loginData.UserID.ToString()))
                    {
                        strG += "and id_user=" + loginData.UserID.ToString();
                    }
                    #endregion
                    DataTable dtG = cnn.CreateDataTable(strG);
                    if (dtG.Rows.Count == 0)
                        return JsonResultCommon.ThanhCong(new List<string>(), null, Visible);
                    DataSet ds = GetTasks(loginData.CustomerID, Request.Headers, cnn, query, long.Parse(loginData.UserID.ToString()), DataAccount, strW);
                    if (cnn.LastError != null || ds == null)
                        return JsonResultCommon.Exception(_logger, cnn.LastError, _config, loginData, ControllerContext);
                    var temp = filterWork(ds.Tables[0].AsEnumerable().Where(x => x["id_parent"] == DBNull.Value), query.filter);//k bao gồm con
                    var tags = ds.Tables[1].AsEnumerable();
                    // Phân trang
                    int total = temp.Count();
                    if (total == 0)
                        return JsonResultCommon.KhongCoDuLieu();
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
                    var Children = getChild_Widget(domain, loginData.CustomerID, "", displayChild, 0, dtNew.CopyToDataTable().AsEnumerable(), tags, DataAccount, loginData, ConnectionString);
                    return JsonResultCommon.ThanhCong(Children, pageModel, Visible);
                }
            }
            catch (Exception ex)
            {
                return JsonResultCommon.Exception(_logger, ex, _config, loginData);
            }
        }
        /// <summary>
        ///  my-list-wiget
        /// </summary>
        /// <param name="query"></param>
        /// <returns></returns>
        [Route("page-my-work")]
        [HttpGet]
        public object Congviecduocgiao([FromQuery] QueryParams query)
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
                    string displayChild = "1";//hiển thị con: 0-không hiển thị, 1- 1 cấp con, 2- nhiều cấp con
                    if (!string.IsNullOrEmpty(query.filter["displayChild"]))
                        displayChild = query.filter["displayChild"];
                    string columnName = "id_project_team";
                    string strW = $" and w.id_department in ({listDept}) ";
                    DataSet ds = new DataSet();
                    int loaicongviec = 0, meetingid = 0;
                    if (!string.IsNullOrEmpty(query.filter["loaicongviec"]))
                    {
                        loaicongviec = int.Parse(query.filter["loaicongviec"].ToString());
                    }
                    switch (loaicongviec)
                    {
                        case 1: // Công việc tôi được giao mà đã hết hạn
                            {
                                strW += " and (w.id_nv=@iduser or w.id_row in (select distinct id_parent from v_wework_new ww where ww.id_nv=@iduser and id_parent > 0))";
                                break;
                            }
                        case 2: // công việc tôi tạo
                            {
                                strW += " and w.createdby=@iduser ";
                                break;
                            }
                        case 3: // công việc tôi giao đi
                            {
                                strW += " and NguoiGiao = @iduser";
                                break;
                            }
                        case 4: // công việc phụ trách (đếm chung nhắc nhở) = công việc tôi làm
                            {
                                string congviecphutrach = " and ((w.deadline >= GETUTCDATE() and w.deadline is not null) or w.deadline is null) and w.end_date is null ";
                                string congviecconphutrach = " and ((ww.deadline >= GETUTCDATE() and ww.deadline is not null) or ww.deadline is null) and ww.end_date is null ";
                                strW += $" and ( (w.id_nv=@iduser {congviecphutrach} ) or w.id_row in (select distinct id_parent from v_wework_new ww where ww.id_nv=@iduser and id_parent > 0 {congviecconphutrach} )) ";
                                break;
                            }
                        case 5: // công việc quá hạn
                            {
                                string congviectrehan = " and w.deadline < GETUTCDATE() and w.deadline is not null and w.end_date is null ";
                                string congvieccontrehan = " and ww.deadline < GETUTCDATE() and ww.deadline is not null and ww.end_date is null ";
                                strW += $" and ((w.id_nv=@iduser  {congviectrehan} ) or w.id_row in (select distinct id_parent from v_wework_new ww where ww.id_nv=@iduser and id_parent > 0 {congvieccontrehan} )) ";
                                break;
                            }
                        case 6:  // công việc quá hạn trong ngày
                            {
                                DateTime today = Common.GetDateTime();
                                DateTime currentTime = today.Date.Add(new TimeSpan(0, 0, 0));
                                string congviechhtrongngay = " and w.deadline >= '" + currentTime + "' and w.deadline < '" + currentTime.AddDays(1) + "'";
                                string congviecconhhtrongngay = " and ww.deadline >= '" + currentTime + "' and ww.deadline < '" + currentTime.AddDays(1) + "'";
                                strW += $" and ((w.id_nv=@iduser {congviechhtrongngay} ) or w.id_row in (select distinct id_parent from v_wework_new ww where ww.id_nv=@iduser and id_parent > 0 {congviecconhhtrongngay})) ";
                                break;
                            }
                        case 10:
                            {
                                return CongviecNhanvien(query);
                            }
                        case 11:
                            {
                                break;
                            }
                    }
                    if (!string.IsNullOrEmpty(query.filter["timedeadline"]))
                        strW += $"and (w.deadline is not null and deadline >= GETUTCDATE() and deadline <= '{query.filter["timedeadline"]}')";
                    if (!string.IsNullOrEmpty(query.filter["tinhtrang"]))
                    {
                        string tinhtrang = query.filter["tinhtrang"];
                        string hoanthanh = ReportController.GetListStatusDynamic(listDept, cnn, " IsFinal "); // IsFinal
                        string quahan = ReportController.GetListStatusDynamic(listDept, cnn, "IsDeadline"); // IsDeadline
                        string todo = ReportController.GetListStatusDynamic(listDept, cnn, "IsTodo"); //IsTodo
                        if ("todo".Equals(tinhtrang))
                        {
                            strW += " and" + sql_dangthuchien;
                        }
                        else
                        if ("deadline".Equals(tinhtrang))
                        {
                            strW += " and" + sql_isquahan;
                        }
                        else
                        {
                            strW += queryhoanthanh;
                        }
                    }
                    #region group
                    string strG = @"select distinct p.id_row, p.title from we_project_team_user u
                                    join we_project_team p on p.id_row=u.id_project_team 
                                    where u.disabled=0 and p.Disabled=0 ";
                    if (!string.IsNullOrEmpty(loginData.UserID.ToString()))
                    {
                        strG += "and id_user=" + loginData.UserID.ToString();
                    }
                    #endregion
                    DataTable dtG = cnn.CreateDataTable(strG);
                    if (dtG.Rows.Count == 0)
                        return JsonResultCommon.ThanhCong(new List<string>(), null, Visible);
                        ds = GetTasks(loginData.CustomerID, Request.Headers, cnn, query, long.Parse(loginData.UserID.ToString()), DataAccount, strW);
                    if (cnn.LastError != null || ds == null)
                        return JsonResultCommon.Exception(_logger, cnn.LastError, _config, loginData, ControllerContext);
                    var temp = filterWork(ds.Tables[0].AsEnumerable().Where(x => x["id_parent"] == DBNull.Value), query.filter);//k bao gồm con
                    var tags = ds.Tables[1].AsEnumerable();
                    // Phân trang
                    int total = temp.Count();
                    if (total == 0)
                        return JsonResultCommon.ThanhCong(new List<string>(), pageModel, Visible);
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
                    var Children = getChild_Widget(domain, loginData.CustomerID, "", displayChild, loginData.UserID, dtNew.CopyToDataTable().AsEnumerable(), tags, DataAccount, loginData, ConnectionString);
                    return JsonResultCommon.ThanhCong(Children, pageModel, Visible);
                }
            }
            catch (Exception ex)
            {
                return JsonResultCommon.Exception(_logger, ex, _config, loginData);
            }
        }
        /// <summary>
        ///  my-list-wiget
        /// </summary>
        /// <param name="query"></param>
        /// <returns></returns>
        [Route("page-work-staff")]
        [HttpGet]
        public object CongviecNhanvien([FromQuery] QueryParams query)
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
                string ConnectionString = JeeWorkLiteController.getConnectionString(ConnectionCache, loginData.CustomerID, _configuration);
                using (DpsConnection cnn = new DpsConnection(ConnectionString))
                {
                    string listDept = JeeWorkLiteController.getListDepartment_GetData(loginData, cnn, HttpContext.Request.Headers, _configuration, ConnectionString);
                    #region Lấy dữ liệu account từ JeeAccount
                    DataAccount = JeeWorkLiteController.GetAccountFromJeeAccount(HttpContext.Request.Headers, _configuration);
                    if (DataAccount == null)
                        return JsonResultCommon.Custom("Lỗi lấy danh sách nhân viên từ hệ thống quản lý tài khoản");
                    string listIDNV = "0";
                    List<AccUsernameModel> DataStaff = JeeWorkLiteController.GetMyStaff(HttpContext.Request.Headers, _configuration, loginData);
                    if (DataStaff == null)
                        return JsonResultCommon.KhongCoDuLieu();
                    List<string> nvs = DataStaff.Select(x => x.UserId.ToString()).ToList();
                    if (nvs.Count() > 0)
                        listIDNV = string.Join(",", nvs);
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
                    string strW = $" and (w.id_nv in ({listIDNV}) or w.createdby in ({listIDNV}))";
                    string displayChild = "0";//hiển thị con: 0-không hiển thị, 1- 1 cấp con, 2- nhiều cấp con
                    if (!string.IsNullOrEmpty(query.filter["displayChild"]))
                        displayChild = query.filter["displayChild"];

                    string columnName = "id_project_team";
                    if (!string.IsNullOrEmpty(query.filter["timedeadline"]))
                        strW += $"and (w.deadline is not null and deadline >= GETUTCDATE() and deadline =< '{query.filter["timedeadline"]}')";
                    if (!string.IsNullOrEmpty(query.filter["tinhtrang"]))
                    {
                        string tinhtrang = query.filter["tinhtrang"];
                        string hoanthanh = ReportController.GetListStatusDynamic(listDept, cnn, " IsFinal "); // IsFinal
                        string quahan = ReportController.GetListStatusDynamic(listDept, cnn, "IsDeadline"); // IsDeadline
                        string todo = ReportController.GetListStatusDynamic(listDept, cnn, "IsTodo"); //IsTodo
                        if (tinhtrang == "todo")
                        {
                            strW += " and" + sql_dangthuchien; ;
                        }
                        else if (tinhtrang == "deadline")
                        {
                            strW += " and" + sql_isquahan;
                        }
                        else
                        {
                            strW += queryhoanthanh;
                        };
                    }
                    #region group
                    string strG = @"select distinct p.id_row, p.title from we_project_team_user u
                                    join we_project_team p on p.id_row=u.id_project_team 
                                    where u.disabled=0 and p.Disabled=0 ";
                    if (!string.IsNullOrEmpty(loginData.UserID.ToString()))
                    {
                        strG += "and id_user=" + loginData.UserID.ToString();
                    }
                    #endregion
                    DataTable dtG = cnn.CreateDataTable(strG);
                    if (dtG.Rows.Count == 0)
                        return JsonResultCommon.ThanhCong(new List<string>(), null, Visible);
                    DataSet ds = GetTasks(loginData.CustomerID, Request.Headers, cnn, query, long.Parse(loginData.UserID.ToString()), DataAccount, strW);
                    if (cnn.LastError != null || ds == null)
                        return JsonResultCommon.Exception(_logger, cnn.LastError, _config, loginData, ControllerContext);
                    var temp = filterWork(ds.Tables[0].AsEnumerable().Where(x => x["id_parent"] == DBNull.Value), query.filter);//k bao gồm con
                    var tags = ds.Tables[1].AsEnumerable();
                    // Phân trang
                    int total = temp.Count();
                    if (total == 0)
                        return JsonResultCommon.ThanhCong(new List<string>(), pageModel, Visible);
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
                    var Children = getChild_Widget("", loginData.CustomerID, "", displayChild, loginData.UserID, dtNew.CopyToDataTable().AsEnumerable(), tags, DataAccount, loginData, ConnectionString);
                    return JsonResultCommon.ThanhCong(Children, pageModel, Visible);
                }
            }
            catch (Exception ex)
            {
                return JsonResultCommon.Exception(_logger, ex, _config, loginData);
            }
        }
        /// <summary>
        /// Write log
        /// </summary>
        /// <param name="cnn"></param>
        /// <param name="id_action"></param>
        /// <param name="object_id"></param>
        /// <param name="id_user"></param>
        /// <param name="log_content"></param>
        /// <param name="_old"></param>
        /// <param name="_new"></param>
        /// <returns></returns>
        public static bool log(DpsConnection cnn, int id_action, long object_id, long id_user, string log_content = "", object _old = null, object _new = null)
        {
            Hashtable val = new Hashtable();
            val["id_action"] = id_action;
            val["object_id"] = object_id;
            val["CreatedBy"] = id_user;
            if (!string.IsNullOrEmpty(log_content))
                val["log_content"] = log_content;
            if (_old == null)
                val["oldvalue"] = DBNull.Value;
            else
                val["oldvalue"] = _old;
            if (_new == null)
                val["newvalue"] = DBNull.Value;
            else
                val["newvalue"] = _new;
            return cnn.Insert(val, "we_log") == 1;
        }

        /// <summary>
        /// Trang danh sách công việc chính
        /// </summary>
        /// <param name="query"></param>
        /// <returns></returns>
        [Route("works-by-project")]
        [HttpGet]
        public object WorksByProject([FromQuery] QueryParams query)
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

                    #region filter thời gian, keyword, group by
                    DateTime from = Common.GetDateTime();
                    DateTime to = Common.GetDateTime();
                    long id_project_team = 0;
                    DataTable dt_filter_tmp = new DataTable();
                    id_project_team = int.Parse(query.filter["id_project_team"]);
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
                    string strW = "";
                    DataTable dt_Fields = JeeWorkLiteController.ListField(long.Parse(query.filter["id_project_team"]), 3, cnn);

                    if (!string.IsNullOrEmpty(query.filter["keyword"]))
                    {
                        strW = " and (w.title like N'%@keyword%' or w.description like N'%@keyword%')";
                        strW = strW.Replace("@keyword", query.filter["keyword"]);
                    }
                    DataTable dt_tag = new DataTable();
                    DataTable dt_user = new DataTable();
                    // kiểm tra quyền xem công việc người khác
                    new Common(ConnectionString);
                    bool rs = Common.CheckRoleByProject(query.filter["id_project_team"].ToString(), loginData, cnn, ConnectionString);
                    if (!rs)
                    {
                        long userid = loginData.UserID;
                        strW += @$" and w.id_row in (select id_parent from we_work ww join we_work_user wu on ww.id_row = wu.id_work where ww.disabled = 0 and wu.disabled = 0 and id_parent is not null and id_user = {userid}
 union all select id_row from v_wework_new where id_nv = {userid} or createdby = {userid} )";
                    }
                    string sql = "select num_comment as comments, *, iIf(id_group is null,0,id_group) as id_group ,work_group ";
                    sql += $@" from v_wework_clickup_new w where (where) and w.disabled = 0 " + strW;
                    SqlConditions conds = new SqlConditions();
                    conds.Add("id_project_team", id_project_team);

                    DataTable result = new DataTable();
                    result = cnn.CreateDataTable(sql, "(where)", conds);
                    DataTable dt_comments = cnn.CreateDataTable("select id_row, object_type, object_id, comment, id_parent, Disabled " +
                       "from we_comment where disabled = 0 and object_type = 1");
                    string queryTag = @"select a.id_row,a.title,a.color,b.id_work from we_tag a join we_work_tag b on a.id_row=b.id_tag 
                                                    where a.disabled=0 and b.disabled = 0 and a.id_project_team = " + query.filter["id_project_team"] + "and id_work = ";
                    string queryUser = $@"select w_user.id_work, w_user.id_user, w_user.loai, id_child, w_user.Disabled, '' as hoten,'' as image,'' as username,'' as tenchucdanh , id_project_team
                                                    from we_work_user w_user join we_work on we_work.id_row = w_user.id_work 
                                                    where w_user.Disabled = 0 and w_user.loai = 1 and we_work.id_project_team = " + query.filter["id_project_team"] + "and id_work = ";
                    result.Columns.Add("Tags", typeof(DataTable));
                    result.Columns.Add("User", typeof(DataTable));
                    result.Columns.Add("DataStatus", typeof(DataTable));
                    result.Columns.Add("DataChildren", typeof(DataTable));
                    DataColumnCollection columns = result.Columns;
                    if (!columns.Contains("id_nv"))
                    {
                        result.Columns.Add("id_nv", typeof(string));
                    }
                    if (result.Rows.Count > 0)
                    {
                        foreach (DataRow dr in result.Rows)
                        {
                            dr["comments"] = WorkClickupController.SoluongComment(dr["id_row"].ToString(), ConnectionString);//  dt_comments.Compute("count(id_row)", "object_id =" + dr["id_row"].ToString() + "").ToString();
                            dr["Tags"] = cnn.CreateDataTable(queryTag + dr["id_row"]);

                            DataTable dt_u = cnn.CreateDataTable(queryUser + dr["id_row"]);
                            #region Map info account từ JeeAccount
                            foreach (DataRow item in dt_u.Rows)
                            {
                                var info = DataAccount.Where(x => item["id_user"].ToString().Contains(x.UserId.ToString())).FirstOrDefault();
                                if (info != null)
                                {
                                    item["hoten"] = info.FullName;
                                    item["image"] = info.AvartarImgURL;
                                    item["username"] = info.Username;
                                    item["tenchucdanh"] = info.Jobtitle;
                                }
                            }
                            #endregion
                            dr["User"] = dt_u;
                            dr["DataStatus"] = WorkClickupController.list_status_user(dr["id_row"].ToString(), dr["id_project_team"].ToString(), loginData, ConnectionString, DataAccount);
                            result.AcceptChanges();
                            DataRow[] r_parent = result.Select("id_parent is not null and id_parent =" + dr["id_row"]);
                            DataTable dt_parent = new DataTable();
                            if (string.IsNullOrEmpty(dr["id_parent"].ToString()) && r_parent.Length > 0)
                            {
                                #region Lấy thông tin subtask tương ứng
                                dt_parent = r_parent.CopyToDataTable();
                                for (int i = 0; i < dt_parent.Rows.Count; i++)
                                {
                                    dr["comments"] = WorkClickupController.SoluongComment(dt_parent.Rows[i]["id_row"].ToString(), ConnectionString);//  dt_comments.Compute("count(id_row)", "object_id =" + dr["id_row"].ToString() + "").ToString();
                                    dt_parent.Rows[i]["Tags"] = cnn.CreateDataTable(queryTag + dt_parent.Rows[i]["id_row"]);
                                    dr["User"] = dt_u;
                                    dt_parent.Rows[i]["DataStatus"] = WorkClickupController.list_status_user(dt_parent.Rows[i]["id_row"].ToString(), id_project_team.ToString(), loginData, ConnectionString, DataAccount);
                                    dt_parent.AcceptChanges();
                                }
                                #endregion
                                dr["DataChildren"] = dt_parent;
                            }
                            else
                                dr["DataChildren"] = dt_parent;
                        }
                    }
                    DataTable dt_data = new DataTable();
                    DataTable tmp = new DataTable();
                    if (result.Rows.Count > 0)
                    {
                        var filterTeam = " id_parent is null and id_project_team = " + (query.filter["id_project_team"]);
                        var rows = result.Select(filterTeam);
                        if (rows.Any())
                            tmp = rows.CopyToDataTable();
                        //foreach (DataRow item in tmp.Rows)
                        //{
                        //    item["DataChildren"] = dtChildren(item["id_row"].ToString(), result, cnn, dt_Fields, query.filter["id_project_team"], DataAccount, loginData);
                        //}
                        //DataSet ds = GetWork_ClickUp(cnn, query, loginData.UserID, DataAccount, listDept, strW);
                        //if (cnn.LastError != null || ds == null)
                        //    return JsonResultCommon.Exception(_logger, cnn.LastError, _config, loginData, ControllerContext);
                        //dt_data = ds.Tables[0];
                        //var tags = ds.Tables[1].AsEnumerable();
                    }
                    // Phân trang
                    int total = tmp.Rows.Count;
                    if (total == 0)
                        return JsonResultCommon.KhongCoDuLieu();
                    if (query.more)
                    {
                        query.page = 1;
                        query.record = total;
                    }
                    pageModel.TotalCount = total;
                    pageModel.AllPage = (int)Math.Ceiling(total / (decimal)query.record);
                    pageModel.Size = query.record;
                    pageModel.Page = query.page;
                    tmp = tmp.AsEnumerable().Skip((query.page - 1) * query.record).Take(query.record).CopyToDataTable();
                    //chỉ lấy datawork
                    return JsonResultCommon.ThanhCong(tmp, pageModel, Visible);
                }
            }
            catch (Exception ex)
            {
                return JsonResultCommon.Exception(_logger, ex, _config, loginData);
            }
        }
        /// <summary>
        /// danh sách hoạt động
        /// </summary>
        /// <param name="query"></param>
        /// <returns></returns>
        [Route("List-activities")]
        [HttpGet]
        public object ListActivities([FromQuery] QueryParams query)
        {
            UserJWT loginData = Ulities.GetUserByHeader(HttpContext.Request.Headers);
            if (loginData == null)
                return JsonResultCommon.DangNhap();
            if (query == null)
                query = new QueryParams();
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
                string ConnectionString = JeeWorkLiteController.getConnectionString(ConnectionCache, loginData.CustomerID, _configuration);
                string domain = _configuration.GetValue<string>("Host:JeeWork_API") + "/";
                using (DpsConnection cnn = new DpsConnection(ConnectionString))
                {
                    bool Visible = Common.CheckRoleByUserID(loginData, 3502, cnn);

                    string sql = @"select distinct p.id_row, p.title, is_project from we_project_team p
                                join we_department d on d.id_row = p.id_department
                                join we_project_team_user u on u.id_project_team = p.id_row
                                 where u.Disabled = 0 and id_user = " + loginData.UserID + " and p.Disabled = 0  and d.Disabled = 0 and IdKH=" + loginData.CustomerID + " (where) order by title";

                    SqlConditions Conds = new SqlConditions();
                    string dieukienSort = "id_row";
                    if (!string.IsNullOrEmpty(query.filter["id_project_team"]))
                    {
                        Conds.Add("id_project_team", query.filter["id_project_team"]);
                        sql = sql.Replace("(where)", " and p.id_row = " + query.filter["id_project_team"]);
                    }
                    else
                    {
                        Conds.Add("id_project_team", 0);
                        sql = sql.Replace("(where)", " ");
                    }
                    Conds.Add("IDKH", loginData.CustomerID);
                    //load team 
                    DataTable dt_team = cnn.CreateDataTable(sql);
                    #region Sort data theo các dữ liệu bên dưới
                    Dictionary<string, string> sortableFields = new Dictionary<string, string>
                        {
                            { "id_row", "id_row"},
                            { "title", "title"},
                            { "CreatedDate", "CreatedDate"}
                        };
                    #endregion
                    if (!string.IsNullOrEmpty(query.sortField) && sortableFields.ContainsKey(query.sortField))
                        dieukienSort = sortableFields[query.sortField] + ("desc".Equals(query.sortOrder) ? " desc" : " asc");
                    #region Trả dữ liệu về backend để hiển thị lên giao diện
                    string sqlq = "";
                    //if (!role.IsUserInRole(loginData.UserName, "3502"))
                    //{
                    sqlq = @$"exec GetActivitiesNew @IDKH, @id_project_team";
                    //}
                    DataSet ds = cnn.CreateDataSet(sqlq, Conds);
                    #region Map info account từ JeeAccount
                    ds.Tables[0].Columns.Add("hoten", typeof(string));
                    ds.Tables[0].Columns.Add("username", typeof(string));
                    ds.Tables[0].Columns.Add("tenchucdanh", typeof(string));
                    ds.Tables[0].Columns.Add("mobile", typeof(string));
                    ds.Tables[0].Columns.Add("image", typeof(string));
                    ds.Tables[0].Columns.Add("ColorStatus_Old", typeof(string));
                    ds.Tables[0].Columns.Add("ColorStatus_New", typeof(string));
                    foreach (DataRow item in ds.Tables[0].Rows)
                    {
                        var info = DataAccount.Where(x => item["id_nv"].ToString().Contains(x.UserId.ToString())).FirstOrDefault();
                        if (info != null)
                        {
                            item["hoten"] = info.FullName;
                            item["username"] = info.Username;
                            item["tenchucdanh"] = info.Jobtitle;
                            item["mobile"] = info.PhoneNumber;
                            item["image"] = info.AvartarImgURL;
                        }
                    }
                    #endregion
                    if (cnn.LastError != null || ds == null)
                        return JsonResultCommon.Exception(_logger, cnn.LastError, _config, loginData, ControllerContext);
                    DataTable dt = ds.Tables[0];
                    if (dt.Rows.Count == 0)
                        return JsonResultCommon.ThanhCong(new List<string>(), pageModel, Visible);
                    if (Visible)
                    {
                        for (int i = dt.Rows.Count - 1; i >= 0; i--)
                        {
                            DataRow dr = dt.Rows[i];
                            if (int.Parse(dr["createdby"].ToString()) != loginData.UserID)
                                dr.Delete();
                        }
                        dt.AcceptChanges();
                    }
                    var temp = dt.AsEnumerable();
                    if (!string.IsNullOrEmpty(query.filter["keyword"]))
                    {
                        string keyword = query.filter["keyword"].ToLower();
                        temp = temp.Where(x => x["title"].ToString().ToLower().Contains(keyword) ||
                        (x["log_content"] != DBNull.Value && x["log_content"].ToString().ToLower().Contains(keyword)) ||
                        x["action"].ToString().ToLower().Contains(keyword) ||
                        x["action_en"].ToString().ToLower().Contains(keyword));
                    }
                    if (temp.Count() == 0)
                    {
                        return JsonResultCommon.ThanhCong(new List<string>(), pageModel, Visible);
                    }
                    string sql_query = ""; // áp dụng cho trường hợp lấy dữ liệu khóa ngoại
                    dt = temp.CopyToDataTable();
                    foreach (DataRow dr in dt.Rows)
                    {
                        if (dr["sql"] != DBNull.Value)
                        {
                            sql_query = dr["sql"].ToString();
                            sql_query = sql_query.Replace("$DB_HR$", _config.HRCatalog);
                            DataTable dt_temp = cnn.CreateDataTable(sql_query, new SqlConditions() { { "old", dr["oldvalue"] }, { "new", dr["newvalue"] } });
                            if (dt_temp == null)
                                return JsonResultCommon.Exception(_logger, cnn.LastError, _config, loginData, ControllerContext);

                            if (int.Parse(dr["id_action"].ToString()) == 9 || int.Parse(dr["id_action"].ToString()) == 5 || int.Parse(dr["id_action"].ToString()) == 6) // Đối với tag gắn title
                            {
                                if (dt_temp.Rows.Count > 0)
                                    dr["action"] = dr["action"].ToString().Replace("{0}", dt_temp.Rows[0]["title"].ToString());
                                else
                                    dr["action"] = dr["action"].ToString().Replace("{0}", "");
                            }

                            if (int.Parse(dr["id_action"].ToString()) == 44) // Đối với status trả về thêm Color
                            {
                                dr["ColorStatus_Old"] = dt_temp.AsEnumerable().Where(x => x[0].ToString() == dr["oldvalue"].ToString()).Select(x => x[2]).FirstOrDefault();
                                dr["ColorStatus_New"] = dt_temp.AsEnumerable().Where(x => x[0].ToString() == dr["newvalue"].ToString()).Select(x => x[2]).FirstOrDefault();
                            }

                            dr["oldvalue"] = dt_temp.AsEnumerable().Where(x => x[0].ToString() == dr["oldvalue"].ToString()).Select(x => x[1]).FirstOrDefault();
                            dr["newvalue"] = dt_temp.AsEnumerable().Where(x => x[0].ToString() == dr["newvalue"].ToString()).Select(x => x[1]).FirstOrDefault();
                        }
                        #region Map info account từ JeeAccount 
                        var info = DataAccount.Where(x => dr["id_nv"].ToString().Contains(x.UserId.ToString())).FirstOrDefault();

                        if (dr["id_action"].ToString().Equals("15") || dr["id_action"].ToString().Equals("55") || dr["id_action"].ToString().Equals("56") || dr["id_action"].ToString().Equals("57"))
                        {
                            var infoUser = DataAccount.Where(x => dr["newvalue"].ToString().Contains(x.UserId.ToString())).FirstOrDefault();
                            if (infoUser != null)
                            {
                                dr["action"] = dr["action"].ToString().Replace("{0}", infoUser.FullName);
                                dr["action_en"] = dr["action_en"].ToString().Replace("{0}", infoUser.FullName);
                            }
                            dr["oldvalue"] = DBNull.Value;
                            dr["newvalue"] = DBNull.Value;

                        }
                        #endregion
                        #region Load dữ liệu hiển thị gắn thẻ quan trong 
                        //if (dr["id_action"].ToString().Equals("8"))
                        //{
                        //    dr["oldvalue"] = ProjectTeamController.getDouutien(dr["oldvalue"].ToString());
                        //    dr["newvalue"] = ProjectTeamController.getDouutien(dr["newvalue"].ToString());
                        //}
                        #endregion
                        #region Map info account từ JeeAccount 
                        if (dr["object_type"].ToString().Equals("3"))
                        {
                            dr["oldvalue"] = DBNull.Value;
                            dr["newvalue"] = DBNull.Value;
                        }
                        #endregion
                    }
                    int total = dt.Rows.Count;
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
                    dt = dt.AsEnumerable().Skip((query.page - 1) * query.record).Take(query.record).CopyToDataTable();
                    var data = from r in dt.AsEnumerable()
                               group r by new { a = r["object_type"], b = r["object_id"], c = r["title"], d = r["project_team"], e = r["id_project_team"] } into g
                               select new
                               {
                                   object_type = g.Key.a,
                                   object_id = g.Key.b,
                                   title = g.Key.c,
                                   project_team = g.Key.d,
                                   id_project_team = g.Key.e,
                                   Activities = from u in g
                                                select new
                                                {
                                                    id_row = u["id_row"],
                                                    action = u["action"],
                                                    action_en = u["action_en"],
                                                    view_detail = u["view_detail"],
                                                    log_content = u["log_content"],
                                                    icon = u["icon"],
                                                    id_action = u["id_action"],
                                                    ColorStatus_Old = u["ColorStatus_Old"],
                                                    ColorStatus_New = u["ColorStatus_New"],
                                                    oldvalue = u["oldvalue"],
                                                    newvalue = u["newvalue"],
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
                                                }
                               };
                    var res = from t in dt_team.AsEnumerable()
                              select new
                              {
                                  id_row = t["id_row"],
                                  title = t["title"],
                                  task = from r in dt.AsEnumerable()
                                         group r by new { a = r["object_type"], b = r["object_id"], c = r["title"], d = r["project_team"], e = r["id_project_team"] } into g
                                         where t["id_row"].ToString() == g.Key.e.ToString()
                                         select new
                                         {
                                             object_type = g.Key.a,
                                             object_id = g.Key.b,
                                             title = g.Key.c,
                                             project_team = g.Key.d,
                                             id_project_team = g.Key.e,
                                             Activities = from u in g
                                                          select new
                                                          {
                                                              id_row = u["id_row"],
                                                              action = u["action"],
                                                              action_en = u["action_en"],
                                                              view_detail = u["view_detail"],
                                                              log_content = u["log_content"],
                                                              icon = u["icon"],
                                                              id_action = u["id_action"],
                                                              ColorStatus_Old = u["ColorStatus_Old"],
                                                              ColorStatus_New = u["ColorStatus_New"],
                                                              oldvalue = u["oldvalue"],
                                                              newvalue = u["newvalue"],
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
                                                          }
                                         }
                              };
                    res = res.Where(x => x.task.Count() > 0);
                    return JsonResultCommon.ThanhCong(res, pageModel, Visible);
                }
                #endregion
            }
            catch (Exception ex)
            {
                return JsonResultCommon.Exception(_logger, ex, _config, loginData);
            }
        }
        public static DataTable dtChildren(string id_parent, DataTable data, DpsConnection cnn, DataTable dataField, string id_project_team, List<AccUsernameModel> DataAccount, UserJWT loginData)
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
                                , id_child, w_user.disabled, '' as hoten,'' as image, id_project_team
                                from we_work_user w_user 
                                join we_work 
                                on we_work.id_row = w_user.id_work 
                                where w_user.Disabled = 0 
                                and we_work.id_project_team = " + id_project_team + " and id_work = ";
            result.Columns.Add("Tags", typeof(DataTable));
            result.Columns.Add("User", typeof(DataTable));
            result.Columns.Add("Follower", typeof(DataTable));
            //result.Columns.Add("comments", typeof(string));
            result.Columns.Add("DataChildren", typeof(DataTable));
            result.Columns.Add("DataStatus", typeof(DataTable));
            DataRow[] row = data.Select("id_parent in (" + id_parent + ")");
            foreach (DataRow dr in row)
            {
                DataRow drow = result.NewRow();
                foreach (DataRow field in dataField.Rows)
                {
                    if (!(bool)field["isnewfield"])
                        drow[field["fieldName"].ToString()] = dr[field["fieldName"].ToString()];
                }
                drow["DataChildren"] = dtChildren(dr["id_row"].ToString(), data, cnn, dataField, id_project_team, DataAccount, loginData);
                result.Rows.Add(drow);
            }
            if (result.Rows.Count > 0)
            {
                foreach (DataRow dr in result.Rows)
                {
                    dr["Tags"] = cnn.CreateDataTable(queryTag + dr["id_row"]);
                    //DataTable user = cnn.CreateDataTable(queryUser + dr["id_row"]);
                    DataTable user = cnn.CreateDataTable(queryUser + dr["id_row"] + " and loai = 1");
                    DataTable follower = cnn.CreateDataTable(queryUser + dr["id_row"] + " and loai = 2");
                    #region Map info account từ JeeAccount
                    foreach (DataRow item in user.Rows)
                    {
                        var info = DataAccount.Where(x => item["id_user"].ToString().Contains(x.UserId.ToString())).FirstOrDefault();
                        if (info != null)
                        {
                            item["hoten"] = info.FullName;
                            item["image"] = info.AvartarImgURL;
                        }
                    }
                    dr["User"] = user;
                    foreach (DataRow item in follower.Rows)
                    {
                        var info = DataAccount.Where(x => item["id_user"].ToString().Contains(x.UserId.ToString())).FirstOrDefault();
                        if (info != null)
                        {
                            item["hoten"] = info.FullName;
                            item["image"] = info.AvartarImgURL;
                        }
                    }
                    dr["Follower"] = follower;
                    #endregion
                }
            }
            return result;
        }
        internal static List<bool> CheckKeyChange(List<string> keys, DataTable Old_Data, DataTable New_Data)
        {
            List<bool> re = new List<bool>();
            string logEditContent = GetEditLogContent(Old_Data, New_Data);
            logEditContent = " | " + logEditContent;
            foreach (var key in keys)
            {
                string temp = " | " + key + ":";
                re.Add(logEditContent.Contains(temp));
            }
            return re;
        }
        public static string GetEditLogContent(DataTable Old_Data, DataTable New_Data)
        {
            string result = "";
            if ((Old_Data.Rows.Count > 0) && (New_Data.Rows.Count > 0))
            {
                for (int i = 0; i < Old_Data.Columns.Count; i++)
                {
                    if (Old_Data.Rows[0][i].ToString() != New_Data.Rows[0][i].ToString())
                        result += " | " + Old_Data.Columns[i].ColumnName + ": " + Old_Data.Rows[0][i].ToString() + " ^ " + New_Data.Rows[0][i].ToString();
                }
            }
            if (!"".Equals(result)) result = result.Substring(3);
            return result;
        }
        public static DataSet GetTasks(long customerid, IHeaderDictionary _header, DpsConnection cnn, QueryParams query, long curUser, List<AccUsernameModel> DataAccount, string dieukien_where = "", long meetingid = 0)
        {
            SqlConditions Conds = new SqlConditions();
            Conds.Add("iduser", curUser);
            #region Code filter
            string dieukienSort = "w.createddate";
            long id_project_team = 0;
            if (!string.IsNullOrEmpty(query.filter["id_project_team"]))
            {
                id_project_team = long.Parse(query.filter["id_project_team"].ToString());
                dieukien_where += " and id_project_team=@id_project_team";
                Conds.Add("id_project_team", id_project_team);
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
            string id_moitao = "";
            id_moitao = GetListStatusDynamic(cnn, customerid);
            if (!string.IsNullOrEmpty(query.sortField) && sortableFields.ContainsKey(query.sortField))
                dieukienSort = sortableFields[query.sortField] + ("desc".Equals(query.sortOrder) ? " desc" : " asc");
            #region Return data to backend to display on the interface
            string sqlq = @$"select  distinct w.id_row,w.title,w.description,w.id_project_team,w.id_group,w.estimates
                            ,w.deadline,w.id_milestone, w.milestone,
                            w.id_parent,w.start_date,w.end_date,w.urgent,w.important,w.prioritize
                            ,w.status,w.result,w.createddate,w.createdby,
                            w.UpdatedDate,w.updatedby, w.project_team,w.id_department
                            , w.clickup_prioritize , w.nguoigiao,'' as hoten_nguoigiao, Id_NV,''as hoten
                            , Iif(fa.id_row is null ,0,1) as favourite 
                            ,coalesce( f.count,0) as num_file, coalesce( com.count,0) as num_com
                            ,'' as NguoiTao, '' as NguoiSua 
                            , w.accepted_date, w.activated_date, w.closed_date, w.state_change_date,
                            w.activated_by, w.closed_by, w.closed, w.closed_work_date, w.closed_work_by
                            ,IIf((select count(*) from we_status tbu where tbu.Disabled=0 and tbu.id_row=tb.w and w.status in( {id_moitao} )>0,1,0) as isnew 
                            ,iIf(w.deadline < GETUTCDATE() and w.deadline is not null and w.end_date is null  ,1,0) as TreHan -- Trễ hạn: Ngày kết thúc is null và deadline is not null và deadline < GETUTCDATE()
                            ,iIf(w.end_date is not null ,1,0) as Done --Hoàn thành: Ngày kết thúc is not null và deadline is not null và deadline < GETUTCDATE()
                            from v_wework_new w 
                            left join (select count(*) as count,object_id 
                            from we_attachment where object_type=1 group by object_id) f on f.object_id=w.id_row
                            left join (select count(*) as count,object_id
                            from we_comment where object_type=1 group by object_id) com on com.object_id=w.id_row
                            left join we_work_favourite fa 
                            on fa.id_work=w.id_row and fa.createdby=@iduser and fa.disabled=0
                            where 1=1 " + dieukien_where + " order by " + dieukienSort;
            sqlq += ";select id_work, id_tag,color, title " +
                "from we_work_tag wt join we_tag t on wt.id_tag=t.id_row where wt.disabled=0 and t.disabled=0";
            string where_string = "";
                where_string = " and id_project_team=" + id_project_team;
            if (!string.IsNullOrEmpty(query.filter["status"]))
            {
                dieukien_where += " and status=" + query.filter["status"];
            }
            sqlq += where_string;
            //người theo dõi
            sqlq += @$";select id_work,id_user,'' as hoten from we_work_user u 
                        where u.disabled = 0 and u.loai = 2";
            DataSet ds = cnn.CreateDataSet(sqlq, Conds);
            #endregion
            DataTable dt_task = new DataTable();
            dt_task = ds.Tables[0];
            if (!string.IsNullOrEmpty(query.filter["stage"]))
            {
                
            }
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
        }
        public static string GetListStatusDynamic(DpsConnection cnn, long customerid)
        {
            string sql = "";
            sql = @$"select distinct * from we_status 
                           where disabled = 0 and type = 1 and isdefault = 1 
                            and isfinal = 0 and IsDeadline = 0 and IsToDo = 0
                            and id_project_team 
                            in (select p.id_row from we_project_team p 
                            join we_department de 
                            on de.id_row = p.id_department where p.disabled = 0 
                            and de.disabled = 0 and idkh = "+customerid+")";
            DataTable dt = cnn.CreateDataTable(sql);
            List<string> nvs = dt.AsEnumerable().Select(x => x["id_row"].ToString()).ToList();
            string ids = string.Join(",", nvs);
            if (string.IsNullOrEmpty(ids))
            {
                return "0";
            }
            return ids;
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
        public static object getChild_Widget(string domain, long IdKHDPS, string columnName, string displayChild, object id, EnumerableRowCollection<DataRow> temp, EnumerableRowCollection<DataRow> tags, List<AccUsernameModel> DataAccount, UserJWT loginData, string ConnectString, object parent = null)
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
                         comments = WorkClickupController.SoluongComment(r["id_row"].ToString(), ConnectString),  // SL bình luận
                         status_info = JeeWorkLiteController.get_info_status(r["status"].ToString(), ConnectString),
                         DataStatus = WorkClickupController.list_status_user(r["id_row"].ToString(), r["id_project_team"].ToString(), loginData, ConnectString, DataAccount),
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
                         Childs = displayChild == "0" ? new List<string>() : getChild_Widget(domain, IdKHDPS, columnName, displayChild == "1" ? "0" : "2", id, temp, tags, DataAccount, loginData, ConnectString, r["id_row"])
                     };
            return re;
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

    }
}