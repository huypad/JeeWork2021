import { WorkTagModel, WorkUserModel } from './../../work/work.model';
/*
    public class Auto_Task_TagModel
    {
        public long id_row { get; set; }
        public long id_work { get; set; }
        public long id_tag { get; set; }
    }
    public class Auto_Task_UserModel
    {
        public long id_row { get; set; }
        public long id_work { get; set; }
        public long id_user { get; set; }
        public int loai { get; set; }//1: assign, 2 follow

    }
*/

export class AutomationListModel {
	rowid: number;
	title: string;
	description: string;
	listid: string;
	departmentid: string;
	eventid: string;
	condition: string;
	actionid: number;
	data: string;
	status: string;
	subaction: Array<Automation_SubAction_Model> = [];
	task: Array<Auto_Task_Model> = [];
	clear() {
		this.rowid = 0;
		this.title = "";
		this.description = "";
		this.listid = "";
		this.departmentid = '';
		this.eventid = '';
		this.condition = '';
		this.actionid = 0;
		this.data = '';
		this.status = '';
	}
}

export class Automation_SubAction_Model {
	rowid: number;
	autoid: string;
	subactionid: string;
	value: string;
	clear() {
        
	}
}
/**public class Auto_Task_Model
    {
        public long id_row { get; set; }
        public string title { get; set; }
        public DateTime deadline { get; set; }
        public DateTime start_date { get; set; }
        public string description { get; set; }
        public long id_project_team { get; set; }
        public long id_group { get; set; }
        public long id_parent { get; set; }
        public long status { get; set; }
        /// <summary>
        /// assign và follower
        /// </summary>
        public List<WorkUserModel> users { get; set; }
        public List<WorkTagModel> tags { get; set; }
        public long priority { get; set; }
        public string startdate_type { get; set; }
        public string deadline_type { get; set; }

    } */
export class Auto_Task_Model {
	id_row: number;
	title: string;
	description: string;
	deadline: string;
	start_date: string;
	id_project_team: number;
	id_group: number;
	id_parent: number;
	status: number;
	users: Array<WorkUserModel> = [];
	tags: Array<WorkTagModel> = [];
	priority: number;
	startdate_type: string;
	deadline_type: string;

	clear() {
		this.id_row = 0;
		// this.title = "";
		// this.description = "";
		// this.color = "";
		// this.isdefault = true;
		// this.customerid = 0;
		// this.Status = [];
	}
}