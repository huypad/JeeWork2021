import { BehaviorSubject, Subject } from 'rxjs';
import { Injectable } from '@angular/core';

@Injectable()
export class CommunicateService {

  constructor() { }
  // khúc này giao tiếp service giữa các component
  messageSource = new BehaviorSubject<any>(false);
  currentMessage = this.messageSource.asObservable();
  changeMessage(message) {
    this.messageSource.next(message);
  }
  //end
}
