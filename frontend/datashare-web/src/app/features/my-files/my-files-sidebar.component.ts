import { ChangeDetectionStrategy, Component, EventEmitter, HostListener, Input, Output } from '@angular/core';
import { RouterLink, RouterLinkActive } from '@angular/router';
import { A11yModule } from '@angular/cdk/a11y';

@Component({
  selector: 'app-my-files-sidebar',
  standalone: true,
  imports: [RouterLink, RouterLinkActive, A11yModule],
  templateUrl: './my-files-sidebar.component.html',
  styleUrl: './my-files-sidebar.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class MyFilesSidebarComponent {
  @Input() mobileOpen = false;
  @Output() close = new EventEmitter<void>();

  onClose(): void {
    this.close.emit();
  }

  @HostListener('document:keydown.escape')
  onEscape(): void {
    if (this.mobileOpen) this.onClose();
  }
}
