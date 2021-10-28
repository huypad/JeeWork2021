import { FileUploadModel } from '../../projects-team/Model/department-and-project.model';
import {BaseModel} from './../../../../_metronic/jeework_old/core/_base/crud/models/_base.model';

export class TopicModel extends BaseModel {
    id_row: number;
    title: string;
    description: string;
    id_project_team: string;
    email: string;
    Users: Array<TopicUserModel> = [];
    Follower: Array<TopicUserModel> = [];
    Attachments: Array<FileUploadModel> = [];

    clear() {
        this.id_row = 0;
        this.title = '';
        this.description = '';
        this.id_project_team = '';
        this.email = '';
    }
}

export class TopicUserModel extends BaseModel {
    id_row: number;
    id_topic: number;
    id_user: number;
    favourite: boolean;
    id_nv: number;

    clear() {
        this.id_row = 0;
        this.id_topic = 0;
        this.id_user = 0;
        this.favourite = false;
    }
}
