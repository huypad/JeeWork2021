import { LayoutUtilsService } from './../../../../../_metronic/jeework_old/core/utils/layout-utils.service';
import { ProjectsTeamService } from './../../Services/department-and-project.service';
 import { WeWorkService } from "./../../../services/wework.services";
import { DialogData } from "./../../../report/report-tab-dashboard/report-tab-dashboard.component";
import { MatDialogRef, MAT_DIALOG_DATA } from "@angular/material/dialog";
import { Component, OnInit, Inject } from "@angular/core";
import { OptionsModel } from "../drap-drop-item.model";

@Component({
	selector: "kt-add-new-fields",
	templateUrl: "./add-new-fields.component.html",
	styleUrls: ["./add-new-fields.component.scss"],
})
export class AddNewFieldsComponent implements OnInit {
	listField: any = [];
	listOptions: any = [];
	TypeID = 0;
	data: any = [];
	public defaultColors: string[] = [
		"rgb(187, 181, 181)",
		"rgb(29, 126, 236)",
		"rgb(250, 162, 140)",
		"rgb(14, 201, 204)",
		"rgb(11, 165, 11)",
		"rgb(123, 58, 245)",
		"rgb(238, 177, 8)",
		"rgb(236, 100, 27)",
		"rgb(124, 212, 8)",
		"rgb(240, 56, 102)",
		"rgb(255, 0, 0)",
		"rgb(0, 0, 0)",
		"rgb(255, 0, 255)",
	];
	constructor(
		public weworkService: WeWorkService,
		public dialogRef: MatDialogRef<AddNewFieldsComponent>,
		public LayoutUtilsService: LayoutUtilsService,
		public _service: ProjectsTeamService,
		@Inject(MAT_DIALOG_DATA) public _data: DialogData
	) {}

	ngOnInit() {
		this.data = this._data;
		this.weworkService.GetNewField().subscribe((res) => {
			if (res && res.status == 1) {
				this.listField = res.data;
				this.selectedCol(this.data);
			}
		});

		if (this.data.id_row > 0) {
			this._service
				.Detail_column_new_field(this.data.id_row)
				.subscribe((res) => {
					if (res && res.status == 1) {
						this.listOptions = res.data.options;
					} else {
						this.LayoutUtilsService.showError(res.error.message);
					}
				});
		}else{
			this.addRow();
		}
	}
	onNoClick(): void {
		// this.data.startDate = this.start_temp;
		// this.data.endDate = this.end_temp;
		this.dialogRef.close();
	}
	getTitle() {
		if (this.data.id_row > 0) {
			return "Chỉnh sửa dữ liệu";
		}
		return "Thêm cột dữ liệu";
	}
	selectedCol(data) {
		var x = this.listField.find((x) => x.fieldname == data.columnname);
		if (x) {
			this.data.TypeID = this.TypeID = x.typeid;
		}
	}

	addRow() {
		const option = new OptionsModel();
		option.Color = this.RandomColor();
		if(this.data.id_row >0){
			option.FieldID = this.data.id_row;
		}
		this.listOptions.push(option);
	}
	randomInteger(min, max) {
		return Math.floor(Math.random() * (max - min + 1)) + min;
	}
	RandomColor() {
		var idrandom = this.randomInteger(0,this.defaultColors.length-1);
		return this.defaultColors[idrandom];
	}
	Submit() {

		var x = this.listOptions.filter((x) => x.Value != "");
		this.listOptions.forEach((element) => {
			element.TypeID = this.TypeID;
			element.ID_project_team = this.data.id_project_team;
		});
		this.data.Options = x;
		if (
			this.data.columnname == "dropdown" ||
			this.data.columnname == "labels"
		) {
			if (x.length == 0) {
				this.LayoutUtilsService.showError("Nhập Options");
				return;
			}
		}

		if (this.data.id_row > 0) {
			this.UpdateCol(this.data);
		} else {
			this.InsertCol(this.data);
		}
	}

	InsertCol(_item) {
		this._service.UpdateColumnWork(this.data).subscribe((res) => {
			if (res && res.status == 1) {
				// this.LoadData();
				this.dialogRef.close(this.data);
			}
			else{
				this.LayoutUtilsService.showError(res.error.message);
			}
		});
	}

	UpdateCol(_item) {
		this._service.UpdateColumnNewField(this.data).subscribe((res) => {
			if (res && res.status == 1) {
				// this.LoadData();
				this.dialogRef.close(this.data);
			}else{
				this.LayoutUtilsService.showError(res.error.message);
			}
		});
	}
}
