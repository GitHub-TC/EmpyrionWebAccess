import { Component, OnInit, ViewChild } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { YesNoDialogComponent, YesNoData } from '../yes-no-dialog/yes-no-dialog.component';

@Component({
  selector: 'app-restore-manager',
  templateUrl: './restore-manager.component.html',
  styleUrls: ['./restore-manager.component.less']
})
export class RestoreManagerComponent implements OnInit {
  @ViewChild(YesNoDialogComponent, { static: true }) YesNo: YesNoDialogComponent;
  MarkBackup: string = "";
  SelectedBackup: string;
  Backups: string[];
  error: any;

  constructor(
    private http: HttpClient
  ) { }

  ngOnInit() {
    this.LoadBackupList();
  }

  LoadBackupList() {
    let locationsSubscription = this.http.get<string[]>("Backups/GetBackups")
      .pipe()
      .subscribe(
        B => this.Backups = B,
        error => this.error = error // error path
      );
    // Stop listening for location after 10 seconds
    setTimeout(() => { locationsSubscription.unsubscribe(); }, 120000);
  }

  Save() {
    this.http.post("Backups/MarkBackup", { backup: this.SelectedBackup, mark: this.MarkBackup })
      .pipe()
      .subscribe(
        B => this.LoadBackupList(),
        error => this.error = error // error path
      );
  }

  Zip() {
    if (!this.SelectedBackup) return;

    this.YesNo.openDialog({ title: "Zip this backup directory", question: this.SelectedBackup }).afterClosed().subscribe(
      (YesNoData: YesNoData) => {
        if (!YesNoData.result) return;

        this.http.post("Backups/ZipBackup", { backup: this.SelectedBackup, mark: this.MarkBackup })
          .pipe()
          .subscribe(
            B => this.LoadBackupList(),
            error => this.error = error // error path
          );
      });

  }



}
