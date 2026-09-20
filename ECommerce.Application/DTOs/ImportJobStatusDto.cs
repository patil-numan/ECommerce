namespace ECommerce.Application.DTOs;

public class ImportJobStatusDto
{
    public int JobId { get; set; }

    public string FileName { get; set; } = string.Empty;

    public string Status { get; set; } = string.Empty;

    public int TotalRows { get; set; }

    public int ProcessedRows { get; set; }

    public int UpdatedRows { get; set; }

    public int CreatedRows { get; set; }

    public int FailedRows { get; set; }

    public int RetryCount { get; set; }

    public string? ErrorMessage { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? CompletedAt { get; set; }
}