import { TestBed } from '@angular/core/testing';
import { FileIconComponent } from './file-icon.component';

describe('FileIconComponent', () => {
  function buildWith(mime: string, name: string) {
    const fixture = TestBed.createComponent(FileIconComponent);
    fixture.componentInstance.mimeType = mime;
    fixture.componentInstance.fileName = name;
    return fixture.componentInstance;
  }

  beforeEach(() => {
    TestBed.configureTestingModule({ imports: [FileIconComponent] });
  });

  it.each([
    ['image/png', 'a.png', 'image'],
    ['video/mp4', 'b.mp4', 'video'],
    ['audio/mpeg', 'c.mp3', 'audio'],
    ['application/pdf', 'd.pdf', 'pdf'],
    ['application/zip', 'e.zip', 'archive'],
    ['application/vnd.openxmlformats-officedocument.wordprocessingml.document', 'f.docx', 'document'],
    ['text/plain', 'g.txt', 'text'],
    ['application/octet-stream', 'binary.bin', 'default']
  ])('resolves %s / %s to icon type %s', (mime, name, expected) => {
    const cmp = buildWith(mime, name);
    expect(cmp.iconType()).toBe(expected);
  });

  it('detects archive by filename extension when mime is generic', () => {
    const cmp = buildWith('application/octet-stream', 'backup.tar');
    expect(cmp.iconType()).toBe('archive');
  });

  it('detects text by filename extension when mime is generic', () => {
    const cmp = buildWith('application/octet-stream', 'notes.md');
    expect(cmp.iconType()).toBe('text');
  });
});
