using AdPlatformServ.DTO;
using AdPlatformServ.Services;
using Microsoft.AspNetCore.Mvc;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddSingleton<AdPlatformIndex>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Первый метод, Загрузка как «сырой текст» в JSON
app.MapPost("/api/platforms/upload-text", ([FromBody] UploadRequest req, AdPlatformIndex index) =>
{
    if (req is null || string.IsNullOrWhiteSpace(req.FileContent))
        return Results.BadRequest(new { error = "FileContent is required" });

    index.LoadFromString(req.FileContent);
    return Results.Ok(new { status = "ok" });
})
.Produces(StatusCodes.Status200OK)
.Produces(StatusCodes.Status400BadRequest);

// Второй метод, с возможностью загрусить в формате ФАЙЛа
app.MapPost("/api/platforms/upload-file", async ([FromForm] UploadFileRequest req, AdPlatformIndex index) =>
{
    if (req.File is null || req.File.Length == 0)
        return Results.BadRequest(new { error = "File is empty" });

    using var reader = new StreamReader(req.File.OpenReadStream());
    var content = await reader.ReadToEndAsync();
    index.LoadFromString(content);
    return Results.Ok(new { status = "ok" });
})
.Accepts<IFormFile>("multipart/form-data")
.Produces(StatusCodes.Status200OK)
.Produces(StatusCodes.Status400BadRequest);

// Метод третий, Поиск по локации
app.MapGet("/api/platforms/search", (string location, AdPlatformIndex index) =>
{
    var normalized = AdPlatformIndex.NormalizeLocation(location);
    if (normalized is null)
        return Results.BadRequest(new { error = "location is required" });

    var platforms = index.Search(normalized);
    return Results.Ok(new SearchResponse
    {
        Location = normalized,
        Platforms = platforms.ToList()
    });
})
.Produces<SearchResponse>(StatusCodes.Status200OK)
.Produces(StatusCodes.Status400BadRequest);

app.Run();