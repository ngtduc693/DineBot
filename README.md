# Food Ordering Bot

This is a food ordering bot built with C#.NET. The bot can handle inquiries about menu items, prices, and process food orders based on natural language input.

## Features

- **Intent Recognition**: Recognizes various user intents like ordering food, inquiring about prices, checking availability, and more.
- **Entity Extraction**: Extracts key information from user messages such as food items, drinks, customizations, and combos.
- **Natural Conversation Flow**: Handles multi-turn conversations to collect all necessary information.
- **Order Management**: Process food orders with confirmation and estimated delivery time.

## Prerequisites

- [.NET 10.0 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- [Azure Subscription](https://azure.microsoft.com/free/)

## Setup

### 1. Clone the repository

```bash
git clone https://github.com/ngtduc693/DineBot
```

### 2. Configure Azure Language Understanding

1. Create a new Language service in Azure Portal
2. Create a new Language Understanding project named 'Food'
3. Import the model from the provided JSON definition
4. Train and deploy your model to a production endpoint

### 3. Update Configuration

Update `appsettings.json` with your Azure Language Understanding credentials:

```json
{
  "LanguageUnderstanding": {
    "Endpoint": "https://your-language-resource.cognitiveservices.azure.com/",
    "Key": "your-key",
    "ProjectName": "Food",
    "DeploymentName": "your-deployment-name"
  }
}
```

### 4. Run the bot locally

```bash
dotnet build
dotnet run
```

### 5. Connect using Bot Framework Emulator

1. Open Bot Framework Emulator
2. Click "Open Bot"
3. Enter the endpoint URL: http://localhost:3978/api/messages
4. Leave MicrosoftAppId and MicrosoftAppPassword empty for local testing
5. Click "Connect"

## Project Structure

- **Controllers**: Contains BotController for handling HTTP requests.
- **Bots**: Contains the main FoodOrderingBot implementation.
- **Dialogs**: Contains conversation dialogs for handling different user intents.
- **Models**: Contains data models like FoodOrderDetails.
- **Services**: Contains the LanguageUnderstandingService implementation using Azure.AI.Language.Conversations.

## Testing the Bot

Here are some example utterances you can use to test the bot:

- "I want to order a cheeseburger and a coke"
- "What's the price for a grilled chicken combo?"
- "Is the mango smoothie still available?"
- "Can you add extra ketchup to my burger?"
- "How long will my order take?"
- "Do you have any vegan options?"

## Deployment to Azure

To deploy to Azure, follow these steps:

1. Create an Azure Bot resource
2. Set up App Service plan and Web App
3. Configure the necessary application settings
4. Deploy the code using Visual Studio or Azure DevOps

## Further Development

1. Add support for order persistence
2. Implement payment processing
3. Add support for additional dialog flows
4. Expand the menu database
5. Implement user authentication

## License

This project is licensed under the MIT License - see the LICENSE file for details