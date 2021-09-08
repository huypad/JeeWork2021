using JeeWork_Core2021.Helpers;
using JeeWork_Core2021.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.Net.Http.Headers;
using Newtonsoft.Json;
using OfficeOpenXml;
using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.IdentityModel.Tokens.Jwt;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace JeeWork_Core2021.Classes
{
    public static class Ulities
    {
        public static string Remove_Multiple_Space(string p_keyword)
        {
            try
            {
                return Regex.Replace(p_keyword.Trim(), @"\s+", " ", RegexOptions.Multiline);
            }
            catch (Exception ex)
            {
                return p_keyword;
            }
        }

        public static string Remove_All_Space(string p_keyword)
        {
            try
            {
                return Regex.Replace(p_keyword.Trim(), @"\s+", "", RegexOptions.Multiline);
            }
            catch (Exception ex)
            {
                return p_keyword;
            }
        }

        public static string RandomString(int length)
        {
            Random random = new Random();

            string chars1 = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
            var Str1 = new string(Enumerable.Repeat(chars1, 1)
              .Select(s => s[random.Next(s.Length)]).ToArray());

            string chars2 = "0123456789";
            var Str2 = new string(Enumerable.Repeat(chars2, 1)
              .Select(s => s[random.Next(s.Length)]).ToArray());

            string chars3 = "!@#$%";
            var Str3 = new string(Enumerable.Repeat(chars3, 1)
              .Select(s => s[random.Next(s.Length)]).ToArray());

            string chars4 = "abcdefghijklmnopqrstvwxyz";
            var Str4 = new string(Enumerable.Repeat(chars4, 5)
              .Select(s => s[random.Next(s.Length)]).ToArray());

            return Str1 + Str2 + Str3 + Str4;
        }

        /// <summary>
        /// Format string follow length input and concat '...'
        /// sample: 123456789
        /// lenght input = 5
        /// output: 12345...
        /// </summary>
        /// <param name="k"></param>
        /// <param name="pLength"></param>
        /// <returns></returns>
        public static string TruncateString_ConcatDot(string k, int pLength = 100)
        {
            try
            {
                if (k.Length > pLength)
                    k = k.Substring(0, pLength) + "...";
                return k;
            }
            catch (Exception ex)
            {
                return k;
            }
        }

        /// <summary>
        /// Format string follow length input
        /// sample: 123456789
        /// length input = 7 , length in database is 7
        /// output: 1234... => length = 3
        /// </summary>
        /// <param name="k"></param>
        /// <param name="pLength"></param>
        /// <returns></returns>
        public static string TruncateString(string k, int pLength = 100)
        {
            try
            {
                if (k.Length > pLength)
                    k = k.Substring(0, pLength > 3 ? pLength - 3 : pLength) + "...";
                return k;
            }
            catch (Exception ex)
            {
                return k;
            }
        }

        /// <summary>
        /// Remove all unicode of string
        /// Sample: 
        /// Input = "Thế giới hòa binh"
        /// Output = "The gioi hoa binh"
        /// </summary>
        /// <param name="text">Input string</param>
        /// <returns></returns>
        public static string RemoveUnicodeFromStr(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return text;

            text = text.Normalize(NormalizationForm.FormD);
            var chars = text.Where(c => CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark).ToArray();
            return new string(chars).Normalize(NormalizationForm.FormC);
        }

        public static List<string> GET_EXTENSION_UPLOAD_FILE()
        {
            List<string> v_str = new List<string>();
            string s = ConfigurationManager_JeeWork.AppSettings.EXTENSION_UPLOAD_FILE;
            if (s != null)
                if (s.Length > 0)
                    v_str = s.Split(',').ToList();
            v_str.ForEach(x => x.Trim());
            return v_str;
        }

        /// <summary>
        /// Remove all special Character in a string
        /// </summary>
        /// <param name="input"></param>
        /// <param name="char_replTo"></param>
        /// <returns></returns>
        public static string RemoveAllSpecialChar(string input, string char_replTo = "")
        {
            try
            {
                return Regex.Replace(input, @"[^0-9a-zA-Z]+", char_replTo);
            }
            catch (Exception ex)
            {
                return input;
            }
        }

        ///// <summary>
        ///// Read content file Excel
        ///// </summary>
        ///// <param name="pathFile"></param>
        ///// <returns></returns>
        //public static BaseModel ReadExcelContent(string pathFile)
        //{
        //    try
        //    {
        //        DataTable dt = GetDataTableFromExcel(pathFile);

        //        return new BaseModel
        //        {
        //            status = 1,
        //            data = dt
        //        };
        //    }
        //    catch(Exception ex)
        //    {
        //        return new BaseModel
        //        {
        //            status = 0,
        //            error = new ErrorModel(ErrCode_Const.FILE_READ_EXCEPTION)
        //        };
        //    }
        //}

        /// <summary>
        /// Read content Excel to DataTable
        /// </summary>
        /// <param name="path"></param>
        /// <param name="hasHeader"></param>
        /// <returns></returns>
        public static DataTable GetDataTableFromExcel(string path, bool hasHeader = true)
        {
            // If you are a commercial business and have
            // purchased commercial licenses use the static property
            // LicenseContext of the ExcelPackage class:
            //ExcelPackage.LicenseContext = LicenseContext.Commercial;

            // If you use EPPlus in a noncommercial context
            // according to the Polyform Noncommercial license:
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            using (var pck = new OfficeOpenXml.ExcelPackage())
            {
                using (var stream = File.OpenRead(path))
                {
                    pck.Load(stream);
                }
                var ws = pck.Workbook.Worksheets.First();
                DataTable tbl = new DataTable();
                string ColNameDT = "";
                int indCol = 0;
                foreach (var firstRowCell in ws.Cells[1, 1, 1, ws.Dimension.End.Column])
                {
                    ColNameDT = hasHeader ? firstRowCell.Text : string.Format("Column {0}", firstRowCell.Start.Column);
                    if (tbl.Columns.Contains(ColNameDT))
                        ColNameDT += (++indCol);
                    tbl.Columns.Add(ColNameDT);
                }

                var startRow = hasHeader ? 2 : 1;

                for (int rowNum = startRow; rowNum <= ws.Dimension.End.Row; rowNum++)
                {
                    var wsRow = ws.Cells[rowNum, 1, rowNum, ws.Dimension.End.Column];
                    DataRow row = tbl.Rows.Add();
                    foreach (var cell in wsRow)
                    {
                        row[cell.Start.Column - 1] = cell.Text;
                    }
                }
                return tbl;
            }
        }
        public static UserJWT GetUserByHeader(IHeaderDictionary pHeader)
        {
            try
            {
                if (pHeader == null) return null;
                if (!pHeader.ContainsKey(HeaderNames.Authorization)) return null;
                IHeaderDictionary _d = pHeader;
                string bearer_token, username, customdata;
                bearer_token = _d[HeaderNames.Authorization].ToString().Replace("Bearer ", "");
                var handler = new JwtSecurityTokenHandler();
                var tokenS = handler.ReadToken(bearer_token) as JwtSecurityToken;
                username = tokenS.Claims.Where(x => x.Type == "username").FirstOrDefault().Value;
                customdata = tokenS.Claims.Where(x => x.Type == "customdata").FirstOrDefault().Value;
                if (string.IsNullOrEmpty(username))
                    return null;
                UserJWT q = new UserJWT();
                q.Username = username;
                q.customdata = JsonConvert.DeserializeObject<CustomData>(customdata);
                if(q.customdata.jeeAccount == null)
                    return q;
                else
                {
                    q.UserID = q.customdata.jeeAccount.userID;
                    q.CustomerID = long.Parse(q.customdata.jeeAccount.customerID);
                }
                return q;
            }
            catch (Exception ex)
            {
                return null;
            }
        }
        public static string GetAccessTokenByHeader(IHeaderDictionary pHeader)
        {
            try
            {
                if (pHeader == null) return null;
                if (!pHeader.ContainsKey(HeaderNames.Authorization)) return null;

                IHeaderDictionary _d = pHeader;
                string _bearer_token, _user;
                _bearer_token = _d[HeaderNames.Authorization].ToString().Replace("Bearer ", "");

                return _bearer_token;
            }
            catch (Exception ex)
            {
                return null;
            }
        }
        public static string GetUsernameByHeader(IHeaderDictionary pHeader)
        {
            try
            {
                if (pHeader == null) return null;
                if (!pHeader.ContainsKey(HeaderNames.Authorization)) return null;

                IHeaderDictionary _d = pHeader;
                string _bearer_token, _user;
                _bearer_token = _d[HeaderNames.Authorization].ToString().Replace("Bearer ", "");
                var handler = new JwtSecurityTokenHandler();
                var tokenS = handler.ReadToken(_bearer_token) as JwtSecurityToken;

                _user = tokenS.Claims.Where(x => x.Type == "username").FirstOrDefault().Value;
                if (string.IsNullOrEmpty(_user))
                    return null;

                var User = _user;
                return User;
            }
            catch (Exception ex)
            {
                return null;
            }
        }


    }
}
