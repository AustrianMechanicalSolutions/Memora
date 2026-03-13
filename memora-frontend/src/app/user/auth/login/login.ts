import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router, RouterModule } from '@angular/router';
import { AuthService } from '../../auth.service';

@Component({
  standalone: true,
  selector: 'app-login',
  imports: [CommonModule, FormsModule, RouterModule],
  templateUrl: './login.html',
  styleUrls: ['./login.css']
})
export class LoginComponent {
  email = '';
  password = '';
  error = '';

  twoFactorCode = '';
  show2fa = false;
  errorMsg = '';

  constructor(
    private auth: AuthService,
    private router: Router,
    private route: ActivatedRoute
  ) {}

  login() {
    this.errorMsg = '';

    this.auth.login(this.email, this.password, this.show2fa ? this.twoFactorCode : undefined)
      .subscribe({
        next: () => {
          const returnUrl = this.route.snapshot.queryParamMap.get('returnUrl') || '/home';
          this.router.navigateByUrl(returnUrl);
        },
        error: (e) => {
          const err = e?.error?.error;

          if (err === '2fa_required') {
            this.show2fa = true;
            this.errorMsg = 'Enter your 2FA code from Microsoft Authenticator.';
            return;
          }

          if (err === '2fa_invalid') {
            this.errorMsg = 'Invalid 2FA code.';
            return;
          }

          this.errorMsg = 'Login failed.';
        }
      });
  }
}
