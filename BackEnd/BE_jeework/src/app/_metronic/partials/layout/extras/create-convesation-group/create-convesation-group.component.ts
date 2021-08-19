import { ConversationService } from './../../../../../modules/my-chat/services/conversation.service';
import { ConversationModel } from './../../../../../modules/my-chat/models/conversation';
import { AuthService } from './../../../../../modules/auth/_services/auth.service';
import { environment } from './../../../../../../environments/environment';
import { ChangeDetectorRef, Component, ElementRef, OnInit, ViewChild } from '@angular/core';
import { FormControl } from '@angular/forms';
import { Observable } from 'rxjs';
import { MatChipInputEvent } from '@angular/material/chips';
import { map, startWith } from 'rxjs/operators';
import { COMMA, ENTER } from '@angular/cdk/keycodes';
import { MatAutocomplete, MatAutocompleteSelectedEvent } from '@angular/material/autocomplete';
import { Router } from '@angular/router';
import { MatDialogRef } from '@angular/material/dialog';
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
  ten_group:string;
  listTT_user:any={};
  authLocalStorageToken = `${environment.appVersion}-${environment.USERDATA_KEY}`;
  constructor(
    private router: Router,
    private auth:AuthService,
    private changeDetectorRefs: ChangeDetectorRef,
    private conversation_sevices:ConversationService,
    private dialogRef:MatDialogRef<CreateConvesationGroupComponent>,) {

    this.user$ = this.auth.currentUserSubject.asObservable();
   }
 
   CloseDia(data = undefined)
   {
       this.dialogRef.close(data);
   }
   goBack() {
   
      this.dialogRef.close();
    
    }


    ItemConversation(): ConversationModel
    {
      const item = new ConversationModel();
      item.GroupName=this.ten_group;
      item.IsGroup=true;
      if(this.user_tam.length>0)
      {
        item.ListMember=this.user_tam.slice();
      }
       
      return item
    }
    
    
    CreateConverSation()
    {
  
      let  data=this.ItemConversation();
      this.conversation_sevices.CreateConversation(data).subscribe(res=>
        {
          if (res && res.status === 1) {
            this.CloseDia(res.data);
          }
        })
      
    }
    
submit()
{
 this.CreateConverSation();
}


addTagName(item: any) {
  let vitri;
      var tam = Object.assign({}, item);
      this.user_tam.push(tam);
       for(let i=0;i< this.user_tam.length;i++)
       {
        let index=this.itemuser.findIndex(x=>x.Username== this.user_tam[i].Username)
        vitri=index;
       }  
       this.itemuser.splice(vitri, 1);
     
      this.listUser = this.userControl.valueChanges
        .pipe(
          startWith(''),
          map(state => state ? this._filterStates(state) : this.itemuser.slice())
  
        );
  
  
  
  
  
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
        alert('Vui lòng chọn nhân viên khác !')
      }
      else {
  
        this.user_tam.push(
          {
            //  ID_NV:this.id_nv.value,
            Username: event.option.viewValue,
          })
  
  
        this.remove(event.option.value);
        // this.userInput.nativeElement.value = '';
        // this.userCtrl.setValue(null);
  
        // let obj = this.user_tam.find(x => x.ID_type == event.option.value);
        //this.deleteHT1(obj);
        this.userInput.nativeElement.value = '';
        this.userCtrl.setValue(null);
  
  
  
      }
    }
  
    private _normalizeValue(value: string): string {
      return value.toLowerCase().replace(/\s/g, '');
    }
    private _filterStates(value: string): any[] {
      //	const filterValue = value.toLowerCase();
      const filterValue = this._normalizeValue(value);
      return this.itemuser.filter(state => this._normalizeValue(state.Fullname).includes(filterValue));
    }
    
    loadTT() {
      this.conversation_sevices.getAllUsers().subscribe(res => {
        //  console.log(res);
        this.itemuser = res.data;
       for(let i=0;i<this.user_tam.length;i++)
       {
          let vitri=this.itemuser.findIndex(x=>x.UserId==this.user_tam[i].UserId)
          this.itemuser.splice(vitri,1)
       }
  
  
  
        this.listUser = this.userControl.valueChanges
          .pipe(
            startWith(''),
            map(state => state ? this._filterStates(state) : this.itemuser.slice())
  
          );
          this.changeDetectorRefs.detectChanges();
  
  
      })
    
    }
  
    loadTTuser()
    {
      
      const authData = JSON.parse(localStorage.getItem(this.authLocalStorageToken));
      this.listTT_user=authData.user.customData.personalInfo;
    }
    
  ngOnInit(): void {
   this.loadTT();
    this.loadTTuser();
  }

}
