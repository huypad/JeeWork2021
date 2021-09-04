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
using DPSinfra.ConnectionCache;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace JeeWork_Core2021.Controllers.Wework
{
    [ApiController]
    [Route("api/reportbyprojects")]
    [EnableCors("JeeWorkPolicy")]
    /// <summary>
    /// Báo cáo theo dự án
    /// </summary>
    // [EnableCors(origins: "*", headers: "*", methods: "*")]
    public class ReportByProjectController : ControllerBase
    {
        private readonly IHostingEnvironment _hostingEnvironment;
        private JeeWorkConfig _config;
        public static string excel_member;
        public static string excel_project;
        public static string excel_department;
        public List<AccUsernameModel> DataAccount;
        private IConnectionCache ConnectionCache;
        private IConfiguration _configuration;
        private readonly ILogger<ReportByProjectController> _logger;

        public ReportByProjectController(IOptions<JeeWorkConfig> config, IHostingEnvironment hostingEnvironment, IConnectionCache _cache, IConfiguration configuration, ILogger<ReportByProjectController> logger)
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
                string strW = "", strP = "";
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

                if (!string.IsNullOrEmpty(query.filter["id_projectteam"]))
                {
                    strW += " and  id_project_team= @id_projectteam ";
                    cond.Add("id_projectteam", query.filter["id_projectteam"]);
                }
                else
                {
                    return JsonResultCommon.Custom("Dự án không hợp lệ");
                }
                int id_projectteam = int.Parse(query.filter["id_projectteam"]);

                if (!string.IsNullOrEmpty(query.filter["status"]))//1: đang thực hiên(đang làm & phải làm)||2: đã xong
                {
                    if (query.filter["status"].ToString().Equals(1.ToString()))
                    {
                        strW += " and w.status not in (select stt.id_row from we_status stt where stt.id_project_team = @id_projectteam and IsFinal = 1)";
                    }
                    else if (query.filter["status"].ToString().Equals(2.ToString()))
                    {
                        strW += " and w.status  in (select stt.id_row from we_status stt where stt.id_project_team = @id_projectteam and IsFinal = 1)";
                    }
                }
                string displayChild = "0";//hiển thị con: 0-không hiển thị, 1- 1 cấp con, 2- nhiều cấp con
                if (!string.IsNullOrEmpty(query.filter["displayChild"]))
                {
                    displayChild = query.filter["displayChild"];
                    if(displayChild == "0")
                    {
                        strW += " and id_parent is null ";
                    }
                }

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
                    #region get trạng thái của dự án - hoàn thành - đang làm 
                    long hoanthanh = GetStatusComplete(id_projectteam, cnn);
                    long deadline = GetStatusDeadline(id_projectteam, cnn);
                    long danglam = GetStatusTodo(id_projectteam,cnn);
                    #endregion
                    cond.Add("hoanthanh", hoanthanh);
                    cond.Add("danglam", danglam);
                    cond.Add("deadline", deadline);
                    #region Trả dữ liệu về backend để hiển thị lên giao diện 
                    string sqlq = @"select count(*) as Tong, COUNT(CASE WHEN w.status = @hoanthanh THEN 1 END) as HoanThanh
,COUNT(CASE WHEN w.status NOT IN (@deadline,@hoanthanh) THEN 1 END) as DangThucHien,COUNT(CASE WHEN w.status = @deadline THEN 1 END) as TreHan from v_wework_clickup_new w
join we_project_team p on p.id_row = w.id_project_team
where w.disabled=0  " + strW; 
                    sqlq += @$";select count(*) as Tong,COUNT(CASE WHEN pu.admin = 1 THEN 1 END) as QuanTriVien,COUNT(CASE WHEN pu.admin = 0 THEN 1 END) as ThanhVien from we_project_team_user pu
join we_project_team p on p.id_row=pu.id_project_team
where pu.Disabled=0 and p.Disabled=0 and id_project_team= @id_projectteam  ";
                    
                    DataSet ds = cnn.CreateDataSet(sqlq, cond);
                    if (cnn.LastError != null || ds == null)
                        return JsonResultCommon.Exception(_logger,cnn.LastError, _config, loginData , ControllerContext);
                    var data = new
                    {
                        //DuAn = ds.Tables[0] != null && ds.Tables[0].Rows.Count > 0 ? new
                        //{
                        //    Tong = ds.Tables[0].Rows[0]["DuAn"],
                        //    DuAnNoiBo = ds.Tables[0].Rows[0]["DuAnNoiBo"],
                        //    DuAnKH = ds.Tables[0].Rows[0]["DuAnKH"]
                        //} : null,
                        //PhongBan = ds.Tables[0] != null && ds.Tables[0].Rows.Count > 0 ? new
                        //{
                        //    Tong = ds.Tables[0].Rows[0]["PhongBan"],
                        //    DuAnNoiBo = ds.Tables[0].Rows[0]["PhongBanNoiBo"],
                        //    DuAnKH = ds.Tables[0].Rows[0]["PhongBanNgoai"]
                        //} : null,
                        CongViec = ds.Tables[0] != null && ds.Tables[0].Rows.Count > 0 ? new
                        {
                            Tong = ds.Tables[0].Rows[0]["Tong"],
                            HoanThanh = ds.Tables[0].Rows[0]["HoanThanh"],
                            DangThucHien = ds.Tables[0].Rows[0]["DangThucHien"],
                            TreHan = ds.Tables[0].Rows[0]["TreHan"],
                        } : null,
                        //MucTieu = ds.Tables[2] != null && ds.Tables[2].Rows.Count > 0 ? new
                        //{
                        //    Tong = ds.Tables[2].Rows[0]["Tong"],
                        //    HoanThanh = ds.Tables[2].Rows[0]["HoanThanh"],
                        //    DangThucHien = ds.Tables[2].Rows[0]["DangThucHien"],
                        //} : null,
                        ThanhVien = ds.Tables[1] != null && ds.Tables[1].Rows.Count > 0 ? new
                        {
                            Tong = ds.Tables[1].Rows[0]["Tong"],
                            QuanTriVien = ds.Tables[1].Rows[0]["QuanTriVien"],
                            ThanhVien = ds.Tables[1].Rows[0]["ThanhVien"],
                            Khach = 0
                        } : null
                    };
                    return JsonResultCommon.ThanhCong(data);
                    #endregion
                }
            }
            catch (Exception ex)
            {
                return JsonResultCommon.Exception(_logger,ex, _config, loginData);
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
                    strW += " and id_department=@id_department";
                    cond.Add("id_department", query.filter["id_department"]);
                }
                if (!string.IsNullOrEmpty(query.filter["status"]))//1: đang thực hiên(đang làm & phải làm)||2: đã xong
                {
                    if (query.filter["status"].ToString().Equals(1.ToString()))
                    {
                        strW += " and w.status not in (select stt.id_row from we_status stt where stt.id_project_team = @id_projectteam and IsFinal = 1)";
                    }
                    else if (query.filter["status"].ToString().Equals(2.ToString()))
                    {
                        strW += " and w.status  in (select stt.id_row from we_status stt where stt.id_project_team = @id_projectteam and IsFinal = 1)";
                    }
                }
                string ConnectionString = WeworkLiteController.getConnectionString(ConnectionCache, loginData.CustomerID, _configuration);
                using (DpsConnection cnn = new DpsConnection(ConnectionString))
                {
                    #region Trả dữ liệu về backend để hiển thị lên giao diện
                    string sqlq = @"select status, count(*) as value 
                                    from we_project_team p
                                    where Disabled=0 and is_project=1" + strW + " group by status ";
                    DataSet ds = cnn.CreateDataSet(sqlq, cond);
                    if (cnn.LastError != null || ds == null)
                    {
                        return JsonResultCommon.Exception(_logger,cnn.LastError, _config, loginData , ControllerContext);
                    }
                    var dtW = ds.Tables[0].AsEnumerable(); //1: đúng tiến độ, 2: chậm tiến độ, 3: rủi ro cao,4: thành công, 5: thất bại,6: bị hủy
                    List<string> label = new List<string>() { "Đúng tiến độ", "Chậm tiến độ", "Có rủi to cao", "Dự án đóng và hoàn thành", "Dự án đóng và chưa hoàn thành", "Dự án đóng và tạm dừng" };
                    List<int> data = new List<int>();
                    data.Add(dtW.Where(x => x["status"].ToString() == "1").Select(x => x["value"] == DBNull.Value ? 0 : (int)x["value"]).FirstOrDefault());
                    data.Add(dtW.Where(x => x["status"].ToString() == "2").Select(x => x["value"] == DBNull.Value ? 0 : (int)x["value"]).FirstOrDefault());
                    data.Add(dtW.Where(x => x["status"].ToString() == "3").Select(x => x["value"] == DBNull.Value ? 0 : (int)x["value"]).FirstOrDefault());
                    data.Add(dtW.Where(x => x["status"].ToString() == "4").Select(x => x["value"] == DBNull.Value ? 0 : (int)x["value"]).FirstOrDefault());
                    data.Add(dtW.Where(x => x["status"].ToString() == "5").Select(x => x["value"] == DBNull.Value ? 0 : (int)x["value"]).FirstOrDefault());
                    data.Add(dtW.Where(x => x["status"].ToString() == "6").Select(x => x["value"] == DBNull.Value ? 0 : (int)x["value"]).FirstOrDefault());
                    return JsonResultCommon.ThanhCong(new { label = label, datasets = data });
                    #endregion
                }
            }
            catch (Exception ex)
            {
                return JsonResultCommon.Exception(_logger,ex, _config, loginData);
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
                    strW += " and id_department=@id_department";
                    strW1 += " and id_department=@id_department";
                    cond.Add("id_department", query.filter["id_department"]);
                }
                if (!string.IsNullOrEmpty(query.filter["status"]))//1: đang thực hiên(đang làm & phải làm)||2: đã xong
                {
                    strW += " and status=@status";
                    cond.Add("status", query.filter["status"]);
                }

                string ConnectionString = WeworkLiteController.getConnectionString(ConnectionCache, loginData.CustomerID, _configuration);
                using (DpsConnection cnn = new DpsConnection(ConnectionString))
                {
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
                        return JsonResultCommon.Exception(_logger,cnn.LastError, _config, loginData , ControllerContext);
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
                    #endregion
                }
            }
            catch (Exception ex)
            {
                return JsonResultCommon.Exception(_logger,ex, _config, loginData);
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
                //if (!string.IsNullOrEmpty(query.filter["id_department"]))
                //{
                //    strW += " and id_department=@id_department";
                //    cond.Add("id_department", query.filter["id_department"]);
                //}
                if (!string.IsNullOrEmpty(query.filter["id_projectteam"]))
                {
                    strW += " and id_project_team=@id_projectteam";
                    cond.Add("id_projectteam", query.filter["id_projectteam"]);
                }
                if (!string.IsNullOrEmpty(query.filter["status"]))//1: đang thực hiên(đang làm & phải làm)||2: đã xong
                {
                    if (query.filter["status"].ToString().Equals(1.ToString()))
                    {
                        strW += " and w.status not in (select stt.id_row from we_status stt where stt.id_project_team = @id_projectteam and IsFinal = 1)";
                    }
                    else if (query.filter["status"].ToString().Equals(2.ToString()))
                    {
                        strW += " and w.status  in (select stt.id_row from we_status stt where stt.id_project_team = @id_projectteam and IsFinal = 1)";
                    }
                }
                string displayChild = "0";//hiển thị con: 0-không hiển thị, 1- 1 cấp con, 2- nhiều cấp con
                if (!string.IsNullOrEmpty(query.filter["displayChild"]))
                    displayChild = query.filter["displayChild"];

                string ConnectionString = WeworkLiteController.getConnectionString(ConnectionCache, loginData.CustomerID, _configuration);
                using (DpsConnection cnn = new DpsConnection(ConnectionString))
                {
                    long hoanthanh = GetStatusComplete(int.Parse(query.filter["id_projectteam"].ToString()), cnn);
                    long quahan = GetStatusDeadline(int.Parse(query.filter["id_projectteam"].ToString()), cnn);
                    long todo = GetStatusTodo(int.Parse(query.filter["id_projectteam"].ToString()), cnn);
                    cond.Add("hoanthanh", hoanthanh);
                    cond.Add("quahan", quahan);

                    #region Trả dữ liệu về backend để hiển thị lên giao diện
                    string sqlq = @"select id_row, id_nv, status,iIf(w.Status=@hoanthanh and w.end_date>w.deadline,1,0) as is_htquahan,
                                    iIf(w.Status=@hoanthanh and (w.end_date <= w.deadline or w.end_date is null or w.deadline is null),1,0) as is_ht,
                                    iIf(w.Status = @quahan, 1, 0) as is_quahan, deadline 
                                    from v_wework_clickup_new w where 1=1 " + strW;
                    if (displayChild == "0")
                        sqlq += " and id_parent is null";
                    DataSet ds = cnn.CreateDataSet(sqlq, cond);
                    if (cnn.LastError != null || ds == null)
                    {
                        return JsonResultCommon.Exception(_logger,cnn.LastError, _config, loginData , ControllerContext);
                    }
                    DataTable dtW = ds.Tables[0];
                    List<object> data = new List<object>();
                    int ht = (int)dtW.Compute("count(id_row)",  "is_ht=1");
                    int htm = (int)dtW.Compute("count(id_row)", " is_htquahan=1 ");
                    int dth = (int)dtW.Compute("count(id_row)", " status not in ( "+hoanthanh + "," + quahan+")");
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
                    #endregion
                }
            }
            catch (Exception ex)
            {
                return JsonResultCommon.Exception(_logger,ex, _config, loginData);
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
                    //strW += " and id_department=@id_department";
                    cond.Add("id_department", query.filter["id_department"]);
                }
                if (!string.IsNullOrEmpty(query.filter["id_projectteam"]))
                {
                    strW += " and id_project_team=@id_projectteam";
                    cond.Add("id_projectteam", query.filter["id_projectteam"]);
                }
                if (!string.IsNullOrEmpty(query.filter["status"]))//1: đang thực hiên(đang làm & phải làm)||2: đã xong
                {
                    if (query.filter["status"].ToString().Equals(1.ToString()))
                    {
                        strW += " and w.status not in (select stt.id_row from we_status stt where stt.id_project_team = @id_projectteam and IsFinal = 1)";
                    }
                    else if (query.filter["status"].ToString().Equals(2.ToString()))
                    {
                        strW += " and w.status  in (select stt.id_row from we_status stt where stt.id_project_team = @id_projectteam and IsFinal = 1)";
                    }
                }
                string displayChild = "0";//hiển thị con: 0-không hiển thị, 1- 1 cấp con, 2- nhiều cấp con
                if (!string.IsNullOrEmpty(query.filter["displayChild"]))
                    displayChild = query.filter["displayChild"];

                string ConnectionString = WeworkLiteController.getConnectionString(ConnectionCache, loginData.CustomerID, _configuration);
                using (DpsConnection cnn = new DpsConnection(ConnectionString))
                {

                    long hoanthanh = GetStatusComplete(int.Parse(query.filter["id_projectteam"].ToString()), cnn);
                    long quahan = GetStatusDeadline(int.Parse(query.filter["id_projectteam"].ToString()), cnn);
                    long todo = GetStatusTodo(int.Parse(query.filter["id_projectteam"].ToString()), cnn);
                    cond.Add("hoanthanh", hoanthanh);
                    cond.Add("quahan", quahan);

                    #region Trả dữ liệu về backend để hiển thị lên giao diện
                    string sqlq = @"select id_row, id_nv, status, CreatedDate, Deadline,iIf(w.Status = @hoanthanh and w.end_date>w.deadline,1,0) as is_htquahan,
                                    iIf(w.Status = @quahan, 1, 0) as is_quahan 
                                    from v_wework_clickup_new w where 1=1 " + strW;
                    if (displayChild == "0")
                        sqlq += " and id_parent is null";
                    DataSet ds = cnn.CreateDataSet(sqlq, cond);
                    if (cnn.LastError != null || ds == null)
                    {
                        return JsonResultCommon.Exception(_logger,cnn.LastError, _config, loginData , ControllerContext);
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
                                thuchien = (int)_as.CopyToDataTable().Compute("count(id_row)", " status =  " + todo),
                                hoanthanh = (int)_as.CopyToDataTable().Compute("count(id_row)", " status= " + hoanthanh),
                                quahan = (int)_as.CopyToDataTable().Compute("count(id_row)", " is_quahan= "+1)
                            });
                        }
                    }
                    return JsonResultCommon.ThanhCong(data);
                    #endregion
                }
            }
            catch (Exception ex)
            {
                return JsonResultCommon.Exception(_logger,ex, _config, loginData);
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
                    //strW += " and id_department=@id_department";
                    cond.Add("id_department", query.filter["id_department"]);
                }
                if (!string.IsNullOrEmpty(query.filter["id_projectteam"]))
                {
                    strW += " and id_project_team=@id_projectteam";
                    cond.Add("id_projectteam", query.filter["id_projectteam"]);
                }
                if (!string.IsNullOrEmpty(query.filter["status"]))//1: đang thực hiên(đang làm & phải làm)||2: đã xong
                {
                    if (query.filter["status"].ToString().Equals(1.ToString()))
                    {
                        strW += " and w.status not in (select stt.id_row from we_status stt where stt.id_project_team = @id_projectteam and IsFinal = 1)";
                    }
                    else if (query.filter["status"].ToString().Equals(2.ToString()))
                    {
                        strW += " and w.status  in (select stt.id_row from we_status stt where stt.id_project_team = @id_projectteam and IsFinal = 1)";
                    }
                }
                string displayChild = "0";//hiển thị con: 0-không hiển thị, 1- 1 cấp con, 2- nhiều cấp con
                if (!string.IsNullOrEmpty(query.filter["displayChild"]))
                    displayChild = query.filter["displayChild"];

                string ConnectionString = WeworkLiteController.getConnectionString(ConnectionCache, loginData.CustomerID, _configuration);
                using (DpsConnection cnn = new DpsConnection(ConnectionString))
                {
                    long hoanthanh = GetStatusComplete(int.Parse(query.filter["id_projectteam"].ToString()), cnn);
                    long quahan = GetStatusDeadline(int.Parse(query.filter["id_projectteam"].ToString()), cnn);
                    long todo = GetStatusTodo(int.Parse(query.filter["id_projectteam"].ToString()), cnn);
                    cond.Add("hoanthanh", hoanthanh);
                    cond.Add("quahan", quahan);


                    #region Trả dữ liệu về backend để hiển thị lên giao diện
                    string sqlq = @"select id_row, id_nv, status, CreatedDate, Deadline,iIf(w.Status = @hoanthanh and w.end_date>w.deadline,1,0) as is_htquahan,
                                     iIf(w.Status not in (" + hoanthanh + "," + quahan + @") , 1, 0) as dangthuchien,
                                    iIf(w.Status = @quahan, 1, 0) as is_quahan 
                                    from v_wework_clickup_new w where 1=1 " + strW;

                    if (displayChild == "0")
                        sqlq += " and id_parent is null";
                    DataSet ds = cnn.CreateDataSet(sqlq, cond);
                    if (cnn.LastError != null || ds == null)
                    {
                        return JsonResultCommon.Exception(_logger,cnn.LastError, _config, loginData , ControllerContext);
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
                                       tencot = WorkController.FirstDateOfWeek(year, g.Key).Day + " - " + WorkController.FirstDateOfWeek(year, g.Key).AddDays(6).ToString("dd/MM"),
                                       tatca = g.Count(),
                                       dangthuchien = (int)g.CopyToDataTable().Compute("count(id_row)", " dangthuchien = 1"),
                                       hoanthanh = (int)g.CopyToDataTable().Compute("count(id_row)", " status = "+hoanthanh),
                                       quahan = (int)g.CopyToDataTable().Compute("count(id_row)", " is_quahan=1 ")
                                   };
                    return JsonResultCommon.ThanhCong(Children);
                    #endregion
                }
            }
            catch (Exception ex)
            {
                return JsonResultCommon.Exception(_logger,ex, _config, loginData);
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
            //Token = "f3d23d99-8342-49fe-afbb-211f525cae73";
            //UserJWT loginData = Ulities.GetUserByHeader(HttpContext.Request.Headers);
            UserJWT loginData = Ulities.GetUserByHeader(HttpContext.Request.Headers);
            if (loginData == null)
                return JsonResultCommon.DangNhap();
            try
            {
                if (query == null)
                    query = new QueryParams();
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
                    strW += " and id_department=@id_department";
                    cond.Add("id_department", query.filter["id_department"]);
                } 
                if (!string.IsNullOrEmpty(query.filter["id_projectteam"]))
                {
                    strW += " and  id_project_team= @id_projectteam ";
                    cond.Add("id_projectteam", query.filter["id_projectteam"]);
                }
                if (!string.IsNullOrEmpty(query.filter["status"]))//1: đang thực hiện (đang làm & phải làm)||2: đã xong
                { 
                    if (query.filter["status"].ToString().Equals(1.ToString()))
                    {
                        strW += " and w.status not in (select stt.id_row from we_status stt where stt.id_project_team = @id_projectteam and IsFinal = 1)";
                    }
                    else if (query.filter["status"].ToString().Equals(2.ToString()))
                    {
                        strW += " and w.status  in (select stt.id_row from we_status stt where stt.id_project_team = @id_projectteam and IsFinal = 1)";
                    }
                }
                string displayChild = "0";//hiển thị con: 0-không hiển thị, 1- 1 cấp con, 2- nhiều cấp con
                if (!string.IsNullOrEmpty(query.filter["displayChild"]))
                    displayChild = query.filter["displayChild"];
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
                    
                    long hoanthanh = GetStatusComplete(int.Parse(query.filter["id_projectteam"].ToString()), cnn);
                    long quahan = GetStatusDeadline(int.Parse(query.filter["id_projectteam"].ToString()), cnn);
                    long todo = GetStatusTodo(int.Parse(query.filter["id_projectteam"].ToString()), cnn);
                    cond.Add("hoanthanh", hoanthanh);
                    cond.Add("quahan", quahan);


                    string queryuser = @$"select distinct u.id_user as Id_NV,'' as hoten,'' as mobile, '' as username, '' as Email, '' as image,'' as Tenchucdanh, 0 as tong,0 as ht,0 as ht_quahan,0 as quahan,0 as danglam,0 as dangdanhgia
                    from  we_project_team_user u
                    left join we_project_team p on u.id_project_team=p.id_row where  u.id_user in ({listID}) and id_project_team = " + query.filter["id_projectteam"];

                    DataTable dt = cnn.CreateDataTable(queryuser);


                    List<string> nvs = dt.AsEnumerable().Select(x => x["id_nv"].ToString()).ToList();
                    if (nvs.Count == 0)
                        return JsonResultCommon.ThanhCong(nvs);
                    #region Map info account từ JeeAccount

                    foreach (DataRow item in dt.Rows)
                    {
                        var info = DataAccount.Where(x => item["Id_NV"].ToString().Contains(x.UserId.ToString())).FirstOrDefault();

                        if (info != null)
                        {
                            item["hoten"] = info.FullName;
                            item["mobile"] = info.PhoneNumber;
                            item["username"] = info.Username;
                            item["image"] = info.AvartarImgURL;
                            item["Tenchucdanh"] = info.Jobtitle;
                        }
                    }
                    #endregion
                    string ids = string.Join(",", nvs);
                    string sqlq = @"select count(distinct p.id_row) as dem, id_user from we_project_team p 
                                    join we_project_team_user u 
                                    on p.id_row=u.id_project_team 
                                    where p.disabled=0 and u.disabled=0 " +
                                    "group by u.id_user";
                    sqlq += @";select id_row, id_nv, status, CreatedDate, Deadline,iIf(w.Status = @hoanthanh and w.end_date>w.deadline,1,0) as is_htquahan,
                                    iIf(w.Status  = @hoanthanh and (w.end_date <= w.deadline or w.end_date is null or w.deadline is null),1,0) as is_ht,
                                     iIf(w.Status not in (" + hoanthanh + "," + quahan + @") , 1, 0) as dangthuchien,
                                    iIf(w.Status = @quahan, 1, 0) as is_quahan 
                                    from v_wework_new w where 1=1  " + strW + " (parent)";
                    if (displayChild == "0")
                        sqlq = sqlq.Replace("(parent)", " and id_parent is null");
                    else
                        sqlq = sqlq.Replace("(parent)", " ");

                    DataSet ds = cnn.CreateDataSet(sqlq, cond);
                    var asP = ds.Tables[0].AsEnumerable();
                    DataTable dtW = ds.Tables[1];
                    //bool hasValue = dtW.Rows.Count > 0;

                    // get công việc 
                    //string sqlwork = @"select * from v_wework_new w where 1 =1 and" + strW;
                    //DataTable listWork = cnn.CreateDataTable(sqlwork);
                    bool hasValue = dtW.Rows.Count > 0;
                    int total = 0, success = 0;
                    foreach (DataRow dr in dt.Rows)
                    {
                        DataRow[] row = dtW.Select("id_nv = " + dr["id_nv"].ToString());
                        if (row.Length > 0)
                        {
                            dr["tong"] = total = (hasValue ? (int)dtW.Compute("count(id_nv)", "id_nv=" + dr["id_nv"].ToString()) : 0);
                            dr["ht"] = (hasValue ? (int)dtW.Compute("count(id_nv)", " is_ht=1 and id_nv=" + dr["id_nv"].ToString()) : 0);
                            dr["ht_quahan"] = hasValue ? dtW.Compute("count(id_nv)", " is_htquahan=1 and id_nv=" + dr["id_nv"].ToString()) : 0;
                            dr["quahan"] = hasValue ? dtW.Compute("count(id_nv)", " is_quahan=1 and id_nv=" + dr["id_nv"].ToString()) : 0;
                            dr["danglam"] = hasValue ? dtW.Compute("count(id_nv)", " dangthuchien=1 and id_nv=" + dr["id_nv"].ToString()) : 0;
                            dr["dangdanhgia"] = 0;
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
                                   num_project = 1,
                                   num_work = total = int.Parse(r["tong"].ToString()),
                                   danglam = int.Parse(r["danglam"].ToString()),
                                   hoanthanh = success = int.Parse(r["ht"].ToString()),
                                   dangdanhgia = int.Parse(r["dangdanhgia"].ToString()),
                                   ht_quahan = int.Parse(r["ht_quahan"].ToString()),
                                   quahan = int.Parse(r["quahan"].ToString()),
                                   percentage = total == 0 ? 0 : (success * 100 / total)
                               };
                    if ("desc".Equals(query.sortOrder))
                        data = data.OrderByDescending(x => x.num_work);
                    else
                        data = data.OrderBy(x => x.num_work);
                    // #update lại data trả về
                    return JsonResultCommon.ThanhCong(data);
                }
            }
            catch (Exception ex)
            {
                return JsonResultCommon.Exception(_logger,ex, _config, loginData);
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
                        if (query.filter["status"].ToString().Equals(1.ToString()))
                        {
                            strW += " and w.status not in (select stt.id_row from we_status stt where stt.id_project_team = @id_projectteam and IsFinal = 1)";
                        }
                        else if (query.filter["status"].ToString().Equals(2.ToString()))
                        {
                            strW += " and w.status  in (select stt.id_row from we_status stt where stt.id_project_team = @id_projectteam and IsFinal = 1)";
                        }
                    }
                    string displayChild = "0";//hiển thị con: 0-không hiển thị, 1- 1 cấp con, 2- nhiều cấp con
                    if (!string.IsNullOrEmpty(query.filter["displayChild"]))
                        displayChild = query.filter["displayChild"];
                   
                    long hoanthanh = GetStatusComplete(int.Parse(query.filter["id_projectteam"].ToString()), cnn);
                    long quahan = GetStatusDeadline(int.Parse(query.filter["id_projectteam"].ToString()), cnn);
                    long todo = GetStatusTodo(int.Parse(query.filter["id_projectteam"].ToString()), cnn);
                    cond.Add("hoanthanh", hoanthanh);
                    cond.Add("quahan", quahan);


                    string queryuser = @$"select distinct u.id_user as Id_NV,'' as hoten,'' as mobile, '' as username, '' as Email, '' as image,'' as Tenchucdanh, 0 as tong,0 as ht,0 as ht_quahan,0 as quahan,0 as danglam,0 as dangdanhgia
                    from  we_project_team_user u
                    left join we_project_team p on u.id_project_team=p.id_row where u.id_user in (" + listID + ") and id_project_team = " + query.filter["id_projectteam"];

                    DataTable dt = cnn.CreateDataTable(queryuser);

                    List<string> nvs = dt.AsEnumerable().Select(x => x["id_nv"].ToString()).ToList();
                    if (nvs.Count == 0)
                        return JsonResultCommon.ThanhCong(nvs);

                    #region Map info account từ JeeAccount

                    foreach (DataRow item in dt.Rows)
                    {
                        var info = DataAccount.Where(x => item["id_nv"].ToString().Contains(x.UserId.ToString())).FirstOrDefault();

                        if (info != null)
                        {
                            item["hoten"] = info.FullName;
                            item["mobile"] = info.PhoneNumber;
                            item["username"] = info.Username;
                            item["image"] = info.AvartarImgURL;
                            item["Tenchucdanh"] = info.Jobtitle;
                        }
                    }
                    #endregion
                    string ids = string.Join(",", nvs);
                    string sqlq = @"select count(distinct p.id_row) as dem,id_user from we_project_team p 
                                    join we_project_team_user u 
                                    on p.id_row=u.id_project_team 
                                    where p.disabled=0 and u.disabled=0   " +
                                    "group by u.id_user";

                    sqlq += @";select id_row, id_nv, status,iIf(w.Status=@hoanthanh and w.end_date>w.deadline,1,0) as is_htquahan,
                                    iIf(w.Status = @quahan, 1, 0) as is_quahan 
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
                                dr["ht"] = success = (hasValue ? (int)dtW.Compute("count(id_nv)", " status="+hoanthanh+" and id_nv=" + dr["id_nv"].ToString()) : 0);
                                dr["ht_quahan"] = hasValue ? dtW.Compute("count(id_nv)", " is_htquahan=1 and id_nv=" + dr["id_nv"].ToString()) : 0;
                                dr["quahan"] = hasValue ? dtW.Compute("count(id_nv)", " is_quahan=1 and id_nv=" + dr["id_nv"].ToString()) : 0;
                                dr["danglam"] = hasValue ? dtW.Compute("count(id_nv)", " status<>" + hoanthanh + " and id_nv=" + dr["id_nv"].ToString()) : 0;
                                dr["dangdanhgia"] = hasValue ? dtW.Compute("count(id_nv)", " status=3 and id_nv=" + dr["id_nv"].ToString()) : 0;
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
                            int slquahan = int.Parse(dr["quahan"].ToString());
                            int htmuon = int.Parse(dr["ht_quahan"].ToString());
                            if ((slquahan) <= 0 && (htmuon) <= 0)
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
                                    num_project = asP.Where(x => x["id_user"].Equals(r["id_nv"])).Select(x => x["dem"]).DefaultIfEmpty(0).First(),
                                    num_work = r["tong"],
                                    danglam = r["danglam"],
                                    hoanthanh = r["ht"],
                                    dangdanhgia = r["dangdanhgia"],
                                    ht_quahan = r["ht_quahan"],
                                    quahan = r["quahan"],
                                    percentage = (total == 0 || int.Parse(r["tong"].ToString()) == 0) ? 0 : (int.Parse(r["ht"].ToString()) * 100 / int.Parse(r["tong"].ToString()))
                                });
                    if ("excellent".Equals(query.filter["type"]))
                        data = data.OrderByDescending(x => x.hoanthanh);
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
                return JsonResultCommon.Exception(_logger,ex, _config, loginData);
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
                cond.Add("from", from);
                bool to1 = DateTime.TryParseExact(query.filter["DenNgay"], "dd/MM/yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out to);
                if (!to1)
                    return JsonResultCommon.Custom("Thời gian kết thúc không hợp lệ");
                strW += " and w." + collect_by + "<@to";
                cond.Add("to", to);
                if (!string.IsNullOrEmpty(query.filter["id_projectteam"]))
                {
                    strW += " and id_project_team=@id_projectteam";
                    strD += " and id_row=@id_projectteam";
                    cond.Add("id_projectteam", query.filter["id_projectteam"]);
                }
                if (!string.IsNullOrEmpty(query.filter["status"]))//1: đang thực hiên(đang làm & phải làm)||2: đã xong
                {
                    if (query.filter["status"].ToString().Equals(1.ToString()))
                    {
                        strW += " and w.status not in (select stt.id_row from we_status stt where stt.id_project_team = @id_projectteam and IsFinal = 1)";
                    }
                    else if (query.filter["status"].ToString().Equals(2.ToString()))
                    {
                        strW += " and w.status  in (select stt.id_row from we_status stt where stt.id_project_team = @id_projectteam and IsFinal = 1)";
                    }
                }

                string displayChild = "0";//hiển thị con: 0-không hiển thị, 1- 1 cấp con, 2- nhiều cấp con
                if (!string.IsNullOrEmpty(query.filter["displayChild"]))
                    displayChild = query.filter["displayChild"];
                string ConnectionString = WeworkLiteController.getConnectionString(ConnectionCache, loginData.CustomerID, _configuration);
                using (DpsConnection cnn = new DpsConnection(ConnectionString))
                {
                    long hoanthanh = GetStatusComplete(int.Parse(query.filter["id_projectteam"].ToString()), cnn);
                    long quahan = GetStatusDeadline(int.Parse(query.filter["id_projectteam"].ToString()), cnn);
                    long todo = GetStatusTodo(int.Parse(query.filter["id_projectteam"].ToString()), cnn);
                    cond.Add("hoanthanh", hoanthanh);
                    cond.Add("quahan", quahan); 


                    string sqlq = @"select id_row, title, deadline, urgent, important, status, clickup_prioritize as level
                                    ,iIf(w.Status = @quahan, 1, 0) as is_quahan
                                    from v_wework_clickup_new w 
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
                        dangdanhgia = hasValue ? dt.Compute("count(id_row)", "status not in ("+hoanthanh+","+quahan+")") : 0,
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
                }
            }
            catch (Exception ex)
            {
                return JsonResultCommon.Exception(_logger,ex, _config, loginData);
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
                        //strW += " and id_department=@id_department";
                        //strW1 += " and id_department=@id_department";
                        cond.Add("id_department", query.filter["id_department"]);
                    }
                    if (!string.IsNullOrEmpty(query.filter["id_projectteam"]))
                    {
                        strW += " and id_project_team=@id_projectteam";
                        strW1 += " and id_project_team=@id_projectteam";
                        cond.Add("id_projectteam", query.filter["id_projectteam"]);
                    }
                    if (!string.IsNullOrEmpty(query.filter["status"]))//1: đang thực hiên(đang làm & phải làm)||2: đã xong
                    {
                        if (query.filter["status"].ToString().Equals(1.ToString()))
                        {
                            strW += " and w.status not in (select stt.id_row from we_status stt where stt.id_project_team = @id_projectteam and IsFinal = 1)";
                        }
                        else if (query.filter["status"].ToString().Equals(2.ToString()))
                        {
                            strW += " and w.status  in (select stt.id_row from we_status stt where stt.id_project_team = @id_projectteam and IsFinal = 1)";
                        }
                    }
                    string displayChild = "0";//hiển thị con: 0-không hiển thị, 1- 1 cấp con, 2- nhiều cấp con
                    if (!string.IsNullOrEmpty(query.filter["displayChild"]))
                        displayChild = query.filter["displayChild"];

                    long hoanthanh = GetStatusComplete(int.Parse(query.filter["id_projectteam"].ToString()), cnn);
                    cond.Add("hoanthanh", hoanthanh);
                    DataTable dt = cnn.CreateDataTable(@$"select m.*, coalesce(w.tong,0) as tong,coalesce( w.ht,0) as ht , p.title as project_team, 
                                                        m.person_in_charge as Id_NV,'' as hoten,'' as mobile, '' as username, '' as Email, '' as image,'' as Tenchucdanh,'' as NguoiTao, '' as NguoiSua
                                                        from we_milestone m 
                                                        join we_project_team p on m.id_project_team=p.id_row
                                                        left join (select count(*) as tong, COUNT(CASE WHEN w.status=@hoanthanh THEN 1 END) as ht
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
                                   deadline_day = r["deadline"].Equals(DBNull.Value) ? "" : Common.GetFormatDate(Convert.ToDateTime(r["deadline"]), "dd/MM/yyyy"),
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
                return JsonResultCommon.Exception(_logger,ex, _config, loginData);
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
                        if (query.filter["status"].ToString().Equals(1.ToString()))
                        {
                            strW += " and w.status not in (select stt.id_row from we_status stt where stt.id_project_team = @id_projectteam and IsFinal = 1)";
                        }
                        else if (query.filter["status"].ToString().Equals(2.ToString()))
                        {
                            strW += " and w.status  in (select stt.id_row from we_status stt where stt.id_project_team = @id_projectteam and IsFinal = 1)";
                        }
                    }
                    string displayChild = "0";//hiển thị con: 0-không hiển thị, 1- 1 cấp con, 2- nhiều cấp con
                    if (!string.IsNullOrEmpty(query.filter["displayChild"]))
                        displayChild = query.filter["displayChild"];
                    // #update status động
                    DataTable dt_data = cnn.CreateDataTable(@$"select m.*, coalesce(w.tong,0) as tong,coalesce( w.ht,0) as ht , p.title as project_team,
                                                        m.person_in_charge as Id_NV,'' as hoten,'' as mobile, '' as username, '' as Email, '' as image,'' as Tenchucdanh,'' as NguoiTao, '' as NguoiSua from we_milestone m 
                                                        from we_milestone m 
                                                        join we_project_team p on m.id_project_team=p.id_row
                                                        left join (select count(*) as tong, COUNT(CASE WHEN w.status=2 THEN 1 END) as ht
                                                        ,w.id_milestone from v_wework_new w where 1=1 " + strW + " group by w.id_milestone) w on m.id_row=w.id_milestone " +
                                                        $"where m.Disabled=0 and m.person_in_charge in ({listID}) and and ht > 0 order by title", cond);
                    bool hasValue = dt_data.Rows.Count > 0;
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
                    DataView view = new DataView(dt_data.Copy());
                    DataTable dt = view.ToTable(true, new string[7] { "id_nv", "username", "mobile", "email", "hoten", "tong", "ht" });

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
                return JsonResultCommon.Exception(_logger,ex, _config, loginData);
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
                //if (!string.IsNullOrEmpty(query.filter["id_department"]))
                //{
                //    strW += " and id_department=@id_department";
                //    strD += " and id_row=@id_department";
                //    cond.Add("id_department", query.filter["id_department"]);
                //}
                if (!string.IsNullOrEmpty(query.filter["id_projectteam"]))
                {
                    strW += " and id_project_team=@id_projectteam";
                    strD += " and id_row=@id_projectteam";
                    cond.Add("id_projectteam", query.filter["id_projectteam"]);
                }
                if (!string.IsNullOrEmpty(query.filter["status"]))//1: đang thực hiện (đang làm & phải làm)||2: đã xong
                {
                    if (query.filter["status"].ToString().Equals(1.ToString()))
                    {
                        strW += " and w.status not in (select stt.id_row from we_status stt where stt.id_project_team = @id_projectteam and IsFinal = 1)";
                    }
                    else if (query.filter["status"].ToString().Equals(2.ToString()))
                    {
                        strW += " and w.status  in (select stt.id_row from we_status stt where stt.id_project_team = @id_projectteam and IsFinal = 1)";
                    }
                }
                string displayChild = "0";//hiển thị con: 0-không hiển thị, 1- 1 cấp con, 2- nhiều cấp con
                if (!string.IsNullOrEmpty(query.filter["displayChild"]))
                    displayChild = query.filter["displayChild"];

                string ConnectionString = WeworkLiteController.getConnectionString(ConnectionCache, loginData.CustomerID, _configuration);
                using (DpsConnection cnn = new DpsConnection(ConnectionString))
                {
                    #region Trả dữ liệu về backend để hiển thị lên giao diện
                    string sqlq = "select id_row, title from we_department d where d.disabled=0 " + strD;
                    sqlq += @";select id_row, id_nv, status, CreatedDate
                            ,Deadline, id_department 
                            from v_wework_clickup_new w where 1=1 " + strW;
                    string sql_comment = @$"select iif(sum(num_comment)>0,sum(num_comment),0) from we_work where id_row in (select id_row 
                            from v_wework_clickup_new w where 1=1 {strW})";
                    string sql_object = @" select id_user from we_project_team_user where disabled = 0 and id_project_team = "+ query.filter["id_projectteam"] + " ; " +
                                            "select count(DISTINCT id_project_team) " +
                                            "from v_wework_new w where 1 = 1 " + strW + "(child) " +
                                            "group by id_project_team";
                    if (displayChild == "0")
                    {
                        sqlq += " and id_parent is null";
                        sql_object = sql_object.Replace("(child)", " and id_parent is null");
                    }
                    DataSet ds_object = cnn.CreateDataSet(sql_object, cond);
                    DataSet ds = cnn.CreateDataSet(sqlq, cond);
                    double daynum, weeknum, work_of_week, work_of_member = 0, work_of_project = 0, comment= 0;
                    GetWeekInMonth(from, to, out daynum, out weeknum);
                    DataTable dt_work = ds.Tables[1];
                    work_of_week = dt_work.Rows.Count / weeknum;
                    if (ds_object.Tables[0].Rows.Count > 0)
                        work_of_member = work_of_week / ds_object.Tables[0].Rows.Count;
                    if (ds_object.Tables[1].Rows.Count > 0)
                        work_of_project = work_of_week / ds_object.Tables[1].Rows.Count;

                    comment = double.Parse(cnn.ExecuteScalar(sql_comment, cond).ToString());

                    if (cnn.LastError != null || ds == null)
                        return JsonResultCommon.Exception(_logger,cnn.LastError, _config, loginData , ControllerContext);
                    var
                                       data = new
                                       {
                                           week = string.Format("{0:###,##0.00}", work_of_week),
                                           member = string.Format("{0:###,##0.00}", work_of_member),
                                           project = string.Format("{0:###,##0.00}", work_of_project),
                                           numcomment = comment,
                                       };
                    return JsonResultCommon.ThanhCong(data);
                    #endregion
                }
            }
            catch (Exception ex)
            {
                return JsonResultCommon.Exception(_logger,ex, _config, loginData);
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
                //if (!string.IsNullOrEmpty(query.filter["id_department"]))
                //{
                //    strW += " and id_department=@id_department";
                //    strW1 += " and id_department=@id_department";
                //    cond.Add("id_department", query.filter["id_department"]);
                //}
                if (!string.IsNullOrEmpty(query.filter["id_projectteam"]))
                {
                    //strW += " and w.id_project_team=@id_projectteam";
                    strW1 += " and id_project_team=@id_projectteam";
                    cond.Add("id_projectteam", query.filter["id_projectteam"]);
                }
                if (!string.IsNullOrEmpty(query.filter["status"]))//1: đang thực hiên(đang làm & phải làm)||2: đã xong
                {
                    if (query.filter["status"].ToString().Equals(1.ToString()))
                    {
                        strW += " and w.status not in (select stt.id_row from we_status stt where stt.id_project_team = @id_projectteam and IsFinal = 1)";
                    }
                    else if (query.filter["status"].ToString().Equals(2.ToString()))
                    {
                        strW += " and w.status  in (select stt.id_row from we_status stt where stt.id_project_team = @id_projectteam and IsFinal = 1)";
                    }
                }
                string displayChild = "0";//hiển thị con: 0-không hiển thị, 1- 1 cấp con, 2- nhiều cấp con
                if (!string.IsNullOrEmpty(query.filter["displayChild"]))
                    displayChild = query.filter["displayChild"];
                string ConnectionString = WeworkLiteController.getConnectionString(ConnectionCache, loginData.CustomerID, _configuration);
                using (DpsConnection cnn = new DpsConnection(ConnectionString))
                {
                    string sqlq = @"select tag.id_row,tag.title from we_tag tag 
join we_project_team p on tag.id_project_team=p.id_row
where tag.Disabled=0 and p.Disabled=0 " + strW1;
                    DataTable dt = cnn.CreateDataTable(sqlq, cond);
                    sqlq = @"select distinct tag.id_row, id_work, id_tag, we_tag.title 
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
                return JsonResultCommon.Exception(_logger,ex, _config, loginData);
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

        public static long GetStatusTodo(int id_projectteam, DpsConnection cnn)
        {
            SqlConditions cond = new SqlConditions();
            cond.Add("id_projectteam", id_projectteam);
            string sql = "select stt.id_row from we_status stt where stt.id_project_team = @id_projectteam and IsTodo = 1  and Disabled = 0";
            long danglam = 0;
            if (cnn.ExecuteScalar(sql, cond) != null && cnn.LastError is null)
            {
                danglam = long.Parse(cnn.ExecuteScalar(sql, cond).ToString());
            }
            return danglam;
        }
        public static long GetStatusComplete(int id_projectteam, DpsConnection cnn)
        {
            SqlConditions cond = new SqlConditions();
            cond.Add("id_projectteam", id_projectteam);
            string sql = "select stt.id_row from we_status stt where stt.id_project_team = @id_projectteam and IsFinal = 1  and Disabled = 0";
            long hoanthanh = 0;
            if (cnn.ExecuteScalar(sql, cond) != null && cnn.LastError is null)
            {
                hoanthanh = long.Parse(cnn.ExecuteScalar(sql, cond).ToString());
            }
            return hoanthanh;
        }
        public static long GetStatusDeadline(int id_projectteam, DpsConnection cnn)
        {
            SqlConditions cond = new SqlConditions();
            cond.Add("id_projectteam", id_projectteam);
            string sql = "select stt.id_row from we_status stt where stt.id_project_team = @id_projectteam and IsDeadline = 1  and Disabled = 0";
            long deadline = 0;
            if (cnn.ExecuteScalar(sql, cond) != null && cnn.LastError is null)
            {
                deadline = long.Parse(cnn.ExecuteScalar(sql, cond).ToString());
            }
            return deadline;
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
