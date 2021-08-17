import { MessengerComponent } from './dropdown-inner/messenger/messenger.component';
import { ChatBoxComponent } from './dropdown-inner/chat-box/chat-box.component';
import { CreateConvesationGroupComponent } from './create-convesation-group/create-convesation-group.component';
import { JeeWork_CoreModule } from "./../../../../pages/JeeWork_Core/JeeWork_Core.module";
import { NgModule } from "@angular/core";
import { CommonModule } from "@angular/common";
import { RouterModule } from "@angular/router";
import { InlineSVGModule } from "ng-inline-svg";
import { PerfectScrollbarModule } from "ngx-perfect-scrollbar";
import { PERFECT_SCROLLBAR_CONFIG } from "ngx-perfect-scrollbar";
import { PerfectScrollbarConfigInterface } from "ngx-perfect-scrollbar";
import { SearchDropdownInnerComponent } from "./dropdown-inner/search-dropdown-inner/search-dropdown-inner.component";
import { NotificationsDropdownInnerComponent } from "./dropdown-inner/notifications-dropdown-inner/notifications-dropdown-inner.component";
import { QuickActionsDropdownInnerComponent } from "./dropdown-inner/quick-actions-dropdown-inner/quick-actions-dropdown-inner.component";
import { CartDropdownInnerComponent } from "./dropdown-inner/cart-dropdown-inner/cart-dropdown-inner.component";
import { UserDropdownInnerComponent } from "./dropdown-inner/user-dropdown-inner/user-dropdown-inner.component";
import { SearchOffcanvasComponent } from "./offcanvas/search-offcanvas/search-offcanvas.component";
import { SearchResultComponent } from "./dropdown-inner/search-dropdown-inner/search-result/search-result.component";
import { NotificationsOffcanvasComponent } from "./offcanvas/notifications-offcanvas/notifications-offcanvas.component";
import { QuickActionsOffcanvasComponent } from "./offcanvas/quick-actions-offcanvas/quick-actions-offcanvas.component";
import { CartOffcanvasComponent } from "./offcanvas/cart-offcanvas/cart-offcanvas.component";
import { QuickPanelOffcanvasComponent } from "./offcanvas/quick-panel-offcanvas/quick-panel-offcanvas.component";
import { UserOffcanvasComponent } from "./offcanvas/user-offcanvas/user-offcanvas.component";
import { CoreModule } from "../../../core";
import { ScrollTopComponent } from "./scroll-top/scroll-top.component";
import { ToolbarComponent } from "./toolbar/toolbar.component";
import { AvatarModule } from "ngx-avatar";
import { SocketioService } from "src/app/modules/auth/_services/socketio.service";
import { TranslationModule } from "src/app/modules/i18n/translation.module";
import { MatTooltipModule } from "@angular/material/tooltip";
import { CollapseModule } from 'ngx-bootstrap/collapse';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatChipsModule } from '@angular/material/chips';
import { MatAutocompleteModule } from '@angular/material/autocomplete';
import { MatInputModule } from '@angular/material/input';
import { FormsModule, ReactiveFormsModule } from '@angular/forms';
import { FilterPipe } from './filter,pipe';
import { CreateConversationUserComponent } from './create-conversation-user/create-conversation-user.component';
import { ScrollingModule } from '@angular/cdk/scrolling';
import { MatSnackBarModule } from '@angular/material/snack-bar';
import { MatDialogModule } from '@angular/material/dialog';
import { NgxAutoScrollModule } from 'ngx-auto-scroll';
const DEFAULT_PERFECT_SCROLLBAR_CONFIG: PerfectScrollbarConfigInterface = {
  suppressScrollX: true,
};

@NgModule({
  declarations: [
    CreateConversationUserComponent,
    FilterPipe,
    SearchDropdownInnerComponent,
    NotificationsDropdownInnerComponent,
    QuickActionsDropdownInnerComponent,
    CartDropdownInnerComponent,
    UserDropdownInnerComponent,
    SearchOffcanvasComponent,
    SearchResultComponent,
    NotificationsOffcanvasComponent,
    QuickActionsOffcanvasComponent,
    CartOffcanvasComponent,
    QuickPanelOffcanvasComponent,
    UserOffcanvasComponent,
    ScrollTopComponent,
    ToolbarComponent,
    CreateConvesationGroupComponent,
    ChatBoxComponent,
    MessengerComponent
  ],
  imports: [
    // npm lại nó mới ăn cái thư viện ở module ko pk sao luôn
    NgxAutoScrollModule,ScrollingModule,CommonModule,MatSnackBarModule,MatDialogModule,
    CommonModule,
    InlineSVGModule,
    PerfectScrollbarModule,
    CoreModule,
    FormsModule,
    ReactiveFormsModule,
    RouterModule,
    JeeWork_CoreModule,
    AvatarModule,
    TranslationModule, 
    MatTooltipModule,
    CollapseModule,
    MatFormFieldModule,
    MatChipsModule,
    MatAutocompleteModule,
    MatInputModule ,
  ],
  providers: [
    {
      provide: PERFECT_SCROLLBAR_CONFIG,
      useValue: DEFAULT_PERFECT_SCROLLBAR_CONFIG,
    },
    SocketioService,
  ],
  entryComponents: [CreateConvesationGroupComponent,CreateConversationUserComponent],
  exports: [
    SearchDropdownInnerComponent,
    NotificationsDropdownInnerComponent,
    QuickActionsDropdownInnerComponent,
    CartDropdownInnerComponent,
    UserDropdownInnerComponent,
    SearchOffcanvasComponent,
    NotificationsOffcanvasComponent,
    QuickActionsOffcanvasComponent,
    CartOffcanvasComponent,
    QuickPanelOffcanvasComponent,
    UserOffcanvasComponent,
    ToolbarComponent,
    ScrollTopComponent,
    CreateConvesationGroupComponent,
    ChatBoxComponent,
    MessengerComponent
  ],
})
export class ExtrasModule {}
