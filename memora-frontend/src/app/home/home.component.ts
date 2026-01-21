import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { AuthService } from '../user/auth.service';

@Component({
  standalone: true,
  selector: 'app-home',
  imports: [CommonModule],
  template: `
    <div class="home">
      <h1>Home</h1>
      <p>This is your homepage.</p>
      <button (click)="logout()">Logout</button>
    </div>
  `,
  styles: [`
    .home {
      padding: 3rem;
      text-align: center;
    }
  `]
})
export class HomeComponent {
  constructor(private auth: AuthService) {}

  logout() {
    this.auth.logout();
  }
}