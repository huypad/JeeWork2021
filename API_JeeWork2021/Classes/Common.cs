using DpsLibs.Data;
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
using RestSharp;
using Newtonsoft.Json;
using JeeWork_Core2021.Models;
using JeeWork_Core2021.Controllers.Wework;
using System.Globalization;
using Microsoft.Extensions.Configuration;
using System.IO;
using DPSinfra.Logger;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Mvc;

namespace JeeWork_Core2021.Classes
{
    public class Common
    {
        public static string ConnectionString;
        public Common(string _connectionString)
        {
            ConnectionString = _connectionString;
        }
        public static ILogger<object> _logger;
        public Common(ILogger<Common> logger)
        {
            _logger = logger;
        }
        public static string[] GetRolesForUser_WeWork(string username, DpsConnection Conn)
        {
            SqlConditions Conds = new SqlConditions();
            Conds.Add("Username", username);
            DataTable quyenusers = Conn.CreateDataTable("select Id_permit from Tbl_Account_Permit where (where)", "(where)", Conds);
            DataTable quyennhom = Conn.CreateDataTable("select Id_permit from tbl_group_permit gp " +
                "inner join tbl_group_account gu on gp.id_group=gu.id_group where Username=@Username", Conds);
            StringCollection colroles = new StringCollection();
            for (int i = 0; i < quyennhom.Rows.Count; i++)
            {
                if (!colroles.Contains(quyennhom.Rows[i]["Id_permit"].ToString()))
                    colroles.Add(quyennhom.Rows[i]["Id_permit"].ToString());
            }
            for (int i = 0; i < quyenusers.Rows.Count; i++)
            {
                if (!colroles.Contains(quyenusers.Rows[i]["Id_permit"].ToString()))
                    colroles.Add(quyenusers.Rows[i]["Id_permit"].ToString());
            }
            string[] roles = new string[colroles.Count];
            for (int i = 0; i < colroles.Count; i++)
            {
                roles[i] = colroles[i];
            }
            return roles;
        }
        public static bool UpdatePermitNew(UserJWT loginData, DpsConnection cnn)
        {
            if (loginData.UserID <= 0)
                return false;
            //Lấy username
            SqlConditions Conds = new SqlConditions();
            Conds.Add("userID", loginData.UserID);
            Conds = new SqlConditions();
            Conds.Add("Username", loginData.Username);
            string sqlq = "";
            sqlq = @"select * from tbl_permision 
                        where Id_Permit not in (select Id_permit from tbl_group_permit 
                        where Id_Group in (select Id_group from tbl_group 
                        where isadmin = 1 and CustemerID = " + loginData.CustomerID + ")) and CustemerID is null";
            DataTable dt_permit = cnn.CreateDataTable(sqlq);
            DataTable dt_group_admin = cnn.CreateDataTable("select * from tbl_group " +
                   "where isadmin = 1 and CustemerID = " + loginData.CustomerID + "");
            if (dt_permit.Rows.Count > 0 && dt_group_admin.Rows.Count > 0)
            {
                // insert thêm quyền cho group admin
                foreach (DataRow item in dt_group_admin.Rows)
                {
                    Hashtable has = new Hashtable();
                    has["Id_Group"] = item["Id_group"].ToString();
                    foreach (DataRow _permit in dt_permit.Rows)
                    {
                        has["Id_Permit"] = _permit["Id_permit"].ToString();
                        if (cnn.Insert(has, "tbl_group_permit") != 1)
                        {
                            cnn.RollbackTransaction();
                            return false;
                        }
                    }
                }
            }
            return true;
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
            return _d[HeaderNames.Authorization].ToString().Replace("Bearer ", "");
        }

        /// <summary>
        /// Kiểm tra quyền bằng UserID
        /// </summary>
        /// <param name="token">token</param>
        /// <param name="role">role</param>
        /// <returns></returns>
        public static bool CheckRoleByUserID(UserJWT loginData, long role, DpsConnection cnn)
        {
            if (loginData.UserID <= 0)
                return false;
            try
            {
                //Lấy username
                SqlConditions Conds = new SqlConditions();
                Conds.Add("userID", loginData.UserID);
                Conds = new SqlConditions();
                Conds.Add("Id_permit", role);
                Conds.Add("Username", loginData.Username);
                DataTable Tb = cnn.CreateDataTable("select * from Tbl_Account_Permit where (where)", "(where)", Conds);
                if (Tb.Rows.Count > 0)
                {
                    return true;
                }
                Tb = cnn.CreateDataTable("select Id_permit from tbl_group_permit gp " +
                    "join tbl_group_account gu " +
                    "on gp.id_group=gu.id_group " +
                    "where (where)", "(where)", Conds);
                return Tb.Rows.Count > 0;
            }
            catch (Exception ex)
            {
                return false;
            }
        }
        public static bool IsReadOnlyPermit(string roleName, string username)
        {
            SqlConditions Conds = new SqlConditions();
            Conds.Add("id_permit", roleName);
            Conds.Add("username", username);
            using (DpsConnection Conn = new DpsConnection(ConnectionString))
            {
                DataTable Tb = Conn.CreateDataTable("select edit from tbl_account_permit where (where)", "(where)", Conds);
                if ((Tb.Rows.Count > 0) && (bool.FalseString.Equals(Tb.Rows[0][0].ToString())))
                {
                    return true;
                }
                Tb = Conn.CreateDataTable("select id_permit, edit from tbl_group_permit gp " +
                    "join tbl_group_account gu " +
                    "on gp.id_group=gu.id_group " +
                    "where (where) order by edit desc", "(where)", Conds);
                if ((Tb.Rows.Count > 0) && (bool.FalseString.Equals(Tb.Rows[0][1].ToString())))
                {
                    return true;
                }
                return false;
            }
        }
        public static bool CheckAdminWorkSpace(long id_user, long spacetype, long id)
        {
            using (DpsConnection Conn = new DpsConnection(ConnectionString))
            {
                DataTable Tb = new DataTable();
                string sqlid = "";
                string sqlq = "";
                if (spacetype < 4 && spacetype > 0)
                {
                    sqlq = "select Type from we_department_owner where (where)";
                    if (spacetype == 3)
                        sqlq = "select * from we_project_team_user where admin = 1 and id_user = " + id_user + " and id_project_team = " + id;
                    if (spacetype < 3)
                        sqlq = "select * from we_department_owner where admin = 1 and id_user = " + id_user + " and id_department = " + id;
                    Tb = Conn.CreateDataTable(sqlq);
                    if (Tb.Rows.Count == 0)
                    {
                        spacetype--;
                        if (spacetype == 2 && spacetype > 0)
                        {
                            sqlid = "select ISNULL((select id_department from we_project_team where id_row = " + id + "),0)";
                        }
                        if (spacetype == 1 && spacetype > 0)
                        {
                            sqlid = "select ISNULL((select parentid from we_department where id_row = " + id + "),0)";
                        }
                        if (long.Parse(Conn.ExecuteScalar(sqlq).ToString()) > 0)
                        {
                            id = int.Parse(sqlid);
                            CheckAdminWorkSpace(id_user, spacetype, id);
                        }
                    }
                    else
                        return true;
                }
                return false;
            }
        }
        public static string ListIDDepartment(DpsConnection cnn, long id_project_team)
        {
            string listid = "0";
            SqlConditions conds = new SqlConditions();
            long id_department = 0;
            conds.Add("id_row", id_project_team);
            conds.Add("disabled", 0);
            DataTable dt = new DataTable();
            string sql_dept = "select ISNULL((select id_department from we_project_team where id_row = " + id_project_team + "),0)";
            id_department = long.Parse(cnn.ExecuteScalar(sql_dept).ToString());
            string sql = "";
            long ParentID = 0;
            sql = @"select id_row from we_department 
                    where id_row = " + id_department + "";
            sql_dept = "select ISNULL((" + sql + "),0)";
            ParentID = long.Parse(cnn.ExecuteScalar(sql_dept).ToString());
            if (ParentID > 0) // Tiếp tục lấy con của thư mục dưới phòng ban
                sql += " union all select ParentID from we_department " +
                    "where id_row = " + id_department;
            dt = cnn.CreateDataTable(sql);

            return listid;
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
            GetDateTime UTCdate = new GetDateTime();
            if (denngay.Date.Equals(DateTime.MinValue))
                return $"{GetDayOfWeek(tungay)} {(tungay.Year.Equals(UTCdate.Date.Year) ? string.Format("{0:dd/MM}", tungay) : string.Format("{0:dd/MM/yyyy}", tungay))}";
            if (tungay.Date.Equals(denngay.Date))
                return $"{GetDayOfWeek(tungay)} {(tungay.Year.Equals(UTCdate.Date.Year) ? string.Format("{0:dd/MM}", tungay) : string.Format("{0:dd/MM/yyyy}", tungay))} {tugio} - {dengio}";
            return $"{GetDayOfWeek(tungay)} {(tungay.Year.Equals(UTCdate.Date.Year) ? string.Format("{0:dd/MM}", tungay) : string.Format("{0:dd/MM/yyyy}", tungay))} {tugio} - {GetDayOfWeek(denngay)} {(denngay.Year.Equals(DateTime.Now.Year) ? string.Format("{0:dd/MM}", denngay) : string.Format("{0:dd/MM/yyyy}", denngay))} { dengio}";
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
        /// <summary>
        /// 
        /// </summary>
        /// <param name="loginData"></param>
        /// <param name="type"></param>
        /// <param name="id"></param>
        /// <returns></returns>
        public static DataSet GetWorkSpace(UserJWT loginData, long id, long type, string ConnectionString)
        {
            string sql_space = "", sql_project = "", sql_folder = "", where_dpm = "";
            using (DpsConnection cnn = new DpsConnection(ConnectionString))
            {
                SqlConditions cond = new SqlConditions();
                cond.Add("de.idkh", loginData.CustomerID);
                cond.Add("de.disabled", 0);
                sql_space = @$"select de.id_row, de.title, de.id_cocau, de.idkh, de.priority, de.disabled, de.parentid, 
				            IIf((select type from we_department_owner do where do.disabled = 0 
                            and do.id_user = " + loginData.UserID + " " +
                            "and do.id_department = de.id_row) = 1,1,0) as owner " +
                            ",IIf((select type from we_department_owner do where do.disabled = 0 " +
                            "and do.id_user = " + loginData.UserID + " " +
                            "and do.id_department = de.parentid) = 1,1,0) as parentowner " +
                            "from we_department de where (where)";
                sql_folder = ";" + sql_space;
                where_dpm = @$"and (de.id_row in (select parentid from we_department fd where fd.disabled = 0
                and fd.id_row in (select do.id_department from we_department_owner do 
                where do.disabled = 0 and do.id_user = " + loginData.UserID + ") " +
                "union all " +
                "select p.id_department from we_project_team p" +
                " where p.disabled = 0 " +
                "and p.id_row in (select u.id_project_team from we_project_team_user u where u.id_user = " + loginData.UserID + " " +
                "and u.disabled = 0)) " +
                "or de.id_row in (select dsp.id_department from we_department_owner dsp " +
                "where dsp.disabled = 0 and dsp.id_user = " + loginData.UserID + ") (parentid)";
                sql_folder += " and parentid is not null (admin)";
                sql_space += " and parentid is null (admin)";
                sql_project = ";select p.id_row, p.icon, p.title, p.detail, p.id_department" +
                            ", p.loai, p.start_date, p.end_date, p.color, p.template, p.status, p.is_project" +
                            ", p.priority, p.locked, p.disabled, p.CreatedDate, default_view, '0' as admin_project " +
                            "from we_project_team p " +
                            $"where p.disabled = 0 and p.id_department in (select id_row from we_department where idkh = " + loginData.CustomerID + ") (dk_proj)";
                if (!MenuController.CheckGroupAdministrator(loginData.Username, cnn, loginData.CustomerID))
                {
                    string and_folder = "or de.parentid in (select dsp.id_department from we_department_owner dsp " +
                                            "where dsp.disabled = 0 and dsp.id_user = " + loginData.UserID + "))";
                    sql_space = sql_space.Replace("(admin)", where_dpm);
                    sql_space = sql_space.Replace("(parentid)", ")");
                    sql_folder = sql_folder.Replace("(admin)", where_dpm);
                    sql_folder = sql_folder.Replace("(parentid)", and_folder);
                    string dk_proj = @" and 
                (id_department in (select id_row from we_department where ParentID is null and id_row in (select id_department from we_department_owner do where do.disabled = 0 and do.id_user = " + loginData.UserID + " and type = 1)) " +
                "or id_department in (select id_row from we_department where ParentID is not null and ParentID in (select id_department from we_department_owner do where do.disabled = 0 and do.id_user = " + loginData.UserID + " and type = 1)) " +
                "or p.id_row in (select id_project_team from we_project_team_user where id_user = " + loginData.UserID + "  and Disabled = 0 ))";
                    sql_project = sql_project.Replace("(dk_proj)", dk_proj);
                }
                else
                {
                    sql_space = sql_space.Replace("(admin)", "");
                    sql_folder = sql_folder.Replace("(admin)", "");
                    sql_project = sql_project.Replace("(dk_proj)", " ");
                }
                sql_project += " order by createddate desc";
                DataTable dt_space = new DataTable();
                DataTable dt_folder = new DataTable();
                DataTable dt_project = new DataTable();
                DataTable dt_admin_proj = new DataTable();
                dt_admin_proj = cnn.CreateDataTable(@"select id_row, id_project_team 
                                                    from we_project_team_user where disabled = 0
                                                    and id_user = " + loginData.UserID + " and admin = 1");
                dt_space = cnn.CreateDataTable(sql_space, "(where)", cond);
                dt_folder = cnn.CreateDataTable(sql_folder, "(where)", cond);
                dt_project = cnn.CreateDataTable(sql_project);
                dt_project.Columns.Add("parentowner");
                dt_project.Columns.Add("owner");
                foreach (DataRow dr in dt_project.Rows)
                {
                    DataRow[] dr_parent = dt_folder.Select("id_row=" + dr["id_department"]);
                    if (dr_parent.Length > 0)
                    {
                        dr["parentowner"] = dr_parent[0]["parentowner"].ToString();
                        dr["owner"] = "0";
                    }
                    DataRow[] dr_de = dt_space.Select("id_row=" + dr["id_department"]);
                    if (dr_de.Length > 0)
                    {
                        dr["owner"] = dr_de[0]["owner"].ToString();
                        dr["parentowner"] = "0";
                    }
                    DataRow[] dr_pr = dt_admin_proj.Select("id_project_team=" + dr["id_row"]);
                    if (dr_pr.Length > 0)
                    {
                        dr["admin_project"] = "1";
                    }
                }
                DataSet ds_workspace = new DataSet();
                ds_workspace.Tables.Add(dt_space);
                ds_workspace.Tables.Add(dt_folder);
                ds_workspace.Tables.Add(dt_project);
                return ds_workspace;
            }
        }
        public static string GetIDByWorkSpace(long id, long type, UserJWT loginData)
        {
            string list = "0";
            using (DpsConnection cnn = new DpsConnection(ConnectionString))
            {
                SqlConditions cond = new SqlConditions();
                DataTable dt = new DataTable();
                if (type < 3)
                {
                    string columname = "";
                    cond.Add("idkh", loginData.CustomerID);
                    if (type == 2)
                    {
                        cond.Add("id_row", id);
                        columname = "parentid";
                    }
                    if (type == 1)
                    {
                        cond.Add("parentid", id);
                        columname = "id_row";
                    }
                    dt = cnn.CreateDataTable("select " + columname + " from we_department where (where)", "(where)", cond);
                    if (dt.Rows.Count > 0)
                    {
                        foreach (DataRow r in dt.Rows)
                        {
                            list += "," + r[0];
                        }
                    }
                }

            }
            return list;
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


        public static DateTime convertStringToDatetime(string strNgay)
        {
            string[] formats = new string[] { "dd/MM/yyyy HH:mm:ss", "dd/MM/yyyy HH:mm", "dd/MM/yyyy HH", "dd/MM/yyyy",
                                                        "yyyy-MM-dd'T'00:00:00:000'Z'", "d/MM/yyyy", "dd/M/yyyy", "d/M/yyyy" };
            DateTime ngay = new DateTime();
            DateTime.TryParseExact(strNgay, formats, null, DateTimeStyles.None, out ngay);
            return ngay;
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
        public static bool InsertGroupType(DpsConnection cnn, long CustomerID)
        {
            long group_type = 1;
            string[,] arr = {
                    {"Nhóm thành viên", "3","3402,3403,3501,3502,3503,3610,3700,3800"},
                    //{"Nhóm quản lý dự án", "2","3501,3502,3503,3610"}
                    //{"Thành viên", "3","0"}
                    };
            for (int i = 0; i < arr.GetLength(0); i++)
            {
                group_type = long.Parse(arr[i, 1].ToString());
                SqlConditions cond = new SqlConditions();
                cond.Add("CustemerID", CustomerID);
                cond.Add("GroupType", group_type);
                DataTable dt = new DataTable();
                string sqlq = "select * from tbl_group where (where)";
                dt = cnn.CreateDataTable(sqlq, "(where)", cond);
                if (dt.Rows.Count == 0)
                {
                    Hashtable val = new Hashtable();
                    val.Add("groupname", arr[i, 0]);
                    val.Add("grouptype", arr[i, 1]);
                    val.Add("datecreated", GetDateTime());
                    val.Add("custemerid", CustomerID);
                    val.Add("module", 0);
                    val.Add("isadmin", 0);
                    if (cnn.Insert(val, "tbl_group") == 1)
                    {
                        var MaxId_Group = int.Parse(cnn.ExecuteScalar("SELECT ISNULL(MAX(Id_Group), 0) from tbl_group").ToString());
                        foreach (var tmp in arr[i, 2].Split(","))
                        {
                            Hashtable _sub = new Hashtable();
                            _sub.Add("id_group", MaxId_Group);
                            _sub.Add("id_permit", tmp);
                            if (cnn.Insert(_sub, "tbl_group_permit") != 1)
                            {
                                return false;
                            }
                        }
                    }
                    else
                    {
                        return false;
                    }
                }
            }
            return true;
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
        public static string getIDUserbyUserName(string username, IHeaderDictionary pHeader, string URL)
        {
            string userID = "";
            #region Lấy danh sách nhân viên từ JeeAccount
            var dataJA = GetEmployeeByJA(pHeader, URL);
            if (dataJA == null)
                return "";
            foreach (var str in dataJA)
            {
                if (username.Equals(str.Username))
                {
                    userID = str.UserId.ToString();
                }
            }
            #endregion
            return userID;
        }
        public static List<AccUsernameModel> GetEmployeeByJA(IHeaderDictionary pHeader, string URL)
        {
            if (pHeader == null) return null;
            if (!pHeader.ContainsKey(HeaderNames.Authorization)) return null;
            IHeaderDictionary _d = pHeader;
            string _bearer_token;
            _bearer_token = _d[HeaderNames.Authorization].ToString();
            string link_api = URL + @$"/api/accountmanagement/usernamesByCustermerID";
            var client = new RestClient(link_api);
            var request = new RestRequest(Method.GET);
            request.AddHeader("Authorization", _bearer_token);
            IRestResponse response = client.Execute(request);
            var model = JsonConvert.DeserializeObject<BaseModel<List<AccUsernameModel>>>(response.Content);
            if (model == null)
            {
                return null;
            }
            return model.data;
        }
        public static object UpdateCustomData(IConfiguration _configuration, string URL, ObjCustomData objCustomData)
        {
            string internal_secret = WeworkLiteController.getSecretToken(_configuration);
            var content = new ObjCustomData
            {
                userId = objCustomData.userId.ToString(),
                updateField = objCustomData.updateField,
                fieldValue = objCustomData.fieldValue,
            };
            object stringContent = JsonConvert.SerializeObject(content);
            string link_api = URL + @$"/api/accountmanagement/UppdateCustomData/internal";
            var client = new RestClient(link_api);
            var request = new RestRequest(Method.POST);
            request.AddHeader("Authorization", internal_secret);
            request.AddJsonBody(stringContent);
            IRestResponse response = client.Execute(request);
            var model = JsonConvert.DeserializeObject<ResultModel>(response.Content);
            if (model == null || model.status == 0)
            {
                return null;
            }
            return model;
        }
        public static bool CheckRoleByProject(string id_project_team, UserJWT loginData, DpsConnection Conn, string ConnectionString)
        {
            bool IsAdmin = MenuController.CheckGroupAdministrator(loginData.Username, Conn, loginData.CustomerID);
            DataSet ds = GetWorkSpace(loginData, 0, 0, ConnectionString);
            if (Conn.LastError != null || ds == null)
                return false;
            SqlConditions conds = new SqlConditions();
            conds.Add("id_project_team", id_project_team);
            conds.Add("member", 1);
            conds.Add("id_role", 3);
            string sql_role = "select * from we_project_role where (where)";
            // Quyền xem công việc của người khác
            DataTable dt_view_task = Conn.CreateDataTable(sql_role, "(where)", conds);
            // Quyền giao công việc của người khác
            conds.Remove(conds["id_role"]);
            conds.Add("id_role", 4);
            DataTable dt_assign_task = Conn.CreateDataTable(sql_role, "(where)", conds);
            if (ds.Tables.Count == 3)
            {
                DataRow[] dr = ds.Tables[2].Select("id_row =" + id_project_team + " and (owner = 1 or parentowner = 1 or admin_project = 1)");
                if (dr.Length > 0 || IsAdmin || dt_view_task.Rows.Count > 0 || dt_assign_task.Rows.Count > 0)
                {
                    return true;
                }
                return false;
            }
            return false;
        }
        public static bool CheckPermitUpdate(string id_project_team, long id_role, UserJWT loginData, DpsConnection cnn, string ConnectionString)
        {
            // Kiểm tra các role admin của tk đăng nhập
            if (CheckRoleByProject(id_project_team, loginData, cnn, ConnectionString))
            {
                return true;
            }
            // Nếu không có các quyền admin, kiểm tra quyền đc cập nhật trong dự án
            string sql_role = "select * from we_project_role where (where)";
            SqlConditions conds = new SqlConditions();
            conds.Add("id_project_team", id_project_team);
            conds.Add("member", 1);
            conds.Add("id_role", id_role);
            DataTable dt = cnn.CreateDataTable(sql_role, "(where)", conds);
            if (dt.Rows.Count > 0) return true;
            return false;
        }
        public static bool CheckTaskUser(string id_project_team, long id_user, long id_work, UserJWT loginData, DpsConnection cnn, string ConnectionString)
        {
            if (IsAdminTeam(id_project_team, loginData, cnn, ConnectionString))
            {
                return true;
            }
            // kiểm tra công việc đó của mình hay không
            SqlConditions conds = new SqlConditions();
            conds.Add("userid", id_user);
            conds.Add("id_work", id_work);
            string sql = "select * from v_wework_new where id_row = @id_work and ( createdby = @userid or id_nv = @userid)";
            DataTable dt = cnn.CreateDataTable(sql, conds);
            if (dt.Rows.Count > 0)
            {
                return true;
            }
            return false;
        }
        public static bool IsTaskClosed(long id_work, DpsConnection cnn)
        {
            // kiểm tra công việc đó của mình hay không
            SqlConditions conds = new SqlConditions();
            conds.Add("id_work", id_work);
            string sql = "select * from we_work where id_row = @id_work and Disabled = 0 and closed = 1 ";
            DataTable dt = cnn.CreateDataTable(sql, conds);
            if (dt.Rows.Count > 0)
            {
                return true;
            }
            return false;
        }

        public static bool IsProjectClosed(string id_project_team, DpsConnection cnn)
        {
            // kiểm tra công việc đó của mình hay không
            SqlConditions conds = new SqlConditions();
            conds.Add("id_project_team", id_project_team);
            string sql = "select * from we_project_team where  id_row = @id_project_team and Disabled = 0 and Locked = 1 ";
            DataTable dt = cnn.CreateDataTable(sql, conds);
            if (dt.Rows.Count > 0)
            {
                return true;
            }
            return false;
        }
        public static bool IsAdminTeam(string id_project_team, UserJWT loginData, DpsConnection cnn, string ConnectionString)
        {

            bool IsAdmin = MenuController.CheckGroupAdministrator(loginData.Username, cnn, loginData.CustomerID);
            DataSet ds = GetWorkSpace(loginData, 0, 0, ConnectionString);
            if (cnn.LastError != null || ds == null)
                return false;
            if (ds.Tables.Count == 3)
            {
                DataRow[] dr = ds.Tables[2].Select("id_row =" + id_project_team + " and (owner = 1 or parentowner = 1 or admin_project = 1)");
                if (dr.Length > 0 || IsAdmin)
                {
                    return true;
                }
                return false;
            }
            return false;
        }
        public static bool ChuyenNgay(out DateTime kq, string ngay_ddMMyyy, bool isUTC = false)
        {
            string[] formats = new string[] { "yyyy-MM-dd HH:mm:ss'.'fff", "dd/MM/yyyy HH:mm:ss", "dd/MM/yyyy HH:mm", "dd/MM/yyyy HH", "dd/MM/yyyy", "yyyy/MM/dd", "yyyy/M/dd", "yyyy/M/d", "yyyy/MM/d", "yyyy-MM-dd'T'HH:mm:ss'.'fff'Z'", "O", "yyyy-MM-dd'T'HH:mm:ssZ", "yyyy-MM-dd'T'HH:mm:ss", "d/MM/yyyy", "d/M/yyyy", "dd/M/yyyy", "yyyy", "HH:mm", "MM/yyyy", "HH:mm:ss", "M/d/yyyy h:mm:ss tt" };
            kq = new DateTime();
            if (isUTC) DateTime.TryParseExact(ngay_ddMMyyy, formats, null, DateTimeStyles.AssumeUniversal, out kq);
            else DateTime.TryParseExact(ngay_ddMMyyy, formats, null, DateTimeStyles.None, out kq);
            if (new DateTime(1753, 1, 1) <= kq && kq <= new DateTime(9999, 12, 31)) return true;
            return false;
        }
        public static DateTime getLocalTime()
        {
            return DateTime.UtcNow.AddHours(7);
        }
        public static DateTime GetDateTime()
        {
            return DateTime.UtcNow;
        }
        public static string BasePath
        {
            get
            {
                return "";
            }
            set
            {
                _basePath = value;
            }
        }
        private static string _basePath;
        public static string module = "";
        public static string Pagename = "NoName";
        public static void Ghilogfile(string CustemerID, string LogEditcontent, string LogContent, string username, ControllerContext ControllerContext = null)
        {
            if (ControllerContext != null)
                Pagename = ControllerContext.ActionDescriptor.ControllerName + "_" + ControllerContext.ActionDescriptor.ActionName;
            //if("updatebykey".Equals(ControllerContext.ActionDescriptor.ActionName.ToLower()))
            //    Pagename = ControllerContext.ActionDescriptor.ControllerName + "_" + ControllerContext.ActionDescriptor.ActionName+"_"+DateTime.Now.ToString("yyyyMMddHHmmss");
            if (!"".Equals(LogEditcontent))
            {
                WriteLogByFunction(Pagename, LogEditcontent, CustemerID, username);
            }
            if (!"".Equals(LogContent))
            {
                WriteLogByDay_JeeWork(Pagename, LogContent, CustemerID, username);
                WriteLogByUser_JeeWork(Pagename, LogContent, CustemerID, username);
            }
        }
        public static string GetEditLogContent(DataTable Old_Data, DataTable New_Data)
        {
            string result = "";
            if ((Old_Data.Rows.Count > 0) && (New_Data.Rows.Count > 0))
            {
                for (int i = 0; i < Old_Data.Columns.Count; i++)
                {
                    if (Old_Data.Rows[0][i].ToString() != New_Data.Rows[0][i].ToString())
                        result += " | " + Old_Data.Columns[i].ColumnName + ": " + Old_Data.Rows[0][i].ToString() + " ^ " + New_Data.Rows[0][i].ToString();
                }
            }
            if (!"".Equals(result)) result = result.Substring(3);
            return result;
        }
        public static void WriteLogByFunction(string Pagename, string content, string CustemerID, string username)
        {
            StreamWriter w;
            string pathdir = Path.Combine(BasePath, "Logs/" + CustemerID + "/theochucnang/");
            if (!Directory.Exists(pathdir))
            {
                Directory.CreateDirectory(pathdir);
            }
            string fullpath_filename = pathdir + Pagename + ".txt";

            if (!File.Exists(fullpath_filename))
            {
                w = File.CreateText(fullpath_filename);
            }
            else w = File.AppendText(fullpath_filename);
            try
            {
                w.WriteLine(username + " - " + GetDateTime().ToString("dd/MM/yyyy HH:mm:ss") + " - " + content);
                w.Flush();
                w.Close();
            }
            catch (Exception ex)
            {
                w.Flush();
                w.Close();
            }
        }
        public static void WriteLogByDay_JeeWork(string page, string content, string CustemerID, string username)
        {
            string pathdir = Path.Combine(BasePath, "Logs/" + CustemerID + "/theongay/");
            if (!Directory.Exists(pathdir))
            {
                Directory.CreateDirectory(pathdir);
            }
            string fullpath_filename = pathdir + DateTime.Today.ToString("yyyyMMdd") + ".txt";
            StreamWriter w;
            if (!File.Exists(fullpath_filename))
            {
                w = File.CreateText(fullpath_filename);
            }
            else w = File.AppendText(fullpath_filename);
            w.WriteLine(username + " - " + GetDateTime().ToString("dd/MM/yyyy HH:mm:ss") + " - " + content);
            w.Flush();
            w.Close();
        }
        public static void WriteLogByUser_JeeWork(string page, string content, string CustemerID, string id_nv)
        {
            string pathdir = Path.Combine(BasePath, "Logs/" + CustemerID + "/theonguoidung/");
            if (!Directory.Exists(pathdir))
            {
                Directory.CreateDirectory(pathdir);
            }
            string fullpath_filename = pathdir + id_nv + ".txt";
            StreamWriter w;
            if (!File.Exists(fullpath_filename))
            {
                w = File.CreateText(fullpath_filename);
            }
            else w = File.AppendText(fullpath_filename);
            w.WriteLine(GetDateTime().ToString("dd/MM/yyyy HH:mm:ss") + " - " + content);
            w.Flush();
            w.Close();
        }
        /// <summary>
        /// Hàm lưu log sử dụng Flatform
        /// </summary>
        /// <param name="CustemerID"></param>
        /// <param name="LogEditcontent"></param>
        /// <param name="LogContent"></param>
        /// <param name="username"></param>
        /// <param name="actions"></param>
        /// <param name="data"></param>
        public static void Ghilogfile_Flatform(string CustemerID, string LogEditcontent, string LogContent, string username, string actions, object data)
        {
            if (!"".Equals(LogEditcontent))
            {
                WriteLogByFunction_Flatform(module + "_" + Pagename, LogEditcontent, CustemerID, username, actions, data);
            }
            //if (!"".Equals(LogContent))
            //{
            //    WriteLogByDay_Flatform(module + "_" + Pagename, LogContent, CustemerID, username, actions, data);
            //    WriteLogByUser_Flatform(module + "_" + Pagename, LogContent, CustemerID, username, actions, data);
            //}
        }
        public static void WriteLogByDay_Flatform(string page, string content, string CustemerID, string username, string actions, object data)
        {
            //string pathdir = Path.Combine(BasePath, "Logs/" + CustemerID + "/theongay/");
            var d2 = new ActivityLog()
            {
                username = username,
                category = content,
                action = actions,
                data = JsonConvert.SerializeObject(data)
            };
            _logger.LogInformation(JsonConvert.SerializeObject(d2));

        }
        public static void WriteLogByUser_Flatform(string page, string content, string CustemerID, string id_nv, string actions, object data)
        {
            //string pathdir = Path.Combine(BasePath, "Logs/" + CustemerID + "/theonguoidung/");
            var d2 = new ActivityLog()
            {
                username = id_nv,
                category = content,
                action = actions,
                data = JsonConvert.SerializeObject(data)
            };
            _logger.LogInformation(JsonConvert.SerializeObject(d2));
        }
        public static void WriteLogByFunction_Flatform(string Pagename, string content, string CustemerID, string username, string actions, object data)
        {
            var d2 = new ActivityLog()
            {
                username = username,
                category = content,
                action = actions,
                data = JsonConvert.SerializeObject(data)
            };
            _logger.LogInformation(JsonConvert.SerializeObject(d2));
        }
    }
}
