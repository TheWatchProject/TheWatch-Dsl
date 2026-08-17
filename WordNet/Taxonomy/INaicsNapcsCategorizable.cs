// <copyright file="INaicsNapcsCategorizable.cs" company="The Watch, LLC">
// Copyright (c) 2026 Barton Milnor Mallory, The Watch, LLC. All rights reserved.
// </copyright>

/// <summary>
/// Source file: src/Libraries/TheWatch.Dsl/WordNet/Taxonomy/INaicsNapcsCategorizable.cs
/// Module: NAICS & NAPCS Multi-Classification Interfaces and Records
/// Defines: interface INaicsNapcsCategorizable, record NaicsClassification, record NapcsClassification
/// Namespace: TheWatch.Dsl.WordNet.Taxonomy
/// </summary>

using System;
using System.Collections.Generic;

namespace TheWatch.Dsl.WordNet.Taxonomy;

/// <summary>
/// Authoritative NAICS (North American Industry Classification System) 6-digit classification.
/// </summary>
public sealed record NaicsClassification(
    string Code,
    string Title,
    string Sector,
    bool IsCriticalInfrastructure = true,
    double CriticalityWeight = 1.0);

/// <summary>
/// Authoritative NAPCS (North American Product Classification System) 7-digit product/service classification.
/// </summary>
public sealed record NapcsClassification(
    string Code,
    string Title,
    string ServiceLine,
    string DemandCategory = "EmergencyEssential");

/// <summary>
/// Enforces multi-code NAICS and NAPCS categorization on all domain data models and WordNet entities.
/// </summary>
public interface INaicsNapcsCategorizable
{
    IReadOnlyList<string> NaicsCodes { get; }
    IReadOnlyList<string> NapcsCodes { get; }
    IReadOnlyList<NaicsClassification> NaicsClassifications { get; }
    IReadOnlyList<NapcsClassification> NapcsClassifications { get; }
}
