﻿using DpsLibs.Data;
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
        private readonly IHostingEnvironment _hostingEnvironment;
        private JeeWorkConfig _config;
        public List<AccUsernameModel> DataAccount;
        private IConnectionCache ConnectionCache;
        private IConfiguration _configuration;
        private readonly ILogger<WidgetsController> _logger;

        static string sql_isquahan = " w.deadline < GETUTCDATE() and w.deadline is not null and w.end_date is null ";
        static string sql_dangthuchien = "((w.deadline >= GETUTCDATE() and deadline is not null) or deadline is null ) and w.end_date is null";
        static string sqlhoanthanhdunghan = " w.end_date is not null and (w.deadline >= w.end_date or w.deadline is null) ";
        static string sqlhoanthanhquahan = " w.end_date is not null and w.deadline < w.end_date";
        static string sqlhoanthanh = " w.end_date is not null ";
        // kiểm tra điều kiện hoành thành
        string queryhoanthanh = " and w.end_date is not null ";
        string querydangthuchien = " and w.end_date is null ";

        public WidgetsController(IOptions<JeeWorkConfig> config, IHostingEnvironment hostingEnvironment, IConnectionCache _cache, IConfiguration configuration, ILogger<WidgetsController> logger)
        {
            _hostingEnvironment = hostingEnvironment;
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
                    #region Trả dữ liệu về backend để hiển thị lên giao diện

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
                                       percentage = WeworkLiteController.calPercentage(r["tong"], r["ht"])
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
                string ConnectionString = WeworkLiteController.getConnectionString(ConnectionCache, loginData.CustomerID, _configuration);
                using (DpsConnection cnn = new DpsConnection(ConnectionString))
                {
                    string listDept = WeworkLiteController.getListDepartment_GetData(loginData, cnn, HttpContext.Request.Headers, _configuration, ConnectionString);
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
                    cond.Add("from", WeworkLiteController.GetUTCTime(Request.Headers, from.ToString()));
                    bool to1 = DateTime.TryParseExact(query.filter["DenNgay"], "dd/MM/yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out to);
                    if (!to1)
                        return JsonResultCommon.Custom("Thời gian kết thúc không hợp lệ");
                    strW += " and w." + collect_by + "<@to";
                    cond.Add("to", WeworkLiteController.GetUTCTime(Request.Headers, to.ToString()));

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
                    DataAccount = WeworkLiteController.GetAccountFromJeeAccount(HttpContext.Request.Headers, _configuration);
                    if (DataAccount == null)
                        return JsonResultCommon.Custom("Lỗi lấy danh sách nhân viên từ hệ thống quản lý tài khoản");

                    string error = "";
                    string listID = WeworkLiteController.ListAccount(HttpContext.Request.Headers, out error, _configuration);
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
                string ConnectionString = WeworkLiteController.getConnectionString(ConnectionCache, loginData.CustomerID, _configuration);
                using (DpsConnection cnn = new DpsConnection(ConnectionString))
                {
                    if (!string.IsNullOrEmpty(query.filter["TuNgay"]))
                    {
                        bool from1 = DateTime.TryParseExact(query.filter["TuNgay"], "dd/MM/yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out from);
                        if (!from1)
                            return JsonResultCommon.Custom("Thời gian bắt đầu không hợp lệ");
                        strW += " and w." + collect_by + ">=@from";
                        cond.Add("from", WeworkLiteController.GetUTCTime(Request.Headers, from.ToString()));
                    }
                    if (!string.IsNullOrEmpty(query.filter["DenNgay"]))
                    {
                        bool to1 = DateTime.TryParseExact(query.filter["DenNgay"], "dd/MM/yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out to);
                        if (!to1)
                            return JsonResultCommon.Custom("Thời gian kết thúc không hợp lệ");
                        strW += " and w." + collect_by + "<@to";
                        cond.Add("to", WeworkLiteController.GetUTCTime(Request.Headers, to.ToString()));
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
                        string listDept = WeworkLiteController.getListDepartment_GetData(loginData, cnn, HttpContext.Request.Headers, _configuration, ConnectionString);
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
                    DataSet ds = WorkClickupController.GetWorkByEmployee(Request.Headers, cnn, query, long.Parse(loginData.UserID.ToString()), DataAccount, strW);
                    if (cnn.LastError != null || ds == null)
                        return JsonResultCommon.Exception(_logger, cnn.LastError, _config, loginData, ControllerContext);
                    var temp = WorkClickupController.filterWork(ds.Tables[0].AsEnumerable().Where(x => x["id_parent"] == DBNull.Value), query.filter);//k bao gồm con
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
                    var Children = WorkClickupController.getChild(domain, loginData.CustomerID, "", displayChild, 0, dtNew.CopyToDataTable().AsEnumerable(), tags, DataAccount, loginData, ConnectionString);
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
                    string strW = $"  and w.id_department in ({listDept}) ";

                    if (!string.IsNullOrEmpty(query.filter["loaicongviec"]))
                    {
                        if (int.Parse(query.filter["loaicongviec"].ToString()) == 10)
                        {
                            return CongviecNhanvien(query);
                        }

                        if (int.Parse(query.filter["loaicongviec"].ToString()) == 1) // công việc tôi được giao 
                        {
                            strW += " and (w.id_nv=@iduser or w.id_row in (select distinct id_parent from v_wework_new ww where ww.id_nv=@iduser and id_parent > 0))";
                        }
                        else if (int.Parse(query.filter["loaicongviec"].ToString()) == 2) // công việc tôi tạo
                        {
                            strW += " and w.createdby=@iduser ";
                            //strW += " and ( w.createdby=@iduser and (w.id_nv is null  or w.id_parent > 0 ))";
                        }
                        else if (int.Parse(query.filter["loaicongviec"].ToString()) == 3) // công việc giao
                        {
                            strW += " and NguoiGiao = @iduser";
                            //strW += " and ( w.id_row in (select distinct id_work from we_work_user where CreatedBy = @iduser and CreatedDate>=@from and CreatedDate<@to and Disabled = 0))";
                        }
                        else if (int.Parse(query.filter["loaicongviec"].ToString()) == 4) // công việc phụ trách (đếm chung nhắc nhở) = công việc tôi làm
                        {
                            string congviecphutrach = " and ( (w.deadline >= GETUTCDATE() and w.deadline is not null) or w.deadline is null) and w.end_date is null ";
                            string congviecconphutrach = " and ( (ww.deadline >= GETUTCDATE() and ww.deadline is not null) or ww.deadline is null) and ww.end_date is null ";
                            strW += $" and ( (w.id_nv=@iduser  {congviecphutrach} ) or w.id_row in (select distinct id_parent from v_wework_new ww where ww.id_nv=@iduser and id_parent > 0 {congviecconphutrach} )) ";
                        }
                        else if (int.Parse(query.filter["loaicongviec"].ToString()) == 5) // công việc quá hạn
                        {
                            string congviectrehan = " and w.deadline < GETUTCDATE() and w.deadline is not null and w.end_date is null ";
                            string congvieccontrehan = " and ww.deadline < GETUTCDATE() and ww.deadline is not null and ww.end_date is null ";
                            strW += $" and ( (w.id_nv=@iduser  {congviectrehan} ) or w.id_row in (select distinct id_parent from v_wework_new ww where ww.id_nv=@iduser and id_parent > 0 {congvieccontrehan} )) ";
                        }
                        else if (int.Parse(query.filter["loaicongviec"].ToString()) == 6) // công việc quá hạn trong ngày
                        {
                            DateTime today = Common.GetDateTime();
                            DateTime currentTime = today.Date.Add(new TimeSpan(0, 0, 0));
                            string congviechhtrongngay = " and w.deadline >= '" + currentTime + "' and w.deadline < '" + currentTime.AddDays(1) + "'";
                            string congviecconhhtrongngay = " and ww.deadline >= '" + currentTime + "' and ww.deadline < '" + currentTime.AddDays(1) + "'";
                            strW += $" and ( (w.id_nv=@iduser  {congviechhtrongngay} ) or w.id_row in (select distinct id_parent from v_wework_new ww where ww.id_nv=@iduser and id_parent > 0 {congviecconhhtrongngay} )) ";
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
                    DataSet ds = WorkClickupController.GetWorkByEmployee(Request.Headers, cnn, query, long.Parse(loginData.UserID.ToString()), DataAccount, strW);
                    if (cnn.LastError != null || ds == null)
                        return JsonResultCommon.Exception(_logger, cnn.LastError, _config, loginData, ControllerContext);
                    var temp = WorkClickupController.filterWork(ds.Tables[0].AsEnumerable().Where(x => x["id_parent"] == DBNull.Value), query.filter);//k bao gồm con
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
                    var Children = WorkClickupController.getChild(domain, loginData.CustomerID, "", displayChild, loginData.UserID, dtNew.CopyToDataTable().AsEnumerable(), tags, DataAccount, loginData, ConnectionString);
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
                string ConnectionString = WeworkLiteController.getConnectionString(ConnectionCache, loginData.CustomerID, _configuration);
                using (DpsConnection cnn = new DpsConnection(ConnectionString))
                {
                    string listDept = WeworkLiteController.getListDepartment_GetData(loginData, cnn, HttpContext.Request.Headers, _configuration, ConnectionString);
                    #region Lấy dữ liệu account từ JeeAccount
                    DataAccount = WeworkLiteController.GetAccountFromJeeAccount(HttpContext.Request.Headers, _configuration);
                    if (DataAccount == null)
                        return JsonResultCommon.Custom("Lỗi lấy danh sách nhân viên từ hệ thống quản lý tài khoản");

                    List<AccUsernameModel> DataStaff = WeworkLiteController.GetMyStaff(HttpContext.Request.Headers, _configuration, loginData);
                    if (DataStaff == null)
                        return JsonResultCommon.KhongCoDuLieu();
                    List<string> nvs = DataStaff.Select(x => x.UserId.ToString()).ToList();
                    string listIDNV = string.Join(",", nvs);

                    string error = "";
                    string listID = WeworkLiteController.ListAccount(HttpContext.Request.Headers, out error, _configuration);
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
                    DataSet ds = WorkClickupController.GetWorkByEmployee(Request.Headers, cnn, query, long.Parse(loginData.UserID.ToString()), DataAccount, strW);
                    if (cnn.LastError != null || ds == null)
                        return JsonResultCommon.Exception(_logger, cnn.LastError, _config, loginData, ControllerContext);
                    var temp = WorkClickupController.filterWork(ds.Tables[0].AsEnumerable().Where(x => x["id_parent"] == DBNull.Value), query.filter);//k bao gồm con
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
                    var Children = WorkClickupController.getChild("", loginData.CustomerID, "", displayChild, loginData.UserID, dtNew.CopyToDataTable().AsEnumerable(), tags, DataAccount, loginData, ConnectionString);
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

                    #region filter thời gian, keyword, group by
                    DateTime from = Common.GetDateTime();
                    DateTime to = Common.GetDateTime();
                    string groupby = "status";
                    string tableName = "";
                    string querySQL = "";
                    string data_newfield = "";
                    DataTable dt_filter_tmp = new DataTable();
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
                    string strW = "";
                    DataTable dt_Fields = WeworkLiteController.ListField(long.Parse(query.filter["id_project_team"]), 3, cnn);

                    if (!string.IsNullOrEmpty(query.filter["keyword"]))
                    {
                        strW = " and (w.title like N'%@keyword%' or w.description like N'%@keyword%')";
                        strW = strW.Replace("@keyword", query.filter["keyword"]);
                    }
                    if (!string.IsNullOrEmpty(query.filter["groupby"]))
                    {
                        groupby = query.filter["groupby"];
                        if ("priority".Equals(groupby))
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
                        }
                        else
                        {
                            SqlConditions conds1 = new SqlConditions();
                            if ("status".Equals(groupby))
                            {
                                tableName = "we_status";
                                querySQL = "select id_row, statusname, color, Follower, Description from " + tableName + " " +
                                    "where disabled = 0 and id_project_team  = " + int.Parse(query.filter["id_project_team"]) + "";
                                dt_filter_tmp = cnn.CreateDataTable(querySQL);
                            }
                            if ("groupwork".Equals(groupby))
                            {
                                tableName = "we_group";
                                querySQL = "select  id_row, title,'' as color, '' as Follower, '' as Description  from " + tableName + " " +
                                    "where disabled = 0 and id_project_team  = " + int.Parse(query.filter["id_project_team"]) + "";
                                dt_filter_tmp = cnn.CreateDataTable(querySQL);
                                DataRow newRow = dt_filter_tmp.NewRow();
                                newRow[0] = 0;
                                newRow[1] = "Chưa phân loại";
                                dt_filter_tmp.Rows.InsertAt(newRow, 0);
                            }
                            if ("assignee".Equals(groupby))
                            {
                                tableName = "v_wework_clickup_new";
                                querySQL = "select distinct id_nv, hoten, '' as color, '' as Follower, '' as Description from " + tableName + " " +
                                    "where id_project_team  = " + int.Parse(query.filter["id_project_team"]) + "";

                                conds1 = new SqlConditions();
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
                            }
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

                    }
                    DataTable dt_tag = new DataTable();
                    DataTable dt_user = new DataTable();
                    SqlConditions conds = new SqlConditions();

                    string FieldsSelected = "";
                    for (int i = 0; i < dt_Fields.Rows.Count; i++)
                    {
                        if (!(bool)dt_Fields.Rows[i]["isnewfield"])
                        {
                            string fieldname = dt_Fields.Rows[i]["FieldName"].ToString().ToLower();
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
                    }
                    if (!FieldsSelected.Equals(""))
                        FieldsSelected = FieldsSelected.Substring(1);
                    // kiểm tra quyền xem công việc người khác
                    new Common(ConnectionString);
                    bool rs = Common.CheckRoleByProject(query.filter["id_project_team"].ToString(), loginData, cnn, ConnectionString);
                    if (!rs)
                    {
                        long userid = loginData.UserID;
                        strW += @$" and w.id_row in (select id_parent from we_work ww join we_work_user wu on ww.id_row = wu.id_work where ww.disabled = 0 and wu.disabled = 0 and id_parent is not null and id_user = {userid}
 union all select id_row from v_wework_new where id_nv = {userid} or createdby = {userid} )";
                    }

                    string sql = "Select  iIf(id_group is null,0,id_group) as id_group ,work_group, " + FieldsSelected;
                    sql += $@" from v_wework_clickup_new w where w.Disabled = 0 " + strW;
                    DataTable result = new DataTable();
                    result = cnn.CreateDataTable(sql);
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
                    result.Columns.Add("id_nv", typeof(string));
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
                            item["DataChildren"] = dtChildren(item["id_row"].ToString(), result, cnn, dt_Fields, query.filter["id_project_team"], DataAccount, loginData);
                        }
                        DataSet ds = GetWork_ClickUp(cnn, query, loginData.UserID, DataAccount, listDept, strW);
                        if (cnn.LastError != null || ds == null)
                            return JsonResultCommon.Exception(_logger, cnn.LastError, _config, loginData, ControllerContext);
                        dt_data = ds.Tables[0];
                        var tags = ds.Tables[1].AsEnumerable();
                    }
                    int a = tmp.Rows.Count;
                    var data = new
                    {
                        datawork = tmp,
                        TenCot = dt_Fields,
                        Tag = dt_tag,
                        User = dt_user,
                    };
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
                DataAccount = WeworkLiteController.GetAccountFromJeeAccount(HttpContext.Request.Headers, _configuration);
                if (DataAccount == null)
                    return JsonResultCommon.Custom("Lỗi lấy danh sách nhân viên từ hệ thống quản lý tài khoản");

                string error = "";
                string listID = WeworkLiteController.ListAccount(HttpContext.Request.Headers, out error, _configuration);
                if (error != "")
                    return JsonResultCommon.Custom(error);
                #endregion
                string ConnectionString = WeworkLiteController.getConnectionString(ConnectionCache, loginData.CustomerID, _configuration);
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
                                                        //image = WeworkLiteController.genLinkImage(domain, loginData.CustomerID, u["id_nv"].ToString(), _hostingEnvironment.ContentRootPath)
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
                                                                  //image = WeworkLiteController.genLinkImage(domain, loginData.CustomerID, u["id_nv"].ToString(), _hostingEnvironment.ContentRootPath)
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
                //drow["DataStatus"] = WorkClickupController.list_status_user(dr["id_row"].ToString(), id_project_team, loginData, cnn, DataAccount);
                result.Rows.Add(drow);
            }
            if (result.Rows.Count > 0)
            {
                foreach (DataRow dr in result.Rows)
                {
                    dr["Tags"] = cnn.CreateDataTable(queryTag + dr["id_row"]);
                    //DataTable user = cnn.CreateDataTable(queryUser + dr["id_row"]);

                    DataTable user = cnn.CreateDataTable(queryUser + dr["id_row"] + "  and loai = 1");
                    DataTable follower = cnn.CreateDataTable(queryUser + dr["id_row"] + "  and loai = 2");
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
        /// <summary>
        /// get sql từ key
        /// </summary>
        /// <param name="dtKey"></param>
        /// <param name="object_id"></param>
        /// <returns></returns>
        private static string getSqlFromKeys(DataTable dtKey, long object_id)
        {
            string sql = "";
            if (dtKey != null && dtKey.Rows.Count > 0)
            {
                string HRCatalog = JeeWorkConstant.getConfig("JeeWorkConfig:HRCatalog");
                string table = dtKey.Rows[0]["object"].ToString();
                List<string> joins = dtKey.AsEnumerable().Where(x => x["join"] != DBNull.Value).Select(x => x["join"].ToString()).ToList();
                List<string> vals = dtKey.AsEnumerable().Where(x => x["value"] != DBNull.Value).Select(x => x["value"].ToString()).ToList();
                sql = "select " + string.Join(", ", vals) + " from " + table + " " + (joins.Count > 1 ? string.Join(" ", joins) : string.Join(",", joins)) + " where " + table + ".id_row=" + object_id;
                sql = sql.Replace("$DB_Name$", HRCatalog);
            }
            return sql;
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
        public static DataSet GetWork_ClickUp(DpsConnection cnn, QueryParams query, long curUser, List<AccUsernameModel> DataAccount, string listDept, string dieukien_where = "")
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
            /*
                      
iIf(w.Status in (" + list_Complete + @") and w.end_date>w.deadline,1,0) as is_htquahan,
iIf(w.Status  in (" + list_Complete + @") and (w.end_date <= w.deadline or w.end_date is null or w.deadline is null),1,0) as is_htdunghan,
iIf(w.Status not in (" + list_Complete + "," + list_Deadline + @") , 1, 0) as is_danglam, 
iIf(w.Status in (" + list_Deadline + @$") , 1, 0) as is_quahan,
                     */
            if (!string.IsNullOrEmpty(query.sortField) && sortableFields.ContainsKey(query.sortField))
                dieukienSort = sortableFields[query.sortField] + ("desc".Equals(query.sortOrder) ? " desc" : " asc");
            #region Trả dữ liệu về backend để hiển thị lên giao diện
            string sqlq = @$"select Distinct w.*, '' as hoten_nguoigiao
, Iif(fa.id_row is null ,0,1) as favourite 
,coalesce( f.count,0) as num_file,coalesce( com.count,0) as num_com,
 iIf( {sqlhoanthanhquahan} ,1,0) as is_htquahan,
iIf({sqlhoanthanhdunghan} ,1,0) as is_htdunghan,
iIf( {sql_dangthuchien} , 1, 0) as is_danglam, 
iIf( {sql_isquahan} , 1, 0) as is_quahan,
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


    }
}