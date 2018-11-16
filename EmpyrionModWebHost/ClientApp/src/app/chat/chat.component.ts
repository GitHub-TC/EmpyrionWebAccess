import { Component, OnInit, Input } from '@angular/core';

import { ChatModel } from '../model/chat-model'

import { ChatService } from '../services/chat.service'
import { ChatListComponent } from '../chat-list/chat-list.component';

@Component({
  selector: 'app-chat',
  templateUrl: './chat.component.html',
  styleUrls: ['./chat.component.less']
})
export class ChatComponent implements OnInit {
  displayedColumns = ['type', 'timestamp', 'faction', 'playerName', 'message'];

  message: string = "";
  chatTarget: string = "All";
  chatToAll: boolean = true;
  @Input() chatList: ChatListComponent;

  constructor(private mChatService: ChatService) { }

  ngOnInit() {
  }

  SendMessage() {
    this.mChatService.SendMessage(this.message);
    this.message = "";
  }

  getLineClass(aMsg: ChatModel) {
    return aMsg.mark;
  }
}
