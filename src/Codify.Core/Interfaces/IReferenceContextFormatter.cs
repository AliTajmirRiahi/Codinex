using Codify.Core.Models;

namespace Codify.Core.Interfaces
{
    public interface IReferenceContextFormatter
    {
        string Format(ReferenceItem reference);
    }
}