import { ConversationModel } from './../../../../../modules/my-chat/models/conversation';
import { ConversationService } from './../../../../../modules/my-chat/services/conversation.service';
import { Component, OnInit, ChangeDetectorRef } from '@angular/core';
import { FormControl } from '@angular/forms';
import { MatDialogRef } from '@angular/material/dialog';
import { ReplaySubject } from 'rxjs';
import { CreateConvesationGroupComponent } from '../create-convesation-group/create-convesation-group.component';

@Component({
  selector: 'app-create-conversation-user',
  templateUrl: './create-conversation-user.component.html',
  styleUrls: ['./create-conversation-user.component.scss']
})
export class CreateConversationUserComponent implements OnInit {
	public searchControl: FormControl = new FormControl();

  public filteredGroups: ReplaySubject<any[]> = new ReplaySubject<any[]>(1);
  constructor(private conversation_sevices:ConversationService,
    private dialogRef:MatDialogRef<CreateConvesationGroupComponent>,
    private changeDetectorRefs:ChangeDetectorRef
    
    ) { }

  listDanhBa:any[]=[];
  listUser:any[]=[]

  ItemConversation(ten_group:string,data:any): ConversationModel
  {
    
    this.listUser.push(data);
    const item = new ConversationModel();
    item.GroupName=ten_group;
    item.IsGroup=false;
  
      item.ListMember=this.listUser.slice();
    
     
    return item
  }
  
  CreateConverSation(item)
  {

   let it=  this.ItemConversation(item.FullName,item);
    this.conversation_sevices.CreateConversation(it).subscribe(res=>
      {
        if (res && res.status === 1) {
          this.listUser=[]
          this.CloseDia(res.data);
        }
      })
    
  }
  CloseDia(data = undefined)
  {
      this.dialogRef.close(data);
  }
  goBack() {
  
     this.dialogRef.close();
   
   }
  
  protected filterBankGroups() {
    if (!this.listDanhBa) {
      return;
    }
    // get the search keyword
    let search = this.searchControl.value;
    // const bankGroupsCopy = this.copyGroups(this.list_group);
    if (!search) {
      this.filteredGroups.next(this.listDanhBa.slice());
     
    } else {
      search = search.toLowerCase();
    }
    
      this.filteredGroups.next(
      this.listDanhBa.filter(bank => (bank.FullName.toLowerCase().indexOf(search) > -1)
      )
      );
    }
   
  GetDanhBa()
  
  {
	this.conversation_sevices.GetDanhBaNotConversation().subscribe(res=>{
	  this.listDanhBa= res.data;
	 this.filteredGroups.next(this.listDanhBa.slice());
	  this.changeDetectorRefs.detectChanges();
	})
   
  }
  ngOnInit(): void {
    this.GetDanhBa();
    this.searchControl.valueChanges
    .pipe()
    .subscribe(() => {
      this.filterBankGroups();
    });
  }

}
