import { Component } from '@angular/core';

@Component({
  selector: 'app-footer',
  standalone: true,
  template: `
    <footer class="footer">
      <div class="footer-content">
        <p>Copyright DataShare® 2025</p>
      </div>
    </footer>
  `,
  styles: [`
    .footer {
      padding: 1rem 2rem;
    }
    .footer-content {
      max-width: 1280px;
      margin: 0 auto;
    }
    .footer p {
      margin: 0;
      color: white;
      font-size: 1rem;
      text-align: left;
    }
  `]
})
export class FooterComponent {}
