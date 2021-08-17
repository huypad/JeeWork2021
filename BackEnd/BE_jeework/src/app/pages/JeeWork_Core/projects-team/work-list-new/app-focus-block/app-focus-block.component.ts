import { Component, OnInit, ViewChild, ElementRef, Input, Output, EventEmitter, HostListener } from '@angular/core';

@Component({
  selector: 'app-focus-block',
  templateUrl: './app-focus-block.component.html',
  styleUrls: ['./app-focus-block.component.scss']
})
export class AppFocusBlockComponent implements OnInit {

  @ViewChild('innerInput',{static:true}) innerInput: ElementRef;
  @Input() type: string;
  @Input() value: string ="";
  @Input() placeholder: string ="";
  @Input() isbd: boolean = false;
  @Output()  out  = new EventEmitter<any>();
  focussed = false;
  error = false; 
  constructor() { }
  ngOnInit() {
    if(this.value!='-'){
        this.innerInput.nativeElement.value = this.value;
    }
    this.focus();
  }

  f_convertDate(p_Val: any) {
    let a = p_Val === "" ? new Date() : new Date(p_Val);
    return ("0" + (a.getDate())).slice(-2) + "/" + ("0" + (a.getMonth() + 1)).slice(-2) + "/" + a.getFullYear();
  }

  focus() {
    this.innerInput.nativeElement.focus();
  }
  FocusOut() {
    if(this.type == 'email'){
      var filter = /^([a-zA-Z0-9_\.\-])+\@(([a-zA-Z0-9\-])+\.)+([a-zA-Z0-9]{2,4})+$/; 
      if (!filter.test(this.innerInput.nativeElement.value)) { 
        this.error = true;
      }
    }

    if(this.innerInput.nativeElement.value == this.value || this.error){
      this.out.emit('');
    }else{
      this.out.emit(this.innerInput.nativeElement.value);
    }
  }

  handler(){
    if(this.type == 'date'){
      this.FocusOut();
    }
  }

  innerInputFocussed() {
  }

  innerInputBlurred() {
    if(this.type == 'email'){
      var filter = /^([a-zA-Z0-9_\.\-])+\@(([a-zA-Z0-9\-])+\.)+([a-zA-Z0-9]{2,4})+$/; 
      if (!filter.test(this.innerInput.nativeElement.value)) { 
        this.error = true;
      }
    }
  }

  @HostListener('document:keydown', ['$event'])
    onKeydownHandler1(event: KeyboardEvent) {
      if (event.keyCode == 27)//phím ESC
      {
        this.innerInput.nativeElement.value = "";
        this.FocusOut();
      }
    }
}
