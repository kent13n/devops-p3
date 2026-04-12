import { Component } from '@angular/core';
import { HeaderComponent } from '../../shared/header/header.component';

@Component({
  selector: 'app-my-files',
  standalone: true,
  imports: [HeaderComponent],
  templateUrl: './my-files.component.html',
  styles: [`
    .my-files-page {
      min-height: 100vh;
      background: linear-gradient(135deg, #FF8A65 0%, #FFAB91 50%, #FFE0B2 100%);
    }
    .my-files-content {
      padding: 6rem 2rem 2rem;
      max-width: 900px;
      margin: 0 auto;
    }
    .placeholder {
      background: white;
      border-radius: 12px;
      padding: 3rem;
      text-align: center;
      color: #666;
      box-shadow: 0 2px 12px rgba(0,0,0,0.08);
    }
  `]
})
export class MyFilesComponent {}
