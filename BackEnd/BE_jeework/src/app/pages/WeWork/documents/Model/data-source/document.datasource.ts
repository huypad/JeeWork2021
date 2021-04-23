import { QueryResultsModel } from './../../../../../_metronic/jeework_old/core/_base/crud/models/query-models/query-results.model';
import { QueryParamsModelNew } from './../../../../../_metronic/jeework_old/core/models/query-models/query-params.model';
import { BaseDataSource } from './../../../../../_metronic/jeework_old/core/_base/crud/models/_base.datasource';
import { DocumentsService } from './../../documents.service';
import { of } from 'rxjs';
import { catchError, finalize, tap } from 'rxjs/operators'; 

export class DocumentDataSource extends BaseDataSource {
	constructor(private _service: DocumentsService) {
		super();
	}
	loadListDocument(queryParams: QueryParamsModelNew) {
		this._service.lastFilter$.next(queryParams);
		this.loadingSubject.next(true);

		this._service.ListDocuments(queryParams)
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
