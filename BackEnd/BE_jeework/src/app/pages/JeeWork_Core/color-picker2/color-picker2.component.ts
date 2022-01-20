import { JeeWorkLiteService } from './../services/wework.services';
import {MatDialog} from '@angular/material/dialog';
import {FormBuilder} from '@angular/forms';
import {Component, OnInit, ChangeDetectorRef, Input, EventEmitter, Output} from '@angular/core';

@Component({
    selector: 'kt-color-picker2',
    templateUrl: './color-picker2.component.html',
    styleUrls: ['./color-picker2.component.scss']
})
export class ColorPicker2Component implements OnInit {

    // Public properties
    @Input() selected: string;
    @Output() ItemSelected = new EventEmitter<any>();

    public defaultColors: string[] = [
        '#848E9E',
        // 'rgb(187, 181, 181)',
        'rgb(29, 126, 236)',
        'rgb(250, 162, 140)',
        'rgb(14, 201, 204)',
        'rgb(11, 165, 11)',
        'rgb(123, 58, 245)',
        'rgb(238, 177, 8)',
        'rgb(236, 100, 27)',
        'rgb(124, 212, 8)',
        'rgb(240, 56, 102)',
        'rgb(255, 0, 0)',
        'rgb(0, 0, 0)',
        'rgb(255, 0, 255)',
    ];

    constructor(
        private FormControlFB: FormBuilder,
        public dialog: MatDialog,
        public weworkService: JeeWorkLiteService,
        private changeDetectorRefs: ChangeDetectorRef) {
    }

    /**
     * On init
     */
    ngOnInit() {
        // if(!this.selected){
        // 	this.select(this.defaultColors[0])
        // }
    }

    ngOnChanges() {

    }

    select(color) {
        this.selected = color;
        this.ItemSelected.emit(color);
    }
}
