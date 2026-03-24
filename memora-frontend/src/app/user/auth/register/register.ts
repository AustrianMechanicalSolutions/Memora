import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterModule } from '@angular/router';
import { AuthService } from '../../auth.service';
import { Router } from '@angular/router';
import { TranslatePipe } from '../../../translation/translate.pipe';
import { I18nService } from '../../../translation/i18n.service';

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
  error = '';

  constructor(
    private auth: AuthService,
    private router: Router,
    private i18n: I18nService
  ) {}

  register() {
    this.auth.register(this.email, this.password).subscribe({
      next: () => this.router.navigate(['/']),
      error: err => this.error = err.error?.message ?? this.i18n.translate('auth.register.failed')
    });
  }
}
