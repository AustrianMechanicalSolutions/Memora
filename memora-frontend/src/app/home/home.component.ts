import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { AuthService } from '../user/auth.service';
import { GroupsService, GroupListItemDto } from '../groups/groups';
import { OverviewSidebarComponent } from './overview-sidebar/overview-sidebar';
import { forkJoin, of } from 'rxjs';
import { catchError, map, switchMap } from 'rxjs/operators';
import { TranslatePipe } from '../translation/translate.pipe';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../environment';

interface GroupCardData extends GroupListItemDto {
  imagePreview: (string | null)[];
  memoryCount: number;
}

@Component({
  standalone: true,
  selector: 'app-home',
  imports: [CommonModule, RouterLink, OverviewSidebarComponent, TranslatePipe],
  templateUrl: './home.component.html',
  styleUrls: ['./home.component.css']
})
export class HomeComponent implements OnInit {
  username = '';
  groups: GroupCardData[] = [];

  // blob
  imageSrcMap = new Map<string, string>();
  loadingSet = new Set<string>();

  constructor(private auth: AuthService, private groupsService: GroupsService, private http: HttpClient) {}

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
          const previews = imageMemories
            .map((m) => m.thumbUrl || m.mediaUrl)
            .filter((url): url is string => !!url);

          previews.forEach(url => this.loadMedia(url));

          return {
            ...group,
            imagePreview: previews,
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

  private buildMediaUrl(url?: string | null): string | null {
    if (!url) return null;

    const token = localStorage.getItem('token');
    return `${location.origin === 'http://localhost:4200'
      ? 'http://localhost:5000'
      : 'https://api.memora.at'}${url}?token=${token}`;
  }

  logout() {
    this.auth.logout();
  }

  private loadMedia(url?: string | null) {
    if (!url || this.imageSrcMap.has(url) || this.loadingSet.has(url)) return;

    this.loadingSet.add(url);

    const fullUrl = environment.apiUrl + url;

    this.http.get(fullUrl, { responseType: 'blob' }).subscribe({
      next: (blob: Blob) => {
        const objectUrl = URL.createObjectURL(blob);
        this.imageSrcMap.set(url, objectUrl);
        this.loadingSet.delete(url);
      },
      error: (err: unknown) => {
        console.error('Media load failed', err);
        this.loadingSet.delete(url);
      }
    });
  }
}
