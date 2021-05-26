using DPSinfra.Kafka;
using DpsLibs.Data;
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
        public static int insertUsertoAdmin(string connectStr, string userName, long idgroup)
        {
            using (DpsConnection cnn = new DpsConnection(connectStr))
            {
                Hashtable _item = new Hashtable();
                _item.Add("Username", userName);
                _item.Add("Id_group", idgroup);

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

        public static int removeUserfromAdmin(string connectStr, string userName)
        {
            using (DpsConnection cnn = new DpsConnection(connectStr))
            {
                Hashtable _item = new Hashtable();
                _item.Add("Username", userName);
                _item.Add("Id_group", 1);

                cnn.BeginTransaction();
                if (cnn.Delete(new SqlConditions { { "Username", userName }, { "Id_group", 1 } }, "") < 0)
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
            //publish lại mess gửi lại jee-work
            UpdateMessage updateMess = new UpdateMessage();
            var topicUpdateAccount = topic;
            updateMess.userID = idUser;
            updateMess.updateField = "jee-work";
            updateMess.fieldValue.WeWorkRoles = roles;

            var mess_send = Newtonsoft.Json.JsonConvert.SerializeObject(updateMess);
            producer.PublishAsync(topicUpdateAccount, mess_send);
        }

        public static List<string> getRolesAdmin(string connectStr)
        {
            using (DpsConnection cnn = new DpsConnection(connectStr))
            {
                string sql = @" select  IdRole from HT_GroupUser g
                                join    HT_GroupUser_Roles r on g.IdGroupUser = r.IdGroupUser
                                where   g.IdGroupUser = 1";
                DataTable dt = cnn.CreateDataTable(sql);
                List<string> role = new List<string>();
                DataTable Quyenmacdinh = cnn.CreateDataTable(@$"select Id_permit from tbl_permision");
                if (cnn.LastError != null)
                {
                    return new List<string>();
                }
                else
                {
                    if (dt.Rows.Count == 0)
                    {
                        foreach (var r in Quyenmacdinh.AsEnumerable())
                        {
                            role.Add(r["Id_permit"].ToString());
                        }
                    }
                    else
                    {
                        foreach (var r in dt.AsEnumerable())
                        {
                            role.Add(r["Id_permit"].ToString());
                        }
                    }
                }
                return role;
            }
        }
    }
}
