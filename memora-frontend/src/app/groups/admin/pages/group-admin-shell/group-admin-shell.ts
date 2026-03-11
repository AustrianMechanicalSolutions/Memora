import { Component, OnDestroy, OnInit } from '@angular/core';
import { ActivatedRoute, NavigationEnd, Router, RouterModule } from '@angular/router';
import { filter, Subscription } from 'rxjs';
import { GroupAdminService, GroupDetailDto } from '../../services/admin';
import { CommonModule } from '@angular/common';
import { FormsModule, ReactiveFormsModule } from '@angular/forms';
import { ModerationComponent } from '../moderation/moderation';
import { SettingsComponent } from '../settings/settings';
import { AlbumsComponent } from '../albums/albums';
import { MemoriesComponent } from '../memories/memories';
import { MembersComponent } from '../members/members';
import { DashboardComponent } from '../dashboard/dashboard';

@Component({
  selector: 'app-group-admin-shell',
  standalone: true,
  imports: [
    CommonModule,
    RouterModule,
    FormsModule
  ],
  templateUrl: './group-admin-shell.html',
  styleUrls: ['./group-admin-shell.css']
})
export class GroupAdminShellComponent implements OnInit, OnDestroy {
  groupId!: string;
  group?: GroupDetailDto;
  activeUrl = '';
  private sub = new Subscription();

  constructor(
    private route: ActivatedRoute,
    private router: Router,
    private service: GroupAdminService
  ) {}

  ngOnInit(): void {
    this.route.paramMap.subscribe(params => {
      const id = params.get('id');
      if (!id) {
        console.error('Group ID missing in route');
        return;
      }

      this.groupId = id;
      this.loadGroup();
    });
  }

  loadGroup() {
    this.service.getGroup(this.groupId).subscribe({
      next: g => this.group = g,
      error: err => console.error(err)
    });
  }

  ngOnDestroy(): void {
    this.sub.unsubscribe();
  }
}
