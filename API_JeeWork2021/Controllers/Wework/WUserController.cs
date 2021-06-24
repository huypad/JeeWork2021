﻿using DpsLibs.Data;
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
using API_JeeWork2021.Classes;
using JeeWork_Core2021.ConsumerServices;
using static API_JeeWork2021.Classes.NhacNho;
using DPSinfra.Kafka;

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
        private IProducer _producer;
        private readonly ILogger<WUserController> _logger;

        public WUserController(IOptions<JeeWorkConfig> config, IHostingEnvironment hostingEnvironment, IConnectionCache _cache, IConfiguration configuration, ILogger<WUserController> logger, IProducer producer)
        {
            _hostingEnvironment = hostingEnvironment;
            _config = config.Value;
            ConnectionCache = _cache;
            _configuration = configuration;
            _logger = logger;
            _producer = producer;
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
                        dieukien_where += " and (hoten like N'%@keyword%' ')";
                        dieukien_where = dieukien_where.Replace("@keyword", query.filter["keyword"]);
                        keywork = query.filter["keyword"];
                    }
                }
                string domain = _configuration.GetValue<string>("Host:JeeWork_API") + "/";
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
                    //var list = data.Skip((query.page - 1) * query.record).Take(query.record).ToList();
                    if (query.sortField == "hoten")
                    {
                        if(query.sortOrder == "asc")
                        {
                            data = data.OrderBy(x => x.hoten);
                        }
                        else
                        {
                            data = data.OrderByDescending(x => x.hoten);
                        }
                    }
                    return JsonResultCommon.ThanhCong(data, pageModel);
                }
            }
            catch (Exception ex)
            {
                return JsonResultCommon.Exception(_logger,ex, _config, loginData);
            }
        }
        
        /// <summary>
        ///  Danh sách ủy quyền
        /// </summary>
        /// <returns></returns>
        [Route("list-uy-quyen")]
        [HttpGet]
        public object ListUyQuyen([FromQuery] QueryParams query)
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
                //if (query.filter != null && query.filter.keys != null)
                //{
                //    if (query.filter.keys.Contains("keyword") && !string.IsNullOrEmpty(query.filter["keyword"]))
                //    {
                //        dieukien_where += " and (hoten like N'%@keyword%' ')";
                //        dieukien_where = dieukien_where.Replace("@keyword", query.filter["keyword"]);
                //        keywork = query.filter["keyword"];
                //    }
                //}
                string domain = _configuration.GetValue<string>("Host:JeeWork_API") + "/";
                string ConnectionString = WeworkLiteController.getConnectionString(ConnectionCache, loginData.CustomerID, _configuration);
                using (DpsConnection cnn = new DpsConnection(ConnectionString))
                {
                    string sqlq = $@"select *,'' as hoten,'' as image,'' as tenchucdanh from we_authorize where Disabled = 0 and CreatedBy = {loginData.UserID}";
                    DataTable dt = cnn.CreateDataTable(sqlq);

                    #region Map info account từ JeeAccount
                    foreach (DataRow item in dt.Rows)
                    {
                        var info = DataAccount.Where(x => item["id_user"].ToString().Contains(x.UserId.ToString())).FirstOrDefault();
                        if (info != null)
                        {
                            item["hoten"] = info.FullName;
                            item["image"] = info.AvartarImgURL;
                            item["tenchucdanh"] = info.Jobtitle;
                        }
                    }
                    #endregion
                    //DataTable dt = new DataTable();
                    //dt.Columns.Add("UserId");
                    //dt.Columns.Add("FullName");
                    //dt.Columns.Add("Username");
                    //dt.Columns.Add("PhoneNumber");
                    //dt.Columns.Add("Email");
                    //dt.Columns.Add("Jobtitle");
                    //dt.Columns.Add("AvartarImgURL");
                    //int total = DataAccount.Count;
                    //if (total == 0)
                    //    return JsonResultCommon.ThanhCong(new List<string>(), pageModel, Visible);
                    //foreach (AccUsernameModel acc in DataAccount)
                    //    dt.Rows.Add(acc.UserId, acc.FullName, acc.Username, acc.PhoneNumber, acc.Email, acc.Jobtitle, acc.AvartarImgURL);
                    //total = dt.Rows.Count;
                    int total = dt.Rows.Count;
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
                    //dt = temp.Skip((query.page - 1) * query.record).Take(query.record).CopyToDataTable();
                    var data = (from r in dt.AsEnumerable()
                                select new
                                {
                                    id_row = r["id_row"],
                                    id_user = r["id_user"],
                                    hoten = r["hoten"],
                                    tenchucdanh = r["tenchucdanh"],
                                    image = r["image"],
                                    CreatedBy = r["CreatedBy"],
                                    is_all_project = r["is_all_project"],
                                    list_project = r["list_project"],
                                    start_date = r["start_date"],
                                    end_date = r["end_date"],
                                });
                    //var list = data.Skip((query.page - 1) * query.record).Take(query.record).ToList();
                    if (query.sortField == "hoten")
                    {
                        if(query.sortOrder == "asc")
                        {
                            data = data.OrderBy(x => x.hoten);
                        }
                        else
                        {
                            data = data.OrderByDescending(x => x.hoten);
                        }
                    }
                    return JsonResultCommon.ThanhCong(data, pageModel);
                }
            }
            catch (Exception ex)
            {
                return JsonResultCommon.Exception(_logger,ex, _config, loginData);
            }
        }
        /// <summary>
        ///  Danh sách ủy quyền
        /// </summary>
        /// <returns></returns>
        [Route("detail-uy-quyen")]
        [HttpGet]
        public object DetailUyQuyen(long id)
        {
            string Token = Common.GetHeader(Request);
            UserJWT loginData = Ulities.GetUserByHeader(HttpContext.Request.Headers);
            if (loginData == null)
                return JsonResultCommon.DangNhap();
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
                    string sqlq = $@"select *,'' as hoten,'' as image,'' as tenchucdanh from we_authorize where Disabled = 0 and CreatedBy = {loginData.UserID} and id_row = {id}";
                    DataTable dt = cnn.CreateDataTable(sqlq);

                    #region Map info account từ JeeAccount
                    foreach (DataRow item in dt.Rows)
                    {
                        var info = DataAccount.Where(x => item["id_user"].ToString().Contains(x.UserId.ToString())).FirstOrDefault();
                        if (info != null)
                        {
                            item["hoten"] = info.FullName;
                            item["image"] = info.AvartarImgURL;
                            item["tenchucdanh"] = info.Jobtitle;
                        }
                    }
                    #endregion

                    var data = (from r in dt.AsEnumerable()
                                select new
                                {
                                    id_row = r["id_row"],
                                    id_user = r["id_user"],
                                    hoten = r["hoten"],
                                    tenchucdanh = r["tenchucdanh"],
                                    image = r["image"],
                                    CreatedBy = r["CreatedBy"],
                                    is_all_project = r["is_all_project"],
                                    list_project = r["list_project"],
                                    start_date = r["start_date"],
                                    end_date = r["end_date"],
                                }).FirstOrDefault();
                    return JsonResultCommon.ThanhCong(data);
                }
            }
            catch (Exception ex)
            {
                return JsonResultCommon.Exception(_logger,ex, _config, loginData);
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
                       image = r["image"],
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
                string domain = _configuration.GetValue<string>("Host:JeeWork_API") + "/";
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
                        return JsonResultCommon.Exception(_logger,cnn.LastError, _config, loginData,ControllerContext);
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
                                     image = r["image"],
                                 },
                        uyquyens = from r in ds.Tables[1].AsEnumerable()
                                   select new
                                   {
                                       id_nv = r["id_nv"],
                                       hoten = r["hoten"],
                                       tenchucdanh = r["tenchucdanh"],
                                       username = r["username"],
                                       mobile = r["mobile"],
                                       image = r["image"],
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
                return JsonResultCommon.Exception(_logger,ex, _config, loginData);
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
                        return JsonResultCommon.Exception(_logger,cnn.LastError, _config, loginData,ControllerContext);
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
                return JsonResultCommon.Exception(_logger,ex, _config, loginData);
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
                    //object deleted_old = cnn.ExecuteNonQuery("update we_authorize set Disabled=1, UpdatedDate=getdate(), UpdatedBy=" + iduser + " where (CreatedBy = " + iduser + ")");
                    SqlConditions cond = new SqlConditions();
                    cond.Add("from", data.start_date);
                    cond.Add("to", data.end_date);
                    #region kiểm tra có ủy quyền cho ai chưa
                    string sql = $@"select * from we_authorize where Disabled = 0 and CreatedBy = {loginData.UserID}
and ( (start_date >= @from and start_date <= @to ) or (end_date >= @from and end_date <= @to ) ) ";
                    DataTable dt = cnn.CreateDataTable(sql,cond);
                    int tong = dt.Rows.Count;
                    var temp = dt.AsEnumerable().Where(x => x["is_all_project"].ToString().Equals("1")).CopyToDataTable();
                    if(temp.Rows.Count > 0)
                        return JsonResultCommon.Custom("Khoảng thời gian và dự án không phù hợp!");
                    temp = dt.AsEnumerable().Where(x => x["is_all_project"].ToString().Equals("0")).CopyToDataTable();
                    if (temp.Rows.Count > 0)
                    {
                        if(data.is_all_project)
                            return JsonResultCommon.Custom("Khoảng thời gian và dự án không phù hợp!");
                        //else
                        //{
                        //    List<int> listproject = data.list_project.Split(',').Select(Int32.Parse).ToList();
                        //    foreach (DataRow item in temp.Rows)
                        //    {
                        //        List<int> listpro = item["list_project"].ToString().Split(',').Select(Int32.Parse).ToList();
                        //        List<int> New2 = listproject.Concat(listpro).ToList();

                        //    }
                        //}
                    }    

                    //if(dt.AsEnumerable > 0)

                    //if (dt.Rows.Count > 0)
                    //    return JsonResultCommon.Custom("Khoảng thời gian và dự án không phù hợp!");

                    #endregion
                    val = new Hashtable();
                    val.Add("is_all_project", data.is_all_project);
                    val.Add("list_project", data.list_project);
                    val.Add("start_date", data.start_date);
                    val.Add("end_date", data.end_date);
                    val.Add("id_user", data.id_user);
                    val.Add("CreatedDate", DateTime.Now);
                    val.Add("CreatedBy", iduser);
                    cnn.BeginTransaction();
                    if (cnn.Insert(val, "we_authorize") != 1)
                    {
                        cnn.RollbackTransaction();
                        return JsonResultCommon.Exception(_logger,cnn.LastError, _config, loginData,ControllerContext);
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
                return JsonResultCommon.Exception(_logger,ex, _config, loginData);
            }
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [Route("thong-tin-reminder")]
        [HttpGet]
        public BaseModel<object> reminder()
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
                    #region Lấy dữ liệu account từ JeeAccount
                    DataAccount = WeworkLiteController.GetAccountFromJeeAccount(HttpContext.Request.Headers, _configuration);
                    if (DataAccount == null)
                        return JsonResultCommon.Custom("Lỗi lấy danh sách nhân viên từ hệ thống quản lý tài khoản");
                    #endregion
                    var data = from r in DataAccount
                               select new
                               {
                                   UserID = r.UserId,
                                   Fullname = r.FullName,
                                   congviecphutrach = NhacNho.GetSoluongCongviecUser(r.UserId, r.CustomerID, ConnectionString, _configuration, _producer),
                                   congviecquahan = NhacNho.GetSoluongCongviecQuaHan(r.UserId, r.CustomerID, ConnectionString, _configuration, _producer),
                                   congviechethantrongngay = NhacNho.GetSoluongCongviecHethanTrongngay(r.UserId, r.CustomerID, ConnectionString, _configuration, _producer),
                                   duanquahan = NhacNho.GetSoluongDuAnQuaHan(r.UserId,r.CustomerID, ConnectionString, _configuration, _producer),
                               };
                    return JsonResultCommon.ThanhCong(data);
                }
            }
            catch (Exception ex)
            {
                return JsonResultCommon.Custom(ex.Message);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        //[Route("test-guidata")]
        //[HttpGet]
        //public BaseModel<object> TestReminder()
        //{
        //    try
        //    {
        //        SendTestReminder(_configuration,_producer);
        //        return JsonResultCommon.ThanhCong("OK bro");
        //    }
        //    catch (Exception ex)
        //    {
        //        return JsonResultCommon.Custom(ex.Message);
        //    }
        //}
    }
}