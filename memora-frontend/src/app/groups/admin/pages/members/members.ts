import { Component, OnDestroy, OnInit } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { Subscription } from 'rxjs';
import { GroupAdminService, GroupMemberDto } from '../../services/admin';
import { CommonModule } from '@angular/common';
import { FormsModule, ReactiveFormsModule } from '@angular/forms';

@Component({
  selector: 'app-group-admin-members',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    FormsModule
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
    return this.members.filter(m => (m.displayName || '').toLowerCase().includes(s));
  }

  changeRole(m: GroupMemberDto): void {
    alert('PLACEHOLDER: Role-Change Endpoint fehlt im Backend.');
  }

  remove(m: GroupMemberDto): void {
    alert('PLACEHOLDER: Remove-Member Endpoint fehlt im Backend.');
  }

  ngOnDestroy(): void {
    this.sub.unsubscribe();
  }
}
