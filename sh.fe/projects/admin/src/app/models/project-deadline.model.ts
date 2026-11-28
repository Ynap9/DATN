import { IBaseRequestPaging } from '@/shared/models/request-paging.base.models';

export interface IFindProjectDeadline extends IBaseRequestPaging {
    // 1 sắp hết hạn, 2 đã hết hạn, 3 đang nghiệm thu đã quá hạn, bỏ trống = tất cả
    notificationType?: number;
}

export interface IProjectDeadline {
    projectId: number;
    name: string;
    status: number;
    startedAt: string | null;
    endedAt: string | null;
    daysLeft: number;
    deadlineType: number;
    lastNotifiedDate: string | null;
}

export interface IFindProjectDeadlineNotification extends IBaseRequestPaging {
    projectId?: number;
    notificationType?: number;
    isSuccess?: boolean;
    fromDate?: string;
    toDate?: string;
}

export interface IProjectDeadlineNotification {
    id: number;
    projectId: number;
    projectName: string;
    notificationType: number;
    endedAt: string;
    daysLeft: number;
    toAddress: string;
    subject: string;
    isSuccess: boolean;
    errorMessage: string | null;
    createdDate: string | null;
}

export interface IDeadlineJob {
    id: string;
    cron: string;
    timeZoneId: string | null;
    nextExecution: string | null;
    lastExecution: string | null;
    lastJobId: string | null;
    // Succeeded, Failed, Processing, Enqueued… hoặc null khi chưa chạy lần nào
    lastJobState: string | null;
    error: string | null;
}

export interface IDeadlineCheckStatus {
    // null = job chưa đăng ký trên máy chủ
    job: IDeadlineJob | null;
    warningDays: number;
    mailEnabled: boolean;
    recipients: string[];
}

export interface IRunDeadlineCheck {
    jobId: string;
}

// Dự án đang lọc ở tab Lịch sử mail
export interface IDeadlineProjectFilter {
    projectId: number;
    name: string;
}
