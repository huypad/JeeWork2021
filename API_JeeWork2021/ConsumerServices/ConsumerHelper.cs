using DPSinfra.Kafka;
using DpsLibs.Data;
using JeeWork_Core2021.Classes;
using JeeWork_Core2021.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;

namespace JeeWork_Core2021.ConsumerServices
{
    public static class ConsumerHelper
    {
        public static int createNhom(string connectStr, long idCustomer, List<string> lst_roles, int isAdmin = 0)
        {
            using (DpsConnection cnn = new DpsConnection(connectStr))
            {
                //Chuẩn bị dữ liệu để insert
                Hashtable _item = new Hashtable();
                _item.Add("GroupName", (isAdmin == 0) ? "Thành viên" : "Nhóm quản trị hệ thống");
                _item.Add("DateCreated", Common.GetDateTime());
                _item.Add("CustemerID", idCustomer);
                _item.Add("Module", 0);
                _item.Add("IsAdmin", isAdmin);
                cnn.BeginTransaction();
                if (cnn.Insert(_item, "tbl_group") == 1)
                {
                    var newId = int.Parse(cnn.ExecuteScalar("SELECT ISNULL(MAX(Id_Group), 0) FROM tbl_group").ToString());
                    foreach (var tmp in lst_roles)
                    {
                        Hashtable _sub = new Hashtable();
                        _sub.Add("Id_Group", newId);
                        _sub.Add("Id_Permit", tmp);
                        if (cnn.Insert(_sub, "tbl_group_permit") != 1)
                        {
                            cnn.RollbackTransaction();
                            return -1;
                        }
                    }
                    cnn.EndTransaction();
                    return newId;
                }
                else
                {
                    cnn.RollbackTransaction();
                    return -1;
                }
            }
        }

        public static int getIdGroup(string connectStr, long IdCustomer, int isAdmin = 0)
        {
            using (DpsConnection cnn = new DpsConnection(connectStr))
            {
                var id = cnn.ExecuteScalar($@"select Id_Group from tbl_group 
                                              where CustemerId = {IdCustomer} and IsAdmin = {isAdmin}");
                if (id == null || cnn.LastError != null) return -1;
                return int.Parse(id.ToString());
            }
        }

        public static int insertUsertoGroup(string connectStr, string userName, long idgroup)
        {
            using (DpsConnection cnn = new DpsConnection(connectStr))
            {
                Hashtable _item = new Hashtable();
                _item.Add("Username", userName);
                _item.Add("Id_Group", idgroup);
                cnn.BeginTransaction();
                if (cnn.Insert(_item, "tbl_group_account") == 1)
                {
                    cnn.EndTransaction();
                    return 1;
                }
                else
                {
                    cnn.RollbackTransaction();
                    return 0;
                }
            }
        }
        public static int removeUserfromGroup(string connectStr, string userName, long idgroup)
        {
            using (DpsConnection cnn = new DpsConnection(connectStr))
            {
                Hashtable _item = new Hashtable();
                _item.Add("Username", userName);
                _item.Add("Id_Group", idgroup);
                cnn.BeginTransaction();
                if (cnn.Delete(new SqlConditions { { "Username", userName }, { "Id_Group", 1 } }, "tbl_group_account") < 0)
                {
                    cnn.RollbackTransaction();
                    return 0;
                }
                else
                {
                    cnn.EndTransaction();
                    return 1;
                }
            }
        }

        public static void publishUpdateCustom(IProducer producer, string topic, long idUser, string roles)
        {
            //publish lại mess gửi lại jee-account
            UpdateMessage updateMess = new UpdateMessage();
            var topicUpdateAccount = topic;
            updateMess.userID = idUser;
            updateMess.updateField = "jee-work";
            var datas = new { roles = roles };
            updateMess.fieldValue = datas;
            var mess_send = Newtonsoft.Json.JsonConvert.SerializeObject(updateMess);
            var obj = new
            {
                userId = idUser,
                updateField = "jee-work",
                fieldValue = datas
            };
            var mess_send2 = Newtonsoft.Json.JsonConvert.SerializeObject(obj);
           producer.PublishAsync(topicUpdateAccount, mess_send2);
        }

        public static List<string> getRoles(string connectStr, int idnhom = 0)
        {
            using (DpsConnection cnn = new DpsConnection(connectStr))
            {
                string sql = "";
                if (idnhom == 0) //lấy role admin
                {
                    sql = @" select id_permit from tbl_permision";
                }
                else //lấy role nhóm default
                {
                    sql = $"select id_permit from tbl_group_permit where id_group = {idnhom}";
                }
                DataTable dt = cnn.CreateDataTable(sql);
                if (cnn.LastError != null || dt.Rows.Count == 0)
                {
                    return new List<string>();
                }
                List<string> role = new List<string>();
                foreach (var r in dt.AsEnumerable())
                {
                    role.Add(r["id_permit"].ToString());
                }
                return role;
            }
        }
    }
}
