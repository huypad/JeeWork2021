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

namespace JeeWork_Core2021.Controllers.Wework
{
    [ApiController]
    [Route("api/work")]
    [EnableCors("JeeWorkPolicy")]
    /// <summary>
    /// api quản lý we_authorize
    /// </summary>
    // [EnableCors(origins: "*", headers: "*", methods: "*")]
    public class WorkController : ControllerBase
    {
        private readonly IHostingEnvironment _hostingEnvironment;
        private JeeWorkConfig _config;
        private IConnectionCache ConnectionCache;
        private IConfiguration _configuration;
        private INotifier _notifier;
        private readonly ILogger<WorkController> _logger;

        public WorkController(IOptions<JeeWorkConfig> config, IHostingEnvironment hostingEnvironment, IConnectionCache _cache, IConfiguration configuration, INotifier notifier, ILogger<WorkController> logger)
        {
            _hostingEnvironment = hostingEnvironment;
            _config = config.Value;
            ConnectionCache = _cache;
            _configuration = configuration;
            _notifier = notifier;
            _logger = logger;
        }
        APIModel.Models.Notify Knoti;
        [Route("my-list")]
        [HttpGet]
        public object MyList([FromQuery] QueryParams query)
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

                    string strW = " and (w.id_nv=@iduser or w.nguoigiao=@iduser)";
                    if (!string.IsNullOrEmpty(query.filter["filter"]))
                    {
                        if (query.filter["filter"] == "1")//được giao
                            strW = " and (w.id_nv=@iduser)";
                        if (query.filter["filter"] == "2")//giao đi
                        if (query.filter["filter"] == "2")//giao đi
                            strW = " and (w.nguoigiao=@iduser)";
                    }
                    DataSet ds = getWork(cnn, query, loginData.UserID, strW);
                    if (cnn.LastError != null || ds == null)
                        return JsonResultCommon.Exception(_logger,cnn.LastError, _config, loginData,ControllerContext);
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
                    Func<DateTime, int> weekProjector = d => CultureInfo.CurrentCulture.Calendar.GetWeekOfYear(d, CalendarWeekRule.FirstFourDayWeek, DayOfWeek.Monday);
                    var Children = from rr in dtNew
                                   group rr by weekProjector((DateTime)rr["CreatedDate"]) into g
                                   select new
                                   {
                                       id_row = g.Key,
                                       start = FirstDateOfWeek(2020, g.Key),
                                       end = FirstDateOfWeek(2020, g.Key).AddDays(6),
                                       Children = getChild(domain, loginData.CustomerID, "", displayChild, g.Key, g.Concat(dtChild).CopyToDataTable().AsEnumerable(), tags)
                                   };
                    return JsonResultCommon.ThanhCong(Children, pageModel, Visible);
                }
            }
            catch (Exception ex)
            {
                return JsonResultCommon.Exception(_logger,ex, _config, loginData);
            }
        }

        [Route("my-staff-list")]
        [HttpGet]
        public object MyStaffList([FromQuery] QueryParams query)
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
                DataTable dt = null;
                using (DpsConnection cnn = new DpsConnection(_config.HRConnectionString))
                {
                    dt = Common.GetListByManager(loginData.UserID.ToString(), cnn);//id_nv, hoten...
                }
                string ConnectionString = WeworkLiteController.getConnectionString(ConnectionCache, loginData.CustomerID, _configuration);
                using (DpsConnection cnn = new DpsConnection(ConnectionString))
                {
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
                    string ids = string.Join(",", dt.AsEnumerable().Select(x => x["id_nv"].ToString()));
                    if (ids == "")
                        return JsonResultCommon.Exception(_logger,cnn.LastError, _config, loginData,ControllerContext);
                    string strW = " and w.id_nv in (" + ids + ")";
                    DataSet ds = getWork(cnn, query, loginData.UserID, strW);
                    if (cnn.LastError != null || ds == null)
                        return JsonResultCommon.Exception(_logger,cnn.LastError, _config, loginData,ControllerContext);
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
                    Func<DateTime, int> weekProjector = d => CultureInfo.CurrentCulture.Calendar.GetWeekOfYear(d, CalendarWeekRule.FirstFourDayWeek, DayOfWeek.Monday);
                    var Children = from rr in dtNew
                                   group rr by weekProjector((DateTime)rr["CreatedDate"]) into g
                                   select new
                                   {
                                       id_row = g.Key,
                                       start = FirstDateOfWeek(2020, g.Key),
                                       end = FirstDateOfWeek(2020, g.Key).AddDays(6),
                                       Children = getChild(domain, loginData.CustomerID, "", displayChild, g.Key, g.Concat(dtChild).CopyToDataTable().AsEnumerable(), tags)
                                   };
                    return JsonResultCommon.ThanhCong(Children, pageModel, Visible);
                }
            }
            catch (Exception ex)
            {
                return JsonResultCommon.Exception(_logger,ex, _config, loginData);
            }
        }

        [Route("List-by-project")]
        [HttpGet]
        public object ListByProject([FromQuery] QueryParams query)
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
                    #region group
                    string columnName = "";
                    string strTemp = "";
                    string strG = "";
                    //nhóm theo: 1-work group, 2-milestone, 3-member
                    switch (query.filter["groupby"])
                    {
                        case "2":
                            columnName = "id_milestone";
                            strTemp = "No milestone";
                            strG = @"select id_row, title from we_milestone where disabled=0";
                            break;
                        case "3":
                            columnName = "id_nv";
                            strTemp = "Not assigned";
                            strG = $"select distinct id_user as id_row,nv.holot+' '+ nv.ten as title from we_project_team_user u join {_config.HRCatalog}.dbo.Tbl_Nhanvien nv on u.id_user=nv.Id_NV where u.disabled=0";
                            break;
                        default:
                            columnName = "id_group";
                            strTemp = "Chưa phân loại";
                            strG = @$"select id_row, title,id_nv, username from we_group g
left join {_config.HRCatalog}.dbo.Tbl_Account acc on g.reviewer=acc.id_nv where disabled=0";
                            break;
                    }
                    strG = " select null as id_row, N'" + strTemp + "' as title" + (columnName == "id_group" ? ", null as id_nv,null as username" : "") + " union " + strG + " and id_project_team=" + query.filter["id_project_team"];
                    #endregion
                    DataTable dtG = cnn.CreateDataTable(strG);
                    if (dtG.Rows.Count == 0)
                        return JsonResultCommon.ThanhCong(new List<string>(), null, Visible);
                    DataSet ds = getWork(cnn, query, loginData.UserID);
                    if (cnn.LastError != null || ds == null)
                        return JsonResultCommon.Exception(_logger,cnn.LastError, _config, loginData,ControllerContext);
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
                    dtNew = dtNew.Concat(dtChild);
                    int tong = 0, ht = 0;
                    var Children = from rr in dtG.AsEnumerable()
                                   select new
                                   {
                                       id = rr["id_row"],
                                       title = rr["title"],
                                       reviewer = columnName != "id_group" ? null : (rr["id_nv"] == DBNull.Value ? null : new
                                       {
                                           id_nv = rr["id_nv"],
                                           username = rr["username"]
                                       }),
                                       Count = new
                                       {
                                           tong = tong = (int)temp.Count(r => r[columnName].Equals(rr["id_row"])),
                                           ht = ht = (int)temp.Count(r => r[columnName].Equals(rr["id_row"]) && r["status"].ToString() == "2"),
                                           percentage = tong == 0 ? 0 : (ht * 100 / tong)
                                       },
                                       Children = getChild(domain, loginData.CustomerID, columnName, displayChild, rr["id_row"], dtNew.CopyToDataTable().AsEnumerable(), tags)
                                   };
                    return JsonResultCommon.ThanhCong(Children, pageModel, Visible);
                }
            }
            catch (Exception ex)
            {
                return JsonResultCommon.Exception(_logger,ex, _config, loginData);
            }
        }

        [Route("List-by-user")]
        [HttpGet]
        public object ListByUser([FromQuery] QueryParams query)
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
                    string displayChild = "0";//hiển thị con: 0-không hiển thị, 1- 1 cấp con, 2- nhiều cấp con
                    if (!string.IsNullOrEmpty(query.filter["displayChild"]))
                        displayChild = query.filter["displayChild"];
                    #region group
                    string columnName = "id_project_team";
                    string strG = @"select p.id_row, p.title from we_project_team_user u
join we_project_team p on p.id_row=u.id_project_team where u.disabled=0 and p.Disabled=0 and id_user=" + query.filter["id_nv"];
                    #endregion
                    DataTable dtG = cnn.CreateDataTable(strG);
                    if (dtG.Rows.Count == 0)
                        return JsonResultCommon.ThanhCong(new List<string>(), null, Visible);
                    DataSet ds = getWork(cnn, query, loginData.UserID);
                    if (cnn.LastError != null || ds == null)
                        return JsonResultCommon.Exception(_logger,cnn.LastError, _config, loginData,ControllerContext);
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
                    dtNew = dtNew.Concat(dtChild);
                    int tong = 0, ht = 0;
                    var Children = from rr in dtG.AsEnumerable()
                                   select new
                                   {
                                       id = rr["id_row"],
                                       title = rr["title"],
                                       reviewer = columnName != "id_group" ? null : (rr["id_nv"] == DBNull.Value ? null : new
                                       {
                                           id_nv = rr["id_nv"],
                                           username = rr["username"]
                                       }),
                                       Count = new
                                       {
                                           tong = tong = (int)temp.Count(r => r[columnName].Equals(rr["id_row"])),
                                           ht = ht = (int)temp.Count(r => r[columnName].Equals(rr["id_row"]) && r["status"].ToString() == "2"),
                                           percentage = tong == 0 ? 0 : (ht * 100 / tong)
                                       },
                                       Children = getChild(domain, loginData.CustomerID, columnName, displayChild, rr["id_row"], dtNew.CopyToDataTable().AsEnumerable(), tags)
                                   };
                    return JsonResultCommon.ThanhCong(Children, pageModel, Visible);
                }
            }
            catch (Exception ex)
            {
                return JsonResultCommon.Exception(_logger,ex, _config, loginData);
            }
        }
       
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
                    string strG = @"select p.id_row, p.title from we_project_team_user u
join we_project_team p on p.id_row=u.id_project_team where u.disabled=0 and p.Disabled=0 and id_user=" + query.filter["id_nv"];
                    DataTable dtG = cnn.CreateDataTable(strG);
                    DataSet ds = getWork(cnn, query, loginData.UserID);
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
                return BadRequest(JsonResultCommon.Exception(_logger,ex, _config, loginData));
            }
        }
        [Route("list-by-filter")]
        [HttpGet]
        public object ListByFilter([FromQuery] QueryParams query)
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
                    if (string.IsNullOrEmpty(query.filter["id_filter"]))
                        return JsonResultCommon.Custom("Filter bắt buộc nhập");

                    SqlConditions Conds = new SqlConditions();

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
                    string dieukien_where = FilterWorkController.genStringWhere(cnn, loginData.UserID, query.filter["id_filter"]);
                    //if (string.IsNullOrEmpty(dieukien_where))
                    //    return JsonResultCommon.KhongTonTai("Filter");
                    DataSet ds = getWork(cnn, query, loginData.UserID, dieukien_where);
                    if (cnn.LastError != null || ds == null)
                        return JsonResultCommon.Exception(_logger,cnn.LastError, _config, loginData,ControllerContext);
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
                    Func<DateTime, int> weekProjector = d => CultureInfo.CurrentCulture.Calendar.GetWeekOfYear(d, CalendarWeekRule.FirstFourDayWeek, DayOfWeek.Monday);
                    var Children = from rr in dtNew
                                   group rr by weekProjector((DateTime)rr["CreatedDate"]) into g
                                   select new
                                   {
                                       id_row = g.Key,
                                       start = FirstDateOfWeek(2020, g.Key),
                                       end = FirstDateOfWeek(2020, g.Key).AddDays(6),
                                       Children = getChild(domain, loginData.CustomerID, "", displayChild, g.Key, g.Concat(dtChild).CopyToDataTable().AsEnumerable(), tags)
                                   };
                    return JsonResultCommon.ThanhCong(Children, pageModel, Visible);
                }
            }
            catch (Exception ex)
            {
                return JsonResultCommon.Exception(_logger,ex, _config, loginData);
            }
        }
        [Route("period-view")]
        [HttpGet]
        public object PeriodView([FromQuery] QueryParams query)
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
                    #region group
                    string columnName = "";
                    string strTemp = "";
                    string strG = "";
                    //nhóm theo: 1-work group, 2-milestone, 3-member
                    switch (query.filter["groupby"])
                    {
                        case "2":
                            columnName = "id_milestone";
                            strTemp = "No milestone";
                            strG = @"select id_row, title from we_milestone where disabled=0";
                            break;
                        case "3":
                            columnName = "id_nv";
                            strTemp = "Not assigned";
                            strG = $"select distinct id_user as id_row,nv.holot+' '+ nv.ten as title from we_project_team_user u join {_config.HRCatalog}.dbo.Tbl_Nhanvien nv on u.id_user=nv.Id_NV where u.disabled=0";
                            break;
                        default:
                            columnName = "id_group";
                            strTemp = "Chưa phân loại";
                            strG = @$"select id_row, title,id_nv, username from we_group g
                                    left join {_config.HRCatalog}.dbo.Tbl_Account acc on g.reviewer=acc.id_nv where disabled=0";
                            break;
                    }
                    strG = " select null as id_row, N'" + strTemp + "' as title" + (columnName == "id_group" ? ", null as id_nv,null as username" : "") + " union " + strG + " and id_project_team=" + query.filter["id_project_team"];
                    #endregion
                    DataTable dtG = cnn.CreateDataTable(strG);
                    if (dtG.Rows.Count == 0)
                        return JsonResultCommon.ThanhCong(new List<string>(), null, Visible);
                    DataSet ds = getWork(cnn, query, loginData.UserID);
                    if (cnn.LastError != null || ds == null)
                        return JsonResultCommon.Exception(_logger,cnn.LastError, _config, loginData,ControllerContext);
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
                    dtNew = dtNew.Concat(dtChild);
                    int tong = 0, ht = 0;
                    var Children = from rr in dtG.AsEnumerable()
                                   select new
                                   {
                                       id = rr["id_row"],
                                       title = rr["title"],
                                       reviewer = columnName != "id_group" ? null : (rr["id_nv"] == DBNull.Value ? null : new
                                       {
                                           id_nv = rr["id_nv"],
                                           username = rr["username"]
                                       }),
                                       Count = new
                                       {
                                           tong = tong = (int)temp.Count(r => r[columnName].Equals(rr["id_row"])),
                                           ht = ht = (int)temp.Count(r => r[columnName].Equals(rr["id_row"]) && r["status"].ToString() == "2"),
                                           percentage = tong == 0 ? 0 : (ht * 100 / tong)
                                       },
                                       Children = getChild(domain, loginData.CustomerID, columnName, displayChild, rr["id_row"], dtNew.CopyToDataTable().AsEnumerable(), tags)
                                   };
                    return JsonResultCommon.ThanhCong(Children, pageModel, Visible);
                }
            }
            catch (Exception ex)
            {
                return JsonResultCommon.Exception(_logger,ex, _config, loginData);
            }
        }
        [Route("stream-view")]
        [HttpGet]
        public object Streamview([FromQuery] QueryParams query)
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
                    DataSet ds = getWork(cnn, query, loginData.UserID);
                    if (cnn.LastError != null || ds == null)
                        return JsonResultCommon.Exception(_logger,cnn.LastError, _config, loginData,ControllerContext);
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
                    Func<DateTime, int> weekProjector = d => CultureInfo.CurrentCulture.Calendar.GetWeekOfYear(d, CalendarWeekRule.FirstFourDayWeek, DayOfWeek.Monday);
                    var Children = from rr in dtNew
                                   group rr by weekProjector((DateTime)rr["CreatedDate"]) into g
                                   select new
                                   {
                                       id_row = g.Key,
                                       start = FirstDateOfWeek(2020, g.Key),
                                       end = FirstDateOfWeek(2020, g.Key).AddDays(6),
                                       Children = getChild(domain, loginData.CustomerID, "", displayChild, g.Key, g.Concat(dtChild).CopyToDataTable().AsEnumerable(), tags)
                                   };
                    return JsonResultCommon.ThanhCong(Children, pageModel, Visible);
                }
            }
            catch (Exception ex)
            {
                return JsonResultCommon.Exception(_logger,ex, _config, loginData);
            }
        }

        [Route("gantt-view")]
        [HttpGet]
        public object Ganttview([FromQuery] QueryParams query)
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
                    DataSet ds = getWork(cnn, query, loginData.UserID);
                    if (cnn.LastError != null || ds == null)
                        return JsonResultCommon.Exception(_logger,cnn.LastError, _config, loginData,ControllerContext);
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
                                           status = rr["start_date"] == DBNull.Value ? "TODO" : (rr["status"].ToString() == "2" ? "DONE" : (rr["status"].ToString() == "3" ? "REVIEW" : "DOING"))
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
                                           status = rr["start_date"] == DBNull.Value ? "TODO" : (rr["status"].ToString() == "2" ? "DONE" : (rr["status"].ToString() == "3" ? "REVIEW" : "DOING")),
                                       });
                    double ms = 0;
                    var items = (from rr in dtNew.AsEnumerable()
                                 select new
                                 {
                                     id = rr["id_row"],
                                     rowId = "W" + rr["id_row"],
                                     label = rr["title"],
                                     style = getStyle(rr),
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
                                             style = getStyle(rr),
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
                return JsonResultCommon.Exception(_logger,ex, _config, loginData);
            }
        }

        [Route("list-following")]
        [HttpGet]
        public object ListFollowing([FromQuery] QueryParams query)
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
                    string sql = " select id_work from we_work_user where loai=2 and disabled=0 and id_user=" + loginData.UserID;
                    DataTable dtF = cnn.CreateDataTable(sql);
                    if (dtF == null || dtF.Rows.Count == 0)
                        return JsonResultCommon.ThanhCong(new List<string>());
                    DataSet ds = getWork(cnn, query, loginData.UserID);
                    if (cnn.LastError != null || ds == null)
                        return JsonResultCommon.Exception(_logger,cnn.LastError, _config, loginData,ControllerContext);
                    //var ss = (from f in dtF.AsEnumerable()
                    //          join w in ds.Tables[0].AsEnumerable() on f["id_work"].equals(w["id_row"])
                    //                || f["id_work"].equals(w["id_parent"])// Your join
                    //         select f);
                    var dtJ = dtF.AsEnumerable().Join(ds.Tables[0].AsEnumerable(), f => new { a = f["id_work"]/*, b = f["id_work"]*/ }, w => new { a = w["id_row"]/*, b = w["id_parent"]*/ }, (f, w) => w);
                    if (dtJ.Count() == 0)
                        return JsonResultCommon.ThanhCong(new List<string>());
                    var dt = dtJ.CopyToDataTable();
                    var temp = filterWork(dt.AsEnumerable().Where(x => x["id_parent"] == DBNull.Value), query.filter);//k bao gồm con
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
                    Func<DateTime, int> weekProjector = d => CultureInfo.CurrentCulture.Calendar.GetWeekOfYear(d, CalendarWeekRule.FirstFourDayWeek, DayOfWeek.Monday);
                    var Children = from rr in dtNew
                                   group rr by weekProjector((DateTime)rr["CreatedDate"]) into g
                                   select new
                                   {
                                       id_row = g.Key,
                                       start = FirstDateOfWeek(2020, g.Key),
                                       end = FirstDateOfWeek(2020, g.Key).AddDays(6),
                                       Children = getChild(domain, loginData.CustomerID, "", displayChild, g.Key, g.Concat(dtChild).CopyToDataTable().AsEnumerable(), tags)
                                   };
                    return JsonResultCommon.ThanhCong(Children, pageModel, Visible);
                }
            }
            catch (Exception ex)
            {
                return JsonResultCommon.Exception(_logger,ex, _config, loginData);
            }
        }

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
                    #region Trả dữ liệu về backend để hiển thị lên giao diện
                    string sqlq = @$"select w.*, nv.holot+' '+nv.ten as hoten_nguoigiao, Iif(fa.id_row is null ,0,1) as favourite,
iIf(w.Status=2 and w.end_date>w.deadline,1,0) as is_htquahan,
iIf(w.Status = 2 and w.end_date <= w.deadline, 1, 0) as is_htdunghan ,
iIf(w.Status = 1 and  w.start_date is not null, 1, 0) as is_danglam,
iIf(w.Status = 1 and getdate() > w.deadline, 1, 0) as is_quahan,
iif(convert(varchar, w.deadline,103) like convert(varchar, GETDATE(),103),1,0) as duetoday,
iif(w.status=1 and w.start_date is null,1,0) as require,
tao.UserName as NguoiTao, sua.Username as NguoiSua from v_wework w 
left join {_config.HRCatalog}.dbo.Tbl_Nhanvien nv on w.nguoigiao = nv.id_nv
join {_config.HRCatalog}.dbo.Tbl_Account tao on tao.id_nv=w.CreatedBy
left join {_config.HRCatalog}.dbo.Tbl_Account sua on sua.id_nv=w.UpdatedBy
left join we_work_favourite fa on fa.id_work=w.id_row and fa.createdby=" + loginData.UserID + @" and fa.disabled=0
 where w.id_row= " + id + " or id_parent=" + id;
                    //tag
                    sqlq += @";select a.title, a.id_row, a.color from we_tag a join we_work_tag b on a.id_row=b.id_tag 
where a.disabled=0 and b.disabled = 0 and id_work = " + id;
                    //người theo dõi
                    sqlq += @$";select id_work,nv.* from we_work_user u 
join {_config.HRCatalog}.dbo.v_account nv on u.id_user = nv.id_nv
where u.disabled = 0 and u.loai = 2 and id_work=" + id;
                    //attachment
                    sqlq += @$";select a.*, nv.username from we_attachment a
join {_config.HRCatalog}.dbo.Tbl_Account nv on a.createdby = nv.id_nv where Disabled=0 and object_type in (1,11) and object_id=" + id;
                    //checklist
                    //                    sqlq += @";select * from we_checklist where Disabled=0 and id_work=" + id;
                    //                    sqlq += @";select i.*, l.title as checklist, acc.* from we_checklist l join we_checklist_item i on l.id_row=i.id_checklist
                    //left join {_config.HRCatalog}.dbo.v_account acc on checker=acc.id_nv where l.Disabled=0 and i.disabled=0 and id_work=" + id;

                    DataSet ds = cnn.CreateDataSet(sqlq);
                    if (cnn.LastError != null || ds == null)
                    {
                        return JsonResultCommon.Exception(_logger,cnn.LastError, _config, loginData,ControllerContext);
                    }
                    if (ds.Tables[0] == null || ds.Tables[0].Rows.Count == 0)
                        return JsonResultCommon.KhongTonTai();

                    sqlq = @$"exec getobjectactivities 1,{id},{_config.HRCatalog}";
                    DataTable dtLog = cnn.CreateDataTable(sqlq);
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
                                    assign = r["id_nv"] == DBNull.Value ? null : new
                                    {
                                        id_nv = r["id_nv"],
                                        hoten = r["hoten"],
                                        username = r["username"],
                                        tenchucdanh = r["tenchucdanh"],
                                        mobile = r["mobile"],
                                        image = r["image"],
                                    },
                                    Followers = from f in ds.Tables[2].AsEnumerable()
                                                select new
                                                {
                                                    id_nv = f["id_nv"],
                                                    hoten = f["hoten"],
                                                    username = f["username"],
                                                    tenchucdanh = f["tenchucdanh"],
                                                    mobile = f["mobile"],
                                                    image = r["image"],
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
                                                     }
                                                 },
                                    //CheckLists = from dr in ds.Tables[4].AsEnumerable()
                                    //             select new
                                    //             {
                                    //                 id_row=dr["id_row"],
                                    //                 title=dr["title"],
                                    //                 items=from item in ds.Tables[5].AsEnumerable()
                                    //                       where item["id_checklist"].Equals(dr["id_row"])
                                    //                       select new
                                    //                       {
                                    //                           id_row=item["id_row"],
                                    //                           title=item["title"],
                                    //                           @checked=item["checked"],
                                    //                           CreatedBy = item["CreatedBy"],
                                    //                           CreatedDate = string.Format("{0:dd/MM/yyyy HH:mm}", item["CreatedDate"]),
                                    //                           checker = new
                                    //                           {
                                    //                               id_nv = item["id_nv"],
                                    //                               hoten = item["hoten"],
                                    //                               username = item["username"],
                                    //                               mobile = item["mobile"],
                                    //                               image = WeworkLiteController.genLinkImage(domain,loginData.CustomerID, item["id_nv"].ToString())
                                    //                           },
                                    //                       }
                                    //             },
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
                                                 assign = rr["id_nv"] == DBNull.Value ? null : new
                                                 {
                                                     id_nv = rr["id_nv"],
                                                     hoten = rr["hoten"],
                                                     username = rr["username"],
                                                     tenchucdanh = rr["tenchucdanh"],
                                                     mobile = rr["mobile"],
                                                     image = rr["image"],
                                                 }
                                             }
                                }).FirstOrDefault();
                    return JsonResultCommon.ThanhCong(data);
                }
                #endregion
            }
            catch (Exception ex)
            {
                
                
                return JsonResultCommon.Exception(_logger,ex, _config, loginData);
            }
        }

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
                    if (data.deadline > DateTime.MinValue)
                        val.Add("deadline", data.deadline);
                    if (data.id_group > 0)
                        val.Add("id_group", data.id_group);
                    if (data.id_milestone > 0)
                        val.Add("id_milestone", data.id_milestone);
                    val.Add("CreatedDate", DateTime.Now);
                    val.Add("CreatedBy", iduser);
                    //string strCheck = "select count(*) from we_work where (id_project_team=@id_project_team) and title=@name";
                    //if (int.Parse(cnn.ExecuteScalar(strCheck, new SqlConditions() { { "id_project_team", data.id_project_team }, { "name", data.title } }).ToString()) > 0)
                    //{
                    //    return JsonResultCommon.Custom("công việc đã tồn tại trong dự án/phòng ban");
                    //}
                    cnn.BeginTransaction();
                    if (cnn.Insert(val, "we_work") != 1)
                    {
                        cnn.RollbackTransaction();
                        return JsonResultCommon.Exception(_logger,cnn.LastError, _config, loginData,ControllerContext);
                    }
                    long idc = long.Parse(cnn.ExecuteScalar("select IDENT_CURRENT('we_work')").ToString());
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
                                return JsonResultCommon.Exception(_logger,cnn.LastError, _config, loginData,ControllerContext);
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
                                return JsonResultCommon.Exception(_logger,cnn.LastError, _config, loginData,ControllerContext);
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
                                return JsonResultCommon.Exception(_logger,cnn.LastError, _config, loginData,ControllerContext);
                            }
                        }
                    }
                    //string LogContent = "", LogEditContent = "";
                    //LogContent = LogEditContent = "Thêm mới dữ liệu work: title=" + data.title + ", id_project_team=" + data.id_project_team;
                    //DpsPage.Ghilogfile(loginData.CustomerID.ToString(), LogEditContent, LogContent, loginData.UserName);
                    if (!WeworkLiteController.log(_logger, loginData.Username, cnn, 1, idc, iduser, data.title))
                    {
                        cnn.RollbackTransaction();
                        return JsonResultCommon.Exception(_logger,cnn.LastError, _config, loginData,ControllerContext);
                    }
                    cnn.EndTransaction();
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
                    }
                    #endregion
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
                    cnn.BeginTransaction();
                    if (cnn.Update(val, sqlcond, "we_work") != 1)
                    {
                        cnn.RollbackTransaction();
                        return JsonResultCommon.Exception(_logger,cnn.LastError, _config, loginData,ControllerContext);
                    }
                    #region Chỉnh sửa (Không cho sửa User)
                    //string ids = string.Join(",", data.Users.Where(x => x.loai == 1 && x.id_row > 0).Select(x => x.id_row));
                    //if (ids != "")
                    //{
                    //    string strDel = "Update we_work_user set Disabled=1, UpdatedDate=getdate(), UpdatedBy=" + iduser + " where Disabled=0 and loai=1 and id_work=" + data.id_row + " and id_row not in (" + ids + ")";
                    //    if (cnn.ExecuteNonQuery(strDel) < 0)
                    //    {
                    //        cnn.RollbackTransaction();
                    //        return JsonResultCommon.Exception(_logger,cnn.LastError, _config, loginData,ControllerContext);
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
                    //            return JsonResultCommon.Exception(_logger,cnn.LastError, _config, loginData,ControllerContext);
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
                            return JsonResultCommon.Exception(_logger,cnn.LastError, _config, loginData,ControllerContext);
                        }
                    }
                    if (vals[1])
                    {
                        if (!WeworkLiteController.log(_logger, loginData.Username, cnn, 16, data.id_row, iduser, "", old.Rows[0]["description"], data.description))
                        {
                            cnn.RollbackTransaction();
                            return JsonResultCommon.Exception(_logger,cnn.LastError, _config, loginData,ControllerContext);
                        }
                    }
                    if (vals[2])
                    {
                        if (!WeworkLiteController.log(_logger, loginData.Username, cnn, 12, data.id_row, iduser, "", old.Rows[0]["id_group"], data.id_group))
                        {
                            cnn.RollbackTransaction();
                            return JsonResultCommon.Exception(_logger,cnn.LastError, _config, loginData,ControllerContext);
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
                    }
                    #endregion
                    return JsonResultCommon.ThanhCong(data);
                }
            }
            catch (Exception ex)
            {
                return JsonResultCommon.Exception(_logger,ex, _config, loginData);
            }
        }

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
                    SqlConditions sqlcond = new SqlConditions();
                    sqlcond.Add("id_row", data.id_row);
                    sqlcond.Add("disabled", 0);
                    string s = "select title as tencongviec_old, * from v_wework where (where)";
                    DataTable old = cnn.CreateDataTable(s, "(where)", sqlcond);
                    if (old == null || old.Rows.Count == 0)
                        return JsonResultCommon.KhongTonTai("Công việc");
                    long iduser = loginData.UserID;
                    long idk = loginData.CustomerID;
                    object workname = cnn.ExecuteScalar("select title from we_work where id_row = @id_row", new SqlConditions() { { "id_row", data.id_row } });
                    if (workname != null)
                        workname = workname.ToString();
                    if (data.key != "Tags" && data.key != "Attachments" && data.key != "assign")
                    {
                        Hashtable val = new Hashtable();
                        val.Add("UpdatedDate", DateTime.Now);
                        val.Add("UpdatedBy", iduser);
                        cnn.BeginTransaction();
                        if (data.value == null)
                            val.Add(data.key, DBNull.Value);
                        else
                            val.Add(data.key, data.value);
                        if (cnn.Update(val, sqlcond, "we_work") != 1)
                        {
                            cnn.RollbackTransaction();
                            return JsonResultCommon.Exception(_logger,cnn.LastError, _config, loginData,ControllerContext);
                        }
                        if (data.key == "title")
                        {
                            DataTable dt_user = cnn.CreateDataTable("select id_nv, title, id_row from v_wework where (where)", "(where)", sqlcond);
                            if (dt_user.Rows.Count > 0)
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
                                }
                                #endregion
                            }
                        }
                        if (data.key == "deadline")
                        {
                            DataTable dt_user = cnn.CreateDataTable("select id_nv, title, id_row from v_wework where (where)", "(where)", sqlcond);
                            if (dt_user.Rows.Count > 0)
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
                                }
                                #endregion
                            }
                        }
                        if (data.key == "status")
                        {
                            DataTable dt_user = cnn.CreateDataTable("select id_nv, title, id_row from v_wework where (where)", "(where)", sqlcond);
                            if (dt_user.Rows.Count > 0)
                            {
                                var users = new List<long> { long.Parse(dt_user.Rows[0]["id_nv"].ToString()) };
                                cnn.EndTransaction();
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
                                    }
                                    #endregion
                                }
                            }
                        }
                    }
                    else
                    {
                        if (data.key == "assign")//assign cho 1 người mới hoặc xóa
                        {
                            string strDel = "Update we_work_user set Disabled=1, UpdatedDate=getdate(), UpdatedBy=" + iduser + " where Disabled=0 and loai=1 and id_work=" + data.id_row;
                            if (cnn.ExecuteNonQuery(strDel) < 0)
                            {
                                cnn.RollbackTransaction();
                                return JsonResultCommon.Exception(_logger,cnn.LastError, _config, loginData,ControllerContext);
                            }
                            if (data.value != null)
                            {
                                Hashtable val1 = new Hashtable();
                                val1["id_work"] = data.id_row;
                                val1["CreatedDate"] = DateTime.Now;
                                val1["CreatedBy"] = iduser;
                                val1["id_user"] = data.value;
                                val1["loai"] = 1;
                                if (cnn.Insert(val1, "we_work_user") != 1)
                                {
                                    cnn.RollbackTransaction();
                                    return JsonResultCommon.Exception(_logger,cnn.LastError, _config, loginData,ControllerContext);
                                }
                            }
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
                            }
                            #endregion
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
                                    return JsonResultCommon.Exception(_logger,cnn.LastError, _config, loginData,ControllerContext);
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
                                    return JsonResultCommon.Exception(_logger,cnn.LastError, _config, loginData,ControllerContext);
                                }
                            }
                            //return JsonResultCommon.Custom("Tag đang được sử dụng cho công việc này");


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
                                        return JsonResultCommon.Exception(_logger,cnn.LastError, _config, loginData,ControllerContext);
                                    }
                                }
                            }
                        }
                    }

                    DataTable dt = cnn.CreateDataTable(s, "(where)", sqlcond);
                    //string LogContent = "", LogEditContent = "";
                    //LogEditContent = DpsPage.GetEditLogContent(old, dt);
                    //if (!LogEditContent.Equals(""))
                    //{
                    //    LogEditContent = "Chỉnh sửa dữ liệu (" + data.id_row + ") : " + LogEditContent;
                    //    LogContent = "Chỉnh sửa dữ liệu work (" + data.id_row + "), Chi tiết xem trong log chỉnh sửa chức năng";
                    //}
                    //DpsPage.Ghilogfile(loginData.CustomerID.ToString(), LogEditContent, LogContent, loginData.UserName);
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
                            return JsonResultCommon.Exception(_logger,cnn.LastError, _config, loginData,ControllerContext);
                        }
                    }
                    cnn.EndTransaction();
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
                    string sqlq = "select ISNULL((select count(*) from we_work where disabled=0 and id_row = " + id + "),0)";
                    if (long.Parse(cnn.ExecuteScalar(sqlq).ToString()) != 1)
                        return JsonResultCommon.KhongTonTai("Công việc");
                    //if (Common.TestDuplicate("", id.ToString(), "-1", "v_wework", "id_milestone", "", "-1", cnn, "", true) == false)
                    //{
                    //    return JsonResultCommon.Custom("Đang có công việc thuộc mục tiêu này nên không thể xóa");
                    //}
                    sqlq = "update we_work set Disabled=1, UpdatedDate=getdate(), UpdatedBy=" + iduser + " where id_row = " + id;
                    SqlConditions sqlcond = new SqlConditions();
                    sqlcond.Add("id_row", id);
                    sqlcond.Add("disabled", 0);
                    DataTable dt_user = cnn.CreateDataTable("select id_nv, title, id_row from v_wework where (where)", "(where)", sqlcond);
                    cnn.BeginTransaction();
                    if (cnn.ExecuteNonQuery(sqlq) != 1)
                    {
                        cnn.RollbackTransaction();
                        return JsonResultCommon.Exception(_logger,cnn.LastError, _config, loginData,ControllerContext);
                    }
                    //string LogContent = "Xóa dữ liệu work (" + id + ")";
                    //DpsPage.Ghilogfile(loginData.CustomerID.ToString(), LogContent, LogContent, loginData.UserName);
                    if (!WeworkLiteController.log(_logger, loginData.Username, cnn, 18, id, iduser))
                    {
                        cnn.RollbackTransaction();
                        return JsonResultCommon.Exception(_logger,cnn.LastError, _config, loginData,ControllerContext);
                    }
                    cnn.EndTransaction();
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
                        }
                        #endregion
                    }
                    return JsonResultCommon.ThanhCong();
                }
            }
            catch (Exception ex)
            {
                return JsonResultCommon.Exception(_logger,ex, _config, loginData);
            }
        }

        /// <summary>
        /// 
        /// <param name="id"></param>
        /// <returns></returns>
        [Route("reassign")]
        [HttpGet]
        public BaseModel<object> reassign(long id, long id_user)
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
                    string sql = "select * from we_work_user where disabled=0 and id_work=" + id + " and id_user=" + id_user;
                    DataTable dt = cnn.CreateDataTable(sql);
                    if (dt.Rows.Count == 0)
                        return JsonResultCommon.Custom("Bạn không là người thực hiện công việc");
                    Hashtable val = new Hashtable();
                    val["id_work"] = id;
                    val["id_user"] = id_user;
                    val["loai"] = 1;
                    val["createdBy"] = loginData.UserID;
                    val["CreatedDate"] = DateTime.Now;
                    cnn.BeginTransaction();
                    if (cnn.Insert(val, "we_work_user") <= 0)
                    {
                        cnn.RollbackTransaction();
                        return JsonResultCommon.Exception(_logger,cnn.LastError, _config, loginData,ControllerContext);
                    }
                    long idc = long.Parse(cnn.ExecuteScalar("select IDENT_CURRENT('we_work_user')").ToString());
                    string strU = "update we_work_user set disabled=1, updateddate=getdate(), updatedby=" + id_user + ", id_child=" + idc + " where id_row=" + dt.Rows[0]["id_row"].ToString();
                    if (cnn.ExecuteNonQuery(strU) != 1)
                    {
                        cnn.RollbackTransaction();
                        return JsonResultCommon.Exception(_logger,cnn.LastError, _config, loginData,ControllerContext);
                    }
                    if (!WeworkLiteController.log(_logger, loginData.Username, cnn, 15, id, id_user))
                    {
                        cnn.RollbackTransaction();
                        return JsonResultCommon.Exception(_logger,cnn.LastError, _config, loginData,ControllerContext);
                    }
                    cnn.EndTransaction();
                    return JsonResultCommon.ThanhCong();
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
                        val.Add("id_project_team", data.id_project_team);
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
                        return JsonResultCommon.Exception(_logger,cnn.LastError, _config, loginData,ControllerContext);
                    }
                    long idc = long.Parse(cnn.ExecuteScalar("select IDENT_CURRENT('we_work_duplicate')").ToString());

                    string sql = "exec DuplicateWork " + idc;
                    DataTable dt = cnn.CreateDataTable(sql);
                    if (cnn.LastError != null || dt == null || dt.Rows.Count == 0)
                    {
                        cnn.RollbackTransaction();
                        return JsonResultCommon.Exception(_logger,cnn.LastError, _config, loginData,ControllerContext);
                    }
                    //string LogContent = "", LogEditContent = "";
                    //LogContent = LogEditContent = "Thêm mới dữ liệu work_duplicate: title=" + data.title + ", id=" + data.id + ", type=" + data.type;
                    //DpsPage.Ghilogfile(loginData.CustomerID.ToString(), LogEditContent, LogContent, loginData.UserName);
                    //log trong store
                    //long idw = long.Parse(dt.Rows[0]["id_row"].ToString());
                    //if (!WeworkLiteController.log(_logger, loginData.Username, cnn, 1, idw, iduser))
                    //{
                    //    cnn.RollbackTransaction();
                    //    return JsonResultCommon.Exception(_logger,cnn.LastError, _config, loginData,ControllerContext);
                    //}
                    cnn.EndTransaction();
                    return JsonResultCommon.ThanhCong(dt.AsEnumerable().FirstOrDefault());
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
                    string sql = @$"select l.*, act.action, act.action_en, act.format, act.sql,w.title,acc.* from we_log l 
join we_log_action act on l.id_action = act.id_row
join we_work w on w.id_row = l.object_id
join {_config.HRCatalog}.dbo.v_account acc on l.CreatedBy = acc.id_nv
where act.object_type = 1 and view_detail=1 and l.id_row = " + id;
                    DataTable dt = cnn.CreateDataTable(sql);
                    if (dt == null || dt.Rows.Count == 0)
                        return JsonResultCommon.KhongTonTai("Hoạt động");
                    if (dt.Rows[0]["sql"] != DBNull.Value)
                    {
                        DataTable temp = cnn.CreateDataTable(dt.Rows[0]["sql"].ToString(), new SqlConditions() { { "old", dt.Rows[0]["oldvalue"] }, { "new", dt.Rows[0]["newvalue"] } });
                        if (temp == null)
                            return JsonResultCommon.Exception(_logger,cnn.LastError, _config, loginData,ControllerContext);
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
                                        image = r["image"],
                                    }
                                }).FirstOrDefault();
                    return JsonResultCommon.ThanhCong(data);
                }
            }
            catch (Exception ex)
            {
                return JsonResultCommon.Exception(_logger,ex, _config, loginData);
            }
        }
        #region export
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
                    DataSet ds = getWork(cnn, query, loginData.UserID);
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
        #endregion
        #region import
        /// <summary>
        /// Download file import mẫu
        /// </summary>
        /// <returns></returns>
        [Route("DownloadFileImport")]
        [HttpGet]
        public IActionResult DownloadFileImport()
        {
            //string Token = Common.GetHeader(Request);
            //UserJWT loginData = Ulities.GetUserByHeader(HttpContext.Request.Headers);
            //if (loginData == null)
            //    return Unauthorized();
            ErrorModel error = new ErrorModel();
            SqlConditions Conds = new SqlConditions();
            DataTable dt = new DataTable();
            try
            {

                string file_download = "project_import_work.xlsx";
                string path = System.IO.Path.Combine(_hostingEnvironment.ContentRootPath, "dulieu/FileTemplate/" + file_download);
                this.Response.Headers.Add("X-Filename", file_download);
                this.Response.Headers.Add("Access-Control-Expose-Headers", "X-Filename");
                return PhysicalFile(path, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", file_download);
            }
            catch (Exception ex)
            {
                return BadRequest(JsonResultCommon.Exception(_logger, ex, _config, null));
            }
        }
        public static DataImportModel data_import = new DataImportModel();
        /// <summary>
        /// Import dữ liệu
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
                    string CustemerID = loginData.CustomerID.ToString();
                    string filesave = "", filename = "";
                    if (!UploadHelper.UploadFile(data.File, data.FileName, "/AttWork/", _hostingEnvironment.ContentRootPath, ref filename, _configuration))
                        return JsonResultCommon.Custom("Upload file thất bại");
                    //BLayer.General g = new BLayer.General();

                    //if (!g.SaveFile(data.File, data.FileName, loginData.CustomerID.ToString(), "/AttWork/", out filesave, out filename))
                    //    return JsonResultCommon.Custom("Upload file thất bại");
                    string loi = "";
                    //filename = string.Format("{0}{1}", AppDomain.CurrentDomain.BaseDirectory, "/AttWork/");
                    DataTable dt = Common.ReaddataFromXLSFile(_hostingEnvironment.ContentRootPath+"/dulieu/"+filename, data.Sheet, out loi);
                    if (!loi.Equals(""))
                        return JsonResultCommon.Custom("File không hợp lệ");
                    //List<string> headers = new List<string>() { "STT","Tên công việc","Người giao","Người thực hiện","Người theo dõi","Ưu tiên(urgent)","Tags","Ngày bắt đầu","Deadline","Hoàn thành thực tế","Mô tả công việc","Trạng thái","Kết quả công việc","Mục tiêu" };
                    if (!dt.Columns.Contains("STT"))
                        return JsonResultCommon.Custom("Dữ liệu không đúng định dạng");
                    DataSet ds;
                    using (DpsConnection cnn = new DpsConnection(ConnectionString))
                    {
                        string sql = "select id_row, title from we_group where disabled=0 and id_project_team=@id";
                        sql += @$";select id_user, acc.hoten, acc.Username from we_project_team_user u
join {_config.HRCatalog}.dbo.v_account acc on u.id_user = acc.Id_NV where disabled = 0 and id_project_team = @id";
                        sql += ";select id_row, title from we_tag where disabled=0 and id_project_team=@id";
                        sql += ";select id_row, title from we_milestone where disabled=0 and id_project_team=@id";
                        sql += ";select * from we_work where 1=0";//lấy cấu trúc
                        ds = cnn.CreateDataSet(sql, new SqlConditions() { { "id", data.id_project_team } });
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
                                var find = asG.Where(x => x["title"].ToString() == dr[1].ToString()).FirstOrDefault();
                                if (find == null)
                                {
                                    int stt = dtPK.Rows.Count + 1;
                                    dtPK.Rows.Add(new object[] { 0, dr[1].ToString(), "we_group", stt });
                                    group = new
                                    {
                                        id_row = "stt_" + stt,
                                        title = dr[1].ToString(),
                                    };
                                }
                                else
                                {
                                    group = new
                                    {
                                        id_row = find["id_row"].ToString(),
                                        title = dr[1].ToString(),
                                    };
                                }
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
                            drW["id_group"] = group.id_row;
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
                            return JsonResultCommon.Exception(_logger,cnn.LastError, _config, loginData,ControllerContext);
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
                                return JsonResultCommon.Exception(_logger,cnn.LastError, _config, loginData,ControllerContext);
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
                                    return JsonResultCommon.Exception(_logger,cnn.LastError, _config, loginData,ControllerContext);
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
                                    return JsonResultCommon.Exception(_logger,cnn.LastError, _config, loginData,ControllerContext);
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
                return JsonResultCommon.Exception(_logger,ex, _config, loginData);
            }
        }
        #endregion
        #region follower
        /// <summary>
        /// 
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        [Route("Add-follower")]
        [HttpGet]
        public async Task<object> AddFollower(long id, long id_user, bool follow = true)
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
                    string strF = "select id_row from we_work_user where Disabled=0 and id_work=" + id + " and id_user=" + id_user;
                    var temp = cnn.ExecuteScalar(strF);
                    if ((temp != null && follow) || (temp == null && !follow))
                    {
                        cnn.RollbackTransaction();
                        return JsonResultCommon.Custom("Người dùng đang " + (follow ? "theo dõi" : "không theo dõi") + " công việc");
                    }
                    int re = 0;
                    if (follow)
                    {
                        Hashtable val1 = new Hashtable();
                        val1["id_work"] = id;
                        val1["CreatedDate"] = DateTime.Now;
                        val1["CreatedBy"] = loginData.UserID;
                        val1["id_user"] = id_user;
                        val1["loai"] = 2;
                        re = cnn.Insert(val1, "we_work_user");
                    }
                    else
                    {
                        string sql = "update we_work_user set disabled=1 where id_row=" + temp.ToString();
                        re = cnn.ExecuteNonQuery(sql);

                    }
                    if (re != 1)
                    {
                        cnn.RollbackTransaction();
                        return JsonResultCommon.Exception(_logger,cnn.LastError, _config, loginData,ControllerContext);
                    }
                    return JsonResultCommon.ThanhCong();
                }
            }
            catch (Exception ex)
            {
                return JsonResultCommon.Exception(_logger,ex, _config, loginData);
            }
        }
        /// <summary>
        /// 
        /// <param name="data"></param>
        /// <returns></returns>
        [Route("Add_followers")]
        [HttpPost]
        public async Task<BaseModel<object>> AddFollowers(WorkModel data)
        {
            string Token = Common.GetHeader(Request);
            UserJWT loginData = Ulities.GetUserByHeader(HttpContext.Request.Headers);
            if (loginData == null)
                return JsonResultCommon.DangNhap();
            try
            {
                if (data.Users == null || data.Users.Count == 0)
                    return JsonResultCommon.BatBuoc("người theo dõi");
                string ConnectionString = WeworkLiteController.getConnectionString(ConnectionCache, loginData.CustomerID, _configuration);
                using (DpsConnection cnn = new DpsConnection(ConnectionString))
                {
                    SqlConditions sqlcond = new SqlConditions();
                    sqlcond.Add("id_row", data.id_row);
                    sqlcond.Add("disabled", 0);
                    string s = "select * from we_work where (where)";
                    s += ";select * from we_work_user where loai=2 and disabled=0 and id_work=" + data.id_row;
                    DataSet ds = cnn.CreateDataSet(s, "(where)", sqlcond);
                    if (cnn.LastError != null || ds == null)
                        return JsonResultCommon.Exception(_logger,cnn.LastError, _config, loginData,ControllerContext);
                    DataTable old = ds.Tables[0];
                    if (old == null || old.Rows.Count == 0)
                        return JsonResultCommon.KhongTonTai("Công việc");
                    long iduser = loginData.UserID;
                    var asU = ds.Tables[1].AsEnumerable();
                    Hashtable val1 = new Hashtable();
                    val1["id_work"] = data.id_row;
                    val1["CreatedDate"] = DateTime.Now;
                    val1["CreatedBy"] = iduser;
                    val1["loai"] = 2;
                    cnn.BeginTransaction();
                    foreach (var user in data.Users)
                    {
                        var find = asU.Where(x => x["id_user"].ToString() == user.id_user.ToString()).FirstOrDefault();//đã theo dõi
                        if (find != null)
                            continue;
                        val1["id_user"] = user.id_user;
                        if (cnn.Insert(val1, "we_work_user") != 1)
                        {
                            cnn.RollbackTransaction();
                            return JsonResultCommon.Exception(_logger,cnn.LastError, _config, loginData,ControllerContext);
                        }
                    }
                    cnn.EndTransaction();
                    return JsonResultCommon.ThanhCong(data);
                }
            }
            catch (Exception ex)
            {
                return JsonResultCommon.Exception(_logger,ex, _config, loginData);
            }
        }
        #endregion
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
                    DataSet ds = getWork(cnn, query, loginData.UserID, " and w.id_nv=@iduser");
                    if (cnn.LastError != null || ds == null)
                        return JsonResultCommon.Exception(_logger,cnn.LastError, _config, loginData,ControllerContext);
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
                                       imageurl = WeworkLiteController.genLinkImage(domain, loginData.UserID, rr["id_nv"].ToString(), _hostingEnvironment.ContentRootPath)
                                       //Children = getChild(domain, loginData.CustomerID, "", displayChild, g.Key, g.Concat(dtChild).CopyToDataTable().AsEnumerable(), tags)
                                   };
                    return JsonResultCommon.ThanhCong(Children, pageModel, Visible);
                }
            }
            catch (Exception ex)
            {
                return JsonResultCommon.Exception(_logger,ex, _config, loginData);
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
                    DataSet ds = getWork(cnn, query, loginData.UserID);
                    if (cnn.LastError != null || ds == null)
                        return JsonResultCommon.Exception(_logger,cnn.LastError, _config, loginData,ControllerContext);
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
                                       imageurl = WeworkLiteController.genLinkImage(domain, loginData.UserID, rr["id_nv"].ToString(), _hostingEnvironment.ContentRootPath)
                                       //Children = getChild(domain, loginData.CustomerID, "", displayChild, g.Key, g.Concat(dtChild).CopyToDataTable().AsEnumerable(), tags)
                                   };
                    return JsonResultCommon.ThanhCong(Children, pageModel, Visible);
                }
            }
            catch (Exception ex)
            {
                return JsonResultCommon.Exception(_logger,ex, _config, loginData);
            }
        }
        #endregion
        private DataSet getWork(DpsConnection cnn, QueryParams query, long curUser, string dieukien_where = "")
        {
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
            string sqlq = @$"select Distinct w.*, nv.holot+' '+nv.ten as hoten_nguoigiao
                        , Iif(fa.id_row is null ,0,1) as favourite 
                        ,coalesce( f.count,0) as num_file,coalesce( com.count,0) as num_com,
                        iIf(w.Status=2 and w.end_date>w.deadline,1,0) as is_htquahan,
                        iIf(w.Status = 2 and w.end_date <= w.deadline, 1, 0) as is_htdunghan ,
                        iIf(w.Status = 1 and  w.start_date is not null, 1, 0) as is_danglam,
                        iIf(w.Status = 1 and getdate() > w.deadline, 1, 0) as is_quahan,
                        iif(convert(varchar, w.deadline,103) like convert(varchar, GETDATE(),103),1,0) as duetoday,
                        iif(w.status=1 and w.start_date is null,1,0) as require,
                        tao.UserName as NguoiTao, sua.Username as NguoiSua from v_wework w 
                        left join (select count(*) as count,object_id 
                        from we_attachment where object_type=1 group by object_id) f on f.object_id=w.id_row
                        left join (select count(*) as count,object_id 
                        from we_comment where object_type=1 group by object_id) com on com.object_id=w.id_row
                        left join {_config.HRCatalog}.dbo.Tbl_Nhanvien nv on w.nguoigiao = nv.id_nv
                        join {_config.HRCatalog}.dbo.Tbl_Account tao on tao.id_nv=w.CreatedBy
                        left join {_config.HRCatalog}.dbo.Tbl_Account sua on sua.id_nv=w.UpdatedBy
                        left join we_work_favourite fa 
                        on fa.id_work=w.id_row and fa.createdby=@iduser and fa.disabled=0
                        where 1=1 " + dieukien_where + "  order by " + dieukienSort;
            sqlq += ";select id_work, id_tag,color, title " +
                "from we_work_tag wt join we_tag t on wt.id_tag=t.id_row where wt.Disabled=0";
            if (!string.IsNullOrEmpty(query.filter["id_project_team"]))
                sqlq += " and id_project_team=" + query.filter["id_project_team"];
            //người theo dõi
            sqlq += @$";select id_work,nv.holot+' '+nv.ten as hoten from we_work_user u 
                    join {_config.HRCatalog}.dbo.Tbl_Nhanvien nv on u.id_user = nv.id_nv
                    where u.disabled = 0 and u.loai = 2";
            return cnn.CreateDataSet(sqlq, Conds);
            #endregion
        }
        private EnumerableRowCollection<DataRow> filterWork(EnumerableRowCollection<DataRow> enumerableRowCollections, FilterModel filter)
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
        private object getChild(string domain, long IdKHDPS, string columnName, string displayChild, object id, EnumerableRowCollection<DataRow> temp, EnumerableRowCollection<DataRow> tags, object parent = null)
        {
            if (parent == null)
                parent = DBNull.Value;
            //columnName="" : không group
            var re = from r in temp
                     where r["id_parent"].Equals(parent) && (columnName == "" || (columnName != "" && r[columnName].Equals(id))) //(parent == null &&  r[columnName].Equals(id)) || (r["id_parent"].Equals(parent) && parent != null)
                     select new
                     {
                         id_parent = r["id_parent"],
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
                         num_file = r["num_file"],
                         num_com = r["num_com"],
                         CreatedDate = string.Format("{0:dd/MM/yyyy HH:mm}", r["CreatedDate"]),
                         CreatedBy = r["CreatedBy"],
                         NguoiTao = r["NguoiTao"],
                         UpdatedDate = r["UpdatedDate"] == DBNull.Value ? "" : string.Format("{0:dd/MM/yyyy HH:mm}", r["UpdatedDate"]),
                         UpdatedBy = r["UpdatedBy"],
                         NguoiSua = r["NguoiSua"],
                         NguoiGiao = r["NguoiGiao"],
                         assign = r["id_nv"] == DBNull.Value ? null : new
                         {
                             id_nv = r["id_nv"],
                             hoten = r["hoten"],
                             username = r["username"],
                             tenchucdanh = r["tenchucdanh"],
                             mobile = r["mobile"],
                             image = r["image"],
                         },
                         Tags = from t in tags
                                where r["id_row"].Equals(t["id_work"])
                                select new
                                {
                                    id_row = t["id_tag"],
                                    title = t["title"],
                                    color = t["color"]
                                },
                         Childs = displayChild == "0" ? new List<string>() : getChild(domain, IdKHDPS, columnName, displayChild == "1" ? "0" : "2", id, temp, tags, r["id_row"])
                     };
            return re;
        }
        public static DateTime FirstDateOfWeek(int year, int weekOfYear)
        {
            CultureInfo ci = CultureInfo.CurrentCulture;
            DateTime jan1 = new DateTime(year, 1, 1);
            int daysOffset = (int)DayOfWeek.Monday - (int)jan1.DayOfWeek;
            DateTime firstWeekDay = jan1.AddDays(daysOffset);
            int firstWeek = ci.Calendar.GetWeekOfYear(jan1, ci.DateTimeFormat.CalendarWeekRule, DayOfWeek.Monday);
            if ((firstWeek <= 1 || firstWeek >= 52) && daysOffset >= -3)
            {
                weekOfYear -= 1;
            }
            return firstWeekDay.AddDays(weekOfYear * 7);
        }
        private object getStyle(DataRow v)
        {
            string b = "#4298F4";
            if (v["status"].ToString() == "2")//hoan thanh
                b = "#FFC107";
            if (v["is_quahan"].ToString() == "1")
                b = "#D1483F";
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
    }
}