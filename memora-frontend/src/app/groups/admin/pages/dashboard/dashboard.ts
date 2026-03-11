import { Component, OnDestroy, OnInit } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { Subscription, forkJoin } from 'rxjs';
import {
  GroupAdminService,
  GroupDetailDto,
  GroupMemberActivityDto,
  GroupStatsDto,
  GroupWeeklyActivityDto
} from '../../services/admin';
import { CommonModule } from '@angular/common';
import { FormsModule, ReactiveFormsModule } from '@angular/forms';

@Component({
  selector: 'app-group-admin-dashboard',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    FormsModule
  ],
  templateUrl: './dashboard.html',
  styleUrls: ['./dashboard.css']
})
export class DashboardComponent implements OnInit, OnDestroy {
  groupId!: string;

  stats?: GroupStatsDto;
  weekly?: GroupWeeklyActivityDto;
  memberActivity?: GroupMemberActivityDto[];

  loading = true;
  error?: string;

  private sub = new Subscription();

  constructor(private route: ActivatedRoute, private service: GroupAdminService) {}

  ngOnInit(): void {
    this.groupId = this.route.parent?.snapshot.paramMap.get('id')!;
    this.load();
  }

  load(): void {
    this.loading = true;
    this.error = undefined;

    this.sub.add(
      forkJoin({
        stats: this.service.getStats(this.groupId),
        weekly: this.service.getWeeklyActivity(this.groupId),
        members: this.service.getMemberActivity(this.groupId),
      }).subscribe({
        next: (res) => {
          this.stats = res.stats;
          this.weekly = res.weekly;
          this.memberActivity = res.members;
          this.loading = false;
        },
        error: (err) => {
          console.error(err);
          this.error = 'Konnte Dashboard Daten nicht laden.';
          this.loading = false;
        }
      })
    );
  }

  ngOnDestroy(): void {
    this.sub.unsubscribe();
  }
}
