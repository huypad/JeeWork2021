import { BaseModel } from './../../../../_metronic/jeework_old/core/_base/crud/models/_base.model';


export class AuthorizeModel extends BaseModel {
	id_row: number;
	id_user: string;
	CreatedBy: string;
	clear() {
		this.id_row = 0;
		this.id_user = '';
		this.CreatedBy = '';
	}
}