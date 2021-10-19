import { environment } from 'src/environments/environment';
import { HttpUtilsService } from './../../../_metronic/jeework_old/core/_base/crud/utils/http-utils.service';
import { HttpClient } from '@angular/common/http';
import { Observable, BehaviorSubject } from 'rxjs';
import { Injectable } from '@angular/core';
const API_filter = environment.APIROOTS + '/api/filter';

@Injectable()
export class filterService {
	constructor(private http: HttpClient,
		private httpUtils: HttpUtilsService) { }
}
