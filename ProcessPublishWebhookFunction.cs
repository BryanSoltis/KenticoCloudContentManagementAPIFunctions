
using KenticoCloud.ContentManagement;
using KenticoCloud.ContentManagement.Models.Items;
using KenticoCloud.ContentManagement.Models.StronglyTyped;
using KenticoCloud.Delivery;
using KenticoCloudContentManagementAPIFunctions.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Host;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Security.Cryptography;
using System.Threading.Tasks;
using System.Xml.Linq;
using Microsoft.Extensions.Configuration;

namespace KenticoCloudContentManagementAPIFunctions
{
    public static class ProcessPublishWebhookFunction
    {
        private static bool blnValid = false;
        private static bool blnPublish = false;
        static string host = "https://api.microsofttranslator.com";
        static string path = "/V2/Http.svc/Translate";

        [FunctionName("ProcessPublishWebhookFunction")]
        public async static Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)]HttpRequest req, TraceWriter log, ExecutionContext context)
        {
            try
            {
                var config = new ConfigurationBuilder()
                    .SetBasePath(context.FunctionAppDirectory)
                    .AddJsonFile("local.settings.json", optional: true, reloadOnChange: true)
                    .AddEnvironmentVariables()
                    .Build();

                string strTranslatorAPIKey = config["TranslatorAPIKey"];
                string strLanguageCode = config["KenticoCloudLanguageCode"];

                DeliveryClient client = new DeliveryClient(config["KenticoCloudProjectID"], config["KenticoCloudPreviewAPIKey"]);

                System.Net.ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls11 | SecurityProtocolType.Tls12;

                // Get the content
                Task<string> body = new StreamReader(req.Body).ReadToEndAsync();
                string strCodename = body.Result.ToString();
                log.Info(strCodename);

                DeliveryItemResponse responseOriginal = await client.GetItemAsync(strCodename);
                if (responseOriginal != null)
                {
                    // Defines the content elements to update
                    ArticleModel NewArticleModel = new ArticleModel
                    {
                        OriginalCodename = responseOriginal.Item.System.Codename,
                        NewArticle = new Article
                        {
                            Title = await GetTranslatedText(strTranslatorAPIKey, responseOriginal.Item.GetString("title"), strLanguageCode),
                            Intro = await GetTranslatedText(strTranslatorAPIKey, responseOriginal.Item.GetString("intro"), strLanguageCode),
                            Body = await GetTranslatedText(strTranslatorAPIKey, responseOriginal.Item.GetString("body"), strLanguageCode)
                        }
                    };

                    return (ActionResult)new OkObjectResult(JsonConvert.SerializeObject(NewArticleModel));
                }
                else
                {
                    log.Info("Kentico Cloud item not found!");
                }
                log.Info($"No codename passed!");
            }
            catch (Exception ex)
            {
                log.Info(ex.Message);
            }

            return new OkObjectResult("FAILURE");
        }

        private async static Task<string> GetTranslatedText(string strTranslatorAPIKey, string strOriginal, string strLanguageCode)
        {
            try
            {
                string strTranslated = "";
                using (HttpClient client = new HttpClient())
                {
                    client.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", strTranslatorAPIKey);
                    string uri = host + path + "?to=" + strLanguageCode + "&text=" + System.Net.WebUtility.UrlEncode(strOriginal);
                    HttpResponseMessage translationresponse = await client.GetAsync(uri);
                    string result = await translationresponse.Content.ReadAsStringAsync();
                    strTranslated = XElement.Parse(result).Value;
                }
                return strTranslated;
            }
            catch (Exception)
            {
                return strOriginal;
            }

        }
    }
}
