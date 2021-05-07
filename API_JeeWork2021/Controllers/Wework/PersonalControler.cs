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
using System.Globalization;

namespace JeeWork_Core2021.Controllers.Wework
{
    [ApiController]
    [Route("api/personal")]
    [EnableCors("JeeWorkPolicy")]
    /// <summary>
    /// api quản lý we_authorize
    /// </summary>
    // [EnableCors(origins: "*", headers: "*", methods: "*")]
    public class PersonalController : ControllerBase
    {
        private readonly IHostingEnvironment _hostingEnvironment;
        private JeeWorkConfig _config;
        public List<AccUsernameModel> DataAccount;

        public PersonalController(IOptions<JeeWorkConfig> config, IHostingEnvironment hostingEnvironment)
        {
            _hostingEnvironment = hostingEnvironment;
            _config = config.Value;
        }
        [Route("my-work")]
        [HttpGet]
        public object MyWork()
        {
            string Token = Common.GetHeader(Request);
            UserJWT loginData = Ulities.GetUserByHeader(HttpContext.Request.Headers);
            if (loginData == null)
                return JsonResultCommon.DangNhap();
            try
            {
                using (DpsConnection cnn = new DpsConnection(_config.ConnectionString))
                {
                    #region Trả dữ liệu về backend để hiển thị lên giao diện
                    string sqlq = @"select w.*, IIF(w.Status = 1 and getdate() > w.deadline,1,0) as is_quahan,
IIF(convert(varchar, w.NgayGiao,103) like convert(varchar, GETDATE(),103),1,0) as is_moigiao
from v_wework w where w.disabled=0 and (id_nv = @userID or CreatedBy = @userID ) ";
                    DataSet ds = cnn.CreateDataSet(sqlq, new SqlConditions() { { "userID", loginData.UserID } });
                    if (cnn.LastError != null || ds == null)
                    {
                        return JsonResultCommon.Exception(cnn.LastError, _config, loginData.CustomerID,ControllerContext);
                    }
                    if (ds.Tables[0] == null || ds.Tables[0].Rows.Count == 0)
                        return JsonResultCommon.KhongTonTai();
                    var temp = ds.Tables[0].AsEnumerable();
                    var data = new
                    {
                        Count = new
                        {
                            ht = temp.Count(w => w["status"].ToString() == "2"),
                            tong = temp.Count(),
                            phailam = temp.Count(w => w["status"].ToString() == "1" && w["status"] == DBNull.Value),
                            danglam = temp.Count(w => w["status"].ToString() == "1" && w["status"] != DBNull.Value),
                        },
                        LuuY = (from r in temp
                                where r["is_quahan"].ToString() == "1" || (bool)r["urgent"]
                                select new
                                {
                                    id_row = r["id_row"],
                                    title = r["title"],
                                    status = r["status"],
                                    is_quahan = r["is_quahan"],
                                    urgent = r["urgent"],
                                }),
                        MoiDuocGiao = (from r in temp
                                       where r["is_moigiao"].ToString() == "1" && r["NguoiGiao"].ToString() != loginData.UserID.ToString()
                                       && !(r["is_quahan"].ToString() == "1" || (bool)r["urgent"])
                                       select new
                                       {
                                           id_row = r["id_row"],
                                           title = r["title"],
                                           status = r["status"],
                                       }),
                        GiaoQuaHan = (from r in temp
                                      where r["is_quahan"].ToString() == "1" && r["NguoiGiao"].ToString() == loginData.UserID.ToString()
                                      select new
                                      {
                                          id_row = r["id_row"],
                                          title = r["title"],
                                          status = r["status"],
                                          urgent = r["urgent"],
                                      }),
                    };
                    return JsonResultCommon.ThanhCong(data);
                }
                #endregion
            }
            catch (Exception ex)
            {
                return JsonResultCommon.Exception(ex, _config, loginData.CustomerID);
            }
        }

        [Route("my-milestone")]
        [HttpGet]
        public object MyMilestone()
        {
            string Token = Common.GetHeader(Request);
            UserJWT loginData = Ulities.GetUserByHeader(HttpContext.Request.Headers);
            if (loginData == null)
                return JsonResultCommon.DangNhap();
            try
            {
                string domain = _config.LinkAPI;
                using (DpsConnection cnn = new DpsConnection(_config.ConnectionString))
                {

                    #region danh sách department, list status hoàn thành, trễ,đang làm
                    string listDept = WeworkLiteController.getListDepartment_GetData(loginData, cnn, HttpContext.Request.Headers, _config);
                    string list_Complete = "";
                    list_Complete = ReportController.GetListStatusDynamic(listDept, cnn,"IsFinal");
                    string list_Deadline = "";
                    list_Deadline = ReportController.GetListStatusDynamic(listDept, cnn, "IsDeadline");
                    string list_Todo = "";
                    list_Todo = ReportController.GetListStatusDynamic(listDept, cnn, "IsTodo");
                    #endregion

                    #region Lấy dữ liệu account từ JeeAccount
                    DataAccount = WeworkLiteController.GetAccountFromJeeAccount(HttpContext.Request.Headers, _config);
                    if (DataAccount == null)
                        return JsonResultCommon.Custom("Lỗi lấy danh sách nhân viên từ hệ thống quản lý tài khoản");

                    //List<string> nvs = DataAccount.Select(x => x.UserId.ToString()).ToList();
                    //string ids = string.Join(",", nvs);
                    string error = "";
                    string listID = WeworkLiteController.ListAccount(HttpContext.Request.Headers, out error, _config);
                    if (error != "")
                        return JsonResultCommon.Custom(error);
                    #endregion

                    #region Trả dữ liệu về backend để hiển thị lên giao diện
                    string sqlq = @$"  select m.*, m.person_in_charge as id_nv, '' as hoten,'' as image, '' as mobile, '' as Username,'' as Email, '' as Tenchucdanh,
coalesce(w.tong,0) as tong,coalesce( w.ht,0) as ht from we_milestone m 
 left join (select count(*) as tong, COUNT(CASE WHEN w.status in ({list_Complete}) THEN 1 END) as ht,w.id_milestone from v_wework w group by w.id_milestone) w on m.id_row=w.id_milestone
 where m.person_in_charge=@iduser and m.person_in_charge in ({listID}) and disabled=0";
                    DataTable dt = cnn.CreateDataTable(sqlq, new SqlConditions() { { "iduser", loginData.UserID } });
                    if (cnn.LastError != null || dt == null)
                        return JsonResultCommon.Exception(cnn.LastError, _config, loginData.CustomerID,ControllerContext);
                    if (dt.Rows.Count == 0)
                        return JsonResultCommon.ThanhCong(new List<string>());

                    #region Map info account từ JeeAccount

                    foreach (DataRow item in dt.Rows)
                    {
                        var info = DataAccount.Where(x => item["id_nv"].ToString().Contains(x.UserId.ToString())).FirstOrDefault();

                        if (info != null)
                        {
                            item["hoten"] = info.FullName;
                            item["username"] = info.Username;
                            item["tenchucdanh"] = info.Jobtitle;
                            item["mobile"] = info.PhoneNumber;
                            item["image"] = info.AvartarImgURL;
                            item["email"] = info.Email;
                        }
                    }
                    #endregion
                    var data = from r in dt.AsEnumerable()
                               select new
                               {
                                   id_row = r["id_row"],
                                   title = r["title"],
                                   description = r["description"],
                                   id_project_team = r["id_project_team"],
                                   deadline_weekday = r["deadline"].Equals(DBNull.Value) ? "" : Common.GetFormatDate(Convert.ToDateTime(r["deadline"]), "77622"),
                                   deadline_day = r["deadline"].Equals(DBNull.Value) ? "" : Common.GetFormatDate(Convert.ToDateTime(r["deadline"]), "dd/MM"),
                                   person_in_charge = new
                                   {
                                       id_nv = r["id_nv"],
                                       hoten = r["hoten"],
                                       username = r["username"],
                                       mobile = r["mobile"],
                                       image = r["image"],
                                       //image = WeworkLiteController.genLinkImage(domain, loginData.CustomerID, r["id_nv"].ToString(), _hostingEnvironment.ContentRootPath),
                                   },
                                   Count = new
                                   {
                                       tong = r["tong"],
                                       ht = r["ht"],
                                       percentage = WeworkLiteController.calPercentage(r["tong"], r["ht"])
                                   }
                               };
                    return JsonResultCommon.ThanhCong(data);
                }
                #endregion
            }
            catch (Exception ex)
            {
                return JsonResultCommon.Exception(ex, _config, loginData.CustomerID);
            }
        }
        [Route("favourite-work")]
        [HttpGet]
        public object FavouriteWork(long id)
        {
            string Token = Common.GetHeader(Request);
            UserJWT loginData = Ulities.GetUserByHeader(HttpContext.Request.Headers);
            if (loginData == null)
                return JsonResultCommon.DangNhap();
            try
            {
                using (DpsConnection cnn = new DpsConnection(_config.ConnectionString))
                {
                    string sqlq = "select ISNULL((select count(*) from we_work where id_row = " + id + "),0)";
                    if (long.Parse(cnn.ExecuteScalar(sqlq).ToString()) != 1)
                        return JsonResultCommon.KhongTonTai("Công việc");
                    sqlq = "select * from we_work_favourite where CreatedBy=" + loginData.UserID + " and id_work=" + id;
                    DataTable dt = cnn.CreateDataTable(sqlq);
                    if (cnn.LastError != null || dt == null)
                        return JsonResultCommon.Exception(cnn.LastError, _config, loginData.CustomerID,ControllerContext);
                    bool value = true;
                    int re = 0;
                    Hashtable val = new Hashtable();
                    if (dt.Rows.Count == 0)
                    {
                        val["id_work"] = id;
                        val["CreatedBy"] = loginData.UserID;
                        val["CreatedDate"] = DateTime.Now;
                        re = cnn.Insert(val, "we_work_favourite");
                    }
                    else
                    {
                        value = !(bool)dt.Rows[0]["disabled"];
                        val["disabled"] = value;
                        val["UpdatedBy"] = loginData.UserID;
                        val["UpdatedDate"] = DateTime.Now;
                        re = cnn.Update(val, new SqlConditions() { { "id_row", dt.Rows[0]["id_row"] } }, "we_work_favourite");
                    }
                    if (re <= 0)
                        return JsonResultCommon.Exception(cnn.LastError, _config, loginData.CustomerID,ControllerContext);
                    return JsonResultCommon.ThanhCong(value);
                }
            }
            catch (Exception ex)
            {
                return JsonResultCommon.Exception(ex, _config, loginData.CustomerID);
            }
        }

        [Route("favourite-project")]
        [HttpGet]
        public object FavouriteProject(long id)
        {
            string Token = Common.GetHeader(Request);
            UserJWT loginData = Ulities.GetUserByHeader(HttpContext.Request.Headers);
            if (loginData == null)
                return JsonResultCommon.DangNhap();
            try
            {
                using (DpsConnection cnn = new DpsConnection(_config.ConnectionString))
                {
                    string sqlq = "select ISNULL((select count(*) from we_project_team where id_row = " + id + "),0)";
                    if (long.Parse(cnn.ExecuteScalar(sqlq).ToString()) != 1)
                        return JsonResultCommon.KhongTonTai("Dự án/phòng ban");
                    sqlq = "select * from we_project_team_user where disabled=0 and id_user=" + loginData.UserID + " and id_project_team=" + id;
                    DataTable dt = cnn.CreateDataTable(sqlq);
                    if (cnn.LastError != null || dt == null)
                        return JsonResultCommon.Exception(cnn.LastError, _config, loginData.CustomerID,ControllerContext);
                    cnn.BeginTransaction();
                    bool value = true;
                    int re = 0;
                    Hashtable val = new Hashtable();
                    if (dt.Rows.Count == 0)
                    {
                        cnn.RollbackTransaction();
                        return JsonResultCommon.Custom("Bạn chưa phải là thành viên của dự án/phòng ban này");
                    }
                    else
                    {
                        value = !(bool)dt.Rows[0]["favourite"];
                        val["favourite"] = value;
                        re = cnn.Update(val, new SqlConditions() { { "id_row", dt.Rows[0]["id_row"] } }, "we_project_team_user");
                    }
                    if (re <= 0)
                    {
                        cnn.RollbackTransaction();
                        return JsonResultCommon.Exception(cnn.LastError, _config, loginData.CustomerID,ControllerContext);
                    }
                    if (!WeworkLiteController.log(cnn, 39, id, loginData.UserID, null, loginData.UserID))
                    {
                        cnn.RollbackTransaction();
                        return JsonResultCommon.Exception(cnn.LastError, _config, loginData.CustomerID,ControllerContext);
                    }
                    cnn.EndTransaction();
                    return JsonResultCommon.ThanhCong(value);
                }
            }
            catch (Exception ex)
            {
                return JsonResultCommon.Exception(ex, _config, loginData.CustomerID);
            }
        }

        [Route("favourite-topic")]
        [HttpGet]
        public object FavouriteTopic(long id)
        {
            string Token = Common.GetHeader(Request);
            UserJWT loginData = Ulities.GetUserByHeader(HttpContext.Request.Headers);
            if (loginData == null)
                return JsonResultCommon.DangNhap();
            try
            {
                using (DpsConnection cnn = new DpsConnection(_config.ConnectionString))
                {
                    string sqlq = "select ISNULL((select count(*) from we_topic where id_row = " + id + "),0)";
                    if (long.Parse(cnn.ExecuteScalar(sqlq).ToString()) != 1)
                        return JsonResultCommon.KhongTonTai("Topic");
                    sqlq = "select * from we_topic_user where disabled=0 and id_user=" + loginData.UserID + " and id_topic=" + id;
                    DataTable dt = cnn.CreateDataTable(sqlq);
                    if (cnn.LastError != null || dt == null)
                        return JsonResultCommon.Exception(cnn.LastError, _config, loginData.CustomerID,ControllerContext);
                    cnn.BeginTransaction();
                    bool value = true;
                    int re = 0;
                    Hashtable val = new Hashtable();
                    if (dt.Rows.Count == 0)
                    {
                        cnn.RollbackTransaction();
                        return JsonResultCommon.Custom("Bạn chưa theo dõi topic này");
                    }
                    else
                    {
                        value = !(bool)dt.Rows[0]["favourite"];
                        val["favourite"] = value;
                        val["UpdatedBy"] = loginData.UserID;
                        val["UpdatedDate"] = DateTime.Now;
                        re = cnn.Update(val, new SqlConditions() { { "id_row", dt.Rows[0]["id_row"] } }, "we_topic_user");
                    }
                    if (re <= 0)
                    {
                        cnn.RollbackTransaction();
                        return JsonResultCommon.Exception(cnn.LastError, _config, loginData.CustomerID,ControllerContext);
                    }
                    if (!WeworkLiteController.log(cnn, 29, id, loginData.UserID, null, loginData.UserID))
                    {
                        cnn.RollbackTransaction();
                        return JsonResultCommon.Exception(cnn.LastError, _config, loginData.CustomerID,ControllerContext);
                    }
                    cnn.EndTransaction();
                    return JsonResultCommon.ThanhCong(value);
                }
            }
            catch (Exception ex)
            {
                return JsonResultCommon.Exception(ex, _config, loginData.CustomerID);
            }
        }

        [Route("follow-topic")]
        [HttpGet]
        public object FollowTopic(long id)
        {
            string Token = Common.GetHeader(Request);
            UserJWT loginData = Ulities.GetUserByHeader(HttpContext.Request.Headers);
            if (loginData == null)
                return JsonResultCommon.DangNhap();
            try
            {
                using (DpsConnection cnn = new DpsConnection(_config.ConnectionString))
                {
                    string sqlq = "select ISNULL((select count(*) from we_topic where id_row = " + id + "),0)";
                    if (long.Parse(cnn.ExecuteScalar(sqlq).ToString()) != 1)
                        return JsonResultCommon.KhongTonTai("Topic");
                    sqlq = "select * from we_topic_user where id_user=" + loginData.UserID + " and id_topic=" + id;
                    DataTable dt = cnn.CreateDataTable(sqlq);
                    if (cnn.LastError != null || dt == null)
                        return JsonResultCommon.Exception(cnn.LastError, _config, loginData.CustomerID,ControllerContext);
                    bool value = true;
                    int re = 0;
                    cnn.BeginTransaction();
                    Hashtable val = new Hashtable();
                    if (dt.Rows.Count == 0)
                    {
                        val["id_topic"] = id;
                        val["CreatedBy"] = loginData.UserID;
                        val["CreatedDate"] = DateTime.Now;
                        re = cnn.Insert(val, "we_topic_user");
                    }
                    else
                    {
                        value = !(bool)dt.Rows[0]["disabled"];
                        val["disabled"] = value;
                        val["UpdatedBy"] = loginData.UserID;
                        val["UpdatedDate"] = DateTime.Now;
                        re = cnn.Update(val, new SqlConditions() { { "id_row", dt.Rows[0]["id_row"] } }, "we_topic_user");
                    }
                    if (re <= 0)
                    {
                        cnn.RollbackTransaction();
                        return JsonResultCommon.Exception(cnn.LastError, _config, loginData.CustomerID,ControllerContext);
                    }

                    if (!WeworkLiteController.log(cnn, value ? 28 : 27, id, loginData.UserID, null, loginData.UserID))
                    {
                        cnn.RollbackTransaction();
                        return JsonResultCommon.Exception(cnn.LastError, _config, loginData.CustomerID,ControllerContext);
                    }
                    cnn.EndTransaction();
                    return JsonResultCommon.ThanhCong(value);
                }
            }
            catch (Exception ex)
            {
                return JsonResultCommon.Exception(ex, _config, loginData.CustomerID);
            }
        }

        [Route("my-staff")]
        [HttpGet]
        public object MyStaff()
        {
            string Token = Common.GetHeader(Request);
            UserJWT loginData = Ulities.GetUserByHeader(HttpContext.Request.Headers);
            if (loginData == null)
                return JsonResultCommon.DangNhap();
            try
            {
                string domain = _config.LinkAPI;
                using (DpsConnection cnn = new DpsConnection(_config.HRConnectionString))
                {
                    DataTable dt = Common.GetListByManager(loginData.UserID.ToString(), cnn);//id_nv, hoten...
                    var data = from r in dt.AsEnumerable()
                               select new
                               {
                                   id_nv = r["id_nv"],
                                   hoten = r["hoten"],
                                   tenchucdanh = r["tenchucdanh"],
                                   //username = r["username"],
                                   //mobile = r["mobile"],
                                   image = WeworkLiteController.genLinkImage(domain, loginData.CustomerID, r["id_nv"].ToString(), _hostingEnvironment.ContentRootPath)
                               };
                    return JsonResultCommon.ThanhCong(data);
                }
            }
            catch (Exception ex)
            {
                return JsonResultCommon.Exception(ex, _config, loginData.CustomerID);
            }
        }

        [Route("my-staff-overview")]
        [HttpGet]
        public object MyStaffOverview([FromQuery] QueryParams query)
        {
            string Token = Common.GetHeader(Request);
            UserJWT loginData = Ulities.GetUserByHeader(HttpContext.Request.Headers);
            if (loginData == null)
                return JsonResultCommon.DangNhap();
            try
            {
                if (query == null)
                    query = new QueryParams();
                string domain = _config.LinkAPI;
                DataTable dt = null;
                using (DpsConnection cnn = new DpsConnection(_config.HRConnectionString))
                {
                    dt = Common.GetListByManager(loginData.UserID.ToString(), cnn);//id_nv, hoten,tenchucdanh...
                }
                using (DpsConnection cnn = new DpsConnection(_config.ConnectionString))
                {

                    if (!string.IsNullOrEmpty(query.filter["keyword"]))
                    {
                        string keyword = query.filter["id_project_team"];
                        dt = dt.AsEnumerable().Where(x => x["hoten"].ToString().Contains(keyword)).CopyToDataTable();
                    }
                    List<string> nvs = dt.AsEnumerable().Select(x => x["id_nv"].ToString()).ToList();
                    if (nvs.Count == 0)
                        return JsonResultCommon.ThanhCong(nvs);

                    Dictionary<string, string> collect = new Dictionary<string, string>
                        {
                            { "CreatedDate", "CreatedDate"},
                            { "Deadline", "Deadline"}
                        };
                    string collect_by = "CreatedDate";
                    if (!string.IsNullOrEmpty(query.filter["collect_by"]))
                        collect_by = collect[query.filter["collect_by"]];

                    #region filter thời gian , keyword
                    SqlConditions cond = new SqlConditions();
                    string strW = "";
                    string strWP = "";
                    DateTime from = DateTime.Now;
                    DateTime to = DateTime.Now;
                    if (!string.IsNullOrEmpty(query.filter["TuNgay"]))
                    {
                        bool from1 = DateTime.TryParseExact(query.filter["TuNgay"], "dd/MM/yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out from);
                        if (!from1)
                            return JsonResultCommon.Custom("Thời gian bắt đầu không hợp lệ");
                        strW += " and w." + collect_by + ">=@from";
                        strWP += " and end_date>=@from";
                        cond.Add("from", from);
                    }
                    if (!string.IsNullOrEmpty(query.filter["DenNgay"]))
                    {
                        bool to1 = DateTime.TryParseExact(query.filter["DenNgay"], "dd/MM/yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out to);
                        if (!to1)
                            return JsonResultCommon.Custom("Thời gian kết thúc không hợp lệ");
                        strW += " and w." + collect_by + "<@to";
                        strWP += " and end_date<@to";
                        cond.Add("to", to);
                    }
                    if (!string.IsNullOrEmpty(query.filter["id_project_team"]))
                    {
                        strW += " and id_project_team=@id_project_team";
                        cond.Add("id_project_team", query.filter["id_project_team"]);
                    }
                    #endregion
                    #region Sort data theo các dữ liệu bên dưới
                    string sortField = "num_work";
                    Dictionary<string, string> sortableFields = new Dictionary<string, string>
                        {
                            { "num_work", "num_work"},
                            { "percentage", "percentage"}
                        };
                    #endregion
                    if (!string.IsNullOrEmpty(query.sortField) && sortableFields.ContainsKey(query.sortField))
                        sortField = sortableFields[query.sortField];
                    string ids = string.Join(",", nvs);
                    string sql = @"select count(distinct p.id_row) as dem,id_user from we_project_team p 
join we_project_team_user u on p.id_row=u.id_project_team 
where p.disabled=0 and u.disabled=0 and u.id_user in (" + ids + ")" + strWP + " group by u.id_user";
                    sql += @";select id_row, id_nv, status,iIf(w.Status=2 and w.end_date>w.deadline,1,0) as is_htquahan,
iIf(w.Status = 1 and getdate() > w.deadline, 1, 0) as is_quahan from v_wework w where id_nv in (" + ids + ")" + strW;
                    DataSet ds = cnn.CreateDataSet(sql, cond);
                    var asP = ds.Tables[0].AsEnumerable();
                    DataTable dtW = ds.Tables[1];
                    bool hasValue = dtW.Rows.Count > 0;
                    int total = 0, success = 0;
                    var data = from r in dt.AsEnumerable()
                               select new
                               {
                                   id_nv = r["id_nv"],
                                   hoten = r["hoten"],
                                   tenchucdanh = r["tenchucdanh"],
                                   //username = r["username"],
                                   //mobile = r["mobile"],
                                   image = WeworkLiteController.genLinkImage(domain, loginData.CustomerID, r["id_nv"].ToString(), _hostingEnvironment.ContentRootPath),
                                   num_project = asP.Where(x => x["id_user"].Equals(r["id_nv"])).Select(x => x["dem"]).DefaultIfEmpty(0).First(),
                                   num_work = total = (hasValue ? (int)dtW.Compute("count(id_nv)", "id_nv=" + r["id_nv"].ToString()) : 0),
                                   num1 = hasValue ? dtW.Compute("count(id_nv)", " status=1 and id_nv=" + r["id_nv"].ToString()) : 0,//đang làm
                                   num2 = success = (hasValue ? (int)dtW.Compute("count(id_nv)", " status=2 and id_nv=" + r["id_nv"].ToString()) : 0),//hoàn thành
                                   num3 = hasValue ? dtW.Compute("count(id_nv)", " status=3 and id_nv=" + r["id_nv"].ToString()) : 0,//đang đánh giá
                                   ht_quahan = hasValue ? dtW.Compute("count(id_nv)", " is_htquahan=1 and id_nv=" + r["id_nv"].ToString()) : 0,
                                   quahan = hasValue ? dtW.Compute("count(id_nv)", " is_quahan=1 and id_nv=" + r["id_nv"].ToString()) : 0,
                                   percentage = total == 0 ? 0 : (success * 100 / total)
                               };
                    if ("desc".Equals(query.sortOrder))
                        data = data.OrderByDescending(x => x.num_work);
                    else
                        data = data.OrderBy(x => x.num_work);
                    return JsonResultCommon.ThanhCong(data);
                }
            }
            catch (Exception ex)
            {
                return JsonResultCommon.Exception(ex, _config, loginData.CustomerID);
            }
        }
    }
}