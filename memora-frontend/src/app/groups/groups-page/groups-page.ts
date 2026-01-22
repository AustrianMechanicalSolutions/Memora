import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { FormsModule } from '@angular/forms';

import { GroupsService, GroupListItemDto } from '../groups';

@Component({
  selector: 'app-groups-page',
  standalone: true,
  imports: [CommonModule, RouterModule, FormsModule],
  templateUrl: './groups-page.html',
  styleUrls: ['./groups-page.css']
})
export class GroupsPageComponent {
  groups: GroupListItemDto[] = [];
  loading = true;

  // create group form
  newGroupName = '';
  creating = false;
  errorMsg = '';

  // join group from
  inviteCode = ''
  joining = false;
  joinErrorMsg = '';

  constructor(private groupsService: GroupsService) {}

  ngOnInit() {
    this.loadGroups();
  }

  loadGroups() {
    this.loading = true;
    this.groupsService.myGroups().subscribe({
      next: (data) => {
        this.groups = data;
        this.loading = false;
      },
      error: (err) => {
        console.error(err);
        this.loading = false;
      }
    });
  }

  createGroup() {
    const name = this.newGroupName.trim();
    if (!name) return;

    this.creating = true;
    this.errorMsg = '';

    this.groupsService.createGroup(name).subscribe({
      next: () => {
        this.newGroupName = '';
        this.creating = false;
        this.loadGroups(); // ✅ refresh list
      },
      error: (err) => {
        console.error(err);
        this.creating = false;
        this.errorMsg = err?.error ?? 'Could not create group';
      }
    });
  }

  joinGroup() {
    const code = this.inviteCode.trim();
    if (!code) return;

    this.joining = true;
    this.joinErrorMsg = '';

    this.groupsService.joinGroup(code).subscribe({
      next: () => {
        this.inviteCode = '';
        this.joining = false;
        this.loadGroups();
      },
      error: (err) => {
        console.error(err);
        this.joining = false;
        this.joinErrorMsg = err?.error ?? 'Could not join group';
      }
    });
  }
}
