// <copyright file="DecisionTreeEngine.cs" company="The Watch, LLC">
// Copyright (c) 2026 Barton Milnor Mallory, The Watch, LLC. All rights reserved.
// </copyright>

/// <summary>
/// Source file: src/Libraries/TheWatch.Dsl/DecisionTreeEngine.cs
/// Module: Domain-Specific Language Compiler, Lexers & Scientific Measurements
/// Defines: record DecisionNode, record DecisionConditionNode, record DecisionOutcomeNode
/// Namespace: TheWatch.Dsl
/// </summary>
using System;
using System.Collections.Generic;
using TheWatch.Domain.Entities;

namespace TheWatch.Dsl;

/// <summary>
/// Node structure for hierarchical Decision Trees in emergency dispatching.
/// </summary>
public abstract record DecisionNode;

public sealed record DecisionConditionNode(
    string Field,
    string Operator,
    double Threshold,
    DecisionNode TrueBranch,
    DecisionNode FalseBranch
) : DecisionNode;

public sealed record DecisionOutcomeNode(
    string OutcomeTag,
    int RecommendedPriority,
    IReadOnlyList<string> RequiredResponderRoles,
    double ConfidenceScore
) : DecisionNode;

/// <summary>
/// Decision Tree Evaluation Engine for triage, threat escalation, and tactical dispatching.
/// </summary>
public sealed class DecisionTreeEngine
{
    private readonly Dictionary<string, DecisionNode> _treeRegistry = new();

    public DecisionTreeEngine()
    {
        RegisterStandardTriageTree();
        RegisterWildfireEvacuationTree();
    }

    public void RegisterTree(string name, DecisionNode root)
    {
        _treeRegistry[name] = root;
    }

    public DecisionOutcomeNode Evaluate(string treeName, IReadOnlyDictionary<string, double> numericFeatures)
    {
        if (!_treeRegistry.TryGetValue(treeName, out var root))
        {
            throw new ArgumentException($"Decision tree '{treeName}' not found in registry.");
        }

        return Traverse(root, numericFeatures);
    }

    private DecisionOutcomeNode Traverse(DecisionNode current, IReadOnlyDictionary<string, double> features)
    {
        if (current is DecisionOutcomeNode outcome)
        {
            return outcome;
        }

        if (current is DecisionConditionNode cond)
        {
            double value = features.TryGetValue(cond.Field, out var v) ? v : 0.0;
            bool conditionMet = cond.Operator switch
            {
                ">" => value > cond.Threshold,
                ">=" => value >= cond.Threshold,
                "<" => value < cond.Threshold,
                "<=" => value <= cond.Threshold,
                "==" => Math.Abs(value - cond.Threshold) < 0.0001,
                "!=" => Math.Abs(value - cond.Threshold) >= 0.0001,
                _ => false
            };

            return conditionMet ? Traverse(cond.TrueBranch, features) : Traverse(cond.FalseBranch, features);
        }

        throw new InvalidOperationException("Unknown decision tree node type.");
    }

    private void RegisterStandardTriageTree()
    {
        // START Triage Decision Tree
        // If RespirationRate > 30 -> IMMEDIATE (Red)
        // Else If Pulse > 120 -> IMMEDIATE (Red)
        // Else If GCS < 13 -> IMMEDIATE (Red)
        // Else -> DELAYED (Yellow / Green)
        var root = new DecisionConditionNode(
            Field: "RespirationRate",
            Operator: ">",
            Threshold: 30,
            TrueBranch: new DecisionOutcomeNode("TRIAGE_RED_IMMEDIATE", 1, new[] { "PARAMEDIC", "TRAUMA_SURGEON" }, 0.98),
            FalseBranch: new DecisionConditionNode(
                Field: "Pulse",
                Operator: ">",
                Threshold: 120,
                TrueBranch: new DecisionOutcomeNode("TRIAGE_RED_IMMEDIATE", 1, new[] { "PARAMEDIC" }, 0.95),
                FalseBranch: new DecisionConditionNode(
                    Field: "GlasgowComaScale",
                    Operator: "<",
                    Threshold: 13,
                    TrueBranch: new DecisionOutcomeNode("TRIAGE_RED_IMMEDIATE", 1, new[] { "PARAMEDIC", "NEURO_SPECIALIST" }, 0.92),
                    FalseBranch: new DecisionOutcomeNode("TRIAGE_YELLOW_DELAYED", 2, new[] { "EMT_BASIC" }, 0.88)
                )
            )
        );

        _treeRegistry["START_TRIAGE"] = root;
    }

    private void RegisterWildfireEvacuationTree()
    {
        // Wildfire Evacuation Perimeter Decision Tree
        // If DistanceToFireKm < 2.0 -> MANDATORY_EVACUATION_LEVEL_3 (Go Now)
        // Else If WindSpeedMph > 25.0 -> WARNING_LEVEL_2 (Be Set)
        // Else -> ADVISORY_LEVEL_1 (Be Ready)
        var root = new DecisionConditionNode(
            Field: "DistanceToFireKm",
            Operator: "<",
            Threshold: 2.0,
            TrueBranch: new DecisionOutcomeNode("EVACUATION_LEVEL_3_GO_NOW", 1, new[] { "POLICE", "FIREFIGHTER", "DRONE_OPERATOR" }, 0.99),
            FalseBranch: new DecisionConditionNode(
                Field: "WindSpeedMph",
                Operator: ">",
                Threshold: 25.0,
                TrueBranch: new DecisionOutcomeNode("EVACUATION_LEVEL_2_BE_SET", 2, new[] { "POLICE", "DRONE_OPERATOR" }, 0.90),
                FalseBranch: new DecisionOutcomeNode("EVACUATION_LEVEL_1_BE_READY", 3, new[] { "POLICE" }, 0.85)
            )
        );

        _treeRegistry["WILDFIRE_EVACUATION"] = root;
    }
}
