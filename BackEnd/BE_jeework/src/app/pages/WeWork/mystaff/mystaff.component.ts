import { LayoutUtilsService } from './../../../_metronic/jeework_old/core/utils/layout-utils.service';
// Angular
import { Component, OnInit, ChangeDetectionStrategy, OnDestroy, ChangeDetectorRef, Inject, Input, Output, EventEmitter, ViewEncapsulation, OnChanges } from '@angular/core';
import { FormBuilder, FormGroup, Validators, FormControl } from '@angular/forms';
// Material
// RxJS
import { Observable, BehaviorSubject, Subscription, ReplaySubject } from 'rxjs';
// NGRX
// Service
//Models

import * as moment from 'moment';
import { WeWorkService } from '../services/wework.services';
import { Router } from '@angular/router';
import { TranslateService } from '@ngx-translate/core';

@Component({
	selector: 'kt-mystaff',
	templateUrl: './mystaff.component.html',
	changeDetection: ChangeDetectionStrategy.OnPush,
	encapsulation: ViewEncapsulation.None
})

export class MyStaffComponent implements OnInit {
	// Public properties
	@Input() data: any[] = []
	@Input() title: string = 'summary.nhanviencuatoi';
	constructor(
		private router: Router,
		private FormControlFB: FormBuilder,
		private layoutUtilsService: LayoutUtilsService,
		public WeWorkService: WeWorkService,
		private changeDetectorRefs: ChangeDetectorRef) { }

	/**
	 * On init
	 */
	ngOnInit() {
	}

	click(staff) {
		let url = '/users/' + staff.id_nv;
		this.router.navigateByUrl(url);
	}
}
