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
using System.Collections.Specialized;
using DPSinfra.ConnectionCache;

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
        private readonly IHostingEnvironment _hostingEnvironment;
        public MenuController(IOptions<JeeWorkConfig> config, IHostingEnvironment hostingEnvironment, IConnectionCache _cache)
        {
            ConnectionCache = _cache;
            _hostingEnvironment = hostingEnvironment;
            _config = config.Value;
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
            //JeeWorkConfig _config;
            try
            {
                if (loginData != null)
                {
                    using (DpsConnection Conn = new DpsConnection(ConnectionCache.GetConnectionString(loginData.CustomerID)))
                    {
                        #region Lấy dữ liệu account từ JeeAccount
                        DataAccount = WeworkLiteController.GetAccountFromJeeAccount(HttpContext.Request.Headers, _config);
                        if (DataAccount == null)
                            return JsonResultCommon.Custom("Lỗi lấy danh sách nhân viên từ hệ thống quản lý tài khoản");

                        string err = "";
                        string listID = WeworkLiteController.ListAccount(HttpContext.Request.Headers, out err, _config);
                        if (err != "")
                            return JsonResultCommon.Custom(err);
                        #endregion
                        #region LẤY MAIN MENU - MENU NGANG
                        SqlConditions cond = new SqlConditions();
                        cond.Add("CustemerID", loginData.CustomerID);
                        cond.Add("HienThi", 1);
                        string[] listrole = Common.GetRolesForUser_WeWork(loginData.Username, _config);
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
                        string sql_space = "", sql_project = "";
                        if (v_module.ToLower().Equals("module = 'wework'"))
                        {
                            sql_space = @$"select id_row, title, id_cocau, IdKH, priority, disabled 
                                        from we_department
                                        where Disabled = 0 and we_department.CreatedBy in ({listID}) and IdKH = {loginData.CustomerID} and (id_row in (select id_department from we_project_team 
                                        where (id_row in (select id_project_team from we_project_team_user where id_user = {loginData.UserID} and Disabled = 0) 
                                        or (CreatedBy = {loginData.UserID} or UpdatedBy = { loginData.UserID })) and disabled = 0) or (CreatedBy = {loginData.UserID} or UpdatedBy = {loginData.UserID }));
                                        ";
                            sql_project = "select id_row, icon, title, detail, id_department" +
                                ", loai, start_date, end_date, color, template, status, stage_description" +
                                ", allow_percent_done, require_evaluate, evaluate_by_assignner" +
                                ", allow_estimate_time, stop_reminder, note, is_project, period_type" +
                                ", priority, CreatedDate, CreatedBy, Locked, Disabled, UpdatedDate" +
                                ", email_assign_work, email_update_work, email_update_status, email_delete_work" +
                                ", email_update_team, email_delete_team, email_upload_file, default_view " +
                                "from we_project_team " +
                                $"where Disabled = 0 and CreatedBy in ({listID})";
                        }
                        dt_space = Conn.CreateDataTable(sql_space);
                        dt_project = Conn.CreateDataTable(sql_project);
                        dt_space.Columns.Add("Data", typeof(DataTable));
                        DataTable tmp_ww = new DataTable();
                        foreach (DataRow dr in dt_space.Rows)
                        {
                            tmp_ww = new DataTable();
                            tmp_ww.Columns.Add("ID");
                            tmp_ww.Columns.Add("Title");
                            tmp_ww.Columns.Add("Link");
                            tmp_ww.Columns.Add("Locked");
                            tmp_ww.Columns.Add("Color");
                            tmp_ww.Columns.Add("Status");
                            tmp_ww.Columns.Add("Default_View");
                            tmp_ww.Columns.Add("Is_Project");
                            DataRow[] dr_ = dt_project.Select("id_department=" + dr[0]);
                            foreach (DataRow r in dr_)
                            {
                                tmp_ww.Rows.Add(new object[] { r["id_row"], r["title"], "/" + r["id_row"], r["Locked"], r["Color"], r["Status"], r["Default_View"], r["Is_Project"] });
                            }
                            dr["Data"] = tmp_ww;
                        }
                        ds = Conn.CreateDataSet(select_MainMenu, cond);
                        #endregion
                    }
                    //string link = General.GetDomain(loginData.CustomerID.ToString());
                    //link = link + "images/AppIcon/";
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
                                                       HinhThucTinhLuong = c["Loaihinh"].ToString(),
                                                       IconImage = c["IconImage"].ToString(),
                                                       GroupName = c["GroupName"].ToString(),
                                                       ALink = c["ALink"].ToString(),
                                                       Title_ = c["title_"].ToString(),
                                                   },
                                           ChildApp = from c in ((DataTable)r["Child"]).AsEnumerable()
                                                      where c["groupname"].ToString().Trim().ToLower().Equals(r["groupname"].ToString().Trim().ToLower()) && !c["AppLink"].ToString().Equals("")
                                                      && ((c["id_row"].ToString().Equals("70652") && ds.Tables[2].Rows.Count > 0) || !c["id_row"].ToString().Equals("70652"))
                                                      select new
                                                      {
                                                          Title = c["title"].ToString(),
                                                          Summary = c["Summary"].ToString(),
                                                          PageKey = c["pagekey"].ToString(),
                                                          AllowPermit = c["AllowPermit"].ToString(),
                                                          Target = c["Target"].ToString(),
                                                          HinhThucTinhLuong = c["Loaihinh"].ToString(),
                                                          //IconImage = link + c["AppIcon"].ToString(),
                                                          GroupName = c["GroupName"].ToString(),
                                                          ALink = c["AppLink"],
                                                      }
                                       };
                            var menuWework = from r in dt_space.AsEnumerable()
                                             select new
                                             {
                                                 RowID = r["id_row"],
                                                 Title = r["title"],
                                                 Icon = "flaticon-list",
                                                 Data = from k in ((DataTable)r["data"]).AsEnumerable()
                                                        select new
                                                        {
                                                            ID_Row = k["ID"],
                                                            Title = k["Title"],
                                                            ALink = k["Link"],
                                                            Locked = k["Locked"],
                                                            Color = k["Color"],
                                                            Status = k["Status"],
                                                            Default_View = k["Default_View"],
                                                            Is_Project = k["is_project"],
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
                return JsonResultCommon.Exception(ex, _config, loginData.CustomerID);
            }
        }

        [HttpGet]
        [Route("GetRoleWeWork")]
        public object GetRoleWeWork(string id_nv)
        {
            try
            {
                using (DpsConnection Conn = new DpsConnection(_config.ConnectionString))
                {
                    string sqlq = "";
                    sqlq = "select * from we_project_team " +
                        "join we_project_team_user " +
                        "on we_project_team.id_row = we_project_team_user.id_project_team " +
                        "where we_project_team.disabled = 0 and we_project_team_user.disabled = 0 " +
                        "and locked = 0 and id_user = " + id_nv + ";";
                    sqlq += @"select we_project_role.id_row,id_project_team,id_role, admin, member,customer, KeyPermit 
                            from we_project_role join we_role on we_role.id_row = we_project_role.id_role
                            where member = 1 and we_role.Disabled = 0";
                    DataSet ds = Conn.CreateDataSet(sqlq);
                    DataTable dt_Project = Conn.CreateDataTable(sqlq);
                    if (Conn.LastError != null || ds == null)
                        return JsonResultCommon.Exception(Conn.LastError,_config, int.Parse(id_nv), ControllerContext);
                    DataTable dt = ds.Tables[0];
                    if (dt.Rows.Count == 0)
                        return JsonResultCommon.ThanhCong(new List<string>());
                    var data = from r in dt.AsEnumerable()
                               select new
                               {
                                   id_row = r["id_row"],
                                   id_user = r["title"],
                                   admin = r["admin"],
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
                return JsonResultCommon.Exception(ex, _config, int.Parse(id_nv));
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