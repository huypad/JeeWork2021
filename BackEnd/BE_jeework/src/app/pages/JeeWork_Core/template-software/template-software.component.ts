import {Component, OnInit, ChangeDetectionStrategy} from '@angular/core';
import {CommonService} from '../../../_metronic/jeework_old/core/services/common.service';
import {Router} from '@angular/router';

@Component({
    selector: 'kt-template-software',
    templateUrl: './template-software.component.html',
    changeDetection: ChangeDetectionStrategy.OnPush
})
export class TemplateSoftwareComponent implements OnInit {
    constructor(
        private commonService: CommonService,
        private router: Router,
    ) {
    }

    ngOnInit() {
        if (this.commonService.CheckRole_WeWork(3901).length === 0) {
            this.router.navigate(['']);
        }
    }
}
