using HtmlAgilityPack;
using Microsoft.Graph;
using Microsoft.Identity.Client;
using Microsoft.Kiota.Abstractions.Authentication;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reflection.Metadata;
using System.Text;
using static System.Net.Mime.MediaTypeNames;
using System.Security.Cryptography;
using Svg.Skia;
using SkiaSharp;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json.Linq;

namespace TeamScriber.CommandLine
{
    internal class OneNoteHelper
    {
        private static GraphServiceClient graphClient = null;
        private static IPublicClientApplication _clientApp;

        //Set the API Endpoint to Graph 'me' endpoint
        private const string graphAPIEndpoint = "https://graph.microsoft.com/v1.0/me";
        private static string accessToken = null;


        public async static Task ImportInOneNote(Context context)
        {
            Console.WriteLine("# Importing answers in OneNote");
            Console.WriteLine();

            double progressTranscriptionChunk = LogicConsts.ProgressWeightOneNoteImport / ((double)context.AnswersHtml02Embed.Count + 2);
            double progressTranscriptionChunksCompleted = context.ProgressInfo.Value + LogicConsts.ProgressWeightOneNoteImport;


            // Get or create a notebook
            Console.WriteLine("Getting or creating a notebook called TeamScriber...");
            var notebook = await GetOrCreateNotebookAsync("TeamScriber");
            context.ProgressInfo.Value += progressTranscriptionChunk;
            context.ProgressRepporter?.Report(context.ProgressInfo);
            Console.WriteLine("     Found it with id: " + notebook.Id);
            Console.WriteLine();


            // Get or create a section
            Console.WriteLine("Getting or creating a section called Imports...");
            var section = await GetOrCreateSectionAsync(notebook.Id, "Imports");
            context.ProgressInfo.Value += progressTranscriptionChunk;
            context.ProgressRepporter?.Report(context.ProgressInfo);
            Console.WriteLine("     Found it with id: " + section.Id);
            Console.WriteLine();

            int fileIndex = 0;
            foreach (var answerFilename in context.AnswersHtml02Embed)
            {
                var answerContent = await File.ReadAllTextAsync(answerFilename);

                var title = Path.GetFileNameWithoutExtension(answerFilename).Replace(LogicConsts.MarkdownToHtmlEmbedFileSufixe, "");

                Console.WriteLine($"Creating a page with content for \"{title}\"...");
                // Create a page with content
                await CreatePageWithContentAsync(notebook.Id, section.Id, title, answerContent);
                Console.WriteLine("     Page created");
                Console.WriteLine();

                Console.WriteLine($"Appending sub page with original transcription...");
                var transcription = await File.ReadAllTextAsync(context.Transcriptions[fileIndex]);
                await CreatePageWithContentAsync(notebook.Id, section.Id, "Transcription", transcription, 1);
                Console.WriteLine("     Page created");
                Console.WriteLine();

                Console.WriteLine($"Appending sub page with markdown...");
                var markdown = await File.ReadAllTextAsync(context.AnswersMarkdown[fileIndex]);
                await CreatePageWithContentAsync(notebook.Id, section.Id, "Markdown", markdown, 1);
                Console.WriteLine("     Page created");
                Console.WriteLine();

                context.ProgressInfo.Value += progressTranscriptionChunk;
                context.ProgressRepporter?.Report(context.ProgressInfo);

                ++fileIndex;
            }

            context.ProgressInfo.Value = progressTranscriptionChunksCompleted;
            context.ProgressRepporter?.Report(context.ProgressInfo);

            Console.WriteLine("--> Finished import to OneNote");
            Console.WriteLine();
            Console.WriteLine();
        }

        public static IPublicClientApplication PublicClientApp
        {
            get
            {
                if (_clientApp == null)
                {
                    _clientApp = PublicClientApplicationBuilder.Create(LogicConsts.MicrosoftGraphClientId)
                        .WithAuthority(AzureCloudInstance.AzurePublic, LogicConsts.MicrosoftGraphTenantIdCommon)
                        .WithRedirectUri(LogicConsts.MicrosoftGraphRedirectURI)
                        .Build();
                }

                return _clientApp;
            }
        }

        private async static Task<GraphServiceClient> GetAuthenticatedClient()
        {
            if (graphClient == null)
            {

                AuthenticationResult authResult = null;
                var app = PublicClientApp;

                var accounts = await app.GetAccountsAsync();
                var firstAccount = accounts.FirstOrDefault();

                // Let's try to get a token silently
                try
                {
                    authResult = await app.AcquireTokenSilent(LogicConsts.MicrosoftGraphScopes, firstAccount)
                        .ExecuteAsync();

                    // extract the token from the result
                    accessToken = authResult.AccessToken;

                    var authProvider = new BaseBearerTokenAuthenticationProvider(new TokenProvider(accessToken));
                    graphClient = new GraphServiceClient(authProvider);
                    return graphClient;
                }
                catch (MsalUiRequiredException ex)
                {
                    // A MsalUiRequiredException happened on AcquireTokenSilent.
                    // This indicates you need to call AcquireTokenInteractive to acquire a token
                    Console.WriteLine($"MsalUiRequiredException: {ex.Message}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Microsoft Graph Login Result: Error Acquiring Token Silently:{System.Environment.NewLine}{ex}");
                }

                //Let's try our saved token in the file before prompting the user
                try
                {
                    string encryptedToken = File.ReadAllText(LogicConsts.MicrosoftGraphTokenFilename);
                    accessToken = DecryptToken(encryptedToken);
                    var authProvider = new BaseBearerTokenAuthenticationProvider(new TokenProvider(accessToken));
                    var client = new GraphServiceClient(authProvider);

                    // Simple call on the graph to validate the token
                    var me = await client.Me.GetAsync();
                    graphClient = client;

                    return graphClient;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error decrypting token: {ex.Message}");
                }

                // Final option: prompt the user to sign-in
                try
                {
                    authResult = await app.AcquireTokenInteractive(LogicConsts.MicrosoftGraphScopes)
                        .WithAccount(accounts.FirstOrDefault())
                        .WithPrompt(Prompt.SelectAccount)
                        .ExecuteAsync();

                    accessToken = authResult.AccessToken;

                    string encryptedToken = EncryptToken(accessToken);
                    File.WriteAllText(LogicConsts.MicrosoftGraphTokenFilename, encryptedToken);
                    File.Encrypt(LogicConsts.MicrosoftGraphTokenFilename);

                    var authProvider = new BaseBearerTokenAuthenticationProvider(new TokenProvider(accessToken));
                    graphClient = new GraphServiceClient(authProvider);
                }
                catch (MsalException msalex)
                {
                    Console.WriteLine($"Microsoft Graph Login Result: Error Acquiring Token:{System.Environment.NewLine}{msalex}");
                }
            }

            return graphClient;
        }

        private static async Task<Microsoft.Graph.Models.Notebook> GetOrCreateNotebookAsync(string name)
        {
            var client = await GetAuthenticatedClient();

            // Check if the notebook already exists
            var notebooks = await client.Me.Onenote.Notebooks
                .GetAsync(requestConfiguration =>
                {
                    requestConfiguration.QueryParameters.Filter = $"displayName eq '{name}'";
                });

            if (notebooks?.Value != null && notebooks.Value.Any())
            {
                // Return the first notebook that matches the name
                return notebooks.Value.First();
            }

            // Create the notebook if it doesn't exist
            var newNotebook = new Microsoft.Graph.Models.Notebook
            {
                DisplayName = name
            };

            return await client.Me.Onenote.Notebooks.PostAsync(newNotebook);
        }

        private static async Task<Microsoft.Graph.Models.OnenoteSection> GetOrCreateSectionAsync(string notebookId, string name)
        {
            var client = await GetAuthenticatedClient();

            // Check if the section already exists
            var sections = await client.Me.Onenote.Notebooks[notebookId].Sections
                .GetAsync(requestConfiguration =>
                {
                    requestConfiguration.QueryParameters.Filter = $"displayName eq '{name}'";
                });

            if (sections?.Value != null && sections.Value.Any())
            {
                // Return the first section that matches the name
                return sections.Value.First();
            }

            // Create the section if it doesn't exist
            var newSection = new Microsoft.Graph.Models.OnenoteSection
            {
                DisplayName = name
            };

            return await client.Me.Onenote.Notebooks[notebookId].Sections.PostAsync(newSection);
        }

        private static async Task CreatePageWithContentAsync(string notebookId, string sectionId, string title, string pageContent, int level = 0)
        {
            HtmlDocument htmlDocument = new HtmlDocument();
            htmlDocument.LoadHtml(pageContent);

            // replace svg nodes with png instead
            var svgNodes = htmlDocument.DocumentNode.SelectNodes("//svg");
            if (svgNodes != null)
            {
                foreach (var svgNode in svgNodes)
                {
                    var svgImageText = svgNode.OuterHtml;
                    var pngImage = ConvertSvgToPng(svgImageText);
                    var pngBase64 = Convert.ToBase64String(pngImage);
                    var pngImageText = $"<img src=\"data:image/png;base64,{pngBase64}\" />";
                    svgNode.ParentNode.ReplaceChild(HtmlNode.CreateNode(pngImageText), svgNode);
                }
            }

            // Get the body node
            var bodyNode = htmlDocument.DocumentNode.SelectSingleNode("//body");
            var innerHtml = string.Empty;

            // If we received raw text or an not conform html 
            if (bodyNode == null)
            {

                innerHtml = System.Web.HttpUtility.HtmlEncode(pageContent);
            }
            else
            {
                innerHtml = bodyNode.InnerHtml;
            }

            var htmlContent =
                $"""
                <!DOCTYPE html>
                <html>
                <head>
                    <title>{title}</title>
                    <meta name="created" content="{DateTime.Now.ToString("o")}" />
                </head>
                <body>
                    {innerHtml}
                </body>
                </html>
                """;

            using var client = new HttpClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

            var content = new MultipartFormDataContent("boundary-" + Guid.NewGuid().ToString());
            content.Add(new StringContent(htmlContent, Encoding.UTF8, "text/html"), "Presentation");

            var response = await client.PostAsync(
                $"https://graph.microsoft.com/v1.0/me/onenote/sections/{sectionId}/pages",
                content
            );

            response.EnsureSuccessStatusCode();

            if (!response.IsSuccessStatusCode)
            {
                var errorText = await response.Content.ReadAsStringAsync();
                Console.Out.WriteLine("/!\\ Error creating OneNote Page: " + errorText);
                throw new Exception($"HTTP error! Status: {response.StatusCode}, Message: {errorText}");
            }

            if (level > 0)
            {
                // Let's take some time to reflect on life mystery, like the time it might take to get from an API call to the next where you ressource is ready
                Task.Delay(5000);

                var responseText = await response.Content.ReadAsStringAsync();

                // Parse the JSON string
                JObject jsonObject = JObject.Parse(responseText);

                string id = jsonObject["id"].ToString();
                var contentLevel = new StringContent("{\"level\": " + level + "}", Encoding.UTF8, "application/json");
                var responseLevel = await client.PatchAsync($"https://graph.microsoft.com/v1.0/me/onenote/pages/{id}", contentLevel);

                responseLevel.EnsureSuccessStatusCode();

                if (!responseLevel.IsSuccessStatusCode)
                {
                    var errorText = await response.Content.ReadAsStringAsync();
                    Console.Out.WriteLine("/!\\ Error creating OneNote Page: " + errorText);
                    throw new Exception($"HTTP error! Status: {response.StatusCode}, Message: {errorText}");
                }
            }
        }

        private static byte[] ConvertSvgToPng(string svgImageText)
        {
            svgImageText = svgImageText.Replace("<br>", "");

            // Convert SVG text to a byte stream
            using var stream = new MemoryStream(Encoding.UTF8.GetBytes(svgImageText));

            // Load the SVG using Svg.Skia
            var svg = new SKSvg();
            svg.Load(stream);

            // Get the bounds of the SVG content
            var bounds = svg.Picture.CullRect;

            // Calculate the aspect ratio (width / height)
            float aspectRatio = bounds.Width / bounds.Height;

            // Set the desired width and height
            int width = 2048;
            int height = (int)(width / aspectRatio);

            // Create a bitmap with the desired dimensions
            using var bitmap = new SKBitmap(width, height);
            using var canvas = new SKCanvas(bitmap);

            // Set the background to transparent
            canvas.Clear(SKColors.Transparent);

            // Scale the SVG to fit the canvas
            var scaleX = (float)width / bounds.Width;
            var scaleY = (float)height / bounds.Height;
            var matrix = SKMatrix.CreateScale(scaleX, scaleY);

            // Draw the SVG onto the canvas
            canvas.DrawPicture(svg.Picture, ref matrix);

            // Save the result as a PNG file
            using var image = SKImage.FromBitmap(bitmap);
            using var data = image.Encode(SKEncodedImageFormat.Png, 100);
            using var outputStream = new MemoryStream();
            data.SaveTo(outputStream);

            return outputStream.ToArray();
        }


        public static string EncryptToken(string token)
        {
            byte[] encryptedData = ProtectedData.Protect(
                Encoding.UTF8.GetBytes(token),
                null,
                DataProtectionScope.CurrentUser);
            return Convert.ToBase64String(encryptedData);
        }

        public static string DecryptToken(string encryptedToken)
        {
            byte[] decryptedData = ProtectedData.Unprotect(
                Convert.FromBase64String(encryptedToken),
                null,
                DataProtectionScope.CurrentUser);
            return Encoding.UTF8.GetString(decryptedData);
        }

    }
}
