import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { AuthService } from '../user/auth.service';
import { OverviewSidebarComponent } from './overview-sidebar/overview-sidebar';

@Component({
  standalone: true,
  selector: 'app-home',
  imports: [CommonModule, OverviewSidebarComponent],
  template: `
    <div class="home">
      <div class="home-main">
        <h1>Home</h1>
        <p>This is your homepage.</p>
        <button (click)="logout()">Logout</button>
      </div>
      <app-overview-sidebar></app-overview-sidebar>
    </div>
  `,
  styles: [`
    .home {
      display: flex;
      gap: 24px;
      padding: 3rem;
      align-items: stretch;
      min-height: 100vh;
    }

    .home-main {
      flex: 1;
      text-align: center;
    }

    @media (max-width: 900px) {
      .home {
        flex-direction: column;
      }
    }
  `]
})
export class HomeComponent {
  constructor(private auth: AuthService) {}

  logout() {
    this.auth.logout();
  }
}