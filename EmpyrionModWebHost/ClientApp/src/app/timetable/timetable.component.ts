import { Component, OnInit, ViewChild } from '@angular/core';
import { Router } from '@angular/router';
import { HttpClient } from '@angular/common/http';
import { YesNoDialogComponent, YesNoData } from '../yes-no-dialog/yes-no-dialog.component';
import { Enum } from '../model/Enum';

enum RepeatEnum {
  manual = "manual",
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
  chat                    = "Chat | [Chattext] -> Example:[c][00ffff]Text with color [b] and bold[/b].[/c]",
  chatUntil               = "Chat until | [Chattext] -> Example:[c][00ffff]Text with color [b] and bold[/b].[/c]",
  restart                 = "Server restart | [minutes] (default:0)",
  startEGS                = "Server start|",
  stopEGS                 = "Server stop | [minutes] (default:0)",
  backupFull              = "Backup (complete)|",
  backupStructure         = "Backup (structures)|",
  backupSavegame          = "Backup (savegame)|",
  backupScenario          = "Backup (scenario)|",
  backupMods              = "Backup (mods)|",
  backupEGSMainFiles      = "Backup (EGSMainFiles)|",
  deleteOldBackups        = "Delete old backups | [days] (default:14)",
  deleteOldBackpacks      = "Delete old backpacks | [days] (default:14)",
  deletePlayerOnPlayfield = "Delete player on playfield| [playfield[;playfield]] -> Example: Akua; Omicron; Masperon",
  deleteHistoryBook       = "Delete HistoryBook | [days] (default:14)",
  deleteOldFactoryItems   = "Delete old factory items | [days] (default:14)",
  runShell                = "Run shell| [cmd] -> Working directory ist the current savegame folder",
  consoleCommand          = "InGame console command | [cmd] -> Help description of commands in the console with 'help'",
  wipePlayfield           = "Wipe playfields | [poi deposit terrain player]:[playfield[;playfield]] -> Example: poi deposit : Akua; Hsaa",
  resetPlayfield          = "Reset playfields | [playfield[;playfield]] -> Example: Akua; Hsaa",
  recreatePlayfield       = "Recreate playfields | delete Data,Cache,Template -> [playfield[;playfield]] -> Example: Akua; Hsaa",
  resetPlayfieldIfEmpty   = "Reset playfields if empty | [playfield[;playfield]] -> Example: Akua; Hsaa",
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
  Repeats: Enum<RepeatEnum>[];
  Actions: Enum<ActionType>[];

  constructor(
    public router: Router,
    private http: HttpClient,
  ) {
    this.Repeats = Object.keys(RepeatEnum).map(key => { return <Enum<RepeatEnum>>{ key: key, value: RepeatEnum[key] }; });
    this.Actions = Object.keys(ActionType).map(key => {
      let A = (<string>ActionType[key]).split('|').map(a => a.trim());
      return <Enum<ActionType>>{ key: key, value: A[0], help:A[1] };
    });
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
    setTimeout(() => { locationsSubscription.unsubscribe(); }, 120000);
  }

  ActionHelp(aAction) {
    let Found = this.Actions.find(A => A.key == aAction.actionType);
    return Found ? Found.help : null;
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
