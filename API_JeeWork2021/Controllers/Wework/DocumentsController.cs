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

namespace JeeWork_Core2021.Controllers.Wework
{
    [ApiController]
    [Route("api/documents")]
    [EnableCors("JeeWorkPolicy")]
    /// <summary>
    /// api quản lý we_authorize
    /// </summary>
    public class DocumentsController : ControllerBase
    {
        private readonly IHostingEnvironment _hostingEnvironment;
        private JeeWorkConfig _config;
        public List<AccUsernameModel> DataAccount;
        private IConnectionCache ConnectionCache;
        private IConfiguration _configuration;
        public DocumentsController(IOptions<JeeWorkConfig> config, IHostingEnvironment hostingEnvironment, IConnectionCache _cache, IConfiguration configuration)
        {
            ConnectionCache = _cache;
            _hostingEnvironment = hostingEnvironment;
            _config = config.Value;
            _configuration = configuration;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="query"></param>
        /// <returns></returns>
        [CusAuthorize(Roles = "3610")]
        [Route("List")]
        [HttpGet]
        public object List([FromQuery] QueryParams query)
        {
            UserJWT loginData = Ulities.GetUserByHeader(HttpContext.Request.Headers);
            if (loginData == null)
                return JsonResultCommon.DangNhap();
            if (query == null)
                query = new QueryParams();
            PageModel pageModel = new PageModel();
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
                string domain = _configuration.GetValue<string>("Host:JeeWork_API");
                string ConnectionString = WeworkLiteController.getConnectionString(ConnectionCache, loginData.CustomerID, _configuration);
                bool Visible = Common.CheckRoleByToken(loginData.UserID.ToString(), "3610", ConnectionString, DataAccount);
                using (DpsConnection cnn = new DpsConnection(ConnectionString))
                {
                    SqlConditions Conds = new SqlConditions();
                    string dieukienSort = "title", dieukien_where = " ";
                    if (!string.IsNullOrEmpty(query.filter["id_project_team"]))
                    {
                        dieukien_where += " and project.id_row=@id_project_team";
                        Conds.Add("id_project_team", query.filter["id_project_team"]);
                    }
                    if (!string.IsNullOrEmpty(query.filter["keyword"]))
                    {
                        dieukien_where += " and (filename like N'%@keyword%' or nv_tao.holot+' '+nv_tao.ten like '%@keyword%' or nv_sua.holot+' '+nv_sua.ten like '%@keyword%'')";
                        dieukien_where = dieukien_where.Replace("@keyword", query.filter["keyword"]);
                    }
                    #region Sort data theo các dữ liệu bên dưới
                    Dictionary<string, string> sortableFields = new Dictionary<string, string>
                        {
                            { "filename", "filename"},
                            { "CreatedBy", "hoten_nguoitao"},
                            { "CreatedDate", "CreatedDate"},
                            { "UpdatedBy", "hoten_nguoisua"},
                            {"UpdatedDate","UpdatedDate" }
                        };
                    #endregion
                    if (!string.IsNullOrEmpty(query.sortField) && sortableFields.ContainsKey(query.sortField))
                        dieukienSort = sortableFields[query.sortField] + ("desc".Equals(query.sortOrder) ? " desc" : " asc");
                    #region Trả dữ liệu về backend để hiển thị lên giao diện
                    string sqlq = "";
                    sqlq = @$"select att.id_row, att.object_type, att.object_id, att.path,
filename, att.type, att.size, att.CreatedDate, att.CreatedBy, att.UpdatedDate,
att.UpdatedBy, '' as username_tao, '' as username_sua,
'' as hoten_nguoitao,'' as hoten_nguoisua 
from we_attachment att join we_project_team project on att.object_id= project.id_row and object_type = 4
where att.disabled=0 and object_type=4 and att.CreatedBy in ({listID}) ";

                    DataSet ds = cnn.CreateDataSet(sqlq + dieukien_where, Conds);
                    if (cnn.LastError != null || ds == null)
                        return JsonResultCommon.Exception(cnn.LastError, _config, loginData.CustomerID, ControllerContext);
                    DataTable dt = ds.Tables[0];
                    if (dt.Rows.Count == 0)
                        return JsonResultCommon.ThanhCong(new List<string>(), pageModel, Visible);

                    #region Map info account từ JeeAccount

                    foreach (DataRow item in dt.Rows)
                    {
                        var infoNguoitao = DataAccount.Where(x => item["createdby"].ToString().Contains(x.UserId.ToString())).FirstOrDefault();
                        var infoNguoiSua = DataAccount.Where(x => item["UpdatedBy"].ToString().Contains(x.UserId.ToString())).FirstOrDefault();

                        if (infoNguoitao != null)
                        {
                            item["username_tao"] = infoNguoitao.Username;
                            item["hoten_nguoitao"] = infoNguoitao.FullName;
                        }
                        if (infoNguoiSua != null)
                        {

                            item["username_sua"] = infoNguoiSua.Username;
                            item["hoten_nguoisua"] = infoNguoiSua.FullName;
                        }
                    }
                    #endregion
                    var temp = dt.AsEnumerable();
                    dt = temp.CopyToDataTable();
                    int total = dt.Rows.Count;
                    if (query.more)
                    {
                        query.page = 1;
                        query.record = total;
                    }

                    pageModel.TotalCount = total;
                    pageModel.AllPage = (int)Math.Ceiling(total / (decimal)query.record);
                    pageModel.Size = query.record;
                    pageModel.Page = query.page;
                    // Phân trang
                    dt = dt.AsEnumerable().Skip((query.page - 1) * query.record).Take(query.record).CopyToDataTable();
                    var data = from dr in dt.AsEnumerable()
                               select new
                               {
                                   id_row = dr["id_row"],
                                   path = WeworkLiteController.genLinkAttachment(domain, dr["path"]),
                                   filename = dr["filename"],
                                   type = dr["type"],
                                   isImage = UploadHelper.IsImage(dr["type"].ToString()),
                                   icon = UploadHelper.GetIcon(dr["type"].ToString()),
                                   size = dr["size"],
                                   NguoiTao = dr["hoten_nguoitao"],
                                   CreatedBy = dr["CreatedBy"],
                                   CreatedDate = string.Format("{0:dd/MM/yyyy HH:mm}", dr["CreatedDate"])
                               };
                    return JsonResultCommon.ThanhCong(data, pageModel, Visible);
                }
                #endregion
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
        [Route("Insert")]
        [HttpPost]
        public async Task<object> Insert(AttachmentModel data)
        {
            UserJWT loginData = Ulities.GetUserByHeader(HttpContext.Request.Headers);
            if (loginData == null)
                return JsonResultCommon.DangNhap();
            try
            {
                string sqlq = "";
                string ConnectionString = WeworkLiteController.getConnectionString(ConnectionCache, loginData.CustomerID, _configuration);
                using (DpsConnection cnn = new DpsConnection(ConnectionString))
                {
                    long iduser = loginData.UserID;
                    long idk = loginData.CustomerID;
                    Hashtable val = new Hashtable();
                    cnn.BeginTransaction();

                    if (data != null)
                    {
                        var temp = new AttachmentModel()
                        {
                            item = data.item,
                            object_type = 4,
                            object_id = data.object_id,
                            id_user = loginData.UserID
                        };
                        switch (data.object_type)
                        {
                            case 1: sqlq = "select ISNULL((select count(*) from we_work where Disabled=0 and id_row = " + data.object_id + "),0)"; break;
                            case 2: sqlq = "select ISNULL((select count(*) from we_topic where Disabled=0 and id_row = " + data.object_id + "),0)"; break;
                            case 3: sqlq = "select ISNULL((select count(*) from we_comment where Disabled=0 and id_row = " + data.object_id + "),0)"; break;
                            case 11: sqlq = "select ISNULL((select count(*) from we_work where Disabled=0 and id_row = " + data.object_id + "),0)"; break;
                            case 4: sqlq = "select ISNULL((select count(*) from we_project_team where Disabled=0 and id_row = " + data.object_id + "),0)"; break;

                            default: break;
                        }
                        if (long.Parse(cnn.ExecuteScalar(sqlq).ToString()) != 1)
                        {
                            switch (data.object_type)
                            {
                                case 1:
                                    return JsonResultCommon.KhongTonTai("work");
                                case 2: return JsonResultCommon.KhongTonTai("Topic");
                                case 3: return JsonResultCommon.KhongTonTai("comment");
                                case 11: return JsonResultCommon.KhongTonTai("work");
                                case 4:
                                    return JsonResultCommon.KhongTonTai("project");
                                default: break;
                            }
                        }
                        if (!AttachmentController.upload(temp, cnn, _hostingEnvironment.ContentRootPath))
                        {
                            cnn.RollbackTransaction();
                            return JsonResultCommon.Exception(cnn.LastError, _config, loginData.CustomerID, ControllerContext);
                        }
                    }
                    long idc = long.Parse(cnn.ExecuteScalar("select IDENT_CURRENT('we_attachment')").ToString());
                    if (!WeworkLiteController.log(cnn, 21, idc, iduser, data.item.filename))
                    {
                        cnn.RollbackTransaction();
                        return JsonResultCommon.Exception(cnn.LastError, _config, loginData.CustomerID, ControllerContext);
                    }
                    //data.id_row = idc;
                    //cnn.EndTransaction();

                    //if (data.email)
                    //WeworkLiteController.mailthongbao(idc, data.Users.Select(x => x.id_user).ToList(), 16, loginData);
                    //data.id_row = idc;
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