import { Component, OnDestroy, OnInit } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { Subscription } from 'rxjs';
import { GroupAdminService, GroupDetailDto } from '../../services/admin';
import { CommonModule } from '@angular/common';
import { FormsModule, ReactiveFormsModule } from '@angular/forms';

@Component({
  selector: 'app-group-admin-settings',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    FormsModule
  ],
  templateUrl: './settings.html',
  styleUrls: ['./settings.css']
})
export class SettingsComponent implements OnInit, OnDestroy {
  groupId!: string;
  group?: GroupDetailDto;

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
      this.service.getGroup(this.groupId).subscribe({
        next: (g) => {
          this.group = g;
          this.loading = false;
        },
        error: (err) => {
          console.error(err);
          this.error = 'Konnte Gruppe nicht laden.';
          this.loading = false;
        }
      })
    );
  }

  // PLACEHOLDER actions
  renameGroup(): void {
    alert('PLACEHOLDER: Update-Group Endpoint fehlt im Backend.');
  }

  regenerateInvite(): void {
    alert('PLACEHOLDER: Regenerate-Invite Endpoint fehlt im Backend.');
  }

  deleteGroup(): void {
    alert('PLACEHOLDER: Delete-Group Endpoint fehlt im Backend.');
  }

  ngOnDestroy(): void {
    this.sub.unsubscribe();
  }
}
