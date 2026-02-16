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
  templateUrl: './home.component.html',
  styleUrls: ['./home.component.css']
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