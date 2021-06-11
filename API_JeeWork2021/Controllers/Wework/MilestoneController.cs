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
using DPSinfra.ConnectionCache;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace JeeWork_Core2021.Controllers.Wework
{
    [ApiController]
    [Route("api/milestone")]
    [EnableCors("JeeWorkPolicy")]
    /// <summary>
    /// api quản lý we_authorize
    /// </summary>
    // [EnableCors(origins: "*", headers: "*", methods: "*")]
    public class MilestoneController : ControllerBase
    {
        private readonly IHostingEnvironment _hostingEnvironment;
        private JeeWorkConfig _config;
        public List<AccUsernameModel> DataAccount;
        private IConnectionCache ConnectionCache;
        private IConfiguration _configuration;
        private readonly ILogger<MilestoneController> _logger;

        public MilestoneController(IOptions<JeeWorkConfig> config, IHostingEnvironment hostingEnvironment, IConnectionCache _cache, IConfiguration configuration, ILogger<MilestoneController> logger)
        {
            _hostingEnvironment = hostingEnvironment;
            _config = config.Value;
            ConnectionCache = _cache;
            _configuration = configuration;
            _logger = logger;
        }
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
                    if (string.IsNullOrEmpty(query.filter["id_department"]))
                        return JsonResultCommon.Custom("Ban bắt buộc nhập");
                    dieukien_where += " and id_department=@id_department";
                    Conds.Add("id_department", query.filter["id_department"]);
                    if (!string.IsNullOrEmpty(query.filter["keyword"]))
                    {
                        dieukien_where += " and (m.title like '%@keyword%' or m.description like '%@keyword%' or tao.Username like '%@keyword%' or sua.Username like '%@keyword%')";
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
                            {"hoten","hoten" }
                        };
                    #endregion
                    if (!string.IsNullOrEmpty(query.sortField) && sortableFields.ContainsKey(query.sortField))
                        dieukienSort = sortableFields[query.sortField] + ("desc".Equals(query.sortOrder) ? " desc" : " asc");

                    // cần update status động
                    #region Trả dữ liệu về backend để hiển thị lên giao diện
                    string sqlq = @$"select m.*, coalesce(w.tong,0) as tong,coalesce( w.ht,0) as ht, p.title as project_team, m.person_in_charge as Id_NV,'' as hoten,'' as mobile, '' as username, '' as Email, '' as image,'' as Tenchucdanh,'' as NguoiTao, '' as NguoiSua from we_milestone m 
join we_project_team p on m.id_project_team=p.id_row
left join (select count(*) as tong, COUNT(CASE WHEN w.status=2 THEN 1 END) as ht,w.id_milestone from v_wework_new w group by w.id_milestone) w on m.id_row=w.id_milestone
 where m.Disabled=0 and m.person_in_charge in ({listID}) and m.CreatedBy in ({listID}) " + dieukien_where + "  order by " + dieukienSort;
                    DataTable dt = cnn.CreateDataTable(sqlq, Conds);
                    if (cnn.LastError != null || dt == null)
                        return JsonResultCommon.Exception(_logger,cnn.LastError, _config, loginData,ControllerContext);
                    if (dt.Rows.Count == 0)
                        return JsonResultCommon.ThanhCong(new List<string>(), pageModel, Visible);

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
                                   id_project_team = r["id_project_team"],
                                   project_team = r["project_team"],
                                   deadline_weekday = r["deadline"].Equals(DBNull.Value) ? "" : Common.GetFormatDate(Convert.ToDateTime(r["deadline"]), "77622"),
                                   deadline_day = r["deadline"].Equals(DBNull.Value) ? "" : Common.GetFormatDate(Convert.ToDateTime(r["deadline"]), "dd/MM"),
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
                                       //image = WeworkLiteController.genLinkImage(domain, loginData.CustomerID, r["id_nv"].ToString(), _hostingEnvironment.ContentRootPath)
                                   },
                                   Count = new
                                   {
                                       tong = r["tong"],
                                       ht = r["ht"],
                                       percentage = WeworkLiteController.calPercentage(r["tong"], r["ht"])
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
                    int id_project = int.Parse(cnn.ExecuteScalar("select id_project_team from we_milestone where id_row =" + id).ToString());
                    long hoanthanh = ReportByProjectController.GetStatusComplete(id_project, cnn);
                    long quahan = ReportByProjectController.GetStatusDeadline(id_project, cnn);
                    long todo = ReportByProjectController.GetStatusTodo(id_project, cnn);
                    // #cần update trạng thái công việc
                    #region Trả dữ liệu về backend để hiển thị lên giao diện
                    string sqlq = @$"select m.*, p.title as project_team, p.id_department, d.title as department, 
m.person_in_charge as Id_NV,'' as hoten,'' as mobile, '' as username, '' as Email, '' as image,'' as Tenchucdanh,'' as NguoiTao, '' as NguoiSua from we_milestone m 
join we_project_team p on m.id_project_team=p.id_row
join we_department d on d.id_row=p.id_department
 where m.Disabled=0 and m.person_in_charge in ({listID}) and m.CreatedBy in ({listID})  and m.id_row=" + id;
                    sqlq += @$";select w.*, 
iIf(w.Status={hoanthanh} and w.end_date>w.deadline,1,0) as is_htquahan,
iIf(w.Status={hoanthanh} and (w.end_date <= w.deadline or w.end_date is null or w.deadline is null),1,0) as is_htdunghan,
iIf(w.Status not in ({hoanthanh},{quahan}),1,0) as is_danglam,
iIf(w.Status = {quahan}, 1, 0) as is_quahan
from v_wework_clickup_new w
where w.disabled=0 and id_milestone = " + id;
                    //                    sqlq += @"; select count(CASE WHEN w.Status=2 and w.end_date<=w.deadline THEN 1 END) as htquahan ,
                    //count(CASE WHEN w.Status=2 and w.end_date>w.deadline THEN 1 END) as htdunghan ,
                    //count(CASE WHEN w.Status=1 and (getdate()<=w.deadline or w.deadline is null) THEN 1 END) as danglam ,
                    //count(CASE WHEN w.Status=1 and getdate()>w.deadline THEN 1 END) as  quahan, count(*) as tong
                    //from v_wework w where disabled=0 and id_milestone=" + id;
                    //                    sqlq += @"; select count(CASE WHEN w.Status=2 and w.end_date<=m.deadline then 1 end) as htquahan ,
                    //count(CASE WHEN w.Status=2 and w.end_date>m.deadline then 1 end) as htdunghan ,
                    //count(CASE WHEN w.Status=1 and (getdate()<=m.deadline or m.deadline is null) then 1 end) as danglam ,
                    //count(CASE WHEN w.Status=1 and getdate()>m.deadline then 1 end) as  quahan, count(*) as tong
                    //from v_wework w join we_milestone m on w.id_milestone=m.id_row
                    // where w.disabled=0 and id_milestone=" + id;
                    SqlConditions conds1 = new SqlConditions();
                    conds1 = new SqlConditions();
                    conds1.Add("w_user.Disabled", 0);
                    string select_user = $@"select  distinct w_user.id_user,'' as hoten,'' as Username,'' as Mobile,'' as image, id_work
                                                    from we_work_user w_user join we_work on we_work.id_row = w_user.id_work 
                                                    where (where)";
                    DataTable User = cnn.CreateDataTable(select_user, "(where)", conds1);
                    DataSet ds = cnn.CreateDataSet(sqlq);
                    if (cnn.LastError != null || ds == null)
                    {
                        return JsonResultCommon.Exception(_logger,cnn.LastError, _config, loginData,ControllerContext);
                    }
                    if (ds.Tables[0] == null || ds.Tables[0].Rows.Count == 0)
                        return JsonResultCommon.KhongTonTai();

                    #region Map info account từ JeeAccount

                    foreach (DataRow item in ds.Tables[0].Rows)
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

                    // table 1
                    foreach (DataRow item in ds.Tables[1].Rows)
                    {
                        var info = DataAccount.Where(x => item["id_nv"].ToString().Contains(x.UserId.ToString())).FirstOrDefault();

                        if (info != null)
                        {
                            item["hoten"] = info.FullName;
                            item["Mobile"] = info.PhoneNumber;
                            item["Username"] = info.Username;
                            item["Tenchucdanh"] = info.Jobtitle;
                            item["image"] = info.AvartarImgURL;
                        }

                    }
                    //User
                    foreach (DataRow item in User.Rows)
                    {
                        var info = DataAccount.Where(x => item["id_user"].ToString().Contains(x.UserId.ToString())).FirstOrDefault();

                        if (info != null)
                        {
                            item["hoten"] = info.FullName;
                            item["Mobile"] = info.PhoneNumber;
                            item["Username"] = info.Username;
                            item["image"] = info.AvartarImgURL;
                        }

                    }
                    #endregion
                    var htdunghan = ds.Tables[1].AsEnumerable().Count(w => w["is_htdunghan"].ToString() == "1");
                    var htquahan = ds.Tables[1].AsEnumerable().Count(w => w["is_htquahan"].ToString() == "1");
                    var m_htdunghan = ds.Tables[1].AsEnumerable().Count(w => w["Status"].ToString() == "2" && (DateTime)w["end_date"] > (DateTime)w["m_deadline"]);
                    var m_htquahan = ds.Tables[1].AsEnumerable().Count(w => w["Status"].ToString() == "2" && (DateTime)w["end_date"] <= (DateTime)w["m_deadline"]);
                    var data = (from r in ds.Tables[0].AsEnumerable()
                                select new
                                {
                                    id_row = r["id_row"],
                                    title = r["title"],
                                    description = r["description"],
                                    id_project_team = r["id_project_team"],
                                    project_team = r["project_team"],
                                    id_department = r["id_department"],
                                    department = r["department"],
                                    deadline = r["deadline"],
                                    deadline_weekday = r["deadline"].Equals(DBNull.Value) ? "" : Common.GetFormatDate(Convert.ToDateTime(r["deadline"]), "77622"),
                                    deadline_day = r["deadline"].Equals(DBNull.Value) ? "" : Common.GetFormatDate(Convert.ToDateTime(r["deadline"]), "dd/MM"),
                                    CreatedDate = string.Format("{0:dd/MM/yyyy HH:mm}", r["CreatedDate"]),
                                    CreatedBy = r["CreatedBy"],
                                    NguoiTao = r["NguoiTao"],
                                    UpdatedDate = r["UpdatedDate"] == DBNull.Value ? "" : string.Format("{0:dd/MM/yyyy HH:mm}", r["UpdatedDate"]),
                                    UpdatedBy = r["UpdatedBy"],
                                    NguoiSua = r["NguoiSua"],
                                    person_in_charge = r["id_nv"] == DBNull.Value ? null : new
                                    {
                                        id_nv = r["id_nv"],
                                        hoten = r["hoten"],
                                        username = r["username"],
                                        mobile = r["mobile"],
                                        image = r["image"],
                                    },
                                    List = from w in ds.Tables[1].AsEnumerable()
                                           select new
                                           {
                                               is_htquahan = w["is_htquahan"],
                                               is_htdunghan = w["is_htdunghan"],
                                               is_danglam = w["is_danglam"],
                                               is_quahan = w["is_quahan"],
                                               id_row = w["id_row"],
                                               title = w["title"],
                                               description = w["description"],
                                               important = w["important"],
                                               prioritize = w["prioritize"],
                                               urgent = w["urgent"],
                                               clickup_prioritize = w["clickup_prioritize"],
                                               start_date = w["start_date"],
                                               end_date = w["end_date"],
                                               deadline = w["deadline"],
                                               deadline_weekday = w["deadline"].Equals(DBNull.Value) ? "" : Common.GetFormatDate(Convert.ToDateTime(w["deadline"]), "77622"),
                                               deadline_day = w["deadline"].Equals(DBNull.Value) ? "" : Common.GetFormatDate(Convert.ToDateTime(w["deadline"]), "dd/MM"),
                                               user = (from us in User.AsEnumerable()
                                                      where w["id_row"].Equals(us["id_work"])
                                                      select new
                                                      {
                                                          id_nv = us["id_user"],
                                                          hoten = us["hoten"],
                                                          username = us["username"],
                                                          mobile = us["mobile"],
                                                          image = us["image"]
                                                      }).ToList(),
                                           },
                                    Count = new
                                    {
                                        tong = ds.Tables[1].Rows.Count,
                                        htdunghan = htdunghan,
                                        htquahan = htquahan,
                                        quahan = ds.Tables[1].AsEnumerable().Count(w => w["is_quahan"].ToString() == "1"),
                                        danglam = ds.Tables[1].AsEnumerable().Count(w => w["is_danglam"].ToString() == "1"),
                                        percentage = WeworkLiteController.calPercentage(ds.Tables[1].Rows.Count, htdunghan + htquahan)
                                    },
                                    CountByMucTieu = new
                                    {
                                        tong = ds.Tables[1].Rows.Count,
                                        htdunghan = m_htdunghan,
                                        htquahan = m_htquahan,
                                        quahan = ds.Tables[1].AsEnumerable().Count(w => w["Status"].ToString() == "1" && DateTime.Now > (DateTime)w["m_deadline"]),
                                        danglam = ds.Tables[1].AsEnumerable().Count(w => w["Status"].ToString() == "1" && DateTime.Now <= (DateTime)w["m_deadline"]),
                                        percentage = WeworkLiteController.calPercentage(ds.Tables[1].Rows.Count, htdunghan + htquahan)
                                    },
                                    //Count = new
                                    //{
                                    //    tong = ds.Tables[1].Rows[0]["tong"],
                                    //    htdunghan = ds.Tables[1].Rows[0]["htdunghan"],
                                    //    htquahan = ds.Tables[1].Rows[0]["htquahan"],
                                    //    quahan = ds.Tables[1].Rows[0]["quahan"],
                                    //    danglam = ds.Tables[1].Rows[0]["danglam"],
                                    //    percentage = calPercentage(ds.Tables[1].Rows[0]["tong"], ds.Tables[1].Rows[0]["htdunghan"], ds.Tables[1].Rows[0]["htquahan"])
                                    //},
                                    //CountByMucTieu = new
                                    //{
                                    //    tong = ds.Tables[2].Rows[0]["tong"],
                                    //    htdunghan = ds.Tables[2].Rows[0]["htdunghan"],
                                    //    htquahan = ds.Tables[2].Rows[0]["htquahan"],
                                    //    quahan = ds.Tables[2].Rows[0]["quahan"],
                                    //    danglam = ds.Tables[2].Rows[0]["danglam"],
                                    //    percentage = calPercentage(ds.Tables[2].Rows[0]["tong"], ds.Tables[2].Rows[0]["htdunghan"], ds.Tables[2].Rows[0]["htquahan"])
                                    //}
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
        public async Task<object> Insert(MilestoneModel data)
        {
            string Token = Common.GetHeader(Request);
            UserJWT loginData = Ulities.GetUserByHeader(HttpContext.Request.Headers);
            if (loginData == null)
                return JsonResultCommon.DangNhap();
            try
            {
                string strRe = "";
                if (string.IsNullOrEmpty(data.title))
                    strRe += (strRe == "" ? "" : ",") + "tên mục tiêu";
                if (data.id_project_team <= 0)
                    strRe += (strRe == "" ? "" : ",") + "dự án/phòng ban";
                if (data.deadline == DateTime.MinValue)
                    strRe += (strRe == "" ? "" : ",") + "ngày cột mốc";
                if (data.person_in_charge <= 0)
                    strRe += (strRe == "" ? "" : ",") + "người phụ trách";
                if (strRe != "")
                    return JsonResultCommon.BatBuoc(strRe);

                string ConnectionString = WeworkLiteController.getConnectionString(ConnectionCache, loginData.CustomerID, _configuration);
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
                    val.Add("deadline", data.deadline);
                    val.Add("person_in_charge", data.person_in_charge);
                    val.Add("CreatedDate", DateTime.Now);
                    val.Add("CreatedBy", iduser);
                    string strCheck = "select count(*) from we_milestone where Disabled=0 and  (id_project_team=@id_project_team) and title=@name";
                    if (int.Parse(cnn.ExecuteScalar(strCheck, new SqlConditions() { { "id_project_team", data.id_project_team }, { "name", data.title } }).ToString()) > 0)
                    {
                        return JsonResultCommon.Custom("Mục tiêu đã tồn tại trong dự án/phòng ban");
                    }
                    cnn.BeginTransaction();
                    if (cnn.Insert(val, "we_milestone") != 1)
                    {
                        cnn.RollbackTransaction();
                        return JsonResultCommon.Exception(_logger,cnn.LastError, _config, loginData,ControllerContext);
                    }
                    //string LogContent = "", LogEditContent = "";
                    //LogContent = LogEditContent = "Thêm mới dữ liệu milestone: title=" + data.title + ", id_project_team=" + data.id_project_team + ", description=" + data.description + ", deadline=" + data.deadline + ",person_in_charge=" + data.person_in_charge;
                    //DpsPage.Ghilogfile(loginData.CustomerID.ToString(), LogEditContent, LogContent, loginData.UserName);
                    string idc = cnn.ExecuteScalar("select IDENT_CURRENT('we_milestone')").ToString();
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
        public async Task<BaseModel<object>> Update(MilestoneModel data)
        {
            string Token = Common.GetHeader(Request);
            UserJWT loginData = Ulities.GetUserByHeader(HttpContext.Request.Headers);
            if (loginData == null)
                return JsonResultCommon.DangNhap();
            try
            {
                string strRe = "";
                if (string.IsNullOrEmpty(data.title))
                    strRe += (strRe == "" ? "" : ",") + "tên mục tiêu";
                if (data.id_project_team <= 0)
                    strRe += (strRe == "" ? "" : ",") + "dự án/phòng ban";
                if (data.deadline == DateTime.MinValue)
                    strRe += (strRe == "" ? "" : ",") + "ngày cột mốc";
                if (data.person_in_charge <= 0)
                    strRe += (strRe == "" ? "" : ",") + "người phụ trách";
                if (strRe != "")
                    return JsonResultCommon.BatBuoc(strRe);
                string ConnectionString = WeworkLiteController.getConnectionString(ConnectionCache, loginData.CustomerID, _configuration);
                using (DpsConnection cnn = new DpsConnection(ConnectionString))
                {
                    SqlConditions sqlcond = new SqlConditions();
                    sqlcond.Add("id_row", data.id_row);
                    sqlcond.Add("disabled", 0);
                    string s = "select * from we_milestone where (where)";
                    DataTable old = cnn.CreateDataTable(s, "(where)", sqlcond);
                    if (old == null || old.Rows.Count == 0)
                        return JsonResultCommon.KhongTonTai("Mục tiêu");
                    long iduser = loginData.UserID;
                    long idk = loginData.CustomerID;
                    Hashtable val = new Hashtable();
                    val.Add("title", data.title);
                    if (string.IsNullOrEmpty(data.description))
                        val.Add("description", "");
                    else
                        val.Add("description", data.description);
                    val.Add("id_project_team", data.id_project_team);
                    val.Add("deadline", data.deadline);
                    val.Add("person_in_charge", data.person_in_charge);
                    val.Add("UpdatedDate", DateTime.Now);
                    val.Add("UpdatedBy", iduser);
                    string strCheck = "select count(*) from we_milestone where Disabled=0 and  (id_project_team=@id_project_team) and title=@name and id_row<>@id";
                    if (int.Parse(cnn.ExecuteScalar(strCheck, new SqlConditions() { { "id_project_team", data.id_project_team }, { "name", data.title }, { "id", data.id_row } }).ToString()) > 0)
                    {
                        return JsonResultCommon.Custom("Mục tiêu đã tồn tại trong dự án/phòng ban");
                    }
                    cnn.BeginTransaction();
                    if (cnn.Update(val, sqlcond, "we_milestone") != 1)
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
                    //    LogContent = "Chỉnh sửa dữ liệu milestone (" + data.id_row + "), Chi tiết xem trong log chỉnh sửa chức năng";
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
                    string sqlq = "select ISNULL((select count(*) from we_milestone where Disabled=0 and  id_row = " + id + "),0)";
                    if (long.Parse(cnn.ExecuteScalar(sqlq).ToString()) != 1)
                        return JsonResultCommon.KhongTonTai("Mục tiêu");
                    if (Common.TestDuplicate("", id.ToString(), "-1", "we_work", "id_milestone", "Disabled", "0", cnn, "", true) == false)
                    {
                        return JsonResultCommon.Custom("Đang có công việc thuộc mục tiêu này nên không thể xóa");
                    }
                    sqlq = "update we_milestone set Disabled=1, UpdatedDate=getdate(), UpdatedBy=" + iduser + " where id_row = " + id;
                    cnn.BeginTransaction();
                    if (cnn.ExecuteNonQuery(sqlq) != 1)
                    {
                        cnn.RollbackTransaction();
                        return JsonResultCommon.Exception(_logger,cnn.LastError, _config, loginData,ControllerContext);
                    }
                    //string LogContent = "Xóa dữ liệu milestone (" + id + ")";
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
    }
}