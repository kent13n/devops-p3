import { Routes } from '@angular/router';
import { LandingComponent } from './features/landing/landing.component';
import { MyFilesComponent } from './features/my-files/my-files.component';
import { DownloadComponent } from './features/download/download.component';
import { authGuard } from './core/auth/auth.guard';

export const routes: Routes = [
  { path: '', component: LandingComponent },
  { path: 'my-files', component: MyFilesComponent, canActivate: [authGuard] },
  { path: 'd/:token', component: DownloadComponent },
  { path: '**', redirectTo: '' }
];
