using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Storage;
using Microsoft.Azure.Storage.Blob;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace Prism.HomeAssistant.Functions;

public class NvrSaveClip
{
    private readonly HttpClient _httpClient;

    public NvrSaveClip(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    [FunctionName("NvrSaveClip")]
    public async Task<IActionResult> RunAsync([HttpTrigger(AuthorizationLevel.Function, "post", Route = null)] HttpRequest req, ILogger log)
    {
        log.LogInformation("C# HTTP trigger function processed a request.");

        var requestBody = await new StreamReader(req.Body).ReadToEndAsync();
        dynamic data = JsonConvert.DeserializeObject(requestBody);

        string eventId = data?.eventId;

        if (string.IsNullOrWhiteSpace(eventId))
        {
            return new BadRequestObjectResult("Please pass an eventId in the body");
        }

        var urlPattern = Environment.GetEnvironmentVariable("DOWNLOAD_URL_PATTERN");

        if (string.IsNullOrWhiteSpace(urlPattern))
        {
            return new BadRequestObjectResult("Please specify env : DOWNLOAD_URL_PATTERN");
        }

        var url = urlPattern.Replace("{{eventId}}", eventId);

        var binaryData = await _httpClient.GetByteArrayAsync(url);

        log.LogInformation("Downloaded {bytes} bytes of data", binaryData.Length);

        var storageConnectionString = Environment.GetEnvironmentVariable("STORAGE_CONNECTION_STRING");
        
        if (string.IsNullOrWhiteSpace(storageConnectionString))
        {
            return new BadRequestObjectResult("Please specify env : STORAGE_CONNECTION_STRING");
        }

        var storageAccount = CloudStorageAccount.Parse(storageConnectionString);
        var blobClient = storageAccount.CreateCloudBlobClient();
        var container = blobClient.GetContainerReference("clips");
        await container.CreateIfNotExistsAsync();

        var blob = container.GetBlockBlobReference($"{DateTime.Today:yyyy-MM-dd}/{DateTime.Now:HH-mm-ss}-{Guid.NewGuid()}.mp4");
        await blob.UploadFromStreamAsync(new MemoryStream(binaryData));
        
        log.LogInformation("Clip uploaded to blob {blobName}", blob.Name);

        return new OkResult();
    }
}