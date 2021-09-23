import { of } from 'rxjs';
import { catchError, finalize, tap } from 'rxjs/operators';
import { templateSoftwareService } from '../../Services/template-software.service';
import { BaseDataSource } from '../../../../../_metronic/jeework_old/core/models/data-sources/_base.datasource';
import { QueryResultsModel } from '../../../../../_metronic/jeework_old/core/models/query-models/query-results.model';
import { QueryParamsModel } from '../../../../../_metronic/jeework_old/core/models/query-models/query-params.model';
// import { BaseDataSource } from '../../../../../../components/apps/e-commerce/_core/models/data-sources/_base.datasource';

export class TemplateSoftwareDataSource extends BaseDataSource {
	constructor(public _template: templateSoftwareService) {
		super();
	}
	LoadMohinhDuan(queryParams: QueryParamsModel) {
		this._template.lastFilter$.next(queryParams);
		this.loadingSubject.next(true);
		this._template.MohinhDuan(queryParams)
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
					this._template.Visible = res.Visible;
				}
			);
	}
}
