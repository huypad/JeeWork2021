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
	id_row: number;
	clear() {
		this.id_nv = 0;
		this.id_user = 0;
		this.hoten = '';
		this.tenchucdanh = '';
		this.image = '';
		this.mobile = '';
		this.username = '';
		this.admin = false;
		this.id_row = 0;
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

export class WorkModel {
	id_row: number;
	title: string;
	deadline: string;
	start_date: string;
	end_date: string;
	description: string;
	id_project_team: number;
	id_milestone: number;
	id_group: number;
	prioritize: boolean;
	urgent: number;
	id_parent: number = 0;
	Users: UserInfoModel[];
	Attachments: FileUploadModel[];
	Tags: WorkTagModel[];
	Followers: UserInfoModel[];
	assign: UserInfoModel;
	status: number;
	Activities: any;
	clear() {
		this.id_row = 0;
		this.title = '';
		this.deadline = '';
		this.id_group = 0;
		this.start_date = '';
		this.end_date = '';
		this.description = '';
		this.id_project_team = 0;
		this.id_milestone = 0;
		this.prioritize = false;
		this.Users = [];
		this.Attachments = [];
		this.Tags = [];
		this.id_parent = 0;
		this.urgent = 0;
		this.status = 0;
		this.Activities = [];
	}
}
export class WorkGroupModel {
	id_row: number;
	title: string;
	description: string;
	id_project_team: string;
	reviewer: ReviewerModel;
	id: number;

	clear() {
		this.id_row = 0;
		this.title = '';
		this.description = '';
		this.id_project_team = '';
		// this.reviewer = [];
		this.id = 0;
	}
}
export class ReviewerModel extends BaseModel {
	id_nv: number;
	id_user: number;
	hoten: string;
	username: string;
	clear() {
		this.id_nv = 0;
		this.id_user = 0;
		this.hoten = '';
		this.username = '';
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
export class WorkUserModel extends BaseModel {
	id_row: number;
	id_work: number;
	id_user: number;
	loai: number;
	clear() {
		this.id_row = 0;
		this.id_work = 0;
		this.id_user = 0;
		this.loai = 0;
	}
}

export class WorkDuplicateModel extends BaseModel {
	id_row: number;
	title: string;
	id: number;
	type: number; // 1 dupliacte dự án 2 duplicate dự án ngoài
	deadline: string;
	start_date: string;
	description: string;
	id_parent: number;
	id_project_team: number;
	id_group: number; // k dùng clickup
	duplicate_child: string; // lấy con (true - false)
	assign: number;
	followers: any[];
	urgent: string;
	required_result: string;
	require: string;
	Users: Array<UserInfoModel> = [];
	clear() {
		this.id_row = 0;
		this.id = 0;
		this.title = '';
		this.type = 1;
		this.description = '';
		this.id_parent = 0;
		this.id_project_team = 0;
		this.id_group = 0;
		this.duplicate_child = '';
		this.assign = 0;
		this.followers = [];
		this.urgent = '';
		this.required_result = '';
		this.require = '';
		this.Users = [];
	}
}


export class UpdateWorkModel {
	id_row: number;
	key: string;
	value: string;
	id_log_action: string;
	IsStaff: boolean;
	values: Array<FileUploadModel> = [];
	status_type:string;
	FieldID:number;
	Value:string;
	WorkID:string;
	TypeID:string;
	clear() {
		this.id_row = 0;
		this.key = '';
		this.value = '';
		this.id_log_action = '';
		this.values = [];
		this.IsStaff = false;
	}
}

export class RepeatedModel extends BaseModel {
	id_row: number;
	title: string;
	frequency: string;
	Locked: boolean;
	id_group: string;
	deadline: string;
	start_date: string;
	end_date: string;
	description: string;
	id_parent: string;
	id_project_team: string;
	duplicate_child: string;
	Assign: UserInfoModel;
	assign: number;
	required_result: string;
	repeated_day: string;
	Users: Array<UserInfoModel> = [];
	IsCopy: boolean;
	Tasks: Array<RepeatedTaskModel> = [];
	clear() {
		this.id_row = 0;
		this.title = '';
		this.frequency = '';
		this.Locked = false;
		this.deadline = '';
		this.end_date = '';
		this.start_date = '';
		this.description = '';
		this.id_parent = '';
		this.id_project_team = '';
		this.id_group = '';
		this.duplicate_child = '';
		this.assign = 0;
		this.repeated_day = '';
		this.Users = [];
		this.IsCopy = false;
	}
}
export class RepeatedTaskModel extends BaseModel {
	id_row: number;
	id_repeated: number;
	IsTodo: boolean;
	UserID: number;
	Title: string;
	Deadline: number;
	clear() {
		this.id_row = 0;
		this.id_repeated = 0;
		this.IsTodo = false;
		this.UserID = 0;
		this.Title = '';
		this.Deadline = 0;
	}
}