import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { HttpClient } from '@angular/common/http';
import { RouterModule } from '@angular/router';

import QRCode from 'qrcode';
import { TwoFactorService, TwoFactorSetupResponse } from './twofactor';
import { ThemeService } from '../../theme.service';
import { TranslatePipe } from '../../translate.pipe';
import { AppLanguage, I18nService } from '../../i18n.service';
import { AuthService } from '../auth.service';
import { environment } from '../../../environment';

@Component({
  selector: 'app-settings',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterModule, TranslatePipe],
  templateUrl: './settings.html',
  styleUrls: ['./settings.css']
})
export class SettingsComponent {
  private api = environment.apiUrl + '/api/account';

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
  twoFaEnabled = false;
  twoFaSecret = '';
  twoFaOtpAuthUrl = '';
  twoFaQrDataUrl = '';
  twoFaCode = '';

  constructor(
    private http: HttpClient,
    private twoFactor: TwoFactorService,
    private theme: ThemeService,
    private i18n: I18nService,
    private auth: AuthService
  ) {}

  get themeMode() {
    return this.theme.current;
  }

  get language(): AppLanguage {
    return this.i18n.currentLanguage;
  }

  toggleTheme() {
    this.theme.toggleTheme();
  }

  setLanguage(language: AppLanguage) {
    this.i18n.setLanguage(language);
  }

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

        this.twoFaEnabled = data.twoFactorEnabled ?? false;

        this.loading = false;
      },
      error: (e) => {
        console.error(e);
        this.err = this.i18n.translate('settings.profileLoadFailed');
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
        this.msg = this.i18n.translate('settings.profileSaved');
        this.saving = false;
        this.auth.notifyProfileChanged();
      },
      error: (e) => {
        console.error(e);
        this.err = this.i18n.translate('settings.profileSaveFailed');
        this.saving = false;
      }
    });
  }

  changePassword() {
    this.saving = true;
    this.clearMessages();

    this.http.put(`${this.api}/password`, this.password).subscribe({
      next: () => {
        this.msg = this.i18n.translate('settings.passwordChanged');
        this.password.currentPassword = '';
        this.password.newPassword = '';
        this.saving = false;
      },
      error: (e) => {
        console.error(e);
        this.err = e?.error ?? this.i18n.translate('settings.passwordChangeFailed');
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
        this.msg = this.i18n.translate('settings.twoFactorSetupHint');
      },
      error: (e) => {
        console.error(e);
        this.err = this.i18n.translate('settings.twoFactorSetupFailed');
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
        this.msg = this.i18n.translate('settings.twoFactorEnabled');
      },
      error: (e) => {
        console.error(e);
        this.err = e?.error ?? this.i18n.translate('settings.twoFactorInvalid');
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
        this.msg = this.i18n.translate('settings.twoFactorDisabled');
      },
      error: (e) => {
        console.error(e);
        this.err = this.i18n.translate('settings.twoFactorDisableFailed');
        this.twoFaLoading = false;
      }
    });
  }

  // Profile image
  onProfileImageSelected(event: Event) {
    const input = event.target as HTMLInputElement;
    if (!input.files || input.files.length === 0) return;

    const file = input.files[0];

    if (!file.type.startsWith('image/')) {
      this.err = this.i18n.translate('settings.selectImage');
      return;
    }

    const reader = new FileReader();
    reader.onload = () => {
      this.profile.profileImageUrl = reader.result as string;
    };

    reader.readAsDataURL(file);
  }

  removeProfileImage() {
    this.profile.profileImageUrl = '';
  }
}
