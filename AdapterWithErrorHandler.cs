using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Integration.AspNet.Core;
using Microsoft.Bot.Builder.TraceExtensions;
using Microsoft.Bot.Connector.Authentication;
using Microsoft.Extensions.Logging;
using System;

namespace FoodOrderBots;
public class AdapterWithErrorHandler : CloudAdapter
{
    public AdapterWithErrorHandler(BotFrameworkAuthentication auth, ILogger<CloudAdapter> logger, ConversationState conversationState = null)
        : base(auth, logger)
    {
        OnTurnError = async (turnContext, exception) =>
        {
            logger.LogError(exception, $"[OnTurnError] unhandled error: {exception.Message}");

            await turnContext.SendActivityAsync("Sorry, it looks like something went wrong. Please try again.");

            await turnContext.TraceActivityAsync("OnTurnError Trace", exception.Message, "https://www.botframework.com/schemas/error", "TurnError");

            if (conversationState != null)
            {
                try
                {
                    await conversationState.DeleteAsync(turnContext);
                }
                catch (Exception e)
                {
                    logger.LogError(e, $"Exception caught on attempting to Delete ConversationState: {e.Message}");
                }
            }
        };
    }
}
