using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace TeamScriber
{
    internal class PromptsHelper
    {
        public static async Task<List<string>> GetPrompts(Context context)
        {
            var content = await GetContent(context.Options.Prompts);
            return content.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.RemoveEmptyEntries).ToList();
        }

        public static async Task<string> GetSystemPrompt(Context context)
        {
            return await GetContent(context.Options.SystemPrompts);
        }

        private static async Task<string> GetContent(string prompt)
        {
            if (IsUrl(prompt))
            {
                return await GetContentFromUrl(prompt);
            }
            else if (File.Exists(prompt))
            {
                return await File.ReadAllTextAsync(prompt);
            }
            else
            {
                throw new ArgumentException($"Invalid prompt specified: {prompt}");
            }
        }

        private static bool IsUrl(string prompt)
        {
            return Uri.TryCreate(prompt, UriKind.Absolute, out Uri uriResult)
                && (uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps);
        }

        private static async Task<string> GetContentFromUrl(string url)
        {
            using (HttpClient client = new HttpClient())
            {
                return await client.GetStringAsync(url);
                
            }
        }
    }
}
