import {environment} from 'src/environments/environment';
import {QueryResultsModel} from './../../../_metronic/jeework_old/core/_base/crud/models/query-models/query-results.model';
import {HttpUtilsService} from './../../../_metronic/jeework_old/core/_base/crud/utils/http-utils.service';
import {QueryParamsModelNew} from './../../../_metronic/jeework_old/core/models/query-models/query-params.model';
import {QueryParamsModel} from './../../../_metronic/jeework_old/core/_base/crud/models/query-models/query-params.model';
import {HttpClient} from '@angular/common/http';
import {Observable, forkJoin, BehaviorSubject, of} from 'rxjs';
import {map, retry} from 'rxjs/operators';
import {Injectable} from '@angular/core';

const API_topic = environment.APIROOTS + '/api/topic';
const API_My_Work = environment.APIROOTS + '/api/personal';

@Injectable()
export class DiscussionsService {
    lastFilter$: BehaviorSubject<QueryParamsModel> = new BehaviorSubject(new QueryParamsModel({}, 'asc', '', 0, 10));
    ReadOnlyControl: boolean;

    // khúc này giao tiếp service giữa các component
    messageSource = new BehaviorSubject<any>(false);
    currentMessage = this.messageSource.asObservable();
    changeMessage(message) {
        this.messageSource.next(message);
    }
    // end

    constructor(private http: HttpClient,
                private httpUtils: HttpUtilsService) {
    }

    findListTopic(queryParams: QueryParamsModelNew): Observable<QueryResultsModel> {
        const httpHeaders = this.httpUtils.getHTTPHeaders();
        const httpParams = this.httpUtils.getFindHTTPParams(queryParams);
        const url = API_topic + '/list';
        return this.http.get<QueryResultsModel>(url, {
            headers: httpHeaders,
            params: httpParams
        });
    }

    TopicDetail(id: any): Observable<any> {
        const httpHeaders = this.httpUtils.getHTTPHeaders();
        const url = `${API_topic}/detail?id=${id}`;
        return this.http.get<any>(url, {headers: httpHeaders});
    }

    Add_Followers(topic: number, user: number): Observable<any> {
        const httpHeaders = this.httpUtils.getHTTPHeaders();
        const url = `${API_topic}/add-follower?topic=${topic}&&user=${user}`;
        return this.http.get<any>(url, {headers: httpHeaders});
    }

    Delete_Followers(topic: number, user: number): Observable<any> {
        const httpHeaders = this.httpUtils.getHTTPHeaders();
        const url = `${API_topic}/Remove-follower?topic=${topic}&&user=${user}`;
        return this.http.get<any>(url, {headers: httpHeaders});
    }

    InsertTopic(item): Observable<any> {
        const httpHeaders = this.httpUtils.getHTTPHeaders();
        return this.http.post<any>(API_topic + '/Insert', item, {headers: httpHeaders});
    }

    UpdateTopic(item): Observable<any> {
        const httpHeaders = this.httpUtils.getHTTPHeaders();
        return this.http.post<any>(API_topic + '/Update', item, {headers: httpHeaders});
    }

    Delete_Topic(id: number): Observable<any> {
        const httpHeaders = this.httpUtils.getHTTPHeaders();
        const url = `${API_topic}/Delete?id=${id}`;
        return this.http.get<any>(url, {headers: httpHeaders});
    }

    FollowTopic(id: number): Observable<any> {
        const httpHeaders = this.httpUtils.getHTTPHeaders();
        const url = `${API_My_Work}/follow-topic?id=${id}`;
        return this.http.get<any>(url, {headers: httpHeaders});
    }

    favouriteTopic(id: number): Observable<any> {
        const httpHeaders = this.httpUtils.getHTTPHeaders();
        const url = `${API_My_Work}/favourite-topic?id=${id}`;
        return this.http.get<any>(url, {headers: httpHeaders});
    }
}
