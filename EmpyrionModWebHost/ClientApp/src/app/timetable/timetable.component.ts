import { Component, OnInit } from '@angular/core';
import { Router } from '@angular/router';
import { HttpClient } from '@angular/common/http';

enum RepeatEnum {
  min5 = "5 min",
  min10 = "10 min",
  min15 = "15 min",
  min20 = "20 min",
  min30 = "30 min",
  min45 = "45 min",
  hour1 = "1 hour",
  hour2 = "2 hour",
  hour3 = "3 hour",
  hour6 = "6 hour",
  hour12 = "12 hour",
  day1 = "1 day",
  dailyAt = "daily at",
  mondayAt = "monday at",
  tuesdayAt = "tuesday at",
  wednesdayAt = "wednesday at",
  thursdayAt = "thursday at",
  fridayAt = "friday at",
  saturdayAt = "saturday at",
  sundayAt = "sunday at",
  monthly = "monthly",
  timeAt = "at time"
}

export enum ActionType {
  chat = "Chat",
  restart = "Server restart",
  backupFull = "Backup (complete)",
  backupStructure = "Backup (structures)",
  deletePlayerOnPlayfield = "Delete player on playfield",
  runShell = "Run shell",
  consoleCommand = "InGame console command",
}

class SubTimetableAction{
  active: boolean;
  actionType?: ActionType;
  data?: string;
}

class TimetableAction{
  active: boolean;
  actionType?: ActionType;
  data?: string;
  timestamp?: any;
  repeat?: RepeatEnum;
  subAction?: SubTimetableAction[];
}


@Component({
  selector: 'app-timetable',
  templateUrl: './timetable.component.html',
  styleUrls: ['./timetable.component.less']
})
export class TimetableComponent implements OnInit {
    error: any;
  Timetable: TimetableAction[] = [];
  Repeats: {}
  Actions: {}

  constructor(
    public router: Router,
    private http: HttpClient,
  ) {
    this.Repeats = Object.keys(RepeatEnum).map(key => { return { key: key, value: RepeatEnum[key] }; });
    this.Actions = Object.keys(ActionType).map(key => { return { key: key, value: ActionType[key] }; });
  }

  ngOnInit() {
    let locationsSubscription = this.http.get<TimetableAction[]>("Timetable/GetTimetable")
      .subscribe(
      T => this.Timetable = T.map(t => {
        let date = new Date(t.timestamp);
        let hh = date.getHours();
        let mm = date.getMinutes();
        t.timestamp = (hh < 10 ? "0" : "") + hh + ":" + (mm < 10 ? "0" : "") + mm;
        t.actionType = Object.getOwnPropertyNames(ActionType)[t.actionType];
        t.repeat     = Object.getOwnPropertyNames(RepeatEnum)[t.repeat];
        if (t.subAction) t.subAction.map(st => {
          st.actionType = Object.getOwnPropertyNames(ActionType)[st.actionType];
          return st;
        });
        return t;
      }),
        error => this.error = error // error path
      );
    // Stop listening for location after 10 seconds
    setTimeout(() => { locationsSubscription.unsubscribe(); }, 10000);
  }

  Save() {
    this.http.post("Timetable/SetTimetable", this.Timetable.map(T => {
      let result = Object.assign({}, T);
      result.timestamp = "0001-01-01T" + T.timestamp;
      return result;
    }))
      .subscribe(
        T => { },
        error => this.error = error // error path
      );
  }

  AddAction() {
    if (!this.Timetable) this.Timetable = [];
    this.Timetable.push({ active: true, repeat: RepeatEnum.min5 })
  }

  AddSubAction(aAction: TimetableAction) {
    if (!aAction.subAction) aAction.subAction = [];
    aAction.subAction.push({ active: true })
  }

}
