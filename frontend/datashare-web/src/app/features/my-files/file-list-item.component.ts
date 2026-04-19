import { ChangeDetectionStrategy, Component, EventEmitter, Input, Output } from '@angular/core';
import { FileHistoryItem } from '../../core/api/file.models';

@Component({
  selector: 'app-file-list-item',
  standalone: true,
  imports: [],
  templateUrl: './file-list-item.component.html',
  styleUrl: './file-list-item.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class FileListItemComponent {
  @Input({ required: true }) file!: FileHistoryItem;
  @Output() delete = new EventEmitter<string>();

  get expirationText(): string {
    if (this.file.isExpired || this.file.isPurged) return 'Expiré';

    const expiresAt = new Date(this.file.expiresAt);
    const now = new Date();
    const diffMs = expiresAt.getTime() - now.getTime();
    const diffDays = Math.ceil(diffMs / (1000 * 60 * 60 * 24));

    if (diffDays <= 0) return 'Expiré';
    if (diffDays === 1) return 'Expire demain';
    return `Expire dans ${diffDays} jours`;
  }

  onDelete(): void {
    this.delete.emit(this.file.id);
  }
}
