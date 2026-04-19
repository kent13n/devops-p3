import { ChangeDetectionStrategy, Component, Input, computed, signal } from '@angular/core';

export type FileIconType = 'image' | 'video' | 'audio' | 'pdf' | 'archive' | 'document' | 'text' | 'default';

@Component({
  selector: 'app-file-icon',
  standalone: true,
  imports: [],
  templateUrl: './file-icon.component.html',
  styleUrl: './file-icon.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class FileIconComponent {
  private _mimeType = signal('');
  private _fileName = signal('');

  @Input() set mimeType(value: string | null | undefined) {
    this._mimeType.set(value ?? '');
  }

  @Input() set fileName(value: string | null | undefined) {
    this._fileName.set(value ?? '');
  }

  iconType = computed<FileIconType>(() => {
    const mime = this._mimeType().toLowerCase();
    const name = this._fileName().toLowerCase();

    if (mime.startsWith('image/')) return 'image';
    if (mime.startsWith('video/')) return 'video';
    if (mime.startsWith('audio/')) return 'audio';
    if (mime === 'application/pdf' || name.endsWith('.pdf')) return 'pdf';

    if (mime.includes('zip') || mime.includes('rar') || mime.includes('7z') ||
        mime.includes('tar') || mime.includes('gzip') ||
        /\.(zip|rar|7z|tar|gz|bz2)$/.test(name)) return 'archive';

    if (mime.includes('word') || mime.includes('excel') || mime.includes('spreadsheet') ||
        mime.includes('presentation') || mime.includes('powerpoint') || mime.includes('opendocument') ||
        /\.(docx?|xlsx?|pptx?|odt|ods|odp)$/.test(name)) return 'document';

    if (mime.startsWith('text/') || /\.(txt|md|csv|log|json|xml|yaml|yml)$/.test(name)) return 'text';

    return 'default';
  });
}
