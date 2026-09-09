using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using ClosedXML.Excel;
using ECommerce.Application.DTOs;
using Xunit;

namespace ECommerce.IntegrationTests;

public class ProductsApiTests
    : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;

    public ProductsApiTests(
        CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    // =========================================================
    // GET PRODUCTS
    // =========================================================

    [Fact]
    public async Task GetProducts_WithAuthenticatedCustomer_ReturnsSuccess()
    {
        var response =
            await _client.GetAsync("/api/products");

        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);
    }

    // =========================================================
    // CREATE PRODUCT
    // =========================================================

    [Fact]
    public async Task CreateProduct_WithCustomerRole_ReturnsForbidden()
    {
        var dto = new CreateProductDto
        {
            Name = "Integration Test Product",
            Description = "Test product",
            Price = 100m,
            CategoryId = 1,
            StockQuantity = 10
        };

        var response =
            await _client.PostAsJsonAsync(
                "/api/products",
                dto);

        Assert.Equal(
            HttpStatusCode.Forbidden,
            response.StatusCode);
    }

    // =========================================================
    // BULK UPDATE - CUSTOMER AUTHORIZATION
    // =========================================================

    [Fact]
    public async Task BulkUpdate_WithCustomerRole_ReturnsForbidden()
    {
        using var excelStream =
            CreateExcelStream(
                new[]
                {
                    new[]
                    {
                        "LAP-001",
                        "Updated Laptop",
                        "75000",
                        "20",
                        "Electronics"
                    }
                });

        using var content =
            CreateMultipartContent(
                excelStream,
                "products.xlsx");

        var response =
            await _client.PostAsync(
                "/api/products/bulk-update",
                content);

        Assert.Equal(
            HttpStatusCode.Forbidden,
            response.StatusCode);
    }

    // =========================================================
    // BULK UPDATE - EMPTY FILE
    // =========================================================

    [Fact]
    public async Task BulkUpdate_WithEmptyFile_ReturnsBadRequest()
    {
        using var content =
            new MultipartFormDataContent();

        using var emptyContent =
            new ByteArrayContent(
                Array.Empty<byte>());

        emptyContent.Headers.ContentType =
            new MediaTypeHeaderValue(
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");

        content.Add(
            emptyContent,
            "file",
            "products.xlsx");

        using var request =
            new HttpRequestMessage(
                HttpMethod.Post,
                "/api/products/bulk-update");

        request.Content = content;

        request.Headers.Add(
            "X-Test-Role",
            "Admin");

        var response =
            await _client.SendAsync(request);

        Assert.Equal(
            HttpStatusCode.BadRequest,
            response.StatusCode);
    }

    // =========================================================
    // BULK UPDATE - INVALID FILE TYPE
    // =========================================================

    [Fact]
    public async Task BulkUpdate_WithNonExcelFile_ReturnsBadRequest()
    {
        using var content =
            new MultipartFormDataContent();

        var fileContent =
            new ByteArrayContent(
                "This is not an Excel file."u8.ToArray());

        fileContent.Headers.ContentType =
            new MediaTypeHeaderValue(
                "text/plain");

        content.Add(
            fileContent,
            "file",
            "products.txt");

        using var request =
            new HttpRequestMessage(
                HttpMethod.Post,
                "/api/products/bulk-update");

        request.Content = content;

        request.Headers.Add(
            "X-Test-Role",
            "Admin");

        var response =
            await _client.SendAsync(request);

        Assert.Equal(
            HttpStatusCode.BadRequest,
            response.StatusCode);
    }

    // =========================================================
    // BULK UPDATE - VALID ADMIN REQUEST
    // =========================================================

    [Fact]
    public async Task BulkUpdate_WithAdminRoleAndValidExcel_ReturnsSuccess()
    {
        // -----------------------------------------------------
        // Get existing products
        // -----------------------------------------------------

        var productsResponse =
            await _client.GetAsync("/api/products");

        Assert.Equal(
            HttpStatusCode.OK,
            productsResponse.StatusCode);

        var products =
            await productsResponse.Content
                .ReadFromJsonAsync<List<ProductDto>>();

        Assert.NotNull(products);
        Assert.NotEmpty(products);

        var existingProduct =
            products.First();

        // -----------------------------------------------------
        // Get categories
        // -----------------------------------------------------

        var categoriesResponse =
            await _client.GetAsync("/api/categories");

        Assert.Equal(
            HttpStatusCode.OK,
            categoriesResponse.StatusCode);

        var categories =
            await categoriesResponse.Content
                .ReadFromJsonAsync<List<CategoryDto>>();

        Assert.NotNull(categories);
        Assert.NotEmpty(categories);

        var category =
            categories.First(
                c => c.Id == existingProduct.CategoryId);

        // -----------------------------------------------------
        // Create Excel using the new format:
        //
        // SKU | Name | Price | StockQuantity | Category
        // -----------------------------------------------------

        using var excelStream =
            CreateExcelStream(
                new[]
                {
                    new[]
                    {
                        existingProduct.SKU,
                        existingProduct.Name,
                        existingProduct.Price.ToString(),
                        existingProduct.StockQuantity.ToString(),
                        category.Name
                    }
                });

        using var content =
            CreateMultipartContent(
                excelStream,
                "products.xlsx");

        using var request =
            new HttpRequestMessage(
                HttpMethod.Post,
                "/api/products/bulk-update");

        request.Content = content;

        request.Headers.Add(
            "X-Test-Role",
            "Admin");

        // -----------------------------------------------------
        // Act
        // -----------------------------------------------------

        var response =
            await _client.SendAsync(request);

        // -----------------------------------------------------
        // Assert
        // -----------------------------------------------------

        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);

        var result =
            await response.Content
                .ReadFromJsonAsync<BulkProductUpdateResultDto>();

        Assert.NotNull(result);

        Assert.Equal(
            1,
            result.UpdatedCount);

        Assert.Equal(
            0,
            result.CreatedCount);

        Assert.Equal(
            0,
            result.FailedCount);

        Assert.Empty(
            result.Errors);
    }

    // =========================================================
    // HELPERS
    // =========================================================

    private static MemoryStream CreateExcelStream(
        IEnumerable<string[]> rows)
    {
        var stream =
            new MemoryStream();

        using (var workbook =
               new XLWorkbook())
        {
            var worksheet =
                workbook.Worksheets.Add("Products");

            // -------------------------------------------------
            // NEW BULK IMPORT HEADERS
            // -------------------------------------------------

            worksheet.Cell(1, 1).Value =
                "SKU";

            worksheet.Cell(1, 2).Value =
                "Name";

            worksheet.Cell(1, 3).Value =
                "Price";

            worksheet.Cell(1, 4).Value =
                "StockQuantity";

            worksheet.Cell(1, 5).Value =
                "Category";

            // -------------------------------------------------
            // DATA
            // -------------------------------------------------

            var rowNumber = 2;

            foreach (var row in rows)
            {
                for (
                    var column = 0;
                    column < row.Length;
                    column++)
                {
                    worksheet.Cell(
                        rowNumber,
                        column + 1).Value =
                        row[column];
                }

                rowNumber++;
            }

            workbook.SaveAs(stream);
        }

        stream.Position = 0;

        return stream;
    }

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
}