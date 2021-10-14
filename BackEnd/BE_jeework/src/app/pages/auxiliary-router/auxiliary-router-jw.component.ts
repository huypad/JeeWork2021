import { ProjectsTeamService } from '../JeeWork_Core/projects-team/Services/department-and-project.service';
import { MatDialog } from '@angular/material/dialog';
import { Router, ActivatedRoute } from '@angular/router';
import { Component, OnInit } from '@angular/core';
import { WorkListNewDetailComponent } from '../JeeWork_Core/projects-team/work-list-new/work-list-new-detail/work-list-new-detail.component';
import { MessageType } from 'src/app/_metronic/jeework_old/core/_base/crud';
import { LayoutUtilsService } from 'src/app/_metronic/jeework_old/core/utils/layout-utils.service';

@Component({
    selector: 'app-auxiliary-router',
    templateUrl: './auxiliary-router.component.html',
})
export class AuxiliaryRouterJWComponent implements OnInit {

    constructor(
        private router: Router,
        public dialog: MatDialog,
        public projectsTeamService: ProjectsTeamService,
        private activatedRoute: ActivatedRoute,
        private layoutUtilsService: LayoutUtilsService,

    ) {
    }

    ngOnInit() {
        this.activatedRoute.params.subscribe(params => {
            const ID = params.id;
            this.LoadDetailTask(ID);
        });
    }

    LoadDetailTask(id) {
        this.projectsTeamService.WorkDetail(id).subscribe(res => {
            if (res && res.status === 1) {
                this.openDialogJW(res.data);
            } else {
                this.close();
                // alert(res.error.message);
                this.layoutUtilsService.showActionNotification(
                    res.error.message,
                    MessageType.Update,
                    9999999999,
                    true,
                    false,
                    3000,
                    'top',
                    0
                );
            }
        });
    }

    close() {
        this.router.navigate(['', { outlets: { auxName: null } }]);
    }

    openDialogJW(item) {
        const dialogRef = this.dialog.open(WorkListNewDetailComponent, {
            width: '90vw',
            height: '90vh',
            data: item
        });

        dialogRef.afterClosed().subscribe(result => {
            this.close();
        });
    }
}
