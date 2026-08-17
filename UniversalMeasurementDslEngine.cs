// <copyright file="UniversalMeasurementDslEngine.cs" company="The Watch, LLC">
// Copyright (c) 2026 Barton Milnor Mallory, The Watch, LLC. All rights reserved.
// </copyright>

/// <summary>
/// Source file: src/Libraries/TheWatch.Dsl/UniversalMeasurementDslEngine.cs
/// Module: Domain-Specific Language Compiler, Lexers & Scientific Measurements
/// Defines: record ParsedMeasurement, class UniversalMeasurementDslEngine
/// Namespace: TheWatch.Dsl
/// </summary>
using System;
using System.Globalization;
using System.Text.RegularExpressions;
using TheWatch.Core.Taxonomy;

namespace TheWatch.Dsl;

/// <summary>
/// Parsed scientific measurement token.
/// </summary>
public sealed record ParsedMeasurement
{
    public required double Value { get; init; }
    public required string UnitSymbol { get; init; }
    public double ValueInSi { get; init; }
    public string Domain { get; init; } = "General";
    public string SafetySeverity { get; init; } = "Nominal";
}

/// <summary>
/// Universal Scientific, Biomedical &amp; Sensor Measurement DSL Execution Engine.
/// </summary>
public static class UniversalMeasurementDslEngine
{
    private static readonly Regex MeasurementPattern = new(
        @"^\s*(?<value>[-+]?[0-9]*\.?[0-9]+([eE][-+]?[0-9]+)?)\s*(?<unit>[A-Za-z°%]+(\s+[A-Za-z]+)?)\s*$",
        RegexOptions.Compiled);

    /// <summary>
    /// Parses a measurement expression string into structured telemetry with SI standardization and safety classification.
    /// </summary>
    public static ParsedMeasurement ParseMeasurement(string expression)
    {
        ArgumentNullException.ThrowIfNull(expression);

        var match = MeasurementPattern.Match(expression);
        if (!match.Success)
        {
            throw new FormatException($"Invalid measurement syntax: '{expression}'. Expected '<magnitude> <unit_symbol>' (e.g., '145 BPM', '120 mmHg', '500 ft').");
        }

        double val = double.Parse(match.Groups["value"].Value, CultureInfo.InvariantCulture);
        string symbol = match.Groups["unit"].Value.Trim();

        var unitDef = UniversalMeasurementTaxonomyCatalog.FindBySymbol(symbol);
        double siValue = unitDef is not null ? val * unitDef.ConversionFactorToSI : val;
        string domain = unitDef?.Domain ?? "General";

        string severity = EvaluateSafetyThreshold(domain, symbol, val);

        return new ParsedMeasurement
        {
            Value = val,
            UnitSymbol = symbol,
            ValueInSi = siValue,
            Domain = domain,
            SafetySeverity = severity
        };
    }

    /// <summary>
    /// Evaluates clinical and physical safety bounds across domains.
    /// </summary>
    public static string EvaluateSafetyThreshold(string domain, string symbol, double value)
    {
        // Clinical Vitals (Biology)
        if (string.Equals(symbol, "BPM", StringComparison.OrdinalIgnoreCase))
        {
            if (value < 40 || value > 140) return "Critical";
            if (value < 50 || value > 110) return "Warning";
            return "Nominal";
        }
        if (string.Equals(symbol, "SpO2%", StringComparison.OrdinalIgnoreCase))
        {
            if (value < 90) return "Critical";
            if (value < 94) return "Warning";
            return "Nominal";
        }
        if (string.Equals(symbol, "°C", StringComparison.OrdinalIgnoreCase))
        {
            if (value < 35.0 || value > 40.0) return "Critical";
            if (value < 36.0 || value > 38.5) return "Warning";
            return "Nominal";
        }

        // Ionizing Radiation & Toxins (Chemistry)
        if (string.Equals(symbol, "Sv", StringComparison.OrdinalIgnoreCase))
        {
            if (value >= 1.0) return "Critical";
            if (value >= 0.05) return "Warning";
            return "Nominal";
        }

        // Acoustic Hazard (Audio)
        if (string.Equals(symbol, "dB SPL", StringComparison.OrdinalIgnoreCase))
        {
            if (value >= 130) return "Critical";
            if (value >= 85) return "Warning";
            return "Nominal";
        }

        return "Nominal";
    }
}
