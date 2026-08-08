using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Codinex.Core.DependencyInjection.Attributes;
using Codinex.Core.DependencyInjection.Models;
using Codinex.VisualStudio.Interfaces;
using Codinex.VisualStudio.Models;
using Codinex.VisualStudio.Tools.BuiltIn.Files;

namespace Codinex.VisualStudio.Services;

[AutoDiRegister(Modules.VisualStudio, RegistrationOrder.Platform)]
public sealed class SourceFileElementIndex : ISourceFileElementIndex
{
    private readonly ConcurrentDictionary<string, SourceFileElement> _elementsById =
        new(StringComparer.Ordinal);

    private readonly ConcurrentDictionary<string, IReadOnlyCollection<string>> _elementIdsByFile =
        new(StringComparer.OrdinalIgnoreCase);

    public void UpdateFile(
        string filePath,
        IReadOnlyCollection<SourceFileElement> elements)
    {
        var normalizedFilePath = SourceFileElementParser.NormalizePath(filePath);
        var nextElementIds = elements
            .Where(element => !string.IsNullOrWhiteSpace(element.Id))
            .Select(element => element.Id)
            .ToArray();

        if (_elementIdsByFile.TryGetValue(normalizedFilePath, out var previousElementIds))
        {
            foreach (var elementId in previousElementIds)
            {
                if (!nextElementIds.Contains(elementId, StringComparer.Ordinal))
                {
                    _elementsById.TryRemove(elementId, out _);
                }
            }
        }

        foreach (var element in elements)
        {
            if (string.IsNullOrWhiteSpace(element.Id))
            {
                continue;
            }

            _elementsById[element.Id] = element;
        }

        _elementIdsByFile[normalizedFilePath] = nextElementIds;
    }

    public bool TryGetElement(
        string elementId,
        out SourceFileElement element)
    {
        if (!string.IsNullOrWhiteSpace(elementId)) return _elementsById.TryGetValue(elementId, out element);

        element = null;

        return false;

    }
}
