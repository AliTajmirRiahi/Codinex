using Codinex.Core.Models;

namespace Codinex.Core.Interfaces.Context
{
    public interface IReferenceContextFormatter
    {
        string Format(ReferenceItem reference);
    }
}