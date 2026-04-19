import { Component, inject } from '@angular/core';
import { MatDialog } from '@angular/material/dialog';
import { HeaderComponent } from '../../shared/header/header.component';
import { FooterComponent } from '../../shared/footer/footer.component';
import { UploadDialogComponent } from '../upload/upload-dialog.component';

@Component({
  selector: 'app-landing',
  standalone: true,
  imports: [HeaderComponent, FooterComponent],
  templateUrl: './landing.component.html',
  styleUrl: './landing.component.scss'
})
export class LandingComponent {
  private dialog = inject(MatDialog);

  onUploadClick(): void {
    this.dialog.open(UploadDialogComponent, {
      width: '480px',
      panelClass: ['auth-dialog', 'upload-dialog']
    });
  }
}
