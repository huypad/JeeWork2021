import { SparklineChartOptions } from './../../../../core/_base/layout/directives/sparkline-chart.directive';
import { Component, Input, OnInit } from '@angular/core';

@Component({
	selector: 'kt-widget26',
	templateUrl: './widget26.component.html',
	styleUrls: ['./widget26.component.scss']
})
export class Widget26Component implements OnInit {

	@Input() value: string | number;
	@Input() desc: string;
	@Input() options: SparklineChartOptions;

	constructor() {
	}

	ngOnInit() {
	}

}
