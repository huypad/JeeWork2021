import { QueryParamsModelNew } from './../../../_metronic/jeework_old/core/models/query-models/query-params.model';
import { LayoutUtilsService } from './../../../_metronic/jeework_old/core/utils/layout-utils.service';
import { QueryResultsModel } from './../../../_metronic/jeework_old/core/_base/crud/models/query-models/query-results.model';
import { BaseDataSource } from './../../../_metronic/jeework_old/core/_base/crud/models/_base.datasource';
import { of, BehaviorSubject } from 'rxjs';
import { catchError, finalize, tap } from 'rxjs/operators';
import { WorkService } from './work.service';


export class WorkDataSource extends BaseDataSource {
	constructor(private _service: WorkService,
		private layoutUtilsService: LayoutUtilsService,) {
		super();
	}
	loadList(queryParams: QueryParamsModelNew) {
		this._service.lastFilter$.next(queryParams);
		this.loadingSubject.next(true);
		this.layoutUtilsService.showWaitingDiv();
		this._service.findData(queryParams)
			.pipe(
				tap(resultFromServer => {
					this.layoutUtilsService.OffWaitingDiv();
					this.entitySubject.next(resultFromServer.data);
					var totalCount = resultFromServer.page.TotalCount || (resultFromServer.page.AllPage * resultFromServer.page.Size);
					this.paginatorTotalSubject.next(totalCount);
				}),
				catchError(err => of(new QueryResultsModel([], err))),
				finalize(() => this.loadingSubject.next(false))
			).subscribe(res => {
			});
	}
	loadRepeatedList(queryParams: QueryParamsModelNew) {
		this._service.lastFilter$.next(queryParams);
		this.loadingSubject.next(true);
		this.layoutUtilsService.showWaitingDiv();
		this._service.findDataRepeated(queryParams)
			.pipe(
				tap(resultFromServer => {
					this.layoutUtilsService.OffWaitingDiv();
					this.entitySubject.next(resultFromServer.data);
					var totalCount = resultFromServer.page.TotalCount || (resultFromServer.page.AllPage * resultFromServer.page.Size);
					this.paginatorTotalSubject.next(totalCount);
				}),
				catchError(err => of(new QueryResultsModel([], err))),
				finalize(() => this.loadingSubject.next(false))
			).subscribe(res => {
			});
	}
}
