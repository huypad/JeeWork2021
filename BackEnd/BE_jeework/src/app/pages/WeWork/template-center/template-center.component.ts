import { filter } from 'rxjs/operators';
import { QueryParamsModelNew } from './../../../_metronic/jeework_old/core/models/query-models/query-params.model';
import { LayoutUtilsService } from './../../../_metronic/jeework_old/core/utils/layout-utils.service';
import { TemplateCenterService } from './template-center.service';
import { MatAccordion } from '@angular/material/expansion';
import { MatDialogRef, MAT_DIALOG_DATA } from '@angular/material/dialog';
import { Component, OnInit, Inject, ViewChild, ChangeDetectorRef } from '@angular/core';
import { type } from 'node:os';

@Component({
  selector: 'app-template-center',
  templateUrl: './template-center.component.html',
  styleUrls: ['./template-center.component.scss']
})
export class TemplateCenterComponent implements OnInit {
  @ViewChild(MatAccordion) accordion: MatAccordion;
  buocthuchien = 1;
  importall = true;
  ProjectDatesDefault = true;
  chontacvu = 1;
  DanhSachTC : any = [];
  TemplateDetail : any = [];
  TemplateTypes : any = [];
  TemplateKeyWorks : any = [];
  constructor(
		public dialogRef: MatDialogRef<TemplateCenterComponent>,
		private layoutUtilsService: LayoutUtilsService,
    private templatecenterService:TemplateCenterService,
		private changeDetectorRefs: ChangeDetectorRef,
		@Inject(MAT_DIALOG_DATA) public data: any,
  ) { }

  ngOnInit(): void {
    //load type
    this.templatecenterService.getTemplateTypes().subscribe( (res) => {
      if(res && res.status == 1){
        this.TemplateTypes = res.data;

      }else{
        this.layoutUtilsService.showError(res.error.message)
      }
    });
    this.LoadTC();
  }
  LoadTC(){
    const queryParams = new QueryParamsModelNew(
			this.filterConfiguration(),
			"",
			"",
			1,
			50,
			true
		);
    this.templatecenterService.getTemplateCenter(queryParams).subscribe(res => {
      if(res && res.status == 1){
        this.DanhSachTC = res.data;
        console.log(this.DanhSachTC);
        this.changeDetectorRefs.detectChanges();
      }else{
        this.layoutUtilsService.showError(res.error.message)
      }
    })
  }
  filterConfiguration(): any {
    var listType = [];
    this.Types.forEach(element => {
      if(element.checked){
        listType.push(element.id);
      }
    })
    var listLevel = [];
    this.Levels.forEach(element => {
      if(element.checked){
        listLevel.push(element.id);
      }
    })
    var listTemplateTypes = [];
    this.TemplateTypes.forEach(element => {
      if(element.isdefault){
        listTemplateTypes.push(element.id_row);
      }
    })
		const filter: any = {};
		filter.keyword = this.TemplateKeyWorks;
		filter.template_typeid = listTemplateTypes.join();//API: WeworkLiteController.lite_template_types
		filter.types = listType.join(); //1 - space, 2 - folder, 3 - list (Project)
		filter.levels = listLevel.join();//1 - Beginner, 2 - Intermediate, 3 - Advanced
		filter.collect_by = "";//Người tạo (Table: we_template_customer)
		return filter;
	}
  SelectedTemplate(item){
    this.TemplateDetail = item;
    console.log(item);
    this.NextStep();
    this.templatecenterService.getDetailTemplate(item.id_row).subscribe(res => {
      if(res && res.status == 1){
        console.log(res.data);
      }
    })
  }
  NextStep(){
    this.buocthuchien += 1;
  }
  PrevStep(){ 
    if(this.buocthuchien == 1){
      this.dialogRef.close();
    }
    else{
      this.buocthuchien -= 1;
    }
  }
  getBackground(text){
    return 'rgb(27, 94, 32)';
  }
  getTemplateCenterTemplate(item){
    if(item.types == 1){ // space
      return "cu-template-center-template__space";
    } else if(item.types == 2){ // folder
      return "cu-template-center-template__folder";
    } else if(item.types == 3){ // list
      return "cu-template-center-template__list";
    }
    return "cu-template-center-template__space";
  }
  getTypesName(item){
    if(item.types == 1){ // space
      return "SPACE";
    } else if(item.types == 2){ // folder
      return "FOLDER";
    } else if(item.types == 3){ // list
      return "LIST";
    }
    return "SPACE";
  }
  getIconTemplate(item){
    if(item.types == 1){ // space
      return "https://cdn1.iconfinder.com/data/icons/space-exploration-and-next-big-things-5/512/676_Astrology_Planet_Space-512.png";
    } else if(item.types == 2){ // folder
      return "https://png.pngtree.com/png-vector/20190215/ourlarge/pngtree-vector-folder-icon-png-image_554064.jpg";
    } else if(item.types == 3){ // list
      return "https://img.pngio.com/list-icons-free-download-png-and-svg-list-icon-png-256_256.png";
    }
    return "";
  }
  countID(str){
    if(str=="")
      return 0;
    var lst = str.split(',');
    return lst.length ;
  }
  CheckedType(item){
    if(item.countitem >0 ){
      item.checked = !item.checked;
    }
    this.LoadTC();
  }
  Types = [
    {
      checked:true,
      name:'Space',
      id:'1',
      countitem: 5,
    },
    {
      checked:false,
      name:'Folder',
      id:'2',
      countitem: 68,
    },
    {
      checked:false,
      name:'List',
      id:'3',
      countitem: 15,
    },
    // {
    //   checked:false,
    //   name:'Task',
    //   countitem: 0,
    // },
    // {
    //   checked:false,
    //   name:'Doc',
    //   countitem: 40,
    // },
    // {
    //   checked:false,
    //   name:'View',
    //   countitem: 0,
    // },
  ];
  Levels = [ //1 - Beginner, 2 - Intermediate, 3 - Advanced
    {
      checked:false,
      name:'Beginner',
      id:'1',
      countitem: 79,
    },
    {
      checked:false,
      name:'Intermediate',
      id:'2',
      countitem: 50,
    },
    {
      checked:false,
      name:'Advanced',
      id:'3',
      countitem: 17,
    },
  ]
}
