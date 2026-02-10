import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { AuthService, CurrentUser } from '../user/auth.service';
import { Router } from '@angular/router';

@Component({
  selector: 'app-sidebar',
  standalone: true,
  imports: [CommonModule, RouterModule],
  templateUrl: './sidebar.html',
  styleUrl: './sidebar.css'
})
export class SidebarComponent implements OnInit {
  userProfileImageUrl: string | null = null;
  currentUser: CurrentUser | null = null;

  constructor(private auth: AuthService, private router: Router) {}

  ngOnInit() {
    this.loadUserProfile();
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

  logout() {
    if (this.auth.confirmLogout()) {
      this.auth.logout();
      this.router.navigate(['/login']);
    }
  }
}
