import {HttpUtilsService} from './../../../_metronic/jeework_old/core/_base/crud/utils/http-utils.service';
import {QueryResultsModel} from './../../../_metronic/jeework_old/core/_base/crud/models/query-models/query-results.model';
import {QueryParamsModelNew} from './../../../_metronic/jeework_old/core/models/query-models/query-params.model';
import {environment} from 'src/environments/environment';
import {HttpClient} from '@angular/common/http';
import {Injectable} from '@angular/core';
import {Observable} from 'rxjs';

@Injectable({
    providedIn: 'root'
})
export class ReportProjectService {

    constructor(
        public http: HttpClient,
        public httpUtils: HttpUtilsService
    ) {
    }

    RootURL = environment.APIROOTS + '/api/reportbyprojects/';

    GetTrangthai(queryParams: QueryParamsModelNew): Observable<any> {
        const httpHeaders = this.httpUtils.getHTTPHeaders();
        const httpParams = this.httpUtils.getFindHTTPParams(queryParams);
        const url = this.RootURL + 'trang-thai-cv';
        return this.http.get<QueryResultsModel>(url, {
            headers: httpHeaders,
            params: httpParams
        });
    }

    GetQuaTrinh(queryParams: QueryParamsModelNew): Observable<QueryResultsModel> {
        const httpHeaders = this.httpUtils.getHTTPHeaders();
        const httpParams = this.httpUtils.getFindHTTPParams(queryParams);
        const url = this.RootURL + 'qua-trinh-hoan-thanh';
        return this.http.get<QueryResultsModel>(url, {
            headers: httpHeaders,
            params: httpParams
        });
    }

    GetTongHopTheoTuan(queryParams: QueryParamsModelNew): Observable<QueryResultsModel> {
        const httpHeaders = this.httpUtils.getHTTPHeaders();
        const httpParams = this.httpUtils.getFindHTTPParams(queryParams);
        const url = this.RootURL + 'tong-hop-theo-tuan';
        return this.http.get<QueryResultsModel>(url, {
            headers: httpHeaders,
            params: httpParams
        });
    }

    GetTongHopTheoDuan(queryParams: QueryParamsModelNew): Observable<QueryResultsModel> {
        const httpHeaders = this.httpUtils.getHTTPHeaders();
        const httpParams = this.httpUtils.getFindHTTPParams(queryParams);
        const url = this.RootURL + 'tong-hop-du-an';
        return this.http.get<QueryResultsModel>(url, {
            headers: httpHeaders,
            params: httpParams
        });
    }

    GetOverview(queryParams: QueryParamsModelNew): Observable<any> {
        const httpHeaders = this.httpUtils.getHTTPHeaders();
        const httpParams = this.httpUtils.getFindHTTPParams(queryParams);
        const url = this.RootURL + 'overview';
        return this.http.get<QueryResultsModel>(url, {
            headers: httpHeaders,
            params: httpParams
        });
    }

    GetPhanbocongviecDepartment(queryParams: QueryParamsModelNew): Observable<QueryResultsModel> {
        const httpHeaders = this.httpUtils.getHTTPHeaders();
        const httpParams = this.httpUtils.getFindHTTPParams(queryParams);
        const url = this.RootURL + 'phan-bo-theo-department';
        return this.http.get<QueryResultsModel>(url, {
            headers: httpHeaders,
            params: httpParams
        });
    }

    GetMuctieuDepartment(queryParams: QueryParamsModelNew): Observable<QueryResultsModel> {
        const httpHeaders = this.httpUtils.getHTTPHeaders();
        const httpParams = this.httpUtils.getFindHTTPParams(queryParams);
        const url = this.RootURL + 'muc-tieu-theo-department';
        return this.http.get<QueryResultsModel>(url, {
            headers: httpHeaders,
            params: httpParams
        });
    }

    TagClouds(queryParams: QueryParamsModelNew): Observable<QueryResultsModel> {
        const httpHeaders = this.httpUtils.getHTTPHeaders();
        const httpParams = this.httpUtils.getFindHTTPParams(queryParams);
        const url = this.RootURL + 'TagCloud';
        return this.http.get<QueryResultsModel>(url, {
            headers: httpHeaders,
            params: httpParams
        });
    }

    ExportExcel(filename: string): Observable<any> {
        const httpHeaders = this.httpUtils.getHTTPHeaders();
        const url = this.RootURL + 'ExportExcel?FileName=' + filename;
        return this.http.get(url, {
            headers: httpHeaders,
            responseType: 'blob',
            observe: 'response'
        });
    }

    ReportByStaff(queryParams: QueryParamsModelNew): Observable<QueryResultsModel> {
        const httpHeaders = this.httpUtils.getHTTPHeaders();
        const httpParams = this.httpUtils.getFindHTTPParams(queryParams);
        const url = this.RootURL + 'report-by-staff';
        return this.http.get<QueryResultsModel>(url, {
            headers: httpHeaders,
            params: httpParams
        });
    }

    ReportByConditions(queryParams: QueryParamsModelNew): Observable<QueryResultsModel> {
        const httpParams = this.httpUtils.getFindHTTPParams(queryParams);
        const httpHeaders = this.httpUtils.getHTTPHeaders();
        const url = this.RootURL + 'report-by-conditions';
        return this.http.get<QueryResultsModel>(url, {
            headers: httpHeaders,
            params: httpParams
        });
    }

    ReportByMilestone(queryParams: QueryParamsModelNew): Observable<QueryResultsModel> {
        const httpParams = this.httpUtils.getFindHTTPParams(queryParams);
        const httpHeaders = this.httpUtils.getHTTPHeaders();
        const url = this.RootURL + 'milestone';
        return this.http.get<QueryResultsModel>(url, {
            headers: httpHeaders,
            params: httpParams
        });
    }

    TopMilestone(queryParams: QueryParamsModelNew): Observable<QueryResultsModel> {
        const httpParams = this.httpUtils.getFindHTTPParams(queryParams);
        const httpHeaders = this.httpUtils.getHTTPHeaders();
        const url = this.RootURL + 'top_milestone';
        return this.http.get<QueryResultsModel>(url, {
            headers: httpHeaders,
            params: httpParams
        });
    }

    ReportByDepartment(queryParams: QueryParamsModelNew): Observable<QueryResultsModel> {
        const httpParams = this.httpUtils.getFindHTTPParams(queryParams);
        const httpHeaders = this.httpUtils.getHTTPHeaders();
        const url = this.RootURL + 'report-by-department';
        return this.http.get<QueryResultsModel>(url, {
            headers: httpHeaders,
            params: httpParams
        });
    }

    ReportByProjectTeam(queryParams: QueryParamsModelNew): Observable<QueryResultsModel> {
        const httpParams = this.httpUtils.getFindHTTPParams(queryParams);
        const httpHeaders = this.httpUtils.getHTTPHeaders();
        const url = this.RootURL + 'report-by-project';
        return this.http.get<QueryResultsModel>(url, {
            headers: httpHeaders,
            params: httpParams
        });
    }

    ReportToDepartments(queryParams: QueryParamsModelNew): Observable<QueryResultsModel> {
        const httpParams = this.httpUtils.getFindHTTPParams(queryParams);
        const httpHeaders = this.httpUtils.getHTTPHeaders();
        const url = this.RootURL + 'report-to-departments';
        return this.http.get<QueryResultsModel>(url, {
            headers: httpHeaders,
            params: httpParams
        });
    }

    CacConSoThongKe(queryParams: QueryParamsModelNew): Observable<QueryResultsModel> {
        const httpParams = this.httpUtils.getFindHTTPParams(queryParams);
        const httpHeaders = this.httpUtils.getHTTPHeaders();
        const url = this.RootURL + 'cac-con-so-thong-ke';
        return this.http.get<QueryResultsModel>(url, {
            headers: httpHeaders,
            params: httpParams
        });
    }

    Eisenhower(queryParams: QueryParamsModelNew): Observable<QueryResultsModel> {
        const httpParams = this.httpUtils.getFindHTTPParams(queryParams);
        const httpHeaders = this.httpUtils.getHTTPHeaders();
        const url = this.RootURL + 'eisenhower';
        return this.http.get<QueryResultsModel>(url, {
            headers: httpHeaders,
            params: httpParams
        });
    }
}
