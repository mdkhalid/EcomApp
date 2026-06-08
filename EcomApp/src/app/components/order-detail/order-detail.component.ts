import { Component, inject, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { OrderService } from '../../services/order.service';
import { ReturnService } from '../../services/return.service';
import { ReturnPolicyService } from '../../services/return-policy.service';
import { NotificationService } from '../../services/notification.service';
import { Order } from '../../models/order.model';
import { ReturnRequest } from '../../models/return.model';
import { ReturnPolicy } from '../../models/return-policy.model';
import { OrderTrackingComponent } from '../order-tracking/order-tracking.component';
import { getFullImageUrl as buildImageUrl, API_URL } from '../../utils/api-config';

@Component({
  selector: 'app-order-detail',
  standalone: true,
  imports: [CommonModule, RouterLink, FormsModule, OrderTrackingComponent],
  templateUrl: './order-detail.component.html',
  styleUrl: './order-detail.component.scss'
})
export class OrderDetailComponent implements OnInit {
  private readonly orderService = inject(OrderService);
  private readonly returnService = inject(ReturnService);
  private readonly returnPolicyService = inject(ReturnPolicyService);
  private readonly route = inject(ActivatedRoute);
  readonly notification = inject(NotificationService);
  protected readonly notifications = this.notification.notifications;

  order = signal<Order | null>(null);
  loading = signal(true);

  returnRequest = signal<ReturnRequest | null>(null);
  returnPolicy = signal<ReturnPolicy | null>(null);
  showReturnModal = signal(false);
  returnReason = signal('');
  returnComment = signal('');
  submittingReturn = signal(false);
  returnReasons = ['Defective', 'WrongItem', 'NotAsDescribed', 'SizeIssue', 'ChangedMind', 'Other'];

  statusSteps = ['Pending', 'Processing', 'Shipped', 'OutForDelivery', 'Delivered'];

  ngOnInit(): void {
    const id = Number(this.route.snapshot.paramMap.get('id'));
    if (id) {
      this.returnPolicyService.get().subscribe({
        next: (p) => this.returnPolicy.set(p)
      });
      this.orderService.getById(id).subscribe({
        next: (order) => {
          this.order.set(order);
          this.loadReturnRequest(order.id);
          this.loading.set(false);
        },
        error: () => {
          this.notification.showError('Failed to load order details.');
          this.loading.set(false);
        }
      });
    }
  }

  loadReturnRequest(orderId: number): void {
    this.returnService.getByOrder(orderId).subscribe({
      next: (returnReq) => this.returnRequest.set(returnReq)
    });
  }

  getStatusClass(status: string): string {
    switch (status.toLowerCase()) {
      case 'pending': return 'status-pending';
      case 'processing': return 'status-processing';
      case 'shipped': return 'status-shipped';
      case 'outfordelivery': return 'status-out-for-delivery';
      case 'delivered': return 'status-delivered';
      case 'cancelled': return 'status-cancelled';
      case 'returned': return 'status-returned';
      default: return '';
    }
  }

  getStatusLabel(status: string): string {
    switch (status) {
      case 'OutForDelivery': return 'Out for Delivery';
      default: return status;
    }
  }

  getReturnStatusClass(status: string): string {
    switch (status.toLowerCase()) {
      case 'requested': return 'status-pending';
      case 'approved': return 'status-processing';
      case 'rejected': return 'status-cancelled';
      case 'refundinitiated': return 'status-shipped';
      case 'refunded': return 'status-delivered';
      default: return '';
    }
  }

  getReturnStatusLabel(status: string): string {
    switch (status) {
      case 'RefundInitiated': return 'Refund Initiated';
      default: return status;
    }
  }

  openReturnModal(): void {
    this.returnReason.set('');
    this.returnComment.set('');
    this.showReturnModal.set(true);
  }

  closeReturnModal(): void {
    this.showReturnModal.set(false);
  }

  submitReturnRequest(): void {
    const order = this.order();
    if (!order) return;

    if (!this.returnReason()) {
      this.notification.showError('Please select a return reason.');
      return;
    }

    this.submittingReturn.set(true);
    this.returnService.createReturnRequest({
      orderId: order.id,
      reason: this.returnReason(),
      comment: this.returnComment() || undefined
    }).subscribe({
      next: (returnReq) => {
        this.returnRequest.set(returnReq);
        this.notification.showSuccess('Return request submitted successfully.');
        this.closeReturnModal();
        this.submittingReturn.set(false);
      },
      error: (err) => {
        this.notification.showError(err.error?.error || 'Failed to submit return request.');
        this.submittingReturn.set(false);
      }
    });
  }

  downloadInvoice(): void {
    const order = this.order();
    if (!order) return;
    window.open(`http://localhost:5068/api/orders/${order.id}/invoice`, '_blank');
  }

  getFullImageUrl(path: string): string {
    return `http://localhost:5068${path}`;
  }

  formatDate(dateStr?: string): string {
    if (!dateStr) return '';
    const date = new Date(dateStr);
    return date.toLocaleDateString('en-IN', {
      day: 'numeric',
      month: 'short',
      year: 'numeric'
    });
  }
}
