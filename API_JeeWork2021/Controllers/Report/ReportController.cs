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
using static JeeWork_Core2021.Controllers.Wework.ReportController.Simulate;
using Microsoft.AspNetCore.Http;
using DPSinfra.ConnectionCache;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.IO;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Spreadsheet;

namespace JeeWork_Core2021.Controllers.Wework
{
    [ApiController]
    [Route("api/report")]
    [EnableCors("JeeWorkPolicy")]
    /// <summary>
    /// api quản lý we_authorize
    /// </summary>
    // [EnableCors(origins: "*", headers: "*", methods: "*")]
    public class ReportController : ControllerBase
    {
        private readonly IHostingEnvironment _hostingEnvironment;
        public static JeeWorkConfig _config;
        public static string excel_member;
        public static string excel_project;
        public static string excel_department;
        public List<AccUsernameModel> DataAccount;
        private IConnectionCache ConnectionCache;
        private IConfiguration _configuration;
        private readonly ILogger<ReportController> _logger;
        string sqltrehan = " and w.deadline < getdate() and w.deadline is not null and w.end_date is null ";
        string sqldanglam = " and (deadline >= getdate() and deadline is not null) or deadline is null) and w.end_date is null";
        string sqlhoanthanh = " and w.end_date is not null";

        public ReportController(IOptions<JeeWorkConfig> config, IHostingEnvironment hostingEnvironment, IConnectionCache _cache, IConfiguration configuration, ILogger<ReportController> logger)
        {
            _hostingEnvironment = hostingEnvironment;
            _config = config.Value;
            ConnectionCache = _cache;
            _configuration = configuration;
            _logger = logger;
        }

        /// <summary>
        /// Overview
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [Route("overview")]
        [HttpGet]
        public object Overview([FromQuery] QueryParams query)
        {
            string Token = Common.GetHeader(Request);
            UserJWT loginData = Ulities.GetUserByHeader(HttpContext.Request.Headers);
            if (loginData == null)
                return JsonResultCommon.DangNhap();
            try
            {
                if (query == null)
                    query = new QueryParams();
                string listDept = "";
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

                    listDept = WeworkLiteController.getListDepartment_GetData(loginData, cnn, HttpContext.Request.Headers, _configuration, ConnectionString);
                    Dictionary<string, string> collect = new Dictionary<string, string>
                        {
                            { "CreatedDate", "CreatedDate"},
                            { "Deadline", "end_date"},
                            { "StartDate", "StartDate"}
                        };
                    string collect_by = "CreatedDate";
                    if (!string.IsNullOrEmpty(query.filter["collect_by"]))
                        collect_by = collect[query.filter["collect_by"]];
                    SqlConditions cond = new SqlConditions();
                    string strW = "", strP = "", strD = "";
                    DateTime from = DateTime.Now;
                    DateTime to = DateTime.Now;
                    if (string.IsNullOrEmpty(query.filter["TuNgay"]) || string.IsNullOrEmpty(query.filter["DenNgay"]))
                        return JsonResultCommon.Custom("Khoảng thời gian không hợp lệ");
                    bool from1 = DateTime.TryParseExact(query.filter["TuNgay"], "dd/MM/yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out from);
                    if (!from1)
                        return JsonResultCommon.Custom("Thời gian bắt đầu không hợp lệ");
                    strW += " and w." + collect_by + ">=@from";
                    strP += " and p." + collect_by + ">=@from";
                    cond.Add("from", from);
                    bool to1 = DateTime.TryParseExact(query.filter["DenNgay"], "dd/MM/yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out to);
                    if (!to1)
                        return JsonResultCommon.Custom("Thời gian kết thúc không hợp lệ");
                    strW += " and w." + collect_by + "<@to";
                    strP += " and p." + collect_by + "<@to";
                    cond.Add("to", to);
                    if (!string.IsNullOrEmpty(query.filter["id_department"]))
                    {
                        //strP += " and id_department=@id_department";
                        //strW += " and p.id_department=@id_department";
                        //cond.Add("id_department", query.filter["id_department"]);
                        listDept = query.filter["id_department"].ToString();
                    }
                    string displayChild = "0";//hiển thị con: 0-không hiển thị, 1- 1 cấp con, 2- nhiều cấp con
                    if (!string.IsNullOrEmpty(query.filter["displayChild"]))
                        displayChild = query.filter["displayChild"];

                    #region danh sách department, list status hoàn thành, trễ,đang làm
                    if (listDept != "")
                    {
                        strP += $" and id_department in ( {listDept} ) ";
                        strW += $" and p.id_department in ( {listDept}) ";
                        strD += " and  id_row in (" + listDept + ") ";
                    }

                    string list_Complete = GetListStatusDynamic(listDept, cnn, " IsFinal "); // IsFinal
                    string  list_Deadline = GetListStatusDynamic(listDept, cnn, " IsDeadline "); // IsDeadline
                    string list_Todo = GetListStatusDynamic(listDept, cnn, " IsTodo "); // IsTodo

                    if (!string.IsNullOrEmpty(query.filter["status"]))//1: đang thực hiên(đang làm & phải làm)||2: đã xong
                    {
                        if (query.filter["status"].ToString().Equals(1.ToString()))
                        {
                            strW += $" and w.status not in ({list_Complete})";
                        }
                        else if (query.filter["status"].ToString().Equals(2.ToString()))
                        {
                            strW += $" and w.status in ({list_Complete})";
                        }

                    }
                    #endregion
                    if (displayChild == "0")
                        strW += " and id_parent is null";

                    #region Trả dữ liệu về backend để hiển thị lên giao diện 
                    string sqlq = @"select  COUNT(CASE WHEN is_project = 1 THEN 1 END) as DuAn,
                                    COUNT(CASE WHEN is_project = 1 and status = 1  THEN 1 END) as DungTienDo, -- and (end_date is null or end_date <= GETDATE())
                                    COUNT(CASE WHEN is_project = 1 and ( status = 2  )THEN 1 END) as ChamTienDo --or end_date > GETDATE()
                                    from we_project_team p where p.Disabled=0   " + strP;
                    sqlq += @"select COUNT(*) as Tong,
                                    COUNT(CASE WHEN ParentID is null THEN 1 END) as PhongBan ,
                                    COUNT(CASE WHEN ParentID is not null THEN 1 END) as ThuMuc 
                                    from we_department d
                                    where d.Disabled=0  " + strD;
                    sqlq += @";select count(*) as Tong, COUNT(CASE WHEN w.status in (" + list_Complete + @") THEN 1 END) as HoanThanh
                                    ,COUNT(CASE WHEN w.status not in (" + list_Complete + "," + list_Deadline + @") THEN 1 END) as DangThucHien,
                                    COUNT(CASE WHEN w.status in (" + list_Deadline + @") THEN 1 END) as TreHan from v_wework_clickup_new w
                        join we_project_team p on p.id_row = w.id_project_team
                        where w.disabled=0" + strW;
                    //sqlq += @";select count(*) as Tong, COUNT(CASE WHEN deadline>=getdate() THEN 1 END) as HoanThanh
                    //        ,COUNT(CASE WHEN deadline<=getdate() THEN 1 END) as DangThucHien from we_milestone m
                    //        join we_project_team p on p.id_row = m.id_project_team
                    //        where m.disabled = 0" + strP;
                    sqlq += @$";select count(pu.id_user) as Tong,
                                    COUNT(CASE WHEN pu.admin = 1 THEN 1 END) as QuanTriVien,
                                    COUNT(CASE WHEN pu.admin = 0 THEN 1 END) as ThanhVien 
                                    from we_project_team_user pu
                                    join we_project_team p on p.id_row=pu.id_project_team
                                    where pu.Disabled=0 and p.Disabled=0" + strP;

                    DataSet ds = cnn.CreateDataSet(sqlq, cond);
                    if (cnn.LastError != null || ds == null)
                        return JsonResultCommon.Exception(_logger, cnn.LastError, _config, loginData, ControllerContext);
                    var data = new
                    {
                        DuAn = ds.Tables[0] != null && ds.Tables[0].Rows.Count > 0 ? new
                        {
                            Tong = ds.Tables[0].Rows[0]["DuAn"],
                            DuAnNoiBo = ds.Tables[0].Rows[0]["DungTienDo"],
                            DuAnKH = ds.Tables[0].Rows[0]["ChamTienDo"]
                        } : null,
                        PhongBan = ds.Tables[1] != null && ds.Tables[1].Rows.Count > 0 ? new
                        {
                            Tong = ds.Tables[1].Rows[0]["Tong"],
                            DuAnNoiBo = ds.Tables[1].Rows[0]["PhongBan"],
                            DuAnKH = ds.Tables[1].Rows[0]["ThuMuc"]
                        } : null,
                        CongViec = ds.Tables[2] != null && ds.Tables[1].Rows.Count > 0 ? new
                        {
                            Tong = ds.Tables[2].Rows[0]["Tong"],
                            HoanThanh = ds.Tables[2].Rows[0]["HoanThanh"],
                            DangThucHien = ds.Tables[2].Rows[0]["DangThucHien"],
                            TreHan = ds.Tables[2].Rows[0]["TreHan"],
                        } : null,
                        //MucTieu = ds.Tables[2] != null && ds.Tables[2].Rows.Count > 0 ? new
                        //{
                        //    Tong = ds.Tables[2].Rows[0]["Tong"],
                        //    HoanThanh = ds.Tables[2].Rows[0]["HoanThanh"],
                        //    DangThucHien = ds.Tables[2].Rows[0]["DangThucHien"],
                        //} : null,
                        ThanhVien = ds.Tables[3] != null && ds.Tables[3].Rows.Count > 0 ? new
                        {
                            Tong = ds.Tables[3].Rows[0]["Tong"],
                            QuanTriVien = ds.Tables[3].Rows[0]["QuanTriVien"],
                            ThanhVien = ds.Tables[3].Rows[0]["ThanhVien"],
                            Khach = 0
                        } : null
                    };
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
        /// Tổng hợp dự án tròn
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [Route("tong-hop-du-an")]
        [HttpGet]
        public object TongHopDuAn([FromQuery] QueryParams query)
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
                    string list_Complete = "";
                    list_Complete = GetListStatusDynamic(listDept, cnn, " IsFinal "); // IsFinal
                    string list_Deadline = "";
                     list_Deadline = GetListStatusDynamic(listDept, cnn, " IsDeadline "); // IsDeadline
                    string list_Todo = "";
                    list_Todo = GetListStatusDynamic(listDept, cnn, " IsTodo "); // IsTodo
                    Dictionary<string, string> collect = new Dictionary<string, string>
                            {
                                { "CreatedDate", "CreatedDate"},
                                { "Deadline", "end_date"},
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
                    strW += " and p." + collect_by + ">=@from";
                    cond.Add("from", from);
                    bool to1 = DateTime.TryParseExact(query.filter["DenNgay"], "dd/MM/yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out to);
                    if (!to1)
                        return JsonResultCommon.Custom("Thời gian kết thúc không hợp lệ");
                    strW += " and p." + collect_by + "<@to";
                    cond.Add("to", to);
                    if (!string.IsNullOrEmpty(query.filter["id_department"]))
                    {
                        strW += " and  id_department in (" + query.filter["id_department"] + ") ";
                        //strW += " and id_department=@id_department";
                        //cond.Add("id_department", query.filter["id_department"]);
                    }
                    //if (!string.IsNullOrEmpty(query.filter["status"]))//1: đang thực hiên(đang làm & phải làm)||2: đã xong
                    //{
                    //    if (query.filter["status"].ToString().Equals(1.ToString()))
                    //    {
                    //        strW += $" and w.status not in ({list_Complete})";
                    //    }
                    //    else if (query.filter["status"].ToString().Equals(2.ToString()))
                    //    {
                    //        strW += $" and w.status in ({list_Complete})";
                    //    }

                    //}

                    #region Trả dữ liệu về backend để hiển thị lên giao diện
                    string sqlq = @"select status, count(*) as value 
                                    from we_project_team p
                                    where Disabled=0 and is_project=1" + strW + " group by status ";
                    DataSet ds = cnn.CreateDataSet(sqlq, cond);
                    if (cnn.LastError != null || ds == null)
                    {
                        return JsonResultCommon.Exception(_logger, cnn.LastError, _config, loginData, ControllerContext);
                    }
                    var dtW = ds.Tables[0].AsEnumerable();
                    List<string> label = new List<string>() { "Đúng tiến độ", "Chậm tiến độ", "Có rủi to cao", "Dự án đóng và hoàn thành", "Dự án đóng và chưa hoàn thành", "Dự án đóng và tạm dừng" };
                    List<int> data = new List<int>();
                    data.Add(dtW.Where(x => x["status"].ToString() == "1").Select(x => x["value"] == DBNull.Value ? 0 : (int)x["value"]).FirstOrDefault());
                    data.Add(dtW.Where(x => x["status"].ToString() == "2").Select(x => x["value"] == DBNull.Value ? 0 : (int)x["value"]).FirstOrDefault());
                    data.Add(dtW.Where(x => x["status"].ToString() == "3").Select(x => x["value"] == DBNull.Value ? 0 : (int)x["value"]).FirstOrDefault());
                    data.Add(dtW.Where(x => x["status"].ToString() == "4").Select(x => x["value"] == DBNull.Value ? 0 : (int)x["value"]).FirstOrDefault());
                    data.Add(dtW.Where(x => x["status"].ToString() == "5").Select(x => x["value"] == DBNull.Value ? 0 : (int)x["value"]).FirstOrDefault());
                    data.Add(dtW.Where(x => x["status"].ToString() == "6").Select(x => x["value"] == DBNull.Value ? 0 : (int)x["value"]).FirstOrDefault());
                    return JsonResultCommon.ThanhCong(new { label = label, datasets = data });
                }
                #endregion
            }
            catch (Exception ex)
            {
                return JsonResultCommon.Exception(_logger, ex, _config, loginData);
            }
        }

        /// <summary>
        /// Mục tiêu theo department
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [Route("muc-tieu-theo-department")]
        [HttpGet]
        public object MucTieuTheoDepartment([FromQuery] QueryParams query)
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
                    string strW = "", strW1 = "";
                    DateTime from = DateTime.Now;
                    DateTime to = DateTime.Now;
                    if (string.IsNullOrEmpty(query.filter["TuNgay"]) || string.IsNullOrEmpty(query.filter["DenNgay"]))
                        return JsonResultCommon.Custom("Khoảng thời gian không hợp lệ");
                    bool from1 = DateTime.TryParseExact(query.filter["TuNgay"], "dd/MM/yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out from);
                    if (!from1)
                        return JsonResultCommon.Custom("Thời gian bắt đầu không hợp lệ");
                    strW += " and m." + collect_by + ">=@from";
                    cond.Add("from", from);
                    bool to1 = DateTime.TryParseExact(query.filter["DenNgay"], "dd/MM/yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out to);
                    if (!to1)
                        return JsonResultCommon.Custom("Thời gian kết thúc không hợp lệ");
                    strW += " and m." + collect_by + "<@to";
                    cond.Add("to", to);
                    if (!string.IsNullOrEmpty(query.filter["id_department"]))
                    {

                        listDept = query.filter["id_department"];
                        //strW += " and id_department=@id_department";
                        //strW1 += " and id_department=@id_department";
                        //cond.Add("id_department", query.filter["id_department"]);
                    }

                    #region danh sách department, list status hoàn thành, trễ,đang làm
                    if (listDept != "")
                    {
                        strW += " and id_department in (" + listDept + ") ";
                        strW1 += " and id_row in (" + listDept + ") ";
                    }
                    string list_Complete = "";
                    list_Complete = GetListStatusDynamic(listDept, cnn, " IsFinal ");
                    //string list_Deadline = "";
                    // list_Deadline = GetListStatusDynamic(listDept, cnn, " IsDeadline ");
                    string list_Todo = "";
                    list_Todo = GetListStatusDynamic(listDept, cnn, " IsTodo ");

                    if (!string.IsNullOrEmpty(query.filter["status"]))//1: đang thực hiên(đang làm & phải làm)||2: đã xong
                    {
                        if (query.filter["status"].ToString().Equals(1.ToString()))
                        {
                            strW += $" and w.status not in ({list_Complete})";
                        }
                        else if (query.filter["status"].ToString().Equals(2.ToString()))
                        {
                            strW += $" and w.status in ({list_Complete})";
                        }

                    }
                    #endregion
                    #region Trả dữ liệu về backend để hiển thị lên giao diện
                    string sqlq = @"select d.id_row, d.title, coalesce(value,0) as value from  we_department d
                                " + (string.IsNullOrEmpty(query.filter["id_department"]) ? "left" : "") + @" 
                                    join (select id_department,count(*) as value from we_milestone m
                                    join we_project_team p on m.id_project_team = p.id_row 
                                    where m.Disabled = 0 and p.disabled = 0 " + strW + @" 
                                    group by id_department) v on d.id_row = v.id_department
                                    where d.Disabled = 0 " + strW1;
                    DataSet ds = cnn.CreateDataSet(sqlq, cond);
                    if (cnn.LastError != null || ds == null)
                    {
                        return JsonResultCommon.Exception(_logger, cnn.LastError, _config, loginData, ControllerContext);
                    }
                    var dtW = ds.Tables[0].AsEnumerable();
                    //var data = from r in dtW
                    //           select new
                    //           {
                    //               trangthai = r["title"],
                    //               value = r["value"]
                    //           };
                    var data = new
                    {
                        label = dtW.Select(r => r["title"].ToString()).ToList(),
                        datasets = dtW.Select(r => (int)r["value"]).ToList()
                    };
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

                string listDept = "";
                string ConnectionString = WeworkLiteController.getConnectionString(ConnectionCache, loginData.CustomerID, _configuration);
                using (DpsConnection cnn = new DpsConnection(ConnectionString))
                {
                    listDept = WeworkLiteController.getListDepartment_GetData(loginData, cnn, HttpContext.Request.Headers, _configuration, ConnectionString);
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
                    }
                    string displayChild = "0";//hiển thị con: 0-không hiển thị, 1- 1 cấp con, 2- nhiều cấp con
                    if (!string.IsNullOrEmpty(query.filter["displayChild"]))
                        displayChild = query.filter["displayChild"];
                    #region danh sách department, list status hoàn thành, trễ,đang làm
                    if (listDept != "")
                    {
                        strW += " and id_department in (" + listDept + ") ";
                    }
                    string list_Complete = "";
                    list_Complete = GetListStatusDynamic(listDept, cnn, " IsFinal ");
                    string list_Deadline = "";
                    list_Deadline = GetListStatusDynamic(listDept, cnn, " IsDeadline ");
                    string list_Todo = "";
                    list_Todo = GetListStatusDynamic(listDept, cnn, " IsTodo ");

                    if (!string.IsNullOrEmpty(query.filter["status"]))//1: đang thực hiên(đang làm & phải làm)||2: đã xong
                    {
                        if (query.filter["status"].ToString().Equals(1.ToString()))
                        {
                            strW += $" and w.status not in ({list_Complete})";
                        }
                        else if (query.filter["status"].ToString().Equals(2.ToString()))
                        {
                            strW += $" and w.status in ({list_Complete})";
                        }

                    }
                    #endregion
                    #region Trả dữ liệu về backend để hiển thị lên giao diện
                    string sqlq = @"select id_row, id_nv, status,
                                    iIf(w.Status in (" + list_Complete + @") and w.end_date>w.deadline,1,0) as is_htquahan,
                                    iIf(w.Status  in (" + list_Complete + @") and (w.end_date <= w.deadline or w.end_date is null or w.deadline is null),1,0) as is_ht,
                                    iIf(w.Status not in (" + list_Complete + "," + list_Deadline + @") , 1, 0) as dangthuchien, 
                                    iIf(w.Status in (" + list_Deadline + @") , 1, 0) as is_quahan, deadline 
                                    from v_wework_clickup_new  w where 1=1 " + strW;
                    if (displayChild == "0")
                        sqlq += " and id_parent is null";
                    DataSet ds = cnn.CreateDataSet(sqlq, cond);
                    if (cnn.LastError != null || ds == null)
                    {
                        return JsonResultCommon.Exception(_logger, cnn.LastError, _config, loginData, ControllerContext);
                    }
                    DataTable dtW = ds.Tables[0];
                    List<object> data = new List<object>();
                    int ht = (int)dtW.Compute("count(id_row)", " is_ht=1 ");
                    int htm = (int)dtW.Compute("count(id_row)", " is_htquahan=1 ");
                    int dth = (int)dtW.Compute("count(id_row)", " dangthuchien=1 ");
                    int qh = (int)dtW.Compute("count(id_row)", " is_quahan=1 ");
                    int khong = (int)dtW.Compute("count(id_row)", " deadline is null ");
                    data.Add(new
                    {
                        trangthai = "Hoàn thành",
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
                    return JsonResultCommon.ThanhCong(new
                    {
                        TrangThaiCongViec = data,
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
                }
                #endregion
            }
            catch (Exception ex)
            {
                return JsonResultCommon.Exception(_logger, ex, _config, loginData);
            }
        }

        /// <summary>
        /// Biểu dodod cột chồng qus trình hoàn thành theo ngày
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [Route("qua-trinh-hoan-thanh")]
        [HttpGet]
        public object QuaTrinhHoanThanh([FromQuery] QueryParams query)
        {
            string Token = Common.GetHeader(Request);
            UserJWT loginData = Ulities.GetUserByHeader(HttpContext.Request.Headers);
            if (loginData == null)
                return JsonResultCommon.DangNhap();
            try
            {
                if (query == null)
                    query = new QueryParams();
                string listDept = "";
                string ConnectionString = WeworkLiteController.getConnectionString(ConnectionCache, loginData.CustomerID, _configuration);
                using (DpsConnection cnn = new DpsConnection(ConnectionString))
                {
                    listDept = WeworkLiteController.getListDepartment_GetData(loginData, cnn, HttpContext.Request.Headers, _configuration, ConnectionString);
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

                    string displayChild = "0";//hiển thị con: 0-không hiển thị, 1- 1 cấp con, 2- nhiều cấp con
                    if (!string.IsNullOrEmpty(query.filter["displayChild"]))
                        displayChild = query.filter["displayChild"];
                    #region danh sách department, list status hoàn thành, trễ,đang làm
                    if (listDept != "")
                    {
                        strW += " and id_department in (" + listDept + ") ";
                    }
                    string list_Complete = "";
                    list_Complete = GetListStatusDynamic(listDept, cnn, " IsFinal ");
                    string list_Deadline = "";
                    list_Deadline = GetListStatusDynamic(listDept, cnn, " IsDeadline ");
                    string list_Todo = "";
                    list_Todo = GetListStatusDynamic(listDept, cnn, " IsTodo ");
                    #endregion
                    if (!string.IsNullOrEmpty(query.filter["status"]))//1: đang thực hiên(đang làm & phải làm)||2: đã xong
                    {
                        if (query.filter["status"].ToString().Equals(1.ToString()))
                        {
                            strW += $" and w.status not in ({list_Complete})";
                        }
                        else if (query.filter["status"].ToString().Equals(2.ToString()))
                        {
                            strW += $" and w.status in ({list_Complete})";
                        }

                    }
                    #region Trả dữ liệu về backend để hiển thị lên giao diện
                    string sqlq = @"select id_row, id_nv, status, CreatedDate, Deadline,iIf(w.Status in (" + list_Complete + @") and w.end_date>w.deadline,1,0) as is_htquahan,
                                    iIf(w.Status in (" + list_Deadline + @"), 1, 0) as is_quahan ,
                                    iIf(w.Status not in (" + list_Complete + "," + list_Deadline + @") , 1, 0) as dangthuchien, 
                                    iIf(w.Status in (" + list_Complete + @") ,1,0) as is_ht
                                    from v_wework_clickup_new  w where 1=1 " + strW;
                    if (displayChild == "0")
                        sqlq += " and id_parent is null";
                    DataSet ds = cnn.CreateDataSet(sqlq, cond);
                    if (cnn.LastError != null || ds == null)
                    {
                        return JsonResultCommon.Exception(_logger, cnn.LastError, _config, loginData, ControllerContext);
                    }
                    DataTable dtW = ds.Tables[0];
                    List<object> data = new List<object>();
                    for (int i = 0; from.AddDays(i) <= to; i++)
                    {
                        var temp = from.AddDays(i);
                        var _as = dtW.AsEnumerable().Where(x => ((DateTime)x[collect_by]).Date == temp.Date);
                        if (_as.Count() == 0)
                        {
                            data.Add(new
                            {
                                tencot = string.Format("{0:dd/MM}", temp),
                                tatca = 0,
                                thuchien = 0,
                                hoanthanh = 0,
                                quahan = 0
                            });
                        }
                        else
                        {
                            data.Add(new
                            {
                                tencot = string.Format("{0:dd/MM}", temp),
                                tatca = _as.Count(),
                                thuchien = (int)_as.CopyToDataTable().Compute("count(id_row)", " dangthuchien = 1 "),
                                hoanthanh = (int)_as.CopyToDataTable().Compute("count(id_row)", " is_ht = 1"),
                                quahan = (int)_as.CopyToDataTable().Compute("count(id_row)", " is_quahan=1 ")
                            });
                        }
                    }
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
        /// Biểu tổng hợp theo tuần
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [Route("tong-hop-theo-tuan")]
        [HttpGet]
        public object TongHopTheoTuan([FromQuery] QueryParams query)
        {
            string Token = Common.GetHeader(Request);
            UserJWT loginData = Ulities.GetUserByHeader(HttpContext.Request.Headers);
            if (loginData == null)
                return JsonResultCommon.DangNhap();
            try
            {
                if (query == null)
                    query = new QueryParams();
                string listDept = "";
                string ConnectionString = WeworkLiteController.getConnectionString(ConnectionCache, loginData.CustomerID, _configuration);
                using (DpsConnection cnn = new DpsConnection(ConnectionString))
                {
                    listDept = WeworkLiteController.getListDepartment_GetData(loginData, cnn, HttpContext.Request.Headers, _configuration, ConnectionString);
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

                    string displayChild = "0";//hiển thị con: 0-không hiển thị, 1- 1 cấp con, 2- nhiều cấp con
                    if (!string.IsNullOrEmpty(query.filter["displayChild"]))
                        displayChild = query.filter["displayChild"];


                    #region danh sách department, list status hoàn thành, trễ,đang làm
                    if (listDept != "")
                    {

                        strW += " and id_department in (" + listDept + ") ";
                    }
                    string list_Complete = "";
                    list_Complete = GetListStatusDynamic(listDept, cnn, " IsFinal ");
                    string list_Deadline = "";
                    list_Deadline = GetListStatusDynamic(listDept, cnn, " IsDeadline ");
                    string list_Todo = "";
                    list_Todo = GetListStatusDynamic(listDept, cnn, " IsTodo ");
                    #endregion
                    if (!string.IsNullOrEmpty(query.filter["status"]))//1: đang thực hiên(đang làm & phải làm)||2: đã xong
                    {
                        if (query.filter["status"].ToString().Equals(1.ToString()))
                        {
                            strW += $" and w.status not in ({list_Complete})";
                        }
                        else if (query.filter["status"].ToString().Equals(2.ToString()))
                        {
                            strW += $" and w.status in ({list_Complete})";
                        }

                    }
                    #region Trả dữ liệu về backend để hiển thị lên giao diện
                    string sqlq = @"select id_row, id_nv, status, CreatedDate, Deadline ,
                                    iIf(w.Status not in (" + list_Complete + "," + list_Deadline + @") , 1, 0) as dangthuchien, 
                                    iIf(w.Status in (" + list_Deadline + @"), 1, 0) as is_quahan ,
                                    iIf(w.Status in (" + list_Complete + @") ,1,0) as is_ht
                                    from v_wework_clickup_new  w where 1=1 " + strW;
                    if (displayChild == "0")
                        sqlq += " and id_parent is null";
                    DataSet ds = cnn.CreateDataSet(sqlq, cond);
                    if (cnn.LastError != null || ds == null)
                    {
                        return JsonResultCommon.Exception(_logger, cnn.LastError, _config, loginData, ControllerContext);
                    }
                    DataTable dtW = ds.Tables[0];
                    List<object> data = new List<object>();
                    Func<DateTime, int> weekProjector = d => CultureInfo.CurrentCulture.Calendar.GetWeekOfYear(d, CalendarWeekRule.FirstFourDayWeek, DayOfWeek.Monday);
                    int year = DateTime.Now.Year;
                    var Children = from rr in dtW.AsEnumerable()
                                   group rr by weekProjector((DateTime)rr["CreatedDate"]) into g
                                   select new
                                   {
                                       //id_row = g.Key,
                                       //start = WorkController.FirstDateOfWeek(2020, g.Key),
                                       //end = WorkController.FirstDateOfWeek(2020, g.Key).AddDays(6),
                                       tencot = WorkController.FirstDateOfWeek(year, g.Key).Day + " - " + WorkController.FirstDateOfWeek(year, g.Key).AddDays(6).ToString("dd/MM/yyyy"),
                                       tatca = g.Count(),
                                       dangthuchien = (int)g.CopyToDataTable().Compute("count(id_row)", " dangthuchien=1 "),
                                       hoanthanh = (int)g.CopyToDataTable().Compute("count(id_row)", " is_ht=1 "),
                                       quahan = (int)g.CopyToDataTable().Compute("count(id_row)", " is_quahan=1 ")
                                   };
                    return JsonResultCommon.ThanhCong(Children);
                    #endregion
                }
            }
            catch (Exception ex)
            {
                return JsonResultCommon.Exception(_logger, ex, _config, loginData);
            }
        }

        /// <summary>
        /// Phân bổ cv theo department
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [Route("phan-bo-theo-department")]
        [HttpGet]
        public object PhanBoTheoDepartment([FromQuery] QueryParams query)
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
                    string strD = "";
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

                    string displayChild = "0";//hiển thị con: 0-không hiển thị, 1- 1 cấp con, 2- nhiều cấp con
                    if (!string.IsNullOrEmpty(query.filter["displayChild"]))
                        displayChild = query.filter["displayChild"];
                    #region danh sách department, list status hoàn thành, trễ,đang làm
                    if (listDept != "")
                    {
                        strW += " and id_department in (" + listDept + ") ";
                        strD += $" and id_row in ({listDept})";
                    }
                    string list_Complete = "";
                    list_Complete = GetListStatusDynamic(listDept, cnn, " IsFinal ");
                    string list_Deadline = "";
                    list_Deadline = GetListStatusDynamic(listDept, cnn, " IsDeadline ");
                    string list_Todo = "";
                    list_Todo = GetListStatusDynamic(listDept, cnn, " IsTodo ");
                    #endregion
                    if (!string.IsNullOrEmpty(query.filter["status"]))//1: đang thực hiên(đang làm & phải làm)||2: đã xong
                    {
                        if (query.filter["status"].ToString().Equals(1.ToString()))
                        {
                            strW += $" and w.status not in ({list_Complete})";
                        }
                        else if (query.filter["status"].ToString().Equals(2.ToString()))
                        {
                            strW += $" and w.status in ({list_Complete})";
                        }

                    }
                    #region Trả dữ liệu về backend để hiển thị lên giao diện
                    string sqlq = "select id_row, title from we_department d where d.disabled=0 " + strD;
                    sqlq += @";select distinct id_row , status, CreatedDate, Deadline,iIf(w.Status in (" + list_Complete + @") and w.end_date>w.deadline,1,0) as is_htquahan,
iIf(w.Status  in (" + list_Complete + @") and (w.end_date <= w.deadline or w.end_date is null or w.deadline is null),1,0) as is_ht,
iIf(w.Status not in (" + list_Complete + "," + list_Deadline + @") , 1, 0) as dangthuchien, 
iIf(w.Status in (" + list_Deadline + @") , 1, 0) as is_quahan,id_department 
                                from v_wework_new w where 1=1 " + strW;
                    if (displayChild == "0")
                        sqlq += " and id_parent is null";
                    DataSet ds = cnn.CreateDataSet(sqlq, cond);
                    if (cnn.LastError != null || ds == null)
                    {
                        return JsonResultCommon.Exception(_logger, cnn.LastError, _config, loginData, ControllerContext);
                    }
                    DataTable dtW = ds.Tables[1];
                    var Children = from r in ds.Tables[0].AsEnumerable()
                                   select new
                                   {
                                       title = r["title"],
                                       data = new
                                       {
                                           tatca = dtW.AsEnumerable().Where(rr => rr["id_department"].Equals(r["id_row"])).Count(),
                                           dangthuchien = (int)dtW.Compute("count( id_row)", " id_department=" + r["id_row"] + " and dangthuchien=1  "),
                                           //dangdanhgia = 0,
                                           //dangdanhgia = (int)dtW.Compute("count(id_row)", " id_department=" + r["id_row"] + " and status=3 "),
                                           hoanthanh = (int)dtW.Compute("count( id_row)", " id_department=" + r["id_row"] + " and (is_ht=1 or is_htquahan=1)"),
                                           quahan = (int)dtW.Compute("count( id_row)", " id_department=" + r["id_row"] + " and is_quahan=1 ")
                                       }
                                   };
                    return JsonResultCommon.ThanhCong(Children);
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
        [Route("report-by-staff")]
        [HttpGet]
        public object ReportByStaff([FromQuery] QueryParams query)
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

                    string displayChild = "0";//hiển thị con: 0-không hiển thị, 1- 1 cấp con, 2- nhiều cấp con
                    if (!string.IsNullOrEmpty(query.filter["displayChild"]))
                        displayChild = query.filter["displayChild"];

                    #region danh sách department, list status hoàn thành, trễ,đang làm
                    if (listDept != "")
                    {

                        strW += " and id_department in (" + listDept + ") ";
                    }
                    string list_Complete = "";
                    list_Complete = GetListStatusDynamic(listDept, cnn, " IsFinal ");
                    string list_Deadline = "";
                     list_Deadline = GetListStatusDynamic(listDept, cnn, " IsDeadline ");
                    string list_Todo = "";
                    list_Todo = GetListStatusDynamic(listDept, cnn, " IsTodo ");
                    #endregion
                    if (!string.IsNullOrEmpty(query.filter["status"]))//1: đang thực hiên(đang làm & phải làm)||2: đã xong
                    {
                        if (query.filter["status"].ToString().Equals(1.ToString()))
                        {
                            strW += $" and w.status not in ({list_Complete})";
                        }
                        else if (query.filter["status"].ToString().Equals(2.ToString()))
                        {
                            strW += $" and w.status in ({list_Complete})";
                        }

                    }
                    #region Lấy dữ liệu account từ JeeAccount
                    DataAccount = WeworkLiteController.GetAccountFromJeeAccount(HttpContext.Request.Headers, _configuration);
                    if (DataAccount == null)
                        return JsonResultCommon.Custom("Lỗi lấy danh sách nhân viên từ hệ thống quản lý tài khoản");

                    string error = "";
                    string listID = WeworkLiteController.ListAccount(HttpContext.Request.Headers, out error, _configuration);
                    if (error != "")
                        return JsonResultCommon.Custom(error);
                    #endregion

                    //DataTable dt = cnn.CreateDataTable(@$"select  *
                    //                                    ,0 as tong,0 as ht,0 as ht_quahan,0 as quahan,0 as danglam,0 as dangdanhgia
                    //                                     from {_config.HRCatalog}.dbo.v_account ");
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
                                    where p.disabled=0 and u.disabled=0  " +
                                    "group by u.id_user";
                    sqlq += @";select id_row, id_nv, status, iIf(w.Status in (" + list_Complete + @") and w.end_date>w.deadline,1,0) as is_htquahan,
                                    iIf(w.Status  in (" + list_Complete + @") and (w.end_date <= w.deadline or w.end_date is null or w.deadline is null),1,0) as is_ht,
                                    iIf(w.Status not in (" + list_Complete + "," + list_Deadline + @") , 1, 0) as dangthuchien, 
                                    iIf(w.Status in (" + list_Deadline + @") , 1, 0) as is_quahan
                                    from v_wework_new w 
                                    where 1=1 " + strW + " (parent)";
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
                    //Xuất dữ liệu
                    string title = "BÁO CÁO CHI TIẾT THEO THÀNH VIÊN";
                    string[] header = { "Mã NV", "Họ tên", "Phòng ban/BP", "Chức danh", "Tổng số CV được giao", "Hoàn thành", "Hoàn thành quá hạn", "Quá hạn", "Đang làm", "Đang đánh giá" };
                    string[] width = { "100", "180", "120", "120", "100", "100", "100", "100", "100", "100" };
                    Hashtable format = new Hashtable();
                    string rowheight = "18.5";
                    excel_member = ExportExcelHelper.ExportToExcel(dt, title, header, width, rowheight, "26", format);
                    var data = from r in dt.AsEnumerable()
                               select new
                               {
                                   id_nv = r["id_nv"],
                                   hoten = r["hoten"],
                                   tenchucdanh = r["tenchucdanh"],
                                   image = r["image"],
                                   //image = WeworkLiteController.genLinkImage(domain, loginData.CustomerID, r["id_nv"].ToString(), _hostingEnvironment.ContentRootPath),
                                   num_project = asP.Where(x => x["id_user"].Equals(r["id_nv"])).Select(x => x["dem"]).DefaultIfEmpty(0).First(),
                                   num_work = total = (hasValue ? (int)dtW.Compute("count(id_nv)", "id_nv=" + r["id_nv"].ToString()) : 0),
                                   danglam = hasValue ? dtW.Compute("count(id_nv)", " dangthuchien=1  and id_nv=" + r["id_nv"].ToString()) : 0,
                                   hoanthanh = success = (hasValue ? (int)dtW.Compute("count(id_nv)", " is_ht=1 and id_nv=" + r["id_nv"].ToString()) : 0),
                                   dangdanhgia = 0,
                                   //dangdanhgia = hasValue ? dtW.Compute("count(id_nv)", " status=3 and id_nv=" + r["id_nv"].ToString()) : 0,
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
        /// Lấy danh sách thành viên theo loại: Params (type (như bên dưới))
        /// 5 thành viên xuất sắc nhất (type: excellent)
        /// 10 thành viên có việc đang làm nhiều nhất (type: most)
        /// 10 thành viên có công việc quá hạn nhiều nhất (type: late)
        /// </summary>
        /// <param name="query"></param>
        /// <returns></returns>
        [Route("report-by-conditions")]
        [HttpGet]
        public object ReportByConditions([FromQuery] QueryParams query)
        {
            string Token = Common.GetHeader(Request);
            UserJWT loginData = Ulities.GetUserByHeader(HttpContext.Request.Headers);
            if (loginData == null)
                return JsonResultCommon.DangNhap();
            try
            {
                if (query == null)
                    query = new QueryParams();
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

                    string listDept = WeworkLiteController.getListDepartment_GetData(loginData, cnn, HttpContext.Request.Headers, _configuration, ConnectionString);
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

                    string displayChild = "0";//hiển thị con: 0-không hiển thị, 1- 1 cấp con, 2- nhiều cấp con
                    if (!string.IsNullOrEmpty(query.filter["displayChild"]))
                        displayChild = query.filter["displayChild"];
                    //DataTable dt = cnn.CreateDataTable(@$"select  *
                    //                                    ,0 as tong,0 as ht,0 as ht_quahan,0 as quahan,0 as danglam,0 as dangdanhgia
                    //                                     from {_config.HRCatalog}.dbo.v_account ");
                    DataTable dt = new DataTable();
                    dt.Columns.Add("id_nv");
                    dt.Columns.Add("hoten");
                    dt.Columns.Add("mobile");
                    dt.Columns.Add("Username");
                    dt.Columns.Add("Email");
                    dt.Columns.Add("Tenchucdanh");
                    dt.Columns.Add("image");
                    dt.Columns.Add("tong");
                    dt.Columns.Add("ht");
                    dt.Columns.Add("ht_quahan");
                    dt.Columns.Add("quahan");
                    dt.Columns.Add("danglam");
                    dt.Columns.Add("dangdanhgia");
                    foreach (var item in DataAccount)
                    {
                        dt.Rows.Add(item.UserId, item.FullName, item.PhoneNumber, item.Username, item.Email, item.Jobtitle, item.AvartarImgURL, 0, 0, 0, 0, 0, 0);
                    }
                    List<string> nvs = dt.AsEnumerable().Select(x => x["id_nv"].ToString()).ToList();
                    if (nvs.Count == 0)
                        return JsonResultCommon.ThanhCong(nvs);
                    string ids = string.Join(",", nvs);
                    #region danh sách department, list status hoàn thành, trễ,đang làm
                    if (listDept != "")
                    {
                        strW += " and id_department in (" + listDept + ") ";
                    }
                    string list_Complete = "";
                    list_Complete = GetListStatusDynamic(listDept, cnn, " IsFinal ");
                    string list_Deadline = "";
                     list_Deadline = GetListStatusDynamic(listDept, cnn, " IsDeadline ");
                    string list_Todo = "";
                    list_Todo = GetListStatusDynamic(listDept, cnn, " IsTodo ");
                    #endregion
                    if (!string.IsNullOrEmpty(query.filter["status"]))//1: đang thực hiên(đang làm & phải làm)||2: đã xong
                    {
                        if (query.filter["status"].ToString().Equals(1.ToString()))
                        {
                            strW += $" and w.status not in ({list_Complete})";
                        }
                        else if (query.filter["status"].ToString().Equals(2.ToString()))
                        {
                            strW += $" and w.status in ({list_Complete})";
                        }

                    }
                    string sqlq = @"select count(distinct p.id_row) as dem,id_user from we_project_team p 
                                    join we_project_team_user u 
                                    on p.id_row=u.id_project_team 
                                    where p.disabled=0 and u.disabled=0  " +
                                    "group by u.id_user";
                    sqlq += @";select id_row, id_nv, status,iIf(w.Status in (" + list_Complete + @") and w.end_date>w.deadline,1,0) as is_htquahan,
iIf(w.Status  in (" + list_Complete + @") and (w.end_date <= w.deadline or w.end_date is null or w.deadline is null),1,0) as is_ht,
iIf(w.Status not in (" + list_Complete + "," + list_Deadline + @") , 1, 0) as dangthuchien, 
iIf(w.Status in (" + list_Deadline + @") , 1, 0) as is_quahan
                                    from v_wework_new w 
                                    where 1=1  " + strW + " (parent)";
                    if (displayChild == "0")
                        sqlq = sqlq.Replace("(parent)", " and id_parent is null");
                    else
                        sqlq = sqlq.Replace("(parent)", " ");
                    DataSet ds = cnn.CreateDataSet(sqlq, cond);
                    var asP = ds.Tables[0].AsEnumerable();
                    DataTable dtW = ds.Tables[1];
                    bool hasValue = dtW.Rows.Count > 0;
                    int total = 0, success = 0;
                    int top = 10;
                    if ("excellent".Equals(query.filter["type"]))
                        top = 5;
                    else
                        top = 10;
                    foreach (DataRow dr in dt.Rows)
                    {
                        if (dtW.Rows.Count > 0)
                        {
                            DataRow[] row = dtW.Select("id_nv=" + dr["id_nv"].ToString());
                            if (row.Length > 0)
                            {
                                dr["tong"] = total = (hasValue ? (int)dtW.Compute("count(id_nv)", "id_nv=" + dr["id_nv"].ToString()) : 0);
                                dr["ht"] = success = (hasValue ? (int)dtW.Compute("count(id_nv)", "is_ht=1 and id_nv=" + dr["id_nv"].ToString()) : 0);
                                dr["ht_quahan"] = hasValue ? dtW.Compute("count(id_nv)", " is_htquahan=1 and id_nv=" + dr["id_nv"].ToString()) : 0;
                                dr["quahan"] = hasValue ? dtW.Compute("count(id_nv)", " is_quahan=1 and id_nv=" + dr["id_nv"].ToString()) : 0;
                                dr["danglam"] = hasValue ? dtW.Compute("count(id_nv)", " dangthuchien=1 and id_nv=" + dr["id_nv"].ToString()) : 0;
                                dr["dangdanhgia"] = 0;
                                //dr["dangdanhgia"] = hasValue ? dtW.Compute("count(id_nv)", " status=3 and id_nv=" + dr["id_nv"].ToString()) : 0;
                            }
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
                        // xóa có công việc nhưng không có công việc trễ
                        else if ("late".Equals(query.filter["type"]))
                        {
                            int quahan = int.Parse(dr["quahan"].ToString());
                            int htmuon = int.Parse(dr["ht_quahan"].ToString());
                            if ((quahan) <= 0 && (htmuon) <= 0)
                            {
                                dr.Delete();
                            }
                        }
                        // xóa công việc xuất sắc mà hoàn thành = 0
                        else if ("excellent".Equals(query.filter["type"]))
                        {
                            int ht = int.Parse(dr["ht"].ToString());
                            int htmuon = int.Parse(dr["ht_quahan"].ToString());
                            if ((ht) <= 0 && (htmuon) <= 0)
                            {
                                dr.Delete();
                            }
                        }
                    }
                    dt.AcceptChanges();
                    var data = (from r in dt.AsEnumerable()
                               .Take(top)
                                where long.Parse(r["tong"].ToString()) > 0
                                select new
                                {
                                    id_nv = r["id_nv"],
                                    hoten = r["hoten"],
                                    tenchucdanh = r["tenchucdanh"],
                                    image = r["image"],
                                    //image = WeworkLiteController.genLinkImage(domain, loginData.CustomerID, r["id_nv"].ToString(), _hostingEnvironment.ContentRootPath),
                                    num_project = asP.Where(x => x["id_user"].Equals(r["id_nv"])).Select(x => x["dem"]).DefaultIfEmpty(0).First(),
                                    num_work = r["tong"],
                                    danglam = r["danglam"],
                                    hoanthanh = r["ht"],
                                    dangdanhgia = r["dangdanhgia"],
                                    ht_quahan = r["ht_quahan"],
                                    quahan = r["quahan"],
                                    tonghoanthanh = (long.Parse(r["ht"].ToString()) + long.Parse(r["ht_quahan"].ToString())),
                                    percentage = long.Parse(r["tong"].ToString()) == 0 ? 0 : (long.Parse(r["ht"].ToString()) * 100 / long.Parse(r["tong"].ToString())),
                                    percentageexcellent = long.Parse(r["tong"].ToString()) == 0 ? 0 : (long.Parse(r["ht"].ToString()) + long.Parse(r["ht_quahan"].ToString()) * 100 / long.Parse(r["tong"].ToString()))
                                });
                    if ("excellent".Equals(query.filter["type"]))
                        data = data.OrderByDescending(x => x.percentageexcellent); 
                    else
                        if ("most".Equals(query.filter["type"]))
                        data = data.OrderByDescending(x => x.danglam);
                    else
                        data = data.OrderByDescending(x => x.quahan);
                    return JsonResultCommon.ThanhCong(data);
                }
            }
            catch (Exception ex)
            {
                return JsonResultCommon.Exception(_logger, ex, _config, loginData);
            }
        }
        /// <summary>
        /// Ma trận Eisenhower
        /// </summary>
        /// <param name="query"></param>
        /// <returns></returns>
        [Route("eisenhower")]
        [HttpGet]
        public object Eisenhower([FromQuery] QueryParams query)
        {
            string Token = Common.GetHeader(Request);
            UserJWT loginData = Ulities.GetUserByHeader(HttpContext.Request.Headers);
            if (loginData == null)
                return JsonResultCommon.DangNhap();
            try
            {
                if (query == null)
                    query = new QueryParams();

                string listDept = "";
                string ConnectionString = WeworkLiteController.getConnectionString(ConnectionCache, loginData.CustomerID, _configuration);
                using (DpsConnection cnn = new DpsConnection(ConnectionString))
                {
                    listDept = WeworkLiteController.getListDepartment_GetData(loginData, cnn, HttpContext.Request.Headers, _configuration, ConnectionString);
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
                    string strD = "";
                    DateTime from = DateTime.Now;
                    DateTime to = DateTime.Now;
                    if (string.IsNullOrEmpty(query.filter["TuNgay"]) || string.IsNullOrEmpty(query.filter["DenNgay"]))
                        return JsonResultCommon.Custom("Khoảng thời gian không hợp lệ");
                    bool from1 = DateTime.TryParseExact(query.filter["TuNgay"], "dd/MM/yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out from);
                    if (!from1)
                        return JsonResultCommon.Custom("Thời gian bắt đầu không hợp lệ");
                    strW += " and w." + collect_by + ">=@from";
                    strD += " and " + collect_by + ">=@from";
                    cond.Add("from", from);
                    bool to1 = DateTime.TryParseExact(query.filter["DenNgay"], "dd/MM/yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out to);
                    if (!to1)
                        return JsonResultCommon.Custom("Thời gian kết thúc không hợp lệ");
                    strW += " and w." + collect_by + "<@to";
                    strD += " and " + collect_by + "<@to";
                    cond.Add("to", to);
                    if (!string.IsNullOrEmpty(query.filter["id_department"]))
                    {
                        listDept = query.filter["id_department"];
                    }


                    string displayChild = "0";//hiển thị con: 0-không hiển thị, 1- 1 cấp con, 2- nhiều cấp con
                    if (!string.IsNullOrEmpty(query.filter["displayChild"]))
                        displayChild = query.filter["displayChild"];

                    #region danh sách department, list status hoàn thành, trễ,đang làm
                    if (listDept != "")
                    {

                        strD += " and id_department in (" + listDept + ") ";
                        strW += " and id_department in (" + listDept + ") ";
                        //strW += " and id_department=@id_department";
                        //strD += " and id_row=@id_department";
                    }
                    string list_Complete = "";
                    list_Complete = GetListStatusDynamic(listDept, cnn, " IsFinal ");
                    string list_Deadline = "";
                     list_Deadline = GetListStatusDynamic(listDept, cnn, " IsDeadline ");
                    string list_Todo = "";
                    list_Todo = GetListStatusDynamic(listDept, cnn, " IsTodo ");
                    #endregion
                    if (!string.IsNullOrEmpty(query.filter["status"]))//1: đang thực hiên(đang làm & phải làm)||2: đã xong
                    {
                        if (query.filter["status"].ToString().Equals(1.ToString()))
                        {
                            strW += $" and w.status not in ({list_Complete})";
                        }
                        else if (query.filter["status"].ToString().Equals(2.ToString()))
                        {
                            strW += $" and w.status in ({list_Complete})";
                        }

                    }
                    string sqlq = @"select id_row, title, deadline, urgent, important, status, clickup_prioritize as level
                                    ,iIf(w.Status in (" + list_Deadline + @"), 1, 0) as is_quahan
                                    from v_wework_clickup_new  w 
                                    where disabled = 0" + strW;
                    if (displayChild == "0")
                        sqlq += " and id_parent is null";
                    DataTable dt = cnn.CreateDataTable(sqlq, cond);
                    bool hasValue = dt.Rows.Count > 0;
                    DataTable list_project = cnn.CreateDataTable("select id_row, require_evaluate from we_project_team where disabled = 0" + strD, cond);
                    bool is_evaluate = list_project.Rows.Count > 0;
                    var listdata = (new
                    {
                        khancap_quantrong = hasValue ? dt.Compute("count( id_row)", " level = 1") : 0,
                        quantrong_khongkhancap = hasValue ? dt.Compute("count( id_row)", " level = 2") : 0,
                        khancap_khongquantrong = hasValue ? dt.Compute("count( id_row)", " level = 3") : 0,
                        khongkhancap_khongquantrong = hasValue ? dt.Compute("count( id_row)", " level <> 1 and level <> 2 and level <> 3") : 0,
                        dangdanhgia = hasValue ? dt.Compute("count(id_row)", "status not in (" + list_Complete + "," + list_Deadline + ")") : 0,
                        late = hasValue ? dt.Compute("count(id_row)", " is_quahan = 1") : 0,
                    });
                    var project_team = (new
                    {
                        total = list_project.Rows.Count,
                        evaluate = is_evaluate ? list_project.Compute("count(id_row)", " require_evaluate = 1") : 0,
                    });
                    return JsonResultCommon.ThanhCong(new
                    {
                        data = listdata,
                        projectteam = project_team
                    });
                    //return JsonResultCommon.ThanhCong(data);
                }
            }
            catch (Exception ex)
            {
                return JsonResultCommon.Exception(_logger, ex, _config, loginData);
            }
        }
        /// <summary>
        /// Danh sách mục tiêu
        /// </summary>
        /// <param name="query"></param>
        /// <returns></returns>
        [Route("milestone")]
        [HttpGet]
        public object milestone([FromQuery] QueryParams query)
        {
            string Token = Common.GetHeader(Request);
            UserJWT loginData = Ulities.GetUserByHeader(HttpContext.Request.Headers);
            if (loginData == null)
                return JsonResultCommon.DangNhap();
            try
            {
                if (query == null)
                    query = new QueryParams();
                string domain = _configuration.GetValue<string>("Host:JeeWork_API") + "/";
                string ConnectionString = WeworkLiteController.getConnectionString(ConnectionCache, loginData.CustomerID, _configuration);
                using (DpsConnection cnn = new DpsConnection(ConnectionString))
                {
                    string listDept = WeworkLiteController.getListDepartment_GetData(loginData, cnn, HttpContext.Request.Headers, _configuration, ConnectionString);
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
                    string strW = "", strW1 = "";
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
                        //strW1 += " and id_department=@id_department";
                        //cond.Add("id_department", query.filter["id_department"]);
                    }

                    string displayChild = "0";//hiển thị con: 0-không hiển thị, 1- 1 cấp con, 2- nhiều cấp con
                    if (!string.IsNullOrEmpty(query.filter["displayChild"]))
                        displayChild = query.filter["displayChild"];

                    #region danh sách department, list status hoàn thành, trễ,đang làm
                    if (listDept != "")
                    {
                        strW += " and id_department in (" + listDept + ") ";
                        strW1 += " and id_department in (" + listDept + ") ";
                    }
                    string list_Complete = "";
                    list_Complete = GetListStatusDynamic(listDept, cnn, " IsFinal ");
                    string list_Deadline = "";
                     list_Deadline = GetListStatusDynamic(listDept, cnn, " IsDeadline ");
                    string list_Todo = "";
                    list_Todo = GetListStatusDynamic(listDept, cnn, " IsTodo ");
                    #endregion
                    if (!string.IsNullOrEmpty(query.filter["status"]))//1: đang thực hiên(đang làm & phải làm)||2: đã xong
                    {
                        if (query.filter["status"].ToString().Equals(1.ToString()))
                        {
                            strW += $" and w.status not in ({list_Complete})";
                            strW1 += $" and w.status not in ({list_Complete})";
                        }
                        else if (query.filter["status"].ToString().Equals(2.ToString()))
                        {
                            strW += $" and w.status in ({list_Complete})";
                            strW1 += $" and w.status in ({list_Complete})";
                        }

                    }
                    DataTable dt = cnn.CreateDataTable(@$"select m.*, coalesce(w.tong,0) as tong,coalesce( w.ht,0) as ht , p.title as project_team,
                                                        m.person_in_charge as Id_NV,'' as hoten,'' as mobile, '' as username, '' as Email, '' as image,'' as Tenchucdanh,'' as NguoiTao, '' as NguoiSua 
                                                        from we_milestone m 
                                                        join we_project_team p on m.id_project_team=p.id_row
                                                        left join (select count(*) as tong, COUNT(CASE WHEN w.Status in (" + list_Complete + @") THEN 1 END) as ht
                                                        ,w.id_milestone from v_wework_new w where 1=1 " + strW + " group by w.id_milestone) w on m.id_row=w.id_milestone " +
                                                        $"where m.Disabled=0 and m.person_in_charge in ({listID}) and m.CreatedBy in ({listID}) " + strW1 + " order by title", cond);
                    #region Map info account từ JeeAccount

                    foreach (DataRow item in dt.Rows)
                    {
                        var info = DataAccount.Where(x => item["Id_NV"].ToString().Contains(x.UserId.ToString())).FirstOrDefault();
                        var infoNguoiTao = DataAccount.Where(x => item["CreatedBy"].ToString().Contains(x.UserId.ToString())).FirstOrDefault();
                        var infoNguoiSua = DataAccount.Where(x => item["UpdatedBy"].ToString().Contains(x.UserId.ToString())).FirstOrDefault();

                        if (info != null)
                        {
                            item["hoten"] = info.FullName;
                            item["mobile"] = info.PhoneNumber;
                            item["username"] = info.Username;
                            item["image"] = info.AvartarImgURL;
                            item["Tenchucdanh"] = info.Jobtitle;
                        }
                        if (infoNguoiSua != null)
                        {
                            item["NguoiSua"] = infoNguoiSua.Username;
                        }
                        if (infoNguoiTao != null)
                        {
                            item["NguoiTao"] = infoNguoiTao.Username;
                        }
                    }
                    #endregion
                    var data = from r in dt.AsEnumerable()
                               select new
                               {
                                   id_row = r["id_row"],
                                   title = r["title"],
                                   description = r["description"],
                                   id_project_team = r["id_project_team"],
                                   project_team = r["project_team"],
                                   deadline_weekday = r["deadline"].Equals(DBNull.Value) ? "" : Common.GetFormatDate(Convert.ToDateTime(r["deadline"]), "77622"),
                                   deadline_day = r["deadline"].Equals(DBNull.Value) ? "" : Common.GetFormatDate(Convert.ToDateTime(r["deadline"]), "dd/MM"),
                                   CreatedDate = string.Format("{0:dd/MM/yyyy HH:mm}", r["CreatedDate"]),
                                   CreatedBy = r["CreatedBy"],
                                   NguoiTao = r["NguoiTao"],
                                   UpdatedDate = r["UpdatedDate"] == DBNull.Value ? "" : string.Format("{0:dd/MM/yyyy HH:mm}", r["UpdatedDate"]),
                                   UpdatedBy = r["UpdatedBy"],
                                   NguoiSua = r["NguoiSua"],
                                   person_in_charge = new
                                   {
                                       id_nv = r["id_nv"],
                                       hoten = r["hoten"],
                                       username = r["username"],
                                       mobile = r["mobile"],
                                       image = r["image"],
                                       //image = WeworkLiteController.genLinkImage(domain, loginData.CustomerID, r["id_nv"].ToString(), _hostingEnvironment.ContentRootPath)
                                   },
                                   Count = new
                                   {
                                       tong = r["tong"],
                                       ht = r["ht"],
                                       percentage = WeworkLiteController.calPercentage(r["tong"], r["ht"])
                                   }
                               };
                    if (!string.IsNullOrEmpty(query.filter["istop"]) && int.Parse(query.filter["istop"].ToString()) == 1)
                    {
                        data.Take(5);
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
        /// Top hoàn thành mục tiêu
        /// </summary>
        /// <param name="query"></param>
        /// <returns></returns>
        [Route("top_milestone")]
        [HttpGet]
        public object Top_done_milestone([FromQuery] QueryParams query)
        {
            string Token = Common.GetHeader(Request);
            UserJWT loginData = Ulities.GetUserByHeader(HttpContext.Request.Headers);
            if (loginData == null)
                return JsonResultCommon.DangNhap();
            try
            {
                if (query == null)
                    query = new QueryParams();
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

                    string displayChild = "0";//hiển thị con: 0-không hiển thị, 1- 1 cấp con, 2- nhiều cấp con
                    if (!string.IsNullOrEmpty(query.filter["displayChild"]))
                        displayChild = query.filter["displayChild"];
                    #region danh sách department, list status hoàn thành, trễ,đang làm
                    if (listDept != "")
                    {
                        strW += " and id_department in (" + listDept + ") ";
                    }
                    string list_Complete = "";
                    list_Complete = GetListStatusDynamic(listDept, cnn, " IsFinal ");
                    string list_Deadline = "";
                     list_Deadline = GetListStatusDynamic(listDept, cnn, " IsDeadline ");
                    string list_Todo = "";
                    list_Todo = GetListStatusDynamic(listDept, cnn, " IsTodo ");
                    #endregion
                    if (!string.IsNullOrEmpty(query.filter["status"]))//1: đang thực hiên(đang làm & phải làm)||2: đã xong
                    {
                        if (query.filter["status"].ToString().Equals(1.ToString()))
                        {
                            strW += $" and w.status not in ({list_Complete})";
                        }
                        else if (query.filter["status"].ToString().Equals(2.ToString()))
                        {
                            strW += $" and w.status in ({list_Complete})";
                        }

                    }
                    DataTable dt_data = cnn.CreateDataTable(@$"select m.*, coalesce(w.tong,0) as tong,coalesce( w.ht,0) as ht , p.title as project_team,
                                                        m.person_in_charge as Id_NV,'' as hoten,'' as mobile, '' as username, '' as Email, '' as image,'' as Tenchucdanh,'' as NguoiTao, '' as NguoiSua 
                                                        from we_milestone m 
                                                        join we_project_team p on m.id_project_team=p.id_row
                                                        left join (select count(*) as tong, COUNT(CASE WHEN w.Status in (" + list_Complete + @") THEN 1 END) as ht
                                                        ,w.id_milestone from v_wework_new w where 1=1 " + strW + " group by w.id_milestone) w on m.id_row=w.id_milestone " +
                                                        $"where m.Disabled=0 and m.person_in_charge in ({listID}) and m.CreatedBy in ({listID}) and ht > 0 order by title", cond);
                    //DataTable dt_data = cnn.CreateDataTable(@$"select m.*, coalesce(w.tong,0) as tong,coalesce( w.ht,0) as ht , p.title as project_team,
                    //                                    m.person_in_charge as Id_NV,'' as hoten,'' as mobile, '' as username, '' as Email, '' as image,'' as Tenchucdanh,'' as NguoiTao, '' as NguoiSua 
                    //                                    from we_milestone m 
                    //                                    join we_project_team p on m.id_project_team=p.id_row
                    //                                    left join (select count(*) as tong, COUNT(CASE WHEN w.status=2 THEN 1 END) as ht
                    //                                    ,w.id_milestone from v_wework_new w where 1=1 " + strW + " group by w.id_milestone) w on m.id_row=w.id_milestone " +
                    //                                    $"where m.Disabled=0 and m.person_in_charge in ({listID}) and m.CreatedBy in ({listID}) " + strW + " order by title", cond);
                    #region Map info account từ JeeAccount

                    foreach (DataRow item in dt_data.Rows)
                    {
                        var info = DataAccount.Where(x => item["Id_NV"].ToString().Contains(x.UserId.ToString())).FirstOrDefault();
                        var infoNguoiTao = DataAccount.Where(x => item["CreatedBy"].ToString().Contains(x.UserId.ToString())).FirstOrDefault();
                        var infoNguoiSua = DataAccount.Where(x => item["UpdatedBy"].ToString().Contains(x.UserId.ToString())).FirstOrDefault();

                        if (info != null)
                        {
                            item["hoten"] = info.FullName;
                            item["mobile"] = info.PhoneNumber;
                            item["username"] = info.Username;
                            item["image"] = info.AvartarImgURL;
                            item["Tenchucdanh"] = info.Jobtitle;
                        }
                        if (infoNguoiSua != null)
                        {
                            item["NguoiSua"] = infoNguoiSua.Username;
                        }
                        if (infoNguoiTao != null)
                        {
                            item["NguoiTao"] = infoNguoiTao.Username;
                        }
                    }
                    #endregion

                    bool hasValue = dt_data.Rows.Count > 0;
                    DataView view = new DataView(dt_data.Copy());
                    DataTable dt = view.ToTable(true, new string[8] { "id_nv", "username", "mobile", "email", "hoten", "image", "tong", "ht" });

                    foreach (DataRow dr in dt.Rows)
                    {
                        if (dt_data.Rows.Count > 0)
                        {
                            DataRow[] row = dt_data.Select("person_in_charge=" + dr["id_nv"].ToString());
                            if (row.Length > 0)
                            {
                                dr["tong"] = row.Sum(x => int.Parse(x["tong"].ToString()));
                                dr["ht"] = row.Sum(x => int.Parse(x["ht"].ToString()));

                            }
                            else
                            {
                                dr["tong"] = dr["ht"] = dr["percentage"] = 0;
                            }
                        }
                        double total = int.Parse(dr["tong"].ToString());
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
                                   username = r["username"],
                                   mobile = r["mobile"],
                                   percentage = WeworkLiteController.calPercentage(r["tong"], r["ht"]),
                                   image = r["image"],
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
        /// Báo cáo theo project team
        /// </summary>
        /// <param name="query"></param>
        /// <returns></returns>
        [Route("report-by-project")]
        [HttpGet]
        public object ReportByDepartment_ProjectTeam([FromQuery] QueryParams query)
        {
            string Token = Common.GetHeader(Request);
            UserJWT loginData = Ulities.GetUserByHeader(HttpContext.Request.Headers);
            if (loginData == null)
                return JsonResultCommon.DangNhap();
            try
            {
                if (query == null)
                    query = new QueryParams();
                string key = "";
                string domain = _configuration.GetValue<string>("Host:JeeWork_API") + "/";
                string ConnectionString = WeworkLiteController.getConnectionString(ConnectionCache, loginData.CustomerID, _configuration);
                using (DpsConnection cnn = new DpsConnection(ConnectionString))
                {
                    string listDept = WeworkLiteController.getListDepartment_GetData(loginData, cnn, HttpContext.Request.Headers, _configuration, ConnectionString);
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

                    string displayChild = "0";//hiển thị con: 0-không hiển thị, 1- 1 cấp con, 2- nhiều cấp con
                    if (!string.IsNullOrEmpty(query.filter["displayChild"]))
                        displayChild = query.filter["displayChild"];

                    #region danh sách department, list status hoàn thành, trễ,đang làm
                    if (listDept != "")
                    {
                        strW += " and id_department in (" + listDept + ") ";
                    }
                    string list_Complete = "";
                    list_Complete = GetListStatusDynamic(listDept, cnn, " IsFinal ");
                    string list_Deadline = "";
                     list_Deadline = GetListStatusDynamic(listDept, cnn, " IsDeadline ");
                    string list_Todo = "";
                    list_Todo = GetListStatusDynamic(listDept, cnn, " IsTodo ");
                    #endregion
                    if (!string.IsNullOrEmpty(query.filter["status"]))//1: đang thực hiên(đang làm & phải làm)||2: đã xong
                    {
                        if (query.filter["status"].ToString().Equals(1.ToString()))
                        {
                            strW += $" and w.status not in ({list_Complete})";
                        }
                        else if (query.filter["status"].ToString().Equals(2.ToString()))
                        {
                            strW += $" and w.status in ({list_Complete})";
                        }

                    }
                    string sql_query = "";
                    if (string.IsNullOrEmpty(query.filter["key"]))
                        return JsonResultCommon.Custom("Key không hợp lệ");
                    key = query.filter["key"];
                    string title = "", _header = "";
                    SqlConditions conds = new SqlConditions();
                    sql_query = "select title " +
                            ",'' as tong,'' as ht,'' as ht_quahan" +
                            ",'' as quahan,'' as danglam,'' as dangdanhgia" +
                            ",disabled, CreatedDate, id_row, description, detail " +
                            "from we_project_team where Disabled = 0";
                    if (!string.IsNullOrEmpty(query.filter["id_department"]))
                    {
                        sql_query += " and  id_department in (" + query.filter["id_department"] + ") ";
                    }
                    title = "BÁO CÁO CHI TIẾT THEO DỰ ÁN & PHÒNG BAN";
                    _header = "Dự án & phòng ban";
                    DataTable dt = cnn.CreateDataTable(sql_query, conds);
                    if (dt.Rows.Count == 0)
                        return JsonResultCommon.Custom("Không có dữ liệu");
                    List<string> ID = dt.AsEnumerable().Select(x => x["id_row"].ToString()).ToList();
                    if (ID.Count == 0)
                        return JsonResultCommon.ThanhCong(ID);
                    string ids = string.Join(",", ID);

                    string sqlq = "";
                    sqlq += @";select id_row, status,iIf(w.Status in (" + list_Complete + @") and w.end_date>w.deadline,1,0) as is_htquahan,
iIf(w.Status  in (" + list_Complete + @") and (w.end_date <= w.deadline or w.end_date is null or w.deadline is null),1,0) as is_ht,
iIf(w.Status not in (" + list_Complete + "," + list_Deadline + @") , 1, 0) as dangthuchien, 
iIf(w.Status in (" + list_Deadline + @") , 1, 0) as is_quahan, id_department, id_project_team
                                    from v_wework_clickup_new w 
                                    where disabled = 0 " + strW;
                    if (displayChild == "0")
                        sqlq += " and id_parent is null";
                    DataTable dt_data = cnn.CreateDataTable(sqlq, cond);
                    bool hasValue = dt_data.Rows.Count > 0;
                    int total = 0, success = 0;
                    foreach (DataRow dr in dt.Rows)
                    {
                        DataRow[] row = dt_data.Select($"{key}={dr["id_row"]}");
                        if (row.Length > 0)
                        {
                            dr["tong"] = total = (hasValue ? (int)dt_data.Compute($"count(id_row)", $" {key}={dr["id_row"]}") : 0);
                            dr["ht"] = success = (hasValue ? (int)dt_data.Compute("count(id_row)", $" is_ht=1  and {key}={dr["id_row"]}") : 0);
                            dr["ht_quahan"] = hasValue ? dt_data.Compute("count(id_row)", $" is_htquahan=1 and {key}={dr["id_row"]}") : 0;
                            dr["quahan"] = hasValue ? dt_data.Compute("count(id_row)", $" is_quahan=1 and {key}={dr["id_row"]}") : 0;
                            dr["danglam"] = hasValue ? dt_data.Compute("count(id_row)", $" dangthuchien=1 and {key}={dr["id_row"]}") : 0;
                            dr["dangdanhgia"] = 0;
                            //dr["dangdanhgia"] = hasValue ? dt_data.Compute("count(id_row)", $" status=3 and {key}={dr["id_row"]}") : 0;
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
                    //Xuất dữ liệu
                    string[] header = { _header, "Tổng số CV được giao", "Hoàn thành", "Hoàn thành quá hạn", "Quá hạn", "Đang làm", "Đang đánh giá" };
                    string[] width = { "180", "100", "100", "100", "100", "100", "100" };
                    Hashtable format = new Hashtable();
                    string rowheight = "18.5";
                    excel_project = ExportExcelHelper.ExportToExcel(dt, title, header, width, rowheight, "26", format);
                    var data = from r in dt.AsEnumerable()
                               where total > 0
                               select new
                               {
                                   id_row = r["id_row"],
                                   title = r["title"],
                                   num_work = r["tong"],
                                   danglam = r["danglam"],
                                   hoanthanh = r["ht"],
                                   dangdanhgia = r["dangdanhgia"],
                                   ht_quahan = r["ht_quahan"],
                                   quahan = r["quahan"],
                                   description = (key.Equals("id_project_team") ? r["description"] : ""),
                                   percentage = total == 0 ? 0 : (success * 100 / total)
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
        /// Báo cáo theo department
        /// </summary>
        /// <param name="query"></param>
        /// <returns></returns>
        [Route("report-by-department")]
        [HttpGet]
        public object ReportByDepartment([FromQuery] QueryParams query)
        {
            string Token = Common.GetHeader(Request);
            UserJWT loginData = Ulities.GetUserByHeader(HttpContext.Request.Headers);
            if (loginData == null)
                return JsonResultCommon.DangNhap();
            string domain = _configuration.GetValue<string>("Host:JeeWork_API") + "/";
            try
            {
                if (query == null)
                    query = new QueryParams();
                string key = "";
                string ConnectionString = WeworkLiteController.getConnectionString(ConnectionCache, loginData.CustomerID, _configuration);
                using (DpsConnection cnn = new DpsConnection(ConnectionString))
                {
                    string listDept = WeworkLiteController.getListDepartment_GetData(loginData, cnn, HttpContext.Request.Headers, _configuration, ConnectionString);
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

                    string displayChild = "0";//hiển thị con: 0-không hiển thị, 1- 1 cấp con, 2- nhiều cấp con
                    if (!string.IsNullOrEmpty(query.filter["displayChild"]))
                        displayChild = query.filter["displayChild"];

                    #region danh sách department, list status hoàn thành, trễ,đang làm
                    if (listDept != "")
                    {
                        strW += " and id_department in (" + listDept + ") ";
                    }
                    string list_Complete = "";
                    list_Complete = GetListStatusDynamic(listDept, cnn, " IsFinal ");
                    string list_Deadline = "";
                     list_Deadline = GetListStatusDynamic(listDept, cnn, " IsDeadline ");
                    string list_Todo = "";
                    list_Todo = GetListStatusDynamic(listDept, cnn, " IsTodo ");
                    #endregion
                    if (!string.IsNullOrEmpty(query.filter["status"]))//1: đang thực hiên(đang làm & phải làm)||2: đã xong
                    {
                        if (query.filter["status"].ToString().Equals(1.ToString()))
                        {
                            strW += $" and w.status not in ({list_Complete})";
                        }
                        else if (query.filter["status"].ToString().Equals(2.ToString()))
                        {
                            strW += $" and w.status in ({list_Complete})";
                        }

                    }
                    string sql_query = "";
                    if (string.IsNullOrEmpty(query.filter["key"]))
                        return JsonResultCommon.Custom("Key không hợp lệ");
                    key = query.filter["key"];
                    string title = "", _header = "";
                    SqlConditions conds = new SqlConditions();
                    sql_query = "select title " +
                            ",'' as tong,'' as ht,'' as ht_quahan" +
                            ",'' as quahan,'' as danglam,'' as dangdanhgia" +
                            ",id_cocau, IdKH, priority, Disabled, CreatedDate, id_row " +
                            "from we_department where Disabled = 0";
                    if (!string.IsNullOrEmpty(query.filter["id_department"]))
                    {
                        sql_query += " and  id_row in (" + query.filter["id_department"] + ") ";
                        //sql_query += " and id_row=@id_department";
                        //conds.Add("id_department", query.filter["id_department"]);
                    }
                    title = "BÁO CÁO CHI TIẾT THEO PHÒNG BAN";
                    _header = "Phòng ban";
                    DataTable dt = cnn.CreateDataTable(sql_query, conds);
                    if (dt.Rows.Count == 0)
                        return JsonResultCommon.Custom("Không có dữ liệu");
                    List<string> ID = dt.AsEnumerable().Select(x => x["id_row"].ToString()).ToList();
                    if (ID.Count == 0)
                        return JsonResultCommon.ThanhCong(ID);
                    string ids = string.Join(",", ID);

                    string sqlq = "";
                    sqlq += @";select id_row, status,iIf(w.Status in (" + list_Complete + @") and w.end_date>w.deadline,1,0) as is_htquahan,
iIf(w.Status  in (" + list_Complete + @") and (w.end_date <= w.deadline or w.end_date is null or w.deadline is null),1,0) as is_ht,
iIf(w.Status not in (" + list_Complete + "," + list_Deadline + @") , 1, 0) as dangthuchien, 
iIf(w.Status in (" + list_Deadline + @") , 1, 0) as is_quahan, id_department, id_project_team
                                    from v_wework_clickup_new w 
                                    where disabled = 0 " + strW;
                    if (displayChild == "0")
                        sqlq += " and id_parent is null";
                    DataTable dt_data = cnn.CreateDataTable(sqlq, cond);
                    bool hasValue = dt_data.Rows.Count > 0;
                    int total = 0, success = 0;
                    foreach (DataRow dr in dt.Rows)
                    {
                        DataRow[] row = dt_data.Select($"{key}={dr["id_row"]}");
                        if (row.Length > 0)
                        {
                            dr["tong"] = total = (hasValue ? (int)dt_data.Compute($"count(id_row)", $" {key}={dr["id_row"]}") : 0);
                            dr["ht"] = success = (hasValue ? (int)dt_data.Compute("count(id_row)", $" is_ht=1  and {key}={dr["id_row"]}") : 0);
                            dr["ht_quahan"] = hasValue ? dt_data.Compute("count(id_row)", $" is_htquahan=1 and {key}={dr["id_row"]}") : 0;
                            dr["quahan"] = hasValue ? dt_data.Compute("count(id_row)", $" is_quahan=1 and {key}={dr["id_row"]}") : 0;
                            dr["danglam"] = hasValue ? dt_data.Compute("count(id_row)", $" dangthuchien=1 and {key}={dr["id_row"]}") : 0;
                            dr["dangdanhgia"] = 0;
                            //dr["dangdanhgia"] = hasValue ? dt_data.Compute("count(id_row)", $" status=3 and {key}={dr["id_row"]}") : 0;
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
                    //Xuất dữ liệu
                    string[] header = { _header, "Tổng số CV được giao", "Hoàn thành", "Hoàn thành quá hạn", "Quá hạn", "Đang làm", "Đang đánh giá" };
                    string[] width = { "180", "100", "100", "100", "100", "100", "100" };
                    Hashtable format = new Hashtable();
                    string rowheight = "18.5";
                    excel_department = ExportExcelHelper.ExportToExcel(dt, title, header, width, rowheight, "26", format);
                    var data = from r in dt.AsEnumerable()
                               select new
                               {
                                   id_row = r["id_row"],
                                   title = r["title"],
                                   num_work = r["tong"],
                                   danglam = r["danglam"],
                                   hoanthanh = r["ht"],
                                   dangdanhgia = r["dangdanhgia"],
                                   ht_quahan = r["ht_quahan"],
                                   quahan = r["quahan"],
                                   description = (key.Equals("id_project_team") ? r["description"] : ""),
                                   percentage = total == 0 ? 0 : (success * 100 / total)
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
        /// Báo cáo các phòng ban
        /// </summary>
        /// <param name="query"></param>
        /// <returns></returns>
        [Route("report-to-departments")]
        [HttpGet]
        public object ReportToDepartments([FromQuery] QueryParams query)
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
                    string key = "";
                    string domain = _configuration.GetValue<string>("Host:JeeWork_API") + "/";
                    string listDept = WeworkLiteController.getListDepartment_GetData(loginData, cnn, HttpContext.Request.Headers, _configuration, ConnectionString);
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

                    string displayChild = "0";//hiển thị con: 0-không hiển thị, 1- 1 cấp con, 2- nhiều cấp con
                    if (!string.IsNullOrEmpty(query.filter["displayChild"]))
                        displayChild = query.filter["displayChild"];
                    string sql_query = "";
                    sql_query = @"select title  ,'' as tong,'' as ht,'' as ht_quahan ,ParentID
,'' as quahan,'' as danglam,'' as dangdanhgia
,id_cocau, IdKH, priority, Disabled, CreatedDate, id_row 
from we_department where Disabled = 0";
                    SqlConditions conds = new SqlConditions();
                    //if (!string.IsNullOrEmpty(query.filter["id_department"]))
                    //{
                    //    sql_query += " and id_row=@id_department";
                    //    conds.Add("id_department", query.filter["id_department"]);
                    //}
                    #region danh sách department, list status hoàn thành, trễ,đang làm
                    if (listDept != "")
                    {
                        strW += " and id_department in (" + listDept + ") ";
                        sql_query += " and id_row in (" + listDept + ") ";
                    }
                    string list_Complete = "";
                    list_Complete = GetListStatusDynamic(listDept, cnn, " IsFinal ");
                    string list_Deadline = "";
                     list_Deadline = GetListStatusDynamic(listDept, cnn, " IsDeadline ");
                    string list_Todo = "";
                    list_Todo = GetListStatusDynamic(listDept, cnn, " IsTodo ");
                    #endregion
                    if (!string.IsNullOrEmpty(query.filter["status"]))//1: đang thực hiên(đang làm & phải làm)||2: đã xong
                    {
                        if (query.filter["status"].ToString().Equals(1.ToString()))
                        {
                            strW += $" and w.status not in ({list_Complete})";
                        }
                        else if (query.filter["status"].ToString().Equals(2.ToString()))
                        {
                            strW += $" and w.status in ({list_Complete})";
                        }

                    }
                    DataTable dt = cnn.CreateDataTable(sql_query, conds);
                    if (dt.Rows.Count == 0)
                        return JsonResultCommon.Custom("Không có dữ liệu");
                    List<string> ID = dt.AsEnumerable().Select(x => x["id_row"].ToString()).ToList();
                    if (ID.Count == 0)
                        return JsonResultCommon.ThanhCong(ID);
                    string ids = string.Join(",", ID);

                    DataTable dt_group = cnn.CreateDataTable(@"select id_row, id_project_team, title, description
                                                              ,Locked, CreatedDate, Disabled, UpdatedDate, reviewer
                                                              from we_group where id_project_team in 
                                                              (select id_row from we_project_team where id_department in (" + ids + "))");
                    bool is_group = dt_group.Rows.Count > 0;
                    string sqlq = "";
                    sqlq += @"select id_row, status,iIf(w.Status in (" + list_Complete + @") and w.end_date>w.deadline,1,0) as is_htquahan,
iIf(w.Status  in (" + list_Complete + @") and (w.end_date <= w.deadline or w.end_date is null or w.deadline is null),1,0) as is_ht,
iIf(w.Status not in (" + list_Complete + "," + list_Deadline + @") , 1, 0) as dangthuchien, 
iIf(w.Status in (" + list_Deadline + @") , 1, 0) as is_quahan, id_department, id_project_team
                                    from v_wework_clickup_new w 
                                    where disabled = 0 " + strW;
                    if (displayChild == "0")
                        sqlq += " and id_parent is null";
                    DataTable dt_data = cnn.CreateDataTable(sqlq, cond);
                    bool is_work = dt_data.Rows.Count > 0;
                    var data = new
                    {
                        department_active = dt.AsEnumerable().Where(x => string.IsNullOrEmpty(x["ParentID"].ToString())).ToList().Count(), //Rows.Count
                        group_closed = is_group ? dt_group.Compute("count(id_row)", "Locked=1") : 0,
                        num_work = dt_data.Rows.Count,
                        hoanthanh = is_work ? dt_data.Compute("count(id_row)", "is_ht = 1 or is_htquahan = 1") : 0,
                        quahan = is_work ? dt_data.Compute("count(id_row)", "is_quahan = 1") : 0,
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
        /// Phân bổ cv theo department
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [Route("cac-con-so-thong-ke")]
        [HttpGet]
        public object CacConSoThongKe([FromQuery] QueryParams query)
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
                    string strD = "";
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
                    }

                    string displayChild = "0";//hiển thị con: 0-không hiển thị, 1- 1 cấp con, 2- nhiều cấp con
                    if (!string.IsNullOrEmpty(query.filter["displayChild"]))
                        displayChild = query.filter["displayChild"];
                    #region danh sách department, list status hoàn thành, trễ,đang làm
                    if (listDept != "")
                    {
                        strW += " and id_department in (" + listDept + ") ";
                        strD += " and id_row in (" + listDept + ") ";
                    }
                    string list_Complete = "";
                    list_Complete = GetListStatusDynamic(listDept, cnn, " IsFinal ");
                    string list_Deadline = "";
                     list_Deadline = GetListStatusDynamic(listDept, cnn, " IsDeadline ");
                    string list_Todo = "";
                    list_Todo = GetListStatusDynamic(listDept, cnn, " IsTodo ");
                    #endregion
                    if (!string.IsNullOrEmpty(query.filter["status"]))//1: đang thực hiên(đang làm & phải làm)||2: đã xong
                    {
                        if (query.filter["status"].ToString().Equals(1.ToString()))
                        {
                            strW += $" and w.status not in ({list_Complete})";
                        }
                        else if (query.filter["status"].ToString().Equals(2.ToString()))
                        {
                            strW += $" and w.status in ({list_Complete})";
                        }

                    }
                    #region Trả dữ liệu về backend để hiển thị lên giao diện
                    string sqlq = "select id_row, title from we_department d where d.disabled=0 " + strD;
                    sqlq += @";select id_row, id_nv, status, CreatedDate
                            ,Deadline,iIf(w.Status in (" + list_Complete + @") and w.end_date>w.deadline,1,0) as is_htquahan,
iIf(w.Status  in (" + list_Complete + @") and (w.end_date <= w.deadline or w.end_date is null or w.deadline is null),1,0) as is_ht,
iIf(w.Status not in (" + list_Complete + "," + list_Deadline + @") , 1, 0) as dangthuchien, 
iIf(w.Status in (" + list_Deadline + @") , 1, 0) as is_quahan,id_department 
                            from v_wework_new w where 1=1 " + strW;
                    string sql_comment = @$"select iif(sum(num_comment)>0,sum(num_comment),0) from we_work where id_row in (select distinct id_row
from v_wework_new w where 1=1 {strW})";
                    string sql_object = @"select u.id_user from we_project_team_user u join we_project_team p on u.id_project_team = p.id_row
where u.Disabled = 0 and p.Disabled = 0 and p.id_department in (" + listDept + ") ; " +
                                            "select count(id_project_team) " +
                                            "from v_wework_new w where 1 = 1 " + strW + "(child) " +
                                            "group by id_project_team";
                    if (displayChild == "0")
                    {
                        sqlq += " and id_parent is null";
                        sql_object = sql_object.Replace("(child)", " and id_parent is null");
                    }
                    else
                    {
                        sql_object = sql_object.Replace("(child)", "");
                    }
                    DataSet ds_object = cnn.CreateDataSet(sql_object, cond);
                    DataSet ds = cnn.CreateDataSet(sqlq, cond);
                    double daynum, weeknum, work_of_week, work_of_member = 0, work_of_project = 0, comment = 0;
                    GetWeekInMonth(from, to, out daynum, out weeknum);
                    DataTable dt_work = ds.Tables[1];
                    work_of_week = dt_work.Rows.Count / weeknum;
                    if (ds_object.Tables[0].Rows.Count > 0)
                        work_of_member = work_of_week / ds_object.Tables[0].Rows.Count;
                    if (ds_object.Tables[1].Rows.Count > 0)
                        work_of_project = work_of_week / ds_object.Tables[1].Rows.Count;
                    comment = double.Parse(cnn.ExecuteScalar(sql_comment, cond).ToString());
                    if (cnn.LastError != null || ds == null)
                        return JsonResultCommon.Exception(_logger, cnn.LastError, _config, loginData, ControllerContext);
                    var data = new
                    {
                        week = string.Format("{0:###,##0.00}", work_of_week),
                        member = string.Format("{0:###,##0.00}", work_of_member),
                        project = string.Format("{0:###,##0.00}", work_of_project),
                        numcomment = comment,
                    };
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
        /// Phân bổ cv theo department
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [Route("thong-ke-he-thong")]
        [HttpGet]
        public object ThongKeHeThong([FromQuery] QueryParams query)
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
                    string strD = "";
                    string strP = "";
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
                    }

                    string displayChild = "0";//hiển thị con: 0-không hiển thị, 1- 1 cấp con, 2- nhiều cấp con
                    if (!string.IsNullOrEmpty(query.filter["displayChild"]))
                        displayChild = query.filter["displayChild"];
                    #region danh sách department, list status hoàn thành, trễ,đang làm
                    if (listDept != "")
                    {
                        strW += " and w.id_department in (" + listDept + ") ";
                        strP += " and p.id_department in (" + listDept + ") ";
                        strD += " and d.id_row in (" + listDept + ") ";
                    }
                    string list_Complete = "";
                    list_Complete = GetListStatusDynamic(listDept, cnn, " IsFinal ");
                    string list_Deadline = "";
                     list_Deadline = GetListStatusDynamic(listDept, cnn, " IsDeadline ");
                    string list_Todo = "";
                    list_Todo = GetListStatusDynamic(listDept, cnn, " IsTodo ");
                    #endregion
                    if (!string.IsNullOrEmpty(query.filter["status"]))//1: đang thực hiên(đang làm & phải làm)||2: đã xong
                    {
                        if (query.filter["status"].ToString().Equals(1.ToString()))
                        {
                            strW += $" and w.status not in ({list_Complete})";
                        }
                        else if (query.filter["status"].ToString().Equals(2.ToString()))
                        {
                            strW += $" and w.status in ({list_Complete})";
                        }

                    }
                    #region Trả dữ liệu về backend để hiển thị lên giao diện
                    string sqlthongkechung = @"select id_row, title,Locked from we_project_team p where disabled=0 " + strP;
                    sqlthongkechung += @";select id_row, title from we_department d where d.disabled=0 " + strD;
                    sqlthongkechung += @";select id_row, id_nv, status, CreatedDate,id_parent
                            ,Deadline,iIf(w.Status in (" + list_Complete + @") and w.end_date>w.deadline,1,0) as is_htquahan,
iIf(w.Status  in (" + list_Complete + @") and (w.end_date <= w.deadline or w.end_date is null or w.deadline is null),1,0) as is_ht,
iIf(w.Status not in (" + list_Complete + "," + list_Deadline + @") , 1, 0) as dangthuchien, 
iIf(w.Status in (" + list_Deadline + @") , 1, 0) as is_quahan,id_department 
                            from v_wework_new w where 1=1 " + strW + " (parent)";
                    sqlthongkechung += @";select * from we_project_team_user u join we_project_team p on
u.id_project_team = p.id_row where u.Disabled = 0 and p.Disabled = 0 " + strP;

                    if (displayChild == "0")
                        sqlthongkechung = sqlthongkechung.Replace("(parent)", " and id_parent is null");
                    else
                        sqlthongkechung = sqlthongkechung.Replace("(parent)", " ");
                    DataSet ds_thongkechung = cnn.CreateDataSet(sqlthongkechung, cond);

                    // gắn người
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
                    dt.Columns.Add("soduan");
                    dt.Columns.Add("sophongban");
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
                                    where p.disabled=0 and u.disabled=0  " +
                                    "group by u.id_user";
                    sqlq += @";select id_row, id_nv, status, iIf(w.Status in (" + list_Complete + @") and w.end_date>w.deadline,1,0) as is_htquahan,
                                    iIf(w.Status  in (" + list_Complete + @") and (w.end_date <= w.deadline or w.end_date is null or w.deadline is null),1,0) as is_ht,
                                    iIf(w.Status not in (" + list_Complete + "," + list_Deadline + @") , 1, 0) as dangthuchien, 
                                    iIf(w.Status in (" + list_Deadline + @") , 1, 0) as is_quahan
                                    from v_wework_new w 
                                    where 1=1 " + strW + " (parent)";
                    sqlq += @"; select * from we_project_team_user u join we_project_team p on
u.id_project_team = p.id_row where u.Disabled = 0 and p.Disabled = 0 " + strP;
                    sqlq += @"; select o.* from we_department_owner o join we_department d on o.id_department = d.id_row 
and o.Disabled = 0 and d.Disabled = 0 " + strD;
                    if (displayChild == "0")
                        sqlq = sqlq.Replace("(parent)", " and id_parent is null");
                    else
                        sqlq = sqlq.Replace("(parent)", " ");

                    DataSet ds = cnn.CreateDataSet(sqlq, cond);
                    var asP = ds.Tables[0].AsEnumerable();
                    DataTable dtW = ds.Tables[1];
                    bool hasValue = dtW.Rows.Count > 0;
                    bool hasValueDA = ds.Tables[2].Rows.Count > 0;
                    bool hasValuePB = ds.Tables[3].Rows.Count > 0;
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
                            dr["soduan"] = hasValueDA ? ds.Tables[2].Compute("count(id_user)", " id_user =" + dr["id_nv"].ToString()) : 0;
                            dr["sophongban"] = hasValuePB ? ds.Tables[3].Compute("count(id_user)", " id_user=" + dr["id_nv"].ToString()) : 0;
                            //dr["dangdanhgia"] = hasValue ? dtW.Compute("count(id_nv)", " status=3 and id_nv=" + dr["id_nv"].ToString()) : 0;
                        }
                        else
                        {
                            dr["tong"] = dr["ht"] = dr["ht_quahan"] = dr["quahan"] = dr["danglam"] = dr["dangdanhgia"] = 0;
                        }
                    }
                    //for (int i = dt.Rows.Count - 1; i >= 0; i--)
                    //{
                    //    DataRow dr = dt.Rows[i];
                    //    total = int.Parse(dr["tong"].ToString());
                    //    if ((total) <= 0)
                    //    {
                    //        dr.Delete();
                    //    }
                    //}
                    //dt.AcceptChanges();

                    var dataU = from r in dt.AsEnumerable()
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
                                    soduan = r["soduan"],
                                    sophongban = r["sophongban"],
                                    ht_quahan = hasValue ? dtW.Compute("count(id_nv)", " is_htquahan=1 and id_nv=" + r["id_nv"].ToString()) : 0,
                                    quahan = hasValue ? dtW.Compute("count(id_nv)", " is_quahan=1 and id_nv=" + r["id_nv"].ToString()) : 0,
                                    percentage = total == 0 ? 0 : (success * 100 / total)
                                };
                    var dataThongKe = new
                    {
                        Soduan = ds_thongkechung.Tables[0].Rows.Count,
                        Sophongban = ds_thongkechung.Tables[1].Rows.Count,
                        Soduandangchay = ds_thongkechung.Tables[0].Compute("count(id_row)", " Locked = 0 "),
                        Soduandadong = ds_thongkechung.Tables[0].Compute("count(id_row)", " Locked = 1 "),
                        Socongviec = ds_thongkechung.Tables[2].Compute("count(id_row)", " id_parent is null "),
                        Socongvieccon = ds_thongkechung.Tables[2].Compute("count(id_row)", " id_parent is not null "),
                        Sothanhvien = ds_thongkechung.Tables[3].Rows.Count,
                    };

                    dataU = dataU.OrderByDescending(x => x.num_work);

                    if (cnn.LastError != null || ds == null)
                        return JsonResultCommon.Exception(_logger, cnn.LastError, _config, loginData, ControllerContext);
                    var data = new
                    {
                        dataThongKe = dataThongKe,
                        dataUser = dataU,
                    };
                    return JsonResultCommon.ThanhCong(data);
                }
                #endregion
            }
            catch (Exception ex)
            {
                return JsonResultCommon.Exception(_logger, ex, _config, loginData);
            }
        }



        [Route("TagCloud")]
        [HttpGet]
        public object TagCloud([FromQuery] QueryParams query)
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
                    string strW = "", strW1 = "";
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
                        //strW1 += " and id_department=@id_department";
                        //cond.Add("id_department", query.filter["id_department"]);
                    }
                    string list_Complete = "";
                    list_Complete = GetListStatusDynamic(listDept, cnn, " IsFinal "); // IsFinal
                    string list_Deadline = "";
                     list_Deadline = GetListStatusDynamic(listDept, cnn, " IsDeadline "); // IsDeadline
                    string list_Todo = "";
                    list_Todo = GetListStatusDynamic(listDept, cnn, " IsTodo "); // IsTodo
                    if (!string.IsNullOrEmpty(query.filter["status"]))//1: đang thực hiên(đang làm & phải làm)||2: đã xong
                    {
                        if (query.filter["status"].ToString().Equals(1.ToString()))
                        {
                            strW += $" and w.status not in ({list_Complete})";
                        }
                        else if (query.filter["status"].ToString().Equals(2.ToString()))
                        {
                            strW += $" and w.status in ({list_Complete})";
                        }

                    }
                    string displayChild = "0";//hiển thị con: 0-không hiển thị, 1- 1 cấp con, 2- nhiều cấp con
                    if (!string.IsNullOrEmpty(query.filter["displayChild"]))
                        displayChild = query.filter["displayChild"];
                    if (listDept != "")
                    {
                        strW += " and id_department in (" + listDept + ") ";
                        strW1 += " and id_department in (" + listDept + ") ";
                    }
                    string sqlq = @"select tag.id_row,tag.title from we_tag tag 
join we_project_team p on tag.id_project_team=p.id_row
where tag.Disabled=0 and p.Disabled=0 " + strW1;
                    DataTable dt = cnn.CreateDataTable(sqlq, cond);
                    sqlq = @"select tag.id_row, id_work, id_tag, we_tag.title 
                                                                from we_work_tag tag
                                                                join we_tag on we_tag.id_row = tag.id_tag
                                                                join v_wework_new w on w.id_row = tag.id_work
                                                                where tag.disabled = 0 and we_tag.disabled = 0 " + strW + " (parent) ";
                    if (displayChild == "0")
                        sqlq = sqlq.Replace("(parent)", " and id_parent is null");
                    else
                        sqlq = sqlq.Replace("(parent)", " ");
                    DataTable dt_detail = cnn.CreateDataTable(sqlq, cond);
                    var data = from r in dt.AsEnumerable()
                               select new
                               {
                                   id_row = r["id_row"],
                                   name = r["title"],
                                   weight = dt_detail.Compute("count(id_work)", "id_tag= " + r["id_row"])
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
        /// <param name="Loai">xls:Excel;doc:Word</param>
        /// <returns></returns>
        [CusAuthorize]
        [Route("ExportExcel")]
        [HttpGet]
        public async Task<IActionResult> ExportExcel(string FileName)
        {
            string excel = "";
            switch (FileName)
            {
                case "member":
                    excel = excel_member;
                    break;
                case "department":
                    excel = excel_department;
                    break;
                case "project":
                    excel = excel_project;
                    break;
                default:
                    break;
            }
            string fileName = "List" + FileName + "_" + DateTime.Now.ToString("ddMMyyyy_HHmmss") + ".xls";
            var bytearr = Encoding.UTF8.GetBytes(excel);
            this.Response.Headers.Add("X-Filename", fileName);
            this.Response.Headers.Add("Access-Control-Expose-Headers", "X-Filename");
            return new FileContentResult(bytearr, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");
        }

        [Route("ExportReportExcel")]
        [HttpPost]
        public BaseModel<object> ExportReportExcel([FromBody] List<BaoCaoThongKe> data, string FileName)
        {
            UserJWT loginData = Ulities.GetUserByHeader(HttpContext.Request.Headers);
            List<BaoCaoThongKe> ListDetail = data;
            string Tenfile = "";
            string TenBC = "";
            try
            {
                if (FileName == "member")
                {
                    Tenfile = "THANHVIEN";
                    TenBC = "THEO THÀNH VIÊN";
                }
                else if (FileName == "project")
                {
                    Tenfile = "DUAN";
                    TenBC = "THEO DỰ ÁN";
                }
                else
                {
                    Tenfile = "PHONGBAN";
                    TenBC = "PHÒNG BAN";
                }

                if (ListDetail != null && ListDetail.Count > 0)
                {
                    using (MemoryStream mem = new MemoryStream())
                    {
                        using (SpreadsheetDocument spreadsheet = SpreadsheetDocument.Create(mem, SpreadsheetDocumentType.Workbook))
                        {
                            WorkbookPart workbookPart = spreadsheet.AddWorkbookPart();
                            workbookPart.Workbook = new Workbook();
                            DocumentFormat.OpenXml.Spreadsheet.Sheets sheets1 = new DocumentFormat.OpenXml.Spreadsheet.Sheets();
                            #region

                            WorksheetPart worksheetPart_K = workbookPart.AddNewPart<WorksheetPart>();
                            worksheetPart_K.Worksheet = new Worksheet();


                            MergeCells mergeCells_K = new MergeCells();

                            // Adding style
                            WorkbookStylesPart stylePart = workbookPart.AddNewPart<WorkbookStylesPart>();
                            ExportExcelHelper excelHelper = new ExportExcelHelper();
                            stylePart.Stylesheet = excelHelper.GenerateStylesheet();
                            stylePart.Stylesheet.Save();


                            SheetData sheetData_K = new SheetData();

                            //DocumentFormat.OpenXml.Spreadsheet.Sheets sheets1_K = new DocumentFormat.OpenXml.Spreadsheet.Sheets();

                            Sheet sheet_K = new Sheet();
                            sheet_K.Id = spreadsheet.WorkbookPart.GetIdOfPart(worksheetPart_K);
                            sheet_K.SheetId = 2; //sheet Id, anything but unique
                            sheet_K.Name = "THỐNG KÊ CHI TIẾT";
                            sheets1.Append(sheet_K);


                            DocumentFormat.OpenXml.Spreadsheet.Row rowTitle_Null = new DocumentFormat.OpenXml.Spreadsheet.Row();

                            DocumentFormat.OpenXml.Spreadsheet.Cell dataCellnd_Null = new DocumentFormat.OpenXml.Spreadsheet.Cell();
                            dataCellnd_Null.CellReference = "A1";
                            dataCellnd_Null.DataType = CellValues.String;
                            dataCellnd_Null.StyleIndex = 8;
                            CellValue cellValue_Null = new CellValue();
                            cellValue_Null.Text = ("THỐNG KÊ CHI TIẾT " + TenBC);//"BÁO CÁO TỔNG HỢP TÌNH HÌNH XỬ LÝ CÔNG VIỆC";
                            dataCellnd_Null.Append(cellValue_Null);
                            rowTitle_Null.RowIndex = 1;
                            rowTitle_Null.AppendChild(dataCellnd_Null);
                            sheetData_K.AppendChild(rowTitle_Null);


                            Row rowTitle2 = new Row();
                            Cell dataCellnd2 = new Cell();
                            dataCellnd2.CellReference = "A3";
                            dataCellnd2.DataType = CellValues.String;
                            dataCellnd2.StyleIndex = 10;
                            CellValue cellValue2 = new CellValue();
                            cellValue2.Text = "Tổng số dữ liệu: " + data.Count.ToString();
                            dataCellnd2.Append(cellValue2);
                            rowTitle2.RowIndex = 3;
                            rowTitle2.AppendChild(dataCellnd2);
                            sheetData_K.AppendChild(rowTitle2);

                            MergeCells mergeCells_Null = new MergeCells();

                            //append a MergeCell to the mergeCells for each set of merged cells
                            mergeCells_K.Append(new MergeCell() { Reference = new StringValue("A1:F1") });
                            mergeCells_K.Append(new MergeCell() { Reference = new StringValue("A2:F2") });
                            //mergeCells_K.Append(new MergeCell() { Reference = new StringValue("A3:H3") });

                            // Constructing header
                            DocumentFormat.OpenXml.Spreadsheet.Row row_K = new DocumentFormat.OpenXml.Spreadsheet.Row();
                            row_K.RowIndex = (uint)5;
                            if (FileName == "member")
                            {
                                row_K.Append(
                                  excelHelper.ConstructCell("STT", CellValues.String, 2),
                                  excelHelper.ConstructCell("THÀNH VIÊN", CellValues.String, 2),
                                  excelHelper.ConstructCell("HOÀN THÀNH", CellValues.String, 2),
                                  excelHelper.ConstructCell("HOÀN THÀNH MUỘN", CellValues.String, 2),
                                  excelHelper.ConstructCell("QUÁ HẠN", CellValues.String, 2),
                                  excelHelper.ConstructCell("ĐANG THỰC HIỆN", CellValues.String, 2)
                               //excelHelper.ConstructCell("ĐANG ĐÁNH GIÁ", CellValues.String, 2)
                               );
                            }
                            else
                            {
                                row_K.Append(
                                excelHelper.ConstructCell("STT", CellValues.String, 2),
                                  excelHelper.ConstructCell("PHÒNG BAN", CellValues.String, 2),
                                  excelHelper.ConstructCell("HOÀN THÀNH", CellValues.String, 2),
                                  excelHelper.ConstructCell("HOÀN THÀNH MUỘN", CellValues.String, 2),
                                  excelHelper.ConstructCell("QUÁ HẠN", CellValues.String, 2),
                                  excelHelper.ConstructCell("ĐANG THỰC HIỆN", CellValues.String, 2)
                                 //excelHelper.ConstructCell("ĐANG ĐÁNH GIÁ", CellValues.String, 2)
                                 );
                            }

                            sheetData_K.AppendChild(row_K);
                            int stt = 1;
                            foreach (var item in ListDetail)
                            {
                                row_K = new DocumentFormat.OpenXml.Spreadsheet.Row();
                                //row.RowIndex = (uint)i + 3; //RowIndex must be start with 1, since i = 0
                                row_K.RowIndex = (uint)(ListDetail.IndexOf(item) + 6); //RowIndex must be start with 1, since i = 0, khúc này là dòng sẽ insert vào Excel

                                row_K.Append(
                                excelHelper.ConstructCell(stt++.ToString(), CellValues.Number, 9),
                                    excelHelper.ConstructCell(item.Ten, CellValues.String, 4),
                                    excelHelper.ConstructCell(item.col1, CellValues.String, 4),
                                    excelHelper.ConstructCell(item.col2, CellValues.String, 4),
                                    excelHelper.ConstructCell((item.col3), CellValues.String, 4),
                                    excelHelper.ConstructCell(item.col4, CellValues.String, 4)
                                //excelHelper.ConstructCell(item.col5, CellValues.String, 4)
                                );
                                sheetData_K.Append(row_K);
                            }

                            worksheetPart_K.Worksheet = new DocumentFormat.OpenXml.Spreadsheet.Worksheet(sheetData_K);




                            #endregion
                            worksheetPart_K.Worksheet.InsertAfter(mergeCells_K, worksheetPart_K.Worksheet.Elements<SheetData>().First());
                            spreadsheet.WorkbookPart.Workbook.AppendChild<DocumentFormat.OpenXml.Spreadsheet.Sheets>(sheets1);

                            workbookPart.Workbook.Save();

                            spreadsheet.Close();

                            FileContentResult file = new FileContentResult(mem.ToArray(), "application/octet-stream")
                            {
                                FileDownloadName = "BCTK_" + Tenfile + "_" + DateTime.Now.ToString("ddMMyyyymmss") + ".xlsx"
                            };

                            return JsonResultCommon.ThanhCong(file);
                        }
                    }
                }
                else
                {
                    return JsonResultCommon.Custom("Không có dữ liệu");
                    //return null;
                }
            }
            catch (Exception ex)
            {
                return JsonResultCommon.Exception(_logger, ex, _config, loginData);
            }
        }

        [Route("ExportReportExcelHeThong")]
        [HttpPost]
        public BaseModel<object> ExportReportExcelHeThong([FromBody] ThongkehethongModel data)
        {
            UserJWT loginData = Ulities.GetUserByHeader(HttpContext.Request.Headers);
            List<ThongkethanhvienhethongModel> ListDetail = data.sanhsachthanhvien;
            string Tenfile = "HETHONG";
            try
            {
                if (ListDetail != null && ListDetail.Count > 0)
                {
                    using (MemoryStream mem = new MemoryStream())
                    {
                        using (SpreadsheetDocument spreadsheet = SpreadsheetDocument.Create(mem, SpreadsheetDocumentType.Workbook))
                        {
                            WorkbookPart workbookPart = spreadsheet.AddWorkbookPart();
                            workbookPart.Workbook = new Workbook();
                            DocumentFormat.OpenXml.Spreadsheet.Sheets sheets1 = new DocumentFormat.OpenXml.Spreadsheet.Sheets();
                            #region
                            WorksheetPart worksheetPart_K = workbookPart.AddNewPart<WorksheetPart>();
                            worksheetPart_K.Worksheet = new Worksheet();
                            MergeCells mergeCells_K = new MergeCells();
                            // Adding style
                            WorkbookStylesPart stylePart = workbookPart.AddNewPart<WorkbookStylesPart>();
                            ExportExcelHelper excelHelper = new ExportExcelHelper();
                            stylePart.Stylesheet = excelHelper.GenerateStylesheet();
                            stylePart.Stylesheet.Save();
                            SheetData sheetData_K = new SheetData();
                            //DocumentFormat.OpenXml.Spreadsheet.Sheets sheets1_K = new DocumentFormat.OpenXml.Spreadsheet.Sheets();
                            Sheet sheet_K = new Sheet();
                            sheet_K.Id = spreadsheet.WorkbookPart.GetIdOfPart(worksheetPart_K);
                            sheet_K.SheetId = 2; //sheet Id, anything but unique
                            sheet_K.Name = "THỐNG KÊ CHI TIẾT";
                            sheets1.Append(sheet_K);
                            DocumentFormat.OpenXml.Spreadsheet.Row rowTitle_Null = new DocumentFormat.OpenXml.Spreadsheet.Row();
                            DocumentFormat.OpenXml.Spreadsheet.Cell dataCellnd_Null = new DocumentFormat.OpenXml.Spreadsheet.Cell();
                            dataCellnd_Null.CellReference = "A1";
                            dataCellnd_Null.DataType = CellValues.String;
                            dataCellnd_Null.StyleIndex = 8;
                            CellValue cellValue_Null = new CellValue();
                            cellValue_Null.Text = ("THỐNG KÊ BÁO CÁO HỆ THỐNG ");//"BÁO CÁO TỔNG HỢP TÌNH HÌNH XỬ LÝ CÔNG VIỆC";
                            dataCellnd_Null.Append(cellValue_Null);
                            rowTitle_Null.RowIndex = 1;
                            rowTitle_Null.AppendChild(dataCellnd_Null);
                            sheetData_K.AppendChild(rowTitle_Null);

                            DocumentFormat.OpenXml.Spreadsheet.Row row_Ktitle = new DocumentFormat.OpenXml.Spreadsheet.Row();
                            row_Ktitle.RowIndex = (uint)3;
                            row_Ktitle.Append(
                                   excelHelper.ConstructCell("", CellValues.String, 2),
                                   excelHelper.ConstructCell("SỐ DỰ ÁN", CellValues.String, 2),
                                   excelHelper.ConstructCell("SỐ PHÒNG BAN", CellValues.String, 2),
                                   excelHelper.ConstructCell("DỰ ÁN ĐANG CHẠY", CellValues.String, 2),
                                   excelHelper.ConstructCell("DỰ ÁN ĐÃ ĐÓNG", CellValues.String, 2),
                                   excelHelper.ConstructCell("SỐ CÔNG VIỆC", CellValues.String, 2),
                                   excelHelper.ConstructCell("SỐ CÔNG VIỆC CON", CellValues.String, 2),
                                    excelHelper.ConstructCell("THÀNH VIÊN", CellValues.String, 2)
                                );
                            sheetData_K.AppendChild(row_Ktitle);
                            DocumentFormat.OpenXml.Spreadsheet.Row row_Kval = new DocumentFormat.OpenXml.Spreadsheet.Row();
                            row_Kval.RowIndex = (uint)4;
                            row_Kval.Append(
                                   excelHelper.ConstructCell("", CellValues.String, 2),
                                   excelHelper.ConstructCell(data.soduan, CellValues.String, 2),
                                   excelHelper.ConstructCell(data.phongban, CellValues.String, 2),
                                   excelHelper.ConstructCell(data.soduandangchay, CellValues.String, 2),
                                   excelHelper.ConstructCell(data.soduandadong, CellValues.String, 2),
                                   excelHelper.ConstructCell(data.congviec, CellValues.String, 2),
                                    excelHelper.ConstructCell(data.convieccon, CellValues.String, 2),
                                    excelHelper.ConstructCell(data.tongthanhvien, CellValues.String, 2)
                                );
                            sheetData_K.AppendChild(row_Kval);
                            MergeCells mergeCells_Null = new MergeCells();
                            //append a MergeCell to the mergeCells for each set of merged cells
                            mergeCells_K.Append(new MergeCell() { Reference = new StringValue("A1:H1") });
                            mergeCells_K.Append(new MergeCell() { Reference = new StringValue("A2:H2") });
                            //mergeCells_K.Append(new MergeCell() { Reference = new StringValue("A3:H3") });
                            // Constructing header
                            DocumentFormat.OpenXml.Spreadsheet.Row row_K = new DocumentFormat.OpenXml.Spreadsheet.Row();
                            row_K.RowIndex = (uint)6;
                            row_K.Append(
                                   excelHelper.ConstructCell("STT", CellValues.String, 2),
                                   excelHelper.ConstructCell("THÀNH VIÊN", CellValues.String, 2),
                                   excelHelper.ConstructCell("HOÀN THÀNH", CellValues.String, 2),
                                   excelHelper.ConstructCell("HOÀN THÀNH MUỘN", CellValues.String, 2),
                                   excelHelper.ConstructCell("QUÁ HẠN", CellValues.String, 2),
                                   excelHelper.ConstructCell("ĐANG THỰC HIỆN", CellValues.String, 2),
                                    excelHelper.ConstructCell("ĐANG ĐÁNH GIÁ", CellValues.String, 2),
                                    excelHelper.ConstructCell("ĐANG ĐÁNH GIÁ", CellValues.String, 2)
                                );
                            sheetData_K.AppendChild(row_K);
                            int stt = 1;
                            foreach (var item in ListDetail)
                            {
                                row_K = new DocumentFormat.OpenXml.Spreadsheet.Row();
                                //row.RowIndex = (uint)i + 3; //RowIndex must be start with 1, since i = 0
                                row_K.RowIndex = (uint)(ListDetail.IndexOf(item) + 7); //RowIndex must be start with 1, since i = 0, khúc này là dòng sẽ insert vào Excel
                                row_K.Append(
                                excelHelper.ConstructCell(stt++.ToString(), CellValues.Number, 9),
                                    excelHelper.ConstructCell(item.thanhvien, CellValues.String, 4),
                                    excelHelper.ConstructCell(item.soduan, CellValues.String, 4),
                                    excelHelper.ConstructCell(item.phongban, CellValues.String, 4),
                                    excelHelper.ConstructCell((item.congviec), CellValues.String, 4),
                                    excelHelper.ConstructCell(item.hoanthanh, CellValues.String, 4),
                                    excelHelper.ConstructCell(item.quahan, CellValues.String, 4),
                                    excelHelper.ConstructCell(item.dangthuchien, CellValues.String, 4)
                                );
                                sheetData_K.Append(row_K);
                            }
                            worksheetPart_K.Worksheet = new DocumentFormat.OpenXml.Spreadsheet.Worksheet(sheetData_K);
                            #endregion
                            worksheetPart_K.Worksheet.InsertAfter(mergeCells_K, worksheetPart_K.Worksheet.Elements<SheetData>().First());
                            spreadsheet.WorkbookPart.Workbook.AppendChild<DocumentFormat.OpenXml.Spreadsheet.Sheets>(sheets1);
                            workbookPart.Workbook.Save();
                            spreadsheet.Close();
                            FileContentResult file = new FileContentResult(mem.ToArray(), "application/octet-stream")
                            {
                                FileDownloadName = "BCTK_" + Tenfile + "_" + DateTime.Now.ToString("ddMMyyyymmss") + ".xlsx"
                            };
                            return JsonResultCommon.ThanhCong(file);
                        }
                    }
                }
                else
                {
                    return JsonResultCommon.Custom("Không có dữ liệu");
                    //return null;
                }
            }
            catch (Exception ex)
            {
                return JsonResultCommon.Exception(_logger, ex, _config, loginData);
            }
        }
        public class BaoCaoThongKe
        {
            public string Ten { get; set; }
            public string col1 { get; set; }
            public string col2 { get; set; }
            public string col3 { get; set; }
            public string col4 { get; set; }
            public string col5 { get; set; }
        }
        public class ThongkehethongModel
        {
            public string soduan { get; set; }
            public string soduandadong { get; set; }
            public string soduandangchay { get; set; }
            public string phongban { get; set; }
            public string congviec { get; set; }
            public string convieccon { get; set; }
            public string tongthanhvien { get; set; }
            public List<ThongkethanhvienhethongModel> sanhsachthanhvien { get; set; }
        }
        public class ThongkethanhvienhethongModel
        {
            public string thanhvien { get; set; }
            public string soduan { get; set; }
            public string phongban { get; set; }
            public string congviec { get; set; }
            public string hoanthanh { get; set; }
            public string quahan { get; set; }
            public string dangthuchien { get; set; }
        }

        public static string GetListStatusDynamic(string lst_dept, DpsConnection cnn, string fieldname)
        {
            string strW = "";
            if (lst_dept != "")
            {
                strW += "and id_department in (" + lst_dept + ") ";
            };
            string sql = @$"select distinct * from we_status where Disabled = 0 and {fieldname} = 1 and id_project_team 
        in ( select p.id_row from we_project_team p where Disabled = 0 " + strW + " )";
            DataTable dt = cnn.CreateDataTable(sql);
            List<string> nvs = dt.AsEnumerable().Select(x => x["id_row"].ToString()).ToList();
            string ids = string.Join(",", nvs);
            if (string.IsNullOrEmpty(ids))
            {
                return "0";
            }
            return ids;
        }


        public static void GetWeekInMonth(DateTime from, DateTime to, out double daynum, out double weekNum)
        {
            weekNum = 0;
            daynum = 0;
            daynum = DateDiff(DateInterval.Day, from, to);
            weekNum = (daynum % 7 == 0) ? weekNum - 1 : (daynum / 7);
        }
        public static partial class Simulate
        {
            public enum DateInterval
            {
                Day,
                DayOfYear,
                Hour,
                Minute,
                Month,
                Quarter,
                Second,
                Weekday,
                WeekOfYear,
                Year
            }
            public static long DateDiff(DateInterval intervalType, System.DateTime dateOne, System.DateTime dateTwo)
            {
                switch (intervalType)
                {
                    case DateInterval.Day:
                    case DateInterval.DayOfYear:
                        System.TimeSpan spanForDays = dateTwo - dateOne;
                        return (long)spanForDays.TotalDays;
                    case DateInterval.Hour:
                        System.TimeSpan spanForHours = dateTwo - dateOne;
                        return (long)spanForHours.TotalHours;
                    case DateInterval.Minute:
                        System.TimeSpan spanForMinutes = dateTwo - dateOne;
                        return (long)spanForMinutes.TotalMinutes;
                    case DateInterval.Month:
                        return ((dateTwo.Year - dateOne.Year) * 12) + (dateTwo.Month - dateOne.Month);
                    case DateInterval.Quarter:
                        long dateOneQuarter = (long)System.Math.Ceiling(dateOne.Month / 3.0);
                        long dateTwoQuarter = (long)System.Math.Ceiling(dateTwo.Month / 3.0);
                        return (4 * (dateTwo.Year - dateOne.Year)) + dateTwoQuarter - dateOneQuarter;
                    case DateInterval.Second:
                        System.TimeSpan spanForSeconds = dateTwo - dateOne;
                        return (long)spanForSeconds.TotalSeconds;
                    case DateInterval.Weekday:
                        System.TimeSpan spanForWeekdays = dateTwo - dateOne;
                        return (long)(spanForWeekdays.TotalDays / 7.0);
                    case DateInterval.WeekOfYear:
                        System.DateTime dateOneModified = dateOne;
                        System.DateTime dateTwoModified = dateTwo;
                        while (dateTwoModified.DayOfWeek != System.Globalization.DateTimeFormatInfo.CurrentInfo.FirstDayOfWeek)
                        {
                            dateTwoModified = dateTwoModified.AddDays(-1);
                        }
                        while (dateOneModified.DayOfWeek != System.Globalization.DateTimeFormatInfo.CurrentInfo.FirstDayOfWeek)
                        {
                            dateOneModified = dateOneModified.AddDays(-1);
                        }
                        System.TimeSpan spanForWeekOfYear = dateTwoModified - dateOneModified;
                        return (long)(spanForWeekOfYear.TotalDays / 7.0);
                    case DateInterval.Year:
                        return dateTwo.Year - dateOne.Year;
                    default:
                        return 0;
                }
            }
        }
    }
}
