import { Component, OnInit, inject } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { ApiService, dealContinuePath } from '@core/services/api.service';
import { AuthService } from '@core/services/auth.service';
import {
  SortOrder,
  formatCreatedDate,
  paginateSlice,
  sortByCreatedAt,
  totalPages
} from '@shared/utils/pagination.util';

@Component({
  selector: 'app-home',
  standalone: true,
  imports: [RouterLink, FormsModule],
  templateUrl: './home.component.html',
  styleUrl: './home.component.scss'
})
export class HomeComponent implements OnInit {
  private api = inject(ApiService);
  auth = inject(AuthService);
  data: any = null;
  chatMessage = '';
  chatReply = '';
  continuePath = dealContinuePath;

  readonly pageSize = 5;
  dealsSort: SortOrder = 'newest';
  dealsPage = 1;
  tasksPage = 1;
  remindersPage = 1;

  formatCreatedDate = formatCreatedDate;

  ngOnInit() {
    this.refresh();
  }

  refresh() {
    this.api.getDashboard().subscribe(d => {
      this.data = d;
      this.dealsPage = 1;
      this.tasksPage = 1;
      this.remindersPage = 1;
    });
  }

  get sortedDeals(): any[] {
    return sortByCreatedAt(this.data?.openDealsList || [], this.dealsSort);
  }

  get pagedDeals(): any[] {
    return paginateSlice(this.sortedDeals, this.dealsPage, this.pageSize);
  }

  get dealsTotalPages(): number {
    return totalPages(this.sortedDeals.length, this.pageSize);
  }

  get pagedTasks(): any[] {
    return paginateSlice(this.data?.tasks || [], this.tasksPage, this.pageSize);
  }

  get tasksTotalPages(): number {
    return totalPages((this.data?.tasks || []).length, this.pageSize);
  }

  get pagedReminders(): any[] {
    return paginateSlice(this.data?.reminders || [], this.remindersPage, this.pageSize);
  }

  get remindersTotalPages(): number {
    return totalPages((this.data?.reminders || []).length, this.pageSize);
  }

  onDealsSortChange() {
    this.dealsPage = 1;
  }

  prevDealsPage() {
    if (this.dealsPage > 1) this.dealsPage--;
  }

  nextDealsPage() {
    if (this.dealsPage < this.dealsTotalPages) this.dealsPage++;
  }

  prevTasksPage() {
    if (this.tasksPage > 1) this.tasksPage--;
  }

  nextTasksPage() {
    if (this.tasksPage < this.tasksTotalPages) this.tasksPage++;
  }

  prevRemindersPage() {
    if (this.remindersPage > 1) this.remindersPage--;
  }

  nextRemindersPage() {
    if (this.remindersPage < this.remindersTotalPages) this.remindersPage++;
  }

  askCopilot() {
    if (!this.chatMessage.trim()) return;
    this.api.copilotChat(this.chatMessage).subscribe(r => this.chatReply = r.reply);
  }
}
