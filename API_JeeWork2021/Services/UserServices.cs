using JeeWork_Core2021.Classes;
using JeeWork_Core2021.Helpers;
using JeeWork_Core2021.Models;
using DpsLibs.Data;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using JeeWork_Core2021.Models.AuthorizeConnect;

namespace JeeWork_Core2021.Services
{
    public interface IUserService
    {
        AuthenticateResponse Authenticate(AuthenticateRequest model, string ipAddress);
        AuthenticateResponse RefreshToken(string token, string ipAddress);
        bool RevokeToken(string token, string ipAddress);
        IEnumerable<UserJWT> GetAll();
        UserJWT GetById(int id);
    }

    public class UserService : IUserService
    {
        // users hardcoded for simplicity, store in a db with hashed passwords in production applications
        private List<UserJWT> _users = new List<UserJWT>
        {
            new UserJWT { UserID = 1, FirstName = "Test", LastName = "User", Username = "test", Password = "test", WhenLog = DateTime.Now.Ticks }
        };

        private readonly AppSettings _appSettings;

        public UserService(IOptions<AppSettings> appSettings)
        {
            _appSettings = appSettings.Value;
        }


        public AuthenticateResponse Authenticate(AuthenticateRequest model, string ipAddress)
        {
            try
            {
                using (DpsConnection cnn = new DpsConnection(ConfigurationManager_JeeWork.ConnDps.ConnectSource))
                {
                    //                    SELECT acc.[AutoID],acc.[UserName],acc.[FirstName],acc.[LastName],acc.[LastLogin],
                    //acc.Password, CONVERT(VARCHAR(32), HashBytes('MD5', 'pass'), 2) as appWhere,
                    //acc.passhint, EncryptByPassPhrase('nemo8872', N'pass'), CONVERT(VARCHAR(4000), DECRYPTBYPASSPHRASE('nemo8872', acc.passhint))

                    //    FROM[dbo].[Tbl_Account] acc
                    //   where acc.UserName = 'user' and acc.[Password] = CONVERT(VARCHAR(32), HashBytes('MD5', 'pass'), 2)
                    string sql = $@"EXEC spn_Login_User @UserName, @Pass";
                    SqlConditions conds = new SqlConditions
                    {
                        { "UserName", model.Username },
                        { "Pass", model.Password },
                    };

                    DataTable dt = cnn.CreateDataTable(sql, conds);
                    if (cnn.LastError != null || dt == null)
                        return null;
                    if (dt.Rows.Count.Equals(0))
                        return null;

                    // authentication successful so generate jwt token
                    UserJWT _user = dt.AsEnumerable().Select(x => new UserJWT
                    {
                        UserID = int.Parse(x["AutoId"].ToString()),
                        FirstName = x["FirstName"].ToString(),
                        LastName = x["LastName"].ToString(),
                        Username = x["UserName"].ToString(),
                        Password = string.Empty,
                        WhenLog = DateTime.Now.Ticks,
                        TokenSys = x["GuidID"].ToString(),
                        RefreshTokens = new List<RefreshToken>()

                    }).SingleOrDefault();

                    sql = $@"EXEC spn_Update_LoginSession @IdUser, @IdApp, @GuidID";
                    cnn.ExecuteNonQuery(sql, new SqlConditions { { "IdUser", _user.UserID }, { "IdApp", AppSessionConstants.Web }, { "GuidID", _user.TokenSys } });

                    var token = generateJwtToken(_user);
                    var refreshToken = generateRefreshToken(ipAddress);
                    _user.RefreshTokens.Add(refreshToken);

                    sql = $@"EXEC spn_Update_Refresh_Token @IdUser, @IpAddress, @IdOldToken, @NewRfTok, @Token";
                    cnn.ExecuteNonQuery(sql, new SqlConditions { { "IdUser", _user.UserID }, { "IpAddress", ipAddress }, { "IdOldToken",0 }, { "NewRfTok", refreshToken.Token }, { "Token", token } });

                    return new AuthenticateResponse(_user, token, refreshToken.Token);

                }
            }
            catch (Exception ex)
            {
                //error exception when get value JWT
                return null;
            }
        }

        /// <summary>
        /// Function refresh token by API
        /// </summary>
        /// <param name="token"></param>
        /// <param name="ipAddress"></param>
        /// <returns></returns>
        public AuthenticateResponse RefreshToken(string token, string ipAddress)
        {
            try
            {
                using (DpsConnection cnn = new DpsConnection(ConfigurationManager_JeeWork.ConnDps.ConnectSource))
                {
                    string sqlq = $@"EXEC spn_Get_User_By_Token @Token";
                    SqlConditions conds = new SqlConditions { { "Token", token } };
                    DataTable dt = cnn.CreateDataTable(sqlq, conds);
                    if (cnn.LastError != null || dt == null) return null;
                    if (dt.Rows.Count.Equals(0)) return null;
                    // authentication successful so generate jwt token
                    UserJWT _user = dt.AsEnumerable().Select(x => new UserJWT
                    {
                        UserID = int.Parse(x["IdUser"].ToString()),
                        FirstName = x["FirstName"].ToString(),
                        LastName = x["LastName"].ToString(),
                        Username = x["UserName"].ToString(),
                        Password = string.Empty,
                        WhenLog = DateTime.Now.Ticks,
                        TokenSys = x["GuidID"].ToString(),
                        RefreshTokens = new List<RefreshToken>
                        {
                           new RefreshToken{ 
                               Id = int.Parse(x["IdJWT"].ToString()),
                               Token = x["Token"].ToString()
                           }
                        }

                    }).SingleOrDefault();

                    // replace old refresh token with a new one and save
                    var newRefreshToken = generateRefreshToken(ipAddress);
                    RefreshToken _ref = _user.RefreshTokens[0];
                    _ref.Revoked = DateTime.UtcNow;
                    _ref.RevokedByIp = ipAddress;
                    _ref.ReplacedByToken = newRefreshToken.Token;
                    _user.RefreshTokens.Add(newRefreshToken);

                    sqlq = "EXEC spn_Update_Refresh_Token @IdUser, @IpAddress, @IdOldToken, @NewRfTok";
                    conds = new SqlConditions { { "IdUser", _user.UserID }, { "IpAddress", ipAddress },
                        { "IdOldToken", _user.RefreshTokens[0].Id },
                        { "NewRfTok", newRefreshToken.Token },
                    };
                    cnn.ExecuteNonQuery(sqlq, conds);

                    // generate new jwt
                    var jwtToken = generateJwtToken(_user);

                    return new AuthenticateResponse(_user, jwtToken, newRefreshToken.Token);

                }
                
            }
            catch (Exception ex)
            {
                return null;
            }

        }

        public IEnumerable<UserJWT> GetAll()
        {
            return _users;
        }

        public UserJWT GetById(int id)
        {
            return _users.FirstOrDefault(x => x.UserID == id);
        }

        // helper methods

        private string generateJwtToken(UserJWT user)
        {
            // generate token that is valid for 7 days
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.ASCII.GetBytes(_appSettings.Secret);
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                //Subject = new ClaimsIdentity(new[] { new Claim("id", user.Id.ToString()) }),
                Subject = new ClaimsIdentity(new[] { new Claim(JeeWorkConstant._User, JsonConvert.SerializeObject(user)) }),
                Expires = DateTime.UtcNow.AddDays(7),
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)

            };
            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        }


        private RefreshToken generateRefreshToken(string ipAddress)
        {
            using (var rngCryptoServiceProvider = new RNGCryptoServiceProvider())
            {
                var randomBytes = new byte[64];
                rngCryptoServiceProvider.GetBytes(randomBytes);
                return new RefreshToken
                {
                    Token = Convert.ToBase64String(randomBytes),
                    Expires = DateTime.UtcNow.AddDays(7),
                    Created = DateTime.UtcNow,
                    CreatedByIp = ipAddress
                };
            }
        }

        public bool RevokeToken(string token, string ipAddress)
        {
            try
            {
                using (DpsConnection cnn = new DpsConnection(ConfigurationManager_JeeWork.ConnDps.ConnectSource))
                {
                    string sqlq = $@"EXEC spn_Update_Revoke_Token @TokenCur, @RevokedByIp";
                    SqlConditions conds = new SqlConditions { { "TokenCur", token }, { "RevokedByIp", ipAddress } };
                    cnn.ExecuteNonQuery(sqlq, conds);
                    if(cnn.LastError == null) 
                        return true;
                    return false;
                }
            }
            catch(Exception ex)
            {
                return false;
            }
           
        }


    }




}
