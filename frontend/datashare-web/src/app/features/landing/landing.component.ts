import { Component, inject } from '@angular/core';
import { MatDialog } from '@angular/material/dialog';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { HeaderComponent } from '../../shared/header/header.component';
import { AuthService } from '../../core/auth/auth.service';
import { LoginDialogComponent } from '../auth/login-dialog.component';

@Component({
  selector: 'app-landing',
  standalone: true,
  imports: [HeaderComponent, MatButtonModule, MatIconModule],
  templateUrl: './landing.component.html',
  styleUrl: './landing.component.scss'
})
export class LandingComponent {
  private authService = inject(AuthService);
  private dialog = inject(MatDialog);

  onUploadClick(): void {
    if (this.authService.isAuthenticated()) {
      // L'upload sera implémenté à l'étape 4
      console.log('Upload à implémenter');
    } else {
      this.dialog.open(LoginDialogComponent, {
        width: '400px',
        panelClass: 'auth-dialog'
      });
    }
  }
}
