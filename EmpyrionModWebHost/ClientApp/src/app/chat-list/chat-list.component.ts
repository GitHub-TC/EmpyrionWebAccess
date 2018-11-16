import { Component, OnInit } from '@angular/core';

import { ChatModel } from '../model/chat-model'
import { CHAT } from '../model/chat-mock';

@Component({
  selector: 'app-chat-list',
  templateUrl: './chat-list.component.html',
  styleUrls: ['./chat-list.component.less']
})
export class ChatListComponent implements OnInit {

  displayedColumns = ['type', 'timestamp', 'faction', 'playerName', 'message'];

  messages: ChatModel[] = [];
  message: string;
  autoscroll: boolean = true;

  constructor() { }

  ngOnInit() {
    this.messages = CHAT;

  }

  getLineClass(aMsg: ChatModel) {
    return aMsg.mark;
  }

}
