import { Component, OnInit } from '@angular/core';
import { Router } from '@angular/router';

@Component({
  selector: 'app-entities',
  templateUrl: './entities.component.html',
  styleUrls: ['./entities.component.less']
})
export class EntitiesComponent implements OnInit {

  constructor(
    public router: Router,
  ) {
  }

  ngOnInit() {
  }

}
