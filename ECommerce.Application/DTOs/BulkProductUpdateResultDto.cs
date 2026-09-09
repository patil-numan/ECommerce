namespace ECommerce.Application.DTOs;

public class BulkProductUpdateResultDto
{
    public int UpdatedCount { get; set; }

    public int CreatedCount { get; set; }

    public int FailedCount { get; set; }

    public List<BulkProductUpdateErrorDto> Errors { get; set; } = new();
}

public class BulkProductUpdateErrorDto
{
    public int RowNumber { get; set; }

    public string SKU { get; set; } = string.Empty;

    public string Error { get; set; } = string.Empty;
}