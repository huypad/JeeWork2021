// Angular
import { Component, OnInit, ChangeDetectionStrategy, OnDestroy, ChangeDetectorRef, Inject, Input, Output, EventEmitter, ViewEncapsulation, OnChanges } from '@angular/core';
import { FormBuilder } from '@angular/forms';
// Material
import { MatDialog  } from '@angular/material/dialog';
// RxJS
// NGRX
// Service
import { LayoutUtilsService } from './../../../_metronic/jeework_old/core/utils/layout-utils.service';

//Models

import { JeeWorkLiteService } from '../services/wework.services';

@Component({
	selector: 'kt-color-picker',
	templateUrl: './color-picker.component.html',
	changeDetection: ChangeDetectionStrategy.OnPush,
	encapsulation: ViewEncapsulation.None
})

export class ColorPickerComponent implements OnInit, OnChanges {
	// Public properties
	@Input() selected: string;
	@Output() ItemSelected = new EventEmitter<any>();

	public defaultColors: string[] = [
		'bg1',
		'bg2',
		'bg3',
		'bg4',
		'bg5',
		'bg6',
		'bg7',
		'bg8',
		'bg9',
		'bg10',
	];
	constructor(
		private FormControlFB: FormBuilder,
		public dialog: MatDialog,
		private layoutUtilsService: LayoutUtilsService,
		public weworkService: JeeWorkLiteService,
		private changeDetectorRefs: ChangeDetectorRef) { }

	/**
	 * On init
	 */
	ngOnInit() {
	}
	ngOnChanges() {

	}
	select(user) {
		this.ItemSelected.emit(user)
	}
}
