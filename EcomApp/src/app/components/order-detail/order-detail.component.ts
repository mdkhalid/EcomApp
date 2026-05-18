import { Component, inject, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { OrderService } from '../../services/order.service';
import { NotificationService } from '../../services/notification.service';
import { Order } from '../../models/order.model';

@Component({
  selector: 'app-order-detail',
  standalone: true,
  imports: [CommonModule, RouterLink],
  templateUrl: './order-detail.component.html',
  styleUrl: './order-detail.component.scss'
})
export class OrderDetailComponent implements OnInit {
  private readonly orderService = inject(OrderService);
  private readonly route = inject(ActivatedRoute);
  readonly notification = inject(NotificationService);
  protected readonly notifications = this.notification.notifications;

  order = signal<Order | null>(null);
  loading = signal(true);

  statusSteps = ['Pending', 'Processing', 'Shipped', 'Delivered'];

  ngOnInit(): void {
    const id = Number(this.route.snapshot.paramMap.get('id'));
    if (id) {
      this.orderService.getById(id).subscribe({
        next: (order) => {
          this.order.set(order);
          this.loading.set(false);
        },
        error: () => {
          this.notification.showError('Failed to load order details.');
          this.loading.set(false);
        }
      });
    }
  }

  getStatusClass(status: string): string {
    switch (status.toLowerCase()) {
      case 'pending': return 'status-pending';
      case 'processing': return 'status-processing';
      case 'shipped': return 'status-shipped';
      case 'delivered': return 'status-delivered';
      case 'cancelled': return 'status-cancelled';
      default: return '';
    }
  }

  getStepClass(step: string): string {
    const o = this.order();
    if (!o) return '';
    const currentIndex = this.statusSteps.indexOf(o.status);
    const stepIndex = this.statusSteps.indexOf(step);
    if (o.status === 'Cancelled') return 'step-cancelled';
    if (stepIndex <= currentIndex) return 'step-completed';
    if (stepIndex === currentIndex + 1) return 'step-current';
    return '';
  }

  getFullImageUrl(path: string): string {
    return `http://localhost:5068${path}`;
  }
}
