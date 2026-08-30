import { Component, input, output } from '@angular/core';
import { CommonModule } from '@angular/common';

export interface ColumnDef<T> {
  key: string;
  header: string;
  render?: (item: T) => string;
  class?: string;
}

export interface ActionDef<T> {
  label: string;
  icon?: string;
  action: (item: T) => void;
  class?: string;
  condition?: (item: T) => boolean;
}

@Component({
  selector: 'app-admin-table',
  standalone: true,
  imports: [CommonModule],
  template: `
    <div class="table-container">
      <table class="admin-table">
        <thead>
          <tr>
            @for (col of columns(); track col.key) {
              <th [class]="col.class">{{ col.header }}</th>
            }
            @if (actions().length > 0) {
              <th>Actions</th>
            }
          </tr>
        </thead>
        <tbody>
          @for (item of data(); track $index) {
            <tr>
              @for (col of columns(); track col.key) {
                <td [class]="col.class">
                  @if (col.render) {
                    {{ col.render(item) }}
                  } @else {
                    {{ item[col.key] }}
                  }
                </td>
              }
              @if (actions().length > 0) {
                <td class="actions-cell">
                  @for (action of actions(); track action.label) {
                    @if (!action.condition || action.condition(item)) {
                      <button
                        class="action-btn"
                        [class]="action.class"
                        (click)="action.action(item)"
                        [attr.aria-label]="action.label">
                        @if (action.icon) {
                          <i [class]="action.icon"></i>
                        } @else {
                          {{ action.label }}
                        }
                      </button>
                    }
                  }
                </td>
              }
            </tr>
          } @empty {
            <tr>
              <td [attr.colspan]="columns().length + (actions().length > 0 ? 1 : 0)" class="empty-state">
                {{ emptyMessage() || 'No data available' }}
              </td>
            </tr>
          }
        </tbody>
      </table>
    </div>

    @if (pagination(); as pg) {
      <div class="pagination">
        <button
          (click)="pageChange.emit(pg.current - 1)"
          [disabled]="pg.current <= 1"
          class="pagination-btn">
          Previous
        </button>
        <span class="pagination-info">
          Page {{ pg.current }} of {{ pg.total }}
        </span>
        <button
          (click)="pageChange.emit(pg.current + 1)"
          [disabled]="pg.current >= pg.total"
          class="pagination-btn">
          Next
        </button>
      </div>
    }
  `,
  styles: [`
    .table-container {
      overflow-x: auto;
    }
    .admin-table {
      width: 100%;
      border-collapse: collapse;
      font-size: 0.875rem;
    }
    .admin-table th,
    .admin-table td {
      padding: 0.75rem 1rem;
      text-align: left;
      border-bottom: 1px solid var(--border-color, #e0e0e0);
    }
    .admin-table th {
      background-color: var(--surface-variant, #f5f5f5);
      font-weight: 600;
      color: var(--on-surface-variant, #333);
    }
    .admin-table tbody tr:hover {
      background-color: var(--hover-color, #fafafa);
    }
    .actions-cell {
      display: flex;
      gap: 0.5rem;
      flex-wrap: wrap;
    }
    .action-btn {
      padding: 0.375rem 0.75rem;
      border: none;
      border-radius: 4px;
      cursor: pointer;
      font-size: 0.75rem;
      font-weight: 500;
      transition: all 0.2s;
    }
    .action-btn.primary {
      background-color: var(--primary, #2874F0);
      color: white;
    }
    .action-btn.secondary {
      background-color: var(--secondary, #6c757d);
      color: white;
    }
    .action-btn.danger {
      background-color: var(--danger, #dc3545);
      color: white;
    }
    .action-btn:hover {
      opacity: 0.9;
      transform: translateY(-1px);
    }
    .empty-state {
      text-align: center;
      padding: 3rem;
      color: var(--on-surface-variant, #666);
    }
    .pagination {
      display: flex;
      justify-content: center;
      align-items: center;
      gap: 1rem;
      padding: 1rem;
      flex-wrap: wrap;
    }
    .pagination-btn {
      padding: 0.5rem 1rem;
      border: 1px solid var(--border-color, #ddd);
      background: var(--surface, white);
      border-radius: 4px;
      cursor: pointer;
    }
    .pagination-btn:disabled {
      opacity: 0.5;
      cursor: not-allowed;
    }
  `]
})
export class AdminTableComponent<T extends Record<string, any>> {
  data = input.required<T[]>();
  columns = input.required<ColumnDef<T>[]>();
  actions = input<ActionDef<T>[]>([]);
  pagination = input<{ current: number; total: number } | null>(null);
  emptyMessage = input<string>('No data available');
  pageChange = output<number>();
}