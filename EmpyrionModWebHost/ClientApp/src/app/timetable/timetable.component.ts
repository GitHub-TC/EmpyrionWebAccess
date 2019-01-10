import { Component, OnInit, ViewChild } from '@angular/core';
import { Router } from '@angular/router';
import { HttpClient } from '@angular/common/http';
import { YesNoDialogComponent, YesNoData } from '../yes-no-dialog/yes-no-dialog.component';

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
  chat                    = "Chat",
  restart                 = "Server restart",
  startEGS                = "Server start",
  stopEGS                 = "Server stop",
  backupFull              = "Backup (complete)",
  backupStructure         = "Backup (structures)",
  backupSavegame          = "Backup (savegame)",
  backupScenario          = "Backup (scenario)",
  backupMods              = "Backup (mods)",
  backupEGSMainFiles      = "Backup (EGSMainFiles)",
  deleteOldBackups        = "Delete old backups",
  deleteOldBackpacks      = "Delete old backpacks",
  deletePlayerOnPlayfield = "Delete player on playfield",
  runShell                = "Run shell",
  consoleCommand          = "InGame console command",
}

class SubTimetableAction{
  active: boolean;
  actionType?: ActionType;
  data?: string;
}

class TimetableAction extends SubTimetableAction{
  timestamp?: any;
  repeat?: RepeatEnum;
  nextExecute?: string;
  subAction?: SubTimetableAction[];
}


@Component({
  selector: 'app-timetable',
  templateUrl: './timetable.component.html',
  styleUrls: ['./timetable.component.less']
})
export class TimetableComponent implements OnInit {
  @ViewChild(YesNoDialogComponent) YesNo: YesNoDialogComponent;
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
    this.GetTimetable();
  }

  GetTimetable() {
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
      result.timestamp = "0001-01-01T" + (T.timestamp ? T.timestamp : "00:00");
      return result;
    }))
      .subscribe(
        T => this.GetTimetable(),
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

  RunThis(aAction: SubTimetableAction) {
    this.YesNo.openDialog({ title: "Run action", question: aAction.actionType }).afterClosed().subscribe(
      (YesNoData: YesNoData) => {
        if (!YesNoData.result) return;

        this.http.post("Timetable/RunThis", aAction)
          .subscribe(
            T => { },
            error => this.error = error // error path
          );
      });
  }

  DeleteThis(aAction: TimetableAction) {
    this.YesNo.openDialog({ title: "Delete action", question: aAction.actionType }).afterClosed().subscribe(
      (YesNoData: YesNoData) => {
        if (!YesNoData.result) return;
        this.Timetable = this.Timetable.filter(T => T != aAction);
      });
  }

  DeleteThisSubAction(aAction: TimetableAction, aSubAction: SubTimetableAction) {
    this.YesNo.openDialog({ title: "Delete action", question: aAction.actionType }).afterClosed().subscribe(
      (YesNoData: YesNoData) => {
        if (!YesNoData.result) return;
        aAction.subAction = aAction.subAction.filter(T => T != aSubAction);
      });
  }

  GetNextExecute(aAction: TimetableAction) {
    return aAction.nextExecute ? new Date(aAction.nextExecute) : new Date();
  }

}
