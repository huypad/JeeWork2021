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

        public DepartmentController(IOptions<JeeWorkConfig> config, IHostingEnvironment hostingEnvironment)
        {
            _hostingEnvironment = hostingEnvironment;
            _config = config.Value;
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
            PageModel pageModel = new PageModel();
            try
            {
                bool Visible = Common.CheckRoleByToken(loginData.UserID.ToString(), "3400");
                Visible = true;
                #region Lấy dữ liệu account từ JeeAccount
                DataAccount = WeworkLiteController.GetAccountFromJeeAccount(HttpContext.Request.Headers, _config);
                if (DataAccount == null)
                    return JsonResultCommon.Custom("Lỗi lấy danh sách nhân viên từ hệ thống quản lý tài khoản");
                string error = "";
                string listID = WeworkLiteController.ListAccount(HttpContext.Request.Headers, out error, _config);
                if (error != "")
                    return JsonResultCommon.Custom(error);
                #endregion
                using (DpsConnection cnn = new DpsConnection(_config.ConnectionString))
                {
                    SqlConditions Conds = new SqlConditions();
                    string dieukienSort = "title", dieukien_where = " de.Disabled=0 and (IdKH = @CustemerID)";
                    if (!string.IsNullOrEmpty(query.filter["keyword"]))
                    {
                        dieukien_where += " and (title like '%@keyword%') ";
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
                    dieukien_where += $"and de.CreatedBy in ({listID}) ";
                   string sqlq = @$"select de.*, '' as NguoiTao, '' as TenNguoiTao, '' as NguoiSua, '' as TenNguoiSua 
                                    from we_department de (admin) " + dieukien_where +$"  order by " + dieukienSort;
                    if (!Visible)
                    {
                        sqlq = sqlq.Replace("(admin)", "left join we_department_owner do on de.id_row = do.id_department " +
                            "where de.Disabled = 0 and (do.id_user = " + loginData.UserID + " " +
                            "or de.id_row in (select distinct p1.id_department from we_project_team p1 join we_project_team_user pu on p1.id_row = pu.id_project_team " +
                            "where p1.Disabled = 0 and id_user = " + loginData.UserID + ")) and de.Disabled = 0 ");
                    }
                    else
                        sqlq = sqlq.Replace("(admin)", " where ");
                    DataTable dt = cnn.CreateDataTable(sqlq, Conds);
                    if (cnn.LastError != null || dt == null)
                        return JsonResultCommon.Exception(cnn.LastError, _config, loginData.CustomerID, ControllerContext);
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
                    dt = dt.AsEnumerable().Skip((query.page - 1) * query.record).Take(query.record).CopyToDataTable();
                    var data = from r in dt.AsEnumerable()
                               select new
                               {
                                   id_row = r["id_row"],
                                   title = r["title"],
                                   id_cocau = r["id_cocau"],
                                   CreatedDate = string.Format("{0:dd/MM/yyyy HH:mm}", r["CreatedDate"]),
                                   CreatedBy = r["CreatedBy"],
                                   NguoiTao = r["NguoiTao"],
                                   UpdatedDate = r["UpdatedDate"] == DBNull.Value ? "" : string.Format("{0:dd/MM/yyyy HH:mm}", r["UpdatedDate"]),
                                   UpdatedBy = r["UpdatedBy"],
                                   NguoiSua = r["NguoiSua"]
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
                bool Visible = Common.CheckRoleByToken(Token, "3403");
                PageModel pageModel = new PageModel();
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
                    // update later
                    #region Trả dữ liệu về backend để hiển thị lên giao diện
                    // left join {_config.HRCatalog}.dbo.Tbl_Cocautochuc cc on cc.RowID=we_department.id_cocau
                    string sqlq = @$"select we_department.*, null as TenCoCau, '' as NguoiTao, '' as NguoiSua from we_department 
                                    where Disabled=0 and  we_department.CreatedBy in ({listID}) and id_row=" + id;
                    sqlq += @$"; select own.id_row,own.id_user as id_nv, '' as hoten, '' as mobile, '' as Username,own.Type as type from we_department_owner own
                                where disabled=0 and own.id_user in ({listID}) and id_department=" + id;
                    sqlq += @$";select id_department, de.id_row as id_view_de, viewid, view_name, de.is_default as default_view, icon, _view.is_default 
                                from we_department_view de join we_default_views _view
                                on de.viewid = _view.id_row 
                                where de.disabled = 0 and id_department=" + id;
                    sqlq += @$";select de.id_row, cus.id_row, IsDefault, Color, de.TemplateID, cus.Title
                                from we_template_customer cus join we_department de on cus.id_row = de.TemplateID
                                where cus.disabled = 0 and de.disabled = 0 and de.id_row=" + id;
                    sqlq += @$";SELECT _status.Id_row, StatusID, _status.TemplateID, StatusName, description,Type, IsDefault, color, Position, IsFinal, IsDeadline, IsTodo, 
                                Follower
                                from we_template_status _status join we_department de on de.TemplateID = _status.TemplateID
                                where de.disabled = 0 and de.id_row=" + id;
                    DataSet ds = cnn.CreateDataSet(sqlq);
                    if (cnn.LastError != null || ds == null)
                    {
                        return JsonResultCommon.Exception(cnn.LastError, _config, loginData.CustomerID, ControllerContext);
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
                        }
                    }
                    #endregion
                    var data = (from r in ds.Tables[0].AsEnumerable()
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
                                                 image = WeworkLiteController.genLinkImage(domain, loginData.CustomerID, rr["id_nv"].ToString(), _hostingEnvironment.ContentRootPath)
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
                                               }
                                }).FirstOrDefault();

                    return JsonResultCommon.ThanhCong(data, pageModel, Visible);
                }
                #endregion
            }
            catch (Exception ex)
            {
                return JsonResultCommon.Exception(ex, _config, loginData.CustomerID);
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
                using (DpsConnection cnn = new DpsConnection(_config.ConnectionString))
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
                    string strCheck = "select count(*) from we_department where Disabled=0 and (IdKH=@custemerid) and title=@name";
                    if (int.Parse(cnn.ExecuteScalar(strCheck, new SqlConditions() { { "custemerid", idk }, { "name", data.title } }).ToString()) > 0)
                    {
                        return JsonResultCommon.Trung("Ban");
                    }
                    cnn.BeginTransaction();
                    if (cnn.Insert(val, "we_department") != 1)
                    {
                        cnn.RollbackTransaction();
                        return JsonResultCommon.Exception(cnn.LastError, _config, loginData.CustomerID, ControllerContext);
                    }
                    //string LogContent = "", LogEditContent = "";
                    //LogContent = LogEditContent = "Thêm mới dữ liệu department: title=" + data.title + ", id_cocau=" + data.id_cocau;
                    //DpsPage.Ghilogfile(loginData.CustomerID.ToString(), LogEditContent, LogContent, loginData.UserName);
                    string idc = cnn.ExecuteScalar("select IDENT_CURRENT('we_department')").ToString();
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
                                return JsonResultCommon.Exception(cnn.LastError, _config, loginData.CustomerID, ControllerContext);
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
                                return JsonResultCommon.Exception(cnn.LastError, _config, loginData.CustomerID, ControllerContext);
                            }
                        }
                    }
                    if (data.ReUpdated)
                    {
                        if (data.IsDataStaff_HR)
                        {
                            DataTable dt_Staff_HR = WeworkLiteController.List_Account_HR(data.id_cocau, HttpContext.Request.Headers, _config);
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
                                            return JsonResultCommon.Exception(cnn.LastError, _config, loginData.CustomerID, ControllerContext);
                                        }
                                    }
                                }
                            }
                        }
                    }
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
                using (DpsConnection cnn = new DpsConnection(_config.ConnectionString))
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
                        return JsonResultCommon.Exception(cnn.LastError, _config, loginData.CustomerID, ControllerContext);
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
                                return JsonResultCommon.Exception(cnn.LastError, _config, loginData.CustomerID, ControllerContext);
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
                                    return JsonResultCommon.Exception(cnn.LastError, _config, loginData.CustomerID, ControllerContext);
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
                                return JsonResultCommon.Exception(cnn.LastError, _config, loginData.CustomerID, ControllerContext);
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
                                    return JsonResultCommon.Exception(cnn.LastError, _config, loginData.CustomerID, ControllerContext);
                                }
                            }
                        }
                    }
                    if (data.ReUpdated)
                    {
                        if (data.IsDataStaff_HR) // Lấy dữ liệu nhân viên
                        {
                            DataTable dt_Staff_HR = WeworkLiteController.List_Account_HR(data.id_cocau, HttpContext.Request.Headers, _config);
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
                                            return JsonResultCommon.Exception(cnn.LastError, _config, loginData.CustomerID, ControllerContext);
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
                return JsonResultCommon.Exception(ex, _config, loginData.CustomerID);
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
                using (DpsConnection cnn = new DpsConnection(_config.ConnectionString))
                {
                    string sqlq = "select ISNULL((select count(*) from we_department where Disabled=0 and  id_row = " + id + "),0)";
                    if (long.Parse(cnn.ExecuteScalar(sqlq).ToString()) != 1)
                        return JsonResultCommon.KhongTonTai("Ban");
                    //if (Common.TestDuplicate("", id.ToString(), "-1", "we_project_team", "id_department", "Disabled", "0", cnn, "", true) == false)
                    //    return JsonResultCommon.Custom("Đang có dự án, phòng ban thuộc ban này nên không thể xóa");
                    sqlq = "update we_department set Disabled=1, UpdatedDate=getdate(), UpdatedBy=" + iduser + " where id_row = " + id;
                    cnn.BeginTransaction();
                    if (cnn.ExecuteNonQuery(sqlq) != 1)
                    {
                        cnn.RollbackTransaction();
                        return JsonResultCommon.Exception(cnn.LastError, _config, loginData.CustomerID, ControllerContext);
                    }
                    //string LogContent = "Xóa dữ liệu department (" + id + ")";
                    //DpsPage.Ghilogfile(loginData.CustomerID.ToString(), LogContent, LogContent, loginData.UserName);
                    cnn.EndTransaction();
                    return JsonResultCommon.ThanhCong();
                }
            }
            catch (Exception ex)
            {
                return JsonResultCommon.Exception(ex, _config, loginData.CustomerID);
            }
        }
    }
}