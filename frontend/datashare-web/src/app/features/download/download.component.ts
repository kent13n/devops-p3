import { Component, OnInit, inject, signal, ChangeDetectionStrategy } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { DatePipe } from '@angular/common';
import { HeaderComponent } from '../../shared/header/header.component';
import { FooterComponent } from '../../shared/footer/footer.component';
import { FileService } from '../../core/api/file.service';
import { FileMetadataDto } from '../../core/api/file.models';

@Component({
  selector: 'app-download',
  standalone: true,
  imports: [FormsModule, DatePipe, HeaderComponent, FooterComponent],
  templateUrl: './download.component.html',
  styleUrl: './download.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class DownloadComponent implements OnInit {
  private route = inject(ActivatedRoute);
  private fileService = inject(FileService);

  token = '';
  metadata = signal<FileMetadataDto | null>(null);
  password = '';
  loading = signal(false);
  downloading = signal(false);
  errorMessage = signal('');
  errorType = signal<'not_found' | 'expired' | 'password' | 'generic' | null>(null);

  get fileSizeFormatted(): string {
    const meta = this.metadata();
    if (!meta) return '';
    const bytes = meta.sizeBytes;
    if (bytes < 1024) return `${bytes} o`;
    if (bytes < 1024 * 1024) return `${(bytes / 1024).toFixed(1)} Ko`;
    if (bytes < 1024 * 1024 * 1024) return `${(bytes / (1024 * 1024)).toFixed(1)} Mo`;
    return `${(bytes / (1024 * 1024 * 1024)).toFixed(2)} Go`;
  }

  get expirationMessage(): string {
    const meta = this.metadata();
    if (!meta) return '';
    const expiresAt = new Date(meta.expiresAt);
    const now = new Date();
    const diffMs = expiresAt.getTime() - now.getTime();
    const diffDays = Math.ceil(diffMs / (1000 * 60 * 60 * 24));
    const diffHours = Math.ceil(diffMs / (1000 * 60 * 60));

    if (diffDays > 1) return `Ce fichier expirera dans ${diffDays} jours.`;
    if (diffDays === 1) return 'Ce fichier expirera demain.';
    if (diffHours > 1) return `Ce fichier expirera dans ${diffHours} heures.`;
    return 'Ce fichier expire bientôt.';
  }

  ngOnInit(): void {
    this.token = this.route.snapshot.paramMap.get('token') ?? '';
    this.loadMetadata();
  }

  private loadMetadata(): void {
    this.loading.set(true);
    this.fileService.getMetadata(this.token).subscribe({
      next: (meta) => {
        this.metadata.set(meta);
        this.loading.set(false);
      },
      error: (err) => {
        this.loading.set(false);
        if (err.status === 404) {
          this.errorType.set('not_found');
          this.errorMessage.set('Ce lien de téléchargement est invalide ou inconnu');
        } else if (err.status === 410) {
          this.errorType.set('expired');
          this.errorMessage.set('Ce lien de téléchargement a expiré');
        } else {
          this.errorType.set('generic');
          this.errorMessage.set('Erreur lors du chargement');
        }
      }
    });
  }

  download(): void {
    this.downloading.set(true);
    this.errorType.set(null);
    this.errorMessage.set('');

    this.fileService.download(this.token, this.password || undefined).subscribe({
      next: (blob) => {
        this.downloading.set(false);
        const url = URL.createObjectURL(blob);
        const a = document.createElement('a');
        a.href = url;
        a.download = this.metadata()?.originalName ?? 'fichier';
        a.click();
        // Délai pour éviter que Safari annule le download
        setTimeout(() => URL.revokeObjectURL(url), 1000);
      },
      error: (err) => {
        this.downloading.set(false);
        if (err.status === 401) {
          this.errorType.set('password');
          this.errorMessage.set('Mot de passe incorrect');
        } else if (err.status === 410) {
          this.errorType.set('expired');
          this.errorMessage.set('Ce lien a expiré');
        } else {
          this.errorType.set('generic');
          this.errorMessage.set('Erreur lors du téléchargement');
        }
      }
    });
  }
}
