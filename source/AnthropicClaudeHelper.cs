﻿using Microsoft.VisualBasic;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using NAudio.Wave;
using NAudio.MediaFoundation;
using System.IO;
using Anthropic;
using Anthropic.Services;
using OpenAI.Managers;
using OpenAI.ObjectModels.RequestModels;
using Anthropic.ApiModels.RequestModels;
using Anthropic.ApiModels.SharedModels;

namespace TeamScriber
{
    internal class AnthropicClaudeHelper
    {
        public async static Task GenerateAnswers(Context context)
        {
            // We wont validate the model name, just send it directly as is to OpenAI
            var model = context.Options.Model.Split(':')[1];

            // Configure your OpenAI API key
            if (string.IsNullOrEmpty(context.Options.OpenAIAPIKey))
            {
                context.Options.AnthropicAPIKey = Environment.GetEnvironmentVariable("ANTHROPIC_API_KEY");
            }

            var anthropicAiService = new AnthropicService(new()
            {
                ApiKey = context.Options.AnthropicAPIKey
            });

            var prompts = await PromptsHelper.GetPrompts(context);

            foreach (var transcriptionFilename in context.Transcriptions)
            {
                var transcription = await File.ReadAllTextAsync(transcriptionFilename);
                var systemPrompt = await PromptsHelper.GetSystemPrompt(context);
                systemPrompt += Environment.NewLine + Environment.NewLine + TranslationHelper.GetTranslation("en") + Environment.NewLine + transcription;

                Console.WriteLine($"Generating answers: {transcriptionFilename}");
                Console.WriteLine();

                var outputDirectory = context.Options.QuestionsOutputDirectory;
                if (string.IsNullOrEmpty(outputDirectory))
                {
                    outputDirectory = Path.GetDirectoryName(transcriptionFilename);
                }

                var answersFilename = Path.Combine(outputDirectory, Path.GetFileNameWithoutExtension(transcriptionFilename) + ".md");

                int promptIndex = 0;

                StringBuilder sb = new StringBuilder();

                foreach (var prompt in prompts)
                {
                    int retryCount = 5;
                    bool success = false;
                    ++promptIndex;

                    while (!success && retryCount >= 0)
                    {
                        try
                        {
                            Console.WriteLine($"Asking prompt question #{promptIndex} of {prompts.Count} : {prompt}");

                            var messageRequest = new MessageRequest
                            {
                                Model = model,
                                System = systemPrompt,
                                Messages = [Message.FromUser(prompt)],
                                MaxTokens = 4 * 1024
                            };

                            var answerResult = await anthropicAiService.Messages.Create(messageRequest);

                            if (answerResult.Successful)
                            {
                                success = true;
                                var answer = answerResult.ToString();
                                sb.AppendLine("# " + prompt);
                                sb.AppendLine();
                                sb.AppendLine(answer);
                                sb.AppendLine();
                                sb.AppendLine();

                                Console.WriteLine($"Answer received.");

                                if (context.Options.Verbose)
                                {
                                    Console.WriteLine($"Answer is: {answer}");
                                    Console.WriteLine();
                                }
                            }
                            else
                            {
                                if (answerResult.Error == null)
                                {
                                    Console.WriteLine();
                                    Console.WriteLine($"/!\\ Did not receive a successful response from OpenAI {model} API /!\\");
                                    Console.WriteLine();
                                }
                                else
                                {
                                    Console.WriteLine();
                                    Console.WriteLine($"/!\\ Did not receive a successful response from OpenAI API {model} /!\\");
                                    Console.WriteLine("Error " + answerResult.Error);
                                    Console.WriteLine();
                                }
                            }

                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine();
                            Console.WriteLine($"/!\\ Did not receive a proper response from OpenAI API {model} /!\\");
                            Console.WriteLine(ex.ToString());
                            Console.WriteLine();
                        }

                        --retryCount;

                        if (!success)
                        {
                            Console.WriteLine();
                            Console.WriteLine($"Retrying prompt query...");
                            Console.WriteLine();
                        }
                    }
                }

                Console.WriteLine("Finished querying of " + answersFilename);
                Console.WriteLine();

                var fullAnswers = sb.ToString();
                File.WriteAllText(answersFilename, fullAnswers);

            } // foreach transcription file
        }

    } // end of class
}





