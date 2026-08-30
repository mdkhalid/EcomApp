import { Component, inject, signal, OnInit, DestroyRef } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { CategoryService } from '../../../services/category.service';
import { NotificationService } from '../../../services/notification.service';
import { Category, CreateCategory } from '../../../models/category.model';
import { AdminTableComponent, ColumnDef, ActionDef } from '../shared/admin-table.component';
import { AdminModalComponent } from '../shared/admin-modal.component';

@Component({
  selector: 'app-admin-categories',
  standalone: true,
  imports: [CommonModule, FormsModule, AdminTableComponent, AdminModalComponent],
  template: `
    <div class="admin-categories">
      <header class="section-header">
        <h2>Categories</h2>
        <button class="btn btn-primary" (click)="openAddCategory()">Add Category</button>
      </header>

      <app-admin-table
        [data]="categories()"
        [columns]="categoryColumns"
        [actions]="categoryActions"
        [emptyMessage]="'No categories found'"
      />

      @if (showCategoryModal()) {
        <app-admin-modal
          [isOpen]="showCategoryModal()"
          [title]="editingCategory() ? 'Edit Category' : 'Add Category'"
          [loading]="saving()"
          (close)="closeCategoryModal()"
          (confirm)="saveCategory()">
          <form class="category-form">
            <div class="form-group">
              <label>Name *</label>
              <input type="text" [(ngModel)]="categoryForm.name" name="name" required>
            </div>
            <div class="form-group">
              <label>Icon</label>
              <input type="text" [(ngModel)]="categoryForm.icon" name="icon" placeholder="e.g., devices, fashion, home">
            </div>
          </form>
        </app-admin-modal>
      }
    </div>
  `,
  styles: [`
    .admin-categories { padding: 1.5rem; }
    .section-header { display: flex; justify-content: space-between; align-items: center; margin-bottom: 1.5rem; }
    .section-header h2 { margin: 0; font-size: 1.5rem; font-weight: 600; }
    .btn { padding: 0.625rem 1.25rem; border: none; border-radius: 6px; font-weight: 500; cursor: pointer; }
    .btn-primary { background: var(--primary, #2874F0); color: white; }
    .category-form { display: flex; flex-direction: column; gap: 1rem; }
    .form-group { display: flex; flex-direction: column; gap: 0.375rem; }
    .form-group label { font-weight: 500; font-size: 0.875rem; }
    .form-group input { padding: 0.5rem; border: 1px solid var(--border-color, #ddd); border-radius: 6px; font-size: 1rem; }
  `]
})
export class AdminCategoriesComponent implements OnInit {
  private readonly destroyRef = inject(DestroyRef);
  private readonly categoryService = inject(CategoryService);
  private readonly notificationService = inject(NotificationService);

  categories = signal<Category[]>([]);

  showCategoryModal = signal(false);
  editingCategory = signal<Category | null>(null);
  categoryForm: CreateCategory = { name: '', icon: '' };
  saving = signal(false);

  categoryColumns: ColumnDef<Category>[] = [
    { key: 'id', header: 'ID', render: (c) => `#${c.id}` },
    { key: 'name', header: 'Name' },
    { key: 'icon', header: 'Icon' }
  ];

  categoryActions: ActionDef<Category>[] = [
    { label: 'Edit', action: (c) => this.openEditCategory(c), class: 'primary' },
    { label: 'Delete', action: (c) => this.deleteCategory(c), class: 'danger' }
  ];

  ngOnInit(): void {
    this.loadCategories();
  }

  loadCategories(): void {
    this.categoryService.getAll().pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (data) => this.categories.set(data),
      error: () => this.notificationService.showError('Failed to load categories')
    });
  }

  openAddCategory(): void {
    this.editingCategory.set(null);
    this.categoryForm = { name: '', icon: '' };
    this.showCategoryModal.set(true);
  }

  openEditCategory(cat: Category): void {
    this.editingCategory.set(cat);
    this.categoryForm = { name: cat.name, icon: cat.icon || '' };
    this.showCategoryModal.set(true);
  }

  closeCategoryModal(): void {
    this.showCategoryModal.set(false);
    this.editingCategory.set(null);
  }

  saveCategory(): void {
    if (!this.categoryForm.name) {
      this.notificationService.showError('Category name is required');
      return;
    }
    this.saving.set(true);
    const iconVal = this.categoryForm.icon || undefined;

    if (this.editingCategory()) {
      this.categoryService.update(this.editingCategory()!.id, { name: this.categoryForm.name, icon: iconVal }).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
        next: () => {
          this.notificationService.showSuccess('Category updated');
          this.closeCategoryModal();
          this.loadCategories();
          this.saving.set(false);
        },
        error: (err) => {
          this.notificationService.showError(err.error?.error || 'Failed to update category');
          this.saving.set(false);
        }
      });
    } else {
      this.categoryService.create({ name: this.categoryForm.name, icon: iconVal }).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
        next: () => {
          this.notificationService.showSuccess('Category created');
          this.closeCategoryModal();
          this.loadCategories();
          this.saving.set(false);
        },
        error: (err) => {
          this.notificationService.showError(err.error?.error || 'Failed to create category');
          this.saving.set(false);
        }
      });
    }
  }

  deleteCategory(cat: Category): void {
    if (!confirm('Delete this category? Products using it will keep the category name but it will be removed from the list.')) return;
    this.categoryService.delete(cat.id).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: () => {
        this.notificationService.showSuccess('Category deleted');
        this.categories.update(c => c.filter(x => x.id !== cat.id));
      },
      error: (err) => this.notificationService.showError(err.error?.error || 'Failed to delete category')
    });
  }
}