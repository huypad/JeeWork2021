import {Subject} from 'rxjs';

export class EventBusServiceLink {
    nodeCheckedChange = new Subject();
    pageRoutingChange = new Subject();
}
