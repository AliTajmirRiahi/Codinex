using Codinex.Core.Models.References;

namespace Codinex.Core.Interfaces.Context
{
    public interface IReferenceContextFormatter
    {
        string Format(ReferenceItem reference);
    }
}