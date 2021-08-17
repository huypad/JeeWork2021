import { WorkService } from './../../../work/work.service';
import { QueryResultsModel } from './../../../../../_metronic/jeework_old/core/_base/crud/models/query-models/query-results.model';
import { QueryParamsModelNew } from './../../../../../_metronic/jeework_old/core/models/query-models/query-params.model';
import { BaseDataSource } from './../../../../../_metronic/jeework_old/core/models/data-sources/_base.datasource';
import { of } from 'rxjs';
import { catchError, finalize, tap } from 'rxjs/operators';
import { ProjectsTeamService } from '../../Services/department-and-project.service';

export class WorkGroupDataSource extends BaseDataSource {
	constructor(private _service: WorkService) {
		super();
	}
	loadListWorkGroup(queryParams: QueryParamsModelNew) {
		this._service.lastFilter$.next(queryParams);
		this.loadingSubject.next(true);

		this._service.ListWorkGroup(queryParams)
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
