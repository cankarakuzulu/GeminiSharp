using GeminiSharp;
using GeminiSharp.Common;
using GeminiSharp.Configuration;
using GeminiSharp.Models;
using GeminiSharp.Models.FileUpload;
using GeminiSharp.Models.FileUpload.Responses;
using Microsoft.Extensions.Options;
using System.Text;

public class Program
{
    static string apiKey = "AIzaSyBi5FyPkpQMeHPxR_RmbDz7O6OtfV_iPOY";

    public static async Task Main(string[] args)
    {
        var geminiApiOptions = new GeminiApiClientOptions { ApiKey = apiKey };
        IOptions<GeminiApiClientOptions> optionsWrapper = Options.Create(geminiApiOptions);
        using var httpClient = new HttpClient();
        IGeminiApiClient geminiClient = new GeminiApiClient(httpClient, optionsWrapper);
        var chatHistory = new List<GeminiRequestContent>();

        Console.WriteLine("Starting chat with Gemini. Type 'upload', 'image', 'stream', or 'exit'.");
        Console.WriteLine("-------------------------------------------------");

        while (true)
        {
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.Write("You: ");
            Console.ResetColor();
            string userInput = Console.ReadLine()?.Trim();

            if (string.IsNullOrWhiteSpace(userInput)) continue;
            if (userInput.Equals("exit", StringComparison.OrdinalIgnoreCase)) break;

            if (userInput.Equals("image", StringComparison.OrdinalIgnoreCase))
            {
                // ... (image generation logic as before) ...
            }
            else if (userInput.Equals("stream", StringComparison.OrdinalIgnoreCase))
            {
                Console.Write("Enter prompt for streaming chat: ");
                string streamPrompt = Console.ReadLine()?.Trim();
                if (string.IsNullOrWhiteSpace(streamPrompt))
                {
                    Console.ForegroundColor = ConsoleColor.Yellow; Console.WriteLine("Stream prompt cannot be empty."); Console.ResetColor();
                    continue;
                }

                Console.ForegroundColor = ConsoleColor.Yellow; Console.WriteLine("Gemini is streaming..."); Console.ResetColor();
                Console.ForegroundColor = ConsoleColor.Green; Console.Write("Gemini: "); Console.ResetColor();
                
                var fullStreamedResponse = new StringBuilder();
                bool firstChunk = true;

                await foreach (var resultChunk in geminiClient.StreamGenerateChatResponseAsync(chatHistory, streamPrompt))
                {
                    if (resultChunk.IsSuccess)
                    {
                        var chunk = resultChunk.Value;
                        if (chunk?.Candidates != null && chunk.Candidates.Any())
                        {
                            foreach (var candidate in chunk.Candidates)
                            {
                                if (candidate.Content?.Parts != null)
                                {
                                    foreach (var part in candidate.Content.Parts)
                                    {
                                        if (!string.IsNullOrEmpty(part.Text))
                                        {
                                            Console.Write(part.Text); // Write incrementally to the console
                                            fullStreamedResponse.Append(part.Text);
                                        }
                                    }
                                }
                            }
                        }
                    }
                    else
                    {
                        Console.WriteLine(); // Newline after partial streamed response
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine($"Stream Error: {resultChunk.Error}");
                        Console.ResetColor();
                        break; // Stop processing stream on error
                    }
                }
                Console.WriteLine(); // Ensure a newline after streaming is complete

                // Add to history after successful streaming
                if (fullStreamedResponse.Length > 0) // Check if anything was actually streamed
                {
                    chatHistory.Add(new GeminiRequestContent { Role = "user", Parts = new List<GeminiRequestPart> { new GeminiRequestPart { Text = streamPrompt } } });
                    chatHistory.Add(new GeminiRequestContent { Role = "model", Parts = new List<GeminiRequestPart> { new GeminiRequestPart { Text = fullStreamedResponse.ToString() } } });
                }
            }
            else // Regular chat or Upload
            {
                // ... (existing chat and upload logic as before) ...
                List<GeminiRequestPart> newUserParts = new List<GeminiRequestPart>();
                if (userInput.Equals("upload", StringComparison.OrdinalIgnoreCase))
                {
                    Console.Write("Enter file path to upload: ");
                    string filePath = Console.ReadLine()?.Trim();
                    if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath)) {
                        Console.ForegroundColor = ConsoleColor.Yellow; Console.WriteLine("Invalid file path."); Console.ResetColor(); continue;
                    }
                    Console.WriteLine("Uploading file...");
                    Result<UploadedFile, ApiError> uploadResult = await geminiClient.UploadFileAsync(filePath);
                    if (uploadResult.IsFailure) {
                        Console.ForegroundColor = ConsoleColor.Red; Console.WriteLine($"Upload Error: {uploadResult.Error}"); Console.ResetColor(); continue;
                    }
                    UploadedFile uploadedFile = uploadResult.Value;
                    Console.ForegroundColor = ConsoleColor.DarkGreen; Console.WriteLine($"File '{uploadedFile.DisplayName}' uploaded."); Console.ResetColor();
                    newUserParts.Add(new GeminiRequestPart { FileData = new GeminiFileDataPart { MimeType = uploadedFile.MimeType, FileUri = uploadedFile.Uri } });
                    Console.Write("Prompt for the file: ");
                    string filePrompt = Console.ReadLine()?.Trim();
                    if(!string.IsNullOrWhiteSpace(filePrompt)) newUserParts.Add(new GeminiRequestPart { Text = filePrompt });
                }
                else
                {
                    newUserParts.Add(new GeminiRequestPart { Text = userInput });
                }

                if (!newUserParts.Any()) continue;

                Console.ForegroundColor = ConsoleColor.Yellow; Console.WriteLine("Gemini is thinking..."); Console.ResetColor();
                Result<string, ApiError> chatResult = await geminiClient.GenerateChatResponseAsync(chatHistory, newUserParts);
                chatResult.Match(
                    onSuccess: modelResponseText => {
                        Console.ForegroundColor = ConsoleColor.Green; Console.Write("Gemini: "); Console.ResetColor(); Console.WriteLine(modelResponseText);
                        chatHistory.Add(new GeminiRequestContent { Role = "user", Parts = newUserParts });
                        if (!modelResponseText.StartsWith("Blocked due to:") && !modelResponseText.Contains("No content generated")) {
                            chatHistory.Add(new GeminiRequestContent { Role = "model", Parts = new List<GeminiRequestPart> { new GeminiRequestPart { Text = modelResponseText } } });
                        }
                    },
                    onFailure: error => { Console.ForegroundColor = ConsoleColor.Red; Console.WriteLine($"Chat Error: {error}"); Console.ResetColor(); }
                );
            }
            Console.WriteLine("-------------------------------------------------");
        }
        Console.WriteLine("Exiting application.");
    }
}