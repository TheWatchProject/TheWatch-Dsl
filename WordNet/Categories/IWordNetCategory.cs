// <copyright file="IWordNetCategory.cs" company="The Watch, LLC">
// Copyright (c) 2026 Barton Milnor Mallory, The Watch, LLC. All rights reserved.
// </copyright>

/// <summary>
/// Source file: src/Libraries/TheWatch.Dsl/WordNet/Categories/IWordNetCategory.cs
/// Module: WordNet 3.0 Lexicographer Category Interfaces
/// Defines: interface IWordNetCategory, PartOfSpeech enum, and all 45 Lexicographer Category Interfaces
/// Namespace: TheWatch.Dsl.WordNet.Categories
/// </summary>

using System;
using System.Collections.Generic;

namespace TheWatch.Dsl.WordNet.Categories;

public enum WordNetPos
{
    Noun = 1,
    Verb = 2,
    Adjective = 3,
    Adverb = 4,
    AdjectiveSatellite = 5
}

/// <summary>
/// Base interface for all WordNet 3.0 Lexicographer Category hierarchies.
/// </summary>
public interface IWordNetCategory
{
    int LexographerFileNumber { get; }
    string CategoryName { get; }
    WordNetPos PartOfSpeech { get; }
    string Description { get; }
}

#region Noun Category Interfaces (26 Categories)

public interface INounCategory : IWordNetCategory { }

public interface INounTopsCategory : INounCategory { }
public interface INounActCategory : INounCategory { }
public interface INounAnimalCategory : INounCategory { }
public interface INounArtifactCategory : INounCategory { }
public interface INounAttributeCategory : INounCategory { }
public interface INounBodyCategory : INounCategory { }
public interface INounCognitionCategory : INounCategory { }
public interface INounCommunicationCategory : INounCategory { }
public interface INounEventCategory : INounCategory { }
public interface INounFeelingCategory : INounCategory { }
public interface INounFoodCategory : INounCategory { }
public interface INounGroupCategory : INounCategory { }
public interface INounLocationCategory : INounCategory { }
public interface INounMotiveCategory : INounCategory { }
public interface INounObjectCategory : INounCategory { }
public interface INounPersonCategory : INounCategory { }
public interface INounPhenomenonCategory : INounCategory { }
public interface INounPlantCategory : INounCategory { }
public interface INounPossessionCategory : INounCategory { }
public interface INounProcessCategory : INounCategory { }
public interface INounQuantityCategory : INounCategory { }
public interface INounRelationCategory : INounCategory { }
public interface INounShapeCategory : INounCategory { }
public interface INounStateCategory : INounCategory { }
public interface INounSubstanceCategory : INounCategory { }
public interface INounTimeCategory : INounCategory { }

#endregion

#region Verb Category Interfaces (15 Categories)

public interface IVerbCategory : IWordNetCategory { }

public interface IVerbBodyCategory : IVerbCategory { }
public interface IVerbChangeCategory : IVerbCategory { }
public interface IVerbCognitionCategory : IVerbCategory { }
public interface IVerbCommunicationCategory : IVerbCategory { }
public interface IVerbCompetitionCategory : IVerbCategory { }
public interface IVerbConsumptionCategory : IVerbCategory { }
public interface IVerbContactCategory : IVerbCategory { }
public interface IVerbCreationCategory : IVerbCategory { }
public interface IVerbEmotionCategory : IVerbCategory { }
public interface IVerbMotionCategory : IVerbCategory { }
public interface IVerbPerceptionCategory : IVerbCategory { }
public interface IVerbPossessionCategory : IVerbCategory { }
public interface IVerbSocialCategory : IVerbCategory { }
public interface IVerbStativeCategory : IVerbCategory { }
public interface IVerbWeatherCategory : IVerbCategory { }

#endregion

#region Adjective and Adverb Category Interfaces (4 Categories)

public interface IAdjectiveCategory : IWordNetCategory { }
public interface IAdverbCategory : IWordNetCategory { }

public interface IAdjAllCategory : IAdjectiveCategory { }
public interface IAdjPertCategory : IAdjectiveCategory { }
public interface IAdjPplCategory : IAdjectiveCategory { }
public interface IAdvAllCategory : IAdverbCategory { }

#endregion
