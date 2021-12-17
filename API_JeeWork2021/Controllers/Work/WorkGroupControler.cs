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
using System.Collections.Specialized;
using DPSinfra.ConnectionCache;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace JeeWork_Core2021.Controllers.Wework
{
    /// <summary>
    /// api quản lý work
    /// </summary>
    [ApiController]
    [Route("api/work-group")]
    [EnableCors("JeeWorkPolicy")]
    public class WorkGroupController : ControllerBase
    {
        private JeeWorkConfig _config;
        private IConnectionCache ConnectionCache;
        public List<AccUsernameModel> DataAccount;
        private IConfiguration _configuration;
        private readonly ILogger<WorkGroupController> _logger;

        public WorkGroupController(IOptions<JeeWorkConfig> config, IConnectionCache _cache, IConfiguration configuration, ILogger<WorkGroupController> logger)
        {
            _config = config.Value;
            ConnectionCache = _cache;
            _configuration = configuration;
            _logger = logger;
        }
        [Route("List")]
        [HttpGet]
        public object List([FromQuery] QueryParams query)
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
                    #region Lấy dữ liệu account từ JeeAccount
                    DataAccount = JeeWorkLiteController.GetAccountFromJeeAccount(HttpContext.Request.Headers, _configuration);
                    if (DataAccount == null)
                        return JsonResultCommon.Custom("Lỗi lấy danh sách nhân viên từ hệ thống quản lý tài khoản");

                    //List<string> nvs = DataAccount.Select(x => x.UserId.ToString()).ToList();
                    //string ids = string.Join(",", nvs);
                    string error = "";
                    string listID = JeeWorkLiteController.ListAccount(HttpContext.Request.Headers, out error, _configuration);
                    if (error != "")
                        return JsonResultCommon.Custom(error);
                    #endregion

                    SqlConditions Conds = new SqlConditions();
                    string dieukienSort = "title", dieukien_where = " ";
                    if (string.IsNullOrEmpty(query.filter["id_project_team"]))
                        return JsonResultCommon.Custom("Dự án/phòng ban bắt buộc nhập");
                    dieukien_where += " and id_project_team=@id_project_team";
                    Conds.Add("id_project_team", query.filter["id_project_team"]);
                    #region Sort data theo các dữ liệu bên dưới
                    Dictionary<string, string> sortableFields = new Dictionary<string, string>
                        {
                            { "title", "title"},
                            { "description", "description"},
                        };
                    #endregion
                    if (!string.IsNullOrEmpty(query.sortField) && sortableFields.ContainsKey(query.sortField))
                        dieukienSort = sortableFields[query.sortField] + ("desc".Equals(query.sortOrder) ? " desc" : " asc");
                    #region Trả dữ liệu về backend để hiển thị lên giao diện
                    long hoanthanh = ReportByProjectController.GetStatusComplete(int.Parse(query.filter["id_project_team"]),cnn);
                    Conds.Add("hoanthanh", hoanthanh);
                    //string sqlq = "select  null as id_row, '' as description, N'Chưa phân loại' as title, id_project_team,count(*) as tong, COUNT(CASE WHEN w.status=2 THEN 1 END) as ht from v_wework w where id_group is null" + dieukien_where+ "  group by id_group, id_project_team  union ";
                    string sqlq = @"select g.*, coalesce( w.tong,0) as tong, coalesce( w.ht,0) as ht from we_group g 
left join (select count(*) as tong, COUNT(CASE WHEN w.status=@hoanthanh THEN 1 END) as ht,w.id_group from we_work w where id_parent is null group by w.id_group) w on g.id_row=w.id_group
where g.Disabled=0 " + dieukien_where + "  order by " + dieukienSort;
                    DataTable dt = cnn.CreateDataTable(sqlq, Conds);
                    if (cnn.LastError != null || dt == null)
                        return JsonResultCommon.Exception(_logger,cnn.LastError, _config, loginData,ControllerContext);
                    if (dt.Rows.Count == 0)
                        return JsonResultCommon.ThanhCong(new List<string>(), pageModel, Visible);
                    var temp = dt.AsEnumerable();
                    dt = temp.CopyToDataTable();
                    int total = dt.Rows.Count;
                    pageModel.TotalCount = total;
                    pageModel.AllPage = (int)Math.Ceiling(total / (decimal)query.record);
                    pageModel.Size = query.record;
                    pageModel.Page = query.page;
                    if (query.more)
                    {
                        query.page = 1;
                        query.record = pageModel.TotalCount;
                    }
                    var nguoitao = new { };
                    var nguoisua = new { }; 
                    var checker = new { }; 
                    // Phân trang
                    dt = dt.AsEnumerable().Skip((query.page - 1) * query.record).Take(query.record).CopyToDataTable();
                    var data = from r in dt.AsEnumerable()
                               select new
                               {
                                   id_row = r["id_row"],
                                   title = r["title"],
                                   description = r["description"],
                                   id_project_team = r["id_project_team"],
                                   locked = r["locked"],
                                   CreatedDate = r["CreatedDate"],
                                   UpdatedDate = r["UpdatedDate"],
                                   nguoitao = r["CreatedBy"].Equals(DBNull.Value) ? new { } : getUser(r["CreatedBy"].ToString(), DataAccount),
                                   nguoisua = r["UpdatedBy"].Equals(DBNull.Value) ? new { } : getUser(r["UpdatedBy"].ToString(), DataAccount),
                                   reviewer = r["reviewer"].Equals(DBNull.Value) ? new { } : getUser(r["reviewer"].ToString(), DataAccount),
                                   Count = new
                                   {
                                       tong = r["tong"],
                                       ht = r["ht"],
                                   }
                               };
                    return JsonResultCommon.ThanhCong(data, pageModel, Visible);
                }
                #endregion
            }
            catch (Exception ex)
            {
                return JsonResultCommon.Exception(_logger,ex, _config, loginData);
            }
        }

        object getUser(string iduser, List<AccUsernameModel> DataAccount)
        {
            var info = DataAccount.Where(x => iduser.Contains(x.UserId.ToString())).FirstOrDefault();
            if (info != null)
            { 
               var data = new
                {
                    hoten = info.FullName,
                    mobile = info.PhoneNumber,
                    username = info.Username,
                    image = info.AvartarImgURL,
                    jobtitle = info.Jobtitle,
                    department = info.Department,
               };
                return data;
            }
            return new { };
        }


        [Route("Detail")]
        [HttpGet]
        public object Detail(long id)
        {
           
            UserJWT loginData = Ulities.GetUserByHeader(HttpContext.Request.Headers);
            if (loginData == null)
                return JsonResultCommon.DangNhap();
            try
            {
                string ConnectionString = JeeWorkLiteController.getConnectionString(ConnectionCache, loginData.CustomerID, _configuration);
                using (DpsConnection cnn = new DpsConnection(ConnectionString))
                {
                    string sqlq = @"select * from we_group where disabled=0 and id_row=" + id;
                    DataTable dt = cnn.CreateDataTable(sqlq);
                    if (cnn.LastError != null || dt == null)
                        return JsonResultCommon.Exception(_logger,cnn.LastError, _config, loginData,ControllerContext);
                    if (dt.Rows.Count == 0)
                        return JsonResultCommon.KhongTonTai();
                    #region Trả dữ liệu về backend để hiển thị lên giao diện
                    var data = (from r in dt.AsEnumerable()
                                select new
                                {
                                    id_row = r["id_row"],
                                    title = r["title"],
                                    description = r["description"],
                                    id_project_team = r["id_project_team"],
                                    reviewer = r["reviewer"]
                                }).FirstOrDefault();
                    return JsonResultCommon.ThanhCong(data);
                    #endregion
                }
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
        public async Task<object> Insert(WorkGroupModel data)
        {
           
            UserJWT loginData = Ulities.GetUserByHeader(HttpContext.Request.Headers);
            if (loginData == null)
                return JsonResultCommon.DangNhap();
            try
            {
                string strRe = "";
                if (string.IsNullOrEmpty(data.title))
                    strRe += (strRe == "" ? "" : ",") + "tên nhóm công việc";
                if (data.id_project_team <= 0)
                    strRe += (strRe == "" ? "" : ",") + "dự án/phòng ban";
                if (strRe != "")
                    return JsonResultCommon.BatBuoc(strRe);

                string ConnectionString = JeeWorkLiteController.getConnectionString(ConnectionCache, loginData.CustomerID, _configuration);
                using (DpsConnection cnn = new DpsConnection(ConnectionString))
                {
                    long iduser = loginData.UserID;
                    long idk = loginData.CustomerID;
                    Hashtable val = new Hashtable();
                    val.Add("title", data.title);
                    if (string.IsNullOrEmpty(data.description))
                        val.Add("description", "");
                    else
                        val.Add("description", data.description);
                    val.Add("id_project_team", data.id_project_team);
                    if (data.reviewer > 0)
                        val.Add("reviewer", data.reviewer);
                    val.Add("CreatedDate", Common.GetDateTime());
                    val.Add("CreatedBy", iduser);
                    string strCheck = "select count(*) from we_group where Disabled=0 and  (id_project_team=@id_project_team) and title=@name";
                    if (int.Parse(cnn.ExecuteScalar(strCheck, new SqlConditions() { { "id_project_team", data.id_project_team }, { "name", data.title } }).ToString()) > 0)
                    {
                        return JsonResultCommon.Custom("Nhóm công việc đã tồn tại trong dự án/phòng ban");
                    }
                    cnn.BeginTransaction();
                    if (cnn.Insert(val, "we_group") != 1)
                    {
                        cnn.RollbackTransaction();
                        return JsonResultCommon.Exception(_logger,cnn.LastError, _config, loginData,ControllerContext);
                    }
                    //string LogContent = "", LogEditContent = "";
                    //LogContent = LogEditContent = "Thêm mới dữ liệu work group: title=" + data.title + ", id_project_team=" + data.id_project_team + ", description=" + data.description;
                    //DpsPage.Ghilogfile(loginData.CustomerID.ToString(), LogEditContent, LogContent, loginData.UserName);
                    string idc = cnn.ExecuteScalar("select IDENT_CURRENT('we_group')").ToString();
                    cnn.EndTransaction();
                    data.id_row = int.Parse(idc);
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
        [Route("Update")]
        [HttpPost]
        public async Task<BaseModel<object>> Update(WorkGroupModel data)
        {
           
            UserJWT loginData = Ulities.GetUserByHeader(HttpContext.Request.Headers);
            if (loginData == null)
                return JsonResultCommon.DangNhap();
            try
            {
                string strRe = "";
                if (string.IsNullOrEmpty(data.title))
                    strRe += (strRe == "" ? "" : ",") + "tên nhóm làm việc";
                if (strRe != "")
                    return JsonResultCommon.BatBuoc(strRe);
                string ConnectionString = JeeWorkLiteController.getConnectionString(ConnectionCache, loginData.CustomerID, _configuration);
                using (DpsConnection cnn = new DpsConnection(ConnectionString))
                {
                    SqlConditions sqlcond = new SqlConditions();
                    sqlcond.Add("id_row", data.id_row);
                    sqlcond.Add("disabled", 0);
                    string s = "select * from we_group where (where)";
                    DataTable old = cnn.CreateDataTable(s, "(where)", sqlcond);
                    if (old == null || old.Rows.Count == 0)
                        return JsonResultCommon.KhongTonTai("Nhóm công việc");
                    long iduser = loginData.UserID;
                    long idk = loginData.CustomerID;
                    Hashtable val = new Hashtable();
                    val.Add("title", data.title);
                    if (string.IsNullOrEmpty(data.description))
                        val.Add("description", "");
                    else
                        val.Add("description", data.description);
                    if (data.reviewer > 0)
                        val.Add("reviewer", data.reviewer);
                    else
                        val.Add("reviewer", DBNull.Value);
                    val.Add("UpdatedDate", Common.GetDateTime());
                    val.Add("UpdatedBy", iduser);
                    string strCheck = "select count(*) from we_group where Disabled=0 and  (id_project_team=@id_project_team) and title=@name and id_row<>@id";
                    if (int.Parse(cnn.ExecuteScalar(strCheck, new SqlConditions() { { "id_project_team", data.id_project_team }, { "name", data.title }, { "id", data.id_row } }).ToString()) > 0)
                    {
                        return JsonResultCommon.Custom("Nhóm làm việc đã tồn tại trong dự án/phòng ban");
                    }
                    cnn.BeginTransaction();
                    if (cnn.Update(val, sqlcond, "we_group") != 1)
                    {
                        cnn.RollbackTransaction();
                        return JsonResultCommon.Exception(_logger,cnn.LastError, _config, loginData,ControllerContext);
                    }
                    DataTable dt = cnn.CreateDataTable(s, "(where)", sqlcond);
                    //string LogContent = "", LogEditContent = "";
                    //LogEditContent = DpsPage.GetEditLogContent(old, dt);
                    //if (!LogEditContent.Equals(""))
                    //{
                    //    LogEditContent = "Chỉnh sửa dữ liệu (" + data.id_row + ") : " + LogEditContent;
                    //    LogContent = "Chỉnh sửa dữ liệu work group (" + data.id_row + "), Chi tiết xem trong log chỉnh sửa chức năng";
                    //}
                    //DpsPage.Ghilogfile(loginData.CustomerID.ToString(), LogEditContent, LogContent, loginData.UserName);
                    cnn.EndTransaction();
                    return JsonResultCommon.ThanhCong(data);
                }
            }
            catch (Exception ex)
            {
                return JsonResultCommon.Exception(_logger,ex, _config, loginData);
            }
        }

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
                    string sqlq = "select ISNULL((select count(*) from we_group where Disabled=0 and  id_row = " + id + "),0)";
                    if (long.Parse(cnn.ExecuteScalar(sqlq).ToString()) != 1)
                        return JsonResultCommon.KhongTonTai("Nhóm làm việc");
                    //if (Common.TestDuplicate("", id.ToString(), "-1", "we_work", "id_group", "", "-1", cnn, "", true) == false)
                    //{
                    //    return JsonResultCommon.Custom("Đang có công việc thuộc nhóm công việc này nên không thể xóa");
                    //}
                    sqlq = "update we_group set Disabled=1, UpdatedDate=GETUTCDATE(), UpdatedBy=" + iduser + " where id_row = " + id;
                    cnn.BeginTransaction();
                    if (cnn.ExecuteNonQuery(sqlq) != 1)
                    {
                        cnn.RollbackTransaction();
                        return JsonResultCommon.Exception(_logger,cnn.LastError, _config, loginData,ControllerContext);
                    }
                    //string LogContent = "Xóa dữ liệu work group (" + id + ")";
                    //DpsPage.Ghilogfile(loginData.CustomerID.ToString(), LogContent, LogContent, loginData.UserName);
                    cnn.EndTransaction();
                    return JsonResultCommon.ThanhCong();
                }
            }
            catch (Exception ex)
            {
                return JsonResultCommon.Exception(_logger,ex, _config, loginData);
            }
        }

        /// <param name="data"></param>
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
                    sqlcond.Add("id_row", id);
                    sqlcond.Add("disabled", 0);
                    string s = "select * from we_group where (where)";
                    DataTable old = cnn.CreateDataTable(s, "(where)", sqlcond);
                    if (old == null || old.Rows.Count == 0)
                        return JsonResultCommon.KhongTonTai("Nhóm công việc");
                    if ((bool)old.Rows[0]["Locked"] == closed)
                        return JsonResultCommon.Custom("Nhóm làm việc này vẫn chưa " + (closed ? "mở" : "đóng"));
                    long iduser = loginData.UserID;
                    long idk = loginData.CustomerID;
                    Hashtable val = new Hashtable();
                    val.Add("locked", closed);
                    val.Add("UpdatedDate", Common.GetDateTime());
                    val.Add("UpdatedBy", iduser);
                    cnn.BeginTransaction();
                    if (cnn.Update(val, sqlcond, "we_group") != 1)
                    {
                        cnn.RollbackTransaction();
                        return JsonResultCommon.Exception(_logger,cnn.LastError, _config, loginData,ControllerContext);
                    }
                    DataTable dt = cnn.CreateDataTable(s, "(where)", sqlcond);
                    //string LogContent = "", LogEditContent = "";
                    //LogEditContent = DpsPage.GetEditLogContent(old, dt);
                    //if (!LogEditContent.Equals(""))
                    //{
                    //    LogEditContent = "Chỉnh sửa dữ liệu (" + id + ") : " + LogEditContent;
                    //    LogContent = "Chỉnh sửa dữ liệu work group (" + id + "), Chi tiết xem trong log chỉnh sửa chức năng";
                    //}
                    //DpsPage.Ghilogfile(loginData.CustomerID.ToString(), LogEditContent, LogContent, loginData.UserName);

                    cnn.EndTransaction();
                    return JsonResultCommon.ThanhCong(closed);
                }
            }
            catch (Exception ex)
            {
                return JsonResultCommon.Exception(_logger,ex, _config, loginData);
            }
        }

    }
}