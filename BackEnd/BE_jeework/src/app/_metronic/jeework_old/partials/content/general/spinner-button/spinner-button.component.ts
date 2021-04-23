import { Component, OnInit, Input, ChangeDetectionStrategy } from '@angular/core';
import { SpinnerButtonOptions } from './button-options.interface';

@Component({
	selector: 'kt-spinner-button',
	templateUrl: './spinner-button.component.html',
	styleUrls: ['./spinner-button.component.scss'],
})
export class SpinnerButtonComponent implements OnInit {
	@Input() options: SpinnerButtonOptions;

	constructor() {}

	ngOnInit() {}
}
