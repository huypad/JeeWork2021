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

namespace JeeWork_Core2021.Controllers.Wework
{
    [ApiController]
    [Route("api/widgets")]
    [EnableCors("JeeWorkPolicy")]
    /// <summary>
    /// các ds lite dành cho wework
    /// </summary>
    public class WidgetsController : ControllerBase
    {
        private readonly IHostingEnvironment _hostingEnvironment;
        private JeeWorkConfig _config;
        public List<AccUsernameModel> DataAccount;
        private IConnectionCache ConnectionCache;
        private IConfiguration _configuration;

        public WidgetsController(IOptions<JeeWorkConfig> config, IHostingEnvironment hostingEnvironment, IConnectionCache _cache, IConfiguration configuration)
        {
            _hostingEnvironment = hostingEnvironment;
            _config = config.Value;
            ConnectionCache = _cache;
            _configuration = configuration;
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
                string domain = _configuration.GetValue<string>("Host:JeeWork_API");
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
									and project_user.id_user = "+ loginData.UserID+ " " +
                                    "where p.Disabled=0 and de.Disabled = 0 " +
                                    ""+dieukien_where+ " order by " + dieukienSort;
                                    sqlq += @$";select u.*,admin,'' as hoten,'' as username, '' as tenchucdanh
                                            ,'' as mobile,'' as image
                                            from we_project_team_user u 
                                            join we_project_team p on p.id_row=u.id_project_team 
                                            where u.disabled=0 and u.Id_user in (" + listID + " )";
                    DataSet ds = cnn.CreateDataSet(sqlq, Conds);
                    if (cnn.LastError != null || ds == null)
                        return JsonResultCommon.Exception(cnn.LastError, _config, loginData.CustomerID, ControllerContext);
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
                                               //id_nv = u["id_nv"],
                                               hoten = u["hoten"],
                                               username = u["username"],
                                               tenchucdanh = u["tenchucdanh"],
                                               mobile = u["mobile"],
                                               image = u["image"],
                                               favourite = u["favourite"],
                                               //image = WeworkLiteController.genLinkImage(domain, 1119, "16116", _hostingEnvironment.ContentRootPath)
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
                return JsonResultCommon.Exception(ex, _config, loginData.CustomerID);
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
            string Token = Common.GetHeader(Request);
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
                    string domain = _configuration.GetValue<string>("Host:JeeWork_API");
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
                    DateTime from = DateTime.Now;
                    DateTime to = DateTime.Now;
                    if (string.IsNullOrEmpty(query.filter["TuNgay"]) || string.IsNullOrEmpty(query.filter["DenNgay"]))
                        return JsonResultCommon.Custom("Khoảng thời gian không hợp lệ");
                    bool from1 = DateTime.TryParseExact(query.filter["TuNgay"], "dd/MM/yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out from);
                    if (!from1)
                        return JsonResultCommon.Custom("Thời gian bắt đầu không hợp lệ");
                    strW += " and w." + collect_by + ">=@from";
                    cond.Add("from", from);
                    bool to1 = DateTime.TryParseExact(query.filter["DenNgay"], "dd/MM/yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out to);
                    if (!to1)
                        return JsonResultCommon.Custom("Thời gian kết thúc không hợp lệ");
                    strW += " and w." + collect_by + "<@to";
                    cond.Add("to", to);
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
                    sqlq += @";select id_row, id_nv, status, iIf(w.Status in (" + list_Complete + @") and w.end_date>w.deadline,1,0) as is_htquahan,
                                    iIf(w.Status  in (" + list_Complete + @") and (w.end_date <= w.deadline or w.end_date is null or w.deadline is null),1,0) as is_ht,
                                    iIf(w.Status not in (" + list_Complete + "," + list_Deadline + @") , 1, 0) as dangthuchien, 
                                    iIf(w.Status in (" + list_Deadline + @") , 1, 0) as is_quahan
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
                return JsonResultCommon.Exception(ex, _config, loginData.CustomerID);
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
            string Token = Common.GetHeader(Request);
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
                DateTime from = DateTime.Now;
                DateTime to = DateTime.Now;
                if (string.IsNullOrEmpty(query.filter["TuNgay"]) || string.IsNullOrEmpty(query.filter["DenNgay"]))
                    return JsonResultCommon.Custom("Khoảng thời gian không hợp lệ");
                bool from1 = DateTime.TryParseExact(query.filter["TuNgay"], "dd/MM/yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out from);
                if (!from1)
                    return JsonResultCommon.Custom("Thời gian bắt đầu không hợp lệ");
                strW += " and w." + collect_by + ">=@from";
                cond.Add("from", from);
                bool to1 = DateTime.TryParseExact(query.filter["DenNgay"], "dd/MM/yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out to);
                if (!to1)
                    return JsonResultCommon.Custom("Thời gian kết thúc không hợp lệ");
                strW += " and w." + collect_by + "<@to";
                cond.Add("to", to);
                if (!string.IsNullOrEmpty(query.filter["id_projectteam"]))
                {
                    strW += " and id_project_team=@id_projectteam";
                    cond.Add("id_projectteam", query.filter["id_projectteam"]);
                }
                if (!string.IsNullOrEmpty(query.filter["status"]))//1: đang thực hiên(đang làm & phải làm)||2: đã xong
                {
                    strW += " and status=@status";
                    cond.Add("status", query.filter["status"]);
                }
                string displayChild = "0";//hiển thị con: 0-không hiển thị, 1- 1 cấp con, 2- nhiều cấp con
                if (!string.IsNullOrEmpty(query.filter["displayChild"]))
                    displayChild = query.filter["displayChild"];

                string ConnectionString = WeworkLiteController.getConnectionString(ConnectionCache, loginData.CustomerID, _configuration);
                using (DpsConnection cnn = new DpsConnection(ConnectionString))
                {
                    string hoanthanh = "";
                    string quahan = "";
                    string todo = "";
                    if (!string.IsNullOrEmpty(query.filter["id_projectteam"]))
                    {
                        hoanthanh = ReportByProjectController.GetStatusComplete(int.Parse(query.filter["id_projectteam"].ToString()), cnn).ToString();
                        quahan = ReportByProjectController.GetStatusDeadline(int.Parse(query.filter["id_projectteam"].ToString()), cnn).ToString();
                        todo = ReportByProjectController.GetStatusTodo(int.Parse(query.filter["id_projectteam"].ToString()), cnn).ToString();
                    }
                    else
                    {
                        string listDept = WeworkLiteController.getListDepartment_GetData(loginData, cnn, HttpContext.Request.Headers, _configuration,ConnectionString);
                        hoanthanh = ReportController.GetListStatusDynamic(listDept, cnn, " IsFinal "); // IsFinal
                        quahan = ReportController.GetListStatusDynamic(listDept, cnn, "IsDeadline"); // IsDeadline
                        todo = ReportController.GetListStatusDynamic(listDept, cnn, "IsTodo"); //IsTodo
                        strW += $" and id_department in ({listDept})"; 
                    }

                    #region Trả dữ liệu về backend để hiển thị lên giao diện
                    string sqlq = @$"select id_row, id_nv, status,iIf(w.Status in ({hoanthanh}) and w.end_date>w.deadline,1,0) as is_htquahan,
                                    iIf(w.Status in ({hoanthanh}) and (w.end_date <= w.deadline or w.end_date is null or w.deadline is null),1,0) as is_ht,
                                    iIf(w.Status in ({quahan}), 1, 0) as is_quahan, deadline 
                                    from v_wework_clickup_new w where 1=1 " + strW;
                    if (displayChild == "0")
                        sqlq += " and id_parent is null";
                   DataSet ds = cnn.CreateDataSet(sqlq, cond);
                    if (cnn.LastError != null || ds == null)
                    {
                        return JsonResultCommon.Exception(cnn.LastError, _config, loginData.CustomerID, ControllerContext);
                    }
                    DataTable dtW = ds.Tables[0];
                    List<object> data = new List<object>();
                    int ht = (int)dtW.Compute("count(id_row)", "is_ht=1");
                    int htm = (int)dtW.Compute("count(id_row)", " is_htquahan=1 ");
                    int dth = (int)dtW.Compute("count(id_row)", " status not in ( " + hoanthanh + "," + quahan + ")");
                    int qh = (int)dtW.Compute("count(id_row)", " is_quahan=1 ");
                    int khong = (int)dtW.Compute("count(id_row)", " deadline is null ");
                    data.Add(new
                    {
                        trangthai = "Hoàn thành đúng hạn",
                        value = ht
                    });
                    data.Add(new
                    {
                        trangthai = "Hoàn thành muộn",
                        value = htm
                    });
                    data.Add(new
                    {
                        trangthai = "Đang thực hiện",
                        value = dth
                    });
                    data.Add(new
                    {
                        trangthai = "Quá hạn",
                        value = qh
                    });
                    var listValue = new List<int>() { ht, htm, dth, qh };
                    return JsonResultCommon.ThanhCong(new
                    {
                        TrangThaiCongViec = data,
                        DataTrangThaiCongViec = listValue,
                        KhongDungHan = new
                        {
                            pie1 = new List<object>()
                            {
                                new{trangthai = "Hoàn thành",value = (ht+htm)},
                                new {trangthai = "Hoàn thành muộn",value = htm}
                            },
                            pie2 = new List<object>()
                            {
                                new{trangthai = "Đang thực hiện",value = (dth+qh)},
                                new {trangthai = "Quá hạn",value = qh}
                            },
                            khong = khong
                        }
                    });
                    #endregion
                }
            }
            catch (Exception ex)
            {
                return JsonResultCommon.Exception(ex, _config, loginData.CustomerID);
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
                string domain = _configuration.GetValue<string>("Host:JeeWork_API");
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
                    string strW = " and (w.id_nv=@iduser or (w.nguoigiao=@iduser or w.createdby=@iduser) and (w.id_nv is null  or w.id_parent > 0 ))";
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
                    DataSet ds = WorkClickupController.getWork(cnn, query, long.Parse(loginData.UserID.ToString()), DataAccount, strW);
                    if (cnn.LastError != null || ds == null)
                        return JsonResultCommon.Exception(cnn.LastError, _config, loginData.CustomerID, ControllerContext);
                    var temp = WorkClickupController.filterWork(ds.Tables[0].AsEnumerable().Where(x => x["id_parent"] == DBNull.Value), query.filter);//k bao gồm con
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
                    var Children = WorkClickupController.getChild(domain, loginData.CustomerID, "", displayChild, 0, dtNew.CopyToDataTable().AsEnumerable(), tags, DataAccount, ConnectionString);
                    return JsonResultCommon.ThanhCong(Children, pageModel, Visible);
                }
            }
            catch (Exception ex)
            {


                return JsonResultCommon.Exception(ex, _config, loginData.CustomerID);
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
                string domain = _configuration.GetValue<string>("Host:JeeWork_API");
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
                    string strW = $"  and w.id_department in ({listDept}) ";
                    if (!string.IsNullOrEmpty(query.filter["loaicongviec"]))
                    {
                        if (int.Parse(query.filter["loaicongviec"].ToString()) == 1) // công việc tôi được giao
                        {
                            strW += " and (w.id_nv=@iduser or w.id_row in (select distinct id_parent from v_wework_new ww where ww.id_nv=@iduser and id_parent > 0))";
                        }
                        else if (int.Parse(query.filter["loaicongviec"].ToString()) == 2) // công việc tôi tạo
                        {
                            strW += " and ( w.createdby=@iduser and (w.id_nv is null  or w.id_parent > 0 ))";
                        }
                    }
                    if (!string.IsNullOrEmpty(query.filter["timedeadline"]))
                        strW += $"and (w.deadline is not null and deadline >= GETDATE() and deadline =< '{query.filter["timedeadline"]}')";
                    if (!string.IsNullOrEmpty(query.filter["tinhtrang"]))
                    {
                        string tinhtrang = query.filter["tinhtrang"];
                        string hoanthanh = ReportController.GetListStatusDynamic(listDept, cnn, " IsFinal "); // IsFinal
                        string quahan = ReportController.GetListStatusDynamic(listDept, cnn, "IsDeadline"); // IsDeadline
                        string todo = ReportController.GetListStatusDynamic(listDept, cnn, "IsTodo"); //IsTodo
                        if (tinhtrang == "todo")
                        {
                            strW += $" and w.status not in ({quahan},{hoanthanh}) ";
                        }
                        else if(tinhtrang == "deadline")
                        {
                            strW += $" and w.status in ({quahan}) ";
                        }
                        else {
                            strW += $" and w.status in ({hoanthanh}) ";
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
                    DataSet ds = WorkClickupController.getWork(cnn, query, long.Parse(loginData.UserID.ToString()), DataAccount, strW);
                    if (cnn.LastError != null || ds == null)
                        return JsonResultCommon.Exception(cnn.LastError, _config, loginData.CustomerID, ControllerContext);
                    var temp = WorkClickupController.filterWork(ds.Tables[0].AsEnumerable().Where(x => x["id_parent"] == DBNull.Value), query.filter);//k bao gồm con
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
                    var Children = WorkClickupController.getChild(domain, loginData.CustomerID, "Id_NV", displayChild, loginData.UserID, dtNew.CopyToDataTable().AsEnumerable(), tags, DataAccount, ConnectionString);
                    return JsonResultCommon.ThanhCong(Children, pageModel, Visible);
                }
            }
            catch (Exception ex)
            {


                return JsonResultCommon.Exception(ex, _config, loginData.CustomerID);
            }
        }
        /// <summary>
        /// Lấy link image
        /// </summary>
        /// <param name="domain"></param>
        /// <param name="idKH"></param>
        /// <param name="id_nv"></param>
        /// <param name="contentRootPath"></param>
        /// <returns></returns>
        public static string genLinkImage(string domain, long idKH, string id_nv, string contentRootPath)
        {
            //string Image = domain + "dulieu/Images/Noimage.jpg";
            string Image = "";
            string str = "dulieu/images/nhanvien/" + idKH + "/" + id_nv + ".jpg";
            string path = Path.Combine(contentRootPath, str);
            if (System.IO.File.Exists(path))
            {
                Image = domain + str;
            }
            return Image;
        }
        /// <summary>
        /// Get link chỗ lưu file
        /// </summary>
        /// <param name="domain"></param>
        /// <param name="path"></param>
        /// <returns></returns>
        public static string genLinkAttachment(string domain, object path)
        {
            if (path == null)
                return "";
            return domain + JeeWorkConstant.RootUpload + path;
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
        /// Notify mail
        /// </summary>
        /// <param name="id_template">we_template.id_row</param>
        /// <param name="object_id"></param>
        /// <param name="nguoigui"></param>
        /// <param name="dtUser">gồm id_nv, hoten, email</param>
        /// <returns></returns>
        public static bool NotifyMail(int id_template, long object_id, UserJWT nguoigui, DataTable dtUser, string ConnectionString, DataTable dtOld = null)
        {
            using (DpsConnection cnn = new DpsConnection(ConnectionString))
            {
                //get template
                string sql = "select * from we_template where id_row=" + id_template;
                DataTable dt = cnn.CreateDataTable(sql);
                bool exclude_sender = (bool)dt.Rows[0]["exclude_sender"];//loại bỏ người gửi khỏi ds người nhận
                string link = JeeWorkConstant.LinkWework + dt.Rows[0]["link"].ToString().Replace("$id$", object_id.ToString());
                string title = dt.Rows[0]["title"].ToString();
                string template = dt.Rows[0]["template"].ToString();
                title = title.Replace("$nguoigui$", nguoigui.Username);
                template = template.Replace("$nguoigui$", nguoigui.Username);
                template = template.Replace("$link$", link);
                //get key_value replace
                sql = "select * from we_template_key where id_key in (" + dt.Rows[0]["keys"] + ") order by id_key";
                DataTable dtKey = cnn.CreateDataTable(sql);
                //get data replace
                string sqlq = getSqlFromKeys(dtKey, object_id);
                if ("$data_account$".Equals(sqlq))
                {
                    sqlq = sqlq.Replace("$data_account$", "");
                }
                DataTable dtFind = cnn.CreateDataTable(sqlq);
                if (cnn.LastError != null)
                    return false;
                #region Xử lý khi gửi file đính kèm thảo luận qua Email (id_template = 16 - Thảo luận)
                //if (id_template == 16)
                //{
                //    // Xử lý cho trường hợp gửi link tải file qua email
                //    if (dtFind.Rows.Count > 0)
                //    {
                //        string list_file = "";
                //        for (int i = 0; i < dtFind.Rows.Count; i++)
                //        {
                //            list_file = dtFind.Rows[i]["filename"].ToString();
                //            if (!string.IsNullOrEmpty(list_file))
                //            {
                //                list_file += "\n " + list_file;
                //                dtFind.Rows[i]["path"] = JeeWorkConstant.LinkWework + dtFind.Rows[0]["path"].ToString();
                //            }
                //            //list_file += "\n " + JeeWorkConstant.LinkWework + "" + list_file;
                //        }
                //        dtFind.Rows[0]["filename"] = list_file;
                //    }
                //}
                #endregion
                DataRow values = dtFind.Rows[0];
                DataRow old_values = dtOld == null ? null : dtOld.Rows[0];
                foreach (DataRow dr in dtKey.Rows)
                {
                    string f = "";
                    if (dr["format"] != DBNull.Value)
                        f = "{0:" + dr["format"].ToString() + "}";
                    string key = dr["key"].ToString();
                    string val = dr["value"].ToString();
                    var temp = val.Split(new string[] { " as " }, StringSplitOptions.None);
                    val = temp[temp.Length - 1];
                    if (!(bool)dr["is_old"])
                    {
                        if (!string.IsNullOrEmpty(f))
                            val = string.Format(f, values[val]);
                        else
                            val = values[val].ToString();
                    }
                    else
                    {//dữ liệu cũ
                        if (old_values != null)
                        {
                            if (!string.IsNullOrEmpty(f))
                                val = string.Format(f, old_values[val]);
                            else
                                val = old_values[val].ToString();
                        }
                    }
                    title = title.Replace(key, val);
                    template = template.Replace(key, val);
                }

                // #update guimail
                string HRConnectionString = JeeWorkConstant.getHRCnn();
                DpsConnection cnnHR = new DpsConnection(HRConnectionString);
                MailInfo MInfo = new MailInfo(nguoigui.CustomerID.ToString(), cnnHR);
                cnn.Disconnect();
                if (MInfo.Email != null)
                {
                    for (int i = 0; i < dtUser.Rows.Count; i++)
                    {
                        //Gửi mail cho người nhận
                        if (!"".Equals(dtUser.Rows[i]["email"].ToString()))
                        {
                            if (exclude_sender && dtUser.Rows[i]["id_nv"].ToString() == nguoigui.UserID.ToString())
                                continue;
                            string contents = template.Replace("$nguoinhan$", dtUser.Rows[i]["hoten"].ToString());
                            string ErrorMessage = "";
                            SendMail.Send_Synchronized(dtUser.Rows[i]["email"].ToString(), title, new MailAddressCollection(), contents, nguoigui.CustomerID.ToString(), "", true, out ErrorMessage, MInfo, ConnectionString);
                        }
                    }
                }
            }
            return true;
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
                string domain = _configuration.GetValue<string>("Host:JeeWork_API");
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
                    DateTime from = DateTime.Now;
                    DateTime to = DateTime.Now;
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
                    DataTable dt_Fields = WeworkLiteController.GetListField(int.Parse(query.filter["id_project_team"]), ConnectionString);
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
                    string sql = "Select  iIf(id_group is null,0,id_group) as id_group ,work_group, " + FieldsSelected;
                    sql += $@" from v_wework_clickup_new w where w.Disabled = 0 " + strW;
                    DataTable result = new DataTable();
                    result = cnn.CreateDataTable(sql);
                    DataTable dt_comments = cnn.CreateDataTable("select id_row, object_type, object_id, comment, id_parent, Disabled " +
                       "from we_comment where disabled = 0 and object_type = 1");
                    string queryTag = @"select a.id_row,a.title,a.color,b.id_work from we_tag a join we_work_tag b on a.id_row=b.id_tag 
                                                    where a.disabled=0 and b.disabled = 0 and a.id_project_team = " + query.filter["id_project_team"] + "and id_work = ";
                    string queryUser = $@"select w_user.id_work, w_user.id_user, w_user.loai, id_child, w_user.Disabled, '' as hoten, id_project_team
                                                    from we_work_user w_user join we_work on we_work.id_row = w_user.id_work 
                                                    where w_user.Disabled = 0 and we_work.id_project_team = " + query.filter["id_project_team"] + "and id_work = ";
                    result.Columns.Add("Tags", typeof(DataTable));
                    result.Columns.Add("User", typeof(DataTable));
                    result.Columns.Add("id_nv", typeof(string));
                    if (result.Rows.Count > 0)
                    {
                        foreach (DataRow dr in result.Rows)
                        {
                            dr["comments"] = dt_comments.Compute("count(id_row)", "object_id =" + dr["id_row"].ToString() + "").ToString();
                            dr["Tags"] = cnn.CreateDataTable(queryTag + dr["id_row"]);
                            DataTable dt_u = cnn.CreateDataTable(queryUser + dr["id_row"]);
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
                            item["DataChildren"] = WorkClickupController.dtChildren(item["id_row"].ToString(), result, cnn, dt_Fields, query.filter["id_project_team"], DataAccount);
                        }
                        DataSet ds = WorkClickupController.GetWork_ClickUp(cnn, query, loginData.UserID, DataAccount, listDept, strW);
                        if (cnn.LastError != null || ds == null)
                            return JsonResultCommon.Exception(cnn.LastError, _config, loginData.CustomerID, ControllerContext);
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
                    tmp = tmp.AsEnumerable().Skip((query.page - 1) * query.record).Take(query.record).CopyToDataTable();
                    //chỉ lấy datawork
                    return JsonResultCommon.ThanhCong(tmp, pageModel, Visible);
                }
            }
            catch (Exception ex)
            {
                return JsonResultCommon.Exception(ex, _config, loginData.CustomerID);
            }
        }

        [Route("List-activities")]
        [HttpGet]
        public object ListActivities([FromQuery] QueryParams query)
        {
            string Token = Common.GetHeader(Request);
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
                bool Visible = Common.CheckRoleByToken(loginData.UserID.ToString(), "3502", ConnectionString, DataAccount);
                string domain = _configuration.GetValue<string>("Host:JeeWork_API");
                using (DpsConnection cnn = new DpsConnection(ConnectionString))
                {
                    string sql = @"select distinct p.id_row, p.title, is_project from we_project_team p
join we_department d on d.id_row = p.id_department
join we_project_team_user u on u.id_project_team = p.id_row
 where u.Disabled = 0 and id_user = " + loginData.UserID + " and p.Disabled = 0  and d.Disabled = 0 and IdKH=" + loginData.CustomerID + " (where) order by title";

                    SqlConditions Conds = new SqlConditions();
                    string dieukienSort = "id_row";
                    if (!string.IsNullOrEmpty(query.filter["id_project_team"]))
                    {
                        Conds.Add("id_project_team", query.filter["id_project_team"]);
                        sql = sql.Replace("(where)", " and p.id_row = "+ query.filter["id_project_team"] );
                    }    
                    else
                    {
                        Conds.Add("id_project_team", 0);
                        sql = sql.Replace("(where)", " ");
                    }
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
                    sqlq = @$"exec GetActivitiesNew '{listID}',@id_project_team";
                    //}
                    DataSet ds = cnn.CreateDataSet(sqlq, Conds);
                    #region Map info account từ JeeAccount
                    ds.Tables[0].Columns.Add("hoten", typeof(string));
                    ds.Tables[0].Columns.Add("username", typeof(string));
                    ds.Tables[0].Columns.Add("tenchucdanh", typeof(string));
                    ds.Tables[0].Columns.Add("mobile", typeof(string));
                    ds.Tables[0].Columns.Add("image", typeof(string));
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
                        return JsonResultCommon.Exception(cnn.LastError, _config, loginData.CustomerID, ControllerContext);
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
                                return JsonResultCommon.Exception(cnn.LastError, _config, loginData.CustomerID, ControllerContext);
                            dr["oldvalue"] = dt_temp.AsEnumerable().Where(x => x[0].ToString() == dr["oldvalue"].ToString()).Select(x => x[1]).FirstOrDefault();
                            dr["newvalue"] = dt_temp.AsEnumerable().Where(x => x[0].ToString() == dr["newvalue"].ToString()).Select(x => x[1]).FirstOrDefault();
                        }
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
                                         where t["id_row"].ToString()==g.Key.e.ToString()
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
                return JsonResultCommon.Exception(ex, _config, loginData.CustomerID);
            }
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
        public static void mailthongbao(long id, List<long> users, int id_template, UserJWT loginData, string ConnectionString, DataTable dtOld = null)
        {
            if (users == null || users.Count == 0)
                return;
            using (DpsConnection cnn = new DpsConnection(ConnectionString))
            {
                List<AccUsernameModel> DataAccount = new List<AccUsernameModel>();
                DataTable dtUser = new DataTable();
                dtUser.Columns.Add("id_nv");
                dtUser.Columns.Add("hoten");
                dtUser.Columns.Add("email");
                foreach (var item in users)
                {
                    var info = DataAccount.Where(x => item.ToString().Contains(x.UserId.ToString())).FirstOrDefault();

                    if (info != null)
                    {
                        dtUser.Rows.Add(info.UserId, info.FullName, info.Email);
                    }
                }
                NotifyMail(id_template, id, loginData, dtUser, ConnectionString, dtOld);
            }
        }
        /// <summary>
        /// Lấy danh sách cột hiển thị ứng với từng project
        /// </summary>
        /// <param name="id_project_team"></param>
        /// <returns></returns>
        //public static DataTable GetListField(long id_project_team)
        //{
        //    using (DpsConnection cnn = new DpsConnection(JeeWorkConstant.getConfig("JeeWorkConfig:ConnectionString")))
        //    {
        //        SqlConditions cond = new SqlConditions();
        //        cond.Add("1", 1);
        //        //cond.Add("id_project_team", id_project_team);
        //        //string select = "select id_project_team, fieldname, disabled, objectid, position " +
        //        //    "from we_fields_project_team where (where) order by position";
        //        string select = "";
        //        select = "select id_project_team, fieldname, disabled, objectid, position " +
        //            "from we_fields_project_team where (where) order by position";
        //        //select = " select we_fields.*, id_project_team " +
        //        //    "from we_fields left join we_fields_project_team " +
        //        //    "on we_fields.FieldName = we_fields_project_team.fieldname and Disabled = 0 " +
        //        //    "where (where) and (id_project_team is null or id_project_team = " + id_project_team + ") order by position ";
        //        DataTable dt_field = cnn.CreateDataTable(select,"(where)", cond);
        //        DataTable dt = new DataTable();
        //        if (dt_field.Rows.Count <= 0)
        //        {
        //            cond = new SqlConditions();
        //            cond.Add("IsVisible", 0);
        //            cond.Add("isbatbuoc", 1);
        //            select = " select we_fields.*, " + id_project_team + " as id_project_team, type " +
        //                    "from we_fields " +
        //                    "where (where) order by position ";
        //        }
        //        if (id_project_team == 0)
        //        {
        //            dt = cnn.CreateDataTable("select fieldname, title, isvisible, note, type, position, isbatbuoc, isnewfield, IsDefault, 0 as id_project_team " +
        //                "from we_fields where IsDefault = 1");
        //        }
        //        //select = " select we_fields.*, id_project_team " +
        //        //    "from we_fields left join we_fields_project_team " +
        //        //    "on we_fields.FieldName = we_fields_project_team.fieldname " +
        //        //    "where (where) and (id_project_team is null or id_project_team = "+id_project_team +") order by position ";
        //        dt = cnn.CreateDataTable(select, "(where)", cond);
        //        DataTable dt_field_project = cnn.CreateDataTable("select id_project_team, fieldname, ObjectID, position " +
        //            "from we_fields_project_team " +
        //            "where id_project_team = " + id_project_team);
        //        if (dt_field_project.Rows.Count > 0)
        //        {
        //            foreach (DataRow row in dt.Rows)
        //            {
        //                DataRow[] dr = dt_field_project.Select("fieldname='" + row["fieldname"] + "'");
        //                if (dr.Length > 0)
        //                {
        //                    row["id_project_team"] = dr[0]["id_project_team"];
        //                }
        //            }
        //        }

        //        //if (dt.Rows.Count == 0) // Trường hợp chưa chọn cột thì load mặc định cho người dùng
        //        //{
        //        //    cond = new SqlConditions();
        //        //    cond.Add("isbatbuoc", 1);
        //        //    cond.Add("IsNewField", 0);
        //        //    select = "select fieldname, title, isvisible, type, position, isbatbuoc, isnewfield " +
        //        //        "from we_fields where (where) order by position";
        //        //    dt = cnn.CreateDataTable(select, "(where)", cond);
        //        //}
        //        cnn.Disconnect();
        //        return dt;
        //    }
        //}
        /// <summary>
        /// Lấy danh sách cột hiển thị ứng với từng project
        /// </summary>
        /// <param name="id_project_team"></param>
        /// <returns></returns>
        public static DataTable GetListField(long id_project_team, string ConnectionString)
        {
            using (DpsConnection cnn = new DpsConnection(ConnectionString))
            {
                SqlConditions cond = new SqlConditions();
                cond.Add("1", 1);
                string select = "";
                select = "select id_row, id_project_team, fieldname, disabled, objectid, position, Options " +
                    "from we_fields_project_team where (where) order by IsNewField, position";
                DataTable dt_field = cnn.CreateDataTable(select, "(where)", cond);
                DataTable dt = new DataTable();
                if (dt_field.Rows.Count <= 0 || id_project_team == 0) // Dự án chưa chọn field
                {
                    cond = new SqlConditions();
                    cond.Add("IsVisible", 0);
                    cond.Add("IsDefault", 1);
                    select = " select we_fields.*, " + id_project_team + " as id_project_team, type, '' as Title_NewField, ''as id_row, 0 as IsHidden " +
                            "from we_fields " +
                            "where (where) order by isNewField, position";
                    dt = cnn.CreateDataTable(select, "(where)", cond);
                }
                else
                {
                    cond = new SqlConditions();
                    cond.Add("Disabled", 0);
                    select = $@"select we_fields_project_team.id_row, we_fields.fieldname, we_fields.title, IsHidden
                                            ,we_fields_project_team.Title as Title_NewField, we_fields.isnewfield
                                            ,type, TypeID, id_project_team, IsDefault, we_fields_project_team.position
                                             from we_fields left join we_fields_project_team
                                             on we_fields.FieldName = we_fields_project_team.fieldname 
                                            and id_project_team = " + id_project_team + " " +
                                            "where (where) and id_project_team = " + id_project_team + " or id_project_team is null " +
                                            "order by we_fields.isNewField, we_fields_project_team.position";
                    dt = cnn.CreateDataTable(select, "(where)", cond);
                }
                cnn.Disconnect();
                return dt;
            }
        }
        public static bool CheckRole(long role, string user, long id_project, string ConnectionString)
        {
            BaseModel<object> model = new BaseModel<object>();
            PageModel pageModel = new PageModel();
            ErrorModel error = new ErrorModel();
            DataTable dt_role = new DataTable();
            DataTable dt_checkuser = new DataTable();
            string sqlq = "";
            SqlConditions cond = new SqlConditions();
            try
            {
                using (DpsConnection cnn = new DpsConnection(ConnectionString))
                {
                    cond.Add("id_project_team", id_project);
                    cond.Add("id_user", user);
                    cond.Add("admin", 1);
                    cond.Add("disabled", 0);
                    string sql_user = "";
                    #region Check user admin trong project trước
                    sql_user = "select * from we_project_team_user where (where)";
                    dt_checkuser = cnn.CreateDataTable(sql_user, "(where)", cond);
                    if (dt_checkuser.Rows.Count > 0) // thuộc project, có trong dự án và là admin
                        return true; // Đối với admin mặc định là có quyền
                    #endregion
                    #region Check user thành viên trong project
                    else
                    {
                        cond.Remove(cond["admin"]);
                        dt_checkuser = cnn.CreateDataTable(sql_user, "(where)", cond);
                        if (dt_checkuser.Rows.Count > 0) // có user trong dự án và là thành viên
                        {
                            #region Check các quyền của project
                            cond.Remove(cond["id_user"]);
                            cond.Remove(cond["disabled"]);
                            cond.Add("id_role", role);
                            cond.Add("member", 1);
                            sqlq = "select id_row, id_project_team, id_role, admin, member, customer from we_project_role where (where)";
                            dt_role = cnn.CreateDataTable(sqlq, "(where)", cond);
                            if (dt_role.Rows.Count > 0)
                                return true;
                            else
                                return false;
                            #endregion
                        }
                        #endregion
                        else // User không có trong dự án đó
                            return false;
                    }
                }
            }
            catch (Exception ex)
            {
                return false;
            }
        }
        public static bool CheckNotify_ByConditions(long id_project, string key, bool IsProject, string ConnectionString)
        {
            DataTable dt_Key = new DataTable();
            string sqlq = "";
            SqlConditions cond = new SqlConditions();
            try
            {
                using (DpsConnection cnn = new DpsConnection(ConnectionString))
                {
                    cond.Add("id_project_team", id_project);
                    cond.Add("disabled", 0);
                    //Kiểm tra trạng thái dừng nhắc nhở trước
                    sqlq = "select stop_reminder from we_project_team where (where)";
                    dt_Key = cnn.CreateDataTable(sqlq, "(where)", cond);
                    if (dt_Key.Rows.Count > 0)
                    {
                        if ((bool)dt_Key.Rows[0][0])
                            return true;
                        else // Nếu không dừng nhắc nhở => Kiểm tra những điều kiện nào được nhắc.
                        {
                            sqlq = "select " + key.ToLower() + " as Key_Email from we_project_team where (where)";
                            dt_Key = cnn.CreateDataTable(sqlq, "(where)", cond);
                            if (dt_Key.Rows.Count > 0)
                            {
                                if ((bool)dt_Key.Rows[0][0])
                                    return true;
                            }
                            return false;
                        }
                    }
                    return false;
                }
            }
            catch (Exception ex)
            {
                return false;
            }
        }
        public static DataTable StatusDynamic(long id_project, List<AccUsernameModel> DataAccount, string ConnectionString)
        {
            DataTable dt = new DataTable();
            using (DpsConnection cnn = new DpsConnection(ConnectionString))
            {
                string query = "";
                query = $@"select id_row, StatusName, description, id_project_team,IsToDo
,Type, IsDefault, color, Position, IsFinal, Follower, IsDeadline, '' as hoten_Follower
from we_status 
where Disabled = 0";
                if (id_project > 0)
                    query += " and id_project_team =" + id_project + "";
                query += " order by IsFinal,id_row";
                dt = cnn.CreateDataTable(query);
            }

            #region Map info account từ JeeAccount

            foreach (DataRow item in dt.Rows)
            {
                var info = DataAccount.Where(x => item["Follower"].ToString().Contains(x.UserId.ToString())).FirstOrDefault();

                if (info != null)
                {
                    item["hoten_Follower"] = info.FullName;
                }
            }

            #endregion
            return dt;
        }
        public static bool Init_RoleDefault(long projectid, List<long> list_roles, string ConnectionString)
        {
            SqlConditions cond = new SqlConditions();
            DataTable dt = new DataTable();
            using (DpsConnection cnn = new DpsConnection(ConnectionString))
            {
                string role = "";
                for (int i = 0; i < list_roles.Count; i++)
                {
                    Hashtable has = new Hashtable();
                    has.Add("id_project_team", projectid);
                    has.Add("id_role", list_roles[i].ToString());
                    has.Add("admin", 0);
                    has.Add("member", 1);
                    has.Add("customer", 0);
                    DataTable dt_role = new DataTable();
                    role = "select * " +
                        "from we_project_role where id_project_team = " + projectid + " " +
                        "and member = 1 and id_role = " + list_roles[i].ToString() + "";
                    dt_role = cnn.CreateDataTable(role);
                    if (dt_role.Rows.Count <= 0)
                    {
                        cnn.BeginTransaction();
                        if (cnn.Insert(has, "we_project_role") != 1)
                        {
                            cnn.RollbackTransaction();
                            return false;
                        }
                    }
                    cnn.EndTransaction();
                    return true;
                }
                return true;
            }
        }
        /// <summary>
        /// Khởi tạo View cho project
        /// </summary>
        /// <param name="id_project"></param>
        /// <param name="conn"></param>
        /// <returns></returns>
        public static bool Init_DefaultView_Project(long id_project, DpsConnection conn)
        {
            SqlConditions cond = new SqlConditions();
            DataTable dt = new DataTable();

            string sqlq = ""; string sql_cmd = "";
            SqlConditions conds = new SqlConditions();
            Hashtable has = new Hashtable();
            conds.Add("disabled", 0);
            conds.Add("id_project_team", id_project);
            sqlq = "select id_row, id_project_team, viewid, view_name_new, default_everyone, default_for_me, " +
                "pin_view, personal_view, favourite, disabled, updateddate, updatedby, id_department " +
                "from we_projects_view where (where)";
            dt = conn.CreateDataTable(sqlq, "(where)", conds);
            if (dt.Rows.Count <= 0)
            {
                sql_cmd = "select id_department " +
                            "from we_project_team " +
                            "where disabled = 0 and id_row = " + id_project + "";
                long department = long.Parse(conn.ExecuteScalar(sql_cmd).ToString());

                if (department > 0)
                {
                    sqlq = "select id_department, view_de.is_default, viewid, view_name, _view.is_default as view_default " +
                        "from we_department_view view_de " +
                        "join we_default_views _view on _view.id_row = view_de.viewid " +
                        "where id_department = " + department + " and view_de.disabled = 0 ";
                    dt = conn.CreateDataTable(sqlq);
                    if (dt.Rows.Count > 0) // nếu department đã có default_views
                    {
                        foreach (DataRow item in dt.Rows)
                        {
                            has = new Hashtable();
                            has.Add("id_project_team", id_project);
                            has.Add("viewid", item["viewid"].ToString());
                            has.Add("view_name_new", item["view_name"].ToString());
                            has.Add("id_department", department);
                            has.Add("default_view", item["is_default"].ToString());
                            has.Add("createddate", DateTime.Now);
                            has.Add("createdby", 0);
                            conn.BeginTransaction();
                            if (conn.Insert(has, "we_projects_view") != 1)
                            {
                                conn.RollbackTransaction();
                                return false;
                            }
                        }
                    }
                    else // Nếu project không tham chiếu từ phòng ban
                    {
                        sqlq = "select id_row, view_name, description, is_default, icon from we_default_views";
                        dt = conn.CreateDataTable(sqlq);
                        foreach (DataRow item in dt.Rows)
                        {
                            has = new Hashtable();
                            has.Add("id_project_team", id_project);
                            has.Add("viewid", item["id_row"].ToString());
                            has.Add("view_name_new", item["view_name"].ToString());
                            has.Add("default_view", item["is_default"].ToString());
                            has.Add("id_department", 0);
                            has.Add("createddate", DateTime.Now);
                            has.Add("createdby", 0);
                            conn.BeginTransaction();
                            if (conn.Insert(has, "we_projects_view") != 1)
                            {
                                conn.RollbackTransaction();
                                return false;
                            }
                        }
                    }
                    conn.EndTransaction();
                }
            }
            return true;

        }
        /// <summary>
        /// Khởi tạo cột mặc định cho project
        /// </summary>
        /// <param name="id_project"></param>
        /// <param name="conn"></param>
        /// <returns></returns>
        public static bool Init_Column_Project(long id_project, DpsConnection conn)
        {
            SqlConditions cond = new SqlConditions();
            DataTable dt = new DataTable();

            string sqlq = "";
            SqlConditions conds = new SqlConditions();
            Hashtable has = new Hashtable();
            conds.Add("disabled", 0);
            conds.Add("id_project_team", id_project);
            sqlq = "select * from we_fields_project_team where (where)";
            dt = conn.CreateDataTable(sqlq, "(where)", conds);
            if (dt.Rows.Count <= 0)
            {
                sqlq = "select fieldname, title, position, isNewField from we_fields where IsDefault = 1";
                dt = conn.CreateDataTable(sqlq);
                if (dt.Rows.Count > 0)
                {
                    foreach (DataRow item in dt.Rows)
                    {
                        has = new Hashtable();
                        has.Add("id_project_team", id_project);
                        has.Add("fieldname", item["fieldname"].ToString());
                        has.Add("Title", item["title"].ToString());
                        has.Add("Disabled", 0);
                        has.Add("position", item["position"].ToString());
                        has.Add("createddate", DateTime.Now);
                        has.Add("createdby", 0);
                        has.Add("IsNewField", 0);
                        if (conn.Insert(has, "we_fields_project_team") != 1)
                        {
                            conn.RollbackTransaction();
                            return false;
                        }
                    }
                }
                conn.EndTransaction();
            }
            return true;

        }
        /// <summary>
        /// Lấy danh sách view theo project
        /// </summary>
        /// <param name="id_project_team"></param>
        /// <param name="isnewfield"></param>
        /// <returns></returns>
        /// <summary>
        /// 
        /// </summary>
        /// <param name="TableName">Bảng lấy dữ liệu</param>
        /// <param name="objectID">khóa chính của bảng lấy dữ liệu</param>
        /// <param name="ColumnName">Tên cột khóa chính</param>
        /// <param name="ColumnDateTime">Cột cần xét (Datetime)</param>
        /// <param name="Deleted">Bảng đó có cột Xóa thì Deleted = true</param>
        /// <param name="cnn">Chuỗi kết nối</param>
        /// <returns></returns>
        public static DateTime Check_ConditionDate(string TableName, string ColumnDateTime, long id, out string ngaykiemtra, DpsConnection cnn)
        {
            DateTime date = new DateTime();
            ngaykiemtra = date.ToString();
            string where = " where ";
            string sqlq = "";
            sqlq = "select " + ColumnDateTime + " from " + TableName + "";
            where += "id_row = " + id + " and Disabled = 0";
            var Value = cnn.ExecuteScalar(sqlq + where);
            if (Value != null)
                date = (DateTime)Value;
            ngaykiemtra = date.ToString();
            return date;
        }
        public static bool ConvertToDateTime(out DateTime kq, string ngay_ddMMyyy, bool isUTC = false)
        {
            string[] formats = new string[] { "dd/MM/yyyy HH:mm:ss", "dd/MM/yyyy HH:mm", "dd/MM/yyyy HH", "dd/MM/yyyy", "yyyy/MM/dd", "yyyy/M/dd", "yyyy/M/d", "yyyy/MM/d", "yyyy-MM-dd'T'HH:mm:ss'.'fff'Z'", "O", "yyyy-MM-dd'T'HH:mm:ssZ", "yyyy-MM-dd'T'HH:mm:ss", "d/MM/yyyy", "d/M/yyyy", "dd/M/yyyy", "yyyy", "HH:mm", "MM/yyyy", "HH:mm:ss", "M/d/yyyy h:mm:ss tt" };
            kq = new DateTime();

            if (isUTC)
            {
                return DateTime.TryParseExact(ngay_ddMMyyy, formats, null, DateTimeStyles.AssumeUniversal, out kq);
            }
            else
                return DateTime.TryParseExact(ngay_ddMMyyy, formats, null, DateTimeStyles.None, out kq);
        }
        /// <summary>
        /// Insert người vào công việc
        /// </summary>
        /// <param name="WorkID"></param>
        /// <param name="statusID"></param>
        /// <returns></returns>
        public static bool ProcessWork(long WorkID, long StatusID, UserJWT data, string ConnectionString)
        {
            SqlConditions cond = new SqlConditions();
            DataTable dt = new DataTable();
            using (DpsConnection cnn = new DpsConnection(ConnectionString))
            {
                string sqlq = "";
                cond.Add("WorkID", WorkID);
                cond.Add("StatusID", StatusID);
                sqlq = "select id_project_team, WorkID, StatusID, Checker, CheckedDate, CheckNote from We_Work_Process where (where)";
                dt = cnn.CreateDataTable(sqlq, "(where)", cond);
                if (cnn.LastError != null || dt.Rows.Count == 0)
                    return false;

                if (!string.IsNullOrEmpty(dt.Rows[0]["Checker"].ToString()))
                {
                    string sql_user = "select * from we_work_user where id_work = " + WorkID + " " +
                        "and Disabled = 0 and id_user = " + dt.Rows[0]["Checker"].ToString() + " " +
                        "and loai = 2";
                    if (cnn.ExecuteScalar(sql_user) == null)
                    {
                        Hashtable val = new Hashtable();
                        val.Add("id_work", WorkID);
                        val.Add("id_user", dt.Rows[0]["Checker"].ToString());
                        val.Add("CreatedDate", DateTime.Now);
                        val.Add("CreatedBy", data.UserID);
                        val.Add("loai", 1);
                        if (cnn.Insert(val, "we_work_user") != 1)
                        {
                            return false;
                        }
                        var users = new List<long> { long.Parse(dt.Rows[0]["Checker"].ToString()) };
                        mailthongbao(WorkID, users, 10, data, ConnectionString);
                        return true;
                    }
                    return true;
                }
                return true;
            }
        }
        /// <summary>
        /// Gửi mail từng giai đoạn của công việc
        /// </summary>
        /// <param name="id_template">we_template.id_row</param>
        /// <param name="object_id"></param>
        /// <param name="nguoigui"></param>
        /// <param name="dtUser">gồm id_nv, hoten, email</param>
        /// <returns></returns>
        public static bool SendMail_WorkProcess(long id_work, long status_current)
        {
            //SqlConditions conds = new SqlConditions();
            //conds.Add("id_row", id_work);
            //conds.Add("status", status_current);

            //var users = new List<long> { long.Parse(row["id_user"].ToString()) };
            //WeworkLiteController.mailthongbao(int.Parse(row["id_work"].ToString()), users, 18, loginData);
            return true;
        }
    }
}