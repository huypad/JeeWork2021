import { CommonService } from './../../../../_metronic/jeework_old/core/services/common.service';
import { Component, OnInit, ChangeDetectionStrategy } from '@angular/core';
import { Router } from '@angular/router';

@Component({
	selector: 'kt-all-project-team',
	templateUrl: './all-project-team.component.html',
	changeDetection: ChangeDetectionStrategy.OnPush
})
export class AllProjectTeamComponent implements OnInit {
	activeLink = '';
	constructor(
		private router: Router,
		public commonService: CommonService,
	) {
		var path = this.router.url;
		if (path) {
			var arr = path.split('/');
			this.activeLink = arr[arr.length - 1];
		}
	}

	ngOnInit() {
	}
}
