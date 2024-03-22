using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.AspNetCore.Mvc;

namespace AzureDevOps_QYWX_Webhook.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class WebhookController : ControllerBase
    {
        private const string URL = "https://qyapi.weixin.qq.com/cgi-bin/webhook/send?key=xxx";

        private readonly ILogger<WebhookController> _logger;

        private readonly HttpClient _httpClient;

        public WebhookController(ILogger<WebhookController> logger, HttpClient httpClient)
        {
            _logger = logger;
            _httpClient = httpClient;
        }

        /// <summary>
        /// 转发 Azure DevOps Webhook 通知到企业微信机器人
        /// </summary>
        /// <param name="json">请输入json格式数据 <see cref="https://developer.work.weixin.qq.com/document/path/91770"/> </param>
        /// <returns></returns>
        [HttpPost]
        [Route("PostNotification")]
        public async Task<IActionResult> PostNotification(object json)
        {
            _logger.LogInformation("Webhook received");
            
            
            if(json == null)
            {
                _logger.LogError("No content");
                return NoContent();
            }

            JsonElement ele = (JsonElement)json;

            string? md = ele.GetProperty("detailedMessage").GetProperty("markdown").GetString();

            if(string.IsNullOrWhiteSpace(md))
            {
                _logger.LogError("No markdown content");
                return NoContent();
            }

            await Send2QYWXBot(md);

            return Ok();
        }

        private async Task Send2QYWXBot(string md)
        {
            JsonObject markdown = new JsonObject
            {
                { "content", md }
            };

            JsonObject msg = new JsonObject
            {
                { "msgtype", "markdown" },
                { "markdown", markdown }
            };

            string jsonMessage = JsonSerializer.Serialize(msg);

            using var request = new HttpRequestMessage
            {
                Method = HttpMethod.Post,
                Headers =
                {
                    { HttpRequestHeader.ContentType.ToString(), "application/json" }
                },

                RequestUri = new Uri(URL),
                Content = new StringContent(jsonMessage, Encoding.UTF8, "application/json")
            };

            try
            {
                HttpResponseMessage response = await _httpClient.SendAsync(request);
                response.EnsureSuccessStatusCode();

                string? result = await response.Content.ReadAsStringAsync();
                _logger.LogInformation($"Message sent to QYWX Bot result: {result}");
            }
            catch (Exception ex)
            {
                // 处理错误，例如记录日志
                _logger.LogError($"Failed to send message to QYWX Bot: {ex.Message}");
            }
        }
    }
}

/*
 * 
 {
  "subscriptionId": "d5315316-b5f5-42b2-8b9a-69aa32b770ea",
  "notificationId": 4,
  "id": "03c164c2-8912-4d5e-8009-3707d5f83734",
  "eventType": "git.push",
  "publisherId": "tfs",
  "message": {
    "markdown": "Jamal Hartnett pushed updates to `Fabrikam-Fiber-Git`:`master`."
  },
  "detailedMessage": {
    "markdown": "Jamal Hartnett pushed a commit to [Fabrikam-Fiber-Git](https://fabrikam-fiber-inc.visualstudio.com/DefaultCollection/_git/Fabrikam-Fiber-Git/):[master](https://fabrikam-fiber-inc.visualstudio.com/DefaultCollection/_git/Fabrikam-Fiber-Git/#version=GBmaster).\n* Fixed bug in web.config file [33b55f7c](https://fabrikam-fiber-inc.visualstudio.com/DefaultCollection/_git/Fabrikam-Fiber-Git/commit/33b55f7cb7e7e245323987634f960cf4a6e6bc74)"
  },
  "resource": {
    "url": "https://fabrikam-fiber-inc.visualstudio.com/DefaultCollection/_apis/git/repositories/278d5cd2-584d-4b63-824a-2ba458937249/pushes/14",
    "pushId": 14
  },
  "resourceVersion": "1.0",
  "resourceContainers": {
    "collection": {
      "id": "c12d0eb8-e382-443b-9f9c-c52cba5014c2"
    },
    "account": {
      "id": "f844ec47-a9db-4511-8281-8b63f4eaf94e"
    },
    "project": {
      "id": "be9b3917-87e6-42a4-a549-2bc06a7a878f"
    }
  },
  "createdDate": "2024-03-22T01:40:45.145485Z"
}


 */