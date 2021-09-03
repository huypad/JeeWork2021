import { Component, OnInit, ChangeDetectorRef } from '@angular/core';
import { FormControl } from '@angular/forms';
import { MatDialogRef } from '@angular/material/dialog';
import { ReplaySubject } from 'rxjs';
import { CreateConvesationGroupComponent } from '../create-convesation-group/create-convesation-group.component';
import { ConversationModel } from '../my-chat/models/conversation';
import { ConversationService } from '../my-chat/services/conversation.service';

@Component({
  selector: 'app-create-conversation-user',
  templateUrl: './create-conversation-user.component.html',
  styleUrls: ['./create-conversation-user.component.scss']
})
export class CreateConversationUserComponent implements OnInit {
  public searchControl: FormControl = new FormControl();

  public filteredGroups: ReplaySubject<any[]> = new ReplaySubject<any[]>(1);
  constructor(private conversation_sevices: ConversationService,
    private dialogRef: MatDialogRef<CreateConvesationGroupComponent>,
    private changeDetectorRefs: ChangeDetectorRef

  ) { }

  listDanhBa: any[] = [];
  listUser: any[] = []

  ItemConversation(ten_group: string, data: any): ConversationModel {

    this.listUser.push(data);
    const item = new ConversationModel();
    item.GroupName = ten_group;
    item.IsGroup = false;

    item.ListMember = this.listUser.slice();


    return item
  }

  CreateConverSation(item) {

    let it = this.ItemConversation(item.FullName, item);
    this.conversation_sevices.CreateConversation(it).subscribe(res => {
      console.log('create conversat', res.data)
      if (res && res.status === 1) {
        this.listUser = []
        this.CloseDia(res.data);
      }
    })

  }
  CloseDia(data = undefined) {
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

  GetDanhBa() {
    this.conversation_sevices.GetDanhBaNotConversation().subscribe(res => {
      this.listDanhBa = res.data;
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

  getNameUser(item: any) {
    let name = item.FullName.split(" ")[item.FullName.split(" ").length - 1];
    return name.substring(0, 1).toUpperCase();
  }

  getColorNameUser(item: any) {
    let value = item.FullName.split(" ")[item.FullName.split(" ").length - 1];
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
}
