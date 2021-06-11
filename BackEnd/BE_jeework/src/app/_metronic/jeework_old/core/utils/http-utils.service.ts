import { environment } from 'src/environments/environment';
import { TokenStorage } from './../auth/_services/token-storage.service';
import { Injectable } from '@angular/core';
import { map, catchError, tap, switchMap } from 'rxjs/operators';
import { HttpParams, HttpHeaders } from '@angular/common/http';
import { AuthService } from 'src/app/modules/auth';


@Injectable()
export class HttpUtilsService {
	public authLocalStorageToken = `${environment.appVersion}-${environment.USERDATA_KEY}`;
	fake_token = `eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJ1c2VySWQiOiI2MDZkMTI2OWI0MzA0NjAwMWRlODJmNmUiLCJ1c2VybmFtZSI6Imh1eXBhZCIsImN1c3RvbWRhdGEiOnsicGVyc29uYWxJbmZvIjp7IkF2YXRhciI6IiIsIkZpcnN0bmFtZSI6IiIsIkxhc3RuYW1lIjoiIiwiSm9idGl0bGUiOiIiLCJEZXBhcnRtZW50IjoiIiwiQmlydGhkYXkiOiIiLCJQaG9uZW51bWJlciI6IiJ9LCJqZWUtcmVxdWVzdCI6eyJyb2xlIjoiMSwyLDMifSwiamVlLXdvcmsiOnsicm9sZSI6IjEsMiwzIn19LCJpYXQiOjE2MTgyMDgzOTcsImV4cCI6MTYxODIxMTk5N30.seJpb-i852EuKgVu9e7sZSSM6eUICLhe7lzPXnIE5wM`;
	constructor(private tokenStorage: TokenStorage,private auth: AuthService) {
	}
	getFindHTTPParams(queryParams): HttpParams {
		let params = new HttpParams()
			//.set('filter',  queryParams.filter )
			.set('sortOrder', queryParams.sortOrder)
			.set('sortField', queryParams.sortField)
			.set('page', (queryParams.pageNumber + 1).toString())
			.set('record', queryParams.pageSize.toString());
		let keys = [], values = [];
		if (queryParams.more) {
			params = params.append('more', 'true');
		}
		Object.keys(queryParams.filter).forEach(function (key) {
			if (typeof queryParams.filter[key] !== 'string' || queryParams.filter[key] !== '') {
				keys.push(key);
				values.push(queryParams.filter[key]);
			}
		});
		if (keys.length > 0) {
			params = params.append('filter.keys', keys.join('|'))
				.append('filter.vals', values.join('|'));
		}
		return params;
	}

	parseFilter(data){
		var filter={
			keys:'',
			vals:''
		}
		let keys = [], values = [];
		Object.keys(data).forEach(function (key) {
			if (typeof data[key] !== 'string' || data[key] !== '') {
				keys.push(key);
				values.push(data[key]);
			}
		});
		if (keys.length > 0) {
			filter.keys= keys.join('|');
			filter.vals= values.join('|');
		}
		return filter;
	}

	getHTTPHeaders(): HttpHeaders {
		const auth = this.auth.getAuthFromLocalStorage();
		let result = new HttpHeaders({
			'Content-Type': 'application/json', 
			'Authorization': `Bearer ${auth!=null?auth.access_token:''}`,
			'Access-Control-Allow-Origin': '*',
			'Access-Control-Allow-Headers': 'Content-Type'
		});
		return result;
	}
	getHttpHeadersRefresh() {
		const auth = this.getAuthFromLocalStorage();
		var p = new HttpHeaders({
		  'Content-Type': 'application/json',
		  "Authorization": `Bearer ${auth!=null?auth.refresh_token:''}`
		});
		return p;

		
	  }
	  public getAuthFromLocalStorage(): any {
		try {
		  const authData = JSON.parse(
			localStorage.getItem(this.authLocalStorageToken)
		  );
		  return authData;
		} catch (error) {
		  console.error(error);
		  return undefined;
		}
	  }

	getHTTPHeaders_Das(isFormData? : boolean): HttpHeaders {
		var _token = '';
		this.tokenStorage.getAccessToken().subscribe(t => { _token = t; });
		let result = new HttpHeaders({
			'Token': _token
		});
		if (!isFormData) {
			result.append("Content-Type", "application/json");
		}
		return result;
	}
}
