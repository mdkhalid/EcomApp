import { Component, signal, inject } from '@angular/core';
import { RouterLink, RouterOutlet, Router } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { AuthService } from './services/auth.service';

@Component({
  selector: 'app-root',
  imports: [RouterLink, RouterOutlet, FormsModule],
  templateUrl: './app.html',
  styleUrl: './app.scss'
})
export class App {
  protected readonly title = signal('ShopKart');
  protected readonly authService = inject(AuthService);
  protected readonly router = inject(Router);
  protected searchQuery = signal('');

  protected onSearch(): void {
    const q = this.searchQuery().trim();
    if (q) {
      this.router.navigate(['/products'], { queryParams: { search: q } });
    }
  }

  protected logout(): void {
    this.authService.logout();
    this.router.navigate(['/login']);
  }
}
