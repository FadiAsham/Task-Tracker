import { Component, DestroyRef, inject, signal } from '@angular/core';
import { DatePipe } from '@angular/common';
import { RouterLink } from '@angular/router';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { Subject, catchError, of, startWith, switchMap } from 'rxjs';
import { TaskApi, TaskItem, TaskPage, WorkStatus, requestError, statusLabels } from './task-api';
@Component({
  selector: 'app-task-list',
  imports: [RouterLink, DatePipe],
  templateUrl: './task-list.html',
})
export class TaskList {
  private readonly api = inject(TaskApi);
  private readonly destroyRef = inject(DestroyRef);
  private readonly refresh = new Subject<void>();
  readonly data = signal<TaskPage | null>(null);
  readonly loading = signal(true);
  readonly error = signal('');
  readonly deleting = signal<string | null>(null);
  readonly notice = signal('');
  readonly labels = statusLabels;
  page = 1;
  status: WorkStatus | '' = '';
  get pageCount() {
    return Math.max(1, Math.ceil((this.data()?.totalCount ?? 0) / 20));
  }
  constructor() {
    this.refresh
      .pipe(
        startWith(undefined),
        // Cancel an older list request when the user changes the filter or page.
        switchMap(() => {
          this.loading.set(true);
          this.error.set('');
          return this.api.list(this.page, this.status).pipe(
            catchError((error) => {
              this.error.set(requestError(error));
              return of(null);
            }),
          );
        }),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe((data) => {
        this.data.set(data);
        this.loading.set(false);
      });
  }
  reload() {
    this.refresh.next();
  }
  filter(value: string) {
    this.status = value as WorkStatus | '';
    this.page = 1;
    this.reload();
  }
  go(page: number) {
    this.page = page;
    this.reload();
  }
  remove(task: TaskItem) {
    if (this.deleting() || !window.confirm(`Delete "${task.title}"? This cannot be undone.`))
      return;
    this.deleting.set(task.id);
    this.error.set('');
    this.notice.set('');
    this.api
      .delete(task.id)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: () => {
          this.deleting.set(null);
          this.notice.set('Task deleted.');
          // Move back a page when deleting its last remaining task.
          if (this.data()?.items.length === 1 && this.page > 1) this.page--;
          this.reload();
        },
        error: (error) => {
          this.deleting.set(null);
          this.error.set(requestError(error));
        },
      });
  }
}
