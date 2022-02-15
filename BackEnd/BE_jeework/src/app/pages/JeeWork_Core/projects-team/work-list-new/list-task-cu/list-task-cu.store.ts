import { ChangeDetectorRef, Component, Injectable, OnInit } from "@angular/core";
import { ActivatedRoute, Route, Router } from "@angular/router";
import { SubheaderService } from 'src/app/_metronic/partials/layout';
import { BehaviorSubject, Observable } from 'rxjs';

@Injectable({ providedIn: 'root' })
export class ListTasksStore  {
  private readonly _updateEvent = new BehaviorSubject<boolean>(false);
  readonly updateEvent$ = this._updateEvent.asObservable();
  get updateEvent(): boolean {
    return this._updateEvent.getValue();
  }
  set updateEvent(val: boolean) {
    this._updateEvent.next(val);
  }
  constructor() {

  }
}
