import { Component, Input, signal, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { OrderStatusHistory } from '../../models/order.model';

export interface TimelineStep {
  status: string;
  label: string;
  icon: string;
  completed: boolean;
  current: boolean;
  timestamp?: string;
  note?: string;
  location?: string;
}

@Component({
  selector: 'app-order-tracking',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './order-tracking.component.html',
  styleUrl: './order-tracking.component.scss'
})
export class OrderTrackingComponent {
  @Input() currentStatus = '';
  @Input() statusHistory: OrderStatusHistory[] = [];
  @Input() estimatedDeliveryDate?: string;
  @Input() trackingNumber?: string;
  @Input() carrier?: string;
  @Input() createdAt = '';

  allSteps = [
    { status: 'Pending', label: 'Order Placed', icon: 'receipt_long' },
    { status: 'Processing', label: 'Processing', icon: 'inventory_2' },
    { status: 'Shipped', label: 'Shipped', icon: 'local_shipping' },
    { status: 'OutForDelivery', label: 'Out for Delivery', icon: 'delivery_dining' },
    { status: 'Delivered', label: 'Delivered', icon: 'check_circle' }
  ];

  cancelledSteps = [
    { status: 'Pending', label: 'Order Placed', icon: 'receipt_long' },
    { status: 'Cancelled', label: 'Cancelled', icon: 'cancel' }
  ];

  returnedSteps = [
    { status: 'Pending', label: 'Order Placed', icon: 'receipt_long' },
    { status: 'Processing', label: 'Processing', icon: 'inventory_2' },
    { status: 'Shipped', label: 'Shipped', icon: 'local_shipping' },
    { status: 'Returned', label: 'Returned', icon: 'assignment_return' }
  ];

  get steps() {
    if (this.currentStatus === 'Cancelled') return this.cancelledSteps;
    if (this.currentStatus === 'Returned') return this.returnedSteps;
    return this.allSteps;
  }

  get timelineSteps(): TimelineStep[] {
    const currentIdx = this.steps.findIndex(s => s.status === this.currentStatus);
    const historyMap = new Map(this.statusHistory.map(h => [h.status, h]));

    return this.steps.map((step, idx) => {
      const history = historyMap.get(step.status);
      const isCancelled = this.currentStatus === 'Cancelled';
      const isReturned = this.currentStatus === 'Returned';

      let completed = false;
      let current = false;

      if (isCancelled || isReturned) {
        completed = history !== undefined;
        current = step.status === this.currentStatus;
      } else {
        completed = idx < currentIdx;
        current = idx === currentIdx;
      }

      return {
        ...step,
        completed,
        current,
        timestamp: history?.createdAt,
        note: history?.note,
        location: history?.location
      };
    });
  }

  get completedStepsCount(): number {
    return this.timelineSteps.filter(s => s.completed || s.current).length;
  }

  get progressPercentage(): number {
    return (this.completedStepsCount / this.steps.length) * 100;
  }

  formatDateTime(dateStr?: string): string {
    if (!dateStr) return '';
    const date = new Date(dateStr);
    return date.toLocaleDateString('en-IN', {
      day: 'numeric',
      month: 'short',
      year: 'numeric',
      hour: '2-digit',
      minute: '2-digit'
    });
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

  getStatusColor(status: string): string {
    switch (status) {
      case 'Delivered': return '#4caf50';
      case 'Shipped':
      case 'OutForDelivery': return '#2196f3';
      case 'Processing': return '#ff9800';
      case 'Pending': return '#9e9e9e';
      case 'Cancelled': return '#f44336';
      case 'Returned': return '#9c27b0';
      default: return '#9e9e9e';
    }
  }
}
