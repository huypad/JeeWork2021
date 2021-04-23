import { Component, OnInit, Input, ChangeDetectionStrategy, HostListener, ElementRef, ChangeDetectorRef, ViewChild, ViewContainerRef, SimpleChange, Output, EventEmitter } from '@angular/core';
import { DynamicFormService } from './dynamic-form.service';
import { Observable, fromEvent, of, BehaviorSubject, ReplaySubject } from 'rxjs';
 import { FormGroup, FormBuilder, FormControl, Validators } from '@angular/forms';
import { Moment } from 'moment';
import * as moment from 'moment';
import { DatePipe } from '@angular/common';
import { TranslateService } from '@ngx-translate/core';
import { tinyMCE } from '../components/tinyMCE';
import { LayoutUtilsService, MessageType } from '../JeeHR/_core/utils/layout-utils.service';
import { DanhMucChungService } from '../JeeHR/_core/services/danhmuc.service';
import { BaseModel } from '../JeeHR/_core/models/_base.model';
import { ChuyenGiaiDoanData } from './dynamic-form.model';
import { Router } from '@angular/router';
//#region Type Control DymaForm Thiên:
//0: 
//1: TextBox (Input)
//2: TextBox (Input_Number)
//3: DateTime
//4: Textarea
//5: Select
//6: Multiselect
//7: boolean
//8: Checkbox
//9: Radio Button
//10: Image
//11: Dropdowntree
//12: Multipleimage
//13: File
//14: Multiplefile
//Đối với loại 5,6 cần truyền api để tải dữ liệu xổ xuống
//#endregion


//Xem thông tin
@Component({
    selector: 'm-dynamic-form',
    templateUrl: './dynamic-form.component.html',
    changeDetection: ChangeDetectionStrategy.OnPush
})


//TypeID 0: Tạo công viêc - 2: Chỉ xem
//implements OnInit
export class DynamicFormComponent implements OnInit {
    constructor(public dynamicFormService: DynamicFormService,
        private fb: FormBuilder,
        private datePipe: DatePipe,
        private translate: TranslateService,
        private changeDetectorRefs: ChangeDetectorRef,
        private layoutUtilsService: LayoutUtilsService,
        private danhMucChungService: DanhMucChungService,) { }

    formControls: FormGroup; 1
    showSearch: boolean = false;
    controls: any[] = [];
    ID_Struct: string = '';
    listChucDanh: any[] = [];
    listChucVu: any[] = [];
    d_minDate: any;
    d_maxDate: any;
    selectedNode: BehaviorSubject<any> = new BehaviorSubject([]);
    listData: any[] = [];
    tmp_thang: string = '';
    tmp_nam: string = '';
    search: string = '';

    listControls: any[] = [];
    @Input() ID: any;
    @Input() TypeID: any;
    @Input() ViewData: any; //Dùng cho xem chi tiết dữ liệu
    tinyMCE = {};
    @Output() Close = new EventEmitter();
    ProcessID: number;
    listProcess: any[] = [];
    listNguoiThucHien: any[] = [];
    disabledBtn: boolean;
    //====================Người theo dõi===================
    listNguoiTheoDoi: any[] = [];
    public bankFilterCtrlNTD: FormControl = new FormControl();
    public filteredBanksNTD: ReplaySubject<any[]> = new ReplaySubject<any[]>(1);
    showLuu: boolean = false;
    ngOnChanges(changes: SimpleChange) {
        if (changes['ViewData']) {
            this.ngOnInit();
        }
    }

    ngOnInit(): void {
        this.reset();
        this.tinyMCE = tinyMCE;
        if (this.TypeID == 2) {
            this.viewForm();
        }
        this.danhMucChungService.GetDSQuyTrinhDong().subscribe(res => {
            if (res && res.status == 1) {
                this.listProcess = res.data;
                this.changeDetectorRefs.detectChanges();
            } else {
                this.layoutUtilsService.showActionNotification(res.error.message, MessageType.Read, 9999999999, true, false, 3000, 'top', 0);
            }
        })
    }


    buildForm() {
        for (var i = 0; i < this.controls.length; i++) {
            let index = i;
            if (this.controls[i].APIData) {
                if (this.controls[index].ControlID == 5 || this.controls[index].ControlID == 6 || this.controls[index].ControlID == 9 || this.controls[index].ControlID == 11) {
                    let LinkAPI = "";
                    if (this.controls[index].DependID == null && !this.controls[index].IsDepend) {
                        LinkAPI = this.controls[index].APIData + this.controls[index].FieldID;
                    } else {
                        LinkAPI = this.controls[index].APIData;
                    }
                    if (this.controls[index].ControlID == 5 || this.controls[index].ControlID == 6 || this.controls[index].ControlID == 9) {
                        this.dynamicFormService.getInitData(LinkAPI).subscribe(res => {
                            if (res.data.length > 0) {
                                this.controls[index].init = res.data;
                            } else {
                                this.controls[index].init = [];
                            }
                            this.changeDetectorRefs.detectChanges();
                        });
                    } else {
                        this.dynamicFormService.getInitData(LinkAPI).subscribe(res => {
                            this.controls[index].init = new BehaviorSubject([]);
                            if (res.data.length > 0) {
                                this.controls[index].init.next(res.data);
                            } else {
                                this.controls[index].init.next([]);
                            }
                        });
                    }
                }
            }
        }
        this.createForm();
    }

    viewForm() {
        let count_Luu = 0;
        this.controls = this.ViewData;
        for (var i = 0; i < this.controls.length; i++) {
            if (this.controls[i].IsFieldNode) {
                count_Luu++;
            }
            let index = i;
            if (this.controls[i].APIData) {
                if (this.controls[index].ControlID == 5 || this.controls[index].ControlID == 6 || this.controls[index].ControlID == 9 || this.controls[index].ControlID == 11) {
                    let LinkAPI = "";
                    if (this.controls[index].DependID == null && !this.controls[index].IsDepend) {
                        LinkAPI = this.controls[index].APIData + this.controls[index].FieldID;
                    } else {
                        LinkAPI = this.controls[index].APIData;
                    }
                    if (this.controls[index].ControlID == 5 || this.controls[index].ControlID == 6 || this.controls[index].ControlID == 9) {
                        this.dynamicFormService.getInitData(LinkAPI).subscribe(res => {
                            if (res.data.length > 0) {
                                this.controls[index].init = res.data;
                            } else {
                                this.controls[index].init = [];
                            }
                            this.changeDetectorRefs.detectChanges();
                        });
                    } else {
                        this.dynamicFormService.getInitData(LinkAPI).subscribe(res => {
                            this.controls[index].init = new BehaviorSubject([]);
                            if (res.data.length > 0) {
                                this.controls[index].init.next(res.data);
                            } else {
                                this.controls[index].init.next([]);
                            }
                        });
                    }
                }
            }
        }
        if (count_Luu > 0) {
            this.showLuu = true;
        } else {
            this.showLuu = false;
            this.goBack();
        }
        this.createViewForm();
    }

    reset() {
        let item = {};
        this.formControls = this.fb.group(item);
        if (this.TypeID == 0) {
            this.formControls.addControl('quyTrinh', new FormControl('', [Validators.required]));
            this.formControls.addControl('tenCongViec', new FormControl('', [Validators.required]));
            this.formControls.addControl('nguoiThucHien', new FormControl(''));
            this.formControls.addControl('file', new FormControl(''));
            this.formControls.addControl('noiDung', new FormControl(''));
            this.formControls.addControl('nguoiTheoDoi', new FormControl(''));

            this.formControls.controls['tenCongViec'].markAsTouched();
            this.formControls.controls['quyTrinh'].markAsTouched();
        }
    }

    createForm() {
        let item = {};
        this.formControls = this.fb.group(item);
        this.formControls.addControl('tenCongViec', new FormControl('', [Validators.required]));
        this.formControls.addControl('quyTrinh', new FormControl('' + this.ProcessID, [Validators.required]));
        this.formControls.addControl('nguoiThucHien', new FormControl(''));
        this.formControls.addControl('file', new FormControl(''));
        this.formControls.addControl('noiDung', new FormControl(''));
        this.formControls.addControl('nguoiTheoDoi', new FormControl(''));

        this.formControls.controls['tenCongViec'].markAsTouched();
        this.formControls.controls['quyTrinh'].markAsTouched();

        for (var i = 0; i < this.controls.length; i++) {
            let control = this.controls[i];
            if (control.Required) {
                this.formControls.addControl(control.RowID, new FormControl('', [Validators.required]));
                this.formControls.controls[control.RowID].markAsTouched();
            } else {
                this.formControls.addControl(control.RowID, new FormControl(''));
            }
        }
    }

    createViewForm() {
        let item = {};
        this.formControls = this.fb.group(item);
        for (var i = 0; i < this.controls.length; i++) {
            let control = this.controls[i];
            if (control.ControlID == 6) {
                this.formControls.addControl(control.RowID, new FormControl(control.Value ? control.Value : ''));
            } else if (control.ControlID == 7 || control.ControlID == 8) {
                this.formControls.addControl(control.RowID, new FormControl(control.Value == "True" ? true : false));
            } else if (control.ControlID == 10 || control.ControlID == 12 || control.ControlID == 13 || control.ControlID == 14) {
                this.formControls.addControl(control.RowID, new FormControl(control.files ? control.files : []));
            } else {
                this.formControls.addControl(control.RowID, new FormControl(control.Value ? '' + control.Value : ''));
            }
            if (control.Required) {
                this.formControls.controls['' + control.RowID].markAsTouched();
            }
        }
    }



    //====================================Xử lý sự kiện change=========================
    GetValueNode(val: any, item: any) {
        let StructID = val.RowID;
        let obj = this.controls.find(x => x.RowID === item.DependID);
        let index = this.controls.findIndex(x => x.RowID === item.DependID);
        let LinkAPI = obj.APIData + StructID;
        this.dynamicFormService.getInitData(LinkAPI).subscribe(res => {
            if (res.data.length > 0) {
                this.controls[index].init = res.data;
            } else {
                this.controls[index].init = [];
            }
        });
    }

    changeQuyTrinh(val: any) {
        this.ProcessID = val;
        if (this.TypeID == 0) {
            this.dynamicFormService.Get_ControlsList(this.ProcessID).subscribe(res => {
                if (res && res.status == 1) {
                    if (res.data.length > 0) {
                        this.controls = res.data;
                        this.buildForm();
                        this.changeDetectorRefs.detectChanges();
                    } else {
                        this.controls = [];
                        this.buildForm();
                        this.changeDetectorRefs.detectChanges();
                    }
                } else {
                    this.controls = [];
                    this.buildForm();
                    this.changeDetectorRefs.detectChanges();
                }
            })
        } else {
            this.dynamicFormService.Get_ControlsListNext(this.ProcessID).subscribe(res => {
                if (res && res.status == 1) {
                    if (res.data.length > 0) {
                        this.controls = res.data;
                        this.buildForm();
                        this.changeDetectorRefs.detectChanges();
                    } else {
                        this.controls = [];
                        this.buildForm();
                        this.changeDetectorRefs.detectChanges();
                    }
                } else {
                    this.controls = [];
                    this.buildForm();
                    this.changeDetectorRefs.detectChanges();
                }
            })
        }

        this.loadData();
    }

    loadData() {
        this.danhMucChungService.GetDSNguoiApDung().subscribe(res => {
            if (res.data && res.data.length > 0) {
                this.listNguoiTheoDoi = res.data;
                this.setUpDropSearcTheoDoi();
                this.changeDetectorRefs.detectChanges();
            }
        });

        this.dynamicFormService.GetImplementerList(this.ProcessID).subscribe(res => {
            if (res.data && res.data.length > 0) {
                this.listNguoiThucHien = res.data;
                this.changeDetectorRefs.detectChanges();
            }
        })
    }

    setUpDropSearcTheoDoi() {
        this.bankFilterCtrlNTD.setValue('');
        this.filterBanksNTD();
        this.bankFilterCtrlNTD.valueChanges
            .pipe()
            .subscribe(() => {
                this.filterBanksNTD();
            });
    }

    protected filterBanksNTD() {
        if (!this.listNguoiTheoDoi) {
            return;
        }
        // get the search keyword
        let search = this.bankFilterCtrlNTD.value;
        if (!search) {
            this.filteredBanksNTD.next(this.listNguoiTheoDoi.slice());
            return;
        } else {
            search = search.toLowerCase();
        }
        // filter the banks
        this.filteredBanksNTD.next(
            this.listNguoiTheoDoi.filter(bank => bank.Title.toLowerCase().indexOf(search) > -1)
        );
    }
    //===========================================================================================

    submit() {
        const controls = this.formControls.controls;
        if (this.formControls.invalid) {
            Object.keys(controls).forEach(controlName =>
                controls[controlName].markAsTouched()
            );
            return;
        }

        const updatedegree = this.prepareCustomer();
        this.Create(updatedegree);


    }

    prepareCustomer(): any {
        const controls = this.formControls.controls;
        //=========Xử lý cho phần form động=====================
        let Data_Field = [];
        if (this.controls.length > 0) {
            for (var i = 0; i < this.controls.length; i++) {
                let _field = {
                    RowID: this.controls[i].RowID,
                    ControlID: this.controls[i].ControlID,
                    Value: controls[this.controls[i].RowID].value,
                }
                Data_Field.push(_field);
            }
        }
        return Data_Field;
    }

    Create(_item: any) {
        this.disabledBtn = true;
        this.layoutUtilsService.showWaitingDiv();
        this.dynamicFormService.updateThongTinCanNhap(_item).subscribe(res => {
            this.disabledBtn = false;
            this.layoutUtilsService.OffWaitingDiv();
            this.changeDetectorRefs.detectChanges();
            if (res && res.status === 1) {
                const _messageType = "Câp nhật thành công";
                this.layoutUtilsService.showActionNotification(_messageType, MessageType.Update, 4000, true, false)
                this.goBack();
            }
            else {
                this.layoutUtilsService.showActionNotification(res.error.message, MessageType.Read, 9999999999, true, false, 3000, 'top', 0);
            }
        });
    }

    goBack() {
        this.Close.emit();
    }

    @HostListener('document:keydown', ['$event'])
    onKeydownHandler1(event: KeyboardEvent) {
        if (this.showSearch) {
            if (event.keyCode == 13)//phím Enter
            {
                this.submit();
            }
        }
    }

    //đến ngày luôn lấy min là từ ngày
    DateChangeMin(val: any, index) {
        if (val.value != null && val.value != "") {
            let mindate = val.value.toDate();
            this.controls[index].to.min = mindate;
        }
    }

    //từ ngày luôn lấy max là đến ngày
    DateChangemMax(val: any, index) {
        if (val.value != null && val.value != "") {
            let maxdate_cur = val.value.toDate();
            this.controls[index].from.max = maxdate_cur;
        }
    }

    getFirstDay_LastDay(b_firstday) {
        var date = new Date(), y = date.getFullYear(), m = date.getMonth();
        var firstDay = new Date(y, m, 1); // ngày đầu tháng
        var lastDay = new Date(y, m + 1, 0); // ngày cuối tháng
        var curent = new Date(); // ngày hiện tại
        return b_firstday ? this.datePipe.transform(firstDay, 'dd/MM/yyyy') : this.datePipe.transform(lastDay, 'dd/MM/yyyy');
    }



    //============================================================================
    DateChangeMinCK(val: any) {
        this.d_minDate = val.value;
    }

    DateChangeMaxCK(val: any) {
        this.d_maxDate = val.value;
    }

    f_convertDate(v: any) {
        if (v != "" && v != undefined) {
            let a = new Date(v);
            return a.getFullYear() + "-" + ("0" + (a.getMonth() + 1)).slice(-2) + "-" + ("0" + (a.getDate())).slice(-2) + "T00:00:00.0000000";
        }
    }
    ChangeChucDanh(event: any, index) {
        this.controls[index].value = "," + event.value;
    }
    textNam(e: any) {
        if (!((e.keyCode > 95 && e.keyCode < 106)
            || (e.keyCode > 47 && e.keyCode < 58)
            || e.keyCode == 8)) {
            e.preventDefault();
        }
    }
}


//Dùng cho chuyển giai đoạn
@Component({
    selector: 'm-dynamic-form-move',
    templateUrl: './dynamic-form-move.component.html',
    changeDetection: ChangeDetectionStrategy.OnPush
})

export class DynamicFormMoveComponent implements OnInit {
    constructor(public dynamicFormService: DynamicFormService,
        private fb: FormBuilder,
        private datePipe: DatePipe,
        private translate: TranslateService,
        private changeDetectorRefs: ChangeDetectorRef,
        private layoutUtilsService: LayoutUtilsService,
        private danhMucChungService: DanhMucChungService,) { }

    formControls: FormGroup;
    showSearch: boolean = false;
    controls: any[] = [];
    ID_Struct: string = '';
    listChucDanh: any[] = [];
    listChucVu: any[] = [];
    d_minDate: any;
    d_maxDate: any;
    selectedNode: BehaviorSubject<any> = new BehaviorSubject([]);
    tmp_thang: string = '';
    tmp_nam: string = '';
    search: string = '';

    listControls: any[] = [];
    @Input() ID: any;
    @Input() TypeID: any;
    @Input() ViewData: any; //Dùng cho xem chi tiết dữ liệu
    @Input() isNext: any;
    @Input() nodeListID: any;//ID giai đoạn chuyển tới (Dùng trong kanban)
    tinyMCE = {};
    @Output() Close = new EventEmitter();
    ProcessID: number;
    listProcess: any[] = [];
    listNguoiThucHien: any[] = [];
    disabledBtn: boolean;
    //====================Người theo dõi===================
    listNguoiTheoDoi: any[] = [];
    public bankFilterCtrlNTD: FormControl = new FormControl();
    public filteredBanksNTD: ReplaySubject<any[]> = new ReplaySubject<any[]>(1);
    //===============Thông tin chuyển giai đoạn======================
    listData: any[] = [];
    GiaiDoanID: number;
    IsNext: boolean = false;
    NodeListID: number;
    //===============================================================
    ngOnInit(): void {
        this.reset();
        this.GiaiDoanID = this.ID;
        this.IsNext = this.isNext;
        this.NodeListID = this.nodeListID;
        this.dynamicFormService.Get_FieldsNode(this.GiaiDoanID, this.NodeListID).subscribe(res => {
            if (res && res.status == 1) {
                this.listData = res.data;
                this.buildForm();
                this.changeDetectorRefs.detectChanges();
            }
        })
    }


    buildForm() {
        for (let j = 0; j < this.listData.length; j++) {
            for (var i = 0; i < this.listData[j].dt_fieldnode.length; i++) {
                let index = i;
                if (this.listData[j].dt_fieldnode[i].APIData) {
                    if (this.listData[j].dt_fieldnode[index].ControlID == 5 || this.listData[j].dt_fieldnode[index].ControlID == 6 || this.listData[j].dt_fieldnode[index].ControlID == 9 || this.listData[j].dt_fieldnode[index].ControlID == 11) {
                        let LinkAPI = "";
                        if (this.listData[j].dt_fieldnode[index].DependID == null && !this.listData[j].dt_fieldnode[index].IsDepend) {
                            LinkAPI = this.listData[j].dt_fieldnode[index].APIData + this.listData[j].dt_fieldnode[index].FieldID;
                        } else {
                            LinkAPI = this.listData[j].dt_fieldnode[index].APIData;
                        }
                        if (this.listData[j].dt_fieldnode[index].ControlID == 5 || this.listData[j].dt_fieldnode[index].ControlID == 6 || this.listData[j].dt_fieldnode[index].ControlID == 9) {
                            this.dynamicFormService.getInitData(LinkAPI).subscribe(res => {
                                if (res.data.length > 0) {
                                    this.listData[j].dt_fieldnode[index].init = res.data;
                                } else {
                                    this.listData[j].dt_fieldnode[index].init = [];
                                }
                                this.changeDetectorRefs.detectChanges();
                            });
                        } else {
                            this.dynamicFormService.getInitData(LinkAPI).subscribe(res => {
                                this.listData[j].dt_fieldnode[index].init = new BehaviorSubject([]);
                                if (res.data.length > 0) {
                                    this.listData[j].dt_fieldnode[index].init.next(res.data);
                                } else {
                                    this.listData[j].dt_fieldnode[index].init.next([]);
                                }
                            });
                        }
                    }
                }
            }
        }
        this.createForm();
    }

    reset() {
        let item = {};
        this.formControls = this.fb.group(item);
    }

    createForm() {
        let item = {};
        this.formControls = this.fb.group(item);
        for (let j = 0; j < this.listData.length; j++) {
            for (var i = 0; i < this.listData[j].dt_fieldnode.length; i++) {
                let control = this.listData[j].dt_fieldnode[i];
                if (control.Required) {
                    // if (control.ControlID == 7 || control.ControlID == 8) {
                    //     this.formControls.addControl(control.RowID, new FormControl(false, [Validators.required]));
                    //     this.formControls.controls[control.RowID].markAsTouched();
                    // } else {
                    //     this.formControls.addControl(control.RowID, new FormControl('', [Validators.required]));
                    //     this.formControls.controls[control.RowID].markAsTouched();
                    // }
                    if (control.ControlID == 6) {
                        this.formControls.addControl(control.RowID, new FormControl(control.Value ? control.Value : ''));
                    } else if (control.ControlID == 7 || control.ControlID == 8) {
                        this.formControls.addControl(control.RowID, new FormControl(control.Value == "True" ? true : false));
                    } else if (control.ControlID == 10 || control.ControlID == 12 || control.ControlID == 13 || control.ControlID == 14) {
                        this.formControls.addControl(control.RowID, new FormControl(control.files ? control.files : []));
                    } else {
                        this.formControls.addControl(control.RowID, new FormControl(control.Value ? '' + control.Value : ''));
                    }
                    this.formControls.controls[control.RowID].markAsTouched()

                } else {
                    // this.formControls.addControl(control.RowID, new FormControl(''));

                    if (control.ControlID == 6) {
                        this.formControls.addControl(control.RowID, new FormControl(control.Value ? control.Value : ''));
                    } else if (control.ControlID == 7 || control.ControlID == 8) {
                        this.formControls.addControl(control.RowID, new FormControl(control.Value == "True" ? true : false));
                    } else if (control.ControlID == 10 || control.ControlID == 12 || control.ControlID == 13 || control.ControlID == 14) {
                        this.formControls.addControl(control.RowID, new FormControl(control.files ? control.files : []));
                    } else {
                        this.formControls.addControl(control.RowID, new FormControl(control.Value ? '' + control.Value : ''));
                    }
                }
            }
        }

    }

    //====================================Xử lý sự kiện change=========================
    GetValueNode(val: any, item: any) {
        let StructID = val.RowID;
        let obj = this.controls.find(x => x.RowID === item.DependID);
        let index = this.controls.findIndex(x => x.RowID === item.DependID);
        let LinkAPI = obj.APIData + StructID;
        this.dynamicFormService.getInitData(LinkAPI).subscribe(res => {
            if (res.data.length > 0) {
                this.controls[index].init = res.data;
            } else {
                this.controls[index].init = [];
            }
        });
    }

    nguoiThucHienChange(val: any, item: any) {
        item.IDnguoiThucHien = val;
    }
    //===========================================================================================

    submit() {
        const controls = this.formControls.controls;
        if (this.formControls.invalid) {
            Object.keys(controls).forEach(controlName =>
                controls[controlName].markAsTouched()
            );
            let message = 'Vui lòng nhập đầy đủ thông tin trường dữ liệu bắt buộc';
            this.layoutUtilsService.showActionNotification(message, MessageType.Read, 9999999999, true, false, 3000, 'top', 0);
            return;
        }

        const updatedegree = this.prepareCustomer();
        this.Create(updatedegree);

    }

    prepareCustomer(): any {
        const controls = this.formControls.controls;
        let Data_InfoChuyenGiaiDoanData = [];
        //=========Xử lý cho phần form động=====================
        if (this.listData.length > 0) {
            for (var j = 0; j < this.listData.length; j++) {
                let Data_Field = [];
                for (var i = 0; i < this.listData[j].dt_fieldnode.length; i++) {
                    let _field = {
                        RowID: this.listData[j].dt_fieldnode[i].RowID,
                        ControlID: this.listData[j].dt_fieldnode[i].ControlID,
                        Value: controls[this.listData[j].dt_fieldnode[i].RowID].value,
                    }
                    Data_Field.push(_field);
                }
                let _info = {
                    RowID: this.listData[j].RowID,
                    NguoiThucHienID: this.listData[j].IDnguoiThucHien,
                    FieldNode: Data_Field,
                };
                Data_InfoChuyenGiaiDoanData.push(_info);
            }
        }

        let _item = {
            NodeID: this.GiaiDoanID,
            InfoChuyenGiaiDoanData: Data_InfoChuyenGiaiDoanData,
            IsNext: this.IsNext,
            NodeListID: this.NodeListID,
        };
        return _item;
    }

    Create(_item: any) {
        this.disabledBtn = true;
        this.layoutUtilsService.showWaitingDiv();
        this.dynamicFormService.ChuyenGiaiDoan(_item).subscribe(res => {
            this.layoutUtilsService.OffWaitingDiv();
            this.disabledBtn = false;
            this.changeDetectorRefs.detectChanges();
            if (res && res.status === 1) {
                const _messageType = this.translate.instant('workprocess.chuyengiaidoanthanhcong');
                this.layoutUtilsService.showActionNotification(_messageType, MessageType.Update, 4000, true, false)
                this.goBack();
            }
            else {
                this.layoutUtilsService.showActionNotification(res.error.message, MessageType.Read, 9999999999, true, false, 3000, 'top', 0);
            }
        });
    }

    goBack() {
        this.Close.emit();
    }
    //bắt sự kiện click ngoài form search thì đóng form
    @HostListener('document:click', ['$event'])
    clickout(event) {
        if (event.target.id == 'id-form-filter-container')
            this.showSearch = false;
    }

    @HostListener('document:keydown', ['$event'])
    onKeydownHandler1(event: KeyboardEvent) {
        if (this.showSearch) {
            if (event.keyCode == 13)//phím Enter
            {
                this.submit();
            }
        }
    }

    //đến ngày luôn lấy min là từ ngày
    DateChangeMin(val: any, index) {
        if (val.value != null && val.value != "") {
            let mindate = val.value.toDate();
            this.controls[index].to.min = mindate;
        }
    }

    //từ ngày luôn lấy max là đến ngày
    DateChangemMax(val: any, index) {
        if (val.value != null && val.value != "") {
            let maxdate_cur = val.value.toDate();
            this.controls[index].from.max = maxdate_cur;
        }
    }

    getFirstDay_LastDay(b_firstday) {
        var date = new Date(), y = date.getFullYear(), m = date.getMonth();
        var firstDay = new Date(y, m, 1); // ngày đầu tháng
        var lastDay = new Date(y, m + 1, 0); // ngày cuối tháng
        var curent = new Date(); // ngày hiện tại
        return b_firstday ? this.datePipe.transform(firstDay, 'dd/MM/yyyy') : this.datePipe.transform(lastDay, 'dd/MM/yyyy');
    }



    //============================================================================
    DateChangeMinCK(val: any) {
        this.d_minDate = val.value;
    }

    DateChangeMaxCK(val: any) {
        this.d_maxDate = val.value;
    }

    f_convertDate(v: any) {
        if (v != "" && v != undefined) {
            let a = new Date(v);
            return a.getFullYear() + "-" + ("0" + (a.getMonth() + 1)).slice(-2) + "-" + ("0" + (a.getDate())).slice(-2) + "T00:00:00.0000000";
        }
    }
    ChangeChucDanh(event: any, index) {
        this.controls[index].value = "," + event.value;
    }
    textNam(e: any) {
        if (!((e.keyCode > 95 && e.keyCode < 106)
            || (e.keyCode > 47 && e.keyCode < 58)
            || e.keyCode == 8)) {
            e.preventDefault();
        }
    }
}


//Dùng cho tạo công việc
@Component({
    selector: 'm-dynamic-form-create',
    templateUrl: './dynamic-form-create.component.html',
    changeDetection: ChangeDetectionStrategy.OnPush
})

export class DynamicFormCreateComponent implements OnInit {
    constructor(public dynamicFormService: DynamicFormService,
        private fb: FormBuilder,
        private datePipe: DatePipe,
        private translate: TranslateService,
        private changeDetectorRefs: ChangeDetectorRef,
        private layoutUtilsService: LayoutUtilsService,
        private danhMucChungService: DanhMucChungService,
        private route: Router,) { }

    formControls: FormGroup;
    showSearch: boolean = false;
    controls: any[] = [];
    ID_Struct: string = '';
    listChucDanh: any[] = [];
    listChucVu: any[] = [];
    d_minDate: any;
    d_maxDate: any;
    selectedNode: BehaviorSubject<any> = new BehaviorSubject([]);
    tmp_thang: string = '';
    tmp_nam: string = '';
    search: string = '';

    listControls: any[] = [];
    @Input() ID: any;
    @Input() TypeID: any;
    @Input() ViewData: any; //Dùng cho xem chi tiết dữ liệu
    tinyMCE = {};
    @Output() Close = new EventEmitter();
    ProcessID: number;
    listProcess: any[] = [];
    listNguoiThucHien: any[] = [];
    disabledBtn: boolean;
    //====================Người theo dõi===================
    listNguoiTheoDoi: any[] = [];
    public bankFilterCtrlNTD: FormControl = new FormControl();
    public filteredBanksNTD: ReplaySubject<any[]> = new ReplaySubject<any[]>(1);
    //===============Thông tin chuyển giai đoạn======================
    listData: any[] = [];
    GiaiDoanID: number;
    //===============================================================
    ngOnInit(): void {
        this.reset();
        this.tinyMCE = tinyMCE;
        this.danhMucChungService.GetDSQuyTrinhDong().subscribe(res => {
            if (res && res.status == 1) {
                this.listProcess = res.data;
                if (this.ID > 0) {
                    this.formControls.controls['quyTrinh'].setValue('' + this.ID);
                    this.changeQuyTrinh(this.ID);
                    this.formControls.controls['quyTrinh'].disable();
                }
                this.changeDetectorRefs.detectChanges();
            } else {
                this.layoutUtilsService.showActionNotification(res.error.message, MessageType.Read, 9999999999, true, false, 3000, 'top', 0);
            }
        })
    }

    changeQuyTrinh(val: any) {
        this.ProcessID = val;
        this.dynamicFormService.Get_ControlsList(this.ProcessID).subscribe(res => {
            if (res && res.status == 1) {
                if (res.data.length > 0) {
                    this.listData = res.data;
                    this.buildForm();
                    this.changeDetectorRefs.detectChanges();
                } else {
                    this.listData = [];
                    this.buildForm();
                    this.changeDetectorRefs.detectChanges();
                }
            } else {
                this.listData = [];
                this.buildForm();
                this.layoutUtilsService.showActionNotification(res.error.message, MessageType.Read, 9999999999, true, false, 3000, 'top', 0);
                this.changeDetectorRefs.detectChanges();
            }
        })

        this.loadData();
    }

    loadData() {
        this.danhMucChungService.GetDSNguoiApDung().subscribe(res => {
            if (res.data && res.data.length > 0) {
                this.listNguoiTheoDoi = res.data;
                this.setUpDropSearcTheoDoi();
                this.changeDetectorRefs.detectChanges();
            }
        });
    }

    setUpDropSearcTheoDoi() {
        this.bankFilterCtrlNTD.setValue('');
        this.filterBanksNTD();
        this.bankFilterCtrlNTD.valueChanges
            .pipe()
            .subscribe(() => {
                this.filterBanksNTD();
            });
    }

    protected filterBanksNTD() {
        if (!this.listNguoiTheoDoi) {
            return;
        }
        // get the search keyword
        let search = this.bankFilterCtrlNTD.value;
        if (!search) {
            this.filteredBanksNTD.next(this.listNguoiTheoDoi.slice());
            return;
        } else {
            search = search.toLowerCase();
        }
        // filter the banks
        this.filteredBanksNTD.next(
            this.listNguoiTheoDoi.filter(bank => bank.Title.toLowerCase().indexOf(search) > -1)
        );
    }


    buildForm() {
        for (let j = 0; j < this.listData.length; j++) {
            for (var i = 0; i < this.listData[j].dt_fieldnode.length; i++) {
                let index = i;
                if (this.listData[j].dt_fieldnode[i].APIData) {
                    if (this.listData[j].dt_fieldnode[index].ControlID == 5 || this.listData[j].dt_fieldnode[index].ControlID == 6 || this.listData[j].dt_fieldnode[index].ControlID == 9 || this.listData[j].dt_fieldnode[index].ControlID == 11) {
                        let LinkAPI = "";
                        if (this.listData[j].dt_fieldnode[index].DependID == null && !this.listData[j].dt_fieldnode[index].IsDepend) {
                            LinkAPI = this.listData[j].dt_fieldnode[index].APIData + this.listData[j].dt_fieldnode[index].FieldID;
                        } else {
                            LinkAPI = this.listData[j].dt_fieldnode[index].APIData;
                        }
                        if (this.listData[j].dt_fieldnode[index].ControlID == 5 || this.listData[j].dt_fieldnode[index].ControlID == 6 || this.listData[j].dt_fieldnode[index].ControlID == 9) {
                            this.dynamicFormService.getInitData(LinkAPI).subscribe(res => {
                                if (res.data.length > 0) {
                                    this.listData[j].dt_fieldnode[index].init = res.data;
                                } else {
                                    this.listData[j].dt_fieldnode[index].init = [];
                                }
                                this.changeDetectorRefs.detectChanges();
                            });
                        } else {
                            this.dynamicFormService.getInitData(LinkAPI).subscribe(res => {
                                this.listData[j].dt_fieldnode[index].init = new BehaviorSubject([]);
                                if (res.data.length > 0) {
                                    this.listData[j].dt_fieldnode[index].init.next(res.data);
                                } else {
                                    this.listData[j].dt_fieldnode[index].init.next([]);
                                }
                            });
                        }
                    }
                }
            }
        }
        this.createForm();
    }

    reset() {
        let item = {};
        this.formControls = this.fb.group(item);
        this.formControls.addControl('quyTrinh', new FormControl('', [Validators.required]));
        this.formControls.addControl('tenCongViec', new FormControl('', [Validators.required]));
        this.formControls.addControl('file', new FormControl(''));
        this.formControls.addControl('noiDung', new FormControl(''));
        this.formControls.addControl('nguoiTheoDoi', new FormControl(''));

        this.formControls.controls['tenCongViec'].markAsTouched();
        this.formControls.controls['quyTrinh'].markAsTouched();
    }

    createForm() {
        let item = {};
        this.formControls = this.fb.group(item);
        this.formControls.addControl('tenCongViec', new FormControl('', [Validators.required]));
        this.formControls.addControl('quyTrinh', new FormControl('' + this.ProcessID, [Validators.required]));
        this.formControls.addControl('file', new FormControl(''));
        this.formControls.addControl('noiDung', new FormControl(''));
        this.formControls.addControl('nguoiTheoDoi', new FormControl(''));

        this.formControls.controls['tenCongViec'].markAsTouched();
        this.formControls.controls['quyTrinh'].markAsTouched();
        for (let j = 0; j < this.listData.length; j++) {
            for (var i = 0; i < this.listData[j].dt_fieldnode.length; i++) {
                let control = this.listData[j].dt_fieldnode[i];
                if (control.Required) {
                    if (control.ControlID == 7 || control.ControlID == 8) {
                        this.formControls.addControl(control.RowID, new FormControl(false, [Validators.required]));
                        this.formControls.controls[control.RowID].markAsTouched();
                    } else {
                        this.formControls.addControl(control.RowID, new FormControl('', [Validators.required]));
                        this.formControls.controls[control.RowID].markAsTouched();
                    }
                } else {
                    this.formControls.addControl(control.RowID, new FormControl(''));
                }
            }
        }

    }

    //====================================Xử lý sự kiện change=========================
    GetValueNode(val: any, item: any) {
        let StructID = val.RowID;
        let obj = this.controls.find(x => x.RowID === item.DependID);
        let index = this.controls.findIndex(x => x.RowID === item.DependID);
        let LinkAPI = obj.APIData + StructID;
        this.dynamicFormService.getInitData(LinkAPI).subscribe(res => {
            if (res.data.length > 0) {
                this.controls[index].init = res.data;
            } else {
                this.controls[index].init = [];
            }
        });
    }

    nguoiThucHienChange(val: any, item: any) {
        item.IDnguoiThucHien = val;
    }
    //===========================================================================================

    submit() {
        const controls = this.formControls.controls;
        if (this.formControls.invalid) {
            Object.keys(controls).forEach(controlName =>
                controls[controlName].markAsTouched()
            );
            let message = 'Vui lòng nhập đầy đủ thông tin trường dữ liệu bắt buộc';
            this.layoutUtilsService.showActionNotification(message, MessageType.Read, 9999999999, true, false, 3000, 'top', 0);
            return;
        }

        const updatedegree = this.prepareCustomer();
        this.Create(updatedegree);

    }

    prepareCustomer(): any {
        const controls = this.formControls.controls;
        let Data_InfoChuyenGiaiDoanData = [];
        //=========Xử lý cho phần form động=====================
        if (this.listData.length > 0) {
            for (var j = 0; j < this.listData.length; j++) {
                let Data_Field = [];
                for (var i = 0; i < this.listData[j].dt_fieldnode.length; i++) {
                    let _field = {
                        RowID: this.listData[j].dt_fieldnode[i].RowID,
                        ControlID: this.listData[j].dt_fieldnode[i].ControlID,
                        Value: controls[this.listData[j].dt_fieldnode[i].RowID].value,
                    }
                    Data_Field.push(_field);
                }
                let _info = {
                    RowID: this.listData[j].RowID,
                    NguoiThucHienID: this.listData[j].IDnguoiThucHien,
                    FieldNode: Data_Field,
                };
                Data_InfoChuyenGiaiDoanData.push(_info);
            }
        }

        let _ChuyenGiaiDoanData = new ChuyenGiaiDoanData();
        _ChuyenGiaiDoanData.NodeID = 0;
        _ChuyenGiaiDoanData.InfoChuyenGiaiDoanData = Data_InfoChuyenGiaiDoanData;

        //============Xử lý cho phần lưu nhiều file============
        let Data_File = [];
        if (controls['file'].value.length > 0) {
            for (var i = 0; i < controls['file'].value.length; i++) {
                let _file = {
                    File: controls["file"].value[i].strBase64,
                    FileName: controls["file"].value[i].filename,
                }
                Data_File.push(_file);
            }
        }
        //============Xử lý cho phần lưu người theo dõi============
        let dataTheoDoi = [];
        if (controls['nguoiTheoDoi'].value.length > 0) {
            controls['nguoiTheoDoi'].value.map((item, index) => {
                let obj = this.listNguoiTheoDoi.find(x => +x.RowID == +item);
                let dt = {
                    ObjectID: obj.RowID,
                    ObjectType: obj.Type,
                }
                dataTheoDoi.push(dt);
            });
        }

        let _item = {
            TaskName: controls['tenCongViec'].value,
            ProcessID: this.ProcessID,
            Description: controls["noiDung"].value,
            DescriptionFileList: Data_File,
            ChuyenGiaiDoanData: _ChuyenGiaiDoanData,
            Data_Follower: dataTheoDoi,
        };

        return _item;
    }

    Create(_item: any) {
        this.disabledBtn = true;
        this.layoutUtilsService.showWaitingDiv();
        this.dynamicFormService.CreateWorkProcess(_item).subscribe(res => {
            this.disabledBtn = false;
            this.layoutUtilsService.OffWaitingDiv();
            this.changeDetectorRefs.detectChanges();
            if (res && res.status === 1) {
                const _messageType = this.translate.instant('workprocess.taocongviecthanhcong');
                this.layoutUtilsService.showActionNotification(_messageType, MessageType.Update, 4000, true, false)
                // let ProcessName = document.getElementById("tenquytrinh").textContent;
                // this.route.navigate([`/ProcessWork/Node/${res.data}/${this.ProcessID}/${ProcessName}`]);
                this.goBack(res.data);
            }
            else {
                this.layoutUtilsService.showActionNotification(res.error.message, MessageType.Read, 9999999999, true, false, 3000, 'top', 0);
            }
        });
    }

    goBack(id: any) {
        this.Close.emit(id);
    }
    //bắt sự kiện click ngoài form search thì đóng form
    @HostListener('document:click', ['$event'])
    clickout(event) {
        if (event.target.id == 'id-form-filter-container')
            this.showSearch = false;
    }

    @HostListener('document:keydown', ['$event'])
    onKeydownHandler1(event: KeyboardEvent) {
        if (this.showSearch) {
            if (event.keyCode == 13)//phím Enter
            {
                this.submit();
            }
        }
    }

    //đến ngày luôn lấy min là từ ngày
    DateChangeMin(val: any, index) {
        if (val.value != null && val.value != "") {
            let mindate = val.value.toDate();
            this.controls[index].to.min = mindate;
        }
    }

    //từ ngày luôn lấy max là đến ngày
    DateChangemMax(val: any, index) {
        if (val.value != null && val.value != "") {
            let maxdate_cur = val.value.toDate();
            this.controls[index].from.max = maxdate_cur;
        }
    }

    getFirstDay_LastDay(b_firstday) {
        var date = new Date(), y = date.getFullYear(), m = date.getMonth();
        var firstDay = new Date(y, m, 1); // ngày đầu tháng
        var lastDay = new Date(y, m + 1, 0); // ngày cuối tháng
        var curent = new Date(); // ngày hiện tại
        return b_firstday ? this.datePipe.transform(firstDay, 'dd/MM/yyyy') : this.datePipe.transform(lastDay, 'dd/MM/yyyy');
    }



    //============================================================================
    DateChangeMinCK(val: any) {
        this.d_minDate = val.value;
    }

    DateChangeMaxCK(val: any) {
        this.d_maxDate = val.value;
    }

    f_convertDate(v: any) {
        if (v != "" && v != undefined) {
            let a = new Date(v);
            return a.getFullYear() + "-" + ("0" + (a.getMonth() + 1)).slice(-2) + "-" + ("0" + (a.getDate())).slice(-2) + "T00:00:00.0000000";
        }
    }
    ChangeChucDanh(event: any, index) {
        this.controls[index].value = "," + event.value;
    }
    textNam(e: any) {
        if (!((e.keyCode > 95 && e.keyCode < 106)
            || (e.keyCode > 47 && e.keyCode < 58)
            || e.keyCode == 8)) {
            e.preventDefault();
        }
    }
}


//Dùng cho nhân bản nhiệm vụ
@Component({
    selector: 'm-dynamic-form-copy',
    templateUrl: './dynamic-form-copy.component.html',
    changeDetection: ChangeDetectionStrategy.OnPush
})

export class DynamicFormCopyComponent implements OnInit {
    constructor(public dynamicFormService: DynamicFormService,
        private fb: FormBuilder,
        private datePipe: DatePipe,
        private translate: TranslateService,
        private changeDetectorRefs: ChangeDetectorRef,
        private layoutUtilsService: LayoutUtilsService,
        private danhMucChungService: DanhMucChungService,
        private route: Router,) { }

    formControls: FormGroup;
    showSearch: boolean = false;
    controls: any[] = [];
    ID_Struct: string = '';
    listChucDanh: any[] = [];
    listChucVu: any[] = [];
    d_minDate: any;
    d_maxDate: any;
    selectedNode: BehaviorSubject<any> = new BehaviorSubject([]);
    tmp_thang: string = '';
    tmp_nam: string = '';
    search: string = '';

    listControls: any[] = [];
    @Input() ID: any;
    @Input() TypeID: any;
    @Input() ViewData: any; //Dùng cho xem chi tiết dữ liệu
    tinyMCE = {};
    @Output() Close = new EventEmitter();
    ProcessID: number;
    TasksID: number;
    listProcess: any[] = [];
    listNguoiThucHien: any[] = [];
    disabledBtn: boolean;
    //====================Người theo dõi===================
    listNguoiTheoDoi: any[] = [];
    public bankFilterCtrlNTD: FormControl = new FormControl();
    public filteredBanksNTD: ReplaySubject<any[]> = new ReplaySubject<any[]>(1);
    //===============Thông tin nhân bản======================
    TenNhiemVu: string = '';
    NoiDung: string = '';
    NguoiThucHien: any[] = [];
    listData: any[] = [];
    GiaiDoanID: number;
    //===============================================================
    ngOnInit(): void {
        this.reset();
        this.tinyMCE = tinyMCE;
        this.changeNhiemVu(this.ID);
    }

    changeNhiemVu(val: any) {
        this.TasksID = val;
        this.dynamicFormService.copyTasks(this.TasksID).subscribe(res => {
            if (res && res.status == 1) {
                this.ProcessID = res.ProcessID;
                this.TenNhiemVu = res.CongViec;
                this.NoiDung = res.Description;
                if (res.NguoiQuanLy.length > 0) {
                    let dt_follower = [];
                    res.NguoiQuanLy.map((item, index) => {
                        dt_follower.push('' + item.ObjectID);
                    });
                    this.NguoiThucHien = dt_follower;
                }
                if (res.data.length > 0) {
                    this.listData = res.data;
                    this.buildForm();
                    this.changeDetectorRefs.detectChanges();
                } else {
                    this.listData = [];
                    this.buildForm();
                    this.changeDetectorRefs.detectChanges();
                }
            } else {
                this.listData = [];
                this.buildForm();
                this.layoutUtilsService.showActionNotification(res.error.message, MessageType.Read, 9999999999, true, false, 3000, 'top', 0);
                this.changeDetectorRefs.detectChanges();
            }
        })

        this.loadData();
    }

    loadData() {
        this.danhMucChungService.GetDSNguoiApDung().subscribe(res => {
            if (res.data && res.data.length > 0) {
                this.listNguoiTheoDoi = res.data;
                this.setUpDropSearcTheoDoi();
                this.changeDetectorRefs.detectChanges();
            }
        });
    }

    setUpDropSearcTheoDoi() {
        this.bankFilterCtrlNTD.setValue('');
        this.filterBanksNTD();
        this.bankFilterCtrlNTD.valueChanges
            .pipe()
            .subscribe(() => {
                this.filterBanksNTD();
            });
    }

    protected filterBanksNTD() {
        if (!this.listNguoiTheoDoi) {
            return;
        }
        // get the search keyword
        let search = this.bankFilterCtrlNTD.value;
        if (!search) {
            this.filteredBanksNTD.next(this.listNguoiTheoDoi.slice());
            return;
        } else {
            search = search.toLowerCase();
        }
        // filter the banks
        this.filteredBanksNTD.next(
            this.listNguoiTheoDoi.filter(bank => bank.Title.toLowerCase().indexOf(search) > -1)
        );
    }


    buildForm() {
        for (let j = 0; j < this.listData.length; j++) {
            for (var i = 0; i < this.listData[j].dt_fieldnode.length; i++) {
                let index = i;
                if (this.listData[j].dt_fieldnode[i].APIData) {
                    if (this.listData[j].dt_fieldnode[index].ControlID == 5 || this.listData[j].dt_fieldnode[index].ControlID == 6 || this.listData[j].dt_fieldnode[index].ControlID == 9 || this.listData[j].dt_fieldnode[index].ControlID == 11) {
                        let LinkAPI = "";
                        if (this.listData[j].dt_fieldnode[index].DependID == null && !this.listData[j].dt_fieldnode[index].IsDepend) {
                            LinkAPI = this.listData[j].dt_fieldnode[index].APIData + this.listData[j].dt_fieldnode[index].FieldID;
                        } else {
                            LinkAPI = this.listData[j].dt_fieldnode[index].APIData;
                        }
                        if (this.listData[j].dt_fieldnode[index].ControlID == 5 || this.listData[j].dt_fieldnode[index].ControlID == 6 || this.listData[j].dt_fieldnode[index].ControlID == 9) {
                            this.dynamicFormService.getInitData(LinkAPI).subscribe(res => {
                                if (res.data.length > 0) {
                                    this.listData[j].dt_fieldnode[index].init = res.data;
                                } else {
                                    this.listData[j].dt_fieldnode[index].init = [];
                                }
                                this.changeDetectorRefs.detectChanges();
                            });
                        } else {
                            this.dynamicFormService.getInitData(LinkAPI).subscribe(res => {
                                this.listData[j].dt_fieldnode[index].init = new BehaviorSubject([]);
                                if (res.data.length > 0) {
                                    this.listData[j].dt_fieldnode[index].init.next(res.data);
                                } else {
                                    this.listData[j].dt_fieldnode[index].init.next([]);
                                }
                            });
                        }
                    }
                }
            }
        }
        this.createForm();
    }

    reset() {
        let item = {};
        this.formControls = this.fb.group(item);
        this.formControls.addControl('tenCongViec', new FormControl('', [Validators.required]));
        this.formControls.addControl('file', new FormControl(''));
        this.formControls.addControl('noiDung', new FormControl(''));
        this.formControls.addControl('nguoiTheoDoi', new FormControl(''));

        this.formControls.controls['tenCongViec'].markAsTouched();
    }

    createForm() {
        let item = {};
        this.formControls = this.fb.group(item);
        this.formControls.addControl('tenCongViec', new FormControl(this.TenNhiemVu, [Validators.required]));
        this.formControls.addControl('file', new FormControl(''));
        this.formControls.addControl('noiDung', new FormControl('' + this.NoiDung));
        this.formControls.addControl('nguoiTheoDoi', new FormControl(this.NguoiThucHien));

        this.formControls.controls['tenCongViec'].markAsTouched();
        for (let j = 0; j < this.listData.length; j++) {
            for (var i = 0; i < this.listData[j].dt_fieldnode.length; i++) {
                let control = this.listData[j].dt_fieldnode[i];
                if (control.ControlID == 6) {
                    this.formControls.addControl(control.RowID, new FormControl(control.Value ? control.Value : ''));
                } else if (control.ControlID == 7 || control.ControlID == 8) {
                    this.formControls.addControl(control.RowID, new FormControl(control.Value == "True" ? true : false));
                } else if (control.ControlID == 10 || control.ControlID == 12 || control.ControlID == 13 || control.ControlID == 14) {
                    this.formControls.addControl(control.RowID, new FormControl(control.files ? control.files : []));
                } else {
                    this.formControls.addControl(control.RowID, new FormControl(control.Value ? '' + control.Value : ''));
                }
                if (control.Required) {
                    this.formControls.controls[control.RowID].markAsTouched();
                }
            }
        }

    }

    //====================================Xử lý sự kiện change=========================
    GetValueNode(val: any, item: any) {
        let StructID = val.RowID;
        let obj = this.controls.find(x => x.RowID === item.DependID);
        let index = this.controls.findIndex(x => x.RowID === item.DependID);
        let LinkAPI = obj.APIData + StructID;
        this.dynamicFormService.getInitData(LinkAPI).subscribe(res => {
            if (res.data.length > 0) {
                this.controls[index].init = res.data;
            } else {
                this.controls[index].init = [];
            }
        });
    }

    nguoiThucHienChange(val: any, item: any) {
        item.IDnguoiThucHien = val;
    }
    //===========================================================================================

    submit() {
        const controls = this.formControls.controls;
        if (this.formControls.invalid) {
            Object.keys(controls).forEach(controlName =>
                controls[controlName].markAsTouched()
            );
            let message = 'Vui lòng nhập đầy đủ thông tin trường dữ liệu bắt buộc';
            this.layoutUtilsService.showActionNotification(message, MessageType.Read, 9999999999, true, false, 3000, 'top', 0);
            return;
        }

        const updatedegree = this.prepareCustomer();
        this.Create(updatedegree);

    }

    prepareCustomer(): any {

        const controls = this.formControls.controls;
        let Data_InfoChuyenGiaiDoanData = [];
        //=========Xử lý cho phần form động=====================
        if (this.listData.length > 0) {
            for (var j = 0; j < this.listData.length; j++) {
                let Data_Field = [];
                for (var i = 0; i < this.listData[j].dt_fieldnode.length; i++) {
                    let _field = {
                        RowID: this.listData[j].dt_fieldnode[i].RowID,
                        ControlID: this.listData[j].dt_fieldnode[i].ControlID,
                        Value: controls[this.listData[j].dt_fieldnode[i].RowID].value,
                    }
                    Data_Field.push(_field);
                }
                let _info = {
                    RowID: this.listData[j].RowID,
                    NguoiThucHienID: this.listData[j].IDnguoiThucHien,
                    FieldNode: Data_Field,
                };
                Data_InfoChuyenGiaiDoanData.push(_info);
            }
        }

        let _ChuyenGiaiDoanData = new ChuyenGiaiDoanData();
        _ChuyenGiaiDoanData.NodeID = 0;
        _ChuyenGiaiDoanData.InfoChuyenGiaiDoanData = Data_InfoChuyenGiaiDoanData;

        //============Xử lý cho phần lưu nhiều file============
        let Data_File = [];
        if (controls['file'].value.length > 0) {
            for (var i = 0; i < controls['file'].value.length; i++) {
                let _file = {
                    File: controls["file"].value[i].strBase64,
                    FileName: controls["file"].value[i].filename,
                }
                Data_File.push(_file);
            }
        }
        //============Xử lý cho phần lưu người theo dõi============
        let dataTheoDoi = [];
        if (controls['nguoiTheoDoi'].value.length > 0) {
            controls['nguoiTheoDoi'].value.map((item, index) => {
                let obj = this.listNguoiTheoDoi.find(x => +x.RowID == +item);
                let dt = {
                    ObjectID: obj.RowID,
                    ObjectType: obj.Type,
                }
                dataTheoDoi.push(dt);
            });
        }

        let _item = {
            TaskName: controls['tenCongViec'].value,
            ProcessID: this.ProcessID,
            Description: controls["noiDung"].value,
            DescriptionFileList: Data_File,
            ChuyenGiaiDoanData: _ChuyenGiaiDoanData,
            Data_Follower: dataTheoDoi,
        };

        return _item;
    }

    Create(_item: any) {
        this.disabledBtn = true;
        this.layoutUtilsService.showWaitingDiv();
        this.dynamicFormService.CreateWorkProcess(_item).subscribe(res => {
            this.disabledBtn = false;
            this.layoutUtilsService.OffWaitingDiv();
            this.changeDetectorRefs.detectChanges();
            if (res && res.status === 1) {
                const _messageType = this.translate.instant('workprocess.taocongviecthanhcong');
                this.layoutUtilsService.showActionNotification(_messageType, MessageType.Update, 4000, true, false)
                this.goBack(res.data);
            }
            else {
                this.layoutUtilsService.showActionNotification(res.error.message, MessageType.Read, 9999999999, true, false, 3000, 'top', 0);
            }
        });
    }

    goBack(id: any) {
        this.Close.emit(id);
    }
    //bắt sự kiện click ngoài form search thì đóng form
    @HostListener('document:click', ['$event'])
    clickout(event) {
        if (event.target.id == 'id-form-filter-container')
            this.showSearch = false;
    }

    @HostListener('document:keydown', ['$event'])
    onKeydownHandler1(event: KeyboardEvent) {
        if (this.showSearch) {
            if (event.keyCode == 13)//phím Enter
            {
                this.submit();
            }
        }
    }

    //đến ngày luôn lấy min là từ ngày
    DateChangeMin(val: any, index) {
        if (val.value != null && val.value != "") {
            let mindate = val.value.toDate();
            this.controls[index].to.min = mindate;
        }
    }

    //từ ngày luôn lấy max là đến ngày
    DateChangemMax(val: any, index) {
        if (val.value != null && val.value != "") {
            let maxdate_cur = val.value.toDate();
            this.controls[index].from.max = maxdate_cur;
        }
    }

    getFirstDay_LastDay(b_firstday) {
        var date = new Date(), y = date.getFullYear(), m = date.getMonth();
        var firstDay = new Date(y, m, 1); // ngày đầu tháng
        var lastDay = new Date(y, m + 1, 0); // ngày cuối tháng
        var curent = new Date(); // ngày hiện tại
        return b_firstday ? this.datePipe.transform(firstDay, 'dd/MM/yyyy') : this.datePipe.transform(lastDay, 'dd/MM/yyyy');
    }



    //============================================================================
    DateChangeMinCK(val: any) {
        this.d_minDate = val.value;
    }

    DateChangeMaxCK(val: any) {
        this.d_maxDate = val.value;
    }

    f_convertDate(v: any) {
        if (v != "" && v != undefined) {
            let a = new Date(v);
            return a.getFullYear() + "-" + ("0" + (a.getMonth() + 1)).slice(-2) + "-" + ("0" + (a.getDate())).slice(-2) + "T00:00:00.0000000";
        }
    }
    ChangeChucDanh(event: any, index) {
        this.controls[index].value = "," + event.value;
    }
    textNam(e: any) {
        if (!((e.keyCode > 95 && e.keyCode < 106)
            || (e.keyCode > 47 && e.keyCode < 58)
            || e.keyCode == 8)) {
            e.preventDefault();
        }
    }
}

