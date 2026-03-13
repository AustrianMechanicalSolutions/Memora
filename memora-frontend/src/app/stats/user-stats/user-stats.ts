import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { HttpClient } from '@angular/common/http';
import { forkJoin, of } from 'rxjs';
import { catchError, map, switchMap } from 'rxjs/operators';
import { GroupsService, MemoryDto } from '../../groups/groups';

interface UserMeDto {
  id: string;
  displayName: string;
}

interface StatTile {
  label: string;
  value: string;
  hint?: string;
  placeholder?: boolean;
}

interface GroupSummary {
  id: string;
  name: string;
}

@Component({
  selector: 'app-user-stats-page',
  standalone: true,
  imports: [CommonModule, RouterLink],
  templateUrl: './user-stats.html',
  styleUrls: ['./user-stats.css']
})
export class UserStatsPageComponent {
  loading = true;
  error = '';

  isGroupMode = false;
  groupId: string | null = null;
  groupName = '';
  userDisplayName = 'You';

  tiles: StatTile[] = [];
  groupSwitchLinks: GroupSummary[] = [];

  activityBars = [
    { label: 'Quotes', value: 0 },
    { label: 'Photos', value: 0 },
    { label: 'Videos', value: 0 }
  ];

  constructor(
    private readonly route: ActivatedRoute,
    private readonly http: HttpClient,
    private readonly groupsService: GroupsService
  ) {}

  ngOnInit() {
    this.groupId = this.route.snapshot.paramMap.get('id');
    this.isGroupMode = !!this.groupId;
    this.load();
  }

  private load() {
    this.loading = true;
    this.error = '';

    this.http.get<UserMeDto>('/api/account/me').pipe(
      switchMap((me) => {
        this.userDisplayName = me.displayName?.trim() || 'You';

        if (this.groupId) {
          return forkJoin({
            group: this.groupsService.groupDetail(this.groupId),
            memories: this.collectGroupMemories(this.groupId)
          }).pipe(
            map(({ group, memories }) => {
              this.groupName = group.name;
              this.groupSwitchLinks = [{ id: group.id, name: group.name }];
              return this.computeStats(memories, me.id, true);
            })
          );
        }

        return this.groupsService.myGroups().pipe(
          switchMap((groups) => {
            this.groupSwitchLinks = groups.map((g) => ({ id: g.id, name: g.name }));

            if (groups.length === 0) {
              return of(this.computeStats([], me.id, false));
            }

            return forkJoin(
              groups.map((g) => this.collectGroupMemories(g.id))
            ).pipe(
              map((memoryBatches) => this.computeStats(memoryBatches.flat(), me.id, false))
            );
          })
        );
      }),
      catchError((err) => {
        console.error(err);
        this.error = 'Stats konnten nicht geladen werden.';
        return of(null);
      })
    ).subscribe((result) => {
      if (result) {
        this.tiles = result.tiles;
        this.activityBars = result.bars;
      }

      this.loading = false;
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

  private computeStats(memories: MemoryDto[], userId: string, isGroup: boolean) {
    const own = memories.filter((m) => m.createdByUserId === userId);
    const quoteCount = own.filter((m) => m.type === 2).length;
    const photoCount = own.filter((m) => m.type === 0).length;
    const videoCount = own.filter((m) => m.type === 1).length;
    const postCount = own.length;

    // Like data does not exist yet in backend -> deterministic placeholder.
    const likesTotal = postCount === 0 ? 0 : Math.round(postCount * 1.8 + quoteCount * 0.7 + 9);
    const likesPerPost = postCount === 0 ? 0 : likesTotal / postCount;

    const groupPosts = memories.length;
    const contributionShare = groupPosts === 0 ? 0 : (postCount / groupPosts) * 100;

    const scopeLabel = isGroup ? 'in dieser Gruppe' : 'gruppenuebergreifend';

    const tiles: StatTile[] = [
      { label: `Posts ${scopeLabel}`, value: `${postCount}` },
      { label: `Zitate gepostet ${scopeLabel}`, value: `${quoteCount}` },
      { label: `Likes gesamt ${scopeLabel}`, value: `${likesTotal}`, hint: 'Placeholder', placeholder: true },
      { label: 'Likes/Post Ratio', value: likesPerPost.toFixed(2), hint: 'Placeholder', placeholder: true },
      { label: 'Photos', value: `${photoCount}` },
      { label: 'Videos', value: `${videoCount}` },
      { label: 'Anteil deiner Posts', value: `${contributionShare.toFixed(1)}%` }
    ];

    const bars = [
      { label: 'Quotes', value: quoteCount },
      { label: 'Photos', value: photoCount },
      { label: 'Videos', value: videoCount }
    ];

    return { tiles, bars };
  }
}
