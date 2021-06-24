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

export class TemplateCenterModel extends BaseModel {
	id_row: number;
	title: string;
	templateid: number;
	customerid: number;
	ObjectTypesID: number;
	ParentID: number;
	types: number;
	levels: number;
	viewid: number;
	group_statusid: number;
	template_typeid: number;
	img_temp: string;
	field_id: string;
	is_customitems: boolean;
	is_task: boolean;
	is_views: boolean;
	is_projectdates: boolean;
	list_field_name: Array<ListFieldModel> = [];
	projectdates: ProjectDatesModel;
	clear() {
		this.id_row = 0;
		this.title = "";
		this.templateid = 0;
		this.customerid = 0;
		this.ObjectTypesID = 0;
		this.ParentID = 0;
		this.types = 0;
		this.levels = 0;
		this.viewid = 0;
		this.group_statusid = 0;
		this.template_typeid = 0;
		this.img_temp = "";
		this.field_id = "";
		this.is_customitems = false;
		this.is_task = false;
		this.is_views = false;
		this.is_projectdates = false;
		this.list_field_name = [];
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

export class ProjectDatesModel {
	start_date: string;
	end_date: string;
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
