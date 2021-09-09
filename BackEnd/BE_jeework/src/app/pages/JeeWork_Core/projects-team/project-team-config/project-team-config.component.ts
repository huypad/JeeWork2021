import {ProjectsTeamService} from './../Services/department-and-project.service';
import {WeWorkService} from './../../services/wework.services';
import {TokenStorage} from 'src/app/_metronic/jeework_old/core/auth/_services';
import {
    Component,
    OnInit,
    ChangeDetectionStrategy,
    ChangeDetectorRef,
    ElementRef,
    Input,
    HostListener,
} from '@angular/core';
import {Router, ActivatedRoute} from '@angular/router';
import {TranslateService} from '@ngx-translate/core';
import {FormGroup, FormBuilder} from '@angular/forms';
import {
    LayoutUtilsService,
    MessageType,
} from './../../../../_metronic/jeework_old/core/utils/layout-utils.service';
import {WorkService} from '../../work/work.service';
import {MatDialog} from '@angular/material/dialog';
import {ExportDialogComponent} from './export_dialog/export_dialog.component';
import {MenuPhanQuyenServices} from 'src/app/_metronic/jeework_old/core/_base/layout';

@Component({
    selector: 'm-project-team-config',
    templateUrl: './project-team-config.component.html',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ProjectTeamConfigComponent implements OnInit {
    id_project_team: number;

    constructor(
        public dialog: MatDialog,
        private changeDetect: ChangeDetectorRef,
        private tokenStorage: TokenStorage,
        private itemFB: FormBuilder,
        private layoutUtilsService: LayoutUtilsService,
        private changeDetectorRefs: ChangeDetectorRef,
        private el: ElementRef,
        private activatedRoute: ActivatedRoute,
        private router: Router,
        private _service: WorkService,
        private menuServices: MenuPhanQuyenServices
    ) {
    }

    UserID: any = localStorage.getItem('idUser');

    ngOnInit() {
        var arr = this.router.url.split('/');
        this.id_project_team = +arr[2];

        // kiểm tra quyền của tài khoản
        this.menuServices.GetRoleWeWork(this.UserID).subscribe((res) => {
            if (res && res.status === 1) {
                if (!this.IsAdmin(res.data)) {
                    this.router.navigate(['project', this.id_project_team]);
                }
            } else {
                this.router.navigate(['project', this.id_project_team]);
            }
        });
    }

    IsAdmin(data) {
        if (data.IsAdminGroup) {
            return true;
        }
        const list_role = data.dataRole;
        if (list_role) {
            const x = list_role.find((x) => x.id_row === this.id_project_team);
            if (x) {
                if (x.admin === true || x.admin === 1 || +x.owner === 1 || +x.parentowner === 1) {
                    return true;
                }
            }
        }
        return false;
    }

    export() {
        this.dialog
            .open(ExportDialogComponent, {data: {}, width: '500px'})
            .afterClosed()
            .subscribe((res) => {
                if (res) {
                    res.filter.id_project_team = this.id_project_team;
                    this._service.ExportExcel(res).subscribe((response) => {
                        var headers = response.headers;
                        let filename = headers.get('x-filename');
                        let type = headers.get('content-type');
                        let blob = new Blob([response.body], {type: type});
                        const fileURL = URL.createObjectURL(blob);
                        var link = document.createElement('a');
                        link.href = fileURL;
                        link.download = filename;
                        link.click();
                        //window.open(fileURL, '_blank');
                    });
                }
            });
    }

    getHeight() {
        return window.innerHeight - 60 - this.tokenStorage.getHeightHeader();
    }
}
