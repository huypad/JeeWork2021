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
        public AttachmentController(IOptions<JeeWorkConfig> config, IHostingEnvironment hostingEnvironment, IConnectionCache _cache, IConfiguration configuration, ILogger<AttachmentController> logger)
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
            string folder = "/attachment/" + getFolderByType(data.object_type) + "/";
            
            if (!UploadHelper.UploadFile(item.strBase64, item.filename, folder, ContentRootPath, ref x, _configuration))
            {
                return false;
            }
            Hashtable val2 = new Hashtable();
            val2["object_type"] = data.object_type;//topic
            val2["object_id"] = data.object_id;
            val2.Add("path", x);
            val2.Add("filename", item.filename);
            val2.Add("size", Convert.FromBase64String(item.strBase64).Length);
            val2.Add("type", UploadHelper.GetContentType(x));
            val2["CreatedDate"] = Common.GetDateTime();
            val2["CreatedBy"] = data.id_user;
            if (cnn.Insert(val2, "we_attachment") != 1)
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
        /// 1: work,2: topic, 3:comment, 11 - Kết quả công việc, 4 Dự án
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
