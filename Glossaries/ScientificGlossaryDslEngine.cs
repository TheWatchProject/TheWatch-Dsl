// <copyright file="ScientificGlossaryDslEngine.cs" company="The Watch, LLC">
// Copyright (c) 2026 Barton Milnor Mallory, The Watch, LLC. All rights reserved.
// </copyright>

/// <summary>
/// Source file: src/Libraries/TheWatch.Dsl/Glossaries/ScientificGlossaryDslEngine.cs
/// Module: Scientific, Engineering & Mathematical Glossary NLP/DSL Semantic Matching Engine
/// Defines: ScientificGlossaryDslEngine and GlossarySemanticMatchResult
/// Namespace: TheWatch.Dsl.Glossaries
/// </summary>

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using TheWatch.Abstractions.Glossaries;
using TheWatch.Abstractions.Taxonomy;
using TheWatch.Domain.Glossaries;

namespace TheWatch.Dsl.Glossaries;

/// <summary>
/// Result of an NLP entity-linking operation matching scientific terms in unstructured text.
/// </summary>
public sealed record GlossarySemanticMatchResult
{
    public required IGlossaryTerm Term { get; init; }
    public required string MatchedText { get; init; }
    public required int StartIndex { get; init; }
    public required int Length { get; init; }
    public required double ConfidenceScore { get; init; }
    public required ScientificGlossaryDomain Domain { get; init; }
    public required string WikipediaUrl { get; init; }
    public required IReadOnlyList<string> NaicsCodes { get; init; }
    public required IReadOnlyList<string> NapcsCodes { get; init; }
}

/// <summary>
/// High-speed NLP/DSL semantic engine for entity linking, scientific glossary resolution,
/// cross-domain term disambiguation, and Wikipedia knowledge base extraction.
/// </summary>
public sealed class ScientificGlossaryDslEngine
{
    private readonly IReadOnlyList<GlossaryTermRecord> _allTerms;

    public ScientificGlossaryDslEngine()
    {
        _allTerms = ScientificAndEngineeringGlossaryRegistry.GetAll();
    }

    public ScientificGlossaryDslEngine(IEnumerable<GlossaryTermRecord> customTerms)
    {
        _allTerms = customTerms.ToList().AsReadOnly();
    }

    /// <summary>
    /// Scans unstructured input text (such as dispatch transcripts or telemetry notes)
    /// and extracts all matched scientific, engineering, mathematical, legal, and grammatical glossary terms.
    /// </summary>
    public IReadOnlyList<GlossarySemanticMatchResult> ExtractTerms(string text, ScientificGlossaryDomain? domainFilter = null)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return Array.Empty<GlossarySemanticMatchResult>();
        }

        var results = new List<GlossarySemanticMatchResult>();
        var candidates = domainFilter.HasValue
            ? _allTerms.Where(t => t.Domain == domainFilter.Value)
            : _allTerms;

        foreach (var candidate in candidates)
        {
            // Match canonical term
            MatchTermInText(text, candidate.Term, candidate, 1.0, results);

            // If term contains parenthetical parts like "BLEVE (Boiling Liquid Expanding Vapor Explosion)", extract parts
            var parenIdx = candidate.Term.IndexOf('(');
            if (parenIdx > 0)
            {
                var prefix = candidate.Term[..parenIdx].Trim();
                if (prefix.Length >= 2)
                {
                    MatchTermInText(text, prefix, candidate, 0.95, results);
                }

                var closingIdx = candidate.Term.IndexOf(')', parenIdx);
                if (closingIdx > parenIdx)
                {
                    var inside = candidate.Term.Substring(parenIdx + 1, closingIdx - parenIdx - 1).Trim();
                    if (inside.Length >= 3)
                    {
                        MatchTermInText(text, inside, candidate, 0.95, results);
                    }
                }
            }

            // Match related aliases/synonyms
            foreach (var alias in candidate.RelatedTerms)
            {
                if (alias.Length >= 3)
                {
                    MatchTermInText(text, alias, candidate, 0.85, results);
                }
            }
        }

        return results
            .OrderBy(r => r.StartIndex)
            .ThenByDescending(r => r.ConfidenceScore)
            .ToList()
            .AsReadOnly();
    }

    /// <summary>
    /// Finds the closest matching scientific or engineering definition for a given query term or acronym.
    /// </summary>
    public GlossaryTermRecord? ResolveTerm(string termName, ScientificGlossaryDomain? domainHint = null)
    {
        if (string.IsNullOrWhiteSpace(termName)) return null;

        var clean = termName.Trim();

        // Exact match
        var exact = domainHint.HasValue
            ? ScientificAndEngineeringGlossaryRegistry.GetByDomain(domainHint.Value).FirstOrDefault(t => t.Term.Equals(clean, StringComparison.OrdinalIgnoreCase))
            : ScientificAndEngineeringGlossaryRegistry.FindByTerm(clean);

        if (exact != null) return exact;

        // Alias or partial match
        var candidates = domainHint.HasValue
            ? ScientificAndEngineeringGlossaryRegistry.GetByDomain(domainHint.Value)
            : _allTerms;

        return candidates.FirstOrDefault(t =>
            t.Term.Contains(clean, StringComparison.OrdinalIgnoreCase) ||
            t.RelatedTerms.Any(r => r.Equals(clean, StringComparison.OrdinalIgnoreCase))
        );
    }

    /// <summary>
    /// Aggregates all unique NAICS and NAPCS codes from terms identified in the text.
    /// </summary>
    public (IReadOnlyList<string> NaicsCodes, IReadOnlyList<string> NapcsCodes) ExtractClassifications(string text)
    {
        var matches = ExtractTerms(text);
        var naics = matches.SelectMany(m => m.NaicsCodes).Distinct().ToList().AsReadOnly();
        var napcs = matches.SelectMany(m => m.NapcsCodes).Distinct().ToList().AsReadOnly();
        return (naics, napcs);
    }

    private static void MatchTermInText(
        string text,
        string pattern,
        GlossaryTermRecord termRecord,
        double baseConfidence,
        List<GlossarySemanticMatchResult> results)
    {
        var escaped = Regex.Escape(pattern);
        var regex = new Regex($@"\b{escaped}\b", RegexOptions.IgnoreCase);
        var matches = regex.Matches(text);

        foreach (Match match in matches)
        {
            // Avoid duplicate exact spans
            if (!results.Any(r => r.StartIndex == match.Index && r.Length == match.Length && r.Term.TermId == termRecord.TermId))
            {
                results.Add(new GlossarySemanticMatchResult
                {
                    Term = termRecord,
                    MatchedText = match.Value,
                    StartIndex = match.Index,
                    Length = match.Length,
                    ConfidenceScore = baseConfidence,
                    Domain = termRecord.Domain,
                    WikipediaUrl = termRecord.WikipediaUrl,
                    NaicsCodes = termRecord.NaicsCodes,
                    NapcsCodes = termRecord.NapcsCodes
                });
            }
        }
    }
}
