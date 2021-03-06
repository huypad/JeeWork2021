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
using DPSinfra.Notifier;
using JeeWork_Core2021.Controller;

namespace JeeWork_Core2021.Controllers.Wework
{
    [ApiController]
    [Route("api/comment")]
    [EnableCors("JeeWorkPolicy")]
    /// <summary>
    /// api quản lý we_authorize
    /// </summary>
    // [EnableCors(origins: "*", headers: "*", methods: "*")]
    public class CommentController : ControllerBase
    {
        private readonly IHostingEnvironment _hostingEnvironment;
        private JeeWorkConfig _config;
        public List<AccUsernameModel> DataAccount;
        private IConnectionCache ConnectionCache;
        private IConfiguration _configuration;
        private INotifier _notifier;
        private readonly ILogger<CommentController> _logger;
        public CommentController(IOptions<JeeWorkConfig> config, IHostingEnvironment hostingEnvironment, IConnectionCache _cache, IConfiguration configuration, INotifier notifier, ILogger<CommentController> logger)
        {
            _notifier = notifier;
            _hostingEnvironment = hostingEnvironment;
            _config = config.Value;
            ConnectionCache = _cache;
            _configuration = configuration;
            _logger = logger;
        }
        APIModel.Models.Notify Knoti;
        /// <summary>
        /// ds comment
        /// </summary>
        /// <param name="query"></param>
        /// <returns></returns>
        [Route("List")]
        [HttpGet]
        public object List([FromQuery] QueryParams query)
        {
            UserJWT loginData = Ulities.GetUserByHeader(HttpContext.Request.Headers);
            if (loginData == null)
                return JsonResultCommon.DangNhap();
            if (query == null)
                query = new QueryParams();
            bool Visible = true;
            PageModel pageModel = new PageModel();
            try
            {
                string domain = _configuration.GetValue<string>("Host:JeeWork_API") + "/";
                string ConnectionString = JeeWorkLiteController.getConnectionString(ConnectionCache, loginData.CustomerID, _configuration);
                using (DpsConnection cnn = new DpsConnection(ConnectionString))
                {
                    #region Lấy dữ liệu account từ JeeAccount
                    DataAccount = JeeWorkLiteController.GetAccountFromJeeAccount(HttpContext.Request.Headers, _configuration);
                    if (DataAccount == null)
                        return JsonResultCommon.Custom("Lỗi lấy danh sách nhân viên từ hệ thống quản lý tài khoản");
                    //List<string> nvs = DataAccount.Select(x => x.UserId.ToString()).ToList();
                    //string ids = string.Join(",", nvs);
                    string error = "";
                    string listID = JeeWorkLiteController.ListAccount(HttpContext.Request.Headers, out error, _configuration);
                    if (error != "")
                        return JsonResultCommon.Custom(error);
                    #endregion
                    SqlConditions Conds = new SqlConditions();
                    string dieukienSort = "CreatedDate", dieukien_where = " ";
                    if (string.IsNullOrEmpty(query.filter["object_type"]) || string.IsNullOrEmpty(query.filter["object_id"]))
                        return JsonResultCommon.Custom("Đói tượng bắt buộc nhập");
                    dieukien_where += " and object_type=@object_type and object_id=@object_id";
                    Conds.Add("object_type", query.filter["object_type"]);
                    Conds.Add("object_id", query.filter["object_id"]);
                    if (!string.IsNullOrEmpty(query.filter["LastID"]))
                    {

                    }
                    if (!string.IsNullOrEmpty(query.filter["id_parent"]))
                    {
                        dieukien_where += " and (c.id_parent = @id_parent)";
                        dieukien_where = dieukien_where.Replace("@id_parent", query.filter["id_parent"]);
                    }
                    if (!string.IsNullOrEmpty(query.filter["keyword"]))
                    {
                        dieukien_where += " and (comment like '%@keyword%')";
                        dieukien_where = dieukien_where.Replace("@keyword", query.filter["keyword"]);
                    }
                    #region Sort data theo các dữ liệu bên dưới
                    Dictionary<string, string> sortableFields = new Dictionary<string, string>
                        {
                            { "comment", "comment"},
                            { "CreatedBy", "NguoiTao"},
                            { "CreatedDate", "CreatedDate"},
                            { "UpdatedBy", "NguoiSua"},
                            {"UpdatedDate","UpdatedDate" }
                        };
                    #endregion
                    if (!string.IsNullOrEmpty(query.sortField) && sortableFields.ContainsKey(query.sortField))
                        dieukienSort = sortableFields[query.sortField] + ("desc".Equals(query.sortOrder) ? " desc" : " asc");
                    else
                        dieukienSort = "CreatedDate desc";
                    #region Trả dữ liệu về backend để hiển thị lên giao diện
                    string sqlq = @$"select c.*,c.CreatedBy as Id_NV, '' as hoten,'' as mobile, '' as username, '' as Email, '' as image,'' as Tenchucdanh, coalesce(tong,0) as reply from we_comment c
                                    left join (select count(*) as tong, id_parent from we_comment where disabled=0 group by id_parent) child on child.id_parent=c.id_row where c.disabled=0 " + dieukien_where + "  order by " + dieukienSort;
                    sqlq += $";select att.*,att.createdby as id_nv, '' as username from we_attachment att where disabled=0 and object_type=3";
                    sqlq += @$";select l.*, ico.title, ico.icon, '' as hoten from we_comment_like l 
                            join we_like_icon ico on ico.id_row = l.type where l.disabled = 0 and ico.disabled = 0";
                    DataSet ds = cnn.CreateDataSet(sqlq, Conds);
                    if (cnn.LastError != null || ds == null)
                        return JsonResultCommon.Exception(_logger, cnn.LastError, _config, loginData, ControllerContext);
                    DataTable dt = ds.Tables[0];
                    if (dt.Rows.Count == 0)
                        return JsonResultCommon.ThanhCong(new List<string>(), pageModel, Visible);
                    #region Map info account từ JeeAccount
                    foreach (DataRow item in ds.Tables[0].Rows)
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
                    foreach (DataRow item in ds.Tables[1].Rows)
                    {
                        var info = DataAccount.Where(x => item["Id_NV"].ToString().Contains(x.UserId.ToString())).FirstOrDefault();
                        if (info != null)
                        {
                            item["username"] = info.Username;
                        }
                    }
                    foreach (DataRow item in ds.Tables[2].Rows)
                    {
                        var info = DataAccount.Where(x => item["createdby"].ToString().Contains(x.UserId.ToString())).FirstOrDefault();
                        if (info != null)
                        {
                            item["hoten"] = info.FullName;
                        }
                    }
                    #endregion
                    var temp = dt.AsEnumerable();
                    dt = temp.CopyToDataTable();
                    int total = dt.Rows.Count;
                    pageModel.TotalCount = total;
                    pageModel.AllPage = (int)Math.Ceiling(total / (decimal)query.record);
                    pageModel.Size = query.record;
                    pageModel.Page = query.page;
                    if (query.more)
                    {
                        query.page = 1;
                        query.record = pageModel.TotalCount;
                    }
                    // Phân trang
                    var ttttt = dt.AsEnumerable().Where(x => x["id_parent"] == DBNull.Value).Skip((query.page - 1) * query.record).Take(query.record);
                    var asChilds = dt.AsEnumerable().Where(x => x["id_parent"] != DBNull.Value).AsEnumerable();
                    ttttt = asChilds.Concat(ttttt);
                    var data = getChild(dt.AsEnumerable(), domain, loginData, ds.Tables[1].AsEnumerable(), ds.Tables[2].AsEnumerable(), null);
                    return JsonResultCommon.ThanhCong(data, pageModel, Visible);
                }
                #endregion
            }
            catch (Exception ex)
            {
                return JsonResultCommon.Exception(_logger, ex, _config, loginData);
            }
        }
        private object getChild(IEnumerable<DataRow> data, string domain, UserJWT loginData, IEnumerable<DataRow> asAttachment, IEnumerable<DataRow> asLike, object parent = null)
        {
            if (parent == null)
                parent = DBNull.Value;
            return from r in data
                   where r["id_parent"].Equals(parent)
                   select parseObject(r, domain, loginData, asAttachment, asLike, data);
        }

        private object parseObject(DataRow r, string domain, UserJWT loginData, IEnumerable<DataRow> asAttachment, IEnumerable<DataRow> asLike = null, IEnumerable<DataRow> data = null)
        {
            return new
            {
                id_row = r["id_row"],
                comment = r["comment"],
                CreatedDate = string.Format("{0:dd/MM/yyyy HH:mm}", r["CreatedDate"]),
                CreatedBy = r["CreatedBy"],
                NguoiNhans = new List<string>(),
                AllowEdit = r["CreatedBy"].ToString() == loginData.UserID.ToString(),
                NguoiTao = new
                {
                    id_nv = r["id_nv"],
                    hoten = r["hoten"],
                    username = r["username"],
                    mobile = r["mobile"],
                    image = r["image"],
                },
                UpdatedDate = r["UpdatedDate"] == DBNull.Value ? "" : string.Format("{0:dd/MM/yyyy HH:mm}", r["UpdatedDate"]),
                Attachment = from dr in asAttachment
                             where dr["object_id"].Equals(r["id_row"])
                             select new
                             {
                                 id_row = dr["id_row"],
                                 path = JeeWorkLiteController.genLinkAttachment(_configuration, dr["path"]),
                                 filename = dr["filename"],
                                 type = dr["type"],
                                 isImage = UploadHelper.IsImage(dr["type"].ToString()),
                                 icon = UploadHelper.GetIcon(dr["type"].ToString()),
                                 size = dr["size"],
                                 NguoiTao = dr["username"],
                                 CreatedBy = dr["CreatedBy"],
                                 CreatedDate = string.Format("{0:dd/MM/yyyy HH:mm}", dr["CreatedDate"])
                             },
                Like = asLike == null ? null : (from dr in asLike
                                                where dr["id_comment"].Equals(r["id_row"]) && dr["CreatedBy"].ToString() == loginData.UserID.ToString()
                                                select new
                                                {
                                                    type = dr["type"],
                                                    title = dr["title"],
                                                    icon = "assets/media/icons/" + dr["icon"],
                                                }).FirstOrDefault(),
                Likes = asLike == null ? null : from dr in asLike
                                                where dr["id_comment"].Equals(r["id_row"])
                                                group dr by new { a = dr["type"], b = dr["icon"], c = dr["title"] } into g
                                                select new
                                                {
                                                    type = g.Key.a,
                                                    title = g.Key.c,
                                                    icon = "assets/media/icons/" + g.Key.b,
                                                    tong = g.Count(),
                                                    Users = string.Join(Environment.NewLine, from u in g select u["hoten"]),
                                                },
                Children = data == null ? new List<string>() : getChild(data, domain, loginData, asAttachment, asLike, r["id_row"])
            };
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        [Route("Insert")]
        [HttpPost]
        public async Task<object> Insert([FromBody] CommentModel data)
        {
            UserJWT loginData = Ulities.GetUserByHeader(HttpContext.Request.Headers);
            if (loginData == null)
                return JsonResultCommon.DangNhap();
            try
            {
                string strRe = "";
                if (string.IsNullOrEmpty(data.comment))
                    strRe += (strRe == "" ? "" : ",") + "nội dung";
                if (data.object_type <= 0 || data.object_id <= 0)
                    strRe += (strRe == "" ? "" : ",") + "đối tượng";
                if (strRe != "")
                    return JsonResultCommon.BatBuoc(strRe);
                int id_action = data.object_type == 2 ? 24 : 0;


                string domain = _configuration.GetValue<string>("Host:JeeWork_API") + "/";
                string ConnectionString = JeeWorkLiteController.getConnectionString(ConnectionCache, loginData.CustomerID, _configuration);
                using (DpsConnection cnn = new DpsConnection(ConnectionString))
                {
                    #region Lấy dữ liệu account từ JeeAccount
                    DataAccount = JeeWorkLiteController.GetAccountFromJeeAccount(HttpContext.Request.Headers, _configuration);
                    if (DataAccount == null)
                        return JsonResultCommon.Custom("Lỗi lấy danh sách nhân viên từ hệ thống quản lý tài khoản");

                    //List<string> nvs = DataAccount.Select(x => x.UserId.ToString()).ToList();
                    //string ids = string.Join(",", nvs);
                    string error = "";
                    string listID = JeeWorkLiteController.ListAccount(HttpContext.Request.Headers, out error, _configuration);
                    if (error != "")
                        return JsonResultCommon.Custom(error);
                    #endregion
                    long iduser = loginData.UserID;
                    long idk = loginData.CustomerID;
                    Hashtable val = new Hashtable();
                    val.Add("comment", data.comment);
                    if (data.id_parent > 0)
                        val.Add("id_parent", data.id_parent);
                    val.Add("object_type", data.object_type);
                    val.Add("object_id", data.object_id);
                    val.Add("CreatedDate", Common.GetDateTime());
                    val.Add("CreatedBy", iduser);
                    cnn.BeginTransaction();
                    if (cnn.Insert(val, "we_comment") != 1)
                    {
                        cnn.RollbackTransaction();
                        return JsonResultCommon.Exception(_logger, cnn.LastError, _config, loginData, ControllerContext);
                    }
                    long idc = long.Parse(cnn.ExecuteScalar("select IDENT_CURRENT('we_comment')").ToString());
                    if (data.Attachments != null)
                    {
                        foreach (var item in data.Attachments)
                        {
                            var temp = new AttachmentModel()
                            {
                                item = item,
                                object_type = 3,
                                object_id = idc,
                                id_user = loginData.UserID
                            };
                            if (!AttachmentController.upload(temp, cnn, _hostingEnvironment.ContentRootPath, _configuration))
                            {
                                cnn.RollbackTransaction();
                                return JsonResultCommon.Exception(_logger, cnn.LastError, _config, loginData, ControllerContext);
                            }
                        }
                    }
                    //if (!JeeWorkLiteController.log(_logger, loginData.Username, cnn, 39, idc, iduser, data.comment))
                    //{
                    //    cnn.RollbackTransaction();
                    //    return JsonResultCommon.Exception(cnn.LastError, _config, loginData.CustomerID, ControllerContext);
                    //}
                    if (!JeeWorkLiteController.log(_logger, loginData.Username, cnn, 39, data.object_id, loginData.UserID, data.comment, null, idc))
                    {
                        cnn.RollbackTransaction();
                        return JsonResultCommon.Exception(_logger, cnn.LastError, _config, loginData, ControllerContext);
                    }
                    //string LogContent = "", LogEditContent = "";
                    //LogContent = LogEditContent = "Thêm mới dữ liệu comment: comment=" + data.comment + ", object_type=" + data.object_type + ", object_id=" + data.object_id;
                    //DpsPage.Ghilogfile(loginData.CustomerID.ToString(), LogEditContent, LogContent, loginData.UserName);
                    //if (data.id_parent == 0)
                    //{
                    //    if (!JeeWorkLiteController.log(_logger, loginData.Username, cnn, id_action, data.object_id, loginData.UserID, data.comment, null, idc))
                    //    {
                    //        cnn.RollbackTransaction();
                    //        return JsonResultCommon.Exception(_logger,cnn.LastError, _config, loginData,ControllerContext);
                    //    }
                    //}
                    cnn.EndTransaction();
                    data.id_row = idc;
                    string sql = @$"select c.*, c.CreatedBy as Id_NV, '' as hoten,'' as mobile, '' as username, '' as Email, '' as image,'' as Tenchucdanh, coalesce(tong,0) as reply from we_comment c
                                left join(select count(*) as tong, id_parent from we_comment 
                                where disabled = 0 group by id_parent) child 
                                on child.id_parent = c.id_row where c.disabled = 0 
                                and c.CreatedBy in ({listID}) and c.id_row=" + idc;
                    sql += $";select att.*,att.createdby as id_nv , '' as username " +
                        $"from we_attachment att where disabled=0 " +
                        $"and att.createdby in ({listID}) " +
                        $"and object_type=3 and object_id=" + idc;
                    DataSet ds = cnn.CreateDataSet(sql);
                    #region Map info account từ JeeAccount
                    foreach (DataRow item in ds.Tables[0].Rows)
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
                    foreach (DataRow item in ds.Tables[1].Rows)
                    {
                        var info = DataAccount.Where(x => item["Id_NV"].ToString().Contains(x.UserId.ToString())).FirstOrDefault();
                        if (info != null)
                        {
                            item["username"] = info.Username;
                        }
                    }
                    #endregion
                    var re = parseObject(ds.Tables[0].Rows[0], domain, loginData, ds.Tables[1].AsEnumerable());
                    #region Notify replay comment 
                    if (data.id_parent > 0)
                    {
                        // get user comment
                        //string query = @"SELECT CreatedBy  FROM we_comment  WHERE (id_row = )" + data.id_parent ;
                        //DataSet user = cnn.CreateDataSet(sql);
                        object nguoitao = cnn.ExecuteScalar("SELECT CreatedBy  FROM we_comment  WHERE id_row = " + data.id_parent).ToString();
                        if (nguoitao != null && nguoitao.ToString() != loginData.UserID.ToString())
                        {
                            Hashtable has_replace1 = new Hashtable();
                            NotifyModel notify_model = new NotifyModel();
                            has_replace1 = new Hashtable();
                            has_replace1.Add("nguoigui", loginData.Username);
                            //has_replace.Add("project_team", data.title);
                            notify_model.AppCode = "WORK";
                            notify_model.From_IDNV = loginData.UserID.ToString();
                            notify_model.To_IDNV = nguoitao.ToString();
                            notify_model.TitleLanguageKey = LocalizationUtility.GetBackendMessage("ww_replaycomment", "", "vi");
                            notify_model.TitleLanguageKey = notify_model.TitleLanguageKey.Replace("$nguoigui$", loginData.customdata.personalInfo.Fullname);
                            notify_model.ReplaceData = has_replace1;
                            notify_model.To_Link_MobileApp = "";
                            notify_model.To_Link_WebApp = "/tasks/detail/" + data.object_id;
                            //try
                            //{
                            //    if (notify_model != null)
                            //    {
                            //        Knoti = new APIModel.Models.Notify();
                            //        bool kq = Knoti.PushNotify(notify_model.From_IDNV, notify_model.To_IDNV, notify_model.AppCode, notify_model.TitleLanguageKey, notify_model.ReplaceData, notify_model.To_Link_WebApp, notify_model.To_Link_MobileApp, notify_model.ComponentName, notify_model.Component);
                            //    }
                            //}
                            //catch
                            //{ }
                            var info = DataAccount.Where(x => notify_model.To_IDNV.ToString().Contains(x.UserId.ToString())).FirstOrDefault();
                            if (info is not null)
                            {
                                bool kq_noti = JeeWorkLiteController.SendNotify(loginData, info.Username, notify_model, _notifier, _configuration);
                            }
                        }
                    }
                    #endregion
                    #region Notify nhắc tên người được tag
                    Hashtable has_replace = new Hashtable();
                    for (int i = 0; i < data.Users.Count; i++)
                    {
                        if (data.Users[i].id_nv.ToString() != loginData.UserID.ToString())
                        {
                            NotifyModel notify_model = new NotifyModel();
                            has_replace = new Hashtable();
                            has_replace.Add("nguoigui", loginData.Username);
                            //has_replace.Add("project_team", data.title);
                            notify_model.AppCode = "WORK";
                            notify_model.From_IDNV = loginData.UserID.ToString();
                            notify_model.To_IDNV = data.Users[i].id_nv.ToString();
                            notify_model.TitleLanguageKey = "ww_comment";
                            notify_model.TitleLanguageKey = LocalizationUtility.GetBackendMessage("ww_comment", "", "vi");
                            notify_model.TitleLanguageKey = notify_model.TitleLanguageKey.Replace("$nguoigui$", loginData.customdata.personalInfo.Fullname);
                            notify_model.ReplaceData = has_replace;
                            notify_model.To_Link_MobileApp = "";
                            notify_model.To_Link_WebApp = "/tasks/detail/" + data.object_id;
                            try
                            {
                                if (notify_model != null)
                                {
                                    Knoti = new APIModel.Models.Notify();
                                    bool kq = Knoti.PushNotify(notify_model.From_IDNV, notify_model.To_IDNV, notify_model.AppCode, notify_model.TitleLanguageKey, notify_model.ReplaceData, notify_model.To_Link_WebApp, notify_model.To_Link_MobileApp, notify_model.ComponentName, notify_model.Component);
                                }
                            }
                            catch
                            { }
                            var info = DataAccount.Where(x => notify_model.To_IDNV.ToString().Contains(x.UserId.ToString())).FirstOrDefault();
                            if (info is not null)
                            {
                                bool kq_noti = JeeWorkLiteController.SendNotify(loginData, info.Username, notify_model, _notifier, _configuration);
                            }
                        }
                    }
                    #endregion
                    return JsonResultCommon.ThanhCong(re);
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
        [Route("luu-log-comment")]
        [HttpPost]
        public async Task<object> SaveLogComment([FromBody] CommentModel data)
        {
            UserJWT loginData = Ulities.GetUserByHeader(HttpContext.Request.Headers);
            if (loginData == null)
                return JsonResultCommon.DangNhap();
            try
            {
                string ConnectionString = JeeWorkLiteController.getConnectionString(ConnectionCache, loginData.CustomerID, _configuration);
                using (DpsConnection cnn = new DpsConnection(ConnectionString))
                {
                    SqlConditions cond = new SqlConditions();
                    cond.Add("ObjectID", data.object_id_new);
                    string sqlq = "select * from CommentList where ObjectID = @ObjectID";
                    DataTable dt = cnn.CreateDataTable(sqlq, cond);
                    if (cnn.LastError != null || dt.Rows.Count == 0)
                    {
                        return JsonResultCommon.KhongCoDuLieu();
                    }
                    List<string> objectdata = dt.Rows[0]["ComponentName"].ToString().Split("_").ToList();
                    string strRe = "";
                    data.object_id = long.Parse(objectdata[1]);
                    if (objectdata[0] == "kt-task")
                    {
                        data.object_type = 1; // công việc
                    }
                    else
                    {
                        data.object_type = 2; // thảo luận
                    }
                    if (string.IsNullOrEmpty(data.comment))
                        strRe += (strRe == "" ? "" : ",") + "nội dung";
                    if (data.object_type <= 0 || data.object_id <= 0)
                        strRe += (strRe == "" ? "" : ",") + "đối tượng";
                    if (strRe != "")
                        return JsonResultCommon.BatBuoc(strRe);
                    if (!JeeWorkLiteController.log(_logger, loginData.Username, cnn, 39, data.object_id, loginData.UserID, data.comment, null, data.id_topic))
                    {
                        cnn.RollbackTransaction();
                        return JsonResultCommon.Exception(_logger, cnn.LastError, _config, loginData, ControllerContext);
                    }
                    #region Lấy dữ liệu account từ JeeAccount
                    DataAccount = JeeWorkLiteController.GetAccountFromJeeAccount(HttpContext.Request.Headers, _configuration);
                    if (DataAccount == null)
                        return JsonResultCommon.ThanhCong();
                    #endregion
                    #region kiểm tra comment gửi thông báo cho người liên quan
                    if (data.object_type == 1)
                    {
                        #region Lấy thông tin để thông báo
                        SendNotifyModel noti = JeeWorkLiteController.GetInfoNotify(40, ConnectionString);
                        #endregion
                        JeeWorkLiteController.count_comment(data.object_id, cnn, true);
                        string sqlu = "select distinct id_user from we_work_user where (where) and id_user<>" + loginData.UserID;
                        DataTable dtu = cnn.CreateDataTable(sqlu, "(where)", new SqlConditions() { { "Disabled", 0 }, { "id_work", data.object_id } });
                        if (dtu.Rows.Count > 0)
                        {
                            #region Notify assign
                            Hashtable has_replace = new Hashtable();
                            foreach (DataRow item in dtu.Rows)
                            {
                                NotifyModel notify_model = new NotifyModel();
                                has_replace = new Hashtable();
                                notify_model.AppCode = "WORK";
                                notify_model.From_IDNV = loginData.UserID.ToString();
                                notify_model.To_IDNV = item["id_user"].ToString();
                                notify_model.TitleLanguageKey = LocalizationUtility.GetBackendMessage("ww_comment", "", "vi");
                                notify_model.TitleLanguageKey = notify_model.TitleLanguageKey.Replace("$nguoigui$", loginData.customdata.personalInfo.Fullname);
                                notify_model.ReplaceData = has_replace;
                                notify_model.To_Link_MobileApp = noti.link_mobileapp.Replace("$id$", data.object_id.ToString());
                                notify_model.To_Link_WebApp = noti.link.Replace("$id$", data.object_id.ToString());
                                var info = DataAccount.Where(x => notify_model.To_IDNV.ToString().Contains(x.UserId.ToString())).FirstOrDefault();
                                if (info is not null)
                                {
                                    bool kq_noti = JeeWorkLiteController.SendNotify(loginData, info.Username, notify_model, _notifier, _configuration);
                                }
                            }
                            #endregion
                        }
                    }
                    else
                    {
                        // nếu comment mà thảo luận thì kiểm tra có thông báo hay không
                        string sqlcheck = $"select count(*) from we_topic where id_row = { data.object_id } and Disabled = 0 and email = 1";
                        bool CheckGuimail = long.Parse(cnn.ExecuteScalar(sqlcheck).ToString()) > 0;
                        if (CheckGuimail)
                        {
                            //string sqlu = "select distinct id_user from we_topic_user where (where) and id_user<>" + loginData.UserID;
                            //DataTable dtu = cnn.CreateDataTable(sqlu, "(where)", new SqlConditions() { { "Disabled", 0 }, { "id_topic", data.object_id } });
                            //var users = new List<long> { long.Parse(dtu.Rows[0]["id_nv"].ToString()) };
                            //JeeWorkLiteController.SendEmail(data.id_row, users, 25, loginData, ConnectionString, _notifier, _configuration);
                            //if (dtu.Rows.Count > 0)
                            //{
                            //    #region Notify assign
                            //    Hashtable has_replace = new Hashtable();
                            //    foreach (DataRow item in dtu.Rows)
                            //    {
                            //        NotifyModel notify_model = new NotifyModel();
                            //        has_replace = new Hashtable();
                            //        notify_model.AppCode = "WORK";
                            //        notify_model.From_IDNV = loginData.UserID.ToString();
                            //        notify_model.To_IDNV = item["id_user"].ToString();
                            //        notify_model.TitleLanguageKey = LocalizationUtility.GetBackendMessage("ww_comment", "", "vi");
                            //        notify_model.TitleLanguageKey = notify_model.TitleLanguageKey.Replace("$nguoigui$", loginData.customdata.personalInfo.Fullname);
                            //        notify_model.ReplaceData = has_replace;
                            //        notify_model.To_Link_MobileApp = "";
                            //        notify_model.To_Link_WebApp = "/tasks(auxName:aux/detail/" + data.object_id + ")";

                            //        var info = DataAccount.Where(x => notify_model.To_IDNV.ToString().Contains(x.UserId.ToString())).FirstOrDefault();
                            //        if (info is not null)
                            //        {
                            //            bool kq_noti = JeeWorkLiteController.SendNotify(loginData, info.Username, notify_model, _notifier, _configuration);
                            //        }
                            //    }
                            //    #endregion
                            //}
                        }
                    }
                    #endregion
                    return JsonResultCommon.ThanhCong();
                }
            }
            catch (Exception ex)
            {
                return JsonResultCommon.Exception(_logger, ex, _config, loginData);
            }
        }
        private List<String> ListUser(string str)
        {
            var list = new List<String>();
            var newArr = str.Split(' ');
            foreach (string character in newArr)
            {
                if (character[0] == '@')
                {
                    list.Add(character.Replace('@', ' '));
                }
            }
            return list;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        [Route("Update")]
        [HttpPost]
        public async Task<object> Update([FromBody] CommentModel data)
        {
            UserJWT loginData = Ulities.GetUserByHeader(HttpContext.Request.Headers);
            if (loginData == null)
                return JsonResultCommon.DangNhap();
            try
            {
                string strRe = "";
                if (string.IsNullOrEmpty(data.comment))
                    strRe += (strRe == "" ? "" : ",") + "nội dung";
                if (strRe != "")
                    return JsonResultCommon.BatBuoc(strRe);

                string ConnectionString = JeeWorkLiteController.getConnectionString(ConnectionCache, loginData.CustomerID, _configuration);
                using (DpsConnection cnn = new DpsConnection(ConnectionString))
                {
                    #region Lấy dữ liệu account từ JeeAccount
                    DataAccount = JeeWorkLiteController.GetAccountFromJeeAccount(HttpContext.Request.Headers, _configuration);
                    if (DataAccount == null)
                        return JsonResultCommon.Custom("Lỗi lấy danh sách nhân viên từ hệ thống quản lý tài khoản");
                    #endregion
                    long iduser = loginData.UserID;
                    long idk = loginData.CustomerID;
                    SqlConditions sqlcond = new SqlConditions();
                    sqlcond.Add("id_row", data.id_row);
                    sqlcond.Add("disabled", 0);
                    string s = "select * from we_comment where (where)";
                    DataTable old = cnn.CreateDataTable(s, "(where)", sqlcond);
                    if (old == null || old.Rows.Count == 0)
                        return JsonResultCommon.KhongTonTai("Bình luận");
                    Hashtable val = new Hashtable();
                    val.Add("comment", data.comment);
                    val.Add("UpdatedDate", Common.GetDateTime());
                    val.Add("UpdatedBy", iduser);
                    cnn.BeginTransaction();
                    if (cnn.Update(val, sqlcond, "we_comment") != 1)
                    {
                        cnn.RollbackTransaction();
                        return JsonResultCommon.Exception(_logger, cnn.LastError, _config, loginData, ControllerContext);
                    }
                    DataTable dt = cnn.CreateDataTable(s, "(where)", sqlcond);
                    if (!JeeWorkLiteController.log(_logger, loginData.Username, cnn, 39, data.id_row, iduser, data.comment))
                    {
                        cnn.RollbackTransaction();
                        return JsonResultCommon.Exception(_logger, cnn.LastError, _config, loginData, ControllerContext);
                    }
                    //string LogContent = "", LogEditContent = "";
                    //LogEditContent = DpsPage.GetEditLogContent(old, dt);
                    //if (!LogEditContent.Equals(""))
                    //{
                    //    LogEditContent = "Chỉnh sửa dữ liệu (" + data.id_row + ") : " + LogEditContent;
                    //    LogContent = "Chỉnh sửa dữ liệu comment (" + data.id_row + "), Chi tiết xem trong log chỉnh sửa chức năng";
                    //}
                    //DpsPage.Ghilogfile(loginData.CustomerID.ToString(), LogEditContent, LogContent, loginData.UserName);
                    cnn.EndTransaction();

                    #region Notify nhắc tên người được tag
                    if (data.Users != null)
                    {
                        Hashtable has_replace = new Hashtable();
                        for (int i = 0; i < data.Users.Count; i++)
                        {
                            NotifyModel notify_model = new NotifyModel();
                            has_replace = new Hashtable();
                            has_replace.Add("nguoigui", loginData.Username);
                            //has_replace.Add("project_team", data.title);
                            notify_model.AppCode = "WORK";
                            notify_model.From_IDNV = loginData.UserID.ToString();
                            notify_model.To_IDNV = data.Users[i].id_nv.ToString();
                            notify_model.TitleLanguageKey = "ww_comment";
                            notify_model.TitleLanguageKey = LocalizationUtility.GetBackendMessage("ww_comment", "", "vi");
                            notify_model.TitleLanguageKey = notify_model.TitleLanguageKey.Replace("$nguoigui$", loginData.customdata.personalInfo.Fullname);
                            notify_model.ReplaceData = has_replace;
                            notify_model.To_Link_MobileApp = "";
                            notify_model.To_Link_WebApp = "/tasks/detail/" + data.object_id;
                            try
                            {
                                if (notify_model != null)
                                {
                                    Knoti = new APIModel.Models.Notify();
                                    bool kq = Knoti.PushNotify(notify_model.From_IDNV, notify_model.To_IDNV, notify_model.AppCode, notify_model.TitleLanguageKey, notify_model.ReplaceData, notify_model.To_Link_WebApp, notify_model.To_Link_MobileApp, notify_model.ComponentName, notify_model.Component);
                                }
                            }
                            catch
                            { }
                            var info = DataAccount.Where(x => notify_model.To_IDNV.ToString().Contains(x.UserId.ToString())).FirstOrDefault();
                            if (info is not null)
                            {
                                bool kq_noti = JeeWorkLiteController.SendNotify(loginData, info.Username, notify_model, _notifier, _configuration);
                            }
                        }
                    }
                    #endregion

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
        public BaseModel<object> Delete(long id)
        {
            UserJWT loginData = Ulities.GetUserByHeader(HttpContext.Request.Headers);
            if (loginData == null)
                return JsonResultCommon.DangNhap();
            try
            {
                long iduser = loginData.UserID;
                long idk = loginData.CustomerID;
                string ConnectionString = JeeWorkLiteController.getConnectionString(ConnectionCache, loginData.CustomerID, _configuration);
                using (DpsConnection cnn = new DpsConnection(ConnectionString))
                {
                    string sqlq = "select ISNULL((select count(*) from we_comment where Disabled=0 and  id_row = " + id + "),0)";
                    if (long.Parse(cnn.ExecuteScalar(sqlq).ToString()) != 1)
                        return JsonResultCommon.KhongTonTai("Bình luận");
                    sqlq = "update we_comment set Disabled=1, UpdatedDate=GETUTCDATE(), UpdatedBy=" + iduser + " where id_row = " + id;
                    cnn.BeginTransaction();
                    if (cnn.ExecuteNonQuery(sqlq) != 1)
                    {
                        cnn.RollbackTransaction();
                        return JsonResultCommon.Exception(_logger, cnn.LastError, _config, loginData, ControllerContext);
                    }
                    //string LogContent = "Xóa dữ liệu comment (" + id + ")";
                    //DpsPage.Ghilogfile(loginData.CustomerID.ToString(), LogContent, LogContent, loginData.UserName);
                    cnn.EndTransaction();
                    return JsonResultCommon.ThanhCong();
                }
            }
            catch (Exception ex)
            {
                return JsonResultCommon.Exception(_logger, ex, _config, loginData);
            }
        }

        #region like
        /// <summary>
        /// 
        /// </summary>
        /// <param name="id"></param>
        /// <param name="type">0:unline, we_like_icon: 1: like,2 love,3: haha, 4 wow, 5 sad, 6 care, 7 ảngy</param>
        /// <returns></returns>
        [Route("like")]
        [HttpGet]
        public object Like(long id, int type)
        {
            UserJWT loginData = Ulities.GetUserByHeader(HttpContext.Request.Headers);
            if (loginData == null)
                return JsonResultCommon.DangNhap();
            try
            {
                string ConnectionString = JeeWorkLiteController.getConnectionString(ConnectionCache, loginData.CustomerID, _configuration);
                using (DpsConnection cnn = new DpsConnection(ConnectionString))
                {
                    #region Lấy dữ liệu account từ JeeAccount
                    DataAccount = JeeWorkLiteController.GetAccountFromJeeAccount(HttpContext.Request.Headers, _configuration);
                    if (DataAccount == null)
                        return JsonResultCommon.Custom("Lỗi lấy danh sách nhân viên từ hệ thống quản lý tài khoản");

                    //List<string> nvs = DataAccount.Select(x => x.UserId.ToString()).ToList();
                    //string ids = string.Join(",", nvs);
                    string error = "";
                    string listID = JeeWorkLiteController.ListAccount(HttpContext.Request.Headers, out error, _configuration);
                    if (error != "")
                        return JsonResultCommon.Custom(error);
                    #endregion

                    string sqlq = "select ISNULL((select count(*) from we_comment where id_row = " + id + "),0)";
                    if (long.Parse(cnn.ExecuteScalar(sqlq).ToString()) != 1)
                        return JsonResultCommon.KhongTonTai("Bình luận");
                    sqlq = "select '' as username, * from we_comment_like l join we_like_icon ico on l.type=ico.id_row where CreatedBy=" + loginData.UserID + " and id_comment=" + id;
                    DataTable dt = cnn.CreateDataTable(sqlq);
                    if (cnn.LastError != null || dt == null)
                        return JsonResultCommon.Exception(_logger, cnn.LastError, _config, loginData, ControllerContext);
                    bool value = true;
                    int re = 0;
                    Hashtable val = new Hashtable();
                    if (dt.Rows.Count == 0)
                    {
                        val["id_comment"] = id;
                        val["type"] = type;
                        val["CreatedBy"] = loginData.UserID;
                        val["CreatedDate"] = Common.GetDateTime();
                        re = cnn.Insert(val, "we_comment_like");
                    }
                    else
                    {
                        value = type == 0;// !(bool)dt.Rows[0]["disabled"];
                        val["disabled"] = value;
                        if (type > 0)
                            val["type"] = type;
                        val["UpdatedBy"] = loginData.UserID;
                        val["UpdatedDate"] = Common.GetDateTime();
                        re = cnn.Update(val, new SqlConditions() { { "id_row", dt.Rows[0]["id_row"] } }, "we_comment_like");
                    }
                    if (re <= 0)
                        return JsonResultCommon.Exception(_logger, cnn.LastError, _config, loginData, ControllerContext);
                    sqlq += @$";select l.*, ico.title, ico.icon,'' as hoten from we_comment_like l 
join we_like_icon ico on ico.id_row = l.type where l.disabled = 0 and l.createdby in ({listID}) and ico.disabled = 0 and l.id_comment=" + id;
                    DataSet ds = cnn.CreateDataSet(sqlq);
                    #region Map info account từ JeeAccount

                    foreach (DataRow item in dt.Rows)
                    {
                        var info = DataAccount.Where(x => item["createdby"].ToString().Contains(x.UserId.ToString())).FirstOrDefault();

                        if (info != null)
                        {
                            item["username"] = info.Username;
                        }
                    }
                    #endregion
                    DataRow r = ds.Tables[0].Rows[0];
                    var data = new
                    {
                        Like = (bool)r["disabled"] ? null : new
                        {
                            type = r["type"],
                            title = r["title"],
                            icon = "assets/media/icons/" + r["icon"],
                        },
                        Likes = from dr in ds.Tables[1].AsEnumerable()
                                group dr by new { a = dr["type"], b = dr["icon"], c = dr["title"] } into g
                                select new
                                {
                                    type = g.Key.a,
                                    title = g.Key.c,
                                    icon = "assets/media/icons/" + g.Key.b,
                                    tong = g.Count(),
                                    Users = string.Join(Environment.NewLine, from u in g select u["hoten"]),

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
        #endregion
    }

    public class CommentModel
    {
        public long id_row { get; set; }
        /// <summary>
        /// 1: work,2 topic
        /// </summary>
        public int object_type { get; set; }
        public long object_id { get; set; }
        public string object_id_new { get; set; }
        public string comment { get; set; }
        public string id_topic { get; set; }
        public long id_parent { get; set; }
        public List<CommentUserModel> Users { get; set; }
        public List<FileUploadModel> Attachments { get; set; }
    }
    public class CommentUserModel
    {
        public long id_nv { get; set; } = 0;
        public string hoten { get; set; }
        public string username { get; set; }
    }
}
