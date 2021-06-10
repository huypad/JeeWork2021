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

namespace JeeWork_Core2021.Controllers.Wework
{
    [ApiController]
    [Route("api/wuser")]
    [EnableCors("JeeWorkPolicy")]
    /// <summary>
    /// api quản lý we_authorize
    /// </summary>
    public class WUserController : ControllerBase
    {
        private readonly IHostingEnvironment _hostingEnvironment;
        private JeeWorkConfig _config;
        public List<AccUsernameModel> DataAccount;
        private IConnectionCache ConnectionCache;
        private IConfiguration _configuration;

        public WUserController(IOptions<JeeWorkConfig> config, IHostingEnvironment hostingEnvironment, IConnectionCache _cache, IConfiguration configuration)
        {
            _hostingEnvironment = hostingEnvironment;
            _config = config.Value;
            ConnectionCache = _cache;
            _configuration = configuration;
        }
        /// <summary>
        /// DS account
        /// </summary>
        /// <returns></returns>
        [Route("list")]
        [HttpGet]
        public object List([FromQuery] QueryParams query)
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
                SqlConditions Conds = new SqlConditions();
                string dieukienSort = "hoten", dieukien_where = " ";
                #region Sort data theo các dữ liệu bên dưới
                Dictionary<string, string> sortableFields = new Dictionary<string, string>
                        {
                            { "hoten", "hoten"},
                            { "username", "username"}
                        };
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
                string keywork = "";
                if (!string.IsNullOrEmpty(query.sortField) && sortableFields.ContainsKey(query.sortField))
                    dieukienSort = sortableFields[query.sortField] + ("desc".Equals(query.sortOrder) ? " desc" : " asc");
                if (query.filter != null && query.filter.keys != null)
                {
                    if (query.filter.keys.Contains("keyword") && !string.IsNullOrEmpty(query.filter["keyword"]))
                    {
                        dieukien_where += " and (hoten like N'%@keyword%' or username like N'%@keyword%')";
                        dieukien_where = dieukien_where.Replace("@keyword", query.filter["keyword"]);
                        keywork = query.filter["keyword"];
                    }
                }
                string domain = _configuration.GetValue<string>("Host:JeeWork_API");
                string ConnectionString = WeworkLiteController.getConnectionString(ConnectionCache, loginData.CustomerID, _configuration);
                using (DpsConnection cnn = new DpsConnection(ConnectionString))
                {
                    DataTable dt = new DataTable();
                    dt.Columns.Add("UserId");
                    dt.Columns.Add("FullName");
                    dt.Columns.Add("Username");
                    dt.Columns.Add("PhoneNumber");
                    dt.Columns.Add("Email");
                    dt.Columns.Add("Jobtitle");
                    dt.Columns.Add("AvartarImgURL");
                    int total = DataAccount.Count;
                    if (total == 0)
                        return JsonResultCommon.ThanhCong(new List<string>(), pageModel, Visible);
                    foreach (AccUsernameModel acc in DataAccount)
                        dt.Rows.Add(acc.UserId, acc.FullName, acc.Username, acc.PhoneNumber, acc.Email, acc.Jobtitle, acc.AvartarImgURL);
                    total = dt.Rows.Count;
                    var temp = dt.AsEnumerable();
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
                    dt = temp.Skip((query.page - 1) * query.record).Take(query.record).CopyToDataTable();
                    var data = (from r in dt.AsEnumerable()
                                where r["FullName"].ToString().ToLower().Contains(keywork.ToLower()) || r["Username"].ToString().ToLower().Contains(keywork.ToLower())
                                select new
                                {
                                    id_nv = r["UserId"],
                                    hoten = r["FullName"],
                                    username = r["Username"],
                                    mobile = r["PhoneNumber"],
                                    email = r["Email"],
                                    tenchucdanh = r["Jobtitle"],
                                    image = r["AvartarImgURL"],
                                    //image = WeworkLiteController.genLinkImage(domain, loginData.CustomerID, r["id_nv"].ToString(), _hostingEnvironment.ContentRootPath),
                                    //manangers = getMananger(r.Jobtitle, ds, domain, loginData.CustomerID),
                                    manangers = ""
                                });
                    var list = data.Skip((query.page - 1) * query.record).Take(query.record).ToList();
                    return JsonResultCommon.ThanhCong(list, pageModel);
                }
            }
            catch (Exception ex)
            {
                return JsonResultCommon.Exception(ex, _config, loginData.CustomerID);
            }
        }

        private object getMananger(object v, DataSet ds, string domain, long IDKHDPS)
        {
            List<object> parents = (from r in ds.Tables[1].AsEnumerable()
                                    where r["id_row"].Equals(v)
                                    select r["id_parent"]).ToList();
            return from r in ds.Tables[0].AsEnumerable()
                   where parents.Contains(r["id_chucdanh"])
                   select new
                   {
                       id_nv = r["id_nv"],
                       hoten = r["hoten"],
                       username = r["username"],
                       mobile = r["mobile"],
                       email = r["email"],
                       tenchucdanh = r["tenchucdanh"],
                       image = WeworkLiteController.genLinkImage(domain, IDKHDPS, r["id_nv"].ToString(), _hostingEnvironment.ContentRootPath),
                   };
        }

        [Route("detail")]
        [HttpGet]
        public object Detail(long id)
        {
            string Token = Common.GetHeader(Request);
            UserJWT loginData = Ulities.GetUserByHeader(HttpContext.Request.Headers);
            if (loginData == null)
                return JsonResultCommon.DangNhap();
            try
            {
                if (id <= 0)
                    return JsonResultCommon.BatBuoc("Thành viên");
                string domain = _configuration.GetValue<string>("Host:JeeWork_API");
                DataTable dtStaff = null;
                using (DpsConnection cnn = new DpsConnection(_config.HRConnectionString))
                {
                    dtStaff = Common.GetListByManager(id.ToString(), cnn);//id_nv, hoten...
                }
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
                    AccUsernameModel infoAccount = DataAccount.Where(x => id.ToString().Contains(x.UserId.ToString())).FirstOrDefault();
                    #region Trả dữ liệu về backend để hiển thị lên giao diện
                    string sqlq = $"";
                    sqlq += @"select w.*, IIF(w.Status = 1 and getdate() > w.deadline,1,0) as is_quahan,
IIF(convert(varchar, w.NgayGiao,103) like convert(varchar, GETDATE(),103),1,0) as is_moigiao
from v_wework_new w where w.disabled=0 and id_nv = " + id;
                    sqlq += @$";select a.id_user as id_nv, '' as hoten, '' as username, '' as image, '' as Tenchucdanh, '' as mobile from we_authorize a
where disabled = 0 and a.CreatedBy = " + id;
                    sqlq += @"select p.id_row, p.title from we_project_team_user u
join we_project_team p on p.id_row=u.id_project_team where u.disabled=0 and p.Disabled=0 and id_user=" + id;
                    DataSet ds = cnn.CreateDataSet(sqlq);
                    if (cnn.LastError != null || ds == null)
                    {
                        return JsonResultCommon.Exception(cnn.LastError, _config, loginData.CustomerID,ControllerContext);
                    }
                    if (infoAccount == null)
                        return JsonResultCommon.KhongTonTai();
                    var temp = ds.Tables[0].AsEnumerable();

                    #region Map info account từ JeeAccount
                    foreach (DataRow item in ds.Tables[1].Rows)
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
                    //var data = new
                    //{
                    //    LuuY = (from r in temp
                    //            where r["is_quahan"].ToString() == "1" || (bool)r["urgent"]
                    //            select new
                    //            {
                    //                id_row = r["id_row"],
                    //                title = r["title"],
                    //                status = r["status"],
                    //                is_quahan = r["is_quahan"],
                    //                urgent = r["urgent"],
                    //            }),
                    //    MoiDuocGiao = (from r in temp
                    //                   where r["is_moigiao"].ToString() == "1" && r["NguoiGiao"].ToString() != loginData.Id.ToString()
                    //                   && !(r["is_quahan"].ToString() == "1" || (bool)r["urgent"])
                    //                   select new
                    //                   {
                    //                       id_row = r["id_row"],
                    //                       title = r["title"],
                    //                       status = r["status"],
                    //                   }),
                    //    GiaoQuaHan = (from r in temp
                    //                  where r["is_quahan"].ToString() == "1" && r["NguoiGiao"].ToString() == loginData.Id.ToString()
                    //                  select new
                    //                  {
                    //                      id_row = r["id_row"],
                    //                      title = r["title"],
                    //                      status = r["status"],
                    //                      urgent = r["urgent"],
                    //                  }),
                    //};
                    //DataRow rNV = ds.Tables[0].Rows[0];
                    return JsonResultCommon.ThanhCong(new
                    {
                        id_nv = infoAccount.UserId,
                        hoten = infoAccount.FullName,
                        tenchucdanh = infoAccount.Jobtitle,
                        username = infoAccount.Username,
                        mobile = infoAccount.PhoneNumber,
                        image = infoAccount.AvartarImgURL,
                        //image = WeworkLiteController.genLinkImage(domain, loginData.CustomerID, rNV["id_nv"].ToString(), _hostingEnvironment.ContentRootPath),
                        Count = new
                        {
                            ht = temp.Count(w => w["status"].ToString() == "2"),
                            tong = temp.Count(),
                            phailam = temp.Count(w => w["status"].ToString() == "1" && w["status"] == DBNull.Value),
                            danglam = temp.Count(w => w["status"].ToString() == "1" && w["status"] != DBNull.Value),
                            quahan = temp.Count(w => w["status"].ToString() != "2" && w["is_quahan"].ToString() == "1"),
                            khancap = temp.Count(w => (bool)w["urgent"]),
                        },
                        staffs = from r in dtStaff.AsEnumerable()
                                 select new
                                 {
                                     id_nv = r["id_nv"],
                                     hoten = r["hoten"],
                                     tenchucdanh = r["tenchucdanh"],
                                     //username = r["username"],
                                     //mobile = r["mobile"],
                                     image = WeworkLiteController.genLinkImage(domain, loginData.CustomerID, r["id_nv"].ToString(), _hostingEnvironment.ContentRootPath)
                                 },
                        uyquyens = from r in ds.Tables[1].AsEnumerable()
                                   select new
                                   {
                                       id_nv = r["id_nv"],
                                       hoten = r["hoten"],
                                       tenchucdanh = r["tenchucdanh"],
                                       username = r["username"],
                                       mobile = r["mobile"],
                                       image = WeworkLiteController.genLinkImage(domain, loginData.CustomerID, r["id_nv"].ToString(), _hostingEnvironment.ContentRootPath)
                                   },
                        projects = from rr in ds.Tables[2].AsEnumerable()
                                   select new
                                   {
                                       id = rr["id_row"],
                                       title = rr["title"],
                                       count = temp.Count(w => w["id_project_team"].Equals(rr["id_row"]))
                                   }
                    });
                }
                #endregion
            }
            catch (Exception ex)
            {
                return JsonResultCommon.Exception(ex, _config, loginData.CustomerID);
            }
        }
        [Route("ListAuthorize")]
        [HttpGet]
        public object ListAuthorize([FromQuery] QueryParams query)
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
                    string dieukienSort = "authorize.CreatedDate";
                    #region Sort data theo các dữ liệu bên dưới
                    Dictionary<string, string> sortableFields = new Dictionary<string, string>
                        {
                            { "CreatedDate", "CreatedDate"},
                        };
                    #endregion
                    if (!string.IsNullOrEmpty(query.sortField) && sortableFields.ContainsKey(query.sortField))
                        dieukienSort = sortableFields[query.sortField] + ("desc".Equals(query.sortOrder) ? " desc" : " asc");
                    #region Trả dữ liệu về backend để hiển thị lên giao diện
                    string sqlq = @$"select id_row, id_user, CreatedDate, CreatedBy, Disabled
, UpdatedDate, UpdatedBy, '' as hoten_nguoiuyquyen
, '' as username_nguoiuyquyen
, '' as username_nhanuyquyen
, '' as user_nhanuyquyen
from we_authorize authorize 
where authorize.Disabled = 0  and authorize.id_user in ({listID}) and authorize.CreatedBy in ({listID})
and authorize.Createdby =" + loginData.UserID + " " +
"order by " + dieukienSort;
                    DataTable dt = cnn.CreateDataTable(sqlq, Conds);
                    if (cnn.LastError != null || dt == null)
                        return JsonResultCommon.Exception(cnn.LastError, _config, loginData.CustomerID,ControllerContext);
                    if (dt.Rows.Count == 0)
                        return JsonResultCommon.ThanhCong(new List<string>(), pageModel, Visible);
                    #region Map info account từ JeeAccount

                    foreach (DataRow item in dt.Rows)
                    {
                        var infonguoiuyquyen = DataAccount.Where(x => item["CreatedBy"].ToString().Contains(x.UserId.ToString())).FirstOrDefault();
                        var infonhanuyquyen = DataAccount.Where(x => item["id_user"].ToString().Contains(x.UserId.ToString())).FirstOrDefault();

                        if (infonguoiuyquyen != null)
                        {
                            item["hoten_nguoiuyquyen"] = infonguoiuyquyen.FullName;
                            item["username_nguoiuyquyen"] = infonguoiuyquyen.Username;
                        }
                        if (infonhanuyquyen != null)
                        {
                            item["username_nhanuyquyen"] = infonhanuyquyen.FullName;
                            item["user_nhanuyquyen"] = infonhanuyquyen.Username;
                        }
                    }
                    #endregion
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
                                   id_user = r["id_user"],
                                   CreatedDate = r["CreatedDate"],
                                   CreatedBy = r["CreatedBy"],
                                   UpdatedDate = r["UpdatedDate"],
                                   hoten_nguoiuyquyen = r["hoten_nguoiuyquyen"],
                                   username_nguoiuyquyen = r["username_nguoiuyquyen"],
                                   username_nhanuyquyen = r["username_nhanuyquyen"],
                                   user_nhanuyquyen = r["user_nhanuyquyen"],
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
        [Route("Authorize")]
        [HttpPost]
        public async Task<object> Authorize(AuthorizeModel data)
        {
            string Token = Common.GetHeader(Request);
            UserJWT loginData = Ulities.GetUserByHeader(HttpContext.Request.Headers);
            if (loginData == null)
                return JsonResultCommon.DangNhap();
            try
            {
                string strRe = "";
                if (data.id_user < 0)
                    strRe += (strRe == "" ? "" : ",") + "thông tin người ủy quyền";
                if (strRe != "")
                    return JsonResultCommon.BatBuoc(strRe);

                string ConnectionString = WeworkLiteController.getConnectionString(ConnectionCache, loginData.CustomerID, _configuration);
                using (DpsConnection cnn = new DpsConnection(ConnectionString))
                {
                    long iduser = loginData.UserID;
                    long idk = loginData.CustomerID;
                    Hashtable val = new Hashtable();
                    //Update lại những người đã ủy quyền trước đó
                    object deleted_old = cnn.ExecuteNonQuery("update we_authorize set Disabled=1, UpdatedDate=getdate(), UpdatedBy=" + iduser + " where (CreatedBy = " + iduser + ")");
                    val = new Hashtable();
                    val.Add("id_user", data.id_user);
                    val.Add("CreatedDate", DateTime.Now);
                    val.Add("CreatedBy", iduser);
                    cnn.BeginTransaction();
                    if (cnn.Insert(val, "we_authorize") != 1)
                    {
                        cnn.RollbackTransaction();
                        return JsonResultCommon.Exception(cnn.LastError, _config, loginData.CustomerID,ControllerContext);
                    }
                    //string LogContent = "", LogEditContent = "";
                    //LogContent = LogEditContent = "Thêm mới dữ liệu authorize: User được ủy quyền=" + data.id_user + ", Người ủy quyền=" + data.CreatedBy;
                    //DpsPage.Ghilogfile(loginData.CustomerID.ToString(), LogEditContent, LogContent, loginData.UserName);
                    string idc = cnn.ExecuteScalar("select IDENT_CURRENT('we_authorize')").ToString();
                    cnn.EndTransaction();
                    data.id_row = int.Parse(idc);
                    return JsonResultCommon.ThanhCong(data);
                }
            }
            catch (Exception ex)
            {
                return JsonResultCommon.Exception(ex, _config, loginData.CustomerID);
            }
        }
    }
}