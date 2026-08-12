namespace Codinex.Core.Interfaces.Search;

/// <summary>
/// Marker for the fuzzy (approximate) search strategy. Kept as a distinct interface so fuzzy-matching
/// logic never gets mixed into exact-search algorithm implementations, per the engine's design rules.
/// </summary>
public interface IFuzzyStringSearchAlgorithm : IStringSearchAlgorithm
{
}
