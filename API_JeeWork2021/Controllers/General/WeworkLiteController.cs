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
using API_JeeWork2021.Classes;
using Microsoft.Extensions.Configuration;
using DPSinfra.Kafka;
using DPSinfra.Notifier;
using DPSinfra.ConnectionCache;
using DPSinfra.Logger;
using Microsoft.Extensions.Logging;
using DPSinfra.Utils;
using JeeWork_Core2021.Controller;

namespace JeeWork_Core2021.Controllers.Wework
{
    [ApiController]
    [Route("api/wework-lite")]
    [EnableCors("JeeWorkPolicy")]
    /// <summary>
    /// các ds lite dành cho wework
    /// </summary>
    public class WeworkLiteController : ControllerBase
    {
        private static Notification notify;
        private readonly IHostingEnvironment _hostingEnvironment;
        private JeeWorkConfig _config;
        public List<AccUsernameModel> DataAccount;
        private IConfiguration _iconfig;
        private INotifier _notifier;
        private IProducer _producer;
        private IConnectionCache ConnectionCache;
        private IConfiguration _configuration;
        private readonly ILogger<IHostingEnvironment> _logger;
        public WeworkLiteController(IOptions<JeeWorkConfig> config, IHostingEnvironment hostingEnvironment, IProducer producer, INotifier notifier, IConfiguration Configuration, IConnectionCache _cache, IConfiguration configuration, ILogger<IHostingEnvironment> logger)
        {
            notify = new Notification(notifier);
            _hostingEnvironment = hostingEnvironment;
            _config = config.Value;
            _producer = producer;
            _iconfig = Configuration;
            ConnectionCache = _cache;
            _configuration = configuration;
            _logger = logger;
        }
        /// <summary>
        /// DS department theo customerID
        /// </summary>
        /// <returns></returns>
        [Route("lite_department")]
        [HttpGet]
        public object Lite_Department()
        {

            UserJWT loginData = Ulities.GetUserByHeader(HttpContext.Request.Headers);
            if (loginData == null)
                return JsonResultCommon.DangNhap();
            try
            {
                string ConnectionString = getConnectionString(ConnectionCache, loginData.CustomerID, _configuration);
                using (DpsConnection cnn = new DpsConnection(ConnectionString))
                {
                    string sql = "select id_row, title from we_department where Disabled=0 and IdKH=" + loginData.CustomerID + " order by title";
                    DataTable dt = cnn.CreateDataTable(sql);
                    if (cnn.LastError != null || dt == null)
                        return JsonResultCommon.Exception(_logger, cnn.LastError, _config, loginData, ControllerContext);
                    var data = from r in dt.AsEnumerable()
                               select new
                               {
                                   id_row = r["id_row"],
                                   title = r["title"],
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
        /// DS dự án theo user tham gia (quản lý hoặc thành viên)
        /// </summary>
        /// <returns></returns>
        [Route("lite_project_team_byuser")]
        [HttpGet]
        public object Lite_Project_Team_ByUser(string keyword = "")
        {

            UserJWT loginData = Ulities.GetUserByHeader(HttpContext.Request.Headers);
            if (loginData == null)
                return JsonResultCommon.DangNhap();
            try
            {
                string ConnectionString = getConnectionString(ConnectionCache, loginData.CustomerID, _configuration);
                using (DpsConnection cnn = new DpsConnection(ConnectionString))
                {
                    DataTable dt = project_by_user(loginData.UserID, loginData.CustomerID, cnn);
                    if (cnn.LastError != null || dt == null)
                        return JsonResultCommon.Exception(_logger, cnn.LastError, _config, loginData, ControllerContext);
                    var data = from r in dt.AsEnumerable()
                               select new
                               {
                                   id_row = r["id_row"],
                                   title = r["title"],
                                   isproject = r["is_project"],
                                   start_date = r["start_date"] == DBNull.Value ? "" : string.Format("{0:dd/MM/yyyy HH:mm}", r["start_date"]),
                                   end_date = r["end_date"] == DBNull.Value ? "" : string.Format("{0:dd/MM/yyyy HH:mm}", r["end_date"]),
                                   color = r["color"],
                                   status = r["status"],
                                   locked = r["locked"],
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
        /// DS dự án theo user tham gia (quản lý hoặc thành viên)
        /// </summary>
        /// <returns></returns>
        [Route("lite_project_team_bydepartment")]
        [HttpGet]
        public object Lite_Project_Team_ByDepartment(string id = "")
        {

            UserJWT loginData = Ulities.GetUserByHeader(HttpContext.Request.Headers);
            if (loginData == null)
                return JsonResultCommon.DangNhap();
            try
            {
                string ConnectionString = getConnectionString(ConnectionCache, loginData.CustomerID, _configuration);
                using (DpsConnection cnn = new DpsConnection(ConnectionString))
                {
                    string sql = @"select distinct p.id_row, p.title, is_project from we_project_team p
                                join we_department d on d.id_row = p.id_department
                                join we_project_team_user u on u.id_project_team = p.id_row
                                 where u.Disabled = 0 and id_user = " + loginData.UserID + " " +
                                 "and p.Disabled = 0  and d.Disabled = 0 and d.id_row= " + id + " " +
                                 "and IdKH=" + loginData.CustomerID + " order by title";
                    DataTable dt = cnn.CreateDataTable(sql);
                    if (cnn.LastError != null || dt == null)
                        return JsonResultCommon.Exception(_logger, cnn.LastError, _config, loginData, ControllerContext);
                    var data = from r in dt.AsEnumerable()
                               select new
                               {
                                   id_row = r["id_row"],
                                   title = r["title"],
                                   isproject = r["is_project"],
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
        /// Danh sách phòng ban theo user tham gia
        /// </summary>
        /// <param name="keyword"></param>
        /// <returns></returns>
        [Route("lite_department_byuser")]
        [HttpGet]
        public object Lite_Department_ByUser(string keyword = "")
        {

            UserJWT loginData = Ulities.GetUserByHeader(HttpContext.Request.Headers);
            if (loginData == null)
                return JsonResultCommon.DangNhap();
            try
            {
                string ConnectionString = getConnectionString(ConnectionCache, loginData.CustomerID, _configuration);
                using (DpsConnection cnn = new DpsConnection(ConnectionString))
                {
                    string listDept = getListDepartment_GetData(loginData, cnn, HttpContext.Request.Headers, _configuration, ConnectionString);
                    #region Lấy dữ liệu account từ JeeAccount
                    DataAccount = GetAccountFromJeeAccount(HttpContext.Request.Headers, _configuration);
                    if (DataAccount == null)
                        return JsonResultCommon.Custom("Lỗi lấy danh sách nhân viên từ hệ thống quản lý tài khoản");
                    string error = "";
                    string listID = ListAccount(HttpContext.Request.Headers, out error, _configuration);
                    if (error != "")
                        return JsonResultCommon.Custom(error);
                    #endregion
                    SqlConditions conds = new SqlConditions();
                    conds.Add("id_user", loginData.UserID);
                    #region Trả dữ liệu về backend để hiển thị lên giao diện
                    string sqlq = @$"select distinct de.*, '' as NguoiTao, '' as TenNguoiTao, '' as NguoiSua, '' as TenNguoiSua 
from we_department de where de.Disabled = 0  and de.CreatedBy in ({listID}) and id_row in ({listDept})";
                    //if (!Visible)
                    //{
                    //    sqlq = sqlq.Replace("(admin)", "left join we_department_owner do on de.id_row = do.id_department " +
                    //        "where de.Disabled = 0 and (do.id_user = " + loginData.UserID + " " +
                    //        "or de.id_row in (select distinct p1.id_department from we_project_team p1 join we_project_team_user pu on p1.id_row = pu.id_project_team " +
                    //        "where p1.Disabled = 0 and id_user = " + loginData.UserID + ")) and de.Disabled = 0 ");
                    //}
                    //else
                    //    sqlq = sqlq.Replace("(admin)", " where de.Disabled = 0  ");
                    //DataTable dt = cnn.CreateDataTable(sqlq, Conds);
                    #endregion

                    DataTable dt = cnn.CreateDataTable(sqlq, conds);
                    if (cnn.LastError != null || dt == null)
                        return JsonResultCommon.Exception(_logger, cnn.LastError, _config, loginData, ControllerContext);
                    #region Map info account từ JeeAccount

                    foreach (DataRow item in dt.Rows)
                    {
                        var infoNguoiTao = DataAccount.Where(x => item["CreatedBy"].ToString().Contains(x.UserId.ToString())).FirstOrDefault();
                        var infoNguoiSua = DataAccount.Where(x => item["UpdatedBy"].ToString().Contains(x.UserId.ToString())).FirstOrDefault();
                        if (infoNguoiTao != null)
                        {
                            item["NguoiTao"] = infoNguoiTao.Username;
                            item["TenNguoiTao"] = infoNguoiTao.FullName;
                        }
                        if (infoNguoiSua != null)
                        {
                            item["NguoiSua"] = infoNguoiSua.Username;
                            item["TenNguoiSua"] = infoNguoiSua.FullName;
                        }
                    }
                    #endregion
                    var data = from r in dt.AsEnumerable()
                               select new
                               {
                                   id_row = r["id_row"],
                                   title = r["title"],
                               };
                    return JsonResultCommon.ThanhCong(data);
                }
            }
            catch (Exception ex)
            {
                return JsonResultCommon.Exception(_logger, ex, _config, loginData);
            }
        }/// <summary>
        /// Danh sách phòng ban theo user tham gia
        /// </summary>
        /// <param name="keyword"></param>
        /// <returns></returns>
        [Route("lite_department_folder_byuser")]
        [HttpGet]
        public object Lite_Department_Folder_ByUser(string keyword = "",long DepartmentID = 0)
        {

            UserJWT loginData = Ulities.GetUserByHeader(HttpContext.Request.Headers);
            if (loginData == null)
                return JsonResultCommon.DangNhap();
            try
            {
                string ConnectionString = getConnectionString(ConnectionCache, loginData.CustomerID, _configuration);
                using (DpsConnection cnn = new DpsConnection(ConnectionString))
                {
                    string listDept = getListDepartment_GetData(loginData, cnn, HttpContext.Request.Headers, _configuration, ConnectionString);
                    #region Lấy dữ liệu account từ JeeAccount
                    DataAccount = GetAccountFromJeeAccount(HttpContext.Request.Headers, _configuration);
                    if (DataAccount == null)
                        return JsonResultCommon.Custom("Lỗi lấy danh sách nhân viên từ hệ thống quản lý tài khoản");
                    string error = "";
                    string listID = ListAccount(HttpContext.Request.Headers, out error, _configuration);
                    if (error != "")
                        return JsonResultCommon.Custom(error);
                    #endregion
                    SqlConditions conds = new SqlConditions();
                    conds.Add("id_user", loginData.UserID);
                    #region Trả dữ liệu về backend để hiển thị lên giao diện
                    string sqlq = @$"select distinct de.*, '' as NguoiTao, '' as TenNguoiTao, '' as NguoiSua, '' as TenNguoiSua 
from we_department de where de.Disabled = 0  and de.CreatedBy in ({listID}) and id_row in ({listDept})"; 
                    #endregion

                    DataTable dt = cnn.CreateDataTable(sqlq, conds);
                    if (cnn.LastError != null || dt == null)
                        return JsonResultCommon.Exception(_logger, cnn.LastError, _config, loginData, ControllerContext);
                    #region Map info account từ JeeAccount

                    foreach (DataRow item in dt.Rows)
                    {
                        var infoNguoiTao = DataAccount.Where(x => item["CreatedBy"].ToString().Contains(x.UserId.ToString())).FirstOrDefault();
                        var infoNguoiSua = DataAccount.Where(x => item["UpdatedBy"].ToString().Contains(x.UserId.ToString())).FirstOrDefault();
                        if (infoNguoiTao != null)
                        {
                            item["NguoiTao"] = infoNguoiTao.Username;
                            item["TenNguoiTao"] = infoNguoiTao.FullName;
                        }
                        if (infoNguoiSua != null)
                        {
                            item["NguoiSua"] = infoNguoiSua.Username;
                            item["TenNguoiSua"] = infoNguoiSua.FullName;
                        }
                    }
                    #endregion
                    var data = from r in dt.AsEnumerable()
                               where  r["parentid"] == DBNull.Value
                               select new
                               {
                                   id_row = r["id_row"],
                                   title = r["title"],
                                   data_folder = from s in dt.AsEnumerable()
                                                 where s["parentid"].ToString() == r["id_row"].ToString()
                                                 select new
                                                 {
                                                     id_row = s["id_row"],
                                                     title = s["title"],
                                                 },
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
        /// DS milestone lite by id_project_team
        /// </summary>
        /// <returns></returns>
        [Route("lite_milestone")]
        [HttpGet]
        public object Lite_Milestone(long id_project_team)
        {

            UserJWT loginData = Ulities.GetUserByHeader(HttpContext.Request.Headers);
            if (loginData == null)
                return JsonResultCommon.DangNhap();
            try
            {
                if (id_project_team <= 0)
                    return new List<string>();
                string ConnectionString = getConnectionString(ConnectionCache, loginData.CustomerID, _configuration);
                using (DpsConnection cnn = new DpsConnection(ConnectionString))
                {
                    string sql = "select id_row, title,deadline from we_milestone where Disabled=0 and id_project_team=" + id_project_team + " order by title";
                    DataTable dt = cnn.CreateDataTable(sql);
                    if (cnn.LastError != null || dt == null)
                        return JsonResultCommon.Exception(_logger, cnn.LastError, _config, loginData, ControllerContext);
                    var data = from r in dt.AsEnumerable()
                               select new
                               {
                                   id_row = r["id_row"],
                                   title = r["title"],
                                   deadline = r["deadline"] == DBNull.Value ? "" : string.Format("{0:dd/MM/yyyy}", r["deadline"]),
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
        /// DS tag lite by id_project_team
        /// </summary>
        /// <returns></returns>
        [Route("lite_tag")]
        [HttpGet]
        public object Lite_Tag(long id_project_team)
        {

            UserJWT loginData = Ulities.GetUserByHeader(HttpContext.Request.Headers);
            if (loginData == null)
                return JsonResultCommon.DangNhap();
            try
            {
                if (id_project_team <= 0)
                    return new List<string>();
                string ConnectionString = getConnectionString(ConnectionCache, loginData.CustomerID, _configuration);
                using (DpsConnection cnn = new DpsConnection(ConnectionString))
                {
                    string sql = "select id_row, title, color from we_tag where Disabled=0 and id_project_team=" + id_project_team + " order by title";
                    DataTable dt = cnn.CreateDataTable(sql);
                    if (cnn.LastError != null || dt == null)
                        return JsonResultCommon.Exception(_logger, cnn.LastError, _config, loginData, ControllerContext);
                    var data = from r in dt.AsEnumerable()
                               select new
                               {
                                   id_row = r["id_row"],
                                   title = r["title"],
                                   color = r["color"]
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
        /// DS work group lite by id_project_team
        /// </summary>
        /// <returns></returns>
        [Route("lite_workgroup")]
        [HttpGet]
        public object Lite_WorkGroup(long id_project_team)
        {

            UserJWT loginData = Ulities.GetUserByHeader(HttpContext.Request.Headers);
            if (loginData == null)
                return JsonResultCommon.DangNhap();
            try
            {
                if (id_project_team <= 0)
                    return new List<string>();
                string ConnectionString = getConnectionString(ConnectionCache, loginData.CustomerID, _configuration);
                using (DpsConnection cnn = new DpsConnection(ConnectionString))
                {
                    string sql = "select 0 as id_row, N'Chưa phân loại' as title union select id_row, title from we_group where Disabled=0 and id_project_team=" + id_project_team + " order by title";
                    DataTable dt = cnn.CreateDataTable(sql);
                    if (cnn.LastError != null || dt == null)
                        return JsonResultCommon.Exception(_logger, cnn.LastError, _config, loginData, ControllerContext);
                    var data = from r in dt.AsEnumerable()
                               select new
                               {
                                   id_row = r["id_row"],
                                   title = r["title"]
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
        /// DS account
        /// </summary>
        /// <returns></returns>
        [Route("lite_account")]
        [HttpGet]
        public object Lite_Account([FromQuery] FilterModel filter)
        {

            UserJWT loginData = Ulities.GetUserByHeader(HttpContext.Request.Headers);
            if (loginData == null)
                return JsonResultCommon.DangNhap();
            try
            {
                string domain = _configuration.GetValue<string>("Host:JeeWork_API") + "/";
                string ConnectionString = getConnectionString(ConnectionCache, loginData.CustomerID, _configuration);
                using (DpsConnection cnn = new DpsConnection(ConnectionString))
                {
                    #region Lấy dữ liệu account từ JeeAccount
                    DataAccount = GetAccountFromJeeAccount(HttpContext.Request.Headers, _configuration);
                    if (DataAccount == null)
                        return JsonResultCommon.Custom("Lỗi lấy danh sách nhân viên từ hệ thống quản lý tài khoản");

                    //List<string> nvs = DataAccount.Select(x => x.UserId.ToString()).ToList();
                    //string ids = string.Join(",", nvs);
                    string error = "";
                    string listID = ListAccount(HttpContext.Request.Headers, out error, _configuration);
                    if (error != "")
                        return JsonResultCommon.Custom(error);
                    #endregion
                    string sql = $@"";
                    if (filter != null && filter.keys != null)
                    {
                        sql = $@"select distinct u.id_user as Id_NV, '' AS hoten, '' as Mobile
                                , '' as Username, '' as Email, '' as CocauID
                                , '' as CoCauToChuc, '' as ParentID, '' as Id_Chucdanh, '' AS Tenchucdanh
                                from we_project_team_user u
                                left join we_project_team p on u.id_project_team=p.id_row
                                where u.id_user in ({listID})";
                        if (filter.keys.Contains("id_department") && !string.IsNullOrEmpty(filter["id_department"]))
                            sql += " and id_department=" + filter["id_department"];
                        if (filter.keys.Contains("id_project_team") && !string.IsNullOrEmpty(filter["id_project_team"]))
                            sql += " and id_project_team=" + filter["id_project_team"];
                    }
                    DataTable dt = new DataTable();
                    if (sql != "")
                    {
                        dt = cnn.CreateDataTable(sql);
                        if (cnn.LastError != null || dt == null)
                            return JsonResultCommon.Exception(_logger, cnn.LastError, _config, loginData, ControllerContext);
                        List<string> nvs = dt.AsEnumerable().Select(x => x["id_nv"].ToString()).ToList();
                        List<AccUsernameModel> listTemp = new List<AccUsernameModel>();
                        foreach (var item in nvs)
                        {
                            var info = DataAccount.Where(x => item.Contains(x.UserId.ToString())).FirstOrDefault();
                            if (info != null)
                            {
                                listTemp.Add(info);
                            }
                        }
                        if (listTemp != null)
                        {
                            DataAccount = listTemp;
                        }
                    }
                    var danhsach = (from r in DataAccount
                                    select new
                                    {
                                        id_nv = r.UserId,
                                        hoten = r.FullName,
                                        username = r.Username,
                                        mobile = r.PhoneNumber,
                                        tenchucdanh = r.Jobtitle,
                                        image = r.AvartarImgURL,
                                        Email = r.Email,
                                    }).Distinct();
                    return JsonResultCommon.ThanhCong(danhsach);
                }
            }
            catch (Exception ex)
            {
                return JsonResultCommon.Exception(_logger, ex, _config, loginData);
            }
        }
        public static DataTable List_Account_HR(long CocauID, IHeaderDictionary pHeader, IConfiguration _configuration)
        {
            List<AccUsernameModel> DataAccount;
            DataTable dt = new DataTable();
            #region Lấy dữ liệu account từ JeeAccount
            DataAccount = GetAccountFromJeeAccount(pHeader, _configuration);
            if (DataAccount == null)
                return new DataTable();
            #endregion

            dt.Columns.Add("Id_NV");
            dt.Columns.Add("hoten");
            dt.Columns.Add("Mobile");
            dt.Columns.Add("Username");
            dt.Columns.Add("Email");
            dt.Columns.Add("CocauID");
            dt.Columns.Add("ParentID");
            dt.Columns.Add("Id_Chucdanh");
            dt.Columns.Add("Tenchucdanh");
            foreach (var item in DataAccount)
            {
                dt.Rows.Add(item.UserId, item.FullName, item.PhoneNumber, item.Username, item.Email, "", "", "", item.Jobtitle);
            }

            return dt;
        }
        /// <summary>
        /// DS emotion + ds account để replace vào comment
        /// </summary>
        /// <returns></returns>
        [Route("get-dicionary")]
        [HttpGet]
        public object getDictionary()
        {
            UserJWT loginData = Ulities.GetUserByHeader(HttpContext.Request.Headers);
            if (loginData == null)
                return JsonResultCommon.DangNhap();
            try
            {
                string domain = _configuration.GetValue<string>("Host:JeeWork_API") + "/";
                string ConnectionString = getConnectionString(ConnectionCache, loginData.CustomerID, _configuration);
                using (DpsConnection cnn = new DpsConnection(ConnectionString))
                {
                    #region Lấy dữ liệu account từ JeeAccount
                    DataAccount = GetAccountFromJeeAccount(HttpContext.Request.Headers, _configuration);
                    if (DataAccount == null)
                        return JsonResultCommon.Custom("Lỗi lấy danh sách nhân viên từ hệ thống quản lý tài khoản");
                    string error = "";
                    string listID = ListAccount(HttpContext.Request.Headers, out error, _configuration);
                    if (error != "")
                        return JsonResultCommon.Custom(error);
                    #endregion
                    string sql = ";select * from we_emotion";
                    sql += ";select * from we_like_icon where disabled=0";
                    DataSet ds = cnn.CreateDataSet(sql);
                    if (cnn.LastError != null || ds == null)
                        return JsonResultCommon.Exception(_logger, cnn.LastError, _config, loginData, ControllerContext);
                    var emotions = new List<object>();
                    foreach (DataRow r in ds.Tables[0].Rows)
                    {
                        string folderName = r["path"].ToString();
                        string Base_Path = Path.Combine(_hostingEnvironment.ContentRootPath, folderName);
                        var dr = new DirectoryInfo(Base_Path);
                        FileInfo[] files = dr.GetFiles();
                        var temp = (from f in files
                                    select new
                                    {
                                        key = ":" + f.Name.Split('.')[0] + ":",
                                        value = domain + folderName + f.Name
                                    }).ToList();
                        emotions.AddRange(temp);
                    }
                    var accounts = DataAccount.Select(x => new
                    {
                        key = "@" + x.Username,
                        value = x.FullName
                    }).ToList();
                    var data = new
                    {
                        emotions = emotions,
                        accounts = accounts,
                        icons = ds.Tables[1].AsEnumerable().Select(x => new
                        {
                            id_row = x["id_row"],
                            title = x["title"],
                            icon = "assets/media/icons/" + x["icon"],
                        })
                    };
                    return JsonResultCommon.ThanhCong(data);
                }
            }
            catch (Exception ex)
            {
                return JsonResultCommon.Exception(_logger, ex, _config, loginData);
            }
        }
        [Route("lite_emotion")]
        [HttpGet]
        public object LiteEmotion(int id = 0, string keyword = "")
        {

            UserJWT loginData = Ulities.GetUserByHeader(HttpContext.Request.Headers);
            if (loginData == null)
                return JsonResultCommon.DangNhap();
            try
            {
                string ConnectionString = getConnectionString(ConnectionCache, loginData.CustomerID, _configuration);
                using (DpsConnection cnn = new DpsConnection(ConnectionString))
                {
                    string sql = "select * from we_emotion";
                    if (id > 0)
                        sql += " where id_row=" + id;
                    DataTable dt = cnn.CreateDataTable(sql);
                    if (cnn.LastError != null || dt == null)
                        return JsonResultCommon.Exception(_logger, cnn.LastError, _config, loginData, ControllerContext);
                    string domain = _configuration.GetValue<string>("Host:JeeWork_API") + "/";
                    var data = new List<object>();
                    foreach (DataRow r in dt.Rows)
                    {
                        string folderName = r["path"].ToString();
                        string Base_Path = Path.Combine(_hostingEnvironment.ContentRootPath, folderName);
                        var dr = new DirectoryInfo(Base_Path);
                        FileInfo[] files = dr.GetFiles();
                        data.Add(new
                        {
                            id_row = r["id_row"],
                            title = r["title"],
                            icons = from f in files
                                    where string.IsNullOrEmpty(keyword) || (!string.IsNullOrEmpty(keyword) && f.Name.Contains(keyword))
                                    select new
                                    {
                                        key = ":" + f.Name.Split('.')[0] + ":",
                                        path = domain + folderName + f.Name
                                    }
                        });
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
        /// DS tag by id_project_team and id_work
        /// </summary>
        /// <returns></returns>
        [Route("work_tag")]
        [HttpGet]
        public object work_tag(long id_work, long id_project_team)
        {

            UserJWT loginData = Ulities.GetUserByHeader(HttpContext.Request.Headers);
            if (loginData == null)
                return JsonResultCommon.DangNhap();
            try
            {
                if (id_work <= 0)
                    return new List<string>();
                string ConnectionString = getConnectionString(ConnectionCache, loginData.CustomerID, _configuration);
                using (DpsConnection cnn = new DpsConnection(ConnectionString))
                {
                    SqlConditions cond = new SqlConditions();
                    cond.Add("tag.Disabled", 0);
                    cond.Add("work_tag.Disabled", 0);
                    cond.Add("id_work", id_work);
                    if (id_project_team > 0)
                        cond.Add("id_project_team", id_project_team);
                    string sql = @"select tag.id_row, id_project_team, title, color, tag.CreatedDate
                                    , tag.CreatedBy, tag.Disabled, tag.UpdatedDate, tag.UpdatedBy
                                    from we_work_tag work_tag join we_tag tag
                                    on tag.id_row = work_tag.id_tag
                                    where (where)";
                    DataTable dt = cnn.CreateDataTable(sql, "(where)", cond);
                    if (cnn.LastError != null || dt == null)
                        return JsonResultCommon.Exception(_logger, cnn.LastError, _config, loginData, ControllerContext);
                    var data = from r in dt.AsEnumerable()
                               select new
                               {
                                   id_row = r["id_row"],
                                   title = r["title"],
                                   color = r["color"]
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
        /// Bind icon cho tên dự án (Trường hợp dự án chưa có icon)
        /// </summary>
        /// <returns></returns>
        [Route("get-color-name")]
        [HttpGet]
        public object getColorName(string name)
        {

            UserJWT loginData = Ulities.GetUserByHeader(HttpContext.Request.Headers);
            string result = "";
            if (loginData == null)
                return JsonResultCommon.DangNhap();
            try
            {
                switch (name)
                {
                    case "A":
                        result = "rgb(197, 90, 240)";
                        break;
                    case "Ă":
                        result = "rgb(241, 196, 15)";
                        break;
                    case "Â":
                        result = "rgb(142, 68, 173)";
                        break;
                    case "B":
                        result = "#02c7ad";
                        break;
                    case "C":
                        result = "#0cb929";
                        break;
                    case "D":
                        result = "rgb(44, 62, 80)";
                        break;
                    case "Đ":
                        result = "rgb(127, 140, 141)";
                        break;
                    case "E":
                        result = "rgb(26, 188, 156)";
                        break;
                    case "Ê":
                        result = "rgb(51 152 219)";
                        break;
                    case "G":
                        result = "rgb(44, 62, 80)";
                        break;
                    case "H":
                        result = "rgb(248, 48, 109)";
                        break;
                    case "I":
                        result = "rgb(142, 68, 173)";
                        break;
                    case "K":
                        result = "#2209b7";
                        break;
                    case "L":
                        result = "#759e13";
                        break;
                    case "M":
                        result = "rgb(236, 157, 92)";
                        break;
                    case "N":
                        result = "#bd3d0a";
                        break;
                    case "O":
                        result = "rgb(51 152 219)";
                        break;
                    case "Ô":
                        result = "rgb(241, 196, 15)";
                        break;
                    case "Ơ":
                        result = "rgb(142, 68, 173)";
                        break;
                    case "P":
                        result = "rgb(142, 68, 173)";
                        break;
                    case "Q":
                        result = "rgb(91, 101, 243)";
                        break;
                    case "R":
                        result = "rgb(44, 62, 80)";
                        break;
                    case "S":
                        result = "rgb(122, 8, 56)";
                        break;
                    case "T":
                        result = "rgb(120, 76, 240)";
                        break;
                    case "U":
                        result = "rgb(51, 152, 219)";
                        break;
                    case "Ư":
                        result = "rgb(241, 196, 15)";
                        break;
                    case "V":
                        result = "rgb(142, 68, 173)";
                        break;
                    case "X":
                        result = "rgb(142, 68, 173)";
                        break;
                    case "W":
                        result = "rgb(211, 84, 0)";
                        break;
                }
                var data = new
                {
                    Color = result
                };
                return JsonResultCommon.ThanhCong(data);
            }
            catch (Exception ex)
            {
                return JsonResultCommon.Exception(_logger, ex, _config, loginData);
            }
        }
        /// <summary>
        /// Lấy danh sách field động theo id_project_team, isnewfield: Field bên ngoài không select trong DB tùy mục đích người dùng
        /// </summary>
        /// <param name="id_project_team"></param>
        /// <param name="isnewfield"></param>
        /// <returns></returns>
        [Route("list-field")]
        [HttpGet]
        public object ListFields(long id_project_team, bool isnewfield)
        {

            UserJWT loginData = Ulities.GetUserByHeader(HttpContext.Request.Headers);
            if (loginData == null)
                return JsonResultCommon.DangNhap();
            try
            {
                string ConnectionString = getConnectionString(ConnectionCache, loginData.CustomerID, _configuration);
                using (DpsConnection cnn = new DpsConnection(ConnectionString))
                {
                    DataTable dt = GetListField(id_project_team, ConnectionString);
                    if (cnn.LastError != null || dt == null)
                        return JsonResultCommon.Exception(_logger, cnn.LastError, _config, loginData, ControllerContext);

                    var data = from r in dt.AsEnumerable()
                               select new
                               {
                                   id_project_team = r["id_project_team"],
                                   fieldname = r["fieldname"],
                                   position = r["position"],
                                   type = r["type"],
                                   isnewfield = r["isnewfield"],
                                   Id_row = r["id_row"],
                                   Title = r["title"],
                                   Title_NewField = r["Title_NewField"],
                                   IsHidden = r["IsHidden"],
                                   show_default_type = r["show_default_type"]
                               };
                    data.OrderBy(x => x.position).ThenByDescending(x => x.id_project_team);
                    return JsonResultCommon.ThanhCong(data);
                }
            }
            catch (Exception ex)
            {
                return JsonResultCommon.Exception(_logger, ex, _config, loginData);
            }
        }
        /// <summary>
        /// Danh sách field mới
        /// </summary>
        /// <returns></returns>
        [Route("list-new-field")]
        [HttpGet]
        public object ListNewFields()
        {

            UserJWT loginData = Ulities.GetUserByHeader(HttpContext.Request.Headers);
            if (loginData == null)
                return JsonResultCommon.DangNhap();
            try
            {
                string ConnectionString = getConnectionString(ConnectionCache, loginData.CustomerID, _configuration);
                using (DpsConnection cnn = new DpsConnection(ConnectionString))
                {
                    string sql = "select * from we_fields where isnewfield = 1";
                    DataTable dt = cnn.CreateDataTable(sql);
                    if (cnn.LastError != null || dt == null)
                        return JsonResultCommon.Exception(_logger, cnn.LastError, _config, loginData, ControllerContext);
                    var data = from r in dt.AsEnumerable()
                               select new
                               {
                                   fieldname = r["fieldname"],
                                   position = r["position"],
                                   type = r["type"],
                                   isnewfield = r["isnewfield"],
                                   title = r["title"],
                                   typeid = r["TypeID"],
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
        /// Danh sách field mới
        /// </summary>
        /// <returns></returns>
        [Route("list-field-template")]
        [HttpGet]
        public object GetListFields()
        {

            UserJWT loginData = Ulities.GetUserByHeader(HttpContext.Request.Headers);
            if (loginData == null)
                return JsonResultCommon.DangNhap();
            try
            {
                string ConnectionString = getConnectionString(ConnectionCache, loginData.CustomerID, _configuration);
                using (DpsConnection cnn = new DpsConnection(ConnectionString))
                {
                    string sql = @"select id_field, fieldname, title, isvisible, note, type, position
                                , isNewField, IsDefault, TypeID, IsDel
                                from we_fields
                                where (isNewField = 0) and (IsDel = 0)
                                order by IsDefault DESC";
                    DataTable dt = cnn.CreateDataTable(sql);
                    if (cnn.LastError != null || dt == null)
                        return JsonResultCommon.Exception(_logger, cnn.LastError, _config, loginData, ControllerContext);
                    var data = from r in dt.AsEnumerable()
                               select new
                               {
                                   id_field = r["id_field"],
                                   fieldname = r["fieldname"],
                                   title = r["title"],
                                   isvisible = r["isvisible"],
                                   note = r["note"],
                                   type = r["type"],
                                   position = r["position"],
                                   isnewfield = r["isnewfield"],
                                   isdefault = r["isdefault"],
                                   typeid = r["typeid"]
                               };

                    return JsonResultCommon.ThanhCong(data);
                }
            }
            catch (Exception ex)
            {
                return JsonResultCommon.Exception(_logger, ex, _config, loginData);
            }
        }

        [Route("list-processing")]
        [HttpGet]
        public object ListProcessing()
        {

            UserJWT loginData = Ulities.GetUserByHeader(HttpContext.Request.Headers);
            if (loginData == null)
                return JsonResultCommon.DangNhap();
            try
            {
                string ConnectionString = getConnectionString(ConnectionCache, loginData.CustomerID, _configuration);
                using (DpsConnection cnn = new DpsConnection(ConnectionString))
                {
                    DataTable dt = cnn.CreateDataTable("select id_row, title, ColumnName, description, priority, Disabled " +
                        "from we_list_processing " +
                        "where Disabled = 0 " +
                        "order by priority");
                    if (cnn.LastError != null || dt == null)
                        return JsonResultCommon.Exception(_logger, cnn.LastError, _config, loginData, ControllerContext);
                    var data = from r in dt.AsEnumerable()
                               select new
                               {
                                   Id_row = r["id_project_team"],
                                   Title = r["fieldname"],
                                   ColumnName = r["ColumnName"],
                                   Description = r["type"],
                                   Priority = r["priority"],
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
        /// Lấy danh sách tất cả trạng thái theo dự
        /// </summary>
        /// <param name="id_project_team"></param>
        /// <param name="isnewfield"></param>
        /// <returns></returns>
        [Route("list-status-dynamic")]
        [HttpGet]
        public object ListStatusDynamic(long id_project_team)
        {

            UserJWT loginData = Ulities.GetUserByHeader(HttpContext.Request.Headers);
            if (loginData == null)
                return JsonResultCommon.DangNhap();
            try
            {
                string ConnectionString = getConnectionString(ConnectionCache, loginData.CustomerID, _configuration);
                using (DpsConnection cnn = new DpsConnection(ConnectionString))
                {
                    #region Lấy dữ liệu account từ JeeAccount
                    DataAccount = GetAccountFromJeeAccount(HttpContext.Request.Headers, _configuration);
                    if (DataAccount == null)
                        return JsonResultCommon.Custom("Lỗi lấy danh sách nhân viên từ hệ thống quản lý tài khoản");

                    string error = "";
                    string listID = ListAccount(HttpContext.Request.Headers, out error, _configuration);
                    if (error != "")
                        return JsonResultCommon.Custom(error);
                    #endregion

                    DataTable dt = StatusDynamic(id_project_team, DataAccount, cnn);
                    DataTable dt_work = cnn.CreateDataTable("select id_row, title, status " +
                        "from we_work " +
                        "where disabled = 0 and id_project_team = " + id_project_team + "");
                    //khancap_quantrong = hasValue ? dt.Compute("count( id_row)", " level = 1") : 0,
                    dt.Columns.Add("SL_Tasks", typeof(double));
                    if (dt_work.Rows.Count > 0)
                    {
                        foreach (DataRow item in dt.Rows)
                        {
                            DataRow[] row = dt_work.Select("status=" + item["id_row"].ToString());
                            if (row.Length > 0)
                            {
                                item["SL_Tasks"] = (int)dt_work.Compute("count(id_row)", " status = " + item["id_row"] + "");
                            }
                            else
                            {
                                item["SL_Tasks"] = 0;
                            }
                        }
                    }
                    dt.AcceptChanges();
                    if (cnn.LastError != null || dt == null)
                        return JsonResultCommon.Exception(_logger, cnn.LastError, _config, loginData, ControllerContext);

                    var data = from r in dt.AsEnumerable()
                               select new
                               {
                                   id_row = r["id_row"],
                                   statusname = r["StatusName"],
                                   id_project_team = r["id_project_team"],
                                   isdefault = r["IsDefault"],
                                   color = r["color"],
                                   position = r["Position"],
                                   IsFinal = r["IsFinal"],
                                   Follower = r["Follower"],
                                   hoten_Follower = r["hoten_Follower"],
                                   IsDeadline = r["IsDeadline"],
                                   IsToDo = r["IsToDo"],
                                   Description = r["description"],
                                   SL_Tasks = r["SL_Tasks"],
                                   Type = r["Type"],
                               };
                    return JsonResultCommon.ThanhCong(data);
                }
            }
            catch (Exception ex)
            {
                return JsonResultCommon.Exception(_logger, ex, _config, loginData);
            }
        }

        [Route("list-status-dynamic-bydepartment")]
        [HttpGet]
        public object ListStatusDynamicByDepartment(long id_department)
        {

            UserJWT loginData = Ulities.GetUserByHeader(HttpContext.Request.Headers);
            if (loginData == null)
                return JsonResultCommon.DangNhap();
            try
            {
                string ConnectionString = getConnectionString(ConnectionCache, loginData.CustomerID, _configuration);
                using (DpsConnection cnn = new DpsConnection(ConnectionString))
                {
                    #region Lấy dữ liệu account từ JeeAccount
                    DataAccount = GetAccountFromJeeAccount(HttpContext.Request.Headers, _configuration);
                    if (DataAccount == null)
                        return JsonResultCommon.Custom("Lỗi lấy danh sách nhân viên từ hệ thống quản lý tài khoản");

                    string error = "";
                    string listID = ListAccount(HttpContext.Request.Headers, out error, _configuration);
                    if (error != "")
                        return JsonResultCommon.Custom(error);
                    #endregion
                    string query = "";
                    query = $@"select id_row, StatusName, description, id_project_team,IsToDo
                    ,Type, IsDefault, color, Position, IsFinal, Follower, IsDeadline, '' as hoten_Follower
                    from we_status 
                    where Disabled = 0 and  id_project_team in ( select distinct p.id_row from we_project_team p
join we_department d on d.id_row = p.id_department
join we_project_team_user u on u.id_project_team = p.id_row
where u.Disabled = 0 and id_user = {loginData.UserID} 
and p.Disabled = 0  and d.Disabled = 0 and  ( d.id_row= {id_department} or d.ParentID = {id_department} )
and IdKH={loginData.CustomerID} )";
                    query += " order by IsFinal,id_row";

                    DataTable dt = cnn.CreateDataTable(query);

                    if (cnn.LastError != null || dt == null)
                        return JsonResultCommon.Exception(_logger, cnn.LastError, _config, loginData, ControllerContext);
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
                    var data = from r in dt.AsEnumerable()
                               select new
                               {
                                   id_row = r["id_row"],
                                   statusname = r["StatusName"],
                                   id_project_team = r["id_project_team"],
                                   isdefault = r["IsDefault"],
                                   color = r["color"],
                                   position = r["Position"],
                                   IsFinal = r["IsFinal"],
                                   Follower = r["Follower"],
                                   hoten_Follower = r["hoten_Follower"],
                                   IsDeadline = r["IsDeadline"],
                                   IsToDo = r["IsToDo"],
                                   Description = r["description"],
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
        /// Danh sách tất cả status (Không phân biệt project) dùng để load cho Công việc cá nhân
        /// </summary>
        /// <returns></returns>
        [Route("list-all-status-dynamic")]
        [HttpGet]
        public object ListAllStatusDynamic()
        {

            UserJWT loginData = Ulities.GetUserByHeader(HttpContext.Request.Headers);
            if (loginData == null)
                return JsonResultCommon.DangNhap();
            try
            {
                string query = "";
                query = "select id_row, title, color, locked from we_project_team where Disabled = 0;" +
                    "select id_row, StatusName, description, id_project_team, Type, IsDefault, color, Position, IsFinal, Follower, IsDeadline, IsToDo " +
                    "from we_status where Disabled = 0";
                string ConnectionString = getConnectionString(ConnectionCache, loginData.CustomerID, _configuration);
                using (DpsConnection cnn = new DpsConnection(ConnectionString))
                {
                    DataSet ds = cnn.CreateDataSet(query);
                    DataTable dt = ds.Tables[0];
                    if (cnn.LastError != null || dt == null)
                        return JsonResultCommon.Exception(_logger, cnn.LastError, _config, loginData, ControllerContext);
                    var data = from r in dt.AsEnumerable()
                               select new
                               {
                                   id_row = r["id_row"],
                                   title = r["title"],
                                   color = r["color"],
                                   locked = r["locked"],
                                   status = from s in ds.Tables[1].AsEnumerable()
                                            where s["id_project_team"].ToString() == r["id_row"].ToString()
                                            select new
                                            {
                                                id_row = s["id_row"],
                                                statusname = s["StatusName"],
                                                id_project_team = s["id_project_team"],
                                                isdefault = s["IsDefault"],
                                                color = s["color"],
                                                position = s["Position"],
                                                IsFinal = s["IsFinal"],
                                                Follower = s["Follower"],
                                                IsToDo = s["IsToDo"],
                                                IsDeadline = s["IsDeadline"],
                                                Description = s["Description"],
                                            },
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
        /// Danh sách field mới
        /// </summary>
        /// <param name="id_project_team"></param>
        /// <param name="fieldID"></param>
        /// <returns></returns>
        [Route("get-options-new-field")]
        [HttpGet]
        public object GetOptions_NewField(long id_project_team, long fieldID)
        {

            UserJWT loginData = Ulities.GetUserByHeader(HttpContext.Request.Headers);
            if (loginData == null)
                return JsonResultCommon.DangNhap();
            try
            {
                string ConnectionString = getConnectionString(ConnectionCache, loginData.CustomerID, _configuration);
                using (DpsConnection cnn = new DpsConnection(ConnectionString))
                {
                    string sqlq = "";
                    SqlConditions conditions = new SqlConditions();
                    conditions.Add("Disabled", 0);
                    conditions.Add("we_fields_project_team.id_project_team", id_project_team);
                    sqlq = $@"select we_newfields_options.RowID, we_fields_project_team.id_project_team,we_newfields_options.FieldID,
                            fieldname, title, Value, position, Color, Note, IsNewField
                            from we_newfields_options join we_fields_project_team
                            on we_fields_project_team.id_row = we_newfields_options.FieldID
                            where (where)";
                    DataTable dt = cnn.CreateDataTable(sqlq, "(where)", conditions);
                    if (cnn.LastError != null || dt == null)
                        return JsonResultCommon.Exception(_logger, cnn.LastError, _config, loginData, ControllerContext);
                    var data = from r in dt.AsEnumerable()
                               select new
                               {
                                   RowID = r["RowID"],
                                   Id_project_team = r["id_project_team"],
                                   Fieldname = r["fieldname"],
                                   Position = r["position"],
                                   Color = r["Color"],
                                   Isnewfield = r["isnewfield"],
                                   Value = r["Value"],
                                   Title = r["title"],
                                   FieldID = r["FieldID"],
                               };
                    data.OrderBy(x => x.Position).ThenByDescending(x => x.Id_project_team);
                    return JsonResultCommon.ThanhCong(data);
                }
            }
            catch (Exception ex)
            {
                return JsonResultCommon.Exception(_logger, ex, _config, loginData);
            }
        }
        /// <summary>
        /// Danh sách view mặc định
        /// </summary>
        /// <param name="filter"></param>
        /// <returns></returns>
        [Route("get-list-default-view")]
        [HttpGet]
        public object GetListDefaultView([FromQuery] FilterModel filter)
        {
            //this.item.RowID
            UserJWT loginData = Ulities.GetUserByHeader(HttpContext.Request.Headers);
            if (loginData == null)
                return JsonResultCommon.DangNhap();
            try
            {
                string domain = _configuration.GetValue<string>("Host:JeeWork_API") + "/";
                string ConnectionString = getConnectionString(ConnectionCache, loginData.CustomerID, _configuration);
                using (DpsConnection cnn = new DpsConnection(ConnectionString))
                {
                    string sqlq = ""; string getValue = ""; string sql_cmd = "";
                    string column_name = ", '' as DefaultView, '' as ObjectID, '' as view_id";
                    sqlq = $@"select _view.id_row, _view.view_name, _view.description, _view.is_default, _view.icon " + column_name + " " +
                        "from we_default_views _view";
                    if (filter != null && filter.keys != null)
                    {
                        if (filter.keys.Contains("id_department") && !string.IsNullOrEmpty(filter["id_department"]) && int.Parse(filter["id_department"]) > 0)
                        {
                            //sqlq_default += " and id_department=" + filter["id_department"];
                            sql_cmd = "select id_row, id_department, viewid, disabled, is_default " +
                                "from we_department_view " +
                                "where disabled = 0";
                            getValue = cnn.ExecuteScalar(sql_cmd).ToString();
                            if (getValue != null || getValue != "")
                            {
                                column_name = ",id_row as view_id,id_department as ObjectID,is_default as DefaultView,viewid";
                                sqlq += " left join we_department_view de " +
                                    "on de.viewid = _view.id_row and de.id_department = " + filter["id_department"] + " " +
                                    "where disabled = 0";
                            }
                        }
                        if (filter.keys.Contains("id_project_team") && !string.IsNullOrEmpty(filter["id_project_team"]))
                        {
                            //sqlq += " and id_project_team=" + filter["id_project_team"];
                            sql_cmd = "select id_row, id_project_team, viewid, disabled, is_default " +
                                "from we_projects_view " +
                                "where disabled = 0";
                            getValue = cnn.ExecuteScalar(sql_cmd).ToString();
                            if (getValue != null || getValue != "")
                            {
                                column_name = ",id_row as view_id,id_project_team as ObjectID,is_default as DefaultView,viewid";
                                sqlq += " left join we_projects_view pr " +
                                    "on pr.viewid = _view.id_row and pr.id_project_team = " + filter["id_project_team"] + " " +
                                    "where disabled = 0";
                            }
                        }
                    }
                    DataTable dt = cnn.CreateDataTable(sqlq);
                    if (cnn.LastError != null || dt == null)
                        return JsonResultCommon.Exception(_logger, cnn.LastError, _config, loginData, ControllerContext);
                    var temp = dt.AsEnumerable();
                    var data = (from r in temp
                                select new
                                {
                                    id_row = r["id_row"],
                                    view_name = r["view_name"],
                                    description = r["description"],
                                    is_default = r["is_default"],
                                    icon = r["icon"],
                                    DefaultView = r["DefaultView"],
                                    ObjectID = r["ObjectID"],
                                    view_id = r["view_id"],
                                }).Distinct();
                    return JsonResultCommon.ThanhCong(data);
                }
            }
            catch (Exception ex)
            {
                return JsonResultCommon.Exception(_logger, ex, _config, loginData);
            }
        }
        /// <summary>
        /// DS template theo khách hàng
        /// </summary>
        /// <returns></returns>
        [Route("lite_template_by_customer")]
        [HttpGet]
        public object ListTemplateByCustomer()
        {

            UserJWT loginData = Ulities.GetUserByHeader(HttpContext.Request.Headers);
            if (loginData == null)
                return JsonResultCommon.DangNhap();
            try
            {
                string ConnectionString = getConnectionString(ConnectionCache, loginData.CustomerID, _configuration);
                using (DpsConnection cnn = new DpsConnection(ConnectionString))
                {
                    SqlConditions conds = new SqlConditions(); string sql = "";
                    conds.Add("Disabled", 0);
                    conds.Add("is_template_center", 0);
                    conds.Add("CustomerID", loginData.CustomerID);
                    sql = "select id_row, Title, Description, IsDefault, Color, id_department, TemplateID, CustomerID " +
                        "from we_template_customer " +
                        "where (where) order by Title";
                    //Check CustommerID có template chưa nếu chưa thì thêm vào
                    cnn.BeginTransaction();
                    #region ktra k có thì thêm mới danh sách template cho kh mới
                    int soluong = int.Parse(cnn.ExecuteScalar("select count(*) from we_template_customer where Disabled = 0 and is_template_center =0 and CustomerID = " + loginData.CustomerID).ToString());
                    if (soluong == 0)
                    {
                        DataTable dt_listSTT = cnn.CreateDataTable("select * from we_template_list where is_template_center <>1 and disabled = 0");
                        Hashtable val = new Hashtable();
                        foreach (DataRow item in dt_listSTT.Rows)
                        {
                            val["Title"] = item["Title"];
                            val["Description"] = item["Description"];
                            val["TemplateID"] = item["id_row"];
                            val["CustomerID"] = loginData.CustomerID;
                            val["CreatedDate"] = DateTime.Now;
                            val["CreatedBy"] = loginData.UserID;
                            if (cnn.Insert(val, "we_template_customer") != 1)
                            {
                                cnn.RollbackTransaction();
                                return JsonResultCommon.Exception(_logger, cnn.LastError, _config, loginData, ControllerContext);
                            }
                            string idc = cnn.ExecuteScalar("select IDENT_CURRENT('we_template_customer')").ToString();
                            string sql_insert = "";
                            sql_insert = $@"insert into we_template_status (StatusID, TemplateID, StatusName, description, CreatedDate, CreatedBy, Disabled, Type, IsDefault, color, Position, IsFinal, IsDeadline, IsTodo) " +
                                "select id_Row, " + idc + ", StatusName, description, getdate(), 0, Disabled, Type, IsDefault, color, Position, IsFinal, IsDeadline, IsTodo " +
                                "from we_status_list where disabled = 0 and id_template_list = " + item["id_row"] + "";
                            cnn.ExecuteNonQuery(sql_insert);
                            if (cnn.LastError != null)
                            {
                                cnn.RollbackTransaction();
                                return JsonResultCommon.Exception(_logger, cnn.LastError, _config, loginData, ControllerContext);
                            }

                        }
                    }
                    #endregion
                    cnn.EndTransaction();
                    DataTable dt_template = cnn.CreateDataTable(sql, "(where)", conds);
                    if (cnn.LastError != null || dt_template == null)
                        return JsonResultCommon.Exception(_logger, cnn.LastError, _config, loginData, ControllerContext);
                    string sql_status = "";
                    sql_status = "select Id_row, StatusID, TemplateID, StatusName, description, CreatedDate, Type, IsDefault, color, Position, IsFinal, IsDeadline, IsTodo " +
                        "from we_Template_Status where Disabled = 0 and TemplateID in (select id_row from we_template_customer where Disabled = 0 and CustomerID = " + loginData.CustomerID + ")";
                    DataTable dt_status = cnn.CreateDataTable(sql_status);
                    var data = from r in dt_template.AsEnumerable()
                               select new
                               {
                                   id_row = r["id_row"],
                                   title = r["title"],
                                   description = r["Description"],
                                   isdefault = r["IsDefault"],
                                   color = r["color"],
                                   templateid = r["TemplateID"],
                                   customerid = r["CustomerID"],
                                   status = from dr in dt_status.AsEnumerable()
                                            where dr["TemplateID"].Equals(r["id_row"])
                                            select new
                                            {
                                                id_row = dr["id_row"],
                                                StatusID = dr["StatusID"],
                                                TemplateID = dr["TemplateID"],
                                                StatusName = dr["StatusName"],
                                                description = dr["description"],
                                                CreatedDate = dr["CreatedDate"],
                                                IsDefault = dr["IsDefault"],
                                                color = dr["color"],
                                                Position = dr["Position"],
                                                IsFinal = dr["IsFinal"],
                                                IsDeadline = dr["IsDeadline"],
                                                IsTodo = dr["IsTodo"],
                                            }
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
        /// Danh sách view theo project
        /// </summary>
        /// <param name="id_project_team"></param>
        /// <returns></returns>
        [Route("list-view-project")]
        [HttpGet]
        public object ListViewByProject(long id_project_team)
        {

            UserJWT loginData = Ulities.GetUserByHeader(HttpContext.Request.Headers);
            if (loginData == null)
                return JsonResultCommon.DangNhap();
            try
            {
                string ConnectionString = getConnectionString(ConnectionCache, loginData.CustomerID, _configuration);
                using (DpsConnection cnn = new DpsConnection(ConnectionString))
                {
                    string sqlq = "";
                    //SqlConditions conds = new SqlConditions();
                    //conds.Add("disabled", 0);
                    //conds.Add("id_project_team", id_project_team);
                    sqlq = @"select wp.id_row, wp.id_project_team, wp.viewid, dv.id_row as dv_id
                            , wp.view_name_new, dv.view_name, wp.default_everyone, wp.default_for_me, 
                             wp.pin_view, wp.personal_view, wp.favourite, disabled, id_department,dv.icon, dv.link, image, default_view
                            from we_default_views dv left join we_projects_view wp 
                            on wp.viewid = dv.id_row 
                            and disabled = 0 and id_project_team = " + id_project_team + "";
                    DataTable dt = cnn.CreateDataTable(sqlq);
                    if (dt.Rows.Count <= 0)
                    {
                        Init_DefaultView_Project(id_project_team, cnn);
                    }
                    dt = cnn.CreateDataTable(sqlq);
                    if (cnn.LastError != null || dt == null)
                        return JsonResultCommon.Exception(_logger, cnn.LastError, _config, loginData, ControllerContext);

                    var data = from r in dt.AsEnumerable()
                               select new
                               {
                                   id_row = r["id_row"],
                                   id_project_team = r["id_project_team"],
                                   viewid = !string.IsNullOrEmpty(r["viewid"].ToString()) ? r["viewid"] : r["dv_id"],
                                   view_name_new = !string.IsNullOrEmpty(r["view_name_new"].ToString()) ? r["view_name_new"] : r["view_name"],
                                   default_everyone = r["default_everyone"],
                                   default_for_me = r["default_for_me"],
                                   pin_view = r["pin_view"],
                                   personal_view = r["personal_view"],
                                   favourite = r["favourite"],
                                   id_department = r["id_department"],
                                   icon = r["icon"],
                                   link = r["link"],
                                   image = r["image"],
                                   default_view = r["default_view"],
                               };
                    return JsonResultCommon.ThanhCong(data);
                }
            }
            catch (Exception ex)
            {
                return JsonResultCommon.Exception(_logger, ex, _config, loginData);
            }
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
                string ConnectionString = getConnectionString(ConnectionCache, loginData.CustomerID, _configuration);
                using (DpsConnection cnn = new DpsConnection(ConnectionString))
                {
                    #region Lấy dữ liệu account từ JeeAccount
                    DataAccount = GetAccountFromJeeAccount(HttpContext.Request.Headers, _configuration);
                    if (DataAccount == null)
                        return JsonResultCommon.Custom("Lỗi lấy danh sách nhân viên từ hệ thống quản lý tài khoản");
                    string error = "";
                    string listID = ListAccount(HttpContext.Request.Headers, out error, _configuration);
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
                                               //id_nv = u["id_nv"],
                                               hoten = u["hoten"],
                                               username = u["username"],
                                               tenchucdanh = u["tenchucdanh"],
                                               mobile = u["mobile"],
                                               image = u["image"],
                                               favourite = u["favourite"],
                                               //image = genLinkImage(domain, 1119, "16116", _hostingEnvironment.ContentRootPath)
                                           },
                                   Count = new
                                   {
                                       tong = r["tong"],
                                       ht = r["ht"],
                                       quahan = r["quahan"],
                                       percentage = calPercentage(r["tong"], r["ht"])
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
        /// DS loại template
        /// </summary>
        /// <returns></returns>
        [Route("lite_template_types")]
        [HttpGet]
        public object Lite_Template_Types()
        {
            UserJWT loginData = Ulities.GetUserByHeader(HttpContext.Request.Headers);
            if (loginData == null)
                return JsonResultCommon.DangNhap();
            try
            {
                string ConnectionString = getConnectionString(ConnectionCache, loginData.CustomerID, _configuration);
                using (DpsConnection cnn = new DpsConnection(ConnectionString))
                {
                    string sql = "select id_row, title, description, disabled, isdefault, types " +
                        "from we_template_types " +
                        "where isdefault = 1 and disabled = 0 order by title";
                    DataTable dt = cnn.CreateDataTable(sql);
                    if (cnn.LastError != null || dt == null)
                        return JsonResultCommon.Exception(_logger, cnn.LastError, _config, loginData, ControllerContext);
                    var data = from r in dt.AsEnumerable()
                               select new
                               {
                                   id_row = r["id_row"],
                                   title = r["title"],
                                   isdefault = r["isdefault"],
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
        /// DS loại template
        /// </summary>
        /// <returns></returns>
        [Route("lite_department_folder_user")]
        [HttpGet]
        public object Lite_Department_Folder_User()
        {
            UserJWT loginData = Ulities.GetUserByHeader(HttpContext.Request.Headers);
            if (loginData == null)
                return JsonResultCommon.DangNhap();
            try
            {
                #region Lấy dữ liệu account từ JeeAccount
                DataAccount = GetAccountFromJeeAccount(HttpContext.Request.Headers, _configuration);
                if (DataAccount == null)
                    return JsonResultCommon.Custom("Lỗi lấy danh sách nhân viên từ hệ thống quản lý tài khoản");
                string err = "";
                string listID = ListAccount(HttpContext.Request.Headers, out err, _configuration);
                if (err != "")
                    return JsonResultCommon.Custom(err);
                #endregion
                string ConnectionString = getConnectionString(ConnectionCache, loginData.CustomerID, _configuration);
                using (DpsConnection cnn = new DpsConnection(ConnectionString))
                {
                    string listDept = getListDepartment_GetData(loginData, cnn, HttpContext.Request.Headers, _configuration, ConnectionString);

                    string sql = @$"select * from we_department where Disabled = 0 and ParentID is null and id_row in ({listDept}) order by title;
                                            select * from we_department where Disabled = 0 and ParentID is not null and ParentID in ({listDept}) order by title";
                    DataSet ds = cnn.CreateDataSet(sql);
                    string sql_space = "", sql_project = "", sql_folder = "", where_department = "";
                    //if (v_module.ToLower().Equals("module = 'wework'"))
                    //{
                    where_department = @$" disabled = 0 and CreatedBy in ({listID}) 
                                        and IdKH = {loginData.CustomerID} and (id_row in (select id_department from we_project_team 
                                        where (id_row in (select id_project_team from we_project_team_user where id_user = { loginData.UserID}
                                        and Disabled = 0) or (CreatedBy = { loginData.UserID})) and disabled = 0) or (CreatedBy = { loginData.UserID}));";
                    sql_space = @$"select id_row, title, id_cocau, IdKH, priority, disabled, ParentID
                                        from we_department
                                        where ParentID is null and " + where_department + "";
                    sql_project = "select p.id_row, p.icon, p.title, p.detail, p.id_department" +
                        ", p.loai, p.start_date, p.end_date, p.color, p.template, p.status, p.is_project" +
                        ", p.priority, p.CreatedDate, p.CreatedBy, p.Locked, p.Disabled, default_view " +
                        "from we_project_team p (admin_group)" +
                        $" p.Disabled = 0 and p.CreatedBy in ({listID})";
                    //}
                    sql_folder = @$"select id_row, title, id_cocau, IdKH, priority, disabled, ParentID 
                                        from we_department
                                        where ParentID is not null and " + where_department + "";
                    if (!MenuController.CheckGroupAdministrator(loginData.Username, cnn, loginData.CustomerID))
                    {
                        sql_project = sql_project.Replace("(admin_group)", "join we_project_team_user " +
                        "on we_project_team_user.id_project_team = p.id_row " +
                        "and (we_project_team_user.id_user = " + loginData.UserID + ") " +
                        "where we_project_team_user.id_user = " + loginData.UserID + " and ");
                    }
                    else
                    {
                        sql_project = sql_project.Replace("(admin_group)", " where ");
                    }
                    DataTable dt_space = cnn.CreateDataTable(sql_space);
                    DataTable dt_project = cnn.CreateDataTable(sql_project);
                    DataTable dt_folder = cnn.CreateDataTable(sql_folder);
                    if (cnn.LastError != null || ds == null)
                        return JsonResultCommon.Exception(_logger, cnn.LastError, _config, loginData, ControllerContext);
                    var data = from r in dt_space.AsEnumerable()
                               select new
                               {
                                   id_row = r["id_row"],
                                   title = r["title"],
                                   type = 1,
                                   folder = from f in dt_folder.AsEnumerable()
                                            where int.Parse(r["id_row"].ToString()) == int.Parse(f["ParentID"].ToString())
                                            select new
                                            {
                                                id_row = f["id_row"],
                                                title = f["title"],
                                                type = 2,
                                                project = from p in dt_project.AsEnumerable()
                                                          where int.Parse(f["id_row"].ToString()) == int.Parse(p["id_department"].ToString())
                                                          select new
                                                          {
                                                              id_row = p["id_row"],
                                                              title = p["title"],
                                                              type = 3,
                                                          }
                                            },
                                   project = from p in dt_project.AsEnumerable()
                                             where int.Parse(r["id_row"].ToString()) == int.Parse(p["id_department"].ToString())
                                             select new
                                             {
                                                 id_row = p["id_row"],
                                                 title = p["title"],
                                                 type = 3,
                                             }
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
        /// danh sách hành động
        /// </summary>
        /// <returns></returns>
        [Route("lite_automation_actionlist")]
        [HttpGet]
        public object lite_automation_actionlist()
        {
            UserJWT loginData = Ulities.GetUserByHeader(HttpContext.Request.Headers);
            if (loginData == null)
                return JsonResultCommon.DangNhap();
            try
            {
                string ConnectionString = getConnectionString(ConnectionCache, loginData.CustomerID, _configuration);
                using (DpsConnection cnn = new DpsConnection(ConnectionString))
                {
                    string sql = "select rowid, actionname, description, disabled " +
                        "from automation_actionList " +
                        "where disabled = 0 order by actionname";
                    DataTable dt = cnn.CreateDataTable(sql);
                    if (cnn.LastError != null || dt == null)
                        return JsonResultCommon.Exception(_logger, cnn.LastError, _config, loginData, ControllerContext);
                    var data = from r in dt.AsEnumerable()
                               select new
                               {
                                   rowid = r["rowid"],
                                   actionname = r["actionname"],
                                   description = r["description"],
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
        /// danh sách sự kiện
        /// </summary>
        /// <returns></returns>
        [Route("lite_automation_eventlist")]
        [HttpGet]
        public object lite_automation_eventlist()
        {
            UserJWT loginData = Ulities.GetUserByHeader(HttpContext.Request.Headers);
            if (loginData == null)
                return JsonResultCommon.DangNhap();
            try
            {
                string ConnectionString = getConnectionString(ConnectionCache, loginData.CustomerID, _configuration);
                using (DpsConnection cnn = new DpsConnection(ConnectionString))
                {
                    string sql = "select rowid, title, description, tiltelangkey, datacantruyen " +
                        "from automation_eventList " +
                        "order by title";
                    DataTable dt = cnn.CreateDataTable(sql);
                    if (cnn.LastError != null || dt == null)
                        return JsonResultCommon.Exception(_logger, cnn.LastError, _config, loginData, ControllerContext);
                    var data = from r in dt.AsEnumerable()
                               select new
                               {
                                   rowid = r["rowid"],
                                   title = r["title"],
                                   description = r["description"],
                                   tiltelangkey = r["tiltelangkey"],
                                   datacantruyen = r["datacantruyen"],
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
        /// Tính phần trăm
        /// </summary>
        /// <param name="tong"></param>
        /// <param name="v"></param>
        /// <returns></returns>
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
        public static string genLinkAttachment(IConfiguration _configuration, object path)
        {
            string domain = _configuration.GetValue<string>("Host:MinIOBrowser") + "/" + _configuration.GetValue<string>("KafkaConfig:ProjectName") + "/";
            //string domain = _configuration.GetValue<string>("Host:JeeWork_API") +"/";
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
        public static bool log<T>(ILogger<T> logger, string username, DpsConnection cnn, int id_action, long object_id, long id_user, string log_content = "", object _old = null, object _new = null)
        {
            string category = "";
            string action = "";
            if (id_action > 0)
            {
                category = cnn.ExecuteScalar("select action from we_log_action where id_row = " + id_action).ToString();
            }
            if (string.IsNullOrEmpty(log_content))
            {
                action = @$"@{username} {category} từ {_old} thành {_new}";
            }
            else
            {
                action = @$"@{username} {category} : {log_content}";
            }

            var d2 = new ActivityLog()
            {
                username = username,
                category = category,
                action = action,
                data = action,
            };
            logger.LogDebug(JsonConvert.SerializeObject(d2));
            Hashtable val = new Hashtable();
            val["id_action"] = id_action;
            val["object_id"] = object_id;
            val["CreatedBy"] = id_user;
            val["CreatedDate"] = DateTime.Now;
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
            if (cnn.Insert(val, "we_log") == 1)
            {
                return true;
            }
            return true;
            //return cnn.Insert(val, "we_log") == 1;
        }
        public static bool count_comment(long work_id, DpsConnection cnn, bool is_add_comment)
        {
            long num_comment = 0;
            SqlConditions cond = new SqlConditions();
            cond.Add("disabled", 0);
            cond.Add("id_row", work_id);
            object present = cnn.ExecuteScalar("select num_comment from we_work where disabled = @disabled and id_row=@id_row", cond);
            if (string.IsNullOrEmpty(present.ToString()))
                present = 0;
            else
                num_comment = long.Parse(present.ToString());
            if (is_add_comment)
                num_comment = num_comment + 1;
            else
                num_comment = num_comment - 1;
            if (num_comment < 0)
                num_comment = 0;
            Hashtable val = new Hashtable();
            val["num_comment"] = num_comment;
            if (cnn.Update(val, cond, "we_work") != 1)
            {
                return false;
            }
            return true;
        }
        /// <summary>
        /// test kafka
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        [Route("testkafka")]
        public string testkafka()
        {
            string topic = _iconfig.GetValue<string>("KafkaConfig:topicProduceByAccount");
            _producer.PublishAsync(topic, "{\"CustomerID\":31,\"AppCode\":[\"HR\",\"ADMIN\",\"Land\",\"REQ\",\"WF\",\"jee-doc\",\"OFFICE\",\"WW\",\"WMS\",\"TEST\",\"AMS\",\"ACC\"],\"UserID\":76745,\"Username\":\"powerplus.admin\"}");
            return "Oke";
        }


        [HttpGet]
        [Route("roles-by-project")]
        public object GetRoleWeWork(string id_project_team)
        {
            UserJWT loginData = Ulities.GetUserByHeader(HttpContext.Request.Headers);
            if (loginData == null)
            {
                return JsonResultCommon.DangNhap();
            }
            try
            {
                string ConnectionString = getConnectionString(ConnectionCache, loginData.CustomerID, _configuration);
                using (DpsConnection Conn = new DpsConnection(ConnectionString))
                {
                    bool IsAdmin = MenuController.CheckGroupAdministrator(loginData.Username, Conn, loginData.CustomerID);
                    string sqlq = "";
                    string authorize = "";
                    authorize = @$"select createdby from we_authorize where id_user = {loginData.UserID} and disabled =0 and start_date <= GETDATE() and end_date >= GETDATE())";

                    sqlq = "select users.admin, proj.title, proj.description, proj.start_date, proj.end_date, Iif( users.admin = 1 and id_user <> " + loginData.UserID + ",1,0 ) as isuyquyen " +
                        " from we_project_team proj join we_project_team_user users " +
                        "on proj.id_row = users.id_project_team " +
                        "where proj.disabled = 0 " +
                        "and  users.disabled = 0 " +
                        "and locked = 0 and  proj.id_row = " + id_project_team + " " +
                        "and (id_user = " + loginData.UserID + $" " +
                        $"or id_user in (" + authorize + ")";
                    sqlq += @";select id_project_team, id_role, we_role.title
                            , admin, member, keypermit, we_role.icon, is_assign, we_role.[group]
                            from we_project_role proj join we_role on we_role.id_row = proj.id_role
                            where we_role.Disabled = 0 and id_project_team = " + id_project_team + "";
                    DataSet ds = Conn.CreateDataSet(sqlq);
                    DataTable dt_Project = Conn.CreateDataTable(sqlq);
                    if (Conn.LastError != null || ds == null)
                        return JsonResultCommon.Exception(_logger, Conn.LastError, _config, loginData, ControllerContext);
                    DataTable dt = ds.Tables[0];
                    if (dt.Rows.Count == 0)
                        return JsonResultCommon.ThanhCong(new List<string>());
                    var data =
                                new
                                {
                                    is_admin_app = IsAdmin,
                                    title = dt.Rows[0]["title"],
                                    admin_project = dt.Rows[0]["admin"],
                                    isuyquyen = dt.Rows[0]["isuyquyen"],
                                    start_date = dt.Rows[0]["start_date"] == DBNull.Value ? "" : string.Format("{0:dd/MM/yyyy HH:mm}", dt.Rows[0]["start_date"]),
                                    end_date = dt.Rows[0]["end_date"] == DBNull.Value ? "" : string.Format("{0:dd/MM/yyyy HH:mm}", dt.Rows[0]["end_date"]),
                                    data_roles = from u in ds.Tables[1].AsEnumerable()
                                                 where u["id_project_team"].ToString() == id_project_team.ToString()
                                                 select new
                                                 {
                                                     id_project_team = u["id_project_team"],
                                                     id_role = u["id_role"],
                                                     title = u["title"],
                                                     admin = u["admin"],
                                                     member = u["member"],
                                                     group_permit = u["group"],
                                                     is_assign = u["is_assign"],
                                                     keypermit = u["KeyPermit"],
                                                     icon = u["icon"],
                                                 },
                                };
                    var dataNew = new { data = data, IsAdminGroup = IsAdmin };
                    return JsonResultCommon.ThanhCong(dataNew);
                }
            }
            catch (Exception ex)
            {
                return JsonResultCommon.Exception(_logger, ex, _config, loginData);
            }
        }

        [HttpGet]
        [Route("test-getaccount-notoken")]
        public object GetAccountNonuseToken()
        {

            List<long> DanhSachCustomer = GetDanhSachCustomerID(_configuration);
            var data = from r in DanhSachCustomer
                       select new
                       {
                           CustomerID = r,
                           DataAccount = GetDanhSachAccountFromCustomerID(_configuration, r),
                       };
            return data;
        }
        /// <summary>
        /// tesst notify
        /// </summary>
        /// <returns></returns>
        //[HttpGet]
        //[Route("testNotify")]
        //public string testNotify(string title)
        //{
        //    notify.notification("huypad", title);
        //    return "Oke";
        //}

        public static bool SendNotify(string sender, string receivers, NotifyModel notify_model, INotifier notifier, IConfiguration _configuration)
        {
            string jeeglobal_be = _configuration.GetValue<string>("Host:jeeglobal_be");
            string appcode_land = _configuration.GetValue<string>("AppConfig:AppCode_Land");

            notify = new Notification(notifier);
            NotificationMess noti_mess = new NotificationMess();
            noti_mess.AppCode = notify_model.AppCode;
            noti_mess.Content = notify_model.TitleLanguageKey;
            noti_mess.Icon = "https://jeework.jee.vn/assets/images/Jee_Work.png";
            noti_mess.Img = "https://jeework.jee.vn/assets/images/Jee_Work.png";
            noti_mess.Link = notify_model.To_Link_WebApp;
            noti_mess.oslink = notify_model.To_Link_MobileApp;
            string html = "<h1>Gửi nội dung thông báo</h1>";
            notify.notification(sender, receivers, notify_model.TitleLanguageKey, html, noti_mess,_configuration);
            return true;
        }


        /// <summary>
        /// Notify mail
        /// </summary>
        /// <param name="id_template">we_template.id_row</param>
        /// <param name="object_id"></param>
        /// <param name="nguoigui"></param>
        /// <param name="dtUser">gồm id_nv, hoten, email</param>
        /// <returns></returns>
        public static bool NotifyMail(int id_template, long object_id, UserJWT nguoigui, DataTable dtUser, string ConnectionString, INotifier _notifier, List<AccUsernameModel> DataAccount, IConfiguration _configuration, DataTable dtOld = null)
        {
            using (DpsConnection cnn = new DpsConnection(ConnectionString))
            {
                //get template
                string sql = "select * from we_template where id_row=" + id_template;
                DataTable dt = cnn.CreateDataTable(sql);
                bool exclude_sender = (bool)dt.Rows[0]["exclude_sender"];//loại bỏ người gửi khỏi ds người nhận
                string jeework_be = _configuration.GetValue<string>("Host:JeeWork_BE");
                string link = jeework_be + dt.Rows[0]["link"].ToString().Replace("$id$", object_id.ToString());
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
                if (id_template.Equals(15))
                {
                    // trường hợp cv bị xóa k thể lấy trong view
                    sqlq = sqlq.Replace("v_wework_new", "we_work");
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
                if (dtFind.Columns.Contains("id_nv"))
                {
                    #region Map info account từ JeeAccount
                    var info = DataAccount.Where(x => values["id_nv"].ToString().Contains(x.UserId.ToString())).FirstOrDefault();
                    if (info != null)
                    {
                        values["hoten"] = info.FullName;
                    }
                    #endregion
                }
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

                for (int i = 0; i < dtUser.Rows.Count; i++)
                {
                    //Gửi mail cho người nhận
                    if (!"".Equals(dtUser.Rows[i]["email"].ToString()))
                    {
                        if (exclude_sender && dtUser.Rows[i]["id_nv"].ToString() == nguoigui.UserID.ToString())
                            continue;
                        string contents = template.Replace("$nguoinhan$", dtUser.Rows[i]["hoten"].ToString());
                        string ErrorMessage = "";
                        //s.Send(m);
                        emailMessage asyncnotice = new emailMessage()
                        {
                            CustomerID = nguoigui.CustomerID,
                            access_token = "",
                            //from = "derhades1998@gmail.com",
                            //to = "thanhthang1798@gmail.com", //
                            //to = dtUser.Rows[i]["email"].ToString(), //thanhthang1798@gmail.com
                            to = "thanhthang1798@gmail.com", //
                            subject = title,
                            html = contents //nội dung html
                        };
                        _notifier.sendEmail(asyncnotice);
                        //SendMail.Send_Synchronized(dtUser.Rows[i]["email"].ToString(), title, new MailAddressCollection(), contents, nguoigui.CustomerID.ToString(), "", true, out ErrorMessage, MInfo, ConnectionString, _notifier);
                    }
                }
               

                //string HRConnectionString = JeeWorkConstant.getHRCnn();
                //DpsConnection cnnHR = new DpsConnection(HRConnectionString);
                //MailInfo MInfo = new MailInfo(nguoigui.CustomerID.ToString(), cnnHR);
                cnn.Disconnect();
                //if (MInfo.Email != null)
                //{
                //    for (int i = 0; i < dtUser.Rows.Count; i++)
                //    {
                //        //Gửi mail cho người nhận
                //        if (!"".Equals(dtUser.Rows[i]["email"].ToString()))
                //        {
                //            if (exclude_sender && dtUser.Rows[i]["id_nv"].ToString() == nguoigui.UserID.ToString())
                //                continue;
                //            string contents = template.Replace("$nguoinhan$", dtUser.Rows[i]["hoten"].ToString());
                //            string ErrorMessage = "";
                //            SendMail.Send_Synchronized(dtUser.Rows[i]["email"].ToString(), title, new MailAddressCollection(), contents, nguoigui.CustomerID.ToString(), "", true, out ErrorMessage, MInfo, ConnectionString, _notifier);
                //        }
                //    }
                //}
            }
            return true;
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
        public static void mailthongbao(long id, List<long> users, int id_template, UserJWT loginData, string ConnectionString, INotifier _notifier, IConfiguration _configuration, DataTable dtOld = null)
        {
            if (users == null || users.Count == 0)
                return;
            using (DpsConnection cnn = new DpsConnection(ConnectionString))
            {
                //List<AccUsernameModel> DataAccount = new List<AccUsernameModel>();
                List<AccUsernameModel> DataAccount = GetDanhSachAccountFromCustomerID(_configuration, loginData.CustomerID);
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
                NotifyMail(id_template, id, loginData, dtUser, ConnectionString, _notifier, DataAccount, _configuration, dtOld);
            }
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="id_template">ID Mẫu template trong bảng we_template</param>
        /// <param name="ConnectionString"></param>
        /// <returns></returns>
        public static SendNotifyModel GetInfoNotify(int id_template, string ConnectionString)
        {
            using (DpsConnection cnn = new DpsConnection(ConnectionString))
            {
                SendNotifyModel templatemodel = new SendNotifyModel();
                string sqlq = "";
                sqlq = "select id_row, name, title, template, keys, link, lang, exclude_sender, link_mobileapp " +
                        "from we_template " +
                        "where id_row=" + id_template;
                DataTable dt = cnn.CreateDataTable(sqlq);
                templatemodel.name = dt.Rows[0]["name"].ToString();
                templatemodel.title = dt.Rows[0]["title"].ToString();
                templatemodel.template = dt.Rows[0]["template"].ToString();
                templatemodel.keys = dt.Rows[0]["keys"].ToString();
                bool exclude_sender = (bool)dt.Rows[0]["exclude_sender"]; // loại bỏ người gửi khỏi ds người nhận
                templatemodel.exclude_sender = exclude_sender;
                templatemodel.link = dt.Rows[0]["link"].ToString();
                templatemodel.link_mobileapp = dt.Rows[0]["link_mobileapp"].ToString();
                templatemodel.lang = dt.Rows[0]["lang"].ToString();
                return templatemodel;
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
                    cond.Add("isDel", 0);
                    select = $@"select we_fields_project_team.id_row, we_fields.fieldname, we_fields.title, IsHidden
                                            ,we_fields_project_team.Title as Title_NewField, we_fields.isnewfield
                                            ,type, TypeID, id_project_team, IsDefault
                                            ,we_fields.show_default_type, we_fields_project_team.position
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
                    cond.Add("id_row", id_project);
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
        public static DataTable StatusDynamic(long id_project, List<AccUsernameModel> DataAccount, DpsConnection cnn)
        {
            DataTable dt = new DataTable();
            string query = "";
            query = $@"select id_row, statusname, description, id_project_team,IsToDo
                    ,Type, IsDefault, color, Position, IsFinal, Follower, IsDeadline, '' as hoten_Follower
                    from we_status 
                    where Disabled = 0 ";
            if (id_project > 0)
                query += " and id_project_team =" + id_project + "";
            query += " order by type, position";
            dt = cnn.CreateDataTable(query);
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
        public static DataTable list_status_by_user(long id_project, List<AccUsernameModel> DataAccount, DpsConnection cnn)
        {
            DataTable dt = new DataTable();
            string query = "";
            query = $@"select id_row, statusname, description, id_project_team, istodo
                        ,type, isdefault, color, position, isfinal, follower
                        , isdeadline, '' as hoten_follower
                        from we_status 
                        where disabled = 0";
            if (id_project > 0)
                query += " and id_project_team =" + id_project + "";
            query += " order by type, Position";
            dt = cnn.CreateDataTable(query);
            #region Map info account từ JeeAccount

            foreach (DataRow item in dt.Rows)
            {
                var info = DataAccount.Where(x => item["follower"].ToString().Contains(x.UserId.ToString())).FirstOrDefault();

                if (info != null)
                {
                    item["hoten_follower"] = info.FullName;
                }
            }

            #endregion
            return dt;
        }
        public static bool Init_RoleDefault(long projectid, List<long> list_roles, DpsConnection cnn)
        {
            SqlConditions cond = new SqlConditions();
            DataTable dt = new DataTable();
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
                    if (cnn.Insert(has, "we_project_role") != 1)
                    {
                        return false;
                    }
                }
            }
            return true;
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
                            if (conn.Insert(has, "we_projects_view") != 1)
                            {
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
                            if (conn.Insert(has, "we_projects_view") != 1)
                            {
                                return false;
                            }
                        }
                    }
                    return true;
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
        public static bool Init_Column_Project(long id_project, DpsConnection conn, bool is_custom = false, string list_field = "")
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
                sqlq = "select fieldname, title, position, isnewfield, id_field " +
                    "from we_fields where isdefault = 1";
                if (is_custom)
                {
                    sqlq += " and isnewfield = 0 and id_field in (" + list_field + ")";
                }
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
                            return false;
                        }
                    }
                }
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
            if (!string.IsNullOrEmpty(Value.ToString()))
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
        public static bool ProcessWork(long WorkID, long StatusID, UserJWT data, JeeWorkConfig config, string ConnectionString, INotifier _notifier, IConfiguration _configuration)
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
                        SqlConditions sqlcond123 = new SqlConditions();
                        sqlcond123.Add("id_work", WorkID);
                        sqlcond123.Add("id_user", dt.Rows[0]["Checker"].ToString());
                        sqlcond123.Add("loai", 1);
                        sqlcond123.Add("Disabled", 0);
                        var sql = @"select * from we_work_user where id_work = @id_work and id_user = @id_user and loai = @loai and Disabled = @Disabled";
                        DataTable dtG = cnn.CreateDataTable(sql, sqlcond123);
                        if (dtG.Rows.Count == 0)
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
                            mailthongbao(WorkID, users, 10, data, ConnectionString, _notifier, _configuration);
                            return true;
                        }
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
            //mailthongbao(int.Parse(row["id_work"].ToString()), users, 18, loginData);
            return true;
        }
        public static string ListAccount(IHeaderDictionary pHeader, out string error, IConfiguration _configuration)
        {
            error = "";
            List<AccUsernameModel> DataAccount;
            DataAccount = GetAccountFromJeeAccount(pHeader, _configuration);
            if (DataAccount == null)
            {
                error += "Lỗi lấy danh sách nhân viên từ hệ thống quản lý tài khoản";
                return "Lỗi lấy danh sách nhân viên từ hệ thống quản lý tài khoản";
            }

            List<string> nvs = DataAccount.Select(x => x.UserId.ToString()).ToList();
            string ids = string.Join(",", nvs);
            error += error;
            return ids;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="info"></param>
        /// <param name="cnn"></param>
        /// <param name="pHeader"></param>
        /// <returns></returns>
        public static string getListDepartment_GetData(UserJWT info, DpsConnection cnn, IHeaderDictionary pHeader, IConfiguration _configuration, string ConnectionString)
        {
            #region Lấy dữ liệu account từ JeeAccount
            List<AccUsernameModel> DataAccount = GetAccountFromJeeAccount(pHeader, _configuration);
            if (DataAccount == null)
                return "";

            //List<string> nvs = DataAccount.Select(x => x.UserId.ToString()).ToList();
            //string ids = string.Join(",", nvs);
            string error = "";
            string listID = ListAccount(pHeader, out error, _configuration);
            if (error != "")
                return "";
            #endregion

            bool Visible = Common.CheckRoleByUserID(info, 3400, cnn);
            SqlConditions conds = new SqlConditions();
            conds.Add("id_user", info.UserID);
            conds.Add("idkh", info.UserID);
            string where_string = "de.Disabled = 0 and idkh = " + info.CustomerID + "";
            #region Trả dữ liệu về backend để hiển thị lên giao diện
            string sqlq = @$"select de.*, '' as NguoiTao,'' as TenNguoiTao, '' as NguoiSua,'' as TenNguoiSua 
                                    from we_department de (admin)";
            if (!Visible)
            {
                sqlq = sqlq.Replace("(admin)", "left join we_department_owner do on de.id_row = do.id_department " +
                    "where de.Disabled = 0 and (do.id_user = " + info.UserID + " " +
                    "or de.id_row in (select distinct p1.id_department from we_project_team p1 join we_project_team_user pu on p1.id_row = pu.id_project_team " +
                    "where p1.disabled = 0 and id_user = " + info.UserID + ")) and " + where_string + "");
            }
            else
                sqlq = sqlq.Replace("(admin)", " where " + where_string + "");
            #endregion
            DataTable dt = cnn.CreateDataTable(sqlq, conds);
            List<string> nvs = dt.AsEnumerable().Select(x => x["id_row"].ToString()).Distinct().ToList();
            if (nvs.Count == 0)
                return "null";
            string ids = string.Join(",", nvs);
            if (string.IsNullOrEmpty(ids)) ids = "null";
            return ids;
        }
        public static List<AccUsernameModel> GetAccountFromJeeAccount(IHeaderDictionary pHeader, IConfiguration _configuration)
        {
            if (pHeader == null) return null;
            if (!pHeader.ContainsKey(HeaderNames.Authorization)) return null;
            IHeaderDictionary _d = pHeader;
            string _bearer_token;
            _bearer_token = _d[HeaderNames.Authorization].ToString();
            string API_Account = _configuration.GetValue<string>("Host:JeeAccount_API");
            string link_api = API_Account + "/api/accountmanagement/usernamesByCustermerID";
            var client = new RestClient(link_api);
            var request = new RestRequest(Method.GET);
            request.AddHeader("Authorization", _bearer_token);
            IRestResponse response = client.Execute(request);
            var model = JsonConvert.DeserializeObject<BaseModel<List<AccUsernameModel>>>(response.Content);
            if (model != null && model.status == 1)
            {
                return model.data;
            }
            else
                return null;
        }
        public static List<AccountManagementDTO> GetListAccountManagement(IHeaderDictionary pHeader, IConfiguration _configuration)
        {
            if (pHeader == null) return null;
            if (!pHeader.ContainsKey(HeaderNames.Authorization)) return null;
            IHeaderDictionary _d = pHeader;
            string _bearer_token;
            _bearer_token = _d[HeaderNames.Authorization].ToString();
            string API_Account = _configuration.GetValue<string>("Host:JeeAccount_API");
            string link_api = API_Account + "/api/accountmanagement/GetListAccountManagement";
            var client = new RestClient(link_api);
            var request = new RestRequest(Method.GET);
            request.AddHeader("Authorization", _bearer_token);
            IRestResponse response = client.Execute(request);
            var model = JsonConvert.DeserializeObject<BaseModel<List<AccountManagementDTO>>>(response.Content);
            if (model != null)
            {
                return model.data;
            }
            else
                return null;
        }
        public static string getSecretToken(IConfiguration _configuration)
        {
            var secret = _configuration.GetValue<string>("Jwt:internal_secret");
            var projectName = _configuration.GetValue<string>("KafkaConfig:ProjectName");
            var token = JsonWebToken.issueToken(new TokenClaims { projectName = projectName }, secret);
            return token;
        }
        public static List<long> GetDanhSachCustomerID(IConfiguration _configuration)
        {
            string API_Account = _configuration.GetValue<string>("Host:JeeAccount_API");
            string internal_secret = getSecretToken(_configuration);
            string link_api = API_Account + "/api/accountmanagement/GetListCustomerID/internal/WORK"; // work là appcode jeework
            var client = new RestClient(link_api);
            var request = new RestRequest(Method.GET);
            request.AddHeader("Authorization", internal_secret);
            IRestResponse response = client.Execute(request);
            var model = JsonConvert.DeserializeObject<List<long>>(response.Content);
            if (model != null)
            {
                return model;
            }
            else
                return null;
        }
        public static List<AccUsernameModel> GetDanhSachAccountFromCustomerID(IConfiguration _configuration, long CustomerID)
        {
            string API_Account = _configuration.GetValue<string>("Host:JeeAccount_API");
            string internal_secret = getSecretToken(_configuration);
            string link_api = API_Account + "/api/accountmanagement/usernamesByCustermerID/internal/" + CustomerID;
            var client = new RestClient(link_api);
            var request = new RestRequest(Method.GET);
            request.AddHeader("Authorization", internal_secret);
            IRestResponse response = client.Execute(request);
            var model = JsonConvert.DeserializeObject<List<AccUsernameModel>>(response.Content);
            if (model != null)
            {
                return model;
            }
            else
                return null;
        }
        public static List<AccUsernameModel> GetMyStaff(IHeaderDictionary pHeader, IConfiguration _configuration, UserJWT loginData)
        {
            if (pHeader == null) return null;
            if (!pHeader.ContainsKey(HeaderNames.Authorization)) return null;
            IHeaderDictionary _d = pHeader;
            string _bearer_token;
            _bearer_token = _d[HeaderNames.Authorization].ToString();
            string API_Account = _configuration.GetValue<string>("Host:JeeAccount_API");
            string link_api = API_Account + "/api/accountmanagement/ListNhanVienCapDuoiDirectManager";
            var client = new RestClient(link_api);
            var request = new RestRequest(Method.POST);
            request.AddHeader("Authorization", _bearer_token);
            request.AddHeader("Accept", "application/json");
            // request.AddJsonBody(new { Username = "huypad" }); // Anonymous type object is converted to Json body
            request.AddJsonBody(new { username = loginData.Username });
            IRestResponse response = client.Execute(request);
            if ((int)response.StatusCode >= 400)
            {
                return null;
            };

            var model = JsonConvert.DeserializeObject<List<AccUsernameModel>>(response.Content);
            if (model != null)
            {
                return model;
            }
            else
                return null;
        }
        /// <summary>
        /// getConnectionString
        /// </summary>
        /// <param name="pHeader"></param>
        /// <param name="config"></param>
        /// <returns></returns>
        public static string getConnectionString(IConnectionCache ConnectionCache, long CustomerID, IConfiguration _configuration)
        {
            var x = _configuration.GetValue<string>("AppConfig:IsOnlineDB");
            string ConnectionString = ConnectionCache.GetConnectionString(CustomerID);
            if (string.IsNullOrEmpty(x))
            {
                ConnectionString = _configuration.GetValue<string>("AppConfig:ConnectionString");
            }
            return ConnectionString;
        }
        public static void Insert_Template(DpsConnection cnn, string CustemerID)
        {
            SqlConditions Conds = new SqlConditions();
            string select = "select * from we_template_customer where disabled = 0 and customerid = " + CustemerID;
            DataTable dt = cnn.CreateDataTable(select);
            string sql_insert = "";
            if (dt.Rows.Count <= 0)
            {
                Conds.Add("CustomerID", CustemerID);
                sql_insert = $@"insert into we_template_customer (Title, Description, CreatedDate, CreatedBy, Disabled, IsDefault, Color, id_department, TemplateID, CustomerID)
                        select Title, Description, getdate(), 0, Disabled, IsDefault, Color,0, id_row, " + CustemerID + " as CustomerID from we_template_list where Disabled = 0  and is_template_center <> 1";
                cnn.ExecuteNonQuery(sql_insert);
                dt = cnn.CreateDataTable(select);
                if (dt.Rows.Count > 0)
                {
                    foreach (DataRow item in dt.Rows)
                    {
                        sql_insert = $@"insert into we_template_status (StatusID, TemplateID, StatusName, description, CreatedDate, CreatedBy, Disabled, Type, IsDefault, color, Position, IsFinal, IsDeadline, IsTodo) " +
                            "select id_Row, " + item["id_row"] + ", StatusName, description, getdate(), 0, Disabled, Type, IsDefault, color, Position, IsFinal, IsDeadline, IsTodo " +
                            "from we_status_list where disabled = 0 and id_template_list = " + item["templateid"] + "";
                        cnn.ExecuteNonQuery(sql_insert);
                        sql_insert = "";
                    }
                }
            }
        }
        public static bool init_view_by_project(DpsConnection cnn, string list_views, long id_project_team, UserJWT loginData, out string error)
        {
            error = "";
            string sql_insert = "";
            sql_insert = $@"insert into we_projects_view (id_project_team, viewid, view_name_new, createddate, createdby, default_view, disabled) " +
                    "select " + id_project_team + ", id_row, view_name, getdate(), " + loginData.UserID + ", is_default, 0 " +
                    "from we_default_views where id_row in (" + list_views + ")";
            cnn.ExecuteNonQuery(sql_insert);
            if (cnn.LastError == null)
                return true;
            else
            {
                error = cnn.LastError.Message;
                return false;
            }
        }
        public static bool init_save_as_new_template(DpsConnection cnn, TemplateCenterModel template, UserJWT loginData, out string error)
        {
            error = "";
            Hashtable has = new Hashtable();
            string table_name = "we_department";
            string sqlq = "";
            DataTable dt = new DataTable();
            SqlConditions conds = new SqlConditions();
            conds.Add("disabled", 0);
            conds.Add("id_row", template.save_as_id);
            if (template.types == 2)
            {
                conds.Remove(conds["id_row"]);
                conds.Add("parentid", template.save_as_id);
            }
            if (template.types == 3)
            {
                table_name = "we_project_team";
            }
            sqlq = "select id_row, title,'' as description from " + table_name + " where (where)";
            dt = cnn.CreateDataTable(sqlq, "(where)", conds);
            if (cnn.LastError != null || dt == null)
            {
                cnn.RollbackTransaction();
                error = cnn.LastError.Message;
                return false;
            }
            if (dt.Rows.Count == 1)
            {
                foreach (DataRow dr in dt.Rows) // 1 dòng
                {
                    insert_sample_data(cnn, dr, template.types, 0, loginData, out error);
                }
            }
            return true;
        }
        public static bool insert_sample_data(DpsConnection cnn, DataRow dr, long types, long maxid_new, UserJWT loginData, out string error)
        {
            error = "";
            string table_name = "we_department";
            string sqlq = "";
            DataTable dt = new DataTable();
            SqlConditions conds = new SqlConditions();
            Hashtable has = new Hashtable();
            has["title"] = dr["title"].ToString();
            has["description"] = dr["description"].ToString();
            if (types > 1)
                has["parentid"] = maxid_new;
            else
                has["parentid"] = DBNull.Value; // insert lần đầu không có parentid 
            has["levels"] = types;
            has["disabled"] = 0;
            has["createddate"] = DateTime.Now;
            has["createdby"] = loginData.UserID;
            has["customerid"] = loginData.CustomerID;
            has["id_reference"] = dr["id_row"].ToString();
            if (cnn.Insert(has, "we_sample_data") != 1)
            {
                cnn.RollbackTransaction();
                error = cnn.LastError.Message;
                return false;
            }
            types = types + 1;
            conds.Add("disabled", 0);
            conds.Add("id_row", dr["id_row"].ToString());
            if (types == 2)
            {
                conds.Remove(conds["id_row"]);
                conds.Add("parentid", dr["id_row"].ToString());
            }
            if (types == 3)
            {
                conds = new SqlConditions();
                conds.Add("disabled", 0);
                conds.Add("id_department", dr["id_row"].ToString());
                table_name = "we_project_team";
            }
            if (types == 4)
            {
                conds = new SqlConditions();
                conds.Add("disabled", 0);
                conds.Add("id_project_team", dr["id_row"].ToString());
                table_name = "we_work";
            }
            while (types < 5)
            {
                maxid_new = long.Parse(cnn.ExecuteScalar("select IDENT_CURRENT('we_sample_data')").ToString());
                sqlq = "select id_row, title,'' as description from " + table_name + " where (where)";
                dt = cnn.CreateDataTable(sqlq, "(where)", conds);
                if (cnn.LastError != null || dt == null)
                {
                    cnn.RollbackTransaction();
                    error = cnn.LastError.Message;
                    return false;
                }
                if (dt.Rows.Count > 0)
                {
                    foreach (DataRow item in dt.Rows)
                    {
                        insert_sample_data(cnn, item, types, maxid_new, loginData, out error);
                    }
                    types++;
                }
                else
                {
                    types++;
                    conds = new SqlConditions();
                    conds.Add("disabled", 0);
                    conds.Add("id_department", dr["id_row"].ToString());
                    table_name = "we_project_team";
                }
            }
            return true;
        }
        // áp dụng cho các trường hợp chưa có space nào
        public static bool init_space(DpsConnection cnn, UserJWT loginData, GenerateProjectAutoModel data, out string error)
        {
            error = "";
            Hashtable val = new Hashtable();
            Insert_Template(cnn, loginData.CustomerID.ToString());
            string max_template = "";
            if (data.id_department == 0)
            {
                max_template = cnn.ExecuteScalar("select top 1 (id_row) " +
                    "from we_template_customer " +
                    "where Disabled = 0 and (is_template_center = 0 or is_template_center is null) " +
                    "and customerID =" + loginData.CustomerID + "").ToString();
            }
            else
                max_template = cnn.ExecuteScalar("select templateid from we_department " +
                    "where disabled = 0 and id_row =" + data.id_department + "").ToString();
            val.Add("title", "Phòng ban theo yêu cầu " + data.meetingid);
            val.Add("id_cocau", 0);
            val.Add("idkh", loginData.CustomerID);
            val.Add("createddate", DateTime.Now);
            val.Add("createdby", loginData.UserID);
            val.Add("IsDataStaff_HR", 0);
            val.Add("templateid", max_template);
            if (data.meetingid > 0)
            {
                val.Add("phanloaiid", 1);
            }
            cnn.BeginTransaction();
            if (cnn.Insert(val, "we_department") != 1)
            {
                cnn.RollbackTransaction();
                error = cnn.LastError.Message;
                return false;
            }
            string idc = cnn.ExecuteScalar("select IDENT_CURRENT('we_department')").ToString();
            val = new Hashtable();
            val["id_department"] = idc;
            val["CreatedDate"] = DateTime.Now;
            val["CreatedBy"] = loginData.UserID;
            val["viewid"] = 1;
            val["is_default"] = 1;
            if (cnn.Insert(val, "we_department_view") != 1)
            {
                cnn.RollbackTransaction();
                error = cnn.LastError.Message;
                return false;
            }
            val = new Hashtable();
            val["id_department"] = idc;
            val["CreatedDate"] = DateTime.Now;
            val["CreatedBy"] = loginData.UserID;
            val["id_user"] = loginData.UserID;
            val["type"] = 1;
            if (cnn.Insert(val, "we_department_owner") != 1)
            {
                cnn.RollbackTransaction();
                error = cnn.LastError.Message;
                return false;
            }
            return true;
        }
        public static string getErrorMessageFromBackend(string ErrorCode, string LangCode = "vi", string _space = "")
        {
            string Mess = "";
            string code = ErrorCode;
            string space = _space;
            if (LangCode == "vi")
            {
                Mess = LocalizationUtility.GetBackendMessage(code, space, "vi");
                if (Mess == null)
                {
                    Mess = LocalizationUtility.GetBackendMessage("null", "", "vi");
                }
            }
            else
            {
                Mess = LocalizationUtility.GetBackendMessage(code, space, "en");
                if (Mess == null)
                {
                    Mess = LocalizationUtility.GetBackendMessage("null", "", "en");
                }
            }
            return Mess;
        }
        public static DataTable project_by_user(long userid, long customerid, DpsConnection cnn)
        {
            DataTable dt = new DataTable();
            string sql = @"select distinct p.id_row, p.title, is_project, start_date, end_date, color, status, locked 
                                    from we_project_team p
                                    join we_department d on d.id_row = p.id_department
                                    join we_project_team_user u on u.id_project_team = p.id_row
                                     where u.Disabled = 0 and id_user = " + userid + " " +
                                    "and p.Disabled = 0  and d.disabled = 0 " +
                                    "and idkh=" + customerid + " order by title";
            dt = cnn.CreateDataTable(sql);
            if (cnn.LastError != null || dt == null)
                return new DataTable();
            else
                return dt;
        }
        public static bool insert_processwork(DpsConnection cnn)
        {
            Hashtable val = new Hashtable();
            string sqlq = "";
            sqlq = $@"select w.id_row, w.id_project_team, w.createddate
                    , w.createdby, _st.id_row as statusid
                    from we_work w join we_status _st 
                    on w.id_project_team = _st.id_project_team
                    where w.id_row not in (select workid from we_work_process where disabled = 0)
                    and w.disabled = 0 and _st.disabled = 0";
            DataTable dt = new DataTable();
            dt = cnn.CreateDataTable(sqlq);
            if (dt.Rows.Count > 0)
            {
                foreach (DataRow item in dt.Rows)
                {
                    val = new Hashtable();
                    val.Add("id_project_team", item["id_project_team"]);
                    val.Add("workid", item["id_row"]);
                    val.Add("statusid", item["statusid"]);
                    val.Add("checker", DBNull.Value);
                    val.Add("createddate", item["createddate"]);
                    val.Add("createdby", item["createdby"]);
                    if (cnn.Insert(val, "we_work_process") != 1)
                    {
                        cnn.RollbackTransaction();
                        return false;
                    }
                    long processid = long.Parse(cnn.ExecuteScalar("select IDENT_CURRENT('we_work_process')").ToString());
                    val = new Hashtable();
                    val.Add("processid", processid);
                    val.Add("new_checker", DBNull.Value);
                    val.Add("createddate", item["createddate"]);
                    val.Add("createdby", item["createdby"]);
                    if (cnn.Insert(val, "we_work_process_log") != 1)
                    {
                        cnn.RollbackTransaction();
                        return false;
                    }
                }
            }
            return true;
        }
        public static bool update_position_status(long id_project_team, DpsConnection cnn)
        {
            long position = 0;
            Hashtable val = new Hashtable();
            SqlConditions cond = new SqlConditions();
            cond.Add("disabled", 0);
            cond.Add("id_project_team", id_project_team);
            string sqlq = "select id_row, id_project_team, position " +
                        "from we_status " +
                        "where (where) order by type, position";
            DataTable dt = cnn.CreateDataTable(sqlq, "(where)", cond);
            if (dt.Rows.Count > 0)
            {
                foreach (DataRow item in dt.Rows)
                {
                    val = new Hashtable();
                    position++;
                    val.Add("position", position);
                    cond = new SqlConditions();
                    cond.Add("id_row", item["id_row"].ToString());
                    if (cnn.Update(val, cond, "we_status") != 1)
                    {
                        return false;
                    }
                }

            }
            return true;
        }
    }
}