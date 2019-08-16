import { Component, OnInit } from '@angular/core';
import { HttpClient } from '@angular/common/http';

@Component({
  selector: 'app-restore-manager',
  templateUrl: './restore-manager.component.html',
  styleUrls: ['./restore-manager.component.less']
})
export class RestoreManagerComponent implements OnInit {
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



}
