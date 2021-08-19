import { Component, OnInit,Output,Input,EventEmitter } from '@angular/core';

@Component({
  selector: 'app-task-comment',
  templateUrl: './task-comment.component.html',
  styleUrls: ['./task-comment.component.scss']
})
export class TaskCommentComponent implements OnInit {
  @Output() submit = new EventEmitter<any>();
  @Input() value = 0; 
  @Input() id_row = 0; 
  private readonly componentName: string = "kt-task_";
  showcomment = false;
  compopnent = "";
  constructor() { }

  ngOnInit(): void { 
    this.compopnent = this.componentName + this.id_row;
  } 
}
