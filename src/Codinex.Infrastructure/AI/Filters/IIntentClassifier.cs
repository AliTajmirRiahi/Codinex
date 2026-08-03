

namespace Codify.Infrastructure.AI.Filters
{
    public interface IIntentClassifier
    {
        bool IsTechnical(string text);
    }
}
