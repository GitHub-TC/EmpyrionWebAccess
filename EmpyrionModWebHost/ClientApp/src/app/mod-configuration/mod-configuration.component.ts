import { Component, OnInit, Input, ViewChild } from '@angular/core';
import { HttpClient, HttpRequest, HttpEventType, HttpHeaders } from '@angular/common/http';
import { JsonEditorComponent, JsonEditorOptions } from 'ang-jsoneditor';
import { ModData } from '../server-mod-manager/server-mod-manager.component';

@Component({
  selector: 'app-mod-configuration',
  templateUrl: './mod-configuration.component.html',
  styleUrls: ['./mod-configuration.component.less']
})
export class ModConfigurationComponent implements OnInit {
  _ModData: ModData;
  @Input() get ModData() { return this._ModData; }
  set ModData(data: ModData) { this._ModData = data; this.GetModConfig(); }
  ConfigData: any;
  error: any;
  ChangedData: boolean;
  public editorOptions: JsonEditorOptions;
  public data: any;
  @ViewChild(JsonEditorComponent, { static: false }) editor: JsonEditorComponent;

  constructor(
    private http: HttpClient,
  ) {
    this.editorOptions = new JsonEditorOptions();
    this.editorOptions.expandAll = true;
    this.editorOptions.modes = ['code', 'text', 'tree', 'view']; // set all allowed modes
   }

  ngOnInit() {
  }

  GetModConfig() {
    let locationsSubscription = this.http.get("Mod/GetModConfig/" + encodeURIComponent(this.ModData.name), { responseType: "text" as 'json' })
      .pipe()
      .subscribe(
      data => {
        this.ChangedData = false;
        switch (this.ModData.configurationType) {
          case "json": this.ConfigData = JSON.parse(<string>data); break;
          default:     this.ConfigData = data
        }
      },
      error => this.error = error // error path
    );
    // Stop listening for location after 10 seconds
    setTimeout(() => { locationsSubscription.unsubscribe(); }, 120000);
  }

  updateData(e) {
    this.ChangedData = true;

    if (e.initEvent) return;
    this.ConfigData = e;
  }

  Save() {
    const formData = new FormData();

    formData.append(this.ModData.name, this.ModData.configurationType == "json" ? JSON.stringify(this.ConfigData, null, 2) : this.ConfigData);

    const uploadReq = new HttpRequest('POST', "Mod/UploadModConfig/" + encodeURIComponent(this.ModData.name), formData);

    this.http.request(uploadReq).subscribe(event => {
      if (event.type === HttpEventType.Response) {
        this.ChangedData = false;
      }
    });;
  }
}
