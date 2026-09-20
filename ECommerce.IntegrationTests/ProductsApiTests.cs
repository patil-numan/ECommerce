using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using ClosedXML.Excel;
using ECommerce.Application.DTOs;
using Xunit;

namespace ECommerce.IntegrationTests;

public class ProductsApiTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;

    public ProductsApiTests(
        CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetProducts_WithCustomerRole_ReturnsSuccess()
    {
        using var request =
            new HttpRequestMessage(
                HttpMethod.Get,
                "/api/products");

        request.Headers.Add(
            "X-Test-Role",
            "Customer");

        var response =
            await _client.SendAsync(request);

        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);

        var products =
            await response.Content
                .ReadFromJsonAsync<List<ProductDto>>();

        Assert.NotNull(products);
    }

    [Fact]
    public async Task CreateProduct_WithCustomerRole_ReturnsForbidden()
    {
        using var request =
            new HttpRequestMessage(
                HttpMethod.Post,
                "/api/products");

        request.Headers.Add(
            "X-Test-Role",
            "Customer");

        request.Content =
            JsonContent.Create(
                new
                {
                    Name = "Test Product",
                    SKU = "TEST-001",
                    Price = 1000m,
                    StockQuantity = 10,
                    CategoryId = 1
                });

        var response =
            await _client.SendAsync(request);

        Assert.Equal(
            HttpStatusCode.Forbidden,
            response.StatusCode);
    }

    [Fact]
    public async Task BulkUpdate_WithCustomerRole_ReturnsForbidden()
    {
        using var excelStream =
            CreateExcelStream(
                new List<BulkProductRowDto>
                {
                    new BulkProductRowDto
                    {
                        SKU = "TEST-001",
                        Name = "Test Product",
                        Price = 1000m,
                        StockQuantity = 10,
                        Category = "Test Category"
                    }
                });

        using var request =
            new HttpRequestMessage(
                HttpMethod.Post,
                "/api/products/bulk-update");

        request.Headers.Add(
            "X-Test-Role",
            "Customer");

        request.Content =
            CreateMultipartContent(
                excelStream,
                "test.xlsx");

        var response =
            await _client.SendAsync(request);

        Assert.Equal(
            HttpStatusCode.Forbidden,
            response.StatusCode);
    }

    [Fact]
    public async Task BulkUpdate_WithEmptyFile_ReturnsBadRequest()
    {
        using var content =
            new MultipartFormDataContent();

        using var emptyStream =
            new MemoryStream();

        using var fileContent =
            new StreamContent(emptyStream);

        fileContent.Headers.ContentType =
            new MediaTypeHeaderValue(
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");

        content.Add(
            fileContent,
            "file",
            "empty.xlsx");

        using var request =
            new HttpRequestMessage(
                HttpMethod.Post,
                "/api/products/bulk-update");

        request.Headers.Add(
            "X-Test-Role",
            "Admin");

        request.Content = content;

        var response =
            await _client.SendAsync(request);

        Assert.Equal(
            HttpStatusCode.BadRequest,
            response.StatusCode);
    }

    [Fact]
    public async Task BulkUpdate_WithNonExcelFile_ReturnsBadRequest()
    {
        using var content =
            new MultipartFormDataContent();

        var textContent =
            new StringContent(
                "This is not an Excel file.");

        content.Add(
            textContent,
            "file",
            "test.txt");

        using var request =
            new HttpRequestMessage(
                HttpMethod.Post,
                "/api/products/bulk-update");

        request.Headers.Add(
            "X-Test-Role",
            "Admin");

        request.Content = content;

        var response =
            await _client.SendAsync(request);

        Assert.Equal(
            HttpStatusCode.BadRequest,
            response.StatusCode);
    }

    [Fact]
    public async Task BulkUpdate_WithAdminRoleAndValidExcel_ReturnsAccepted()
    {
        using var excelStream =
            CreateExcelStream(
                new List<BulkProductRowDto>
                {
                    new BulkProductRowDto
                    {
                        SKU = "LAP-001",
                        Name = "Integration Laptop",
                        Price = 85000m,
                        StockQuantity = 10,
                        Category = "Electronics"
                    }
                });

        using var request =
            new HttpRequestMessage(
                HttpMethod.Post,
                "/api/products/bulk-update");

        request.Headers.Add(
            "X-Test-Role",
            "Admin");

        request.Content =
            CreateMultipartContent(
                excelStream,
                "bulk-test.xlsx");

        var response =
            await _client.SendAsync(request);

        Assert.Equal(
            HttpStatusCode.Accepted,
            response.StatusCode);

        var responseBody =
            await response.Content
                .ReadFromJsonAsync<ImportJobResponse>();

        Assert.NotNull(responseBody);

        Assert.True(
            responseBody.JobId > 0);

        var jobStatus =
            await WaitForImportJobAsync(
                responseBody.JobId);

        Assert.NotNull(jobStatus);

        Assert.Equal(
            "Completed",
            jobStatus.Status);

        Assert.Equal(
            1,
            jobStatus.TotalRows);

        Assert.Equal(
            1,
            jobStatus.ProcessedRows);

        Assert.Equal(
            1,
            jobStatus.UpdatedRows);

        Assert.Equal(
            0,
            jobStatus.CreatedRows);

        Assert.Equal(
            0,
            jobStatus.FailedRows);
    }

    [Fact]
    public async Task BulkUpdate_WithNewProductAndCategory_CreatesBoth()
    {
        /*
         * Generate unique values for every test run.
         *
         * This prevents the test from accidentally finding
         * a product/category created by an earlier test run.
         */
        var uniqueId =
            Guid.NewGuid().ToString("N");

        var sku =
            $"INTEGRATION-NEW-{uniqueId}";

        var productName =
            $"Integration New Product {uniqueId}";

        var categoryName =
            $"Integration New Category {uniqueId}";

        const decimal price =
            12345m;

        const int stockQuantity =
            25;

        /*
         * Create the Excel file.
         */
        using var excelStream =
            CreateExcelStream(
                new List<BulkProductRowDto>
                {
                    new BulkProductRowDto
                    {
                        SKU = sku,
                        Name = productName,
                        Price = price,
                        StockQuantity = stockQuantity,
                        Category = categoryName
                    }
                });

        /*
         * Create the HTTP request.
         */
        using var request =
            new HttpRequestMessage(
                HttpMethod.Post,
                "/api/products/bulk-update");

        /*
         * The endpoint requires the Admin role.
         */
        request.Headers.Add(
            "X-Test-Role",
            "Admin");

        request.Content =
            CreateMultipartContent(
                excelStream,
                "new-product-test.xlsx");

        /*
         * Send the Excel file to the API.
         */
        var response =
            await _client.SendAsync(request);

        /*
         * Because the import runs in the background,
         * the API should immediately return 202 Accepted.
         */
        Assert.Equal(
            HttpStatusCode.Accepted,
            response.StatusCode);

        /*
         * Read the job ID returned by the API.
         */
        var responseBody =
            await response.Content
                .ReadFromJsonAsync<ImportJobResponse>();

        Assert.NotNull(responseBody);

        Assert.True(
            responseBody.JobId > 0);

        /*
         * Wait until the background worker finishes.
         */
        var jobStatus =
            await WaitForImportJobAsync(
                responseBody.JobId);

        Assert.NotNull(jobStatus);

        /*
         * Verify the import job completed successfully.
         */
        Assert.Equal(
            "Completed",
            jobStatus.Status);

        Assert.Equal(
            1,
            jobStatus.TotalRows);

        Assert.Equal(
            1,
            jobStatus.ProcessedRows);

        Assert.Equal(
            0,
            jobStatus.UpdatedRows);

        Assert.Equal(
            1,
            jobStatus.CreatedRows);

        Assert.Equal(
            0,
            jobStatus.FailedRows);

        /*
         * Now retrieve all products.
         */
        using var productsRequest =
            new HttpRequestMessage(
                HttpMethod.Get,
                "/api/products");

        productsRequest.Headers.Add(
            "X-Test-Role",
            "Admin");

        var productsResponse =
            await _client.SendAsync(
                productsRequest);

        Assert.Equal(
            HttpStatusCode.OK,
            productsResponse.StatusCode);

        var products =
            await productsResponse.Content
                .ReadFromJsonAsync<List<ProductDto>>();

        Assert.NotNull(products);

        /*
         * Find the exact product created by THIS test run.
         */
        var createdProduct =
            products.FirstOrDefault(
                product =>
                    string.Equals(
                        product.SKU,
                        sku,
                        StringComparison.OrdinalIgnoreCase));

        Assert.NotNull(
            createdProduct);

        /*
         * Verify the product's values.
         */
        Assert.Equal(
            productName,
            createdProduct.Name);

        Assert.Equal(
            price,
            createdProduct.Price);

        Assert.Equal(
            stockQuantity,
            createdProduct.StockQuantity);

        /*
         * Retrieve all categories.
         */
        using var categoriesRequest =
            new HttpRequestMessage(
                HttpMethod.Get,
                "/api/categories");

        categoriesRequest.Headers.Add(
            "X-Test-Role",
            "Admin");

        var categoriesResponse =
            await _client.SendAsync(
                categoriesRequest);

        Assert.Equal(
            HttpStatusCode.OK,
            categoriesResponse.StatusCode);

        var categories =
            await categoriesResponse.Content
                .ReadFromJsonAsync<List<CategoryDto>>();

        Assert.NotNull(categories);

        /*
         * Find the category created by THIS test run.
         */
        var createdCategory =
            categories.FirstOrDefault(
                category =>
                    string.Equals(
                        category.Name,
                        categoryName,
                        StringComparison.OrdinalIgnoreCase));

        Assert.NotNull(
            createdCategory);

        /*
         * Verify that the product is linked
         * to the correct category.
         */
        Assert.Equal(
            createdCategory.Id,
            createdProduct.CategoryId);
    }

    /*
     * Waits for the background import job to finish.
     *
     * The API returns immediately after queuing the job,
     * so the test cannot expect the job to already be completed.
     */
    private async Task<ImportJobStatusDto>
        WaitForImportJobAsync(
            int jobId)
    {
        const int maxAttempts = 30;

        for (var attempt = 0;
             attempt < maxAttempts;
             attempt++)
        {
            using var request =
                new HttpRequestMessage(
                    HttpMethod.Get,
                    $"/api/products/import-jobs/{jobId}");

            request.Headers.Add(
                "X-Test-Role",
                "Admin");

            var response =
                await _client.SendAsync(request);

            Assert.Equal(
                HttpStatusCode.OK,
                response.StatusCode);

            var jobStatus =
                await response.Content
                    .ReadFromJsonAsync<ImportJobStatusDto>();

            Assert.NotNull(jobStatus);

            if (jobStatus.Status == "Completed")
            {
                return jobStatus;
            }

            if (jobStatus.Status == "Failed")
            {
                throw new Exception(
                    $"Import job failed: {jobStatus.ErrorMessage}");
            }

            if (jobStatus.Status == "Cancelled")
            {
                throw new Exception(
                    "Import job was cancelled.");
            }

            /*
             * Give the background worker some time
             * to process the job before checking again.
             */
            await Task.Delay(
                TimeSpan.FromMilliseconds(200));
        }

        throw new TimeoutException(
            $"Import job {jobId} did not complete within the expected time.");
    }

    /*
     * Creates an Excel file in memory.
     *
     * The Excel structure is:
     *
     * SKU | Name | Price | StockQuantity | Category
     */
    private static MemoryStream CreateExcelStream(
        IEnumerable<BulkProductRowDto> rows)
    {
        var stream =
            new MemoryStream();

        using (var workbook =
            new XLWorkbook())
        {
            var worksheet =
                workbook.Worksheets.Add(
                    "Products");

            /*
             * Header row.
             */
            worksheet.Cell(1, 1)
                .Value = "SKU";

            worksheet.Cell(1, 2)
                .Value = "Name";

            worksheet.Cell(1, 3)
                .Value = "Price";

            worksheet.Cell(1, 4)
                .Value = "StockQuantity";

            worksheet.Cell(1, 5)
                .Value = "Category";

            /*
             * Data rows.
             */
            var rowNumber = 2;

            foreach (var row in rows)
            {
                worksheet.Cell(rowNumber, 1)
                    .Value = row.SKU;

                worksheet.Cell(rowNumber, 2)
                    .Value = row.Name;

                worksheet.Cell(rowNumber, 3)
                    .Value = row.Price;

                worksheet.Cell(rowNumber, 4)
                    .Value = row.StockQuantity;

                worksheet.Cell(rowNumber, 5)
                    .Value = row.Category;

                rowNumber++;
            }

            worksheet.Columns()
                .AdjustToContents();

            workbook.SaveAs(stream);
        }

        stream.Position = 0;

        return stream;
    }

    /*
     * Converts the Excel stream into multipart/form-data
     * so it can be uploaded to the API.
     */
    private static MultipartFormDataContent
        CreateMultipartContent(
            Stream stream,
            string fileName)
    {
        var content =
            new MultipartFormDataContent();

        var fileContent =
            new StreamContent(stream);

        fileContent.Headers.ContentType =
            new MediaTypeHeaderValue(
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");

        content.Add(
            fileContent,
            "file",
            fileName);

        return content;
    }

    /*
     * Represents the response returned when
     * a background import job is created.
     */
    private class ImportJobResponse
    {
        public int JobId { get; set; }
    }
}