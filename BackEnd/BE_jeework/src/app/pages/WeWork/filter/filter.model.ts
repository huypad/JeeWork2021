import { BaseModel } from './../../../_metronic/jeework_old/core/_base/crud/models/_base.model';

export class FilterModel extends BaseModel {
	id_row: number;
	title: string;
	color: string;
	loai: string;
	operators: string;
	options: string;
	value: string;
	details: Array<FilterDetailModel> = [];
	clear() {
		this.id_row = 0;
		this.title = '';
		this.color = '';
		this.loai = '0';
		this.details = [];
		this.operators = '';
		this.options = '';
		this.value = '';
	}
}

export class FilterDetailModel extends BaseModel {
	id_row: number;
	id_key: number;
	operator: string;
	value: string;
	title: string;
	StrTitle: string;
	clear() {
		this.id_row = 0;
		this.id_key = 0;
		this.operator = '';
		this.value = '';
		this.title = '';
		this.StrTitle = '';
	}
}