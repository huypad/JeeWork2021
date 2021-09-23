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
using DPSinfra.ConnectionCache;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace JeeWork_Core2021.Controllers.Wework
{
    [ApiController]
    [Route("api/filter")]
    [EnableCors("JeeWorkPolicy")]
    /// <summary>
    /// api quản lý we_authorize
    /// </summary>
    // [EnableCors(origins: "*", headers: "*", methods: "*")]
    public class FilterWorkController : ControllerBase
    {
        private readonly IHostingEnvironment _hostingEnvironment;
        private JeeWorkConfig _config;
        private IConnectionCache ConnectionCache;
        private IConfiguration _configuration;
        private readonly ILogger<FilterWorkController> _logger;
        public FilterWorkController(IOptions<JeeWorkConfig> config, IHostingEnvironment hostingEnvironment, IConnectionCache _cache, IConfiguration configuration, ILogger<FilterWorkController> logger)
        {
            _hostingEnvironment = hostingEnvironment;
            _config = config.Value;
            ConnectionCache = _cache;
            _configuration = configuration;
            _logger = logger;
        }
        /// <summary>
        /// list custom filter of current user
        /// </summary>
        /// <returns></returns>
        [Route("List")]
        [HttpGet]
        public object List()
        {
           
            UserJWT loginData = Ulities.GetUserByHeader(HttpContext.Request.Headers);
            if (loginData == null)
                return JsonResultCommon.DangNhap();
            try
            {
                string ConnectionString = WeworkLiteController.getConnectionString(ConnectionCache, loginData.CustomerID, _configuration);
                using (DpsConnection cnn = new DpsConnection(ConnectionString))
                {
                    string sql = @"select * from we_filter where disabled=0 and createdby=" + loginData.UserID;
                    DataTable dt = cnn.CreateDataTable(sql);
                    var data = from r in dt.AsEnumerable()
                               select new
                               {
                                   id_row = r["id_row"],
                                   title = r["title"],
                                   color = r["color"]
                               };
                    return JsonResultCommon.ThanhCong(data);
                }
            }
            catch (Exception ex)
            {
                return JsonResultCommon.Exception(_logger,ex, _config, loginData);
            }
        }

        /// <summary>
        /// list filter key in system: we_filter_key
        /// </summary>
        /// <returns></returns>
        [Route("list_filterkey")]
        [HttpGet]
        public object Lite_FilterKey()
        {
           
            UserJWT loginData = Ulities.GetUserByHeader(HttpContext.Request.Headers);
            if (loginData == null)
                return JsonResultCommon.DangNhap();
            try
            {
                string ConnectionString = WeworkLiteController.getConnectionString(ConnectionCache, loginData.CustomerID, _configuration);
                using (DpsConnection cnn = new DpsConnection(ConnectionString))
                {
                    string sql = "select id_row, title, loai,sql from we_filter_key where disabled=0";
                    DataTable dt = cnn.CreateDataTable(sql);
                    if (cnn.LastError != null || dt == null)
                        return JsonResultCommon.Exception(_logger,cnn.LastError, _config, loginData,ControllerContext);
                    cnn.Disconnect();
                    var data = from r in dt.AsEnumerable()
                               select new
                               {
                                   id_row = r["id_row"],
                                   title = r["title"],
                                   loai = r["loai"],
                                   operators = getOperatorVi(r["loai"].ToString()),
                                   options = getOption(r["sql"].ToString(), loginData.CustomerID)
                               };
                    return JsonResultCommon.ThanhCong(data);
                }
            }
            catch (Exception ex)
            {
                return JsonResultCommon.Exception(_logger,ex, _config, loginData);
            }
        }

        /// <summary>
        /// Detail of custom filter by id
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [Route("Detail")]
        [HttpGet]
        public object Detail(long id)
        {
           
            UserJWT loginData = Ulities.GetUserByHeader(HttpContext.Request.Headers);
            if (loginData == null)
                return JsonResultCommon.DangNhap();
            try
            {
                string ConnectionString = WeworkLiteController.getConnectionString(ConnectionCache, loginData.CustomerID, _configuration);
                using (DpsConnection cnn = new DpsConnection(ConnectionString))
                {
                    #region Trả dữ liệu về backend để hiển thị lên giao diện
                    string sqlq = @"select * from we_filter where disabled=0 and createdby=" + loginData.UserID + " and id_row=" + id;
                    sqlq += @";select * from we_filter_detail where id_filter = " + id;
                    DataSet ds = cnn.CreateDataSet(sqlq);
                    if (cnn.LastError != null || ds == null)
                    {
                        return JsonResultCommon.Exception(_logger,cnn.LastError, _config, loginData,ControllerContext);
                    }
                    if (ds.Tables[0] == null || ds.Tables[0].Rows.Count == 0)
                        return JsonResultCommon.KhongTonTai();
                    var data = (from r in ds.Tables[0].AsEnumerable()
                                select new
                                {
                                    id_row = r["id_row"],
                                    title = r["title"],
                                    color = r["color"],
                                    details = from w in ds.Tables[1].AsEnumerable()
                                              select new
                                              {
                                                  id_row = w["id_row"],
                                                  id_key = w["id_key"],
                                                  @operator = w["operator"],
                                                  value = w["value"]
                                              }
                                }).FirstOrDefault();
                    return JsonResultCommon.ThanhCong(data);
                }
                #endregion
            }
            catch (Exception ex)
            {
                return JsonResultCommon.Exception(_logger,ex, _config, loginData);
            }
        }

        /// <summary>
        /// Insert filter
        /// </summary>
        /// <param name="data">FilterModel: title and details are required</param>
        /// <returns></returns>
        [Route("Insert")]
        [HttpPost]
        public async Task<object> Insert(FilterWorkModel data)
        {
           
            UserJWT loginData = Ulities.GetUserByHeader(HttpContext.Request.Headers);
            if (loginData == null)
                return JsonResultCommon.DangNhap();
            try
            {
                string strRe = "";
                if (string.IsNullOrEmpty(data.title))
                    strRe += (strRe == "" ? "" : ",") + "tên filter";
                if (data.details == null || data.details.Count == 0)
                    strRe += (strRe == "" ? "" : ",") + "trường thông tin filter";
                if (strRe != "")
                    return JsonResultCommon.BatBuoc(strRe);

                string ConnectionString = WeworkLiteController.getConnectionString(ConnectionCache, loginData.CustomerID, _configuration);
                using (DpsConnection cnn = new DpsConnection(ConnectionString))
                {
                    long iduser = loginData.UserID;
                    long idk = loginData.CustomerID;
                    Hashtable val = new Hashtable();
                    val.Add("title", data.title);
                    if (string.IsNullOrEmpty(data.color))
                        val.Add("color", "");
                    else
                        val.Add("color", data.color);
                    val.Add("CreatedDate", Common.GetDateTime());
                    val.Add("CreatedBy", iduser);
                    string strCheck = "select count(*) from we_filter where Disabled=0 and (CreatedBy=@id_user) and title=@name";
                    if (int.Parse(cnn.ExecuteScalar(strCheck, new SqlConditions() { { "id_user", loginData.UserID }, { "name", data.title } }).ToString()) > 0)
                    {
                        return JsonResultCommon.Custom("Filter đã tồn tại");
                    }
                    cnn.BeginTransaction();
                    if (cnn.Insert(val, "we_filter") != 1)
                    {
                        cnn.RollbackTransaction();
                        return JsonResultCommon.Exception(_logger,cnn.LastError, _config, loginData,ControllerContext);
                    }
                    string idc = cnn.ExecuteScalar("select IDENT_CURRENT('we_filter')").ToString();
                    Hashtable val1 = new Hashtable();
                    val1["id_filter"] = idc;
                    foreach (var key in data.details)
                    {
                        val1["id_key"] = key.id_key;
                        val1["value"] = key.value;
                        val1["operator"] = key.@operator;
                        strCheck = "select count(*) from we_filter_detail where id_filter = @id_filter and id_key=@id_key";
                        if (int.Parse(cnn.ExecuteScalar(strCheck, new SqlConditions() { { "id_key", key.id_key }, { "id_filter", idc } }).ToString()) > 1)
                        {
                            return JsonResultCommon.Custom("Filter đã tồn tại trong bộ lọc");
                        }
                        if (cnn.Insert(val1, "we_filter_detail") != 1)
                        {
                            cnn.RollbackTransaction();
                            return JsonResultCommon.Exception(_logger,cnn.LastError, _config, loginData,ControllerContext);
                        }
                    }
                    //string LogContent = "", LogEditContent = "";
                    //LogContent = LogEditContent = "Thêm mới dữ liệu filter: title=" + data.title + ", id_user=" + loginData.UserID;
                    //DpsPage.Ghilogfile(loginData.CustomerID.ToString(), LogEditContent, LogContent, loginData.UserName);
                    cnn.EndTransaction();
                    data.id_row = int.Parse(idc);
                    return JsonResultCommon.ThanhCong(data);
                }
            }
            catch (Exception ex)
            {
                return JsonResultCommon.Exception(_logger,ex, _config, loginData);
            }
        }

        /// <summary>
        /// Update filter
        /// </summary>
        /// <param name="data">FilterModel: title and details are required; old detail not in details will be deleted and then inserted detail.idrow=0</param>
        /// <returns></returns>
        [Route("Update")]
        [HttpPost]
        public async Task<object> Update(FilterWorkModel data)
        {
           
            UserJWT loginData = Ulities.GetUserByHeader(HttpContext.Request.Headers);
            if (loginData == null)
                return JsonResultCommon.DangNhap();
            try
            {
                string strRe = "";
                if (string.IsNullOrEmpty(data.title))
                    strRe += (strRe == "" ? "" : ",") + "tên filter";
                if (data.details == null || data.details.Count == 0)
                    strRe += (strRe == "" ? "" : ",") + "trường thông tin filter";
                if (strRe != "")
                    return JsonResultCommon.BatBuoc(strRe);

                string ConnectionString = WeworkLiteController.getConnectionString(ConnectionCache, loginData.CustomerID, _configuration);
                using (DpsConnection cnn = new DpsConnection(ConnectionString))
                {
                    long iduser = loginData.UserID;
                    long idk = loginData.CustomerID;
                    SqlConditions sqlcond = new SqlConditions();
                    sqlcond.Add("id_row", data.id_row);
                    sqlcond.Add("CreatedBy", iduser);
                    string s = "select * from we_filter where createdby=@CreatedBy and disabled=0 and id_row=@id_row";
                    DataTable old = cnn.CreateDataTable(s, sqlcond);
                    if (cnn.LastError != null || old == null)
                        return JsonResultCommon.Exception(_logger,cnn.LastError, _config, loginData,ControllerContext);
                    if (old.Rows.Count == 0)
                        return JsonResultCommon.KhongTonTai("Filter");
                    Hashtable val = new Hashtable();
                    val.Add("title", data.title);
                    if (string.IsNullOrEmpty(data.color))
                        val.Add("color", "");
                    else
                        val.Add("color", data.color);
                    val.Add("UpdatedDate", Common.GetDateTime());
                    val.Add("UpdatedBy", iduser);
                    string strCheck = "select count(*) from we_filter where Disabled=0 and (CreatedBy=@id_user) and title=@name and id_row<>@id_row";
                    if (int.Parse(cnn.ExecuteScalar(strCheck, new SqlConditions() { { "id_user", loginData.UserID }, { "name", data.title }, { "id_row", data.id_row } }).ToString()) > 0)
                    {
                        return JsonResultCommon.Custom("Filter đã tồn tại");
                    }
                    cnn.BeginTransaction();
                    if (cnn.Update(val, sqlcond, "we_filter") != 1)
                    {
                        cnn.RollbackTransaction();
                        return JsonResultCommon.Exception(_logger,cnn.LastError, _config, loginData,ControllerContext);
                    }
                    string ids = string.Join(",", data.details.Where(x => x.id_row > 0).Select(x => x.id_row));
                    if (ids != "")//xóa
                    {
                        string strDel = "delete we_filter_detail where id_filter=" + data.id_row + " and id_row not in (" + ids + ")";
                        if (cnn.ExecuteNonQuery(strDel) < 0)
                        {
                            cnn.RollbackTransaction();
                            return JsonResultCommon.Exception(_logger,cnn.LastError, _config, loginData,ControllerContext);
                        }
                    }
                    else
                    {
                        string strDel = "delete we_filter_detail where id_filter=" + data.id_row ;
                        if (cnn.ExecuteNonQuery(strDel) < 0)
                        {
                            cnn.RollbackTransaction();
                            return JsonResultCommon.Exception(_logger, cnn.LastError, _config, loginData, ControllerContext);
                        }
                    }
                    Hashtable val1 = new Hashtable();
                    val1["id_filter"] = data.id_row;
                    foreach (var key in data.details)
                    {
                        if (key.id_row == 0)
                        {
                            val1["id_key"] = key.id_key;
                            val1["value"] = key.value;
                            val1["operator"] = key.@operator;
                            strCheck = "select count(*) from we_filter_detail where id_filter = @id_filter and id_key=@id_key";
                            if (int.Parse(cnn.ExecuteScalar(strCheck, new SqlConditions() { { "id_key", key.id_key }, { "id_filter", data.id_row } }).ToString()) > 1)
                            {
                                return JsonResultCommon.Custom("Filter đã tồn tại trong bộ lọc");
                            }
                            if (cnn.Insert(val1, "we_filter_detail") != 1)
                            {
                                cnn.RollbackTransaction();
                                return JsonResultCommon.Exception(_logger,cnn.LastError, _config, loginData,ControllerContext);
                            }
                        }
                    }
                    DataTable dt = cnn.CreateDataTable(s, sqlcond);
                    //string LogContent = "", LogEditContent = "";
                    //LogEditContent = DpsPage.GetEditLogContent(old, dt);
                    //if (!LogEditContent.Equals(""))
                    //{
                    //    LogEditContent = "Chỉnh sửa dữ liệu (" + data.id_row + ") : " + LogEditContent;
                    //    LogContent = "Chỉnh sửa dữ liệu filter (" + data.id_row + "), Chi tiết xem trong log chỉnh sửa chức năng";
                    //}
                    //DpsPage.Ghilogfile(loginData.CustomerID.ToString(), LogEditContent, LogContent, loginData.UserName);
                    cnn.EndTransaction();
                    return JsonResultCommon.ThanhCong(data);
                }
            }
            catch (Exception ex)
            {
                return JsonResultCommon.Exception(_logger,ex, _config, loginData);
            }
        }

        /// <summary>
        /// delete filter by id
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [Route("Delete")]
        [HttpGet]
        public BaseModel<object> Delete(long id)
        {
           
            UserJWT loginData = Ulities.GetUserByHeader(HttpContext.Request.Headers);
            if (loginData == null)
                return JsonResultCommon.DangNhap();
            try
            {
                long iduser = loginData.UserID;
                long idk = loginData.CustomerID;
                string ConnectionString = WeworkLiteController.getConnectionString(ConnectionCache, loginData.CustomerID, _configuration);
                using (DpsConnection cnn = new DpsConnection(ConnectionString))
                {
                    string sqlq = "select ISNULL((select count(*) from we_filter where disabled=0 and createdby=" + iduser + " and id_row = " + id + "),0)";
                    if (long.Parse(cnn.ExecuteScalar(sqlq).ToString()) != 1)
                        return JsonResultCommon.KhongTonTai("Filter");
                    //if (Common.TestDuplicate("", id.ToString(), "-1", "v_wework", "id_milestone", "", "-1", cnn, "", true) == false)
                    //{
                    //    return JsonResultCommon.Custom("Đang có công việc thuộc mục tiêu này nên không thể xóa");
                    //}
                    sqlq = "update we_filter set Disabled=1, UpdatedDate=GETUTCDATE(), UpdatedBy=" + iduser + " where id_row = " + id;
                    cnn.BeginTransaction();
                    if (cnn.ExecuteNonQuery(sqlq) != 1)
                    {
                        cnn.RollbackTransaction();
                        return JsonResultCommon.Exception(_logger,cnn.LastError, _config, loginData,ControllerContext);
                    }
                    //string LogContent = "Xóa dữ liệu filter (" + id + ")";
                    //DpsPage.Ghilogfile(loginData.CustomerID.ToString(), LogContent, LogContent, loginData.UserName);
                    cnn.EndTransaction();
                    return JsonResultCommon.ThanhCong();
                }
            }
            catch (Exception ex)
            {
                return JsonResultCommon.Exception(_logger,ex, _config, loginData);
            }
        }

        /// <summary>
        /// get condition string from custom filter
        /// </summary>
        /// <param name="cnn">current connection</param>
        /// <param name="iduser">current user</param>
        /// <param name="id_filter">id_row of filter</param>
        /// <returns>condition string or empty string if filter is not exist</returns>
        public static string genStringWhere(DpsConnection cnn, long iduser, string id_filter, List<AccUsernameModel> DataAccount)
        {
            string re = "";
            string sql = @"select k.*, d.value as fvalue, d.operator from we_filter f join we_filter_detail d  on f.id_row=d.id_filter
join we_filter_key k on k.id_row=d.id_key
where f.disabled=0 and f.createdby=" + iduser + " and f.id_row = " + id_filter;
            DataTable dtKey = cnn.CreateDataTable(sql);
            if (dtKey != null && dtKey.Rows.Count > 0)
            {
                foreach (DataRow dr in dtKey.Rows)
                {
                    string id_row = dr["id_row"].ToString();
                    if (id_row == "1")//loại công việc
                    {
                        if (dr["fvalue"].ToString() == "1")//tôi được giao
                            re += " and w.id_nv=@iduser";
                        if (dr["fvalue"].ToString() == "2")//cv tôi tạo
                            re += " and w.createdby=@iduser";
                        if (dr["fvalue"].ToString() == "3")//cv trong project/team tôi quản lý
                            re += " and w.id_project_team in (select id_project_team from we_project_team_user where disabled=0 and admin=1 and id_user=@iduser)";
                        continue;
                    }
                    if (id_row == "12")//tag
                    {
                        re += @" and w.id_row in (select distinct id_work from we_work_tag wt 
join we_tag t on t.id_row = wt.id_tag
join we_project_team_user u on t.id_project_team = u.id_project_team
where wt.disabled = 0 and t.disabled = 0 and id_user = @iduser and t.title like '%" + dr["fvalue"].ToString() + "%')";
                        continue;
                    }
                    
                    if (id_row == "13" || id_row == "14")//tag
                    {
                        
                        var info = DataAccount.Where(x => x.FullName.ToString().ToLower().Contains(dr["fvalue"].ToString().ToLower())).Select(x => x.UserId.ToString()).ToList();
                        string ids = string.Join(",", info);
                        if (string.IsNullOrEmpty(ids))
                        {
                            ids = "0";
                        }
                        if(id_row == "13")
                        {
                            re += @$" and w.id_nv in ({ids})";
                        }
                        else if(id_row == "16")
                        {
                            re += @$" and w.nguoigiao in ({ids})";
                        }
                        continue;
                    }

                    if (dr["loai"].ToString() == "2")
                        re += string.Format(" and {0} like '%{1}%'", dr["value"], dr["fvalue"]);
                    else
                    {
                        if (dr["loai"].ToString() == "4")//dr["value"] là chuỗi format
                            re += string.Format(dr["value"].ToString(), dr["fvalue"]);
                        else
                        {
                            //3,5,7 kiểu datetime
                            if (id_row == "3" || id_row == "5" || id_row == "7")
                            {
                                if (dr["operator"].ToString() == "=")
                                {
                                    //re += string.Format(" and {0}'{2}'", dr["value"], dr["operator"], dr["fvalue"])
                                    re += string.Format(" and {0} > '{1}' and  {0} < DATEADD(day, 1, '{1}')", dr["value"], dr["fvalue"]);
                                }
                                else
                                {
                                    re += string.Format(" and {0}{1}'{2}'", dr["value"], dr["operator"], dr["fvalue"]);
                                }
                            }
                            else if (id_row == "4" || id_row == "6" || id_row == "8")
                            {
                                re += string.Format(dr["value"].ToString(), dr["fvalue"]);
                            }
                            else
                            {
                                re += string.Format(" and {0}{1}{2}", dr["value"], dr["operator"], dr["fvalue"]);
                            }
                                
                        }
                    }
                }
            }
            return re;
        }

        private object getOption(string v,long CustomerID)
        {
            if (string.IsNullOrEmpty(v))
                return null;
            string ConnectionString = WeworkLiteController.getConnectionString(ConnectionCache, CustomerID, _configuration);
            using (DpsConnection cnn = new DpsConnection(ConnectionString))
            {
                DataTable dt = cnn.CreateDataTable(v);
                if (cnn.LastError != null || dt == null)
                    return null;
                cnn.Disconnect();
                return from r in dt.AsEnumerable()
                       select new
                       {
                           id = r["id"],
                           title = r["title"],
                       };
            }
        }

        private object getOperator(string v)
        {
            List<object> re = new List<object>();
            switch (v)
            {
                case "2":
                    re.Add(new { id = "like", title = "Contains" });
                    break;
                case "3":
                    re.Add(new { id = "=", title = "Equal" });
                    re.Add(new { id = "<>", title = "Not equal" });
                    re.Add(new { id = ">", title = "Larger than" });
                    re.Add(new { id = "<", title = "Smaller thann" });
                    break;
                default:
                    re.Add(new { id = "=", title = "Match" });
                    break;
            }
            return re;
        }
        private object getOperatorVi(string v)
        {
            List<object> re = new List<object>();
            switch (v)
            {
                case "2":
                    re.Add(new { id = "like", title = "Có" });
                    break;
                case "3":
                    re.Add(new { id = "=", title = "Bằng" });
                    re.Add(new { id = "<>", title = "Khác" });
                    re.Add(new { id = ">", title = "Lớn hơn" });
                    re.Add(new { id = "<", title = "Nhỏ hơn" });
                    break;
                default:
                    re.Add(new { id = "=", title = "Bằng" });
                    break;
            }
            return re;
        }
    }
}
