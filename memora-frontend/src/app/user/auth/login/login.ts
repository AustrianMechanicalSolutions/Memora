import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router, RouterModule } from '@angular/router';
import { AuthService } from '../../auth.service';
import { TranslatePipe } from '../../../translate.pipe';
import { I18nService } from '../../../i18n.service';

@Component({
  standalone: true,
  selector: 'app-login',
  imports: [CommonModule, FormsModule, RouterModule, TranslatePipe],
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
    private route: ActivatedRoute,
    private i18n: I18nService
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
          const status = e?.status;
          const err = e?.error?.error;
          
          // 2FA handling
          if (err === '2fa_required') {
            this.show2fa = true;
            this.errorMsg = this.i18n.translate('auth.login.twoFactorHelp');
            return;
          }

          if (err === '2fa_invalid') {
            this.errorMsg = this.i18n.translate('auth.login.twoFactorInvalid');
            return;
          }

          // Unauthorized (wrong login)
          if (status === 401) {
            this.errorMsg = this.i18n.translate('auth.login.failed');
            return;
          }

          if (status === 429) {
            this.errorMsg = this.i18n.translate('auth.login.ratelimited');
            return;
          }

          this.errorMsg = this.i18n.translate('common.error');
        }
      });
  }
}
