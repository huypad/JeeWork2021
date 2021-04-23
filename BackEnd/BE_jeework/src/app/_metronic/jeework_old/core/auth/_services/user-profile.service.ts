import { HttpClient } from '@angular/common/http';
import { Observable,  BehaviorSubject } from 'rxjs';
import { Injectable } from '@angular/core';
import { environment } from 'src/environments/environment';
import { HttpUtilsService } from '../../_base/crud/utils/http-utils.service';
import { QueryParamsModel, QueryResultsModel } from '../../_base/crud';
import { MasterPageNhanVienModel } from '../_models/user.model';
import { map } from 'rxjs/operators';

const API_DashBoard = environment.ApiRootsLanding + '/dashboard';
@Injectable()
export class UserProfileService {
	lastFilter$: BehaviorSubject<QueryParamsModel> = new BehaviorSubject(new QueryParamsModel({}, 'asc', '', 0, 10));
	ReadOnlyControl: boolean;
	constructor(private http: HttpClient,
		private httpUtils: HttpUtilsService) { }

	
	// getHinhAnhByID(Id: number): Observable<MasterPageNhanVienModel> {
	// 	const httpHeaders = this.httpUtils.getHTTPHeaders();
	// 	return this.http.get<any>(API_PRODUCTS_URL2 + `/Get_Hinh_Login?id_nv=${Id}`, { headers: httpHeaders }).pipe(
	// 		map(res => {
	// 			if (res && res.status === 1) {
	// 				let nhomtc = new MasterPageNhanVienModel();
	// 				nhomtc.clear();
	// 				Object.assign(nhomtc, {
	// 					MaNV: '' + res.MaNV,
	// 					HoTen: '' + res.HoTen,
	// 					Image: '' + res.Image,
	// 					DefaultModuleID: '' + res.DefaultModuleID,
	// 					ChucVu: '' + res.ChucVu,
	// 				});
	// 				return nhomtc;
	// 			}
	// 		})
	// 	);
	// }

    // // Check session 
    // LogOut(): Observable<any> {
	// 	const httpHeaders = this.httpUtils.getHTTPHeaders();
	// 	return this.http.get<any>(API_PRODUCTS_URL_1 + '/LogOut', { headers: httpHeaders });
	// }
	getDictionary(): Observable<any> {
		const httpHeaders = this.httpUtils.getHTTPHeaders();
		debugger
		return this.http.get<any>(environment.ApiRoots + `api/wework-lite/get-dicionary`, { headers: httpHeaders });
	}
	// Get_ListModule_New() {
	// 	const httpHeaders = this.httpUtils.getHTTPHeaders();
	// 	return this.http.get<any>(environment.ApiRoot + `/controllergeneral/GetListModuleNew`, { headers: httpHeaders });
	// }
	// Get_DSThongBao(appcode:string,langcode:string) {
	// 	const httpHeaders = this.httpUtils.getHTTPHeaders();
	// 	return this.http.get<any>(API_DashBoard + `/Get_DSThongBao?appcode=${appcode}&langcode=${langcode}`, { headers: httpHeaders });
	// }
	// Get_DSNhacNho(appcode:string,langcode:string) {
	// 	const httpHeaders = this.httpUtils.getHTTPHeaders();
	// 	return this.http.get<any>(API_DashBoard + `/Get_DSNhacNho?appcode=${appcode}&langcode=${langcode}`, { headers: httpHeaders });
	// }
}
