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

export const routes: Routes = [
    { path: '', redirectTo: 'login', pathMatch: 'full' },

    { path: 'login', component: LoginComponent },
    { path: 'register', component: RegisterComponent },

    { path: 'home', component: HomeComponent },

    { path: 'groups', component: GroupsPageComponent },
    { path: 'groups/:id', component: GroupDetailComponent },

    { path: 'groups/:id/albums', component: GroupAlbumsComponent },
    { path: 'groups/:id/albums/:albumIds', component: AlbumDetailComponent },
    
    { path: 'settings', component: SettingsComponent },

    { path: '**', redirectTo: 'login' },
];

@NgModule({
  imports: [RouterModule.forRoot(routes)],
  exports: [RouterModule]
})
export class AppRoutingModule {}