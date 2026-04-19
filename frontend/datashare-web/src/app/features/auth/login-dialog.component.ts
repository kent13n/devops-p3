import { Component, inject } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatDialogRef, MatDialogModule, MatDialog } from '@angular/material/dialog';
import { MatSnackBar } from '@angular/material/snack-bar';
import { Router } from '@angular/router';
import { AuthService } from '../../core/auth/auth.service';
import { RegisterDialogComponent } from './register-dialog.component';

@Component({
  selector: 'app-login-dialog',
  standalone: true,
  imports: [ReactiveFormsModule, MatDialogModule],
  templateUrl: './login-dialog.component.html',
  styleUrl: './auth-dialog.scss'
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
      width: '480px',
      panelClass: 'auth-dialog',
      ariaLabelledBy: 'register-dialog-title'
    });
  }
}
