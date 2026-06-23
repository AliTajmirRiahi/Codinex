

namespace Codify.Infrastructure.Filters
{
    public interface IIntentClassifier
    {
        bool IsTechnical(string text);
    }
}
