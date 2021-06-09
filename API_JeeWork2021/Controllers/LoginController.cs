using DpsLibs.Data;
using JeeWork_Core2021.Classes;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Net.Http;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using JeeWork_Core2021.Models;
using APIModel.APIModelFolder;
using UserManager = JeeWork_Core2021.Classes.UserManager;
using DPSinfra.ConnectionCache;

namespace JeeWork_Core2021.Controllers.Users
{
    public class LoginController : ControllerBase
    {
        LoginController lc;
        private IConfiguration _config;
        private UserManager CustomUserManager { get; set; }
        private IConnectionCache ConnectionCache;

        public LoginController()
        {
            // hàm khởi tạo không tham số
        }
        public LoginController(IOptions<JeeWorkConfig> config, IConfiguration configLogin, IConnectionCache _cache)
        {
            ConnectionCache = _cache;
            // hàm khởi tạo có tham số
            CustomUserManager = new UserManager(config,_cache);
            _config = configLogin;
        }
        // trả về thông tin user
        public LoginData AuthenticateUser(string username, string password,long CustomerID, long cur_Vaitro = 0)
        {
            try
            {
                var account = CustomUserManager.FindAsync(username, password, CustomerID, cur_Vaitro);
                if (account != null)
                {
                    //account.Rules = CustomUserManager.GetRules(username);
                    account.Token = GenerateJSONWebToken(account);
                    return account;
                }
                return null;

            }
            catch (Exception ex)
            {
                return null;
            }

        }
        // tạo mã JWT
        private string GenerateJSONWebToken(LoginData userInfo)
        {
            string json = JsonConvert.SerializeObject(userInfo);
            LoginData account = JsonConvert.DeserializeObject<LoginData>(json);
            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:access_secret"]));
            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

            var claims = new List<Claim>();
            if (account.Rules != null)
            {
                foreach (var role in account.Rules)
                {
                    claims.Add(new Claim(ClaimTypes.Role, role.ToString()));
                }
            }
            account.Rules = null;
            account.Token = "";
            claims.Add(new Claim("user", JsonConvert.SerializeObject(account)));
            claims.Add(new Claim(JwtRegisteredClaimNames.Sub, account.UserName));
            claims.Add(new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()));

            var token = new JwtSecurityToken(
              // _config["Jwt:Issuer"],
              //_config["Jwt:Issuer"],
              null,
              null,
              claims,
              expires: DateTime.Now.AddHours(24),
              signingCredentials: credentials);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        // làm mới token
        public string RefreshJSONWebToken(ref LoginData account)
        {
            //account.Rules = CustomUserManager.GetRules(account.UserName);
            string Token = GenerateJSONWebToken(account);
            return Token;
        }

        // lấy danh sách quyền
        public List<string> _GetAllRuleUser(string token)
        {
            var handler = new JwtSecurityTokenHandler();
            try
            {
                token = token.Replace("Bearer ", string.Empty);

                var tokenS = handler.ReadJwtToken(token) as JwtSecurityToken;

                List<string> rules = new List<string>();

                foreach (var r in tokenS.Claims.Where(x => x.Type == "http://schemas.microsoft.com/ws/2008/06/identity/claims/role").ToList())
                {
                    rules.Add(r.Value);
                }
                return rules;

            }
            catch (Exception ex)
            {
                return null;
            }
        }

        // Convert token ra thông tin nhân viên
        public LoginData _GetInfoUser(string token)
        {
            var handler = new JwtSecurityTokenHandler();
            try
            {
                token = token.Replace("Bearer ", string.Empty);

                var tokenS = handler.ReadJwtToken(token) as JwtSecurityToken;
                long exp = long.Parse(tokenS.Claims.First(claim => claim.Type == "exp").Value);
                DateTimeOffset dateTimeOffset = DateTimeOffset.FromUnixTimeSeconds(exp);
                LoginData account = JsonConvert.DeserializeObject<LoginData>(tokenS.Claims.First(claim => claim.Type == "customdata").Value);
                account.Token = token;
                account.UserName = tokenS.Claims.First(claim => claim.Type == "username").Value;
                if (dateTimeOffset <= DateTime.Now)
                    return null;
                return account;

            }
            catch (Exception ex)
            {
                return null;
            }
        }
        //get token
        public string GetHeader(HttpRequest request)
        {
            try
            {
                Microsoft.Extensions.Primitives.StringValues headerValues;
                request.Headers.TryGetValue("Authorization", out headerValues);
                return headerValues.FirstOrDefault();
            }
            catch (Exception ex)
            {
                return string.Empty;
            }

        }
    }
}
