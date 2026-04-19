import { Component, inject, signal, ChangeDetectionStrategy } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { DatePipe } from '@angular/common';
import { MatDialogRef, MatDialogModule } from '@angular/material/dialog';
import { MatSnackBar } from '@angular/material/snack-bar';
import { MatChipsModule, MatChipInputEvent } from '@angular/material/chips';
import { MatIconModule } from '@angular/material/icon';
import { ENTER, COMMA, SPACE } from '@angular/cdk/keycodes';
import { Clipboard } from '@angular/cdk/clipboard';
import { HttpEventType } from '@angular/common/http';
import { FileService } from '../../core/api/file.service';
import { AuthService } from '../../core/auth/auth.service';
import { FileDto } from '../../core/api/file.models';
import { FileIconComponent } from '../../shared/file-icon/file-icon.component';

@Component({
  selector: 'app-upload-dialog',
  standalone: true,
  imports: [
    ReactiveFormsModule,
    DatePipe,
    MatDialogModule,
    MatChipsModule,
    MatIconModule,
    FileIconComponent
  ],
  templateUrl: './upload-dialog.component.html',
  styleUrl: './upload-dialog.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class UploadDialogComponent {
  private fb = inject(FormBuilder);
  private fileService = inject(FileService);
  private authService = inject(AuthService);
  private dialogRef = inject(MatDialogRef<UploadDialogComponent>);
  private snackBar = inject(MatSnackBar);
  private clipboard = inject(Clipboard);

  private static readonly blockedExtensions = new Set([
    '.exe', '.bat', '.cmd', '.com', '.scr', '.msi', '.sh',
    '.hta', '.vbs', '.vbe', '.js', '.jse', '.wsf', '.wsh',
    '.ps1', '.psc1', '.dll', '.iso', '.svg'
  ]);
  private static readonly maxSizeBytes = 1_073_741_824;
  private static readonly maxTagLength = 30;

  readonly separatorKeysCodes = [ENTER, COMMA, SPACE];

  selectedFile = signal<File | null>(null);
  uploadProgress = signal<number>(0);
  loading = signal(false);
  errorMessage = signal('');
  uploadedFile = signal<FileDto | null>(null);
  tags = signal<string[]>([]);

  form = this.fb.group({
    expiresInDays: [7],
    password: ['']
  });

  get isAuthenticated(): boolean {
    return this.authService.isAuthenticated();
  }

  get fileSizeFormatted(): string {
    const file = this.selectedFile();
    return file ? this.formatSize(file.size) : '';
  }

  get uploadedSizeFormatted(): string {
    const file = this.uploadedFile();
    return file ? this.formatSize(file.sizeBytes) : '';
  }

  get successMessage(): string {
    const days = this.form.value.expiresInDays ?? 7;
    if (days === 7) return 'Félicitations, ton fichier sera conservé chez nous pendant une semaine !';
    if (days === 1) return 'Félicitations, ton fichier sera conservé chez nous pendant une journée !';
    return `Félicitations, ton fichier sera conservé chez nous pendant ${days} jours !`;
  }

  private formatSize(bytes: number): string {
    if (bytes < 1024) return `${bytes} o`;
    if (bytes < 1024 * 1024) return `${(bytes / 1024).toFixed(1)} Ko`;
    if (bytes < 1024 * 1024 * 1024) return `${(bytes / (1024 * 1024)).toFixed(1)} Mo`;
    return `${(bytes / (1024 * 1024 * 1024)).toFixed(2)} Go`;
  }

  addTag(event: MatChipInputEvent): void {
    const value = (event.value || '').trim();
    if (value && value.length <= UploadDialogComponent.maxTagLength) {
      const current = this.tags();
      if (!current.some(t => t.toLowerCase() === value.toLowerCase())) {
        this.tags.set([...current, value]);
      }
    }
    event.chipInput.clear();
  }

  removeTag(tag: string): void {
    this.tags.set(this.tags().filter(t => t !== tag));
  }

  onFileSelected(event: Event): void {
    const input = event.target as HTMLInputElement;
    if (!input.files?.length) return;

    const file = input.files[0];

    const lastDot = file.name.lastIndexOf('.');
    const ext = lastDot >= 0 ? file.name.substring(lastDot).toLowerCase() : '';
    if (ext && UploadDialogComponent.blockedExtensions.has(ext)) {
      this.errorMessage.set(`Le type de fichier ${ext} n'est pas autorisé`);
      this.selectedFile.set(null);
      return;
    }

    if (file.size > UploadDialogComponent.maxSizeBytes) {
      this.errorMessage.set('Le fichier dépasse la taille maximale de 1 Go');
      this.selectedFile.set(null);
      return;
    }

    this.selectedFile.set(file);
    this.errorMessage.set('');
  }

  submit(): void {
    const file = this.selectedFile();
    if (!file) return;

    this.loading.set(true);
    this.errorMessage.set('');
    this.uploadProgress.set(0);

    const password = this.form.value.password || undefined;
    const tagArray = this.tags().length > 0 ? this.tags() : undefined;

    this.fileService.uploadWithProgress(file, {
      expiresInDays: this.form.value.expiresInDays ?? 7,
      password,
      tags: tagArray
    }).subscribe({
      next: (event) => {
        if (event.type === HttpEventType.UploadProgress && event.total) {
          this.uploadProgress.set(Math.round(100 * event.loaded / event.total));
        } else if (event.type === HttpEventType.Response) {
          this.loading.set(false);
          this.uploadedFile.set(event.body as FileDto);
        }
      },
      error: (err) => {
        this.loading.set(false);
        const body = err.error;
        if (body?.code === 'BLOCKED_EXTENSION') {
          this.errorMessage.set("Ce type de fichier n'est pas autorisé");
        } else if (body?.code === 'FILE_TOO_LARGE') {
          this.errorMessage.set('Le fichier dépasse la taille maximale de 1 Go');
        } else {
          this.errorMessage.set(body?.message ?? 'Erreur lors du téléversement');
        }
      }
    });
  }

  copyLink(): void {
    const file = this.uploadedFile();
    if (file) {
      this.clipboard.copy(file.downloadUrl);
      this.snackBar.open('Lien copié !', 'OK', { duration: 2000 });
    }
  }

  close(): void {
    this.dialogRef.close(this.uploadedFile());
  }
}
