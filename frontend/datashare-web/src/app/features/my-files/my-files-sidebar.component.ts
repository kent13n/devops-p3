import { ChangeDetectionStrategy, Component, EventEmitter, Input, Output } from '@angular/core';
import { RouterLink, RouterLinkActive } from '@angular/router';

@Component({
  selector: 'app-my-files-sidebar',
  standalone: true,
  imports: [RouterLink, RouterLinkActive],
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
}
