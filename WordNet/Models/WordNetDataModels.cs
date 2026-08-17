// <copyright file="WordNetDataModels.cs" company="The Watch, LLC">
// Copyright (c) 2026 Barton Milnor Mallory, The Watch, LLC. All rights reserved.
// </copyright>

/// <summary>
/// Source file: src/Libraries/TheWatch.Dsl/WordNet/Models/WordNetDataModels.cs
/// Module: WordNet 3.0 Strongly-Typed Category Data Models with NAICS & NAPCS Multi-Classification
/// Defines: Synset, Lexeme, and Category Data Models
/// Namespace: TheWatch.Dsl.WordNet.Models
/// </summary>

using System;
using System.Collections.Generic;
using System.Linq;
using TheWatch.Abstractions.Audit;
using TheWatch.Dsl.WordNet.Categories;
using TheWatch.Dsl.WordNet.Taxonomy;

namespace TheWatch.Dsl.WordNet.Models;

public interface IWordNetDataModel
{
    string SynsetId { get; }
    string Lemma { get; }
    string Gloss { get; }
    IReadOnlyList<string> Synonyms { get; }
    IReadOnlyList<string> HypernymSynsetIds { get; }
    IReadOnlyList<string> HyponymSynsetIds { get; }
}

public abstract class BaseWordNetEntity : AuditableEntity, IWordNetDataModel, INaicsNapcsCategorizable
{
    public string SynsetId { get; set; } = string.Empty;
    public string Lemma { get; set; } = string.Empty;
    public string Gloss { get; set; } = string.Empty;
    public List<string> Synonyms { get; set; } = new();
    public List<string> HypernymSynsetIds { get; set; } = new();
    public List<string> HyponymSynsetIds { get; set; } = new();

    // NAICS & NAPCS Multi-Classification
    public List<NaicsClassification> NaicsClassifications { get; set; } = new();
    public List<NapcsClassification> NapcsClassifications { get; set; } = new();

    public IReadOnlyList<string> NaicsCodes => NaicsClassifications.Select(n => n.Code).ToList();
    public IReadOnlyList<string> NapcsCodes => NapcsClassifications.Select(n => n.Code).ToList();
    IReadOnlyList<NaicsClassification> INaicsNapcsCategorizable.NaicsClassifications => NaicsClassifications;
    IReadOnlyList<NapcsClassification> INaicsNapcsCategorizable.NapcsClassifications => NapcsClassifications;

    IReadOnlyList<string> IWordNetDataModel.Synonyms => Synonyms;
    IReadOnlyList<string> IWordNetDataModel.HypernymSynsetIds => HypernymSynsetIds;
    IReadOnlyList<string> IWordNetDataModel.HyponymSynsetIds => HyponymSynsetIds;

    public void PopulateTaxonomy(int lexFileNumber)
    {
        var (naics, napcs) = NaicsNapcsTaxonomyRegistry.GetClassifications(lexFileNumber, Lemma);
        NaicsClassifications = naics.ToList();
        NapcsClassifications = napcs.ToList();
    }
}

#region Noun Data Models

public class ArtifactEntityModel : BaseWordNetEntity, INounArtifactCategory
{
    public int LexographerFileNumber => 6;
    public string CategoryName => "noun.artifact";
    public WordNetPos PartOfSpeech => WordNetPos.Noun;
    public string Description => "Man-made tactical objects, vehicles, radios, and gear.";
    public string? SerialNumber { get; set; }
    public string? Manufacturer { get; set; }
    public double MaintenanceScore { get; set; } = 100.0;

    public ArtifactEntityModel()
    {
        PopulateTaxonomy(6);
    }
}

public class PersonEntityModel : BaseWordNetEntity, INounPersonCategory
{
    public int LexographerFileNumber => 18;
    public string CategoryName => "noun.person";
    public WordNetPos PartOfSpeech => WordNetPos.Noun;
    public string Description => "People, emergency responders, dispatchers, civilians, and victims.";
    public string? CallSign { get; set; }
    public string? BadgeNumber { get; set; }
    public string? Role { get; set; }
    public double DutyScore { get; set; } = 100.0;

    public PersonEntityModel()
    {
        PopulateTaxonomy(18);
    }
}

public class LocationEntityModel : BaseWordNetEntity, INounLocationCategory
{
    public int LexographerFileNumber => 15;
    public string CategoryName => "noun.location";
    public WordNetPos PartOfSpeech => WordNetPos.Noun;
    public string Description => "Spatial positions, coordinates, emergency perimeter zones, and shelters.";
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public double ElevationMeters { get; set; }
    public double GeofenceRadiusMeters { get; set; }

    public LocationEntityModel()
    {
        PopulateTaxonomy(15);
    }
}

public class EventEntityModel : BaseWordNetEntity, INounEventCategory
{
    public int LexographerFileNumber => 11;
    public string CategoryName => "noun.event";
    public WordNetPos PartOfSpeech => WordNetPos.Noun;
    public string Description => "Natural events, incidents, emergencies, and crisis occurrences.";
    public string Severity { get; set; } = "High";
    public DateTimeOffset OccurredAtUtc { get; set; } = DateTimeOffset.UtcNow;

    public EventEntityModel()
    {
        PopulateTaxonomy(11);
    }
}

public class ActEntityModel : BaseWordNetEntity, INounActCategory
{
    public int LexographerFileNumber => 4;
    public string CategoryName => "noun.act";
    public WordNetPos PartOfSpeech => WordNetPos.Noun;
    public string Description => "Acts, operations, interventions, and triage actions.";
    public string TargetIncidentId { get; set; } = string.Empty;

    public ActEntityModel()
    {
        PopulateTaxonomy(4);
    }
}

public class GroupEntityModel : BaseWordNetEntity, INounGroupCategory
{
    public int LexographerFileNumber => 14;
    public string CategoryName => "noun.group";
    public WordNetPos PartOfSpeech => WordNetPos.Noun;
    public string Description => "Squads, strike teams, search and rescue units, and coalitions.";
    public int MemberCount { get; set; }
    public string CommanderId { get; set; } = string.Empty;

    public GroupEntityModel()
    {
        PopulateTaxonomy(14);
    }
}

public class SubstanceEntityModel : BaseWordNetEntity, INounSubstanceCategory
{
    public int LexographerFileNumber => 27;
    public string CategoryName => "noun.substance";
    public WordNetPos PartOfSpeech => WordNetPos.Noun;
    public string Description => "Hazmat materials, toxic chemicals, water, and fuel.";
    public string HazmatClass { get; set; } = "Class-0";
    public bool IsFlammable { get; set; }

    public SubstanceEntityModel()
    {
        PopulateTaxonomy(27);
    }
}

public class StateEntityModel : BaseWordNetEntity, INounStateCategory
{
    public int LexographerFileNumber => 26;
    public string CategoryName => "noun.state";
    public WordNetPos PartOfSpeech => WordNetPos.Noun;
    public string Description => "Stable conditions, triage red/yellow/green states, and readiness levels.";
    public string ReadinessLevel { get; set; } = "DEFCON-4";

    public StateEntityModel()
    {
        PopulateTaxonomy(26);
    }
}

public class TimeEntityModel : BaseWordNetEntity, INounTimeCategory
{
    public int LexographerFileNumber => 28;
    public string CategoryName => "noun.time";
    public WordNetPos PartOfSpeech => WordNetPos.Noun;
    public string Description => "Time intervals, duration, and chronological windows.";
    public TimeSpan Duration { get; set; }

    public TimeEntityModel()
    {
        PopulateTaxonomy(28);
    }
}

public class CognitionEntityModel : BaseWordNetEntity, INounCognitionCategory
{
    public int LexographerFileNumber => 9;
    public string CategoryName => "noun.cognition";
    public WordNetPos PartOfSpeech => WordNetPos.Noun;
    public string Description => "AI decisions, inference predictions, and situational analyses.";
    public double ConfidenceScore { get; set; } = 1.0;

    public CognitionEntityModel()
    {
        PopulateTaxonomy(9);
    }
}

#endregion

#region Verb Data Models

public class MotionActionModel : BaseWordNetEntity, IVerbMotionCategory
{
    public int LexographerFileNumber => 38;
    public string CategoryName => "verb.motion";
    public WordNetPos PartOfSpeech => WordNetPos.Verb;
    public string Description => "Navigating, dispatching, evacuating, patrolling, and flying.";
    public double SpeedKph { get; set; }
    public string Destination { get; set; } = string.Empty;

    public MotionActionModel()
    {
        PopulateTaxonomy(38);
    }
}

public class CommunicationActionModel : BaseWordNetEntity, IVerbCommunicationCategory
{
    public int LexographerFileNumber => 32;
    public string CategoryName => "verb.communication";
    public WordNetPos PartOfSpeech => WordNetPos.Verb;
    public string Description => "Broadcasting, signaling, alerting, and transmitting radio packets.";
    public string RadioChannel { get; set; } = "TACTICAL-1";
    public string MessagePayload { get; set; } = string.Empty;

    public CommunicationActionModel()
    {
        PopulateTaxonomy(32);
    }
}

public class PerceptionActionModel : BaseWordNetEntity, IVerbPerceptionCategory
{
    public int LexographerFileNumber => 39;
    public string CategoryName => "verb.perception";
    public WordNetPos PartOfSpeech => WordNetPos.Verb;
    public string Description => "Detecting, scanning, acoustic listening, and thermal imaging.";
    public string SensorType { get; set; } = "ThermalInfrared";
    public double SensorSignalToNoiseRatio { get; set; } = 28.5;

    public PerceptionActionModel()
    {
        PopulateTaxonomy(39);
    }
}

public class ChangeActionModel : BaseWordNetEntity, IVerbChangeCategory
{
    public int LexographerFileNumber => 30;
    public string CategoryName => "verb.change";
    public WordNetPos PartOfSpeech => WordNetPos.Verb;
    public string Description => "State transitions, escalating triage levels, and modifying perimeters.";
    public string FromState { get; set; } = "Normal";
    public string ToState { get; set; } = "Emergency";

    public ChangeActionModel()
    {
        PopulateTaxonomy(30);
    }
}

public class WeatherActionModel : BaseWordNetEntity, IVerbWeatherCategory
{
    public int LexographerFileNumber => 43;
    public string CategoryName => "verb.weather";
    public WordNetPos PartOfSpeech => WordNetPos.Verb;
    public string Description => "Storming, raining, flooding, and wildfire spreading.";
    public double WindSpeedKnots { get; set; }
    public double PrecipitationMmPerHour { get; set; }

    public WeatherActionModel()
    {
        PopulateTaxonomy(43);
    }
}

#endregion
