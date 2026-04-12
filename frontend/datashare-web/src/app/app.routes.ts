import { Routes } from '@angular/router';
import { LandingComponent } from './features/landing/landing.component';
import { MyFilesComponent } from './features/my-files/my-files.component';
import { authGuard } from './core/auth/auth.guard';

export const routes: Routes = [
  { path: '', component: LandingComponent },
  { path: 'my-files', component: MyFilesComponent, canActivate: [authGuard] },
  { path: '**', redirectTo: '' }
];
