import { Component, inject, signal } from '@angular/core';
import { DatePipe } from '@angular/common';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { catchError, combineLatest, of, startWith, Subject, switchMap } from 'rxjs';
import { TaskApi, TaskItem, requestError, statusLabels } from './task-api';

@Component({
  selector: 'app-task-detail',
  imports: [DatePipe, RouterLink],
  templateUrl: './task-detail.html',
  styleUrl: './task-detail.scss',
})
export class TaskDetail {
  private readonly api = inject(TaskApi);
  private readonly route = inject(ActivatedRoute);
  private readonly refresh = new Subject<void>();
  readonly task = signal<TaskItem | null>(null);
  readonly loading = signal(true);
  readonly error = signal('');
  readonly labels = statusLabels;

  constructor() {
    combineLatest([this.route.paramMap, this.refresh.pipe(startWith(undefined))]).pipe(
      switchMap(([params]) => {
        this.loading.set(true);
        this.error.set('');
        this.task.set(null);
        return this.api.get(params.get('id')!).pipe(
          catchError(error => {
            this.error.set(requestError(error));
            return of(null);
          }),
        );
      }),
      takeUntilDestroyed(),
    ).subscribe(task => {
      this.task.set(task);
      this.loading.set(false);
    });
  }

  retry(): void { this.refresh.next(); }
}
