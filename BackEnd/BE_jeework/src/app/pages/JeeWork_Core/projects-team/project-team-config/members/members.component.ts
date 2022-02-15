import { LayoutUtilsService, MessageType } from './../../../../../_metronic/jeework_old/core/utils/layout-utils.service';
import { TokenStorage } from './../../../../../_metronic/jeework_old/core/auth/_services/token-storage.service';
import { TranslateService } from '@ngx-translate/core';
import {
    Component,
    OnInit,
    ElementRef,
    ViewChild,
    ChangeDetectionStrategy,
    ChangeDetectorRef,
    Inject,
    HostListener,
    Input,
    SimpleChange
} from '@angular/core';
import { ActivatedRoute, Router, Route } from '@angular/router';
// Material
import { MatDialog } from '@angular/material/dialog';
// Models

import { PopoverContentComponent } from 'ngx-smart-popover';
import { AddUsersDialogComponent } from '../add-users-dialog/add-users-dialog.component';
import { ProjectsTeamService } from '../../Services/department-and-project.service';
import { JeeWorkLiteService } from '../../../services/wework.services';
import { MenuAsideService, MenuPhanQuyenServices } from '../../../../../_metronic/jeework_old/core/_base/layout';

@Component({
    selector: 'kt-members',
    templateUrl: './members.component.html',
    changeDetection: ChangeDetectionStrategy.OnPush
})
export class MembersComponent {
    constructor(public _services: ProjectsTeamService,
        public dialog: MatDialog,
        private layoutUtilsService: LayoutUtilsService,
        private activatedRoute: ActivatedRoute,
        private menuServices: MenuPhanQuyenServices,
        private menuAsideService: MenuAsideService,
        private translate: TranslateService,
        private changeDetectorRefs: ChangeDetectorRef,
        private router: Router, 
        private _service: ProjectsTeamService,
        public WeWorkService: JeeWorkLiteService,
        private tokenStorage: TokenStorage) {
    }

    id_project_team: number;
    admins: any[] = [];
    members: any[] = [];
    options: any = {};
    IsAdmin = false;
    customStyle: any = {};
    UserID: any = localStorage.getItem('idUser');
    @ViewChild('myPopoverA', { static: true }) myPopoverA: PopoverContentComponent;


    // chat nhanh
    chatBoxUsers = [];

    ngOnInit() {
        const arr = this.router.url.split('/');
        this.id_project_team = +arr[2];
        this.options = this.getOptions();
        this.layoutUtilsService.showWaitingDiv();
        this._services.List_user(this.id_project_team).subscribe(res => {
            this.layoutUtilsService.OffWaitingDiv();
            if (res && res.status == 1) {
                this.admins = res.data.filter(x => x.admin);
                this.members = res.data.filter(x => !x.admin);
                this.changeDetectorRefs.detectChanges();
            }
            this.changeDetectorRefs.detectChanges();
        });
        // kiểm tra quyền của tài khoản
        this.menuServices.GetRoleWeWork(this.UserID).subscribe((res) => {
            if (res && res.status === 1) {
                this.CheckAdmin(res.data);
            } else {
                this.router.navigate(['']);
                this.menuAsideService.loadMenu();
            }
        });
        // this.GetDanhBa();
    }

    CheckAdmin(data) {
        if (data.IsAdminGroup) {
            return true;
        }
        const list_role = data.dataRole;
        if (list_role) {
            const x = list_role.find((x) => x.id_row === this.id_project_team);
            if (x) {
                if (x.admin === true || +x.admin === 1 || +x.owner === 1 || +x.parentowner === 1) {
                    return true;
                } else {
                    ;
                    this.router.navigate(['project', this.id_project_team]);
                    return;
                }
            } else {
                ;
                this.router.navigate(['']);
                return;
                this.menuAsideService.loadMenu();
            }
        }
        return false;
    }


    getOptions() {
        const options: any = {};
        const filter: any = {};
        filter.key = 'id_project_team';
        filter.value = this.id_project_team;
        options.filter = filter;
        if (this.IsAdmin) {
            options.excludes = this.admins.map(x => x.id_nv);
        } else {
            options.excludes = this.members.map(x => x.id_nv);
        }
        return options;
    }

    initAddUser($event, admin = false) {
        this.options = this.getOptions();
        this.IsAdmin = admin;
        const el = $event.currentTarget.offsetParent;
        this.myPopoverA.show();
        this.myPopoverA.top = el.offsetTop + 50;
        this.myPopoverA.left = el.offsetLeft;
        this.changeDetectorRefs.detectChanges();
    }

    ItemSelected(user) {
        this.myPopoverA.hide();
        this.addMember(user.id_nv);
        // this.layoutUtilsService.showInfo("ItemSelected " + user.id_nv);
    }

    initAddMembers(admin = false) {
        const title = admin ? this.translate.instant('GeneralKey.themnhieuquanlyduan') : this.translate.instant('GeneralKey.themnhieuthanhvien');
        const dialogRef = this.dialog.open(AddUsersDialogComponent, { data: { title, filter: {}, excludes: [] }, width: '500px' });
        dialogRef.afterClosed().subscribe(res => {
            if (!res) {
                return;
            } else {
                this.addMembers(res, admin);
            }
        });
    }

    addMembers(users, admin) {
        this.layoutUtilsService.showWaitingDiv();
        const data = {
            id_row: this.id_project_team,
            Users: users.map(x => {
                return {
                    id_user: x,
                    admin
                };
            })
        };
        this._services.Add_user(data).subscribe(res => {
            this.layoutUtilsService.OffWaitingDiv();
            if (res && res.status == 1) {
                this.LoadParent(true);
                this.ngOnInit();
            } else {
                this.layoutUtilsService.showActionNotification(res.error.message, MessageType.Update, 9999999999, true, false, 3000, 'top', 0);
            }
        });
        // this.layoutUtilsService.showInfo("add " + (admin ? "admins" : "members"));
    }

    addMember(id_nv) {
        this.layoutUtilsService.showWaitingDiv();
        const data = {
            id_row: this.id_project_team,
            Users: [{
                id_user: id_nv,
                admin: this.IsAdmin
            }]
        };
        this._services.Add_user(data).subscribe(res => {
            this.layoutUtilsService.OffWaitingDiv();
            if (res && res.status == 1) {
                this.ngOnInit();
                this.LoadParent(true);
            } else {
                this.layoutUtilsService.showActionNotification(res.error.message, MessageType.Update, 9999999999, true, false, 3000, 'top', 0);
            }
        });
        // this.layoutUtilsService.showInfo("add " + (admin ? "admin" : "member"));
    }

    chatWith(user) {
        // this.layoutUtilsService.showInfo('chatWith ' + user.username);
        // this.chatService.GetChatWithFriend(user.username).subscribe(res => {
        //     if (res && res.status === 1) {
        //         if (res.data?.length > 0) {
        //             this.selectUser(res.data[0]);
        //         } else {
        //             this.CreateConverSation(user);
        //         }
        //     }
        // })
    }
    // listDanhBa = [];
    // GetDanhBa() {
    //     this.conversation_sevices.GetDanhBaNotConversation().subscribe(res => {
    //         this.listDanhBa = res.data;
    //         this.changeDetectorRefs.detectChanges();
    //     })
    // }
    // ItemConversation(ten_group: string, data: any): ConversationModel {
    //     var listUser = [];
    //     listUser.push(data);
    //     const item = new ConversationModel();
    //     item.GroupName = ten_group;
    //     item.IsGroup = false;

    //     item.ListMember = listUser.slice();


    //     return item
    // }

    // CreateConverSation(item) {
    //     if (this.listDanhBa) {
    //         var user = this.listDanhBa.find(x => x.UserID = item.id_nv);
    //         if (user) {
    //             let it = this.ItemConversation(user.FullName, user);
    //             this.conversation_sevices.CreateConversation(it).subscribe(res => {
    //                 if (res && res.status === 1) {
    //                     if (res.data?.length > 0) {
    //                         this.selectUser(res.data[0]);
    //                     }
    //                     // this.listUser = []
    //                     // this.CloseDia(res.data);
    //                 }
    //             })
    //         }
    //     }
    // }
    viewProfile(id_nv) {
        this.layoutUtilsService.showInfo('viewProfile ' + id_nv);
    }

    delete(id_row) {
        this.layoutUtilsService.showWaitingDiv();
        this._services.Delete_user(id_row).subscribe(res => {
            this.layoutUtilsService.OffWaitingDiv();
            if (res && res.status == 1) {

                this.ngOnInit();
                this.LoadParent(true);
            } else {
                this.layoutUtilsService.showActionNotification(res.error.message, MessageType.Update, 9999999999, true, false, 3000, 'top', 0);
            }
        });
        // this.layoutUtilsService.showInfo("delete " + id_row);
    }

    updateRule(id_row, admin = true) {
        this.layoutUtilsService.showWaitingDiv();
        this._services.update_user(id_row, admin).subscribe(res => {
            this.layoutUtilsService.OffWaitingDiv();
            if (res && res.status == 1) {
                this.ngOnInit();
                this.LoadParent(true);
            } else {
                this.layoutUtilsService.showActionNotification(res.error.message, MessageType.Update, 9999999999, true, false, 3000, 'top', 0);
            }
        });
        // this.layoutUtilsService.showInfo("updateRule " + id_row + ", " + admin);
    }

    // selectUser(user: any) {
    //     const userChatBox: UserChatBox[] = JSON.parse(localStorage.getItem('chatboxusers'));
    //     if (userChatBox) {
    //         this.chatBoxUsers = userChatBox;
    //     } else {
    //         this.chatBoxUsers = [];
    //     }

    //     // if (user.UnreadMess > 0) {
    //     // 	this.UpdateUnreadMess(user.IdGroup, user.UserId, user.UnreadMess);
    //     // }
    //     // this.usernameActived = user.IdGroup;

    //     switch (this.chatBoxUsers.length) {

    //         case 2: {
    //             const u = this.chatBoxUsers.find(x => x.user.IdGroup === user.IdGroup);
    //             if (u != undefined) {
    //                 this.chatBoxUsers = this.chatBoxUsers.filter(x => x.user.IdGroup !== user.IdGroup);
    //                 this.chatBoxUsers.push(u);
    //             } else {
    //                 this.chatBoxUsers.push(new UserChatBox(user, 625 + 325));
    //                 this.chatService.OpenMiniChat$.next(new UserChatBox(user, 625 + 325));
    //             }
    //             localStorage.setItem('chatboxusers', JSON.stringify(this.chatBoxUsers));
    //             break;
    //         }
    //         case 1: {
    //             const u = this.chatBoxUsers.find(x => x.user.IdGroup === user.IdGroup);
    //             if (u != undefined) {
    //                 this.chatBoxUsers = this.chatBoxUsers.filter(x => x.user.IdGroup !== user.IdGroup);
    //                 this.chatBoxUsers.push(u);
    //             } else {
    //                 this.chatBoxUsers.push(new UserChatBox(user, 300 + 325));
    //                 this.chatService.OpenMiniChat$.next(new UserChatBox(user, 300 + 325));
    //             }
    //             localStorage.setItem('chatboxusers', JSON.stringify(this.chatBoxUsers));
    //             break;
    //         }
    //         default: {// 0 nó vào đây trc nếu có 1 hội thoại để xem thử
    //             const u = this.chatBoxUsers.find(x => x.user.IdGroup === user.IdGroup);
    //             if (u != undefined) {
    //                 this.chatBoxUsers = this.chatBoxUsers.filter(x => x.user.IdGroup !== user.IdGroup);
    //                 this.chatBoxUsers.push(u);
    //             } else {
    //                 this.chatBoxUsers.push(new UserChatBox(user, 300));
    //                 const item = new UserChatBox(user, 300);
    //                 // this.chatService.ChangeDatachat(new UserChatBox(user, 625 + 325));
    //                 this.chatService.OpenMiniChat$.next(item);
    //             }
    //             localStorage.setItem('chatboxusers', JSON.stringify(this.chatBoxUsers));
    //             break;
    //         }
    //     }
    //     // this.searchText = "";
    //     this.chatService.search$.next('activechat');
    //     this.changeDetectorRefs.detectChanges();
    // }

    LoadParent(value): void {
        this._service.changeMessage(value);
    }


}
