import { TokenStorage } from './../../../../_metronic/jeework_old/core/auth/_services/token-storage.service';
import { LayoutUtilsService } from './../../../../_metronic/jeework_old/core/utils/layout-utils.service';
import { QueryParamsModelNew } from './../../../../_metronic/jeework_old/core/models/query-models/query-params.model';
import { environment } from 'src/environments/environment';
import { ProjectsTeamService } from './../../projects-team/Services/department-and-project.service';
import { ReportProjectService } from './../report-project.service';
import { TranslateService } from '@ngx-translate/core';
import { ChartModal } from './../modal/report.modal';
import { ReportService } from './../report.service';
import { MatDialog } from '@angular/material/dialog';
import { ChangeDetectorRef, Component, OnInit, Type } from '@angular/core';
import { DialogSelectdayComponent } from '../dialog-selectday/dialog-selectday.component';
import { Color } from 'ng2-charts';
import * as Highcharts from 'highcharts';

declare var require: any;
const More = require('highcharts/highcharts-more');
More(Highcharts);

import Histogram from 'highcharts/modules/histogram-bellcurve';
import { WeWorkService } from '../../services/wework.services';
import { ActivatedRoute } from '@angular/router';
Histogram(Highcharts);

const Wordcloud = require('highcharts/modules/wordcloud');
Wordcloud(Highcharts);

@Component({
  selector: 'kt-report-by-project',
  templateUrl: './report-by-project.component.html',
  styleUrls: ['./report-by-project.component.scss']
})
export class ReportByProjectComponent implements OnInit {

  ID_department: number = 0;
  ID_ProjectTeam: number = 0;
  ProjectTeam: any = [];
  public filterCVC: any = [];
  public column_sort: any = [];
  public status_dynamic: any = [];
  constructor(
    public dialog: MatDialog,
    public reportService: ReportProjectService,
    public ProjectsTeamService: ProjectsTeamService,
    private detectChange: ChangeDetectorRef,
    private translate: TranslateService,
    private layoutUtilsService: LayoutUtilsService,
    private weworkService: WeWorkService,
    private activatedRoute: ActivatedRoute,
    private tokenStorage: TokenStorage,
    // private TongHopHieuQuaTuyenDungService:TongHopHieuQuaTuyenDungService
  ) {
    this.filter_dept = {
      title: this.translate.instant('filter.tatcaphongban'),
      id_row: ''
    }

    var today = new Date();
    this.selectedDate = {
      endDate: new Date(today.setDate(today.getDate() + 1)),
      startDate: new Date(today.setDate(1)),
    }
    this.filterCVC = this._filterCV[0];
    this.trangthai = {
      statusname: this.translate.instant('filter.tatcatrangthai'),
      id_row: '',
    };
    this.column_sort = this.sortField[0];
  }

  DontChange = false
  // filter header
  selectedDate = {
    startDate: new Date('09/01/2020'),
    endDate: new Date('09/30/2020'),
  }
  trangthai: any;
  ngOnInit() {
    this.activatedRoute.params.subscribe(params => {
      if (params.id) {
        this.ID_department = +params.id;
        this.ID_ProjectTeam = +params.id;
        this.DontChange = true;
        this.filter_dept.id_row = this.ID_department.toString();
        // this.LoadData();
      }
      else {
      }
    });
    this.LoadData();
  }

  Filter(item) {
    if (item.loai == 'displayChild') {
      this.filterCVC = item
    }
    if (item.loai == 'trangthai') {
      this.trangthai = item;
    }
    this.LoadData();
  }

  SelectedField(item) {
    this.column_sort = item;
    this.LoadData();
  }
  LoadDetailProject() {
    // ID_ProjectTeam
    this.ProjectsTeamService.DeptDetail(this.ID_ProjectTeam).subscribe(res => {
      if (res && res.status == 1) {
        this.ProjectTeam = res.data;
      }
    });
  }

  filterConfiguration(): any {
    const filter: any = {};
    // filter.TuNgay = this.selectedDate.startDate;
    // filter.DenNgay = this.selectedDate.endDate;
    filter.TuNgay = (this.f_convertDate(this.selectedDate.startDate)).toString();
    filter.DenNgay = (this.f_convertDate(this.selectedDate.endDate)).toString();
    filter.id_department = this.filter_dept.id_row;
    filter.id_projectteam = this.ID_ProjectTeam;
    filter.collect_by = this.column_sort.value;
    filter.displayChild = this.filterCVC.value;
    filter.status = this.trangthai.id_row;
    return filter;
  }

  _filterCV = [
    {
      title: this.translate.instant('filter.khongtinhcongvieccon'),
      value: '0',
      loai: 'displayChild'
    },
    {
      title: this.translate.instant('filter.congviecvacongvieccon'),
      value: '1',
      loai: 'displayChild'
    },
  ]
  sortField = [
    {
      title: this.translate.instant('day.theongaytao'),
      value: 'CreatedDate',
    },
    {
      title: this.translate.instant('day.theothoihan'),
      value: 'Deadline',
    },
    {
      title: this.translate.instant('day.theongaybatdau'),
      value: 'StartDate',
    },
  ]
  _filterTT = [
    {
      title: this.translate.instant('filter.tatcatrangthai'),
      value: 'status',
      id_row: '',
      loai: 'trangthai'
    },
    {
      title: this.translate.instant('filter.congviecdangthuchien'),
      value: 'status',
      id_row: '1',
      loai: 'trangthai'
    },
    {
      title: this.translate.instant('filter.congviecdahoanthanh'),
      value: 'status',
      id_row: '2',
      loai: 'trangthai'
    },
  ]

  NameDept = (id) => {
    var title = this.translate.instant('filter.tatcaphongban');
    if (this.list_department) {
      this.list_department.forEach(res => {
        if (res.id_row == id) {
          title = res.title
        }
      })
    }
    return title
  }

  list_department: any[];
  filter_dept: any = {};

  LoadData() {
    this.layoutUtilsService.showWaitingDiv();
    this.LoadDetailProject();
    this.getChart1();
    this.getChart2();
    this.getChart3();
    this.getChart4();
    this.getChartkhongdunghan();
    this.getOverview();
    this.getChartPhanbocongviecDepartment();
    // this.GetMuctieuDepartment();
    this.DataTagClouds();
    this.ReportByStaff();
    this.getStaffMostTask();
    this.getStaffExcellent();
    this.getStaffMostLate();
    this.getReportByMilestone();
    // this.getReportByDepartment();
    this.getReportByProjectTeam();
    // this.getReportToDepartments();
    this.getCacConSoThongKe();
    this.getEisenhower();
    this.LoadDatafilter();
    setTimeout(() => {
      this.layoutUtilsService.OffWaitingDiv();
    }, 500);
  }

  LoadDatafilter() {
    // this.weworkService.lite_department().subscribe(res => {
    //   if (res && res.status === 1) {
    //     this.list_department = res.data;
    //     this.list_department.unshift({
    //       title: this.translate.instant('filter.tatcaphongban'),
    //       id_row: ''
    //     })
    //   };
    //   this.detectChange.detectChanges();
    // });

    this.weworkService.ListStatusDynamic(this.ID_ProjectTeam).subscribe(res => {
      if (res && res.status === 1) {
        this.status_dynamic = res.data;
        this.status_dynamic.unshift(
          {
            statusname: this.translate.instant('filter.tatcatrangthai'),
            id_row: '',
          }
        )
        this.detectChange.detectChanges();
      };
    });
  }

  selected_Dept(item) {
    this.filter_dept = item;
    this.LoadData();
    //note
  }


  //Load overview 
  ListOverview = []
  getOverview() {
    const queryParams = new QueryParamsModelNew(
      this.filterConfiguration(),
    );
    //queryParams.sortField = this.column_sort.value;
    this.reportService.GetOverview(queryParams).subscribe(data => {
      this.ListOverview = [
        {
          title: this.translate.instant('report.duan'),
          tong: 0,
          text1: this.translate.instant('report.duannoibo'),
          text2: this.translate.instant('report.duanbenngoai'),
          isShow: false,
        },
        {
          title: this.translate.instant('report.phongban'),
          tong: 0,
          text1: this.translate.instant('report.phongbannoibo'),
          text2: this.translate.instant('report.phongbanbenngoai'),
          isShow: false,
        },
        {
          title: this.translate.instant('report.congviec'),
          tong: 0,
          text1: this.translate.instant('report.hoanthanh'),
          text2: this.translate.instant('report.dangthuchien'),
          isShow: true,
        },
        {
          title: this.translate.instant('report.muctieu'),
          tong: 0,
          text1: this.translate.instant('report.hoanthanh'),
          text2: this.translate.instant('report.dangthuchien'),
          isShow: true,
        },
        {
          title: this.translate.instant('report.thanhvien'),
          tong: 0,
          text1: this.translate.instant('report.nhanvien'),
          text2: this.translate.instant('report.khach'),
          isShow: true,
        },
      ]
      if (data && data.status == 1) {
        let arrdata = (data.data);
        //Dự án
        this.ListOverview[0].tong = arrdata['DuAn'].Tong;
        this.ListOverview[0].text1 = arrdata['DuAn'].DuAnNoiBo + ' ' + this.ListOverview[0].text1;
        this.ListOverview[0].text2 = arrdata['DuAn'].DuAnKH + ' ' + this.ListOverview[0].text2;
        //phòng ban
        this.ListOverview[1].tong = arrdata['PhongBan'].Tong;
        this.ListOverview[1].text1 = arrdata['PhongBan'].DuAnNoiBo + ' ' + this.ListOverview[1].text1;
        this.ListOverview[1].text2 = arrdata['PhongBan'].DuAnKH + ' ' + this.ListOverview[1].text2;
        //Công việc
        this.ListOverview[2].tong = arrdata['CongViec'].Tong;
        this.ListOverview[2].text1 = arrdata['CongViec'].HoanThanh + ' ' + this.ListOverview[2].text1;
        this.ListOverview[2].text2 = arrdata['CongViec'].DangThucHien + ' ' + this.ListOverview[2].text2;
        //Mục tiêu
        this.ListOverview[3].tong = arrdata['MucTieu'].Tong;
        this.ListOverview[3].text1 = arrdata['MucTieu'].HoanThanh + ' ' + this.ListOverview[3].text1;
        this.ListOverview[3].text2 = arrdata['MucTieu'].DangThucHien + ' ' + this.ListOverview[3].text2;
        //Thành viên
        this.ListOverview[4].tong = arrdata['ThanhVien'].Tong;
        this.ListOverview[4].text1 = arrdata['ThanhVien'].NhanVien + ' ' + this.ListOverview[4].text1;
        this.ListOverview[4].text2 = arrdata['ThanhVien'].Khach + ' ' + this.ListOverview[4].text2;
      }


    })
  }

  //Báo cáo trạng thái công việc
  listColor = ['#40FF00', '#F7FE2E', 'rgb(124, 181, 236)', '#FE2E2E']
  public pieChartData = [];
  public pieChartLabel: string[] = [
    this.translate.instant('filter.hoanthanh'),
    this.translate.instant('filter.hoanthanhmuon'),
    this.translate.instant('filter.dangthuchien'),
    this.translate.instant('filter.quhan'),
    this.translate.instant('filter.chuacocongviec'),
  ];
  public pieChartOptions = { cutoutPercentage: 80 };
  public pieChartLegend = false;
  public pieChartColor = [{
    backgroundColor: this.listColor,
  }];
  public pieChartType = 'pie';
  public Tongcongviec = 0;

  Trangthai: any[];

  getChart1() {
    const queryParams = new QueryParamsModelNew(
      this.filterConfiguration(),
    );
    //queryParams.sortField = this.column_sort.value;
    this.reportService.GetTrangthai(queryParams).subscribe(data => {
      if (data && data.status == 1) {
        this.Trangthai = data.data['TrangThaiCongViec'];
        for (var i = 0; i < this.Trangthai.length; i++) {
          this.Trangthai[i].color = this.listColor[i];
        }
        this.pieChartData = this.ElementObjectToArr(this.Trangthai, 'value');
        this.Tongcongviec = 0;
        for (let i of this.pieChartData) {
          this.Tongcongviec += i;
        }
        if (this.Tongcongviec == 0) {
          this.pieChartData.push(1);
          this.listColor.push('#eee');
        }
      }
      this.detectChange.detectChanges();
    });
  }

  getStrNowork() {
    return this.translate.instant('filter.chuacocongviec');
  }

  //Quá trình hoàn thành theo ngày

  listColor2 = ['rgb(72, 133, 108)', 'rgb(245, 78, 59)', 'rgb(20, 204, 63)']
  public chart2Ready = false;
  public chartData2 = [

  ];
  public chartLabel2: string[] = [''];
  public chartOptions2 = {
    responsive: true,
    scales: {
      yAxes: [{
        ticks: {
          beginAtZero: true,
          min: 0,
        }
      }]
    }
  };
  public chartLegend2 = false;
  public chartColor2 = [
    { backgroundColor: this.listColor2[0], fill: false, borderColor: this.listColor2[0], },
    { backgroundColor: this.listColor2[1] },
    { backgroundColor: this.listColor2[2] },
  ];
  public chartType2 = 'bar';
  ListLabel = [this.translate.instant('filter.tatca'), this.translate.instant('filter.quahan'), this.translate.instant('filter.hoanthanh')];
  DataChart2 = [];
  getChart2() {
    this.chartData2 = [];
    this.chartLabel2 = [];
    const queryParams = new QueryParamsModelNew(
      this.filterConfiguration(),
    );
    //queryParams.sortField = this.column_sort.value;
    this.reportService.GetQuaTrinh(queryParams).subscribe(data => {
      if (data && data.status == 1) {
        this.DataChart2 = data.data;
        this.chartLabel2 = this.ElementObjectToArr(this.DataChart2, 'tencot');
        this.chartData2.push(
          { "data": this.ElementObjectToArr(this.DataChart2, 'tatca'), "label": this.translate.instant('filter.tatca'), "type": 'line' },
          { "data": this.ElementObjectToArr(this.DataChart2, 'quahan'), "label": this.translate.instant('filter.quahan'), "stack": 'a' },
          { "data": this.ElementObjectToArr(this.DataChart2, 'hoanthanh'), "label": this.translate.instant('filter.hoanthanh'), "stack": 'a' }
        );
        this.chart2Ready = true
      }
      this.detectChange.detectChanges();
    });
  }

  getInfoCVDTH(congviec, tongconviec) {
    var text = 'report.congviecdangthuchien';
    return congviec + "/" + tongconviec + " " + this.translate.instant(text);
  }
  getInfoCVHT(congviec, tongconviec) {
    var text = 'report.congviecdahoanthanh';
    return congviec + "/" + tongconviec + " " + this.translate.instant(text);
  }
  getInfoquahan(quahan, ht_quahan) {
    return quahan + " " + this.translate.instant('filter.quahan') + '· ' + ht_quahan + " " + this.translate.instant('filter.hoanthanhmuon');
  }

  // Tổng hợp theo tuần
  titleChart3 = [
    {
      ten: this.translate.instant('filter.tatca'),
      mau: 'rgb(124, 181, 236)',
    },
    {
      ten: this.translate.instant('filter.dangthuchien'),
      mau: 'rgb(247, 224, 21)',
    },
    {
      ten: this.translate.instant('filter.hoanthanh'),
      mau: 'rgb(20, 204, 63)',
    },
    {
      ten: this.translate.instant('filter.quahan'),
      mau: 'rgb(245, 78, 59)',
    },
  ]
  public chartData3 = [];
  public chartData3Ready = false;
  public chartLabel3: string[] = [];
  public chartOptions3 = {
    responsive: true,
    scales: {
      yAxes: [
        {
          ticks: {
            beginAtZero: true
          }
        }
      ]
    }
  };
  public chartLegend3 = false;
  public chartColor3 = [
    { backgroundColor: this.titleChart3[0].mau },
    { backgroundColor: this.titleChart3[1].mau },
    { backgroundColor: this.titleChart3[2].mau },
    { backgroundColor: this.titleChart3[3].mau },
  ];
  public chartType3 = 'bar';


  Data3 = [];

  getChart3() {
    this.chartData3 = [];
    const queryParams = new QueryParamsModelNew(
      this.filterConfiguration(),
    );
    //queryParams.sortField = this.column_sort.value;
    this.reportService.GetTongHopTheoTuan(queryParams).subscribe(data => {
      if (data && data.status == 1) {
        this.Data3 = data.data;
        this.chartLabel3 = this.ElementObjectToArr(this.Data3, 'tencot');
        this.chartData3.push(
          { data: this.ElementObjectToArr(this.Data3, 'tatca'), label: this.titleChart3[0].ten },
          { data: this.ElementObjectToArr(this.Data3, 'dangthuchien'), label: this.titleChart3[1].ten },
          { data: this.ElementObjectToArr(this.Data3, 'hoanthanh'), label: this.titleChart3[2].ten },
          { data: this.ElementObjectToArr(this.Data3, 'quahan'), label: this.titleChart3[3].ten },
        );
        this.chartData3Ready = true;
      }
      this.detectChange.detectChanges();
    });
  }

  // chart 4 Tong hop theo du an
  listColorChart4 = ['#40FF00', '#F7FE2E', 'rgb(124, 181, 236)', '#FE2E2E', 'gray'];
  Data4: any;
  chart4Ready = false;
  TongDuAn = 0;
  titleChart4: string[] = [];
  chartData4 = [];
  chartLabel4: string[] = [''];
  chartOptions4 = { cutoutPercentage: 80 }
  chartLegend4 = false;
  chartColor4 = [{
    backgroundColor: this.listColorChart4,
  }];;
  chartType4 = 'pie';
  getChart4() {
    const queryParams = new QueryParamsModelNew(
      this.filterConfiguration(),
    );
    //queryParams.sortField = this.column_sort.value;
    this.reportService.GetTongHopTheoDuan(queryParams).subscribe(data => {
      if (data && data.status == 1) {
        this.Data4 = data.data;
        this.Data4.color = this.listColorChart4;
        this.chartData4 = this.Data4.datasets;
        this.chartLabel4 = this.Data4.label;
        this.TongDuAn = this.chartData4.reduce((a, b) => a + b);
        if (this.TongDuAn == 0) {
          this.chartData4.push(1);
          this.listColorChart4.push('#eee');
          this.Data4.label.push(this.translate.instant('filter.chuacocongviec'))
        }
        this.chart4Ready = true;
      }
      this.detectChange.detectChanges();
    })
  }

  //chart Công việc không đúng hạn
  tasknodeadline = 0;
  dataKhongdunghan = []
  listpieColor = ['red', '#f7e015']
  getChartkhongdunghan() {
    const queryParams = new QueryParamsModelNew(
      this.filterConfiguration(),
    );
    //queryParams.sortField = this.column_sort.value;
    this.reportService.GetTrangthai(queryParams).subscribe(data => {
      this.dataKhongdunghan = []
      if (data && data.status == 1) {
        this.dataKhongdunghan.push(data.data['KhongDungHan'].pie1);
        this.dataKhongdunghan.push(data.data['KhongDungHan'].pie2);
        this.tasknodeadline = data.data['KhongDungHan'].khong;
        for (let i = 0; i < this.dataKhongdunghan.length; i++) {
          this.dataKhongdunghan[i].push(this.setPieColor(this.listpieColor[i]));
        }
      }
      this.detectChange.detectChanges();
    })
  }

  pieKhongdunghan = {
    legend: false,
    labels: ['', ''],
    chartType: 'pie',
    options: {
      cutoutPercentage: 70,
      tooltips: { enabled: false },
      hover: { mode: null },
    },
    color: [],
  }

  setPieColor(value) {
    return [{
      backgroundColor: [value, '#eee'],
    }]
  }

  //chart phân bổ công việc theo department
  dataPhanbocongviec = [];
  listColorPhanbocongviec = [];
  // chartPhanbocongviecDepartment = {
  //   label: [],
  //   datasets: [
  //     ],
  //   legend: false,
  //   options: {
  //     responsive: true
  //   },
  //   color: [],
  //   titleLegend:[]
  // }
  public chartPhanbocongviecDepartment = new ChartModal();
  chartDeptReady = false;
  chartPhanbocongviecDepartmentData: any[] = [];
  chartPhanbocongviecDepartmentLabel: string[] = [];
  chartPhanbocongviecDepartmentColor = [];
  keyObject: string[] = [];
  getTitle(value: string) {
    switch (value) {
      case 'tatca': return this.translate.instant('filter.tatca');
      case 'quahan': return this.translate.instant('filter.quahan');
      case 'dangthuchien': return this.translate.instant('filter.dangthuchien');
      case 'dangdanhgia': return this.translate.instant('filter.dangdanhgia');
      case 'hoanthanh': return this.translate.instant('filter.hoanthanh');
      default: return '';
    }
  }
  getColor(value: string) {
    switch (value) {
      case 'tatca': return 'blue';
      case 'quahan': return 'red';
      case 'dangthuchien': return 'green';
      case 'dangdanhgia': return 'yellow';
      case 'hoanthanh': return 'violet';
      default: return '';
    }
  }

  getChartPhanbocongviecDepartment() {
    this.chartPhanbocongviecDepartment = new ChartModal();
    this.chartPhanbocongviecDepartment.type = "bar";
    const queryParams = new QueryParamsModelNew(
      this.filterConfiguration(),
    );
    //queryParams.sortField = this.column_sort.value;
    this.reportService.GetPhanbocongviecDepartment(queryParams).subscribe(data => {
      if (data.data && data.status == 1) {
        var getData = data.data;
        if (getData[0]) {
          this.keyObject = (Object.keys(getData[0].data));
        }
        var dt1: any = {};
        for (let i of getData) {
          this.chartPhanbocongviecDepartment.label.push(i.title);
          // res2.push(i.data)
          for (let index of this.keyObject) {
            if (dt1[index] == undefined) {
              dt1[index] = [];
            }
            dt1[index].push(i.data[index])
          }
        }
        for (let index of this.keyObject) {
          this.chartPhanbocongviecDepartment.datasets.push(
            { data: dt1[index], label: this.getTitle(index), }
          )
          this.chartPhanbocongviecDepartment.color.push(
            { backgroundColor: this.getColor(index) }
          )
          this.chartPhanbocongviecDepartment.titleLegend.push(
            {
              title: this.getTitle(index),
              color: this.getColor(index)
            }
          )
        }

        this.chartPhanbocongviecDepartmentData = this.chartPhanbocongviecDepartment.datasets;
        this.chartPhanbocongviecDepartmentLabel = this.chartPhanbocongviecDepartment.label;
        this.chartDeptReady = true;
        // this.chartPhanbocongviecDepartmentColor = this.chartPhanbocongviecDepartment.color;
      }
      this.detectChange.detectChanges();
    })
  }

  // mục tiêu department
  MuctieuDepartment: any;
  listColorDepartment = ['#40FF00', '#F7FE2E', 'rgb(124, 181, 236)', '#FE2E2E', 'gray'];
  GetMuctieuDepartment() {
    const queryParams = new QueryParamsModelNew(
      this.filterConfiguration(),
    );
    //queryParams.sortField = this.column_sort.value;
    this.reportService.GetMuctieuDepartment(queryParams).subscribe(data => {
      if (data && data.status == 1) {
        let getData = data.data;
        this.MuctieuDepartment = getData;
        this.MuctieuDepartment.color = [{
          backgroundColor: this.listColorDepartment,
        }]
      }
      this.detectChange.detectChanges();
    })

  }


  f_convertDate(p_Val: any) {
    let a = p_Val === "" ? new Date() : new Date(p_Val);
    return ("0" + (a.getDate())).slice(-2) + "/" + ("0" + (a.getMonth() + 1)).slice(-2) + "/" + a.getFullYear();
  }




  openDialog(): void {
    const dialogRef = this.dialog.open(DialogSelectdayComponent, {
      width: '500px',
      data: this.selectedDate
    });

    dialogRef.afterClosed().subscribe(result => {
      if (result != undefined) {
        this.selectedDate.startDate = new Date(result.startDate)
        this.selectedDate.endDate = new Date(result.endDate)
        // this.filter.TuNgay = (this.f_convertDate(this.selectedDate.startDate)).toString();
        // this.filter.DenNgay = (this.f_convertDate(this.selectedDate.endDate)).toString();
        this.LoadData();
      }
    });
  }

  ListItemGroup = [1, 2, 3, 4, 5, 6, 7, 8]

  // trạng thái công việc
  Congviec = [
    {
      status: this.translate.instant('filter.hoanthanh'),
      value: 72,
      color: 'rgb(20, 204, 63)'

    },
    {
      status: this.translate.instant('filter.hoanthanhmuon'),
      value: 34,
      color: 'rgb(247, 224, 21)'
    },
    {
      status: this.translate.instant('filter.dangthuchien'),
      value: 8,
      color: 'blue'
    },
    {
      status: this.translate.instant('filter.quahan'),
      value: 297,
      color: 'rgb(245, 78, 59)'
    },
  ];

  ElementObjectToArr(arr, eml) {
    var newArr = [];
    for (let item of arr) {
      newArr.push(item[eml])
    }
    return newArr;
  }

  Random(min, max) {
    return Math.floor(Math.random() * (max - min)) + min;
  }

  private options: any = {
    legend: { position: 'bottom' },
    responsive: true,
    maintainAspectRatio: false,
  }

  Tagoptions: any;

  DataTagClouds() {
    const queryParams = new QueryParamsModelNew(
      this.filterConfiguration(),
    );
    //queryParams.sortField = this.column_sort.value;
    this.reportService.TagClouds(queryParams).subscribe(data => {
      if (data && data.status == 1) {

        var arr: any[] = [];
        for (let a of data.data) {
          arr.push(
            {
              name: a.name,
              weight: a.weight
            }
          )
        }
        this.DataHighChart(arr);
        Highcharts.chart('container', this.Tagoptions);
      }
      this.detectChange.detectChanges();
    })
  }

  DataHighChart(data) {
    this.Tagoptions = {
      series: [{
        type: 'wordcloud',
        data: data,
        name: this.translate.instant('filter.congviec'),
        rotation: {
          from: -45,
          to: 45,
          orientations: 5
        },
        minFontSize: 10,
        maxFontSize: 25,
        style: {
          fontWeight: 500,
        },
      }],
      title: {
        text: ''
      }
    };
  }

  Staff: any[] = [];
  // Report by staff
  colorCrossbar = ['rgb(20, 204, 63)', 'blue', '#ff9900', 'green', 'violet'];
  ReportByStaff() {
    const queryParams = new QueryParamsModelNew(
      this.filterConfiguration(),
    );
    //queryParams.sortField = this.column_sort.value;
    this.reportService.ReportByStaff(queryParams).subscribe(data => {
      if (data && data.status == 1) {
        this.Staff = data.data;
        for (let i of this.Staff) {
          i.datasets = [
            i.hoanthanh,
            i.ht_quahan,
            i.quahan,
            i.danglam,
            i.dangdanhgia,
          ]
        }
      }
      this.detectChange.detectChanges();
    })
  }
  ExportExcel(filename: string) {
    var linkdownload = environment.ApiRoots + `/wework/report/ExportExcel?FileName=` + filename;
    window.open(linkdownload);
  }

  // staff xuat sac nhat  
  StaffExcellent: any = [];
  getStaffExcellent() {
    var filter: any = this.filterConfiguration();
    filter.type = 'excellent';
    const queryParams = new QueryParamsModelNew(
      filter,
    );
    //queryParams.sortField = this.column_sort.value;
    this.reportService.ReportByConditions(queryParams).subscribe(data => {
      if (data && data.status == 1) {
        this.StaffExcellent = data.data;
      }
      this.detectChange.detectChanges();
    })
  }
  // staff con nhieu viec nhat
  StaffMostTask: any[] = [];
  getStaffMostTask() {
    var filter: any = this.filterConfiguration();
    filter.type = 'most';
    const queryParams = new QueryParamsModelNew(
      filter,
    );
    //queryParams.sortField = this.column_sort.value;
    this.reportService.ReportByConditions(queryParams).subscribe(data => {
      if (data && data.status == 1) {
        this.StaffMostTask = data.data;
      }
      this.detectChange.detectChanges();
    })
  }
  // staff tre viec nhieu nhat
  StaffMostLate: any[] = [];
  getStaffMostLate() {
    var filter: any = this.filterConfiguration();
    filter.type = 'late';
    const queryParams = new QueryParamsModelNew(
      filter,
    );
    //queryParams.sortField = this.column_sort.value;
    this.reportService.ReportByConditions(queryParams).subscribe(data => {
      if (data && data.status == 1) {
        this.StaffMostLate = data.data;
      }
      this.detectChange.detectChanges();
    });
  };
  // report by mileston
  Milestone: any[] = [];
  TopMilestone: any[] = [];
  getReportByMilestone() {
    var filter: any = this.filterConfiguration();
    filter.istop = 1;
    const queryParams = new QueryParamsModelNew(
      filter,
    );
    //queryParams.sortField = this.column_sort.value;
    this.reportService.ReportByMilestone(queryParams).subscribe(data => {
      if (data && data.status == 1) {
        this.Milestone = data.data;
      }
      this.detectChange.detectChanges();
    })
    this.reportService.TopMilestone(new QueryParamsModelNew(filter,)).subscribe(data => {
      if (data && data.status == 1) {
        this.TopMilestone = data.data;
      }
      this.detectChange.detectChanges();
    })
  }

  getPercentage(value) {
    return [+value, (100 - value)]
  }

  //get Report By Department
  colorCrossbarDept = ['#8fc79c', '#dbd491', '#d1837d', '#a8d3f0'];
  Department: any[] = [];
  getReportByDepartment() {
    var filter: any = this.filterConfiguration();
    filter.key = 'id_department';
    const queryParams = new QueryParamsModelNew(
      filter,
    );
    //queryParams.sortField = this.column_sort.value;
    this.reportService.ReportByDepartment(queryParams).subscribe(data => {
      if (data && data.status == 1) {
        this.Department = data.data;
        this.Department.forEach(i => {
          i.color = this.colorCrossbarDept;
          i.datasets = [
            +i.hoanthanh,
            +i.ht_quahan,
            +i.danglam,
            +i.dangdanhgia,
          ]
        });
      }
      this.detectChange.detectChanges();
    })
  }

  //get Report By Department
  ProjectTeams: any[] = [];
  getReportByProjectTeam() {
    var filter: any = this.filterConfiguration();
    filter.key = 'id_project_team';
    const queryParams = new QueryParamsModelNew(
      filter,
    );
    //queryParams.sortField = this.column_sort.value;
    this.reportService.ReportByProjectTeam(queryParams).subscribe(data => {
      if (data && data.status == 1) {
        this.ProjectTeams = data.data;
        this.ProjectTeams.forEach(i => {
          i.color = this.colorCrossbarDept;
          i.datasets = [
            +i.hoanthanh,
            +i.ht_quahan,
            +i.danglam,
            +i.dangdanhgia,
          ]
        });
      }
      this.detectChange.detectChanges();
    })
  }

  //get Report By Department
  ReportToDepartments: any = {};
  getReportToDepartments() {
    var filter: any = this.filterConfiguration();
    const queryParams = new QueryParamsModelNew(
      filter,
    );
    //queryParams.sortField = this.column_sort.value;
    this.reportService.ReportToDepartments(queryParams).subscribe(data => {
      if (data && data.status == 1) {
        this.ReportToDepartments = data.data;
      }
      this.detectChange.detectChanges();
    })
  }

  //get Report By Department
  CacConSoThongKe: any = {};
  getCacConSoThongKe() {
    var filter: any = this.filterConfiguration();
    const queryParams = new QueryParamsModelNew(
      filter,
    );
    //queryParams.sortField = this.column_sort.value;
    this.reportService.CacConSoThongKe(queryParams).subscribe(data => {
      if (data && data.status == 1) {
        this.CacConSoThongKe = data.data;
      }
      this.detectChange.detectChanges();

    })
  }

  //get Report By Department
  Eisenhower: any = {};
  getEisenhower() {
    const queryParams = new QueryParamsModelNew(
      this.filterConfiguration(),
    );
    //queryParams.sortField = this.column_sort.value;
    this.reportService.Eisenhower(queryParams).subscribe(data => {

      if (data && data.status == 1) {
        this.Eisenhower = data.data;
      }
      this.detectChange.detectChanges();

    })
  }

  getHeight() {
		return (window.innerHeight - 60 - this.tokenStorage.getHeightHeader());
	}

}

export interface DialogData {
  startDate: string;
  endDate: string;
}