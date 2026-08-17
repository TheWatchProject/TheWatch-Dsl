// <copyright file="WordNetSemanticDslEngine.cs" company="The Watch, LLC">
// Copyright (c) 2026 Barton Milnor Mallory, The Watch, LLC. All rights reserved.
// </copyright>

/// <summary>
/// Source file: src/Libraries/TheWatch.Dsl/WordNet/Engine/WordNetSemanticDslEngine.cs
/// Module: NLP Semantic Category Resolution & Strongly-Typed Data Model Synthesis
/// Defines: class WordNetSemanticDslEngine, record SemanticClassificationResult
/// Namespace: TheWatch.Dsl.WordNet.Engine
/// </summary>

using System;
using System.Collections.Generic;
using System.Linq;
using TheWatch.Dsl.WordNet.Categories;
using TheWatch.Dsl.WordNet.Models;

namespace TheWatch.Dsl.WordNet.Engine;

public sealed record SemanticClassificationResult(
    string RawText,
    string MatchedLemma,
    WordNetSynsetRecord Synset,
    string CategoryName,
    IWordNetCategory CategoryInterface,
    BaseWordNetEntity SynthesizedDataModel);

public sealed class WordNetSemanticDslEngine
{
    private readonly WordNetDatabaseLoader _loader;

    public WordNetDatabaseLoader Loader => _loader;

    public WordNetSemanticDslEngine(WordNetDatabaseLoader? loader = null)
    {
        _loader = loader ?? new WordNetDatabaseLoader();
    }

    /// <summary>
    /// Parses an NLP command or sentence and extracts all semantic concepts mapped to WordNet categories and data models.
    /// </summary>
    public IReadOnlyList<SemanticClassificationResult> ExtractConcepts(string naturalLanguageSentence)
    {
        var results = new List<SemanticClassificationResult>();
        if (string.IsNullOrWhiteSpace(naturalLanguageSentence)) return results;

        var words = naturalLanguageSentence
            .ToLowerInvariant()
            .Split(new[] { ' ', ',', '.', '!', '?', ';', ':', '-', '/' }, StringSplitOptions.RemoveEmptyEntries);

        foreach (var word in words)
        {
            var synsets = _loader.LookupByLemma(word);
            if (synsets.Count > 0)
            {
                var primarySynset = synsets[0];
                var model = CreateDataModelFromSynset(primarySynset, word);
                if (model is IWordNetCategory categoryInterface)
                {
                    results.Add(new SemanticClassificationResult(
                        RawText: word,
                        MatchedLemma: word,
                        Synset: primarySynset,
                        CategoryName: categoryInterface.CategoryName,
                        CategoryInterface: categoryInterface,
                        SynthesizedDataModel: model));
                }
            }
        }

        return results;
    }

    /// <summary>
    /// Instantiates a strongly-typed Category Data Model from a WordNet Synset based on its Lexicographer category.
    /// </summary>
    public BaseWordNetEntity CreateDataModelFromSynset(WordNetSynsetRecord synset, string? activeLemma = null)
    {
        var lemma = activeLemma ?? synset.Words.FirstOrDefault() ?? "concept";

        BaseWordNetEntity entity = synset.LexFileNum switch
        {
            6 => new ArtifactEntityModel(),        // noun.artifact
            18 => new PersonEntityModel(),        // noun.person
            15 => new LocationEntityModel(),      // noun.location
            11 => new EventEntityModel(),         // noun.event
            4 => new ActEntityModel(),            // noun.act
            14 => new GroupEntityModel(),         // noun.group
            27 => new SubstanceEntityModel(),     // noun.substance
            26 => new StateEntityModel(),         // noun.state
            28 => new TimeEntityModel(),          // noun.time
            9 => new CognitionEntityModel(),      // noun.cognition
            38 => new MotionActionModel(),        // verb.motion
            32 => new CommunicationActionModel(), // verb.communication
            39 => new PerceptionActionModel(),    // verb.perception
            30 => new ChangeActionModel(),        // verb.change
            43 => new WeatherActionModel(),       // verb.weather
            _ => synset.Pos == WordNetPos.Verb ? new MotionActionModel() : new ArtifactEntityModel()
        };

        entity.SynsetId = synset.SynsetId;
        entity.Lemma = lemma;
        entity.Gloss = synset.Gloss;
        entity.Synonyms = synset.Words.ToList();
        entity.HypernymSynsetIds = synset.Hypernyms.ToList();
        entity.HyponymSynsetIds = synset.Hyponyms.ToList();

        return entity;
    }

    /// <summary>
    /// Calculates semantic similarity score between two words using WordNet category and hypernym tree overlaps.
    /// </summary>
    public double CalculateSemanticSimilarity(string word1, string word2)
    {
        if (string.Equals(word1, word2, StringComparison.OrdinalIgnoreCase)) return 1.0;

        var syns1 = _loader.LookupByLemma(word1);
        var syns2 = _loader.LookupByLemma(word2);

        if (syns1.Count == 0 || syns2.Count == 0) return 0.0;

        // Exact synset match
        if (syns1.Any(s1 => syns2.Any(s2 => s1.SynsetId == s2.SynsetId))) return 0.95;

        // Same lexicographer category
        if (syns1.Any(s1 => syns2.Any(s2 => s1.LexFileNum == s2.LexFileNum))) return 0.70;

        // Same Part of Speech
        if (syns1.Any(s1 => syns2.Any(s2 => s1.Pos == s2.Pos))) return 0.40;

        return 0.10;
    }
}
