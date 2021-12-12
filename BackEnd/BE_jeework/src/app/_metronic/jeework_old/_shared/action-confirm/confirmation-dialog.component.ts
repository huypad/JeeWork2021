import { Component, EventEmitter, Input, OnInit, Output, ViewChild } from '@angular/core';
import { MatButton } from '@angular/material/button';
import { NgbActiveModal } from '@ng-bootstrap/ng-bootstrap';

@Component({
  selector: 'app-confirmation-dialog',
  templateUrl: './confirmation-dialog.component.html',
})
export class ConfirmationDialogComponent implements OnInit {

  @Input() title: string;
  @Input() message: string;
  @Input() btnOkText: string;
  @Input() btnSaveChange: string;
  @Input() btnCancelText: string;
  @ViewChild('btnRef') buttonRef: MatButton;
  constructor(public activeModal: NgbActiveModal) { }

  ngOnInit() {
    // this.buttonRef.focus();
  }

  public decline() {
    this.activeModal.close(false);
    // this.onCancel.emit();

  }
  ngAfterViewInit() {
    this.buttonRef.focus();
  }
  public accept() {
    this.activeModal.close(true);
  }

  public dismiss() {// save and close
    this.activeModal.dismiss('pad');
  }

}