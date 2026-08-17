// <copyright file="EmergencyNlpIntentParser.cs" company="The Watch, LLC">
// Copyright (c) 2026 Barton Milnor Mallory, The Watch, LLC. All rights reserved.
// </copyright>

/// <summary>
/// Source file: src/Libraries/TheWatch.Dsl/EmergencyNlpIntentParser.cs
/// Module: Domain-Specific Language Compiler, Lexers & Scientific Measurements
/// Defines: record NlpEntity, record NlpIntentClassification, class EmergencyNlpIntentParser
/// Namespace: TheWatch.Dsl
/// </summary>
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using TheWatch.Dsl.WordNet.Engine;
using TheWatch.Dsl.WordNet.Models;

namespace TheWatch.Dsl;

public sealed record NlpEntity(
    string Category,
    string Value,
    int StartIndex,
    int Length
);

public sealed record NlpIntentClassification(
    string PrimaryIntent,
    double Confidence,
    IReadOnlyList<NlpEntity> ExtractedEntities,
    IReadOnlyList<string> RecommendedDispatchProtocols,
    bool RequiresSilentDuressMode,
    IReadOnlyList<SemanticClassificationResult>? WordNetConcepts = null,
    IReadOnlyList<BaseWordNetEntity>? SynthesizedCategoryModels = null
);

/// <summary>
/// Natural Language Processing (NLP) and Semantic Intent Extraction Engine for 911 Calls and Voice Triggers.
/// </summary>
public sealed class EmergencyNlpIntentParser
{
    private static readonly Dictionary<string, (string Intent, string[] Protocols, double BaseConfidence)> TriggerKeywords = new(StringComparer.OrdinalIgnoreCase)
    {
        ["mayday"] = ("MEDICAL_MAYDAY", new[] { "DISPATCH_ALS_AMBULANCE", "STAGE_HELO" }, 0.99),
        ["fire"] = ("STRUCTURAL_FIRE", new[] { "DISPATCH_FIRE_ENGINE", "POLICE_PERIMETER" }, 0.95),
        ["smoke"] = ("HAZARD_INVESTIGATION", new[] { "DISPATCH_FIRE_RECON" }, 0.85),
        ["trauma"] = ("MEDICAL_TRAUMA", new[] { "DISPATCH_LEVEL_1_TRAUMA_TEAM" }, 0.95),
        ["gunshot"] = ("ACTIVE_VIOLENCE", new[] { "DISPATCH_TACTICAL_POLICE", "PARAMEDIC_STAGE_SAFE" }, 0.98),
        ["shooting"] = ("ACTIVE_VIOLENCE", new[] { "DISPATCH_TACTICAL_POLICE", "MASS_CASUALTY_ALERT" }, 0.98),
        ["evacuate"] = ("REQUEST_EVACUATION", new[] { "SOUND_WEA_ALERTS", "ACTIVATE_CORRIDOR_GEOFENCE" }, 0.94),
        ["trapped"] = ("SEARCH_AND_RESCUE", new[] { "DISPATCH_USAR_TECHNICAL_RESCUE" }, 0.96),
        ["duress"] = ("TRIGGER_DURESS", new[] { "SILENT_POLICE_DISPATCH", "MASK_UI_NORMAL" }, 0.99),
        ["help me"] = ("GENERAL_EMERGENCY", new[] { "DISPATCH_NEAREST_UNIT" }, 0.90),
        ["chest pain"] = ("CARDIAC_ARREST_RISK", new[] { "DISPATCH_AED_UNIT", "PARAMEDIC_ALS" }, 0.96),
        ["unconscious"] = ("UNRESPONSIVE_PATIENT", new[] { "DISPATCH_ALS_PARAMEDIC" }, 0.95)
    };

    private readonly WordNetSemanticDslEngine _wordNetEngine;

    public EmergencyNlpIntentParser(WordNetSemanticDslEngine? wordNetEngine = null)
    {
        _wordNetEngine = wordNetEngine ?? new WordNetSemanticDslEngine();
    }

    public NlpIntentClassification ParseCallTranscript(string transcript)
    {
        if (string.IsNullOrWhiteSpace(transcript))
        {
            return new NlpIntentClassification(
                PrimaryIntent: "UNKNOWN",
                Confidence: 0.0,
                ExtractedEntities: Array.Empty<NlpEntity>(),
                RecommendedDispatchProtocols: Array.Empty<string>(),
                RequiresSilentDuressMode: false,
                WordNetConcepts: Array.Empty<SemanticClassificationResult>(),
                SynthesizedCategoryModels: Array.Empty<BaseWordNetEntity>()
            );
        }

        var entities = new List<NlpEntity>();

        // 1. Extract Trapped Victims / Persons Count (e.g. "3 people trapped", "two victims")
        var countMatch = Regex.Match(transcript, @"\b(\d+|one|two|three|four|five|six|seven|eight|nine|ten)\s+(people|persons|victims|injured|trapped)\b", RegexOptions.IgnoreCase);
        if (countMatch.Success)
        {
            entities.Add(new NlpEntity("VICTIM_COUNT", countMatch.Value, countMatch.Index, countMatch.Length));
        }

        // 2. Extract Location Street Addresses (e.g. "at 123 Main St", "on Broadway Avenue")
        var addressMatch = Regex.Match(transcript, @"\b\d+\s+[A-Za-z0-9\s]+(Street|St|Avenue|Ave|Road|Rd|Boulevard|Blvd|Lane|Ln|Drive|Dr|Way)\b", RegexOptions.IgnoreCase);
        if (addressMatch.Success)
        {
            entities.Add(new NlpEntity("STREET_ADDRESS", addressMatch.Value, addressMatch.Index, addressMatch.Length));
        }

        // 3. Match Intent Keywords
        string bestIntent = "GENERAL_EMERGENCY";
        double maxConf = 0.50;
        var protocols = new HashSet<string>();
        bool isDuress = false;

        foreach (var (kw, meta) in TriggerKeywords)
        {
            var match = Regex.Match(transcript, $@"\b{Regex.Escape(kw)}\b", RegexOptions.IgnoreCase);
            if (match.Success)
            {
                entities.Add(new NlpEntity("KEYWORD_TRIGGER", kw, match.Index, match.Length));
                if (meta.BaseConfidence > maxConf)
                {
                    maxConf = meta.BaseConfidence;
                    bestIntent = meta.Intent;
                }
                foreach (var p in meta.Protocols)
                {
                    protocols.Add(p);
                }
                if (meta.Intent == "TRIGGER_DURESS")
                {
                    isDuress = true;
                }
            }
        }

        // 4. WordNet Semantic Extraction & Category Model Synthesis
        var wordNetConcepts = _wordNetEngine.ExtractConcepts(transcript);
        var categoryModels = wordNetConcepts.Select(c => c.SynthesizedDataModel).ToList();

        return new NlpIntentClassification(
            PrimaryIntent: bestIntent,
            Confidence: maxConf,
            ExtractedEntities: entities,
            RecommendedDispatchProtocols: protocols.ToList(),
            RequiresSilentDuressMode: isDuress,
            WordNetConcepts: wordNetConcepts,
            SynthesizedCategoryModels: categoryModels
        );
    }
}
