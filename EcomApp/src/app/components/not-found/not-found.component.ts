import { Component } from '@angular/core';
import { RouterLink } from '@angular/router';

@Component({
  selector: 'app-not-found',
  standalone: true,
  imports: [RouterLink],
  template: `
    <div class="not-found">
      <div class="not-found-content">
        <h1>404</h1>
        <h2>Page Not Found</h2>
        <p>The page you're looking for doesn't exist or has been moved.</p>
        <a routerLink="/products" class="btn btn-primary">Back to Shop</a>
      </div>
    </div>
  `,
  styles: [`
    .not-found {
      min-height: 60vh;
      display: flex;
      align-items: center;
      justify-content: center;
      padding: 2rem;
    }
    .not-found-content {
      text-align: center;
      max-width: 400px;
    }
    .not-found-content h1 {
      font-size: 8rem;
      font-weight: 700;
      color: var(--primary, #2874F0);
      margin: 0;
      line-height: 1;
    }
    .not-found-content h2 {
      font-size: 2rem;
      margin: 1rem 0;
      color: var(--on-surface, #333);
    }
    .not-found-content p {
      color: var(--on-surface-variant, #666);
      margin-bottom: 2rem;
      font-size: 1.125rem;
    }
    .btn {
      display: inline-block;
      padding: 0.875rem 2rem;
      background: var(--primary, #2874F0);
      color: white;
      border: none;
      border-radius: 8px;
      font-weight: 600;
      font-size: 1rem;
      text-decoration: none;
      transition: all 0.2s;
    }
    .btn:hover {
      background: var(--primary-dark, #1a5dc8);
      transform: translateY(-2px);
      box-shadow: 0 4px 12px rgba(40, 116, 240, 0.3);
    }
  `]
})
export class NotFoundComponent {}