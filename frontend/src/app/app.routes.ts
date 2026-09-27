import { Routes } from '@angular/router';
export const routes: Routes = [
  { path: '', pathMatch: 'full', redirectTo: 'tasks' },
  { path: 'tasks', loadComponent: () => import('./tasks/task-list').then((m) => m.TaskList) },
  { path: 'tasks/new', loadComponent: () => import('./tasks/task-form').then((m) => m.TaskForm) },
  {
    path: 'tasks/:id/edit',
    loadComponent: () => import('./tasks/task-form').then((m) => m.TaskForm),
  },
  { path: 'tasks/:id', loadComponent: () => import('./tasks/task-detail').then((m) => m.TaskDetail) },
  { path: '**', redirectTo: 'tasks' },
];

