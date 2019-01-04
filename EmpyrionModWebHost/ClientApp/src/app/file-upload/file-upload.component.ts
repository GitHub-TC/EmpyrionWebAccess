import { Component, Input, EventEmitter, Output } from '@angular/core';
import { HttpClient, HttpRequest, HttpEventType, HttpResponse } from '@angular/common/http'
import { Event } from '@angular/router';

@Component({
  selector: 'app-file-upload',
  templateUrl: './file-upload.component.html',
  styleUrls: ['./file-upload.component.less']
})
export class FileUploadComponent {

public progress: number;
  public message: string;
  constructor(private http: HttpClient) { }

  @Output() uploaded: EventEmitter<any> = new EventEmitter();
  @Input() UploadURL: string;
  @Input() UploadTitle: string = "Choose a file...";

  onChange(mainEvent) {
    const files = mainEvent.target.files;

    if (files.length === 0)
      return;

    const formData = new FormData();

    for (let file of files)
      formData.append(file.name, file);

    const uploadReq = new HttpRequest('POST', this.UploadURL, formData, {
      reportProgress: true,
    });

    this.http.request(uploadReq).subscribe(event => {
      if (event.type === HttpEventType.UploadProgress)
        this.progress = Math.round(100 * event.loaded / event.total);
      else if (event.type === HttpEventType.Response) {
        try { this.message = event.body.toString(); } catch { }
        this.uploaded.emit();
        mainEvent.srcElement.value = null;
      }
    });
  }
}
