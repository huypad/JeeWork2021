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
using DPSinfra.Notifier;

namespace JeeWork_Core2021.Controllers.Wework
{
    [ApiController]
    [Route("api/topic")]
    [EnableCors("JeeWorkPolicy")]
    /// <summary>
    /// api quản lý we_authorize
    /// </summary>
    [CusAuthorize(Roles = "3610")]
    public class TopicController : ControllerBase
    {
        private readonly IHostingEnvironment _hostingEnvironment;
        private JeeWorkConfig _config;
        public List<AccUsernameModel> DataAccount;
        private IConnectionCache ConnectionCache;
        private IConfiguration _configuration;
        private INotifier _notifier;
        public TopicController(IOptions<JeeWorkConfig> config, IHostingEnvironment hostingEnvironment, IConnectionCache _cache, IConfiguration configuration, INotifier notifier)
        {
            ConnectionCache = _cache;
            _hostingEnvironment = hostingEnvironment;
            _config = config.Value;
            _configuration = configuration;
            _notifier = notifier;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="query"></param>
        /// <returns></returns>
        [Route("List")]
        [HttpGet]
        public object List([FromQuery] QueryParams query)
        {
            string Token = Common.GetHeader(Request);
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

                string ConnectionString = WeworkLiteController.getConnectionString(ConnectionCache, loginData.CustomerID, _configuration);
                bool Visible = Common.CheckRoleByToken(Token, "3610", ConnectionString, DataAccount);
                string domain = _configuration.GetValue<string>("Host:JeeWork_API");
                using (DpsConnection cnn = new DpsConnection(ConnectionString))
                {

                    SqlConditions Conds = new SqlConditions();
                    string dieukienSort = "title", dieukien_where = " ";
                    if (!string.IsNullOrEmpty(query.filter["id_project_team"]))
                    {
                        dieukien_where += " and id_project_team=@id_project_team";
                        Conds.Add("id_project_team", query.filter["id_project_team"]);
                    }
                    if (!string.IsNullOrEmpty(query.filter["keyword"]))
                    {
                        dieukien_where += " and (t.title like N'%@keyword%' )";
                        dieukien_where = dieukien_where.Replace("@keyword", query.filter["keyword"]);
                    }
                    #region Sort data theo các dữ liệu bên dưới
                    Dictionary<string, string> sortableFields = new Dictionary<string, string>
                        {
                            { "title", "title"},
                            { "CreatedBy", "NguoiTao"},
                            { "CreatedDate", "CreatedDate"},
                            { "UpdatedBy", "NguoiSua"},
                            {"UpdatedDate","UpdatedDate" }
                        };
                    #endregion
                    if (!string.IsNullOrEmpty(query.sortField) && sortableFields.ContainsKey(query.sortField))
                        dieukienSort = sortableFields[query.sortField] + ("desc".Equals(query.sortOrder) ? " desc" : " asc");
                    #region Trả dữ liệu về backend để hiển thị lên giao diện
                    string sqlq = @$"select distinct t.*, p.title as project_team, t.CreatedBy as Id_NV, '' as hoten,'' as mobile, '' as username, '' as Email, '' as image,'' as Tenchucdanh,'' as NguoiTao, '' as NguoiSua,c.tong, coalesce (u.favourite,0) as favourite from we_topic t
                                join we_project_team p on t.id_project_team=p.id_row
                                join we_department d on d.id_row=p.id_department
                                left join ( select count(*) tong,object_id from we_comment where object_type=2 and Disabled=0 group by object_id) c on c.object_id=t.id_row
                                join we_topic_user u on u.Disabled=0 and u.id_topic=t.id_row and u.id_user=" + loginData.UserID + $" where t.Disabled=0 and d.Disabled = 0 and t.CreatedBy in ({listID}) "
                                + dieukien_where + "  order by " + dieukienSort;
                                                    sqlq += $" select u.id_user as Id_NV, '' as hoten,'' as mobile, '' as username, '' as Email, '' as image,'' as Tenchucdanh, id_topic from we_topic_user u where u.Disabled=0 and u.id_user in ({listID})";
                    DataSet ds = cnn.CreateDataSet(sqlq, Conds);
                    if (cnn.LastError != null || ds == null)
                        return JsonResultCommon.Exception(cnn.LastError, _config, loginData.CustomerID,ControllerContext);
                    DataTable dt = ds.Tables[0];
                    if (dt.Rows.Count == 0)
                        return JsonResultCommon.ThanhCong(new List<string>(), pageModel, Visible);

                    #region Map info account từ JeeAccount

                    foreach (DataRow item in dt.Rows)
                    {
                        var info = DataAccount.Where(x => item["Id_NV"].ToString().Contains(x.UserId.ToString())).FirstOrDefault();
                        var infoNguoiSua = DataAccount.Where(x => item["UpdatedBy"].ToString().Contains(x.UserId.ToString())).FirstOrDefault();

                        if (info != null)
                        {
                            item["hoten"] = info.FullName;
                            item["mobile"] = info.PhoneNumber;
                            item["username"] = info.Username;
                            item["image"] = info.AvartarImgURL;
                            item["Tenchucdanh"] = info.Jobtitle;
                        }
                        if (infoNguoiSua != null)
                        {
                            item["NguoiSua"] = infoNguoiSua.Username;
                        }
                    }

                    foreach (DataRow item in ds.Tables[1].Rows)
                    {
                        var info = DataAccount.Where(x => item["Id_NV"].ToString().Contains(x.UserId.ToString())).FirstOrDefault();

                        if (info != null)
                        {
                            item["hoten"] = info.FullName;
                            item["mobile"] = info.PhoneNumber;
                            item["username"] = info.Username;
                            item["image"] = info.AvartarImgURL;
                            item["Tenchucdanh"] = info.Jobtitle;
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
                    var data = from r in dt.AsEnumerable()
                               select new
                               {
                                   id_row = r["id_row"],
                                   title = r["title"],
                                   description = r["description"],
                                   id_project_team = r["id_project_team"],
                                   project_team = r["project_team"],
                                   email = r["email"],
                                   favourite = r["favourite"],
                                   CreatedDate = string.Format("{0:dd/MM/yyyy HH:mm}", r["CreatedDate"]),
                                   CreatedBy = r["CreatedBy"],
                                   NguoiTao = new
                                   {
                                       id_nv = r["id_nv"],
                                       hoten = r["hoten"],
                                       username = r["username"],
                                       mobile = r["mobile"],
                                       image = r["image"],
                                   },
                                   UpdatedDate = r["UpdatedDate"] == DBNull.Value ? "" : string.Format("{0:dd/MM/yyyy HH:mm}", r["UpdatedDate"]),
                                   UpdatedBy = r["UpdatedBy"],
                                   NguoiSua = r["NguoiSua"],
                                   Follower = from dr in ds.Tables[1].AsEnumerable()
                                              where dr["id_topic"].Equals(r["id_row"])
                                              select new
                                              {
                                                  id_nv = dr["id_nv"],
                                                  hoten = dr["hoten"],
                                                  username = dr["username"],
                                                  mobile = dr["mobile"],
                                                  image = r["image"],
                                              }
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
                string domain = _configuration.GetValue<string>("Host:JeeWork_API");
                string ConnectionString = WeworkLiteController.getConnectionString(ConnectionCache, loginData.CustomerID, _configuration);
                using (DpsConnection cnn = new DpsConnection(ConnectionString))
                {
                    #region Lấy dữ liệu account từ JeeAccount
                    DataAccount = WeworkLiteController.GetAccountFromJeeAccount(HttpContext.Request.Headers, _configuration);
                    if (DataAccount == null)
                        return JsonResultCommon.Custom("Lỗi lấy danh sách nhân viên từ hệ thống quản lý tài khoản");

                    //List<string> nvs = DataAccount.Select(x => x.UserId.ToString()).ToList();
                    //string ids = string.Join(",", nvs);
                    string error = "";
                    string listID = WeworkLiteController.ListAccount(HttpContext.Request.Headers, out error, _configuration);
                    if (error != "")
                        return JsonResultCommon.Custom(error);
                    #endregion

                    #region Trả dữ liệu về backend để hiển thị lên giao diện
                    string sqlq = @$"select t.*, p.title as project_team, t.CreatedBy as Id_NV, '' as hoten,'' as mobile, '' as username, '' as Email, '' as image,'' as Tenchucdanh,'' as NguoiTao, '' as NguoiSua ,c.tong, coalesce (u.favourite,0) as favourite from we_topic t
join we_project_team p on t.id_project_team=p.id_row
left join ( select count(*) tong,object_id from we_comment where object_type=2 and Disabled=0 group by object_id) c on c.object_id=t.id_row
left join we_topic_user u on u.Disabled=0 and u.id_topic=t.id_row and u.id_user=" + loginData.UserID + $" where t.Disabled=0 and t.CreatedBy in ({listID}) and t.id_row=" + id;
                    sqlq += @$";select u.id_user as Id_NV, '' as hoten,'' as mobile, '' as username, '' as Email, '' as image,'' as Tenchucdanh, u.id_row from we_topic_user u where u.Disabled=0 and u.id_user in ({listID}) and id_topic = " + id;
                    sqlq += $";select att.*, '' as username from we_attachment att where disabled=0 and object_type=2 and att.createdby in ({listID}) and object_id=" + id;
                    DataSet ds = cnn.CreateDataSet(sqlq);
                    if (cnn.LastError != null || ds == null)
                    {
                        return JsonResultCommon.Exception(cnn.LastError, _config, loginData.CustomerID,ControllerContext);
                    }
                    if (ds.Tables[0] == null || ds.Tables[0].Rows.Count == 0)
                        return JsonResultCommon.KhongTonTai();

                    #region Map info account từ JeeAccount

                    foreach (DataRow item in ds.Tables[0].Rows)
                    {
                        var info = DataAccount.Where(x => item["Id_NV"].ToString().Contains(x.UserId.ToString())).FirstOrDefault();
                        var infoNguoiSua = DataAccount.Where(x => item["UpdatedBy"].ToString().Contains(x.UserId.ToString())).FirstOrDefault();

                        if (info != null)
                        {
                            item["hoten"] = info.FullName;
                            item["mobile"] = info.PhoneNumber;
                            item["username"] = info.Username;
                            item["image"] = info.AvartarImgURL;
                            item["Tenchucdanh"] = info.Jobtitle;
                        }
                        if (infoNguoiSua != null)
                        {
                            item["NguoiSua"] = infoNguoiSua.Username;
                        }
                    }
                    foreach (DataRow item in ds.Tables[1].Rows)
                    {
                        var info = DataAccount.Where(x => item["Id_NV"].ToString().Contains(x.UserId.ToString())).FirstOrDefault();

                        if (info != null)
                        {
                            item["hoten"] = info.FullName;
                            item["mobile"] = info.PhoneNumber;
                            item["username"] = info.Username;
                            item["image"] = info.AvartarImgURL;
                            item["Tenchucdanh"] = info.Jobtitle;
                        }
                    }
                    foreach (DataRow item in ds.Tables[2].Rows)
                    {
                        var info = DataAccount.Where(x => item["createdby"].ToString().Contains(x.UserId.ToString())).FirstOrDefault();

                        if (info != null)
                        {
                            item["username"] = info.Username;
                        }
                    }
                    #endregion
                    bool Followed = false;
                    var data = (from r in ds.Tables[0].AsEnumerable()
                                select new
                                {
                                    id_row = r["id_row"],
                                    title = r["title"],
                                    description = r["description"],
                                    id_project_team = r["id_project_team"],
                                    project_team = r["project_team"],
                                    email = r["email"],
                                    favourite = r["favourite"],
                                    Followed = (from dr in ds.Tables[1].AsEnumerable()
                                                where dr["id_nv"].ToString() == loginData.UserID.ToString()
                                                select dr).Count() > 0,//user hiện tại có follow hay chưa
                                    CreatedDate = string.Format("{0:dd/MM/yyyy HH:mm}", r["CreatedDate"]),
                                    CreatedBy = r["CreatedBy"],
                                    NguoiTao = new
                                    {
                                        id_nv = r["id_nv"],
                                        hoten = r["hoten"],
                                        username = r["username"],
                                        mobile = r["mobile"],
                                        image = r["image"],
                                    },
                                    UpdatedDate = r["UpdatedDate"] == DBNull.Value ? "" : string.Format("{0:dd/MM/yyyy HH:mm}", r["UpdatedDate"]),
                                    UpdatedBy = r["UpdatedBy"],
                                    NguoiSua = r["NguoiSua"],
                                    Follower = from dr in ds.Tables[1].AsEnumerable()
                                               select new
                                               {
                                                   id_row = dr["id_row"],
                                                   id_nv = dr["id_nv"],
                                                   hoten = dr["hoten"],
                                                   username = dr["username"],
                                                   mobile = dr["mobile"],
                                                   image = dr["image"],
                                               },
                                    Attachment = from dr in ds.Tables[2].AsEnumerable()
                                                 select new
                                                 {
                                                     id_row = dr["id_row"],
                                                     path = WeworkLiteController.genLinkAttachment(domain, dr["path"]),
                                                     filename = dr["filename"],
                                                     type = dr["type"],
                                                     isImage = UploadHelper.IsImage(dr["type"].ToString()),
                                                     icon = UploadHelper.GetIcon(dr["type"].ToString()),
                                                     size = dr["size"],
                                                     NguoiTao = dr["username"],
                                                     CreatedBy = dr["CreatedBy"],
                                                     CreatedDate = string.Format("{0:dd/MM/yyyy HH:mm}", dr["CreatedDate"])
                                                 },
                                }).FirstOrDefault();
                    return JsonResultCommon.ThanhCong(data);
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
        public async Task<object> Insert(TopicModel data)
        {
            string Token = Common.GetHeader(Request);
            UserJWT loginData = Ulities.GetUserByHeader(HttpContext.Request.Headers);
            if (loginData == null)
                return JsonResultCommon.DangNhap();
            try
            {
                string strRe = "";
                if (string.IsNullOrEmpty(data.title))
                    strRe += (strRe == "" ? "" : ",") + "tên topic";
                if (string.IsNullOrEmpty(data.description))
                    strRe += (strRe == "" ? "" : ",") + "nội dung topic";
                if (data.id_project_team <= 0)
                    strRe += (strRe == "" ? "" : ",") + "dự án/phòng ban";
                if (strRe != "")
                    return JsonResultCommon.BatBuoc(strRe);

                string ConnectionString = WeworkLiteController.getConnectionString(ConnectionCache, loginData.CustomerID, _configuration);
                using (DpsConnection cnn = new DpsConnection(ConnectionString))
                {
                    long iduser = loginData.UserID;
                    long idk = loginData.CustomerID;
                    Hashtable val = new Hashtable();
                    val.Add("title", data.title);
                    val.Add("description", data.description);
                    val.Add("id_project_team", data.id_project_team);
                    val.Add("email", data.email);
                    val.Add("CreatedDate", DateTime.Now);
                    val.Add("CreatedBy", iduser);
                    string strCheck = "select count(*) from we_topic where Disabled=0 and  (id_project_team=@id_project_team) and title=@name";
                    if (int.Parse(cnn.ExecuteScalar(strCheck, new SqlConditions() { { "id_project_team", data.id_project_team }, { "name", data.title } }).ToString()) > 0)
                    {
                        return JsonResultCommon.Custom("Topic đã tồn tại trong dự án/phòng ban");
                    }
                    cnn.BeginTransaction();
                    if (cnn.Insert(val, "we_topic") != 1)
                    {
                        cnn.RollbackTransaction();
                        return JsonResultCommon.Exception(cnn.LastError, _config, loginData.CustomerID,ControllerContext);
                    }
                    long idc = long.Parse(cnn.ExecuteScalar("select IDENT_CURRENT('we_topic')").ToString());
                    if (data.Attachments != null)
                    {
                        foreach (var item in data.Attachments)
                        {
                            var temp = new AttachmentModel()
                            {
                                item = item,
                                object_type = 2,
                                object_id = idc,
                                id_user = loginData.UserID
                            };
                            if (!AttachmentController.upload(temp, cnn, _hostingEnvironment.ContentRootPath))
                            {
                                cnn.RollbackTransaction();
                                return JsonResultCommon.Exception(cnn.LastError, _config, loginData.CustomerID,ControllerContext);
                            }
                        }
                    }
                    if (data.Users != null)
                    {
                        Hashtable val1 = new Hashtable();
                        val1["id_topic"] = idc;
                        val1["CreatedDate"] = DateTime.Now;
                        val1["CreatedBy"] = iduser;
                        foreach (var u in data.Users)
                        {
                            val1["id_user"] = u.id_user;
                            if (cnn.Insert(val1, "we_topic_user") != 1)
                            {
                                cnn.RollbackTransaction();
                                return JsonResultCommon.Exception(cnn.LastError, _config, loginData.CustomerID,ControllerContext);
                            }
                        }
                    }
                    //string LogContent = "", LogEditContent = "";
                    //LogContent = LogEditContent = "Thêm mới dữ liệu topic: title=" + data.title + ", id_project_team=" + data.id_project_team + ", description=" + data.description + ", email=" + data.email;
                    //DpsPage.Ghilogfile(loginData.CustomerID.ToString(), LogEditContent, LogContent, loginData.UserName);
                    if (!WeworkLiteController.log(cnn, 21, idc, iduser, data.title))
                    {
                        cnn.RollbackTransaction();
                        return JsonResultCommon.Exception(cnn.LastError, _config, loginData.CustomerID,ControllerContext);
                    }
                    data.id_row = idc;
                    cnn.EndTransaction();

                    if (data.email)
                    WeworkLiteController.mailthongbao(idc, data.Users.Select(x => x.id_user).ToList(), 16, loginData, ConnectionString, _notifier);
                    data.id_row = idc;
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
        public async Task<BaseModel<object>> Update(TopicModel data)
        {
            string Token = Common.GetHeader(Request);
            UserJWT loginData = Ulities.GetUserByHeader(HttpContext.Request.Headers);
            if (loginData == null)
                return JsonResultCommon.DangNhap();
            try
            {
                string strRe = "";
                if (string.IsNullOrEmpty(data.title))
                    strRe += (strRe == "" ? "" : ",") + "tên topic";
                if (string.IsNullOrEmpty(data.title))
                    strRe += (strRe == "" ? "" : ",") + "nội dung topic";
                if (strRe != "")
                    return JsonResultCommon.BatBuoc(strRe);
                string ConnectionString = WeworkLiteController.getConnectionString(ConnectionCache, loginData.CustomerID, _configuration);
                using (DpsConnection cnn = new DpsConnection(ConnectionString))
                {
                    SqlConditions sqlcond = new SqlConditions();
                    sqlcond.Add("id_row", data.id_row);
                    sqlcond.Add("disabled", 0);
                    string s = "select * from we_topic where (where)";
                    DataTable old = cnn.CreateDataTable(s, "(where)", sqlcond);
                    if (old == null || old.Rows.Count == 0)
                        return JsonResultCommon.KhongTonTai("Topic");
                    long iduser = loginData.UserID;
                    long idk = loginData.CustomerID;
                    Hashtable val = new Hashtable();
                    val.Add("title", data.title);
                    val.Add("description", data.description);
                    val.Add("email", data.email);
                    val.Add("UpdatedDate", DateTime.Now);
                    val.Add("UpdatedBy", iduser);
                    string strCheck = "select count(*) from we_topic where Disabled=0 and  (id_project_team=@id_project_team) and title=@name and id_row<>@id";
                    if (int.Parse(cnn.ExecuteScalar(strCheck, new SqlConditions() { { "id_project_team", data.id_project_team }, { "name", data.title }, { "id", data.id_row } }).ToString()) > 0)
                    {
                        return JsonResultCommon.Custom("Topic đã tồn tại trong dự án/phòng ban");
                    }
                    cnn.BeginTransaction();
                    if (cnn.Update(val, sqlcond, "we_topic") != 1)
                    {
                        cnn.RollbackTransaction();
                        return JsonResultCommon.Exception(cnn.LastError, _config, loginData.CustomerID,ControllerContext);
                    }
                    if (data.Attachments != null)
                    {
                        foreach (var item in data.Attachments)
                        {
                            var temp = new AttachmentModel()
                            {
                                item = item,
                                object_type = 2,
                                object_id = data.id_row,
                                id_user = loginData.UserID
                            };
                            if (!AttachmentController.upload(temp, cnn, _hostingEnvironment.ContentRootPath))
                            {
                                cnn.RollbackTransaction();
                                return JsonResultCommon.Exception(cnn.LastError, _config, loginData.CustomerID,ControllerContext);
                            }
                        }
                    }

                    string ids = string.Join(",", data.Users.Where(x => x.id_row > 0).Select(x => x.id_row));
                    if (ids != "")//xóa follower
                    {
                        string strDel = "Update we_topic_user set Disabled=1, UpdatedDate=getdate(), UpdatedBy=" + iduser + " where Disabled=0 and  id_topic=" + data.id_row + " and id_row not in (" + ids + ")";
                        if (cnn.ExecuteNonQuery(strDel) < 0)
                        {
                            cnn.RollbackTransaction();
                            return JsonResultCommon.Exception(cnn.LastError, _config, loginData.CustomerID,ControllerContext);
                        }
                    }
                    Hashtable val1 = new Hashtable();
                    val1["id_topic"] = data.id_row;
                    val1["CreatedDate"] = DateTime.Now;
                    val1["CreatedBy"] = iduser;
                    foreach (var owner in data.Users)
                    {
                        if (owner.id_row == 0)
                        {
                            val1["id_user"] = owner.id_user;
                            if (cnn.Insert(val1, "we_topic_user") != 1)
                            {
                                cnn.RollbackTransaction();
                                return JsonResultCommon.Exception(cnn.LastError, _config, loginData.CustomerID,ControllerContext);
                            }
                        }
                    }

                    DataTable dt = cnn.CreateDataTable(s, "(where)", sqlcond);
                    //string LogContent = "", LogEditContent = "";
                    //LogEditContent = DpsPage.GetEditLogContent(old, dt);
                    //if (!LogEditContent.Equals(""))
                    //{
                    //    LogEditContent = "Chỉnh sửa dữ liệu (" + data.id_row + ") : " + LogEditContent;
                    //    LogContent = "Chỉnh sửa dữ liệu topic (" + data.id_row + "), Chi tiết xem trong log chỉnh sửa chức năng";
                    //}
                    //DpsPage.Ghilogfile(loginData.CustomerID.ToString(), LogEditContent, LogContent, loginData.UserName);
                    if (!WeworkLiteController.log(cnn, 22, data.id_row, iduser))
                    {
                        cnn.RollbackTransaction();
                        return JsonResultCommon.Exception(cnn.LastError, _config, loginData.CustomerID,ControllerContext);
                    }
                    cnn.EndTransaction();
                    if (data.email)
                        WeworkLiteController.mailthongbao(data.id_row, data.Users.Select(x => x.id_user).ToList(), 16, loginData, ConnectionString, _notifier);
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
        public BaseModel<object> Delete(long id)
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
                    string sqlq = "select ISNULL((select count(*) from we_topic where Disabled=0 and  id_row = " + id + "),0)";
                    if (long.Parse(cnn.ExecuteScalar(sqlq).ToString()) != 1)
                        return JsonResultCommon.KhongTonTai("Topic");
                    //if (Common.TestDuplicate("", id.ToString(), "-1", "we_work", "id_topic", "Disabled", "0", cnn, "", true) == false)
                    //{
                    //    return JsonResultCommon.Custom("Đang có công việc thuộc mục tiêu này nên không thể xóa");
                    //}
                    sqlq = "update we_topic set Disabled=1, UpdatedDate=getdate(), UpdatedBy=" + iduser + " where id_row = " + id;
                    cnn.BeginTransaction();
                    if (cnn.ExecuteNonQuery(sqlq) != 1)
                    {
                        cnn.RollbackTransaction();
                        return JsonResultCommon.Exception(cnn.LastError, _config, loginData.CustomerID,ControllerContext);
                    }
                    //string LogContent = "Xóa dữ liệu topic (" + id + ")";
                    //DpsPage.Ghilogfile(loginData.CustomerID.ToString(), LogContent, LogContent, loginData.UserName);
                    if (!WeworkLiteController.log(cnn, 23, id, iduser))
                    {
                        cnn.RollbackTransaction();
                        return JsonResultCommon.Exception(cnn.LastError, _config, loginData.CustomerID,ControllerContext);
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

        #region follower
        /// <summary>
        /// 
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [Route("Add-follower")]
        [HttpGet]
        public BaseModel<object> AddFollower(long topic, long user)
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
                    string sqlq = "select ISNULL((select count(*) from we_topic where Disabled=0 and  id_row = " + topic + "),0)";
                    if (long.Parse(cnn.ExecuteScalar(sqlq).ToString()) != 1)
                        return JsonResultCommon.KhongTonTai("Topic");
                    sqlq = "select * from we_topic_user where id_topic = " + topic + " and id_user=" + user;
                    DataTable dt = cnn.CreateDataTable(sqlq);
                    cnn.BeginTransaction();
                    if (dt.Rows.Count > 0)
                    {
                        if (!(bool)dt.Rows[0]["disabled"])
                            return JsonResultCommon.Custom("Người dùng đang theo dõi topic này");
                        else
                        {
                            sqlq = "update we_topic_user set Disabled=0, UpdatedDate=getdate(), UpdatedBy=" + iduser + " where id_row = " + dt.Rows[0]["id_row"];
                            if (cnn.ExecuteNonQuery(sqlq) != 1)
                            {
                                cnn.RollbackTransaction();
                                return JsonResultCommon.Exception(cnn.LastError, _config, loginData.CustomerID,ControllerContext);
                            }
                        }
                    }
                    else
                    {
                        Hashtable val = new Hashtable();
                        val["id_topic"] = topic;
                        val["id_user"] = user;
                        val["CreatedDate"] = DateTime.Now;
                        val["CreatedBy"] = iduser;
                        if (cnn.Insert(val, "we_topic_user") != 1)
                        {
                            cnn.RollbackTransaction();
                            return JsonResultCommon.Exception(cnn.LastError, _config, loginData.CustomerID,ControllerContext);
                        }
                    }
                    if (!WeworkLiteController.log(cnn, 25, topic, iduser, null, user))
                    {
                        cnn.RollbackTransaction();
                        return JsonResultCommon.Exception(cnn.LastError, _config, loginData.CustomerID,ControllerContext);
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
        /// 
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [Route("Remove-follower")]
        [HttpGet]
        public BaseModel<object> RemoveFollower(long topic, long user)
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
                    string sqlq = "select ISNULL((select count(*) from we_topic where Disabled=0 and  id_row = " + topic + "),0)";
                    if (long.Parse(cnn.ExecuteScalar(sqlq).ToString()) != 1)
                        return JsonResultCommon.KhongTonTai("Topic");
                    sqlq = "select * from we_topic_user where id_topic = " + topic + " and id_user=" + user + " and Disabled = 0";
                    DataTable dt = cnn.CreateDataTable(sqlq);
                    if (dt.Rows.Count > 0)
                    {
                        cnn.BeginTransaction();
                        if ((bool)dt.Rows[0]["disabled"])
                        {
                            cnn.RollbackTransaction();
                            return JsonResultCommon.Custom("Người dùng không theo dõi topic này");
                        }
                        else
                        {
                            sqlq = "update we_topic_user set Disabled=1, UpdatedDate=getdate(), UpdatedBy=" + iduser + " where id_row = " + dt.Rows[0]["id_row"];
                            if (cnn.ExecuteNonQuery(sqlq) != 1)
                            {
                                cnn.RollbackTransaction();
                                return JsonResultCommon.Exception(cnn.LastError, _config, loginData.CustomerID,ControllerContext);
                            }
                        }
                        if (!WeworkLiteController.log(cnn, 26, topic, iduser, null, user))
                        {
                            cnn.RollbackTransaction();
                            return JsonResultCommon.Exception(cnn.LastError, _config, loginData.CustomerID,ControllerContext);
                        }
                        cnn.EndTransaction();
                        return JsonResultCommon.ThanhCong();
                    }
                    else
                    {
                        return JsonResultCommon.Custom("Người dùng không theo dõi topic này");
                    }
                }
            }
            catch (Exception ex)
            {
                return JsonResultCommon.Exception(ex, _config, loginData.CustomerID);
            }
        }
        #endregion
    }
}