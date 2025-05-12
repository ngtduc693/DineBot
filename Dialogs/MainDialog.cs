using FoodOrderBots.Models;
using FoodOrderBots.Services;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Schema;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace FoodOrderBots.Dialogs;

public class MainDialog : ComponentDialog
{
    private readonly ILanguageUnderstandingService _languageUnderstanding;
    private readonly MenuService _menuService;
    private readonly OrderService _orderService;
    private readonly ILogger<MainDialog> _logger;

    public MainDialog(ILanguageUnderstandingService languageUnderstanding, MenuService menuService, OrderService orderService, ILogger<MainDialog> logger)
        : base(nameof(MainDialog))
    {
        _languageUnderstanding = languageUnderstanding;
        _menuService = menuService;
        _orderService = orderService;
        _logger = logger;

        AddDialog(new OrderFoodDialog(_languageUnderstanding, _menuService, _orderService));
        AddDialog(new TextPrompt(nameof(TextPrompt)));
        AddDialog(new ConfirmPrompt(nameof(ConfirmPrompt)));

        AddDialog(new WaterfallDialog(nameof(WaterfallDialog), new WaterfallStep[]
        {
                InitialStepAsync,
                ProcessResponseAsync,
                FinalStepAsync
        }));

        InitialDialogId = nameof(WaterfallDialog);
    }

    private async Task<DialogTurnResult> InitialStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
    {
        if (stepContext.Context.Activity.Type == ActivityTypes.Message)
        {
            var utterance = stepContext.Context.Activity.Text;

            if (string.IsNullOrWhiteSpace(utterance))
            {
                await stepContext.Context.SendActivityAsync(
                    MessageFactory.Text("Please tell me how I can assist you with your order."),
                    cancellationToken);
                return await stepContext.PromptAsync(
                    nameof(TextPrompt),
                    new PromptOptions { Prompt = MessageFactory.Text("What would you like to do?") },
                    cancellationToken);
            }

            var orderDetails = await _languageUnderstanding.RecognizeAsync(utterance, cancellationToken);
            stepContext.Values["orderDetails"] = orderDetails;

            switch (orderDetails.Intent?.ToLower())
            {
                case "placeorder":
                case "orderfood":
                    return await stepContext.BeginDialogAsync(nameof(OrderFoodDialog), orderDetails, cancellationToken);

                case "inquireprice":
                    return await HandleInquirePriceAsync(stepContext, orderDetails, cancellationToken);

                case "checkavailability":
                    return await HandleCheckAvailabilityAsync(stepContext, orderDetails, cancellationToken);

                case "cancelorder":
                    return await HandleCancelOrderAsync(stepContext, orderDetails, cancellationToken);

                case "modifyorder":
                    return await HandleModifyOrderAsync(stepContext, orderDetails, cancellationToken);

                case "inquirewaittime":
                    return await HandleInquireWaitTimeAsync(stepContext, orderDetails, cancellationToken);

                case "inquiremenu":
                    return await HandleInquireMenuAsync(stepContext, orderDetails, cancellationToken);

                case "none":
                default:
                    await stepContext.Context.SendActivityAsync(
                        MessageFactory.Text("I'm sorry, I didn't understand that. Can you please rephrase?"),
                        cancellationToken);
                    return await stepContext.PromptAsync(
                        nameof(TextPrompt),
                        new PromptOptions { Prompt = MessageFactory.Text("How can I help you with your food order?") },
                        cancellationToken);
            }
        }

        return await stepContext.NextAsync(null, cancellationToken);
    }

    private async Task<DialogTurnResult> ProcessResponseAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
    {
        if (stepContext.Result is FoodOrderDetails)
        {
            return await stepContext.EndDialogAsync(null, cancellationToken);
        }

        return await stepContext.NextAsync(null, cancellationToken);
    }

    private async Task<DialogTurnResult> FinalStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
    {
        return await stepContext.EndDialogAsync(null, cancellationToken);
    }

    private async Task<DialogTurnResult> HandleInquirePriceAsync(WaterfallStepContext stepContext, FoodOrderDetails orderDetails, CancellationToken cancellationToken)
    {
        string responseMessage = "I can provide pricing information. ";

        if (orderDetails.Combos.Any())
        {
            var comboName = orderDetails.Combos.First().Key;
            var item = _menuService.GetItem(comboName);
            responseMessage += item != null
                ? $"The price for {item.Name} is ${item.Price:F2}."
                : $"Sorry, {comboName} is not on the menu.";
        }
        else if (orderDetails.FoodItems.Any())
        {
            var foodName = orderDetails.FoodItems.First().Key;
            var item = _menuService.GetItem(foodName);
            responseMessage += item != null
                ? $"The price for {item.Name} is ${item.Price:F2}."
                : $"Sorry, {foodName} is not on the menu.";
        }
        else
        {
            responseMessage += "Please specify which item you'd like to know the price for.";
        }

        await stepContext.Context.SendActivityAsync(MessageFactory.Text(responseMessage), cancellationToken);
        return await stepContext.PromptAsync(
            nameof(TextPrompt),
            new PromptOptions { Prompt = MessageFactory.Text("Is there anything else you'd like to know?") },
            cancellationToken);
    }

    private async Task<DialogTurnResult> HandleCheckAvailabilityAsync(WaterfallStepContext stepContext, FoodOrderDetails orderDetails, CancellationToken cancellationToken)
    {
        string responseMessage = "Let me check availability for you. ";

        if (orderDetails.FoodItems.Any())
        {
            var foodName = orderDetails.FoodItems.First().Key;
            responseMessage += _menuService.IsItemAvailable(foodName)
                ? $"Yes, {foodName} is available today."
                : $"Sorry, {foodName} is not available at the moment.";
        }
        else if (orderDetails.Drinks.Any())
        {
            var drinkName = orderDetails.Drinks.First().Key;
            responseMessage += _menuService.IsItemAvailable(drinkName)
                ? $"Yes, we have {drinkName} in stock."
                : $"Sorry, {drinkName} is not available at the moment.";
        }
        else
        {
            responseMessage += "Please specify which item you'd like to check availability for.";
        }

        await stepContext.Context.SendActivityAsync(MessageFactory.Text(responseMessage), cancellationToken);
        return await stepContext.PromptAsync(
            nameof(TextPrompt),
            new PromptOptions { Prompt = MessageFactory.Text("Is there anything else you'd like to know?") },
            cancellationToken);
    }

    private async Task<DialogTurnResult> HandleCancelOrderAsync(WaterfallStepContext stepContext, FoodOrderDetails orderDetails, CancellationToken cancellationToken)
    {
        string item = "your order";
        string orderId = orderDetails.OrderId;

        if (orderDetails.FoodItems.Any())
            item = orderDetails.FoodItems.First().Key;
        else if (orderDetails.Drinks.Any())
            item = orderDetails.Drinks.First().Key;

        var cancelled = await _orderService.CancelOrderAsync(orderId);
        string responseMessage = cancelled
            ? $"I've cancelled your order for {item}."
            : $"No order found with ID {orderId}.";

        await stepContext.Context.SendActivityAsync(MessageFactory.Text(responseMessage), cancellationToken);
        return await stepContext.PromptAsync(
            nameof(TextPrompt),
            new PromptOptions { Prompt = MessageFactory.Text("Would you like to place a new order?") },
            cancellationToken);
    }

    private async Task<DialogTurnResult> HandleModifyOrderAsync(WaterfallStepContext stepContext, FoodOrderDetails orderDetails, CancellationToken cancellationToken)
    {
        string responseMessage = "I can modify your order. ";
        var order = await _orderService.GetOrderAsync(orderDetails.OrderId);

        if (order == null)
        {
            responseMessage = "No existing order found. Would you like to place a new order?";
        }
        else if (orderDetails.FoodItems.Any() && orderDetails.Customizations.Any())
        {
            var foodName = orderDetails.FoodItems.First().Key;
            var customization = orderDetails.Customizations.First();
            order.Customizations.Add(customization);
            await _orderService.SaveOrderAsync(order);
            responseMessage += $"I've updated your {foodName} with {customization}.";
        }
        else
        {
            responseMessage += "Please specify what item you'd like to modify and how.";
        }

        await stepContext.Context.SendActivityAsync(MessageFactory.Text(responseMessage), cancellationToken);
        return await stepContext.PromptAsync(
            nameof(TextPrompt),
            new PromptOptions { Prompt = MessageFactory.Text("Is there anything else you'd like to modify?") },
            cancellationToken);
    }

    private async Task<DialogTurnResult> HandleInquireWaitTimeAsync(WaterfallStepContext stepContext, FoodOrderDetails orderDetails, CancellationToken cancellationToken)
    {
        string item = "your order";
        var random = new Random();
        int estimatedTime = random.Next(10, 16);

        if (orderDetails.Combos.Any())
            item = orderDetails.Combos.First().Key;
        else if (orderDetails.FoodItems.Any())
            item = orderDetails.FoodItems.First().Key;
        else if (orderDetails.Drinks.Any())
            item = orderDetails.Drinks.First().Key;

        string responseMessage = $"The wait time for {item} is approximately {estimatedTime} minutes.";

        await stepContext.Context.SendActivityAsync(MessageFactory.Text(responseMessage), cancellationToken);
        return await stepContext.PromptAsync(
            nameof(TextPrompt),
            new PromptOptions { Prompt = MessageFactory.Text("Is there anything else you'd like to know?") },
            cancellationToken);
    }

    private async Task<DialogTurnResult> HandleInquireMenuAsync(WaterfallStepContext stepContext, FoodOrderDetails orderDetails, CancellationToken cancellationToken)
    {
        string responseMessage = "I can help you with our menu. ";

        if (orderDetails.FoodTypes.Any())
        {
            var foodType = orderDetails.FoodTypes.First().ToLower();
            var items = _menuService.GetItemsByFoodType(foodType);
            if (items.Any())
            {
                responseMessage += $"Our {foodType} options include: {string.Join(", ", items.Select(i => i.Name))}.";
            }
            else
            {
                responseMessage += $"Sorry, we don't have specific {foodType} options at the moment.";
            }
        }
        else
        {
            var foodItems = _menuService.GetItemsByType("FoodItem").Select(i => i.Name);
            var drinks = _menuService.GetItemsByType("Drink").Select(i => i.Name);
            var sides = _menuService.GetItemsByType("Side").Select(i => i.Name);
            var combos = _menuService.GetItemsByType("Combo").Select(i => i.Name);
            responseMessage += "Our menu includes:\n" +
                              $"- Food: {string.Join(", ", foodItems)}\n" +
                              $"- Drinks: {string.Join(", ", drinks)}\n" +
                              $"- Sides: {string.Join(", ", sides)}\n" +
                              $"- Combos: {string.Join(", ", combos)}";
        }

        await stepContext.Context.SendActivityAsync(MessageFactory.Text(responseMessage), cancellationToken);
        return await stepContext.PromptAsync(
            nameof(TextPrompt),
            new PromptOptions { Prompt = MessageFactory.Text("Would you like to know about any specific menu items?") },
            cancellationToken);
    }
}
