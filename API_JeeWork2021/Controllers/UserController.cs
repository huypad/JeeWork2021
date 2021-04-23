using DpsLibs.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using JeeWork_Core2021.Models;
using JeeWork_Core2021.Classes;
using API.Controllers.Users;
using APIModel.APIModelFolder;

namespace JeeWork_Core2021.Controllers.Users
{

    [EnableCors("AllowOrigin")]
    [Route("api")]
    [ApiController]
   

    public class UserController : ControllerBase
    {
        private LoginController lc;
        private JeeWorkConfig _config;
        private readonly IHostingEnvironment _hostingEnvironment;
        UserManager dpsUserMr;
        public UserController(IOptions<JeeWorkConfig> config, IConfiguration configLogin, IHostingEnvironment hostingEnvironment)
        {
            _config = config.Value;
            lc = new LoginController(config, configLogin);
            dpsUserMr = new UserManager(config);
            _hostingEnvironment = hostingEnvironment;
        }
        [AllowAnonymous]
        [HttpPost]
        [Route("Login")]
        public async Task<BaseModel<object>> Login([FromBody] UserModel login)
        {
            BaseModel<object> model = new BaseModel<object>();
            if (login.checkReCaptCha)
            {
                var IsValid_capcha = await validateCaptchaAsync(login.GReCaptCha ?? "");
                if (!IsValid_capcha)
                {
                    return JsonResultCommon.Custom("Mã Captcha không hợp lệ!"); 
                }
            }
            long vt = 0;
            if (login.cur_vaitro.HasValue)
                vt = login.cur_vaitro.Value;
            var user = lc.AuthenticateUser(login.username, login.password, vt);

            if (user != null)
            {
                if (user.Active != 1)
                {
                    return JsonResultCommon.Custom("Tài khoản này đã bị khoá, vui lòng liên hệ quản trị viên.");
                }
                if (user.ExpDate < DateTime.Now)
                {
                    return JsonResultCommon.Custom("Tài khoản này đã bị khoá do hết thời hạn đăng nhập, vui lòng liên hệ quản trị viên.");
                }
                //logHelper.Log(6, user.Id, "Đăng nhập");
                return JsonResultCommon.ThanhCong(user);
            }
            return JsonResultCommon.Custom("Tài khoản hoặc mật khẩu không chính xác.");
        }

        [Authorize]
        [HttpGet]
        [Route("Logout")]
        public async Task<BaseModel<object>> Logout()
        {
            string Token = lc.GetHeader(Request);
            var user = lc._GetInfoUser(Token);

            if (user == null)
            {
                return JsonResultCommon.DangNhap();
            }
            //lưu log đăng xuất
            //logHelper.Log(5, user.Id, "Đăng xuất");
            return JsonResultCommon.ThanhCong();
        }
        [HttpPost]
        [Authorize()]
        [Route("ResetSession")]
        public BaseModel<object> ResetSession()
        {
            BaseModel<object> _baseModel = new BaseModel<object>();
            string Token = lc.GetHeader(Request);
            var user = lc._GetInfoUser(Token);

            if (user == null)
            {
                return JsonResultCommon.DangNhap();
            }
            var reset = lc.RefreshJSONWebToken(ref user);
            user.ResetToken = reset;
            return JsonResultCommon.ThanhCong(user);
        }

        private async Task<bool> validateCaptchaAsync(string captchares)
        {
            ErrorModel error = new ErrorModel();
            object jres = new object();
            if (String.IsNullOrEmpty(captchares))
            {
                return false;
            }
            //var data = WebAPI_TayNinh.Classs.Common.getConfig();
            string secret_key = _config.SecretKey;
            if (string.IsNullOrEmpty(secret_key))
            {
                error.message = "Captcha không hợp lệ";
                return false;
            }
            var content = new FormUrlEncodedContent(new[]
              {
                new KeyValuePair<string, string>("secret",  secret_key),
                new KeyValuePair<string, string>("response", captchares)
              });
            HttpClient client = new HttpClient();
            var res = await client.PostAsync("https://www.google.com/recaptcha/api/siteverify", content);
            captchaResult captchaRes = JsonConvert.DeserializeObject<captchaResult>(res.Content.ReadAsStringAsync().Result);

            if (!captchaRes.success)
            {
                error.message = "Captcha không hợp lệ";
                return false;
            }


            error.message = "Xác thực Capcha Thành công";
            return true;
        }


        [HttpPost]
        //[Authorize]
        [Route("test")]
        public BaseModel<object> test()
        {
            BaseModel<object> _baseModel = new BaseModel<object>();
            string Token = lc.GetHeader(Request);
            var user = lc._GetInfoUser(Token);
            return JsonResultCommon.ThanhCong(user);
        }
        }

    public class captchaResult
    {
        public bool success { get; set; }
        public DateTime challenge_ts { get; set; }
        public string hostname { get; set; }
        [JsonProperty("error-codes")]
        public List<string> errors { get; set; }
    }
    public class UserModel
    {
        public string username { get; set; }
        public string password { get; set; }
        public bool checkReCaptCha { get; set; }
        public string GReCaptCha { get; set; }
        public long? cur_vaitro { get; set; }
    }
    internal class ThongTinPhanMem
    {
        public string IdApp { get; set; }
        public string AppName { get; set; }
        public string HomePath { get; set; }
        public string IdCus { get; set; }
    }
}
