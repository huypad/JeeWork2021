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
using System.Globalization;

namespace JeeWork_Core2021.Controllers.Wework
{
    [ApiController]
    [Route("api/template")]
    [EnableCors("JeeWorkPolicy")]
    /// <summary>
    /// api quản lý we_authorize
    /// </summary>
    //[CusAuthorize(Roles = "3610")]
    public class TemplateController : ControllerBase
    {
        private readonly IHostingEnvironment _hostingEnvironment;
        private JeeWorkConfig _config;
        private IConnectionCache ConnectionCache;
        private IConfiguration _configuration;
        private readonly ILogger<TemplateController> _logger;
        public List<AccUsernameModel> DataAccount;
        public TemplateController(IOptions<JeeWorkConfig> config, IHostingEnvironment hostingEnvironment, IConnectionCache _cache, IConfiguration configuration, ILogger<TemplateController> logger)
        {
            _hostingEnvironment = hostingEnvironment;
            _config = config.Value;
            ConnectionCache = _cache;
            _configuration = configuration;
            _logger = logger;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        [Route("Insert")]
        [HttpPost]
        public async Task<object> Insert(TemplateModel data)
        {
            string Token = Common.GetHeader(Request);
            UserJWT loginData = Ulities.GetUserByHeader(HttpContext.Request.Headers);
            if (loginData == null)
                return JsonResultCommon.DangNhap();
            try
            {
                string strRe = "";
                if (string.IsNullOrEmpty(data.title))
                    strRe += (strRe == "" ? "" : ",") + "tên mẫu";
                if (strRe != "")
                    return JsonResultCommon.BatBuoc(strRe);
                string ConnectionString = WeworkLiteController.getConnectionString(ConnectionCache, loginData.CustomerID, _configuration);
                using (DpsConnection cnn = new DpsConnection(ConnectionString))
                {
                    long iduser = loginData.UserID;
                    long idk = loginData.CustomerID;
                    Hashtable val = new Hashtable();
                    val.Add("title", data.title);
                    if (!string.IsNullOrEmpty(data.description))
                        val.Add("description", data.description);
                    else
                        val.Add("description", DBNull.Value);
                    val.Add("CustomerID", loginData.CustomerID);
                    if (!string.IsNullOrEmpty(data.color))
                        val.Add("color", data.color);
                    val.Add("CreatedDate", DateTime.Now);
                    val.Add("CreatedBy", iduser);
                    val.Add("IsDefault", 0);
                    string strCheck = "select count(*) from we_template_customer where Disabled=0 and (CustomerID=@customerid) and title=@name";
                    if (int.Parse(cnn.ExecuteScalar(strCheck, new SqlConditions() { { "customerid", data.customerid }, { "name", data.title } }).ToString()) > 0)
                    {
                        return JsonResultCommon.Custom("Mẫu đã tồn tại trong danh sách");
                    }
                    cnn.BeginTransaction();
                    if (cnn.Insert(val, "we_template_customer") != 1)
                    {
                        cnn.RollbackTransaction();
                        return JsonResultCommon.Exception(_logger, cnn.LastError, _config, loginData, ControllerContext);
                    }
                    long idc = long.Parse(cnn.ExecuteScalar("select IDENT_CURRENT('we_template_customer')").ToString());
                    if (data.Status != null)
                    {
                        Hashtable has = new Hashtable();
                        has["TemplateID"] = idc;
                        has["CreatedDate"] = DateTime.Now;
                        has["CreatedBy"] = iduser;
                        foreach (var item in data.Status)
                        {
                            string position = cnn.ExecuteScalar("select Max(position) from we_Template_Status where TemplateID =" + idc + "").ToString();
                            if (position == null)
                                position = "4";
                            has["StatusName"] = item.StatusName;
                            has["Type"] = 1;
                            has["IsDefault"] = 0;
                            has["color"] = item.color;
                            has["Position"] = int.Parse(position) + 1;
                            has["IsFinal"] = 0;
                            has["IsDeadline"] = 0;
                            has["IsTodo"] = 0;
                            if (cnn.Insert(has, "we_Template_Status") != 1)
                            {
                                cnn.RollbackTransaction();
                                return JsonResultCommon.Exception(_logger, cnn.LastError, _config, loginData, ControllerContext);
                            }
                        }
                    }
                    if (!WeworkLiteController.log(_logger, loginData.Username, cnn, 45, idc, iduser, data.title))
                    {
                        cnn.RollbackTransaction();
                        return JsonResultCommon.Exception(_logger, cnn.LastError, _config, loginData, ControllerContext);
                    }
                    data.id_row = idc;
                    cnn.EndTransaction();
                    return JsonResultCommon.ThanhCong(data);
                }
            }
            catch (Exception ex)
            {
                return JsonResultCommon.Exception(_logger, ex, _config, loginData);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        [Route("Update")]
        [HttpPost]
        public async Task<BaseModel<object>> Update(TemplateModel data)
        {
            string Token = Common.GetHeader(Request);
            UserJWT loginData = Ulities.GetUserByHeader(HttpContext.Request.Headers);
            if (loginData == null)
                return JsonResultCommon.DangNhap();
            try
            {
                string strRe = "";
                if (string.IsNullOrEmpty(data.title))
                    strRe += (strRe == "" ? "" : ",") + "tên mẫu";
                if (strRe != "")
                    return JsonResultCommon.BatBuoc(strRe);
                string ConnectionString = WeworkLiteController.getConnectionString(ConnectionCache, loginData.CustomerID, _configuration);
                using (DpsConnection cnn = new DpsConnection(ConnectionString))
                {
                    SqlConditions sqlcond = new SqlConditions();
                    sqlcond.Add("id_row", data.id_row);
                    sqlcond.Add("disabled", 0);
                    sqlcond.Add("CustomerID", loginData.CustomerID);
                    string s = "select * from we_template_customer where (where)";
                    DataTable old = cnn.CreateDataTable(s, "(where)", sqlcond);
                    if (old == null || old.Rows.Count == 0)
                        return JsonResultCommon.KhongTonTai("Template");
                    long iduser = loginData.UserID;
                    long idk = loginData.CustomerID;
                    Hashtable val = new Hashtable();
                    val.Add("title", data.title);
                    if (!string.IsNullOrEmpty(data.description))
                        val.Add("description", data.description);
                    else
                        val.Add("description", DBNull.Value);
                    val.Add("CustomerID", loginData.CustomerID);
                    if (!string.IsNullOrEmpty(data.color))
                        val.Add("color", data.color);
                    val.Add("CreatedDate", DateTime.Now);
                    val.Add("CreatedBy", iduser);
                    val.Add("IsDefault", 0);
                    string strCheck = "select count(*) from we_Template_Status where Disabled=0 and (customerid=@customerid) and title=@name and id_row<>@id";
                    if (int.Parse(cnn.ExecuteScalar(strCheck, new SqlConditions() { { "customerid", data.customerid }, { "name", data.title }, { "id", data.id_row } }).ToString()) > 0)
                    {
                        return JsonResultCommon.Custom("Mẫu đã có trong danh sách");
                    }
                    cnn.BeginTransaction();
                    if (cnn.Update(val, sqlcond, "we_Template_Status") != 1)
                    {
                        cnn.RollbackTransaction();
                        return JsonResultCommon.Exception(_logger, cnn.LastError, _config, loginData, ControllerContext);
                    }
                    if (data.Status != null)
                    {
                        Hashtable has = new Hashtable();
                        has["TemplateID"] = data.id_row;
                        foreach (var item in data.Status)
                        {
                            sqlcond = new SqlConditions();
                            sqlcond.Add("id_row", item.id_row);
                            string position = cnn.ExecuteScalar("select Max(position) from we_Template_Status where TemplateID =" + data.id_row + "").ToString();
                            if (position == null)
                                position = "4";
                            has["StatusName"] = item.StatusName;
                            has["color"] = item.color;
                            if (item.id_row > 0)
                            {
                                has["UpdatedDate"] = DateTime.Now;
                                has["UpdatedBy"] = iduser;
                                if (cnn.Update(has, sqlcond, "we_Template_Status") != 1)
                                {
                                    cnn.RollbackTransaction();
                                    return JsonResultCommon.Exception(_logger, cnn.LastError, _config, loginData, ControllerContext);
                                }
                            }
                            else
                            {
                                has["Position"] = int.Parse(position) + 1;
                                has["IsFinal"] = 0;
                                has["IsDeadline"] = 0;
                                has["IsTodo"] = 0;
                                has["Type"] = 1;
                                has["IsDefault"] = 0;
                                has["CreatedDate"] = DateTime.Now;
                                has["CreatedBy"] = iduser;
                                if (cnn.Insert(has, "we_Template_Status") != 1)
                                {
                                    cnn.RollbackTransaction();
                                    return JsonResultCommon.Exception(_logger, cnn.LastError, _config, loginData, ControllerContext);
                                }
                            }
                        }
                    }
                    DataTable dt = cnn.CreateDataTable(s, "(where)", sqlcond);
                    if (!WeworkLiteController.log(_logger, loginData.Username, cnn, 45, data.id_row, iduser))
                    {
                        cnn.RollbackTransaction();
                        return JsonResultCommon.Exception(_logger, cnn.LastError, _config, loginData, ControllerContext);
                    }
                    cnn.EndTransaction();
                    return JsonResultCommon.ThanhCong(data);
                }
            }
            catch (Exception ex)
            {
                return JsonResultCommon.Exception(_logger, ex, _config, loginData);
            }
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [Route("Delete")]
        [HttpGet]
        public BaseModel<object> Delete(long id, bool isDelStatus)
        {
            string Token = Common.GetHeader(Request);
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
                    string sqlq = "";
                    string errors = "";
                    string tablename = "we_template_status";
                    if (isDelStatus)
                    {
                        sqlq = "select ISNULL((select count(*) from we_Template_Status where Disabled=0 and  id_row = " + id + "),0)";
                        errors = "Tình trạng";
                    }
                    else
                    {
                        sqlq = "select ISNULL((select count(*) from we_template_customer where Disabled=0 and  id_row = " + id + "),0)";
                        errors = "Mẫu";
                        tablename = "we_template_customer";

                    }
                    if (long.Parse(cnn.ExecuteScalar(sqlq).ToString()) != 1)
                        return JsonResultCommon.KhongTonTai(errors);
                    sqlq = "update " + tablename + " set Disabled=1, UpdatedDate=getdate(), UpdatedBy=" + iduser + " where id_row = " + id;
                    cnn.BeginTransaction();
                    if (cnn.ExecuteNonQuery(sqlq) != 1)
                    {
                        cnn.RollbackTransaction();
                        return JsonResultCommon.Exception(_logger, cnn.LastError, _config, loginData, ControllerContext);
                    }
                    if (!isDelStatus)
                    {
                        //sqlq = "update we_template_status set disabled = 1, UpdatedDate=getdate(), UpdatedBy=" + iduser + " where TemplateID = " + id;
                        cnn.BeginTransaction();
                        if (cnn.ExecuteNonQuery(sqlq) != 1)
                        {
                            cnn.RollbackTransaction();
                            return JsonResultCommon.Exception(_logger, cnn.LastError, _config, loginData, ControllerContext);
                        }
                    }
                    if (!WeworkLiteController.log(_logger, loginData.Username, cnn, 47, id, iduser))
                    {
                        cnn.RollbackTransaction();
                        return JsonResultCommon.Exception(_logger, cnn.LastError, _config, loginData, ControllerContext);
                    }
                    cnn.EndTransaction();
                    return JsonResultCommon.ThanhCong();
                }
            }
            catch (Exception ex)
            {
                return JsonResultCommon.Exception(_logger, ex, _config, loginData);
            }
        }
        /// <summary>
        /// Thêm/Sửa nhanh các trường title, color cho template & status
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        [Route("Update_Quick")]
        [HttpPost]
        public async Task<BaseModel<object>> Update_Quick(UpdateQuickModel data)
        {
            string Token = Common.GetHeader(Request);
            UserJWT loginData = Ulities.GetUserByHeader(HttpContext.Request.Headers);
            if (loginData == null)
                return JsonResultCommon.DangNhap();
            try
            {
                Hashtable val = new Hashtable();
                SqlConditions sqlcond = new SqlConditions();
                long iduser = loginData.UserID;
                long idk = loginData.CustomerID;
                string tablename = "we_template_status";
                string strRe = "";
                if (string.IsNullOrEmpty(data.values))
                    strRe += (strRe == "" ? "" : ",") + "tên";
                if (strRe != "")
                    return JsonResultCommon.BatBuoc(strRe);
                string strCheck = "";

                sqlcond = new SqlConditions();
                //sqlcond.Add("customerid", loginData.CustomerID);
                sqlcond.Add("name", data.values);
                long _object = 0;
                strCheck = "select count(*) from " + tablename + " where Disabled=0";
                if (data.istemplate)
                {
                    val.Add("CustomerID", loginData.CustomerID);
                    sqlcond.Add("customerid", loginData.CustomerID);
                    tablename = "we_template_customer";
                    strCheck = "select count(*) from " + tablename + " where Disabled=0";
                    strCheck += " and title=@name";
                    strCheck += " and (customerid=@customerid)";
                }
                else
                {
                    val.Add("TemplateID", data.id_template);
                    val.Remove("CustomerID");
                    strCheck += " and statusname=@name";
                    sqlcond.Remove(sqlcond["customerid"]);
                }
                string ConnectionString = WeworkLiteController.getConnectionString(ConnectionCache, loginData.CustomerID, _configuration);
                using (DpsConnection cnn = new DpsConnection(ConnectionString))
                {
                    if (data.columname.Equals("title"))
                    {

                        _object = long.Parse(cnn.ExecuteScalar(strCheck, sqlcond).ToString());
                        if (_object > 0)
                        {
                            if (data.istemplate)
                                return JsonResultCommon.Custom("Mẫu đã tồn tại trong danh sách");
                            else
                                return JsonResultCommon.Custom("Tình trạng đã tồn tại trong danh sách");
                        }

                    }
                    val.Add(data.columname, data.values);
                    string s = "";
                    cnn.BeginTransaction();
                    if (data.id_row > 0)
                    {
                        sqlcond.Remove(sqlcond["name"]);
                        //sqlcond.Add("title", data.values);
                        sqlcond.Add("id_row", data.id_row);
                        sqlcond.Add("disabled", 0);
                        s = "select * from " + tablename + " where (where)";

                        DataTable old = cnn.CreateDataTable(s, "(where)", sqlcond);
                        if (old == null || old.Rows.Count == 0)
                            return JsonResultCommon.KhongTonTai("Template");
                        val.Add("UpdatedDate", DateTime.Now);
                        val.Add("UpdatedBy", iduser);
                        if (cnn.Update(val, sqlcond, tablename) != 1)
                        {
                            cnn.RollbackTransaction();
                            return JsonResultCommon.Exception(_logger, cnn.LastError, _config, loginData, ControllerContext);
                        }

                        if (!data.istemplate)
                        {
                            string updatenew = "update we_status set  UpdatedDate=getdate(), UpdatedBy=" + iduser + ", " + data.columname + " = N'" + data.values + "' where StatusID_Reference = " + data.id_row;
                            if (cnn.ExecuteNonQuery(updatenew) < 0)
                            {
                                cnn.RollbackTransaction();
                                return JsonResultCommon.Exception(_logger, cnn.LastError, _config, loginData, ControllerContext);
                            }
                        }

                        if (!WeworkLiteController.log(_logger, loginData.Username, cnn, 46, data.id_row, iduser))
                        {
                            cnn.RollbackTransaction();
                            return JsonResultCommon.Exception(_logger, cnn.LastError, _config, loginData, ControllerContext);
                        }
                    }
                    else
                    {
                        val.Add("CreatedDate", DateTime.Now);
                        val.Add("CreatedBy", iduser);
                        if (cnn.Insert(val, tablename) != 1)
                        {
                            cnn.RollbackTransaction();
                            return JsonResultCommon.Exception(_logger, cnn.LastError, _config, loginData, ControllerContext);
                        }
                        long idc = long.Parse(cnn.ExecuteScalar("select IDENT_CURRENT('" + tablename + "')").ToString());
                        if (data.istemplate)
                        {
                            string sql_insert = "";
                            sql_insert = $@"insert into we_Template_Status (StatusID, TemplateID, StatusName, description, CreatedDate, CreatedBy, Disabled, Type, IsDefault, color, Position, IsFinal, IsDeadline, IsTodo) " +
                            "select id_Row, " + idc + ", StatusName, description, getdate(), 0, Disabled, Type, IsDefault, color, Position, IsFinal, IsDeadline, IsTodo " +
                            "from we_Status_List where Disabled = 0 and IsDefault = 1";
                            cnn.ExecuteNonQuery(sql_insert);
                        }
                        if (!WeworkLiteController.log(_logger, loginData.Username, cnn, 45, data.id_row, iduser))
                        {
                            cnn.RollbackTransaction();
                            return JsonResultCommon.Exception(_logger, cnn.LastError, _config, loginData, ControllerContext);
                        }
                    }
                    cnn.EndTransaction();
                    return JsonResultCommon.ThanhCong(data);
                }
            }
            catch (Exception ex)
            {
                return JsonResultCommon.Exception(_logger, ex, _config, loginData);
            }
        }
        #region Các API liên quan đến Template center
        /// <summary>
        /// GetListTemplateCenter
        /// </summary>
        /// <param name="query"></param>
        /// <returns></returns>
        [Route("get-list-template-center")]
        [HttpGet]
        public object GetListTemplateCenter([FromQuery] QueryParams query)
        {
            UserJWT loginData = Ulities.GetUserByHeader(HttpContext.Request.Headers);
            if (loginData == null)
                return JsonResultCommon.DangNhap();
            try
            {
                if (query == null)
                    query = new QueryParams();
                string ConnectionString = WeworkLiteController.getConnectionString(ConnectionCache, loginData.CustomerID, _configuration);
                using (DpsConnection cnn = new DpsConnection(ConnectionString))
                {
                    Dictionary<string, string> collect = new Dictionary<string, string>
                        {
                            { "id_row", "id_row"},
                            { "title", "title"},
                        };
                    SqlConditions cond = new SqlConditions();
                    string where_template = "";
                    string types = "", levels = "", template_typeid = "", collect_by = "";
                    #region Filter
                    if (!string.IsNullOrEmpty(query.filter["keyword"]))
                    {
                        where_template += " and (title like N'%@keyword%') ";
                        where_template = where_template.Replace("@keyword", query.filter["keyword"]);
                    }
                    if (!string.IsNullOrEmpty(query.filter["template_typeid"]))//API: WeworkLiteController.lite_template_types
                    {
                        template_typeid = query.filter["template_typeid"];
                        where_template += $" and list.template_typeid in ({template_typeid})";
                    }
                    if (!string.IsNullOrEmpty(query.filter["types"]))//1 - space, 2 - folder, 3 - list (Project)
                    {
                        types = query.filter["types"];
                        where_template += $" and list.types in ({types})"; // 1,2,3 || 1 || 1,3
                    }
                    if (!string.IsNullOrEmpty(query.filter["levels"]))//1 - Beginner, 2 - Intermediate, 3 - Advanced
                    {
                        levels = query.filter["levels"];
                        where_template += $" and list.levels in ({levels})"; // 1,2,3 || 1 || 1,3
                    }
                    if (!string.IsNullOrEmpty(query.filter["collect_by"]))//Người tạo (Table: we_template_customer)
                    {
                        collect_by = query.filter["collect_by"];
                        where_template += $" and list.createdby in ({collect_by})";
                    }
                    #endregion
                    #region Trả dữ liệu về backend để hiển thị lên giao diện
                    string sqlq = @"select id_row, title, isdefault, types
                                    from we_template_types 
                                    where disabled = 0 
                                    and id_row in (select template_typeid 
                                    from we_template_customer list
                                    where disabled = 0 and is_template_center = 1 " + where_template + ")";
                    sqlq += @";select id_row, title, description, isdefault, color
                                    , is_template_center, types, levels, img_temp, share_with, sample_id
                                    , viewid, group_statusid, template_typeid, field_id
                                    from we_template_customer list
                                    where disabled = 0 and is_template_center = 1 " + where_template;
                    sqlq += @";select id_row, title, description, isdefault, color
                                    , is_template_center, types, levels, img_temp, share_with, sample_id
                                    , viewid, group_statusid, template_typeid, field_id
                                    from we_template_list list
                                    where disabled = 0 and is_template_center = 1 " + where_template;
                    DataSet ds = cnn.CreateDataSet(sqlq, cond);
                    if (cnn.LastError != null || ds == null)
                        return JsonResultCommon.Exception(_logger, cnn.LastError, _config, loginData, ControllerContext);
                    var data = from r in ds.Tables[0].AsEnumerable()
                               select new
                               {
                                   id_row = r["id_row"],
                                   title = r["title"],
                                   isdefault = r["isdefault"],
                                   types = r["types"],
                                   Data_Templates = from s in ds.Tables[1].AsEnumerable()
                                                    where s["template_typeid"].ToString() == r["id_row"].ToString()
                                                    select new
                                                    {
                                                        id_row = s["id_row"],
                                                        title = s["title"],
                                                        description = s["description"],
                                                        isdefault = s["isdefault"],
                                                        color = s["color"],
                                                        is_template_center = s["is_template_center"],
                                                        types = s["types"],
                                                        levels = s["levels"],
                                                        viewid = s["viewid"],
                                                        group_statusid = s["group_statusid"],
                                                        template_typeid = s["template_typeid"],
                                                        img_temp = s["img_temp"],
                                                        field_id = s["field_id"],
                                                        share_with = s["share_with"],
                                                        sample_id = s["sample_id"]
                                                    },
                                   Data_Templates_Default = from d in ds.Tables[2].AsEnumerable()
                                                            where d["template_typeid"].ToString() == r["id_row"].ToString()
                                                            select new
                                                            {
                                                                id_row = d["id_row"],
                                                                title = d["title"],
                                                                description = d["description"],
                                                                isdefault = d["isdefault"],
                                                                color = d["color"],
                                                                is_template_center = d["is_template_center"],
                                                                types = d["types"],
                                                                levels = d["levels"],
                                                                viewid = d["viewid"],
                                                                group_statusid = d["group_statusid"],
                                                                template_typeid = d["template_typeid"],
                                                                img_temp = d["img_temp"],
                                                                field_id = d["field_id"],
                                                                share_with = d["share_with"],
                                                                sample_id = d["sample_id"]
                                                            },
                               };
                    return JsonResultCommon.ThanhCong(data);
                    #endregion
                }
            }
            catch (Exception ex)
            {
                return JsonResultCommon.Exception(_logger, ex, _config, loginData);
            }
        }
        /// <summary>
        /// GetListTemplateCenter
        /// </summary>
        /// <param name="query"></param>
        /// <returns></returns>
        [Route("get-template-by-user")]
        [HttpGet]
        public object GetTemplate_ByUser([FromQuery] QueryParams query)
        {
            UserJWT loginData = Ulities.GetUserByHeader(HttpContext.Request.Headers);
            if (loginData == null)
                return JsonResultCommon.DangNhap();
            try
            {
                if (query == null)
                    query = new QueryParams();
                string ConnectionString = WeworkLiteController.getConnectionString(ConnectionCache, loginData.CustomerID, _configuration);
                using (DpsConnection cnn = new DpsConnection(ConnectionString))
                {
                    Dictionary<string, string> collect = new Dictionary<string, string>
                        {
                            { "id_row", "id_row"},
                            { "title", "title"},
                        };
                    SqlConditions cond = new SqlConditions();
                    string where_template = "";
                    string collect_by = "";
                    #region Filter
                    if (!string.IsNullOrEmpty(query.filter["keyword"]))
                    {
                        where_template += " and (title like N'%@keyword%') ";
                        where_template = where_template.Replace("@keyword", query.filter["keyword"]);
                    }
                    if (string.IsNullOrEmpty(query.filter["collect_by"]))// Nếu không truyền mặc định lấy người tạo OR list_share có người tạo
                    {
                        collect_by = loginData.UserID.ToString();
                        //where_template += $" and (list.createdby = " + loginData.UserID + " " +
                        //    "or (list_share like '" + loginData.UserID + "%' " +
                        //    "or list_share like '%" + loginData.UserID + "')) ";
                    }
                    else
                    {
                        collect_by = query.filter["collect_by"];
                    }
                    where_template += $" and (list.id_row in (select id_template from we_template_library where disabled = 0 and id_user =" + collect_by + "))";

                    #endregion
                    #region Trả dữ liệu về backend để hiển thị lên giao diện
                    string sqlq = "";
                    sqlq += @"select id_row, title, description, isdefault, color
                                    , is_template_center, types, levels, img_temp
                                    , viewid, group_statusid, template_typeid
                                    , field_id, sample_id, share_with
                                    from we_template_customer list
                                    where disabled = 0 
                                    and is_template_center = 1 " + where_template + " " +
                                    "order by title";
                    string sqlListShare = "select * from we_template_library where disabled = 0";
                    DataTable dtShare = cnn.CreateDataTable(sqlListShare, cond);
                    DataTable dt = cnn.CreateDataTable(sqlq, cond);

                    if (cnn.LastError != null || dt == null)
                        return JsonResultCommon.Exception(_logger, cnn.LastError, _config, loginData, ControllerContext);
                    if (dt.Rows.Count == 0)
                        return JsonResultCommon.ThanhCong(new List<string>());
                    var data = from r in dt.AsEnumerable()
                               select new
                               {
                                   id_row = r["id_row"],
                                   title = r["title"],
                                   description = r["description"],
                                   isdefault = r["isdefault"],
                                   color = r["color"],
                                   is_template_center = r["is_template_center"],
                                   types = r["types"],
                                   levels = r["levels"],
                                   img_temp = r["img_temp"],
                                   viewid = r["viewid"],
                                   field_id = r["field_id"],
                                   group_statusid = r["group_statusid"],
                                   template_typeid = r["template_typeid"],
                                   share_with = r["share_with"],
                                   sample_id = r["sample_id"],
                                   list_share = from s in dtShare.AsEnumerable()
                                                where r["id_row"].ToString().Equals(s["id_template"].ToString())
                                                select new
                                                {
                                                    id_user = s["id_user"],
                                                    id_row = s["id_row"],
                                                    id_template = s["id_template"],
                                                },
                               };
                    return JsonResultCommon.ThanhCong(data);
                    #endregion
                }
            }
            catch (Exception ex)
            {
                return JsonResultCommon.Exception(_logger, ex, _config, loginData);
            }
        }
        /// <summary>
        /// Detail (id của template trong bảng we_template_customer)
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [Route("Detail")]
        [HttpGet]
        public object Detail(long id)
        {
            string Token = Common.GetHeader(Request);
            UserJWT loginData = Ulities.GetUserByHeader(HttpContext.Request.Headers);
            if (loginData == null)
                return JsonResultCommon.DangNhap();
            try
            {
                #region Lấy dữ liệu account từ JeeAccount
                DataAccount = WeworkLiteController.GetAccountFromJeeAccount(HttpContext.Request.Headers, _configuration);
                if (DataAccount == null)
                    return JsonResultCommon.Custom("Lỗi lấy danh sách nhân viên từ hệ thống quản lý tài khoản");

                string error = "";
                string listID = WeworkLiteController.ListAccount(HttpContext.Request.Headers, out error, _configuration);
                if (error != "")
                    return JsonResultCommon.Custom(error);
                #endregion
                PageModel pageModel = new PageModel();
                string domain = _configuration.GetValue<string>("Host:JeeWork_API") + "/";
                string ConnectionString = WeworkLiteController.getConnectionString(ConnectionCache, loginData.CustomerID, _configuration);
                //bool Visible = Common.CheckRoleByToken(Token, "3403", ConnectionString, DataAccount);
                bool Visible = true;
                using (DpsConnection cnn = new DpsConnection(ConnectionString))
                {
                    #region Trả dữ liệu về backend để hiển thị lên giao diện
                    string sqlq = @$"select id_row, title, description, createddate, createdby
                                    , isdefault, color, id_department, templateid, customerid
                                    , is_template_center, types, levels, viewid, group_statusid 
                                    , template_typeid, img_temp, field_id, share_with, sample_id
                                    from we_template_customer 
                                    where is_template_center = 1 and id_row=" + id;
                    string list_viewid = "", group_statusid = "", field_id = "";
                    DataTable dt_Detail = new DataTable();
                    dt_Detail = cnn.CreateDataTable(sqlq);
                    list_viewid = dt_Detail.Rows[0]["viewid"].ToString();
                    if (string.IsNullOrEmpty(list_viewid)) list_viewid = "0";
                    group_statusid = dt_Detail.Rows[0]["group_statusid"].ToString();
                    if (string.IsNullOrEmpty(group_statusid)) group_statusid = "0";
                    field_id = dt_Detail.Rows[0]["field_id"].ToString();
                    if (string.IsNullOrEmpty(field_id)) field_id = "0";
                    sqlq += @$";select id_row, view_name, description, is_default, icon, link, image, templateid 
                                from  we_default_views 
                                where id_row in (" + list_viewid + ") " +
                                "order by is_default desc";
                    sqlq += @$";select id_field, fieldname, title, type, position, isdefault, typeid
                            from we_fields 
                            where isNewField = 1 and IsDel = 0 and isvisible = 0 
                            and id_field in (" + field_id + ") " +
                            "order by position, title";
                    sqlq += @$";select id_row, title, description, locked, array_status 
                                from we_status_group 
                                where id_row in (" + group_statusid + ") " +
                                "order by title";
                    sqlq += @$";select distinct id_user, '' as hoten, '' as id_user,'' as image,'' as username,'' as mobile,'' as Email,'' as tenchucdanh
from we_template_library where disabled = 0 and id_template = " + id;
                    DataSet ds = cnn.CreateDataSet(sqlq);
                    if (cnn.LastError != null || ds == null)
                    {
                        return JsonResultCommon.Exception(_logger, cnn.LastError, _config, loginData, ControllerContext);
                    }
                    #region Map info account từ JeeAccount

                    foreach (DataRow item in ds.Tables[4].Rows)
                    {
                        var info = DataAccount.Where(x => item["id_user"].ToString().Contains(x.UserId.ToString())).FirstOrDefault();
                        if (info != null)
                        {
                            item["hoten"] = info.FullName;
                            item["image"] = info.AvartarImgURL;
                            item["username"] = info.Username;
                            item["mobile"] = info.PhoneNumber;
                            item["Email"] = info.Email;
                            item["tenchucdanh"] = info.Jobtitle;
                        }
                    }
                    #endregion
                    if (ds.Tables[0] == null || ds.Tables[0].Rows.Count == 0)
                        return JsonResultCommon.KhongTonTai();
                    ds.Tables[3].Columns.Add("DataStatus", typeof(DataTable));
                    if (ds.Tables[3].Rows.Count > 0)
                    {
                        foreach (DataRow dr in ds.Tables[3].Rows)
                        {
                            dr["DataStatus"] = dtChildren(dr["array_status"].ToString(), cnn);
                        }
                    }
                    var data = (from r in ds.Tables[0].AsEnumerable()
                                select new
                                {
                                    id_row = r["id_row"],
                                    title = r["title"],
                                    description = r["description"],
                                    isdefault = r["isdefault"],
                                    createddate = string.Format("{0:dd/MM/yyyy HH:mm}", r["createddate"]),
                                    createdby = r["createdby"],
                                    color = r["color"],
                                    templateid = r["templateid"],
                                    customerid = r["customerid"],
                                    is_template_center = r["is_template_center"],
                                    types = r["types"],
                                    levels = r["levels"],
                                    viewid = r["viewid"],
                                    group_statusid = r["group_statusid"],
                                    template_typeid = r["template_typeid"],
                                    img_temp = r["img_temp"],
                                    field_id = r["field_id"],
                                    share_with = r["share_with"],
                                    sample_id = r["sample_id"],
                                    data_views = from rr in ds.Tables[1].AsEnumerable()
                                                 select new
                                                 {
                                                     id_row = rr["id_row"],
                                                     view_name = rr["view_name"],
                                                     description = rr["description"],
                                                     is_default = rr["is_default"],
                                                     icon = rr["icon"],
                                                     link = rr["link"],
                                                     image = rr["image"],
                                                 },
                                    data_fields = from field in ds.Tables[2].AsEnumerable()
                                                  select new
                                                  {
                                                      id_field = field["id_field"],
                                                      fieldname = field["fieldname"],
                                                      title = field["title"],
                                                      type = field["type"],
                                                      position = field["position"],
                                                      isdefault = field["isdefault"],
                                                      typeid = field["typeid"],
                                                  },
                                    list_share = from share in ds.Tables[4].AsEnumerable()
                                                 select new
                                                 {
                                                     id_nv = share["id_user"],
                                                     hoten = share["hoten"],
                                                     image = share["image"],
                                                     username = share["username"],
                                                     mobile = share["mobile"],
                                                     Email = share["Email"],
                                                 },
                                    data_status = from status in ds.Tables[3].AsEnumerable()
                                                  select new
                                                  {
                                                      id_row = status["id_row"],
                                                      title = status["title"],
                                                      description = status["description"],
                                                      locked = status["locked"],
                                                      status_list = (((DataTable)status["DataStatus"]) != null
                                                      && ((DataTable)status["DataStatus"]).Rows.Count > 0) ?
                                                      from _status in ((DataTable)status["DataStatus"]).AsEnumerable()
                                                      select new
                                                      {
                                                          id_row = _status["id_row"],
                                                          statusname = _status["statusname"],
                                                          isdefault = _status["isdefault"],
                                                          color = _status["color"],
                                                          position = _status["position"],
                                                          isfinal = _status["isfinal"],
                                                          isdeadline = _status["isdeadline"],
                                                          istodo = _status["istodo"],
                                                      } : null,
                                                  }
                                }).FirstOrDefault();

                    return JsonResultCommon.ThanhCong(data, pageModel, Visible);
                }
                #endregion
            }
            catch (Exception ex)
            {
                return JsonResultCommon.Exception(_logger, ex, _config, loginData);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        [Route("save-as-template")]
        [HttpPost]
        public async Task<object> SaveAsTemplate(TemplateCenterModel data)
        {
            string Token = Common.GetHeader(Request);
            UserJWT loginData = Ulities.GetUserByHeader(HttpContext.Request.Headers);
            if (loginData == null)
                return JsonResultCommon.DangNhap();
            try
            {
                string strRe = "";
                string error = "";
                if (string.IsNullOrEmpty(data.title))
                    strRe += (strRe == "" ? "" : ",") + "tên mẫu";
                if (strRe != "")
                    return JsonResultCommon.BatBuoc(strRe);
                string ConnectionString = WeworkLiteController.getConnectionString(ConnectionCache, loginData.CustomerID, _configuration);
                using (DpsConnection cnn = new DpsConnection(ConnectionString))
                {
                    cnn.BeginTransaction();
                    if (!WeworkLiteController.init_save_as_new_template(cnn, data, loginData, out error))
                    {
                        cnn.RollbackTransaction();
                        return JsonResultCommon.Custom(error);
                    }
                    if (!WeworkLiteController.init_status_group(cnn, data, loginData, out error))
                    {
                        cnn.RollbackTransaction();
                        return JsonResultCommon.Custom(error);
                    }
                    long sampleid = long.Parse(cnn.ExecuteScalar("select max(id_row) from we_sample_data where parentid is null").ToString());
                    long group_id = long.Parse(cnn.ExecuteScalar("select max(id_row) from we_status_group").ToString());
                    long iduser = loginData.UserID;
                    long idk = loginData.CustomerID;
                    Hashtable val = new Hashtable();
                    val.Add("title", data.title);
                    if (!string.IsNullOrEmpty(data.description))
                        val.Add("description", data.description);
                    else
                        val.Add("description", DBNull.Value);
                    val.Add("customerid", loginData.CustomerID);
                    val.Add("createddate", DateTime.Now);
                    val.Add("createdby", iduser);
                    val.Add("isdefault", 0);
                    val.Add("is_template_center", 1);
                    val.Add("types", data.types);
                    val.Add("levels", data.levels);
                    //val.Add("viewid", data.viewid);
                    val.Add("group_statusid", group_id);
                    val.Add("template_typeid", 1); // lấy mặc định trong we_template_types
                    if (!string.IsNullOrEmpty(data.img_temp))
                        val.Add("img_temp", data.img_temp);
                    else
                        val.Add("img_temp", DBNull.Value);
                    if (!string.IsNullOrEmpty(data.viewid))
                        val.Add("viewid", data.viewid);
                    else
                        val.Add("viewid", DBNull.Value);
                    if (!string.IsNullOrEmpty(data.field_id))
                        val.Add("field_id", data.field_id);
                    else
                        val.Add("field_id", DBNull.Value);
                    val.Add("share_with", data.share_with);
                    val.Add("sample_id", sampleid);
                    val.Add("save_as_id", data.save_as_id);
                    string strCheck = "select count(*) from we_template_customer where disabled=0 and (CustomerID=@customerid) and title=@name";
                    if (int.Parse(cnn.ExecuteScalar(strCheck, new SqlConditions() { { "customerid", data.customerid }, { "name", data.title } }).ToString()) > 0)
                    {
                        return JsonResultCommon.Custom("Mẫu đã tồn tại trong danh sách");
                    }
                    if (cnn.Insert(val, "we_template_customer") != 1)
                    {
                        cnn.RollbackTransaction();
                        return JsonResultCommon.Exception(_logger, cnn.LastError, _config, loginData, ControllerContext);
                    }
                    long idc = long.Parse(cnn.ExecuteScalar("select IDENT_CURRENT('we_template_customer')").ToString());
                    #region insert we_template_library
                    val = new Hashtable();
                    val.Add("id_template", idc);
                    val.Add("createddate", DateTime.Now);
                    val.Add("createdby", iduser);
                    val.Add("disabled", 0);
                    val.Add("id_user", loginData.UserID);
                    if (cnn.Insert(val, "we_template_library") != 1)
                    {
                        cnn.RollbackTransaction();
                        return JsonResultCommon.Exception(_logger, cnn.LastError, _config, loginData, ControllerContext);
                    }
                    #endregion
                    if (!WeworkLiteController.log(_logger, loginData.Username, cnn, 45, idc, iduser, data.title))
                    {
                        cnn.RollbackTransaction();
                        return JsonResultCommon.Exception(_logger, cnn.LastError, _config, loginData, ControllerContext);
                    }
                    data.id_row = idc;
                    cnn.EndTransaction();
                    return JsonResultCommon.ThanhCong(data);
                }
            }
            catch (Exception ex)
            {
                return JsonResultCommon.Exception(_logger, ex, _config, loginData);
            }
        }
        /// <summary>
        /// Cập nhật template center
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        [Route("update-template-center")]
        [HttpPost]
        public async Task<BaseModel<object>> update_template_center(TemplateCenterModel data)
        {
            string Token = Common.GetHeader(Request);
            UserJWT loginData = Ulities.GetUserByHeader(HttpContext.Request.Headers);
            if (loginData == null)
                return JsonResultCommon.DangNhap();
            try
            {
                Hashtable val = new Hashtable();
                SqlConditions sqlcond = new SqlConditions();
                long iduser = loginData.UserID;
                long idk = loginData.CustomerID;
                string strRe = "";
                if (string.IsNullOrEmpty(data.title))
                    strRe += (strRe == "" ? "" : ",") + " tên mẫu";
                if (strRe != "")
                    return JsonResultCommon.BatBuoc(strRe);
                string ConnectionString = WeworkLiteController.getConnectionString(ConnectionCache, loginData.CustomerID, _configuration);
                using (DpsConnection cnn = new DpsConnection(ConnectionString))
                {
                    string s = "";
                    if (data.id_row > 0)
                    {
                        sqlcond = new SqlConditions();
                        sqlcond.Add("id_row", data.id_row);
                        sqlcond.Add("disabled", 0);
                        sqlcond.Add("is_template_center", 1);
                        s = "select * from we_template_customer where (where)";
                        DataTable old = cnn.CreateDataTable(s, "(where)", sqlcond);
                        if (old == null || old.Rows.Count == 0)
                            return JsonResultCommon.KhongTonTai("Template");
                        val.Add("title", data.title);
                        if (!string.IsNullOrEmpty(data.description)) val.Add("description", data.description);
                        val.Add("field_id", data.field_id);
                        val.Add("share_with", data.share_with);
                        val.Add("updatedby", iduser);
                        val.Add("updateddate", DateTime.Now);
                        cnn.BeginTransaction();
                        if (cnn.Update(val, sqlcond, "we_template_customer") != 1)
                        {
                            cnn.RollbackTransaction();
                            return JsonResultCommon.Exception(_logger, cnn.LastError, _config, loginData, ControllerContext);
                        }
                        string ids = string.Join(",", data.list_share.Select(x => x.id_user));
                        // xóa tv k có trong danhsach => ktra tv có chưa > chưa thì insert
                        if (ids != "")//xóa thành viên
                        {
                            string strDel = "update we_template_library set disabled=1, updateddate=getdate(), updatedby=" + iduser + " where disabled=0 and id_template=" + data.id_row + " and id_user not in (" + ids + ")";
                            if (cnn.ExecuteNonQuery(strDel) < 0)
                            {
                                cnn.RollbackTransaction();
                                return JsonResultCommon.Exception(_logger, cnn.LastError, _config, loginData, ControllerContext);
                            }
                        }
                        Hashtable val1 = new Hashtable();
                        val1["id_template"] = data.id_row;
                        val1["createddate"] = DateTime.Now;
                        val1["createdby"] = iduser;
                        foreach (var owner in data.list_share)
                        {
                            if (owner.id_row == 0)
                            {
                                bool HasItem = long.Parse(cnn.ExecuteScalar($"select count(*) from we_template_library where disabled = 0 and id_template = {data.id_row} and id_user = {owner.id_user}").ToString()) > 0;
                                if (!HasItem)
                                {
                                    val1["id_user"] = owner.id_user;
                                    if (cnn.Insert(val1, "we_template_library") != 1)
                                    {
                                        cnn.RollbackTransaction();
                                        return JsonResultCommon.Exception(_logger, cnn.LastError, _config, loginData, ControllerContext);
                                    }
                                }
                            }
                        }
                        if (!WeworkLiteController.log(_logger, loginData.Username, cnn, 46, data.id_row, iduser))
                        {
                            cnn.RollbackTransaction();
                            return JsonResultCommon.Exception(_logger, cnn.LastError, _config, loginData, ControllerContext);
                        }
                    }
                    cnn.EndTransaction();
                    return JsonResultCommon.ThanhCong(data);
                }
            }
            catch (Exception ex)
            {
                return JsonResultCommon.Exception(_logger, ex, _config, loginData);
            }
        }
        /// <summary>
        /// add user vào template library
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        [Route("add-template-library")]
        [HttpPost]
        public async Task<BaseModel<object>> add_template_library(add_template_library_Model data)
        {
            string Token = Common.GetHeader(Request);
            UserJWT loginData = Ulities.GetUserByHeader(HttpContext.Request.Headers);
            if (loginData == null)
                return JsonResultCommon.DangNhap();
            try
            {
                Hashtable val = new Hashtable();
                SqlConditions sqlcond = new SqlConditions();
                long iduser = loginData.UserID;
                long idk = loginData.CustomerID;
                string strRe = "";
                if (data.templateid <= 0)
                    strRe += (strRe == "" ? "" : ",") + " mẫu";
                if (strRe != "")
                    return JsonResultCommon.KhongTonTai(strRe);
                string ConnectionString = WeworkLiteController.getConnectionString(ConnectionCache, loginData.CustomerID, _configuration);
                using (DpsConnection cnn = new DpsConnection(ConnectionString))
                {
                    string s = "";
                    if (data.templateid > 0)
                    {
                        string sql_insert = "";
                        sql_insert = $@"insert into we_template_customer (title, description, createdDate, createdby, disabled, isdefault, color, id_department, templateID, customerid)
                        select title, description, getdate(), " + loginData.UserID + ", 0, isdefault, color, 0, id_row, " + loginData.CustomerID + " as CustomerID from we_template_list where Disabled = 0 and id_row = " + data.templateid + "";
                        cnn.ExecuteNonQuery(sql_insert);
                        if (cnn.LastError != null)
                        {
                            cnn.RollbackTransaction();
                            return JsonResultCommon.Exception(_logger, cnn.LastError, _config, loginData, ControllerContext);
                        }
                        sqlcond = new SqlConditions();
                        sqlcond.Add("id_row", data.templateid);
                        sqlcond.Add("disabled", 0);
                        sqlcond.Add("is_template_center", 1);
                        s = "select * from we_template_customer where (where)";
                        DataTable old = cnn.CreateDataTable(s, "(where)", sqlcond);
                        if (old == null || old.Rows.Count == 0)
                            return JsonResultCommon.KhongTonTai("Template");
                        cnn.BeginTransaction();
                        string ids = string.Join(",", data.list_share.Select(x => x.id_user));
                        // xóa tv k có trong danhsach => ktra tv có chưa > chưa thì insert
                        if (ids != "")//xóa thành viên
                        {
                            string strDel = "update we_template_library set disabled=1, updateddate=getdate(), updatedby=" + iduser + " where disabled=0 and id_template=" + data.templateid + " and id_user not in (" + ids + ")";
                            if (cnn.ExecuteNonQuery(strDel) < 0)
                            {
                                cnn.RollbackTransaction();
                                return JsonResultCommon.Exception(_logger, cnn.LastError, _config, loginData, ControllerContext);
                            }
                        }
                        Hashtable val1 = new Hashtable();
                        val1["id_template"] = data.templateid;
                        val1["createddate"] = DateTime.Now;
                        val1["createdby"] = iduser;
                        foreach (var owner in data.list_share)
                        {
                            if (owner.id_row == 0)
                            {
                                bool HasItem = long.Parse(cnn.ExecuteScalar($"select count(*) from we_template_library where disabled = 0 and id_template = {data.templateid} and id_user = {owner.id_user}").ToString()) > 0;
                                if (!HasItem)
                                {
                                    val1["id_user"] = owner.id_user;
                                    if (cnn.Insert(val1, "we_template_library") != 1)
                                    {
                                        cnn.RollbackTransaction();
                                        return JsonResultCommon.Exception(_logger, cnn.LastError, _config, loginData, ControllerContext);
                                    }
                                }
                            }
                        }
                        if (!WeworkLiteController.log(_logger, loginData.Username, cnn, 46, data.templateid, iduser))
                        {
                            cnn.RollbackTransaction();
                            return JsonResultCommon.Exception(_logger, cnn.LastError, _config, loginData, ControllerContext);
                        }
                    }
                    cnn.EndTransaction();
                    return JsonResultCommon.ThanhCong(data);
                }
            }
            catch (Exception ex)
            {
                return JsonResultCommon.Exception(_logger, ex, _config, loginData);
            }
        }
        private DataTable dtChildren(string array_status, DpsConnection cnn)
        {
            DataTable result = new DataTable();
            string sqlq = "select id_row, statusname, isdefault, color, position, isfinal, isdeadline, istodo " +
                "from we_status_list " +
                "where Disabled = 0 and isdefault = 1 and id_row in (" + array_status + ")" +
                "order by position, statusname";
            result = cnn.CreateDataTable(sqlq);
            if (result.Rows.Count > 0)

                return result;
            else
                return new DataTable();
        }
        #endregion
    }
}