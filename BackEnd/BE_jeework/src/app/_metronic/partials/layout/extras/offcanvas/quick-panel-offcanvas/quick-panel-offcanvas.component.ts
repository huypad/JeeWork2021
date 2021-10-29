import { environment } from 'src/environments/environment';
import { Component, NgZone, OnInit } from '@angular/core';
import { LayoutService } from '../../../../../core';
import {ChatService} from '../../jee-chat/my-chat/services/chat.service';
const   HOST_JEEChat=environment.HOST_JEECHAT;
@Component({
  selector: 'app-quick-panel-offcanvas',
  templateUrl: './quick-panel-offcanvas.component.html',
  styleUrls: ['./quick-panel-offcanvas.component.scss'],
})
export class QuickPanelOffcanvasComponent implements OnInit {
  
  extrasQuickPanelOffcanvasDirectionCSSClass = 'offcanvas-right';
  activeTabId:
    | 'kt_quick_panel_notifications'
    | 'kt_quick_panel_settings' = 'kt_quick_panel_notifications';

  constructor(private layout: LayoutService,private chatService:ChatService,
    private _ngZone: NgZone,  
    ) {}
  elementId:string;
  public CData: number;
  search:string;
  hostjeechat:string=HOST_JEEChat;

  ngOnInit(): void {
   this.SetValue();
     this.elementId='kt_quick_panel_close';
    // this.elementId='';
    this.extrasQuickPanelOffcanvasDirectionCSSClass = `offcanvas-${this.layout.getProp(
      'extras.quickPanel.offcanvas.direction'
    )}`;
  }

  SetValue()
  {	this._ngZone.run(() => {  
    this.chatService.search$.subscribe(res=>
      {
        this.search="";
      })
    })
  }

  

	
  setActiveTabId(tabId) {
    this.activeTabId = tabId;
  }

  getActiveCSSClasses(tabId) {
    if (tabId !== this.activeTabId) {
      return '';
    }
    return 'active show';
  }
}
