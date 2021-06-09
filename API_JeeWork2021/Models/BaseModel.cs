using JeeWork_Core2021.Classes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace JeeWork_Core2021.Models
{
    public class BaseModel<T>
    {
        /// <summary>
        /// Khởi tạo nhanh
        /// //code
        //0: không thành công
        //7: phiên đăng nhập hết hạn
        //8: không có quyền
        //9: lỗi khác
        /// </summary>
        /// <param name="loai">1: không có quyền, 2 lỗi lấy dữ liệu;</param>
        public BaseModel(StateCode code)
        {
            switch (code)
            {
                case StateCode.NoPermit:
                    status = 0;
                    error = new ErrorModel() { message = "Không có quyền truy cập", code = "8" };
                    break;
                case StateCode.CannotGetData:
                    status = 0;
                    error = new ErrorModel() { message = "Lấy dữ liệu thất bại", code = "0" };
                    break;
            }
        }
        public BaseModel()
        {
            error = new ErrorModel();
        }
        //khởi tạo nhanh trả về lỗi
        public BaseModel(string errorMessage)
        {
            status = 0;
            error = new ErrorModel() { message = errorMessage, code = "9" };
        }

        public int status { get; set; }
        public T data { get; set; }
        public PageModel page { get; set; }
        public ErrorModel error { get; set; }
        public bool Visible { get; set; }
    }

    public class ErrorModel
    {
        public ErrorModel()
        { }
        public ErrorModel(string _msg, string _code)
        {
            message = _msg;
            code = _code;
        }
        public string message { get; set; }
        public string code { get; set; }
        public string LastError { get; set; }
    }
    
    public class PageModel
    {
        public int Page { get; set; } = 1;
        public int AllPage { get; set; } = 0;
        public int Size { get; set; } = 10;
        public int TotalCount { get; set; } = 0;
    }

    public class QueryParams
    {
        public bool more { get; set; } = false;
        public int page { get; set; } = 1;
        public int record { get; set; } = 10;
        public string sortOrder { get; set; } = "";
        public string sortField { get; set; } = "";
        public FilterModel filter { get; set; }
        public QueryParams()
        {
            filter = new FilterModel();
        }
    }
    public class FilterModel
    {
        public string keys { get; set; }
        public string vals { get; set; }
        private Dictionary<string, string> _dic = new Dictionary<string, string>();
        public FilterModel() { keys = vals = ""; }
        public FilterModel(string keys, string vals)
        {
            this.keys = keys;
            this.vals = vals;
            initDictionary();
        }

        private void initDictionary()
        {
            string[] arrKeys = keys.Split('|');
            string[] arrVals = vals.Split('|');
            for (int i = 0; i < arrKeys.Length && i < arrVals.Length; i++)
            {
                _dic.Add(arrKeys[i], arrVals[i]);
            }
        }

        public string this[string key]
        {
            get
            {
                if (keys.Length > 0 && _dic.Count == 0)
                    initDictionary();
                if (_dic.ContainsKey(key))
                    return _dic[key];
                return null;
            }
        }
       
    }
    public class UpdateMessage
    {
        public long userID { get; set; }
        public string updateField { get; set; }
        public object fieldValue { get; set; }
        //public JeeWork fieldValue { get; set; }
    }
    public class InitMessage
    {
        public long CustomerID { get; set; }
        public List<string> AppCode { get; set; }
        public long UserID { get; set; }
        public string Username { get; set; }
        public bool IsInitial { get; set; }
        public bool IsAdmin { get; set; }
    }

    public class UpdateAdminMessage
    {
        public long CustomerID { get; set; }
        public string AppCode { get; set; }
        public long UserID { get; set; }
        public string Username { get; set; }
        public string Action { get; set; }
    }
}