import { BaseModel } from './../../../../../_metronic/jeework_old/core/_base/crud/models/_base.model';

export class GroupNameModel extends BaseModel {
	ID_Nhom: number;
	TenNhom: string;
	Module: string;
	clear() {
		this.ID_Nhom = 0;
		this.TenNhom = '';
		this.Module = '';
	}
}

export class QuyenAddData extends BaseModel {
	ID: string;
	ID_NhomChucNang: number;
	Ten: string;
	ID_Quyen: number;
	TenQuyen: string;
	IsEdit: boolean;
	IsRead: boolean;
	IsGroup: boolean;
	clear() {
		this.ID = '';
		this.ID_NhomChucNang = 0;
		this.Ten = '';
		this.ID_Quyen = 0;
		this.TenQuyen = '';
		this.IsEdit = false;
		this.IsRead = false;
		this.IsGroup = false;
	}
}

export class UserModel extends BaseModel {
	ID_NV: number;
	HoTen: string;
	TenDangNhap: string;
	CheckChon: boolean;
	clear() {
		this.ID_NV = 0;
		this.HoTen = '';
		this.TenDangNhap = '';
	}
}

export class UserAddData extends BaseModel {
	ID_Nhom: number;
	UserName: string;
	clear() {
		this.ID_Nhom = 0;
		this.UserName = '';
	}
}
