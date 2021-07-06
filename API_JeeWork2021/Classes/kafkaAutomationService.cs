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
            _ = Task.Run(() =>
            {
                testSoLuong.SubscribeAsync("jeework.automationservice", Get_Automation);
            }, cancellationToken);
            await Task.CompletedTask;
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            await testSoLuong.closeAsync();
        }
        public void getMessTestSL(string msg)
        {
            var post = JObject.Parse(msg);
            Console.OutputEncoding = Encoding.UTF8;
            Console.WriteLine("========SL nhắc nhở==============================================================");
            Console.WriteLine(msg);
            Console.WriteLine("==========End SL nhắc nhở============================================================");
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
                "where disabled = 0 and eventid = " + data.eventid;
            using (DpsConnection cnn = new DpsConnection(connectionString))
            {
                SqlConditions conds = new SqlConditions();
                conds.Add("disabled", 0);
                sqlq_execute = "select id_row " +
                    "from we_work where id_parent is null and (where)";
                if (data.departmentid > 0)
                {
                    sqlq_execute += " and id_project_team in (select id_row from we_project_team where id_department =" + data.listid + ")";
                    sqlq_action += " and departmentid = " + data.departmentid;
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
                        switch (actionid)
                        {
                            case 1:
                                DataTable dt_sub = cnn.CreateDataTable("select rowid, autoid, subactionid, value " +
                                    "from automation_subaction " +
                                    "where autoid =" + autoid + "");
                                if (dt_sub.Rows.Count > 0)
                                {
                                    process_automation_subaction(dt_sub, get_list_id(dt_execute), cnn);
                                }
                                break;
                            case 7:
                                break;
                            case 4:
                            case 8:
                            case 9:
                            case 10:
                            case 11:
                            case 12:
                            case 13:
                            case 14:
                                break;
                            case 5:
                                break;
                            case 6:
                                break;
                            case 2:
                            case 3:
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
            foreach (DataRow dr in dt_sub.Rows)
            {
                long SubActionID = long.Parse(dr["subactionid"].ToString());
                string TableName = dr["tablename"].ToString();
                string[] values = dr["value"].ToString().Split(",");
                Hashtable has = new Hashtable();
                switch (SubActionID)
                {
                    case 1: // Thêm người thực hiện (we_work_user)
                        foreach (string item in values)
                        {
                            for (int i = 0; i < condition_update.Length; i++)
                            {
                                has = new Hashtable();
                                has["id_work"] = i;
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
                        break;
                    case 2: // Xóa người thực hiện -- id_work=$objectid$,id_user in ($list_value$),loai=1
                        break;
                    case 3: // Xóa tất cả người hiện tại -- id_work=$objectid$,loai=1
                    case 4: // Gán lại người thực hiện
                    case 5: // Thêm tag (we_work_tag)
                    case 6: // Xóa tag
                        break;
                    default:
                        break;
                }
            }
            return true;
        }
        public static bool get_condition_by_events(DataTable dt_auto, string conditions_input, DpsConnection cnn)
        {
            bool result = true;
            if (dt_auto.Rows.Count > 0)
            {
                long eventid = 3;
                switch (eventid)
                {
                    case 1:
                    case 2:
                        string[] conditions = dt_auto.Rows[0]["condition"].ToString().Split(";");
                        List<string> from = conditions[0].Replace("From:", "").Split(",").ToList();
                        List<string> to = conditions[0].Replace("To:", "").Split(",").ToList();
                        if ("any".Equals(from) && "any".Equals(to))
                            result = true;
                        else
                        {
                            bool isfrom = true, isto = true;
                            if (!"any".Equals(from))
                            {
                                isfrom = from.Contains(conditions_input.Split(",")[0]);
                            }
                            if (!"any".Equals(to))
                            {
                                isto = to.Contains(conditions_input.Split(",")[1]);
                            }
                            if (isfrom && isto)
                                return result;
                        }
                        break;
                    case 3:
                    case 4:
                    case 7:
                    case 10:
                    case 11:
                        return result;
                    case 5:
                    case 6:
                    case 8:
                        string list = dt_auto.Rows[0]["condition"].ToString();
                        return list.Contains(conditions_input);
                }
            }
            return result;
        }
    }
}