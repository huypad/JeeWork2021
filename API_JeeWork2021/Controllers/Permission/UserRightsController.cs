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
using System.Collections.Specialized;
using Newtonsoft.Json;
using DPSinfra.ConnectionCache;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace JeeWork_Core2021.Controllers.Wework
{
    [ApiController]
    [Route("api/ww_userrights")]
    [EnableCors("JeeWorkPolicy")]
    public class WW_UserRightsController : ControllerBase
    {
        private readonly IHostingEnvironment _hostingEnvironment;
        private JeeWorkConfig _config;
        public List<AccUsernameModel> DataAccount;
        private IConnectionCache ConnectionCache;
        private IConfiguration _configuration;
        private readonly ILogger<WW_UserRightsController> _logger;

        public WW_UserRightsController(IOptions<JeeWorkConfig> config, IHostingEnvironment hostingEnvironment, IConnectionCache _cache, IConfiguration configuration, ILogger<WW_UserRightsController> logger)
        {
            _hostingEnvironment = hostingEnvironment;
            _config = config.Value;
            ConnectionCache = _cache;
            _configuration = configuration;
            _logger = logger;
        }
        /// <summary>
        /// Nhóm người dùng ---
        /// Load thông tin nhóm ---
        /// !Visible: chỉ xem, visible tất cả các button thao tác ---
        /// (Cloumn "Xóa").Visible = !IsAdmin ---
        /// </summary>
        /// <param name="query"></param>
        /// <returns></returns>
        [CusAuthorize(Roles = "3900")]
        [Route("Get_DSNhom")]
        [HttpGet]
        public object Get_DSNhom([FromQuery] QueryParams query)
        {
            #region Lấy dữ liệu account từ JeeAccount
            DataAccount = JeeWorkLiteController.GetAccountFromJeeAccount(HttpContext.Request.Headers, _configuration);
            if (DataAccount == null)
                return JsonResultCommon.Custom("Lỗi lấy danh sách nhân viên từ hệ thống quản lý tài khoản");
            #endregion
            UserJWT loginData = Ulities.GetUserByHeader(HttpContext.Request.Headers);
            if (loginData == null)
                return JsonResultCommon.DangNhap();
            query = query == null ? new QueryParams() : query;
            BaseModel<object> model = new BaseModel<object>();
            PageModel pageModel = new PageModel();
            ErrorModel error = new ErrorModel();
            string sqlq = "";
            SqlConditions Conds = new SqlConditions();
            string orderByStr = "isadmin desc, GroupType desc", whereStr = "CustemerID=@CustemerID and (Module=@Module) ";
            string ConnectionString = JeeWorkLiteController.getConnectionString(ConnectionCache, loginData.CustomerID, _configuration);
            DataTable dt = new DataTable();
            bool Visible = true;
            try
            {
                using (DpsConnection cnn = new DpsConnection(ConnectionString))
                {
                    #region khởi tạo dữ liệu quyền cho nhóm quản trị
                        Common.InsertGroupType(cnn, loginData.CustomerID);
                    #endregion
                    Conds.Add("CustemerID", loginData.customdata.jeeAccount.customerID);
                    Conds.Add("Module", query.filter["Module"]);
                    Dictionary<string, string> sortableFields = new Dictionary<string, string>
                        {
                            { "TenNhom", "GroupName"},
                        };
                    if (!string.IsNullOrEmpty(query.sortField) && sortableFields.ContainsKey(query.sortField))
                    {
                        orderByStr = sortableFields[query.sortField] + ("desc".Equals(query.sortOrder) ? " desc" : " asc");
                    }
                    #region Kiểm tra quyền chỉ xem
                    Common permit = new Common(ConnectionString);
                    if (Common.IsReadOnlyPermit("3900", loginData.Username))
                    {
                        Visible = false;
                    }
                    #endregion
                    sqlq = $@"select Id_group, GroupName, isadmin, DateCreated, LastModified, GroupType
                            from Tbl_Group
                            where { whereStr } order by { orderByStr}";
                    dt = cnn.CreateDataTable(sqlq, Conds);
                    if (dt == null || cnn.LastError != null)
                        return JsonResultCommon.Exception(_logger, cnn.LastError, _config, loginData, ControllerContext);
                    int total = dt.Rows.Count;
                    if (total == 0)
                        return JsonResultCommon.ThanhCong(null, pageModel, Visible);
                    if (query.more)
                    {
                        query.page = 1;
                        query.record = pageModel.TotalCount;
                    }
                    pageModel.TotalCount = total;
                    pageModel.AllPage = (int)Math.Ceiling(total / (decimal)query.record);
                    pageModel.Size = query.record;
                    pageModel.Page = query.page;
                }
                // Phân trang
                dt = dt.AsEnumerable().Skip((query.page - 1) * query.record).Take(query.record).CopyToDataTable();
                var data = from r in dt.AsEnumerable()
                           select new
                           {
                               TenNhom = r["GroupName"],
                               ID_Nhom = r["id_group"],
                               IsAdmin = r["Isadmin"],
                               grouptype = r["GroupType"],
                               icon_class = bool.TrueString.Equals(r["Isadmin"].ToString()) ? "fa fa-cog" : (r["GroupType"] != DBNull.Value ? ("1".Equals(r["GroupType"].ToString())? "fa fa-check-double" : "fa fa-check"):""),
                               color_class = bool.TrueString.Equals(r["Isadmin"].ToString()) ? "#82c91e" : (r["GroupType"] != DBNull.Value ? ("1".Equals(r["GroupType"].ToString()) ? "#4dabf7" : "#ffd43b") : ""),
                               tooltip = bool.TrueString.Equals(r["Isadmin"].ToString()) ? "Quản trị hệ thống" : (r["GroupType"] != DBNull.Value ? ("1".Equals(r["GroupType"].ToString()) ? "Nhóm quản trị phòng ban/thư mục" : "Nhóm thành viên") : ""),
                               DateCreated = string.Format("{0:dd/MM/yyyy HH:mm}", r["DateCreated"]),
                               LastModified = r["LastModified"] == DBNull.Value ? "" : string.Format("{0:dd/MM/yyyy HH:mm}", r["LastModified"]),
                           };
                return JsonResultCommon.ThanhCong(data, pageModel, Visible);
            }
            catch (Exception ex)
            {
                return JsonResultCommon.Exception(_logger, ex, _config, loginData);
            }
        }
        /// <summary>
        /// Danh sách người dùng ---
        /// Load danh sách người dùng nhóm ---
        /// !Visible: chỉ xem, visible tất cả các button thao tác
        /// </summary>
        /// <param name="query"></param>
        /// <returns></returns>
        [CusAuthorize(Roles = "3900")]
        [Route("Get_UserGroup")]
        [HttpGet]
        public object Get_DSNguoiDungNhom([FromQuery] QueryParams query)
        {
            UserJWT loginData = Ulities.GetUserByHeader(HttpContext.Request.Headers);
            if (loginData == null)
                return JsonResultCommon.DangNhap();
            query = query == null ? new QueryParams() : query;
            BaseModel<object> model = new BaseModel<object>();
            PageModel pageModel = new PageModel();
            ErrorModel error = new ErrorModel();
            string sqlq = "";
            SqlConditions Conds = new SqlConditions();
            DataTable dt_staff = new DataTable();
            dt_staff = new DataTable();
            dt_staff.Columns.Add("UserId", typeof(int));
            dt_staff.Columns.Add("Username", typeof(string));
            dt_staff.Columns.Add("FirstName", typeof(string));
            dt_staff.Columns.Add("LastName", typeof(string));
            dt_staff.Columns.Add("FullName", typeof(string));
            dt_staff.Columns.Add("Jobtitle", typeof(string));
            dt_staff.Columns.Add("Department", typeof(string));
            dt_staff.Columns.Add("AvartarImgURL", typeof(string));
            dt_staff.Columns.Add("CustomerID", typeof(int));
            string orderByStr = "Username asc", whereStr = $@"Id_group=@ID_Nhom";
            bool Visible = true;
            DataTable dt = new DataTable();
            try
            {

                if (string.IsNullOrEmpty(query.filter["ID_Nhom"]))
                {
                    model.status = 0;
                    error = new ErrorModel();
                    error.message = "Không tìm thấy nhóm";
                    error.code = JeeWorkConstant.ERRORCODE;
                    return JsonResultCommon.ThanhCong(new
                    {
                        status = 0,
                        error = error,
                        data = String.Empty,
                    });
                }
                Conds.Add("ID_Nhom", query.filter["ID_Nhom"]);
                Dictionary<string, string> sortableFields = new Dictionary<string, string>
                        {
                            { "Username", "Username"},
                        };
                if (!string.IsNullOrEmpty(query.sortField) && sortableFields.ContainsKey(query.sortField))
                {
                    orderByStr = sortableFields[query.sortField] + ("desc".Equals(query.sortOrder) ? " desc" : " asc");
                }
                string ConnectionString = JeeWorkLiteController.getConnectionString(ConnectionCache, loginData.CustomerID, _configuration);
                #region Kiểm tra quyền chỉ xem
                Common permit = new Common(ConnectionString);
                if (Common.IsReadOnlyPermit("3900", loginData.Username))
                {
                    Visible = false;
                }
                #endregion
                using (DpsConnection cnn = new DpsConnection(ConnectionString))
                {
                    sqlq = $@"select * from Tbl_Group_Account
                        where { whereStr } order by { orderByStr}";
                    dt = cnn.CreateDataTable(sqlq, Conds);
                    if (dt == null | cnn.LastError != null)
                        return JsonResultCommon.Exception(_logger, cnn.LastError, _config, loginData, ControllerContext);
                    #region Lấy danh sách nhân viên từ JeeAccount
                    DataAccount = JeeWorkLiteController.GetAccountFromJeeAccount(HttpContext.Request.Headers, _configuration);
                    if (DataAccount == null)
                        return JsonResultCommon.Custom("Lỗi lấy danh sách nhân viên từ JeeAccount");
                    DataRow row;
                    foreach (AccUsernameModel str in DataAccount)
                    {
                        row = dt_staff.NewRow();
                        row["UserId"] = str.UserId;
                        row["Username"] = str.Username;
                        row["FirstName"] = str.FirstName;
                        row["LastName"] = str.LastName;
                        row["FullName"] = str.FullName;
                        row["Jobtitle"] = str.Jobtitle;
                        row["Department"] = str.Department;
                        row["AvartarImgURL"] = str.AvartarImgURL;
                        row["CustomerID"] = str.CustomerID;
                        dt_staff.Rows.Add(row);
                    }
                    #endregion
                }
                int total = dt.Rows.Count;
                if (total == 0)
                    return JsonResultCommon.ThanhCong(null, pageModel, Visible);
                pageModel.TotalCount = total;
                pageModel.AllPage = (int)Math.Ceiling(total / (decimal)query.record);
                pageModel.Size = query.record;
                pageModel.Page = query.page;
                pageModel.Page = query.page;
                if (query.more)
                {
                    query.page = 1;
                    query.record = pageModel.TotalCount;
                }
                // Phân trang
                dt = dt.AsEnumerable().Skip((query.page - 1) * query.record).Take(query.record).CopyToDataTable();
                var data = from r in dt.AsEnumerable()
                           from nv in dt_staff.AsEnumerable()
                           where nv["Username"].ToString().Equals(r["Username"].ToString())
                           select new
                           {
                               Username = nv["Username"],
                               ID_NV = nv["UserId"],
                               HoTen = nv["FullName"],
                               ChucVu = nv["Jobtitle"],
                           };
                return JsonResultCommon.ThanhCong(data, pageModel, Visible);
            }
            catch (Exception ex)
            {
                return JsonResultCommon.Exception(_logger, ex, _config, loginData);
            }
        }
        /// <summary>
        /// Nhóm người dùng ---
        /// Thêm nhóm ---
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        [CusAuthorize(Roles = "3900")]
        [Route("Insert_Nhom")]
        [HttpPost]
        public async Task<BaseModel<object>> Insert_Nhom(NhomAddData data)
        {
            UserJWT loginData = Ulities.GetUserByHeader(HttpContext.Request.Headers);
            if (loginData == null)
                return JsonResultCommon.DangNhap();
            SqlConditions Conds = new SqlConditions();
            Hashtable val = new Hashtable();
            try
            {
                if (string.IsNullOrEmpty(data.TenNhom))
                    return JsonResultCommon.BatBuoc("Tên nhóm");
                val.Add("groupname", data.TenNhom);
                val.Add("DateCreated", Common.GetDateTime());
                val.Add("LastModified", Common.GetDateTime());
                val.Add("CustemerID", loginData.CustomerID);
                val.Add("Module", data.Module);
                string ConnectionString = JeeWorkLiteController.getConnectionString(ConnectionCache, loginData.CustomerID, _configuration);
                using (DpsConnection cnn = new DpsConnection(ConnectionString))
                {
                    string sqlq = $@"select ISNULL((select count(*) from tbl_group 
                                where groupname = N'" + data.TenNhom + "'),0)";
                    if (long.Parse(cnn.ExecuteScalar(sqlq).ToString()) > 0)
                        return JsonResultCommon.Trung("Tên nhóm");
                    if (cnn.Insert(val, "tbl_group") == -1)
                        return JsonResultCommon.Exception(_logger, cnn.LastError, _config, loginData, ControllerContext);
                    cnn.Disconnect();
                    //string LogContent = "Thêm nhóm " + data.TenNhom;
                    //DpsPage.Ghilogfile(loginData.CustomerID.ToString(), LogContent, LogContent, loginData.UserID.ToString());
                }
                return JsonResultCommon.ThanhCong(data);
            }
            catch (Exception ex)
            {
                return JsonResultCommon.Exception(_logger, ex, _config, loginData);
            }
        }
        /// <summary>
        /// Nhóm người dùng ---
        /// Thêm user vào nhóm ---
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        [CusAuthorize(Roles = "3900")]
        [Route("Insert_User")]
        [HttpPost]
        public async Task<BaseModel<object>> Insert_User(UserAddData data)
        {
            UserJWT loginData = Ulities.GetUserByHeader(HttpContext.Request.Headers);
            if (loginData == null)
                return JsonResultCommon.DangNhap();
            SqlConditions Conds = new SqlConditions();
            Hashtable val = new Hashtable();
            try
            {
                string ConnectionString = JeeWorkLiteController.getConnectionString(ConnectionCache, loginData.CustomerID, _configuration);
                using (DpsConnection cnn = new DpsConnection(ConnectionString))
                {
                    val.Add("username", data.UserName);
                    val.Add("id_group", data.ID_Nhom);
                    // kiểm tra account đã có trong tab_Account
                    Conds = new SqlConditions();
                    //Conds.Add("username", data.UserName);
                    #region Lấy dữ liệu account từ JeeAccount
                    DataAccount = JeeWorkLiteController.GetAccountFromJeeAccount(HttpContext.Request.Headers, _configuration);
                    if (DataAccount == null)
                        return JsonResultCommon.Custom("Lỗi lấy danh sách nhân viên từ hệ thống quản lý tài khoản");
                    #endregion
                    var info = DataAccount.Where(x => data.UserName.ToString().Contains(x.Username.ToString())).FirstOrDefault();
                    if (info is null)
                    {
                        return JsonResultCommon.Custom("Không tìm thấy thông tin tài khoản trong hệ thống JeeAccount");
                    }
                    if (cnn.Insert(val, "tbl_group_account") == -1)
                    {
                        return JsonResultCommon.Exception(_logger, cnn.LastError, _config, loginData, ControllerContext);
                    }
                    #region update customdata
                    Conds.Add("Username", data.UserName);
                    string sqlq = $@"select distinct id_permit from tbl_Account_Permit where Username = @Username
                                    UNION
                                    select distinct id_permit from tbl_Group_Permit g 
                                    join tbl_group_account g_a  on g.id_group=g_a.id_group 
                                    where Username = @Username";
                    DataTable dt = cnn.CreateDataTable(sqlq, Conds);
                    string role = "";
                    if (dt.Rows.Count > 0)
                    {
                        List<string> listrole = dt.AsEnumerable().Select(x => x["id_permit"].ToString()).ToList();
                        role = string.Join(",", listrole);
                    }
                    ObjCustomData objCustomData = new ObjCustomData();
                    objCustomData.userId = Common.getIDUserbyUserName(data.UserName, HttpContext.Request.Headers, _configuration.GetValue<string>("Host:JeeAccount_API"));
                    objCustomData.updateField = "jee-work";
                    var datas = new
                    {
                        WeWorkRoles = role
                    };
                    objCustomData.fieldValue = datas;
                    var dataJA = Common.UpdateCustomData(_configuration, _configuration.GetValue<string>("Host:JeeAccount_API"), objCustomData);
                    if (dataJA == null)
                    {
                        cnn.RollbackTransaction();
                        return JsonResultCommon.ThatBai("Lỗi cập nhật quyền lên hệ thống quản lý tài khoản! Vui lòng đợi cập nhật");
                    }
                    cnn.EndTransaction();
                    #endregion
                }
                return JsonResultCommon.ThanhCong(data);
            }
            catch (Exception ex)
            {
                return JsonResultCommon.Exception(_logger, ex, _config, loginData);
            }
        }
        /// <summary>
        /// Nhóm người dùng ---
        /// Xóa user khỏi nhóm ---
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        [CusAuthorize(Roles = "3900")]
        [Route("Delete_User")]
        [HttpPost]
        public async Task<BaseModel<object>> Delete_User(UserAddData data)
        {
            UserJWT loginData = Ulities.GetUserByHeader(HttpContext.Request.Headers);
            if (loginData == null)
                return JsonResultCommon.DangNhap();
            SqlConditions Conds = new SqlConditions();
            Hashtable val = new Hashtable();
            try
            {
                Conds.Add("username", data.UserName);
                Conds.Add("id_group", data.ID_Nhom);
                string ConnectionString = JeeWorkLiteController.getConnectionString(ConnectionCache, loginData.CustomerID, _configuration);
                using (DpsConnection cnn = new DpsConnection(ConnectionString))
                {
                    #region Lấy dữ liệu account từ JeeAccount
                    DataAccount = JeeWorkLiteController.GetAccountFromJeeAccount(HttpContext.Request.Headers, _configuration);
                    if (DataAccount == null)
                        return JsonResultCommon.Custom("Lỗi lấy danh sách nhân viên từ hệ thống quản lý tài khoản");
                    #endregion
                    var info = DataAccount.Where(x => data.UserName.ToString().Contains(x.Username.ToString())).FirstOrDefault();
                    //Kiểm tra nhóm và tài khoản có phải làm admin hay không
                    string select = $@"select isadmin from tbl_group where id_group=@id_group";
                    DataTable dt = cnn.CreateDataTable(select, Conds);
                    if ((dt.Rows.Count > 0) && (bool.TrueString.Equals(dt.Rows[0][0].ToString())))
                    {
                        if (info is not null && info.isAdmin)
                        {
                            return JsonResultCommon.Custom("Không cho phép bỏ tài khoản admin ra khỏi nhóm admin");
                        }
                    }
                    if (cnn.Delete(Conds, "tbl_group_account") == -1)
                    {
                        return JsonResultCommon.Exception(_logger, cnn.LastError, _config, loginData, ControllerContext);
                    }
                    #region update customdata
                    Conds = new SqlConditions();
                    Conds.Add("Username", data.UserName);
                    string sqlq = $@"select distinct id_permit from tbl_Account_Permit where Username = @Username
                                    UNION
                                    select distinct id_permit  from tbl_Group_Permit g 
                                    join tbl_Group_Account g_a  on g.id_group=g_a.id_group 
                                    where Username = @Username";
                    DataTable dts = cnn.CreateDataTable(sqlq, Conds);
                    string role = "";
                    if (dts.Rows.Count > 0)
                    {
                        List<string> listrole = dts.AsEnumerable().Select(x => x["id_permit"].ToString()).ToList();
                        role = string.Join(",", listrole);
                    }
                    ObjCustomData objCustomData = new ObjCustomData();
                    objCustomData.userId = Common.getIDUserbyUserName(data.UserName, HttpContext.Request.Headers, _configuration.GetValue<string>("Host:JeeAccount_API"));
                    objCustomData.updateField = "jee-work";
                    var datas = new
                    {
                        WeWorkRoles = role
                    };
                    objCustomData.fieldValue = datas;

                    var dataJA = Common.UpdateCustomData(_configuration, _configuration.GetValue<string>("Host:JeeAccount_API"), objCustomData);
                    if (dataJA == null)
                    {
                        cnn.RollbackTransaction();
                        return JsonResultCommon.ThatBai("Lỗi cập nhật dữ liệu quyền lên hệ thống quản lý tài khoản! Vui lòng đợi cập nhật");
                    }
                    cnn.EndTransaction();
                    #endregion
                }
                return JsonResultCommon.ThanhCong(data);
            }
            catch (Exception ex)
            {
                return JsonResultCommon.Exception(_logger, ex, _config, loginData);
            }
        }
        /// <summary>
        /// Xóa nhóm
        /// </summary>
        /// <param name="id">ID_Nhom</param>
        /// <returns></returns>
        [CusAuthorize(Roles = "3900")]
        [Route("Delete_Nhom")]
        [HttpGet]
        public BaseModel<object> Delete_Nhom(long id, string TenNhom)
        {

            UserJWT loginData = Ulities.GetUserByHeader(HttpContext.Request.Headers);
            if (loginData == null)
                return JsonResultCommon.DangNhap();
            SqlConditions Conds = new SqlConditions();
            Hashtable val = new Hashtable();
            try
            {
                Conds.Add("id_group", id);
                string ConnectionString = JeeWorkLiteController.getConnectionString(ConnectionCache, loginData.CustomerID, _configuration);
                using (DpsConnection cnn = new DpsConnection(ConnectionString))
                {
                    string sqlq = $@"select ISNULL((select count(*) 
                                    from Tbl_Group_Account 
                                    where id_group = " + id + "),0)";
                    if (long.Parse(cnn.ExecuteScalar(sqlq).ToString()) > 0)
                    {
                        return JsonResultCommon.Custom("Xóa thất bại. Nhóm đang có người dùng");
                    }
                    int rs = cnn.Delete(Conds, "tbl_group");
                    if (rs < 0)
                        return JsonResultCommon.Exception(_logger, cnn.LastError, _config, loginData, ControllerContext);
                    //string LogContent = "Xóa nhóm " + TenNhom + " (" + id + ")";
                    //DpsPage.Ghilogfile(loginData.CustomerID.ToString(), LogContent, LogContent, loginData.UserID.ToString());
                }
                return JsonResultCommon.ThanhCong(true);
            }
            catch (Exception ex)
            {
                return JsonResultCommon.Exception(_logger, ex, _config, loginData);
            }

        }
        /// <summary>
        /// Người dùng ---
        /// Load danh sách người dùng ---
        /// !Visible: chỉ xem, visible tất cả các button thao tác
        /// </summary>
        /// <param name="query"></param>
        /// <returns></returns>
        [CusAuthorize(Roles = "3900")]
        [Route("Get_DSNguoiDung")]
        [HttpGet]
        public object Get_DSNguoiDung([FromQuery] QueryParams query)
        {
            #region Lấy dữ liệu account từ JeeAccount
            DataAccount = JeeWorkLiteController.GetAccountFromJeeAccount(HttpContext.Request.Headers, _configuration);
            if (DataAccount == null)
                return JsonResultCommon.Custom("Lỗi lấy danh sách nhân viên từ hệ thống quản lý tài khoản");

            #endregion

            UserJWT loginData = Ulities.GetUserByHeader(HttpContext.Request.Headers);
            if (loginData == null)
                return JsonResultCommon.DangNhap();
            query = query == null ? new QueryParams() : query;
            BaseModel<object> model = new BaseModel<object>();
            PageModel pageModel = new PageModel();
            ErrorModel error = new ErrorModel();
            string sqlq = "";
            DataTable dt_staff = new DataTable();
            dt_staff.Columns.Add("UserId", typeof(int));
            dt_staff.Columns.Add("Username", typeof(string));
            dt_staff.Columns.Add("FirstName", typeof(string));
            dt_staff.Columns.Add("LastName", typeof(string));
            dt_staff.Columns.Add("FullName", typeof(string));
            dt_staff.Columns.Add("Jobtitle", typeof(string));
            dt_staff.Columns.Add("Department", typeof(string));
            dt_staff.Columns.Add("AvartarImgURL", typeof(string));
            dt_staff.Columns.Add("CustomerID", typeof(int));
            SqlConditions Conds = new SqlConditions();
            string orderByStr = "Username asc", whereStr = "nv.thoiviec = 0  and nv.disable=0 and CustemerID=@CustemerID";
            DataTable dt = new DataTable();
            try
            {
                if (!string.IsNullOrEmpty(query.filter["HoTen"]))
                {
                    whereStr += " and (nv.holot+' '+nv.ten like @hoten)";
                    Conds.Add("hoten", "%" + query.filter["HoTen"] + "%");
                }
                Conds.Add("CustemerID", loginData.CustomerID);

                Dictionary<string, string> sortableFields = new Dictionary<string, string>
                        {
                            { "HoTen", "hoten"},
                            { "TenDangNhap", "acc.username"},
                            { "ChucDanh", "tenchucdanh"},
                        };
                if (!string.IsNullOrEmpty(query.sortField) && sortableFields.ContainsKey(query.sortField))
                {
                    orderByStr = sortableFields[query.sortField] + ("desc".Equals(query.sortOrder) ? " desc" : " asc");
                }
                bool Visible = true;
                string ConnectionString = JeeWorkLiteController.getConnectionString(ConnectionCache, loginData.CustomerID, _configuration);
                #region Kiểm tra quyền chỉ xem
                Common permit = new Common(ConnectionString);
                if (Common.IsReadOnlyPermit("3900", loginData.Username))
                {
                    Visible = false;
                }
                #endregion
                using (DpsConnection cnn = new DpsConnection(ConnectionString))
                {
                    Visible = Common.CheckRoleByUserID(loginData, 3900, cnn);
                    #region Lấy danh sách nhân viên từ JeeAccount
                    DataAccount = new List<AccUsernameModel>();
                    DataAccount = JeeWorkLiteController.GetAccountFromJeeAccount(HttpContext.Request.Headers, _configuration);
                    if (DataAccount == null)
                        return JsonResultCommon.Custom("Lỗi lấy danh sách nhân viên từ JeeAccount");
                    DataRow row;
                    foreach (AccUsernameModel str in DataAccount)
                    {
                        row = dt_staff.NewRow();
                        row["UserId"] = str.UserId;
                        row["Username"] = str.Username;
                        row["FirstName"] = str.FirstName;
                        row["LastName"] = str.LastName;
                        row["FullName"] = str.FullName;
                        row["Jobtitle"] = str.Jobtitle;
                        row["Department"] = str.Department;
                        row["AvartarImgURL"] = str.AvartarImgURL;
                        row["CustomerID"] = str.CustomerID;
                        dt_staff.Rows.Add(row);
                    }
                    #endregion
                }
                if (!string.IsNullOrEmpty(query.filter["HoTen"]))
                {
                    DataRow[] filteredRows = dt_staff.Select("FullName like '%" + query.filter["HoTen"] + "%'");
                    dt_staff = filteredRows.CopyToDataTable();
                }
                int total = dt_staff.Rows.Count;
                if (total == 0)
                    return JsonResultCommon.ThanhCong(new List<string>(), pageModel, Visible);
                if (query.more)
                {
                    query.page = 1;
                    query.record = pageModel.TotalCount;
                }
                pageModel.TotalCount = total;
                pageModel.AllPage = (int)Math.Ceiling(total / (decimal)query.record);
                pageModel.Size = query.record;
                pageModel.Page = query.page;

                // Phân trang
                dt_staff = dt_staff.AsEnumerable().Skip((query.page - 1) * query.record).Take(query.record).CopyToDataTable();
                var data = from nv in dt_staff.AsEnumerable()
                           select new
                           {
                               Username = nv["Username"],
                               ID_NV = nv["UserId"],
                               HoTen = nv["FullName"],
                               ChucDanh = nv["Jobtitle"],
                               CCTC = nv["Department"],
                               ChucVu = nv["Jobtitle"],
                           };
                if (!string.IsNullOrEmpty(query.filter["HoTen"]))
                {
                    data = data.Where(x => x.HoTen.ToString().ToLower().Contains(query.filter["HoTen"].ToString().ToLower()));
                }
                if (query.sortField == "HoTen")
                {
                    if (query.sortOrder == "asc")
                    {
                        data = data.OrderBy(x => x.HoTen);
                    }
                    else
                    {
                        data = data.OrderByDescending(x => x.HoTen);
                    }
                }
                else if (query.sortField == "TenDangNhap")
                {
                    if (query.sortOrder == "asc")
                    {
                        data = data.OrderBy(x => x.Username);
                    }
                    else
                    {
                        data = data.OrderByDescending(x => x.Username);
                    }
                }
                else if (query.sortField == "ChucDanh")
                {
                    if (query.sortOrder == "asc")
                    {
                        data = data.OrderBy(x => x.ChucDanh);
                    }
                    else
                    {
                        data = data.OrderByDescending(x => x.ChucDanh);
                    }
                }
                return JsonResultCommon.ThanhCong(data, pageModel, Visible);
            }
            catch (Exception ex)
            {
                return JsonResultCommon.Exception(_logger, ex, _config, loginData);
            }
        }
        /// <summary>
        /// Load danh sách chức năng --- 
        /// (Column "Chỉ xem").Visible = !IsReadPermit, (Column "Chỉ xem").Check = IsRead, (Column "Sửa").Check = IsEdit, (Column "Chỉ xem").Enable = IsRead_Enable, (Column "Sửa").Enable = IsEdit_Enable /// (Column "Chỉ xem").Visible = !IsReadPermit 
        /// !Visible: chỉ xem, visible tất cả các button thao tác
        /// </summary>
        /// <param name="query"></param>
        /// <returns></returns>
        //[CusAuthorize(Roles = "3900")]
        [Route("Get_ListFunctions")]
        [HttpGet]
        public object Get_ListFunctions([FromQuery] QueryParams query)
        {
            UserJWT loginData = Ulities.GetUserByHeader(HttpContext.Request.Headers);
            if (loginData == null)
                return JsonResultCommon.DangNhap();
            query = query == null ? new QueryParams() : query;
            BaseModel<object> model = new BaseModel<object>();
            PageModel pageModel = new PageModel();
            ErrorModel error = new ErrorModel();
            string sqlq = "";
            SqlConditions Conds = new SqlConditions();
            string orderByStr = "Position asc";
            string whereStr = "";
            bool Visible = true;
            DataTable dt = new DataTable();
            DataTable dt_permit = new DataTable();
            string Id_Function = query.filter["ID_NhomChucNang"];
            bool isadmin = false;
            try
            {
                whereStr = $"(CustemerID=" + loginData.CustomerID + " or CustemerID is NULL)";
                Dictionary<string, string> sortableFields = new Dictionary<string, string>
                        {
                            { "Tenquyen", "Tenquyen"},
                            { "Id_Quyen", "Id_permit"},
                        };
                if (!string.IsNullOrEmpty(query.sortField) && sortableFields.ContainsKey(query.sortField))
                {
                    orderByStr = sortableFields[query.sortField] + ("desc".Equals(query.sortOrder) ? " desc" : " asc");
                }
                string ConnectionString = JeeWorkLiteController.getConnectionString(ConnectionCache, loginData.CustomerID, _configuration);
                #region Kiểm tra quyền chỉ xem
                Common permit = new Common(ConnectionString);
                if (Common.IsReadOnlyPermit("3900", loginData.Username))
                {
                    Visible = false;
                }
                #endregion
                using (DpsConnection cnn = new DpsConnection(ConnectionString))
                {
                    sqlq = $@"select Id_permit, Tenquyen, Id_group, LangKey, IsReadPermit 
                        from Tbl_Permision 
                        where { whereStr } order by { orderByStr}";
                    dt = cnn.CreateDataTable(sqlq);
                    if (dt == null || cnn.LastError != null)
                        return JsonResultCommon.Exception(_logger, cnn.LastError, _config, loginData, ControllerContext);
                    if (dt.Rows.Count == 0)
                        return JsonResultCommon.KhongCoDuLieu();
                    Conds = new SqlConditions();
                    dt_permit = new DataTable();
                    bool isgroup = bool.Parse(query.filter["Type"].ToString());
                    if (isgroup)
                    {
                        Conds.Add("id_group", query.filter["id_group"]);
                        dt_permit = cnn.CreateDataTable("select id_permit, Edit " +
                                                        "from tbl_Group_Permit " +
                                                        "where (where)", "(where)", Conds);
                    }
                    else
                    {
                        Conds.Add("Username", query.filter["username"]);
                        dt_permit = cnn.CreateDataTable("select id_permit, Edit " +
                                "from tbl_Account_Permit " +
                                "where (where)", "(where)", Conds);
                    }
                    dt.Columns.Add("IsRead", typeof(bool));
                    dt.Columns.Add("IsEdit", typeof(bool));
                    dt.Columns.Add("IsRead_Enable", typeof(bool));
                    dt.Columns.Add("IsEdit_Enable", typeof(bool));
                    foreach (DataRow dr in dt.Rows)
                    {
                        dr["IsRead"] = false;
                        DataRow[] r = dt_permit.Select("id_permit=" + dr["id_permit"]);
                        dr["IsRead_Enable"] = true;
                        dr["IsEdit_Enable"] = true;
                        if (r.Length > 0)
                        {
                            dr["IsEdit"] = true;
                            if ((bool.TrueString.Equals(dr["IsReadPermit"].ToString())) && (bool.FalseString.Equals(r[0]["Edit"].ToString())))
                            {
                                dr["IsRead"] = true;
                            }
                            else dr["IsRead"] = false;
                            if (isadmin)
                            {
                                dr["IsRead_Enable"] = false;
                                dr["IsEdit_Enable"] = false;
                            }
                        }
                        else dr["IsEdit"] = false;
                    }
                    if (isgroup)
                    {
                        // Xét trường hợp group admin thì disabled check
                        Conds = new SqlConditions();
                        Conds.Add("id_group", query.filter["id_group"]);
                        Conds.Add("IsAdmin", 1);
                        DataTable dtAdmin = cnn.CreateDataTable("select Id_group  from tbl_group where (where)", "(where)", Conds);
                        if (dtAdmin.Rows.Count > 0)
                        {
                            foreach (DataRow dr in dt.Rows)
                            {
                                dr["IsRead_Enable"] = false;
                                dr["IsEdit_Enable"] = false;
                            }
                        }
                    }
                    else // Xét trường hợp user nằm trong group
                    {
                        Conds = new SqlConditions();
                        Conds.Add("Username", query.filter["username"]);
                        DataTable permit_group = cnn.CreateDataTable($@"select g.id_permit, g.Edit 
                                                                from tbl_Group_Permit g 
                                                                join tbl_Group_Account g_a 
                                                                on g.id_group=g_a.id_group 
                                                                where (where)", "(where)", Conds);
                        foreach (DataRow dr in dt.Rows)
                        {
                            DataRow[] r = permit_group.Select("id_permit=" + dr["id_permit"]);
                            if (r.Length > 0)
                            {
                                dr["IsEdit"] = true;
                                dr["IsEdit_Enable"] = false;
                                dr["IsRead_Enable"] = false;
                                if ((bool.TrueString.Equals(dr["IsReadPermit"].ToString())) && (bool.FalseString.Equals(r[0]["Edit"].ToString())))
                                {
                                    dr["IsRead"] = true;
                                }
                                else dr["IsRead"] = false;
                            }
                        }
                    }
                }
                var data = from r in dt.AsEnumerable()
                           select new
                           {
                               Id_Quyen = r["Id_permit"],
                               TenQuyen = r["Tenquyen"],
                               LangKey = r["LangKey"],
                               IsReadPermit = r["IsReadPermit"],
                               IsRead = r["IsRead"],
                               IsEdit = r["IsEdit"],
                               IsRead_Enable = r["IsRead_Enable"],
                               IsEdit_Enable = r["IsEdit_Enable"],
                           };
                return JsonResultCommon.ThanhCong(data, pageModel, Visible);
            }
            catch (Exception ex)
            {
                return JsonResultCommon.Exception(_logger, ex, _config, loginData);
            }
        }
        /// <summary>
        /// Lưu quyền nhóm, người dùng ---
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        [CusAuthorize(Roles = "3900")]
        [Route("Save_Permision")]
        [HttpPost]
        public async Task<BaseModel<object>> Save_QuyenNhomNguoiDung(List<PermissionNewModel> arr_data)
        {
            UserJWT loginData = Ulities.GetUserByHeader(HttpContext.Request.Headers);
            if (loginData == null)
                return JsonResultCommon.DangNhap();
            SqlConditions Conds = new SqlConditions();
            Hashtable val = new Hashtable();
            StringCollection permit = new StringCollection();
            StringCollection ReadOnlyPermit = new StringCollection();
            try
            {
                string ConnectionString = JeeWorkLiteController.getConnectionString(ConnectionCache, loginData.CustomerID, _configuration);
                using (DpsConnection cnn = new DpsConnection(ConnectionString))
                {
                    Conds = new SqlConditions();
                    string tableName = "tbl_group_permit";
                    string ColumnKey = "id_group";
                    if (!arr_data[0].IsGroup)
                    {
                        tableName = "tbl_account_permit";
                        ColumnKey = "username";
                    }
                    Conds.Add(ColumnKey, arr_data[0].ID);
                    DataTable dt = cnn.CreateDataTable("select id_permit, Edit " +
                        "from " + tableName + " " +
                        "where (where)", "(where)", Conds);
                    string them = "", xoa = "", capnhat = "";
                    string LogContent = "";
                    foreach (PermissionNewModel data in arr_data)
                    {
                        DataRow[] drow = dt.Select("id_permit=" + data.ID_Quyen);
                        if (data.IsEdit || data.IsRead)
                        {
                            permit.Add(data.ID_Quyen.ToString());
                            bool chixem = data.IsRead;
                            if (data.IsRead)
                            {
                                ReadOnlyPermit.Add(data.ID_Quyen.ToString());
                            }
                            if (drow.Length <= 0) them += ", " + data.TenQuyen + "(" + data.ID_Quyen + ")" + (chixem ? " <Chỉ xem>" : "");
                            else if (drow[0]["edit"].ToString().ToLower().Equals(chixem.ToString().ToLower())) capnhat += ", " + data.TenQuyen + "(" + data.ID_Quyen + ")" + (chixem ? "<Cho phép chỉnh sửa>-> <Chỉ xem>" : "<Chỉ xem>-><Cho phép chỉnh sửa>");
                        }
                        else
                        {
                            if (drow.Length > 0) xoa = ", " + data.TenQuyen + "(" + data.ID_Quyen + ")";
                        }
                    }
                    if (them.Length > 0) LogContent += " Thêm quyền : " + them.Substring(1);
                    if (capnhat.Length > 0) LogContent += " | Chỉnh sửa quyền : " + capnhat.Substring(1);
                    if (xoa.Length > 0) LogContent += " | Xóa quyền : " + xoa.Substring(1);
                    Conds = new SqlConditions();
                    Conds.Add(ColumnKey, arr_data[0].ID);
                    Conds.Add("id_chucnang", arr_data[0].ID_NhomChucNang);
                    string execute = "delete " + tableName + " where (" + ColumnKey + "=@" + ColumnKey + ") " +
                            "and (id_permit in (select Id_Permit from tbl_permision))" +
                            "";
                    int rs = cnn.ExecuteNonQuery(execute, Conds);
                    if (rs == -1)
                    {
                        return JsonResultCommon.Exception(_logger, cnn.LastError, _config, loginData, ControllerContext);
                    }
                    for (int i = 0; i < permit.Count; i++)
                    {
                        val = new Hashtable();
                        val.Add(ColumnKey, arr_data[0].ID);
                        val.Add("id_permit", permit[i]);
                        bool edit = true;
                        if (ReadOnlyPermit.Contains(permit[i])) edit = false;
                        val.Add("edit", edit);
                        val.Add("id_chucnang", arr_data[0].ID_NhomChucNang);
                        if (cnn.Insert(val, tableName) == -1)
                        {
                            return JsonResultCommon.Exception(_logger, cnn.LastError, _config, loginData, ControllerContext);
                        }
                    }
                    #region update customdata
                    if (ColumnKey == "username")
                    {
                        Conds = new SqlConditions();
                        Conds.Add("Username", arr_data[0].ID);
                        string sqlq = $@"select distinct id_permit from tbl_Account_Permit where Username = @Username
                                    UNION
                                    select distinct id_permit  from tbl_Group_Permit g join tbl_Group_Account g_a on g.id_group=g_a.id_group 
                                    where Username = @Username";
                        DataTable dts = cnn.CreateDataTable(sqlq, Conds);
                        string role = "";
                        if (dts.Rows.Count > 0)
                        {
                            List<string> listrole = dts.AsEnumerable().Select(x => x["id_permit"].ToString()).ToList();
                            role = string.Join(",", listrole);
                        }
                        ObjCustomData objCustomData = new ObjCustomData();
                        objCustomData.userId = Common.getIDUserbyUserName(arr_data[0].ID.ToString(), HttpContext.Request.Headers, _configuration.GetValue<string>("Host:JeeAccount_API"));
                        objCustomData.updateField = "jee-work";
                        var datas = new
                        {
                            WeWorkRoles = role
                        };
                        objCustomData.fieldValue = datas;
                        var dataJA = Common.UpdateCustomData(_configuration, _configuration.GetValue<string>("Host:JeeAccount_API"), objCustomData);
                        if (dataJA == null)
                        {
                            cnn.RollbackTransaction();
                            return JsonResultCommon.ThatBai("Lỗi cập nhật nhóm quyền lên hệ thống quản lý tài khoản! Vui lòng đợi cập nhật");
                        }
                    }
                    else
                    {
                        string sqlquyen = @$"select * from Tbl_Group_Account where Id_group = {arr_data[0].ID}";
                        DataTable dtquyen = cnn.CreateDataTable(sqlquyen, Conds);
                        foreach (DataRow vts in dtquyen.Rows)
                        {
                            Conds = new SqlConditions();
                            Conds.Add("Username", vts["Username"]);
                            string sqlq = $@"select distinct id_permit from tbl_account_permit where Username = @Username
                                    UNION
                                    select distinct id_permit  from tbl_Group_Permit g 
                                    join tbl_Group_Account g_a 
                                    on g.id_group=g_a.id_group 
                                    where Username = @Username";
                            DataTable dts = cnn.CreateDataTable(sqlq, Conds);
                            string role = "";
                            if (dts.Rows.Count > 0)
                            {
                                List<string> listrole = dts.AsEnumerable().Select(x => x["id_permit"].ToString()).ToList();
                                role = string.Join(",", listrole);
                            }
                            ObjCustomData objCustomData = new ObjCustomData();
                            objCustomData.userId = Common.getIDUserbyUserName(vts["Username"].ToString(), HttpContext.Request.Headers, _configuration.GetValue<string>("Host:JeeAccount_API"));
                            objCustomData.updateField = "jee-work";
                            var datas = new
                            {
                                WeWorkRoles = role
                            };
                            objCustomData.fieldValue = datas;
                            if (!string.IsNullOrEmpty(objCustomData.userId))
                            {
                                var dataJA = Common.UpdateCustomData(_configuration, _configuration.GetValue<string>("Host:JeeAccount_API"), objCustomData);
                                if (dataJA == null)
                                {
                                    cnn.RollbackTransaction();
                                    return JsonResultCommon.ThatBai("Lỗi cập nhật username vào nhóm lên hệ thống quản lý tài khoản! Vui lòng đợi cập nhật");
                                }
                            }
                        }
                    }
                    #endregion
                    LogContent += " của nhóm " + arr_data[0].Ten + "(" + arr_data[0].ID + ")";
                    cnn.EndTransaction();
                    cnn.Disconnect();
                    //DpsPage.Ghilogfile(loginData.CustomerID.ToString(), LogContent, LogContent, loginData.UserName);
                }
                return JsonResultCommon.ThanhCong();
            }
            catch (Exception ex)
            {
                return JsonResultCommon.Exception(_logger, ex, _config, loginData);
            }
        }
        [HttpGet]
        [Route("GetRolesForUser_WeWork")]
        public object GetRolesForUser_WeWork(string username, long CustomerID)
        {
            UserJWT loginData = Ulities.GetUserByHeader(HttpContext.Request.Headers);
            if (loginData != null)
                CustomerID = loginData.CustomerID;
            try
            {
                string ConnectionString = JeeWorkLiteController.getConnectionString(ConnectionCache, CustomerID, _configuration);
                DpsConnection Conn = new DpsConnection(ConnectionString);
                string[] listrole = Common.GetRolesForUser_WeWork(username, Conn);
                return listrole;
            }
            catch (Exception e)
            {
                return null;
            }
        }
    }
}
