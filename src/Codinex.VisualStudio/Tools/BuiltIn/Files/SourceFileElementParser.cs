using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Codinex.VisualStudio.Models;

namespace Codinex.VisualStudio.Tools.BuiltIn.Files;

internal static class SourceFileElementParser
{
    public static readonly string[] SupportedExtensions =
    [
        ".cs",
        ".js",
        ".jsx",
        ".ts",
        ".tsx",
        ".py",
        ".java",
        ".c",
        ".cc",
        ".cpp",
        ".cxx",
        ".h",
        ".hh",
        ".hpp",
        ".hxx",
        ".html",
        ".htm"
    ];

    // Elements with none of these signals (no id, not one of the always-structural tags, and no
    // named class on one of the class-gated tags) are left out of the outline entirely — HTML has
    // no inherent semantic units, so listing every <div>/<span> would just reproduce the ambiguity
    // (e.g. dozens of unlabeled </label> tags) this outline exists to avoid.
    private static readonly HashSet<string> HtmlVoidTags = new(StringComparer.OrdinalIgnoreCase)
    {
        "area", "base", "br", "col", "embed", "hr", "img", "input",
        "link", "meta", "param", "source", "track", "wbr"
    };

    private static readonly HashSet<string> HtmlAlwaysStructuralTags = new(StringComparer.OrdinalIgnoreCase)
    {
        "section", "form", "nav", "header", "footer", "main", "article", "aside"
    };

    private static readonly HashSet<string> HtmlClassGatedContainerTags = new(StringComparer.OrdinalIgnoreCase)
    {
        "div", "ul", "ol", "table"
    };

    private static readonly Regex HtmlTagOpenRegex = new(
        @"<(?<tag>[A-Za-z][A-Za-z0-9-]*)(?<attrs>(?:[^>""']|""[^""]*""|'[^']*')*)>",
        RegexOptions.Compiled);

    private static readonly Regex HtmlIdAttrRegex = new(
        @"\bid\s*=\s*(?:""(?<value>[^""]*)""|'(?<value>[^']*)')",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private static readonly Regex HtmlClassAttrRegex = new(
        @"\bclass\s*=\s*(?:""(?<value>[^""]*)""|'(?<value>[^']*)')",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private static readonly HashSet<string> JavaScriptLikeControlKeywords = new(StringComparer.Ordinal)
    {
        "if",
        "for",
        "foreach",
        "while",
        "switch",
        "catch",
        "with"
    };

    private static readonly HashSet<string> BraceLanguageControlKeywords = new(StringComparer.Ordinal)
    {
        "if",
        "for",
        "foreach",
        "while",
        "switch",
        "catch",
        "using",
        "lock",
        "return"
    };

    public static bool IsSupported(string path)
    {
        var extension = GetExtension(path);

        return SupportedExtensions.Contains(extension, StringComparer.OrdinalIgnoreCase);
    }

    public static string GetLanguage(string path)
    {
        return GetExtension(path) switch
        {
            ".cs" => "csharp",
            ".js" or ".jsx" => "javascript",
            ".ts" or ".tsx" => "typescript",
            ".py" => "python",
            ".java" => "java",
            ".c" or ".h" => "c",
            ".cc" or ".cpp" or ".cxx" or ".hh" or ".hpp" or ".hxx" => "cpp",
            ".html" or ".htm" => "html",
            _ => "text"
        };
    }

    public static IReadOnlyList<SourceFileElement> Parse(
        string relativePath,
        string content)
    {
        var extension = GetExtension(relativePath);

        if (extension.Equals(".cs", StringComparison.OrdinalIgnoreCase))
        {
            return ParseCSharp(content);
        }

        var language = GetLanguage(relativePath);

        if (language == "python")
        {
            return FinalizeTextElementIds(
                language,
                relativePath,
                ParsePython(content));
        }

        if (language is "javascript" or "typescript")
        {
            return FinalizeTextElementIds(
                language,
                relativePath,
                ParseJavaScriptLike(content));
        }

        if (language is "java" or "c" or "cpp")
        {
            return FinalizeTextElementIds(
                language,
                relativePath,
                ParseBraceLanguage(content, language));
        }

        if (language == "html")
        {
            return FinalizeTextElementIds(
                language,
                relativePath,
                ParseHtml(content));
        }

        return [];
    }

    private static IReadOnlyList<SourceFileElement> ParseCSharp(string content)
    {
        var syntaxTree = CSharpSyntaxTree.ParseText(content);
        var root = syntaxTree.GetRoot();
        var elements = new List<SourceFileElement>();
        var order = 0;

        CollectCSharpElements(root, elements, ref order);

        return elements
            .OrderBy(e => e.Order)
            .ToArray();
    }

    private static void CollectCSharpElements(
        SyntaxNode node,
        ICollection<SourceFileElement> elements,
        ref int order)
    {
        foreach (var child in node.ChildNodes())
        {
            switch (child)
            {
                case ConstructorDeclarationSyntax constructorDeclaration:
                    elements.Add(BuildConstructorElement(constructorDeclaration, order++));
                    break;

                case MethodDeclarationSyntax methodDeclaration:
                    elements.Add(BuildMethodElement(methodDeclaration, order++));
                    break;

                case PropertyDeclarationSyntax propertyDeclaration:
                    elements.Add(BuildPropertyElement(propertyDeclaration, order++));
                    break;

                case FieldDeclarationSyntax fieldDeclaration:
                    foreach (var variable in fieldDeclaration.Declaration.Variables)
                    {
                        elements.Add(BuildFieldElement(fieldDeclaration, variable, order++));
                    }
                    break;

                case EventFieldDeclarationSyntax eventFieldDeclaration:
                    foreach (var variable in eventFieldDeclaration.Declaration.Variables)
                    {
                        elements.Add(BuildEventFieldElement(eventFieldDeclaration, variable, order++));
                    }
                    break;

                case EventDeclarationSyntax eventDeclaration:
                    elements.Add(BuildEventElement(eventDeclaration, order++));
                    break;

                case IndexerDeclarationSyntax indexerDeclaration:
                    elements.Add(BuildIndexerElement(indexerDeclaration, order++));
                    break;

                case RecordDeclarationSyntax recordDeclaration:
                    elements.Add(BuildRecordElement(recordDeclaration, order++));
                    break;

                case EnumDeclarationSyntax enumDeclaration:
                    elements.Add(BuildEnumElement(enumDeclaration, order++));
                    break;
            }

            CollectCSharpElements(child, elements, ref order);
        }
    }

    private static SourceFileElement BuildConstructorElement(
        ConstructorDeclarationSyntax declaration,
        int order)
    {
        var name = declaration.Identifier.ValueText;
        var qualifiedName = BuildQualifiedName(declaration, "#ctor");

        return new SourceFileElement
        {
            Id = $"M:{qualifiedName}{BuildParameterIdList(declaration.ParameterList)}",
            Kind = "Constructor",
            Name = name,
            Signature = Combine(
                GetModifiers(declaration.Modifiers),
                name + GetSyntaxText(declaration.ParameterList)),
            Source = declaration.ToFullString(),
            Order = order
        };
    }

    private static SourceFileElement BuildMethodElement(
        MethodDeclarationSyntax declaration,
        int order)
    {
        var name = declaration.Identifier.ValueText;
        var idName = GetMemberIdName(declaration.ExplicitInterfaceSpecifier, name) + GetMethodTypeParameterIdSuffix(declaration);
        var signatureName = GetMemberSignatureName(declaration.ExplicitInterfaceSpecifier, name) + GetSyntaxText(declaration.TypeParameterList);
        var qualifiedName = BuildQualifiedName(declaration, idName);

        return new SourceFileElement
        {
            Id = $"M:{qualifiedName}{BuildParameterIdList(declaration.ParameterList)}",
            Kind = "Method",
            Name = name,
            Signature = Combine(
                GetModifiers(declaration.Modifiers),
                GetSyntaxText(declaration.ReturnType),
                signatureName + GetSyntaxText(declaration.ParameterList),
                GetSyntaxText(declaration.ConstraintClauses)),
            Source = declaration.ToFullString(),
            Order = order
        };
    }

    private static SourceFileElement BuildPropertyElement(
        PropertyDeclarationSyntax declaration,
        int order)
    {
        var name = declaration.Identifier.ValueText;
        var memberIdName = GetMemberIdName(declaration.ExplicitInterfaceSpecifier, name);
        var signatureName = GetMemberSignatureName(declaration.ExplicitInterfaceSpecifier, name);

        return new SourceFileElement
        {
            Id = $"P:{BuildQualifiedName(declaration, memberIdName)}",
            Kind = "Property",
            Name = name,
            Signature = Combine(
                GetModifiers(declaration.Modifiers),
                GetSyntaxText(declaration.Type),
                signatureName,
                BuildAccessorListSignature(declaration.AccessorList, declaration.ExpressionBody != null)),
            Source = declaration.ToFullString(),
            Order = order
        };
    }

    private static SourceFileElement BuildFieldElement(
        FieldDeclarationSyntax declaration,
        VariableDeclaratorSyntax variable,
        int order)
    {
        var name = variable.Identifier.ValueText;

        return new SourceFileElement
        {
            Id = $"F:{BuildQualifiedName(declaration, name)}",
            Kind = "Field",
            Name = name,
            Signature = Combine(
                GetModifiers(declaration.Modifiers),
                GetSyntaxText(declaration.Declaration.Type),
                name),
            Source = BuildVariableSource(declaration, variable),
            Order = order
        };
    }

    private static SourceFileElement BuildEventFieldElement(
        EventFieldDeclarationSyntax declaration,
        VariableDeclaratorSyntax variable,
        int order)
    {
        var name = variable.Identifier.ValueText;

        return new SourceFileElement
        {
            Id = $"E:{BuildQualifiedName(declaration, name)}",
            Kind = "Event",
            Name = name,
            Signature = Combine(
                GetModifiers(declaration.Modifiers),
                "event",
                GetSyntaxText(declaration.Declaration.Type),
                name),
            Source = BuildVariableSource(declaration, variable),
            Order = order
        };
    }

    private static SourceFileElement BuildEventElement(
        EventDeclarationSyntax declaration,
        int order)
    {
        var name = declaration.Identifier.ValueText;
        var memberIdName = GetMemberIdName(declaration.ExplicitInterfaceSpecifier, name);
        var signatureName = GetMemberSignatureName(declaration.ExplicitInterfaceSpecifier, name);

        return new SourceFileElement
        {
            Id = $"E:{BuildQualifiedName(declaration, memberIdName)}",
            Kind = "Event",
            Name = name,
            Signature = Combine(
                GetModifiers(declaration.Modifiers),
                "event",
                GetSyntaxText(declaration.Type),
                signatureName,
                BuildAccessorListSignature(declaration.AccessorList, false)),
            Source = declaration.ToFullString(),
            Order = order
        };
    }

    private static SourceFileElement BuildIndexerElement(
        IndexerDeclarationSyntax declaration,
        int order)
    {
        const string name = "this";
        var memberIdName = GetMemberIdName(declaration.ExplicitInterfaceSpecifier, "Item");
        var signatureName = GetMemberSignatureName(declaration.ExplicitInterfaceSpecifier, name);

        return new SourceFileElement
        {
            Id = $"P:{BuildQualifiedName(declaration, memberIdName)}{BuildParameterIdList(declaration.ParameterList)}",
            Kind = "Indexer",
            Name = name,
            Signature = Combine(
                GetModifiers(declaration.Modifiers),
                GetSyntaxText(declaration.Type),
                signatureName + GetSyntaxText(declaration.ParameterList),
                BuildAccessorListSignature(declaration.AccessorList, declaration.ExpressionBody != null)),
            Source = declaration.ToFullString(),
            Order = order
        };
    }

    private static SourceFileElement BuildRecordElement(
        RecordDeclarationSyntax declaration,
        int order)
    {
        var name = declaration.Identifier.ValueText;
        var typeName = name + GetSyntaxText(declaration.TypeParameterList);

        return new SourceFileElement
        {
            Id = $"T:{BuildQualifiedName(declaration, GetTypeIdName(declaration))}",
            Kind = "records",
            Name = name,
            Signature = Combine(
                GetModifiers(declaration.Modifiers),
                declaration.Keyword.ValueText,
                declaration.ClassOrStructKeyword.ValueText,
                typeName + GetSyntaxText(declaration.ParameterList),
                GetSyntaxText(declaration.BaseList),
                GetSyntaxText(declaration.ConstraintClauses)),
            Source = declaration.ToFullString(),
            Order = order
        };
    }

    private static SourceFileElement BuildEnumElement(
        EnumDeclarationSyntax declaration,
        int order)
    {
        var name = declaration.Identifier.ValueText;

        return new SourceFileElement
        {
            Id = $"T:{BuildQualifiedName(declaration, name)}",
            Kind = "enums",
            Name = name,
            Signature = Combine(
                GetModifiers(declaration.Modifiers),
                "enum",
                name,
                GetSyntaxText(declaration.BaseList)),
            Source = declaration.ToFullString(),
            Order = order
        };
    }

    private static string BuildVariableSource(
        FieldDeclarationSyntax declaration,
        VariableDeclaratorSyntax variable)
    {
        if (declaration.Declaration.Variables.Count == 1)
        {
            return declaration.ToFullString();
        }

        return Combine(
            declaration.GetLeadingTrivia().ToFullString() + declaration.Modifiers.ToFullString(),
            declaration.Declaration.Type.ToFullString(),
            variable.ToFullString().TrimEnd(),
            declaration.SemicolonToken.ToFullString());
    }

    private static string BuildVariableSource(
        EventFieldDeclarationSyntax declaration,
        VariableDeclaratorSyntax variable)
    {
        if (declaration.Declaration.Variables.Count == 1)
        {
            return declaration.ToFullString();
        }

        return Combine(
            declaration.GetLeadingTrivia().ToFullString() + declaration.Modifiers.ToFullString(),
            "event",
            declaration.Declaration.Type.ToFullString(),
            variable.ToFullString().TrimEnd(),
            declaration.SemicolonToken.ToFullString());
    }

    private static IReadOnlyList<SourceFileElement> ParsePython(string content)
    {
        var elements = new List<SourceFileElement>();
        var lines = SplitLines(content);
        var order = 0;

        for (var i = 0; i < lines.Count; i++)
        {
            var line = lines[i].Text;
            var trimmed = line.TrimStart();

            if (trimmed.StartsWith("#", StringComparison.Ordinal))
            {
                continue;
            }

            var match = Regex.Match(
                line,
                @"^(?<indent>\s*)(?<async>async\s+)?(?<keyword>def|class)\s+(?<name>[A-Za-z_][A-Za-z0-9_]*)\s*(?<tail>[^\r\n]*)",
                RegexOptions.CultureInvariant);

            if (!match.Success)
            {
                continue;
            }

            var sourceStartLine = GetPythonDecoratorStartLine(lines, i, match.Groups["indent"].Value.Length);
            var sourceEndLine = GetPythonElementEndLine(lines, i, match.Groups["indent"].Value.Length);
            var sourceStart = lines[sourceStartLine].Start;
            var sourceEnd = sourceEndLine >= lines.Count
                ? content.Length
                : lines[sourceEndLine].Start;
            var keyword = match.Groups["keyword"].Value;
            var name = match.Groups["name"].Value;
            var tail = StripInlineComment(match.Groups["tail"].Value).TrimEnd();
            var signature = Clean((match.Groups["async"].Value + keyword + " " + name + tail).TrimEnd(':'));

            elements.Add(new SourceFileElement
            {
                Kind = keyword == "class"
                    ? "Class"
                    : match.Groups["indent"].Value.Length == 0 ? "Function" : "Method",
                Name = name,
                Signature = signature,
                Source = content.Substring(sourceStart, sourceEnd - sourceStart),
                Order = order++
            });
        }

        return elements;
    }

    private static IReadOnlyList<SourceFileElement> ParseJavaScriptLike(string content)
    {
        var elements = new List<SourceFileElement>();
        var lines = SplitLines(content);
        var order = 0;

        for (var i = 0; i < lines.Count; i++)
        {
            var line = lines[i].Text;
            var trimmed = line.TrimStart();

            if (trimmed.StartsWith("//", StringComparison.Ordinal) ||
                trimmed.StartsWith("/*", StringComparison.Ordinal) ||
                trimmed.StartsWith("*", StringComparison.Ordinal))
            {
                continue;
            }

            var element = TryBuildJavaScriptLikeElement(content, lines[i].Start, line, order);

            if (element == null)
            {
                continue;
            }

            elements.Add(element);
            order++;
        }

        return elements;
    }

    private static SourceFileElement TryBuildJavaScriptLikeElement(
        string content,
        int start,
        string line,
        int order)
    {
        var declarationMatch = Regex.Match(
            line,
            @"^\s*(?:(?:export|default|declare|abstract)\s+)*(?<kind>class|interface|enum|type)\s+(?<name>[A-Za-z_$][A-Za-z0-9_$]*)\b(?<tail>[^\r\n]*)",
            RegexOptions.CultureInvariant);

        if (declarationMatch.Success)
        {
            var kindText = declarationMatch.Groups["kind"].Value;
            var signature = StripInlineComment(line).Trim();

            return CreateTextElement(
                GetJavaScriptTypeKind(kindText),
                declarationMatch.Groups["name"].Value,
                signature,
                ExtractBraceOrStatementSource(content, start),
                order);
        }

        var functionMatch = Regex.Match(
            line,
            @"^\s*(?:(?:export|default)\s+)*(?<async>async\s+)?function\s+\*?\s*(?<name>[A-Za-z_$][A-Za-z0-9_$]*)\s*(?<tail>[^\r\n]*)",
            RegexOptions.CultureInvariant);

        if (functionMatch.Success)
        {
            return CreateTextElement(
                "Function",
                functionMatch.Groups["name"].Value,
                StripInlineComment(line).Trim(),
                ExtractBraceOrStatementSource(content, start),
                order);
        }

        var arrowMatch = Regex.Match(
            line,
            @"^\s*(?:(?:export|default)\s+)*(?:const|let|var)\s+(?<name>[A-Za-z_$][A-Za-z0-9_$]*)\s*(?::[^=]+)?=\s*(?:async\s*)?(?:\([^\)]*\)|[A-Za-z_$][A-Za-z0-9_$]*)\s*=>",
            RegexOptions.CultureInvariant);

        if (arrowMatch.Success)
        {
            return CreateTextElement(
                "Function",
                arrowMatch.Groups["name"].Value,
                StripInlineComment(line).Trim().TrimEnd(';'),
                ExtractBraceOrStatementSource(content, start),
                order);
        }

        var methodMatch = Regex.Match(
            line,
            @"^\s*(?:(?:public|private|protected|static|async|abstract|override|readonly|get|set)\s+)*(?<name>[A-Za-z_$][A-Za-z0-9_$]*)\s*(?:<[^>]+>)?\s*\([^;{}]*\)\s*(?::\s*[^\{;]+)?\s*(?:\{|=>)",
            RegexOptions.CultureInvariant);

        if (methodMatch.Success &&
            !JavaScriptLikeControlKeywords.Contains(methodMatch.Groups["name"].Value))
        {
            return CreateTextElement(
                "Method",
                methodMatch.Groups["name"].Value,
                StripInlineComment(line).Trim(),
                ExtractBraceOrStatementSource(content, start),
                order);
        }

        var fieldMatch = Regex.Match(
            line,
            @"^\s*(?:(?:public|private|protected|static|readonly|declare|abstract)\s+)+(?<name>[A-Za-z_$][A-Za-z0-9_$]*)\s*(?::[^=;{]+)?(?:=\s*[^;{]+)?;",
            RegexOptions.CultureInvariant);

        if (fieldMatch.Success)
        {
            return CreateTextElement(
                "Field",
                fieldMatch.Groups["name"].Value,
                StripInlineComment(line).Trim(),
                ExtractBraceOrStatementSource(content, start),
                order);
        }

        return null;
    }

    private static IReadOnlyList<SourceFileElement> ParseBraceLanguage(
        string content,
        string language)
    {
        var elements = new List<SourceFileElement>();
        var lines = SplitLines(content);
        var order = 0;

        for (var i = 0; i < lines.Count; i++)
        {
            var line = lines[i].Text;
            var trimmed = line.TrimStart();

            if (trimmed.StartsWith("//", StringComparison.Ordinal) ||
                trimmed.StartsWith("/*", StringComparison.Ordinal) ||
                trimmed.StartsWith("*", StringComparison.Ordinal) ||
                trimmed.StartsWith("#", StringComparison.Ordinal))
            {
                continue;
            }

            var element = TryBuildBraceLanguageElement(content, lines[i].Start, line, language, order);

            if (element == null)
            {
                continue;
            }

            elements.Add(element);
            order++;
        }

        return elements;
    }

    private static SourceFileElement TryBuildBraceLanguageElement(
        string content,
        int start,
        string line,
        string language,
        int order)
    {
        var typeMatch = Regex.Match(
            line,
            @"^\s*(?:(?:public|private|protected|static|final|abstract|sealed|export|template\s*<[^>]+>)\s+)*(?<kind>class|interface|enum|struct)\s+(?<name>[A-Za-z_][A-Za-z0-9_]*)\b(?<tail>[^\r\n]*)",
            RegexOptions.CultureInvariant);

        if (typeMatch.Success)
        {
            var typeKeyword = typeMatch.Groups["kind"].Value;

            return CreateTextElement(
                typeKeyword == "enum" ? "Enum" : ToTitleCase(typeKeyword),
                typeMatch.Groups["name"].Value,
                StripInlineComment(line).Trim(),
                ExtractBraceOrStatementSource(content, start),
                order);
        }

        var functionMatch = Regex.Match(
            line,
            @"^\s*(?:(?:public|private|protected|static|final|abstract|virtual|override|inline|constexpr|extern|friend|synchronized|native)\s+)*(?<before>[A-Za-z_~][A-Za-z0-9_:<>~\[\]\*&\s,?]*\s+)?(?<name>[A-Za-z_~][A-Za-z0-9_]*)\s*\([^;]*\)\s*(?:const\s*)?(?:throws\s+[A-Za-z0-9_,\s]+)?(?:\{|;)",
            RegexOptions.CultureInvariant);

        if (!functionMatch.Success)
        {
            return null;
        }

        var name = functionMatch.Groups["name"].Value;

        if (BraceLanguageControlKeywords.Contains(name))
        {
            return null;
        }

        var kind = language == "c" ? "Function" : "Method";

        return CreateTextElement(
            kind,
            name,
            StripInlineComment(line).Trim().TrimEnd(';'),
            ExtractBraceOrStatementSource(content, start),
            order);
    }

    /// <summary>
    /// HTML has no inherent semantic units, so instead of enumerating every node, only two kinds of
    /// anchor are exposed: elements with an <c>id</c> attribute (the highest-value, most stable
    /// anchors), and structural containers as a fallback — always for a fixed set of semantic tags
    /// (section, form, nav, header, footer, main, article, aside), and for div/ul/ol/table only when
    /// they carry a named class. Everything else (bare divs, spans, labels with neither) is left out;
    /// the model is expected to anchor edits to those via a single verified line instead (see the
    /// "Unsupported source file type" guidance in the system prompt, which still applies to any
    /// element not covered by this outline).
    /// </summary>
    private static IReadOnlyList<SourceFileElement> ParseHtml(string content)
    {
        var elements = new List<SourceFileElement>();
        var order = 0;
        var pos = 0;

        while (pos < content.Length)
        {
            if (IsCommentStart(content, pos))
            {
                pos = SkipComment(content, pos);
                continue;
            }

            var tagMatch = HtmlTagOpenRegex.Match(content, pos);

            if (!tagMatch.Success)
            {
                break;
            }

            var tagName = tagMatch.Groups["tag"].Value;
            var attrs = tagMatch.Groups["attrs"].Value;
            var isSelfClosing = attrs.TrimEnd().EndsWith("/", StringComparison.Ordinal);
            var tagStart = tagMatch.Index;
            var tagEnd = tagMatch.Index + tagMatch.Length;

            var idMatch = HtmlIdAttrRegex.Match(attrs);
            var classMatch = HtmlClassAttrRegex.Match(attrs);
            var id = idMatch.Success ? idMatch.Groups["value"].Value : null;
            var className = classMatch.Success ? classMatch.Groups["value"].Value : null;
            var hasNamedClass = !string.IsNullOrWhiteSpace(className);

            var isAnchor = !string.IsNullOrEmpty(id) ||
                HtmlAlwaysStructuralTags.Contains(tagName) ||
                (HtmlClassGatedContainerTags.Contains(tagName) && hasNamedClass);

            if (isAnchor)
            {
                var blockEnd = tagEnd;

                if (!isSelfClosing && !HtmlVoidTags.Contains(tagName))
                {
                    var closeEnd = FindMatchingHtmlCloseTag(content, tagEnd, tagName);
                    blockEnd = closeEnd >= 0 ? closeEnd : tagEnd;
                }

                var name = !string.IsNullOrEmpty(id)
                    ? id
                    : hasNamedClass
                        ? className.Split([' '], StringSplitOptions.RemoveEmptyEntries).FirstOrDefault() ?? tagName
                        : tagName;

                elements.Add(new SourceFileElement
                {
                    Kind = ToTitleCase(tagName),
                    Name = name,
                    Signature = Clean(tagMatch.Value),
                    Source = content.Substring(tagStart, blockEnd - tagStart),
                    Order = order++
                });
            }

            if (!isSelfClosing && IsRawTextTag(tagName))
            {
                pos = SkipRawTextContent(content, tagEnd, tagName);
                continue;
            }

            pos = tagEnd;
        }

        return elements;
    }

    private static readonly Regex HtmlAnyTagRegex = new(
        @"<(?<close>/)?(?<name>[A-Za-z][A-Za-z0-9-]*)(?<attrs>(?:[^>""']|""[^""]*""|'[^']*')*)>",
        RegexOptions.Compiled);

    /// <summary>
    /// Scans forward from just past a start tag's '&gt;' for the <c>&lt;/tagName&gt;</c> that closes
    /// it, tracking every open/close tag it passes (not just same-name ones) so a nested
    /// &lt;script&gt;/&lt;style&gt; block's raw content — which can contain '&lt;' characters that
    /// look like markup, e.g. a JS comparison or CSS selector — is always skipped rather than only
    /// when it happens to fall on a position this scan already stopped at. Same-name nesting (e.g. a
    /// &lt;div&gt; containing another &lt;div&gt;) is tracked via <paramref name="tagName"/>'s depth so
    /// the match closes on the correct, outer, close tag. Returns the index just past the matching
    /// close tag's '&gt;', or -1 if the file has no matching close tag (malformed/truncated markup).
    /// </summary>
    private static int FindMatchingHtmlCloseTag(string content, int searchStart, string tagName)
    {
        var depth = 1;
        var pos = searchStart;

        while (pos < content.Length)
        {
            if (IsCommentStart(content, pos))
            {
                pos = SkipComment(content, pos);
                continue;
            }

            var match = HtmlAnyTagRegex.Match(content, pos);

            if (!match.Success)
            {
                return -1;
            }

            var name = match.Groups["name"].Value;
            var isClose = match.Groups["close"].Success;
            var isSelfClosing = match.Groups["attrs"].Value.TrimEnd().EndsWith("/", StringComparison.Ordinal);
            var tagEnd = match.Index + match.Length;

            if (isClose)
            {
                if (string.Equals(name, tagName, StringComparison.OrdinalIgnoreCase))
                {
                    depth--;

                    if (depth == 0)
                    {
                        return tagEnd;
                    }
                }

                pos = tagEnd;
                continue;
            }

            if (!isSelfClosing &&
                !HtmlVoidTags.Contains(name) &&
                string.Equals(name, tagName, StringComparison.OrdinalIgnoreCase))
            {
                depth++;
            }

            if (!isSelfClosing && IsRawTextTag(name))
            {
                pos = SkipRawTextContent(content, tagEnd, name);
                continue;
            }

            pos = tagEnd;
        }

        return -1;
    }

    private static bool IsRawTextTag(string tagName)
    {
        return tagName.Equals("script", StringComparison.OrdinalIgnoreCase) ||
            tagName.Equals("style", StringComparison.OrdinalIgnoreCase);
    }

    private static int SkipRawTextContent(string content, int contentStart, string tagName)
    {
        var closeIndex = content.IndexOf(
            "</" + tagName,
            contentStart,
            StringComparison.OrdinalIgnoreCase);

        if (closeIndex < 0)
        {
            return content.Length;
        }

        var closeEnd = content.IndexOf('>', closeIndex);

        return closeEnd < 0 ? content.Length : closeEnd + 1;
    }

    private static bool IsCommentStart(string content, int pos)
    {
        return pos + 4 <= content.Length && string.CompareOrdinal(content.Substring(pos, 4), "<!--") == 0;
    }

    private static int SkipComment(string content, int pos)
    {
        var end = content.IndexOf("-->", pos, StringComparison.Ordinal);

        return end < 0 ? content.Length : end + 3;
    }

    private static IReadOnlyList<SourceFileElement> FinalizeTextElementIds(
        string language,
        string relativePath,
        IReadOnlyList<SourceFileElement> elements)
    {
        var duplicateCounts = new Dictionary<string, int>(StringComparer.Ordinal);

        foreach (var element in elements.OrderBy(e => e.Order))
        {
            var baseId = BuildTextElementId(language, relativePath, element.Kind, element.Name, element.Signature);

            duplicateCounts.TryGetValue(baseId, out var count);
            duplicateCounts[baseId] = count + 1;

            element.Id = count == 0 ? baseId : baseId + ":" + count;
        }

        return elements
            .OrderBy(e => e.Order)
            .ToArray();
    }

    private static SourceFileElement CreateTextElement(
        string kind,
        string name,
        string signature,
        string source,
        int order)
    {
        return new SourceFileElement
        {
            Kind = kind,
            Name = name,
            Signature = Clean(signature),
            Source = source,
            Order = order
        };
    }

    private static string BuildTextElementId(
        string language,
        string relativePath,
        string kind,
        string name,
        string signature)
    {
        return $"G:{language}:{NormalizePath(relativePath)}:{kind}:{name}:{ShortHash(language + "|" + relativePath + "|" + kind + "|" + name + "|" + signature)}";
    }

    private static string ShortHash(string text)
    {
        using var sha256 = SHA256.Create();
        var bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(text ?? string.Empty));

        return BitConverter.ToString(bytes).Replace("-", string.Empty).Substring(0, 12).ToLowerInvariant();
    }

    private static string ExtractBraceOrStatementSource(
        string content,
        int start)
    {
        var braceIndex = IndexOfContentChar(content, '{', start);
        var semicolonIndex = IndexOfContentChar(content, ';', start);
        var lineEndIndex = IndexOfLineEnd(content, start);

        if (braceIndex >= 0 &&
            (semicolonIndex < 0 || braceIndex < semicolonIndex) &&
            (lineEndIndex < 0 || braceIndex <= lineEndIndex || semicolonIndex < 0))
        {
            var end = FindMatchingBrace(content, braceIndex);

            if (end >= 0)
            {
                return content.Substring(start, end - start + 1);
            }
        }

        if (semicolonIndex >= 0)
        {
            return content.Substring(start, semicolonIndex - start + 1);
        }

        if (lineEndIndex >= 0)
        {
            return content.Substring(start, lineEndIndex - start);
        }

        return content.Substring(start);
    }

    private static int FindMatchingBrace(
        string content,
        int openBraceIndex)
    {
        var depth = 0;
        var inString = false;
        var inChar = false;
        var inLineComment = false;
        var inBlockComment = false;
        var escape = false;

        for (var i = openBraceIndex; i < content.Length; i++)
        {
            var current = content[i];
            var next = i + 1 < content.Length ? content[i + 1] : '\0';

            if (inLineComment)
            {
                if (current is '\r' or '\n')
                {
                    inLineComment = false;
                }

                continue;
            }

            if (inBlockComment)
            {
                if (current == '*' && next == '/')
                {
                    inBlockComment = false;
                    i++;
                }

                continue;
            }

            if (inString)
            {
                if (escape)
                {
                    escape = false;
                }
                else if (current == '\\')
                {
                    escape = true;
                }
                else if (current == '"')
                {
                    inString = false;
                }

                continue;
            }

            if (inChar)
            {
                if (escape)
                {
                    escape = false;
                }
                else if (current == '\\')
                {
                    escape = true;
                }
                else if (current == '\'')
                {
                    inChar = false;
                }

                continue;
            }

            if (current == '/' && next == '/')
            {
                inLineComment = true;
                i++;
                continue;
            }

            if (current == '/' && next == '*')
            {
                inBlockComment = true;
                i++;
                continue;
            }

            if (current == '"')
            {
                inString = true;
                continue;
            }

            if (current == '\'')
            {
                inChar = true;
                continue;
            }

            if (current == '{')
            {
                depth++;
            }
            else if (current == '}')
            {
                depth--;

                if (depth == 0)
                {
                    return i;
                }
            }
        }

        return -1;
    }

    private static int IndexOfContentChar(
        string content,
        char character,
        int start)
    {
        var inString = false;
        var inChar = false;
        var escape = false;

        for (var i = start; i < content.Length; i++)
        {
            var current = content[i];

            if (current is '\r' or '\n')
            {
                return -1;
            }

            if (inString)
            {
                if (escape)
                {
                    escape = false;
                }
                else if (current == '\\')
                {
                    escape = true;
                }
                else if (current == '"')
                {
                    inString = false;
                }

                continue;
            }

            if (inChar)
            {
                if (escape)
                {
                    escape = false;
                }
                else if (current == '\\')
                {
                    escape = true;
                }
                else if (current == '\'')
                {
                    inChar = false;
                }

                continue;
            }

            if (current == '"')
            {
                inString = true;
                continue;
            }

            if (current == '\'')
            {
                inChar = true;
                continue;
            }

            if (current == character)
            {
                return i;
            }
        }

        return -1;
    }

    private static int IndexOfLineEnd(
        string content,
        int start)
    {
        for (var i = start; i < content.Length; i++)
        {
            if (content[i] is '\r' or '\n')
            {
                return i;
            }
        }

        return -1;
    }

    private static int GetPythonDecoratorStartLine(
        IReadOnlyList<SourceLine> lines,
        int declarationLine,
        int indent)
    {
        var start = declarationLine;

        for (var i = declarationLine - 1; i >= 0; i--)
        {
            var text = lines[i].Text;

            if (string.IsNullOrWhiteSpace(text))
            {
                continue;
            }

            var trimmed = text.TrimStart();

            if (!trimmed.StartsWith("@", StringComparison.Ordinal) ||
                CountLeadingWhitespace(text) != indent)
            {
                break;
            }

            start = i;
        }

        return start;
    }

    private static int GetPythonElementEndLine(
        IReadOnlyList<SourceLine> lines,
        int declarationLine,
        int indent)
    {
        for (var i = declarationLine + 1; i < lines.Count; i++)
        {
            var text = lines[i].Text;

            if (string.IsNullOrWhiteSpace(text))
            {
                continue;
            }

            var trimmed = text.TrimStart();

            if (trimmed.StartsWith("#", StringComparison.Ordinal))
            {
                continue;
            }

            if (CountLeadingWhitespace(text) <= indent)
            {
                return i;
            }
        }

        return lines.Count;
    }

    private static int CountLeadingWhitespace(string text)
    {
        var count = 0;

        foreach (var character in text)
        {
            if (!char.IsWhiteSpace(character))
            {
                break;
            }

            count++;
        }

        return count;
    }

    private static IReadOnlyList<SourceLine> SplitLines(string content)
    {
        var lines = new List<SourceLine>();
        var start = 0;

        for (var i = 0; i < content.Length; i++)
        {
            if (content[i] != '\r' && content[i] != '\n')
            {
                continue;
            }

            lines.Add(new SourceLine(start, content.Substring(start, i - start)));

            if (content[i] == '\r' && i + 1 < content.Length && content[i + 1] == '\n')
            {
                i++;
            }

            start = i + 1;
        }

        if (start <= content.Length)
        {
            lines.Add(new SourceLine(start, content.Substring(start)));
        }

        return lines;
    }

    private static string GetJavaScriptTypeKind(string keyword)
    {
        return keyword switch
        {
            "class" => "Class",
            "interface" => "Interface",
            "enum" => "Enum",
            "type" => "TypeAlias",
            _ => ToTitleCase(keyword)
        };
    }

    private static string ToTitleCase(string text)
    {
        return string.IsNullOrWhiteSpace(text)
            ? string.Empty
            : char.ToUpperInvariant(text[0]) + text.Substring(1);
    }

    private static string StripInlineComment(string text)
    {
        var index = text.IndexOf("//", StringComparison.Ordinal);

        if (index >= 0)
        {
            return text.Substring(0, index);
        }

        index = text.IndexOf('#');

        return index >= 0 ? text.Substring(0, index) : text;
    }

    public static string NormalizePath(string path)
    {
        return path?.Replace('\\', '/') ?? string.Empty;
    }

    private static string GetExtension(string path)
    {
        var index = path?.LastIndexOf('.') ?? -1;

        return index < 0 ? string.Empty : path.Substring(index).ToLowerInvariant();
    }

    private static string BuildQualifiedName(
        SyntaxNode node,
        string memberName)
    {
        var parts = new List<string>();
        var namespaceName = GetNamespaceName(node);

        if (!string.IsNullOrWhiteSpace(namespaceName))
        {
            parts.Add(namespaceName);
        }

        parts.AddRange(GetContainingTypeNames(node));
        parts.Add(memberName);

        return string.Join(".", parts.Where(p => !string.IsNullOrWhiteSpace(p)));
    }

    private static string GetNamespaceName(SyntaxNode node)
    {
        return string.Join(
            ".",
            node.Ancestors()
                .OfType<BaseNamespaceDeclarationSyntax>()
                .Reverse()
                .Select(n => GetSyntaxText(n.Name))
                .Where(n => !string.IsNullOrWhiteSpace(n)));
    }

    private static IEnumerable<string> GetContainingTypeNames(SyntaxNode node)
    {
        return node.Ancestors()
            .Where(n => n is TypeDeclarationSyntax or EnumDeclarationSyntax)
            .Reverse()
            .Select(GetTypeIdName)
            .Where(n => !string.IsNullOrWhiteSpace(n));
    }

    private static string GetTypeIdName(SyntaxNode node)
    {
        return node switch
        {
            TypeDeclarationSyntax typeDeclaration =>
                typeDeclaration.Identifier.ValueText + GetTypeParameterIdSuffix(typeDeclaration.TypeParameterList),
            EnumDeclarationSyntax enumDeclaration => enumDeclaration.Identifier.ValueText,
            _ => string.Empty
        };
    }

    private static string GetMemberIdName(
        ExplicitInterfaceSpecifierSyntax explicitInterfaceSpecifier,
        string name)
    {
        var explicitName = GetSyntaxText(explicitInterfaceSpecifier);

        return string.IsNullOrWhiteSpace(explicitName)
            ? name
            : explicitName + name;
    }

    private static string GetMemberSignatureName(
        ExplicitInterfaceSpecifierSyntax explicitInterfaceSpecifier,
        string name)
    {
        return GetMemberIdName(explicitInterfaceSpecifier, name);
    }

    private static string BuildParameterIdList(BaseParameterListSyntax parameterList)
    {
        if (parameterList == null)
        {
            return "()";
        }

        var parameterTypes = parameterList.Parameters
            .Select(parameter => Combine(
                GetParameterRefKind(parameter.Modifiers),
                GetSyntaxText(parameter.Type)))
            .ToArray();

        return "(" + string.Join(",", parameterTypes) + ")";
    }

    private static string GetParameterRefKind(SyntaxTokenList modifiers)
    {
        var modifier = modifiers
            .FirstOrDefault(m =>
                m.IsKind(SyntaxKind.RefKeyword) ||
                m.IsKind(SyntaxKind.OutKeyword) ||
                m.IsKind(SyntaxKind.InKeyword) ||
                m.IsKind(SyntaxKind.ParamsKeyword));

        return modifier.RawKind == 0 ? string.Empty : modifier.ValueText;
    }

    private static string GetMethodTypeParameterIdSuffix(MethodDeclarationSyntax declaration)
    {
        var count = declaration.TypeParameterList?.Parameters.Count ?? 0;
        return count == 0 ? string.Empty : "``" + count;
    }

    private static string GetTypeParameterIdSuffix(TypeParameterListSyntax typeParameterList)
    {
        var count = typeParameterList?.Parameters.Count ?? 0;
        return count == 0 ? string.Empty : "`" + count;
    }

    private static string BuildAccessorListSignature(
        AccessorListSyntax accessorList,
        bool isExpressionBodied)
    {
        if (accessorList == null)
        {
            return isExpressionBodied ? "{ get; }" : string.Empty;
        }

        var accessors = accessorList.Accessors
            .Select(accessor => Combine(
                GetModifiers(accessor.Modifiers),
                accessor.Keyword.ValueText + ";"));

        return "{ " + string.Join(" ", accessors) + " }";
    }

    private static string GetModifiers(SyntaxTokenList modifiers)
    {
        return Clean(modifiers.ToString());
    }

    private static string GetSyntaxText(SyntaxNode node)
    {
        return node == null ? string.Empty : Clean(node.WithoutTrivia().ToString());
    }

    private static string GetSyntaxText(SyntaxList<TypeParameterConstraintClauseSyntax> constraintClauses)
    {
        return Clean(string.Join(" ", constraintClauses.Select(GetSyntaxText)));
    }

    private static string Combine(params string[] parts)
    {
        return Clean(string.Join(" ", parts.Where(p => !string.IsNullOrWhiteSpace(p))));
    }

    private static string Clean(string text)
    {
        return string.IsNullOrWhiteSpace(text)
            ? string.Empty
            : Regex.Replace(text, @"\s+", " ").Trim();
    }

    private sealed class SourceLine(int start, string text)
    {
        public int Start { get; } = start;

        public string Text { get; } = text;
    }
}

