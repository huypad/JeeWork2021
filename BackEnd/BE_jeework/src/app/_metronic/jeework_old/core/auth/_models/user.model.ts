import { BaseModel } from '../../_base/crud';
import { Address } from './address.model';
import { SocialNetworks } from './social-networks.model';

export class User extends BaseModel {
    id: number;
    username: string;
    password: string;
    email: string;
    accessToken: string;
    refreshToken: string;
    roles: number[];
    pic: string;
    fullname: string;
    occupation: string;
	companyName: string;
	phone: string;
    address: Address;
    socialNetworks: SocialNetworks;

    clear(): void {
        this.id = undefined;
        this.username = '';
        this.password = '';
        this.email = '';
        this.roles = [];
        this.fullname = '';
        this.accessToken = 'access-token-' + Math.random();
        this.refreshToken = 'access-token-' + Math.random();
        this.pic = 'https://www.takadada.com/wp-content/uploads/2019/07/1-35.jpg';
        this.occupation = '';
        this.companyName = '';
        this.phone = '';
        this.address = new Address();
        this.address.clear();
        this.socialNetworks = new SocialNetworks();
        this.socialNetworks.clear();
    }
}

export class MasterPageNhanVienModel extends BaseModel {
	ID_NV: number;
	MaNV: string;
	HoTen: string;
	MaChamCong: string;
	HoLot: string;
	Ten: string;
	GioiTinh: string;
	NgaySinh: string;
	TenThuongGoi: string;
	ID_NoiSinh: number;
	NguyenQuan: string;
	ChucVu: string;
	Image: string;
	SoThe: string;
	strBase64: string;
	imgPath: string;
	DefaultModuleID: string;
	clear() {
		this.ID_NV = 0;
		this.MaNV = '';
		this.HoTen = '';
		this.MaChamCong = '';
		this.HoLot = ''
		this.Ten = '';
		this.GioiTinh = "";
		this.NgaySinh = "";
		this.TenThuongGoi = '';
		this.ID_NoiSinh = 0;
		this.NguyenQuan = '';
		this.ChucVu = '';
		this.Image = '';
		this.SoThe = '';
		this.strBase64 = '';
		this.imgPath = '';
		this.DefaultModuleID = '';
	}
}

