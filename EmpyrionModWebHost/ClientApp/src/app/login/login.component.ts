import { Component, OnInit } from '@angular/core';
import { Router, ActivatedRoute } from '@angular/router';
import { FormBuilder, FormGroup, Validators } from '@angular/forms';
import { first } from 'rxjs/operators';

import { AuthenticationService } from '../services/authentication.service';

@Component({
  templateUrl: 'login.component.html',
  styleUrls: ['./login.component.less']
})
export class LoginComponent implements OnInit {
  loginForm: FormGroup;
  loading = false;
  submitted = false;
  returnUrl: string;
  steamLoginUrl: string;
  error = '';

  constructor(
    private formBuilder: FormBuilder,
    private route: ActivatedRoute,
    private router: Router,
    private authenticationService: AuthenticationService
  ) { }

  ngOnInit() {
    this.loginForm = this.formBuilder.group({
      username: ['', Validators.required],
      password: ['', Validators.required]
    });

    // reset login status
    this.authenticationService.logout();

    // get return url from route parameters or default to '/'
    this.returnUrl = this.route.snapshot.queryParams['returnUrl'] || '/';

    let currentUrl = new URL(window.location.href);

    let steamUri = new URL("https://steamcommunity.com/openid/login?openid.ns=http");
    steamUri.searchParams.set('openid.ns',         'http://specs.openid.net/auth/2.0');
    steamUri.searchParams.set('openid.mode'      , 'checkid_setup');
    steamUri.searchParams.set('openid.return_to' ,  currentUrl.protocol + '://' + currentUrl.host + "/login");
    steamUri.searchParams.set('openid.realm',       currentUrl.protocol + '://' + currentUrl.host);
    steamUri.searchParams.set('openid.identity'  , 'http://specs.openid.net/auth/2.0/identifier_select');
    steamUri.searchParams.set('openid.claimed_id', 'http://specs.openid.net/auth/2.0/identifier_select');
    this.steamLoginUrl = steamUri.toString();
  }

  // convenience getter for easy access to form fields
  get f() { return this.loginForm.controls; }

  onSubmit() {
    this.submitted = true;

    // stop here if form is invalid
    if (this.loginForm.invalid) {
      return;
    }

    this.loading = true;
    this.authenticationService.login(this.f.username.value, this.f.password.value)
      .pipe(first())
      .subscribe(
        data => {
          this.router.navigate([this.returnUrl]);
        },
        error => {
          this.error = error;
          this.loading = false;
        });
  }
}
