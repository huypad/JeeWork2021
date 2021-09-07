using DPSinfra.UploadFile;
using JeeWork_Core2021.Classes;
using JeeWork_Core2021.Models;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;

namespace JeeWork_Core2021.Classes
{
    /// <summary>
    /// Upload vào thư mục chung /dulieu
    /// </summary>
    public class UploadHelper
    {
        public UploadHelper(IOptions<JeeWorkConfig> configLogin, IHttpContextAccessor accessor)
        {
        }

        public static string error = "";
        public static Image Base64ToImage(string base64String)
        {
            byte[] imageBytes = Convert.FromBase64String(base64String);
            Bitmap tempBmp;
            using (MemoryStream ms = new MemoryStream(imageBytes, 0, imageBytes.Length))
            {
                ms.Write(imageBytes, 0, imageBytes.Length);
                using (Image image = Image.FromStream(ms, true))
                {
                    tempBmp = new Bitmap(image.Width, image.Height);
                    Graphics g = Graphics.FromImage(tempBmp);
                    g.DrawImage(image, 0, 0, image.Width, image.Height);
                }
            }
            return tempBmp;
        }
        public static string ImageToBase64(Image image, ImageFormat format)
        {
            string base64String;
            using (MemoryStream ms = new MemoryStream())
            {
                // Convert Image to byte[]
                image.Save(ms, format);
                ms.Position = 0;
                byte[] imageBytes = ms.ToArray();

                // Convert byte[] to Base64 String
                base64String = Convert.ToBase64String(imageBytes);
            }
            return base64String;
        }

        public static ImageFormat GetExtensionImage(string postedFile)
        {
            ImageFormat extension = ImageFormat.Png;
            if (postedFile == "image/jpg")
                extension = ImageFormat.Jpeg;
            if (postedFile == "image/jpeg")
                extension = ImageFormat.Jpeg;
            if (postedFile == "image/gif")
                extension = ImageFormat.Gif;
            if (postedFile == "image/x-png")
                extension = ImageFormat.Png;
            return extension;
        }
        /// <summary>
        /// upload hình ảnh giữ nguyên size hoặc scale 512
        /// </summary>
        /// <param name="strBase64"></param>
        /// <param name="filename">filename bao gồm .extension</param>
        /// <param name="folder">Thư mục trong rootupload chứa file phân cấp bằng dấu /</param>
        /// <param name="ContentRootPath">_hostingEnvironment.ContentRootPath</param>
        /// <param name="filepath">file path sau khi upload trả về</param>
        /// <param name="keepSize"></param>
        /// <returns></returns>
        public static bool UploadImage(string strBase64, string filename, string folder, string ContentRootPath, ref string filepath, bool keepSize = true)
        {
            error = "";
            if (string.IsNullOrEmpty(strBase64))
            {
                error = "Không có file dữ liệu";
                return false;
            }
            try
            {
                byte[] imageBytes = Convert.FromBase64String(strBase64);
                if (imageBytes.Length > JeeWorkConstant.MaxSize)
                {
                    error = "File hình không được lớn hơn " + JeeWorkConstant.MaxSize / 1000 + "MB";
                    return false;
                }
                string path = JeeWorkConstant.RootUpload + folder;
                string Base_Path = Path.Combine(ContentRootPath, path);
                if (!Directory.Exists(Base_Path)) //tạo thư mục nếu chưa có
                    Directory.CreateDirectory(Base_Path);
                filename = checkFilename(filename, path);
                Image img = Base64ToImage(strBase64);
                if (keepSize)
                {
                    img.Save(Base_Path + "\\" + filename);
                }
                else
                {
                    int maxsize = img.Height > img.Width ? img.Width : img.Height;
                    if (maxsize < 64)
                    {
                        error = "Kích thước hình ảnh quá nhỏ";
                        return false;
                    }
                    maxsize = maxsize > 512 ? 512 : maxsize;
                    using (MemoryStream sr = new MemoryStream())
                    {
                        MemoryStream d = new MemoryStream(imageBytes);
                        if (!Directory.Exists(Base_Path)) //tạo thư mục nếu chưa có
                            Directory.CreateDirectory(Base_Path);
                        var rs = DpsLibs.Common.IProcess.Resize(d, maxsize, maxsize, Base_Path, filename, out filename, System.Drawing.Imaging.ImageFormat.Png, false);//nén hình và lưu file
                        if (rs != DpsLibs.Common.ResizeResult.Success && rs != DpsLibs.Common.ResizeResult.Nochange)
                        {
                            error = "Upload hình ảnh thất bại";
                            return false;
                        }
                    }
                }
                //string s_name = Path.GetFileName(filename);
                filepath = folder + filename;
                return true;
            }
            catch (Exception ex)
            {
                error = "Có gì đó không đúng, vui lòng thử lại sau";
                return false;
            }
        }

        internal static bool DeleteFile(string signedPath)
        {
            try
            {
                if (File.Exists(signedPath))
                {
                    // If file found, delete it    
                    File.Delete(signedPath);
                    return true;
                }
                return false;
            }
            catch (Exception ex)
            {
                return false;
            }
        }

        /// <summary>
        /// upload file
        /// </summary>
        /// <param name="strBase64"></param>
        /// <param name="filename">filename bao gồm .extension</param>
        /// <param name="folder">Thư mục trong rootupload chứa file phân cấp bằng dấu /</param>
        /// <param name="ContentRootPath">_hostingEnvironment.ContentRootPath</param>
        /// <param name="filepath">file path sau khi upload trả về</param>
        /// <returns></returns>
        public static bool UploadFile(string strBase64, string filename, string folder, string ContentRootPath, ref string filepath, IConfiguration _configuration)
        {
            GetDateTime UTCdate = new GetDateTime();
            error = "";
            if (string.IsNullOrEmpty(strBase64))
            {
                error = "Không có file dữ liệu";
                return false;
            }
            try
            {
                byte[] bytes = Convert.FromBase64String(strBase64);
                if (bytes.Length > JeeWorkConstant.MaxSize)
                {
                    error = "File hình không được lớn hơn " + JeeWorkConstant.MaxSize / 1000 + "MB";
                    return false;
                }
                string path = JeeWorkConstant.RootUpload + folder;
                string Base_Path = Path.Combine(ContentRootPath, path);
                if (!Directory.Exists(Base_Path)) //tạo thư mục nếu chưa có
                    Directory.CreateDirectory(Base_Path);
                filename = UTCdate.Date.ToString("yyyyMMddHHmmss") + "_" + filename;
                //filename = checkFilename(filename, path);
                path += filename;
                File.WriteAllBytes(path, bytes);
                //string s_name = Path.GetFileName(filename);
                filepath = folder + filename;
                #region Upload file lên link CDN để quản lý
                string FileResult = "";
                string contentType = GetContentType(path);
                byte[] imageBytes = Convert.FromBase64String(strBase64.ToString());
                upLoadFileModel up = new upLoadFileModel()
                {
                    bs = imageBytes, //Convert sang dạng byte
                    FileName = filename, // ví dụ test.docx
                    Linkfile = JeeWorkConstant.RootUpload + folder // Folder chứ File ví dụ File
                };
                UploadResult kq = DPSinfra.UploadFile.UploadFile.UploadFileAllTypeMinio(up, _configuration, contentType);
                if (kq.status)
                {
                    FileResult = path;
                    return true;
                }
                else
                    return false;
                #endregion
            }
            catch (Exception ex)
            {
                error = "Có gì đó không đúng, vui lòng thử lại sau";
                return false;
            }
        }

        /// <summary>
        /// Kiem tra file ton tai? =>đánh số thêm (n)
        /// </summary>
        /// <param name="filename"></param>
        /// <param name="path"></param>
        /// <returns></returns>
        private static string checkFilename(string filename, string path)
        {
            int i = 1;
            var arr = filename.Split(".");
            string _filename = arr[0];
            string _extension = "." + arr[1];
            string filename_new = _filename;
            string pathFile = _filename + _extension;
            while (File.Exists(path + pathFile))
            {
                filename_new = _filename + " (" + i + ")";
                pathFile = filename_new + _extension;
                i = i + 1;
            }

            return pathFile;
        }

        public static string GetFileName(string hrefLink)
        {
            string[] parts = hrefLink.Split('/');
            string fileName = "";

            if (parts.Length > 0)
                fileName = parts[parts.Length - 1];
            else
                fileName = hrefLink;

            return fileName;
        }

        public static string GetContentType(string path)
        {
            var types = GetMimeTypes();
            var ext = Path.GetExtension(path).ToLowerInvariant();
            if (types.ContainsKey(ext))
                return types[ext];
            return "";
        }

        private static Dictionary<string, string> GetMimeTypes()
        {
            return new Dictionary<string, string>
            {
                {".txt", "text/plain"},
                {".pdf", "application/pdf"},
                {".doc", "application/vnd.ms-word"},
                {".docx", "application/vnd.ms-word"},
                {".ppt", "	application/vnd.ms-powerpoint"},
                {".pptx","	application/vnd.openxmlformats-officedocument.presentationml.presentation" },
                {".xls", "application/vnd.ms-excel"},
                {".xlsx", "application/vnd.openxmlformatsofficedocument.spreadsheetml.sheet"},
                {".png", "image/png"},
                {".jpg", "image/jpeg"},
                {".jpeg", "image/jpeg"},
                {".gif", "image/gif"},
                {".csv", "text/csv"},
                {".rar","application/vnd.rar" },
                {".zip","application/zip" },
                {".7z","application/x-7z-compressed" }
            };
        }
        private static Dictionary<string, string> GetIcon()
        {
            return new Dictionary<string, string>
            {
                {"application/pdf", "pdf.png"},
                {"application/vnd.ms-word","word.png"},
                {"application/vnd.ms-powerpoint","ppt.png"},
                {"application/vnd.openxmlformats-officedocument.presentationml.presentation","ppt.png" },
                {"application/vnd.ms-excel","excel.png"},
                {"application/vnd.openxmlformatsofficedocument.spreadsheetml.sheet","excel.png"},
                {"image/png","png.png"},
                {"image/jpeg","jpg.png"},
                {"image/gif","gif.png"},
                {"text/csv","csv.png"},

            };
        }
        public static string GetIcon(string type)
        {
            var types = GetIcon();
            if (types.ContainsKey(type))
                return "assets/media/mime/" + types[type];
            return "assets/media/mime/text2.png";
        }
        public static bool IsImage(string type)
        {
            return type.StartsWith("image");
        }
    }
    public class FileUploadModel
    {
        public long IdRow { get; set; }
        public string strBase64 { get; set; }
        public string filename { get; set; }
        public string src { get; set; }
        public bool IsAdd { get; set; } = false;
        public bool IsDel { get; set; } = false;
        public bool IsImagePresent { get; set; } = false;
    }
}
