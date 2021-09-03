import { ChangeDetectorRef, Component, Inject, OnInit } from '@angular/core';
import { MatDialogRef, MAT_DIALOG_DATA } from '@angular/material/dialog';
import { ChatService } from '../my-chat/services/chat.service';
import { ConversationService } from '../my-chat/services/conversation.service';

@Component({
  selector: 'app-edit-group-name',
  templateUrl: './edit-group-name.component.html',
  styleUrls: ['./edit-group-name.component.scss']
})
export class EditGroupNameComponent implements OnInit {

  GroupName:string;
  listInfor:any[]=[]
  constructor(
    private changeDetectorRefs: ChangeDetectorRef,
    private chatService:ChatService,
    private conversation_sevices:ConversationService,
    @Inject(MAT_DIALOG_DATA) public data: any,
    private dialogRef:MatDialogRef<EditGroupNameComponent>,) {

   }
 
   CloseDia(data = undefined)
   {
       this.dialogRef.close(data);
   }
   goBack() {
   
      this.dialogRef.close();
    
    }


   
    
    
    EditGroupName()
    {
  
      this.conversation_sevices.EditNameGroup(this.data,this.GroupName).subscribe(res=>
        {
          if (res && res.status === 1) {
            this.CloseDia(res);
          }
        })
      
    }
    
submit()
{
  this.EditGroupName();
}
   
GetInforUserChatwith(IdGroup:number)
{
this.chatService.GetInforUserChatWith(IdGroup).subscribe(res=>{
      this.listInfor=res.data;
      this.GroupName  =this.listInfor[0].GroupName;
      this.changeDetectorRefs.detectChanges();
  })
}
 
  ngOnInit(): void {
   
    this.GetInforUserChatwith(this.data)


  }

}
