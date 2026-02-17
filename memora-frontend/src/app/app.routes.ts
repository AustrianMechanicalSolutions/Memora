import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { LoginComponent } from './user/auth/login/login';
import { RegisterComponent } from './user/auth/register/register';
import { HomeComponent } from './home/home.component';
import { GroupsPageComponent } from './groups/groups-page/groups-page';
import { GroupDetailComponent } from './groups/group-detail/group-detail';
import { SettingsComponent } from './user/settings/settings';
import { GroupAlbumsComponent } from './groups/albums/albums';
import { AlbumDetailComponent } from './groups/albums/album-detail/album-detail';
import { UserStatsPageComponent } from './stats/user-stats/user-stats';
import { ImpressumComponent } from './legal/impressum/impressum';
import { authGuard } from './user/auth.guard';
import { guestGuard } from './user/guest.guard';

export const routes: Routes = [
    { path: '', redirectTo: 'home', pathMatch: 'full' },

    { path: 'login', component: LoginComponent, canActivate: [guestGuard] },
    { path: 'register', component: RegisterComponent, canActivate: [guestGuard] },

    { path: 'home', component: HomeComponent, canActivate: [authGuard] },

    { path: 'groups', component: GroupsPageComponent, canActivate: [authGuard] },
    { path: 'groups/:id', component: GroupDetailComponent, canActivate: [authGuard] },

    { path: 'groups/:id/albums', component: GroupAlbumsComponent, canActivate: [authGuard] },
    { path: 'groups/:id/albums/:albumId', component: AlbumDetailComponent, canActivate: [authGuard] },
    { path: 'groups/:id/stats', component: UserStatsPageComponent, canActivate: [authGuard] },
    
    { path: 'settings', component: SettingsComponent, canActivate: [authGuard] },
    { path: 'stats', component: UserStatsPageComponent, canActivate: [authGuard] },
    { path: 'impressum', component: ImpressumComponent },

    { path: '**', redirectTo: 'home' },
];

@NgModule({
  imports: [RouterModule.forRoot(routes)],
  exports: [RouterModule]
})
export class AppRoutingModule {}
