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
        public WW_UserRightsController(IOptions<JeeWorkConfig> config, IHostingEnvironment hostingEnvironment)
        {
            _hostingEnvironment = hostingEnvironment;
            _config = config.Value;
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
            UserJWT loginData = Ulities.GetUserByHeader(HttpContext.Request.Headers);
            if (loginData == null)
                return JsonResultCommon.DangNhap();
            query = query == null ? new QueryParams() : query;
            BaseModel<object> model = new BaseModel<object>();
            PageModel pageModel = new PageModel();
            ErrorModel error = new ErrorModel();
            string sqlq = "";
            SqlConditions Conds = new SqlConditions();
            string orderByStr = "GroupName asc", whereStr = "CustemerID=@CustemerID and (Module=@Module) ";
            bool Visible = Common.CheckRoleByToken(loginData.UserID.ToString(), "3900");
            DataTable dt = new DataTable();
            try
            {
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
                using (DpsConnection cnn = new DpsConnection(_config.ConnectionString))
                {
                    sqlq = $@"select Id_group, GroupName, isadmin 
                            from Tbl_Group
                            where { whereStr } order by { orderByStr}";
                    dt = cnn.CreateDataTable(sqlq, Conds);
                    if (dt == null || cnn.LastError != null)
                        return JsonResultCommon.Exception(cnn.LastError, loginData.CustomerID, ControllerContext);
                }
                int total = dt.Rows.Count;
                if (total == 0)
                    return JsonResultCommon.KhongCoDuLieu(Visible);
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
                dt = dt.AsEnumerable().Skip((query.page - 1) * query.record).Take(query.record).CopyToDataTable();
                var data = from r in dt.AsEnumerable()
                           select new
                           {
                               TenNhom = r["GroupName"],
                               ID_Nhom = r["id_group"],
                               IsAdmin = r["Isadmin"],
                           };
                return JsonResultCommon.ThanhCong(data, pageModel, Visible);
            }
            catch (Exception ex)
            {
                return JsonResultCommon.Exception(ex, loginData.CustomerID);
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
        [Route("Get_DSNguoiDungNhom")]
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
                using (DpsConnection cnn = new DpsConnection(_config.ConnectionString))
                {
                    sqlq = $@"select * from Tbl_Group_Account
                        where { whereStr } order by { orderByStr}";
                    dt = cnn.CreateDataTable(sqlq, Conds);
                    if (dt == null | cnn.LastError != null)
                        return JsonResultCommon.Exception(cnn.LastError, loginData.CustomerID, ControllerContext);
                    #region Lấy danh sách nhân viên từ JeeAccount
                    DataAccount = WeworkLiteController.GetAccountFromJeeAccount(HttpContext.Request.Headers, _config);
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
                    return JsonResultCommon.KhongCoDuLieu(Visible);
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
                return JsonResultCommon.Exception(ex, loginData.CustomerID);
            }
        }

        /// <summary>
        /// Danh sách người dùng ---
        /// Load danh sách người dùng hệ thống ---
        /// !Visible: chỉ xem, visible tất cả các button thao tác
        /// </summary>
        /// <param name="query"></param>
        /// <returns></returns>
        [CusAuthorize(Roles = "3900")]
        [Route("Get_DSNguoiDungHeThong")]
        [HttpGet]
        public object Get_DSNguoiDungHeThong([FromQuery] QueryParams query)
        {

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
            string orderByStr = "Username asc", whereStr = $@"";
            bool Visible = true;
            DataTable dt = new DataTable();
            try
            {
                Conds.Add("CustemerID", loginData.CustomerID);
                Conds.Add("ID_Nhom", query.filter["ID_Nhom"]);

                Dictionary<string, string> sortableFields = new Dictionary<string, string>
                        {
                            { "Username", "Username"},
                        };
                if (!string.IsNullOrEmpty(query.sortField) && sortableFields.ContainsKey(query.sortField))
                {
                    orderByStr = sortableFields[query.sortField] + ("desc".Equals(query.sortOrder) ? " desc" : " asc");
                }
                using (DpsConnection cnn = new DpsConnection(_config.ConnectionString))
                {
                    sqlq = $@"select username from tbl_group_account where id_group=@ID_Nhom";
                    dt = cnn.CreateDataTable(sqlq, Conds);
                }
                StringCollection danhsach = new StringCollection();
                foreach (DataRow dr in dt.Rows)
                {
                    danhsach.Add(dr["username"].ToString());
                }
                #region Lấy danh sách nhân viên từ JeeAccount
                DataAccount = WeworkLiteController.GetAccountFromJeeAccount(HttpContext.Request.Headers, _config);
                if (DataAccount == null)
                    return JsonResultCommon.Custom("Lỗi lấy danh sách nhân viên từ JeeAccount");
                DataRow row;
                foreach (AccUsernameModel str in DataAccount)
                {
                    if (!danhsach.Contains(str.Username))
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
                }
                #endregion
                int total = dt_staff.Rows.Count;
                if (total == 0)
                    return JsonResultCommon.KhongCoDuLieu(Visible);
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
                var data = from r in dt_staff.AsEnumerable()
                           select new
                           {
                               Username = r["Username"],
                               ID_NV = r["UserId"],
                               HoTen = r["FullName"],
                               ChucVu = r["Jobtitle"],
                           };
                return JsonResultCommon.ThanhCong(data, pageModel, Visible);
            }
            catch (Exception ex)
            {
                return JsonResultCommon.Exception(ex, loginData.CustomerID);
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
                val.Add("DateCreated", DateTime.Now);
                val.Add("LastModified", DateTime.Now);
                val.Add("CustemerID", loginData.CustomerID);
                val.Add("Module", data.Module);
                using (DpsConnection cnn = new DpsConnection(_config.ConnectionString))
                {
                    string sqlq = $@"select ISNULL((select count(*) from tbl_group 
                                where groupname = N'" + data.TenNhom + "'),0)";
                    if (long.Parse(cnn.ExecuteScalar(sqlq).ToString()) > 0)
                        return JsonResultCommon.Trung("Tên nhóm");
                    if (cnn.Insert(val, "tbl_group") == -1)
                        return JsonResultCommon.Exception(cnn.LastError, loginData.CustomerID, ControllerContext);
                    cnn.Disconnect();
                    //string LogContent = "Thêm nhóm " + data.TenNhom;
                    //DpsPage.Ghilogfile(loginData.CustomerID.ToString(), LogContent, LogContent, loginData.UserID.ToString());
                }
                return JsonResultCommon.ThanhCong(data);
            }
            catch (Exception ex)
            {
                return JsonResultCommon.Exception(ex, loginData.CustomerID);
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
                using (DpsConnection cnn = new DpsConnection(_config.ConnectionString))
                {
                    val.Add("username", data.UserName);
                    val.Add("id_group", data.ID_Nhom);
                    // kiểm tra account đã có trong tab_Account
                    Conds = new SqlConditions();
                    Conds.Add("username", data.UserName);

                    string sqlq_account = "select * from tbl_account where (where)";
                    DataTable dt_account = cnn.CreateDataTable(sqlq_account, "(where)", Conds);
                    if (dt_account.Rows.Count <= 0)
                    {
                        sqlq_account = $@"select * from {_config.HRCatalog}.dbo.tbl_account where (where)";
                        dt_account = cnn.CreateDataTable(sqlq_account, "(where)", Conds);
                        if (dt_account.Rows.Count <= 0)
                            return JsonResultCommon.Custom("Không tìm thấy thông tin tài khoản trong databases HR");
                        sqlq_account = $@"select * from Tbl_Group_Account where (where)";
                        Conds.Add("id_group", data.ID_Nhom);
                        dt_account = cnn.CreateDataTable(sqlq_account, "(where)", Conds);
                        string sql_insert = "";
                        if (dt_account.Rows.Count <= 0)
                        {
                            sql_insert = $@"insert into tbl_account (Username, Id_nv, Password, Lock, Disable, LastLogin, d_password, CreatedDate, LastPassChg, LastSend, Token, FailLogin, Solop, Validatecode, ExpireValidate, Loaitaikhoan, ExpPassword, AuthenType, 
                         IsCreateLDAPAccount, IsAdmin, DefaultModule)
                        select Username, Id_nv, Password, Lock, Disable, LastLogin, d_password, CreatedDate, LastPassChg, LastSend, Token, FailLogin, Solop, Validatecode, ExpireValidate, Loaitaikhoan, ExpPassword, AuthenType, 
                         IsCreateLDAPAccount, IsAdmin, DefaultModule
                        from {_config.HRCatalog}.dbo.tbl_account acc where acc.username = '{data.UserName}'";
                            var rs = cnn.ExecuteScalar(sql_insert);
                        }
                    }
                    if (cnn.Insert(val, "tbl_group_account") == -1)
                    {
                        return JsonResultCommon.Exception(cnn.LastError, loginData.CustomerID, ControllerContext);
                    }
                    //string LogContent = "Thêm tài khoản người dùng username=" + data.UserName + " vào nhóm id=" + data.ID_Nhom;
                    //DpsPage.Ghilogfile(loginData.CustomerID.ToString(), LogContent, LogContent, loginData.UserID.ToString());
                }

                return JsonResultCommon.ThanhCong(data);
            }
            catch (Exception ex)
            {
                return JsonResultCommon.Exception(ex, loginData.CustomerID);
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
                using (DpsConnection cnn = new DpsConnection(_config.ConnectionString))
                {
                    //Kiểm tra nhóm và tài khoản có phải làm admin hay không
                    string select = $@"select isadmin from tbl_group where id_group=@id_group";
                    DataTable dt = cnn.CreateDataTable(select, Conds);
                    if ((dt.Rows.Count > 0) && (bool.TrueString.Equals(dt.Rows[0][0].ToString())))
                    {
                        DataTable nguoidung = cnn.CreateDataTable($@"select IsAdmin from Tbl_Account where Username=@Username", Conds);
                        if ((nguoidung.Rows.Count > 0) && (bool.TrueString.Equals(nguoidung.Rows[0][0].ToString())))
                        {
                            return JsonResultCommon.Custom("Không cho phép bỏ tài khoản admin ra khỏi nhóm admin");
                        }
                    }
                    if (cnn.Delete(Conds, "tbl_group_account") == -1)
                    {
                        return JsonResultCommon.Exception(cnn.LastError, loginData.CustomerID, ControllerContext);
                    }
                }
                return JsonResultCommon.ThanhCong(data);
            }
            catch (Exception ex)
            {
                return JsonResultCommon.Exception(ex, loginData.CustomerID);
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
                using (DpsConnection cnn = new DpsConnection(_config.ConnectionString))
                {
                    string sqlq = $@"select ISNULL((select count(*) 
                                    from {_config.HRCatalog}.dbo.Tbl_Group_Account 
                                    where id_group = " + id + "),0)";
                    if (long.Parse(cnn.ExecuteScalar(sqlq).ToString()) > 0)
                    {
                        return JsonResultCommon.Custom("Xóa thất bại. Nhóm đang có người dùng");
                    }
                    int rs = cnn.Delete(Conds, "tbl_group");
                    if (rs < 0)
                        return JsonResultCommon.Exception(cnn.LastError, loginData.CustomerID, ControllerContext);
                    //string LogContent = "Xóa nhóm " + TenNhom + " (" + id + ")";
                    //DpsPage.Ghilogfile(loginData.CustomerID.ToString(), LogContent, LogContent, loginData.UserID.ToString());
                }
                return JsonResultCommon.ThanhCong(true);
            }
            catch (Exception ex)
            {
                return JsonResultCommon.Exception(ex, loginData.CustomerID);
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
            bool Visible = Common.CheckRoleByToken(loginData.UserID.ToString(), "3900");
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

                using (DpsConnection cnn = new DpsConnection(_config.ConnectionString))
                {
                    #region Lấy danh sách nhân viên từ JeeAccount
                    DataAccount = new List<AccUsernameModel>();
                    DataAccount = WeworkLiteController.GetAccountFromJeeAccount(HttpContext.Request.Headers, _config);
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
                               CCTC = nv["Department"]
                           };
                return JsonResultCommon.ThanhCong(data, pageModel, Visible);
            }
            catch (Exception ex)
            {
                return JsonResultCommon.Exception(ex, loginData.CustomerID);
            }
        }

        /// <summary>
        /// Nhóm người dùng ---
        /// Lưu người dùng ---
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        [CusAuthorize(Roles = "3900")]
        [Route("Save_NguoiDung")]
        [HttpPost]
        public async Task<BaseModel<object>> Save_NguoiDung(List<NguoiDungAddData> arr_data)
        {

            UserJWT loginData = Ulities.GetUserByHeader(HttpContext.Request.Headers);
            if (loginData == null)
                return JsonResultCommon.DangNhap();
            SqlConditions Conds = new SqlConditions();
            Hashtable val = new Hashtable();
            try
            {
                using (DpsConnection cnn = new DpsConnection(_config.ConnectionString))
                {
                    foreach (NguoiDungAddData data in arr_data)
                    {
                        val = new Hashtable();
                        if (data.Locked) val.Add("lock", 1);
                        else val.Add("lock", 0);
                        Conds = new SqlConditions();
                        Conds.Add("username", data.UserName);
                        if (cnn.Update(val, Conds, "Tbl_Account") == -1)
                        {
                            return JsonResultCommon.Exception(cnn.LastError, loginData.CustomerID, ControllerContext);
                        }
                        //string LogContent = "Khóa tài khoản " + data.UserName;
                        //DpsPage.Ghilogfile(loginData.CustomerID.ToString(), LogContent, LogContent, loginData.UserID.ToString());
                    }
                }
                return JsonResultCommon.ThanhCong();
            }
            catch (Exception ex)
            {
                return JsonResultCommon.Exception(ex, loginData.CustomerID);
            }
        }


        /// <summary>
        /// Load danh sách chức năng theo nhóm --- 
        /// (Column "Chỉ xem").Visible = !IsReadPermit, (Column "Chỉ xem").Check = IsRead, (Column "Sửa").Check = IsEdit, (Column "Chỉ xem").Enable = IsRead_Enable, (Column "Sửa").Enable = IsEdit_Enable /// (Column "Chỉ xem").Visible = !IsReadPermit 
        /// !Visible: chỉ xem, visible tất cả các button thao tác
        /// </summary>
        /// <param name="query"></param>
        /// <returns></returns>
        [CusAuthorize(Roles = "3900")]
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
                whereStr = $"(CustemerID=" + loginData.CustomerID + " or CustemerID is NULL) and (GFunctionID in (select GroupFunctionID from " + _config.HRCatalog + ".dbo.Sys_Pack_GFunction " +
                    $"inner join {_config.HRCatalog}.dbo.Tbl_Custemers on {_config.HRCatalog}.dbo.Sys_Pack_GFunction.PackID = {_config.HRCatalog}.dbo.Tbl_Custemers.PackID " +
                    $"and {_config.HRCatalog}.dbo.Tbl_Custemers.RowID = " + loginData.CustomerID + ") " +
                    $"or GFunctionID in (select GFunctionID from {_config.HRCatalog}.dbo.Ex_GroupFunction where CustemerID=" + loginData.CustomerID + ")) " +
                    $"and (loaihinh is null or loaihinh = (select loaihinh from {_config.HRCatalog}.dbo.Tbl_Custemers where rowid=" + loginData.CustomerID + ")) " +
                    $"and Id_group=" + Id_Function + "";
                Dictionary<string, string> sortableFields = new Dictionary<string, string>
                        {
                            { "Tenquyen", "Tenquyen"},
                        };
                if (!string.IsNullOrEmpty(query.sortField) && sortableFields.ContainsKey(query.sortField))
                {
                    orderByStr = sortableFields[query.sortField] + ("desc".Equals(query.sortOrder) ? " desc" : " asc");
                }
                using (DpsConnection cnn = new DpsConnection(_config.ConnectionString))
                {
                    sqlq = $@"select Id_permit, Tenquyen, Id_group, LangKey, IsReadPermit 
                        from Tbl_Permision 
                        where { whereStr } order by { orderByStr}";
                    dt = cnn.CreateDataTable(sqlq);
                    if (dt == null || cnn.LastError != null)
                        return JsonResultCommon.Exception(cnn.LastError, loginData.CustomerID, ControllerContext);
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
                    if (!isgroup) // Xét trường hợp user nằm trong group
                    {
                        Conds = new SqlConditions();
                        Conds.Add("Username", query.filter["username"]);
                        DataTable permit_group = cnn.CreateDataTable($@"select g.id_permit, g.Edit 
                                                                from tbl_Group_Permit g 
                                                                join {_config.HRCatalog}.dbo.tbl_Group_Account g_a 
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
                return JsonResultCommon.Exception(ex, loginData.CustomerID);
            }
        }
        /// <summary>
        /// Load danh sách chức năng theo user ---
        /// (Column "Chỉ xem").Visible = !IsReadPermit, (Column "Chỉ xem").Check = IsRead, (Column "Sửa").Check = IsEdit, (Column "Chỉ xem").Enable = IsRead_Enable, (Column "Sửa").Enable = IsEdit_Enable
        /// !Visible: chỉ xem, visible tất cả các button thao tác
        /// </summary>
        /// <param name="query"></param>
        /// <returns></returns>
        [CusAuthorize(Roles = "3900")]
        [Route("Get_DSChucNangUser")]
        [HttpGet]
        public object Get_DSChucNangUser([FromQuery] QueryParams query)
        {

            UserJWT loginData = Ulities.GetUserByHeader(HttpContext.Request.Headers);
            if (loginData == null)
                return JsonResultCommon.DangNhap();
            string a = _config.HRCatalog;
            query = query == null ? new QueryParams() : query;
            BaseModel<object> model = new BaseModel<object>();
            PageModel pageModel = new PageModel();
            ErrorModel error = new ErrorModel();
            string sqlq = "";
            SqlConditions Conds = new SqlConditions();
            string orderByStr = "Position asc", whereStr = " (CustemerID=@CustemerID or CustemerID is NULL)  and (GFunctionID in (select GroupFunctionID from Sys_Pack_GFunction inner join Tbl_Custemers on Sys_Pack_GFunction.PackID = Tbl_Custemers.PackID and Tbl_Custemers.RowID = @CustemerID)) and (loaihinh is null or loaihinh = (select loaihinh from Tbl_Custemers where rowid=@CustemerID))";

            bool Visible = true;
            try
            {

                Conds.Add("CustemerID", loginData.CustomerID);

                string strRe = "";
                if (string.IsNullOrEmpty(query.filter["ID_NhomChucNang"]))
                    strRe += "chức năng";

                if (string.IsNullOrEmpty(query.filter["ID_NhomUser"]))
                {
                    strRe += strRe == "" ? "" : ", ";
                    strRe += "nhóm người dùng";
                }
                if (strRe != "")
                    return JsonResultCommon.BatBuoc(strRe);

                whereStr += " and Id_group=@Id_group";
                Conds.Add("Id_group", query.filter["ID_NhomChucNang"]);
                Dictionary<string, string> sortableFields = new Dictionary<string, string>
                        {
                            { "Tenquyen", "Tenquyen"},
                        };
                if (!string.IsNullOrEmpty(query.sortField) && sortableFields.ContainsKey(query.sortField))
                {
                    orderByStr = sortableFields[query.sortField] + ("desc".Equals(query.sortOrder) ? " desc" : " asc");
                }

                using (DpsConnection cnn = new DpsConnection(_config.ConnectionString))
                {
                    sqlq = $@"SELECT count(*) from Tbl_Permision where { whereStr } ";
                    var dem = cnn.ExecuteScalar(sqlq, Conds);
                    if (cnn.LastError != null)
                        return JsonResultCommon.Exception(cnn.LastError, loginData.CustomerID, ControllerContext);
                    if (int.Parse(dem.ToString()) == 0)
                        return JsonResultCommon.KhongCoDuLieu();
                    sqlq = $@"select Id_permit, Tenquyen, LangKey, IsReadPermit from Tbl_Permision 
                        where { whereStr } order by { orderByStr}";
                    DataTable dt = cnn.CreateDataTable(sqlq, Conds);
                    dt.Columns.Add("IsRead", typeof(bool));
                    dt.Columns.Add("IsEdit", typeof(bool));
                    dt.Columns.Add("IsRead_Enable", typeof(bool));
                    dt.Columns.Add("IsEdit_Enable", typeof(bool));
                    // Phân trang
                    Conds = new SqlConditions();
                    Conds.Add("username", query.filter["ID_User"]);
                    DataTable dt_gruser = cnn.CreateDataTable($@"select id_permit, Edit 
                                                                from Tbl_Account_Permit 
                                                                where (where)", "(where)", Conds);
                    DataTable permit_group = cnn.CreateDataTable($@"select g.id_permit, g.Edit 
                                                                from tbl_Group_Permit g 
                                                                join tbl_Group_Account g_a 
                                                                on g.id_group=g_a.id_group 
                                                                where (where)", "(where)", Conds);
                    bool isadmin = false;
                    foreach (DataRow dr in dt.Rows)
                    {
                        dr["IsRead"] = false;
                        DataRow[] r = dt_gruser.Select("id_permit=" + dr["id_permit"]);
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
            }
            catch (Exception ex)
            {
                return JsonResultCommon.Exception(ex, loginData.CustomerID);
            }
        }

        /// <summary>
        /// Lưu quyền nhóm người dùng ---
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
       // [CusAuthorize(Roles = "3900")]
        [Route("Save_QuyenNhomNguoiDung")]
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
                using (DpsConnection cnn = new DpsConnection(_config.ConnectionString))
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
                        return JsonResultCommon.Exception(cnn.LastError, loginData.CustomerID, ControllerContext);
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
                            return JsonResultCommon.Exception(cnn.LastError, loginData.CustomerID, ControllerContext);
                        }
                    }
                    LogContent += " của nhóm " + arr_data[0].Ten + "(" + arr_data[0].ID + ")";
                    cnn.EndTransaction();
                    cnn.Disconnect();
                    //DpsPage.Ghilogfile(loginData.CustomerID.ToString(), LogContent, LogContent, loginData.UserName);
                }

                return JsonResultCommon.ThanhCong();
            }
            catch (Exception ex)
            {
                return JsonResultCommon.Exception(ex, loginData.CustomerID);
            }
        }

        /// <summary>
        /// Lưu quyền người dùng ---
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        //[CusAuthorize(Roles = "3900")]
        [Route("Save_QuyenNguoiDung")]
        [HttpPost]
        public async Task<BaseModel<object>> Save_QuyenNguoiDung(List<QuyenAddData> arr_data)
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
                using (DpsConnection cnn = new DpsConnection(_config.ConnectionString))
                {
                    Conds = new SqlConditions();
                    Conds.Add("username", arr_data[0].ID);
                    DataTable dt = cnn.CreateDataTable($@"select id_permit, Edit from tbl_Account_Permit where (where)", "(where)", Conds);
                    string status = $@"select id_permit, Edit from Tbl_Account_Permit where username='" + arr_data[0].ID + "'";
                    foreach (QuyenAddData data in arr_data)
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
                            else if (drow[0]["edit"].ToString().ToLower().Equals(chixem.ToString().ToLower()))
                            {
                            }
                        }
                        else
                        {
                        }
                    }
                    Conds = new SqlConditions();
                    Conds.Add("username", arr_data[0].ID);
                    Conds.Add("id_chucnang", arr_data[0].ID_NhomChucNang);
                    string cmd = $@"delete Tbl_Account_permit 
                                where (username=@username) and (id_permit in (select Id_Permit 
                                from tbl_permision where Id_group=@id_chucnang))";
                    int rs = cnn.ExecuteNonQuery(cmd, Conds);
                    if (rs == -1)
                    {
                        return JsonResultCommon.Exception(cnn.LastError, loginData.CustomerID, ControllerContext);
                    }
                    for (int i = 0; i < permit.Count; i++)
                    {
                        val = new Hashtable();
                        val.Add("username", arr_data[0].ID);
                        val.Add("id_permit", permit[i]);
                        bool edit = true;
                        if (ReadOnlyPermit.Contains(permit[i])) edit = false;
                        val.Add("edit", edit);
                        val.Add("id_chucnang", arr_data[0].ID_NhomChucNang);
                        if (cnn.Insert(val, "Tbl_Account_permit") == -1)
                        {
                            return JsonResultCommon.Exception(cnn.LastError, loginData.CustomerID, ControllerContext);
                        }
                    }
                    cnn.EndTransaction();
                    cnn.Disconnect();
                }
                return JsonResultCommon.ThanhCong();
            }
            catch (Exception ex)
            {
                return JsonResultCommon.Exception(ex, loginData.CustomerID);
            }
        }
        [HttpGet]
        [Route("GetRolesForUser_WeWork")]
        public object GetRolesForUser_WeWork(string username)
        {
            try
            {
                string[] listrole = Common.GetRolesForUser_WeWork(username);
                return listrole;
            }
            catch (Exception e)
            {
                return null;
            }


        }
    }
}
