import { ProjectsTeamService } from './../JeeWork_Core/projects-team/Services/department-and-project.service';
import { MatDialog } from '@angular/material/dialog';
import { Router, ActivatedRoute } from '@angular/router';
import { Component, OnInit } from '@angular/core';
import { WorkListNewDetailComponent } from '../JeeWork_Core/projects-team/work-list-new/work-list-new-detail/work-list-new-detail.component';
import { MessageType } from 'src/app/_metronic/jeework_old/core/_base/crud';
import { LayoutUtilsService } from 'src/app/_metronic/jeework_old/core/utils/layout-utils.service';

@Component({
    selector: 'app-auxiliary-router',
    templateUrl: './auxiliary-router.component.html',
    styleUrls: ['./auxiliary-router.component.scss'],

})
export class AuxiliaryRouterComponent implements OnInit {

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
            this.projectsTeamService.WorkDetail(ID).subscribe(res => {
                if (res && res.status === 1) {
                    if (AuxiliaryRouterComponent.dialogRef == null)
                        this.openDialog(res.data);
                } else {
                    // alert(res.error.message);
                    this.close();
                    this.layoutUtilsService.showActionNotification(res.error.message, MessageType.Update, 9999999999, true, false, 3000, 'top', 0);
                    this.router.navigate(['/error']);
                }
            });
        });
    }
    close() {
        this.router.navigate(['', { outlets: { auxName: null } }]);
        AuxiliaryRouterComponent.dialogRef = null;
    }
    public static dialogRef = null;
    openDialog(item) {
        item.notback = true;
        item.notloading = true;
        AuxiliaryRouterComponent.dialogRef = this.dialog.open(WorkListNewDetailComponent, {
            width: '100vw',
            maxWidth: '100vw',
            height: '100vh',
            data: item,
            disableClose: true
        });
        AuxiliaryRouterComponent.dialogRef.afterClosed().subscribe(result => {
            this.close();
            AuxiliaryRouterComponent.dialogRef = null;
        });
    }
}
