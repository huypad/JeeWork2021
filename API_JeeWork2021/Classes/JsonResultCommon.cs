using JeeWork_Core2021.Models;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace JeeWork_Core2021.Classes
{
    public static class JsonResultCommon
    {
        public static BaseModel<object> KhongTonTai(string name = "")
        {
            return new BaseModel<object>
            {
                status = 0,
                error = new ErrorModel()
                {
                    message = string.IsNullOrEmpty(name) ? "Không tồn tại" : name + " không tồn tại",
                    code = JeeWorkConstant.ERRORDATA
                }
            };
        }
        public static BaseModel<object> Trung(string name)
        {
            return new BaseModel<object>
            {
                status = 0,
                error = new ErrorModel()
                {
                    message = name + " đã tồn tại",
                    code = JeeWorkConstant.ERRORDATA
                }
            };
        }
        public static BaseModel<object> BatBuoc(string str_required)
        {
            if (!string.IsNullOrEmpty(str_required))
                str_required = str_required.ToLower();
            return new BaseModel<object>()
            {
                status = 0,
                error = new ErrorModel()
                {
                    message = "Thông tin " + str_required + " là bắt buộc",
                    code = JeeWorkConstant.ERRORDATA
                }
            };
        }
        public static BaseModel<object> Custom(string str_custom)
        {
            return new BaseModel<object>()
            {
                status = 0,
                error = new ErrorModel()
                {
                    message = str_custom,
                    code = JeeWorkConstant.ERRORDATA
                }
            };
        }
        public static BaseModel<object> PhanQuyen(string quyen = "")
        {
            return new BaseModel<object>()
            {
                status = 0,
                error = new ErrorModel()
                {
                    message = "Không có quyền thực hiện chức năng " + (quyen == "" ? "này" : quyen),
                    code = JeeWorkConstant.ERRORCODE_ROLE
                }
            };
        }
        public static BaseModel<object> DangNhap()
        {
            return new BaseModel<object>
            {
                status = 0,
                error = new ErrorModel()
                {
                    message = "Phiên đăng nhập hết hiệu lực. Vui lòng đăng nhập lại!",
                    code = JeeWorkConstant.ERRORCODE
                },
            };
        }
        public static BaseModel<object> Exception(Exception last_error, JeeWorkConfig config, long custemerid = 0, ControllerContext ControllerContext = null)
        {
            string noidung = last_error != null ? last_error.Message : "";
            if (last_error != null && last_error.Data != null)
            {
                string noidungmail = noidung;
                if (ControllerContext != null)
                    noidungmail += "<br>Tại: " + ControllerContext.ActionDescriptor.ControllerName + "/" + ControllerContext.ActionDescriptor.ActionName;
                if (last_error != null)
                    noidungmail += "<br>Chi tiết:<br>" + last_error.StackTrace;
                if (custemerid > 0)
                    AutoSendMail.SendErrorReport(custemerid.ToString(), noidungmail, config);
            }
            return new BaseModel<object>()
            {
                status = 0,
                error = new ErrorModel()
                {
                    message = "Lỗi truy xuất dữ liệu",
                    LastError = noidung,
                    code = JeeWorkConstant.ERRORCODE_EXCEPTION
                }
            };
        }
        public static BaseModel<object> PhanTrang()
        {
            return new BaseModel<object>()
            {
                status = 0,
                error = new ErrorModel()
                {
                    message = "Dữ liệu phân trang không đúng",
                    code = JeeWorkConstant.ERRORCODE
                },
            };
        }
        public static BaseModel<object> ThanhCong()
        {
            return new BaseModel<object>
            {
                status = 1,
            };
        }
        public static BaseModel<object> ThanhCong(object data)
        {
            return new BaseModel<object>
            {
                status = 1,
                data = data
            };
        }
        public static BaseModel<object> ThanhCong(object data, PageModel pageModel)
        {
            return new BaseModel<object>
            {
                status = 1,
                data = data,
                page = pageModel
            };
        }
        public static BaseModel<object> ThanhCong(object data, PageModel pageModel, bool Visible)
        {
            return new BaseModel<object>
            {
                status = 1,
                data = data,
                Visible = Visible,
                page = pageModel
            };
        }
        public static BaseModel<object> ThatBai(string message, Exception last_error)
        {
            return new BaseModel<object>()
            {
                status = 0,
                error = new ErrorModel()
                {
                    message = message,
                    LastError = last_error != null ? last_error.Message : "",
                    code = JeeWorkConstant.ERRORCODE_EXCEPTION
                }
            };
        }
        public static BaseModel<object> ThatBai(string message)
        {
            return new BaseModel<object>()
            {
                status = 0,
                error = new ErrorModel()
                {
                    message = message,
                    code = JeeWorkConstant.ERRORDATA
                }
            };
        }
        public static BaseModel<object> ThatBai(string message, bool Visible)
        {
            return new BaseModel<object>()
            {
                status = 0,
                error = new ErrorModel()
                {
                    message = message,
                    code = JeeWorkConstant.ERRORDATA
                },
                Visible = Visible,
            };
        }
        public static BaseModel<object> KhongHopLe(string name = "")
        {
            return new BaseModel<object>
            {
                status = 0,
                error = new ErrorModel()
                {
                    message = string.IsNullOrEmpty(name) ? "Không hợp lệ" : name + " không hợp lệ",
                    code = JeeWorkConstant.ERRORDATA
                }
            };
        }
        public static BaseModel<object> KhongCoDuLieu(bool Visible = true)
        {
            return new BaseModel<object>
            {
                status = 1,
                data = new List<string>(),
                Visible = Visible
            };
        }
    }
}
