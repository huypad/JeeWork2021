import { BaseModel } from './../../../../_metronic/jeework_old/core/_base/crud/models/_base.model_new';
export class StatusDynamicModel extends BaseModel {
	Id_row: number;
	StatusName: string;
	Title: string;
	Description: string;
	Id_project_team: number;
	id_department : number;
	Type: string;
	IsDefault: boolean;
	Color: string;
	Position: number;
	Follower: string;
	IsDeadline: boolean;
	clear() {
		this.Id_row = 0;
		this.StatusName = '';
		this.Title = '';
		this.Id_project_team = 0;
		this.id_department = 0;
		this.Type = '2';
		this.Description = '';
		this.Color = '';
		this.IsDefault = true;
		this.Position = 0;
		this.Follower = '0';
		this.IsDefault = false;
	}
}
