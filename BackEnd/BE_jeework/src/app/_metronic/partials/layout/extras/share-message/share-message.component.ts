import { AuthService } from './../../../../../modules/auth/_services/auth.service';

import  ClassicEditor  from '@ckeditor/ckeditor5-build-classic';
import { ChangeDetectorRef, Component, Inject, NgZone, OnInit } from '@angular/core';
import { FormControl } from '@angular/forms';
import { MatDialogRef, MAT_DIALOG_DATA } from '@angular/material/dialog';
import { ReplaySubject } from 'rxjs';
import { ChatService } from '../jee-chat/my-chat/services/chat.service';
import { ConversationService } from '../jee-chat/my-chat/services/conversation.service';
import { MessageService } from '../jee-chat/my-chat/services/message.service';
import { Message } from '../jee-chat/my-chat/models/message';

@Component({
  selector: 'app-share-message',
  templateUrl: './share-message.component.html',
  styleUrls: ['./share-message.component.scss']
})
export class ShareMessageComponent implements OnInit {
  dulieu=new FormControl();
  searchText:string;
  userCurrent:string;
   editor = ClassicEditor;
   public filteredGroups: ReplaySubject<any[]> = new ReplaySubject<any[]>(1);
   public searchControl: FormControl = new FormControl();
list_contact:any[]=[];
list_choose:any[]=[];
list_delete:any[]=[];
  constructor(private dialogRef:MatDialogRef<ShareMessageComponent>,
    private changeDetectorRefs: ChangeDetectorRef,
    private conversation_services:ConversationService,
    private messageService:MessageService,
    private _ngZone:NgZone,
    private chat_services:ChatService,
    private auth:AuthService,
    @Inject(MAT_DIALOG_DATA) public data: any,
    
    ) { const dt=this.auth.getAuthFromLocalStorage();
      this.userCurrent=dt.user.username;}
    LoadDSThanhVien()
    {
        this.chat_services.GetContactChatUser().subscribe(res=>{
          this.list_contact=res.data;
          console.log("TTTTVVCHAT",this.list_contact);
          this.changeDetectorRefs.detectChanges();
        })
    }
  ngOnInit(): void {
  
    this.LoadDSThanhVien();
    console.log("DATA SHARE",this.data.item)
    this.dulieu.setValue(this.data.item.Content_mess)
  }


 
  RemoveChooseMemeber(index)
  {this.list_contact.unshift(this.list_choose[index]);
    this.list_choose.splice(index,1);
    console.log("ListChooseUser sau khi xóa",this.list_choose);
   
  }
  ChooseMember(item)
  {
    let vitri=this.list_contact.findIndex(x=>x.IdGroup==item);
    if(vitri>=0)
    {
      this.searchText="";
  
      this.list_choose.push(this.list_contact[vitri]);
      console.log("ListChooseUser",this.list_choose);
      this.list_contact.splice(vitri,1)
      // this.filteredGroups.next(this.list_thanhvien.slice());
      this.changeDetectorRefs.detectChanges();
    }
  }


  CloseDia(data = undefined)
  {
      this.dialogRef.close(data);
  }
  goBack() {
   
    this.dialogRef.close();
  
  }

  ItemMessenger(idGroup): Message {

    const item = new Message();
   
    
    item.Content_mess=this.data.item.Content_mess;
    item.UserName=this.userCurrent;
    item.IdGroup=idGroup;
   
      item.Note=this.data.item.Note;
    
    item.IsDelAll=this.data.item.IsDelAll;
    item.IsVideoFile=this.data.item.IsVideoFile;
    item.Attachment=this.data.item.AttachFileChat;
  
    return item;
  }
  
  submit()
  {
    for(let i=0;i<this.list_choose.length;i++)
    {
     
      this._ngZone.run(() => {  
      this.messageService.connectToken(this.list_choose[i].IdGroup);
  
    })
      
    setTimeout(() => {
      
 
    let  item =this.ItemMessenger(this.list_choose[i].IdGroup)
    const dt=this.auth.getAuthFromLocalStorage();
    this.messageService.sendMessage(dt.access_token,item,this.list_choose[i].IdGroup).then(() => {
     
 
    })
  }, 5000);
  }
  this.dialogRef.close();
  }
}
