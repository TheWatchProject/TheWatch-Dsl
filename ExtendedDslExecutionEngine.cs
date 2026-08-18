// <copyright file="ExtendedDslExecutionEngine.cs" company="The Watch, LLC">
// Copyright (c) 2026 Barton Milnor Mallory, The Watch, LLC. All rights reserved.
// </copyright>

/// <summary>
/// Source file: src/Libraries/TheWatch.Dsl/ExtendedDslExecutionEngine.cs
/// Module: Geospatial & Situational Inferencing DSL Runtime Evaluation Engine
/// Defines: Deterministic AST execution against IExtendedDslServiceBridge.
/// Namespace: TheWatch.Dsl
/// </summary>
using System;
using System.Collections.Generic;
using TheWatch.Contracts;
using static TheWatch.Contracts.GeospatialSituationalContracts;

namespace TheWatch.Dsl;

public sealed record ExtendedDslRuleResult(
    string RuleName,
    bool Matched,
    IReadOnlyList<string> ExecutedActions,
    DateTime EvaluatedAtUtc
);

public sealed record ExtendedDslScriptResult(
    IReadOnlyList<ExtendedDslRuleResult> RuleResults,
    IReadOnlyList<SituationalInferenceSynthesis> QuerySyntheses,
    DateTime CompletedAtUtc
);

public sealed class ExtendedDslExecutionEngine
{
    private readonly IExtendedDslServiceBridge _bridge;

    public ExtendedDslExecutionEngine(IExtendedDslServiceBridge bridge)
    {
        _bridge = bridge ?? throw new ArgumentNullException(nameof(bridge));
    }

    public ExtendedDslScriptResult ExecuteScript(ExtendedDslScriptNode script, IncidentEvaluationContext context)
    {
        var ruleResults = new List<ExtendedDslRuleResult>();
        foreach (var rule in script.Rules)
        {
            var res = ExecuteRule(rule, context);
            ruleResults.Add(res);
        }

        var querySyntheses = new List<SituationalInferenceSynthesis>();
        foreach (var query in script.Queries)
        {
            var synth = _bridge.SynthesizeSituationalInference(query);
            querySyntheses.Add(synth);
        }

        return new ExtendedDslScriptResult(ruleResults, querySyntheses, DateTime.UtcNow);
    }

    public ExtendedDslRuleResult ExecuteRule(AstRuleDeclaration rule, IncidentEvaluationContext context)
    {
        bool matched = rule.Condition == null || EvaluateCondition(rule.Condition, context);
        var actionOutputs = new List<string>();

        if (matched)
        {
            foreach (var action in rule.Actions)
            {
                string output = ExecuteAction(action, context);
                actionOutputs.Add(output);
            }
        }

        return new ExtendedDslRuleResult(rule.RuleName, matched, actionOutputs, DateTime.UtcNow);
    }

    public bool EvaluateCondition(AstConditionNode condition, IncidentEvaluationContext context)
    {
        return condition switch
        {
            AstLogicalCondition logical => logical.LogicalOp switch
            {
                "AND" => EvaluateCondition(logical.Left, context) && EvaluateCondition(logical.Right, context),
                "OR" => EvaluateCondition(logical.Left, context) || EvaluateCondition(logical.Right, context),
                _ => true
            },

            AstInLccClassCondition lcc => _bridge.EvaluateLccClass(context.EventType, lcc.LccClassPattern),
            AstAffectsNaicsCondition naics => _bridge.EvaluateAffectsNaics(context, naics.NaicsCodePattern),
            AstRequiresOccupationCondition occ => _bridge.EvaluateRequiresOccupation(context, occ.SocCodePattern),
            AstRequiresProductCondition prod => _bridge.EvaluateRequiresProduct(context, prod.NapcsCodePattern),
            AstWithinGeoCondition geo => _bridge.EvaluateWithinGeo(context, geo.GeoIdOrName),
            AstAdjacentRiskCondition adj => _bridge.EvaluateAdjacentRisk(context, adj.TargetAreaOrThreshold),
            AstPhaseIsCondition phase => _bridge.EvaluatePhase(context, phase.TargetPhase),
            AstCosoSeverityCondition coso => _bridge.EvaluateCosoSeverity(context, coso.Operator, coso.SeverityThreshold),

            AstSubstanceClassIsCondition sub => _bridge.EvaluateSubstanceClass(context, sub.SubstanceClass),
            AstExposureRouteIsCondition route => _bridge.EvaluateExposureRoute(context, route.ExposureRoute),
            AstAntidoteAvailableCondition ant => _bridge.EvaluateAntidoteAvailable(context, ant.SubstanceOrAntidoteId),
            AstAntidoteWithinRadiusCondition rad => _bridge.EvaluateAntidoteWithinRadius(context, rad.RadiusKm),
            AstDecontaminationRequiredCondition deCon => _bridge.EvaluateDecontaminationRequired(context) == deCon.Required,

            AstBinaryComparisonCondition binary => EvaluateBinaryComparison(binary, context),

            _ => true
        };
    }

    private static bool EvaluateBinaryComparison(AstBinaryComparisonCondition binary, IncidentEvaluationContext context)
    {
        string leftVal = ResolveOperandValue(binary.LeftOperand, context);
        string rightVal = binary.RightOperand.Trim('"', '\'');

        return binary.Operator switch
        {
            "==" => string.Equals(leftVal, rightVal, StringComparison.OrdinalIgnoreCase),
            "!=" => !string.Equals(leftVal, rightVal, StringComparison.OrdinalIgnoreCase),
            "<" => double.TryParse(leftVal, out var l1) && double.TryParse(rightVal, out var r1) && l1 < r1,
            "<=" => double.TryParse(leftVal, out var l2) && double.TryParse(rightVal, out var r2) && l2 <= r2,
            ">" => double.TryParse(leftVal, out var l3) && double.TryParse(rightVal, out var r3) && l3 > r3,
            ">=" => double.TryParse(leftVal, out var l4) && double.TryParse(rightVal, out var r4) && l4 >= r4,
            _ => true
        };
    }

    private static string ResolveOperandValue(string operand, IncidentEvaluationContext context)
    {
        var op = operand.Trim().ToLowerInvariant();
        return op switch
        {
            "event.type" => context.EventType,
            "event.severity" => context.Severity,
            "event.status" => context.Status,
            "event.substanceid" => context.SubstanceId ?? string.Empty,
            "event.substanceclass" => context.SubstanceClass ?? string.Empty,
            "event.exposureroute" => context.ExposureRoute ?? string.Empty,
            "event.ergguide" or "event.ergguidenumber" => context.ErgGuideNumber?.ToString() ?? "0",
            _ => operand.Trim('"', '\'')
        };
    }

    private string ExecuteAction(AstActionStatement action, IncidentEvaluationContext context)
    {
        return action switch
        {
            AstClassifyEventAction a => _bridge.ExecuteClassifyEvent(context, a.Parameters),
            AstMapResourcesAction a => _bridge.ExecuteMapResources(context, a.Parameters),
            AstCascadeAlertAction a => _bridge.ExecuteCascadeAlert(context, a.Parameters),
            AstActivateProtocolAction a => _bridge.ExecuteActivateProtocol(context, a.Parameters),
            AstProjectTimelineAction a => _bridge.ExecuteProjectTimeline(context, a.Parameters),
            AstAssessImpactAction a => _bridge.ExecuteAssessImpact(context, a.Parameters),

            AstActivatePoisonProtocolAction a => _bridge.ExecuteActivatePoisonProtocol(context, a.Parameters),
            AstLocateAntidoteAction a => _bridge.ExecuteLocateAntidote(context, a.Parameters),
            AstEstablishIsolationZoneAction a => _bridge.ExecuteEstablishIsolationZone(context, a.Parameters),
            AstNotifyPoisonControlAction a => _bridge.ExecuteNotifyPoisonControl(context, a.Parameters),
            AstDispatchHazmatAction a => _bridge.ExecuteDispatchHazmat(context, a.Parameters),

            AstDispatchAction a => _bridge.ExecuteDispatch(context, a.Parameters),
            AstNotifyAction a => _bridge.ExecuteNotify(context, a.Parameters),
            _ => _bridge.ExecuteGenericAction(context, action.ActionName, action.Parameters)
        };
    }
}
