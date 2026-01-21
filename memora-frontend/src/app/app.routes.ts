import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
<<<<<<< HEAD
import { LoginComponent } from './user/login/login';
import { RegisterComponent } from './user/register/register';
import { HomeComponent } from './home/home.component';
import { GroupsPageComponent } from './groups/groups-page/groups-page';
import { GroupDetailComponent } from './groups/group-detail/group-detail';
import { SettingsComponent } from './user/settings/settings';
=======
import { LoginComponent } from './auth/login/login.component';
import { RegisterComponent } from './auth/register/register.component';
import { HomeComponent } from './home/home.component';
>>>>>>> origin/main

export const routes: Routes = [
    { path: '', redirectTo: 'login', pathMatch: 'full' },

    { path: 'login', component: LoginComponent },
    { path: 'register', component: RegisterComponent },
<<<<<<< HEAD

    { path: 'home', component: HomeComponent },

    { path: 'groups', component: GroupsPageComponent },
    { path: 'groups/:id', component: GroupDetailComponent },
    
    { path: 'settings', component: SettingsComponent },

    { path: '**', redirectTo: 'login' },
=======
    { path: 'home', component: HomeComponent },
    { path: '**', redirectTo: 'login' }
>>>>>>> origin/main
];

@NgModule({
  imports: [RouterModule.forRoot(routes)],
  exports: [RouterModule]
})
export class AppRoutingModule {}