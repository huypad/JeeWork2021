using JeeWork_Core2021.Classes;
using JeeWork_Core2021.Models;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

[ApiController]
public class ErrorController : ControllerBase
{
    [Route("error")]
    protected IActionResult Error()
    {
        //string Token = Common.GetHeader(Request);
        //LoginData loginData = Common._GetInfoUser(Token);
        //var context = HttpContext.Features.Get<IExceptionHandlerFeature>();
        //string noidung = context.Error.Message;
        //if (loginData != null)
        //{
        //    string custemerid = loginData.IDKHDPS.ToString();
        //    //AutoSendMail.SendErrorReport(custemerid, noidung);
        //}
        return Problem();
    }
}