using FoodOrderBots.Models;
using System.Threading;
using System.Threading.Tasks;

namespace FoodOrderBots.Services;

public interface ILanguageUnderstandingService
{
    Task<FoodOrderDetails> RecognizeAsync(string utterance, CancellationToken cancellationToken = default);
}
