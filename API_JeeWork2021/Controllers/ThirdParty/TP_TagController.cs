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

namespace JeeWork_Core2021.Controllers.Wework
{
    [ApiController]
    [Route("api/tp-tag")]
    [EnableCors("JeeWorkPolicy")]
    /// <summary>
    /// api quản lý tag
    /// </summary>
    // [EnableCors(origins: "*", headers: "*", methods: "*")]
    public class TP_TagController : ControllerBase
    {
        private readonly IHostingEnvironment _hostingEnvironment;
        private JeeWorkConfig _config;
        private IConnectionCache ConnectionCache;
        private IConfiguration _configuration;
        private readonly ILogger<TagController> _logger;
        public List<AccUsernameModel> DataAccount;
        public TP_TagController(IOptions<JeeWorkConfig> config, IHostingEnvironment hostingEnvironment, IConnectionCache _cache, IConfiguration configuration, ILogger<TagController> logger)
        {
            _hostingEnvironment = hostingEnvironment;
            _config = config.Value;
            ConnectionCache = _cache;
            _configuration = configuration;
            _logger = logger;
        }
        [Route("list")]
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
                string ConnectionString = WeworkLiteController.getConnectionString(ConnectionCache, loginData.CustomerID, _configuration);
                using (DpsConnection cnn = new DpsConnection(ConnectionString))
                {
                    SqlConditions Conds = new SqlConditions();
                    string dieukienSort = "title", dieukien_where = " ";
                    if (string.IsNullOrEmpty(query.filter["id_project_team"]))
                        return JsonResultCommon.Custom("Ban bắt buộc nhập");
                    dieukien_where += " and id_project_team=@id_project_team";
                    Conds.Add("id_project_team", query.filter["id_project_team"]);
                    if (!string.IsNullOrEmpty(query.filter["keyword"]))
                    {
                        dieukien_where += " and (m.title like '%@keyword%' or tao.Username like '%@keyword%' or sua.Username like '%@keyword%')";
                        dieukien_where = dieukien_where.Replace("@keyword", query.filter["keyword"]);
                    }
                    #region Sort data theo các dữ liệu bên dưới
                    #region Lấy dữ liệu account từ JeeAccount
                    DataAccount = TP_LiteController.GetAccountFromJeeAccount(HttpContext.Request.Headers, _configuration);
                    if (DataAccount == null)
                        return JsonResultCommon.Custom("Lỗi lấy danh sách nhân viên từ hệ thống quản lý tài khoản");
                    string error = "";
                    string listID = TP_LiteController.ListAccount(HttpContext.Request.Headers, out error, _configuration);
                    if (error != "")
                        return JsonResultCommon.Custom(error);
                    #endregion
                    Dictionary<string, string> sortableFields = new Dictionary<string, string>
                        {
                            { "title", "title"},
                            { "CreatedBy", "NguoiTao"},
                            { "CreatedDate", "CreatedDate"},
                            { "UpdatedBy", "NguoiSua"},
                            {"UpdatedDate","UpdatedDate" }
                        };
                    #endregion
                    if (!string.IsNullOrEmpty(query.sortField) && sortableFields.ContainsKey(query.sortField))
                        dieukienSort = sortableFields[query.sortField] + ("desc".Equals(query.sortOrder) ? " desc" : " asc");
                    #region Return data to backend to display on the interface
                    string sqlq = @"select * from we_tag where Disabled=0 " + dieukien_where + "  order by " + dieukienSort;
                    DataTable dt = cnn.CreateDataTable(sqlq, Conds);
                    if (cnn.LastError != null || dt == null)
                        return JsonResultCommon.Exception(_logger, cnn.LastError, _config, loginData, ControllerContext);
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
                    // Phân trang
                    dt = dt.AsEnumerable().Skip((query.page - 1) * query.record).Take(query.record).CopyToDataTable();
                    var data = from r in dt.AsEnumerable()
                               select new
                               {
                                   id_row = r["id_row"],
                                   title = r["title"],
                                   color = r["color"],
                                   id_project_team = r["id_project_team"],
                                   createddate = string.Format("{0:dd/MM/yyyy HH:mm}", r["CreatedDate"]),
                                   createdby = r["CreatedBy"].Equals(DBNull.Value) ? new { } : TP_LiteController.Get_InfoUsers(r["CreatedBy"].ToString(), DataAccount),
                                   updateddate = r["UpdatedDate"] == DBNull.Value ? "" : string.Format("{0:dd/MM/yyyy HH:mm}", r["UpdatedDate"]),
                                   updatedby = r["UpdatedBy"].Equals(DBNull.Value) ? new { } : TP_LiteController.Get_InfoUsers(r["CreatedBy"].ToString(), DataAccount),
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
        /// <param name="data"></param>
        /// <returns></returns>
        [Route("insert")]
        [HttpPost]
        public async Task<object> Insert(TagModel data)
        {
            UserJWT loginData = Ulities.GetUserByHeader(HttpContext.Request.Headers);
            if (loginData == null)
                return JsonResultCommon.DangNhap();
            try
            {
                string strRe = "";
                if (string.IsNullOrEmpty(data.title))
                    strRe += (strRe == "" ? "" : ",") + "nhãn";
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
                    val.Add("title", data.title);
                    val.Add("id_project_team", data.id_project_team);
                    val.Add("color", string.IsNullOrEmpty(data.color) ? "" : data.color);
                    val.Add("CreatedDate", Common.GetDateTime());
                    val.Add("CreatedBy", iduser);
                    string strCheck = "select count(*) from we_tag where Disabled=0 and  (id_project_team=@id_project_team) and title=@name";
                    if (int.Parse(cnn.ExecuteScalar(strCheck, new SqlConditions() { { "id_project_team", data.id_project_team }, { "name", data.title } }).ToString()) > 0)
                    {
                        return JsonResultCommon.Custom("Nhãn đã tồn tại trong dự án/phòng ban");
                    }
                    cnn.BeginTransaction();
                    if (cnn.Insert(val, "we_tag") != 1)
                    {
                        cnn.RollbackTransaction();
                        return JsonResultCommon.Exception(_logger, cnn.LastError, _config, loginData, ControllerContext);
                    }
                    string idc = cnn.ExecuteScalar("select IDENT_CURRENT('we_tag')").ToString();
                    cnn.EndTransaction();
                    data.id_row = int.Parse(idc);
                    return JsonResultCommon.ThanhCong(data);
                }
            }
            catch (Exception ex)
            {
                return JsonResultCommon.Exception(_logger, ex, _config, loginData);
            }
        }

        /// <param name="data"></param>
        /// <returns></returns>
        [Route("update")]
        [HttpPost]
        public async Task<BaseModel<object>> Update(TagModel data)
        {
            UserJWT loginData = Ulities.GetUserByHeader(HttpContext.Request.Headers);
            if (loginData == null)
                return JsonResultCommon.DangNhap();
            try
            {
                string strRe = "";
                if (string.IsNullOrEmpty(data.title))
                    strRe += (strRe == "" ? "" : ",") + "nhãn";
                if (strRe != "")
                    return JsonResultCommon.BatBuoc(strRe);
                string ConnectionString = WeworkLiteController.getConnectionString(ConnectionCache, loginData.CustomerID, _configuration);
                using (DpsConnection cnn = new DpsConnection(ConnectionString))
                {
                    SqlConditions sqlcond = new SqlConditions();
                    sqlcond.Add("id_row", data.id_row);
                    sqlcond.Add("disabled", 0);
                    string s = "select * from we_tag where (where)";
                    DataTable old = cnn.CreateDataTable(s, "(where)", sqlcond);
                    if (old == null || old.Rows.Count == 0)
                        return JsonResultCommon.KhongTonTai("Nhãn");
                    long iduser = loginData.UserID;
                    long idk = loginData.CustomerID;
                    Hashtable val = new Hashtable();
                    val.Add("title", data.title);
                    if (string.IsNullOrEmpty(data.color))
                        val.Add("color", "");
                    else
                        val.Add("color", data.color);
                    val.Add("UpdatedDate", Common.GetDateTime());
                    val.Add("UpdatedBy", iduser);
                    string strCheck = "select count(*) from we_tag where Disabled=0 and  (id_project_team=@id_project_team) and title=@name and id_row<>@id";
                    if (int.Parse(cnn.ExecuteScalar(strCheck, new SqlConditions() { { "id_project_team", data.id_project_team }, { "name", data.title }, { "id", data.id_row } }).ToString()) > 0)
                    {
                        return JsonResultCommon.Custom("Nhãn đã tồn tại trong dự án/phòng ban");
                    }
                    cnn.BeginTransaction();
                    if (cnn.Update(val, sqlcond, "we_tag") != 1)
                    {
                        cnn.RollbackTransaction();
                        return JsonResultCommon.Exception(_logger, cnn.LastError, _config, loginData, ControllerContext);
                    }
                    DataTable dt = cnn.CreateDataTable(s, "(where)", sqlcond);
                    cnn.EndTransaction();
                    return JsonResultCommon.ThanhCong(data);
                }
            }
            catch (Exception ex)
            {
                return JsonResultCommon.Exception(_logger, ex, _config, loginData);
            }
        }
        /// <param name="id"></param>
        /// <returns></returns>
        [Route("delete")]
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
                string ConnectionString = WeworkLiteController.getConnectionString(ConnectionCache, loginData.CustomerID, _configuration);
                using (DpsConnection cnn = new DpsConnection(ConnectionString))
                {
                    string sqlq = "select ISNULL((select count(*) from we_tag where Disabled=0 and id_row = " + id + "),0)";
                    if (long.Parse(cnn.ExecuteScalar(sqlq).ToString()) != 1)
                        return JsonResultCommon.KhongTonTai("Nhãn");
                    sqlq = "update we_tag set Disabled=1, UpdatedDate=GETUTCDATE(), UpdatedBy=" + iduser + " where id_row = " + id;
                    cnn.BeginTransaction();
                    if (cnn.ExecuteNonQuery(sqlq) != 1)
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
    }
}