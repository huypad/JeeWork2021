using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DPSinfra.Kafka;
using Newtonsoft.Json;
using DPSinfra.Utils;
using DPSinfra.ConnectionCache;
using Microsoft.Extensions.Logging;
using DPSinfra.Logger;
using System.Text;
using JeeWork_Core2021.Models;
using Newtonsoft.Json.Linq;
using System.Data;
using DpsLibs.Data;
using JeeWork_Core2021.Controllers.Wework;
using System.Collections;
using JeeWork_Core2021.Controller;
using DPSinfra.Notifier;

namespace API_JeeWork2021.Classes
{
    public class kafkaAutomationService : IHostedService
    {
        private IConfiguration _config;
        private INotifier _notifier;
        private Consumer testSoLuong;
        private IProducer _producer;
        private IConnectionCache _cache;
        private readonly ILogger<kafkaAutomationService> _logger;
        private GetDateTime UTCdate = new GetDateTime();
        public kafkaAutomationService(IConfiguration config, IProducer producer, IConnectionCache connectionCache, ILogger<kafkaAutomationService> logger, INotifier notifier)
        {
            _cache = connectionCache;
            _producer = producer;
            _config = config;
            _notifier = notifier;
            testSoLuong = new Consumer(_config, "test-sls");
            _logger = logger;
        }
        public async Task StartAsync(CancellationToken cancellationToken)
        {
            var topicnhan = _config.GetValue<string>("KafkaConfig:TopicProduce:JeeWorkAutomationService");
            _ = Task.Run(() =>
            {
                testSoLuong.SubscribeAsync(topicnhan, Get_Automation);
            }, cancellationToken);
            await Task.CompletedTask;
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            await testSoLuong.closeAsync();
        }
        public void Get_Automation(string value)
        {
            Console.OutputEncoding = Encoding.UTF8;
            var data = JsonConvert.DeserializeObject<Post_Automation_Model>(value);
            string connectionString = JeeWorkLiteController.getConnectionString(_cache, data.customerid, _config);
            ExecuteAutomation(data, connectionString);
        }
        public bool ExecuteAutomation(Post_Automation_Model data, string connectionString)
        {
            bool result = true;
            DataTable dt_execute = new DataTable();
            DataTable dt_auto = new DataTable();
            string sqlq_action = "", sqlq_execute = "";
            sqlq_action = "select rowid, title, description, listid, departmentid, status, eventid, condition, actionid, data " +
                "from automationlist " +
                "where disabled = 0 and Status = 1 and eventid = " + data.eventid;
            using (DpsConnection cnn = new DpsConnection(connectionString))
            {
                SqlConditions conds = new SqlConditions();
                conds.Add("disabled", 0);
                conds.Add("id_row", data.taskid);
                sqlq_execute = "select id_row " +
                    "from we_work where id_parent is null and (where)";
                if (data.departmentid > 0)
                {
                    sqlq_execute += $" and id_project_team in (select id_row from we_project_team where id_department  in (select id_row from we_department where Disabled = 0 and (id_row = {data.departmentid} or id_row in (select ParentID from we_department where Disabled = 0 and id_row = {data.departmentid}))))";
                    sqlq_action += $" and departmentid in (select id_row from we_department where Disabled = 0 and (id_row = {data.departmentid} or id_row in (select ParentID from we_department where Disabled = 0 and id_row = {data.departmentid})))";
                }
                else
                {
                    sqlq_execute += " and id_project_team = " + data.listid;
                    sqlq_action += " and listid = " + data.listid;
                }
                // Danh sách cần thực hiện (update)
                dt_execute = cnn.CreateDataTable(sqlq_execute, "(where)", conds);
                // Danh sách hành động theo sự kiện và departmentid/listid
                dt_auto = cnn.CreateDataTable(sqlq_action);
                if (cnn.LastError != null || dt_auto == null)
                {
                    return !result;
                }
                if (dt_execute.Rows.Count <= 0)
                    return result;
                if (dt_auto.Rows.Count > 0)
                {
                    foreach (DataRow dr in dt_auto.Rows)
                    {
                        if (get_condition_by_events(dr, data.data_input, cnn)) // Kiểm tra event để thực thi hành động
                        {
                            long actionid = long.Parse(dr["actionid"].ToString());
                            long autoid = long.Parse(dr["rowid"].ToString());
                            string data_auto = dr["data"].ToString();
                            string columnname = "id_project_team";
                            string TitleKey = "";
                            int TemplateID = 0;
                            switch (actionid)
                            {
                                case 1:
                                case 7:
                                    DataTable dt_sub = cnn.CreateDataTable(@"select sub.rowid, autoid, subactionid, value,sublist.TableName from automation_subaction sub
join Automation_SubActionList sublist on sub.SubActionID = sublist.RowID  where autoid =" + autoid + "");
                                    if (dt_sub.Rows.Count > 0)
                                    {
                                        process_automation_subaction(dt_sub, get_list_id(dt_execute), cnn, connectionString, data.customerid);
                                    }
                                    break;
                                case 4: // comment 
                                    insertComment(data.userid, data_auto, get_list_id(dt_execute), cnn);
                                    break;
                                case 8:
                                    columnname = "estimates";
                                    TitleKey = "ww_capnhatthoigianuoctinh";
                                    TemplateID = 26;
                                    goto case 14;
                                case 9:
                                    columnname = "status";
                                    TitleKey = "ww_capnhattrangthaicongviec";
                                    TemplateID = 21;
                                    goto case 14;
                                case 10:
                                    columnname = "clickup_prioritize";
                                    TitleKey = "ww_thaydoidouutiencongviec";
                                    TemplateID = 25;
                                    goto case 14;
                                case 11:
                                    columnname = "deadline";
                                    TitleKey = "ww_chinhsuadeadline";
                                    TemplateID = 12;
                                    goto case 14;
                                case 12:
                                    columnname = "start_date";
                                    TitleKey = "ww_chinhsuathoigianbatdau";
                                    TemplateID = 27;
                                    goto case 14;
                                case 13:
                                case 14:
                                    doitinhtrang(columnname, data_auto, get_list_id(dt_execute), cnn, connectionString, data.customerid, TemplateID, TitleKey);
                                    break;
                                case 5:
                                    TitleKey = "ww_xoacongviec";
                                    TemplateID = 15;
                                    doitinhtrang("Disabled", "1", get_list_id(dt_execute), cnn, connectionString, data.customerid, TemplateID, TitleKey);
                                    break;
                                case 6:
                                    DuplicateTask(data_auto, get_list_id(dt_execute), cnn);
                                    break;
                                case 2:
                                case 3:
                                    CreatedTask(dr["rowid"].ToString(), data.userid, cnn, connectionString, data.customerid);
                                    break;
                                default:
                                    break;
                            }
                        }
                    }
                }
            }
            return result;
        }
        public static string get_list_id(DataTable dt)
        {
            string list_id_task = "";
            foreach (DataRow r in dt.Rows)
            {
                list_id_task += "," + r["id_row"];
            }
            if (!"".Equals(list_id_task))
                list_id_task = list_id_task.Substring(1);
            return list_id_task;
        }
        public bool process_automation_subaction(DataTable dt_sub, string condition_update, DpsConnection cnn, string ConnectionString, long CustomerID)
        {
            var sub3 = dt_sub.AsEnumerable().Where(x => x["subactionid"].ToString() == "3").FirstOrDefault();
            if (sub3 != null && sub3["value"].ToString() == "true")
            {

                string[] listTask = condition_update.Split(",");
                for (int i = 0; i < listTask.Length; i++)
                {
                    #region lấy danh sách tài khoản trước khi xóa để gửi thông báo
                    DataTable dt = cnn.CreateDataTable("select * from we_work_user where disabled = 0 and loai = 1 and id_work = " + listTask[i]);
                    #endregion
                    Hashtable val2 = new Hashtable();
                    val2["UpdatedDate"] = UTCdate.Date;
                    val2["UpdatedBy"] = 0;
                    val2["Disabled"] = 1;
                    SqlConditions cond = new SqlConditions();
                    cond.Add("id_work", listTask[i]);
                    cond.Add("loai", 1);
                    if (cnn.Update(val2, cond, sub3["tablename"].ToString()) < 0)
                    {
                        cnn.RollbackTransaction();
                        return false;
                    }
                    cnn.EndTransaction();
                    if (dt.Rows.Count > 0)
                    {
                        foreach (DataRow dr in dt.Rows)
                        {
                            var users = new List<long> { long.Parse(dr["id_user"].ToString()) };
                            SendNotifyAndMailAssign(22, ConnectionString, users, long.Parse(listTask[i]), CustomerID, "ww_xoaassign");
                        }
                    }
                }
            }
            else
            {
                foreach (DataRow dr in dt_sub.Rows)
                {
                    long SubActionID = long.Parse(dr["subactionid"].ToString());
                    string TableName = dr["tablename"].ToString();
                    string[] values = dr["value"].ToString().Split(",");
                    string[] listTask = condition_update.Split(",");
                    Hashtable has = new Hashtable();
                    switch (SubActionID)
                    {
                        case 1: // Thêm người thực hiện (we_work_user)
                            foreach (string item in values)
                            {
                                for (int i = 0; i < listTask.Length; i++)
                                {
                                    #region kiểm tra người đó có trong công việc hay chưa
                                    DataTable dtu = cnn.CreateDataTable("select * from we_work_user where disabled = 0 and loai = 1 and id_work = " + listTask[i] + " and id_user = " + item);
                                    #endregion
                                    SqlConditions sqlcond123 = new SqlConditions();
                                    sqlcond123.Add("id_work", listTask[i]);
                                    sqlcond123.Add("id_user", item);
                                    sqlcond123.Add("loai", 1);
                                    sqlcond123.Add("Disabled", 0);
                                    var sql = @"select * from we_work_user where id_work = @id_work and id_user = @id_user and loai = @loai and Disabled = @Disabled";
                                    DataTable dtG = cnn.CreateDataTable(sql, sqlcond123);
                                    if (dtG.Rows.Count == 0)
                                    {
                                        has = new Hashtable();
                                        has["id_work"] = listTask[i];
                                        has["createddate"] = UTCdate.Date;
                                        has["createdby"] = 0;
                                        has["id_user"] = item;
                                        has["loai"] = 1;
                                        if (cnn.Insert(has, TableName) != 1)
                                        {
                                            cnn.RollbackTransaction();
                                            return false;
                                        }
                                    }
                                    cnn.EndTransaction();
                                    if (dtu.Rows.Count == 0)
                                    {
                                        var users = new List<long> { long.Parse(item) };
                                        SendNotifyAndMailAssign(10, ConnectionString, users, long.Parse(listTask[i]), CustomerID, "ww_assign");
                                    }
                                }
                            }
                            break;
                        case 2: // Xóa người thực hiện -- id_work=$objectid$,id_user in ($list_value$),loai=1
                            foreach (string item in values)
                            {
                                for (int i = 0; i < listTask.Length; i++)
                                {
                                    #region lấy danh sách tài khoản trước khi xóa để gửi thông báo
                                    DataTable dtu = cnn.CreateDataTable("select * from we_work_user where disabled = 0 and loai = 1 and id_work = " + listTask[i] + " and id_user = " + item);
                                    #endregion
                                    Hashtable val2 = new Hashtable();
                                    val2["UpdatedDate"] = UTCdate.Date;
                                    val2["UpdatedBy"] = 0;
                                    val2["Disabled"] = 1;
                                    SqlConditions cond = new SqlConditions();
                                    cond.Add("id_work", listTask[i]);
                                    cond.Add("id_user", item);
                                    if (cnn.Update(val2, cond, TableName) < 0)
                                    {
                                        cnn.RollbackTransaction();
                                        return false;
                                    }
                                    cnn.EndTransaction();
                                    if (dtu.Rows.Count > 0)
                                    {
                                        var users = new List<long>() { long.Parse(item) };
                                        SendNotifyAndMailAssign(22, ConnectionString, users, long.Parse(listTask[i]), CustomerID, "ww_xoaassign");
                                    }
                                }
                            }
                            break;
                        case 4: // Gán lại người thực hiện
                            for (int i = 0; i < listTask.Length; i++)
                            {
                                #region lấy danh sách tài khoản trước khi xóa để gửi thông báo
                                DataTable dt = cnn.CreateDataTable("select * from we_work_user where disabled = 0 and loai = 1 and id_work = " + listTask[i]);
                                #endregion
                                Hashtable val2 = new Hashtable();
                                val2["UpdatedDate"] = UTCdate.Date;
                                val2["UpdatedBy"] = 0;
                                val2["Disabled"] = 1;
                                SqlConditions cond = new SqlConditions();
                                cond.Add("id_work", listTask[i]);
                                if (cnn.Update(val2, cond, TableName) < 0)
                                {
                                    cnn.RollbackTransaction();
                                    return false;
                                }
                                cnn.EndTransaction();
                                if (dt.Rows.Count > 0)
                                {
                                    foreach (DataRow dru in dt.Rows)
                                    {
                                        var users = new List<long> { long.Parse(dru["id_user"].ToString()) };
                                        SendNotifyAndMailAssign(22, ConnectionString, users, long.Parse(listTask[i]), CustomerID, "ww_xoaassign");
                                    }
                                }
                            }
                            // gắn lại
                            foreach (string item in values)
                            {
                                for (int i = 0; i < listTask.Length; i++)
                                {
                                    SqlConditions sqlcond123 = new SqlConditions();
                                    sqlcond123.Add("id_work", listTask[i]);
                                    sqlcond123.Add("id_user", item);
                                    sqlcond123.Add("loai", 1);
                                    sqlcond123.Add("Disabled", 0);
                                    var sql = @"select * from we_work_user where id_work = @id_work and id_user = @id_user and loai = @loai and Disabled = @Disabled";
                                    DataTable dtG = cnn.CreateDataTable(sql, sqlcond123);
                                    if (dtG.Rows.Count == 0)
                                    {
                                        has = new Hashtable();
                                        has["id_work"] = listTask[i];
                                        has["createddate"] = UTCdate.Date;
                                        has["createdby"] = 0;
                                        has["id_user"] = item;
                                        has["loai"] = 1;
                                        if (cnn.Insert(has, TableName) != 1)
                                        {
                                            cnn.RollbackTransaction();
                                            return false;
                                        }
                                    }
                                    cnn.EndTransaction();
                                    var users = new List<long> { long.Parse(item) };
                                    SendNotifyAndMailAssign(10, ConnectionString, users, long.Parse(listTask[i]), CustomerID, "ww_assign");

                                }
                            }
                            break;
                        case 5: // Thêm tag (we_work_tag)
                            foreach (var item in values)
                            {
                                for (int i = 0; i < listTask.Length; i++)
                                {
                                    var f = cnn.ExecuteScalar("select count(*) from we_work_tag where disabled=0 and id_work=" + listTask[i] + " and id_tag=" + item);
                                    Hashtable val2 = new Hashtable();
                                    if (int.Parse(f.ToString()) > 0) // Tag đã có => Delete
                                    { }
                                    else
                                    {
                                        val2 = new Hashtable();
                                        val2["id_work"] = listTask[i];
                                        val2["CreatedDate"] = UTCdate.Date;
                                        val2["CreatedBy"] = 0;
                                        val2["id_tag"] = item;
                                        if (cnn.Insert(val2, "we_work_tag") != 1)
                                        {
                                            cnn.RollbackTransaction();
                                            return false;
                                        }
                                    }
                                    cnn.EndTransaction();
                                    // get danh sách người làm công việc đó và người theo dõi
                                    var sql = $@"select * from we_work_user where id_work = {listTask[i]} and Disabled = 0";
                                    DataTable dtu = cnn.CreateDataTable(sql);
                                    if (dtu.Rows.Count > 0)
                                    {
                                        var users = dtu.AsEnumerable().Select(x => long.Parse(x["id_user"].ToString())).ToList();
                                        SendNotifyAndMailAssign(34, ConnectionString, users, long.Parse(listTask[i]), CustomerID, "ww_capnhattag");
                                    }
                                }
                            }
                            break;
                        case 6: // Xóa tag
                            foreach (var item in values)
                            {
                                for (int i = 0; i < listTask.Length; i++)
                                {
                                    var f = cnn.ExecuteScalar("select count(*) from we_work_tag where disabled=0 and id_work=" + listTask[i] + " and id_tag=" + item);
                                    Hashtable val2 = new Hashtable();
                                    if (int.Parse(f.ToString()) > 0) // Tag đã có => Delete
                                    {
                                        val2 = new Hashtable();
                                        val2["UpdatedDate"] = UTCdate.Date;
                                        val2["UpdatedBy"] = 0;
                                        val2["Disabled"] = 1;
                                        SqlConditions cond = new SqlConditions();
                                        cond.Add("id_work", listTask[i]);
                                        cond.Add("id_tag", item);
                                        if (cnn.Update(val2, cond, "we_work_tag") <= 0)
                                        {
                                            cnn.RollbackTransaction();
                                            return false;
                                        }
                                    }

                                    cnn.EndTransaction();
                                    // get danh sách người làm công việc đó và người theo dõi
                                    var sql = $@"select * from we_work_user where id_work = {listTask[i]} and Disabled = 0";
                                    DataTable dtu = cnn.CreateDataTable(sql);
                                    if (dtu.Rows.Count > 0)
                                    {
                                        var users = dtu.AsEnumerable().Select(x => long.Parse(x["id_user"].ToString())).ToList();
                                        SendNotifyAndMailAssign(34, ConnectionString, users, long.Parse(listTask[i]), CustomerID, "ww_capnhattag");
                                    }
                                }
                            }
                            break;
                        default:
                            break;
                    }
                }
            }
            return true;

        }
        public static bool get_condition_by_events(DataRow dr_auto, string conditions_input, DpsConnection cnn)
        {
            bool result = false;

            long eventid = long.Parse(dr_auto["eventid"].ToString());
            switch (eventid)
            {
                case 1:
                case 2:
                    string[] conditions = dr_auto["condition"].ToString().Split(";");
                    List<string> from = conditions[0].Replace("From:", "").Split(",").ToList();
                    List<string> to = conditions[1].Replace("To:", "").Split(",").ToList();
                    if ("any".Equals(from) && "any".Equals(to))
                        result = true;
                    else
                    {
                        bool isfrom = true, isto = true;
                        if (!"any".Equals(from[0]))
                        {
                            isfrom = from.Contains(conditions_input.Split(",")[0]);
                        }
                        if (!"any".Equals(to[0]))
                        {
                            isto = to.Contains(conditions_input.Split(",")[1]);
                        }
                        if (isfrom && isto)
                            return true;
                    }
                    break;
                case 3:
                case 4:
                case 7:
                case 10:
                case 11:
                    return true;
                case 5:
                case 6:
                case 8:
                    string list = dr_auto["condition"].ToString();
                    if (list == "any") return true;
                    return list.Contains(conditions_input);
            }

            return result;
        }
        public static bool insertComment(long userid, string data, string condition_update, DpsConnection cnn)
        {
            GetDateTime UTCdate = new GetDateTime();
            string[] listTask = condition_update.Split(",");
            for (int i = 0; i < listTask.Length; i++)
            {
                Hashtable val = new Hashtable();
                val.Add("comment", data);
                val.Add("object_type", 1);
                val.Add("object_id", listTask[i]);
                val.Add("CreatedDate", UTCdate.Date);
                val.Add("CreatedBy", userid);
                if (cnn.Insert(val, "we_comment") != 1)
                {
                    cnn.RollbackTransaction();
                    return false;
                }
            }
            return true;
        }
        public bool doitinhtrang(string columnname, string data, string condition_update, DpsConnection cnn, string ConnectionString, long CustomerID, int TemplateID, string TitleKey)
        {
            if (columnname == "start_date" || columnname == "deadline")
            {
                string[] list = data.Split(";");
                int type = int.Parse(list[0]);
                string value = list[1];
                if (type == 1)
                {
                    data = UTCdate.Date.AddDays(int.Parse(value)).ToString();
                }
                else if (type == 2)
                {
                    data = UTCdate.Date.ToString();
                }
                else
                {
                    data = DateTime.Parse(value).ToString();
                }
            }
            string[] listTask = condition_update.Split(",");
            for (int i = 0; i < listTask.Length; i++)
            {
                Hashtable val = new Hashtable();
                if (columnname == "status")
                {
                    if (!kiemtratinhtrang(data, listTask[i], cnn)) return false;
                    bool isTodo = long.Parse(cnn.ExecuteScalar("select count(*) from we_status where id_row = " + data + " and isTodo = 1").ToString()) > 0;
                    bool isFinal = long.Parse(cnn.ExecuteScalar("select count(*) from we_status where id_row = " + data + " and IsFinal = 1").ToString()) > 0;
                    if (isTodo)
                    {
                        val.Add("activated_date", UTCdate.Date); // date update isTodo = 1
                        val.Add("activated_by", 0); // user update isTodo = 1
                    }
                    if (isFinal)
                    {
                        val.Add("end_date", UTCdate.Date);
                        val.Add("closed_date", UTCdate.Date); // date update isFilnal = 1
                        val.Add("closed_by", 0); // user update isFilnal = 1
                    }
                    val.Add("state_change_date", UTCdate.Date); // Ngày thay đổi trạng thái (Bất kỳ cập nhật trạng thái là thay đổi)
                }
                val.Add("UpdatedDate", UTCdate.Date);
                val.Add("UpdatedBy", 0);
                val.Add(columnname, data);
                SqlConditions sqlcond = new SqlConditions();
                sqlcond.Add("id_row", listTask[i]);
                sqlcond.Add("disabled", 0);
                if (cnn.Update(val, sqlcond, "we_work") != 1)
                {
                    cnn.RollbackTransaction();
                    return false;
                }

                if (columnname == "id_project_team")
                { // chuyển qua dự án mới thì update lại status
                    string sqlq1 = "select ISNULL((select id_row from we_status where disabled=0 and Position = 1 and id_project_team = " + data + "),0)";
                    var statusID = long.Parse(cnn.ExecuteScalar(sqlq1).ToString());
                    Hashtable val2 = new Hashtable();
                    val2.Add("status", statusID);
                    SqlConditions cond = new SqlConditions();
                    cond.Add("id_row", listTask[i]);
                    if (cnn.Update(val2, cond, "we_work") <= 0)
                    {
                        cnn.RollbackTransaction();
                        return false;
                    }
                }


                cnn.EndTransaction();
                // get danh sách người làm công việc đó và người theo dõi
                var sql = $@"select * from we_work_user where id_work = {listTask[i]} and Disabled = 0";
                DataTable dtu = cnn.CreateDataTable(sql);
                if (dtu.Rows.Count > 0)
                {
                    var users = dtu.AsEnumerable().Select(x => long.Parse(x["id_user"].ToString())).ToList();
                    SendNotifyAndMailAssign(TemplateID, ConnectionString, users, long.Parse(listTask[i]), CustomerID, TitleKey);
                }
            }
            return true;
        }
        public static bool kiemtratinhtrang(string data, string taskid, DpsConnection cnn)
        {
            //kiểm tra data status đó có trong projectteam của task đó hay không
            string sqlq1 = $"select isnull((select id_row from we_status where id_project_team = (select id_project_team from we_work where id_row = {taskid}) and id_row = {data}),0)";
            var statusID = long.Parse(cnn.ExecuteScalar(sqlq1).ToString());
            return statusID > 0;
        }
        public static bool DuplicateTask(string data, string condition_update, DpsConnection cnn)
        {
            string[] listTask = condition_update.Split(",");
            for (int i = 0; i < listTask.Length; i++)
            {
                string sqlq = $@"INSERT INTO [dbo].[we_work_duplicate]
           ([id]
           ,[title]
           ,[description]
		   ,[id_project_team]
           ,[deadline]
           ,[duplicate_child]
           ,[urgent]
           ,[start_date]
           ,[required_result]
           ,[type]
		   ,[CreatedBy]
           ,[CreatedDate])
     select id_row,title,description,{data},deadline,1,1,start_date,1,2,0,GETUTCDATE()
	 from v_wework_clickup_new where id_row = " + listTask[i];
                cnn.BeginTransaction();
                if (cnn.ExecuteNonQuery(sqlq) < 1)
                {
                    cnn.RollbackTransaction();
                    return false;
                }

                long idc = long.Parse(cnn.ExecuteScalar("select IDENT_CURRENT('we_work_duplicate')").ToString());
                string sql = "exec DuplicateWork " + idc;

                DataTable dt = cnn.CreateDataTable(sql);
                if (cnn.LastError != null || dt == null || dt.Rows.Count == 0)
                {
                    cnn.RollbackTransaction();
                    return false;
                }

                string sqlq1 = "select ISNULL((select id_row from we_status where disabled=0 and Position = 1 and id_project_team = " + data + "),0)";
                long workID_New = long.Parse(cnn.ExecuteScalar("select max(id_row) from we_work where disabled = 0").ToString());
                var statusID = long.Parse(cnn.ExecuteScalar(sqlq1).ToString());
                Hashtable val = new Hashtable();
                val.Add("status", statusID);
                SqlConditions cond = new SqlConditions();
                cond.Add("id_row", workID_New);
                cnn.BeginTransaction();
                if (cnn.Update(val, cond, "we_work") <= 0)
                {
                    cnn.RollbackTransaction();
                    return false;
                }
                cnn.EndTransaction();

            }
            return true;
        }

        public bool CreatedTask(string AutoID, long userid, DpsConnection cnn, string ConnectionString, long CustomerID)
        {
            string sqlq = @"select at.* from Automation_Task at
where AutoID = " + AutoID + " order by RowID desc";
            DataTable dt = cnn.CreateDataTable(sqlq);
            if (cnn.LastError != null || dt.Rows.Count == 0)
            {
                return false;
            }
            DataRow dr = dt.Rows[0];
            GetDateTime UTCdate = new GetDateTime();
            Hashtable val = new Hashtable();
            if (!string.IsNullOrEmpty(dr["id_parent"].ToString()))
                val.Add("id_parent", dr["id_parent"]);
            val.Add("title", dr["title"]);
            if (string.IsNullOrEmpty(dr["description"].ToString()))
                val.Add("description", "");
            else
                val.Add("description", dr["description"]);
            val.Add("id_project_team", dr["id_project_team"]);


            if (int.Parse(cnn.ExecuteScalar("select count(*) from we_status where disabled=0 and id_row = " + dr["status"] + "and id_project_team = " + dr["id_project_team"]).ToString()) == 0)
            {
                string sqlq1 = "select ISNULL((select id_row from we_status where disabled=0 and Position = 1 and id_project_team = " + dr["id_project_team"] + "),0)";
                var statusID = long.Parse(cnn.ExecuteScalar(sqlq1).ToString());
                val.Add("status", statusID);
            }
            else
            {
                val.Add("status", dr["status"]);
            }
            if (int.Parse(dr["StartDate_Type"].ToString()) > 0)
            {
                switch (int.Parse(dr["StartDate_Type"].ToString()))
                {
                    case 1:
                        val.Add("start_date", UTCdate.Date.AddDays(int.Parse(dr["start_date"].ToString())));
                        break;
                    case 2:
                        val.Add("start_date", UTCdate.Date);
                        break;
                    case 3:
                        val.Add("start_date", dr["start_date"]);
                        break;
                }
            }
            if (int.Parse(dr["Deadline_Type"].ToString()) > 0)
            {
                switch (int.Parse(dr["Deadline_Type"].ToString()))
                {
                    case 1:
                        val.Add("deadline", UTCdate.Date.AddDays(int.Parse(dr["deadline"].ToString())));
                        break;
                    case 2:
                        val.Add("deadline", UTCdate.Date);
                        break;
                    case 3:
                        val.Add("deadline", dr["deadline"]);
                        break;
                }
            }

            //if (!string.IsNullOrEmpty(dr["id_group"].ToString()))
            //    val.Add("id_group", dr["id_group"].ToString());
            val.Add("CreatedDate", UTCdate.Date);
            val.Add("CreatedBy", userid);
            val.Add("clickup_prioritize", dr["priority"]);
            cnn.BeginTransaction();
            if (cnn.Insert(val, "we_work") != 1)
            {
                cnn.RollbackTransaction();
                return false;
            }
            long weworkID = long.Parse(cnn.ExecuteScalar("select IDENT_CURRENT('we_work')").ToString());
            #region insert user vào công việc mới tạo
            DataTable dtUser = cnn.CreateDataTable("select * from Automation_Task_User where TaskID = " + dr["RowID"]);
            if (dtUser.Rows.Count > 0)
            {

                Hashtable valU = new Hashtable();
                valU["id_work"] = weworkID;
                valU["CreatedDate"] = UTCdate.Date;
                valU["CreatedBy"] = userid;
                foreach (DataRow user in dtUser.Rows)
                {
                    valU["id_user"] = user["id_user"];
                    valU["loai"] = user["loai"];
                    if (cnn.Insert(valU, "we_work_user") != 1)
                    {
                        cnn.RollbackTransaction();
                        return false;
                    }
                }

            }
            #endregion
            //Insert người follow cho từng tình trạng của công việc
            DataTable dt_status = JeeWorkLiteController.StatusDynamic(long.Parse(dr["id_project_team"].ToString()), new List<AccUsernameModel>(), cnn);
            if (dt_status.Rows.Count > 0)
            {
                foreach (DataRow item in dt_status.Rows)
                {
                    val = new Hashtable();
                    val.Add("id_project_team", dr["id_project_team"].ToString());
                    val.Add("workid", weworkID);
                    val.Add("statusid", item["id_row"]);
                    if (string.IsNullOrEmpty(item["follower"].ToString()))
                        val.Add("checker", DBNull.Value);
                    else
                        val.Add("checker", item["follower"]);
                    val.Add("createddate", UTCdate.Date);
                    val.Add("createdby", 0);
                    if (cnn.Insert(val, "we_work_process") != 1)
                    {
                        cnn.RollbackTransaction();
                        return false;
                    }
                    long processid = long.Parse(cnn.ExecuteScalar("select IDENT_CURRENT('we_work_process')").ToString());
                    val = new Hashtable();
                    val.Add("processid", processid);
                    if (string.IsNullOrEmpty(item["follower"].ToString()))
                    {
                        val.Add("new_checker", DBNull.Value);
                    }

                    val.Add("createddate", UTCdate.Date);
                    val.Add("createdby", 0);
                    if (cnn.Insert(val, "we_work_process_log") != 1)
                    {
                        cnn.RollbackTransaction();
                        return false;
                    }
                }
            }

            cnn.EndTransaction();
            var users = dtUser.AsEnumerable().Select(x => long.Parse(x["id_user"].ToString())).ToList();
            SendNotifyAndMailAssign(10, ConnectionString, users, weworkID, CustomerID, "ww_assign");
            return true;
        }

        public void SendNotifyAndMail(long id_project_team, string ConnectionString, long id_user, long id_work, long CustemerID, bool isAssign, string workname)
        {
            UserJWT loginData = new UserJWT();
            loginData.CustomerID = CustemerID;
            loginData.LastName = "Hệ thống tự động";
            loginData.UserID = 0;

            #region Check dự án đó có gửi gửi mail khi chỉnh sửa công việc hay không
            if (JeeWorkLiteController.CheckNotify_ByConditions(id_project_team, "email_update_work", false, ConnectionString))
            {
                var users = new List<long> { id_user };
                int idtemplatemail = 0;
                if (isAssign)
                {
                    idtemplatemail = 10;
                }
                else
                {
                    idtemplatemail = 22;
                }
                JeeWorkLiteController.SendEmail(id_work, users, idtemplatemail, loginData, ConnectionString, _notifier, _config);
                #region Lấy thông tin để thông báo
                SendNotifyModel noti = JeeWorkLiteController.GetInfoNotify(idtemplatemail, ConnectionString);
                #endregion
                #region Notify assign
                Hashtable has_replace = new Hashtable();
                for (int i = 0; i < users.Count; i++)
                {
                    NotifyModel notify_model = new NotifyModel();
                    has_replace = new Hashtable();
                    notify_model.AppCode = "WORK";
                    notify_model.From_IDNV = loginData.UserID.ToString();
                    notify_model.To_IDNV = users[i].ToString();
                    notify_model.TitleLanguageKey = LocalizationUtility.GetBackendMessage("ww_assign", "", "vi");
                    if (!isAssign)
                    {
                        notify_model.TitleLanguageKey = LocalizationUtility.GetBackendMessage("ww_xoaassign", "", "vi");
                    }
                    notify_model.TitleLanguageKey = notify_model.TitleLanguageKey.Replace("$nguoigui$", loginData.LastName);
                    notify_model.TitleLanguageKey = notify_model.TitleLanguageKey.Replace("$tencongviec$", workname);
                    notify_model.ReplaceData = has_replace;
                    notify_model.To_Link_MobileApp = noti.link_mobileapp.Replace("$id$", id_work.ToString());
                    notify_model.To_Link_WebApp = noti.link.Replace("$id$", id_work.ToString());

                    List<AccUsernameModel> DataAccount = JeeWorkLiteController.GetDanhSachAccountFromCustomerID(_config, CustemerID);
                    var info = DataAccount.Where(x => notify_model.To_IDNV.ToString().Contains(x.UserId.ToString())).FirstOrDefault();
                    if (info is not null)
                    {
                        bool kq_noti = JeeWorkLiteController.SendNotify(loginData, info.Username, notify_model, _notifier, _config);
                    }
                }
                #endregion
            }
            #endregion

        }

        public void SendNotifyAndMailAssign(int idtemplatemail, string ConnectionString, List<long> users, long id_work, long CustemerID, string TitleLanguageKey)
        {
            UserJWT loginData = new UserJWT();
            loginData.CustomerID = CustemerID;
            loginData.LastName = "Hệ thống tự động";
            loginData.UserID = 0;
            using (DpsConnection cnn = new DpsConnection(ConnectionString))
            {
                DataTable dt_work = cnn.CreateDataTable("select * from we_work where disabled = 0 and id_row = " + id_work);
                if (dt_work.Rows.Count > 0)
                {
                    #region Check dự án đó có gửi gửi mail khi chỉnh sửa công việc hay không
                    if (JeeWorkLiteController.CheckNotify_ByConditions(long.Parse(dt_work.Rows[0]["id_project_team"].ToString()), "email_update_work", false, ConnectionString))
                    {
                        JeeWorkLiteController.SendEmail(id_work, users, idtemplatemail, loginData, ConnectionString, _notifier, _config);
                        #region Lấy thông tin để thông báo
                        SendNotifyModel noti = JeeWorkLiteController.GetInfoNotify(idtemplatemail, ConnectionString);
                        #endregion
                        #region Notify assign
                        Hashtable has_replace = new Hashtable();
                        for (int i = 0; i < users.Count; i++)
                        {
                            NotifyModel notify_model = new NotifyModel();
                            has_replace = new Hashtable();
                            notify_model.AppCode = "WORK";
                            notify_model.From_IDNV = loginData.UserID.ToString();
                            notify_model.To_IDNV = users[i].ToString();
                            notify_model.TitleLanguageKey = LocalizationUtility.GetBackendMessage(TitleLanguageKey, "", "vi");
                            notify_model.TitleLanguageKey = notify_model.TitleLanguageKey.Replace("$nguoigui$", loginData.LastName);
                            notify_model.TitleLanguageKey = notify_model.TitleLanguageKey.Replace("$tencongviec$", dt_work.Rows[0]["title"].ToString());
                            notify_model.TitleLanguageKey = notify_model.TitleLanguageKey.Replace(" $value$", dt_work.Rows[0]["estimates"].ToString());
                            notify_model.ReplaceData = has_replace;
                            notify_model.To_Link_MobileApp = noti.link_mobileapp.Replace("$id$", id_work.ToString());
                            notify_model.To_Link_WebApp = noti.link.Replace("$id$", id_work.ToString());

                            List<AccUsernameModel> DataAccount = JeeWorkLiteController.GetDanhSachAccountFromCustomerID(_config, CustemerID);
                            var info = DataAccount.Where(x => notify_model.To_IDNV.ToString().Contains(x.UserId.ToString())).FirstOrDefault();
                            if (info is not null)
                            {
                                bool kq_noti = JeeWorkLiteController.SendNotify(loginData, info.Username, notify_model, _notifier, _config);
                            }
                        }
                        #endregion
                    }
                    #endregion
                }
            }
        }
    }
}