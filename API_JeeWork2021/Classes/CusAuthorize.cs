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
            if (string.IsNullOrEmpty(Roles))
            {
                actionContext.Result = new UnauthorizedResult();
                return;
            }
            else
            {
                try
                {
                    IHeaderDictionary _d = actionContext.HttpContext.Request.Headers;
                    if (!_d.ContainsKey(HeaderNames.Authorization))
                        actionContext.Result = new JsonResult(new { message = "Unauthorized" }) { StatusCode = StatusCodes.Status401Unauthorized };
                    string _bearer_token, _customdata;
                    _bearer_token = _d[HeaderNames.Authorization].ToString().Replace("Bearer ", "");
                    var handler = new JwtSecurityTokenHandler();
                    var tokenS = handler.ReadToken(_bearer_token) as JwtSecurityToken;
                    _customdata = tokenS.Claims.Where(x => x.Type == "customdata").FirstOrDefault().Value;
                    if (string.IsNullOrEmpty(_customdata))
                    {
                        actionContext.Result = new UnauthorizedResult();
                        return;
                    }
                    CustomData cusData = new CustomData();
                    cusData = JsonConvert.DeserializeObject<CustomData>(_customdata);
                    //if (DateTime.UtcNow > tokenS.ValidTo)
                    //{
                    //    actionContext.Result = new JsonResult(new { message = "Expired" }) { StatusCode = StatusCodes.Status408RequestTimeout };
                    //}
                    //else
                    {
                        //Kiểm tra quyền ở đây
                        var requiredPermissions = Roles.Split(","); // nhận nhiều mã quyền từ controller, xóa "," để lấy từng quyền
                        List<string> roles = new List<string>();
                        if (cusData.JeeWork.WeWorkRoles != null)
                            roles = cusData.JeeWork.WeWorkRoles.Split(",").ToList();
                        if (cusData.JeeWork.roles != null)
                            roles = cusData.JeeWork.roles.Split(",").ToList();
                        foreach (var x in requiredPermissions)
                        {
                            if (roles.Contains(x))
                                return; //User Authorized
                        }
                        actionContext.Result = new JsonResult(new { message = "Forbidden" }) { StatusCode = StatusCodes.Status403Forbidden };
                        return;
                    }
                }
                catch (Exception ex)
                {
                    actionContext.Result = new JsonResult(new { message = "Forbidden" }) { StatusCode = StatusCodes.Status403Forbidden };
                }
            }
        }
    }
}
