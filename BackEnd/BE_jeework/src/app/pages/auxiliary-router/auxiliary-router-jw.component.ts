import { ProjectsTeamService } from '../JeeWork_Core/projects-team/Services/department-and-project.service';
import { MatDialog } from '@angular/material/dialog';
import { Router, ActivatedRoute, RouterStateSnapshot } from '@angular/router';
import { Component, OnInit } from '@angular/core';
import { WorkListNewDetailComponent } from '../JeeWork_Core/projects-team/work-list-new/work-list-new-detail/work-list-new-detail.component';
import { MessageType } from 'src/app/_metronic/jeework_old/core/_base/crud';
import { LayoutUtilsService } from 'src/app/_metronic/jeework_old/core/utils/layout-utils.service';
import { BehaviorSubject, Observable } from 'rxjs';

@Component({
    selector: 'app-auxiliary-router-jw',
    templateUrl: './auxiliary-router.component.html',
})
export class AuxiliaryRouterJWComponent implements OnInit {
    loadingSubject = new BehaviorSubject<boolean>(true);
    loading$: Observable<boolean>;
    constructor(
        private router: Router,
        public dialog: MatDialog,
        public projectsTeamService: ProjectsTeamService,
        private activatedRoute: ActivatedRoute,
        private layoutUtilsService: LayoutUtilsService,

    ) {
        const snapshot: RouterStateSnapshot = router.routerState.snapshot;
        console.log("snapshot-AuxiliaryRouterJWComponent", snapshot);  // <-- hope it helps
    }
    ngOnInit() {
        this.loading$ = this.loadingSubject.asObservable();
        this.loadingSubject.next(true);
        this.activatedRoute.params.subscribe(async params => {
            this.loadingSubject.next(false);
            const ID = params['id'];
            if (ID && ID > 0) {
                this.projectsTeamService.WorkDetail(ID).subscribe(res => {
                    if (res && res.status === 1) {
                        console.log("activatedRoute", AuxiliaryRouterJWComponent.dialogRef);
                        if (AuxiliaryRouterJWComponent.dialogRef == null)
                            this.openDialogJW(res.data);
                    } else {
                        console.log("activatedRoute-close", res);

                        this.close();
                        // alert(res.error.message);
                        this.layoutUtilsService.showActionNotification(res.error.message, MessageType.Update, 9999999999, true, false, 3000, 'top', 0);
                    }
                });
            }
        });
    }
    close() {
        this.router.navigate(['', { outlets: { auxName: null } }]);
        AuxiliaryRouterJWComponent.dialogRef = null;

    }
    public static dialogRef = null;// chỗ này m đem ra biến cục bộ dạng static đó! ý nghĩa là kt nếu đang tồn tại dialog này thì không cần kt code trong sub nữa! => hiện tại là cách fix tam thời (chưa tìm đc chỗ gây lỗi)
    openDialogJW(item) {
        AuxiliaryRouterJWComponent.dialogRef = this.dialog.open(WorkListNewDetailComponent, {
            width: '90vw',
            height: '90vh',
            data: item,
            disableClose: true
        });
        AuxiliaryRouterJWComponent.dialogRef.afterClosed().subscribe(result => {
            this.close();
            AuxiliaryRouterJWComponent.dialogRef = null;
        });
    }
}
