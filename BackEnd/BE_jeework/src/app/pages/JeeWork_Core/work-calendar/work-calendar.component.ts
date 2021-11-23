import { TokenStorage } from './../../../_metronic/jeework_old/core/auth/_services/token-storage.service';
import { FullCalendarComponent } from "@fullcalendar/angular";
import { LayoutUtilsService } from "./../../../_metronic/jeework_old/core/utils/layout-utils.service";
import { QueryParamsModel } from "./../../../_metronic/jeework_old/core/_base/crud/models/query-models/query-params.model";
import { ProjectsTeamService } from "./../projects-team/Services/department-and-project.service";
import {
  Component,
  OnInit,
  ViewChild,
  ViewEncapsulation,
  ChangeDetectorRef,
  ChangeDetectionStrategy,
  Input,
  EventEmitter,
  Output,
} from "@angular/core";
import { MatDialog } from "@angular/material/dialog";
import { BehaviorSubject, fromEvent } from "rxjs";
import * as moment from "moment";
import { WorkCalendarService } from "./work-calendar.service";
import { TranslateService } from "@ngx-translate/core";
import { Router } from "@angular/router";
import { INITIAL_EVENTS, createEventId } from "./event-utils";

// Angular calendar

import {
  CalendarOptions,
  DateSelectArg,
  EventApi,
  EventClickArg,
  EventInput,
} from "@fullcalendar/common";


@Component({
  selector: "kt-work-calendar",
  templateUrl: "./work-calendar.component.html",
  styleUrls: ["./work-calendar.component.scss"],
  encapsulation: ViewEncapsulation.None,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class WorkCalendarComponent implements OnInit {
  @ViewChild("calendar", { static: true }) calendar: FullCalendarComponent;
  @Input() data: BehaviorSubject<any>; //Dữ liệu truyền vào(nếu có)
  @Output() BookingValue = new EventEmitter();
  // calendarPlugins = [dayGridPlugin, timeGrigPlugin];
  _title: string = "Lịch";
  option: CalendarOptions;
  weekText: string = "";
  lstEvent: any[];
  _data: any = {};
  start_date: any;
  textTime: string = "";
  calendarEvents: EventInput[] = [{ title: "Event Now", start: new Date() }];
  arrDate = [];
  user: any;
  dateString: String = "";
  rR: any = {};
  date: any;
  id_project_team: number = 0;
  disabledEvents: any[] = [];
  constructor(
    public dialog: MatDialog,
    private WorkCalendarService: WorkCalendarService,
    private layoutUtilsService: LayoutUtilsService,
    private router: Router,
    private ProjectsTeamService: ProjectsTeamService,
    private translate: TranslateService,
    private tokenStorage: TokenStorage,
  ) {
    this.user = JSON.parse(localStorage.getItem("UserInfo"));
    this.arrDate = [
      this.translate.instant("day.chunhat"),
      this.translate.instant("day.thuhai"),
      this.translate.instant("day.thuba"),
      this.translate.instant("day.thutu"),
      this.translate.instant("day.thunam"),
      this.translate.instant("day.thusau"),
      this.translate.instant("day.thubay"),
    ];
  }
  ngOnInit() {
    var arr = this.router.url.split("/");
    if (arr.length > 2) this.id_project_team = +arr[2];
    this.getConfigCalendar();
    // this.LoadDatas();
  }

  LoadDatas(){
    const queryParams = new QueryParamsModel(
      this.filterConfiguration()
    );

    if (this.id_project_team > 0) {
      this.WorkCalendarService.get_listeventbyproject(
        queryParams
      ).subscribe((res) => {
        this.lstEvent = [];

        if (res.data && res.status == 1) {
          this.lstEvent = res.data;
        }
      });
    } else {
      this.WorkCalendarService.getEvents(queryParams).subscribe(
        (res) => {
          this.lstEvent = [];
          if (res.data && res.status == 1) {
            this.lstEvent = res.data;
          }
        }
      );
    }
  }

  getDateString(fromdate: Date, todate: Date) {
    this.dateString =
      " <span class='red-text'>" +
      (moment(fromdate).month() + 1) +
      "</span> - (" +
      this.translate.instant("wuser.tungay") +
      " " +
      moment(fromdate).format("DD/MM/YYYY") +
      " " +
      this.translate.instant("wuser.denngay") +
      " " +
      moment(todate).subtract(1).format("DD/MM/YYYY") +
      ")";
  }

  getConfigCalendar() {
    this.option = {
      headerToolbar: {
        left: "title",
        center: " prev, today, next",
        right: "",
      },
      buttonText: {
        today: this.translate.instant("day.tuannay"),
        month: this.translate.instant("day.thangnay"),
        week: this.translate.instant("day.tuannay"),
        day: this.translate.instant("day.homnay"),
        prev: this.translate.instant("day.xemthangtruoc"),
        next: this.translate.instant("day.xemthangsau"),
      },
      editable: true,
      selectable: true,
      selectMirror: true,
      selectOverlap: false,
      displayEventTime: true,
      displayEventEnd: true,
      eventOverlap: false,
      eventTimeFormat: {
        hour: "2-digit",
        hour12: false,
      },
      // select: this.handleDateSelect.bind(this),
      eventClick: this.eventClicked.bind(this),
      // eventsSet: this.handleEvents.bind(this),
      selectAllow: (el) => {
        let now = new Date();
        if (el.start > now) {
          const diffTime = Math.abs(+el.end - +el.start);
          const diffDays = Math.ceil(diffTime / (1000 * 60 * 60 * 24));
          if (diffDays > 1) {
            return false;
          }
          return true;
        }
        return false;
      },
      allDaySlot: false,
      slotLabelFormat: [
        {
          hour: "numeric",
          minute: "2-digit",
          hour12: false,
        },
      ],
      // initialEvents: INITIAL_EVENTS,
      eventSources: [
        {
          events: (info, successCallback, failureCallback) => {
            this.date = {
              start: this.getStrDate(info.start),
              end: this.getStrDate(info.end),
            };
            this.start_date = info.start;
            const queryParams = new QueryParamsModel(
              this.filterConfiguration()
            );

            if (this.id_project_team > 0) {
              this.WorkCalendarService.get_listeventbyproject(
                queryParams
              ).subscribe((res) => {
                this.lstEvent = [];

                if (res.data && res.status == 1) {
                  this.lstEvent = res.data;
                }
                successCallback(res.data);
              });
            } else {
              this.WorkCalendarService.getEvents(queryParams).subscribe(
                (res) => {
                  this.lstEvent = [];
                  if (res.data && res.status == 1) {
                    this.lstEvent = res.data;
                    successCallback(res.data);
                  }
                }
              );
            }
          },
        },
      ],
    };
  }

  /** FILTRATION */
  filterConfiguration(): any {
    const filter: any = {};
    filter.TuNgay = this.date.start;
    filter.DenNgay = this.date.end;
    if (this.id_project_team > 0) filter.id_project_team = this.id_project_team;
    return filter;
  }
  datesRender(arg) {
    this.getDateString(arg.view.currentStart, arg.view.currentEnd);
  }
  eventRender(info) {
    info.el.innerHTML = `<div class="fc-content">
    <span class="fc-image" style="background-image: url('${info.event.extendedProps.imageurl}');"></span>
     <span class="fc-title">${info.event.title}</span></div>`;
  }
  addDays(date: string, days: number): Date {
    let _date = moment(date).toDate();
    _date.setDate(_date.getDate() + days);
    return _date;
  }

  eventClicked(el) {
		this.router.navigate(['', { outlets: { auxName: 'aux/detail/'+ el.event.extendedProps.id_row }, }]);
  }
  handleSelect(el) {
    this.layoutUtilsService.showActionNotification(
      "hand leSelect:" + el.event.extendedProps.id_row
    );
    for (var i = 0; i < this.disabledEvents.length; i++) {
      if (
        el.start >= moment(this.disabledEvents[i].start).toDate() &&
        el.end <= moment(this.disabledEvents[i].end).toDate()
      ) {
        return;
      }
    }
  }
  navLinkDayClick = function (date) {
    let str = moment(date).format("YYYY-MM-DD");
    let calendarApi = this.calendarComponent.getApi();
    calendarApi.changeView("timeGridDay");
    calendarApi.gotoDate(str); // call a method on the Calendar object
  };
  eventDrop(el) {
    let data = this.prepareData(el);
    this.UpdateBooking(data);
  }
  prepareData(el: any) {}
  eventResize(el) {
    let data = this.prepareData(el);
    this.UpdateBooking(data);
  }
  UpdateBooking(data) {
    data.onlyUpdateDate = true;
    data.BatDau = moment(new Date(data.BatDau)).format(
      "YYYY-MM-DD[T]HH:mm:ss.SSS"
    );
    data.KetThuc = moment(new Date(data.KetThuc)).format(
      "YYYY-MM-DD[T]HH:mm:ss.SSS"
    );
  }

  LoadData(event) {
    this.calendar.getApi().refetchEvents();
  }

  getStrDate(date: Date) {
    return moment(date).format("DD/MM/YYYY");
  }
  getStrTime(date: Date) {
    let str = "";
    if (date.getHours() < 10) {
      str += "0" + date.getHours() + ":";
    } else {
      str += date.getHours() + ":";
    }
    if (date.getMinutes() < 10) {
      str += "0" + date.getMinutes() + ":";
    } else {
      str += date.getMinutes() + ":";
    }
    if (date.getSeconds() < 10) {
      str += "0" + date.getSeconds();
    } else {
      str += date.getSeconds();
    }
    return str;
  }

  Close() {
    window.history.back();
  }

  // new calendar
  //==================================================================================================
  //==================================================================================================
  //==================================================================================================
  //==================================================================================================
  //==================================================================================================
  //==================================================================================================
  //==================================================================================================
  // Thắng comment checkin file
  calendarVisible = true;
  calendarOptions: CalendarOptions = {
    headerToolbar: {
      left: "",
      center: "prev, today ,next",
      right: "",
    },
    views: {
      dayGridMonth: {
        firstDay: 0,
        eventLimit: 3,
        titleFormat: { year: "numeric", month: "2-digit" },
        eventLimitText: this.translate.instant("day.mathangkhac"),
        columnHeaderHtml: (date) => {
          let _date = new Date(date);
          let str_day = "";
          if (_date.getDay() == 0) {
            str_day = this.translate.instant("day.chunhat");
          } else {
            var day = "day.thu" + (_date.getDay() + 1);
            str_day = this.translate.instant(day);
          }
          return "" + str_day;
        },
        // height: this.getHeightCalendar()
      },
    },
    initialView: "dayGridMonth",
    initialEvents: INITIAL_EVENTS, // alternatively, use the `events` setting to fetch from a feed
    weekends: true,
    editable: true,
    selectable: true,
    selectMirror: true,
    dayMaxEvents: true,
    select: this.handleDateSelect.bind(this),
    eventClick: this.handleEventClick.bind(this),
    eventsSet: this.handleEvents.bind(this),
  };

  currentEvents: EventApi[] = [];

  handleCalendarToggle() {
    this.calendarVisible = !this.calendarVisible;
  }

  handleWeekendsToggle() {
    const { calendarOptions } = this;
    calendarOptions.weekends = !calendarOptions.weekends;
  }

  handleDateSelect(selectInfo: DateSelectArg) {
    const title = prompt("Please enter a new title for your event");
    const calendarApi = selectInfo.view.calendar;

    calendarApi.unselect(); // clear date selection

    if (title) {
      calendarApi.addEvent({
        id: createEventId(),
        title,
        start: selectInfo.startStr,
        end: selectInfo.endStr,
        allDay: selectInfo.allDay,
      });
    }
  }

  handleEventClick(clickInfo: EventClickArg) {
    if (
      confirm(
        `Are you sure you want to delete the event '${clickInfo.event.title}'`
      )
    ) {
      clickInfo.event.remove();
    }
  }

  handleEvents(events: EventApi[]) {
    this.currentEvents = events;
  }

  // getHeight() {
  //   let tmp_height = 0;
  //   tmp_height = window.innerHeight - 60 - this.tokenStorage.getHeightHeader();
  //   var link = this.router.url.split('/').find(x => x == 'tasks');
  //   if (link) {
  //     tmp_height += 45;
  //   }
    
  //   return tmp_height;
  // }
  getHeight() {
    let tmp_height = 0;
    tmp_height = window.innerHeight - 90 - this.tokenStorage.getHeightHeader();
    var link = this.router.url.split('/').find(x => x == 'tasks');
    if (link) {
      tmp_height += 45;
    }
    
    return tmp_height;
  }
}
