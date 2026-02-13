import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { AuthService } from '../user/auth.service';
import { GroupsService, GroupListItemDto } from '../groups/groups';
import { OverviewSidebarComponent } from './overview-sidebar/overview-sidebar';
import { forkJoin, of } from 'rxjs';
import { catchError, map, switchMap } from 'rxjs/operators';

interface GroupCardData extends GroupListItemDto {
  imagePreview: string[];
  memoryCount: number;
}

@Component({
  standalone: true,
  selector: 'app-home',
  imports: [CommonModule, RouterLink, OverviewSidebarComponent],
  template: `
    <div class="home">
      <div class="home-main">
        <div class="welcome-section">
          <h1>Welcome, <span class="username">{{ username }}</span></h1>
        </div>
        <div class="section-header">
          <h2>Your Groups</h2>
          <p>Shared memory collections</p>
        </div>
        <div class="groups-dashboard">
          <div class="group-card" *ngFor="let group of groups" [routerLink]="['/groups', group.id]">
            <div class="group-header">
              <h3>{{ group.name }}</h3>
            </div>
            <div class="group-preview" *ngIf="group.imagePreview.length > 0">
              <img *ngFor="let img of group.imagePreview" [src]="img" alt="Memory preview" />
            </div>
            <div class="group-footer">
              <div class="group-stats">
                <span class="stat">{{ group.memberCount }} members</span>
                <span class="stat">{{ group.memoryCount }} memories</span>
              </div>
            </div>
          </div>
        </div>
      </div>
      <app-overview-sidebar></app-overview-sidebar>
    </div>
  `,
  styles: [`
    @keyframes fadeInUp {
      from {
        opacity: 0;
        transform: translateY(20px);
      }
      to {
        opacity: 1;
        transform: translateY(0);
      }
    }

    .home {
      display: flex;
      gap: 0;
      align-items: stretch;
      height: 100vh;
    }

    .home-main {
      flex: 1;
      padding: 3rem;
      overflow-y: auto;
      text-align: left;
    }

    .welcome-section {
      margin-bottom: 3rem;
      animation: fadeInUp 0.6s ease-out forwards;
      opacity: 0;
    }

    .welcome-section h1 {
      font-size: 36px;
      margin: 0;
      color: #fff;
    }

    .username {
      font-weight: 700;
      color: #a855f7;
    }

    .section-header {
      margin-bottom: 2rem;
      animation: fadeInUp 0.6s ease-out 0.1s forwards;
      opacity: 0;
    }

    .section-header h2 {
      font-size: 24px;
      margin: 0 0 4px 0;
      color: #fff;
    }

    .section-header p {
      font-size: 14px;
      margin: 0;
      color: #888;
    }

    .groups-dashboard {
      display: grid;
      grid-template-columns: repeat(auto-fill, minmax(280px, 1fr));
      gap: 20px;
    }

    .group-card {
      background: #111113;
      border: 1px solid #2a2a2e;
      border-radius: 12px;
      overflow: hidden;
      cursor: pointer;
      transition: all 0.2s ease;
      text-decoration: none;
      color: inherit;
      animation: fadeInUp 0.5s ease-out forwards;
      opacity: 0;
    }

    .group-card:nth-child(1) { animation-delay: 0.2s; }
    .group-card:nth-child(2) { animation-delay: 0.3s; }
    .group-card:nth-child(3) { animation-delay: 0.4s; }
    .group-card:nth-child(4) { animation-delay: 0.5s; }
    .group-card:nth-child(5) { animation-delay: 0.6s; }
    .group-card:nth-child(6) { animation-delay: 0.7s; }
    .group-card:nth-child(n+7) { animation-delay: 0.8s; }

    .group-card:hover {
      border-color: #4f46e5;
      box-shadow: 0 0 12px rgba(79, 70, 229, 0.2);
      transform: translateY(-2px);
    }

    .group-header {
      padding: 16px;
      border-bottom: 1px solid #2a2a2e;
    }

    .group-header h3 {
      margin: 0;
      font-size: 18px;
      color: #fff;
    }

    .group-preview {
      display: flex;
      gap: 1px;
      height: 100px;
      background: #0a0a0c;
      overflow: hidden;
    }

    .group-preview img {
      flex: 1;
      object-fit: cover;
      object-position: center;
    }

    .group-footer {
      padding: 16px;
      border-top: 1px solid #2a2a2e;
    }

    .group-stats {
      display: flex;
      gap: 12px;
      font-size: 13px;
      color: #888;
    }

    @media (max-width: 900px) {
      .home {
        flex-direction: column;
      }

      .home-main {
        padding: 3rem;
      }

      .groups-dashboard {
        grid-template-columns: 1fr;
      }
    }
  `]
})
export class HomeComponent implements OnInit {
  username = '';
  groups: GroupCardData[] = [];

  constructor(private auth: AuthService, private groupsService: GroupsService) {}

  ngOnInit() {
    this.loadData();
  }

  private loadData() {
    this.auth.currentUser().pipe(
      switchMap((user) => {
        this.username = user.displayName;
        return this.groupsService.myGroups();
      }),
      switchMap((groups) => {
        if (groups.length === 0) {
          return of([]);
        }
        return forkJoin(groups.map((g) => this.enrichGroupWithData(g)));
      }),
      catchError((err) => {
        console.error(err);
        return of([]);
      })
    ).subscribe((enrichedGroups) => {
      this.groups = enrichedGroups;
    });
  }

  private enrichGroupWithData(group: GroupListItemDto) {
    return this.groupsService
      .memories(group.id, { page: 1, pageSize: 10, sort: 'newest' })
      .pipe(
        map((result) => {
          const imageMemories = result.items
            .filter((m) => m.type === 0 || m.type === 1)
            .slice(0, 3);
          return {
            ...group,
            imagePreview: imageMemories.map((m) => m.thumbUrl || m.mediaUrl || '').filter((url) => !!url),
            memoryCount: result.total
          };
        }),
        catchError(() =>
          of({
            ...group,
            imagePreview: [],
            memoryCount: 0
          })
        )
      );
  }

  logout() {
    this.auth.logout();
  }
}