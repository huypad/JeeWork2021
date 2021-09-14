import {Component, OnInit, ChangeDetectionStrategy} from '@angular/core';
import {CommonService} from '../../../../_metronic/jeework_old/core/services/common.service';
import {Router} from '@angular/router';

@Component({
    selector: 'kt-userright',
    templateUrl: './userright.component.html',
    changeDetection: ChangeDetectionStrategy.OnPush
})
export class UserRightComponent implements OnInit {
    constructor(
        private commonService: CommonService,
        private router: Router,
    ) {
    }

    ngOnInit() {
        if (this.commonService.CheckRole_WeWork(3900).length === 0) {
            this.router.navigate(['']);
        }
    }
}
