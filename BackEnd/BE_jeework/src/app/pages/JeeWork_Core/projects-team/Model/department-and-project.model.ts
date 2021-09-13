import {TemplateCenterModel} from './../../template-center/template-model/template.model';
import {BaseModel} from './../../../../_metronic/jeework_old/core/_base/crud/models/_base.model_new';

export class ObjectDetailsModel extends BaseModel {
    RowID: number;
    ObjectID: number;
    Title: string;
    ControlID: string;
    IsRequired: boolean;
    Description: string;
    Values: any[] = [];
    IsEdit: boolean;
    KeyValue: string;

    clear() {
        this.RowID = 0;
        this.ObjectID = 0;
        this.Title = '';
        this.ControlID = '';
        this.IsRequired = false;
        this.Description = '';
        this.Values = [];
        this.IsEdit = true;
        this.KeyValue = '';
    }
}

export class TagsModel extends BaseModel {
    id_row: number;
    title: string;
    color: string;
    id_project_team: string;
    project_team: string;

    clear() {
        this.id_row = 0;
        this.color = '';
        this.title = '';
        this.id_project_team = '0';
        this.project_team = '';
    }
}

export class MilestoneModel extends BaseModel {
    id_row: number;
    title: string;
    description: string;
    person_in_charge: string;
    deadline: string;
    id_project_team: number;
    id_department: number;

    clear() {
        this.id_row = 0;
        this.description = '';
        this.title = '';
        this.person_in_charge = '';
        this.deadline = '';
        this.id_project_team = 0;
        this.id_department = 0;
    }
}

export class ProjectTeamModel extends BaseModel {
    id_row: number;
    default_view: number;
    icon: any;
    title: string;
    description: string;
    detail: string;
    Description: string;
    id_department: string;
    IsEdit: boolean;
    start_date: string;
    end_date: string;
    is_project: boolean;
    loai: string;
    status: string;
    locked: boolean;
    stage_description: string;
    color: string;
    template: string;
    allow_percent_done: boolean;
    require_evaluate: boolean;
    evaluate_by_assignner: boolean;
    allow_estimate_time: boolean;
    period_type: number;
    Users: Array<ProjectTeamUserModel> = [];
    users: Array<ProjectTeamUserModel> = [];
    Count: any;
    DueToday: any;
    NotStarted: any;
    // DueToday: any[];
    child: any;
    person_in_charge: any;
    id_project_team: string;
    templatecenter: TemplateCenterModel;

    clear() {
        this.id_row = 0;
        this.default_view = 4;
        // this.icon = {};
        this.title = '';
        this.description = '';
        this.detail = '';
        this.Description = '';
        this.id_department = '';
        this.IsEdit = true;
        this.start_date = null;
        this.end_date = null;
        this.is_project = false;
        this.loai = '1';
        this.status = '1';
        this.locked = false;
        this.stage_description = '';
        this.color = '';
        this.template = '';
        this.allow_percent_done = false;
        this.require_evaluate = false;
        this.evaluate_by_assignner = false;
        this.allow_estimate_time = false;
        this.period_type = 1;
        this.id_project_team = '';
    }
}

export class ProjectTeamUserModel {
    id_row: number;
    id_project_team: number;
    id_user: number;
    admin: boolean;
    id_nv: number;

    clear() {
        this.id_row = 0;
        this.id_project_team = 0;
        this.id_user = 0;
        this.admin = false;
    }
}

export class ProjectTeamCloseModel extends BaseModel {
    id_row: number;
    stop_reminder: boolean;
    close_status: number;
    note: string;
    title: string;

    // DueToday: any[];
    clear() {
        this.id_row = 0;
        this.stop_reminder = true;
        this.close_status = null;
        this.note = '';
        this.title = '';
    }
}

export class ProjectTeamDuplicateModel extends BaseModel {
    id_row: number;
    id: number;
    title: string;
    type: number;
    keep_creater: boolean;
    keep_checker: boolean;
    keep_deadline: number;
    hour_adjusted: number;
    keep_checklist: boolean;
    keep_child: boolean;
    keep_tag: boolean;
    keep_follower: boolean;
    keep_admin: boolean;
    keep_member: boolean;
    keep_role: boolean;
    keep_milestone: boolean;

    clear() {
        this.id_row = 0;
        this.id = 0;
        this.title = '';
        this.type = 0;
        this.keep_creater = true;
        this.keep_checker = true;
        this.keep_deadline = 0;
        this.hour_adjusted = 1;
        this.keep_checklist = true;
        this.keep_follower = true;
        this.keep_child = true;
        this.keep_tag = true;
        this.keep_admin = true;
        this.keep_member = true;
        this.keep_role = true;
        this.keep_milestone = false;
    }
}


export class TopicModel extends BaseModel {
    id_row: number;
    title: string;
    description: string;
    id_project_team: string;
    email: boolean;
    Users: Array<TopicUserModel> = [];
    Attachments: Array<FileUploadModel> = [];
    Follower: Array<TopicUserModel> = [];
    Attachment: Array<FileUploadModel>;

    clear() {
        this.id_row = 0;
        this.title = '';
        this.description = '';
        this.id_project_team = '';
        this.email = false;
    }
}

export class TopicUserModel extends BaseModel {
    id_row: number;
    id_topic: number;
    id_user: number;
    id_nv: number;
    favourite: boolean;

    clear() {
        this.id_row = 0;
        this.id_topic = 0;
        this.id_user = 0;
        this.favourite = false;
        this.id_nv = 0;
    }
}

export class FileUploadModel extends BaseModel {
    IdRow: number;
    strBase64: string;
    filename: string;
    src: string;
    IsAdd: boolean;
    IsDel: boolean;
    IsImagePresent: boolean;

    clear() {
        this.IdRow = 0;
        this.strBase64 = '';
        this.filename = '';
        this.src = '';
        this.IsAdd = false;
        this.IsDel = false;
        this.IsImagePresent = false;
    }
}

export class AttachmentModel extends BaseModel {
    object_type: number;
    object_id: number;
    id_user: number;
    item: FileUploadModel;

    clear() {
        this.object_type = 0;
        this.object_id = 0;
        this.id_user = 0;
        this.item;
    }
}

export class ProjectViewsModel extends BaseModel {
    id_row: number;
    viewid: number;
    view_name_new: string;
    id_project_team: number;
    link: string;
    default_everyone: boolean = false;
    default_for_me: boolean = false;
    pin_view: boolean = false;
    personal_view: boolean = false;
    favourite: boolean = false;
    id_department: number;

    clear() {
        this.id_row = 0;
        this.viewid = 0;
        this.view_name_new = '';
        this.id_project_team = 0;
        this.link = '';
        this.id_department = 0;
    }
}
