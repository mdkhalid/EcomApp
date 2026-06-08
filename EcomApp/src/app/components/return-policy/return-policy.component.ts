import { Component, inject, OnInit, signal, DestroyRef } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { CommonModule } from '@angular/common';
import { ReturnPolicyService } from '../../services/return-policy.service';
import { ReturnPolicy } from '../../models/return-policy.model';

@Component({
  selector: 'app-return-policy',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './return-policy.component.html',
  styleUrl: './return-policy.component.scss'
})
export class ReturnPolicyComponent implements OnInit {
  private readonly destroyRef = inject(DestroyRef);
  private readonly returnPolicyService = inject(ReturnPolicyService);

  policy = signal<ReturnPolicy | null>(null);
  loading = signal(true);
  error = signal('');

  ngOnInit(): void {
    this.returnPolicyService.get().pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (p) => {
        this.policy.set(p);
        this.loading.set(false);
      },
      error: () => {
        this.error.set('Failed to load return policy. Please try again later.');
        this.loading.set(false);
      }
    });
  }
}
