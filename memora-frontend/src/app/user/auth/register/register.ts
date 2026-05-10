import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterModule } from '@angular/router';
import { AuthService } from '../auth.service';
import { Router } from '@angular/router';
<<<<<<< HEAD
import { TranslatePipe } from '../../../translate.pipe';
import { I18nService } from '../../../i18n.service';
import { HttpErrorResponse } from '@angular/common/http';
=======
import { TranslatePipe } from '../../../translation/translate.pipe';
import { I18nService } from '../../../translation/i18n.service';
>>>>>>> origin/main

@Component({
  standalone: true,
  selector: 'app-register',
  imports: [CommonModule, FormsModule, RouterModule, TranslatePipe],
  templateUrl: './register.html',
  styleUrls: ['./register.css']
})
export class RegisterComponent {
  email = '';
  password = '';
  errorMsg = '';

  constructor(
    private auth: AuthService,
    private router: Router,
    private i18n: I18nService
  ) {}

  register() {
    this.errorMsg = '';

    this.auth.register(this.email, this.password).subscribe({
      next: () => this.router.navigate(['/']),
<<<<<<< HEAD
      error: (err: HttpErrorResponse) => {
        this.error = err.error?.message ?? this.i18n.translate('auth.register.failed')
=======
      error: (e) => {
        const status = e?.status;
        const err = e?.error?.error;
        const message = e?.error?.message;

        // Email already exists
        if (status === 409 || err === 'conflict') {
          this.errorMsg = this.i18n.translate('auth.register.emailExists');
          return;
        }

        // Validation error
        if (status === 400) {
          this.errorMsg = message || this.i18n.translate('auth.register.invalid');
          return;
        }

        // Rate limit
        if (status === 429) {
          this.errorMsg = message || this.i18n.translate('auth.register.rateLimited');
          return;
        }

        // Fallback
        this.errorMsg = this.i18n.translate('auth.register.failed');
>>>>>>> origin/main
      }
    });
  }
}
