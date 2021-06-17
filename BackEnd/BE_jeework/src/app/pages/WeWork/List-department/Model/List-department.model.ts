import { BaseModel } from './../../../../_metronic/jeework_old/core/_base/crud/models/_base.model';

export class DepartmentModel extends BaseModel {
	RowID: number;
	title: string;
	dept_name: string;
	IsEdit: boolean;
	id_cocau: string = '';
	id_row: number;
	// Owners: Array<DepartmentOwnerModel> = [];
	Owners: any = [];
	IsDataStaff_HR: boolean;
	ReUpdated: boolean;
	TemplateID: number;
	DefaultView: any;
	ParentID: any;

	clear() {
		this.RowID = 0;
		this.dept_name = '';
		this.title = '';
		this.IsEdit = true;
		this.id_cocau = '';
		this.Owners = [];
		this.DefaultView = [];
		this.IsDataStaff_HR = false;
		this.ReUpdated = false;
		this.TemplateID = 0;
		this.ParentID = 0;
	}
}
export class DepartmentOwnerModel extends BaseModel {
	id_row: number;
	id_user: number;
	id_department: number;
	type: number;
	clear() {
		this.id_row = 0;
		this.id_user = 0;
		this.id_department = 0;
		this.type = 1;

	}
}
export class DepartmentViewModel extends BaseModel {
	id_row: number;
	viewid: number;
	id_department: number;
	is_default: boolean;
	clear() {
		this.id_row = 0;
		this.viewid = 0;
		this.id_department = 0;
		this.is_default = false;

	}
}

export class MilestoneModel extends BaseModel {
	id_row: number;
	title: string;
	description: string;
	person_in_charge: string;
	deadline: string;
	id_project_team: number;
	Count: Array<CountModel> = [];
	clear() {
		this.id_row = 0;
		this.description = '';
		this.title = '';
		this.person_in_charge = '';
		this.deadline = '';
		this.id_project_team = 0;
		this.Count = [];
	}
}

export class CountModel extends BaseModel {
	id_row: number;
	id_user: number;
	percentage: number;
	clear() {
		this.id_row = 0;
		this.id_user = 0;
		this.percentage = 0;

	}
}

export class UpdateQuickModel extends BaseModel {
	id_row: number;
	columname: string;
	values: string;
	istemplate: boolean;
	customerid: number;
	id_template: number;
	clear() {
		this.id_row = 0;
		this.columname = "";
		this.values = "";
		this.istemplate = false;
		this.customerid = 0;
		this.id_template = 0;
	}
}

// public class Different_Statuses
//     {
//         public long Id_row { get; set; }
//         public string StatusName { get; set; }
//         public string Description { get; set; }
//         public long id_project_team { get; set; }
//         public string Type { get; set; }
//         public bool IsDefault { get; set; }
//         public string Color { get; set; }
//         public long Position { get; set; }
//         public long Follower { get; set; }
//         public bool IsMapAll { get; set; }
//         public long TemplateID_New { get; set; }
//         public long TemplateID_Old { get; set; }
//         public List<MapModel> Map_Detail { get; set; }
//     }
// public class MapModel
// {
// 	public long old_status { get; set; }
// 	public long new_status { get; set; }
// }
export class Different_StatusesModel extends BaseModel {
	// Id_row: number;
	// StatusName: string;
	// Description: string;
	id_project_team: number;
	// Type: string;
	// IsDefault: boolean;
	// Color: string;
	// Position: number;
	// Follower: number;
	IsMapAll: boolean;
	TemplateID_New: number;
	// TemplateID_Old: number;
	Map_Detail: Array<MapModel>;
	clear() {
		// this.Id_row = 0;
		// this.StatusName = "";
		// this.Description = "";
		this.id_project_team = 0;
		// this.Type = '';
		// this.IsDefault = false;
		// this.Color = '';
		// this.Position = 0;
		this.IsMapAll = false;
		this.TemplateID_New = 0;
		// this.TemplateID_Old = 0;
		this.Map_Detail = [];
	}
}

export class MapModel {
	old_status: number;
	new_status: number;
	clear() {
		this.old_status = 0;
		this.new_status = 0;
	}
}
