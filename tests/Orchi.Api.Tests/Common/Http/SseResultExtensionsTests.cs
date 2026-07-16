using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Orchi.Api.Common.Http;
using Orchi.Api.Common.Results;

namespace Orchi.Api.Tests.Common.Http;

public class SseResultExtensionsTests
{
    [Fact]
    public async Task WriteErrorAsync_NotFound_Returns404WithCodeAndMessage()
    {
        DefaultHttpContext httpContext = CreateHttpContext();
        Error error = Error.NotFound("Chat 'abc' was not found.");

        await httpContext.Response.WriteErrorAsync(error, CancellationToken.None);

        Assert.Equal(StatusCodes.Status404NotFound, httpContext.Response.StatusCode);
        using JsonDocument document = await ReadResponseJson(httpContext);
        Assert.Equal("NotFound", document.RootElement.GetProperty("code").GetString());
        Assert.Equal("Chat 'abc' was not found.", document.RootElement.GetProperty("message").GetString());
    }

    [Fact]
    public async Task WriteErrorAsync_ValidationError_Returns400WithErrorsObject()
    {
        DefaultHttpContext httpContext = CreateHttpContext();
        var error = ValidationError.FromErrors([
            Error.Validation("Mode.Invalid", "Kick off all is only available for orchestration chats.")
        ]);

        await httpContext.Response.WriteErrorAsync(error, CancellationToken.None);

        Assert.Equal(StatusCodes.Status400BadRequest, httpContext.Response.StatusCode);
        using JsonDocument document = await ReadResponseJson(httpContext);
        JsonElement errors = document.RootElement.GetProperty("errors");
        Assert.Equal(
            "Kick off all is only available for orchestration chats.",
            errors.GetProperty("Mode.Invalid")[0]!.GetString());
    }

    [Fact]
    public async Task WriteErrorAsync_GenericError_Returns400WithProblemShape()
    {
        DefaultHttpContext httpContext = CreateHttpContext();
        Error error = Error.Validation("Message.Required", "Message content is required.");

        await httpContext.Response.WriteErrorAsync(error, CancellationToken.None);

        Assert.Equal(StatusCodes.Status400BadRequest, httpContext.Response.StatusCode);
        using JsonDocument document = await ReadResponseJson(httpContext);
        Assert.Equal("Message.Required", document.RootElement.GetProperty("title").GetString());
        Assert.Equal("Message content is required.", document.RootElement.GetProperty("detail").GetString());
    }

    private static DefaultHttpContext CreateHttpContext()
    {
        var httpContext = new DefaultHttpContext();
        httpContext.Response.Body = new MemoryStream();
        return httpContext;
    }

    private static async Task<JsonDocument> ReadResponseJson(HttpContext httpContext)
    {
        httpContext.Response.Body.Seek(0, SeekOrigin.Begin);
        return await JsonDocument.ParseAsync(httpContext.Response.Body);
    }
}
