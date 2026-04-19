import { ChangeDetectionStrategy, Component, OnInit, inject, signal } from '@angular/core';
import { Router } from '@angular/router';
import { MatDialog } from '@angular/material/dialog';
import { MatSnackBar } from '@angular/material/snack-bar';
import { AuthService } from '../../core/auth/auth.service';
import { FileService } from '../../core/api/file.service';
import { FileHistoryItem, FileStatus } from '../../core/api/file.models';
import { UploadDialogComponent } from '../upload/upload-dialog.component';
import { ConfirmDialogComponent } from '../../shared/confirm-dialog/confirm-dialog.component';
import { MyFilesSidebarComponent } from './my-files-sidebar.component';
import { FileListItemComponent } from './file-list-item.component';

@Component({
  selector: 'app-my-files',
  standalone: true,
  imports: [MyFilesSidebarComponent, FileListItemComponent],
  templateUrl: './my-files.component.html',
  styleUrl: './my-files.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class MyFilesComponent implements OnInit {
  authService = inject(AuthService);
  private fileService = inject(FileService);
  private router = inject(Router);
  private dialog = inject(MatDialog);
  private snackBar = inject(MatSnackBar);

  activeTab = signal<FileStatus>('all');
  files = signal<FileHistoryItem[]>([]);
  loading = signal(false);
  sidebarOpen = signal(false);

  ngOnInit(): void {
    this.loadFiles();
  }

  loadFiles(): void {
    this.loading.set(true);
    this.fileService.getMyFiles(this.activeTab()).subscribe({
      next: (files) => {
        this.files.set(files);
        this.loading.set(false);
      },
      error: () => {
        this.loading.set(false);
        this.snackBar.open('Erreur lors du chargement des fichiers', 'OK', { duration: 3000 });
      }
    });
  }

  onTabChange(tab: FileStatus): void {
    if (this.activeTab() === tab) return;
    this.activeTab.set(tab);
    this.loadFiles();
  }

  onDelete(id: string): void {
    const dialogRef = this.dialog.open(ConfirmDialogComponent, {
      width: '420px',
      panelClass: 'auth-dialog',
      data: {
        title: 'Supprimer le fichier',
        message: 'Cette action est irréversible. Le fichier sera supprimé définitivement.',
        confirmLabel: 'Supprimer',
        danger: true
      }
    });

    dialogRef.afterClosed().subscribe((confirmed) => {
      if (!confirmed) return;

      const snapshot = this.files();
      const index = snapshot.findIndex(f => f.id === id);
      if (index === -1) return;

      // Optimistic update
      this.files.set(snapshot.filter(f => f.id !== id));

      this.fileService.deleteFile(id).subscribe({
        next: () => {
          this.snackBar.open('Fichier supprimé', 'OK', { duration: 2000 });
        },
        error: (err) => {
          if (err.status === 404) {
            // Déjà supprimé côté serveur, on garde la suppression locale
            return;
          }
          // Rollback
          this.files.set(snapshot);
          this.snackBar.open('Erreur lors de la suppression', 'OK', { duration: 3000 });
        }
      });
    });
  }

  onUpload(): void {
    const dialogRef = this.dialog.open(UploadDialogComponent, {
      width: '480px',
      panelClass: ['auth-dialog', 'upload-dialog']
    });

    // Rechargement systématique après fermeture : l'utilisateur peut fermer
    // via l'overlay/Escape après un upload réussi, sans que close() soit appelé
    // avec la valeur. Reload inconditionnel simple et robuste (coût : 1 GET
    // inutile si l'utilisateur annule sans uploader).
    dialogRef.afterClosed().subscribe(() => this.loadFiles());
  }

  onLogout(): void {
    this.authService.logout();
    this.router.navigate(['/']);
  }

  openSidebar(): void {
    this.sidebarOpen.set(true);
  }

  closeSidebar(): void {
    this.sidebarOpen.set(false);
  }
}
