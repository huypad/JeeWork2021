import { JeeWorkLiteService } from './../../../../../../pages/JeeWork_Core/services/wework.services';
import { Router } from '@angular/router';
import { TokenStorage } from './../../../../../jeework_old/core/auth/_services/token-storage.service';
import { Component, OnInit, ChangeDetectorRef } from '@angular/core';
import { LayoutService } from '../../../../../core';
import { Observable } from 'rxjs';
import { UserModel } from '../../../../../../modules/auth/_models/user.model';
import { AuthService } from '../../../../../../modules/auth/_services/auth.service';
import { RemindService } from '../../../../../../modules/auth/_services/remind.service';

@Component({
    selector: 'app-user-offcanvas',
    templateUrl: './user-offcanvas.component.html',
    styleUrls: ['./user-offcanvas.component.scss'],
})
export class UserOffcanvasComponent implements OnInit {
    extrasUserOffcanvasDirection = 'offcanvas-right';
    user$: Observable<UserModel>;
    _user: any = {};
    listNhacNho: any[] = [];
    AppCode: string = '';

    constructor(
        private layout: LayoutService,
        private tokenStorage: TokenStorage,
        private changeDetectorRefs: ChangeDetectorRef,
        private auth: AuthService,
        private weWorkService: JeeWorkLiteService,
        private router: Router,
        private remindSevices: RemindService,
    ) {
    }

    ngOnInit(): void {
        this.extrasUserOffcanvasDirection = `offcanvas-${this.layout.getProp(
            'extras.user.offcanvas.direction'
        )}`;
        // this.user$ = this.auth.currentUserSubject.asObservable();
        this.LoadData();

        setTimeout(() => {
            this.remindSevices.connectToken();
        }, 500);
        this.LoadDataNhacNho();
        this.EventNhacNho();
    }

    UserData: any = {};

    LoadData() {
        // this.tokenStorage.getUserData().subscribe(res => {
        // 	this.UserData = res;
        // })
        this.user$ = this.auth.getAuthFromLocalStorage();

    }

    public EventNhacNho() {
        this.remindSevices.NewMess$.subscribe(res => {
            if (res) {
                this.LoadDataNhacNho();
            }
        })
    }

    public LoadDataNhacNho() {
        this.weWorkService.Get_DSNhacNho().subscribe(res => {
            if (res && res.status === 1) {
                this.listNhacNho = res.data;
                this.changeDetectorRefs.detectChanges();
            }
        });
    }

    logout() {
        this.remindSevices.disconnectToken();
        this.auth.logoutToSSO().subscribe(
            (res) => {
                this.auth.prepareLogout();
            },
            (err) => {
                this.auth.prepareLogout();
            }
        );
    }

    quanlytaikhoan() {
        window.open(' https://app.jee.vn/ThongTinCaNhan', '_blank');
        // window.open("https://jeeaccount.jee.vn/Management/AccountManagement","_blank")
    }

    ChangeLink(item) {
        if (item.WebAppLink != null && item.WebAppLink != '') {
            if (this.AppCode == item.AppCode) {
                this.router.navigate([item.WebAppLink]);
            }
            else {
                let link = item.Domain + item.WebAppLink;
                window.open(link, '_blank');
            }
        }
    }
}
