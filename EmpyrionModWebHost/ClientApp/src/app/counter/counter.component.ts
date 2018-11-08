import { Component, OnInit } from '@angular/core';
import { HubConnection, HubConnectionBuilder } from '@aspnet/signalr';

@Component({
  selector: 'app-counter-component',
  templateUrl: './counter.component.html'
})
export class CounterComponent implements OnInit {
  public hubConnection: HubConnection;
  public messages: string[] = [];
  public message: string;

  public currentCount = 0;

  public incrementCounter() {
    this.currentCount++;
  }

  ngOnInit() {
    let builder = new HubConnectionBuilder();

    // as per setup in the startup.cs
    this.hubConnection = builder.withUrl('/hubs/chat').build();

    // message coming from the server
    this.hubConnection.on("Send", (message) => {
      this.messages.push(message);
    });

    // starting the connection
    this.hubConnection.start();
  }

  send() {
    // message sent from the client to the server
    this.hubConnection.invoke("SendMessage", "x", this.message);
    this.message = "";
  }
}
