﻿using Microsoft.Extensions.Configuration;
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

namespace API_JeeWork2021.Classes
{
    public class kafkaAutomationService : IHostedService
    {
        private IConfiguration _config;
        private Consumer testSoLuong;
        private IProducer _producer;
        private IConnectionCache _cache;
        private readonly ILogger<kafkaAutomationService> _logger;
        public kafkaAutomationService(IConfiguration config, IProducer producer, IConnectionCache connectionCache, ILogger<kafkaAutomationService> logger)
        {
            _cache = connectionCache;
            _producer = producer;
            _config = config;
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
            string connectionString = WeworkLiteController.getConnectionString(_cache, data.customerid, _config);
            ExecuteAutomation(data, connectionString);
        }
        public static bool ExecuteAutomation(Post_Automation_Model data, string connectionString)
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
                if (get_condition_by_events(dt_auto, data.data_input, cnn)) // Kiểm tra event để thực thi hành động
                {
                    foreach (DataRow dr in dt_auto.Rows)
                    {
                        long actionid = long.Parse(dr["actionid"].ToString());
                        long autoid = long.Parse(dr["rowid"].ToString());
                        string data_auto = dr["data"].ToString();
                        string columnname = "id_project_team";
                        switch (actionid)
                        {
                            case 1:
                                DataTable dt_sub = cnn.CreateDataTable(@"select sub.rowid, autoid, subactionid, value,sublist.TableName from automation_subaction sub
join Automation_SubActionList sublist on sub.SubActionID = sublist.RowID  where autoid =" + autoid + "");
                                if (dt_sub.Rows.Count > 0)
                                {
                                    process_automation_subaction(dt_sub, get_list_id(dt_execute), cnn);
                                }
                                break;
                            case 7:
                                break;
                            case 4: // comment 
                                insertComment(data.userid,data_auto, get_list_id(dt_execute), cnn);
                                break;
                            case 8:
                            case 9:
                                columnname = "status";
                                goto case 14;
                            case 10:
                                columnname = "clickup_prioritize";
                                goto case 14;
                            case 11:
                                columnname = "deadline";
                                goto case 14;
                            case 12:
                                columnname = "start_date";
                                goto case 14;
                            case 13:
                            case 14:
                                doitinhtrang(columnname, data_auto, get_list_id(dt_execute), cnn);
                                break;
                            case 5:
                                doitinhtrang("Disabled", "1", get_list_id(dt_execute), cnn);
                                break;
                            case 6:
                                DuplicateTask(data_auto,get_list_id(dt_execute), cnn);
                                break;
                            case 2:
                            case 3:
                                CreatedTask(dr["rowid"].ToString(), data.userid, cnn);
                                break;
                            default:
                                break;
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
        public static bool process_automation_subaction(DataTable dt_sub, string condition_update, DpsConnection cnn)
        {
            var sub3 = dt_sub.AsEnumerable().Where(x => x["subactionid"].ToString() == "3").FirstOrDefault();
            if(sub3 != null && sub3["value"].ToString() == "true")
            {

                string[] listTask = condition_update.Split(",");
                for (int i = 0; i < listTask.Length; i++)
                {
                    Hashtable val2 = new Hashtable();
                    val2["UpdatedDate"] = DateTime.Now;
                    val2["UpdatedBy"] = 0;
                    val2["Disabled"] = 1;
                    SqlConditions cond = new SqlConditions();
                    cond.Add("id_work", listTask[i]);
                    if (cnn.Update(val2, cond, sub3["tablename"].ToString()) < 0)
                    {
                        cnn.RollbackTransaction();
                        return false;
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
                                        has["createddate"] = DateTime.Now;
                                        has["createdby"] = 0;
                                        has["id_user"] = item;
                                        has["loai"] = 1;
                                        if (cnn.Insert(has, TableName) != 1)
                                        {
                                            cnn.RollbackTransaction();
                                            return false;
                                        }
                                    }

                                }
                            }
                            break;
                        case 2: // Xóa người thực hiện -- id_work=$objectid$,id_user in ($list_value$),loai=1
                            foreach (string item in values)
                            {
                                for (int i = 0; i < listTask.Length; i++)
                                {
                                    Hashtable val2 = new Hashtable();
                                    val2["UpdatedDate"] = DateTime.Now;
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

                                }
                            }
                            break;
                        case 4: // Gán lại người thực hiện
                            for (int i = 0; i < listTask.Length; i++)
                            {
                                Hashtable val2 = new Hashtable();
                                val2["UpdatedDate"] = DateTime.Now;
                                val2["UpdatedBy"] = 0;
                                val2["Disabled"] = 1;
                                SqlConditions cond = new SqlConditions();
                                cond.Add("id_work", listTask[i]);
                                if (cnn.Update(val2, cond, TableName) < 0)
                                {
                                    cnn.RollbackTransaction();
                                    return false;
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
                                        has["createddate"] = DateTime.Now;
                                        has["createdby"] = 0;
                                        has["id_user"] = item;
                                        has["loai"] = 1;
                                        if (cnn.Insert(has, TableName) != 1)
                                        {
                                            cnn.RollbackTransaction();
                                            return false;
                                        }
                                    }

                                }
                            }
                            break;
                        case 3: // Xóa tất cả người hiện tại -- id_work=$objectid$,loai=1
                        case 5: // Thêm tag (we_work_tag)
                        case 6: // Xóa tag
                            break;
                        default:
                            break;
                    }
                }
            }
            return true;
           
        }
        public static bool get_condition_by_events(DataTable dt_auto, string conditions_input, DpsConnection cnn)
        {
            bool result = false;
            if (dt_auto.Rows.Count > 0)
            {
                long eventid = long.Parse(dt_auto.Rows[0]["eventid"].ToString());
                switch (eventid)
                {
                    case 1:
                    case 2:
                        string[] conditions = dt_auto.Rows[0]["condition"].ToString().Split(";");
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
                        string list = dt_auto.Rows[0]["condition"].ToString();
                        if (list == "any") return true;
                        return list.Contains(conditions_input);
                }
            }
            return result;
        }
        public static bool insertComment(long userid, string data,string condition_update, DpsConnection cnn)
        {
            string[] listTask = condition_update.Split(",");
            for (int i = 0; i < listTask.Length; i++)
            {
                Hashtable val = new Hashtable();
                val.Add("comment", data);
                val.Add("object_type", 1);
                val.Add("object_id", listTask[i]);
                val.Add("CreatedDate", DateTime.Now);
                val.Add("CreatedBy", userid);
                if (cnn.Insert(val, "we_comment") != 1)
                {
                    cnn.RollbackTransaction();
                    return false;
                }
            }
            return true;
        }
        public static bool doitinhtrang(string columnname, string data,string condition_update, DpsConnection cnn)
        {
            if (columnname == "start_date" || columnname == "deadline")
            {
                string[] list = data.Split(";");
                int type = int.Parse(list[0]);
                string value = list[1];
                if (type == 1)
                {
                    data = DateTime.Now.AddDays(int.Parse(value)).ToString();
                }
                else if(type == 2)
                {
                    data = DateTime.Now.ToString();
                }
                else
                {
                    data = DateTime.Parse(value).ToString();
                }
            }
            string[] listTask = condition_update.Split(",");
            for (int i = 0; i < listTask.Length; i++)
            {
                if (columnname == "status")
                {
                    if (!kiemtratinhtrang(data, listTask[i], cnn)) return false;
                }
                Hashtable val = new Hashtable();
                val.Add("UpdatedDate", DateTime.Now);
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

                if ( columnname == "id_project_team")
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
            }
            return true;
        }
        public static bool kiemtratinhtrang(string data,string taskid, DpsConnection cnn)
        {
            //kiểm tra data status đó có trong projectteam của task đó hay không
            string sqlq1 = $"select isnull((select id_row from we_status where id_project_team = (select id_project_team from we_work where id_row = {taskid}) and id_row = {data}),0)";
            var statusID = long.Parse(cnn.ExecuteScalar(sqlq1).ToString());
            return statusID > 0;
        }
        public static bool DuplicateTask(string data,string condition_update, DpsConnection cnn)
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
     select id_row,title,description,{data},deadline,1,1,start_date,1,2,0,GETDATE()
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
        
        public static bool CreatedTask(string AutoID, long userid, DpsConnection cnn)
        {
            string sqlq = @"select at.* from Automation_Task at
where AutoID = "+ AutoID;
            DataTable dt = cnn.CreateDataTable(sqlq) ;
            if(cnn.LastError != null || dt.Rows.Count == 0)
            {
                return false;
            }
            DataRow dr = dt.Rows[0];
            Hashtable val = new Hashtable();
            if (!string.IsNullOrEmpty(dr["id_parent"].ToString()))
                val.Add("id_parent", dr["id_parent"]);
            val.Add("title", dr["title"]);
            if (string.IsNullOrEmpty(dr["description"].ToString()))
                val.Add("description", "");
            else
                val.Add("description", dr["description"]);
            val.Add("id_project_team", dr["id_project_team"]);


            if (int.Parse(cnn.ExecuteScalar("select count(*) from we_status where disabled=0 and id_row = " + dr["status"] +"and id_project_team = "+ dr["id_project_team"] ).ToString()) == 0)
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
                        val.Add("start_date", DateTime.Now.AddDays(int.Parse(dr["start_date"].ToString())));
                        break;
                    case 2:
                        val.Add("start_date", DateTime.Now );
                        break;
                    case 3:
                        val.Add("start_date", dr["start_date"]);
                        break;
                }
            }
            if(int.Parse(dr["Deadline_Type"].ToString()) > 0)
            {
                switch (int.Parse(dr["Deadline_Type"].ToString()))
                {
                    case 1:
                        val.Add("deadline", DateTime.Now.AddDays(int.Parse(dr["deadline"].ToString())));
                        break;
                    case 2:
                        val.Add("deadline", DateTime.Now );
                        break;
                    case 3:
                        val.Add("deadline", dr["deadline"]);
                        break;
                }
            }

            if (!string.IsNullOrEmpty(dr["id_group"].ToString()))
                val.Add("id_group", dr["id_group"].ToString());
            val.Add("CreatedDate", DateTime.Now);
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
            if(dtUser.Rows.Count > 0)
            {

                Hashtable valU = new Hashtable();
                valU["id_work"] = weworkID;
                valU["CreatedDate"] = DateTime.Now;
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

            cnn.EndTransaction();

            return true;
        }
    }
}