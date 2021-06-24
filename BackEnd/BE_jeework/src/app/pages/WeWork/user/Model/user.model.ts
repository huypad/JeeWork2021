import { BaseModel } from './../../../../_metronic/jeework_old/core/_base/crud/models/_base.model';


export class AuthorizeModel extends BaseModel {
	id_row: number;
	id_user: number;
	is_all_project: boolean;
	list_project: string;
	start_date: string;
	end_date: string;
	CreatedBy: number;
	clear() {
		this.id_row = 0;
		this.id_user = 0;
		this.is_all_project = false;
		this.list_project = '';
		this.CreatedBy = 0;
	}
}