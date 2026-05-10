import { Component, OnDestroy, OnInit } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { Subscription } from 'rxjs';
import { GroupAdminService, GroupDetailInfoDto } from '../../services/admin';
import { CommonModule } from '@angular/common';
import { FormsModule, ReactiveFormsModule } from '@angular/forms';
import { TranslatePipe } from '../../../../translation/translate.pipe';

@Component({
  selector: 'app-group-admin-settings',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, FormsModule, TranslatePipe],
  templateUrl: './settings.html',
  styleUrls: ['./settings.css']
})
export class SettingsComponent implements OnInit, OnDestroy {
  groupId!: string;
  group?: GroupDetailInfoDto;

  loading = true;
  error?: string;

  // ── Rename ──
  showRenameModal = false;
  renameValue = '';
  renameLoading = false;

  // ── Regenerate invite ──
  showRegenerateModal = false;
  regenerateLoading = false;

  // ── Delete ──
  showDeleteModal = false;
  deleteConfirmValue = '';
  deleteLoading = false;

  private sub = new Subscription();

  constructor(
    private route: ActivatedRoute,
    private router: Router,
    private service: GroupAdminService
  ) {}

  ngOnInit(): void {
    this.groupId = this.route.parent?.snapshot.paramMap.get('id')!;
    this.load();
  }

  load(): void {
    this.loading = true;
    this.error = undefined;

    this.sub.add(
      this.service.getGroupSettings(this.groupId).subscribe({
        next: (g) => { this.group = g; this.loading = false; },
        error: (err) => {
          console.error(err);
          this.error = 'Could not load group.';
          this.loading = false;
        }
      })
    )
  }

  // ── Copy invite code ──
  copyInviteCode(): void {
    console.log(this.group);
    if (this.group?.inviteCode) {
      navigator.clipboard.writeText(this.group.inviteCode).catch(console.error);
    }
  }

  // ── Modal helpers ──
  closeModals(): void {
    this.showRenameModal = false;
    this.showRegenerateModal = false;
    this.showDeleteModal = false;
    this.renameValue = '';
    this.deleteConfirmValue = '';
  }

  // ── Rename ──
  openRename(): void {
    this.renameValue = this.group?.name ?? '';
    this.showRenameModal = true;
  }

  submitRename(): void {
    const name = this.renameValue.trim();
    if (!name || this.renameLoading) return;

    this.renameLoading = true;
    this.sub.add(
      this.service.renameGroup(this.groupId, name).subscribe({
        next: () => {
          if (this.group) this.group = { ...this.group, name };
          this.renameLoading = false;
          this.closeModals();
        },
        error: (err) => {
          console.error(err);
          this.renameLoading = false;
          // Surface error inline so modal stays open
          this.error = 'Failed to rename group.';
        }
      })
    );
  }

  // ── Regenerate invite ──
  openRegenerateInvite(): void {
    this.showRegenerateModal = true;
  }

  submitRegenerateInvite(): void {
    if (this.regenerateLoading) return;
    this.regenerateLoading = true;

    this.sub.add(
      this.service.regenerateInviteCode(this.groupId).subscribe({
        next: (updated) => {
          if (this.group) this.group = { ...this.group, inviteCode: updated.inviteCode };
          this.regenerateLoading = false;
          this.closeModals();
        },
        error: (err) => {
          console.error(err);
          this.regenerateLoading = false;
          this.error = 'Failed to regenerate invite code.';
        }
      })
    );
  }

  // ── Delete ──
  openDeleteGroup(): void {
    this.deleteConfirmValue = '';
    this.showDeleteModal = true;
  }

  submitDeleteGroup(): void {
    if (this.deleteConfirmValue !== this.group?.name || this.deleteLoading) return;
    this.deleteLoading = true;

    this.sub.add(
      this.service.deleteGroup(this.groupId).subscribe({
        next: () => {
          this.deleteLoading = false;
          this.router.navigate(['/']);
        },
        error: (err) => {
          console.error(err);
          this.deleteLoading = false;
          this.error = 'Failed to delete group.';
          this.closeModals();
        }
      })
    );
  }

  ngOnDestroy(): void {
    this.sub.unsubscribe();
  }
}