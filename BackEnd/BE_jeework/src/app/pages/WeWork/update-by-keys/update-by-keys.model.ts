import { BaseModel } from './../../../_metronic/jeework_old/core/_base/crud/models/_base.model';
import { FileUploadModel } from '../projects-team/Model/department-and-project.model';
export class MyWorkModel extends BaseModel {
	Count: Array<CountModel> = [];
	MoiDuocGiao: Array<MoiDuocGiaoModel> = [];
	GiaoQuaHan: Array<GiaoQuaHanModel> = [];
	LuuY: Array<LuuYModel> = [];
	clear() {
		this.Count = [];
		this.MoiDuocGiao = [];
		this.GiaoQuaHan = [];
		this.LuuY = [];
	}
}
export class CountModel {
	ht: number;
	phailam: number;
	danglam: number;
	clear() {
		this.ht = 0;
		this.phailam = 0;
		this.danglam = 0;
	}
}

export class MoiDuocGiaoModel extends BaseModel {
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
export class GiaoQuaHanModel extends BaseModel {
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
export class LuuYModel extends BaseModel {
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
export class UserInfoModel extends BaseModel {
	id_nv: number;
	id_user: number;
	hoten: string;
	tenchucdanh: string;
	image: string;
	mobile: string;
	username: string;
	admin: boolean;
	loai: number;
	clear() {
		this.id_nv = 0;
		this.id_user = 0;
		this.hoten = '';
		this.tenchucdanh = '';
		this.image = '';
		this.mobile = '';
		this.username = '';
		this.admin = false;
		this.loai = 1; //1:giao việc, 2: theo dõi
	}
}

export class MyMilestoneModel extends BaseModel {
	id_row: number;
	title: string;
	description: string;
	id_project_team: number;
	deadline_weekday: string;
	deadline_day: string;
	person_in_charge: Array<UserInfoModel> = [];
	clear() {
		this.id_row = 0;
		this.title = '';
		this.description = '';
		this.id_project_team = 0;
		this.deadline_weekday = '';
		this.deadline_day = '';
		this.person_in_charge = [];
	}
}

export class FilterModel extends BaseModel {
	id_row: number;
	title: string;
	color: string;
	clear() {
		this.id_row = 0;
		this.title = '';
		this.color = '';
	}
}

export class UpdateByKeyModel {
	id_row: number;
	key: string;
	value: string;
	id_log_action: string;
	values: Array<FileUploadModel> = [];
	clear() {
		this.id_row = 0;
		this.key = '';
		this.value = '';
		this.id_log_action = '';
		this.values = [];
	}
}

export class WorkTagModel extends BaseModel {
	id_row: number;
	id_work: number;
	id_tag: number;
	clear() {
		this.id_row = 0;
		this.id_work = 0;
		this.id_tag = 0;
	}
}
export class ChecklistModel {
	id_row: number;
	title: string;
	id_work: number;
	clear() {
		this.id_row = 0;
		this.title = '';
		this.id_work = 0;
	}
}


export class ChecklistItemModel {
	id_row: number;
	id_checklist: number;
	title: string;
	checker: number;
	clear() {
		this.id_row = 0;
		this.title = '';
		this.id_checklist = 0;
		this.checker = 0;
	}
}
// public long id_row { get; set; }
// public long id_checklist { get; set; }
// public string title { get; set; }
// public long checker { get; set; }
// public long priority { get; set; }