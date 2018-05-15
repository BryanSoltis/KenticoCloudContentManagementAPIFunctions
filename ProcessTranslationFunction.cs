
using System.IO;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.WebJobs.Host;
using Newtonsoft.Json;
using KenticoCloud.ContentManagement;
using KenticoCloud.ContentManagement.Models.Items;
using KenticoCloudContentManagementAPIFunctions.Models;
using System.Threading.Tasks;
using KenticoCloud.ContentManagement.Models.StronglyTyped;
using System;
using Microsoft.Extensions.Configuration;

namespace KenticoCloudContentManagementAPIFunctions
{
    public static class ProcessTranslationFunction
    {
        [FunctionName("ProcessTranslationFunction")]
        public async static Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)]HttpRequest req, TraceWriter log, ExecutionContext context)
        {
            try
            { 
                var config = new ConfigurationBuilder()
                    .SetBasePath(context.FunctionAppDirectory)
                    .AddJsonFile("local.settings.json", optional: true, reloadOnChange: true)
                    .AddEnvironmentVariables()
                    .Build();

                
                string strLanguageCode = config["KenticoCloudLanguageCode"];

                ContentManagementOptions options = new ContentManagementOptions
                {
                    ProjectId = config["KenticoCloudProjectID"],
                    ApiKey = config["KenticoCloudContentManagementAPIKey"]
                };

                // Initializes an instance of the ContentManagementClient client
                ContentManagementClient client = new ContentManagementClient(options);

                // Defines the content elements to update
                Task<string> body = new StreamReader(req.Body).ReadToEndAsync();

                ArticleModel NewArticleModel = JsonConvert.DeserializeObject<ArticleModel>(body.Result.ToString());

                // Specifies the content item and the language variant
                ContentItemIdentifier itemIdentifier = ContentItemIdentifier.ByCodename(NewArticleModel.OriginalCodename);
                LanguageIdentifier languageIdentifier = LanguageIdentifier.ByCodename(strLanguageCode);
                ContentItemVariantIdentifier identifier = new ContentItemVariantIdentifier(itemIdentifier, languageIdentifier);

                // Upserts a language variant of your content item
                ContentItemVariantModel<Article> responseUpdate = await client.UpsertContentItemVariantAsync<Article>(identifier, NewArticleModel.NewArticle);

                return (ActionResult)new OkObjectResult($"SUCCESS: Language variant added!");
            }
            catch (Exception ex)
            {
                return new OkObjectResult("FAILURE: " + ex.Message);
            }
        }
    }
}
