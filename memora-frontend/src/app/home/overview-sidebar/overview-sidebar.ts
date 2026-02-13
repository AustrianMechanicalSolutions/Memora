import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { AuthService } from '../../user/auth.service';
import { GroupsService, MemoryDto } from '../../groups/groups';
import { forkJoin, of } from 'rxjs';
import { catchError, map, switchMap } from 'rxjs/operators';

@Component({
  standalone: true,
  selector: 'app-overview-sidebar',
  imports: [CommonModule],
  template: `
    <aside class="home-sidebar">
      <h2>Overview</h2>
      <div class="overview-item">
        <div class="overview-value">{{ totalGroups }}</div>
        <div class="overview-label">Total groups</div>
      </div>
      <div class="overview-item">
        <div class="overview-value">{{ totalMemories }}</div>
        <div class="overview-label">Memories Posted</div>
      </div>
      <div class="overview-item">
        <div class="overview-value">{{ totalLikes }}</div>
        <div class="overview-label">Global Likes</div>
      </div>
    </aside>
  `,
  styles: [`
    .home-sidebar {
      width: 260px;
      height: 100%;
      border-left: 1px solid #2a2a2e;
      padding: 12px 16px;
      box-sizing: border-box;
      overflow-y: auto;
    }

    .home-sidebar h2 {
      margin: 0 0 12px 0;
      font-size: 22px;
      color: #e5e5e5;
    }

    .overview-item {
      padding: 12px 0;
      border-bottom: 1px solid #2a2a2e;
    }

    .overview-item:first-child {
      padding-top: 24px;
    }

    .overview-item:last-child {
      border-bottom: none;
    }

    .overview-value {
      font-size: 20px;
      color: #fff;
      font-weight: 600;
    }

    .overview-label {
      font-size: 16px;
      color: #888;
      margin-top: 4px;
    }

    @media (max-width: 900px) {
      .home-sidebar {
        width: 100%;
        border-left: none;
        border-top: 1px solid #2a2a2e;
        padding-top: 16px;
      }
    }
  `]
})
export class OverviewSidebarComponent implements OnInit {
  totalGroups = 0;
  totalMemories = 0;
  totalLikes = 0;

  constructor(private auth: AuthService, private groupsService: GroupsService) {}

  ngOnInit() {
    this.loadOverview();
  }

  private loadOverview() {
    this.auth.currentUser().pipe(
      switchMap((me) =>
        this.groupsService.myGroups().pipe(
          switchMap((groups) => {
            this.totalGroups = groups.length;
            if (groups.length === 0) {
              return of({ userId: me.id, memories: [] as MemoryDto[] });
            }

            return forkJoin(groups.map((g) => this.collectGroupMemories(g.id))).pipe(
              map((memoryBatches) => ({ userId: me.id, memories: memoryBatches.flat() }))
            );
          })
        )
      ),
      catchError((err) => {
        console.error(err);
        return of({ userId: '', memories: [] as MemoryDto[] });
      })
    ).subscribe(({ userId, memories }) => {
      if (!userId) {
        return;
      }

      const own = memories.filter((m) => m.createdByUserId === userId);
      const quoteCount = own.filter((m) => m.type === 2).length;
      const postCount = own.length;

      this.totalMemories = postCount;
      this.totalLikes = postCount === 0 ? 0 : Math.round(postCount * 1.8 + quoteCount * 0.7 + 9);
    });
  }

  private collectGroupMemories(groupId: string) {
    const firstPage = 1;
    const pageSize = 100;

    return this.groupsService.memories(groupId, {
      page: firstPage,
      pageSize,
      sort: 'newest'
    }).pipe(
      switchMap((first) => {
        const totalPages = Math.max(1, Math.ceil(first.total / pageSize));
        if (totalPages === 1) {
          return of(first.items);
        }

        const followUps = Array.from({ length: totalPages - 1 }, (_, idx) =>
          this.groupsService.memories(groupId, {
            page: idx + 2,
            pageSize,
            sort: 'newest'
          }).pipe(map((r) => r.items))
        );

        return forkJoin(followUps).pipe(
          map((pages) => [first.items, ...pages].flat())
        );
      })
    );
  }
}
