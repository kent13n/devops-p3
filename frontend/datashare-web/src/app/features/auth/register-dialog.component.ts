import { Component, inject } from '@angular/core';
import { AbstractControl, FormBuilder, ReactiveFormsModule, ValidationErrors, Validators } from '@angular/forms';
import { MatDialogRef, MatDialogModule, MatDialog } from '@angular/material/dialog';
import { MatSnackBar } from '@angular/material/snack-bar';
import { Router } from '@angular/router';
import { AuthService } from '../../core/auth/auth.service';
import { LoginDialogComponent } from './login-dialog.component';

@Component({
  selector: 'app-register-dialog',
  standalone: true,
  imports: [ReactiveFormsModule, MatDialogModule],
  templateUrl: './register-dialog.component.html',
  styleUrl: './auth-dialog.scss'
})
export class RegisterDialogComponent {
  private fb = inject(FormBuilder);
  private authService = inject(AuthService);
  private dialogRef = inject(MatDialogRef<RegisterDialogComponent>);
  private dialog = inject(MatDialog);
  private router = inject(Router);
  private snackBar = inject(MatSnackBar);

  errorMessage = '';
  loading = false;

  form = this.fb.group({
    email: ['', [Validators.required, Validators.email]],
    password: ['', [Validators.required, Validators.minLength(8)]],
    confirmPassword: ['', [Validators.required]]
  }, { validators: this.passwordsMatchValidator });

  submit(): void {
    if (this.form.invalid) return;

    this.loading = true;
    this.errorMessage = '';

    this.authService.register({
      email: this.form.value.email!,
      password: this.form.value.password!
    }).subscribe({
      next: () => {
        this.dialogRef.close();
        this.router.navigate(['/my-files']);
        this.snackBar.open('Compte créé avec succès', 'OK', { duration: 3000 });
      },
      error: (err) => {
        this.loading = false;
        if (err.status === 409) {
          this.errorMessage = 'Cet email est déjà utilisé';
        } else {
          this.errorMessage = 'Erreur lors de la création du compte';
        }
      }
    });
  }

  switchToLogin(): void {
    this.dialogRef.close();
    this.dialog.open(LoginDialogComponent, {
      width: '480px',
      panelClass: 'auth-dialog'
    });
  }

  private passwordsMatchValidator(control: AbstractControl): ValidationErrors | null {
    const password = control.get('password');
    const confirm = control.get('confirmPassword');

    if (password && confirm && password.value !== confirm.value) {
      confirm.setErrors({ passwordMismatch: true });
      return { passwordMismatch: true };
    }
    return null;
  }
}
