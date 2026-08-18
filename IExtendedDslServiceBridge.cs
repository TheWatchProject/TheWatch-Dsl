// <copyright file="IExtendedDslServiceBridge.cs" company="The Watch, LLC">
// Copyright (c) 2026 Barton Milnor Mallory, The Watch, LLC. All rights reserved.
// </copyright>

/// <summary>
/// Source file: src/Libraries/TheWatch.Dsl/IExtendedDslServiceBridge.cs
/// Module: Geospatial & Situational Inferencing DSL Service Bridge
/// Defines: Runtime service bridge interface connecting DSL AST conditions and actions to underlying providers.
/// Namespace: TheWatch.Dsl
/// </summary>
using System;
using System.Collections.Generic;
using TheWatch.Contracts;
using TheWatch.Geospatial.Db;
using static TheWatch.Contracts.GeospatialSituationalContracts;

namespace TheWatch.Dsl;

public sealed record IncidentEvaluationContext(
    string IncidentId,
    string EventType,
    string Severity,
    string Status,
    double Latitude,
    double Longitude,
    EventLifeCyclePhase CurrentPhase,
    string? SubstanceId = null,
    string? SubstanceClass = null,
    string? ExposureRoute = null,
    int? ErgGuideNumber = null,
    Dictionary<string, string>? Metadata = null
);

public interface IExtendedDslServiceBridge
{
    ISituationalInferencingEngine SituationalEngine { get; }
    IPoisoningAndMedicalInferencingEngine PoisonEngine { get; }
    ICosoErmRiskEngine CosoEngine { get; }
    IInternationalClassificationEngine IntlEngine { get; }

    // Condition Evaluators
    bool EvaluateLccClass(string eventType, string lccPattern);
    bool EvaluateAffectsNaics(IncidentEvaluationContext context, string naicsPattern);
    bool EvaluateRequiresOccupation(IncidentEvaluationContext context, string socPattern);
    bool EvaluateRequiresProduct(IncidentEvaluationContext context, string napcsPattern);
    bool EvaluateWithinGeo(IncidentEvaluationContext context, string geoTarget);
    bool EvaluateAdjacentRisk(IncidentEvaluationContext context, string targetAreaOrThreshold);
    bool EvaluatePhase(IncidentEvaluationContext context, EventLifeCyclePhase targetPhase);
    bool EvaluateCosoSeverity(IncidentEvaluationContext context, string op, string threshold);
    bool EvaluateSubstanceClass(IncidentEvaluationContext context, string substanceClass);
    bool EvaluateExposureRoute(IncidentEvaluationContext context, string route);
    bool EvaluateAntidoteAvailable(IncidentEvaluationContext context, string? substanceOrAntidoteId);
    bool EvaluateAntidoteWithinRadius(IncidentEvaluationContext context, double radiusKm);
    bool EvaluateDecontaminationRequired(IncidentEvaluationContext context);

    // Action Executors
    string ExecuteClassifyEvent(IncidentEvaluationContext context, IReadOnlyDictionary<string, string> parameters);
    string ExecuteMapResources(IncidentEvaluationContext context, IReadOnlyDictionary<string, string> parameters);
    string ExecuteCascadeAlert(IncidentEvaluationContext context, IReadOnlyDictionary<string, string> parameters);
    string ExecuteActivateProtocol(IncidentEvaluationContext context, IReadOnlyDictionary<string, string> parameters);
    string ExecuteProjectTimeline(IncidentEvaluationContext context, IReadOnlyDictionary<string, string> parameters);
    string ExecuteAssessImpact(IncidentEvaluationContext context, IReadOnlyDictionary<string, string> parameters);

    string ExecuteActivatePoisonProtocol(IncidentEvaluationContext context, IReadOnlyDictionary<string, string> parameters);
    string ExecuteLocateAntidote(IncidentEvaluationContext context, IReadOnlyDictionary<string, string> parameters);
    string ExecuteEstablishIsolationZone(IncidentEvaluationContext context, IReadOnlyDictionary<string, string> parameters);
    string ExecuteNotifyPoisonControl(IncidentEvaluationContext context, IReadOnlyDictionary<string, string> parameters);
    string ExecuteDispatchHazmat(IncidentEvaluationContext context, IReadOnlyDictionary<string, string> parameters);

    string ExecuteDispatch(IncidentEvaluationContext context, IReadOnlyDictionary<string, string> parameters);
    string ExecuteNotify(IncidentEvaluationContext context, IReadOnlyDictionary<string, string> parameters);
    string ExecuteGenericAction(IncidentEvaluationContext context, string actionName, IReadOnlyDictionary<string, string> parameters);

    // 4-Questions Situational Inferencing Query
    SituationalInferenceSynthesis SynthesizeSituationalInference(AstSituationalQueryDeclaration query);
}
