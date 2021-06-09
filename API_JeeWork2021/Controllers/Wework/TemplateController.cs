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

namespace JeeWork_Core2021.Controllers.Wework
{
    [ApiController]
    [Route("api/template")]
    [EnableCors("JeeWorkPolicy")]
    /// <summary>
    /// api quản lý we_authorize
    /// </summary>
    [CusAuthorize(Roles = "3610")]
    public class TemplateController : ControllerBase
    {
        private readonly IHostingEnvironment _hostingEnvironment;
        private JeeWorkConfig _config;
        private IConnectionCache ConnectionCache;

        public TemplateController(IOptions<JeeWorkConfig> config, IHostingEnvironment hostingEnvironment, IConnectionCache _cache)
        {
            _hostingEnvironment = hostingEnvironment;
            _config = config.Value;
            ConnectionCache = _cache;
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
                using (DpsConnection cnn = new DpsConnection(ConnectionCache.GetConnectionString(loginData.CustomerID)))
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
                        return JsonResultCommon.Exception(cnn.LastError, _config, loginData.CustomerID, ControllerContext);
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
                                return JsonResultCommon.Exception(cnn.LastError, _config, loginData.CustomerID, ControllerContext);
                            }
                        }
                    }
                    if (!WeworkLiteController.log(cnn, 45, idc, iduser, data.title))
                    {
                        cnn.RollbackTransaction();
                        return JsonResultCommon.Exception(cnn.LastError, _config, loginData.CustomerID, ControllerContext);
                    }
                    data.id_row = idc;
                    cnn.EndTransaction();
                    return JsonResultCommon.ThanhCong(data);
                }
            }
            catch (Exception ex)
            {
                return JsonResultCommon.Exception(ex, _config, loginData.CustomerID);
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
                using (DpsConnection cnn = new DpsConnection(ConnectionCache.GetConnectionString(loginData.CustomerID)))
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
                        return JsonResultCommon.Exception(cnn.LastError, _config, loginData.CustomerID, ControllerContext);
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
                                    return JsonResultCommon.Exception(cnn.LastError, _config, loginData.CustomerID, ControllerContext);
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
                                    return JsonResultCommon.Exception(cnn.LastError, _config, loginData.CustomerID, ControllerContext);
                                }
                            }
                        }
                    }
                    DataTable dt = cnn.CreateDataTable(s, "(where)", sqlcond);
                    if (!WeworkLiteController.log(cnn, 45, data.id_row, iduser))
                    {
                        cnn.RollbackTransaction();
                        return JsonResultCommon.Exception(cnn.LastError, _config, loginData.CustomerID, ControllerContext);
                    }
                    cnn.EndTransaction();
                    return JsonResultCommon.ThanhCong(data);
                }
            }
            catch (Exception ex)
            {
                return JsonResultCommon.Exception(ex, _config, loginData.CustomerID);
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
                using (DpsConnection cnn = new DpsConnection(ConnectionCache.GetConnectionString(loginData.CustomerID)))
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
                        return JsonResultCommon.Exception(cnn.LastError, _config, loginData.CustomerID, ControllerContext);
                    }
                    if (!isDelStatus)
                    {
                        //sqlq = "update we_template_status set disabled = 1, UpdatedDate=getdate(), UpdatedBy=" + iduser + " where TemplateID = " + id;
                        cnn.BeginTransaction();
                        if (cnn.ExecuteNonQuery(sqlq) != 1)
                        {
                            cnn.RollbackTransaction();
                            return JsonResultCommon.Exception(cnn.LastError, _config, loginData.CustomerID, ControllerContext);
                        }
                    }
                    if (!WeworkLiteController.log(cnn, 47, id, iduser))
                    {
                        cnn.RollbackTransaction();
                        return JsonResultCommon.Exception(cnn.LastError, _config, loginData.CustomerID, ControllerContext);
                    }
                    cnn.EndTransaction();
                    return JsonResultCommon.ThanhCong();
                }
            }
            catch (Exception ex)
            {
                return JsonResultCommon.Exception(ex, _config, loginData.CustomerID);
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
                using (DpsConnection cnn = new DpsConnection(ConnectionCache.GetConnectionString(loginData.CustomerID)))
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
                            return JsonResultCommon.Exception(cnn.LastError, _config, loginData.CustomerID, ControllerContext);
                        }

                        if (!data.istemplate)
                        {
                            string updatenew = "update we_status set  UpdatedDate=getdate(), UpdatedBy=" + iduser + ", "+data.columname+ " = N'"+ data.values+ "' where StatusID_Reference = " + data.id_row;
                            if (cnn.ExecuteNonQuery(updatenew) < 0)
                            {
                                cnn.RollbackTransaction();
                                return JsonResultCommon.Exception(cnn.LastError, _config, loginData.CustomerID, ControllerContext);
                            }
                        }

                        if (!WeworkLiteController.log(cnn, 46, data.id_row, iduser))
                        {
                            cnn.RollbackTransaction();
                            return JsonResultCommon.Exception(cnn.LastError, _config, loginData.CustomerID, ControllerContext);
                        }
                    }
                    else
                    {
                        val.Add("CreatedDate", DateTime.Now);
                        val.Add("CreatedBy", iduser);
                        if (cnn.Insert(val, tablename) != 1)
                        {
                            cnn.RollbackTransaction();
                            return JsonResultCommon.Exception(cnn.LastError, _config, loginData.CustomerID, ControllerContext);
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
                        if (!WeworkLiteController.log(cnn, 45, data.id_row, iduser))
                        {
                            cnn.RollbackTransaction();
                            return JsonResultCommon.Exception(cnn.LastError, _config, loginData.CustomerID, ControllerContext);
                        }
                    }
                    cnn.EndTransaction();
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