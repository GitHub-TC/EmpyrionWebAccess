import { Component, OnInit } from '@angular/core';

@Component({
  selector: 'app-counter-component',
  templateUrl: './counter.component.html'
})
export class CounterComponent implements OnInit {
  public messages: string[] = [];
  public message: string;

  public currentCount = 0;

  public incrementCounter() {
    this.currentCount++;
  }

  ngOnInit() {
  }

  send() {
    // message sent from the client to the server
    this.message = "";
  }
}
