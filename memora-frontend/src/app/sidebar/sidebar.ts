import { Component, OnInit, OnDestroy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { AuthService, CurrentUser } from '../user/auth.service';
import { Router } from '@angular/router';
import { GroupsService, GroupListItemDto } from '../groups/groups';
import { Subscription } from 'rxjs';
import { ThemeService } from '../theme.service';

@Component({
  selector: 'app-sidebar',
  standalone: true,
  imports: [CommonModule, RouterModule],
  templateUrl: './sidebar.html',
  styleUrl: './sidebar.css'
})
export class SidebarComponent implements OnInit, OnDestroy {
  userProfileImageUrl: string | null = null;
  currentUser: CurrentUser | null = null;
  groups: GroupListItemDto[] = [];
  private subscriptions: Subscription = new Subscription();

  constructor(
    private auth: AuthService,
    private router: Router,
    private groupsService: GroupsService,
    private theme: ThemeService
  ) {}

  get themeMode() {
    return this.theme.current;
  }

  toggleTheme() {
    this.theme.toggleTheme();
  }

  ngOnInit() {
    this.loadUserProfile();
    this.loadGroups();
    
    this.subscriptions.add(
      this.groupsService.groupsChanged$.subscribe(() => {
        this.loadGroups();
      })
    );
  }

  loadUserProfile() {
    this.auth.currentUser().subscribe({
      next: (user) => {
        this.currentUser = user;
        this.userProfileImageUrl = user.profileImageUrl || null;
      },
      error: (err) => {
        console.error('Failed to load user profile:', err);
      }
    });
  }

  loadGroups() {
    this.groupsService.myGroups().subscribe({
      next: (groups) => {
        this.groups = groups.slice(0, 5);
      },
      error: (err) => {
        console.error('Failed to load groups:', err);
      }
    });
  }

  logout() {
    if (this.auth.confirmLogout()) {
      this.auth.logout();
      this.router.navigate(['/login']);
    }
  }

  ngOnDestroy() {
    this.subscriptions.unsubscribe();
  }
}
