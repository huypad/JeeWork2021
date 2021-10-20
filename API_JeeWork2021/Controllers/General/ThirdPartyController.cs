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
using DPSinfra.ConnectionCache;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using JeeWork_Core2021.Controller;
using static JeeWork_Core2021.Models.ConfigNotify;

namespace JeeWork_Core2021.Controllers.Wework
{
    [ApiController]
    [Route("api/third-party")]
    [EnableCors("JeeWorkPolicy")]
    /// <summary>
    /// Quản lý các api cung cấp cho bên thứ 3
    /// </summary>
    // [EnableCors(origins: "*", headers: "*", methods: "*")]
    public class ThirdPartyController : ControllerBase
    {
        private readonly IHostingEnvironment _hostingEnvironment;
        private JeeWorkConfig _config;
        private IConnectionCache ConnectionCache;
        private IConfiguration _configuration;
        private readonly ILogger<ConfigNotifyController> _logger;
        public List<AccUsernameModel> DataAccount;
        public ThirdPartyController(IOptions<JeeWorkConfig> config, IHostingEnvironment hostingEnvironment, IConnectionCache _cache, IConfiguration configuration, ILogger<ConfigNotifyController> logger)
        {
            _hostingEnvironment = hostingEnvironment;
            _config = config.Value;
            ConnectionCache = _cache;
            _configuration = configuration;
            _logger = logger;
        }
        /// <summary>
        /// Lấy danh sách work space
        /// </summary>
        /// <returns></returns>
        [Route("get-work-space")]
        [HttpGet]
        public object GetListWorkSpace()
        {
            DataSet ds_workspace = new DataSet();
            UserJWT loginData = Ulities.GetUserByHeader(HttpContext.Request.Headers);
            if (loginData == null)
                return JsonResultCommon.DangNhap();
            try
            {
                string ConnectionString = WeworkLiteController.getConnectionString(ConnectionCache, loginData.CustomerID, _configuration);
                using (DpsConnection Conn = new DpsConnection(ConnectionString))
                {
                    #region Lấy dữ liệu account từ JeeAccount
                    DataAccount = WeworkLiteController.GetAccountFromJeeAccount(HttpContext.Request.Headers, _configuration);
                    if (DataAccount == null)
                        return JsonResultCommon.Custom("Lỗi lấy danh sách nhân viên từ hệ thống quản lý tài khoản");
                    string err = "";
                    string listID = WeworkLiteController.ListAccount(HttpContext.Request.Headers, out err, _configuration);
                    if (err != "")
                        return JsonResultCommon.Custom(err);
                    #endregion
                    Common permit = new Common(ConnectionString);
                    ds_workspace = Common.GetWorkSpace(loginData, 0, 0, ConnectionString);
                }
                if (ds_workspace != null)
                {
                    var workspace = from r in ds_workspace.Tables[0].AsEnumerable()
                                    orderby r["title"]
                                    select new
                                    {
                                        id_row = r["id_row"],
                                        title = r["title"],
                                        icon = "flaticon-signs-1",
                                        priority = r["priority"],
                                        isfolder = false,
                                        type = 1,
                                        parentowner = r["parentowner"],
                                        owner = r["owner"],
                                        list = from r2 in ds_workspace.Tables[2].AsEnumerable()
                                               where r2["id_department"].ToString() == r["id_row"].ToString()
                                               select new
                                               {
                                                   id_row = r2["id_row"],
                                                   title = r2["Title"],
                                                   locked = r2["Locked"],
                                                   color = r2["color"],
                                                   status = r2["Status"],
                                                   default_view = r2["default_view"],
                                                   is_project = r2["is_project"],
                                                   type = 3,
                                                   parentowner = r["parentowner"],
                                                   owner = r["owner"],
                                                   admin_project = r2["admin_project"],
                                               },
                                        folder = from r3 in ds_workspace.Tables[1].AsEnumerable()
                                                 where r3["ParentID"].ToString() == r["id_row"].ToString()
                                                 orderby r3["Title"]
                                                 select new
                                                 {
                                                     id_row = r3["id_row"],
                                                     title = r3["title"],
                                                     icon = "flaticon-folder",
                                                     priority = r3["priority"],
                                                     type = 2,
                                                     isfolder = true,
                                                     owner = r3["owner"],
                                                     parentowner = r3["parentowner"],
                                                     list = from r4 in ds_workspace.Tables[2].AsEnumerable()
                                                            where r4["id_department"].ToString() == r3["id_row"].ToString()
                                                            select new
                                                            {
                                                                id_row = r4["id_row"],
                                                                title = r4["title"],
                                                                locked = r4["locked"],
                                                                color = r4["color"],
                                                                status = r4["status"],
                                                                default_view = r4["Default_View"],
                                                                type = 3,
                                                                is_project = r4["is_project"],
                                                                owner = r3["owner"],
                                                                parentowner = r3["parentowner"],
                                                                admin_project = r4["admin_project"],
                                                            },
                                                 },
                                    };
                    return JsonResultCommon.ThanhCong(workspace);
                }
                else
                {
                    return JsonResultCommon.KhongHopLe("Dữ liệu workspace không đúng chuẩn");
                }
            }
            catch (Exception ex)
            {
                return JsonResultCommon.Exception(_logger, ex, _config, loginData);
            }
        }
    }
}

