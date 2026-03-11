import { Routes } from '@angular/router';
import { GroupAdminShellComponent } from './pages/group-admin-shell/group-admin-shell';
import { DashboardComponent } from './pages/dashboard/dashboard';
import { MembersComponent } from './pages/members/members';
import { MemoriesComponent } from './pages/memories/memories';
import { AlbumsComponent } from './pages/albums/albums';
import { SettingsComponent } from './pages/settings/settings';
import { ModerationComponent } from './pages/moderation/moderation';

export const ADMIN_ROUTES: Routes = [
  {
    path: '',
    component: GroupAdminShellComponent,
    children: [
      { path: '', redirectTo: 'dashboard', pathMatch: 'full' },
      { path: 'dashboard', component: DashboardComponent },
      { path: 'members', component: MembersComponent },
      { path: 'memories', component: MemoriesComponent },
      { path: 'albums', component: AlbumsComponent },
      { path: 'settings', component: SettingsComponent },
      { path: 'moderation', component: ModerationComponent },
    ]
  }
];
