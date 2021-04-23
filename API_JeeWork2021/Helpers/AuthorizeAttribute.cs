
using JeeWork_Core2021.Classes;
using JeeWork_Core2021.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Net.Http.Headers;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Net;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace JeeWork_Core2021.Helpers
{
    /// <summary>
    /// Samepl code
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
    public class AuthorizeAttribute : Attribute, IAuthorizationFilter
    {
        public void OnAuthorization(AuthorizationFilterContext context)
        {
            var user = (UserJWT)context.HttpContext.Items["User"];
            if (user == null)
            {
                // not logged in
                context.Result = new JsonResult(new { message = "Unauthorized" }) { StatusCode = StatusCodes.Status401Unauthorized };
            }
        }
    }
    /// <summary>
    /// Real code
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
    public class AuthorizeProcess : ActionFilterAttribute
    {

        public override void OnActionExecuting(ActionExecutingContext actionContext)
        {
            try
            {
                //var request = actionContext.HttpContext.Request.Headers["Authorization"];
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
        }
    }

    /// <summary>
    /// Real code
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
    public class Authorize_Role_System : ActionFilterAttribute
    {

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
        }
    }


    public class TestNew : System.Web.Http.AuthorizeAttribute
    {
        public override void OnAuthorization(
               System.Web.Http.Controllers.HttpActionContext actionContext)
        {
            try
            {
                base.OnAuthorization(actionContext);
                HttpRequestHeaders p_Authen = actionContext.Request.Headers;
                if (p_Authen == null)
                {
                    //actionContext.Response = "";
                    //actionContext.Request.CreateResponse(HttpStatusCode.ExpectationFailed);
                    //actionContext.Response.ReasonPhrase = "Please provide valid inputs";
                    return;
                }

                BaseModel<object> _baseReturn = new BaseModel<object>();
                AuthenticationHeaderValue _Autho = p_Authen.Authorization;
                long _warning = long.Parse(p_Authen.Warning.ToString());
                //AES_Helper_Ensc_Desc _aes_Script = new AES_Helper_Ensc_Desc();
                //string _baseDescrypt = _aes_Script.DecryptStringAES(_Autho.Parameter);

                //if (_baseDescrypt.ToLower().Equals("keyerror"))
                //{
                //    HttpContext.Current.Response.AddHeader("authenticationToken", "");
                //    HttpContext.Current.Response.AddHeader("AuthenticationStatus", "NotAuthorized");
                //    _baseReturn.status = 0;
                //    _baseReturn.data = string.Empty;
                //    _baseReturn.error = new ErrorModel
                //    {
                //        code = HttpStatusCode.NotAcceptable.ToString(),
                //        message = "Request is locked"
                //    };
                //    actionContext.Response = actionContext.Request.CreateResponse(HttpStatusCode.NotAcceptable,
                //        _baseReturn
                //        );
                //    return;
                //}

                //Header_Object_Model _token_Obj = JsonConvert.DeserializeObject<Header_Object_Model>(_baseDescrypt);
                //if (_token_Obj != null && _token_Obj.TimeStamp > 0)
                //{
                //    DateTime dt_TimeStamp = new DateTime(_token_Obj.TimeStamp),
                //        dt_Warning = new DateTime(_warning);

                //    if (dt_TimeStamp.Equals(dt_Warning))
                //    {
                //        double _expired = dt_Warning.Subtract(DateTime.Now).TotalMinutes;

                //        HttpContext.Current.Response.AddHeader("authenticationToken", "");
                //        HttpContext.Current.Response.AddHeader("AuthenticationStatus", "Authorized");
                //    }
                //    else
                //    {
                //        _baseReturn.status = 0;
                //        _baseReturn.data = string.Empty;
                //        _baseReturn.error = new ErrorModel
                //        {
                //            code = Constants.ERRORCODE_API_FAIL_ROUTINE,
                //            message = "ROUTINE_IS_EXPIRED"
                //        };
                //        HttpContext.Current.Response.AddHeader("authenticationToken", "");
                //        HttpContext.Current.Response.AddHeader("AuthenticationStatus", "NotAuthorized");
                //        actionContext.Response = actionContext.Request.CreateResponse(HttpStatusCode.NotAcceptable,
                //            _baseReturn
                //            );
                //        return;
                //    }
                //}
                //else
                //{
                //    _baseReturn.status = 0;
                //    _baseReturn.data = string.Empty;
                //    _baseReturn.error = new ErrorModel
                //    {
                //        code = Constants.ERRORCODE_API_FORBIDDEN,
                //        message = "Cannot authorize"
                //    };

                //    HttpContext.Current.Response.AddHeader("authenticationToken", "");
                //    HttpContext.Current.Response.AddHeader("AuthenticationStatus", "NotAuthorized");
                //    actionContext.Response = actionContext.Request.CreateResponse(HttpStatusCode.Forbidden, _baseReturn);
                //    return;
                //}
            }
            catch (Exception ex)
            {
                BaseModel<object> _baseReturn = new BaseModel<object>();

                _baseReturn.status = 0;
                _baseReturn.data = string.Empty;
                _baseReturn.error = new ErrorModel
                {
                    //code = Constants.ERRORCODE_API_FORBIDDEN,
                    //message = "Sign not ok"
                };
                //HttpContext.Current.Response.AddHeader("authenticationToken", "");
                //HttpContext.Current.Response.AddHeader("AuthenticationStatus", "NotAuthorized");
                //actionContext.Response = actionContext.Request.CreateResponse(HttpStatusCode.Forbidden, _baseReturn);
                return;
            }

        }
    }

}
