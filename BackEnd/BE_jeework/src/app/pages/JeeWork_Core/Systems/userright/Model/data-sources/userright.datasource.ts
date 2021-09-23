import { QueryResultsModel } from './../../../../../../_metronic/jeework_old/core/_base/crud/models/query-models/query-results.model';
import { BaseDataSource } from './../../../../../../_metronic/jeework_old/core/_base/crud/models/_base.datasource';
import { QueryParamsModel } from './../../../../../../_metronic/jeework_old/core/_base/crud/models/query-models/query-params.model';
import { of } from 'rxjs';
import { catchError, finalize, tap } from 'rxjs/operators';
import { PermissionService } from '../../Services/userright.service';
// import { BaseDataSource } from '../../../../../../components/apps/e-commerce/_core/models/data-sources/_base.datasource';

export class UserRightDataSource extends BaseDataSource {
	constructor(public permitService: PermissionService) {
		super();
	}
	LoadGroup(queryParams: QueryParamsModel) {
		this.permitService.lastFilter$.next(queryParams);
		this.loadingSubject.next(true);
		this.permitService.findDataGroup(queryParams)
			.pipe(
				tap(resultFromServer => {
					this.entitySubject.next(resultFromServer.data);
					var totalCount = resultFromServer.page.TotalCount || (resultFromServer.page.AllPage * resultFromServer.page.Size);
					this.paginatorTotalSubject.next(totalCount);
				}),
				catchError(err => of(new QueryResultsModel([], err))),
				finalize(() => this.loadingSubject.next(false))
			).subscribe(
				res => {
					this.permitService.Visible_Group = res.Visible;
				}
			);
	}

	loadListUsers(queryParams: QueryParamsModel) {
		this.permitService.lastFilter$.next(queryParams);
		this.loadingSubject.next(true);

		this.permitService.findDataUsers(queryParams)
			.pipe(
				tap(resultFromServer => {
					this.entitySubject.next(resultFromServer.data);
					var totalCount = resultFromServer.page.TotalCount || (resultFromServer.page.AllPage * resultFromServer.page.Size);
					this.paginatorTotalSubject.next(totalCount);
				}),
				catchError(err => of(new QueryResultsModel([], err))),
				finalize(() => this.loadingSubject.next(false))
			).subscribe(
				res => {
					this.permitService.Visible_User = res.Visible;
					this.permitService.Visible_UserSystem = res.Visible;
				}
			);
	}

	LoadListFunctions(queryParams: QueryParamsModel) {
		this.permitService.lastFilter$.next(queryParams);
		this.loadingSubject.next(true);

		this.permitService.findData_Functions(queryParams)
			.pipe(
				tap(resultFromServer => {
					this.entitySubject.next(resultFromServer.data);
					var totalCount = resultFromServer.page.TotalCount || (resultFromServer.page.AllPage * resultFromServer.page.Size);
					this.paginatorTotalSubject.next(totalCount);
				}),
				catchError(err => of(new QueryResultsModel([], err))),
				finalize(() => this.loadingSubject.next(false))
			).subscribe(
				res => {
					this.permitService.Visible_Functions = res.Visible;
				}
			);
	}
	loadList_UserGroup(queryParams: QueryParamsModel) {
		this.permitService.lastFilter$.next(queryParams);
		this.loadingSubject.next(true);

		this.permitService.findData_UserGroup(queryParams)
			.pipe(
				tap(resultFromServer => {
					this.entitySubject.next(resultFromServer.data);
					var totalCount = resultFromServer.page.TotalCount || (resultFromServer.page.AllPage * resultFromServer.page.Size);
					this.paginatorTotalSubject.next(totalCount);
				}),
				catchError(err => of(new QueryResultsModel([], err))),
				finalize(() => this.loadingSubject.next(false))
			).subscribe(
				res => {
					this.permitService.Visible_UserGroup = res.Visible;
				}
			);
	}
}
