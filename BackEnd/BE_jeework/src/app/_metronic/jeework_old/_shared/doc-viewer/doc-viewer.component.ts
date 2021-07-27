import { Component, Inject, OnInit, ChangeDetectionStrategy } from '@angular/core';
import { MAT_DIALOG_DATA } from '@angular/material/dialog';
import { timeout, delay } from 'rxjs/operators';
import { of } from 'rxjs';

@Component({
  selector: 'kt-doc-viewer',
  templateUrl: './doc-viewer.component.html',
  styleUrls: ['./doc-viewer.component.scss']
})
export class DocViewerComponent implements OnInit {

  constructor(
    // @Inject(MAT_SNACK_BAR_DATA) public data: any
    @Inject(MAT_DIALOG_DATA) public data: any,
  ) { }

	ngOnInit() {
	}
}
