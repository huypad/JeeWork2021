using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace JeeWork_Core2021.Models
{
    public class LoginModel : BaseModel<LoginData>
    {
    }
    public class LoginData
    {
        //public static long IDKHDPS { get; set; }
        public long Id { get; set; }
        /// <summary>
        /// Tên đăng nhập
        /// </summary>
        public string UserName { get; set; }
        /// <summary>
        /// Họ
        /// </summary>
        public string FirstName { get; set; } = "";
        /// <summary>
        /// Tên
        /// </summary>
        public string LastName { get; set; } = "";
        /// <summary>
        /// Trạng thái (0 là khoá, 1 là kích hoạt, 2:Kích hoạt để gia hạn gói, 4: Kích hoạt để nâng cấp gói, 5: Đăng nhập thiết bị khác)
        /// </summary>
        public int Status { get; set; } = 0;
        /// <summary>
        /// Mảng quyền của người dùng
        /// </summary>
        public List<int> Rules { get; set; }
        /// <summary>
        /// ID khách hàng dps (id nhóm đa người dùng)
        /// </summary>
        public long IDKHDPS { get; set; }
        /// <summary>
        /// ID khách hàng dps (Đã mã hóa)
        /// </summary>
        public string IDKHDPS_Encode { get; set; }
        /// <summary>
        /// Loại người dùng (-1: admin dps; 0: user gốc; 1: user thường)
        /// </summary>
        public int UserType { get; set; } = 0;
        public string FullName { get { return FirstName + " " + LastName; } }
        public string Token { get; set; }
        public string SecurityStamp { get; set; } = "dps";
        /// <summary>
        /// Số lần nhập sai mật khẩu
        /// </summary>
        public int SoLuong { get; set; } = 0;
        public string Logo { get; set; }
        public string Domain { get; set; }
        /// <summary>
        /// Cho phép nhân viên lấy mẫu khuôn mặt
        /// </summary>
        public bool allowRegister { get; set; }
        /// <summary>
        /// Cho phép xác thực chấm công GPS
        /// </summary>
        public bool isCheckCapcha { get; set; }
        public int LoaiHinh { get; set; }
        public int VaiTro { get; set; }
        public string TenVaiTro { get; set; }
        public DateTime ExpDate { get; set; }
    }
    public class LoginViewModel
    {
        public LoginViewModel() { }
        [Required(ErrorMessage = "Vui nhập tên đăng nhập.")]
        [MaxLength(99, ErrorMessage = "Tài khoản tối đa 99 ký tự.")]
        [DisplayName("Tài khoản")]
        public string UserName { get; set; }
        [Required(ErrorMessage = "Mật khẩu không được để trống.")]
        [DisplayName("Mật khẩu")]
        public string Password { get; set; }
        [DisplayName("Ghi nhớ")]
        public bool isPersistent { get; set; }
        public string ReturnUrl { get; set; }
    }

    public class LoginAppViewModel  //dành cho đăng nhập bên mobile
    {
        public LoginAppViewModel() { }
        [Required(ErrorMessage = "Vui nhập tên đăng nhập.")]
        [MaxLength(99, ErrorMessage = "Tài khoản tối đa 99 ký tự.")]
        [DisplayName("Tài khoản")]
        public string UserName { get; set; }
        [Required(ErrorMessage = "Mật khẩu không được để trống.")]
        [DisplayName("Mật khẩu")]
        public string Password { get; set; }
        [DisplayName("Ghi nhớ")]
        public bool isPersistent { get; set; }
        public string ReturnUrl { get; set; }
        public string IdCho { get; set; }
    }

    public class LoginAPIModel
    {
        public string UserName { get; set; }
        public string Password { get; set; }
        public string Token { get; set; }
    }
    public class UserJWT
    {
        public int UserID { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Username { get; set; }
        public string Token { get; set; }
        [System.Text.Json.Serialization.JsonIgnore]
        public string Password { get; set; }
        /// <summary>
        /// Get time login or access
        /// </summary>
        [System.Text.Json.Serialization.JsonIgnore]
        public long? WhenLog { get; set; }
        public long CustomerID { get; set; }

        /// <summary>
        /// Get token of sys of DB , not compulsory
        /// </summary>
        [System.Text.Json.Serialization.JsonIgnore]
        public string TokenSys { get; set; }

        [System.Text.Json.Serialization.JsonIgnore]
        public List<RefreshToken> RefreshTokens { get; set; }
        public CustomData customdata { get; set; }
        public object appCode { get; set; }
    }
    public class RefreshToken
    {
        [System.Text.Json.Serialization.JsonIgnore]
        public int Id { get; set; }

        public string Token { get; set; }
        public DateTime Expires { get; set; }
        public bool IsExpired => DateTime.UtcNow >= Expires;
        public DateTime Created { get; set; }
        public string CreatedByIp { get; set; }
        public DateTime? Revoked { get; set; }
        public string RevokedByIp { get; set; }
        public string ReplacedByToken { get; set; }
        public bool IsActive => Revoked == null && !IsExpired;
    }
    public class CustomData
    {
        public PersonalInfo personalInfo { get; set; }
        [JsonPropertyName("jee-work")]
        [JsonProperty("jee-work")]
        public JeeWork JeeWork { get; set; }
        [JsonPropertyName("jee-account")]
        [JsonProperty("jee-account")]
        public JeeAccount jeeAccount { get; set; }

    }
    public class JeeAccount
    {
        public string customerID { get; set; }
        public object appCode { get; set; }
        public int userID { get; set; }
    }
    public class PersonalInfo
    {
        public string Avatar { get; set; }
        public string Firstname { get; set; }
        public string Lastname { get; set; }
        public string Fullname { get; set; }
        public string Jobtitle { get; set; }
        public string Department { get; set; }
        public string Birthday { get; set; }
        public string Phonenumber { get; set; }
    }
    public class JeeWork
    {
        public string WeWorkRoles { get; set; }
    }
    public class AccUsernameModel
    {
        public long UserId { get; set; }
        public string Username { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string FullName { get; set; }
        public string Jobtitle { get; set; }
        public string Department { get; set; }
        public string AvartarImgURL { get; set; }
        public string PhoneNumber { get; set; }
        public long CustomerID { get; set; }
        public string Email { get; set; }
        public bool isAdmin { get; set; }
    }
    public class ObjCustomData
    {
        public string userId { get; set; }
        public string updateField { get; set; }
        public object fieldValue { get; set; }
    }
}
