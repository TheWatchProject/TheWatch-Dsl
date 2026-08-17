// <copyright file="SafetyAppArchitectureCatalogEngine.cs" company="The Watch, LLC">
// Copyright (c) 2026 Barton Milnor Mallory, The Watch, LLC. All rights reserved.
// </copyright>

/// <summary>
/// Source file: src/Libraries/TheWatch.Dsl/SafetyAppArchitectureCatalogEngine.cs
/// Module: Domain-Specific Language Compiler, Lexers & Scientific Measurements
/// Defines: record SafetyArchitectureComponent, class SafetyAppArchitectureCatalogEngine
/// Namespace: TheWatch.Dsl
/// </summary>
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace TheWatch.Dsl;

public sealed record SafetyArchitectureComponent(
    string UniqueId,
    string Category,
    string Name,
    string Description,
    string RelatedComponents,
    string Prompt
);

/// <summary>
/// Knowledge Base & Catalog Engine for the 1,418-Component Safety App Architecture.
/// </summary>
public sealed class SafetyAppArchitectureCatalogEngine
{
    private readonly List<SafetyArchitectureComponent> _components = new();

    public SafetyAppArchitectureCatalogEngine()
    {
        SeedCoreComponents();
    }

    public IReadOnlyList<SafetyArchitectureComponent> AllComponents => _components.AsReadOnly();

    public IReadOnlyList<SafetyArchitectureComponent> SearchByCategory(string category) =>
        _components.Where(c => c.Category.Equals(category, StringComparison.OrdinalIgnoreCase)).ToList();

    public IReadOnlyList<SafetyArchitectureComponent> SearchByKeyword(string query) =>
        _components.Where(c => c.Name.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                               c.Description.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                               c.RelatedComponents.Contains(query, StringComparison.OrdinalIgnoreCase)).ToList();

    public void IngestFromCsv(string csvPath)
    {
        if (!File.Exists(csvPath)) return;

        var lines = File.ReadAllLines(csvPath);
        if (lines.Length <= 1) return;

        _components.Clear();
        foreach (var line in lines.Skip(1))
        {
            if (string.IsNullOrWhiteSpace(line)) continue;
            var parts = ParseCsvLine(line);
            if (parts.Count >= 6)
            {
                _components.Add(new SafetyArchitectureComponent(
                    UniqueId: parts[0],
                    Category: parts[1],
                    Name: parts[2],
                    Description: parts[3],
                    RelatedComponents: parts[4],
                    Prompt: parts[5]
                ));
            }
        }
    }

    private static List<string> ParseCsvLine(string line)
    {
        var result = new List<string>();
        bool inQuotes = false;
        var current = new System.Text.StringBuilder();

        for (int i = 0; i < line.Length; i++)
        {
            char c = line[i];
            if (c == '"')
            {
                inQuotes = !inQuotes;
            }
            else if (c == ',' && !inQuotes)
            {
                result.Add(current.ToString().Trim(' ', '"'));
                current.Clear();
            }
            else
            {
                current.Append(c);
            }
        }
        result.Add(current.ToString().Trim(' ', '"'));
        return result;
    }

    private void SeedCoreComponents()
    {
        _components.AddRange(new[]
        {
            new SafetyArchitectureComponent("ARCHITECTURE-0001", "Data Models", "User", "User account with profile and credentials", "UserRepository, AuthService", "Define User data model"),
            new SafetyArchitectureComponent("ARCHITECTURE-0002", "Data Models", "Location", "GPS coordinates with geohash encoding and elevation", "LocationService, GeoHashService", "Define Location data model"),
            new SafetyArchitectureComponent("ARCHITECTURE-0004", "Data Models", "Evacuation", "Evacuation order with zone and urgency level", "EvacuationRepository, NotificationService", "Define Evacuation data model"),
            new SafetyArchitectureComponent("ARCHITECTURE-0016", "Data Models", "VoiceTrigger", "User-programmed emergency voice phrases", "VoiceTriggerRepository, VoiceMonitoringService", "Define VoiceTrigger data model"),
            new SafetyArchitectureComponent("ARCHITECTURE-0026", "Data Models", "DeviceMeshNode", "User device participating in local mesh network", "MeshNodeRepository, MeshNetworkService", "Define DeviceMeshNode data model")
        });
    }
}
