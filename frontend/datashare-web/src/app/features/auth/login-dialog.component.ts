import { Component, inject } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatDialogRef, MatDialogModule, MatDialog } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';
import { MatSnackBar } from '@angular/material/snack-bar';
import { Router } from '@angular/router';
import { AuthService } from '../../core/auth/auth.service';
import { RegisterDialogComponent } from './register-dialog.component';

@Component({
  selector: 'app-login-dialog',
  standalone: true,
  imports: [
    ReactiveFormsModule,
    MatDialogModule,
    MatFormFieldModule,
    MatInputModule,
    MatButtonModule
  ],
  templateUrl: './login-dialog.component.html',
  styles: [`
    .auth-form {
      display: flex;
      flex-direction: column;
      gap: 1rem;
      padding: 1rem 0;
    }
    .auth-form mat-form-field {
      width: 100%;
    }
    .switch-link {
      text-align: center;
      margin-top: 0.5rem;
      color: #666;
      font-size: 0.875rem;
    }
    .switch-link a {
      color: #FF8A65;
      cursor: pointer;
      text-decoration: underline;
    }
    .error-message {
      color: #d32f2f;
      text-align: center;
      font-size: 0.875rem;
    }
  `]
})
export class LoginDialogComponent {
  private fb = inject(FormBuilder);
  private authService = inject(AuthService);
  private dialogRef = inject(MatDialogRef<LoginDialogComponent>);
  private dialog = inject(MatDialog);
  private router = inject(Router);
  private snackBar = inject(MatSnackBar);

  errorMessage = '';
  loading = false;

  form = this.fb.group({
    email: ['', [Validators.required, Validators.email]],
    password: ['', [Validators.required]]
  });

  submit(): void {
    if (this.form.invalid) return;

    this.loading = true;
    this.errorMessage = '';

    this.authService.login({
      email: this.form.value.email!,
      password: this.form.value.password!
    }).subscribe({
      next: () => {
        this.dialogRef.close();
        this.router.navigate(['/my-files']);
        this.snackBar.open('Connexion réussie', 'OK', { duration: 3000 });
      },
      error: () => {
        this.loading = false;
        this.errorMessage = 'Email ou mot de passe incorrect';
      }
    });
  }

  switchToRegister(): void {
    this.dialogRef.close();
    this.dialog.open(RegisterDialogComponent, {
      width: '400px',
      panelClass: 'auth-dialog'
    });
  }
}
