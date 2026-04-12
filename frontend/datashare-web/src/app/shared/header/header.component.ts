import { Component, inject } from '@angular/core';
import { RouterLink } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatDialog } from '@angular/material/dialog';
import { AuthService } from '../../core/auth/auth.service';
import { LoginDialogComponent } from '../../features/auth/login-dialog.component';

@Component({
  selector: 'app-header',
  standalone: true,
  imports: [RouterLink, MatButtonModule],
  templateUrl: './header.component.html',
  styleUrl: './header.component.scss'
})
export class HeaderComponent {
  authService = inject(AuthService);
  private dialog = inject(MatDialog);

  openLoginDialog(): void {
    this.dialog.open(LoginDialogComponent, {
      width: '400px',
      panelClass: 'auth-dialog'
    });
  }

  logout(): void {
    this.authService.logout();
  }
}
