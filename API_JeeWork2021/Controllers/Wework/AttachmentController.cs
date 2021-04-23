﻿using DpsLibs.Data;
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

namespace JeeWork_Core2021.Controllers.Wework
{
    [ApiController]
    [Route("api/attachment")]
    [EnableCors("JeeWorkPolicy")]
    public class AttachmentController : ControllerBase
    {
        private readonly IHostingEnvironment _hostingEnvironment;
        private JeeWorkConfig _config;

        public AttachmentController(IOptions<JeeWorkConfig> config, IHostingEnvironment hostingEnvironment)
        {
            _hostingEnvironment = hostingEnvironment;
            _config = config.Value;
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
            string Token = Common.GetHeader(Request);
            UserJWT loginData = Ulities.GetUserByHeader(HttpContext.Request.Headers);
            if (loginData == null)
                return JsonResultCommon.DangNhap();
            try
            {
                long iduser = loginData.UserID;
                long idk = loginData.CustomerID;
                using (DpsConnection cnn = new DpsConnection(_config.ConnectionString))
                {
                    string sqlq = "a";
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
                    data.id_user = iduser;
                    cnn.BeginTransaction();
                    if (!upload(data, cnn, _hostingEnvironment.ContentRootPath))
                    {
                        cnn.RollbackTransaction();
                        return JsonResultCommon.Exception(cnn.LastError, loginData.CustomerID, ControllerContext);
                    }
                    cnn.EndTransaction();
                    return JsonResultCommon.ThanhCong();
                }
            }
            catch (Exception ex)
            {
                return JsonResultCommon.Exception(ex, loginData.CustomerID);
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
                using (DpsConnection cnn = new DpsConnection(_config.ConnectionString))
                {
                    string sqlq = "select ISNULL((select count(*) from we_attachment where Disabled=0 and  id_row = " + id + "),0)";
                    if (long.Parse(cnn.ExecuteScalar(sqlq).ToString()) != 1)
                        return JsonResultCommon.KhongTonTai("Tệp đính kèm");
                    sqlq = "update we_attachment set Disabled=1, UpdatedDate=getdate(), UpdatedBy=" + iduser + " where id_row = " + id;
                    cnn.BeginTransaction();
                    if (cnn.ExecuteNonQuery(sqlq) != 1)
                    {
                        cnn.RollbackTransaction();
                        return JsonResultCommon.Exception(cnn.LastError, loginData.CustomerID, ControllerContext);
                    }
                    string LogContent = "Xóa dữ liệu attachment (" + id + ")";
                    cnn.EndTransaction();
                    return JsonResultCommon.ThanhCong();
                }
            }
            catch (Exception ex)
            {
                return JsonResultCommon.Exception(ex, loginData.CustomerID);
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
        public static bool upload(AttachmentModel data, DpsConnection cnn, string ContentRootPath)
        {
            var item = data.item;
            if (item == null)
                return false;
            string x = "";
            string folder = "/attachment/" + getFolderByType(data.object_type) + "/";
            if (!UploadHelper.UploadFile(item.strBase64, item.filename, folder, ContentRootPath, ref x))
            {
                return false;
            }
            item.src = JeeWorkConstant.linkAPI + x;
            Hashtable val2 = new Hashtable();
            val2["object_type"] = data.object_type;//topic
            val2["object_id"] = data.object_id;
            val2.Add("path", x);
            val2.Add("filename", item.filename);
            val2.Add("size", Convert.FromBase64String(item.strBase64).Length);
            val2.Add("type", UploadHelper.GetContentType(x));
            val2["CreatedDate"] = DateTime.Now;
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
        /// 1: work,2: topic, 3:comment
        /// </summary>
        public int object_type { get; set; }
        public long object_id { get; set; }
        public long id_user { get; set; }
    }
}
