import { NgModule } from '@angular/core';
import { Routes, RouterModule } from '@angular/router';

import { LoginComponent } from './login/login.component';
import { HomeComponent } from './home/home.component';
import { AuthGuard } from './_guards/auth.guard';
import { UserManagerComponent } from './user-manager/user-manager.component';
import { RestoreComponent } from './restore/restore.component';
import { ServerComponent } from './server/server.component';
import { EntitiesComponent } from './entities/entities.component';
import { TimetableComponent } from './timetable/timetable.component';
import { GalaxyMapComponent } from './galaxy-map/galaxy-map.component';
import { RestoreFactoryItemsComponent } from './restore-factoryitems/restore-factoryitems.component';
import { StructuresListComponent } from './structures-list/structures-list.component';

const appRoutes: Routes = [
  {
    path: '',
    component: HomeComponent,
    canActivate: [AuthGuard]
  },
  {
    path: 'login',
    component: LoginComponent
  },
  {
    path: 'user',
    component: UserManagerComponent
  },
  {
    path: 'restore',
    component: RestoreComponent,
    children: [
      {
        path: ':restorefactoryitems',
        component: RestoreFactoryItemsComponent
      }
    ],
  },
  {
    path: 'server',
    component: ServerComponent
  },
  {
    path: 'entities',
    component: EntitiesComponent,
    children: [
      {
        path: ':structureslist',
        component: StructuresListComponent
      }
    ],
  },
  {
    path: 'timetable',
    component: TimetableComponent
  },
  {
    path: 'galaxy',
    component: GalaxyMapComponent
  },

  // otherwise redirect to home
  { path: '**', redirectTo: '' }
];

export const routing = RouterModule.forRoot(appRoutes);

@NgModule({
  imports: [RouterModule.forRoot(appRoutes)],
  exports: [RouterModule]
})

export class AppRoutingModule { }


