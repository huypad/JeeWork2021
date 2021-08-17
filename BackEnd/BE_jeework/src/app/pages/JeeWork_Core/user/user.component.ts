import { Component, OnInit, ChangeDetectionStrategy, ChangeDetectorRef } from '@angular/core';

@Component({
	selector: 'kt-user.component.html',
	templateUrl: './user.component.html',
	changeDetection: ChangeDetectionStrategy.OnPush
})
export class UserComponent implements OnInit {
	constructor() { }

	ngOnInit() {
	}
}
