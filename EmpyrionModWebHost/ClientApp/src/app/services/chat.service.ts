import { Injectable } from '@angular/core';
import { Observable, of, from, BehaviorSubject } from 'rxjs';
import { HubConnection, HubConnectionBuilder } from '@aspnet/signalr';

import { ChatModel } from '../model/chat-model'
import { CHAT } from '../model/chat-mock';

@Injectable({
  providedIn: 'root'
})
export class ChatService {
  public hubConnection: HubConnection;

  private messages: BehaviorSubject<ChatModel[]> = new BehaviorSubject(CHAT);
  public readonly messagesObservable: Observable<ChatModel[]> = this.messages.asObservable();

  constructor() {
    let builder = new HubConnectionBuilder();

    // as per setup in the startup.cs
    this.hubConnection = builder.withUrl('/hubs/chat').build();

    // message coming from the server
    this.hubConnection.on("Send", (message) => {
      this.messages.next(this.messages.getValue().concat(JSON.parse(message)));
    });

    // starting the connection
    this.hubConnection.start();
  }

  GetMessages(): Observable<ChatModel[]> {
    return this.messagesObservable;
  }

  SendMessage(aMessage: string): void {
    let msg: ChatModel = { mark: "N CB", type: "G", timestamp: "08-15:55", faction: "123", playerName: "play", message: aMessage };
    this.messages.next(this.messages.getValue().concat(msg));
    //this.hubConnection.invoke("SendMessage", "x", aMessage);
  }
}
