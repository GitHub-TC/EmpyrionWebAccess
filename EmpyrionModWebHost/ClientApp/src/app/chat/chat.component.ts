import { Component, OnInit } from '@angular/core';
import { HubConnection, HubConnectionBuilder } from '@aspnet/signalr';

import { ChatModel } from './ChatModel'
import { CHAT } from './mock-chat';

@Component({
  selector: 'app-chat',
  templateUrl: './chat.component.html',
  styleUrls: ['./chat.component.css']
})
export class ChatComponent implements OnInit {
  public hubConnection: HubConnection;

  public messages: ChatModel[] = [];
  public message: string;

  constructor() { }

  ngOnInit() {
    this.messages = CHAT;

    let builder = new HubConnectionBuilder();

    // as per setup in the startup.cs
    this.hubConnection = builder.withUrl('/hubs/chat').build();

    // message coming from the server
    this.hubConnection.on("Send", (message) => {
      this.messages.push(JSON.parse(message));
    });

    // starting the connection
    this.hubConnection.start();
  }

  send() {
    this.hubConnection.invoke("SendMessage", "x", this.message);
    this.message = "";
  }

  getLineClass(aMsg: ChatModel) {
    return aMsg.mark;
  }
}
