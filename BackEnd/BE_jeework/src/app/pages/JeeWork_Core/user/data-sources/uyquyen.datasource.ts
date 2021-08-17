import { QueryResultsModel } from '../../../../_metronic/jeework_old/core/_base/crud/models/query-models/query-results.model';
import { BaseDataSource } from '../../../../_metronic/jeework_old/core/_base/crud/models/_base.datasource';
import { of } from 'rxjs';
import { catchError, finalize, tap } from 'rxjs/operators';
import { QueryParamsModelNew } from '../../../../_metronic/jeework_old/core/models/query-models/query-params.model';
import { UserService } from '../Services/user.service';

export class UyQuyenDataSource extends BaseDataSource {
	constructor(private _service: UserService) {
		super();
	}
	loadList(queryParams: QueryParamsModelNew) {
		this._service.lastFilterUQ$.next(queryParams);
		this.loadingSubject.next(true);

		this._service.findDataUQ(queryParams)
			.pipe(
				tap(resultFromServer => {
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
