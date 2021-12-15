using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DpsLibs.Data;
using System.Data;
using System.Linq;
using System.Security.Claims;
using System.Security.Principal;
using DpsLibs;
using System.Collections;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using System.Net.Mail;
using System.IO;
using Microsoft.AspNetCore.Hosting;
using System.Collections.Specialized;
using JeeWork_Core2021.Models;
using JeeWork_Core2021.Classes;
using DPSinfra.ConnectionCache;
using JeeWork_Core2021.Controllers.Wework;

namespace JeeWork_Core2021.Classes
{
    /// <summary>
    /// quản lý tài khoản
    /// </summary>
    public class UserManager
    {
        private JeeWorkConfig _config;
        private const string PASSWORD_ED = "rpNuGJebgtBEp0eQL1xKnqQG";
        private IConnectionCache ConnectionCache;
        private IConfiguration _configuration;
        //private IOptions<JRConfig> MailConfig;
        //private readonly IHostingEnvironment _hostingEnvironment;
        public UserManager( IConnectionCache _cache, IConfiguration configuration)
        {
            ConnectionCache = _cache;
            _configuration = configuration;
        }
        public UserManager(IOptions<JeeWorkConfig> configLogin, IConnectionCache _cache, IConfiguration configuration)
        {
            _config = configLogin.Value;
            ConnectionCache = _cache;
            _configuration = configuration;
        }
        //public UserManager(IOptions<JRConfig> configLogin, IHostingEnvironment hostingEnvironment)
        //{
        //    _config = configLogin.Value;
        //    //MailConfig = configLogin;
        //    _hostingEnvironment = hostingEnvironment;
        //}
        #region Quản lý người dùng
        /// <summary>
        /// Tìm người dùng
        /// </summary>
        /// <param name="userName"></param>
        /// <param name="password"></param>
        /// <returns></returns>
        public LoginData FindAsync(string userName, string password,long CustomerID, long cur_Vaitro = 0)
        {
            DataTable Tb = null;
            string ConnectionString = JeeWorkLiteController.getConnectionString(ConnectionCache, CustomerID, _configuration);
            using (DpsConnection Conn = new DpsConnection(ConnectionString))
            {
                //                string sqlq = @"select u.*, dm.title as DonVi,dm.Code as MaDinhDanh, dm.Capcocau, dm.ID_Goc,cc1.ID_Goc as Id_Goc_Cha from Dps_User u 
                //join Tbl_Cocautochuc dm on u.IdDonVi=dm.RowID
                //left join Tbl_Cocautochuc cc1 on dm.ParentID=cc1.RowID
                //where u.Deleted = 0 and u.Username = @UserName";
                string sqlq = @"select *  from Tbl_Account join Tbl_NhanVien on Tbl_Account.Id_nv = Tbl_NhanVien.Id_nv where Username = @UserName";
                Tb = Conn.CreateDataTable(sqlq, new SqlConditions() { { "UserName", userName } });
                if (Tb == null || Tb.Rows.Count != 1)
                    return null;
                var rw = Tb.Rows[0];
                // mã hóa mật khẩu
                //string hash = rw["PasswordHash"].ToString();
                //string pass = DpsLibs.Common.EncDec.Decrypt(hash, PASSWORD_ED);
                string pass = rw["Password"].ToString();
                if (password.Equals(pass))
                {
                    LoginData _Dpsuser = new LoginData();
                    //_Dpsuser.Id = long.Parse(rw["Id_nv"].ToString());
                    _Dpsuser.UserName = rw["UserName"].ToString();
                    //_Dpsuser.FullName = rw["Holot"].ToString()+ " " + rw["Ten"].ToString();
                    //_Dpsuser.ChucVu = rw["IdChucVu"] != DBNull.Value ? rw["IdChucVu"].ToString() : "";
                    //_Dpsuser.Email = rw["Email"] != DBNull.Value ? rw["Email"].ToString() : "";
                    //_Dpsuser.SDT = rw["Mobile"] != DBNull.Value ? rw["Mobile"].ToString() : "";
                    //_Dpsuser.IdDonVi = rw["IdDonVi"] != DBNull.Value ? int.Parse(rw["IdDonVi"].ToString()) : 0;
                    //_Dpsuser.Capcocau = rw["Capcocau"] != DBNull.Value ? int.Parse(rw["Capcocau"].ToString()) : 0;
                    //_Dpsuser.ID_Goc = rw["ID_Goc"] != DBNull.Value ? int.Parse(rw["ID_Goc"].ToString()) : 0;
                    //_Dpsuser.ID_Goc_Cha = rw["ID_Goc_Cha"] != DBNull.Value ? int.Parse(rw["ID_Goc_Cha"].ToString()) : 0;
                    //_Dpsuser.DonVi = rw["DonVi"] != DBNull.Value ? rw["DonVi"].ToString() : "";
                    //_Dpsuser.MaDinhDanh = rw["MaDinhDanh"] != DBNull.Value ? rw["MaDinhDanh"].ToString() : ""; 
                    //_Dpsuser.Active = rw["Lock"] != DBNull.Value ? int.Parse(rw["Lock"].ToString()) : 0;
                    //_Dpsuser.Avatar = LiteController.genLinkAvatar(_config.LinkAPI, rw["Avatar"]);
                    //_Dpsuser.Avatar = LiteController.genLinkAvatar(_config.LinkAPI, $"{long.Parse(rw["Id_nv"].ToString())}.jpg");
                    //_Dpsuser.LastUpdatePass = (DateTime)rw["LastUpdatePass"];
                    //_Dpsuser.IdTinh = long.Parse(_config.IdTinh);
                    //string strVT = "select IdGroup, GroupName from Dps_User_GroupUser ug join Dps_UserGroups g on ug.IdGroupUser=g.IdGroup where IdUser=" + _Dpsuser.Id + " and ug.Disabled=0 and ug.Locked=0 order by Priority";
                    //DataTable dt = Conn.CreateDataTable(strVT);
                    //if (dt != null && dt.Rows.Count > 0)
                    //{
                    //    if (cur_Vaitro > 0)
                    //    {
                    //        var find = dt.Select("IdGroup=" + cur_Vaitro);
                    //        if(find.Count()>0)
                    //        {
                    //            _Dpsuser.VaiTro = int.Parse(find[0]["IdGroup"].ToString());
                    //            _Dpsuser.TenVaiTro = find[0]["GroupName"].ToString();
                    //        }    
                    //    }
                    //    if(_Dpsuser.VaiTro==0)
                    //    {
                    //        _Dpsuser.VaiTro = int.Parse(dt.Rows[0]["IdGroup"].ToString());
                    //        _Dpsuser.TenVaiTro = dt.Rows[0]["GroupName"].ToString();
                    //    }
                    //}
                    //else
                    //{
                    //    _Dpsuser.VaiTro = 0;
                    //    _Dpsuser.TenVaiTro = "";
                    //}

                    //string str = "select * from Sys_Config where Code='EXP_PASS'";
                    //DataTable conf = Conn.CreateDataTable(str);
                    //if (conf != null && conf.Rows.Count > 0)
                    //{
                    //    int num = int.Parse(conf.Rows[0]["Value"].ToString());
                    //    if (num == 0)//k xét thời hạn
                    //        _Dpsuser.ExpDate = null;
                    //    else
                    //    {
                    //        DateTime exp = _Dpsuser.LastUpdatePass.AddDays(num + (int)rw["GiaHan"]);
                    //        _Dpsuser.ExpDate = exp;
                    //    }
                    //}
                    return _Dpsuser;
                }
            }
            return null;
        }
        
        /// <summary>
        /// đổi mật khẩu
        /// </summary>
        /// <param name="iduser">id người dùng</param>
        /// <param name="oldpassword">mật khẩu cũ</param>
        /// <param name="password">mật khẩu mới</param>
        /// <returns></returns>
        public BaseModel<object> ChangePass(string iduser, string oldpassword, string password,long CustomerID)
        {
            if (string.IsNullOrEmpty(password) || password.Length < 6)
                return JsonResultCommon.Custom("Mật khẩu mới quá ngắn");
            string ConnectionString = JeeWorkLiteController.getConnectionString(ConnectionCache, CustomerID, _configuration);
            using (DpsConnection Conn = new DpsConnection(ConnectionString))
            {
                var Tb = Conn.CreateDataTable("select PasswordHash from Dps_User where UserID = @Id", new SqlConditions() { { "Id", iduser } });
                if (Tb == null || Tb.Rows.Count != 1)
                    return JsonResultCommon.KhongTonTai();
                if (!oldpassword.Equals(DecryptPassword(Tb.Rows[0]["PasswordHash"].ToString())))
                    return JsonResultCommon.Custom("Mật khẩu cũ không chính xác");
                string newpass = EncryptPassword(password);
                var val = new Hashtable();
                val.Add("PasswordHash", newpass);
                val.Add("LastUpdatePass", Common.GetDateTime());
                val.Add("GiaHan", 0);
                if (Conn.Update(val, new SqlConditions { new SqlCondition("UserID", iduser) }, "Dps_User") != 1)
                {
                    //return JsonResultCommon.SQL(Conn.LastError.Message);
                }
                return JsonResultCommon.ThanhCong();
            }
        }
        ///// <summary>
        ///// reset mật khẩu
        ///// </summary>
        ///// <param name="iduser"></param>
        ///// <param name="password"></param>
        ///// <returns></returns>
        //public string ResetPass(string iduser, string password, long CustomerID)
        //{
        //    using (DpsConnection Conn = new DpsConnection(ConnectionCache.GetConnectionString(CustomerID)))
        //    {
        //        var Tb = Conn.CreateDataSet(@"select * from Dps_User where UserID = @Id 
        //                                        select * from Sys_Config where Code='SEND_MAIL_RESET_PASS'", new SqlConditions() { { "Id", iduser } });
        //        if (Tb == null || Tb.Tables[0].Rows.Count != 1)
        //            return "Tài khoản không tồn tại";
        //        string newpass = EncryptPassword(password);
        //        var val = new Hashtable();
        //        val.Add("PasswordHash", newpass);
        //        val.Add("LastUpdatePass", Common.GetDateTime());
        //        val.Add("GiaHan", 0);
        //        Conn.BeginTransaction();
        //        if (Conn.Update(val, new SqlConditions { new SqlCondition("UserID", iduser) }, "Dps_User") != 1)
        //        {
        //            Conn.RollbackTransaction();
        //            return "Không thể thay đổi mật khẩu";
        //        }


        //        #region gửi mail

        //        try
        //        {
        //            if (Tb.Tables[1].Rows.Count > 0)
        //            {
        //                if (Tb.Tables[1].Rows[0]["Value"].ToString() == "1")
        //                {
        //                    if (string.IsNullOrEmpty(Tb.Tables[0].Rows[0]["Email"].ToString()))
        //                    {
        //                        Conn.RollbackTransaction();
        //                        return "Không thể thay đổi mật khẩu";// "Người dùng không có thông tin Email";
        //                    }
        //                    string Error = "";

        //                    //string strHTML = System.IO.File.ReadAllText(_config.LinkAPI + JeeWork_Constant.TEMPLATE_IMPORT_FOLDER + "/User_ForgetPass.html");
        //                    Hashtable kval = new Hashtable();
        //                    kval.Add("{{NewPass}}", password);
        //                    kval.Add("$nguoinhan$", Tb.Tables[0].Rows[0]["Fullname"]);
        //                    kval.Add("$SysName$", _config.SysName);

        //                    MailAddressCollection Lstcc = new MailAddressCollection();
        //                    MailInfo minfo = new MailInfo(MailConfig.Value, int.Parse(Tb.Tables[0].Rows[0]["IdDonVi"].ToString()));
        //                    if (minfo.Id > 0)
        //                    {
        //                        string fileTemp = Path.Combine(_hostingEnvironment.ContentRootPath, JeeWork_Constant.TEMPLATE_IMPORT_FOLDER + "/User_ForgetPass.html");
        //                        var rs = SendMail.Send(fileTemp, kval, Tb.Tables[0].Rows[0]["Email"].ToString(), "RESET MẬT KHẨU NGƯỜI DÙNG", Lstcc, Lstcc, null, false, out Error, minfo);
        //                        if (!string.IsNullOrEmpty(Error))
        //                        {
        //                            Conn.RollbackTransaction();
        //                            return "Không thể thay đổi mật khẩu";//"Gửi mail thất bại";
        //                        }
        //                    }
        //                }
        //            }
        //        }
        //        catch (Exception ex)
        //        {
        //            Conn.RollbackTransaction();
        //            return "Không thể thay đổi mật khẩu";// "Gửi mail thất bại";
        //        }

        //        #endregion




        //        Conn.EndTransaction();
        //        return "";
        //    }
        //}
        /// <summary>
        /// kiểm tra người dùng có tồn tại
        /// </summary>
        /// <param name="UserNameorID">id người dùng hoặc tên đăng nhập</param>
        /// <param name="loai">0: kiểm tra bằng ID, 1: username</param>
        /// <returns></returns>
        public bool CheckNguoiDung(string UserNameorID, int loai, long CustomerID)
        {
            DataTable Tb = null;
            string ConnectionString = JeeWorkLiteController.getConnectionString(ConnectionCache, CustomerID, _configuration);
            using (DpsConnection Conn = new DpsConnection(ConnectionString))
            {
                SqlConditions sqlcond = new SqlConditions();

                string sqlq = "";
                if (loai == 1)
                {
                    sqlcond.Add("UserName", UserNameorID);
                    sqlq = "select [UserID] from Dps_User where Deleted = 0 and UserName = @UserName";
                }
                if (loai == 0)
                {
                    sqlcond.Add("Id", UserNameorID);
                    sqlq = "select [UserID] from Dps_User where Deleted = 0 and UserID = @Id";
                }
                Tb = Conn.CreateDataTable(sqlq, sqlcond);
            }
            if (Tb.Rows.Count == 1)
            {
                return true;
            }
            return false;
        }

        /// <summary>
        /// kiểm tra email người dùng có tồn tại
        /// </summary>
        /// <param name="email">email</param>
        /// <param name="UserId">0: khi insert, 1: khi update</param>
        /// <returns></returns>
        public bool CheckEmail(string email, long UserId,long CustomerID)
        {
            DataTable Tb = null;
            string ConnectionString = JeeWorkLiteController.getConnectionString(ConnectionCache, CustomerID, _configuration);
            using (DpsConnection Conn = new DpsConnection(ConnectionString))
            {
                SqlConditions sqlcond = new SqlConditions();

                string sqlq = "";
                string idstr = "";


                sqlcond.Add("email", email);
                if (UserId > 0)
                {
                    idstr = " and UserID <> @Id";
                    sqlcond.Add("Id", UserId);
                }
                sqlq = $"select [UserID] from Dps_User where Deleted = 0 {idstr} and Email=@email";

                Tb = Conn.CreateDataTable(sqlq, sqlcond);
            }
            if (Tb.Rows.Count == 1)
            {
                return true;
            }
            return false;
        }
        #endregion

        #region mã hóa

        //mã hoá mật khẩu
        public string EncryptPassword(string password)
        {
            return DpsLibs.Common.EncDec.Encrypt(password, PASSWORD_ED);
        }
        public string DecryptPassword(string password)
        {
            return DpsLibs.Common.EncDec.Decrypt(password, PASSWORD_ED);
        }
        #endregion
    }
}