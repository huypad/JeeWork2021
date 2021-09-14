import {Component, OnInit} from '@angular/core';
import {CommonService} from '../../../../_metronic/jeework_old/core/services/common.service';
import {Router} from '@angular/router';

@Component({
    selector: 'kt-report-list-tab',
    templateUrl: './report-list-tab.component.html',
})
export class ReportListTabComponent implements OnInit {

    activeLink = 'home';

    constructor(
        private commonService: CommonService,
        private router: Router,
    ) {
    }

    ngOnInit() {
        if (this.commonService.CheckRole_WeWork(3800).length === 0) {
            this.router.navigate(['']);
        }
    }

    click(activeLink) {
        this.activeLink = activeLink;
    }

    public Danhmuc = [
        {
            ten: 'Dashboard',
            url: '/reports'
        },
        {
            ten: 'Member',
            // url:'/reports'
            url: '/reports/member'
        }
    ];

}
