import { QueryParamsModelNewLazy } from './../models/pagram';
import { QueryResultsModel } from './../../../_metronic/jeework_old/core/_base/crud/models/query-models/query-results.model';
import { AuthService } from 'src/app/modules/auth';
import { environment } from 'src/environments/environment';
import { Injectable } from '@angular/core';
import { BehaviorSubject, Observable } from 'rxjs';
import { ReplaySubject } from 'rxjs';
import { HttpClient, HttpHeaders, HttpParams } from '@angular/common/http';
import { PresenceService } from './presence.service';

@Injectable({
  providedIn: 'root'
})
export class ChatService {

}
