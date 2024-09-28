using System;
using System.Collections.Generic;
using System.Diagnostics.Metrics;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using Markdig;
using NAudio.SoundFont;
using static System.Collections.Specialized.BitVector32;
using static System.Runtime.InteropServices.JavaScript.JSType;
using static OpenAI.ObjectModels.StaticValues.AssistantsStatics.MessageStatics;
using static OpenAI.ObjectModels.StaticValues.ImageStatics;

namespace TeamScriber.CommandLine
{
    internal class HtmlHelper
    {
        public async static Task GenerateHtml(Context context)
        {
            await GenerateHtmlFromMarkdown(context);

        }

        private static async Task GenerateHtmlFromMarkdown(Context context)
        {
            context.AnswersHtml = new List<string>();

            var pipeline = new MarkdownPipelineBuilder()
                .UseAdvancedExtensions()
                .UseSoftlineBreakAsHardlineBreak()
                .Build();

            foreach (var answerFile in context.AnswersMarkdown)
            {
                var answer = await File.ReadAllTextAsync(answerFile);

                var html = Markdown.ToHtml(answer, pipeline);

                html =
                    $$"""
                    <html>
                        <head>
                            <script src="https://cdn.jsdelivr.net/npm/mermaid/dist/mermaid.min.js"></script>
                            <script>mermaid.initialize({startOnLoad:true});</script>
                            <link rel="stylesheet" href="https://cdnjs.cloudflare.com/ajax/libs/github-markdown-css/5.6.1/github-markdown.min.css" />
                            <style>
                                .markdown-body {
                                    box-sizing: border-box;
                                    min-width: 200px;
                                    max-width: 980px;
                                    margin: 0 auto;
                                    padding: 45px;
                                }
                            </style>
                            <link href="https://cdnjs.cloudflare.com/ajax/libs/prism/1.29.0/themes/prism.min.css" rel="stylesheet" />
                            <link href="https://cdnjs.cloudflare.com/ajax/libs/prism/1.29.0/plugins/toolbar/prism-toolbar.min.css" rel="stylesheet" />
                        </head>
                        <body>
                            <div class=""markdown-body"">
                                {{html}}
                            </div>
                            <script src="https://cdnjs.cloudflare.com/ajax/libs/prism/1.29.0/components/prism-core.min.js"></script>
                            <script src="https://cdnjs.cloudflare.com/ajax/libs/prism/1.29.0/plugins/autoloader/prism-autoloader.min.js"></script>
                        </body>
                    </html>
                    """;

                var htmlFile = Path.ChangeExtension(answerFile, ".html");
                await File.WriteAllTextAsync(htmlFile, html);

                context.AnswersHtml.Add(htmlFile);
            }
        }
    }
}
