using FoodOrderBots.Models;
using FoodOrderBots.Services;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace FoodOrderBots.Dialogs;
public class OrderFoodDialog : ComponentDialog
{
    private readonly ILanguageUnderstandingService _languageUnderstanding;
    private readonly MenuService _menuService;
    private readonly OrderService _orderService;

    public OrderFoodDialog(ILanguageUnderstandingService languageUnderstanding, MenuService menuService, OrderService orderService)
        : base(nameof(OrderFoodDialog))
    {
        _languageUnderstanding = languageUnderstanding;
        _menuService = menuService;
        _orderService = orderService;

        AddDialog(new TextPrompt(nameof(TextPrompt)));
        AddDialog(new ConfirmPrompt(nameof(ConfirmPrompt)));
        AddDialog(new WaterfallDialog(nameof(WaterfallDialog), new WaterfallStep[]
        {
                InitialStepAsync,
                ConfirmOrderAsync,
                FinalStepAsync
        }));

        InitialDialogId = nameof(WaterfallDialog);
    }

    private async Task<DialogTurnResult> InitialStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
    {
        var orderDetails = (FoodOrderDetails)stepContext.Options;

        bool validOrder = true;
        foreach (var item in orderDetails?.FoodItems.Keys)
            if (!_menuService.IsItemAvailable(item)) validOrder = false;
        foreach (var item in orderDetails?.Drinks.Keys)
            if (!_menuService.IsItemAvailable(item)) validOrder = false;
        foreach (var item in orderDetails?.Sides.Keys)
            if (!_menuService.IsItemAvailable(item)) validOrder = false;
        foreach (var item in orderDetails?.Combos.Keys)
            if (!_menuService.IsItemAvailable(item)) validOrder = false;

        if (orderDetails.HasItems() && validOrder)
        {
            stepContext.Values["orderDetails"] = orderDetails;
            await stepContext.Context.SendActivityAsync(
                MessageFactory.Text($"Here's your order summary:\n{orderDetails}"),
                cancellationToken);

            return await stepContext.PromptAsync(
                nameof(ConfirmPrompt),
                new PromptOptions { Prompt = MessageFactory.Text("Would you like to confirm this order?") },
                cancellationToken);
        }
        else
        {
            await stepContext.Context.SendActivityAsync(
                MessageFactory.Text("I'd be happy to take your order. Some items were not recognized or are unavailable. What would you like to order?"),
                cancellationToken);

            return await stepContext.PromptAsync(
                nameof(TextPrompt),
                new PromptOptions { Prompt = MessageFactory.Text("Please specify your order (e.g., 'cheeseburger and coke').") },
                cancellationToken);
        }
    }

    private async Task<DialogTurnResult> ConfirmOrderAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
    {
        if (stepContext.Values.TryGetValue("orderDetails", out var orderDetailsObj) && orderDetailsObj is FoodOrderDetails orderDetails)
        {
            bool confirmOrder = (bool)stepContext.Result;

            if (confirmOrder)
            {
                await _orderService.SaveOrderAsync(orderDetails);
                await stepContext.Context.SendActivityAsync(
                    MessageFactory.Text("Great! Your order has been confirmed."),
                    cancellationToken);
                return await stepContext.NextAsync(orderDetails, cancellationToken);
            }
            else
            {
                await _orderService.CancelOrderAsync(orderDetails.OrderId);
                await stepContext.Context.SendActivityAsync(
                    MessageFactory.Text("I've cancelled your order. Is there anything else you'd like to order?"),
                    cancellationToken);
                return await stepContext.EndDialogAsync(null, cancellationToken);
            }
        }
        else
        {
            string utterance = stepContext.Result.ToString();
            var newOrderDetails = await _languageUnderstanding.RecognizeAsync(utterance, cancellationToken);

            bool validOrder = true;
            foreach (var item in newOrderDetails.FoodItems.Keys)
                if (!_menuService.IsItemAvailable(item)) validOrder = false;
            foreach (var item in newOrderDetails.Drinks.Keys)
                if (!_menuService.IsItemAvailable(item)) validOrder = false;
            foreach (var item in newOrderDetails.Sides.Keys)
                if (!_menuService.IsItemAvailable(item)) validOrder = false;
            foreach (var item in newOrderDetails.Combos.Keys)
                if (!_menuService.IsItemAvailable(item)) validOrder = false;

            if (newOrderDetails.HasItems() && validOrder)
            {
                stepContext.Values["orderDetails"] = newOrderDetails;
                await stepContext.Context.SendActivityAsync(
                    MessageFactory.Text($"Here's your order summary:\n{newOrderDetails}"),
                    cancellationToken);

                return await stepContext.PromptAsync(
                    nameof(ConfirmPrompt),
                    new PromptOptions { Prompt = MessageFactory.Text("Would you like to confirm this order?") },
                    cancellationToken);
            }
            else
            {
                await stepContext.Context.SendActivityAsync(
                    MessageFactory.Text("I'm sorry, I couldn't understand your order or some items are unavailable. Let's try again."),
                    cancellationToken);
                return await stepContext.ReplaceDialogAsync(InitialDialogId, null, cancellationToken);
            }
        }
    }

    private async Task<DialogTurnResult> FinalStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
    {
        var orderDetails = (FoodOrderDetails)stepContext.Values["orderDetails"];
        var random = new Random();
        int estimatedTime = random.Next(10, 16);

        await stepContext.Context.SendActivityAsync(
            MessageFactory.Text($"Your order (ID: {orderDetails.OrderId}) has been placed successfully! It will be ready in approximately {estimatedTime} minutes."),
            cancellationToken);

        return await stepContext.EndDialogAsync(orderDetails, cancellationToken);
    }

}