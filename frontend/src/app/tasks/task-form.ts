import { Component, DestroyRef, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators, AbstractControl } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { Priority, TaskApi, WorkStatus, requestError } from './task-api';
// Match backend validation: ignore surrounding spaces and reject blank titles.
export function trimmedTitle(control: AbstractControl) {
  const value = String(control.value ?? '').trim();
  return !value ? { required: true } : value.length > 150 ? { maxlength: true } : null;
}
@Component({
  selector: 'app-task-form',
  imports: [ReactiveFormsModule, RouterLink],
  templateUrl: './task-form.html',
})
export class TaskForm {
  private readonly api = inject(TaskApi);
  private readonly router = inject(Router);
  private readonly destroyRef = inject(DestroyRef);
  // An ID in the route means edit mode; otherwise this form creates a new task.
  readonly id = inject(ActivatedRoute).snapshot.paramMap.get('id');
  readonly loading = signal(false);
  readonly saving = signal(false);
  readonly error = signal('');
  readonly loaded = signal(!this.id);
  readonly form = inject(FormBuilder).nonNullable.group({
    title: ['', [trimmedTitle]],
    description: ['', Validators.maxLength(2000)],
    status: ['Todo' as WorkStatus],
    priority: ['Medium' as Priority],
    dueDate: [''],
  });
  constructor() {
    if (this.id) this.load();
  }
  load() {
    if (!this.id) return;
    this.loading.set(true);
    this.error.set('');
    this.api
      .get(this.id)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (task) => {
          this.form.patchValue({
            ...task,
            description: task.description ?? '',
            dueDate: task.dueDate ?? '',
          });
          this.loaded.set(true);
          this.loading.set(false);
        },
        error: (error) => {
          this.loading.set(false);
          this.error.set(requestError(error));
        },
      });
  }
  save() {
    // Prevent duplicate submissions and saving before the existing task has loaded.
    if (this.saving() || this.loading() || !this.loaded()) return;
    this.form.markAllAsTouched();
    if (this.form.invalid) return;
    this.saving.set(true);
    this.error.set('');
    const value = this.form.getRawValue();
    const task = {
      ...value,
      title: value.title.trim(),
      description: value.description || null,
      dueDate: value.dueDate || null,
    };
    const request = this.id ? this.api.update(this.id, task) : this.api.create(task);
    request.pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: () => {
        this.saving.set(false);
        void this.router.navigate(['/tasks']);
      },
      error: (error) => {
        this.saving.set(false);
        this.error.set(requestError(error));
      },
    });
  }
}
