// <copyright file="ExtendedDslRuleValidator.cs" company="The Watch, LLC">
// Copyright (c) 2026 Barton Milnor Mallory, The Watch, LLC. All rights reserved.
// </copyright>

/// <summary>
/// Source file: src/Libraries/TheWatch.Dsl/ExtendedDslRuleValidator.cs
/// Module: Geospatial & Situational Inferencing DSL Semantic Validator
/// Defines: Semantic type-checking, cross-referencing, and integrity validation for AST nodes.
/// Namespace: TheWatch.Dsl
/// </summary>
using System;
using System.Collections.Generic;
using System.Linq;
using TheWatch.Contracts;
using static TheWatch.Contracts.GeospatialSituationalContracts;

namespace TheWatch.Dsl;

public sealed record DslValidationIssue(
    string Severity, // "ERROR", "WARNING", "INFO"
    string Code,
    string Message,
    string? TargetIdentifier = null
);

public sealed record DslValidationReport(
    bool IsValid,
    IReadOnlyList<DslValidationIssue> Issues,
    int TotalNodesValidated,
    int TotalRelationshipsValidated,
    int TotalRulesValidated
);

public sealed class ExtendedDslRuleValidator
{
    private static readonly HashSet<string> ValidLccClasses = new(StringComparer.OrdinalIgnoreCase)
    {
        "HV", "QH", "QR", "RA", "RC", "S", "T", "TA", "TH", "TL", "UG", "QC", "QE", "UA", "UB", "UC"
    };

    private static readonly HashSet<string> ValidSubstanceClasses = new(StringComparer.OrdinalIgnoreCase)
    {
        "pharmaceutical", "household_chemical", "householdchemical", "industrial_chemical", "industrialchemical",
        "biological", "radiological", "venom", "pesticide", "corrosive", "nerve_agent", "nerveagent"
    };

    private static readonly HashSet<string> ValidExposureRoutes = new(StringComparer.OrdinalIgnoreCase)
    {
        "ingestion", "inhalation", "dermal", "skin", "ocular", "eye", "injection"
    };

    public DslValidationReport ValidateScript(ExtendedDslScriptNode script)
    {
        var issues = new List<DslValidationIssue>();
        var declaredNodeIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        // 1. Validate Node Declarations
        foreach (var node in script.NodeDeclarations)
        {
            if (string.IsNullOrWhiteSpace(node.Identifier))
            {
                issues.Add(new DslValidationIssue("ERROR", "NODE_EMPTY_ID", "Node declaration missing required identifier.", node.Identifier));
            }
            else if (!declaredNodeIds.Add(node.Identifier))
            {
                issues.Add(new DslValidationIssue("ERROR", "NODE_DUPLICATE_ID", $"Duplicate node identifier '{node.Identifier}'.", node.Identifier));
            }

            ValidateNodeSpecifics(node, issues);
        }

        // 2. Validate Relationship Declarations
        foreach (var rel in script.RelationshipDeclarations)
        {
            ValidateRelationshipSpecifics(rel, declaredNodeIds, issues);
        }

        // 3. Validate Rules
        foreach (var rule in script.Rules)
        {
            if (string.IsNullOrWhiteSpace(rule.RuleName))
            {
                issues.Add(new DslValidationIssue("ERROR", "RULE_EMPTY_NAME", "Rule declaration missing required name."));
            }

            if (rule.Actions.Count == 0)
            {
                issues.Add(new DslValidationIssue("WARNING", "RULE_NO_ACTIONS", $"Rule '{rule.RuleName}' has no THEN action statements.", rule.RuleName));
            }

            if (rule.Condition != null)
            {
                ValidateConditionNode(rule.Condition, rule.RuleName, issues);
            }

            foreach (var action in rule.Actions)
            {
                ValidateActionStatement(action, rule.RuleName, issues);
            }
        }

        bool hasErrors = issues.Any(i => i.Severity.Equals("ERROR", StringComparison.OrdinalIgnoreCase));

        return new DslValidationReport(
            IsValid: !hasErrors,
            Issues: issues,
            TotalNodesValidated: script.NodeDeclarations.Count,
            TotalRelationshipsValidated: script.RelationshipDeclarations.Count,
            TotalRulesValidated: script.Rules.Count
        );
    }

    private static void ValidateNodeSpecifics(AstNodeDeclaration node, List<DslValidationIssue> issues)
    {
        switch (node)
        {
            case GeoCountyNodeDeclaration county:
                if (!county.Properties.ContainsKey("stateFips") && !county.Properties.ContainsKey("countyFips"))
                {
                    issues.Add(new DslValidationIssue("INFO", "GEO_COUNTY_PROPS", $"GeoCounty '{county.Identifier}' missing recommended stateFips or countyFips properties.", county.Identifier));
                }
                break;

            case OccupationNodeDeclaration occ:
                if (!occ.Properties.ContainsKey("socCode") && !occ.Identifier.Contains("-"))
                {
                    issues.Add(new DslValidationIssue("INFO", "ONET_SOC_PROPS", $"Occupation '{occ.Identifier}' recommended to specify SOC code.", occ.Identifier));
                }
                break;

            case NaicsSectorNodeDeclaration naics:
                if (!naics.Properties.ContainsKey("sectorCode"))
                {
                    issues.Add(new DslValidationIssue("INFO", "NAICS_CODE_PROPS", $"NAICSSector '{naics.Identifier}' missing sectorCode property.", naics.Identifier));
                }
                break;

            case NapcsProductNodeDeclaration napcs:
                if (!napcs.Properties.ContainsKey("napcsCode"))
                {
                    issues.Add(new DslValidationIssue("INFO", "NAPCS_CODE_PROPS", $"NAPCSProduct '{napcs.Identifier}' missing napcsCode property.", napcs.Identifier));
                }
                break;
        }
    }

    private static void ValidateRelationshipSpecifics(AstRelationshipDeclaration rel, HashSet<string> declaredNodes, List<DslValidationIssue> issues)
    {
        if (declaredNodes.Count > 0)
        {
            if (!declaredNodes.Contains(rel.SourceNodeId))
            {
                issues.Add(new DslValidationIssue("INFO", "REL_UNBOUND_SOURCE", $"Relationship references source '{rel.SourceNodeId}' not declared in script.", rel.SourceNodeId));
            }
            if (!declaredNodes.Contains(rel.TargetNodeId))
            {
                issues.Add(new DslValidationIssue("INFO", "REL_UNBOUND_TARGET", $"Relationship references target '{rel.TargetNodeId}' not declared in script.", rel.TargetNodeId));
            }
        }
    }

    private static void ValidateConditionNode(AstConditionNode condition, string ruleName, List<DslValidationIssue> issues)
    {
        switch (condition)
        {
            case AstLogicalCondition logical:
                ValidateConditionNode(logical.Left, ruleName, issues);
                ValidateConditionNode(logical.Right, ruleName, issues);
                break;

            case AstInLccClassCondition lcc:
                string prefix = lcc.LccClassPattern.Split(new[] { ' ', '.', '-' })[0].Trim();
                if (!string.IsNullOrEmpty(prefix) && !ValidLccClasses.Any(c => prefix.StartsWith(c, StringComparison.OrdinalIgnoreCase)))
                {
                    issues.Add(new DslValidationIssue("WARNING", "LCC_CLASS_UNKNOWN", $"LCC class pattern '{lcc.LccClassPattern}' is outside standard emergency subclasses in rule '{ruleName}'.", ruleName));
                }
                break;

            case AstSubstanceClassIsCondition sub:
                string cleaned = sub.SubstanceClass.Trim('"', '\'').ToLowerInvariant();
                if (!ValidSubstanceClasses.Contains(cleaned))
                {
                    issues.Add(new DslValidationIssue("WARNING", "SUBSTANCE_CLASS_UNKNOWN", $"Substance class '{sub.SubstanceClass}' in rule '{ruleName}' is not recognized.", ruleName));
                }
                break;

            case AstExposureRouteIsCondition route:
                string cleanedRoute = route.ExposureRoute.Trim('"', '\'').ToLowerInvariant();
                if (!ValidExposureRoutes.Contains(cleanedRoute))
                {
                    issues.Add(new DslValidationIssue("WARNING", "EXPOSURE_ROUTE_UNKNOWN", $"Exposure route '{route.ExposureRoute}' in rule '{ruleName}' is not recognized.", ruleName));
                }
                break;

            case AstAntidoteWithinRadiusCondition rad:
                if (rad.RadiusKm <= 0)
                {
                    issues.Add(new DslValidationIssue("ERROR", "INVALID_RADIUS", $"Antidote radius must be positive in rule '{ruleName}'. Got {rad.RadiusKm}km.", ruleName));
                }
                break;
        }
    }

    private static void ValidateActionStatement(AstActionStatement action, string ruleName, List<DslValidationIssue> issues)
    {
        switch (action)
        {
            case AstEstablishIsolationZoneAction iso:
                if (!iso.Parameters.ContainsKey("ERGGuide") && !iso.Parameters.ContainsKey("DistanceMeters") && !iso.Parameters.ContainsKey("Param0"))
                {
                    issues.Add(new DslValidationIssue("WARNING", "ISO_PARAM_MISSING", $"ESTABLISH_ISOLATION_ZONE in rule '{ruleName}' recommended to specify ERGGuide or Distance.", ruleName));
                }
                break;

            case AstLocateAntidoteAction ant:
                if (!ant.Parameters.ContainsKey("Radius") && !ant.Parameters.ContainsKey("SubstanceId") && !ant.Parameters.ContainsKey("Param0"))
                {
                    issues.Add(new DslValidationIssue("WARNING", "ANTIDOTE_PARAM_MISSING", $"LOCATE_ANTIDOTE in rule '{ruleName}' recommended to specify Radius or SubstanceId.", ruleName));
                }
                break;
        }
    }
}
