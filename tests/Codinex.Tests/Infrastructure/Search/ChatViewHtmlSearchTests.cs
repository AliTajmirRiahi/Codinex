using System.Collections.Generic;
using Codinex.Core.Interfaces.Search;
using Codinex.Core.Models.Search;
using Codinex.Infrastructure.Search;
using Codinex.Infrastructure.Search.Algorithms;
using FluentAssertions;
using NUnit.Framework;

namespace Codinex.Tests.Infrastructure.Search;

/// <summary>
/// Exercises the engine against a real-world scenario: an AI-generated <c>TextFileChange.Search</c>
/// payload targeting <c>src/Codinex.UI/ToolWindows/Resources/Chat/view/chat-view.html</c>'s
/// about-peek button, where the file has since drifted from what the payload expects (the real markup
/// uses a &lt;codinex-image&gt; icon and an id-based button; the payload assumes a &lt;span&gt; chevron
/// and a data-action button). This is exactly the "locate a probable location for an AI-generated
/// change" case described in the engine's design brief.
/// </summary>
[TestFixture]
public sealed class ChatViewHtmlSearchTests
{
    // Lines 125-148 of chat-view.html as it exists today. The about-peek-container block below starts
    // at excerpt line 10 (file line 134).
    private const string ChatViewHtmlExcerpt =
        "                    <div class=\"codinex-error-box__title\">Error</div>\n" +
        "                    <div class=\"codinex-error-box__message\">\n" +
        "                        There is some problem, please check output for details\n" +
        "                    </div>\n" +
        "                </div>\n" +
        "            </div>\n" +
        "        </div>\n" +
        "\n" +
        "        <!-- About / donate peek box above the input area -->\n" +
        "        <div class=\"about-peek-container\">\n" +
        "            <div class=\"about-peek-box\" title=\"About Codinex AI\">\n" +
        "                <codinex-image name=\"chevron-up.svg\" class=\"about-peek-chevron\"></codinex-image>\n" +
        "                <button id=\"about-menu-btn\" class=\"about-peek-button\" title=\"About Codinex AI\">\n" +
        "                    <span>About</span>\n" +
        "                </button>\n" +
        "            </div>\n" +
        "        </div>\n" +
        "\n" +
        "        <!-- Shown while a Code Changes review is pending; locks the composer until decided -->\n" +
        "        <div id=\"chat-blocked-banner\" class=\"chat-blocked-banner hidden\" role=\"button\" tabindex=\"0\"\n" +
        "             title=\"Click to reopen the Code Changes window\">\n" +
        "            <codinex-icon name=\"scroll-text\"></codinex-icon>\n" +
        "            <span>You have a pending code change to review before you can continue. Click to reopen it.</span>\n" +
        "        </div>\n";

    // The current, real about-peek-container block (file lines 134-141).
    private const string CurrentAboutButtonBlock =
        "        <div class=\"about-peek-container\">\n" +
        "            <div class=\"about-peek-box\" title=\"About Codinex AI\">\n" +
        "                <codinex-image name=\"chevron-up.svg\" class=\"about-peek-chevron\"></codinex-image>\n" +
        "                <button id=\"about-menu-btn\" class=\"about-peek-button\" title=\"About Codinex AI\">\n" +
        "                    <span>About</span>\n" +
        "                </button>\n" +
        "            </div>\n" +
        "        </div>";

    // Verbatim shape of the "Search" value from the EditFileChange payload targeting chat-view.html:
    // a <span> chevron and a data-action button, neither of which exist in the file above anymore.
    private const string StaleAiGeneratedSearchText =
        "        <div class=\"about-peek-container\">\n" +
        "            <div class=\"about-peek-box\" title=\"About Codinex AI\">\n" +
        "                <span class=\"about-peek-chevron\" aria-hidden=\"true\">⌄</span>\n" +
        "                <button\n" +
        "                    type=\"button\"\n" +
        "                    class=\"about-peek-button\"\n" +
        "                    data-action=\"open-about\"\n" +
        "                    title=\"About Codinex AI\">\n" +
        "                    About\n" +
        "                </button>\n" +
        "            </div>\n" +
        "        </div>";

    private static ICodeSearchEngine CreateSut()
    {
        var algorithms = new List<IStringSearchAlgorithm>
        {
            new NaiveStringSearchAlgorithm(),
            new KmpStringSearchAlgorithm(),
            new BoyerMooreStringSearchAlgorithm(),
            new BoyerMooreHorspoolStringSearchAlgorithm(),
            new RabinKarpStringSearchAlgorithm(),
            new TwoWayStringSearchAlgorithm(),
            new AhoCorasickStringSearchAlgorithm(),
            new FuzzyStringSearchAlgorithm()
        };

        return new CodeSearchEngine(algorithms, new SearchAlgorithmSelector());
    }

    [Test]
    public void Search_CurrentAboutButtonBlock_IsFoundExactlyOnce_AtItsRealLocation()
    {
        var sut = CreateSut();

        var result = sut.Search(new SearchRequest
        {
            Text = ChatViewHtmlExcerpt,
            Pattern = CurrentAboutButtonBlock,
            Options = new StringSearchOptions()
        });

        result.Matches.Should().ContainSingle();
        result.Matches[0].StartLine.Should().Be(10);
        result.Matches[0].StartColumn.Should().Be(1);
        result.Matches[0].MatchedText.Should().Be(CurrentAboutButtonBlock);
    }

    [Test]
    public void Search_StaleAiGeneratedSearchText_DoesNotExactMatch_CurrentFile()
    {
        // Proves the drift is real: an exact/whole-word search engine alone cannot resolve this
        // TextFileChange against today's chat-view.html — a validator needs a fallback strategy.
        var sut = CreateSut();

        var result = sut.Search(new SearchRequest
        {
            Text = ChatViewHtmlExcerpt,
            Pattern = StaleAiGeneratedSearchText,
            Options = new StringSearchOptions { Algorithm = SearchAlgorithmType.BoyerMooreHorspool }
        });

        result.Matches.Should().BeEmpty();
    }

    [Test]
    public void Search_DistinctiveAnchorFromStalePayload_StillLocatesTheRealBlock()
    {
        // "about-peek-container" survived the drift (it's the class name, not the markup that changed),
        // so anchoring on it — rather than the whole stale block — still finds the right neighborhood.
        var sut = CreateSut();

        var result = sut.Search(new SearchRequest
        {
            Text = ChatViewHtmlExcerpt,
            Pattern = "about-peek-container",
            Options = new StringSearchOptions()
        });

        result.Matches.Should().ContainSingle();
        result.Matches[0].StartLine.Should().Be(10);
    }

    [Test]
    public void Search_AboutButtonText_HasMultipleCandidates_LeftForValidatorToDisambiguate()
    {
        // "About" alone appears both in the button's accessible text and inside the surrounding
        // comment/title attributes — the engine must return every candidate, not guess.
        var sut = CreateSut();

        var result = sut.Search(new SearchRequest
        {
            Text = ChatViewHtmlExcerpt,
            Pattern = "About",
            Options = new StringSearchOptions()
        });

        result.Matches.Count.Should().BeGreaterThan(1);
    }
}
