// Angular
import { Injectable } from '@angular/core';
// RxJS
import { mergeMap, map, tap } from 'rxjs/operators';
import { Observable, defer, of, forkJoin } from 'rxjs';
// NGRX
import { Effect, Actions, ofType } from '@ngrx/effects';
import { Store, select, Action } from '@ngrx/store';
// CRUD
import { QueryResultsModel, QueryParamsModel } from '../../_base/crud';
// Services
import { AuthenticationService } from '../../../core/auth/_services';
// State
import { AppState } from '../../../core/reducers';
import {
    UserActionTypes,
    UsersPageRequested,
    UsersPageLoaded,
    UserCreated,
    UserDeleted,
    UserUpdated,
    UserOnServerCreated,
    UsersActionToggleLoading,
    UsersPageToggleLoading
} from '../_actions/user.actions';

@Injectable()
export class UserEffects {
    showPageLoadingDistpatcher = new UsersPageToggleLoading({ isLoading: true });
    hidePageLoadingDistpatcher = new UsersPageToggleLoading({ isLoading: false });

    showActionLoadingDistpatcher = new UsersActionToggleLoading({ isLoading: true });
    hideActionLoadingDistpatcher = new UsersActionToggleLoading({ isLoading: false });
    constructor(private actions$: Actions, private auth: AuthenticationService, private store: Store<AppState>) { }
}
