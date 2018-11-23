import { Component, OnInit, Input } from '@angular/core';

import { ChatService } from '../services/chat.service'
import { ChatListComponent } from '../chat-list/chat-list.component';
import { SessionService } from '../services/session.service';

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
  chatAsUser: boolean = true;
  @Input() chatList: ChatListComponent;

  constructor(public SessionService: SessionService, public ChatService: ChatService) { }

  ngOnInit() {
  }

  SendMessage() {
    this.ChatService.SendMessage(this.chatAsUser ? this.SessionService.CurrentSession.name : null, this.message);
    this.message = "";
  }
}
