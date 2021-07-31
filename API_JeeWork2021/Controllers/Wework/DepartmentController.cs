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
    [Route("api/department")]
    [EnableCors("JeeWorkPolicy")]
    /// <summary>
    /// api quản lý we_authorize
    /// </summary>
    // [EnableCors(origins: "*", headers: "*", methods: "*")]
    public class DepartmentController : ControllerBase
    {
        private readonly IHostingEnvironment _hostingEnvironment;
        private JeeWorkConfig _config;
        public List<AccUsernameModel> DataAccount;
        private IConnectionCache ConnectionCache;
        private IConfiguration _configuration;
        private readonly ILogger<DepartmentController> _logger;

        public DepartmentController(IOptions<JeeWorkConfig> config, IHostingEnvironment hostingEnvironment, IConnectionCache _cache, IConfiguration configuration, ILogger<DepartmentController> logger)
        {
            _hostingEnvironment = hostingEnvironment;
            _config = config.Value;
            ConnectionCache = _cache;
            _configuration = configuration;
            _logger = logger;

        }
        //[CusAuthorize(Roles = "3400")]
        [Route("List")]
        [HttpGet]
        public object List([FromQuery] QueryParams query)
        {
            UserJWT loginData = Ulities.GetUserByHeader(HttpContext.Request.Headers);
            if (loginData == null)
                return JsonResultCommon.DangNhap();
            if (query == null)
                query = new QueryParams();
            string sqlq = "";
            DataTable dt_folder = new DataTable();
            PageModel pageModel = new PageModel();
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
                string ConnectionString = WeworkLiteController.getConnectionString(ConnectionCache, loginData.CustomerID, _configuration);
                using (DpsConnection cnn = new DpsConnection(ConnectionString))
                {
                    bool Visible = Common.CheckRoleByUserID(loginData, 3400, cnn);
                    //WeworkLiteController.Insert_Template(cnn, loginData.CustomerID.ToString());
                    SqlConditions Conds = new SqlConditions();
                    string dieukienSort = "title", dieukien_where = "de.disabled=0 and (idkh = @CustemerID)";
                    if (!string.IsNullOrEmpty(query.filter["keyword"]))
                    {
                        dieukien_where += " and (title like N'%@keyword%') ";
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
                    Conds.Add("CustemerID", loginData.CustomerID);
                    #endregion
                    if (!string.IsNullOrEmpty(query.sortField) && sortableFields.ContainsKey(query.sortField))
                        dieukienSort = sortableFields[query.sortField] + ("desc".Equals(query.sortOrder) ? " desc" : " asc");
                    #region Trả dữ liệu về backend để hiển thị lên giao diện
                    sqlq = @$"select de.*, '' as NguoiTao, '' as TenNguoiTao, '' as NguoiSua, '' as TenNguoiSua,
                                    statuslistid, templateid, IsDataStaff_HR, parentid, phanloaiid
                                    from we_department de (admin) " + dieukien_where + $"  order by " + dieukienSort;
                    if (!Visible)
                    {
                        Conds.Add("UserID", loginData.UserID);
                        sqlq = sqlq.Replace("(admin)", "left join we_department_owner do on de.id_row = do.id_department " +
                            "where de.Disabled = 0 and (do.id_user =  @UserID or de.CreatedBy = @UserID or de.id_row in (select distinct p1.id_department from we_project_team p1 join we_project_team_user pu on p1.id_row = pu.id_project_team " +
                            "where p1.Disabled = 0 and id_user = @UserID )) and ");
                    }
                    else
                        sqlq = sqlq.Replace("(admin)", " where ");
                    DataTable dt = cnn.CreateDataTable(sqlq, Conds);

                    if (cnn.LastError != null || dt == null)
                        return JsonResultCommon.Exception(_logger, cnn.LastError, _config, loginData, ControllerContext);
                    if (dt.Rows.Count == 0)
                        return JsonResultCommon.ThanhCong(new List<string>(), pageModel, Visible);
                    var temp = dt.AsEnumerable();
                    #region filter
                    //if (string.IsNullOrEmpty(query.filter["title"]))
                    //    temp = temp.Where(x => x["title"].ToString().Contains(query.filter["title"]));
                    //if (string.IsNullOrEmpty(query.filter["NguoiTao"]))
                    //    temp = temp.Where(x => x["NguoiTao"].ToString().Contains(query.filter["NguoiTao"]));
                    //if (string.IsNullOrEmpty(query.filter["TenNguoiTao"]))
                    //    temp = temp.Where(x => x["TenNguoiTao"].ToString().Contains(query.filter["TenNguoiTao"]));
                    //if (string.IsNullOrEmpty(query.filter["NguoiSua"]))
                    //    temp = temp.Where(x => x["NguoiSua"].ToString().Contains(query.filter["NguoiSua"]));
                    //if (string.IsNullOrEmpty(query.filter["TenNguoiSua"]))
                    //    temp = temp.Where(x => x["TenNguoiSua"].ToString().Contains(query.filter["TenNguoiSua"]));
                    #endregion

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
                    string icon_folder = "fa fa-folder-open";
                    string icon_space = "fa fa-space-shuttle";
                    dt = dt.AsEnumerable().Skip((query.page - 1) * query.record).Take(query.record).CopyToDataTable();
                    var data = from r in dt.AsEnumerable()
                               where DBNull.Value.Equals(r["parentid"])
                               select new
                               {
                                   id_row = r["id_row"],
                                   title = r["title"],
                                   id_cocau = r["id_cocau"],
                                   CreatedDate = string.Format("{0:dd/MM/yyyy HH:mm}", r["CreatedDate"]),
                                   CreatedBy = r["CreatedBy"],
                                   NguoiTao = r["TenNguoiTao"],
                                   UpdatedDate = r["UpdatedDate"] == DBNull.Value ? "" : string.Format("{0:dd/MM/yyyy HH:mm}", r["UpdatedDate"]),
                                   UpdatedBy = r["UpdatedBy"],
                                   NguoiSua = r["NguoiSua"],
                                   parentid = r["parentid"],
                                   templateid = r["templateid"],
                                   phanloaiid = r["phanloaiid"],
                                   icon = icon_space,
                                   data_folder = from s in dt.AsEnumerable()
                                                 where s["parentid"].ToString() == r["id_row"].ToString() && s["parentid"] != DBNull.Value
                                                 select new
                                                 {
                                                     id_row = s["id_row"],
                                                     title = s["title"],
                                                     id_cocau = s["id_cocau"],
                                                     CreatedDate = string.Format("{0:dd/MM/yyyy HH:mm}", s["CreatedDate"]),
                                                     CreatedBy = s["CreatedBy"],
                                                     NguoiTao = s["TenNguoiTao"],
                                                     UpdatedDate = s["UpdatedDate"] == DBNull.Value ? "" : string.Format("{0:dd/MM/yyyy HH:mm}", s["UpdatedDate"]),
                                                     UpdatedBy = s["UpdatedBy"],
                                                     NguoiSua = s["NguoiSua"],
                                                     parentid = s["parentid"],
                                                     templateid = s["templateid"],
                                                     phanloaiid = s["phanloaiid"],
                                                     icon = icon_folder,
                                                 },
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
                #region Lấy dữ liệu account từ JeeAccount
                DataAccount = WeworkLiteController.GetAccountFromJeeAccount(HttpContext.Request.Headers, _configuration);
                if (DataAccount == null)
                    return JsonResultCommon.Custom("Lỗi lấy danh sách nhân viên từ hệ thống quản lý tài khoản");

                string error = "";
                string listID = WeworkLiteController.ListAccount(HttpContext.Request.Headers, out error, _configuration);
                if (error != "")
                    return JsonResultCommon.Custom(error);
                #endregion
                PageModel pageModel = new PageModel();
                string domain = _configuration.GetValue<string>("Host:JeeWork_API") + "/";
                string ConnectionString = WeworkLiteController.getConnectionString(ConnectionCache, loginData.CustomerID, _configuration);
                using (DpsConnection cnn = new DpsConnection(ConnectionString))
                {
                    bool Visible = Common.CheckRoleByUserID(loginData, 3403, cnn);
                    // update later
                    #region Trả dữ liệu về backend để hiển thị lên giao diện
                    // left join {_config.HRCatalog}.dbo.Tbl_Cocautochuc cc on cc.RowID=we_department.id_cocau
                    string sqlq = @$"select we_department.*, null as TenCoCau, '' as NguoiTao, '' as NguoiSua
                                    from we_department 
                                    where disabled=0 and (id_row=" + id + " or parentid = " + id + ") " +
                                    "order by ParentID";
                    sqlq += @$"; select own.id_row,own.id_user as id_nv, '' as hoten, '' as mobile, '' as Username,'' as image,own.Type as type from we_department_owner own
                                where disabled=0 and own.id_user in ({listID}) and id_department=" + id;
                    sqlq += @$";select id_department, de.id_row as id_view_de, viewid, view_name, de.is_default as default_view, icon, _view.is_default 
                                from we_department_view de join we_default_views _view
                                on de.viewid = _view.id_row 
                                where de.disabled = 0 and id_department=" + id;
                    sqlq += @$";select de.id_row, cus.id_row, IsDefault, Color, de.TemplateID, cus.Title
                                from we_template_customer cus join we_department de on cus.id_row = de.TemplateID
                                where cus.disabled = 0 and de.disabled = 0 and de.id_row=" + id;
                    sqlq += @$";select _status.Id_row, StatusID, _status.TemplateID, StatusName, description,Type, IsDefault, color, Position, IsFinal, IsDeadline, IsTodo, 
                                Follower
                                from we_template_status _status join we_department de on de.TemplateID = _status.TemplateID
                                where de.disabled = 0 and de.id_row=" + id;

                    DataSet ds = cnn.CreateDataSet(sqlq);
                    if (cnn.LastError != null || ds == null)
                    {
                        return JsonResultCommon.Exception(_logger, cnn.LastError, _config, loginData, ControllerContext);
                    }
                    if (ds.Tables[0] == null || ds.Tables[0].Rows.Count == 0)
                        return JsonResultCommon.KhongTonTai();

                    #region Map info account từ JeeAccount

                    foreach (DataRow item in ds.Tables[0].Rows)
                    {
                        var infoNguoiTao = DataAccount.Where(x => item["CreatedBy"].ToString().Contains(x.UserId.ToString())).FirstOrDefault();
                        var infoNguoiSua = DataAccount.Where(x => item["UpdatedBy"].ToString().Contains(x.UserId.ToString())).FirstOrDefault();

                        if (infoNguoiTao != null)
                        {
                            item["NguoiTao"] = infoNguoiTao.Username;
                        }
                        if (infoNguoiSua != null)
                        {
                            item["NguoiSua"] = infoNguoiSua.Username;
                        }
                    }
                    //table 1
                    foreach (DataRow item in ds.Tables[1].Rows)
                    {
                        var info = DataAccount.Where(x => item["id_nv"].ToString().Contains(x.UserId.ToString())).FirstOrDefault();

                        if (info != null)
                        {
                            item["hoten"] = info.FullName;
                            item["mobile"] = info.PhoneNumber;
                            item["Username"] = info.Username;
                            item["image"] = info.AvartarImgURL;
                        }
                    }
                    #endregion
                    var data = (from r in ds.Tables[0].AsEnumerable()
                                where (DBNull.Value.Equals(r["parentid"]) || ds.Tables[0].Rows.Count == 1)
                                select new
                                {
                                    id_row = r["id_row"],
                                    title = r["title"],
                                    id_cocau = r["id_cocau"],
                                    TenCoCau = r["TenCoCau"],
                                    CreatedDate = string.Format("{0:dd/MM/yyyy HH:mm}", r["CreatedDate"]),
                                    CreatedBy = r["CreatedBy"],
                                    NguoiTao = r["NguoiTao"],
                                    UpdatedDate = r["UpdatedDate"] == DBNull.Value ? "" : string.Format("{0:dd/MM/yyyy HH:mm}", r["UpdatedDate"]),
                                    UpdatedBy = r["UpdatedBy"],
                                    NguoiSua = r["NguoiSua"],
                                    viewid = r["TemplateID"],
                                    ParentID = r["ParentID"],
                                    parentid = r["parentid"],
                                    Owners = from rr in ds.Tables[1].AsEnumerable()
                                             select new
                                             {
                                                 id_row = rr["id_row"],
                                                 id_user = rr["id_nv"],
                                                 id_nv = rr["id_nv"],
                                                 hoten = rr["hoten"],
                                                 Username = rr["Username"],
                                                 mobile = rr["mobile"],
                                                 type = rr["type"],
                                                 image = rr["image"],
                                             },
                                    DefaultView = from view in ds.Tables[2].AsEnumerable()
                                                  select new
                                                  {
                                                      id_department = view["id_department"],
                                                      id_view_de = view["id_view_de"],
                                                      viewid = view["viewid"],
                                                      view_name = view["view_name"],
                                                      default_view = view["default_view"],
                                                      icon = view["icon"],
                                                      is_default = view["is_default"],
                                                  },
                                    Template = from view in ds.Tables[3].AsEnumerable()
                                               select new
                                               {
                                                   id_department = view["id_row"],
                                                   IsDefault = view["IsDefault"],
                                                   Color = view["Color"],
                                                   Title = view["Title"],
                                                   TemplateID = view["TemplateID"],
                                                   Status = from _status in ds.Tables[4].AsEnumerable()
                                                            where _status["TemplateID"].ToString() == view["TemplateID"].ToString()
                                                            select new
                                                            {
                                                                Id_row = _status["Id_row"],
                                                                StatusID = _status["StatusID"],
                                                                TemplateID = _status["TemplateID"],
                                                                StatusName = _status["StatusName"],
                                                                IsDefault = _status["IsDefault"],
                                                                color = _status["color"],
                                                                Position = _status["Position"],
                                                                IsFinal = _status["IsFinal"],
                                                                IsDeadline = _status["IsDeadline"],
                                                                IsTodo = _status["IsTodo"],
                                                                Follower = _status["Follower"],
                                                            },
                                               },
                                    data_folder = from s in ds.Tables[0].AsEnumerable()
                                                  where s["parentid"].ToString() == r["id_row"].ToString() && s["parentid"] != DBNull.Value
                                                  select new
                                                  {
                                                      id_row = s["id_row"],
                                                      title = s["title"],
                                                      id_cocau = s["id_cocau"],
                                                      CreatedDate = string.Format("{0:dd/MM/yyyy HH:mm}", s["CreatedDate"]),
                                                      CreatedBy = s["CreatedBy"],
                                                      NguoiTao = s["NguoiTao"],
                                                      UpdatedDate = s["UpdatedDate"] == DBNull.Value ? "" : string.Format("{0:dd/MM/yyyy HH:mm}", s["UpdatedDate"]),
                                                      UpdatedBy = s["UpdatedBy"],
                                                      NguoiSua = s["NguoiSua"],
                                                      parentid = s["parentid"],
                                                      templateid = s["templateid"],
                                                      phanloaiid = s["phanloaiid"],
                                                      //icon = icon_folder,
                                                  },
                                }).FirstOrDefault();

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
        /// 
        [CusAuthorize(Roles = "3402")]
        [Route("Insert")]
        [HttpPost]
        public async Task<object> Insert(DepartmentModel data)
        {
            string Token = Common.GetHeader(Request);
            UserJWT loginData = Ulities.GetUserByHeader(HttpContext.Request.Headers);
            if (loginData == null)
                return JsonResultCommon.DangNhap();
            try
            {
                string strRe = "";
                if (string.IsNullOrEmpty(data.title))
                    strRe += (strRe == "" ? "" : ",") + "tên ban";
                //if (data.id_cocau <= 0)
                //    strRe += (strRe == "" ? "" : ",") + "cơ cấu tổ chức";
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
                    val.Add("title", data.title);
                    val.Add("id_cocau", data.id_cocau);
                    val.Add("IdKH", idk);
                    val.Add("CreatedDate", DateTime.Now);
                    val.Add("CreatedBy", iduser);
                    val.Add("IsDataStaff_HR", data.IsDataStaff_HR);
                    val.Add("TemplateID", data.TemplateID);
                    if (data.ParentID > 0)
                    {
                        val.Add("ParentID", data.ParentID);
                    }
                    string strCheck = "select count(*) from we_department where Disabled=0 and (IdKH=@custemerid) and title=@name";
                    if (int.Parse(cnn.ExecuteScalar(strCheck, new SqlConditions() { { "custemerid", idk }, { "name", data.title } }).ToString()) > 0)
                    {
                        return JsonResultCommon.Trung("Ban");
                    }
                    cnn.BeginTransaction();
                    if (cnn.Insert(val, "we_department") != 1)
                    {
                        cnn.RollbackTransaction();
                        return JsonResultCommon.Exception(_logger, cnn.LastError, _config, loginData, ControllerContext);
                    }
                    //string LogContent = "", LogEditContent = "";
                    //LogContent = LogEditContent = "Thêm mới dữ liệu department: title=" + data.title + ", id_cocau=" + data.id_cocau;
                    //DpsPage.Ghilogfile(loginData.CustomerID.ToString(), LogEditContent, LogContent, loginData.UserName);
                    string idc = cnn.ExecuteScalar("select IDENT_CURRENT('we_department')").ToString();
                    if (data.templatecenter == null)
                    {
                        if (data.DefaultView != null)
                        {
                            Hashtable val1 = new Hashtable();
                            val1["id_department"] = idc;
                            val1["CreatedDate"] = DateTime.Now;
                            val1["CreatedBy"] = iduser;
                            foreach (var view in data.DefaultView)
                            {
                                val1["viewid"] = view.id_row;
                                val1["is_default"] = view.is_default;
                                if (cnn.Insert(val1, "we_department_view") != 1)
                                {
                                    cnn.RollbackTransaction();
                                    return JsonResultCommon.Exception(_logger, cnn.LastError, _config, loginData, ControllerContext);
                                }
                            }
                        }
                        if (data.Owners != null)
                        {
                            Hashtable val1 = new Hashtable();
                            val1["id_department"] = idc;
                            val1["CreatedDate"] = DateTime.Now;
                            val1["CreatedBy"] = iduser;
                            foreach (var owner in data.Owners)
                            {
                                val1["id_user"] = owner.id_user;
                                val1["type"] = owner.type;
                                if (cnn.Insert(val1, "we_department_owner") != 1)
                                {
                                    cnn.RollbackTransaction();
                                    return JsonResultCommon.Exception(_logger, cnn.LastError, _config, loginData, ControllerContext);
                                }
                            }
                        }
                        if (data.ReUpdated)
                        {
                            if (data.IsDataStaff_HR)
                            {
                                DataTable dt_Staff_HR = WeworkLiteController.List_Account_HR(data.id_cocau, HttpContext.Request.Headers, _configuration);
                                if (dt_Staff_HR.Rows.Count > 0)
                                {
                                    foreach (DataRow users in dt_Staff_HR.Rows)
                                    {
                                        string sql_cmd = "select id_user from we_department_owner where type = 2 and id_department = " + data.id_row + " and id_user = " + users["Id_NV"].ToString() + " and Disabled = 0";
                                        string getValue = cnn.ExecuteScalar(sql_cmd).ToString();
                                        if (getValue == null)
                                        {
                                            Hashtable has = new Hashtable();
                                            has["id_department"] = idc;
                                            has["CreatedDate"] = DateTime.Now;
                                            has["CreatedBy"] = iduser;
                                            has["id_user"] = users["Id_NV"].ToString();
                                            has["type"] = 2;
                                            if (cnn.Insert(has, "we_department_owner") != 1)
                                            {
                                                cnn.RollbackTransaction();
                                                return JsonResultCommon.Exception(_logger, cnn.LastError, _config, loginData, ControllerContext);
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                    else
                    {
                        data.templatecenter.ObjectTypesID = long.Parse(idc);
                        //string sql_insert = $@"insert into we_department_owner (id_department,id_user,CreatedDate,CreatedBy,Type)
                        //                    select id_department,id_user,GETDATE(),"+loginData.UserID+ ",Type from we_department_owner where id_department = " + data.templatecenter.id_reference+ "";
                        //cnn.ExecuteNonQuery(sql_insert);
                        //if (cnn.LastError != null)
                        //{
                        //    cnn.RollbackTransaction();
                        //    return JsonResultCommon.Exception(_logger, cnn.LastError, _config, loginData, ControllerContext);
                        //}
                        if (!WeworkLiteController.init_template_center(cnn, data.templatecenter, loginData))
                        {
                            cnn.RollbackTransaction();
                            return false;
                        }
                    }
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
        /// 
        [CusAuthorize(Roles = "3402")]
        [Route("Insert-quick-folder")]
        [HttpPost]
        public async Task<object> InsertQuickFolder(DepartmentModel data)
        {
            string Token = Common.GetHeader(Request);
            UserJWT loginData = Ulities.GetUserByHeader(HttpContext.Request.Headers);
            if (loginData == null)
                return JsonResultCommon.DangNhap();
            try
            {
                string strRe = "";
                if (string.IsNullOrEmpty(data.title))
                    strRe += (strRe == "" ? "" : ",") + "tên thư mục"; 
                if (strRe != "")
                {
                    return JsonResultCommon.BatBuoc(strRe);
                }
                string ConnectionString = WeworkLiteController.getConnectionString(ConnectionCache, loginData.CustomerID, _configuration);
                using (DpsConnection cnn = new DpsConnection(ConnectionString))
                {
                    long iduser = loginData.UserID;
                    long idk = loginData.CustomerID;

                    #region thêm nhanh thư mục
                    //kiểm tra phòng ban
                    string strCheck = "select count(*) from we_department where Disabled=0 and (IdKH=@custemerid) and title=@name";
                    if (int.Parse(cnn.ExecuteScalar(strCheck, new SqlConditions() { { "custemerid", idk }, { "name", data.title } }).ToString()) > 0)
                    {
                        return JsonResultCommon.Trung("Thư mục");
                    }
                    // thêm project thành thư mục
                    SqlConditions conds = new SqlConditions();
                    conds.Add("title", data.title);
                    conds.Add("UserID", loginData.UserID);
                    conds.Add("ParentID", data.ParentID);
                    string sqlq = $@"INSERT INTO [dbo].[we_department]
                                       ([title]
                                       ,[id_cocau]
                                       ,[IdKH]
                                       ,[priority]
                                       ,[CreatedDate]
                                       ,[CreatedBy]
                                       ,[Disabled]
                                       ,[StatusListID]
                                       ,[TemplateID]
                                       ,[ParentID]
                                       ,[phanloaiid])
                            SELECT  @title
                                  ,[id_cocau]
                                  ,[IdKH]
                                  ,[priority]
                                  ,GETDATE()
                                  ,@UserID
                                  ,0
                                  ,[StatusListID]
                                  ,[TemplateID]
                                  ,@ParentID
                                  ,[phanloaiid]
                              FROM [dbo].[we_department] where id_row = @ParentID";

                    cnn.BeginTransaction();
                    if (cnn.ExecuteNonQuery(sqlq,conds) != 1)
                    {
                        cnn.RollbackTransaction();
                        return JsonResultCommon.Exception(_logger, cnn.LastError, _config, loginData, ControllerContext);
                    }
                    string idc = cnn.ExecuteScalar("select IDENT_CURRENT('we_department')").ToString();

                    // thêm view
                    string sqlv = @"select * from we_department_view where id_department = " + data.ParentID;
                    List<long> listView = new List<long>();
                    DataTable dtv = cnn.CreateDataTable(sqlv);
                    if(dtv.Rows.Count == 0)
                    {
                        long viewdf = long.Parse(cnn.ExecuteScalar("select id_row from we_default_views where is_default = 1").ToString());
                        listView.Add(viewdf);
                    }
                    else
                    {
                        foreach(DataRow item in dtv.Rows)
                        {
                            listView.Add(long.Parse(item["viewid"].ToString()));
                        }
                    }

                    Hashtable val1 = new Hashtable();
                    val1["id_department"] = idc;
                    val1["CreatedDate"] = DateTime.Now;
                    val1["CreatedBy"] = iduser;
                    foreach (var view in listView)
                    {
                        val1["viewid"] = view;
                        if (cnn.Insert(val1, "we_department_view") != 1)
                        {
                            cnn.RollbackTransaction();
                            return JsonResultCommon.Exception(_logger, cnn.LastError, _config, loginData, ControllerContext);
                        }
                    }

                    // thêm thành viên 
                    string sqltv = @"select * from we_department_owner where id_department = " + data.ParentID;
                    DataTable dttv = cnn.CreateDataTable(sqltv);
                    if (dtv.Rows.Count == 0)
                    {
                        Hashtable valtv = new Hashtable();
                        valtv["id_department"] = idc;
                        valtv["CreatedDate"] = DateTime.Now;
                        valtv["CreatedBy"] = iduser;
                        valtv["id_user"] = iduser;
                        valtv["type"] = 1;
                        if (cnn.Insert(valtv, "we_department_owner") != 1)
                        {
                            cnn.RollbackTransaction();
                            return JsonResultCommon.Exception(_logger, cnn.LastError, _config, loginData, ControllerContext);
                        }
                    }
                    else
                    {
                        SqlConditions condstv = new SqlConditions();
                        condstv.Add("id_department", idc);
                        condstv.Add("UserID", loginData.UserID);
                        condstv.Add("ParentID", data.ParentID);
                        string sql_insert = @"INSERT INTO [dbo].[we_department_owner]
                                                               ([id_department]
                                                               ,[id_user]
                                                               ,[CreatedDate]
                                                               ,[CreatedBy]
                                                               ,[Disabled]
                                                               ,[Type])
                                                    SELECT @id_department
                                                    ,[id_user]
                                                    ,GETDATE()
                                                    ,@UserID
                                                    ,[Disabled]
                                                    ,[Type]
                                                    FROM [dbo].[we_department_owner]
                                                    where id_department = @ParentID
                                                    and Disabled = 0";
                        if (cnn.ExecuteNonQuery(sql_insert, condstv) <= 0)
                        {
                            cnn.RollbackTransaction();
                            return JsonResultCommon.Exception(_logger, cnn.LastError, _config, loginData, ControllerContext);
                        }
                    }
                    #endregion
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
        [CusAuthorize(Roles = "3402")]
        [Route("Update")]
        [HttpPost]
        public async Task<BaseModel<object>> Update(DepartmentModel data)
        {
            string Token = Common.GetHeader(Request);
            UserJWT loginData = Ulities.GetUserByHeader(HttpContext.Request.Headers);
            if (loginData == null)
                return JsonResultCommon.DangNhap();
            try
            {
                string strRe = "";
                if (string.IsNullOrEmpty(data.title))
                    strRe += (strRe == "" ? "" : ",") + "tên ban";
                //if (data.id_cocau <= 0)
                //    strRe += (strRe == "" ? "" : ",") + "cơ cấu tổ chức";
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
                    string s = "select * from we_department where (where)";
                    DataTable old = cnn.CreateDataTable(s, "(where)", sqlcond);
                    if (old == null || old.Rows.Count == 0)
                        return JsonResultCommon.KhongTonTai("Ban");
                    long iduser = loginData.UserID;
                    long idk = loginData.CustomerID;
                    Hashtable val = new Hashtable();
                    val.Add("title", data.title);
                    val.Add("id_cocau", data.id_cocau);
                    val.Add("TemplateID", data.TemplateID);
                    //val.Add("IdKH", idk);
                    val.Add("UpdatedDate", DateTime.Now);
                    val.Add("UpdatedBy", iduser);
                    string strCheck = "select count(*) from we_department where Disabled=0 and  (IdKH=@custemerid) and title=@name and id_row<>@id";
                    if (int.Parse(cnn.ExecuteScalar(strCheck, new SqlConditions() { { "custemerid", idk }, { "name", data.title }, { "id", data.id_row } }).ToString()) > 0)
                    {
                        return JsonResultCommon.Trung("Ban");
                    }
                    cnn.BeginTransaction();
                    if (cnn.Update(val, sqlcond, "we_department") != 1)
                    {
                        cnn.RollbackTransaction();
                        return JsonResultCommon.Exception(_logger, cnn.LastError, _config, loginData, ControllerContext);
                    }
                    if (data.Owners != null)
                    {
                        string ids = string.Join(",", data.Owners.Where(x => x.id_row > 0).Select(x => x.id_row));
                        if (ids != "")//xóa owner
                        {
                            string strDel = "Update we_department_owner set Disabled=1, UpdatedDate=getdate(), UpdatedBy=" + iduser + " where Disabled=0 and  id_department=" + data.id_row + " and id_row not in (" + ids + ")";
                            if (cnn.ExecuteNonQuery(strDel) < 0)
                            {
                                cnn.RollbackTransaction();
                                return JsonResultCommon.Exception(_logger, cnn.LastError, _config, loginData, ControllerContext);
                            }
                        }
                        Hashtable val1 = new Hashtable();
                        val1["id_department"] = data.id_row;
                        val1["CreatedDate"] = DateTime.Now;
                        val1["CreatedBy"] = iduser;
                        foreach (var owner in data.Owners)
                        {
                            if (owner.id_row == 0)//add owner mới
                            {
                                val1["id_user"] = owner.id_user;
                                val1["Type"] = owner.type;
                                if (cnn.Insert(val1, "we_department_owner") != 1)
                                {
                                    cnn.RollbackTransaction();
                                    return JsonResultCommon.Exception(_logger, cnn.LastError, _config, loginData, ControllerContext);
                                }
                            }
                        }
                    }
                    if (data.DefaultView != null)
                    {
                        string ids = string.Join(",", data.DefaultView.Where(x => x.id_row > 0).Select(x => x.id_row));
                        if (ids != "")//xóa view
                        {
                            string strDel = "Update we_department_view set Disabled=1, UpdatedDate=getdate(), UpdatedBy=" + iduser + " where Disabled=0 and  id_department=" + data.id_row + " and id_row not in (" + ids + ")";
                            if (cnn.ExecuteNonQuery(strDel) < 0)
                            {
                                cnn.RollbackTransaction();
                                return JsonResultCommon.Exception(_logger, cnn.LastError, _config, loginData, ControllerContext);
                            }
                        }
                        Hashtable val1 = new Hashtable();
                        val1["id_department"] = data.id_row;
                        val1["CreatedDate"] = DateTime.Now;
                        val1["CreatedBy"] = iduser;
                        foreach (var _view in data.DefaultView)
                        {
                            if (_view.viewid == 0)//add view mới
                            {
                                val1["viewid"] = _view.id_row;
                                val1["is_default"] = _view.is_default;
                                if (cnn.Insert(val1, "we_department_view") != 1)
                                {
                                    cnn.RollbackTransaction();
                                    return JsonResultCommon.Exception(_logger, cnn.LastError, _config, loginData, ControllerContext);
                                }
                            }
                        }
                    }
                    if (data.ReUpdated)
                    {
                        if (data.IsDataStaff_HR) // Lấy dữ liệu nhân viên
                        {
                            DataTable dt_Staff_HR = WeworkLiteController.List_Account_HR(data.id_cocau, HttpContext.Request.Headers, _configuration);
                            if (dt_Staff_HR.Rows.Count > 0)
                            {
                                foreach (DataRow users in dt_Staff_HR.Rows)
                                {
                                    string sql_cmd = "select id_user from we_department_owner where type = 2 and id_department = " + data.id_row + " and id_user = " + users["Id_NV"].ToString() + " and Disabled = 0";
                                    string getValue = cnn.ExecuteScalar(sql_cmd).ToString();
                                    if (getValue == null)
                                    {
                                        Hashtable has = new Hashtable();
                                        has["id_department"] = data.id_row;
                                        has["CreatedDate"] = DateTime.Now;
                                        has["CreatedBy"] = iduser;
                                        has["id_user"] = users["Id_NV"].ToString();
                                        has["type"] = 2;
                                        if (cnn.Insert(has, "we_department_owner") != 1)
                                        {
                                            cnn.RollbackTransaction();
                                            return JsonResultCommon.Exception(_logger, cnn.LastError, _config, loginData, ControllerContext);
                                        }
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
                    //    LogContent = "Chỉnh sửa dữ liệu department (" + data.id_row + "), Chi tiết xem trong log chỉnh sửa chức năng";
                    //}
                    //DpsPage.Ghilogfile(loginData.CustomerID.ToString(), LogEditContent, LogContent, loginData.UserName);
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
        [CusAuthorize(Roles = "3402")]
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
                    string sqlq = "select ISNULL((select count(*) from we_department where Disabled=0 and  id_row = " + id + "),0)";
                    if (long.Parse(cnn.ExecuteScalar(sqlq).ToString()) != 1)
                        return JsonResultCommon.KhongTonTai("Ban");
                    //if (Common.TestDuplicate("", id.ToString(), "-1", "we_project_team", "id_department", "Disabled", "0", cnn, "", true) == false)
                    //    return JsonResultCommon.Custom("Đang có dự án, phòng ban thuộc ban này nên không thể xóa");
                    sqlq = "update we_department set Disabled=1, UpdatedDate=getdate(), UpdatedBy=" + iduser + " where id_row = " + id + "or ParentID = " + id;
                    cnn.BeginTransaction();
                    if (cnn.ExecuteNonQuery(sqlq) < 1)
                    {
                        cnn.RollbackTransaction();
                        return JsonResultCommon.Exception(_logger, cnn.LastError, _config, loginData, ControllerContext);
                    }
                    //string LogContent = "Xóa dữ liệu department (" + id + ")";
                    //DpsPage.Ghilogfile(loginData.CustomerID.ToString(), LogContent, LogContent, loginData.UserName);
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