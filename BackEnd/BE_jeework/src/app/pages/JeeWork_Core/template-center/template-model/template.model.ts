import { BaseModel } from './../../../../_metronic/jeework_old/core/_base/crud/models/_base.model_new';

export class TemplateModel extends BaseModel {
	id_row: number;
	title: string;
	description: string;
	color: string;
	isdefault: boolean;
	customerid: number;
	Status: Array<TemplateStatusModel> = [];
	clear() {
		this.id_row = 0;
		this.title = "";
		this.description = "";
		this.color = "";
		this.isdefault = true;
		this.customerid = 0;
		this.Status = [];
	}
}

export class UpdateQuickModel {
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
		this.istemplate = true;
		this.customerid = 0;
		this.id_template = 0;
	}
}

// public long share_with { get; set; } // 1 - Only Me, 2 - Everyone (including guests), 3 - All Members, 4 - Select people
// public List<TempalteUserModel> list_share { get; set; } // Nếu share_with = 4 thì nhập thêm cột list_share (Danh sách các member)
export class TemplateCenterModel extends BaseModel {
	id_row: number;
	title: string;
	description: string;
	templateid: number;
	customerid: number;
	ObjectTypesID: number;
	ParentID: number;
	types: number;
	levels: number;
	viewid: string;
	group_statusid: string;
	template_typeid: number;
	img_temp: string;
	field_id: string;
	is_customitems: boolean;
	is_task: boolean;
	is_views: boolean;
	is_projectdates: boolean;
	share_with: number;
	list_share: Array<TempalteUserModel> = [];
	list_field_name: Array<ListFieldModel> = [];
	list_status : Array<StatusListModel> = [];
	// public List<StatusListModel> list_status { get; set; } // Dùng khi save as

	start_date: string;
	end_date: string;
	save_as_id: string;
	sample_id: string;
	clear() {
		this.id_row = 0;
		this.title = "";
		this.description = "";
		this.templateid = 0;
		this.customerid = 0;
		this.ObjectTypesID = 0;
		this.ParentID = 0;
		this.types = 0;
		this.levels = 0;
		this.viewid = "";
		this.group_statusid = "";
		this.template_typeid = 0;
		this.img_temp = "";
		this.field_id = "";
		this.is_customitems = false;
		this.is_task = false;
		this.is_views = false;
		this.is_projectdates = false;
		this.list_field_name = [];
		this.save_as_id = '';
		this.sample_id = '0';
	}
}

export class ListFieldModel extends BaseModel {
	id_field: number;
	fieldname: string;
	title: string;
	isvisible: boolean;
	note: string;
	type: string;
	position: number;
	isnewfield: boolean;
	isdefault: boolean;
	typeid: number;
	clear() {
		this.id_field = 0;
		this.fieldname = '';
		this.title = '';
		this.isvisible = false;
		this.note = '';
		this.type = '';
		this.position = 0;
		this.isnewfield = false;
		this.isdefault = false;
		this.typeid = 0;
	}
}

export class TemplateStatusModel extends BaseModel {
	id_row: number;
	title: string;
	is_quahan: boolean;
	urgent: boolean;
	status: boolean;
	clear() {
		this.id_row = 0;
		this.title = '';
		this.is_quahan = false;
		this.urgent = false;
		this.status = false;
	}
}

export class StatusListModel extends BaseModel {
	id_row: number;
	StatusName: string;
	Description: string;
	color: string;
	Type: number;
	IsDefault: boolean;
	IsFinal: boolean;
	IsDeadline: boolean;
	IsToDo: boolean;
	clear() {
		this.id_row = 0;
		this.StatusName = '';
		this.Description = '';
		this.color = '';
		this.Type = 0;
		this.IsDefault = false;
		this.IsFinal = false;
		this.IsDeadline = false;
		this.IsToDo = false;
	}
}

export class TempalteUserModel {
	id_row: number;
	id_template: number;
	id_user: number;
	clear() {
		this.id_row = 0;
		this.id_template = 0;
		this.id_user = 0;
	}
}
