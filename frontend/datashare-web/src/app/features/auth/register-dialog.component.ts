import { Component, inject } from '@angular/core';
import { AbstractControl, FormBuilder, ReactiveFormsModule, ValidationErrors, Validators } from '@angular/forms';
import { MatDialogRef, MatDialogModule, MatDialog } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';
import { MatSnackBar } from '@angular/material/snack-bar';
import { Router } from '@angular/router';
import { AuthService } from '../../core/auth/auth.service';
import { LoginDialogComponent } from './login-dialog.component';

@Component({
  selector: 'app-register-dialog',
  standalone: true,
  imports: [
    ReactiveFormsModule,
    MatDialogModule,
    MatFormFieldModule,
    MatInputModule,
    MatButtonModule
  ],
  templateUrl: './register-dialog.component.html',
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
      width: '400px',
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
