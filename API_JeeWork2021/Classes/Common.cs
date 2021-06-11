using DpsLibs.Data;
using JeeWork_Core2021.Models;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections;
using System.Data;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Collections.Specialized;
using System.Data.OleDb;
using System.Collections.Generic;
using Microsoft.Net.Http.Headers;

namespace JeeWork_Core2021.Classes
{
    public class Common
    {
        private static string HRCatalog
        {
            get
            {
                return JeeWorkConstant.getConfig("JeeWorkConfig:HRCatalog");
            }
        }
        private static string HRConnectionString
        {
            get
            {
                string ConnectionString = JeeWorkConstant.getConfig("JeeWorkConfig:ConnectionString");
                return ConnectionString.Replace("jeework", HRCatalog);
            }
        }
        public static LoginData _GetInfoUser(string token)
        {
            LoginData user = new LoginData();
            DataTable Tb = new DataTable();
            SqlConditions conds = new SqlConditions();
            if (string.IsNullOrEmpty(token))
                return null;
            try
            {
                conds.Add("Token", token);
                using (DpsConnection cnn = new DpsConnection(HRConnectionString))
                {
                    //Trường hợp đăng nhập thiết bị khác, đăng xuất tất cả các thiết bị
                    string sqlq = "select ISNULL((select count(*) from LoginSection where Token = @Token),0)";
                    if (cnn.ExecuteScalar(sqlq, conds) == null)
                    {
                        user.Status = 5;
                    }
                    else
                    {
                        sqlq = @$"select cus.Ngayhethan, nv.thoiviec, cus.LDAP, cus.LDAPType, cus.LDAPUsernameFormat, cus.RowID as CustemerID, acc.*, nv.holot, nv.ten, Locked, IsTrial,ExpireTrial,cus.Loaihinh
                        from {HRCatalog}.dbo.Tbl_Account acc inner join {HRCatalog}.dbo.Tbl_Nhanvien nv on acc.id_nv=nv.id_nv join tbl_cocautochuc on nv.cocauid = tbl_cocautochuc.rowid inner join tbl_custemers cus on CustemerID=cus.RowID
				        inner join sys_package on sys_package.RowID=PackID where nv.id_nv = isnull(( select Id from LoginSection where Locked = 0 and (ExpiryDate is NULL or ExpiryDate >= GETDATE()) and Token='" + token + "'),0)";
                        Tb = cnn.CreateDataTable(sqlq);
                        if (Tb == null || Tb.Rows.Count == 0)
                            return null;
                        user.Status = 1;
                        //Kiểm tra khách hàng lock
                        if (bool.TrueString.Equals(Tb.Rows[0]["Locked"].ToString())) user.Status = 0;

                        //Kiểm tra nhân viên thôi việc
                        if (bool.TrueString.Equals(Tb.Rows[0]["thoiviec"].ToString())) user.Status = 0;

                        //Kiểm tra tài khoản lock
                        if (bool.TrueString.Equals(Tb.Rows[0]["Lock"].ToString())) user.Status = 0;

                        //Kiểm tra ngày hết hạn của khách hàng
                        if (bool.TrueString.Equals(Tb.Rows[0]["istrial"].ToString()))
                        {
                            if (!Tb.Rows[0]["ExpireTrial"].Equals(DBNull.Value) && ((DateTime)Tb.Rows[0]["ExpireTrial"]).Date < DateTime.Now.Date) user.Status = 4;
                        }
                        else
                        {
                            if (Tb.Rows[0]["ngayhethan"] != DBNull.Value)
                            {
                                DateTime ngayhethan = (DateTime)Tb.Rows[0]["ngayhethan"];
                                if (ngayhethan < DateTime.Today) user.Status = 2;
                            }
                        }
                    }
                }
                user.FirstName = Tb.Rows[0]["ten"].ToString();
                user.LastName = Tb.Rows[0]["holot"].ToString();
                user.Id = Convert.ToInt64(Tb.Rows[0]["Id_nv"].ToString());
                user.UserName = Tb.Rows[0]["Username"].ToString();
                user.IDKHDPS = Convert.ToInt64(Tb.Rows[0]["CustemerID"].ToString());
                user.UserType = 1;
                user.LoaiHinh = int.Parse(Tb.Rows[0]["Loaihinh"].ToString());
                return user;
            }
            catch (Exception ex)
            {
                return null;
            }
        }
        public static string[] GetRolesForUser(string username)
        {
            SqlConditions Conds = new SqlConditions();
            Conds.Add("Username", username);
            using (DpsConnection ConnHR = new DpsConnection(HRConnectionString))
            {
                DpsConnection Conn_Work = new DpsConnection(JeeWorkConstant.getConfig("JeeWorkConfig:ConnectionString"));
                DataTable Tb = Conn_Work.CreateDataTable("select * from Tbl_Account_Permit where (where)", "(where)", Conds);
                DataTable quyennhom = Conn_Work.CreateDataTable("select Id_permit from tbl_group_permit gp inner join tbl_group_account gu on gp.id_group=gu.id_group where (where)", "(where)", Conds);
                DataTable Quyenmacdinh = ConnHR.CreateDataTable(@$"select chucvu.IsManager, chucvu.Yeucautuyendung, chucvu.Capnhatkehoach, chucvu.Taoquytrinh 
                                                            from {HRCatalog}.dbo.tbl_nhanvien nv 
                                                            join {HRCatalog}.dbo.tbl_account acc on nv.id_nv=acc.id_nv 
                                                            join {HRCatalog}.dbo.tbl_chucdanh on nv.id_chucdanh=tbl_chucdanh.id_row 
                                                            join {HRCatalog}.dbo.chucvu on tbl_chucdanh.id_cv=chucvu.id_cv where (where)", "(where)", Conds);
                int soquyenmacdinh = 0;
                bool ismanager = false;
                bool yeucautuyendung = false;
                bool capnhatkehoach = false;
                bool taoquytrinh = false;
                if (Quyenmacdinh.Rows.Count > 0)
                {
                    if (Quyenmacdinh.Rows[0][0] != null)
                        ismanager = (bool)Quyenmacdinh.Rows[0][0];
                    if (Quyenmacdinh.Rows[0][1] != DBNull.Value)
                        yeucautuyendung = (bool)Quyenmacdinh.Rows[0][1];
                    if (Quyenmacdinh.Rows[0][2] != DBNull.Value)
                        capnhatkehoach = (bool)Quyenmacdinh.Rows[0][2];
                    if (Quyenmacdinh.Rows[0][3] != DBNull.Value)
                        taoquytrinh = (bool)Quyenmacdinh.Rows[0][3];
                    if (ismanager) soquyenmacdinh++;
                    if (yeucautuyendung) soquyenmacdinh++;
                    if (capnhatkehoach) soquyenmacdinh++;
                    if (taoquytrinh) soquyenmacdinh++;
                }
                StringCollection colroles = new StringCollection();
                if (ismanager)
                {
                    colroles.Add("4");
                }
                if (yeucautuyendung)
                {
                    colroles.Add("7");
                }
                if (capnhatkehoach)
                {
                    colroles.Add("8");
                }
                if (taoquytrinh)
                {
                    colroles.Add("6");
                }
                for (int i = 0; i < Tb.Rows.Count; i++)
                {
                    if (!colroles.Contains(Tb.Rows[i]["Id_permit"].ToString()))
                        colroles.Add(Tb.Rows[i]["Id_permit"].ToString());
                }
                for (int i = 0; i < quyennhom.Rows.Count; i++)
                {
                    if (!colroles.Contains(quyennhom.Rows[i]["Id_permit"].ToString()))
                        colroles.Add(quyennhom.Rows[i]["Id_permit"].ToString());
                }
                string[] roles = new string[colroles.Count];
                for (int i = 0; i < colroles.Count; i++)
                {
                    roles[i] = colroles[i];
                }
                return roles;
            }
        }
        public static string[] GetRolesForUser_WeWork(string username, DpsConnection Conn)
        {
            SqlConditions Conds = new SqlConditions();
            Conds.Add("Username", username);

                DataTable quyennhom = Conn.CreateDataTable("select Id_permit from tbl_group_permit gp " +
                    "inner join tbl_group_account gu on gp.id_group=gu.id_group where Username=@Username", Conds);
                StringCollection colroles = new StringCollection();
                for (int i = 0; i < quyennhom.Rows.Count; i++)
                {
                    if (!colroles.Contains(quyennhom.Rows[i]["Id_permit"].ToString()))
                        colroles.Add(quyennhom.Rows[i]["Id_permit"].ToString());
                }
                string[] roles = new string[colroles.Count];
                for (int i = 0; i < colroles.Count; i++)
                {
                    roles[i] = colroles[i];
                }
                return roles;


        }
        public static string GetHeader(HttpRequest request)
        {
            try
            {
                Microsoft.Extensions.Primitives.StringValues headerValues;
                //request.Headers.TryGetValue("Authorization", out headerValues);
                request.Headers.TryGetValue("Token", out headerValues);
                return headerValues.FirstOrDefault();
            }
            catch (Exception ex)
            {
                return string.Empty;
            }

        }
        
        public static string GetToken(IHeaderDictionary pHeader)
        {
            if (pHeader == null) return null;
            if (!pHeader.ContainsKey(HeaderNames.Authorization)) return null;

            IHeaderDictionary _d = pHeader;
            return  _d[HeaderNames.Authorization].ToString().Replace("Bearer ", "");
        }

        /// <summary>
        /// Kiểm tra quyền bằng UserID
        /// </summary>
        /// <param name="token">token</param>
        /// <param name="role">role</param>
        /// <returns></returns>
        public static bool CheckRoleByToken(string userID, string role, string ConnectionString, List<AccUsernameModel> DataAccount)
        {
            if (string.IsNullOrEmpty(userID))
                return false;
            try
            {
                //Lấy username
                //string select = "select username from Tbl_Account where id_nv=@userID";
                var info = DataAccount.Where(x => userID.ToString().Contains(x.UserId.ToString())).FirstOrDefault();
                SqlConditions Conds = new SqlConditions();
                Conds.Add("userID", userID);
                using (DpsConnection ConnWW = new DpsConnection(ConnectionString))
                {
                    //DataTable dt = ConnWW.CreateDataTable(select, Conds);
                   // if (dt.Rows.Count <= 0) return false;
                    Conds = new SqlConditions();
                    Conds.Add("Id_permit", role);
                    //string username = dt.Rows[0][0].ToString();
                    string username = info.Username;
                    Conds.Add("Username", username);
                    DataTable Tb = ConnWW.CreateDataTable("select * from Tbl_Account_Permit where (where)", "(where)", Conds);
                    if (Tb.Rows.Count > 0)
                    {
                        return true;
                    }
                    Tb = ConnWW.CreateDataTable("select Id_permit from tbl_group_permit gp inner join tbl_group_account gu on gp.id_group=gu.id_group where (where)", "(where)", Conds);
                    return Tb.Rows.Count > 0;
                }
            }
            catch (Exception ex)
            {
                return false;
            }
        }
        public static string Format_DateHD_ExportExcel(string str = "", bool filename = false)
        {
            try
            {
                if (str.Trim() == "") return "";

                var t = str;
                var pattern1 = @"^(\d{4})(-)(\d{2})(-)(\d{2})";
                if (!filename)
                    t = Regex.Replace(str, pattern1, "$5/$3/$1");
                else
                    t = Regex.Replace(str, pattern1, "$5$3$1");
                return t;
            }
            catch
            {
                return str;
            }
        }

        public static string Format_Currency(string str)//truyền 1 chuỗi dãy số vào
        {
            var t = "";
            if (str.Trim().Replace(" ", "").Length > 0)
            {
                var pattern1 = @"(\d)(?=(\d{3})+(?!\d))";
                t = Regex.Replace(str, pattern1, "$1,");
            }
            return t;
        }

        public static double Format_Double(string str)//truyền 1 chuỗi dãy số vào
        {
            var t = 0.0;
            if (str.Trim().Replace(" ", "").Length > 0)
                t = double.Parse(str);
            return t;
        }

        public static string Filter_SoDienThoai(string p_Input = "")
        {
            try
            {
                Match m = Regex.Match(p_Input.Trim(), @"^([0-9]{1,20})$");
                if (m.Length == 0) return "";
                return p_Input.Trim();
            }
            catch (Exception)
            {
                return "";
            }
        }

        public static string FormatDate_UTC(string p_Input = "")
        {
            try
            {
                if (p_Input.Trim() == "") return "";
                if (p_Input.Contains("01/01/00")) return "";
                Match m;
                var t = p_Input;
                var pattern1 = @"^(\d{2}|\d{1})(\/)(\d{2}|\d{1})(\/)(\d{2}) (\d{1}|\d{2})(:)(\d{1}|\d{2})(:)(\d{1}|\d{2}) (AM|PM)$";
                m = Regex.Match(p_Input.Trim(), pattern1);
                if (m.Length == 0)
                {
                    pattern1 = @"^(\d{2}|\d{1})(\/)(\d{2}|\d{1})(\/)(\d{4}) (\d{1}|\d{2})(:)(\d{1}|\d{2})(:)(\d{1}|\d{2}) (AM|PM)$";
                    t = Regex.Replace(t, pattern1, "$3/$1/$5");
                }
                else
                {
                    t = Regex.Replace(t, pattern1, "$3/$1/20$5");
                }

                if (t.Contains("/1900")) return "";

                return t;
            }
            catch
            {
                return "";
            }
        }

        public static string Remove_Last_Phay(string p_Input = "")
        {
            try
            {
                p_Input = p_Input.Trim();
                var pattern1 = @",$";
                p_Input = Regex.Replace(p_Input, pattern1, "");
                return p_Input;
            }
            catch (Exception ex)
            {
                return p_Input;
            }
        }

        public static string ConvertDateTimeToString(object obj, bool cothoigian = false, bool hiennamtruoc = false)
        {
            if (obj == null || string.IsNullOrEmpty(obj.ToString()))
                return "";
            try
            {

                if (hiennamtruoc)
                {
                    if (cothoigian)
                        return ((DateTime)obj).ToString("yyyy/MM/dd HH:mm:ss");
                    else
                        return ((DateTime)obj).ToString("yyyy/MM/dd");
                }
                else
                {
                    if (cothoigian)
                        return ((DateTime)obj).ToString("dd/MM/yyyy HH:mm:ss");
                    else
                        return ((DateTime)obj).ToString("dd/MM/yyyy");
                }

            }
            catch (Exception)
            {
                return "";
            }
        }
        public static string GetFormatDate(DateTime tungay, DateTime denngay, string tugio, string dengio)
        {
            if (denngay.Date.Equals(DateTime.MinValue))
                return $"{GetDayOfWeek(tungay)} {(tungay.Year.Equals(DateTime.Now.Year) ? string.Format("{0:dd/MM}", tungay) : string.Format("{0:dd/MM/yyyy}", tungay))}";
            if (tungay.Date.Equals(denngay.Date))
                return $"{GetDayOfWeek(tungay)} {(tungay.Year.Equals(DateTime.Now.Year) ? string.Format("{0:dd/MM}", tungay) : string.Format("{0:dd/MM/yyyy}", tungay))} {tugio} - {dengio}";
            return $"{GetDayOfWeek(tungay)} {(tungay.Year.Equals(DateTime.Now.Year) ? string.Format("{0:dd/MM}", tungay) : string.Format("{0:dd/MM/yyyy}", tungay))} {tugio} - {GetDayOfWeek(denngay)} {(denngay.Year.Equals(DateTime.Now.Year) ? string.Format("{0:dd/MM}", denngay) : string.Format("{0:dd/MM/yyyy}", denngay))} { dengio}";
        }
        public static string GetFormatDate(DateTime ngay, string format)
        {
            return string.Format("{0:" + format + "}", ngay).Replace("77622", GetDayOfWeek(ngay));
        }
        public static string GetDayOfWeek(DateTime date)
        {
            switch (date.DayOfWeek)
            {
                case DayOfWeek.Friday: return "T6";
                case DayOfWeek.Monday: return "T2";
                case DayOfWeek.Saturday: return "T7";
                case DayOfWeek.Sunday: return "CN";
                case DayOfWeek.Thursday: return "T5";
                case DayOfWeek.Tuesday: return "T3";
                case DayOfWeek.Wednesday: return "T4";
            }
            return "";
        }
        //Create by Pad: 15 Oct 02:20pm
        /// <summary>
        /// [Wework - Repeated_Task] Trả về kiểu thứ của C# dựa vào chuỗi truyền vào
        /// </summary>
        /// <param name="day"></param>
        /// <returns></returns>
        public static DayOfWeek GetDayOfWeekDay(string day)
        {
            DayOfWeek result = new DayOfWeek();
            switch (day)
            {
                case "T2":
                    return result = DayOfWeek.Monday;
                case "T3":
                    return result = DayOfWeek.Tuesday;
                case "T4":
                    return result = DayOfWeek.Wednesday;
                case "T5":
                    return result = DayOfWeek.Thursday;
                case "T6":
                    return result = DayOfWeek.Friday;
                case "T7":
                    return result = DayOfWeek.Saturday;
                case "CN":
                    return result = DayOfWeek.Sunday;
            }
            return result;
        }
        //Create by Pad: 15 Oct 02:20pm
        /// <summary>
        /// [Wework - Repeated_Task] Trả về ngày của thứ (Tuần hiện tại) truyền vào, dt: Ngày hiện tại
        /// </summary>
        /// <param name="dt"></param>
        /// <param name="startOfWeek"></param>
        /// <returns></returns>
        public static DateTime StartOfWeek(DateTime dt, DayOfWeek startOfWeek)
        {
            int diff = (7 + (dt.DayOfWeek - startOfWeek)) % 7;
            return dt.AddDays(-1 * diff).Date;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="CustemerID">Nếu bảng đó không có customerID thì truyền - 1</param>
        /// <param name="colKey">Áp dụng cho trường hợp xóa, nếu kiểm tra trùng thì truyền Tên cột, nếu kiểm tra xóa truyền ID (Bảng tham chiếu)</param>
        /// <param name="valueID">Áp dụng cho trường hợp thêm/sửa, nếu không có truyền -1</param>
        /// <param name="tableName">Tên bảng kiểm tra</param>
        /// <param name="keyCol">Cột kiểm tra</param>
        /// <param name="ColDisable">Nếu bảng đó có Cột Disable thì truyền tên cột, ngược lại để trống</param>
        /// <param name="isDisable">Nếu bảng đó có cột Disable thì truyền tên cột, giá trị cột, nếu không có truyền -1</param>
        /// <param name="cnn">Chuỗi kết nối</param>
        /// <param name="Values">Giá trị truyền vào để kiểm tra, nếu xóa thì truyền rỗng</param>
        /// <param name="IsDelete">Xóa thì IsDelete = true, ngược lại truyền = false</param>
        /// <returns></returns>
        public static bool TestDuplicate(string colKey, string valueID, string CustemerID, string tableName, string keyCol, string ColDisable, string isDisable, DpsConnection cnn, string Values, bool IsDelete)
        {
            SqlConditions cond = new SqlConditions();
            cond.Add("1", 1);
            string select = @"select " + keyCol + " as NamesDuplicate from " + tableName + " where (CustemerID) and (where)";
            if (!"-1".Equals(CustemerID))
                select = select.Replace("(CustemerID) ", " (CustemerID is null or CustemerID = " + CustemerID + ") ");
            else
                select = select.Replace("(CustemerID) and", " ");
            if (IsDelete) // Trường hợp kiểm tra xóa
                if ("Tbl_Nhanvien".Equals(tableName)) // Nếu lấy dữ liệu trong bảng Tbl_Nhanvien kiểm tra thì viết lại câu select cho nhanh
                {
                    cond = new SqlConditions();
                    cond.Add("thoiviec", 0);
                    cond.Add("disable", 0);
                    cond.Add(keyCol, valueID);
                    select = $"select * from {HRCatalog}.dbo.Tbl_Nhanvien where (where)";
                }
                else // Nếu trường hợp lấy dữ liệu từ bảng khóa ngoại
                    cond.Add(keyCol, valueID);
            else // Trường hợp thêm mới/Chỉnh sửa
            {
                cond.Add(keyCol, Values);
                cond.Add(colKey, valueID, SqlOperator.NotEquals);
            }
            if (!"-1".Equals(isDisable)) // Trường hợp bảng đó có cột Xóa
                cond.Add(ColDisable, isDisable);
            DataTable dt = cnn.CreateDataTable(select, "(where)", cond);
            if (dt.Rows.Count > 0)
                return false;
            return true;
        }
        /// <summary>
        /// Lấy danh sách các cơ cấu tổ chức nhân viên có quyền truy xuất
        /// </summary>
        /// <param name="id_nv">Nhân viên</param>
        public static string GetListStructureByNhanvien(string id_nv)
        {
            string list = "0";
            using (DpsConnection cnn = new DpsConnection(HRConnectionString))
            {
                string custemerid = GetCustemerID(id_nv, cnn).ToString();
                SqlConditions cond = new SqlConditions();
                cond.Add("id_nv", id_nv);
                DataTable dt = cnn.CreateDataTable("select cocauid from P_Phanquyenphamvi where (where)", "(where)", cond);
                cnn.Disconnect();
                foreach (DataRow r in dt.Rows)
                {
                    list += "," + r[0];
                    list += GetListStructureByParent(r["cocauid"].ToString(), custemerid, cnn, false);
                }
            }
            return list;
        }
        /// <summary>
        /// Lấy danh sách các cơ cấu tổ chức nhân viên có quyền truy xuất
        /// </summary>
        /// <param name="id_nv">Nhân viên</param>
        /// <param name="cocauid">Nhân viên</param>
        public static string GetListStructureByNhanvien(string id_nv, string cocauid)
        {
            DataTable dt = new DataTable();
            string list = "0";
            using (DpsConnection cnn = new DpsConnection(HRConnectionString))
            {
                string CustemerID = GetCustemerID(id_nv, cnn).ToString();
                SqlConditions cond = new SqlConditions();
                if (cocauid == GetQuyenCoCauIDTheoNhanVien(long.Parse(id_nv.ToString()), cnn))
                {
                    list += "," + cocauid;
                    cond.Add("id_nv", id_nv);
                    dt = cnn.CreateDataTable("select cocauid from P_Phanquyenphamvi where (where)", "(where)", cond);
                    cnn.Disconnect();
                    foreach (DataRow r in dt.Rows)
                    {
                        list += "," + r[0];
                        list += GetListStructureByParent(r["cocauid"].ToString(), CustemerID, cnn, false);
                    }
                }
                else
                {
                    cnn.ClearError();
                    cond.Add("rowid", cocauid);
                    cond.Add("disable", 0);
                    dt = cnn.CreateDataTable("select rowid from tbl_cocautochuc where (where)", "(where)", cond);
                    foreach (DataRow r in dt.Rows)
                    {
                        list += "," + r[0];
                        list += GetListStructureByParent(r["rowid"].ToString(), CustemerID, cnn, false);
                    }
                }
            }
            return list;
        }
        public static string GetQuyenCoCauIDTheoNhanVien(long id_nv, DpsConnection cnn)
        {
            string id = "0";
            DataTable dt = cnn.CreateDataTable($@"select top 1 cocauid from P_Phanquyendonvi join DM_Donvisudung on rowid = donviid 
                        where startdate <= getdate() and (expiredate is NULL or expiredate >= getdate()) and id_nv = {id_nv}");
            if (dt.Rows.Count > 0)
            {
                id = dt.Rows[0][0].ToString();
            }
            return id;
        }
        public static string GetListStructureByParent(string parentid, string custemerid, DpsConnection cnn, bool IsBaoGomDL)
        {
            string list = "";
            SqlConditions cond = new SqlConditions();
            cond.Add("parentid", parentid);
            cond.Add("tbl_cocautochuc.disable", 0);
            cond.Add("tbl_cocautochuc.custemerid", custemerid);
            string select = "select tbl_cocautochuc.rowid from tbl_cocautochuc where (where)";
            //if (!IsBaoGomDL)
            //    select = "select tbl_cocautochuc.rowid from tbl_cocautochuc left join dm_loaihinhdonvi on loaidonvi = dm_loaihinhdonvi.rowid where (ladonvidoclap is NULL or ladonvidoclap =0) and (where)";
            //DataTable dt = cnn.CreateDataTable($"select tbl_cocautochuc.rowid from tbl_cocautochuc left join dm_loaihinhdonvi on loaidonvi = dm_loaihinhdonvi.rowid where {(IsBaoGomDL ? "" : "(ladonvidoclap is NULL or ladonvidoclap =0) and")} (where)", "(where)", cond);
            DataTable dt = cnn.CreateDataTable(select, "(where)", cond);
            foreach (DataRow r in dt.Rows)
            {
                list += "," + r[0];
                list += GetListStructureByParent(r["rowid"].ToString(), custemerid, cnn, IsBaoGomDL);
            }
            //if (list.Equals("")) list = "," + parentid;
            return list;
        }
        public static int GetCustemerID(string id_nv, DpsConnection cnn)
        {
            int result = 0;
            SqlConditions cond = new SqlConditions();
            cond.Add("id_nv", id_nv);
            string select = $"select CustemerID from {HRCatalog}.dbo.Tbl_Nhanvien inner join tbl_cocautochuc on {HRCatalog}.dbo.Tbl_Nhanvien.cocauid = tbl_cocautochuc.rowid  where (where)";
            DataTable dt = cnn.CreateDataTable(select, "(where)", cond);
            if (dt.Rows.Count <= 0) return result;
            int.TryParse(dt.Rows[0][0].ToString(), out result);
            return result;
        }

        public static string GetQuyenDonViIDTheoNhanVien(long id_nv, DpsConnection cnn)
        {
            string id = "0";
            DataTable dt = cnn.CreateDataTable($@"select top 1 donviid from P_Phanquyendonvi join DM_Donvisudung on rowid = donviid 
                        where startdate <= getdate() and (expiredate is NULL or expiredate >= getdate()) and id_nv = {id_nv}");
            if (dt.Rows.Count > 0)
            {
                id = dt.Rows[0][0].ToString();
            }
            return id;
        }
        public static DataTable ReaddataFromXLSFile(string filename, string sheetname, out string error)
        {
            string tmp_error = "";
            Hashtable cond = new Hashtable();
            string ConnectionString = "";
            string[] s = filename.Split('.');
            if (s[s.Length - 1].Equals("xlsx"))
                ConnectionString = string.Format("Provider=Microsoft.ACE.OLEDB.12.0;Data Source={0};Extended Properties=\"Excel 12.0 Xml;HDR=0;IMEX=1\";", filename);
            if (s[s.Length - 1].Equals("xls"))
                ConnectionString = string.Format("provider=Microsoft.Jet.OLEDB.4.0;Data Source={0};Extended Properties='Excel 8.0;HDR=0;IMEX=1;'", filename);
            DataTable dt = new DataTable();
            OleDbConnection con = new OleDbConnection(ConnectionString);
            try
            {
                con.Open();
                string sql = "SELECT * FROM `" + sheetname + "$`";
                OleDbCommand cmd = new OleDbCommand();
                cmd.CommandText = sql;
                cmd.CommandType = CommandType.Text;
                cmd.Connection = con;

                OleDbDataAdapter da = new OleDbDataAdapter(cmd);
                DataTable ds = new DataTable();
                da.Fill(dt);
                con.Close();
            }
            catch (Exception ex)
            {
                tmp_error = ex.Message;
                con.Close();
            }
            error = tmp_error;
            return dt;
        }
        public static DataTable GetListByManager(string Id_manager, DpsConnection cnn)
        {
            SqlConditions cond = new SqlConditions();
            cond.Add("Id_nv", Id_manager);
            string select = $"select id_row from {HRCatalog}.dbo.tbl_chucdanh where (id_parent in (select id_chucdanh from {HRCatalog}.dbo.Tbl_Nhanvien where (where))) or (id_parent in (select id_chucdanh from lslamviec where active=1 and disable=0 and hinhthuc=3 and (where)))";
            DataTable dt = cnn.CreateDataTable(select, "(where)", cond);
            StringCollection id = new StringCollection();
            for (int i = 0; i < dt.Rows.Count; i++)
            {
                DataTable t = cnn.CreateDataTable($"select id_nv, id_chucdanh,'' as image from {HRCatalog}.dbo.Tbl_Nhanvien where (thoiviec=0) and (tamnghi=0) and ((id_chucdanh=" + dt.Rows[i][0].ToString() + ") or (id_nv in (select id_nv from lslamviec where id_chucdanh=" + dt.Rows[i][0].ToString() + " and active=1 and disable=0 and hinhthuc=3)))");
                if (t.Rows.Count <= 0)
                {
                    StringCollection tmp = GetListChild(dt.Rows[i][0].ToString(), cnn);
                    for (int n = 0; n < tmp.Count; n++)
                    {
                        id.Add(tmp[n]);
                    }
                }
                else
                {
                    for (int j = 0; j < t.Rows.Count; j++)
                    {
                        if (t.Rows[j]["id_chucdanh"].ToString().Equals(dt.Rows[i][0].ToString()))
                            id.Add(t.Rows[j][0].ToString());
                    }
                }
            }

            string id_nv = "";
            for (int k = 0; k < id.Count; k++)
            {
                id_nv += "," + id[k];
            }
            dt = new DataTable();
            if ("".Equals(id_nv))
            {
                return dt;
            }
            id_nv = id_nv.Substring(1);
            dt = cnn.CreateDataTable(@$"select '' as image, nv.id_nv, nv.holot + ' ' + nv.ten as hoten, {HRCatalog}.dbo.tbl_chucdanh.tenchucdanh, nv.manv, cctc.title as tenbp, ngaysinh, isnull(isdangkyfaceid,0) as isdangkyfaceid from {HRCatalog}.dbo.Tbl_Nhanvien nv join {HRCatalog}.dbo.tbl_chucdanh on nv.id_chucdanh = {HRCatalog}.dbo.tbl_chucdanh.id_row
            join {HRCatalog}.dbo.Tbl_Cocautochuc cctc on cctc.RowID = nv.CocauID where nv.thoiviec=0 and nv.disable=0 and id_nv in (" + id_nv + ")");
            if (cnn.LastError != null)
            {
                return null;
            }
            return dt;
        }
        /// <summary>
        /// Lấy danh sách các nhân viên cấp dưới của cấp đưa vào
        /// </summary>
        /// <param name="id_parent"></param>
        /// <returns>Danh sách id_nv</returns>
        public static StringCollection GetListChild(string id_parent, DpsConnection cnn)
        {
            SqlConditions cond = new SqlConditions();
            cond.Add("id_parent", id_parent);
            string select = $"select id_row from {HRCatalog}.dbo.tbl_chucdanh where (where)";
            DataTable dt = cnn.CreateDataTable(select, "(where)", cond);
            StringCollection id = new StringCollection();
            for (int i = 0; i < dt.Rows.Count; i++)
            {
                cond = new SqlConditions();
                cond.Add("id_chucdanh", dt.Rows[i][0].ToString());
                DataTable t = cnn.CreateDataTable($"select id_nv, id_chucdanh from {HRCatalog}.dbo.Tbl_Nhanvien where (thoiviec=0) and (tamnghi=0) and ((id_chucdanh=" + dt.Rows[i][0].ToString() + ") or (id_nv in (select id_nv from lslamviec where id_chucdanh=" + dt.Rows[i][0].ToString() + " and active=1 and disable=0 and hinhthuc=3)))");
                if (t.Rows.Count <= 0)
                {
                    StringCollection tmp = GetListChild(dt.Rows[i][0].ToString(), cnn);
                    for (int n = 0; n < tmp.Count; n++)
                    {
                        id.Add(tmp[n]);
                    }
                }
                else
                {
                    for (int j = 0; j < t.Rows.Count; j++)
                    {
                        if (t.Rows[j]["id_chucdanh"].ToString().Equals(dt.Rows[i][0].ToString()))
                            id.Add(t.Rows[j][0].ToString());
                    }
                }
            }
            return id;
        }
        public static string GetThamSo(DpsConnection cnn, string CustemerID, int id)
        {
            string result = "";
            SqlConditions cond = new SqlConditions();
            cond.Add("id_row", id);
            cond.Add("CustemerID", CustemerID);
            DataTable dt = cnn.CreateDataTable("select giatri, mota from tbl_thamso where (where)", "(where)", cond);
            if (dt.Rows.Count > 0)
                result = dt.Rows[0][0].ToString();
            else
            {
                dt = cnn.CreateDataTable("select * from Temp_Thamso where id_row=@id_row", cond);
                if (dt.Rows.Count > 0)
                {
                    Hashtable val = new Hashtable();
                    val.Add("Id_row", dt.Rows[0]["Id_row"]);
                    val.Add("Giatri", dt.Rows[0]["Giatri"]);
                    val.Add("Mota", dt.Rows[0]["Mota"]);
                    val.Add("Nhom", dt.Rows[0]["Nhom"]);
                    val.Add("Id_nhom", dt.Rows[0]["Id_nhom"]);
                    val.Add("CustemerID", CustemerID);
                    val.Add("Allowedit", dt.Rows[0]["Allowedit"]);
                    cnn.Insert(val, "tbl_thamso");
                }
                dt = cnn.CreateDataTable("select giatri, mota from tbl_thamso where (where)", "(where)", cond);
                if (dt.Rows.Count > 0)
                    result = dt.Rows[0][0].ToString();
            }
            return result;
        }
        public static string getErrorMessageFromBackend(string ErrorCode, string LangCode = "vi", string _space = "")
        {
            string Mess = "";
            string code = ErrorCode;
            string space = _space;
            if (LangCode == "vi")
            {
                //Mess = APIModel.Controller.LocalizationUtility.GetBackendMessage(code, space, "vi");
                //if (Mess == null)
                //{
                //    Mess = APIModel.Controller.LocalizationUtility.GetBackendMessage("null", "", "vi");
                //}
            }
            else
            {
                //Mess = APIModel.Controller.LocalizationUtility.GetBackendMessage(code, space, "en");
                //if (Mess == null)
                //{
                //    Mess = APIModel.Controller.LocalizationUtility.GetBackendMessage("null", "", "en");
                //}
            }
            return Mess;
        }
    }
}
