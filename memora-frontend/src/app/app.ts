import { Component, signal, OnInit, OnDestroy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { SidebarComponent } from './sidebar/sidebar';
import { AuthService } from './user/auth.service';
import { ThemeService } from './theme.service';
import { Subject } from 'rxjs';
import { takeUntil } from 'rxjs/operators';
import { RouterLink, RouterOutlet } from '@angular/router';

@Component({
  selector: 'app-root',
  imports: [CommonModule, RouterOutlet, SidebarComponent, RouterLink],
  templateUrl: './app.html',
  styleUrl: './app.css'
})
export class App implements OnInit, OnDestroy {
  protected readonly title = signal('memora-frontend');
  isLoggedIn = signal(false);
  private destroy$ = new Subject<void>();

  constructor(private auth: AuthService, private theme: ThemeService) {}

  ngOnInit() {
    this.isLoggedIn.set(this.auth.isLoggedIn());
    this.auth.loggedIn$
      .pipe(takeUntil(this.destroy$))
      .subscribe(loggedIn => this.isLoggedIn.set(loggedIn));
  }

  ngOnDestroy() {
    this.destroy$.next();
    this.destroy$.complete();
  }
}
