using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace JeeWork_Core2021.Models
{
    public class UserRightsModel : BaseModel<UserRights>
    {

    }
    public class UserRights
    {
        public PageModel page { get; set; }
    }
    public class NhomAddData
    {
        public string TenNhom { get; set; }
        /// <summary>
        /// 0: Quản lý hồ sơ; 1: Tiền lương
        /// </summary>
        public int Module { get; set; }
    }
    public class UserAddData
    {
        public int ID_Nhom { get; set; }
        public string UserName { get; set; }
    }

    public class NguoiDungAddData
    {
        public string UserName { get; set; }
        public bool Locked { get; set; }
    }

    public class QuyenAddData
    {
        /// <summary>
        /// ID: ID_NhomUser hoặc ID_User
        /// </summary>
        public string ID { get; set; }
        public int ID_NhomChucNang { get; set; }
        public int ID_Quyen { get; set; }
        public bool IsEdit { get; set; }
        public bool IsRead { get; set; }
        public string TenQuyen { get; set; }
        /// <summary>
        /// Tên nhóm hoặc Tên User
        /// </summary>
        public string Ten { get; set; }
    }
    public class PermissionNewModel
    {
        /// <summary>
        /// ID: ID_NhomUser hoặc ID_User
        /// </summary>
        public string ID { get; set; }
        public int ID_NhomChucNang { get; set; }
        public int ID_Quyen { get; set; }
        public bool IsEdit { get; set; }
        public bool IsRead { get; set; }
        public string TenQuyen { get; set; }
        /// <summary>
        /// Tên nhóm hoặc Tên User
        /// </summary>
        public string Ten { get; set; }
        public string ID_Nhom { get; set; }
        public bool IsGroup { get; set; }
        public bool UnCheckedAll { get; set; }
    }
}
