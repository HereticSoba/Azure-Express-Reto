using Azure.Identity;
using Azure.Storage.Blobs;
using System.Net;

var builder = WebApplication.CreateBuilder(args);

string storageAccountName = builder.Configuration["STORAGE_ACCOUNT_NAME"]
    ?? throw new InvalidOperationException("Falta la variable de configuración STORAGE_ACCOUNT_NAME.");
string containerName = builder.Configuration["STORAGE_CONTAINER_NAME"] ?? "mensajes";
string blobName = builder.Configuration["STORAGE_BLOB_NAME"] ?? "mensaje.txt";

Uri blobEndpoint = new($"https://{storageAccountName}.blob.core.windows.net");

// DefaultAzureCredential usa la identidad administrada de App Service en Azure.
builder.Services.AddSingleton(new BlobServiceClient(blobEndpoint, new DefaultAzureCredential()));

var app = builder.Build();

app.MapGet("/", async (BlobServiceClient blobServiceClient) =>
{
    try
    {
        var containerClient = blobServiceClient.GetBlobContainerClient(containerName);
        var blobClient = containerClient.GetBlobClient(blobName);

        if (!await blobClient.ExistsAsync())
        {
            return Results.NotFound($"El archivo {containerName}/{blobName} no existe en Storage.");
        }

        var downloadResult = await blobClient.DownloadContentAsync();
        string mensaje = WebUtility.HtmlEncode(downloadResult.Value.Content.ToString());

        return Results.Content($"""
            <!DOCTYPE html>
            <html lang="es">
            <head>
                <meta charset="utf-8">
                <meta name="viewport" content="width=device-width, initial-scale=1">
                <title>Reto Azure Express</title>
            </head>
            <body style="margin: 0; min-height: 100vh; display: grid; place-items: center; background: linear-gradient(135deg, #eaf3ff, #f7fbff); font-family: Segoe UI, sans-serif;">
                <main style="max-width: 680px; margin: 24px; padding: 40px; text-align: center; background: #fff; border-radius: 18px; box-shadow: 0 12px 40px rgba(0, 60, 120, .12);">
                    <h1 style="margin: 0 0 8px; color: #0078D4;">Reto Azure Express</h1>
                    <p style="margin: 0 0 28px; color: #5b6b7c;">Mensaje leído desde Azure Blob Storage</p>
                    <p style="margin: 0; padding: 22px; border-radius: 12px; background: #eef6ff; color: #123; font-size: 1.25rem; line-height: 1.5;">
                        {mensaje}
                    </p>
                    <div style="margin-top: 28px; display: flex; flex-wrap: wrap; gap: 10px; justify-content: center; font-size: .9rem;">
                        <span style="padding: 8px 14px; border-radius: 999px; background: #e8f5e9; color: #1b5e20;">🔐 Managed Identity</span>
                        <span style="padding: 8px 14px; border-radius: 999px; background: #e3f2fd; color: #0d47a1;">📦 {containerName}/{blobName}</span>
                        <span style="padding: 8px 14px; border-radius: 999px; background: #fff3e0; color: #e65100;">🚫 Sin claves en el código</span>
                    </div>
                </main>
            </body>
            </html>
            """, "text/html; charset=utf-8");
    }
    catch (Exception ex)
    {
        return Results.Problem($"Error al acceder a Azure Storage: {ex.Message}");
    }
});

app.Run();
