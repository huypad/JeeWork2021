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
        private readonly IHostingEnvironment _hostingEnvironment;
        private JeeWorkConfig _config;
        public List<AccUsernameModel> DataAccount;

        public WeworkLiteController(IOptions<JeeWorkConfig> config, IHostingEnvironment hostingEnvironment)
        {
            _hostingEnvironment = hostingEnvironment;
            _config = config.Value;
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
                using (DpsConnection cnn = new DpsConnection(_config.ConnectionString))
                {
                    string sql = "select id_row, title from we_department where Disabled=0 and IdKH=" + loginData.CustomerID + " order by title";
                    DataTable dt = cnn.CreateDataTable(sql);
                    if (cnn.LastError != null || dt == null)
                        return JsonResultCommon.Exception(cnn.LastError, _config, loginData.CustomerID, ControllerContext);
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
                return JsonResultCommon.Exception(ex, _config, loginData.CustomerID);
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
                using (DpsConnection cnn = new DpsConnection(_config.ConnectionString))
                {
                    string sql = @"select distinct p.id_row, title, is_project from we_project_team p
join we_project_team_user u on u.id_project_team = p.id_row
 where u.Disabled = 0 and id_user = " + loginData.UserID + " and p.Disabled = 0 order by title";
                    DataTable dt = cnn.CreateDataTable(sql);
                    if (cnn.LastError != null || dt == null)
                        return JsonResultCommon.Exception(cnn.LastError, _config, loginData.CustomerID, ControllerContext);
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
                return JsonResultCommon.Exception(ex, _config, loginData.CustomerID);
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
                //bool Visible = Common.CheckRoleByToken(HttpContext.Request.Headers, "3400");
                bool Visible = true;
                using (DpsConnection cnn = new DpsConnection(_config.ConnectionString))
                {
                    #region Lấy dữ liệu account từ JeeAccount
                    DataAccount = WeworkLiteController.GetAccountFromJeeAccount(HttpContext.Request.Headers, _config);
                    if (DataAccount == null)
                        return JsonResultCommon.Custom("Lỗi lấy danh sách nhân viên từ hệ thống quản lý tài khoản");

                    //List<string> nvs = DataAccount.Select(x => x.UserId.ToString()).ToList();
                    //string ids = string.Join(",", nvs);
                    string error = "";
                    string listID = WeworkLiteController.ListAccount(HttpContext.Request.Headers, out error, _config);
                    if (error != "")
                        return JsonResultCommon.Custom(error);
                    #endregion

                    SqlConditions conds = new SqlConditions();
                    conds.Add("id_user", loginData.UserID);

                    #region Trả dữ liệu về backend để hiển thị lên giao diện
                    string sqlq = @$"select distinct de.*, '' as NguoiTao, '' as TenNguoiTao, '' as NguoiSua, '' as TenNguoiSua 
from we_department de  (admin) and de.CreatedBy in ({listID})";
                    if (!Visible)
                    {
                        sqlq = sqlq.Replace("(admin)", "left join we_department_owner do on de.id_row = do.id_department " +
                            "where de.Disabled = 0 and (do.id_user = " + loginData.UserID + " " +
                            "or de.id_row in (select distinct p1.id_department from we_project_team p1 join we_project_team_user pu on p1.id_row = pu.id_project_team " +
                            "where p1.Disabled = 0 and id_user = " + loginData.UserID + ")) and de.Disabled = 0 ");
                    }
                    else
                        sqlq = sqlq.Replace("(admin)", " where de.Disabled = 0  ");
                    //DataTable dt = cnn.CreateDataTable(sqlq, Conds);
                    #endregion

                    DataTable dt = cnn.CreateDataTable(sqlq, conds);
                    if (cnn.LastError != null || dt == null)
                        return JsonResultCommon.Exception(cnn.LastError, _config, loginData.CustomerID, ControllerContext);
                    #region Map info account từ JeeAccount

                    foreach (DataRow item in dt.Rows)
                    {
                        var infoNguoiTao = DataAccount.Where(x => item["CreatedBy"].ToString().Contains(x.UserId.ToString())).FirstOrDefault();
                        var infoNguoiSua = DataAccount.Where(x => item["UpdatedBy"].ToString().Contains(x.UserId.ToString())).FirstOrDefault();

                        if (infoNguoiTao != null)
                        {
                            item["NguoiTao"] = infoNguoiTao.Username;
                            item["TenNguoiTao"] = infoNguoiTao.Username;
                        }
                        if (infoNguoiSua != null)
                        {
                            item["NguoiSua"] = infoNguoiSua.Username;
                            item["TenNguoiSua"] = infoNguoiSua.Username;
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
                return JsonResultCommon.Exception(ex, _config, loginData.CustomerID);
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
                using (DpsConnection cnn = new DpsConnection(_config.ConnectionString))
                {
                    string sql = "select id_row, title,deadline from we_milestone where Disabled=0 and id_project_team=" + id_project_team + " order by title";
                    DataTable dt = cnn.CreateDataTable(sql);
                    if (cnn.LastError != null || dt == null)
                        return JsonResultCommon.Exception(cnn.LastError, _config, loginData.CustomerID, ControllerContext);
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
                return JsonResultCommon.Exception(ex, _config, loginData.CustomerID);
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
                using (DpsConnection cnn = new DpsConnection(_config.ConnectionString))
                {
                    string sql = "select id_row, title, color from we_tag where Disabled=0 and id_project_team=" + id_project_team + " order by title";
                    DataTable dt = cnn.CreateDataTable(sql);
                    if (cnn.LastError != null || dt == null)
                        return JsonResultCommon.Exception(cnn.LastError, _config, loginData.CustomerID, ControllerContext);
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
                return JsonResultCommon.Exception(ex, _config, loginData.CustomerID);
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
                using (DpsConnection cnn = new DpsConnection(_config.ConnectionString))
                {
                    string sql = "select id_row, title from we_group where Disabled=0 and id_project_team=" + id_project_team + " order by title";
                    DataTable dt = cnn.CreateDataTable(sql);
                    if (cnn.LastError != null || dt == null)
                        return JsonResultCommon.Exception(cnn.LastError, _config, loginData.CustomerID, ControllerContext);
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
                return JsonResultCommon.Exception(ex, _config, loginData.CustomerID);
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
                string domain = _config.LinkAPI;
                using (DpsConnection cnn = new DpsConnection(_config.ConnectionString))
                {
                    #region Lấy dữ liệu account từ JeeAccount
                    DataAccount = WeworkLiteController.GetAccountFromJeeAccount(HttpContext.Request.Headers, _config);
                    if (DataAccount == null)
                        return JsonResultCommon.Custom("Lỗi lấy danh sách nhân viên từ hệ thống quản lý tài khoản");

                    //List<string> nvs = DataAccount.Select(x => x.UserId.ToString()).ToList();
                    //string ids = string.Join(",", nvs);
                    string error = "";
                    string listID = WeworkLiteController.ListAccount(HttpContext.Request.Headers, out error, _config);
                    if (error != "")
                        return JsonResultCommon.Custom(error);
                    #endregion
                    string sql = $@"";
                    if (filter != null && filter.keys != null)
                    {
                        sql = $@"select distinct u.id_user as Id_NV, '' AS hoten, '' as Mobile, '' as Username, '' as Email, '' as CocauID
, '' as CoCauToChuc, '' as ParentID, '' as Id_Chucdanh, '' AS Tenchucdanh
from we_project_team_user u
left join we_project_team p on u.id_project_team=p.id_row
where u.id_user in ({listID})";
                        //if (filter.keys.Contains("cocauid") && !string.IsNullOrEmpty(filter["cocauid"]))
                        //    sql += " and cocauid=" + filter["cocauid"];
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
                            return JsonResultCommon.Exception(cnn.LastError, _config, loginData.CustomerID, ControllerContext);

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
                    //var temp = dt.AsEnumerable();
                    //var data = (from r in temp
                    //            select new
                    //            {
                    //                id_nv = r["id_nv"],
                    //                hoten = r["hoten"],
                    //                username = r["username"],
                    //                mobile = r["mobile"],
                    //                tenchucdanh = r["tenchucdanh"],
                    //                image = genLinkImage(domain, loginData.CustomerID, r["id_nv"].ToString(), _hostingEnvironment.ContentRootPath),
                    //                CoCauToChuc = r["CoCauToChuc"],
                    //                CoCauID = r["CoCauID"],
                    //                Email = r["Email"],
                    //            }).Distinct();
                    return JsonResultCommon.ThanhCong(danhsach);
                }
            }
            catch (Exception ex)
            {
                return JsonResultCommon.Exception(ex, _config, loginData.CustomerID);
            }
        }
        public static DataTable List_Account_HR(long CocauID, IHeaderDictionary pHeader, JeeWorkConfig config)
        {
            List<AccUsernameModel> DataAccount;
            DataTable dt = new DataTable();
            using (DpsConnection cnn = new DpsConnection(config.ConnectionString))
            {
                #region Lấy dữ liệu account từ JeeAccount
                DataAccount = WeworkLiteController.GetAccountFromJeeAccount(pHeader, config);
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
                string domain = _config.LinkAPI;
                using (DpsConnection cnn = new DpsConnection(_config.ConnectionString))
                {
                    #region Lấy dữ liệu account từ JeeAccount
                    DataAccount = WeworkLiteController.GetAccountFromJeeAccount(HttpContext.Request.Headers, _config);
                    if (DataAccount == null)
                        return JsonResultCommon.Custom("Lỗi lấy danh sách nhân viên từ hệ thống quản lý tài khoản");

                    //List<string> nvs = DataAccount.Select(x => x.UserId.ToString()).ToList();
                    //string ids = string.Join(",", nvs);
                    string error = "";
                    string listID = WeworkLiteController.ListAccount(HttpContext.Request.Headers, out error, _config);
                    if (error != "")
                        return JsonResultCommon.Custom(error);
                    #endregion

                    string sql = ";select * from we_emotion";
                    sql += ";select * from we_like_icon where disabled=0";
                    DataSet ds = cnn.CreateDataSet(sql);
                    if (cnn.LastError != null || ds == null)
                        return JsonResultCommon.Exception(cnn.LastError, _config, loginData.CustomerID, ControllerContext);
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
                return JsonResultCommon.Exception(ex, _config, loginData.CustomerID);
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
                using (DpsConnection cnn = new DpsConnection(_config.ConnectionString))
                {
                    string sql = "select * from we_emotion";
                    if (id > 0)
                        sql += " where id_row=" + id;
                    DataTable dt = cnn.CreateDataTable(sql);
                    if (cnn.LastError != null || dt == null)
                        return JsonResultCommon.Exception(cnn.LastError, _config, loginData.CustomerID, ControllerContext);
                    string domain = _config.LinkAPI;
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
                return JsonResultCommon.Exception(ex, _config, loginData.CustomerID);
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
                using (DpsConnection cnn = new DpsConnection(_config.ConnectionString))
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
                        return JsonResultCommon.Exception(cnn.LastError, _config, loginData.CustomerID, ControllerContext);
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
                return JsonResultCommon.Exception(ex, _config, loginData.CustomerID);
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
                return JsonResultCommon.Exception(ex, _config, loginData.CustomerID);
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
                //if (id_project_team <= 0)
                //    return new List<string>();
                using (DpsConnection cnn = new DpsConnection(_config.ConnectionString))
                {
                    DataTable dt = GetListField(id_project_team, _config);
                    if (cnn.LastError != null || dt == null)
                        return JsonResultCommon.Exception(cnn.LastError, _config, loginData.CustomerID, ControllerContext);

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
                               };
                    data.OrderBy(x => x.position).ThenByDescending(x => x.id_project_team);
                    return JsonResultCommon.ThanhCong(data);
                }
            }
            catch (Exception ex)
            {
                return JsonResultCommon.Exception(ex, _config, loginData.CustomerID);
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
                using (DpsConnection cnn = new DpsConnection(_config.ConnectionString))
                {
                    string sql = "select * from we_fields where isnewfield = 1";
                    DataTable dt = cnn.CreateDataTable(sql);
                    if (cnn.LastError != null || dt == null)
                        return JsonResultCommon.Exception(cnn.LastError, _config, loginData.CustomerID, ControllerContext);
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
                return JsonResultCommon.Exception(ex, _config, loginData.CustomerID);
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
                using (DpsConnection cnn = new DpsConnection(_config.ConnectionString))
                {
                    DataTable dt = cnn.CreateDataTable("select id_row, title, ColumnName, description, priority, Disabled " +
                        "from we_list_processing " +
                        "where Disabled = 0 " +
                        "order by priority");
                    if (cnn.LastError != null || dt == null)
                        return JsonResultCommon.Exception(cnn.LastError, _config, loginData.CustomerID, ControllerContext);
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
                return JsonResultCommon.Exception(ex, _config, loginData.CustomerID);
            }
        }
        /// <summary>
        /// Lấy danh sách field động theo id_project_team, isnewfield: Field bên ngoài không select trong DB tùy mục đích người dùng
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
                using (DpsConnection cnn = new DpsConnection(_config.ConnectionString))
                {
                    #region Lấy dữ liệu account từ JeeAccount
                    DataAccount = WeworkLiteController.GetAccountFromJeeAccount(HttpContext.Request.Headers, _config);
                    if (DataAccount == null)
                        return JsonResultCommon.Custom("Lỗi lấy danh sách nhân viên từ hệ thống quản lý tài khoản");

                    string error = "";
                    string listID = WeworkLiteController.ListAccount(HttpContext.Request.Headers, out error, _config);
                    if (error != "")
                        return JsonResultCommon.Custom(error);
                    #endregion

                    DataTable dt = StatusDynamic(id_project_team, DataAccount, _config);
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
                        return JsonResultCommon.Exception(cnn.LastError, _config, loginData.CustomerID, ControllerContext);

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
                               };
                    return JsonResultCommon.ThanhCong(data);
                }
            }
            catch (Exception ex)
            {
                return JsonResultCommon.Exception(ex, _config, loginData.CustomerID);
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
                using (DpsConnection cnn = new DpsConnection(_config.ConnectionString))
                {
                    DataSet ds = cnn.CreateDataSet(query);
                    DataTable dt = ds.Tables[0];
                    if (cnn.LastError != null || dt == null)
                        return JsonResultCommon.Exception(cnn.LastError, _config, loginData.CustomerID, ControllerContext);
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
                return JsonResultCommon.Exception(ex, _config, loginData.CustomerID);
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
                using (DpsConnection cnn = new DpsConnection(_config.ConnectionString))
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
                        return JsonResultCommon.Exception(cnn.LastError, _config, loginData.CustomerID, ControllerContext);
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
                return JsonResultCommon.Exception(ex, _config, loginData.CustomerID);
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
                string domain = _config.LinkAPI;
                using (DpsConnection cnn = new DpsConnection(_config.ConnectionString))
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
                        return JsonResultCommon.Exception(cnn.LastError, _config, loginData.CustomerID, ControllerContext);
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
                return JsonResultCommon.Exception(ex, _config, loginData.CustomerID);
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
                using (DpsConnection cnn = new DpsConnection(_config.ConnectionString))
                {
                    SqlConditions conds = new SqlConditions(); string sql = "";
                    conds.Add("Disabled", 0);
                    conds.Add("CustomerID", loginData.CustomerID);
                    sql = "select id_row, Title, Description, IsDefault, Color, id_department, TemplateID, CustomerID " +
                        "from we_template_customer " +
                        "where (where) order by Title";
                    DataTable dt_template = cnn.CreateDataTable(sql, "(where)", conds);
                    if (cnn.LastError != null || dt_template == null)
                        return JsonResultCommon.Exception(cnn.LastError, _config, loginData.CustomerID, ControllerContext);
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
                return JsonResultCommon.Exception(ex, _config, loginData.CustomerID);
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
                using (DpsConnection cnn = new DpsConnection(_config.ConnectionString))
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
                        return JsonResultCommon.Exception(cnn.LastError, _config, loginData.CustomerID, ControllerContext);

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
                return JsonResultCommon.Exception(ex, _config, loginData.CustomerID);
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
                string domain = _config.LinkAPI;
                using (DpsConnection cnn = new DpsConnection(_config.ConnectionString))
                {
                    #region Lấy dữ liệu account từ JeeAccount
                    DataAccount = WeworkLiteController.GetAccountFromJeeAccount(HttpContext.Request.Headers, _config);
                    if (DataAccount == null)
                        return JsonResultCommon.Custom("Lỗi lấy danh sách nhân viên từ hệ thống quản lý tài khoản");
                    string error = "";
                    string listID = WeworkLiteController.ListAccount(HttpContext.Request.Headers, out error, _config);
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
        public static bool NotifyMail(int id_template, long object_id, UserJWT nguoigui, DataTable dtUser, JeeWorkConfig config, DataTable dtOld = null)
        {
            using (DpsConnection cnn = new DpsConnection(config.ConnectionString))
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
                            SendMail.Send_Synchronized(dtUser.Rows[i]["email"].ToString(), title, new MailAddressCollection(), contents, nguoigui.CustomerID.ToString(), "", true, out ErrorMessage, MInfo, config);
                        }
                    }
                }
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
        public static void mailthongbao(long id, List<long> users, int id_template, UserJWT loginData, JeeWorkConfig config, DataTable dtOld = null)
        {
            if (users == null || users.Count == 0)
                return;
            using (DpsConnection cnn = new DpsConnection(config.ConnectionString))
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
                NotifyMail(id_template, id, loginData, dtUser, config, dtOld);
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
        public static DataTable GetListField(long id_project_team, JeeWorkConfig config)
        {
            using (DpsConnection cnn = new DpsConnection(config.ConnectionString))
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
        public static bool CheckRole(long role, string user, long id_project, JeeWorkConfig config)
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
                using (DpsConnection cnn = new DpsConnection(config.ConnectionString))
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
        public static bool CheckNotify_ByConditions(long id_project, string key, bool IsProject, JeeWorkConfig config)
        {
            DataTable dt_Key = new DataTable();
            string sqlq = "";
            SqlConditions cond = new SqlConditions();
            try
            {
                using (DpsConnection cnn = new DpsConnection(config.ConnectionString))
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
        public static DataTable StatusDynamic(long id_project, List<AccUsernameModel> DataAccount, JeeWorkConfig config)
        {
            DataTable dt = new DataTable();
            using (DpsConnection cnn = new DpsConnection(config.ConnectionString))
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
        public static bool Init_RoleDefault(long projectid, List<long> list_roles, JeeWorkConfig config)
        {
            SqlConditions cond = new SqlConditions();
            DataTable dt = new DataTable();
            using (DpsConnection cnn = new DpsConnection(config.ConnectionString))
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
        public static bool ProcessWork(long WorkID, long StatusID, UserJWT data, JeeWorkConfig config)
        {
            SqlConditions cond = new SqlConditions();
            DataTable dt = new DataTable();
            using (DpsConnection cnn = new DpsConnection(config.ConnectionString))
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
                        mailthongbao(WorkID, users, 10, data, config);
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
        public static string ListAccount(IHeaderDictionary pHeader, out string error, JeeWorkConfig config)
        {
            error = "";
            List<AccUsernameModel> DataAccount;
            DataAccount = GetAccountFromJeeAccount(pHeader, config);
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
        public static string getListDepartment_GetData(UserJWT info, DpsConnection cnn, IHeaderDictionary pHeader, JeeWorkConfig config)
        {
            bool Visible = Common.CheckRoleByToken(info.Token, "3400", config);
            SqlConditions conds = new SqlConditions();
            conds.Add("id_user", info.UserID);

            #region Lấy dữ liệu account từ JeeAccount
            List<AccUsernameModel> DataAccount = WeworkLiteController.GetAccountFromJeeAccount(pHeader, config);
            if (DataAccount == null)
                return "";

            //List<string> nvs = DataAccount.Select(x => x.UserId.ToString()).ToList();
            //string ids = string.Join(",", nvs);
            string error = "";
            string listID = WeworkLiteController.ListAccount(pHeader, out error, config);
            if (error != "")
                return "";
            #endregion

            #region Trả dữ liệu về backend để hiển thị lên giao diện
            string sqlq = @$"select de.*, '' as NguoiTao,'' as TenNguoiTao, '' as NguoiSua,'' as TenNguoiSua 
                                    from we_department de  (admin) and de.CreatedBy in ({listID}) ";
            if (!Visible)
            {
                sqlq = sqlq.Replace("(admin)", "left join we_department_owner do on de.id_row = do.id_department " +
                    "where de.Disabled = 0 and (do.id_user = " + info.UserID + " " +
                    "or de.id_row in (select distinct p1.id_department from we_project_team p1 join we_project_team_user pu on p1.id_row = pu.id_project_team " +
                    "where p1.Disabled = 0 and id_user = " + info.UserID + ")) and de.Disabled = 0 ");
            }
            else
                sqlq = sqlq.Replace("(admin)", " where de.Disabled = 0  ");
            #endregion
            DataTable dt = cnn.CreateDataTable(sqlq, conds);
            List<string> nvs = dt.AsEnumerable().Select(x => x["id_row"].ToString()).ToList();
            if (nvs.Count == 0)
                return "";
            string ids = string.Join(",", nvs);
            return ids;
        }
        public static List<AccUsernameModel> GetAccountFromJeeAccount(IHeaderDictionary pHeader, JeeWorkConfig config)
        {
            if (pHeader == null) return null;
            if (!pHeader.ContainsKey(HeaderNames.Authorization)) return null;
            IHeaderDictionary _d = pHeader;
            string _bearer_token;
            _bearer_token = _d[HeaderNames.Authorization].ToString();
            string API_Account = config.API_Account;
            string link_api = API_Account + "api/accountmanagement/usernamesByCustermerID";
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
    }
}