using DpsLibs.Data;
using System;
using System.Collections;
using DpsLibs.Data;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Options;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using JeeWork_Core2021.Models;
using JeeWork_Core2021.Classes;
using JeeWork_Core2021.Controllers.Users;
using DPSinfra.Notifier;
using Microsoft.AspNetCore.Http;
using DPSinfra.ConnectionCache;
using Microsoft.Extensions.Logging;
using API_JeeWork2021.Classes;
using DPSinfra.Kafka;
using JeeWork_Core2021.Controller;

namespace JeeWork_Core2021.Controllers.Wework
{

    [ApiController]
    [Route("api/project-team")]
    [EnableCors("JeeWorkPolicy")]
    /// <summary>
    /// api quản lý we_authorize
    /// </summary>
    // [EnableCors(origins: "*", headers: "*", methods: "*")]
    public class ProjectTeamController : ControllerBase
    {
        private readonly IHostingEnvironment _hostingEnvironment;
        private JeeWorkConfig _config;
        private readonly IConfiguration new_config;
        private INotifier _notifier;
        LoginController lc;
        public List<AccUsernameModel> DataAccount;
        private IConnectionCache ConnectionCache;
        private IConfiguration _configuration;
        private IProducer _producer;
        private readonly ILogger<ProjectTeamController> _logger;

        public ProjectTeamController(IOptions<JeeWorkConfig> config, IHostingEnvironment hostingEnvironment, INotifier notifier, IConnectionCache _cache, IConfiguration configuration, ILogger<ProjectTeamController> logger, IProducer producer)
        {
            ConnectionCache = _cache;
            _configuration = configuration;
            _hostingEnvironment = hostingEnvironment;
            _config = config.Value;
            _notifier = notifier;
            _logger = logger;
            _producer = producer;
        }
        APIModel.Models.Notify Knoti;
        /// <summary>
        /// ds project/team theo department
        /// </summary>
        /// <param name="query"></param>
        /// <returns></returns>
        /// 
        [Route("List-by-department")]
        [HttpGet]
        public object ListByDepartment([FromQuery] QueryParams query)
        {
            string Token = Common.GetHeader(Request);
            //Token = "f3d23d99-8342-49fe-afbb-211f525cae73";
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
                    string dieukienSort = "title", dieukien_where = " ";
                    if (string.IsNullOrEmpty(query.filter["id_department"]))
                        return JsonResultCommon.Custom("Ban bắt buộc nhập");
                    dieukien_where += " and id_department=@id_department";
                    Conds.Add("id_department", query.filter["id_department"]);
                    if (!string.IsNullOrEmpty(query.filter["keyword"]))
                    {
                        dieukien_where += " and (p.title like '%@keyword%' or p.description like '%@keyword%' or tao.Username like '%@keyword%' or sua.Username like '%@keyword%')";
                        dieukien_where = dieukien_where.Replace("@keyword", query.filter["keyword"]);
                    }
                    #region Sort data theo các dữ liệu bên dưới
                    Dictionary<string, string> sortableFields = new Dictionary<string, string>
                        {
                            { "title", "title"},
                            { "description", "description"},
                            { "CreatedBy", "NguoiTao"},
                            { "CreatedDate", "CreatedDate"},
                            { "UpdatedBy", "NguoiSua"},
                            {"UpdatedDate","UpdatedDate" },
                            {"TrangThai","status" },
                            {"department","department" },
                            {"Locked","Locked" },
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
                    string sqlq = @$"select p.*, de.title as department,coalesce(w.tong,0) as tong,coalesce( w.ht,0) as ht
                                    , coalesce(w.quahan,0) as quahan, '' as NguoiTao
                                    , '' as NguoiSua from we_project_team p 
                                    join we_department de on de.id_row=p.id_department
                                    left join (select count(*) as tong,COUNT(CASE WHEN w.status in (" + strhoanthanh + @") THEN 1 END) as ht
                                    , COUNT(CASE WHEN w.status in (" + strquahan + @$")THEN 1 END) as quahan
                                    ,w.id_project_team from v_wework_new w group by w.id_project_team) w on p.id_row=w.id_project_team
                                    where p.Disabled=0 and de.Disabled = 0 and p.CreatedBy in ({listID}) " + dieukien_where + "  order by " + dieukienSort;
                    sqlq += @$";select u.*,'' as hoten,'' as username, '' as tenchucdanh,'' as mobile,'' as image from we_project_team_user u 
                                join we_project_team p on p.id_row=u.id_project_team 
                                and id_department=" + query.filter["id_department"] + " where u.disabled=0  and u.Id_user in (" + listID + " )";
                    DataSet ds = cnn.CreateDataSet(sqlq, Conds);
                    if (cnn.LastError != null || ds == null)
                        return JsonResultCommon.Exception(_logger, cnn.LastError, _config, loginData, ControllerContext);
                    DataTable dt = ds.Tables[0];
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
                    #region Map info account từ JeeAccount

                    foreach (DataRow item in ds.Tables[0].Rows)
                    {
                        var infoNguoiTao = DataAccount.Where(x => item["CreatedBy"].ToString().Contains(x.UserId.ToString())).FirstOrDefault();
                        var infoNguoiSua = DataAccount.Where(x => item["UpdatedBy"].ToString().Contains(x.UserId.ToString())).FirstOrDefault();

                        if (infoNguoiTao != null)
                        {
                            item["NguoiTao"] = infoNguoiTao.FullName;
                        }
                        if (infoNguoiSua != null)
                        {
                            item["NguoiSua"] = infoNguoiSua.FullName;
                        }
                    }


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
                    // Phân trang
                    dt = dt.AsEnumerable().Skip((query.page - 1) * query.record).Take(query.record).CopyToDataTable();
                    var data = from r in dt.AsEnumerable()
                               select new
                               {
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
                                   users = from u in ds.Tables[1].AsEnumerable()
                                           where u["id_project_team"].ToString() == r["id_row"].ToString()
                                           select new
                                           {
                                               id_nv = u["id_user"],
                                               hoten = u["hoten"],
                                               username = u["username"],
                                               tenchucdanh = u["tenchucdanh"],
                                               mobile = u["mobile"],
                                               image = u["image"]
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
                return JsonResultCommon.Exception(_logger, ex, _config, loginData);
            }
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
            PageModel pageModel = new PageModel();
            try
            {
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
                    bool Visible = Common.CheckRoleByUserID(loginData, 3500, cnn);
                    string listDept = WeworkLiteController.getListDepartment_GetData(loginData, cnn, HttpContext.Request.Headers, _configuration, ConnectionString);
                    SqlConditions Conds = new SqlConditions();
                    string dieukienSort = "tong desc", dieukien_where = " p.disabled=0 and de.disabled = 0 and idkh = " + loginData.CustomerID + "";
                    if (!string.IsNullOrEmpty(query.filter["keyword"]))
                    {
                        dieukien_where += " and (p.title like N'%@keyword%' or p.description like N'%@keyword%')";
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
                    bool IsMobile = false;
                    if (!string.IsNullOrEmpty(query.filter["join"]) || !string.IsNullOrEmpty(query.filter["created_by"]))
                    {
                        IsMobile = true;
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
                    #region Ủy quyền giao việc
                    string sqluq = $@"or we_project_team_user.id_user in (select createdby from we_authorize
                                        where id_user = {loginData.UserID} and Disabled = 0 and start_date <= GETDATE() and end_date >= GETDATE())";
                    #endregion
                    if (!string.IsNullOrEmpty(query.sortField) && sortableFields.ContainsKey(query.sortField))
                        dieukienSort = sortableFields[query.sortField] + ("desc".Equals(query.sortOrder) ? " desc" : " asc");
                    #region get list trạng thái status 
                    List<string> lstHoanthanh = cnn.CreateDataTable("select id_row from we_status where IsFinal = 1").AsEnumerable().Select(x => x["id_row"].ToString()).ToList();
                    List<string> lstQuahan = cnn.CreateDataTable("select id_row from we_status where isDeadline = 1").AsEnumerable().Select(x => x["id_row"].ToString()).ToList();
                    string strhoanthanh = string.Join(",", lstHoanthanh);
                    string strquahan = string.Join(",", lstQuahan);
                    #endregion
                    string sqlq = @$"select distinct p.id_row, icon, p.title, p.description, p.id_department
                                    , p.loai, p.start_date, p.end_date, p.color, p.template, p.status
                                    , p.stage_description, allow_percent_done, require_evaluate
                                    , evaluate_by_assignner, allow_estimate_time, 
                                     stop_reminder, note, is_project, period_type, p.priority
                                    , p.createddate, p.createdby, p.Locked, p.disabled, p.updatedDate
                                    , p.UpdatedBy, p.email_assign_work, p.email_update_work
                                    , email_update_status, email_delete_work, 
                                     email_update_team, email_delete_team, email_upload_file
                                    , default_view, p.id_template, p.meetingid, de.title as department
                                    ,coalesce(w.tong,0) as tong
                                    ,coalesce( w.ht,0) as ht, coalesce(w.quahan,0) as quahan
                                    , '' as NguoiTao, '' as NguoiSua from we_project_team p 
                                    left join we_department de on de.id_row=p.id_department
                                    left join (select count(*) as tong
                                    , COUNT(CASE WHEN w.status in (" + strhoanthanh + @") THEN 1 END) as ht
                                    , COUNT(CASE WHEN w.status in (" + strquahan + @$")THEN 1 END) as quahan
                                    ,w.id_project_team from we_work w where w.Disabled=0 group by w.id_project_team) w 
                                    on p.id_row=w.id_project_team 
                                    (admin)  
                                    " + dieukien_where + "  order by " + dieukienSort;
                    string dk_user_by_project = " join we_project_team_user " +
                            "on we_project_team_user.id_project_team = p.id_row " +
                            "and (we_project_team_user.id_user = " + loginData.UserID + sqluq + ")" +
                            "where (de.id_row in (select distinct p1.id_department " +
                            "from we_project_team p1 join we_project_team_user pu on p1.id_row = pu.id_project_team " +
                            "where p1.Disabled = 0 and id_user = " + loginData.UserID + sqluq + ")) and ";
                    if (IsMobile)
                    {
                        if (!string.IsNullOrEmpty(query.filter["created_by"]))
                        {
                            sqlq = sqlq.Replace("(admin)", " where p.createdBy=" + loginData.UserID + " and ");
                        }
                        if (!string.IsNullOrEmpty(query.filter["join"]))
                        {
                            sqlq = sqlq.Replace("(admin)", dk_user_by_project);
                        }
                    }
                    else
                    {
                        if (Visible)
                        {
                            sqlq = sqlq.Replace("(admin)", " where ");
                        }
                        else
                        {
                            sqlq = sqlq.Replace("(admin)", dk_user_by_project);
                        }
                    }
                    sqlq += @$";select u.*,admin,'' as hoten,'' as username, '' as tenchucdanh,'' as mobile,'' as image 
                                from we_project_team_user u 
                                join we_project_team p on p.id_row=u.id_project_team 
                                where u.disabled=0";
                    // and u.Id_user in (" + listID + " )
                    #region Trả dữ liệu về backend để hiển thị lên giao diện
                    DataSet ds = cnn.CreateDataSet(sqlq, Conds);
                    if (cnn.LastError != null || ds == null)
                        return JsonResultCommon.Exception(_logger, cnn.LastError, _config, loginData, ControllerContext);
                    DataTable dt = ds.Tables[0];
                    if (dt.Rows.Count == 0)
                        return JsonResultCommon.ThanhCong(new List<string>(), pageModel);
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
                    var temp = dt.AsEnumerable().Distinct();
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
                    var data = (from r in dt.AsEnumerable()
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
                                }).Distinct();
                    return JsonResultCommon.ThanhCong(data, pageModel);
                }
                #endregion
            }
            catch (Exception ex)
            {
                return JsonResultCommon.Exception(_logger, ex, _config, loginData);
            }
        }

        [Route("Detail")]
        [HttpGet]
        public object Detail(long id)
        {
            string Token = Common.GetHeader(Request);
            //Token = "f3d23d99-8342-49fe-afbb-211f525cae73";
            //UserJWT loginData = Ulities.GetUserByHeader(HttpContext.Request.Headers);
            UserJWT loginData = Ulities.GetUserByHeader(HttpContext.Request.Headers);

            if (loginData == null)
                return JsonResultCommon.DangNhap();
            try
            {
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

                    string sqlq = @$"select p.*, de.title as department,coalesce(w.tong,0) as tong,coalesce( w.ht,0) as ht
                                , coalesce(w.quahan,0) as quahan, '' as NguoiTao
                                , '' as NguoiSua from we_project_team p 
                                left join we_department de on de.id_row=p.id_department
                                left join (select count(*) as tong, COUNT(CASE WHEN w.status=2 THEN 1 END) as ht
                                , COUNT(CASE WHEN w.status=1 and getdate()>w.deadline THEN 1 END) as quahan
                                ,w.id_project_team from v_wework_new w group by w.id_project_team) w on p.id_row=w.id_project_team
                                where p.Disabled=0 and p.id_row=" + id;
                    sqlq += @$";select u.*,  '' as hoten,'' as username, '' as tenchucdanh,'' as mobile,'' as image, admin from we_project_team_user u 
                                join we_project_team p on p.id_row=u.id_project_team and p.id_row=" + id + $" where u.disabled=0 ";
                    sqlq += $";select *, '' as hoten,'' as username, '' as tenchucdanh,'' as mobile,'' as image " +
                        $"from we_project_team_stage s where id_project_team=" + id;
                    sqlq += $";exec GetActivitiesNew '{listID}'," + id;
                    sqlq += ";select * from we_group where disabled=0 and  id_project_team=" + id;
                    sqlq += @$"select m.*, coalesce(w.tong,0) as tong,coalesce( w.ht,0) as ht, '' as hoten,'' as username, '' as tenchucdanh,'' as mobile,'' as image  from we_milestone m 
                                left join (select count(*) as tong, COUNT(CASE WHEN w.status=2 THEN 1 END) as ht,w.id_milestone 
                                from v_wework_new w group by w.id_milestone) w on m.id_row=w.id_milestone
                                where m.Disabled=0 and id_project_team=" + id;

                    DataSet ds = cnn.CreateDataSet(sqlq);
                    if (cnn.LastError != null || ds == null)
                    {
                        return JsonResultCommon.Exception(_logger, cnn.LastError, _config, loginData, ControllerContext);
                    }
                    if (ds.Tables[0] == null || ds.Tables[0].Rows.Count == 0)
                        return JsonResultCommon.KhongTonTai();
                    #region Map info account từ JeeAccount
                    // table 0
                    foreach (DataRow item in ds.Tables[0].Rows)
                    {
                        var infoNguoiTao = DataAccount.Where(x => item["CreatedBy"].ToString().Contains(x.UserId.ToString())).FirstOrDefault();
                        var infoNguoiSua = DataAccount.Where(x => item["UpdatedBy"].ToString().Contains(x.UserId.ToString())).FirstOrDefault();

                        if (infoNguoiTao != null)
                        {
                            item["NguoiTao"] = infoNguoiTao.FullName;
                        }
                        if (infoNguoiSua != null)
                        {
                            item["NguoiSua"] = infoNguoiSua.FullName;
                        }

                    }
                    // table 1
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
                    // table 2
                    foreach (DataRow item in ds.Tables[2].Rows)
                    {
                        var info = DataAccount.Where(x => item["CreatedBy"].ToString().Contains(x.UserId.ToString())).FirstOrDefault();

                        if (info != null)
                        {
                            item["hoten"] = info.FullName;
                            item["username"] = info.Username;
                            item["tenchucdanh"] = info.Jobtitle;
                            item["mobile"] = info.PhoneNumber;
                            item["image"] = info.AvartarImgURL;
                        }

                    }
                    // table 3
                    ds.Tables[3].Columns.Add("hoten", typeof(string));
                    ds.Tables[3].Columns.Add("username", typeof(string));
                    ds.Tables[3].Columns.Add("tenchucdanh", typeof(string));
                    ds.Tables[3].Columns.Add("mobile", typeof(string));
                    ds.Tables[3].Columns.Add("image", typeof(string));
                    foreach (DataRow item in ds.Tables[3].Rows)
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

                    // table 5
                    foreach (DataRow item in ds.Tables[5].Rows)
                    {
                        var info = DataAccount.Where(x => item["person_in_charge"].ToString().Contains(x.UserId.ToString())).FirstOrDefault();

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

                    #region Trả dữ liệu về backend để hiển thị lên giao diện
                    var data = (from r in ds.Tables[0].AsEnumerable()
                                select new
                                {
                                    id_row = r["id_row"],
                                    title = r["title"],
                                    icon = r["icon"] == DBNull.Value ? "" : WeworkLiteController.genLinkAttachment(_configuration, r["icon"]),
                                    description = r["description"],
                                    detail = r["detail"],
                                    id_department = r["id_department"],
                                    department = r["department"],
                                    CreatedDate = string.Format("{0:dd/MM/yyyy HH:mm}", r["CreatedDate"]),
                                    CreatedBy = r["CreatedBy"],
                                    NguoiTao = r["NguoiTao"],
                                    UpdatedDate = r["UpdatedDate"] == DBNull.Value ? "" : string.Format("{0:dd/MM/yyyy HH:mm}", r["UpdatedDate"]),
                                    UpdatedBy = r["UpdatedBy"],
                                    NguoiSua = r["NguoiSua"],
                                    loai = r["loai"],
                                    is_project = r["is_project"],
                                    color = r["color"],
                                    status = r["status"],
                                    locked = r["locked"],
                                    stage_description = r["stage_description"],
                                    allow_estimate_time = r["allow_estimate_time"],
                                    require_evaluate = r["require_evaluate"],
                                    evaluate_by_assignner = r["evaluate_by_assignner"],
                                    allow_percent_done = r["allow_percent_done"],
                                    start_date = r["start_date"],
                                    str_start_date = r["start_date"] == DBNull.Value ? "" : string.Format("{0:dd/MM/yyyy}", r["start_date"]),
                                    end_date = r["end_date"],
                                    str_end_date = r["end_date"] == DBNull.Value ? "" : string.Format("{0:dd/MM/yyyy}", r["end_date"]),
                                    //1: streamview; 2: period view, 3: board view, 4: list view, 5: gantt
                                    default_view = r["default_view"],
                                    //1: hàng tuần, 2 hàng tháng
                                    period_type = r["period_type"],
                                    id_template = r["id_template"],
                                    Users = from u in ds.Tables[1].AsEnumerable()
                                            where u["id_project_team"].ToString() == r["id_row"].ToString()
                                            select new
                                            {
                                                id_row = u["id_row"],
                                                id_project_team = u["id_project_team"],
                                                id_user = u["id_user"],
                                                admin = u["admin"],
                                                id_nv = u["id_user"],
                                                hoten = u["hoten"],
                                                username = u["username"],
                                                tenchucdanh = u["tenchucdanh"],
                                                mobile = u["mobile"],
                                                favourite = u["favourite"],
                                                image = u["image"],
                                            },
                                    Count = new
                                    {
                                        tong = r["tong"],
                                        ht = r["ht"],
                                        quahan = r["quahan"],
                                        percentage = WeworkLiteController.calPercentage(r["tong"], r["ht"])
                                    },
                                    Stages = from s in ds.Tables[2].AsEnumerable()
                                             select new
                                             {
                                                 stage = s["stage"],
                                                 description = s["description"],
                                                 CreatedDate = string.Format("{0:dd/MM/yyyy HH:mm}", s["CreatedDate"]),
                                                 CreatedBy = new
                                                 {
                                                     id_nv = s["CreatedBy"],
                                                     hoten = s["hoten"],
                                                     username = s["username"],
                                                     tenchucdanh = s["tenchucdanh"],
                                                     mobile = s["mobile"],
                                                     image = s["image"],
                                                 }
                                             },
                                    Log = from dr in ds.Tables[3].AsEnumerable()
                                          group dr by new { a = dr["object_type"], b = dr["object_id"], c = dr["title"], d = dr["project_team"], e = dr["id_project_team"] } into g
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
                                                           }
                                          },
                                    groups = from dr in ds.Tables[4].AsEnumerable()
                                             select new
                                             {
                                                 id_row = dr["id_row"],
                                                 title = dr["title"]
                                             },
                                    milestone = from m in ds.Tables[5].AsEnumerable()
                                                select new
                                                {
                                                    id_row = m["id_row"],
                                                    title = m["title"],
                                                    deadline_weekday = m["deadline"].Equals(DBNull.Value) ? "" : Common.GetFormatDate(Convert.ToDateTime(m["deadline"]), "77622"),
                                                    deadline_day = m["deadline"].Equals(DBNull.Value) ? "" : Common.GetFormatDate(Convert.ToDateTime(m["deadline"]), "dd/MM"),
                                                    person_in_charge = new
                                                    {
                                                        id_nv = m["person_in_charge"],
                                                        hoten = m["hoten"],
                                                        username = m["username"],
                                                        tenchucdanh = m["tenchucdanh"],
                                                        mobile = m["mobile"],
                                                        image = m["image"],
                                                    },
                                                    Count = new
                                                    {
                                                        tong = m["tong"],
                                                        ht = m["ht"]
                                                    }
                                                }
                                }).FirstOrDefault();
                    return JsonResultCommon.ThanhCong(data);
                    #endregion
                }
            }
            catch (Exception ex)
            {
                return JsonResultCommon.Exception(_logger, ex, _config, loginData);
            }
        }

        [Route("get-department")]
        [HttpGet]
        public object FindDepartmentFromProjectteam(long id)
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
                    string sqlq = @"select id_department from we_project_team where id_row =" + id;
                    string data = cnn.ExecuteScalar(sqlq).ToString();
                    return JsonResultCommon.ThanhCong(data);
                }
            }
            catch (Exception ex)
            {
                return JsonResultCommon.Exception(_logger, ex, _config, loginData);
            }
        }

        [Route("overview")]
        [HttpGet]
        public object Overview(long id)
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
                    #region Lấy dữ liệu account từ JeeAccount
                    DataAccount = WeworkLiteController.GetAccountFromJeeAccount(HttpContext.Request.Headers, _configuration);
                    if (DataAccount == null)
                        return JsonResultCommon.Custom("Lỗi lấy danh sách nhân viên từ hệ thống quản lý tài khoản");

                    string error = "";
                    string listID = WeworkLiteController.ListAccount(HttpContext.Request.Headers, out error, _configuration);
                    if (error != "")
                        return JsonResultCommon.Custom(error);
                    #endregion
                    // cần sửa status
                    #region Trả dữ liệu về backend để hiển thị lên giao diện
                    string sqlq = @"select w.*, 
iIf(w.Status=2 and w.end_date>w.deadline,1,0) as is_htquahan,
iIf(w.Status = 2 and w.end_date <= w.deadline, 1, 0) as is_htdunghan ,
iIf(w.Status = 1, 1, 0) as is_danglam,
iIf(w.Status = 1 and getdate() > w.deadline, 1, 0) as is_quahan,
iIf(w.Status = 3, 1, 0) as is_cho,
Iif(convert(varchar, w.deadline,103) like convert(varchar, GETDATE(),103),1,0) as duetoday
from v_wework_new w 
where w.disabled=0 and w.id_parent is null and id_project_team=" + id;
                    DataSet ds = cnn.CreateDataSet(sqlq);
                    if (cnn.LastError != null || ds == null)
                    {
                        return JsonResultCommon.Exception(_logger, cnn.LastError, _config, loginData, ControllerContext);
                    }
                    #region Map info account từ JeeAccount
                    // table 0
                    foreach (DataRow item in ds.Tables[0].Rows)
                    {
                        var info = DataAccount.Where(x => item["id_nv"].ToString().Contains(x.UserId.ToString())).FirstOrDefault();

                        if (info != null)
                        {
                            item["hoten"] = info.FullName;
                            item["Mobile"] = info.PhoneNumber;
                            item["Username"] = info.Username;
                            item["Tenchucdanh"] = info.Jobtitle;
                        }

                    }
                    #endregion
                    var temp = ds.Tables[0].AsEnumerable();
                    var data = new
                    {
                        DueToday = from r in temp
                                   where r["duetoday"].ToString() == "1"
                                   select new
                                   {
                                       id_row = r["id_row"],
                                       title = r["title"],
                                       status = r["status"],
                                       duetoday = r["duetoday"],
                                       urgent = r["urgent"],
                                   },
                        NotStarted = from r in temp
                                     where r["start_date"] == DBNull.Value
                                     select new
                                     {
                                         id_row = r["id_row"],
                                         title = r["title"],
                                         status = r["status"],
                                         duetoday = r["duetoday"],
                                         urgent = r["urgent"],
                                     },
                        ByNV = temp.GroupBy(x => new { id_nv = x["id_nv"], hoten = x["hoten"] })
                        .Where(group => group.Count() > 0)
                        .Select(group => new
                        {
                            id_nv = group.Key.id_nv,
                            hoten = group.Key.id_nv == DBNull.Value ? "Not assigned" : group.Key.hoten,
                            Count = new
                            {
                                tong = group.Count(),
                                ht = group.Count(x => x["status"].ToString() == "2"),
                            }
                        }),
                        Count = new
                        {
                            tong = temp.Count(),
                            htdunghan = temp.Count(w => w["is_htdunghan"].ToString() == "1"),
                            htquahan = temp.Count(w => w["is_htquahan"].ToString() == "1"),
                            quahan = temp.Count(w => w["is_quahan"].ToString() == "1"),
                            danglam = temp.Count(w => w["is_danglam"].ToString() == "1"),
                            cho = temp.Count(w => w["is_cho"].ToString() == "1")
                        }
                    };
                    return JsonResultCommon.ThanhCong(data);
                    #endregion
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
        /// <param name="data"></param>
        /// <returns></returns>
        /// 
        [CusAuthorize(Roles = "3501")]
        [Route("Insert")]
        [HttpPost]
        public async Task<object> Insert([FromBody] ProjectTeamModel data)
        {
            string Token = Common.GetHeader(Request);
            //UserJWT loginData = Ulities.GetUserByHeader(HttpContext.Request.Headers);
            UserJWT loginData = Ulities.GetUserByHeader(HttpContext.Request.Headers);
            if (loginData == null)
                return JsonResultCommon.DangNhap();
            try
            {
                string strRe = "";
                if (string.IsNullOrEmpty(data.title))
                    strRe += (strRe == "" ? "" : ",") + "tên dự án/phòng ban";
                //if (data.id_department <= 0)
                //    strRe += (strRe == "" ? "" : ",") + "ban";
                if (data.is_project && data.loai <= 0)
                    strRe += (strRe == "" ? "" : ",") + "loại dự án";
                if (data.Users == null || data.Users.Count == 0)
                    strRe += (strRe == "" ? "" : ",") + "người quản trị";
                else
                {
                    if (data.Users.Where(x => x.admin).Count() == 0)
                        strRe += (strRe == "" ? "" : ",") + "người quản trị";
                }

                if (strRe != "")
                    return JsonResultCommon.BatBuoc(strRe);
                string ConnectionString = WeworkLiteController.getConnectionString(ConnectionCache, loginData.CustomerID, _configuration);
                using (DpsConnection cnn = new DpsConnection(ConnectionString))
                {
                    #region Lấy dữ liệu account từ JeeAccount
                    DataAccount = WeworkLiteController.GetAccountFromJeeAccount(HttpContext.Request.Headers, _configuration);
                    if (DataAccount == null)
                        return JsonResultCommon.Custom("Lỗi lấy danh sách nhân viên từ hệ thống quản lý tài khoản");
                    #endregion
                    long iduser = loginData.UserID;
                    long idk = loginData.CustomerID;
                    Hashtable val = new Hashtable();
                    if (data.icon != null)
                    {
                        string x = "";
                        string folder = "/logo/";
                        if (!UploadHelper.UploadFile(data.icon.strBase64, data.icon.filename, folder, _hostingEnvironment.ContentRootPath, ref x, _configuration))
                            return JsonResultCommon.Custom("Không thể cập nhật hình ảnh");
                        val.Add("icon", x);
                    }
                    val.Add("title", data.title);
                    val.Add("id_department", data.id_department);
                    val.Add("description", string.IsNullOrEmpty(data.description) ? "" : data.description);
                    val.Add("stage_description", string.IsNullOrEmpty(data.stage_description) ? "" : data.stage_description);
                    val.Add("loai", data.loai);
                    val.Add("is_project", data.is_project);
                    if (data.start_date != DateTime.MinValue)
                        val.Add("start_date", data.start_date);
                    if (data.end_date != DateTime.MinValue)
                        val.Add("end_date", data.end_date);
                    val.Add("status", data.status);
                    if (!string.IsNullOrEmpty(data.color))
                        val.Add("color", data.color);
                    val.Add("CreatedDate", DateTime.Now);
                    val.Add("CreatedBy", iduser);
                    string strCheck = "select count(*) from we_project_team where Disabled=0 and  (id_department=@id_department) and title=@name";
                    if (int.Parse(cnn.ExecuteScalar(strCheck, new SqlConditions() { { "id_department", data.id_department }, { "name", data.title } }).ToString()) > 0)
                    {
                        return JsonResultCommon.Trung("Dự án/phòng ban");
                    }
                    cnn.BeginTransaction();
                    if (cnn.Insert(val, "we_project_team") != 1)
                    {
                        cnn.RollbackTransaction();
                        return JsonResultCommon.Exception(_logger, cnn.LastError, _config, loginData, ControllerContext);
                    }
                    //string LogContent = "", LogEditContent = "";
                    //LogContent = LogEditContent = "Thêm mới dữ liệu project_team: title=" + data.title + ", id_department=" + data.id_department + ", loai=" + data.loai;
                    //DpsPage.Ghilogfile(loginData.CustomerID.ToString(), LogEditContent, LogContent, loginData.UserName);
                    long idc = long.Parse(cnn.ExecuteScalar("select IDENT_CURRENT('we_project_team')").ToString());
                    Hashtable val1 = new Hashtable();
                    val1["id_project_team"] = idc;
                    val1["CreatedDate"] = DateTime.Now;
                    val1["CreatedBy"] = iduser;
                    foreach (var owner in data.Users)
                    {
                        val1["id_user"] = owner.id_user;
                        val1["admin"] = owner.admin;
                        if (cnn.Insert(val1, "we_project_team_user") != 1)
                        {
                            cnn.RollbackTransaction();
                            return JsonResultCommon.Exception(_logger, cnn.LastError, _config, loginData, ControllerContext);
                        }
                    }
                    if (!WeworkLiteController.log(_logger, loginData.Username, cnn, 31, idc, iduser, data.title))
                    {
                        cnn.RollbackTransaction();
                        return JsonResultCommon.Exception(_logger, cnn.LastError, _config, loginData, ControllerContext);
                    }
                    // Tạo status mặc định cho project này dựa vào id_department
                    string sql_insert = "";
                    long TemplateID = long.Parse(cnn.ExecuteScalar("select TemplateID from we_department").ToString());
                    if (TemplateID > 0)
                    {
                        sql_insert = $@"update we_project_team set id_template = " + TemplateID + " where id_row = " + idc;
                        sql_insert += $@";insert into we_status (StatusName, description, id_project_team, CreatedDate, CreatedBy, Disabled, UpdatedDate, UpdatedBy, Type, IsDefault, color, Position, IsFinal, Follower, IsDeadline, IsToDo, StatusID_Reference)
                        select StatusName, description, " + idc + ", CreatedDate, CreatedBy, Disabled, UpdatedDate, UpdatedBy, Type, IsDefault, color, Position, IsFinal, Follower, IsDeadline, IsToDo, id_row from we_template_status where Disabled = 0 and TemplateID = " + TemplateID + "";
                        cnn.ExecuteNonQuery(sql_insert);
                        if (cnn.LastError != null)
                        {
                            cnn.RollbackTransaction();
                            return JsonResultCommon.Exception(_logger, cnn.LastError, _config, loginData, ControllerContext);
                        }
                        WeworkLiteController.update_position_status(idc, cnn);
                    }
                    #region Khởi tạo các cột hiển thị mặc định cho công việc
                    if (!WeworkLiteController.Init_Column_Project(idc, cnn))
                    {
                        cnn.RollbackTransaction();
                        return JsonResultCommon.Exception(_logger, cnn.LastError, _config, loginData, ControllerContext);
                    }
                    #endregion

                    cnn.EndTransaction();
                    data.id_row = idc;
                    Hashtable has_replace = new Hashtable();
                    List<long> users_admin = data.Users.Where(x => x.id_row == 0 && x.admin).Select(x => x.id_user).ToList();
                    List<long> users_member = data.Users.Where(x => x.id_row == 0 && !x.admin).Select(x => x.id_user).ToList();
                    WeworkLiteController.mailthongbao(idc, data.Users.Where(x => x.admin).Select(x => x.id_user).ToList(), 6, loginData, ConnectionString, _notifier, _configuration);//thiết lập vai trò admin
                    #region Notify thiết lập vai trò admin
                    for (int i = 0; i < users_admin.Count; i++)
                    {
                        NotifyModel notify_model = new NotifyModel();
                        has_replace = new Hashtable();
                        has_replace.Add("nguoigui", loginData.Username);
                        has_replace.Add("project_team", data.title);
                        notify_model.AppCode = "WORK";
                        notify_model.From_IDNV = loginData.UserID.ToString();
                        notify_model.To_IDNV = data.Users[i].id_user.ToString();
                        notify_model.TitleLanguageKey = LocalizationUtility.GetBackendMessage("ww_thietlapvaitroadmin", "", "vi");
                        notify_model.TitleLanguageKey = notify_model.TitleLanguageKey.Replace("$nguoigui$", loginData.customdata.personalInfo.Fullname);
                        notify_model.TitleLanguageKey = notify_model.TitleLanguageKey.Replace("$project_team$", data.title);
                        notify_model.ReplaceData = has_replace;
                        notify_model.To_Link_MobileApp = "";
                        notify_model.ComponentName = "";
                        notify_model.Component = "";
                        notify_model.To_Link_WebApp = "/project/" + data.id_row + "/settings/members";
                        try
                        {
                            if (notify_model != null)
                            {
                                Knoti = new APIModel.Models.Notify();
                                bool kq = Knoti.PushNotify(notify_model.From_IDNV, notify_model.To_IDNV, notify_model.AppCode, "ww_thietlapvaitroadmin", notify_model.ReplaceData, notify_model.To_Link_WebApp, notify_model.To_Link_MobileApp, notify_model.ComponentName, notify_model.Component);
                            }
                        }
                        catch
                        { }
                        var info = DataAccount.Where(x => data.Users[i].id_user.ToString().Contains(x.UserId.ToString())).FirstOrDefault();
                        if (info is not null)
                        {
                            bool kq_noti = WeworkLiteController.SendNotify(loginData.Username, info.Username, notify_model, _notifier);
                        }

                    }
                    #endregion
                    WeworkLiteController.mailthongbao(idc, data.Users.Where(x => !x.admin).Select(x => x.id_user).ToList(), 5, loginData, ConnectionString, _notifier, _configuration);//thêm vào dự án
                    #region Notify thiết lập vai trò member
                    for (int i = 0; i < users_member.Count; i++)
                    {
                        NotifyModel notify_model = new NotifyModel();
                        has_replace = new Hashtable();
                        has_replace.Add("nguoigui", loginData.Username);
                        has_replace.Add("project_team", data.title);
                        notify_model.AppCode = "WORK";
                        notify_model.From_IDNV = loginData.UserID.ToString();
                        notify_model.To_IDNV = data.Users[i].id_user.ToString();
                        notify_model.TitleLanguageKey = "ww_thietlapvaitrothanhvien";
                        notify_model.TitleLanguageKey = LocalizationUtility.GetBackendMessage("ww_thietlapvaitrothanhvien", "", "vi");
                        notify_model.TitleLanguageKey = notify_model.TitleLanguageKey.Replace("$nguoigui$", loginData.customdata.personalInfo.Fullname);
                        notify_model.TitleLanguageKey = notify_model.TitleLanguageKey.Replace("$project_team$", data.title);
                        notify_model.ReplaceData = has_replace;
                        notify_model.To_Link_MobileApp = "";
                        notify_model.ComponentName = "";
                        notify_model.Component = "";
                        notify_model.To_Link_WebApp = "/project/" + data.id_row + "/settings/members";
                        try
                        {
                            if (notify_model != null)
                            {
                                Knoti = new APIModel.Models.Notify();
                                bool kq = Knoti.PushNotify(notify_model.From_IDNV, notify_model.To_IDNV, notify_model.AppCode, "ww_thietlapvaitrothanhvien", notify_model.ReplaceData, notify_model.To_Link_WebApp, notify_model.To_Link_MobileApp, notify_model.ComponentName, notify_model.Component);
                            }
                        }
                        catch
                        { }

                        var info = DataAccount.Where(x => data.Users[i].id_user.ToString().Contains(x.UserId.ToString())).FirstOrDefault();
                        if (info is not null)
                        {
                            bool kq_noti = WeworkLiteController.SendNotify(loginData.Username, info.Username, notify_model, _notifier);
                        }

                    }
                    #endregion
                    return JsonResultCommon.ThanhCong(data);
                }
            }
            catch (Exception ex)
            {
                return JsonResultCommon.Exception(_logger, ex, _config, loginData);
            }
        }

        /// <summary>
        /// Tạo dự án từ JeeMeeting
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        /// 
        [Route("generate-projects-auto")]
        [HttpPost]
        public async Task<object> Generate_Projects([FromBody] GenerateProjectAutoModel data)
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
                    #region Lấy dữ liệu account từ JeeAccount
                    DataAccount = WeworkLiteController.GetAccountFromJeeAccount(HttpContext.Request.Headers, _configuration);
                    if (DataAccount == null)
                        return JsonResultCommon.Custom("Lỗi lấy danh sách nhân viên từ hệ thống quản lý tài khoản");
                    #endregion
                    //string strCheck = @$"select id_row, id_department, id_template, meetingid 
                    //                        from we_project_team where (where)";
                    SqlConditions conds = new SqlConditions();
                    //conds.Add("meetingid", data.meetingid);
                    //conds.Add("disabled", 0);
                    string strCheck = @$"select id_row, idkh, phanloaiid from we_department where (where)";
                    conds.Add("idkh", loginData.CustomerID);
                    DataTable dt_check = cnn.CreateDataTable(strCheck, "(where)", conds);
                    if (cnn.LastError != null || dt_check == null)
                    {
                        cnn.RollbackTransaction();
                        return JsonResultCommon.Exception(_logger, cnn.LastError, _config, loginData, ControllerContext);
                    }
                    string error = "", departmentid = "0";
                    if (dt_check.Rows.Count == 0) // Chưa có dự án trong cuộc họp ==> Khởi tạo dự án
                    {
                        if (WeworkLiteController.init_space(cnn, loginData, data, out error))
                        {
                            departmentid = cnn.ExecuteScalar("select IDENT_CURRENT('we_department')").ToString();
                        }
                    }
                    else
                        departmentid = dt_check.Rows[0]["id_row"].ToString();
                    data.id_department = long.Parse(departmentid);
                    Hashtable val = new Hashtable();
                    val.Add("title", data.title);
                    val.Add("id_department", departmentid);
                    val.Add("loai", data.loai);
                    val.Add("meetingid", data.meetingid);
                    val.Add("CreatedDate", DateTime.Now);
                    val.Add("CreatedBy", loginData.UserID);
                    strCheck = "select count(*) from we_project_team where Disabled=0 and (id_department=@id_department) and title=@name";
                    if (int.Parse(cnn.ExecuteScalar(strCheck, new SqlConditions() { { "id_department", data.id_department }, { "name", data.title } }).ToString()) > 0)
                    {
                        return JsonResultCommon.Trung("Dự án/phòng ban");
                    }
                    cnn.BeginTransaction();
                    if (cnn.Insert(val, "we_project_team") != 1)
                    {
                        cnn.RollbackTransaction();
                        return JsonResultCommon.Exception(_logger, cnn.LastError, _config, loginData, ControllerContext);
                    }
                    long idc = long.Parse(cnn.ExecuteScalar("select IDENT_CURRENT('we_project_team')").ToString());
                    data.listid = idc;
                    DataTable dt_member = new DataTable();
                    // insert thành viên
                    val = new Hashtable();
                    val["id_project_team"] = idc;
                    val["createddate"] = DateTime.Now;
                    val["createdby"] = loginData.UserID;
                    foreach (var owner in data.Users)
                    {
                        val["id_user"] = owner.id_user;
                        val["admin"] = owner.admin;
                        if (cnn.Insert(val, "we_project_team_user") != 1)
                        {
                            cnn.RollbackTransaction();
                            return JsonResultCommon.Exception(_logger, cnn.LastError, _config, loginData, ControllerContext);
                        }
                    }
                    string sql_insert = "";
                    var list_roles = new List<long> { 1, 11 };
                    if (!WeworkLiteController.Init_RoleDefault(idc, list_roles, cnn))
                    {
                        cnn.RollbackTransaction();
                        return JsonResultCommon.Exception(_logger, cnn.LastError, _config, loginData, ControllerContext);
                    }
                    #region Khởi tạo view mặc định
                    if (!WeworkLiteController.Init_DefaultView_Project(idc, cnn))
                    {
                        cnn.RollbackTransaction();
                        return JsonResultCommon.Exception(_logger, cnn.LastError, _config, loginData, ControllerContext);
                    }
                    #endregion
                    // Tạo status mặc định cho project này dựa vào id_department
                    var TemplateID = cnn.ExecuteScalar("select templateid from we_department where id_row = " + departmentid + "").ToString();
                    if (TemplateID == null)
                    {
                        TemplateID = "1";
                    }
                    sql_insert = "";
                    sql_insert = $@"update we_project_team set id_template = " + TemplateID + " where id_row = " + idc;
                    sql_insert += $@";insert into we_status (statusName, description, id_project_team, CreatedDate, CreatedBy, Disabled, UpdatedDate, UpdatedBy, Type, IsDefault, color, Position, IsFinal, Follower, IsDeadline, IsToDo, StatusID_Reference)
                        select statusName, description, " + idc + ", CreatedDate, CreatedBy, Disabled, UpdatedDate, UpdatedBy, Type, IsDefault, color, Position, IsFinal, Follower, IsDeadline, IsToDo, id_row from we_template_status where Disabled = 0 and TemplateID = " + TemplateID + "";
                    cnn.ExecuteNonQuery(sql_insert);
                    if (cnn.LastError != null)
                    {
                        cnn.RollbackTransaction();
                        return JsonResultCommon.Exception(_logger, cnn.LastError, _config, loginData, ControllerContext);
                    }
                    WeworkLiteController.update_position_status(idc, cnn);
                    #region Khởi tạo các cột hiển thị mặc định cho công việc
                    if (!WeworkLiteController.Init_Column_Project(idc, cnn))
                    {
                        cnn.RollbackTransaction();
                        return JsonResultCommon.Exception(_logger, cnn.LastError, _config, loginData, ControllerContext);
                    }
                    #endregion
                    if (!WeworkLiteController.log(_logger, loginData.Username, cnn, 31, idc, loginData.UserID, data.title))
                    {
                        cnn.RollbackTransaction();
                        return JsonResultCommon.Exception(_logger, cnn.LastError, _config, loginData, ControllerContext);
                    }
                    cnn.EndTransaction();
                    data.listid = idc;
                    Hashtable has_replace = new Hashtable();
                    var users_admin = new List<long> { loginData.UserID };
                    WeworkLiteController.mailthongbao(idc, users_admin, 6, loginData, ConnectionString, _notifier, _configuration);//thiết lập vai trò admin
                    #region Notify thiết lập vai trò admin
                    for (int i = 0; i < users_admin.Count; i++)
                    {
                        NotifyModel notify_model = new NotifyModel();
                        has_replace = new Hashtable();
                        has_replace.Add("nguoigui", loginData.Username);
                        has_replace.Add("project_team", "Cuộc họp");
                        notify_model.AppCode = "WORK";
                        notify_model.From_IDNV = loginData.UserID.ToString();
                        notify_model.To_IDNV = users_admin[i].ToString();
                        notify_model.TitleLanguageKey = LocalizationUtility.GetBackendMessage("ww_thietlapvaitroadmin", "", "vi");
                        notify_model.TitleLanguageKey = notify_model.TitleLanguageKey.Replace("$nguoigui$", loginData.customdata.personalInfo.Fullname);
                        notify_model.TitleLanguageKey = notify_model.TitleLanguageKey.Replace("$project_team$", "Cuộc họp");
                        notify_model.ReplaceData = has_replace;
                        notify_model.To_Link_MobileApp = "";
                        notify_model.ComponentName = "";
                        notify_model.Component = "";
                        notify_model.To_Link_WebApp = "/project/" + data.listid + "/settings/members";
                        try
                        {
                            if (notify_model != null)
                            {
                                Knoti = new APIModel.Models.Notify();
                                bool kq = Knoti.PushNotify(notify_model.From_IDNV, notify_model.To_IDNV, notify_model.AppCode, "ww_thietlapvaitroadmin", notify_model.ReplaceData, notify_model.To_Link_WebApp, notify_model.To_Link_MobileApp, notify_model.ComponentName, notify_model.Component);
                            }
                        }
                        catch
                        { }

                        //var info = DataAccount.Where(x => notify_model.To_IDNV.ToString().Contains(x.UserId.ToString())).FirstOrDefault();
                        //if (info is not null)
                        //{
                        //    bool kq_noti = WeworkLiteController.SendNotify(loginData.Username, info.Username, notify_model,_notifier);
                        //}
                    }
                    #endregion
                    foreach (DataRow item in dt_member.Rows)
                    {
                        var users_member = new List<long> { long.Parse(item["id_user"].ToString()) };
                        WeworkLiteController.mailthongbao(idc, users_member, 5, loginData, ConnectionString, _notifier, _configuration);//thêm vào dự án
                        #region Notify thiết lập vai trò member
                        for (int i = 0; i < users_member.Count; i++)
                        {
                            NotifyModel notify_model = new NotifyModel();
                            has_replace = new Hashtable();
                            has_replace.Add("nguoigui", loginData.Username);
                            has_replace.Add("project_team", data.title);
                            notify_model.AppCode = "WORK";
                            notify_model.From_IDNV = loginData.UserID.ToString();
                            notify_model.To_IDNV = users_member[i].ToString();
                            notify_model.TitleLanguageKey = LocalizationUtility.GetBackendMessage("ww_thietlapvaitrothanhvien", "", "vi");
                            notify_model.TitleLanguageKey = notify_model.TitleLanguageKey.Replace("$nguoigui$", loginData.customdata.personalInfo.Fullname);
                            notify_model.TitleLanguageKey = notify_model.TitleLanguageKey.Replace("$project_team$", data.title);
                            notify_model.ReplaceData = has_replace;
                            notify_model.To_Link_MobileApp = "";
                            notify_model.ComponentName = "";
                            notify_model.Component = "";
                            notify_model.To_Link_WebApp = "/project/" + data.listid + "/settings/members";
                            try
                            {
                                if (notify_model != null)
                                {
                                    Knoti = new APIModel.Models.Notify();
                                    bool kq = Knoti.PushNotify(notify_model.From_IDNV, notify_model.To_IDNV, notify_model.AppCode, "ww_thietlapvaitrothanhvien", notify_model.ReplaceData, notify_model.To_Link_WebApp, notify_model.To_Link_MobileApp, notify_model.ComponentName, notify_model.Component);
                                }
                            }
                            catch
                            { }
                            //var info = DataAccount.Where(x => notify_model.To_IDNV.ToString().Contains(x.UserId.ToString())).FirstOrDefault();
                            //if (info is not null)
                            //{
                            //    bool kq_noti = WeworkLiteController.SendNotify(loginData.Username, info.Username, notify_model,_notifier);
                            //}
                        }
                        #endregion
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
        /// Tạo nhanh phòng ban, folder
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        /// 
        [Route("generate-space")]
        [HttpPost]
        public async Task<object> generate_space([FromBody] GenerateProjectAutoModel data)
        {
            UserJWT loginData = Ulities.GetUserByHeader(HttpContext.Request.Headers);
            if (loginData == null)
                return JsonResultCommon.DangNhap();
            try
            {
                string ConnectionString = WeworkLiteController.getConnectionString(ConnectionCache, loginData.CustomerID, _configuration);
                using (DpsConnection cnn = new DpsConnection(ConnectionString))
                {
                    #region Lấy dữ liệu account từ JeeAccount
                    DataAccount = WeworkLiteController.GetAccountFromJeeAccount(HttpContext.Request.Headers, _configuration);
                    if (DataAccount == null)
                        return JsonResultCommon.Custom("Lỗi lấy danh sách nhân viên từ hệ thống quản lý tài khoản");
                    #endregion
                    string error = "";
                    if (WeworkLiteController.init_space(cnn, loginData, data, out error))
                    {
                        string departmentid = cnn.ExecuteScalar("select IDENT_CURRENT('we_department')").ToString();
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
        /// Thêm nhanh dự án trong phòng ban
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        /// 
        [CusAuthorize(Roles = "3501")]
        [Route("Insert_Quick")]
        [HttpPost]
        public async Task<object> Insert_Quick([FromBody] ProjectTeamModel data)
        {
            string Token = Common.GetHeader(Request);
            UserJWT loginData = Ulities.GetUserByHeader(HttpContext.Request.Headers);
            if (loginData == null)
                return JsonResultCommon.DangNhap();
            try
            {
                string strRe = "";
                if (string.IsNullOrEmpty(data.title))
                    strRe += (strRe == "" ? "" : ",") + "tên dự án/phòng ban";
                if (strRe != "")
                    return JsonResultCommon.BatBuoc(strRe);
                string ConnectionString = WeworkLiteController.getConnectionString(ConnectionCache, loginData.CustomerID, _configuration);
                using (DpsConnection cnn = new DpsConnection(ConnectionString))
                {
                    #region Lấy dữ liệu account từ JeeAccount
                    DataAccount = WeworkLiteController.GetAccountFromJeeAccount(HttpContext.Request.Headers, _configuration);
                    if (DataAccount == null)
                        return JsonResultCommon.Custom("Lỗi lấy danh sách nhân viên từ hệ thống quản lý tài khoản");
                    #endregion

                    long iduser = loginData.UserID;
                    long idk = loginData.CustomerID;
                    Hashtable val = new Hashtable();
                    val.Add("title", data.title);
                    val.Add("id_department", data.id_department);
                    val.Add("loai", 1);
                    val.Add("is_project", 1);
                    val.Add("start_date", DateTime.Now);
                    val.Add("end_date", DateTime.Now.AddMonths(3));
                    val.Add("status", data.status);
                    val.Add("color", "bg6");
                    val.Add("CreatedDate", DateTime.Now);
                    val.Add("CreatedBy", iduser);
                    string strCheck = "select count(*) from we_project_team where Disabled=0 and (id_department=@id_department) and title=@name";
                    if (int.Parse(cnn.ExecuteScalar(strCheck, new SqlConditions() { { "id_department", data.id_department }, { "name", data.title } }).ToString()) > 0)
                    {
                        return JsonResultCommon.Trung("Dự án/phòng ban");
                    }
                    cnn.BeginTransaction();
                    if (cnn.Insert(val, "we_project_team") != 1)
                    {
                        cnn.RollbackTransaction();
                        return JsonResultCommon.Exception(_logger, cnn.LastError, _config, loginData, ControllerContext);
                    }
                    long idc = long.Parse(cnn.ExecuteScalar("select IDENT_CURRENT('we_project_team')").ToString());
                    DataTable dt_member = new DataTable();
                    // insert thành viên
                    string sql_insert = "";
                    sql_insert = $@"insert into we_project_team_user (id_project_team, id_user, admin, favourite, CreatedDate, CreatedBy, Disabled)
                        select " + idc + ",id_user,0,0,getdate(), " + iduser + ", Disabled from we_department_owner where Disabled = 0 and id_department = " + data.id_department + $" and id_user != {iduser}";
                    cnn.ExecuteNonQuery(sql_insert);
                    if (cnn.LastError != null)
                    {
                        cnn.RollbackTransaction();
                        return JsonResultCommon.Exception(_logger, cnn.LastError, _config, loginData, ControllerContext);
                    }
                    dt_member = cnn.CreateDataTable("select id_user from we_project_team_user where admin = 0 and id_project_team = " + idc + "");
                    // insert admin
                    Hashtable has = new Hashtable();
                    has["id_project_team"] = idc;
                    has["CreatedDate"] = DateTime.Now;
                    has["CreatedBy"] = iduser;
                    has["id_user"] = iduser;
                    has["admin"] = 1;
                    if (cnn.Insert(has, "we_project_team_user") != 1)
                    {
                        cnn.RollbackTransaction();
                        return JsonResultCommon.Exception(_logger, cnn.LastError, _config, loginData, ControllerContext);
                    }
                    var list_roles = new List<long> { 1, 11 };
                    if (!WeworkLiteController.Init_RoleDefault(idc, list_roles, cnn))
                    {
                        cnn.RollbackTransaction();
                        return JsonResultCommon.Exception(_logger, cnn.LastError, _config, loginData, ControllerContext);
                    }
                    #region Khởi tạo view mặc định
                    if (!WeworkLiteController.Init_DefaultView_Project(idc, cnn))
                    {
                        cnn.RollbackTransaction();
                        return JsonResultCommon.Exception(_logger, cnn.LastError, _config, loginData, ControllerContext);
                    }
                    #endregion
                    // Tạo status mặc định cho project này dựa vào id_department
                    long TemplateID = long.Parse(cnn.ExecuteScalar(@$"select iIf(TemplateID is not null,TemplateID,1) from we_department where id_row = (select id_department from we_project_team where id_row = {idc})").ToString());
                    if (TemplateID > 0)
                    {
                        sql_insert = "";
                        sql_insert = $@"update we_project_team set id_template = " + TemplateID + " where id_row = " + idc;
                        sql_insert += $@";insert into we_status (StatusName, description, id_project_team, CreatedDate, CreatedBy, Disabled, UpdatedDate, UpdatedBy, Type, IsDefault, color, Position, IsFinal, Follower, IsDeadline, IsToDo, StatusID_Reference)
                        select StatusName, description, " + idc + ", CreatedDate, CreatedBy, Disabled, UpdatedDate, UpdatedBy, Type, IsDefault, color, Position, IsFinal, Follower, IsDeadline, IsToDo, id_row from we_template_status where Disabled = 0 and TemplateID = " + TemplateID + "";
                        cnn.ExecuteNonQuery(sql_insert);
                        if (cnn.LastError != null)
                        {
                            cnn.RollbackTransaction();
                            return JsonResultCommon.Exception(_logger, cnn.LastError, _config, loginData, ControllerContext);
                        }
                        WeworkLiteController.update_position_status(idc, cnn);
                    }
                    #region Khởi tạo các cột hiển thị mặc định cho công việc
                    if (!WeworkLiteController.Init_Column_Project(idc, cnn))
                    {
                        cnn.RollbackTransaction();
                        return JsonResultCommon.Exception(_logger, cnn.LastError, _config, loginData, ControllerContext);
                    }
                    #endregion
                    if (!WeworkLiteController.log(_logger, loginData.Username, cnn, 31, idc, iduser, data.title))
                    {
                        cnn.RollbackTransaction();
                        return JsonResultCommon.Exception(_logger, cnn.LastError, _config, loginData, ControllerContext);
                    }
                    cnn.EndTransaction();
                    data.id_row = idc;
                    Hashtable has_replace = new Hashtable();
                    var users_admin = new List<long> { iduser };
                    WeworkLiteController.mailthongbao(idc, users_admin, 6, loginData, ConnectionString, _notifier, _configuration);//thiết lập vai trò admin
                    #region Notify thiết lập vai trò admin
                    for (int i = 0; i < users_admin.Count; i++)
                    {
                        NotifyModel notify_model = new NotifyModel();
                        has_replace = new Hashtable();
                        has_replace.Add("nguoigui", loginData.Username);
                        has_replace.Add("project_team", data.title);
                        notify_model.AppCode = "WORK";
                        notify_model.From_IDNV = loginData.UserID.ToString();
                        notify_model.To_IDNV = users_admin[i].ToString();
                        notify_model.TitleLanguageKey = LocalizationUtility.GetBackendMessage("ww_thietlapvaitroadmin", "", "vi");
                        notify_model.TitleLanguageKey = notify_model.TitleLanguageKey.Replace("$nguoigui$", loginData.customdata.personalInfo.Fullname);
                        notify_model.TitleLanguageKey = notify_model.TitleLanguageKey.Replace("$project_team$", data.title);
                        notify_model.ReplaceData = has_replace;
                        notify_model.To_Link_MobileApp = "";
                        notify_model.ComponentName = "";
                        notify_model.Component = "";
                        notify_model.To_Link_WebApp = "/project/" + data.id_row + "/settings/members";
                        try
                        {
                            if (notify_model != null)
                            {
                                Knoti = new APIModel.Models.Notify();
                                bool kq = Knoti.PushNotify(notify_model.From_IDNV, notify_model.To_IDNV, notify_model.AppCode, "ww_thietlapvaitroadmin", notify_model.ReplaceData, notify_model.To_Link_WebApp, notify_model.To_Link_MobileApp, notify_model.ComponentName, notify_model.Component);
                            }
                        }
                        catch
                        { }

                        var info = DataAccount.Where(x => notify_model.To_IDNV.ToString().Contains(x.UserId.ToString())).FirstOrDefault();
                        if (info is not null)
                        {
                            bool kq_noti = WeworkLiteController.SendNotify(loginData.Username, info.Username, notify_model, _notifier);
                        }
                    }
                    #endregion
                    foreach (DataRow item in dt_member.Rows)
                    {
                        var users_member = new List<long> { long.Parse(item["id_user"].ToString()) };
                        WeworkLiteController.mailthongbao(idc, users_member, 5, loginData, ConnectionString, _notifier, _configuration);//thêm vào dự án
                        #region Notify thiết lập vai trò member
                        for (int i = 0; i < users_member.Count; i++)
                        {
                            NotifyModel notify_model = new NotifyModel();
                            has_replace = new Hashtable();
                            has_replace.Add("nguoigui", loginData.Username);
                            has_replace.Add("project_team", data.title);
                            notify_model.AppCode = "WORK";
                            notify_model.From_IDNV = loginData.UserID.ToString();
                            notify_model.To_IDNV = users_member[i].ToString();
                            notify_model.TitleLanguageKey = LocalizationUtility.GetBackendMessage("ww_thietlapvaitrothanhvien", "", "vi");
                            notify_model.TitleLanguageKey = notify_model.TitleLanguageKey.Replace("$nguoigui$", loginData.customdata.personalInfo.Fullname);
                            notify_model.TitleLanguageKey = notify_model.TitleLanguageKey.Replace("$project_team$", data.title);
                            notify_model.ReplaceData = has_replace;
                            notify_model.To_Link_MobileApp = "";
                            notify_model.ComponentName = "";
                            notify_model.Component = "";
                            notify_model.To_Link_WebApp = "/project/" + data.id_row + "/settings/members";
                            try
                            {
                                if (notify_model != null)
                                {
                                    Knoti = new APIModel.Models.Notify();
                                    bool kq = Knoti.PushNotify(notify_model.From_IDNV, notify_model.To_IDNV, notify_model.AppCode, "ww_thietlapvaitrothanhvien", notify_model.ReplaceData, notify_model.To_Link_WebApp, notify_model.To_Link_MobileApp, notify_model.ComponentName, notify_model.Component);
                                }
                            }
                            catch
                            { }
                            var info = DataAccount.Where(x => notify_model.To_IDNV.ToString().Contains(x.UserId.ToString())).FirstOrDefault();
                            if (info is not null)
                            {
                                bool kq_noti = WeworkLiteController.SendNotify(loginData.Username, info.Username, notify_model, _notifier);
                            }
                        }
                    }
                    #endregion
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
        /// <param name="data"></param>
        /// <returns></returns>
        /// 
        [CusAuthorize(Roles = "3501")]
        [Route("Update")]
        [HttpPost]
        public async Task<object> Update([FromBody] ProjectTeamModel data)
        {
            string Token = Common.GetHeader(Request);
            UserJWT loginData = Ulities.GetUserByHeader(HttpContext.Request.Headers);
            if (loginData == null)
                return JsonResultCommon.DangNhap();
            try
            {
                string strRe = "";
                if (string.IsNullOrEmpty(data.title))
                    strRe += (strRe == "" ? "" : ",") + "tên dự án/phòng ban";
                //if (data.id_department <= 0)
                //    strRe += (strRe == "" ? "" : ",") + "ban";
                if (data.is_project && data.loai <= 0)
                    strRe += (strRe == "" ? "" : ",") + "loại dự án";
                if (data.Users == null || data.Users.Count == 0)
                    strRe += (strRe == "" ? "" : ",") + "người quản trị";
                else
                {
                    if (data.Users.Where(x => x.admin).Count() == 0)
                        strRe += (strRe == "" ? "" : ",") + "người quản trị";
                }
                if (strRe != "")
                {
                    return JsonResultCommon.BatBuoc(strRe);
                }
                string ConnectionString = WeworkLiteController.getConnectionString(ConnectionCache, loginData.CustomerID, _configuration);
                using (DpsConnection cnn = new DpsConnection(ConnectionString))
                {
                    #region Lấy dữ liệu account từ JeeAccount
                    DataAccount = WeworkLiteController.GetAccountFromJeeAccount(HttpContext.Request.Headers, _configuration);
                    if (DataAccount == null)
                        return JsonResultCommon.Custom("Lỗi lấy danh sách nhân viên từ hệ thống quản lý tài khoản");
                    #endregion
                    SqlConditions sqlcond = new SqlConditions();
                    sqlcond.Add("id_row", data.id_row);
                    sqlcond.Add("disabled", 0);
                    string s = "select * from we_project_team where (where)";
                    DataTable old = cnn.CreateDataTable(s, "(where)", sqlcond);
                    if (old == null || old.Rows.Count == 0)
                        return JsonResultCommon.KhongTonTai("Dự án/phòng ban");
                    bool email_update_team = (bool)old.Rows[0]["email_update_team"];
                    long iduser = loginData.UserID;
                    long idk = loginData.CustomerID;
                    Hashtable val = new Hashtable();
                    val.Add("title", data.title);
                    val.Add("id_department", data.id_department);
                    if (data.icon != null && !string.IsNullOrEmpty(data.icon.strBase64))
                    {
                        string x = "";
                        string folder = "/logo/";
                        if (!UploadHelper.UploadFile(data.icon.strBase64, data.icon.filename, folder, _hostingEnvironment.ContentRootPath, ref x, _configuration))
                            return JsonResultCommon.Custom(UploadHelper.error);
                        val.Add("icon", x);
                    }
                    val.Add("description", string.IsNullOrEmpty(data.description) ? "" : data.description);
                    val.Add("detail", string.IsNullOrEmpty(data.detail) ? "" : data.detail);
                    val.Add("stage_description", string.IsNullOrEmpty(data.stage_description) ? "" : data.stage_description);
                    val.Add("loai", data.loai);
                    val.Add("allow_percent_done", data.allow_percent_done);
                    val.Add("require_evaluate", data.require_evaluate);
                    if (data.require_evaluate)
                        val.Add("evaluate_by_assignner", data.evaluate_by_assignner);
                    else
                        val.Add("evaluate_by_assignner", false);
                    val.Add("allow_estimate_time", data.allow_estimate_time);
                    if (data.start_date != DateTime.MinValue)
                        val.Add("start_date", data.start_date);
                    else
                        val.Add("start_date", DBNull.Value);
                    val.Add("status", data.status);
                    if (data.end_date != DateTime.MinValue)
                    {
                        val.Add("end_date", data.end_date);
                        if (data.end_date < DateTime.Now)
                        {
                            val.Remove("status");
                            val.Add("status", 2);
                        }
                    }
                    else
                        val.Add("end_date", DBNull.Value);
                    val.Add("locked", data.locked);
                    if (!string.IsNullOrEmpty(data.color))
                        val.Add("color", data.color);
                    else
                        val.Add("color", DBNull.Value);
                    val.Add("UpdatedDate", DateTime.Now);
                    val.Add("UpdatedBy", iduser);
                    string strCheck = "select count(*) from we_project_team where Disabled=0 and (id_department=@id_department) and title=@name and id_row<>@id_row";
                    if (int.Parse(cnn.ExecuteScalar(strCheck, new SqlConditions() { { "id_department", data.id_department }, { "name", data.title }, { "id_row", data.id_row } }).ToString()) > 0)
                    {
                        return JsonResultCommon.Trung("Dự án/phòng ban");
                    }
                    cnn.BeginTransaction();
                    if (cnn.Update(val, sqlcond, "we_project_team") != 1)
                    {
                        cnn.RollbackTransaction();
                        return JsonResultCommon.Exception(_logger, cnn.LastError, _config, loginData, ControllerContext);
                    }
                    string ids = string.Join(",", data.Users.Where(x => x.id_row > 0).Select(x => x.id_row));
                    if (ids != "")//xóa owner và thành viên
                    {
                        string strDel = "Update we_project_team_user set Disabled=1, UpdatedDate=getdate(), UpdatedBy=" + iduser + " where Disabled=0 and  id_project_team=" + data.id_row + " and id_row not in (" + ids + ")";
                        if (cnn.ExecuteNonQuery(strDel) < 0)
                        {
                            cnn.RollbackTransaction();
                            return JsonResultCommon.Exception(_logger, cnn.LastError, _config, loginData, ControllerContext);
                        }
                    }
                    Hashtable val1 = new Hashtable();
                    val1["id_project_team"] = data.id_row;
                    val1["CreatedDate"] = DateTime.Now;
                    val1["CreatedBy"] = iduser;
                    foreach (var owner in data.Users)
                    {
                        if (owner.id_row == 0)
                        {
                            val1["id_user"] = owner.id_user;
                            val1["admin"] = owner.admin;
                            if (cnn.Insert(val1, "we_project_team_user") != 1)
                            {
                                cnn.RollbackTransaction();
                                return JsonResultCommon.Exception(_logger, cnn.LastError, _config, loginData, ControllerContext);
                            }
                            //NhacNho.UpdateSoluongDuan(owner.id_user, loginData.CustomerID, ConnectionString, _configuration, _producer);
                        }
                    }
                    DataTable dt = cnn.CreateDataTable(s, "(where)", sqlcond);
                    //string LogContent = "", LogEditContent = "";
                    //string LogEditContentTemp = DpsPage.GetEditLogContent(old, dt);
                    //if (!LogEditContent.Equals(""))
                    //{
                    //    LogEditContent = "Chỉnh sửa dữ liệu (" + data.id_row + ") : " + LogEditContentTemp;
                    //    LogContent = "Chỉnh sửa dữ liệu project_team (" + data.id_row + "), Chi tiết xem trong log chỉnh sửa chức năng";
                    //}
                    //DpsPage.Ghilogfile(loginData.CustomerID.ToString(), LogEditContent, LogContent, loginData.UserName);

                    if (!WeworkLiteController.log(_logger, loginData.Username, cnn, 32, data.id_row, iduser))
                    {
                        cnn.RollbackTransaction();
                        return JsonResultCommon.Exception(_logger, cnn.LastError, _config, loginData, ControllerContext);
                    }
                    cnn.EndTransaction();
                    foreach (var owner in data.Users)
                    {
                        if (owner.id_row == 0)
                        {
                            NhacNho.UpdateSoluongDuan(owner.id_user, loginData.CustomerID, ConnectionString, _configuration, _producer);
                        }
                    }
                    WeworkLiteController.mailthongbao(data.id_row, data.Users.Where(x => x.id_row == 0 && x.admin).Select(x => x.id_user).ToList(), 6, loginData, ConnectionString, _notifier, _configuration);//thiết lập vai trò admin
                    Hashtable has_replace = new Hashtable();
                    List<long> users_admin = data.Users.Where(x => x.id_row == 0 && x.admin).Select(x => x.id_user).ToList();
                    List<long> users_member = data.Users.Where(x => x.id_row == 0 && !x.admin).Select(x => x.id_user).ToList();
                    #region Notify thiết lập vai trò admin
                    for (int i = 0; i < users_admin.Count; i++)
                    {
                        NotifyModel notify_model = new NotifyModel();
                        has_replace = new Hashtable();
                        has_replace.Add("nguoigui", loginData.Username);
                        has_replace.Add("project_team", data.title);
                        notify_model.AppCode = "WORK";
                        notify_model.From_IDNV = loginData.UserID.ToString();
                        notify_model.To_IDNV = data.Users[i].id_user.ToString();
                        notify_model.TitleLanguageKey = LocalizationUtility.GetBackendMessage("ww_thietlapvaitroadmin", "", "vi");
                        notify_model.TitleLanguageKey = notify_model.TitleLanguageKey.Replace("$nguoigui$", loginData.customdata.personalInfo.Fullname);
                        notify_model.TitleLanguageKey = notify_model.TitleLanguageKey.Replace("$project_team$", data.title);
                        //notify_model.TitleLanguageKey = WeworkLiteController.getErrorMessageFromBackend("ww_thietlapvaitroadmin", "vi", ""); ;
                        notify_model.ReplaceData = has_replace;
                        notify_model.To_Link_MobileApp = "";
                        notify_model.ComponentName = "";
                        notify_model.Component = "";
                        notify_model.To_Link_WebApp = "/project/" + data.id_row + "/settings/members";
                        try
                        {
                            if (notify_model != null)
                            {
                                Knoti = new APIModel.Models.Notify();
                                bool kq = Knoti.PushNotify(notify_model.From_IDNV, notify_model.To_IDNV, notify_model.AppCode, "ww_thietlapvaitroadmin", notify_model.ReplaceData, notify_model.To_Link_WebApp, notify_model.To_Link_MobileApp, notify_model.ComponentName, notify_model.Component);
                            }
                        }
                        catch
                        { }

                        var info = DataAccount.Where(x => notify_model.To_IDNV.ToString().Contains(x.UserId.ToString())).FirstOrDefault();
                        if (info is not null)
                        {
                            bool kq_noti = WeworkLiteController.SendNotify(loginData.Username, info.Username, notify_model, _notifier);
                        }
                    }
                    #endregion
                    WeworkLiteController.mailthongbao(data.id_row, data.Users.Where(x => x.id_row == 0 && !x.admin).Select(x => x.id_user).ToList(), 5, loginData, ConnectionString, _notifier, _configuration);//thêm vào dự án
                    #region Notify thiết lập vai trò member
                    for (int i = 0; i < users_member.Count; i++)
                    {
                        NotifyModel notify_model = new NotifyModel();
                        has_replace = new Hashtable();
                        has_replace.Add("nguoigui", loginData.Username);
                        has_replace.Add("project_team", data.title);
                        notify_model.AppCode = "WORK";
                        notify_model.From_IDNV = loginData.UserID.ToString();
                        notify_model.To_IDNV = data.Users[i].id_user.ToString();
                        notify_model.TitleLanguageKey = LocalizationUtility.GetBackendMessage("ww_thietlapvaitrothanhvien", "", "vi");
                        notify_model.TitleLanguageKey = notify_model.TitleLanguageKey.Replace("$nguoigui$", loginData.customdata.personalInfo.Fullname);
                        notify_model.TitleLanguageKey = notify_model.TitleLanguageKey.Replace("$project_team$", data.title);
                        notify_model.ReplaceData = has_replace;
                        notify_model.To_Link_MobileApp = "";
                        notify_model.ComponentName = "";
                        notify_model.Component = "";
                        notify_model.To_Link_WebApp = "/project/" + data.id_row + "/settings/members";
                        try
                        {
                            if (notify_model != null)
                            {
                                Knoti = new APIModel.Models.Notify();
                                bool kq = Knoti.PushNotify(notify_model.From_IDNV, notify_model.To_IDNV, notify_model.AppCode, "ww_thietlapvaitrothanhvien", notify_model.ReplaceData, notify_model.To_Link_WebApp, notify_model.To_Link_MobileApp, notify_model.ComponentName, notify_model.Component);
                            }
                        }
                        catch
                        { }
                        var info = DataAccount.Where(x => notify_model.To_IDNV.ToString().Contains(x.UserId.ToString())).FirstOrDefault();
                        if (info is not null)
                        {
                            bool kq_noti = WeworkLiteController.SendNotify(loginData.Username, info.Username, notify_model, _notifier);
                        }
                    }
                    #endregion
                    if (email_update_team)
                    {
                        var keys = new List<string> { "title", "start_date", "end_date" };
                        var vals = WeworkLiteController.CheckKeyChange(keys, old, dt);
                        int id_template = 0;
                        if (vals[0])
                        {
                            id_template = 1;//mail báo thay đổi tên
                        }
                        if (vals[1] || vals[2])
                        {
                            id_template = 2;//mail báo thay đổi thời gian
                            if (data.start_date == DateTime.MinValue && data.end_date == DateTime.MinValue)
                                id_template = 3;//xóa thời gian thực hiện
                        }
                        if (id_template > 0)
                        {
                            WeworkLiteController.mailthongbao(data.id_row, data.Users.Where(x => x.id_row > 0).Select(x => x.id_user).ToList(), id_template, loginData, ConnectionString, _notifier, _configuration, old);
                            #region Notify thiết lập vai trò member
                            for (int i = 0; i < users_member.Count; i++)
                            {
                                NotifyModel notify_model = new NotifyModel();
                                has_replace = new Hashtable();
                                has_replace.Add("nguoigui", loginData.Username);
                                has_replace.Add("project_team", data.title);
                                notify_model.AppCode = "WORK";
                                notify_model.From_IDNV = loginData.UserID.ToString();
                                notify_model.To_IDNV = data.Users[i].id_user.ToString();
                                notify_model.TitleLanguageKey = LocalizationUtility.GetBackendMessage("ww_thietlapvaitrothanhvien", "", "vi");
                                notify_model.TitleLanguageKey = notify_model.TitleLanguageKey.Replace("$nguoigui$", loginData.customdata.personalInfo.Fullname);
                                notify_model.TitleLanguageKey = notify_model.TitleLanguageKey.Replace("$project_team$", data.title);
                                notify_model.ReplaceData = has_replace;
                                notify_model.To_Link_MobileApp = "";
                                notify_model.ComponentName = "";
                                notify_model.Component = "";
                                notify_model.To_Link_WebApp = "/project/" + data.id_row + "/settings/members";
                                try
                                {
                                    if (notify_model != null)
                                    {
                                        Knoti = new APIModel.Models.Notify();
                                        bool kq = Knoti.PushNotify(notify_model.From_IDNV, notify_model.To_IDNV, notify_model.AppCode, "ww_thietlapvaitrothanhvien", notify_model.ReplaceData, notify_model.To_Link_WebApp, notify_model.To_Link_MobileApp, notify_model.ComponentName, notify_model.Component);
                                    }
                                }
                                catch
                                { }
                                var info = DataAccount.Where(x => notify_model.To_IDNV.ToString().Contains(x.UserId.ToString())).FirstOrDefault();
                                if (info is not null)
                                {
                                    bool kq_noti = WeworkLiteController.SendNotify(loginData.Username, info.Username, notify_model, _notifier);
                                }
                            }
                            #endregion
                        }

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
        /// 
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        [CusAuthorize(Roles = "3501")]
        [Route("Update-stage")]
        [HttpPost]
        public async Task<object> Update_Stage(ProjectTeamModel data)
        {
            string Token = Common.GetHeader(Request);
            UserJWT loginData = Ulities.GetUserByHeader(HttpContext.Request.Headers);
            if (loginData == null)
                return JsonResultCommon.DangNhap();
            try
            {
                string strRe = "";
                if (data.status <= 0)
                    strRe += (strRe == "" ? "" : ",") + "trạng thái";

                if (strRe != "")
                {
                    return JsonResultCommon.BatBuoc(strRe);
                }
                string ConnectionString = WeworkLiteController.getConnectionString(ConnectionCache, loginData.CustomerID, _configuration);
                using (DpsConnection cnn = new DpsConnection(ConnectionString))
                {
                    SqlConditions sqlcond = new SqlConditions();
                    sqlcond.Add("id_row", data.id_row);
                    sqlcond.Add("disabled", 0);
                    string s = "select * from we_project_team where (where)";
                    DataTable old = cnn.CreateDataTable(s, "(where)", sqlcond);
                    if (old == null || old.Rows.Count == 0)
                        return JsonResultCommon.KhongTonTai("Dự án/phòng ban");

                    long iduser = loginData.UserID;
                    long idk = loginData.CustomerID;
                    Hashtable val = new Hashtable();
                    val.Add("stage_description", string.IsNullOrEmpty(data.stage_description) ? "" : data.stage_description);
                    val.Add("status", data.status);
                    val.Add("UpdatedDate", DateTime.Now);
                    val.Add("UpdatedBy", iduser);
                    cnn.BeginTransaction();
                    if (cnn.Update(val, sqlcond, "we_project_team") != 1)
                    {
                        cnn.RollbackTransaction();
                        return JsonResultCommon.Exception(_logger, cnn.LastError, _config, loginData, ControllerContext);
                    }

                    Hashtable val1 = new Hashtable();
                    val1.Add("id_project_team", data.id_row);
                    val1.Add("description", string.IsNullOrEmpty(data.stage_description) ? "" : data.stage_description);
                    val1.Add("stage", data.status);
                    val1["CreatedDate"] = DateTime.Now;
                    val1["CreatedBy"] = iduser;
                    if (cnn.Insert(val1, "we_project_team_stage") != 1)
                    {
                        cnn.RollbackTransaction();
                        return JsonResultCommon.Exception(_logger, cnn.LastError, _config, loginData, ControllerContext);
                    }
                    DataTable dt = cnn.CreateDataTable(s, "(where)", sqlcond);
                    //string LogContent = "", LogEditContent = "";
                    //LogEditContent = DpsPage.GetEditLogContent(old, dt);
                    //if (!LogEditContent.Equals(""))
                    //{
                    //    LogEditContent = "Chỉnh sửa dữ liệu (" + data.id_row + ") : " + LogEditContent;
                    //    LogContent = "Chỉnh sửa dữ liệu project_team (" + data.id_row + "), Chi tiết xem trong log chỉnh sửa chức năng";
                    //}
                    //DpsPage.Ghilogfile(loginData.CustomerID.ToString(), LogEditContent, LogContent, loginData.UserName);

                    if (!WeworkLiteController.log(_logger, loginData.Username, cnn, 34, data.id_row, iduser, "", old.Rows[0]["status"], data.status))
                    {
                        cnn.RollbackTransaction();
                        return JsonResultCommon.Exception(_logger, cnn.LastError, _config, loginData, ControllerContext);
                    }
                    cnn.EndTransaction();
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
        /// <param name="id"></param>
        /// <param name="key">key: period_type, color, default_view</param>
        /// <param name="value"></param>
        /// <returns></returns>
        [CusAuthorize(Roles = "3501")]
        [Route("update-by-key")]
        [HttpGet]
        public async Task<object> UpdateByKey(long id, string key, string value)
        {
            string Token = Common.GetHeader(Request);
            UserJWT loginData = Ulities.GetUserByHeader(HttpContext.Request.Headers);
            if (loginData == null)
                return JsonResultCommon.DangNhap();
            try
            {
                List<string> keys = new List<string>() { "period_type", "color", "default_view", "id_department",
                    "email_assign_work", "email_update_work", "email_update_status", "email_delete_work", "email_update_team", "email_delete_team", "email_upload_file" };
                if (string.IsNullOrEmpty(key) || !keys.Contains(key))
                    return JsonResultCommon.Custom("Thông ton cập nhật không được hổ trợ");
                string ConnectionString = WeworkLiteController.getConnectionString(ConnectionCache, loginData.CustomerID, _configuration);
                using (DpsConnection cnn = new DpsConnection(ConnectionString))
                {
                    SqlConditions sqlcond = new SqlConditions();
                    sqlcond.Add("id_row", id);
                    sqlcond.Add("disabled", 0);
                    string s = "select * from we_project_team where (where)";
                    DataTable old = cnn.CreateDataTable(s, "(where)", sqlcond);
                    if (old == null || old.Rows.Count == 0)
                        return JsonResultCommon.KhongTonTai("Dự án/phòng ban");

                    long iduser = loginData.UserID;
                    long idk = loginData.CustomerID;
                    Hashtable val = new Hashtable();
                    val.Add(key, value);
                    val.Add("UpdatedDate", DateTime.Now);
                    val.Add("UpdatedBy", iduser);
                    cnn.BeginTransaction();
                    if (cnn.Update(val, sqlcond, "we_project_team") != 1)
                    {
                        cnn.RollbackTransaction();
                        return JsonResultCommon.Exception(_logger, cnn.LastError, _config, loginData, ControllerContext);
                    }
                    DataTable dt = cnn.CreateDataTable(s, "(where)", sqlcond);
                    //string LogContent = "", LogEditContent = "";
                    //LogEditContent = DpsPage.GetEditLogContent(old, dt);
                    //if (!LogEditContent.Equals(""))
                    //{
                    //    LogEditContent = "Chỉnh sửa dữ liệu (" + id + ") : " + LogEditContent;
                    //    LogContent = "Chỉnh sửa dữ liệu project_team (" + id + "), Chi tiết xem trong log chỉnh sửa chức năng";
                    //}
                    //DpsPage.Ghilogfile(loginData.CustomerID.ToString(), LogEditContent, LogContent, loginData.UserName);

                    if (!WeworkLiteController.log(_logger, loginData.Username, cnn, 32, id, iduser, ""))
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

        /// <summary>
        /// 
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        [CusAuthorize(Roles = "3501")]
        [Route("Close")]
        [HttpPost]
        public async Task<object> Close([FromBody] ProjectTeamCloseModel data)
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
                    string s = "select * from we_project_team where (where)";
                    DataTable old = cnn.CreateDataTable(s, "(where)", sqlcond);
                    if (old == null || old.Rows.Count == 0)
                        return JsonResultCommon.KhongTonTai("Dự án/phòng ban");

                    long iduser = loginData.UserID;
                    long idk = loginData.CustomerID;
                    Hashtable val = new Hashtable();
                    val.Add("locked", 1);
                    val.Add("status", data.close_status);
                    val.Add("stop_reminder", data.stop_reminder);
                    val.Add("note", string.IsNullOrEmpty(data.note) ? "" : data.note);
                    val.Add("UpdatedDate", DateTime.Now);
                    val.Add("UpdatedBy", iduser);
                    cnn.BeginTransaction();
                    if (cnn.Update(val, sqlcond, "we_project_team") != 1)
                    {
                        cnn.RollbackTransaction();
                        return JsonResultCommon.Exception(_logger, cnn.LastError, _config, loginData, ControllerContext);
                    }
                    DataTable dt = cnn.CreateDataTable(s, "(where)", sqlcond);
                    //string LogContent = "", LogEditContent = "";
                    //LogEditContent = DpsPage.GetEditLogContent(old, dt);
                    //if (!LogEditContent.Equals(""))
                    //{
                    //    LogEditContent = "Chỉnh sửa dữ liệu (" + data.id_row + ") : " + LogEditContent;
                    //    LogContent = "Chỉnh sửa dữ liệu project_team (" + data.id_row + "), Chi tiết xem trong log chỉnh sửa chức năng";
                    //}
                    //DpsPage.Ghilogfile(loginData.CustomerID.ToString(), LogEditContent, LogContent, loginData.UserName);

                    if (!WeworkLiteController.log(_logger, loginData.Username, cnn, 34, data.id_row, iduser, "", old.Rows[0]["status"], data.close_status))
                    {
                        cnn.RollbackTransaction();
                        return JsonResultCommon.Exception(_logger, cnn.LastError, _config, loginData, ControllerContext);
                    }
                    cnn.EndTransaction();
                    return JsonResultCommon.ThanhCong(data);
                }
            }
            catch (Exception ex)
            {
                return JsonResultCommon.Exception(_logger, ex, _config, loginData);
            }
        }
        [CusAuthorize(Roles = "3501")]
        [Route("Open")]
        [HttpPost]
        public async Task<object> Open([FromBody] ProjectTeamCloseModel data)
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
                    string s = "select * from we_project_team where (where)";
                    DataTable old = cnn.CreateDataTable(s, "(where)", sqlcond);
                    if (old == null || old.Rows.Count == 0)
                        return JsonResultCommon.KhongTonTai("Dự án/phòng ban");

                    long iduser = loginData.UserID;
                    long idk = loginData.CustomerID;
                    Hashtable val = new Hashtable();
                    val.Add("locked", 0);
                    val.Add("status", data.close_status);
                    val.Add("stop_reminder", data.stop_reminder);
                    val.Add("note", string.IsNullOrEmpty(data.note) ? "" : data.note);
                    val.Add("UpdatedDate", DateTime.Now);
                    val.Add("UpdatedBy", iduser);
                    cnn.BeginTransaction();
                    if (cnn.Update(val, sqlcond, "we_project_team") != 1)
                    {
                        cnn.RollbackTransaction();
                        return JsonResultCommon.Exception(_logger, cnn.LastError, _config, loginData, ControllerContext);
                    }
                    DataTable dt = cnn.CreateDataTable(s, "(where)", sqlcond);

                    if (!WeworkLiteController.log(_logger, loginData.Username, cnn, 34, data.id_row, iduser, "", old.Rows[0]["status"], data.close_status))
                    {
                        cnn.RollbackTransaction();
                        return JsonResultCommon.Exception(_logger, cnn.LastError, _config, loginData, ControllerContext);
                    }
                    cnn.EndTransaction();
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
        /// <param name="id"></param>
        /// <returns></returns>
        [CusAuthorize(Roles = "3501")]
        [Route("Delete")]
        [HttpGet]
        public BaseModel<object> Delete(long id)
        {
            string Token = Common.GetToken(HttpContext.Request.Headers);
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
                    string sqlq = "select id_row,email_delete_team  from we_project_team where Disabled=0 and  id_row = " + id;
                    DataTable dt = cnn.CreateDataTable(sqlq);
                    if (cnn.LastError != null || dt == null || dt.Rows.Count == 0)
                        return JsonResultCommon.KhongTonTai("Dự án/phòng ban");
                    bool email_delete_team = (bool)dt.Rows[0]["email_delete_team"];
                    sqlq = "update we_project_team set Disabled=1, UpdatedDate=getdate(), UpdatedBy=" + iduser + " where id_row = " + id;
                    cnn.BeginTransaction();
                    if (cnn.ExecuteNonQuery(sqlq) != 1)
                    {
                        cnn.RollbackTransaction();
                        return JsonResultCommon.Exception(_logger, cnn.LastError, _config, loginData, ControllerContext);
                    }
                    //string LogContent = "Xóa dữ liệu project_team (" + id + ")";
                    //DpsPage.Ghilogfile(loginData.CustomerID.ToString(), LogContent, LogContent, loginData.UserName);
                    if (!WeworkLiteController.log(_logger, loginData.Username, cnn, 33, id, iduser))
                    {
                        cnn.RollbackTransaction();
                        return JsonResultCommon.Exception(_logger, cnn.LastError, _config, loginData, ControllerContext);
                    }
                    cnn.EndTransaction();
                    if (email_delete_team)
                    {
                        #region Lấy dữ liệu account từ JeeAccount
                        DataAccount = WeworkLiteController.GetAccountFromJeeAccount(HttpContext.Request.Headers, _configuration);
                        if (DataAccount == null)
                            return JsonResultCommon.Custom("Lỗi lấy danh sách nhân viên từ hệ thống quản lý tài khoản");
                        #endregion
                        mailthongbao(id, 4, loginData, Token, cnn, DataAccount);
                    }
                    return JsonResultCommon.ThanhCong();
                }
            }
            catch (Exception ex)
            {
                return JsonResultCommon.Exception(_logger, ex, _config, loginData);
            }
        }

        /// <summary>
        /// Đổi loại project hoặc team
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [CusAuthorize(Roles = "3501")]
        [Route("Change-type")]
        [HttpGet]
        public BaseModel<object> ChangeType(long id)
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
                    string sqlq = "select is_project from we_project_team where Disabled=0 and  id_row = " + id;
                    var temp = cnn.ExecuteScalar(sqlq);
                    if (temp == null)
                        return JsonResultCommon.KhongTonTai("Dự án/phòng ban");
                    bool value = !(bool)temp;
                    sqlq = "update we_project_team set is_project=" + (value ? "1" : "0") + ", UpdatedDate=getdate(), UpdatedBy=" + iduser + " where id_row = " + id;
                    cnn.BeginTransaction();
                    if (cnn.ExecuteNonQuery(sqlq) != 1)
                    {
                        cnn.RollbackTransaction();
                        return JsonResultCommon.Exception(_logger, cnn.LastError, _config, loginData, ControllerContext);
                    }
                    //string LogContent = "Chỉnh sửa dữ liệu project_team (" + id + "):is_project=" + value;
                    //DpsPage.Ghilogfile(loginData.CustomerID.ToString(), LogContent, LogContent, loginData.UserName);

                    if (!WeworkLiteController.log(_logger, loginData.Username, cnn, 35, id, iduser, "", !value, value))
                    {
                        cnn.RollbackTransaction();
                        return JsonResultCommon.Exception(_logger, cnn.LastError, _config, loginData, ControllerContext);
                    }
                    cnn.EndTransaction();
                    return JsonResultCommon.ThanhCong(value);
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
        /// <param name="data"></param>
        /// <returns></returns>
        [CusAuthorize(Roles = "3501")]
        [Route("Duplicate")]
        [HttpPost]
        public async Task<object> Duplicate([FromBody] ProjectTeamDuplicateModel data)
        {
            string Token = Common.GetHeader(Request);
            UserJWT loginData = Ulities.GetUserByHeader(HttpContext.Request.Headers);
            if (loginData == null)
                return JsonResultCommon.DangNhap();
            try
            {
                string strRe = "";
                if (string.IsNullOrEmpty(data.title))
                    strRe += (strRe == "" ? "" : ",") + "tên dự án/phòng ban mới";
                if (data.id <= 0)
                    strRe += (strRe == "" ? "" : ",") + "dự án/phòng ban gốc";
                if (data.type < 0)
                    strRe += (strRe == "" ? "" : ",") + "loại nhân bản";

                if (strRe != "")
                    return JsonResultCommon.BatBuoc(strRe);
                string ConnectionString = WeworkLiteController.getConnectionString(ConnectionCache, loginData.CustomerID, _configuration);
                using (DpsConnection cnn = new DpsConnection(ConnectionString))
                {
                    long iduser = loginData.UserID;
                    long idk = loginData.CustomerID;
                    Hashtable val = new Hashtable();
                    val.Add("id", data.id);
                    val.Add("title", data.title);
                    val.Add("type", data.type);
                    val.Add("keep_creater", data.keep_creater);
                    val.Add("keep_checker", data.keep_checker);
                    val.Add("keep_follower", data.keep_follower);
                    val.Add("keep_deadline", data.keep_deadline);
                    val.Add("hour_adjusted", data.hour_adjusted);
                    val.Add("keep_checklist", data.keep_checklist);
                    val.Add("keep_child", data.keep_child);
                    val.Add("keep_tag", data.keep_tag);
                    val.Add("keep_milestone", data.keep_milestone);
                    val.Add("keep_admin", data.keep_admin);
                    val.Add("keep_member", data.keep_member);
                    val.Add("keep_role", data.keep_role);
                    val.Add("CreatedDate", DateTime.Now);
                    val.Add("CreatedBy", iduser);
                    cnn.BeginTransaction();
                    if (cnn.Insert(val, "we_project_team_dupplicate") != 1)
                    {
                        cnn.RollbackTransaction();
                        return JsonResultCommon.Exception(_logger, cnn.LastError, _config, loginData, ControllerContext);
                    }
                    //string LogContent = "", LogEditContent = "";
                    //LogContent = LogEditContent = "Thêm mới dữ liệu project_team_duplicate: title=" + data.title + ", id=" + data.id + ", type=" + data.type;
                    //DpsPage.Ghilogfile(loginData.CustomerID.ToString(), LogEditContent, LogContent, loginData.UserName);
                    long idc = long.Parse(cnn.ExecuteScalar("select IDENT_CURRENT('we_project_team_dupplicate')").ToString());

                    string sql = "exec DuplicateProjectTeam " + idc;
                    DataTable dt = cnn.CreateDataTable(sql);
                    if (cnn.LastError != null || dt == null || dt.Rows.Count == 0)
                    {
                        cnn.RollbackTransaction();
                        return JsonResultCommon.Exception(_logger, cnn.LastError, _config, loginData, ControllerContext);
                    }
                    #region
                    DataTable dt_liststt = cnn.CreateDataTable("select * from we_status where id_project_team = " + data.id + " and Disabled = 0");
                    foreach (DataRow item in dt_liststt.Rows)
                    {
                        Hashtable val1 = new Hashtable();
                        val1.Add("StatusName", item["StatusName"]);
                        val1.Add("description", item["description"]);
                        val1.Add("id_project_team", dt.Rows[0]["id_row"]);
                        val1.Add("Type", item["Type"]);
                        val1.Add("IsDefault", item["IsDefault"]);
                        val1.Add("color", item["color"]);
                        val1.Add("Position", item["Position"]);
                        val1.Add("IsFinal", item["IsFinal"]);
                        val1.Add("IsDeadline", item["IsDeadline"]);
                        val1.Add("IsToDo", item["IsToDo"]);
                        val1.Add("CreatedDate", DateTime.Now);
                        val1.Add("CreatedBy", iduser);
                        cnn.BeginTransaction();
                        if (cnn.Insert(val1, "we_status") != 1)
                        {
                            cnn.RollbackTransaction();
                            return JsonResultCommon.Exception(_logger, cnn.LastError, _config, loginData, ControllerContext);
                        }
                    }
                    WeworkLiteController.update_position_status(long.Parse(dt.Rows[0]["id_row"].ToString()), cnn);
                    #endregion
                    if (!WeworkLiteController.log(_logger, loginData.Username, cnn, 31, idc, iduser))
                    {
                        cnn.RollbackTransaction();
                        return JsonResultCommon.Exception(_logger, cnn.LastError, _config, loginData, ControllerContext);
                    }
                    cnn.EndTransaction();
                    return JsonResultCommon.ThanhCong(dt.AsEnumerable().FirstOrDefault());
                }
            }
            catch (Exception ex)
            {
                return JsonResultCommon.Exception(_logger, ex, _config, loginData);
            }
        }

        #region quản lý thành viên

        /// <summary>
        /// 
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        //[CusAuthorize(Roles = "3700")]
        [Route("List-user")]
        [HttpGet]
        public BaseModel<object> ListUser(long id)
        {
            string Token = Common.GetHeader(Request);
            UserJWT loginData = Ulities.GetUserByHeader(HttpContext.Request.Headers);
            if (loginData == null)
                return JsonResultCommon.DangNhap();
            try
            {
                bool Visible = true;
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

                    string sqlq = "select ISNULL((select count(*) from we_project_team where Disabled=0 and  id_row = " + id + "),0)";
                    if (long.Parse(cnn.ExecuteScalar(sqlq).ToString()) != 1)
                        return JsonResultCommon.KhongTonTai("Dự án/phòng ban");
                    sqlq = @$"select u.* , '' as hoten,'' as username, '' as tenchucdanh,'' as mobile,'' as image from we_project_team_user u 
join we_project_team p on p.id_row=u.id_project_team and p.id_row=" + id + " where u.disabled=0 and u.Id_user in (" + listID + " )";

                    DataTable dt = cnn.CreateDataTable(sqlq);
                    #region Map info account từ JeeAccount
                    //List<long> users_admin = data.Users.Where(x => x.id_row == 0 && x.admin).Select(x => x.id_user).ToList();
                    //if (string.IsNullOrEmpty(query.filter["NguoiTao"]))
                    //    temp = temp.Where(x => x["NguoiTao"].ToString().Contains(query.filter["NguoiTao"]));
                    foreach (DataRow item in dt.Rows)
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
                    var data = from u in dt.AsEnumerable()
                               select new
                               {
                                   id_row = u["id_row"],
                                   admin = u["admin"],
                                   id_nv = u["id_user"],
                                   hoten = u["hoten"],
                                   username = u["username"],
                                   tenchucdanh = u["tenchucdanh"],
                                   mobile = u["mobile"],
                                   //email = u["email"],
                                   image = u["image"],
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
        /// <param name="data"></param>
        /// <returns></returns>
        [Route("Add-user")]
        [HttpPost]
        public async Task<object> AddUser(AddUserToProjectTeamModel data)
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
                #endregion
                var datas = data.Users;
                string strRe = "";
                if (datas == null || datas.Count == 0)
                    strRe += (strRe == "" ? "" : ",") + "quản lý/thành viên";
                if (strRe != "")
                    return JsonResultCommon.BatBuoc(strRe);

                string ConnectionString = WeworkLiteController.getConnectionString(ConnectionCache, loginData.CustomerID, _configuration);
                using (DpsConnection cnn = new DpsConnection(ConnectionString))
                {
                    string sqlq = "select * from we_project_team where Disabled=0 and  id_row = " + data.id_row;
                    DataTable dtF = cnn.CreateDataTable(sqlq);
                    if (dtF.Rows.Count == 0)
                        return JsonResultCommon.KhongTonTai("Dự án/phòng ban");

                    sqlq = @"select * from we_project_team_user u where u.id_project_team =" + data.id_row + " and u.disabled=0";
                    DataTable dt = cnn.CreateDataTable(sqlq);

                    long iduser = loginData.UserID;
                    long idk = loginData.CustomerID;
                    cnn.BeginTransaction();
                    foreach (var owner in datas)
                    {
                        int re = 0;
                        Hashtable val1 = new Hashtable();
                        val1["admin"] = owner.admin;
                        var find = dt.AsEnumerable().Where(x => x["id_user"].ToString() == owner.id_user.ToString()).FirstOrDefault();
                        if (find != null)
                        {
                            if ((bool)find["admin"] != owner.admin)
                            {
                                val1["UpdatedDate"] = DateTime.Now;
                                val1["UpdatedBy"] = iduser;
                                re = cnn.Update(val1, new SqlConditions() { { "id_row", find["id_row"] } }, "we_project_team_user");
                            }
                            else
                            {
                                owner.id_row = long.Parse(find["id_row"].ToString());
                                re = 1;
                            }
                        }
                        else
                        {
                            val1["id_project_team"] = data.id_row;
                            val1["id_user"] = owner.id_user;
                            val1["CreatedDate"] = DateTime.Now;
                            val1["CreatedBy"] = iduser;
                            re = cnn.Insert(val1, "we_project_team_user");
                        }
                        if (re != 1)
                        {
                            cnn.RollbackTransaction();
                            return JsonResultCommon.Exception(_logger, cnn.LastError, _config, loginData, ControllerContext);
                        }
                        if (!WeworkLiteController.log(_logger, loginData.Username, cnn, 36, data.id_row, iduser, null, owner.id_user))
                        {
                            cnn.RollbackTransaction();
                            return JsonResultCommon.Exception(_logger, cnn.LastError, _config, loginData, ControllerContext);
                        }
                    }
                    //check phải người quản lý
                    sqlq = "select ISNULL((select count(*) from we_project_team_user where disabled=0 and admin=1 and id_project_team = " + data.id_row + "),0)";
                    if (long.Parse(cnn.ExecuteScalar(sqlq).ToString()) < 1)
                    {
                        cnn.RollbackTransaction();
                        return JsonResultCommon.Custom("Dự án/phòng ban phải có ít nhất một người quản lý");
                    }
                    cnn.EndTransaction();
                    foreach (var owner in datas)
                    {
                        NhacNho.UpdateSoluongDuan(owner.id_user, loginData.CustomerID, ConnectionString, _configuration, _producer);
                    }
                    Hashtable has_replace = new Hashtable();
                    List<long> users_admin = data.Users.Where(x => x.id_row == 0 && x.admin).Select(x => x.id_user).ToList();
                    List<long> users_member = data.Users.Where(x => x.id_row == 0 && !x.admin).Select(x => x.id_user).ToList();
                    #region Notify thiết lập vai trò admin
                    if (users_admin.Count > 0)
                    {
                        WeworkLiteController.mailthongbao(data.id_row, users_admin, 6, loginData, ConnectionString, _notifier, _configuration);//thiết lập vai trò admin
                        for (int i = 0; i < users_admin.Count; i++)
                        {
                            NotifyModel notify_model = new NotifyModel();
                            has_replace = new Hashtable();
                            has_replace.Add("nguoigui", loginData.Username);
                            has_replace.Add("project_team", dtF.Rows[0]["title"]);
                            notify_model.AppCode = "WORK";
                            notify_model.From_IDNV = loginData.UserID.ToString();
                            notify_model.To_IDNV = data.Users[i].id_user.ToString();
                            notify_model.TitleLanguageKey = LocalizationUtility.GetBackendMessage("ww_thietlapvaitroadmin", "", "vi");
                            notify_model.TitleLanguageKey = notify_model.TitleLanguageKey.Replace("$nguoigui$", loginData.customdata.personalInfo.Fullname);
                            notify_model.TitleLanguageKey = notify_model.TitleLanguageKey.Replace("$project_team$", dtF.Rows[0]["title"].ToString());
                            notify_model.ReplaceData = has_replace;
                            notify_model.To_Link_MobileApp = "";
                            notify_model.ComponentName = "";
                            notify_model.Component = "";
                            notify_model.To_Link_WebApp = "/project/" + data.id_row + "/settings/members";
                            try
                            {
                                if (notify_model != null)
                                {
                                    Knoti = new APIModel.Models.Notify();
                                    bool kq = Knoti.PushNotify(notify_model.From_IDNV, notify_model.To_IDNV, notify_model.AppCode, "ww_thietlapvaitroadmin", notify_model.ReplaceData, notify_model.To_Link_WebApp, notify_model.To_Link_MobileApp, notify_model.ComponentName, notify_model.Component);
                                }
                            }
                            catch
                            { }

                            var info = DataAccount.Where(x => notify_model.To_IDNV.ToString().Contains(x.UserId.ToString())).FirstOrDefault();
                            if (info is not null)
                            {
                                bool kq_noti = WeworkLiteController.SendNotify(loginData.Username, info.Username, notify_model, _notifier);
                            }
                        }
                    }
                    #endregion
                    #region Notify thiết lập vai trò member
                    if (users_member.Count > 0)
                    {
                        WeworkLiteController.mailthongbao(data.id_row, users_member, 5, loginData, ConnectionString, _notifier, _configuration);//thêm vào dự án
                        for (int i = 0; i < users_member.Count; i++)
                        {
                            NotifyModel notify_model = new NotifyModel();
                            has_replace = new Hashtable();
                            has_replace.Add("nguoigui", loginData.Username);
                            has_replace.Add("project_team", dtF.Rows[0]["title"]);
                            notify_model.AppCode = "WORK";
                            notify_model.From_IDNV = loginData.UserID.ToString();
                            notify_model.To_IDNV = data.Users[i].id_user.ToString();
                            notify_model.TitleLanguageKey = LocalizationUtility.GetBackendMessage("ww_thietlapvaitrothanhvien", "", "vi");
                            notify_model.TitleLanguageKey = notify_model.TitleLanguageKey.Replace("$nguoigui$", loginData.customdata.personalInfo.Fullname);
                            notify_model.TitleLanguageKey = notify_model.TitleLanguageKey.Replace("$project_team$", dtF.Rows[0]["title"].ToString());
                            notify_model.ReplaceData = has_replace;
                            notify_model.To_Link_MobileApp = "";
                            notify_model.ComponentName = "";
                            notify_model.Component = "";
                            notify_model.To_Link_WebApp = "/project/" + data.id_row + "/settings/members";
                            try
                            {
                                if (notify_model != null)
                                {
                                    Knoti = new APIModel.Models.Notify();
                                    bool kq = Knoti.PushNotify(notify_model.From_IDNV, notify_model.To_IDNV, notify_model.AppCode, "ww_thietlapvaitroadmin", notify_model.ReplaceData, notify_model.To_Link_WebApp, notify_model.To_Link_MobileApp, notify_model.ComponentName, notify_model.Component);
                                }
                            }
                            catch
                            { }
                            var info = DataAccount.Where(x => notify_model.To_IDNV.ToString().Contains(x.UserId.ToString())).FirstOrDefault();
                            if (info is not null)
                            {
                                bool kq_noti = WeworkLiteController.SendNotify(loginData.Username, info.Username, notify_model, _notifier);
                            }
                        }
                    }
                    #endregion
                    return JsonResultCommon.ThanhCong();
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
        /// <param name="id"></param>
        /// <returns></returns>
        [Route("Delete-user")]
        [HttpGet]
        public BaseModel<object> DeleteUser(long id)
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
                    string sqlq = "select id_project_team from we_project_team_user where disabled=0 and id_row = " + id + "";
                    var temp = cnn.ExecuteScalar(sqlq);
                    if (temp == null)
                        return JsonResultCommon.Custom("Người dùng không thuộc dự án/phòng ban");
                    cnn.BeginTransaction();
                    string sql = "Update we_project_team_user set Disabled=1, UpdatedDate=getdate(), UpdatedBy=" + loginData.UserID + " where id_row=" + id;
                    int re = cnn.ExecuteNonQuery(sql);
                    if (re <= 0)
                    {
                        cnn.RollbackTransaction();
                        return JsonResultCommon.Exception(_logger, cnn.LastError, _config, loginData, ControllerContext);
                    }
                    //check phải người quản lý
                    sqlq = "select ISNULL((select count(*) from we_project_team_user where disabled=0 and admin=1 and id_project_team = " + temp + "),0)";
                    if (long.Parse(cnn.ExecuteScalar(sqlq).ToString()) < 1)
                    {
                        cnn.RollbackTransaction();
                        return JsonResultCommon.Custom("Dự án/phòng ban phải có ít nhất một người quản lý");
                    }
                    cnn.EndTransaction();

                    NhacNho.UpdateSoluongDuan(id, loginData.CustomerID, ConnectionString, _configuration, _producer);
                    return JsonResultCommon.ThanhCong();
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
        /// <param name="id"></param>
        /// <returns></returns>
        [Route("Update-user")]
        [HttpGet]
        public BaseModel<object> UpdateUser(long id, bool admin)
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
                    #region Lấy dữ liệu account từ JeeAccount
                    DataAccount = WeworkLiteController.GetAccountFromJeeAccount(HttpContext.Request.Headers, _configuration);
                    if (DataAccount == null)
                        return JsonResultCommon.Custom("Lỗi lấy danh sách nhân viên từ hệ thống quản lý tài khoản");
                    #endregion
                    string sqlq = "select id_project_team, id_user from we_project_team_user where disabled=0 and id_row = " + id + "";
                    DataTable dt = cnn.CreateDataTable(sqlq);
                    if (cnn.LastError != null || dt == null || dt.Rows.Count == 0)
                        return JsonResultCommon.Custom("Người dùng không thuộc dự án/phòng ban");
                    var id_project_team = long.Parse(dt.Rows[0]["id_project_team"].ToString());
                    cnn.BeginTransaction();
                    string sql = "Update we_project_team_user set admin=" + (admin ? 1 : 0) + ", UpdatedDate=getdate(), UpdatedBy=" + loginData.UserID + " where id_row=" + id;
                    int re = cnn.ExecuteNonQuery(sql);
                    if (re <= 0)
                    {
                        cnn.RollbackTransaction();
                        return JsonResultCommon.Exception(_logger, cnn.LastError, _config, loginData, ControllerContext);
                    }
                    //check phải người quản lý
                    sqlq = "select ISNULL((select count(*) from we_project_team_user where disabled=0 and admin=1 and id_project_team = " + id_project_team + "),0)";
                    if (long.Parse(cnn.ExecuteScalar(sqlq).ToString()) < 1)
                    {
                        cnn.RollbackTransaction();
                        return JsonResultCommon.Custom("Dự án/phòng ban phải có ít nhất một người quản lý");
                    }
                    if (!WeworkLiteController.log(_logger, loginData.Username, cnn, 37, id, loginData.UserID, "", !admin, admin))
                    {
                        cnn.RollbackTransaction();
                        return JsonResultCommon.Exception(_logger, cnn.LastError, _config, loginData, ControllerContext);
                    }
                    cnn.EndTransaction();
                    var users = new List<long> { long.Parse(dt.Rows[0]["id_user"].ToString()) };
                    int id_template = 0;
                    if (admin)
                        id_template = 6;//thiết lập vai trò admin
                    else
                        id_template = 7;//Thay đổi vai trò từ admin thành member
                    WeworkLiteController.mailthongbao(id_project_team, users, id_template, loginData, ConnectionString, _notifier, _configuration);
                    #region Notify thiết lập vai trò member
                    object projectname = cnn.ExecuteScalar("select title from we_project_team where Disabled = 0 and id_row = @id_row", new SqlConditions() { { "id_row", id_project_team } });
                    if (projectname != null)
                        projectname = projectname.ToString();
                    Hashtable has_replace = new Hashtable();
                    for (int i = 0; i < users.Count; i++)
                    {
                        NotifyModel notify_model = new NotifyModel();
                        has_replace = new Hashtable();
                        has_replace.Add("nguoigui", loginData.Username);
                        has_replace.Add("project_team", projectname);
                        notify_model.AppCode = "WORK";
                        notify_model.From_IDNV = loginData.UserID.ToString();
                        notify_model.To_IDNV = users[i].ToString();
                        notify_model.TitleLanguageKey = LocalizationUtility.GetBackendMessage("ww_thietlapvaitrothanhvien", "", "vi");
                        notify_model.TitleLanguageKey = notify_model.TitleLanguageKey.Replace("$nguoigui$", loginData.customdata.personalInfo.Fullname);
                        notify_model.TitleLanguageKey = notify_model.TitleLanguageKey.Replace("$project_team$", projectname.ToString());
                        notify_model.ReplaceData = has_replace;
                        notify_model.To_Link_MobileApp = "";
                        notify_model.ComponentName = "";
                        notify_model.Component = "";
                        notify_model.To_Link_WebApp = "/project/" + id + "/settings/members";
                        try
                        {
                            if (notify_model != null)
                            {
                                Knoti = new APIModel.Models.Notify();
                                bool kq = Knoti.PushNotify(notify_model.From_IDNV, notify_model.To_IDNV, notify_model.AppCode, "ww_thietlapvaitrothanhvien", notify_model.ReplaceData, notify_model.To_Link_WebApp, notify_model.To_Link_MobileApp, notify_model.ComponentName, notify_model.Component);
                            }
                        }
                        catch
                        { }

                        var info = DataAccount.Where(x => notify_model.To_IDNV.ToString().Contains(x.UserId.ToString())).FirstOrDefault();
                        if (info is not null)
                        {
                            bool kq_noti = WeworkLiteController.SendNotify(loginData.Username, info.Username, notify_model, _notifier);
                        }
                    }
                    #endregion
                    return JsonResultCommon.ThanhCong();
                }
            }
            catch (Exception ex)
            {
                return JsonResultCommon.Exception(_logger, ex, _config, loginData);
            }
        }
        #endregion

        #region config email
        /// <summary>
        /// 
        /// </summary>
        /// <param name="id"></param>
        /// <param name="key">key: email_assign_work, email_update_work, email_update_status, email_delete_work, email_update_team, email_delete_team, email_upload_file</param>
        /// <returns></returns>
        [Route("get-config-email")]
        [HttpGet]
        public BaseModel<object> GetConfigEmail(long id)
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
                    string sqlq = "select * from we_project_team where disabled=0 and id_row = " + id + "";
                    DataTable dt = cnn.CreateDataTable(sqlq);
                    if (cnn.LastError != null || dt == null || dt.Rows.Count == 0)
                        return JsonResultCommon.KhongTonTai("Dự án/phòng ban");
                    var data = (from r in dt.AsEnumerable()
                                select new
                                {
                                    id_row = id,
                                    is_project = r["is_project"],
                                    email_assign_work = r["email_assign_work"],
                                    email_update_work = r["email_update_work"],
                                    email_update_status = r["email_update_status"],
                                    email_delete_work = r["email_delete_work"],
                                    email_update_team = r["email_update_team"],
                                    email_delete_team = r["email_delete_team"],
                                    email_upload_file = r["email_upload_file"]
                                }).FirstOrDefault();
                    return JsonResultCommon.ThanhCong(data);
                }
            }
            catch (Exception ex)
            {
                return JsonResultCommon.Exception(_logger, ex, _config, loginData);
            }
        }
        #endregion

        #region phân quyền
        /// <summary>
        /// DS phân quyền theo id_project_team
        /// </summary>
        /// <returns></returns>
        [Route("list-role")]
        [HttpGet]
        public object ListRole(long id_project_team)
        {
            string Token = Common.GetHeader(Request);
            UserJWT loginData = Ulities.GetUserByHeader(HttpContext.Request.Headers);
            if (loginData == null)
                return JsonResultCommon.DangNhap();
            try
            {

                if (id_project_team <= 0)
                    return new List<string>();
                string ConnectionString = WeworkLiteController.getConnectionString(ConnectionCache, loginData.CustomerID, _configuration);
                using (DpsConnection cnn = new DpsConnection(ConnectionString))
                {
                    string sql = "select * from we_role r left join we_project_role role on r.id_row=role.id_role and id_project_team=" + id_project_team + " where r.disabled = 0 order by stt";
                    DataTable dt = cnn.CreateDataTable(sql);
                    if (cnn.LastError != null || dt == null)
                        return JsonResultCommon.Exception(_logger, cnn.LastError, _config, loginData, ControllerContext);
                    var data = from dr in dt.AsEnumerable()
                               group dr by dr["group"] into g
                               select new
                               {
                                   nhom = g.Key,
                                   roles = from r in g
                                           select new
                                           {
                                               id_row = r["id_row"],
                                               title = r["title"],
                                               description = r["description"],
                                               admin = true,
                                               member = r["member"],
                                               customer = r["customer"],
                                               is_assign = r["is_assign"]
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
        /// 
        /// </summary>
        /// <param name="id"></param>
        /// <param name="role"></param>
        /// <param name="key">key: admin, member, customer</param>
        /// <returns></returns>
        [Route("Update-role")]
        [HttpGet]
        public BaseModel<object> UpdateRole(long id, int role, string key)
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
                    string sqlq = "select role.id_row," + key + " from we_role r left join we_project_role role on r.id_row=role.id_role and id_project_team = " + id + " and id_role=" + role + " order by id_row desc";
                    DataTable dt = cnn.CreateDataTable(sqlq);
                    if (cnn.LastError != null || dt == null)
                        return JsonResultCommon.Exception(_logger, cnn.LastError, _config, loginData, ControllerContext);
                    if (dt.Rows.Count == 0)
                        return JsonResultCommon.Custom("Quyền không tồn tại hoặc đối tượng phân quyền không đúng");
                    cnn.BeginTransaction();
                    int re = 0;
                    if (dt.Rows[0][0] == DBNull.Value)//id_row==null
                    {
                        Hashtable val = new Hashtable();
                        val["id_project_team"] = id;
                        val["id_role"] = role;
                        val[key] = 1;
                        re = cnn.Insert(val, "we_project_role");
                    }
                    else
                    {
                        bool value = true;
                        if (dt.Rows[0][1] != DBNull.Value)
                            value = !(bool)dt.Rows[0][1];
                        string sql = "Update we_project_role set " + key + "=" + (value ? 1 : 0) + ", UpdatedDate=getdate(), UpdatedBy=" + loginData.UserID + " where id_row=" + dt.Rows[0][0];
                        re = cnn.ExecuteNonQuery(sql);
                    }
                    if (re <= 0)
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
        #endregion

        #region hoạt động

        /// <summary>
        /// ds project/team theo department
        /// </summary>
        /// <param name="query"></param>
        /// <returns></returns>
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
                string domain = _configuration.GetValue<string>("Host:JeeWork_API") + "/";
                string ConnectionString = WeworkLiteController.getConnectionString(ConnectionCache, loginData.CustomerID, _configuration);
                using (DpsConnection cnn = new DpsConnection(ConnectionString))
                {
                    bool Visible = Common.CheckRoleByUserID(loginData, 3502, cnn);
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
                    string dieukienSort = "id_row";
                    if (!string.IsNullOrEmpty(query.filter["id_project_team"]))
                        Conds.Add("id_project_team", query.filter["id_project_team"]);
                    else
                        Conds.Add("id_project_team", 0);
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
                        return JsonResultCommon.Exception(_logger, cnn.LastError, _config, loginData, ControllerContext);
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
                            DataTable dt_temp = cnn.CreateDataTable(sql_query, new SqlConditions() { { "old", dr["oldvalue"] }, { "new", dr["newvalue"] } });
                            if (dt_temp == null)
                                return JsonResultCommon.Exception(_logger, cnn.LastError, _config, loginData, ControllerContext);
                            dr["oldvalue"] = dt_temp.AsEnumerable().Where(x => x[0].ToString() == dr["oldvalue"].ToString()).Select(x => x[1]).FirstOrDefault();
                            dr["newvalue"] = dt_temp.AsEnumerable().Where(x => x[0].ToString() == dr["newvalue"].ToString()).Select(x => x[1]).FirstOrDefault();

                            if (int.Parse(dr["id_action"].ToString()) == 9) // Đối với tag gắn title
                            {
                                if (dt_temp.Rows.Count > 0)
                                    dr["action"] = dr["action"].ToString().Replace("{0}", dt_temp.Rows[0]["title"].ToString());
                                else
                                    dr["action"] = dr["action"].ToString().Replace("{0}", "");
                            }
                        }

                        #region Map info account từ JeeAccount  
                        if (dr["id_action"].ToString().Equals("15") || dr["id_action"].ToString().Equals("55") || dr["id_action"].ToString().Equals("56") || dr["id_action"].ToString().Equals("57"))
                        {
                            var value = dr["newvalue"];
                            var action = dr["id_action"];
                            var infoUser = DataAccount.Where(x => dr["newvalue"].ToString().Contains(x.UserId.ToString())).FirstOrDefault();
                            if (infoUser != null)
                            {
                                dr["action"] = dr["action"].ToString().Replace("{0}", infoUser.FullName);
                                dr["action_en"] = dr["action_en"].ToString().Replace("{0}", infoUser.FullName);

                            }

                            dr["oldvalue"] = DBNull.Value;
                            dr["newvalue"] = DBNull.Value;

                        }
                        #endregion
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
                               //orderby g.Key.u descending
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
                                                    UpdatedDate = string.Format("{0:dd/MM/yyyy HH:mm}", u["UpdatedDate"]),
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
                    return JsonResultCommon.ThanhCong(data, pageModel, Visible);
                }
                #endregion
            }
            catch (Exception ex)
            {
                return JsonResultCommon.Exception(_logger, ex, _config, loginData);
            }
        }
        #endregion

        /// <summary>
        /// Add view cho project
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        [Route("add-view")]
        [HttpPost]
        public async Task<object> Add_View(ProjectViewsModel data)
        {
            string Token = Common.GetHeader(Request);
            UserJWT loginData = Ulities.GetUserByHeader(HttpContext.Request.Headers);
            if (loginData == null)
                return JsonResultCommon.DangNhap();
            try
            {
                string strRe = "";
                if (string.IsNullOrEmpty(data.view_name_new))
                    strRe += (strRe == "" ? "" : ",") + "tên mẫu mới";
                if (data.id_project_team <= 0)
                    strRe += (strRe == "" ? "" : ",") + "dự án";
                if (strRe != "")
                    return JsonResultCommon.BatBuoc(strRe);

                string ConnectionString = WeworkLiteController.getConnectionString(ConnectionCache, loginData.CustomerID, _configuration);
                using (DpsConnection cnn = new DpsConnection(ConnectionString))
                {
                    string sqlq = "select * from we_project_team where Disabled=0 and id_row = " + data.id_project_team;
                    DataTable dtF = cnn.CreateDataTable(sqlq);
                    if (dtF.Rows.Count == 0)
                        return JsonResultCommon.KhongTonTai("Dự án/phòng ban");

                    sqlq = @"select * from we_default_views where id_row =" + data.viewid + "";
                    DataTable dt = cnn.CreateDataTable(sqlq);
                    if (dt.Rows.Count == 0)
                        return JsonResultCommon.KhongTonTai("Mẫu");
                    long iduser = loginData.UserID;
                    long idk = loginData.CustomerID;
                    cnn.BeginTransaction();

                    int re = 0;
                    Hashtable has = new Hashtable();
                    has.Add("viewid", data.viewid);
                    has.Add("id_project_team", data.id_project_team);
                    has.Add("id_department", data.id_department);
                    has.Add("view_name_new", data.view_name_new);
                    has.Add("default_everyone", data.default_everyone);
                    has.Add("default_for_me", data.default_for_me);
                    has.Add("pin_view", data.pin_view);
                    has.Add("personal_view", data.personal_view);
                    has.Add("favourite", data.favourite);
                    has.Add("link", dt.Rows[0]["link"].ToString());
                    has["CreatedDate"] = DateTime.Now;
                    has["CreatedBy"] = iduser;
                    re = cnn.Insert(has, "we_projects_view");
                    if (re != 1)
                    {
                        cnn.RollbackTransaction();
                        return JsonResultCommon.Exception(_logger, cnn.LastError, _config, loginData, ControllerContext);
                    }
                    if (!WeworkLiteController.log(_logger, loginData.Username, cnn, 48, data.id_row, iduser, null, iduser))
                    {
                        cnn.RollbackTransaction();
                        return JsonResultCommon.Exception(_logger, cnn.LastError, _config, loginData, ControllerContext);
                    }
                    cnn.EndTransaction();
                }
                return JsonResultCommon.ThanhCong();
            }
            catch (Exception ex)
            {
                return JsonResultCommon.Exception(_logger, ex, _config, loginData);
            }
        }

        /// <summary>
        /// Add view cho project
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        [Route("update-view")]
        [HttpPost]
        public async Task<object> update_view(ProjectViewsModel data)
        {
            string Token = Common.GetHeader(Request);
            UserJWT loginData = Ulities.GetUserByHeader(HttpContext.Request.Headers);
            if (loginData == null)
                return JsonResultCommon.DangNhap();
            try
            {
                string strRe = "";
                if (string.IsNullOrEmpty(data.view_name_new))
                    strRe += (strRe == "" ? "" : ",") + "tên mẫu mới";
                if (data.id_project_team <= 0)
                    strRe += (strRe == "" ? "" : ",") + "dự án";
                if (strRe != "")
                    return JsonResultCommon.BatBuoc(strRe);
                string ConnectionString = WeworkLiteController.getConnectionString(ConnectionCache, loginData.CustomerID, _configuration);
                using (DpsConnection cnn = new DpsConnection(ConnectionString))
                {
                    string sqlq = "select * from we_project_team where Disabled=0 and id_row = " + data.id_project_team;
                    DataTable dtF = cnn.CreateDataTable(sqlq);
                    if (dtF.Rows.Count == 0)
                        return JsonResultCommon.KhongTonTai("Dự án/phòng ban");
                    sqlq = @"select * from we_default_views where id_row =" + data.viewid + "";
                    DataTable dt = cnn.CreateDataTable(sqlq);
                    if (dt.Rows.Count == 0)
                        return JsonResultCommon.KhongTonTai("Mẫu");
                    SqlConditions sqlcond = new SqlConditions();
                    sqlcond.Add("id_row", data.id_row);
                    sqlcond.Add("disabled", 0);
                    string s = "select * from we_projects_view where (where)";
                    DataTable old = cnn.CreateDataTable(s, "(where)", sqlcond);
                    if (old == null || old.Rows.Count == 0)
                        return JsonResultCommon.KhongTonTai("Mẫu của dự án");
                    long iduser = loginData.UserID;
                    long idk = loginData.CustomerID;
                    cnn.BeginTransaction();
                    int re = 0;
                    Hashtable has = new Hashtable();
                    has.Add("viewid", data.viewid);
                    has.Add("id_project_team", data.id_project_team);
                    has.Add("id_department", data.id_department);
                    has.Add("view_name_new", data.view_name_new);
                    if (data.default_everyone != null)
                        has.Add("default_everyone", data.default_everyone);
                    if (data.default_for_me != null)
                        has.Add("default_for_me", data.default_for_me);
                    if (data.pin_view != null)
                        has.Add("pin_view", data.pin_view);
                    if (data.personal_view != null)
                        has.Add("personal_view", data.personal_view);
                    if (data.favourite != null)
                        has.Add("favourite", data.favourite);
                    has.Add("link", dt.Rows[0]["link"].ToString());
                    has["updateddate"] = DateTime.Now;
                    has["updatedby"] = iduser;
                    re = cnn.Update(has, sqlcond, "we_projects_view");
                    if (re != 1)
                    {
                        cnn.RollbackTransaction();
                        return JsonResultCommon.Exception(_logger, cnn.LastError, _config, loginData, ControllerContext);
                    }
                    if (!WeworkLiteController.log(_logger, loginData.Username, cnn, 49, data.id_row, iduser, null, iduser))
                    {
                        cnn.RollbackTransaction();
                        return JsonResultCommon.Exception(_logger, cnn.LastError, _config, loginData, ControllerContext);
                    }
                    cnn.EndTransaction();
                }
                return JsonResultCommon.ThanhCong();
            }
            catch (Exception ex)
            {
                return JsonResultCommon.Exception(_logger, ex, _config, loginData);
            }
        }

        [Route("delete-view")]
        [HttpGet]
        public BaseModel<object> Delete_View(long id)
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
                    string sqlq = "select * from we_projects_view where Disabled=0 and id_row = " + id;
                    DataTable dt = cnn.CreateDataTable(sqlq);
                    if (cnn.LastError != null || dt == null || dt.Rows.Count == 0)
                        return JsonResultCommon.KhongTonTai("Mẫu dự án");
                    //bool email_delete_team = (bool)dt.Rows[0]["email_delete_team"];
                    sqlq = "update we_projects_view set Disabled=1, UpdatedDate=getdate(), UpdatedBy=" + iduser + " where id_row = " + id;
                    cnn.BeginTransaction();
                    if (cnn.ExecuteNonQuery(sqlq) != 1)
                    {
                        cnn.RollbackTransaction();
                        return JsonResultCommon.Exception(_logger, cnn.LastError, _config, loginData, ControllerContext);
                    }
                    if (!WeworkLiteController.log(_logger, loginData.Username, cnn, 50, id, iduser))
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
        #region mail thông báo
        private async void mailthongbao(long id, int id_template, UserJWT loginData, string Token, DpsConnection cnn, List<AccUsernameModel> DataAccount)
        {
            emailMessage mailinfo2 = MailTemplate2(id_template, id, loginData, cnn, DataAccount);
            //get nguoi nhan
            string sqlnguoinhan = "select id_user , '' as hoten,'' as email from we_project_team_user where id_project_team = " + id;
            DataTable dt_user = cnn.CreateDataTable(sqlnguoinhan);
            #region Map info account người nhận từ JeeAccount 
            foreach (DataRow item in dt_user.Rows)
            {
                var info = DataAccount.Where(x => item["id_user"].ToString().Contains(x.UserId.ToString())).FirstOrDefault();
                if (info != null && !string.IsNullOrEmpty(info.Email))
                {
                    item["hoten"] = info.FullName;
                    item["email"] = info.Email;
                    emailMessage asyncnotice = new emailMessage()
                    {
                        access_token = Token,
                        //from = "derhades1998@gmail.com",
                        //to = "thanhthang1798@gmail.com", //
                        to = info.Email, //
                        subject = mailinfo2.subject.Replace("$nguoinhan$", info.FullName),
                        html = mailinfo2.html.Replace("$nguoinhan$", info.FullName) //nội dung html
                    };
                    await _notifier.sendEmail(asyncnotice);
                }

            }
            #endregion
        }
        #endregion
        /// <summary>
        /// Giao diện mail template
        /// </summary>
        /// <param name="id_template"></param>
        /// <param name="object_id"></param>
        /// <param name="nguoigui"></param>
        /// <param name="cnn"></param>
        /// <param name="DataAccount"></param>
        /// <returns></returns>
        public static emailMessage MailTemplate2(int id_template, long object_id, UserJWT nguoigui, DpsConnection cnn, List<AccUsernameModel> DataAccount)
        {
            emailMessage mailinfo = new emailMessage();
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
            string sqlq = "";
            if (dtKey != null && dtKey.Rows.Count > 0)
            {
                string table = dtKey.Rows[0]["object"].ToString();
                List<string> joins = dtKey.AsEnumerable().Where(x => x["join"] != DBNull.Value).Select(x => x["join"].ToString()).ToList();
                List<string> vals = dtKey.AsEnumerable().Where(x => x["value"] != DBNull.Value).Select(x => x["value"].ToString()).ToList();
                sqlq = "select " + string.Join(", ", vals) + " from " + table + " " + (joins.Count > 1 ? string.Join(" ", joins) : string.Join(",", joins)) + " where " + table + ".id_row=" + object_id;
            }

            DataTable dtFind = cnn.CreateDataTable(sqlq);
            if (cnn.LastError != null || dtFind.Rows.Count == 0)
                return null;
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
            if (dt.Columns.Contains("id_nv"))
            {
                #region Map info account từ JeeAccount
                var info = DataAccount.Where(x => values["id_nv"].ToString().Contains(x.UserId.ToString())).FirstOrDefault();
                if (info != null)
                {
                    values["hoten"] = info.FullName;
                }
                #endregion
            }

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
                if (key == "$nguoitao$")
                {
                    var infou = DataAccount.Where(x => values["id_nv"].ToString().Contains(x.UserId.ToString())).FirstOrDefault();
                    if (infou != null)
                    {
                        val = infou.FullName;
                    }
                }
                title = title.Replace(key, val);
                template = template.Replace(key, val);
            }

            #region nội dung info mail
            mailinfo.html = template;
            mailinfo.subject = title;
            #endregion
            return mailinfo;
        }

    }
}