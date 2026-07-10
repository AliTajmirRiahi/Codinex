using Codify.Core.Models;

namespace Codify.Core.Chat
{
    public interface IReferenceContextFormatter
    {
        string Format(ReferenceItem reference);
    }
}