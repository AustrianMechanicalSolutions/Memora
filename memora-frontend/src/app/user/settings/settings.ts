import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { HttpClient } from '@angular/common/http';

import QRCode from 'qrcode';
import { TwoFactorService, TwoFactorSetupResponse } from './twofactor';

@Component({
  selector: 'app-settings',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './settings.html',
  styleUrls: ['./settings.css']
})
export class SettingsComponent {
  private api = 'http://localhost:5000/api/account';

  loading = true;
  saving = false;

  msg = '';
  err = '';

  profile: any = {
    displayName: '',
    bio: '',
    status: '',
    birthDate: '',
    profileImageUrl: '',
    phoneNumber: '',
    discordTag: '',
    instagramUrl: '',
    tiktokUrl: '',
    youtubeUrl: '',
    websiteUrl: ''
  };

  password = {
    currentPassword: '',
    newPassword: ''
  };

  // ===== 2FA UI state =====
  twoFaLoading = false;
  twoFaEnabled = false; // will be set from /me if you add it there
  twoFaSecret = '';
  twoFaOtpAuthUrl = '';
  twoFaQrDataUrl = '';
  twoFaCode = '';

  constructor(
    private http: HttpClient,
    private twoFactor: TwoFactorService
  ) {}

  ngOnInit() {
    this.http.get<any>(`${this.api}/me`).subscribe({
      next: (data) => {
        this.profile.displayName = data.displayName ?? '';
        this.profile.bio = data.bio ?? '';
        this.profile.status = data.status ?? '';
        this.profile.birthDate = data.birthDate ? data.birthDate.slice(0, 10) : '';
        this.profile.profileImageUrl = data.profileImageUrl ?? '';
        this.profile.phoneNumber = data.phoneNumber ?? '';
        this.profile.discordTag = data.discordTag ?? '';
        this.profile.instagramUrl = data.instagramUrl ?? '';
        this.profile.tiktokUrl = data.tiktokUrl ?? '';
        this.profile.youtubeUrl = data.youtubeUrl ?? '';
        this.profile.websiteUrl = data.websiteUrl ?? '';

        // OPTIONAL: if backend returns it
        this.twoFaEnabled = data.twoFactorEnabled ?? false;

        this.loading = false;
      },
      error: (e) => {
        console.error(e);
        this.err = 'Could not load profile.';
        this.loading = false;
      }
    });
  }

  clearMessages() {
    this.msg = '';
    this.err = '';
  }

  saveProfile() {
    this.saving = true;
    this.clearMessages();

    const body = {
      displayName: this.profile.displayName,
      bio: this.profile.bio,
      status: this.profile.status,
      birthDate: this.profile.birthDate ? new Date(this.profile.birthDate).toISOString() : null,
      profileImageUrl: this.profile.profileImageUrl,

      phoneNumber: this.profile.phoneNumber,
      discordTag: this.profile.discordTag,

      instagramUrl: this.profile.instagramUrl,
      tiktokUrl: this.profile.tiktokUrl,
      youtubeUrl: this.profile.youtubeUrl,
      websiteUrl: this.profile.websiteUrl
    };

    this.http.put(`${this.api}/profile`, body).subscribe({
      next: () => {
        this.msg = 'Profile saved!';
        this.saving = false;
      },
      error: (e) => {
        console.error(e);
        this.err = 'Could not save profile.';
        this.saving = false;
      }
    });
  }

  changePassword() {
    this.saving = true;
    this.clearMessages();

    this.http.put(`${this.api}/password`, this.password).subscribe({
      next: () => {
        this.msg = 'Password changed!';
        this.password.currentPassword = '';
        this.password.newPassword = '';
        this.saving = false;
      },
      error: (e) => {
        console.error(e);
        this.err = e?.error ?? 'Could not change password.';
        this.saving = false;
      }
    });
  }

  // ===== 2FA actions =====

  async setup2FA() {
    this.twoFaLoading = true;
    this.clearMessages();

    this.twoFaSecret = '';
    this.twoFaOtpAuthUrl = '';
    this.twoFaQrDataUrl = '';
    this.twoFaCode = '';

    this.twoFactor.setup().subscribe({
      next: async (res: TwoFactorSetupResponse) => {
        this.twoFaSecret = res.secret;
        this.twoFaOtpAuthUrl = res.otpauthUrl;

        // Generate QR image
        this.twoFaQrDataUrl = await QRCode.toDataURL(res.otpauthUrl);

        this.twoFaLoading = false;
        this.msg = 'Scan the QR code with Microsoft Authenticator, then enter the 6-digit code.';
      },
      error: (e) => {
        console.error(e);
        this.err = 'Could not start 2FA setup.';
        this.twoFaLoading = false;
      }
    });
  }

  enable2FA() {
    const code = this.twoFaCode.trim();
    if (!code) return;

    this.twoFaLoading = true;
    this.clearMessages();

    this.twoFactor.enable(code).subscribe({
      next: () => {
        this.twoFaEnabled = true;
        this.twoFaLoading = false;
        this.msg = '2FA enabled successfully!';
      },
      error: (e) => {
        console.error(e);
        this.err = e?.error ?? 'Invalid 2FA code.';
        this.twoFaLoading = false;
      }
    });
  }

  disable2FA() {
    this.twoFaLoading = true;
    this.clearMessages();

    this.twoFactor.disable().subscribe({
      next: () => {
        this.twoFaEnabled = false;

        // clear UI
        this.twoFaSecret = '';
        this.twoFaOtpAuthUrl = '';
        this.twoFaQrDataUrl = '';
        this.twoFaCode = '';

        this.twoFaLoading = false;
        this.msg = '2FA disabled.';
      },
      error: (e) => {
        console.error(e);
        this.err = 'Could not disable 2FA.';
        this.twoFaLoading = false;
      }
    });
  }
}
