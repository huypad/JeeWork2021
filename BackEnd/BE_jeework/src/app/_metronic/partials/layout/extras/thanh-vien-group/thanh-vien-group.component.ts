import { AuthService } from 'src/app/modules/auth';
import { Router } from '@angular/router';
import { ChangeDetectorRef, Component, Inject, OnInit } from '@angular/core';
import { MatDialogRef, MAT_DIALOG_DATA } from '@angular/material/dialog';
import { InsertThanhvienComponent } from '../insert-thanhvien/insert-thanhvien.component';
import { ConversationService } from '../jee-chat/my-chat/services/conversation.service';
import { LayoutUtilsService, MessageType } from 'src/app/_metronic/jeework_old/core/utils/layout-utils.service';

@Component({
  selector: 'app-thanh-vien-group',
  templateUrl: './thanh-vien-group.component.html',
  styleUrls: ['./thanh-vien-group.component.scss']
})
export class ThanhVienGroupComponent implements OnInit {

  constructor(private _service:ConversationService,
    private auth:AuthService,
    private conversation_service:ConversationService,
    private layoutUtilsService: LayoutUtilsService,
    private changeDetectorRefs: ChangeDetectorRef, 
    private router:Router,
    private dialogRef:MatDialogRef<InsertThanhvienComponent>,@Inject(MAT_DIALOG_DATA) public data: any) { 

      const authData=this.auth.getAuthFromLocalStorage();
     this.UserIdCurrent=authData.user.customData["jee-account"].userID
    }

  lstThanhVien:any[]=[];
  UserIdCurrent:number;
  adminGroup:boolean=false;

  
  CloseDia(data = undefined)
  {
      this.dialogRef.close(data);
  }
  goBack() {
  
     this.dialogRef.close();
   
   }

 LeaveGroup(UserId:number)
  {
this._service.DeleteThanhVienInGroup(this.data,UserId).subscribe(res=>{
  
  if(res&&res.status==1)
  {
   
  
      this.CloseDia(res);

      // this.router.navigateByUrl('/Chat') //có link thì chuyển link
      var item={IdGroup:this.data,UserId:res}
      this.conversation_service.refreshConversation.next(item);
  }
  else
  {
    this.layoutUtilsService.showActionNotification('Thất bại !', MessageType.Read, 3000, true, false, 3000, 'top', 0);
    
  }
})
  }
  DeleteMember(UserId:number)
  {
this._service.DeleteThanhVienInGroup(this.data,UserId).subscribe(res=>{
  if(res&&res.status==1)
  {
    this.layoutUtilsService.showActionNotification('Thành công !', MessageType.Read, 3000, true, false, 3000, 'top', 1);
  
      this.CloseDia(res);
  }
  else
  {
    this.layoutUtilsService.showActionNotification('Thất bại !', MessageType.Read, 3000, true, false, 3000, 'top', 0);
    
  }
})
  }
  LoadThanhVien()
  {
      this._service.GetThanhVienGroup(this.data).subscribe(res=>
        {
          this.lstThanhVien=res.data;
          this.CheckLoginAdmin();
          this.changeDetectorRefs.detectChanges();
        })
  }
  UpdateAdmin(IdUser:number,key:number)
  {
        this._service.UpdateAdmin(this.data,IdUser,key).subscribe(res=>
          {
            if(res&&res.status===1)
            {
               let index =  this.lstThanhVien.findIndex(x=>x.UserId===IdUser);
               this.lstThanhVien.splice(index,1,res.data[0])
            }
          })
  }
  CheckLoginAdmin()
  {
    
    if(this.lstThanhVien.length>0)
    {
      let id=this.lstThanhVien.find(x=>x.UserId===this.UserIdCurrent&&x.isAdmin==true);
        if(id)
        {
          this.adminGroup=true;
          this.changeDetectorRefs.detectChanges();
        }
    }
  }
  ngOnInit(): void {
    this.LoadThanhVien();


  }

}
