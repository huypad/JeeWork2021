import { ChangeDetectorRef, Component, ElementRef, OnInit, ViewChild } from '@angular/core';
import { FormControl } from '@angular/forms';
import { Observable } from 'rxjs';
import { MatChipInputEvent } from '@angular/material/chips';
import { map, startWith } from 'rxjs/operators';
import { COMMA, ENTER } from '@angular/cdk/keycodes';
import { MatAutocomplete, MatAutocompleteSelectedEvent } from '@angular/material/autocomplete';
import { Router } from '@angular/router';
import { MatDialogRef } from '@angular/material/dialog';
import { environment } from 'src/environments/environment';
import { AuthService } from 'src/app/modules/auth/_services/auth.service';
import { ConversationService } from '../my-chat/services/conversation.service';
import { ConversationModel } from '../my-chat/models/conversation';
@Component({
  selector: 'app-create-convesation-group',
  templateUrl: './create-convesation-group.component.html',
  styleUrls: ['./create-convesation-group.component.scss']
})
export class CreateConvesationGroupComponent implements OnInit {
  itemuser: any[] = [];
  list_Tag_edit: any = {}
  user_tam: any[] = []
  list_remove_tam: any[] = [];
  listUser: Observable<any[]>;
  userCtrl = new FormControl();
  userControl = new FormControl();
  separatorKeysCodes: number[] = [ENTER, COMMA];
  visible = true;
  selectable = true;
  removable = true;
  @ViewChild('userInput', { static: false }) userInput: ElementRef<HTMLInputElement>;
  @ViewChild('auto', { static: false }) matAutocomplete: MatAutocomplete;
  user$: Observable<any>;
  ten_group: string='';
  listTT_user: any = {};
  fullname: string;
  authLocalStorageToken = `${environment.appVersion}-${environment.USERDATA_KEY}`;
  constructor(
    private router: Router,
    private auth: AuthService,
    private changeDetectorRefs: ChangeDetectorRef,
    private conversation_sevices: ConversationService,
    private dialogRef: MatDialogRef<CreateConvesationGroupComponent>,) {
      const user = this.auth.getAuthFromLocalStorage()['user'];
    this.user$ = this.auth.getAuthFromLocalStorage();
    this.fullname = user['customData']['personalInfo']['Fullname'];
  }

  CloseDia(data = undefined) {
    this.dialogRef.close(data);
  }

  goBack() {
    this.dialogRef.close();
  }

  ItemConversation(): ConversationModel {
    const item = new ConversationModel();
    item.GroupName = this.ten_group;
    item.IsGroup = true;
    if (this.user_tam.length > 0) {
      item.ListMember = this.user_tam.slice();
    }
    return item
  }


  CreateConverSation() {
    let data = this.ItemConversation();
    this.conversation_sevices.CreateConversation(data).subscribe(res => {
      if (res && res.status === 1) {
        this.CloseDia(res.data);
      }
    })
  }

  submit() {
    this.CreateConverSation();
  }

  addTagName(item: any) {
    let vitri;
    var tam = Object.assign({}, item);
    this.user_tam.push(tam);
    for (let i = 0; i < this.user_tam.length; i++) {
      let index = this.itemuser.findIndex(x => x.Username == this.user_tam[i].Username)
      vitri = index;
    }
    this.itemuser.splice(vitri, 1);
    this.listUser = this.userControl.valueChanges
      .pipe(
        startWith(''),
        map(state => state ? this._filterStates(state) : this.itemuser.slice())

      );
    setTimeout(() => {
      this.cellClick(this.cellindex);
    }, 100);
    this.userControl.setValue("");
    document.getElementById('search').focus();
    document.getElementById("row0").scrollIntoView({ behavior: "smooth" });
  }

  remove(user: string): void {
    const index = this.user_tam.indexOf(user);
    if (index >= 0) {
      this.list_remove_tam.push(this.user_tam[index]);
      this.user_tam.splice(index, 1);
      for (let i = 0; i < this.list_remove_tam.length; i++) {
        this.itemuser.unshift(this.list_remove_tam[i])
        this.list_remove_tam.splice(i, 1);
      }
      this.listUser = this.userControl.valueChanges
        .pipe(
          startWith(''),
          map(state => state ? this._filterStates(state) : this.itemuser.slice())
        );
        setTimeout(() => {
          this.cellClick(this.cellindex);
          document.getElementById('search').focus();
          document.getElementById("row0").scrollIntoView({ behavior: "smooth" });
        }, 100);
    }
  }

  add(event: MatChipInputEvent): void {
    // Add fruit only when MatAutocomplete is not open
    // To make sure this does not conflict with OptionSelected Event
    if (!this.matAutocomplete.isOpen) {
      const input = event.input;
      const value = event.value;

      // Add our fruit
      if ((value || '').trim()) {
        this.user_tam.push(value.trim());
      }
      // Reset the input value
      if (input) {
        input.value = '';
      }
      this.userCtrl.setValue(null);
    }
  }

  selected(event: MatAutocompleteSelectedEvent): void {

    let obj = this.user_tam.find(x => x.Username == event.option.viewValue);
    if (obj) {
      alert('Vui lòng chọn thành viên khác')
    }
    else {
      this.user_tam.push(
        {
          Username: event.option.viewValue,
        })
      this.remove(event.option.value);
      this.userInput.nativeElement.value = '';
      this.userCtrl.setValue(null);
    }
  }

  private _normalizeValue(value: string): string {
    return value.toLowerCase().replace(/\s/g, '');
  }
  private _filterStates(value: string): any[] {
    const valie_vie = this.convertVietnamese(value);
    const filterValue = this._normalizeValue(valie_vie);
    setTimeout(() => {
      this.cellClick(this.cellindex);
    }, 100);
    return this.itemuser.filter(state => this._normalizeValue(this.convertVietnamese(state.Fullname)).includes(filterValue));
  }

  loadTT() {
    this.conversation_sevices.getAllUsers().subscribe(res => {
      this.itemuser = res.data;
      for (let i = 0; i < this.user_tam.length; i++) {
        let vitri = this.itemuser.findIndex(x => x.UserId == this.user_tam[i].UserId)
        this.itemuser.splice(vitri, 1)
      }
      this.listUser = this.userControl.valueChanges
        .pipe(
          startWith(''),
          map(state => state ? this._filterStates(state) : this.itemuser.slice())
        );
        setTimeout(() => {
          this.cellClick(this.cellindex);
        }, 100);
      this.changeDetectorRefs.detectChanges();
    })
  }

  loadTTuser() {

    // const authData = JSON.parse(localStorage.getItem(this.authLocalStorageToken));
   const authData = this.auth.getAuthFromLocalStorage()
    debugger
    this.listTT_user = authData.user.customData.personalInfo;
  }
  ngOnInit(): void {
    this.loadTT();
    this.loadTTuser();
  }

  getNameUser(item: any) {
    let name = item.Fullname.split(" ")[item.Fullname.split(" ").length - 1];
    return name.substring(0, 1).toUpperCase();
  }

  getColorNameUser(item: any) {
    let value = item.Fullname.split(" ")[item.Fullname.split(" ").length - 1];
    let result = '';
    switch (value) {
      case 'A':
        return (result = 'rgb(51 152 219)');
      case 'Ă':
        return (result = 'rgb(241, 196, 15)');
      case 'Â':
        return (result = 'rgb(142, 68, 173)');
      case 'B':
        return (result = '#0cb929');
      case 'C':
        return (result = 'rgb(91, 101, 243)');
      case 'D':
        return (result = 'rgb(44, 62, 80)');
      case 'Đ':
        return (result = 'rgb(127, 140, 141)');
      case 'E':
        return (result = 'rgb(26, 188, 156)');
      case 'Ê':
        return (result = 'rgb(51 152 219)');
      case 'G':
        return (result = 'rgb(241, 196, 15)');
      case 'H':
        return (result = 'rgb(248, 48, 109)');
      case 'I':
        return (result = 'rgb(142, 68, 173)');
      case 'K':
        return (result = '#2209b7');
      case 'L':
        return (result = 'rgb(44, 62, 80)');
      case 'M':
        return (result = 'rgb(127, 140, 141)');
      case 'N':
        return (result = 'rgb(197, 90, 240)');
      case 'O':
        return (result = 'rgb(51 152 219)');
      case 'Ô':
        return (result = 'rgb(241, 196, 15)');
      case 'Ơ':
        return (result = 'rgb(142, 68, 173)');
      case 'P':
        return (result = '#02c7ad');
      case 'Q':
        return (result = 'rgb(211, 84, 0)');
      case 'R':
        return (result = 'rgb(44, 62, 80)');
      case 'S':
        return (result = 'rgb(127, 140, 141)');
      case 'T':
        return (result = '#bd3d0a');
      case 'U':
        return (result = 'rgb(51 152 219)');
      case 'Ư':
        return (result = 'rgb(241, 196, 15)');
      case 'V':
        return (result = '#759e13');
      case 'X':
        return (result = 'rgb(142, 68, 173)');
      case 'W':
        return (result = 'rgb(211, 84, 0)');
    }
    return result;
  }
  
  //=====================Bổ sung tìm kiếm không dấu và di chuyển mũi tên lên xuống 11/10/2021============================
  convertVietnamese(str: any) {
    str = str.toLowerCase();
    str = str.replace(/à|á|ạ|ả|ã|â|ầ|ấ|ậ|ẩ|ẫ|ă|ằ|ắ|ặ|ẳ|ẵ/g, "a");
    str = str.replace(/è|é|ẹ|ẻ|ẽ|ê|ề|ế|ệ|ể|ễ/g, "e");
    str = str.replace(/ì|í|ị|ỉ|ĩ/g, "i");
    str = str.replace(/ò|ó|ọ|ỏ|õ|ô|ồ|ố|ộ|ổ|ỗ|ơ|ờ|ớ|ợ|ở|ỡ/g, "o");
    str = str.replace(/ù|ú|ụ|ủ|ũ|ư|ừ|ứ|ự|ử|ữ/g, "u");
    str = str.replace(/ỳ|ý|ỵ|ỷ|ỹ/g, "y");
    str = str.replace(/đ/g, "d");
    str = str.replace(/^\-+|\-+$/g, "");
    str = str.toLowerCase().replace(/\b[a-z](?=[a-z]{2})/g, (letter) => { return letter.toUpperCase(); });
    return str;
  }

  cellselected: any;
  cellSelect: any = {
    rowInd: 0,
    colInd: 0,
    colName: "",
    _editing: false,
  };
  displayedColumns = [];
  public cellindex: number = 0;

  cellClick(rInd: number, colName: string = 'name') {
    this.cellSelect = {
      rowInd: rInd,
      colName: colName,
    };
    let t = document.querySelector(".li.focus");
    let q = document.getElementById("row" + rInd);
    if (t) t.classList.remove("focus");
    if (q) {
      q.classList.add("focus");
      q.focus();
    }
    this.changeDetectorRefs.detectChanges();
  }

  keyArrowInput(e: any, rind: number) {
    switch (e.keyCode) {
      case 8:
        setTimeout(() => {
          this.cellClick(this.cellindex);
          document.getElementById('search').focus();
        }, 500);
        break;
      case 13:
        this.selectedEdit(this.cellSelect.rowInd);
        break;
      case 38: // this is the ascii of arrow up
        e.preventDefault();
        break;
      case 40: // this is the ascii of arrow down
        e.preventDefault();
        break;
    }

    rind = this.cellSelect.rowInd;
    // left: 37, up: 38, right: 39, down: 40
    let _move = false;
    // colInd--;
    switch (e.keyCode) {
      case 38: //up, giữ cột, move row
        _move = true;
        if (rind > 0) {
          rind--;
        }
        break;
      case 40://down, giữ cột, giảm row
        _move = true;
        let listdata = this.GetListData();
        if (rind < listdata.length - 1) {
          rind++;
        }
        break;
    }
    if (_move) {
      this.cellClick(rind);
      document.getElementById("row" + rind).scrollIntoView({ behavior: "smooth" });
    }

    // this.myScrollContainer.nativeElement.focus();
  }

  selectedEdit(vi: any) {
    if (this.userControl.value == null) {
      this.userControl.setValue("");
    }
    const valie_vie = this.convertVietnamese(this.userControl.value);
    const filterValue = this._normalizeValue(valie_vie);
    let data = this.itemuser.filter(state => this._normalizeValue(this.convertVietnamese(state.Fullname)).includes(filterValue));
    this.addTagName(data[vi]);
  }

  GetListData(): any[] {
    if (this.userControl.value == null) {
      this.userControl.setValue("");
    }
    const valie_vie = this.convertVietnamese(this.userControl.value);
    const filterValue = this._normalizeValue(valie_vie);
    return this.itemuser.filter(state => this._normalizeValue(this.convertVietnamese(state.Fullname)).includes(filterValue));
  }
}
