using Microsoft.Graph;
using Microsoft.Identity.Client;
using Microsoft.Kiota.Abstractions.Authentication;
using System.Net.Http.Headers;
using System.Text;

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
            double progressTranscriptionChunk = LogicConsts.ProgressWeightOneNoteImport / ((double)context.AnswersHtml02Embed.Count + 2);
            double progressTranscriptionChunksCompleted = context.ProgressInfo.Value + LogicConsts.ProgressWeightOneNoteImport;


            // Get or create a notebook
            var notebook = await GetOrCreateNotebookAsync("TeamScriber");
            context.ProgressInfo.Value += progressTranscriptionChunk;
            context.ProgressRepporter?.Report(context.ProgressInfo);

            // Get or create a section
            var section = await GetOrCreateSectionAsync(notebook.Id, "Imports");
            context.ProgressInfo.Value += progressTranscriptionChunk;
            context.ProgressRepporter?.Report(context.ProgressInfo);


            foreach (var answerFilename in context.AnswersHtml02Embed)
            {
                var answerContent = await File.ReadAllTextAsync(answerFilename);

                var title = Path.GetFileNameWithoutExtension(answerFilename).Replace(LogicConsts.MarkdownToHtmlEmbedFileSufixe, "");

                // Create a page with content
                await CreatePageWithContentAsync(notebook.Id, section.Id, title, answerContent);
                context.ProgressInfo.Value += progressTranscriptionChunk;
                context.ProgressRepporter?.Report(context.ProgressInfo);
            }

            context.ProgressInfo.Value = progressTranscriptionChunksCompleted;
            context.ProgressRepporter?.Report(context.ProgressInfo);
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
                {
                    AuthenticationResult authResult = null;
                    var app = PublicClientApp;

                    var accounts = await app.GetAccountsAsync();
                    var firstAccount = accounts.FirstOrDefault();

                    try
                    {
                        authResult = await app.AcquireTokenSilent(LogicConsts.MicrosoftGraphScopes, firstAccount)
                            .ExecuteAsync();
                    }
                    catch (MsalUiRequiredException ex)
                    {
                        // A MsalUiRequiredException happened on AcquireTokenSilent.
                        // This indicates you need to call AcquireTokenInteractive to acquire a token
                        System.Diagnostics.Debug.WriteLine($"MsalUiRequiredException: {ex.Message}");

                        try
                        {
                            authResult = await app.AcquireTokenInteractive(LogicConsts.MicrosoftGraphScopes)
                                .WithAccount(accounts.FirstOrDefault())
                                .WithPrompt(Prompt.SelectAccount)
                                .ExecuteAsync();

                            accessToken = authResult.AccessToken;

                            var authProvider = new BaseBearerTokenAuthenticationProvider(new TokenProvider(accessToken));
                            graphClient = new GraphServiceClient(authProvider);

                            var foo = await GetOrCreateNotebookAsync("TeamScriber");
                        }
                        catch (MsalException msalex)
                        {
                            Console.WriteLine($"Microsoft Graph Login Result: Error Acquiring Token:{System.Environment.NewLine}{msalex}");
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Microsoft Graph Login Result: Error Acquiring Token Silently:{System.Environment.NewLine}{ex}");
                        return null;
                    }
                }
            }

            return graphClient;
        }

        private static async Task<Microsoft.Graph.Models.Notebook> GetOrCreateNotebookAsync(string name)
        {
            var client = await GetAuthenticatedClient();

            // Check if the notebook already exists
            var notebooks = await client.Me.Onenote.Notebooks
                .GetAsync( requestConfiguration => 
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

        private static async Task CreatePageWithContentAsync(string notebookId, string sectionId, string title, string content)
        {
            var client = await GetAuthenticatedClient();

            var boundary = "MyPartBoundary" + new Random().Next(1000, 9999).ToString();

            var htmlContent = 
                $@"""
                <!DOCTYPE html>
                <html>
                <head>
                    <title>{title}</title>
                </head>
                <body>
                    {content}
                </body>
                </html>
                """;

            var body = $"--{boundary}\r\n" +
                       "Content-Disposition: form-data; name=\"Presentation\"\r\n" +
                       "Content-Type: text/html\r\n\r\n" +
                       $"{htmlContent}\r\n" +
                       $"--{boundary}--\r\n";

            using var clientHttp = new HttpClient();
            clientHttp.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

            var contentHttp = new StringContent(body, Encoding.UTF8, "application/xhtml+xml");
            var response = await clientHttp.PostAsync(
                $"https://graph.microsoft.com/v1.0/me/onenote/sections/{sectionId}/pages",
                contentHttp
            );

            response.EnsureSuccessStatusCode();

            if (!response.IsSuccessStatusCode)
            {
                var errorText = await response.Content.ReadAsStringAsync();
                throw new Exception($"HTTP error! Status: {response.StatusCode}, Message: {errorText}");
            }
        }


    }
}
