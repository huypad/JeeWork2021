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
using DPSinfra.Notifier;
using Microsoft.Extensions.Logging;

namespace JeeWork_Core2021.Controllers.Wework
{
    [ApiController]
    [Route("api/repeated")]
    [EnableCors("JeeWorkPolicy")]
    /// <summary>
    /// api quản lý we_authorize
    /// </summary>
    // [EnableCors(origins: "*", headers: "*", methods: "*")]
    public class RepeatedController : ControllerBase
    {
        private readonly IHostingEnvironment _hostingEnvironment;
        private JeeWorkConfig _config;
        public List<AccUsernameModel> DataAccount;
        private IConnectionCache ConnectionCache;
        private IConfiguration _configuration;
        private INotifier _notifier;
        private readonly ILogger<RepeatedController> _logger;

        public RepeatedController(IOptions<JeeWorkConfig> config, IHostingEnvironment hostingEnvironment, IConnectionCache _cache, IConfiguration configuration, INotifier notifier, ILogger<RepeatedController> logger)
        {
            _hostingEnvironment = hostingEnvironment;
            _config = config.Value;
            ConnectionCache = _cache;
            _configuration = configuration;
            _notifier = notifier;
            _logger = logger;
        }
        APIModel.Models.Notify Knoti;
        /// <summary>
        /// 
        /// </summary>
        /// <param name="query"></param>
        /// <returns></returns>
        [Route("List")]
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

                    SqlConditions Conds = new SqlConditions();
                    string dieukienSort = "title", dieukien_where = " ";
                    if (!string.IsNullOrEmpty(query.filter["id_project_team"]))
                    {
                        dieukien_where += " and id_project_team=@id_project_team";
                        Conds.Add("id_project_team", query.filter["id_project_team"]);
                    }
                    //return JsonResultCommon.BatBuoc("Dự án/phòng ban");
                    if (!string.IsNullOrEmpty(query.filter["keyword"]))
                    {
                        dieukien_where += " and (t.title like '%@keyword%' or t.description like '%@keyword%' or tao.Username like '%@keyword%' or sua.Username like '%@keyword%')";
                        dieukien_where = dieukien_where.Replace("@keyword", query.filter["keyword"]);
                    }
                    #region Sort data theo các dữ liệu bên dưới
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
                    #region Trả dữ liệu về backend để hiển thị lên giao diện
                    string sqlq = @$"select t.*, p.title as project_team, t.assign as id_nv, '' as hoten,'' as image, '' as mobile, '' as Username,'' as Email, '' as Tenchucdanh,
                            '' as NguoiTao ,'' as NguoiSua from we_repeated t
                            join we_project_team p on t.id_project_team=p.id_row
                            where t.Disabled=0 and t.CreatedBy in ({listID})  " + dieukien_where + "  order by " + dieukienSort;
                    sqlq += @$";select id_row, id_repeated, Title, IsTodo, UserID, CreatedDate, CreatedBy, UpdatedDate, UpdatedBy, Disabled, Deadline,
task.UserID as id_nv, '' as hoten,'' as image, '' as mobile, '' as Username,'' as Email, '' as Tenchucdanh 
                        from we_repeated_Task task
                        where task.Disabled=0";
                    DataTable dt_user = cnn.CreateDataTable(@$"select *,id_user as id_nv,'' as hoten,'' as username,'' as mobile,'' as image 
from we_repeated_user u where u.Disabled = 0");
                    DataSet ds = cnn.CreateDataSet(sqlq, Conds);
                    if (cnn.LastError != null || ds == null)
                        return JsonResultCommon.Exception(_logger,cnn.LastError, _config, loginData , ControllerContext);
                    DataTable dt = ds.Tables[0];
                    if (dt.Rows.Count == 0)
                        return JsonResultCommon.ThanhCong(new List<string>(), pageModel, Visible);
                    #region Map info account từ JeeAccount
                    foreach (DataRow item in dt.Rows)
                    {
                        var info = DataAccount.Where(x => item["id_nv"].ToString().Contains(x.UserId.ToString())).FirstOrDefault();
                        var infoNguoiTao = DataAccount.Where(x => item["CreatedBy"].ToString().Contains(x.UserId.ToString())).FirstOrDefault();
                        var infoNguoiSua = DataAccount.Where(x => item["UpdatedBy"].ToString().Contains(x.UserId.ToString())).FirstOrDefault();

                        if (info != null)
                        {
                            item["hoten"] = info.FullName;
                            item["username"] = info.Username;
                            item["tenchucdanh"] = info.Jobtitle;
                            item["mobile"] = info.PhoneNumber;
                            item["image"] = info.AvartarImgURL;
                            item["email"] = info.Email;
                        };
                        if (infoNguoiTao != null)
                        {
                            item["NguoiTao"] = infoNguoiTao.FullName;
                        };
                        if (infoNguoiSua != null)
                        {
                            item["NguoiSua"] = infoNguoiSua.FullName;
                        };
                    }
                    
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
                            item["email"] = info.Email;
                        };
                    }
                    foreach (DataRow item in dt_user.Rows)
                    {
                        var info = DataAccount.Where(x => item["id_nv"].ToString().Contains(x.UserId.ToString())).FirstOrDefault();

                        if (info != null)
                        {
                            item["hoten"] = info.FullName;
                            item["username"] = info.Username;
                            item["mobile"] = info.PhoneNumber;
                            item["image"] = info.AvartarImgURL;
                        };
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
                                   title = r["title"],
                                   description = r["description"],
                                   frequency = r["frequency"],
                                   repeated_day = r["repeated_day"],
                                   id_project_team = r["id_project_team"],
                                   project_team = r["project_team"],
                                   Locked = r["Locked"],
                                   id_group = r["id_group"],
                                   deadline = r["deadline"],
                                   start_date = !string.IsNullOrEmpty(r["start_date"].ToString()) ? DateTime.Parse(r["start_date"].ToString()).ToString("dd/MM/yyyy") : "",
                                   //start_date = string.Format("{0:dd/MM/yyyy}", r["start_date"]),
                                   end_date = string.Format("{0:dd/MM/yyyy}", r["end_date"]),
                                   CreatedDate = string.Format("{0:dd/MM/yyyy HH:mm}", r["CreatedDate"]),
                                   CreatedBy = r["CreatedBy"],
                                   NguoiTao = r["NguoiTao"],
                                   UpdatedDate = r["UpdatedDate"] == DBNull.Value ? "" : string.Format("{0:dd/MM/yyyy HH:mm}", r["UpdatedDate"]),
                                   UpdatedBy = r["UpdatedBy"],
                                   NguoiSua = r["NguoiSua"],
                                   Assign = new
                                   {
                                       id_nv = r["id_nv"],
                                       hoten = r["hoten"],
                                       username = r["username"],
                                       mobile = r["mobile"],
                                       image = r["image"],
                                   },
                                   Users = from u in dt_user.AsEnumerable()
                                           where u["id_repeated"].Equals(r["id_row"])
                                           select new
                                           {

                                               id_nv = u["id_nv"],
                                               hoten = u["hoten"],
                                               username = u["username"],
                                               mobile = u["mobile"],
                                               image = u["image"],
                                           },
                                   Tasks = from dr in ds.Tables[1].AsEnumerable()
                                           where dr["id_repeated"].Equals(r["id_row"])
                                           select new
                                           {
                                               id_row = dr["id_row"],
                                               id_repeated = dr["id_repeated"],
                                               Title = dr["Title"],
                                               IsTodo = dr["IsTodo"],
                                               Deadline = dr["Deadline"],
                                               UserID = new
                                               {
                                                   id_nv = dr["id_nv"],
                                                   hoten = dr["hoten"],
                                                   username = dr["username"],
                                                   mobile = dr["mobile"],
                                                   image = dr["image"],
                                               },
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

        /// <summary>
        /// 
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
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

                    #region Trả dữ liệu về backend để hiển thị lên giao diện
                    string sqlq = @"select t.* from we_repeated t
                                    where t.Disabled=0 and t.id_row=" + id;
                    sqlq += @$";select u.id_row,  u.id_user  as id_nv, '' as hoten,'' as image, '' as mobile, '' as Username,'' as Email, '' as Tenchucdanh, id_repeated 
from we_repeated_user u where u.Disabled=0 and u.id_user in ({listID}) and id_repeated = " + id;
                    sqlq += @$";select id_row, id_repeated, Title, IsTodo, UserID, CreatedDate, CreatedBy, UpdatedDate, UpdatedBy, Disabled, Deadline,
task.UserID as id_nv, '' as hoten,'' as image, '' as mobile, '' as Username,'' as Email, '' as Tenchucdanh, id_repeated 
from we_repeated_Task task where task.Disabled=0";
                    DataSet ds = cnn.CreateDataSet(sqlq);
                    if (cnn.LastError != null || ds == null)
                    {
                        return JsonResultCommon.Exception(_logger,cnn.LastError, _config, loginData , ControllerContext);
                    }
                    if (ds.Tables[0] == null || ds.Tables[0].Rows.Count == 0)
                        return JsonResultCommon.KhongTonTai();

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
                            item["email"] = info.Email;
                        };
                    }

                    foreach (DataRow item in ds.Tables[2].Rows)
                    {
                        var info = DataAccount.Where(x => item["id_nv"].ToString().Contains(x.UserId.ToString())).FirstOrDefault();

                        if (info != null)
                        {
                            item["hoten"] = info.FullName;
                            item["username"] = info.Username;
                            item["tenchucdanh"] = info.Jobtitle;
                            item["mobile"] = info.PhoneNumber;
                            item["image"] = info.AvartarImgURL;
                            item["email"] = info.Email;
                        };
                    }
                    #endregion

                    var data = (from r in ds.Tables[0].AsEnumerable()
                                select new
                                {
                                    id_row = r["id_row"],
                                    title = r["title"],
                                    description = r["description"],
                                    frequency = r["frequency"],
                                    repeated_day = r["repeated_day"],
                                    id_project_team = r["id_project_team"],
                                    id_group = r["id_group"],
                                    deadline = r["deadline"],
                                    Locked = r["Locked"],
                                    start_date = r["start_date"],
                                    end_date = r["end_date"],
                                    Users = from dr in ds.Tables[1].AsEnumerable()
                                            where dr["id_repeated"].Equals(r["id_row"])
                                            select new
                                            {
                                                id_row = dr["id_row"],
                                                id_nv = dr["id_nv"],
                                                hoten = dr["hoten"],
                                                username = dr["username"],
                                                mobile = dr["mobile"],
                                                //image = WeworkLiteController.genLinkImage(loginData.CustomerID, dr["id_nv"].ToString())
                                            },
                                    Tasks = from dr2 in ds.Tables[2].AsEnumerable()
                                            where dr2["id_repeated"].Equals(r["id_row"])
                                            select new
                                            {
                                                id_row = dr2["id_row"],
                                                id_repeated = dr2["id_repeated"],
                                                Title = dr2["Title"],
                                                IsTodo = dr2["IsTodo"],
                                                Deadline = dr2["Deadline"],
                                                UserID = new
                                                {
                                                    id_nv = dr2["id_nv"],
                                                    hoten = dr2["hoten"],
                                                    username = dr2["username"],
                                                    mobile = dr2["mobile"],
                                                    image = dr2["image"],
                                                },
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

        /// <summary>
        /// 
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        [Route("Insert")]
        [HttpPost]
        public async Task<object> Insert(RepeatedModel data)
        {
            string Token = Common.GetHeader(Request);
            UserJWT loginData = Ulities.GetUserByHeader(HttpContext.Request.Headers);
            if (loginData == null)
                return JsonResultCommon.DangNhap();
            try
            {
                string strRe = "";
                List<string> listIDprojectTeam = data.id_project_team.Split(',').ToList();
                if (string.IsNullOrEmpty(data.title))
                    strRe += (strRe == "" ? "" : ",") + "tên công việc lặp lại";
                if (string.IsNullOrEmpty(data.repeated_day))
                    strRe += (strRe == "" ? "" : ",") + "các ngày lặp lại";
                if (listIDprojectTeam.Count <= 0)
                    strRe += (strRe == "" ? "" : ",") + "dự án/phòng ban";
                if (strRe != "")
                    return JsonResultCommon.BatBuoc(strRe);

                string ConnectionString = WeworkLiteController.getConnectionString(ConnectionCache, loginData.CustomerID, _configuration);
                using (DpsConnection cnn = new DpsConnection(ConnectionString))
                {
                    long iduser = loginData.UserID;
                    long idk = loginData.CustomerID;
                    long idc = 0;

                    foreach (var item in listIDprojectTeam)
                    {
                        Hashtable val = new Hashtable();
                        val.Add("title", data.title);
                        val.Add("description", string.IsNullOrEmpty(data.description) ? "" : data.description);
                        val.Add("repeated_day", data.repeated_day);
                        if (data.id_group > 0)
                            val.Add("id_group", data.id_group);
                        if (data.deadline > 0)
                            val.Add("deadline", data.deadline);
                        if (data.start_date != DateTime.MinValue)
                            val.Add("start_date", data.start_date);
                        if (data.end_date != DateTime.MinValue)
                            val.Add("end_date", data.end_date);
                        if (data.assign > 0)
                            val.Add("assign", data.assign);
                        val.Add("Locked", data.Locked);
                        val.Add("frequency", data.frequency);
                        val.Add("CreatedDate", DateTime.Now);
                        val.Add("CreatedBy", iduser);

                        val.Add("id_project_team", item);
                        string strCheck = "select count(*) from we_repeated where Disabled=0 and  (id_project_team=@id_project_team) and title=@name";
                        if (int.Parse(cnn.ExecuteScalar(strCheck, new SqlConditions() { { "id_project_team", item }, { "name", data.title } }).ToString()) > 0)
                        {
                            return JsonResultCommon.Custom("Công việc lặp lại đã tồn tại trong dự án/phòng ban");
                        }
                        cnn.BeginTransaction();
                        if (cnn.Insert(val, "we_repeated") != 1)
                        {
                            cnn.RollbackTransaction();
                            return JsonResultCommon.Exception(_logger, cnn.LastError, _config, loginData, ControllerContext);
                        }
                        idc = long.Parse(cnn.ExecuteScalar("select IDENT_CURRENT('we_repeated')").ToString());
                        if (data.Users != null)
                        {
                            Hashtable val1 = new Hashtable();
                            val1["id_repeated"] = idc;
                            val1["CreatedDate"] = DateTime.Now;
                            val1["CreatedBy"] = iduser;
                            foreach (var u in data.Users)
                            {
                                val1["id_user"] = u.id_user;
                                val1["loai"] = u.loai;
                                if (cnn.Insert(val1, "we_repeated_user") != 1)
                                {
                                    cnn.RollbackTransaction();
                                    return JsonResultCommon.Exception(_logger, cnn.LastError, _config, loginData, ControllerContext);
                                }
                            }
                        }
                        if (data.Tasks != null)
                        {
                            Hashtable val2 = new Hashtable();
                            val2["id_repeated"] = idc;
                            val2["CreatedDate"] = DateTime.Now;
                            val2["CreatedBy"] = iduser;
                            val2["Disabled"] = 0;
                            foreach (var _task in data.Tasks)
                            {
                                val2["IsTodo"] = _task.IsTodo;
                                val2["Title"] = _task.Title;
                                val2["UserID"] = _task.UserID;
                                val2["Deadline"] = _task.Deadline;
                                if (cnn.Insert(val2, "we_repeated_Task") != 1)
                                {
                                    cnn.RollbackTransaction();
                                    return JsonResultCommon.Exception(_logger, cnn.LastError, _config, loginData, ControllerContext);
                                }
                            }
                        }

                    }

                    //string LogContent = "", LogEditContent = "";
                    //LogContent = LogEditContent = "Thêm mới dữ liệu repeated: title=" + data.title + ", id_project_team=" + data.id_project_team;
                    //DpsPage.Ghilogfile(loginData.CustomerID.ToString(), LogEditContent, LogContent, loginData.UserName);
                    cnn.EndTransaction();
                    data.id_row = idc;
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
        /// <param name="data"></param>
        /// <returns></returns>
        [Route("Update")]
        [HttpPost]
        public async Task<BaseModel<object>> Update(RepeatedModel data)
        {
            string Token = Common.GetHeader(Request);
            UserJWT loginData = Ulities.GetUserByHeader(HttpContext.Request.Headers);
            if (loginData == null)
                return JsonResultCommon.DangNhap();
            try
            {
                string strRe = "";
                if (string.IsNullOrEmpty(data.title))
                    strRe += (strRe == "" ? "" : ",") + "tên công việc lặp lại";
                if (string.IsNullOrEmpty(data.repeated_day))
                    strRe += (strRe == "" ? "" : ",") + "các ngày lặp lại";
                if (strRe != "")
                    return JsonResultCommon.BatBuoc(strRe);
                string ConnectionString = WeworkLiteController.getConnectionString(ConnectionCache, loginData.CustomerID, _configuration);
                using (DpsConnection cnn = new DpsConnection(ConnectionString))
                {
                    SqlConditions sqlcond = new SqlConditions();
                    sqlcond.Add("id_row", data.id_row);
                    sqlcond.Add("disabled", 0);
                    string s = "select * from we_repeated where (where)";
                    DataTable old = cnn.CreateDataTable(s, "(where)", sqlcond);
                    if (old == null || old.Rows.Count == 0)
                        return JsonResultCommon.KhongTonTai("repeated");
                    long iduser = loginData.UserID;
                    long idk = loginData.CustomerID;
                    Hashtable val = new Hashtable();
                    val.Add("title", data.title);
                    val.Add("description", string.IsNullOrEmpty(data.description) ? "" : data.description);
                    val.Add("repeated_day", data.repeated_day);
                    if (data.assign > 0)
                        val.Add("assign", data.assign);
                    else
                        val.Add("assign", DBNull.Value);
                    if (data.id_group > 0)
                        val.Add("id_group", data.id_group);
                    else
                        val.Add("id_group", DBNull.Value);
                    if (data.deadline > 0)
                        val.Add("deadline", data.deadline);
                    else
                        val.Add("deadline", DBNull.Value);
                    if (data.start_date != DateTime.MinValue)
                        val.Add("start_date", data.start_date);
                    else
                        val.Add("start_date", DBNull.Value);
                    if (data.end_date != DateTime.MinValue)
                        val.Add("end_date", data.end_date);
                    else
                        val.Add("end_date", DBNull.Value);
                    val.Add("Locked", data.Locked);
                    val.Add("UpdatedDate", DateTime.Now);
                    val.Add("UpdatedBy", iduser);
                    string strCheck = "select count(*) from we_repeated where Disabled=0 and  (id_project_team=@id_project_team) and title=@name and id_row<>@id";
                    if (int.Parse(cnn.ExecuteScalar(strCheck, new SqlConditions() { { "id_project_team", data.id_project_team }, { "name", data.title }, { "id", data.id_row } }).ToString()) > 0)
                    {
                        return JsonResultCommon.Custom("repeated đã tồn tại trong dự án/phòng ban");
                    }
                    cnn.BeginTransaction();
                    if (cnn.Update(val, sqlcond, "we_repeated") != 1)
                    {
                        cnn.RollbackTransaction();
                        return JsonResultCommon.Exception(_logger,cnn.LastError, _config, loginData , ControllerContext);
                    }
                    if (data.Users.Count > 0) //
                    {
                        string ids = string.Join(",", data.Users.Select(x => x.id_user));
                        if (ids != "")//xóa follower
                        {
                            string strDel = "Update we_repeated_user set Disabled=1, UpdatedDate=getdate(), UpdatedBy=" + iduser + " where Disabled=0 and  id_repeated=" + data.id_row ;
                            if (cnn.ExecuteNonQuery(strDel) < 0)
                            {
                                cnn.RollbackTransaction();
                                return JsonResultCommon.Exception(_logger,cnn.LastError, _config, loginData , ControllerContext);
                            }
                        }
                        DataTable dt_user = cnn.CreateDataTable($"select id_user from we_repeated_user where  Disabled=0 and  id_repeated={data.id_row}");
                        var listU = dt_user.AsEnumerable().Select(x => x["id_user"]).ToList();
                        Hashtable val1 = new Hashtable();
                        val1["id_repeated"] = data.id_row;
                        val1["CreatedDate"] = DateTime.Now;
                        val1["CreatedBy"] = iduser;
                        //data.Users = data.Users.Where(x => listU.Where(y=>y==x));
                        foreach (var user in data.Users)
                        {
                            val1["id_user"] = user.id_user;
                            val1["loai"] = user.loai;
                            if (cnn.Insert(val1, "we_repeated_user") != 1)
                            {
                                cnn.RollbackTransaction();
                                return JsonResultCommon.Exception(_logger,cnn.LastError, _config, loginData , ControllerContext);
                            }
                        }
                    }
                    else // Trường hợp không có Users
                    {
                        string deleteuser = "Update we_repeated_user set Disabled=1, UpdatedDate=getdate(), UpdatedBy=" + iduser + " where Disabled=0 and  id_repeated=" + data.id_row + "";
                        if (cnn.ExecuteNonQuery(deleteuser) < 0)
                        {
                            cnn.RollbackTransaction();
                            return JsonResultCommon.Exception(_logger,cnn.LastError, _config, loginData , ControllerContext);
                        }
                    }
                    string idtask = string.Join(",", data.Tasks.Where(x => x.id_row > 0).Select(x => x.id_row));
                    if (idtask != "")
                    {
                        string strDel = "Update we_repeated_Task set Disabled=1, UpdatedDate=getdate(), UpdatedBy=" + iduser + " " +
                            "where Disabled=0 and  id_repeated=" + data.id_row;// + " and id_row not in (" + idtask + ")";
                        if (cnn.ExecuteNonQuery(strDel) < 0)
                        {
                            cnn.RollbackTransaction();
                            return JsonResultCommon.Exception(_logger,cnn.LastError, _config, loginData , ControllerContext);
                        }
                    }
                    if (data.Tasks != null)
                    {
                        Hashtable val2 = new Hashtable();
                        val2["id_repeated"] = data.id_row;
                        val2["CreatedDate"] = DateTime.Now;
                        val2["CreatedBy"] = iduser;
                        val2["Disabled"] = 0;
                        foreach (var _task in data.Tasks)
                        {
                            val2["IsTodo"] = _task.IsTodo;
                            val2["Title"] = _task.Title;
                            val2["UserID"] = _task.UserID;
                            val2["Deadline"] = _task.Deadline;
                            if (cnn.Insert(val2, "we_repeated_Task") != 1)
                            {
                                cnn.RollbackTransaction();
                                return JsonResultCommon.Exception(_logger,cnn.LastError, _config, loginData , ControllerContext);
                            }
                        }
                    }
                    DataTable dt = cnn.CreateDataTable(s, "(where)", sqlcond);
                    //string LogContent = "", LogEditContent = "";
                    //LogEditContent = DpsPage.GetEditLogContent(old, dt);
                    //if (!LogEditContent.Equals(""))
                    //{
                    //    LogEditContent = "Chỉnh sửa dữ liệu (" + data.id_row + ") : " + LogEditContent;
                    //    LogContent = "Chỉnh sửa dữ liệu repeated (" + data.id_row + "), Chi tiết xem trong log chỉnh sửa chức năng";
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

        /// <summary>
        /// 
        /// </summary>
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
                    string sqlq = "select ISNULL((select count(*) from we_repeated where Disabled=0 and  id_row = " + id + "),0)";
                    if (long.Parse(cnn.ExecuteScalar(sqlq).ToString()) != 1)
                        return JsonResultCommon.KhongTonTai("repeated");
                    //if (Common.TestDuplicate("", id.ToString(), "-1", "we_work", "id_repeated", "Disabled", "0", cnn, "", true) == false)
                    //{
                    //    return JsonResultCommon.Custom("Đang có công việc thuộc mục tiêu này nên không thể xóa");
                    //}
                    sqlq = "update we_repeated set Disabled=1, UpdatedDate=getdate(), UpdatedBy=" + iduser + " where id_row = " + id;
                    cnn.BeginTransaction();
                    if (cnn.ExecuteNonQuery(sqlq) != 1)
                    {
                        cnn.RollbackTransaction();
                        return JsonResultCommon.Exception(_logger,cnn.LastError, _config, loginData , ControllerContext);
                    }
                    //string LogContent = "Xóa dữ liệu repeated (" + id + ")";
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

        /// <summary>
        /// 
        /// <param name="id"></param>
        /// <param name="locked"></param>
        /// <returns></returns>
        [Route("Lock")]
        [HttpGet]
        public async Task<object> Lock(long id, bool locked = true)
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
                    sqlcond.Add("id_row", id);
                    sqlcond.Add("disabled", 0);
                    string s = "select * from we_repeated where (where)";
                    DataTable old = cnn.CreateDataTable(s, "(where)", sqlcond);
                    if (old == null || old.Rows.Count == 0)
                        return JsonResultCommon.KhongTonTai("Công việc lặp lại");
                    if ((bool)old.Rows[0]["Locked"] == locked)
                        return JsonResultCommon.Custom("Nhóm làm việc này vẫn chưa " + (locked ? "kích hoạt" : "vô hiệu hóa"));
                    long iduser = loginData.UserID;
                    long idk = loginData.CustomerID;
                    Hashtable val = new Hashtable();
                    val.Add("locked", locked);
                    val.Add("UpdatedDate", DateTime.Now);
                    val.Add("UpdatedBy", iduser);
                    cnn.BeginTransaction();
                    if (cnn.Update(val, sqlcond, "we_repeated") != 1)
                    {
                        cnn.RollbackTransaction();
                        return JsonResultCommon.Exception(_logger,cnn.LastError, _config, loginData , ControllerContext);
                    }
                    DataTable dt = cnn.CreateDataTable(s, "(where)", sqlcond);
                    //string LogContent = "", LogEditContent = "";
                    //LogEditContent = DpsPage.GetEditLogContent(old, dt);
                    //if (!LogEditContent.Equals(""))
                    //{
                    //    LogEditContent = "Chỉnh sửa dữ liệu (" + id + ") : " + LogEditContent;
                    //    LogContent = "Chỉnh sửa dữ liệu repeated (" + id + "), Chi tiết xem trong log chỉnh sửa chức năng";
                    //}
                    //DpsPage.Ghilogfile(loginData.CustomerID.ToString(), LogEditContent, LogContent, loginData.UserName);

                    cnn.EndTransaction();
                    return JsonResultCommon.ThanhCong(locked);
                }
            }
            catch (Exception ex)
            {
                return JsonResultCommon.Exception(_logger,ex, _config, loginData);
            }
        }

        /// <summary>
        /// Cập nhật người giao việc
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [Route("assign")]
        [HttpGet]
        public BaseModel<object> Assign(long id, long user)
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
                    string sqlq = "select ISNULL((select count(*) from we_repeated where Disabled=0 and  id_row = " + id + "),0)";
                    if (long.Parse(cnn.ExecuteScalar(sqlq).ToString()) != 1)
                        return JsonResultCommon.KhongTonTai("Công việc lặp lại");
                    Hashtable val = new Hashtable();
                    val["assign"] = user;
                    val["UpdatedDate"] = DateTime.Now;
                    val["UpdatedBy"] = iduser;
                    if (cnn.Update(val, new SqlConditions() { { "id_row", id } }, "we_repeated") != 1)
                    {
                        return JsonResultCommon.Exception(_logger,cnn.LastError, _config, loginData , ControllerContext);
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
        /// Cập nhật dự án
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [Route("project_team")]
        [HttpGet]
        public BaseModel<object> project_team(long id, long id_project_team)
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
                    string sqlq = "select ISNULL((select count(*) from we_repeated where Disabled=0 and  id_row = " + id + "),0)";
                    if (long.Parse(cnn.ExecuteScalar(sqlq).ToString()) != 1)
                        return JsonResultCommon.KhongTonTai("Công việc lặp lại");
                    Hashtable val = new Hashtable();
                    val["id_project_team"] = id_project_team;
                    val["UpdatedDate"] = DateTime.Now;
                    val["UpdatedBy"] = iduser;
                    if (cnn.Update(val, new SqlConditions() { { "id_row", id } }, "we_repeated") != 1)
                    {
                        return JsonResultCommon.Exception(_logger,cnn.LastError, _config, loginData , ControllerContext);
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
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [Route("forcerun")]
        [HttpGet]
        public BaseModel<object> Forcerun(long id_repeated)
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
                    string sqlq = "select ISNULL((select count(*) from we_repeated where Disabled=0 and  id_row = " + id_repeated + "),0)";
                    if (long.Parse(cnn.ExecuteScalar(sqlq).ToString()) != 1)
                        return JsonResultCommon.KhongTonTai("repeated");
                    sqlq = "select id_row, title, frequency, repeated_day, id_project_team, id_group" +
                        ",deadline, start_date, end_date" +
                        ",description, assign, Locked, CreatedDate, CreatedBy" +
                        ",Disabled, UpdatedDate, UpdatedBy, last_run, run_by " +
                        "from we_repeated where Disabled = 0 and id_row = " + id_repeated + "";
                    DataTable dt = new DataTable();
                    dt = cnn.CreateDataTable(sqlq);
                    bool IsCreateAuto = false;
                    foreach (DataRow _item in dt.Rows)
                    {
                        DayOfWeek date = new DayOfWeek();
                        DateTime time = DateTime.UtcNow;
                        double loai = double.Parse(_item["frequency"].ToString());
                        string[] repeated_day = _item["repeated_day"].ToString().Split(',');
                        if (!(bool)_item["locked"]) // Chưa lock thì mới cho tạo
                        {
                            foreach (string day in repeated_day)
                            {
                                DateTime WeekdayCurrent = new DateTime();
                                if (loai == 1) // Báo cáo theo tuần
                                {
                                    date = Common.GetDayOfWeekDay(day);
                                    WeekdayCurrent = Common.StartOfWeek(time, date); // Lấy ra ngày (Param: thứ) của tuần hiện tại
                                    if (WeekdayCurrent < DateTime.UtcNow) // Nếu WeekdayCurrent nhỏ hơn ngày hiện tại, thì tạo dữ liệu cho thứ của tuần tiếp theo
                                        WeekdayCurrent = WeekdayCurrent.AddDays(7);
                                }
                                if (loai == 2) // Báo cáo theo tháng
                                {
                                    // Ngày lặp lại = ngày truyền vào của tháng và năm đó
                                    WeekdayCurrent = new DateTime(DateTime.Now.Year, DateTime.Now.Month, int.Parse(day));
                                    if (WeekdayCurrent < DateTime.UtcNow) // Nếu WeekdayCurrent nhỏ hơn ngày hiện tại, thì tạo dữ liệu cho thứ của tuần tiếp theo
                                        WeekdayCurrent = WeekdayCurrent.AddMonths(1);
                                }
                                // Kiểm tra ngày lấy được có nằm trong khoảng time lặp lại không
                                if (WeekdayCurrent >= (DateTime)_item["start_date"] && WeekdayCurrent <= (DateTime)_item["end_date"])
                                {
                                    // Kiểm tra đã có dữ liệu repeated chưa
                                    SqlConditions cond = new SqlConditions();
                                    cond.Add("start_date", WeekdayCurrent);
                                    cond.Add("id_repeated", id_repeated);
                                    cond.Add("Disabled", 0);
                                    DataTable dt_repeated_work = cnn.CreateDataTable("select * from we_work where (where)", "(where)", cond);
                                    if (dt_repeated_work.Rows.Count == 0)
                                        IsCreateAuto = true;
                                }
                                // Nếu ngày kết thúc đã quá thời gian hiện tại
                                if ((DateTime)_item["end_date"] < DateTime.Now)
                                {
                                    return new BaseModel<object>()
                                    {
                                        status = 0,
                                        error = new ErrorModel()
                                        {
                                            message = "Thời gian lặp lại của công việc đã hết. Vui lòng kiểm tra lại ngày kết thúc",
                                            code = JeeWorkConstant.ERRORDATA
                                        }
                                    };
                                }
                                // Tạo công việc
                                if (IsCreateAuto)
                                {
                                    bool result = WeWork_CreateTaskAuto(dt, cnn, loginData.UserID.ToString(), WeekdayCurrent);
                                    if (result) // Gửi mail & notify
                                    {
                                        // Update lại thông tin ForceRun
                                        Hashtable has = new Hashtable();
                                        SqlConditions conds = new SqlConditions();
                                        conds.Add("id_row", _item["id_row"].ToString());
                                        has.Add("last_run", DateTime.Now);
                                        has.Add("run_by", 0);
                                        cnn.Update(has, conds, "we_repeated");
                                        string sql_new = "select we_work_user.*, we_work.title as tencongviec " +
                                            "from we_work join we_work_user on we_work_user.id_work = we_work.id_row " +
                                            "where id_repeated=" + id_repeated + " and we_work.disabled = 0";
                                        DataTable dt_New_Data = cnn.CreateDataTable(sql_new);
                                        if (dt_New_Data.Rows.Count > 0)
                                        {
                                            foreach (DataRow row in dt_New_Data.Rows)
                                            {
                                                var users = new List<long> { long.Parse(row["id_user"].ToString()) };
                                                WeworkLiteController.mailthongbao(int.Parse(row["id_work"].ToString()), users, 10, loginData, ConnectionString, _notifier);
                                                #region Notify thêm mới công việc
                                                Hashtable has_replace = new Hashtable();
                                                for (int i = 0; i < users.Count; i++)
                                                {
                                                    NotifyModel notify_model = new NotifyModel();
                                                    has_replace = new Hashtable();
                                                    has_replace.Add("nguoigui", loginData.Username);
                                                    has_replace.Add("tencongviec", row["tencongviec"]);
                                                    notify_model.AppCode = "WW";
                                                    notify_model.From_IDNV = loginData.UserID.ToString();
                                                    notify_model.To_IDNV = users[i].ToString();
                                                    notify_model.TitleLanguageKey = "ww_themmoicongviec";
                                                    notify_model.ReplaceData = has_replace;
                                                    notify_model.To_Link_MobileApp = "";
                                                    notify_model.To_Link_WebApp = "/tasks/detail/" + int.Parse(row["id_work"].ToString()) + "";
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
                            }
                            //string LogContent = " RUN repeated (" + id_repeated + ")";
                            //DpsPage.Ghilogfile(loginData.CustomerID.ToString(), LogContent, LogContent, loginData.UserName);
                            cnn.EndTransaction();
                            return JsonResultCommon.ThanhCong();
                        }
                        else
                        {
                            return new BaseModel<object>()
                            {
                                status = 0,
                                error = new ErrorModel()
                                {
                                    message = "Công việc đang ở trạng thái VÔ HIỆU HÓA. Vui lòng kích hoạt công việc thực thi lệnh tạo công việc tự động ngay",
                                    code = JeeWorkConstant.ERRORDATA
                                }
                            };
                        }
                    }
                    return JsonResultCommon.ThanhCong();
                }
            }
            catch (Exception ex)
            {
                return JsonResultCommon.Exception(_logger,ex, _config, loginData);
            }
        }
        public static bool WeWork_CreateTaskAuto(DataTable dt, DpsConnection cnn, string runby, DateTime ngaybatdau)
        {
            Hashtable val = new Hashtable();
            foreach (DataRow dr in dt.Rows)
            {
                val = new Hashtable();
                val.Add("title", dr["title"].ToString());
                if (string.IsNullOrEmpty(dr["description"].ToString()))
                    val.Add("description", "");
                else
                    val.Add("description", dr["description"].ToString());
                val.Add("id_project_team", dr["id_project_team"].ToString());
                if (!string.IsNullOrEmpty(dr["id_group"].ToString()))
                {
                    if (int.Parse(dr["id_group"].ToString()) > 0)
                        val.Add("id_group", dr["id_group"].ToString());
                }
                val.Add("CreatedDate", DateTime.Now);
                val.Add("Disabled", 0);
                val.Add("CreatedBy", runby);
                val.Add("id_repeated", dr["id_row"].ToString());
                val.Add("start_date", ngaybatdau);
                cnn.BeginTransaction();
                if (cnn.Insert(val, "we_work") != 1)
                {
                    cnn.RollbackTransaction();
                    return false;
                }
                long maxid = long.Parse(cnn.ExecuteScalar("select IDENT_CURRENT('we_work')").ToString());
                // Insert member
                string sql_user = "select id_row, id_repeated, id_user, CreatedDate" +
                    ", CreatedBy, Disabled, UpdatedDate, UpdatedBy " +
                    "from we_repeated_user " +
                    "where disabled = 0 and id_repeated = " + dr["id_row"];
                DataTable dt_user = cnn.CreateDataTable(sql_user);
                if (dt_user.Rows.Count > 0)
                {
                    foreach (DataRow dr_user in dt_user.Rows)
                    {
                        val = new Hashtable();
                        val.Add("id_user", dr_user["id_user"]);
                        val.Add("id_work", maxid);
                        val.Add("loai", 2); // insert người theo dõi
                        val.Add("CreatedDate", DateTime.Now);
                        val.Add("Disabled", 0);
                        val.Add("CreatedBy", runby);
                        cnn.BeginTransaction();
                        if (cnn.Insert(val, "we_work_user") != 1)
                        {
                            cnn.RollbackTransaction();
                            return false;
                        }
                    }
                }
                // Insert assign
                val = new Hashtable();
                val.Add("id_user", dr["assign"].ToString());
                val.Add("id_work", maxid);
                val.Add("loai", 1); // insert giao việc
                val.Add("CreatedDate", DateTime.Now);
                val.Add("Disabled", 0);
                val.Add("CreatedBy", runby);
                cnn.BeginTransaction();
                if (cnn.Insert(val, "we_work_user") != 1)
                {
                    cnn.RollbackTransaction();
                    return false;
                }
                // Insert check list
                string sql_work = "select id_row, id_repeated, title, istodo, userid, CreatedDate" +
                    ", CreatedBy, Disabled, UpdatedDate, UpdatedBy " +
                    "from we_repeated_Task " +
                    "where disabled = 0 and id_repeated = " + dr["id_row"];
                DataTable dt_work = cnn.CreateDataTable(sql_work);
                if (dt_work.Rows.Count > 0)
                {
                    foreach (DataRow dr_work in dt_work.Rows)
                    {
                        if ((bool)dr_work["istodo"]) // To do
                        {
                            val = new Hashtable();
                            val.Add("id_work", maxid);
                            val.Add("title", dr_work["title"]);
                            val.Add("CreatedDate", DateTime.Now);
                            val.Add("Disabled", 0);
                            val.Add("CreatedBy", runby);
                            cnn.BeginTransaction();
                            if (cnn.Insert(val, "we_checklist") != 1)
                            {
                                cnn.RollbackTransaction();
                                return false;
                            }
                        }
                        else // Sub task
                        {
                            val = new Hashtable();
                            val.Add("title", dr_work["title"].ToString());
                            val.Add("id_project_team", dr["id_project_team"].ToString());
                            if (!string.IsNullOrEmpty(dr["id_group"].ToString()))
                            {
                                if ((int)dr["id_group"] > 0)
                                    val.Add("id_group", dr["id_group"].ToString());
                            }
                            val.Add("CreatedDate", DateTime.Now);
                            val.Add("Disabled", 0);
                            val.Add("CreatedBy", runby);
                            val.Add("id_repeated", dr["id_row"].ToString());
                            val.Add("start_date", ngaybatdau);
                            val.Add("id_parent", maxid);
                            cnn.BeginTransaction();
                            if (cnn.Insert(val, "we_work") != 1)
                            {
                                cnn.EndTransaction();
                                cnn.RollbackTransaction();
                                return false;
                            }
                        }
                    }
                }
            }
            return true;
        }
    }
}
