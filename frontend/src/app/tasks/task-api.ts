import { inject, Injectable } from '@angular/core';
import { HttpClient, HttpErrorResponse, HttpParams } from '@angular/common/http';
export type WorkStatus = 'Todo' | 'InProgress' | 'Done';
export type Priority = 'Low' | 'Medium' | 'High';
export interface SaveTask {
  title: string;
  description: string | null;
  status: WorkStatus;
  priority: Priority;
  dueDate: string | null;
}
export interface TaskItem extends SaveTask {
  createdByName: string | null;
  id: string;
  createdAt: string;
  updatedAt: string;
}
export interface TaskPage {
  items: TaskItem[];
  totalCount: number;
  page: number;
  pageSize: number;
}
export const statusLabels: Record<WorkStatus, string> = {
  Todo: 'To do',
  InProgress: 'In progress',
  Done: 'Done',
};
@Injectable({ providedIn: 'root' })
export class TaskApi {
  private readonly http = inject(HttpClient);
  list(page: number, status: WorkStatus | '') {
    let params = new HttpParams().set('page', page).set('pageSize', 20);
    if (status) params = params.set('status', status);
    return this.http.get<TaskPage>('/api/tasks', { params });
  }
  get(id: string) {
    return this.http.get<TaskItem>(`/api/tasks/${id}`);
  }
  create(task: SaveTask) {
    return this.http.post<TaskItem>('/api/tasks', task);
  }
  update(id: string, task: SaveTask) {
    return this.http.put<TaskItem>(`/api/tasks/${id}`, task);
  }
  delete(id: string) {
    return this.http.delete<void>(`/api/tasks/${id}`);
  }
}
export function requestError(error: unknown): string {
  if (error instanceof HttpErrorResponse) {
    if (error.status === 401) return 'Your session has expired. Refresh the page and sign in again.';
    if (error.status === 403) return 'You do not have permission to perform this action.';
    if (error.status === 404)
      return 'This task no longer exists. Return to the task list and refresh.';
    if (error.status === 400) return 'Check your task details and try again.';
    if (error.status === 409) return 'This task changed. Refresh the page and try again.';
  }
  return 'We could not reach the task service. Check that the API and database are running, then try again.';
}


