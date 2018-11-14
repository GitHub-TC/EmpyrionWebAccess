import { Component, OnInit } from '@angular/core';
import { HubConnection, HubConnectionBuilder } from '@aspnet/signalr';

import { ChatModel } from '../model/chat-model'
import { CHAT } from '../model/chat-mock';

@Component({
  selector: 'app-chat',
  templateUrl: './chat.component.html',
  styleUrls: ['./chat.component.less']
})
export class ChatComponent implements OnInit {
  public hubConnection: HubConnection;

  messages: ChatModel[] = [];
  message: string;

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
