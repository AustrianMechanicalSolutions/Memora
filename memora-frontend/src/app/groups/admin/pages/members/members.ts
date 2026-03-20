import { Component, OnDestroy, OnInit } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { Subscription } from 'rxjs';
import { GroupAdminService, GroupMemberDto } from '../../services/admin';
import { CommonModule } from '@angular/common';
import { FormsModule, ReactiveFormsModule } from '@angular/forms';
import { TranslatePipe } from '../../../../translate.pipe';

@Component({
  selector: 'app-group-admin-members',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    FormsModule,
    TranslatePipe
  ],
  templateUrl: './members.html',
  styleUrls: ['./members.css']
})
export class MembersComponent implements OnInit, OnDestroy {
  groupId!: string;
  members: GroupMemberDto[] = [];
  loading = true;
  error?: string;

  filter = '';

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
      this.service.getMembers(this.groupId).subscribe({
        next: (res) => {
          this.members = res;
          this.loading = false;
        },
        error: (err) => {
          console.error(err);
          this.error = 'Konnte Mitglieder nicht laden.';
          this.loading = false;
        }
      })
    );
  }

  get filtered(): GroupMemberDto[] {
    const s = this.filter.trim().toLowerCase();
    if (!s) return this.members;

    return this.members.filter(m =>
      (m.displayName || '').toLowerCase().includes(s) ||
      (m.role || '').toLowerCase().includes(s)
    );
  }

  changeRole(m: GroupMemberDto): void {
    const newRole = m.role === 'Admin' ? 'Member' : 'Admin';

    this.service.changeMemberRole(this.groupId, m.userId, newRole).subscribe({
      next: () => this.load(),
      error: err => console.error(err)
    });
  }

  remove(m: GroupMemberDto): void {
    const confirmDelete = confirm(`Remove ${m.displayName}?`);
    if (!confirmDelete) return;

    this.service.removeMember(this.groupId, m.userId).subscribe({
      next: () => this.load(),
      error: err => console.error(err)
    });
  }

  ngOnDestroy(): void {
    this.sub.unsubscribe();
  }
}
