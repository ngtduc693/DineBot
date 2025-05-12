using Azure;
using Azure.AI.Language.Conversations;
using Azure.Core;
using FoodOrderBots.Models;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace FoodOrderBots.Services;

public class LanguageUnderstandingService : ILanguageUnderstandingService
{
    private readonly ConversationAnalysisClient _client;
    private readonly string _projectName;
    private readonly string _deploymentName;
    private readonly ILogger<LanguageUnderstandingService> _logger;

    public LanguageUnderstandingService(string endpoint, string key, string projectName, string deploymentName, ILogger<LanguageUnderstandingService> logger)
    {
        if (string.IsNullOrEmpty(endpoint) || string.IsNullOrEmpty(key))
            throw new ArgumentException("Language Understanding endpoint or key is missing.");

        var credential = new AzureKeyCredential(key);
        _client = new ConversationAnalysisClient(new Uri(endpoint), credential);
        _projectName = projectName;
        _deploymentName = deploymentName;
        _logger = logger ?? Microsoft.Extensions.Logging.Abstractions.NullLogger<LanguageUnderstandingService>.Instance;
    }

    public async Task<FoodOrderDetails> RecognizeAsync(string utterance, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(utterance))
        {
            _logger.LogWarning("Empty utterance received.");
            return new FoodOrderDetails { Intent = "None" };
        }

        try
        {
            var data = new
            {
                analysisInput = new
                {
                    conversationItem = new
                    {
                        text = utterance,
                        id = "1",
                        participantId = "user"
                    }
                },
                parameters = new
                {
                    projectName = _projectName,
                    deploymentName = _deploymentName,
                    verbose = true
                },
                kind = "Conversation"
            };

            var requestData = BinaryData.FromObjectAsJson(data);
            Response response = await _client.AnalyzeConversationAsync(RequestContent.Create(requestData));

            BinaryData responseData = response.Content;
            using JsonDocument result = JsonDocument.Parse(responseData);

            var orderDetails = new FoodOrderDetails();

            try
            {
                var topIntent = result.RootElement
                    .GetProperty("result")
                    .GetProperty("prediction")
                    .GetProperty("topIntent").GetString();

                orderDetails.Intent = topIntent ?? "None";
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to extract top intent");
                orderDetails.Intent = "None";
            }

            try
            {
                var entities = result.RootElement
                    .GetProperty("result")
                    .GetProperty("prediction")
                    .GetProperty("entities");

                if (entities.ValueKind == JsonValueKind.Array)
                {
                    foreach (var entity in entities.EnumerateArray())
                    {
                        var category = entity.GetProperty("category").GetString();
                        var text = entity.GetProperty("text").GetString();

                        if (!string.IsNullOrEmpty(category) && !string.IsNullOrEmpty(text))
                        {
                            switch (category)
                            {
                                case "FoodItem":
                                    orderDetails.FoodItems[text] = orderDetails.FoodItems.GetValueOrDefault(text, 0) + 1;
                                    break;
                                case "Drink":
                                    orderDetails.Drinks[text] = orderDetails.Drinks.GetValueOrDefault(text, 0) + 1;
                                    break;
                                case "Side":
                                    orderDetails.Sides[text] = orderDetails.Sides.GetValueOrDefault(text, 0) + 1;
                                    break;
                                case "Customization":
                                    orderDetails.Customizations.Add(text);
                                    break;
                                case "Combo":
                                    orderDetails.Combos[text] = orderDetails.Combos.GetValueOrDefault(text, 0) + 1;
                                    break;
                                case "FoodType":
                                    orderDetails.FoodTypes.Add(text);
                                    break;
                                case "Request":
                                    orderDetails.Requests.Add(text);
                                    break;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to extract entities");
            }

            return orderDetails;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error recognizing intent for utterance: {Utterance}", utterance);
            return new FoodOrderDetails { Intent = "None" };
        }
    }
}