using Azure;
using Azure.AI.Language.Conversations;
using FoodOrderBots.Dialogs;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Schema;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace FoodOrderBots.Bots;

public class FoodOrderingBot : ActivityHandler
{
    private readonly Dialog _dialog;
    private readonly ConversationState _conversationState;
    private readonly UserState _userState;
    private readonly ILogger<FoodOrderingBot> _logger;
    private readonly DialogSet _dialogSet;

    public FoodOrderingBot(ConversationState conversationState, UserState userState, MainDialog dialog, ILogger<FoodOrderingBot> logger)
    {
        _conversationState = conversationState ?? throw new ArgumentNullException(nameof(conversationState));
        _userState = userState ?? throw new ArgumentNullException(nameof(userState));
        _dialog = dialog ?? throw new ArgumentNullException(nameof(dialog));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        _dialogSet = new DialogSet(_conversationState.CreateProperty<DialogState>("DialogState"));
        _dialogSet.Add(_dialog);
    }

    protected override async Task OnMessageActivityAsync(ITurnContext<IMessageActivity> turnContext, CancellationToken cancellationToken)
    {
        _logger.LogInformation($"Received message: {turnContext.Activity.Text}");

        if (string.IsNullOrWhiteSpace(turnContext.Activity.Text))
        {
            await turnContext.SendActivityAsync(
                MessageFactory.Text("Please provide a valid message to proceed."),
                cancellationToken);
            return;
        }

        // Create DialogContext using DialogSet
        var dialogContext = await _dialogSet.CreateContextAsync(turnContext, cancellationToken);

        if (dialogContext == null)
        {
            _logger.LogWarning("Failed to create dialog context.");
            await turnContext.SendActivityAsync(
                MessageFactory.Text("I'm having trouble processing your request. Let's start over."),
                cancellationToken);
            return;
        }

        await dialogContext.ContinueDialogAsync(cancellationToken);

        if (!turnContext.Responded)
        {
            await dialogContext.BeginDialogAsync(_dialog.Id, null, cancellationToken);
        }
    }

    protected override async Task OnTypingActivityAsync(ITurnContext<ITypingActivity> turnContext, CancellationToken cancellationToken)
    {
        _logger.LogInformation("User is typing...");
        await turnContext.SendActivityAsync(
            MessageFactory.Text("I'm waiting for your order!"),
            cancellationToken);
    }

    protected override async Task OnMembersAddedAsync(IList<ChannelAccount> membersAdded, ITurnContext<IConversationUpdateActivity> turnContext, CancellationToken cancellationToken)
    {
        foreach (var member in membersAdded)
        {
            if (member.Id != turnContext.Activity.Recipient.Id)
            {
                _logger.LogInformation($"New member added: {member.Id}");
                await turnContext.SendActivityAsync(
                    MessageFactory.Text("Welcome to the Food Ordering Bot! How can I assist you today?"),
                    cancellationToken);
            }
        }
    }

    public override async Task OnTurnAsync(ITurnContext turnContext, CancellationToken cancellationToken = default)
    {
        try
        {
            await base.OnTurnAsync(turnContext, cancellationToken);
            await _conversationState.SaveChangesAsync(turnContext, false, cancellationToken);
            await _userState.SaveChangesAsync(turnContext, false, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing turn in FoodOrderingBot.");
            await turnContext.SendActivityAsync(
                MessageFactory.Text("An error occurred while processing your request. Please try again."),
                cancellationToken);
        }
    }
}