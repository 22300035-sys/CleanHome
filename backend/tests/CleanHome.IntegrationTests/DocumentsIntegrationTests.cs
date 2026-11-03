using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using CleanHome.Application.Abstractions.Identity;
using CleanHome.Application.Features.Identity;
using CleanHome.Application.Features.Verification;
using CleanHome.Domain.Entities.Identity;
using CleanHome.Domain.Entities.Verification;
using CleanHome.Infrastructure.Persistence;
using CleanHome.Infrastructure.Security;
using CleanHome.IntegrationTests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace CleanHome.IntegrationTests;

[Collection("CleanHome database")]
public sealed class DocumentsIntegrationTests
{
    private const string TestPassword = "ClaveSeguraParaPruebas2026!";

    private readonly CleanHomeApiFixture _fixture;

    public DocumentsIntegrationTests(CleanHomeApiFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task Upload_WithoutToken_ReturnsUnauthorized()
    {
        using var content = CreateMultipartContent(
            new byte[] { 0xFF, 0xD8, 0xFF, 0xE0, 0x00, 0x10 },
            "image/jpeg",
            "portrait");

        var response = await _fixture.Client.PostAsync(
            "/api/v1/documents", content);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Upload_WithValidFakeJpeg_ReturnsCreatedWithPendingDocument()
    {
        var token = await CreateTokenAsync();

        using var content = CreateMultipartContent(
            new byte[] { 0xFF, 0xD8, 0xFF, 0xC0, 0x00, 0x11, 0x08, 0x04, 0x00, 0x04, 0x80, 0x03 },
            "image/jpeg",
            "portrait");

        var response = await SendUploadAsync(token, content);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var result = await response.Content
            .ReadFromJsonAsync<DocumentUploadResponse>();

        Assert.NotNull(result);
        Assert.NotEqual(Guid.Empty, result!.Id);
        Assert.Equal("portrait", result.DocumentType);
        Assert.Equal("clean", result.Status);
    }

    [Fact]
    public async Task Upload_WithDisguisedExecutable_ReturnsBadRequest()
    {
        var token = await CreateTokenAsync();

        using var content = CreateMultipartContent(
            new byte[] { 0x4D, 0x5A, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 },
            "application/octet-stream",
            "portrait");

        var response = await SendUploadAsync(token, content);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Upload_WithOversizedFile_ReturnsPayloadTooLarge()
    {
        var token = await CreateTokenAsync();

        using var content = CreateMultipartContent(
            new byte[10 * 1024 * 1024 + 1],
            "application/pdf",
            "portrait");

        var response = await SendUploadAsync(token, content);

        Assert.Equal(HttpStatusCode.RequestEntityTooLarge, response.StatusCode);
    }

    [Fact]
    public async Task Upload_WithForeignLocation_ReturnsForbidden()
    {
        Guid foreignLocationId;

        using (var scope = _fixture.CreateScope())
        {
            var db = scope.ServiceProvider
                .GetRequiredService<CleanHomeDbContext>();

            var owner = new User(
                $"owner-{Guid.NewGuid():N}@cleanhome.test",
                "Propietario",
                "Auditoria");

            db.Users.Add(owner);

            var credential = new UserCredential(
                owner.Id,
                scope.ServiceProvider
                    .GetRequiredService<IPasswordHasher>()
                    .HashPassword(TestPassword));

            db.UserCredentials.Add(credential);

            var location = new CustomerLocation(
                owner.Id,
                "Casa",
                "Calle Falsa",
                "123",
                null,
                "Colonia",
                "12345",
                "Ciudad",
                "Estado",
                null);

            db.CustomerLocations.Add(location);
            await db.SaveChangesAsync();

            foreignLocationId = location.Id;
        }

        var token = await CreateTokenAsync();

        using var content = CreateMultipartContent(
            new byte[] { 0x25, 0x50, 0x44, 0x46, 0x2D, 0x31, 0x2E, 0x34 },
            "application/pdf",
            "address_proof");

        content.Add(
            new StringContent(foreignLocationId.ToString()),
            "customerLocationId");

        var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/documents")
        {
            Content = content
        };

        request.Headers.Authorization =
            new AuthenticationHeaderValue("Bearer", token);

        var response = await _fixture.Client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Get_DocumentsRoute_WithoutToken_ReturnsUnauthorized()
    {
        var response = await _fixture.Client.GetAsync(
            $"/api/v1/documents/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Get_MyDocument_WithToken_DoesNotExposeStorageKey()
    {
        var token = await CreateTokenAsync();

        using var content = CreateMultipartContent(
            new byte[] { 0xFF, 0xD8, 0xFF, 0xC0, 0x00, 0x11, 0x08, 0x04, 0x00, 0x04, 0x80, 0x03 },
            "image/jpeg",
            "portrait");

        using var uploadRequest = new HttpRequestMessage(
            HttpMethod.Post, "/api/v1/documents")
        {
            Content = content
        };

        uploadRequest.Headers.Authorization =
            new AuthenticationHeaderValue("Bearer", token);

        using var uploadResponse = await _fixture.Client.SendAsync(
            uploadRequest);

        uploadResponse.EnsureSuccessStatusCode();

        var uploadResult = await uploadResponse.Content
            .ReadFromJsonAsync<DocumentUploadResponse>();

        Assert.NotNull(uploadResult);

        using var getRequest = new HttpRequestMessage(
            HttpMethod.Get, $"/api/v1/documents/{uploadResult!.Id}");

        getRequest.Headers.Authorization =
            new AuthenticationHeaderValue("Bearer", token);

        using var getResponse = await _fixture.Client.SendAsync(getRequest);

        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);

        var json = await getResponse.Content.ReadAsStringAsync();

        Assert.DoesNotContain(
            "storageKey", json, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("portrait", json);
    }

    [Fact]
    public async Task Get_MyDocuments_AsDifferentUser_DoesNotReturnForeignDocuments()
    {
        var token = await CreateTokenAsync();

        using var content = CreateMultipartContent(
            new byte[] { 0xFF, 0xD8, 0xFF, 0xC0, 0x00, 0x11, 0x08, 0x04, 0x00, 0x04, 0x80, 0x03 },
            "image/jpeg",
            "portrait");

        using var uploadRequest = new HttpRequestMessage(
            HttpMethod.Post, "/api/v1/documents")
        {
            Content = content
        };

        uploadRequest.Headers.Authorization =
            new AuthenticationHeaderValue("Bearer", token);

        using var uploadResponse = await _fixture.Client.SendAsync(uploadRequest);

        uploadResponse.EnsureSuccessStatusCode();

        var uploadResult = await uploadResponse.Content
            .ReadFromJsonAsync<DocumentUploadResponse>();

        Assert.NotNull(uploadResult);

        var foreignToken = await CreateTokenAsync();

        using var listRequest = new HttpRequestMessage(
            HttpMethod.Get, "/api/v1/documents");

        listRequest.Headers.Authorization =
            new AuthenticationHeaderValue("Bearer", foreignToken);

        using var listResponse = await _fixture.Client.SendAsync(listRequest);

        Assert.Equal(HttpStatusCode.OK, listResponse.StatusCode);

        var listResult = await listResponse.Content
            .ReadFromJsonAsync<List<UserDocumentResponse>>();

        Assert.NotNull(listResult);
        Assert.Empty(listResult!);
    }

    private static MultipartFormDataContent CreateMultipartContent(
        byte[] fileBytes,
        string contentType,
        string? documentType)
    {
        var content = new MultipartFormDataContent();

        var fileContent = new ByteArrayContent(fileBytes);
        fileContent.Headers.ContentType =
            new MediaTypeHeaderValue(contentType);

        content.Add(
            fileContent, "file", "documento.jpg");

        if (documentType is not null)
        {
            content.Add(
                new StringContent(documentType), "documentType");
        }

        return content;
    }

    private async Task<HttpResponseMessage> SendUploadAsync(
        string? token,
        MultipartFormDataContent content,
        CancellationToken cancellationToken = default)
    {
        using var request = new HttpRequestMessage(
            HttpMethod.Post, "/api/v1/documents")
        {
            Content = content
        };

        if (token is not null)
        {
            request.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", token);
        }

        return await _fixture.Client.SendAsync(
            request, cancellationToken);
    }

    private async Task<string> CreateTokenAsync()
    {
        using var scope = _fixture.CreateScope();

        var db = scope.ServiceProvider
            .GetRequiredService<CleanHomeDbContext>();

        var passwordHasher = scope.ServiceProvider
            .GetRequiredService<IPasswordHasher>();

        var user = new User(
            $"auditoria-{Guid.NewGuid():N}@cleanhome.test",
            "Cliente",
            "Auditoria");

        db.Users.Add(user);

        db.UserCredentials.Add(
            new UserCredential(
                user.Id,
                passwordHasher.HashPassword(TestPassword)));

        await db.SaveChangesAsync();

        var login = await _fixture.Client.PostAsJsonAsync(
            "/api/v1/auth/login",
            new
            {
                email = user.Email,
                password = TestPassword,
                deviceName = "Dispositivo Auditoria",
                platform = "web"
            });

        var response = await login.Content.ReadFromJsonAsync<AuthenticationResponse>();

        return response!.AccessToken;
    }
}
