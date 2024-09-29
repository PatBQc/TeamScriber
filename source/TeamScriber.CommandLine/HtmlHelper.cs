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
using PuppeteerSharp;
using Markdig.Extensions.AutoIdentifiers;
using Svg;
using System.Reflection.Metadata;

namespace TeamScriber.CommandLine
{
    internal class HtmlHelper
    {
        public async static Task GenerateHtml(Context context)
        {
            context.AnswersHtml01Base = new List<string>();
            context.AnswersHtml02Embed = new List<string>();

            // TODO optimize


            foreach (var answerFile in context.AnswersMarkdown)
            {
                Console.WriteLine($"Generating HTML for {answerFile}");
                var htmlBaseFile = await GenerateHtmlFromMarkdown(answerFile, context);
                context.AnswersHtml01Base.Add(htmlBaseFile);

                Console.WriteLine($"Embeding HTML for {htmlBaseFile}");
                var htmlEmbedFile = await RenderHtmlAsync(htmlBaseFile, context);
                context.AnswersHtml02Embed.Add(htmlEmbedFile);
            }

        }

        private static async Task<string> GenerateHtmlFromMarkdown(string answerFile, Context context)
        {
            var pipeline = new MarkdownPipelineBuilder()
                .UseAdvancedExtensions()
                .UseAutoIdentifiers(AutoIdentifierOptions.GitHub) // Generate ID based on the heading text
                .UseSoftlineBreakAsHardlineBreak()
                .Build();

            var answer = await File.ReadAllTextAsync(answerFile);

            var toc = GenerateTableOfContents(answer); // Generate TOC from Markdown content

            var htmlContent = Markdown.ToHtml(answer, pipeline);

            htmlContent = htmlContent.Replace("<h1", "<br><br><br><h1");    

            var html =
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
                            <div class="markdown-body">
                                <h1>Table of Contents</h1>
                                {{toc}}
                                <br/><br/>
                                {{htmlContent}}
                            </div>
                            <script src="https://cdnjs.cloudflare.com/ajax/libs/prism/1.29.0/components/prism-core.min.js"></script>
                            <script src="https://cdnjs.cloudflare.com/ajax/libs/prism/1.29.0/plugins/autoloader/prism-autoloader.min.js"></script>
                        </body>
                    </html>
                    """;

            var htmlFile = Path.Combine(Path.GetDirectoryName(answerFile), Path.GetFileNameWithoutExtension(answerFile) + LogicConsts.MarkdownToHtmlBaseFileSufixe + ".html");

            await File.WriteAllTextAsync(htmlFile, html);

            return htmlFile;
        }

        private static async Task<string> RenderHtmlAsync(string htmlFile, Context context)
        {
            var htmlContent = await File.ReadAllTextAsync(htmlFile);

            // Launch the browser, assuming it's path
            using var browser = await Puppeteer.LaunchAsync(new LaunchOptions
            {
                Headless = true,
                ExecutablePath = LogicConsts.MarkdownToHtmlChromePath,
                Args = new[] { "--headless=new", "--disable-gpu", "--window-position=-2400,-2400" }
            });

            // Create a new page
            using var page = await browser.NewPageAsync();

            // Set the HTML content
            await page.SetContentAsync(htmlContent, new NavigationOptions
            {
                WaitUntil = new[] { WaitUntilNavigation.Networkidle0 }
            });

            // Wait for PrismJS and Mermaid to render
            await page.WaitForNetworkIdleAsync();

            // Process Mermaid diagrams (Step 2)
            await ReplaceMermaidWithImagesAsync(page);

            // Inline CSS styles (Step 3)
            await InlineStylesAsync(page);

            // Get the fully rendered HTML
            var renderedHtml = await page.GetContentAsync();

            string filename = htmlFile.Replace(LogicConsts.MarkdownToHtmlBaseFileSufixe, LogicConsts.MarkdownToHtmlEmbedFileSufixe);

            await File.WriteAllTextAsync(filename, renderedHtml);

            // Close the browser (handled by 'using' statement)
            return filename;
        }

        private static async Task ReplaceMermaidWithImagesAsync(IPage page)
        {
            // Initialize Mermaid if not already initialized
            await page.EvaluateExpressionAsync("mermaid.initialize({ startOnLoad: true });");

            // Wait for Mermaid diagrams to render
            await page.WaitForNetworkIdleAsync();

            // Adjust if neededoc
            await Task.Delay(1000);

            // Initialize Mermaid if not already initialized
            await page.EvaluateExpressionAsync("mermaid.initialize({ startOnLoad: true });");

            // Wait for all Mermaid diagrams to be rendered
            await page.WaitForFunctionAsync("() => document.querySelectorAll('.mermaid svg').length === document.querySelectorAll('.mermaid').length");

            // Select all Mermaid elements
            var mermaidElements = await page.QuerySelectorAllAsync(".mermaid");

            foreach (var element in mermaidElements)
            {
                // Extract the SVG content from the Mermaid element
                var svgContent = await element.EvaluateFunctionAsync<string>("(elem) => elem.innerHTML");

                // Create a new page to handle SVG to PNG conversion
                using (var svgPage = await page.Browser.NewPageAsync())
                {
                    // Create a minimal HTML that embeds the SVG content
                    string htmlTemplate = $@"
                        <!DOCTYPE html>
                        <html>
                        <body>
                            <div>{svgContent}</div>
                        </body>
                        </html>";

                    // Set the content with the embedded SVG
                    await svgPage.SetContentAsync(htmlTemplate);

                    // Wait for the SVG to load/render properly
                    await svgPage.WaitForSelectorAsync("svg");

                    // Evaluate the bounding box of the SVG element
                    var boundingBox = await svgPage.EvaluateFunctionAsync<BoundingBox>(@"() => {
                            const svgElement = document.querySelector('svg');
                            const rect = svgElement.getBoundingClientRect();
                            return {
                            x: rect.x,
                            y: rect.y,
                            width: rect.width,
                            height: rect.height
                            };
                        }
                        ");

                    // Take a screenshot of the specific SVG bounding box at 4x resolution
                    var screenshotOptions = new ScreenshotOptions
                    {
                        Type = ScreenshotType.Png,
                        Clip = new PuppeteerSharp.Media.Clip
                        {
                            X = boundingBox.X,
                            Y = boundingBox.Y,
                            Width = boundingBox.Width,
                            Height = boundingBox.Height,
                            Scale = 4 // Ensures the image is captured at 4x resolution
                        }
                    };

                    // Set the device scale factor to 4 to capture at 4x resolution
                    await svgPage.SetViewportAsync(new ViewPortOptions
                    {
                        DeviceScaleFactor = 4
                    });

                    // Take a screenshot and get it as a base64 string
                    var pngBase64 = await svgPage.ScreenshotBase64Async(screenshotOptions);

                    // Create the base64 img tag
                    string imgTag = $"<img src=\"data:image/png;base64,{pngBase64}\" alt=\"Mermaid Diagram\"/>";

                    // Replace the Mermaid element with the base64 img tag
                    await element.EvaluateFunctionAsync("(elem, img) => { elem.outerHTML = img; }", imgTag);
                }
            }
        }

        // BoundingBox class to capture bounding box dimensions
        class BoundingBox
        {
            public decimal X { get; set; }
            public decimal Y { get; set; }
            public decimal Width { get; set; }
            public decimal Height { get; set; }
        }

        private static async Task InlineStylesAsync(IPage page)
        {
            await page.EvaluateFunctionAsync(@"
                    () => {
                        // Create a new style element
                        var styleElement = document.createElement('style');
                        styleElement.type = 'text/css';

                        // Iterate over all stylesheets
                        for (var i = 0; i < document.styleSheets.length; i++) {
                            var sheet = document.styleSheets[i];
                            try {
                                var cssRules = sheet.cssRules;
                                if (cssRules) {
                                    for (var j = 0; j < cssRules.length; j++) {
                                        var rule = cssRules[j];
                                        styleElement.appendChild(document.createTextNode(rule.cssText));
                                    }
                                }
                            } catch (e) {
                                // Ignore CORS issues
                                console.warn('Could not access stylesheet: ', sheet.href);
                            }
                        }

                        // Append the new style element to the head
                        document.head.appendChild(styleElement);
                    }
                ");
        }

        
        private static string GenerateTableOfContents(string markdownContent)
        {
            // Simple regex to find headings (adjust as needed)
            var tocBuilder = new StringBuilder();
            tocBuilder.AppendLine("<ul>");

            var lines = markdownContent.Split('\n');
            foreach (var line in lines)
            {
                if (line.StartsWith("# ")) // Detect a heading (Markdown syntax)
                {
                    int level = line.TakeWhile(c => c == '#').Count(); // Determine heading level
                    string headingText = line.TrimStart('#').Trim();
                    string anchor = headingText.ToLower().Replace(" ", "-"); // Simple anchor generation (same as AutoIdentifiers)

                    tocBuilder.AppendLine($"<li><a href=\"#{anchor}\">{headingText}</a></li>");
                }
            }

            tocBuilder.AppendLine("</ul>");
            return tocBuilder.ToString();
        }




    }
}
