using DPSinfra.ConnectionCache;
using DpsLibs.Data;
using JeeWork_Core2021.Classes;
using JeeWork_Core2021.Models;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;

namespace JeeWork_Core2021.Controllers.Wework
{
    [ApiController]
    [Route("api/menu")]
    [EnableCors("JeeWorkPolicy")]
    public class MenuController : ControllerBase
    {
        private JeeWorkConfig _config;
        public List<AccUsernameModel> DataAccount;
        private IConnectionCache ConnectionCache;
        private IConfiguration _configuration;
        private readonly IHostingEnvironment _hostingEnvironment;
        private readonly ILogger<MenuController> _logger;
        public MenuController(IOptions<JeeWorkConfig> config, IHostingEnvironment hostingEnvironment, IConnectionCache _cache, IConfiguration configuration, ILogger<MenuController> logger)
        {
            ConnectionCache = _cache;
            _hostingEnvironment = hostingEnvironment;
            _config = config.Value;
            _configuration = configuration;
            _logger = logger;
        }
        //private JeeWorkConfig _config;
        // Đang sửa
        /// <summary>
        /// Load menu theo module
        /// </summary>
        /// <param name="v_module">module</param>
        /// <returns></returns>
        [Route("LayMenuChucNang")]
        [HttpGet]
        public object LayMenuChucNang(string v_module)
        {
            ErrorModel error = new ErrorModel();
            DataTable dt_chamcongwf = new DataTable();
            DataSet ds = new DataSet();
            string select_MainMenu = "", select_Menu = "", sql_listRole = "";
            PageModel pageModel = new PageModel();
            UserJWT loginData = Ulities.GetUserByHeader(HttpContext.Request.Headers);
            DataTable dt_space = new DataTable();
            DataTable dt_project = new DataTable();
            DataTable dt_folder = new DataTable();
            //JeeWorkConfig _config;
            try
            {
                if (loginData != null)
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
                        #region LẤY MAIN MENU - MENU NGANG
                        SqlConditions cond = new SqlConditions();
                        cond.Add("CustemerID", loginData.CustomerID);
                        cond.Add("HienThi", 1);
                        string[] listrole = Common.GetRolesForUser_WeWork(loginData.Username, Conn);
                        for (int i = 0; i < listrole.Length; i++)
                        {
                            sql_listRole += ",@IDRole" + i;
                            cond.Add("IDRole" + i, listrole[i]);
                        }
                        if (!"".Equals(sql_listRole)) sql_listRole = sql_listRole.Substring(1);
                        if (listrole.Length == 0)
                        {
                            sql_listRole = "0";
                        }
                        v_module = $"module = '{v_module}'";
                        //câu lệnh này select main menu
                        select_MainMenu = $@"
                            select groupname --,title, PermissionID, page, Target, Summary, isNULL(ALink, '#') as ALink, ISNULL(Icon, 'flaticon-interface-7') as Icon, '' as title_, position
                            from Tbl_Mainmenu 
                            where (PermissionID is not null and PermissionID in ( {sql_listRole}) and ({v_module})) or (({v_module}) 
                            and (Hienthi is NULL) and ((CustemerID is NULL) or (CustemerID=@CustemerID))
                            and groupname in (select distinct groupname from tbl_submenu where ({v_module}) 
                            and (AllowPermit in ( {sql_listRole} ) or (AllowPermit is NULL and ({v_module}))) 
                            and ((CustemerID is NULL) or (CustemerID=@CustemerID)) 
                            and (Hienthi=@HienThi)))
                            --order by position  
                            ";
                        //select menu sub
                        select_Menu = $@"
select title, pagekey, AllowPermit, Target, tbl_submenu.id_row,
Loaihinh, IconImage, GroupName, ALink, Summary, AppLink, AppIcon, '' as title_
from tbl_submenu 
where (AllowPermit IN ({sql_listRole}) or (AllowPermit is NULL and ({v_module})))
and GroupName IN ( {select_MainMenu}  )
and {v_module}
and hienthi=@HienThi and ((CustemerID is null) or (CustemerID=@CustemerID)) order by position 
";
                        // string HRCatalog = JeeWorkConstant.getConfig("JeeWorkConfig:HRCatalog");
                        select_MainMenu = select_MainMenu
                            .Replace("--,title, PermissionID, page, Target, Summary, isNULL(ALink, '#') as ALink, ISNULL(Icon, 'flaticon-interface-7') as Icon", ",title, PermissionID, page, Target, Summary, isNULL(ALink, '#') as ALink, ISNULL(Icon, 'flaticon-interface-7') as Icon")
                            .Replace("--order by position", " order by position ");
                        select_MainMenu += select_Menu;
                        string sql_space = "", sql_project = "", sql_folder = "", where_department = "";
                        //if (v_module.ToLower().Equals("module = 'wework'"))
                        //{
                        where_department = @$" disabled = 0 and CreatedBy in ({listID}) 
                                        and IdKH = {loginData.CustomerID} and (id_row in (select id_department from we_project_team 
                                        where (id_row in (select id_project_team from we_project_team_user where id_user = { loginData.UserID}
                                        and Disabled = 0) or (CreatedBy = { loginData.UserID})) and disabled = 0) or (CreatedBy = { loginData.UserID}));";
                        sql_space = @$"select id_row, title, id_cocau, IdKH, priority, disabled, ParentID
                                        from we_department
                                        where ParentID is null and " + where_department + "";
                        sql_project = "select p.id_row, p.icon, p.title, p.detail, p.id_department" +
                            ", p.loai, p.start_date, p.end_date, p.color, p.template, p.status, p.is_project" +
                            ", p.priority, p.CreatedDate, p.CreatedBy, p.Locked, p.Disabled, default_view " +
                            "from we_project_team p (admin_group)" +
                            $" p.Disabled = 0 and p.CreatedBy in ({listID})";
                        //}
                        sql_folder = @$"select id_row, title, id_cocau, IdKH, priority, disabled, ParentID 
                                        from we_department
                                        where ParentID is not null and " + where_department + "";
                        if (!CheckGroupAdministrator(loginData.Username, Conn, loginData.CustomerID))
                        {
                            sql_project = sql_project.Replace("(admin_group)", "join we_project_team_user " +
                            "on we_project_team_user.id_project_team = p.id_row " +
                            "and (we_project_team_user.id_user = " + loginData.UserID + ") " +
                            "where we_project_team_user.id_user = " + loginData.UserID + " and ");
                        }
                        else
                        {
                            sql_project = sql_project.Replace("(admin_group)", " where ");
                        }    
                        dt_space = Conn.CreateDataTable(sql_space);
                        dt_project = Conn.CreateDataTable(sql_project);
                        dt_folder = Conn.CreateDataTable(sql_folder);
                        DataTable tmp_ww = new DataTable();
                        ds = Conn.CreateDataSet(select_MainMenu, cond);
                        #endregion
                    }
                    System.Data.DataColumn newColumn = new System.Data.DataColumn("Child", typeof(object));
                    newColumn.DefaultValue = ds.Tables[1];
                    ds.Tables[0].Columns.Add(newColumn);
                    DataTable dt1 = ds.Tables[0];
                    if (ds != null)
                    {
                        if (ds.Tables.Count > 0)
                        {
                            var data = from r in ds.Tables[0].AsEnumerable()
                                       orderby r["position"]
                                       select new
                                       {
                                           GroupName = r["groupname"].ToString(),
                                           Title = r["title"].ToString(),
                                           Target = r["Target"],
                                           Summary = r["Summary"].ToString(),
                                           Icon = r["Icon"].ToString(),
                                           ALink = r["ALink"].ToString(),
                                           PermissionID = r["PermissionID"].ToString(),
                                           Child = from c in ((DataTable)r["Child"]).AsEnumerable()
                                                   where c["groupname"].ToString().Trim().ToLower().Equals(r["groupname"].ToString().Trim().ToLower()) && !c["alink"].ToString().Equals("")
                                                   select new
                                                   {
                                                       Title = c["title"].ToString(),
                                                       Summary = c["Summary"].ToString(),
                                                       PageKey = c["pagekey"].ToString(),
                                                       AllowPermit = c["AllowPermit"].ToString(),
                                                       Target = c["Target"].ToString(),
                                                       IconImage = c["IconImage"].ToString(),
                                                       GroupName = c["GroupName"].ToString(),
                                                       ALink = c["ALink"].ToString(),
                                                       Title_ = c["title_"].ToString(),
                                                   },
                                       };
                            var menuWework = from r in dt_space.AsEnumerable()
                                             select new
                                             {
                                                 RowID = r["id_row"],
                                                 Title = r["title"],
                                                 Icon = "flaticon-signs-1",
                                                 Priority = r["priority"],
                                                 IsFolder = false,
                                                 type = 1,
                                                 Data = from r2 in dt_project.AsEnumerable()
                                                        where r2["id_department"].ToString() == r["id_row"].ToString()
                                                        select new
                                                        {
                                                            ID_Row = r2["id_row"],
                                                            Title = r2["Title"],
                                                            Locked = r2["Locked"],
                                                            Color = r2["Color"],
                                                            Status = r2["Status"],
                                                            Default_View = r2["Default_View"],
                                                            Is_Project = r2["is_project"],
                                                            type = 3,
                                                        },
                                                 Data_Folder = from r3 in dt_folder.AsEnumerable()
                                                               where r3["ParentID"].ToString() == r["id_row"].ToString()
                                                               select new
                                                               {
                                                                   RowID = r3["id_row"],
                                                                   Title = r3["title"],
                                                                   Icon = "flaticon-folder",
                                                                   Priority = r3["priority"],
                                                                   type = 2,
                                                                   IsFolder = true,
                                                                   Data = from r4 in dt_project.AsEnumerable()
                                                                          where r4["id_department"].ToString() == r3["id_row"].ToString()
                                                                          select new
                                                                          {
                                                                              ID_Row = r4["id_row"],
                                                                              Title = r4["Title"],
                                                                              Locked = r4["Locked"],
                                                                              Color = r4["Color"],
                                                                              Status = r4["Status"],
                                                                              Default_View = r4["Default_View"],
                                                                              type = 3,
                                                                              Is_Project = r4["is_project"],
                                                                          },
                                                               },
                                             };
                            return JsonResultCommon.ThanhCong(new
                            {
                                data = data,
                                dataww = menuWework
                            });
                        }
                        else
                        {
                            return JsonResultCommon.KhongHopLe("Dữ liệu lấy menu không đúng chuẩn");
                        }
                    }
                    else
                    {
                        return JsonResultCommon.KhongHopLe("Dữ liệu lấy menu không đúng chuẩn");
                    }
                }
                else
                {
                    return JsonResultCommon.KhongHopLe("Tài khoản bạn bị khóa hoặc hết hạn đăng nhập. Bạn vui lòng đăng nhập lại!");
                }
            }
            catch (Exception ex)
            {
                return JsonResultCommon.Exception(_logger, ex, _config, loginData);
            }
        }
        [HttpGet]
        [Route("GetRoleWeWork")]
        public object GetRoleWeWork(string id_nv, long CustomerID)
        {
            UserJWT loginData = Ulities.GetUserByHeader(HttpContext.Request.Headers);
            if (loginData != null)
            {
                CustomerID = loginData.CustomerID;
            }
            try
            {
                //ConnectionCache.GetConnectionString(CustomerID)
                string ConnectionString = WeworkLiteController.getConnectionString(ConnectionCache, loginData.CustomerID, _configuration);
                using (DpsConnection Conn = new DpsConnection(ConnectionString))
                {
                    string sqlq = "";
                    sqlq = "select *,  Iif( we_project_team_user.admin = 1 and id_user <> "+loginData.UserID+",1,0 ) as isuyquyen " +
                        " from we_project_team join we_project_team_user " +
                        "on we_project_team.id_row = we_project_team_user.id_project_team " +
                        "where we_project_team.disabled = 0 and we_project_team_user.disabled = 0 " +
                        "and locked = 0 and id_user = " + id_nv + $" or id_user in (select CreatedBy from we_authorize where id_user = {id_nv} and disabled =0);";
                    sqlq += @"select we_project_role.id_row,id_project_team,id_role, admin, member,customer, KeyPermit 
                            from we_project_role join we_role on we_role.id_row = we_project_role.id_role
                            where member = 1 and we_role.Disabled = 0";
                    DataSet ds = Conn.CreateDataSet(sqlq);
                    DataTable dt_Project = Conn.CreateDataTable(sqlq);
                    if (Conn.LastError != null || ds == null)
                        return JsonResultCommon.Exception(_logger, Conn.LastError, _config, loginData, ControllerContext);
                    DataTable dt = ds.Tables[0];
                    if (dt.Rows.Count == 0)
                        return JsonResultCommon.ThanhCong(new List<string>());
                    var data = from r in dt.AsEnumerable()
                               select new
                               {
                                   id_row = r["id_row"],
                                   id_user = r["title"],
                                   admin = int.Parse(r["isuyquyen"].ToString()) == 1?0:r["admin"],
                                   isuyquyen = r["isuyquyen"],
                                   id_department = r["id_department"],
                                   locked = r["locked"],
                                   Roles = from u in ds.Tables[1].AsEnumerable()
                                           where u["id_project_team"].ToString() == r["id_row"].ToString()
                                           select new
                                           {
                                               id_row = u["id_row"],
                                               id_project_team = u["id_project_team"],
                                               id_role = u["id_role"],
                                               admin = u["admin"],
                                               member = u["member"],
                                               keypermit = u["KeyPermit"],
                                           },
                               };
                    return JsonResultCommon.ThanhCong(data);
                }
            }
            catch (Exception ex)
            {
                return JsonResultCommon.Exception(_logger, ex, _config, loginData);
            }
        }

        public static bool CheckGroupAdministrator(string username, DpsConnection Conn, long CustomerID)
        {
            string sqlq = "";
            SqlConditions cond = new SqlConditions();
            try
            {
                cond.Add("Username", username);
                sqlq = "select Id_group from tbl_group_account " +
                    "where Id_group in (select Id_group from tbl_group " +
                    "where IsAdmin = 1 and CustemerID = " + CustomerID + ") and (where)";
                DataTable dt_checkuser = Conn.CreateDataTable(sqlq, "(where)", cond);
                if (dt_checkuser.Rows.Count > 0)
                    return true;
                else
                    return false;
            }
            catch (Exception ex)
            {
                return false;
            }
        }
        //public static bool ListRole(long id_project)
        //{
        //    BaseModel<object> model = new BaseModel<object>();
        //    PageModel pageModel = new PageModel();
        //    ErrorModel error = new ErrorModel();
        //    DataTable dt_role = new DataTable();
        //    DataTable dt_checkuser = new DataTable();
        //    string sqlq = "";
        //    SqlConditions cond = new SqlConditions();
        //    try
        //    {
        //        using (DpsConnection cnn = new DpsConnection(JeeWorkConstant.getConfig("JeeWorkConfig:ConnectionString")))
        //        {
        //            cond.Add("id_project_team", id_project);
        //            cond.Add("id_user", user);
        //            cond.Add("admin", 1);
        //            cond.Add("disabled", 0);
        //            string sql_user = "";
        //            #region Check user admin trong project trước
        //            sql_user = "select * from we_project_team_user where (where)";
        //            dt_checkuser = cnn.CreateDataTable(sql_user, "(where)", cond);
        //            if (dt_checkuser.Rows.Count > 0) // thuộc project, có trong dự án và là admin
        //                return true; // Đối với admin mặc định là có quyền
        //            #endregion
        //            #region Check user thành viên trong project
        //            else
        //            {
        //                cond.Remove(cond["admin"]);
        //                dt_checkuser = cnn.CreateDataTable(sql_user, "(where)", cond);
        //                if (dt_checkuser.Rows.Count > 0) // có user trong dự án và là thành viên
        //                {
        //                    #region Check các quyền của project
        //                    cond.Remove(cond["id_user"]);
        //                    cond.Remove(cond["disabled"]);
        //                    cond.Add("id_role", role);
        //                    cond.Add("member", 1);
        //                    sqlq = "select id_row, id_project_team, id_role, admin, member, customer from we_project_role where (where)";
        //                    dt_role = cnn.CreateDataTable(sqlq, "(where)", cond);
        //                    if (dt_role.Rows.Count > 0)
        //                        return true;
        //                    else
        //                        return false;
        //                    #endregion
        //                }
        //                #endregion
        //                else // User không có trong dự án đó
        //                    return false;
        //            }
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        return false;
        //    }
        //}
    }
}