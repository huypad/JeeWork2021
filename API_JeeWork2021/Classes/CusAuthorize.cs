using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Threading.Tasks;
using JeeWork_Core2021.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Net.Http.Headers;
using Newtonsoft.Json;

namespace JeeWork_Core2021.Classes
{
    public class CusAuthorize : ActionFilterAttribute
    {
        public string Roles { get; set; }
        public override void OnActionExecuting(ActionExecutingContext actionContext)
        {
            try
            {
                IHeaderDictionary _d = actionContext.HttpContext.Request.Headers;

                if (!_d.ContainsKey(HeaderNames.Authorization))
                    actionContext.Result = new JsonResult(new { message = "Unauthorized" }) { StatusCode = StatusCodes.Status401Unauthorized };

                string _bearer_token, _user;

                _bearer_token = _d[HeaderNames.Authorization].ToString().Replace("Bearer ", "");
                var handler = new JwtSecurityTokenHandler();
                var tokenS = handler.ReadToken(_bearer_token) as JwtSecurityToken;

                if (DateTime.UtcNow > tokenS.ValidTo)
                {
                    actionContext.Result = new JsonResult(new { message = "Expired" }) { StatusCode = StatusCodes.Status408RequestTimeout };
                }
                else
                {
                    _user = tokenS.Claims.Where(x => x.Type == JeeWorkConstant._User).FirstOrDefault().Value;
                    if (string.IsNullOrEmpty(_user))
                        actionContext.Result = new JsonResult(new { message = "UserNotFound" }) { StatusCode = StatusCodes.Status404NotFound };

                    UserJWT q = JsonConvert.DeserializeObject<UserJWT>(_user);
                }
            }
            catch (Exception ex)
            {
                actionContext.Result = new JsonResult(new { message = "Forbidden" }) { StatusCode = StatusCodes.Status403Forbidden };
            }
            //try
            //{
            //    if (!actionContext.HttpContext.Request.Headers.ContainsKey("Token"))
            //    {
            //        actionContext.HttpContext.Response.StatusCode = 403;
            //        return;
            //    }
            //    else
            //    {
            //        string token = actionContext.HttpContext.Request.Headers["Token"].FirstOrDefault();
            //        if (Common._GetInfoUser(token)==null)
            //        {
            //            actionContext.HttpContext.Response.StatusCode = 403;
            //            return;
            //        }
            //        if (!string.IsNullOrEmpty(Roles))   
            //            if (!Common.CheckRoleByToken(token, Roles))
            //            {
            //                actionContext.HttpContext.Response.StatusCode = 403;
            //                return;
            //            }
            //    }
            //}
            //catch (Exception)
            //{
            //    actionContext.HttpContext.Response.StatusCode = 403;
            //    return;
            //}
        }

    }
}
