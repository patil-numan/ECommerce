export interface ImportJobResponse {
  message: string;
  jobId: number;
}

export interface ImportJobStatus {
  jobId: number;
  fileName: string;
  status: string;
  totalRows: number;
  processedRows: number;
  updatedRows: number;
  createdRows: number;
  failedRows: number;
  errorMessage: string | null;
  createdAt: string;
  completedAt: string | null;
}