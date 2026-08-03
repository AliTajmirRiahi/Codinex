using Codinex.Core.Models;

namespace Codinex.Core.Interfaces
{
    public interface IReferenceContextFormatter
    {
        string Format(ReferenceItem reference);
    }
}