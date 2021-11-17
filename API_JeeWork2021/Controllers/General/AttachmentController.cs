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
using System.IO;
using System.Drawing;
using Microsoft.AspNetCore.Http;
using DPSinfra.Logger;
using Newtonsoft.Json;
using DPSinfra.Notifier;
using JeeWork_Core2021.Controller;

namespace JeeWork_Core2021.Controllers.Wework
{
    [ApiController]
    [Route("api/attachment")]
    [EnableCors("JeeWorkPolicy")]
    public class AttachmentController : ControllerBase
    {
        private readonly IHostingEnvironment _hostingEnvironment;
        private JeeWorkConfig _config;
        private IConnectionCache ConnectionCache;
        private IConfiguration _configuration;
        private readonly ILogger<AttachmentController> _logger;
        private INotifier _notifier;
        public List<AccUsernameModel> DataAccount;
        public AttachmentController(IOptions<JeeWorkConfig> config, IHostingEnvironment hostingEnvironment, IConnectionCache _cache, IConfiguration configuration, INotifier notifier, ILogger<AttachmentController> logger)
        {
            _hostingEnvironment = hostingEnvironment;
            _config = config.Value;
            ConnectionCache = _cache;
            _configuration = configuration;
            _logger = logger;
            _notifier = notifier;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [Route("Insert")]
        [HttpPost]
        public BaseModel<object> Insert([FromBody] AttachmentModel data)
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
                    string sqlq = "a";
                    switch (data.object_type)
                    {
                        // Công việc
                        case 1: sqlq = "select ISNULL((select count(*) from we_work where Disabled=0 and id_row = " + data.object_id + "),0)"; break;
                        // Thảo luận
                        case 2: sqlq = "select ISNULL((select count(*) from we_topic where Disabled=0 and id_row = " + data.object_id + "),0)"; break;
                        // Bình luận
                        case 3: sqlq = "select ISNULL((select count(*) from we_comment where Disabled=0 and id_row = " + data.object_id + "),0)"; break;
                        // Kết quả công việc
                        case 11: sqlq = "select ISNULL((select count(*) from we_work where Disabled=0 and id_row = " + data.object_id + "),0)"; break;
                        // Dự án
                        case 4: sqlq = "select ISNULL((select count(*) from we_project_team where Disabled=0 and id_row = " + data.object_id + "),0)"; break;
                        default: break;
                    }
                    if (long.Parse(cnn.ExecuteScalar(sqlq).ToString()) != 1)
                    {
                        switch (data.object_type)
                        {
                            case 1: return JsonResultCommon.KhongTonTai("work");
                            case 2: return JsonResultCommon.KhongTonTai("Topic");
                            case 3: return JsonResultCommon.KhongTonTai("comment");
                            case 11: return JsonResultCommon.KhongTonTai("work");
                            case 4:
                                return JsonResultCommon.KhongTonTai("project");
                            default: break;
                        }
                    }
                    #region Lấy dữ liệu account từ JeeAccount
                    DataAccount = WeworkLiteController.GetAccountFromJeeAccount(HttpContext.Request.Headers, _configuration);
                    if (DataAccount == null)
                        return JsonResultCommon.Custom("Lỗi lấy danh sách nhân viên từ hệ thống quản lý tài khoản");
                    #endregion
                    data.id_user = iduser;
                    cnn.BeginTransaction();
                    byte[] imageBytes = Convert.FromBase64String(data.item.strBase64);
                    long MaxSize = WeworkLiteController.GetMaxSize(_configuration);
                    if (imageBytes.Length > MaxSize)
                    {
                        return JsonResultCommon.KhongTonTai("File không được lớn hơn " + MaxSize / 1000 + "MB");
                    }
                    if (!upload(data, cnn, _hostingEnvironment.ContentRootPath, _configuration))
                    {
                        cnn.RollbackTransaction();
                        return JsonResultCommon.Exception(_logger, cnn.LastError, _config, loginData, ControllerContext);
                    }
                    cnn.EndTransaction();
                    #region Ghi log trong project
                    string LogContent = "", LogEditContent = "";
                    LogContent = LogEditContent = $"Thêm mới file {data.item.filename} vào: loại :{data.object_type}  ";
                    Common.Ghilogfile(loginData.CustomerID.ToString(), LogEditContent, LogContent, loginData.Username);
                    #endregion
                    #region Ghi log lên CDN
                    var d2 = new ActivityLog()
                    {
                        username = loginData.Username,
                        category = LogContent,
                        action = loginData.customdata.personalInfo.Fullname + " thao tác",
                        data = JsonConvert.SerializeObject(data)
                    };
                    _logger.LogInformation(JsonConvert.SerializeObject(d2));
                    #endregion
                    #region Bổ sung tài liệu cho dự án/phòng ban -- Thành viên trong dự án
                    if (data.object_type == 4)
                    {
                        DataTable dt = new DataTable();
                        dt = WeworkLiteController.GetInfoProject(data.object_id, loginData, cnn);
                        string sqlproject = "select id_user from we_project_team_user where Disabled = 0 and id_project_team = " + data.object_id;
                        DataTable dtproject = cnn.CreateDataTable(sqlproject);
                        // lấy thông tin người gửi + cc owner phòng ban tương ứng
                        List<long> list_user = dtproject.AsEnumerable().Select(x => long.Parse(x["id_user"].ToString())).ToList();
                        #region Lấy thông tin để thông báo
                        SendNotifyModel noti = WeworkLiteController.GetInfoNotify(19, ConnectionString);
                        #endregion
                        WeworkLiteController.mailthongbao(data.object_id, list_user, 19, loginData, ConnectionString, _notifier, _configuration);
                        #region Notify upload file cho dự án
                        for (int i = 0; i < list_user.Count; i++)
                        {
                            NotifyModel notify_model = new NotifyModel();
                            Hashtable has_replace = new Hashtable();
                            has_replace.Add("nguoigui", loginData.Username);
                            has_replace.Add("title", dt.Rows[0]["title"].ToString());
                            notify_model.AppCode = "WORK";
                            notify_model.From_IDNV = loginData.UserID.ToString();
                            notify_model.To_IDNV = list_user[i].ToString();
                            notify_model.TitleLanguageKey = LocalizationUtility.GetBackendMessage("project_uploadfile", "", "vi");
                            notify_model.TitleLanguageKey = notify_model.TitleLanguageKey.Replace("$nguoigui$", loginData.customdata.personalInfo.Fullname);
                            notify_model.TitleLanguageKey = notify_model.TitleLanguageKey.Replace("$title$", dt.Rows[0]["title"].ToString());
                            notify_model.TitleLanguageKey = notify_model.TitleLanguageKey.Replace("$filename$", data.item.filename.ToString());
                            notify_model.TitleLanguageKey = notify_model.TitleLanguageKey.Replace("$loai$", "dự án");
                            notify_model.ReplaceData = has_replace;
                            notify_model.To_Link_MobileApp = noti.link_mobileapp.Replace("$id$", data.object_id.ToString());
                            notify_model.To_Link_WebApp = noti.link.Replace("$id$", data.object_id.ToString());
                            var info = DataAccount.Where(x => notify_model.To_IDNV.ToString().Contains(x.UserId.ToString())).FirstOrDefault();
                            if (info is not null)
                            {
                                bool kq_noti = WeworkLiteController.SendNotify(loginData, info.Username, notify_model, _notifier, _configuration);
                            }
                        }
                        #endregion
                    }
                    #endregion
                    if (data.object_type == 1 || data.object_type == 11)
                    {
                        var users = WorkClickupController.getUserTask(cnn, data.object_id);
                        int templateID = 29;
                        if (data.object_type == 11)
                            templateID = 30;
                        DataTable dt = new DataTable();
                        dt = WeworkLiteController.GetInfoTask(data.object_id, loginData, cnn);
                        #region Lấy thông tin để thông báo
                        SendNotifyModel noti = WeworkLiteController.GetInfoNotify(templateID, ConnectionString);
                        #endregion
                        WeworkLiteController.mailthongbao(data.object_id, users, templateID, loginData, ConnectionString, _notifier, _configuration);
                        #region Notify upload file cho công việc
                        for (int i = 0; i < users.Count; i++)
                        {
                            NotifyModel notify_model = new NotifyModel();
                            Hashtable has_replace = new Hashtable();
                            has_replace.Add("nguoigui", loginData.Username);
                            has_replace.Add("title", dt.Rows[0]["title"].ToString());
                            notify_model.AppCode = "WORK";
                            notify_model.From_IDNV = loginData.UserID.ToString();
                            notify_model.To_IDNV = users[i].ToString();
                            if (data.object_type == 1)
                            notify_model.TitleLanguageKey = LocalizationUtility.GetBackendMessage("ww_themtailieucongviec", "", "vi");
                            if (data.object_type == 11)
                                notify_model.TitleLanguageKey = LocalizationUtility.GetBackendMessage("work_capnhatfilekqcongviec", "", "vi");
                            notify_model.TitleLanguageKey = notify_model.TitleLanguageKey.Replace("$nguoigui$", loginData.customdata.personalInfo.Fullname);
                            notify_model.TitleLanguageKey = notify_model.TitleLanguageKey.Replace("$tencongviec$", dt.Rows[0]["title"].ToString());
                            notify_model.ReplaceData = has_replace;
                            notify_model.To_Link_MobileApp = noti.link_mobileapp.Replace("$id$", data.object_id.ToString());
                            notify_model.To_Link_WebApp = noti.link.Replace("$id$", data.object_id.ToString());
                            var info = DataAccount.Where(x => notify_model.To_IDNV.ToString().Contains(x.UserId.ToString())).FirstOrDefault();
                            if (info is not null)
                            {
                                bool kq_noti = WeworkLiteController.SendNotify(loginData, info.Username, notify_model, _notifier, _configuration);
                            }
                        }
                        #endregion
                    }
                    return JsonResultCommon.ThanhCong();
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
                string ConnectionString = WeworkLiteController.getConnectionString(ConnectionCache, loginData.CustomerID, _configuration);
                using (DpsConnection cnn = new DpsConnection(ConnectionString))
                {
                    string sqlq = "select * from we_attachment where disabled=0 and id_row = " + id + "";
                    DataTable dt_file = cnn.CreateDataTable(sqlq);
                    if (dt_file.Rows.Count != 1)
                        return JsonResultCommon.KhongTonTai("Tệp đính kèm");
                    string signedPath = dt_file.Rows[0]["path"].ToString();
                    sqlq = "update we_attachment set Disabled=1, UpdatedDate=GETUTCDATE(), UpdatedBy=" + iduser + " where id_row = " + id;
                    cnn.BeginTransaction();
                    if (cnn.ExecuteNonQuery(sqlq) != 1)
                    {
                        cnn.RollbackTransaction();
                        return JsonResultCommon.Exception(_logger, cnn.LastError, _config, loginData, ControllerContext);
                    }
                    UploadHelper.DeleteFileAtt(signedPath);
                    // string LogContent = "Xóa dữ liệu attachment (" + id + ")";
                    cnn.EndTransaction();
                    #region Ghi log trong project
                    string LogContent = "", LogEditContent = "";
                    LogContent = LogEditContent = $"Xóa dữ liệu attachment (" + id + ")  ";
                    Common.Ghilogfile(loginData.CustomerID.ToString(), LogEditContent, LogContent, loginData.Username);
                    #endregion
                    #region Ghi log lên CDN
                    var d2 = new ActivityLog()
                    {
                        username = loginData.Username,
                        category = LogContent,
                        action = loginData.customdata.personalInfo.Fullname + " thao tác",
                        data = JsonConvert.SerializeObject((new { id = id }))
                    };
                    _logger.LogInformation(JsonConvert.SerializeObject(d2));
                    #endregion
                    return JsonResultCommon.ThanhCong();
                }
            }
            catch (Exception ex)
            {
                return JsonResultCommon.Exception(_logger, ex, _config, loginData);
            }
        }
        [Route("upload-img")]
        [HttpPost]
        public async Task<object> uploadImg(IFormFile file)
        {
            var dirPath = Path.Combine(_hostingEnvironment.ContentRootPath, "Upload\\editor\\");
            var saveimg = Path.Combine(dirPath, file.FileName);
            string imgext = Path.GetExtension(file.FileName);

            if (imgext.ToLower() == ".jpg" || imgext.ToLower() == ".png")
            {
                using (var ms = new MemoryStream())
                {
                    file.CopyTo(ms);
                    var fileBytes = ms.ToArray();
                    string s = Convert.ToBase64String(fileBytes);
                    string x = "";
                    string folder = "/attachment/" + getFolderByType(1) + "/";
                    if (!UploadHelper.UploadFile(s, file.FileName, folder, _hostingEnvironment.ContentRootPath, ref x, _configuration))
                    {
                        return (new
                        {
                            succeeded = false,
                        });
                    }
                    await Task.Delay(1000);
                    return new
                    {
                        succeeded = true,
                        imageUrl = WeworkLiteController.genLinkAttachment(_configuration, x)
                    };
                }
            }
            else
            {
                return (new
                {
                    ERROR = "Định dạng không hợp lệ",
                });
            }
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="item"></param>
        /// <param name="cnn"></param>
        /// <param name="id"></param>
        /// <param name="type">1: work,2: topic</param>
        /// <returns></returns>
        public static bool upload(AttachmentModel data, DpsConnection cnn, string ContentRootPath, IConfiguration _configuration)
        {
            var item = data.item;
            if (item == null)
                return false;
            string x = "";
            Hashtable val = new Hashtable();
            string folder = "/attachment/" + getFolderByType(data.object_type) + "/";
            if (string.IsNullOrEmpty(item.link_cloud))
            {
                if (!UploadHelper.UploadFile(item.strBase64, item.filename, folder, ContentRootPath, ref x, _configuration))
                {
                    return false;
                }
                val.Add("path", x);
                val.Add("size", Convert.FromBase64String(item.strBase64).Length);
                val.Add("type", UploadHelper.GetContentType(x));
            }
            else
                val.Add("path", item.link_cloud);
            val["object_type"] = data.object_type;//1 work, 2 discussion, 3 comment, 4 Project, 11 work result
            val["object_id"] = data.object_id;
            val.Add("filename", item.filename);
            val["CreatedDate"] = Common.GetDateTime();
            val["CreatedBy"] = data.id_user;
            if (string.IsNullOrEmpty(item.link_cloud))
                val["link_cloud"] = DBNull.Value;
            else
                val["link_cloud"] = item.link_cloud;
            if (cnn.Insert(val, "we_attachment") != 1)
            {
                return false;
            }
            return true;
        }
        private static string getFolderByType(int type)
        {
            string re = "";
            switch (type)
            {
                case 1: re = "work"; break;
                case 2: re = "topic"; break;
                case 3: re = "comment"; break;
                case 4: re = "project"; break;
                case 11: re = "work_result"; break;
                default: break;
            }
            return re;
        }
    }

    public class AttachmentModel
    {
        public FileUploadModel item { get; set; }
        /// <summary>
        /// 1 work, 2 discussion, 3 comment, 4 Project, 11 work result
        /// </summary>
        public int object_type { get; set; }
        public long object_id { get; set; }
        public long id_user { get; set; }
    }
    public class DeleteAttachmentModel
    {
        public string _path { get; set; }
    }
}
