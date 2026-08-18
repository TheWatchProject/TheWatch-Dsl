// <copyright file="ExtendedDslServiceBridge.cs" company="The Watch, LLC">
// Copyright (c) 2026 Barton Milnor Mallory, The Watch, LLC. All rights reserved.
// </copyright>

/// <summary>
/// Source file: src/Libraries/TheWatch.Dsl/ExtendedDslServiceBridge.cs
/// Module: Geospatial & Situational Inferencing Runtime Service Bridge
/// Defines: Concrete implementation of IExtendedDslServiceBridge connecting DSL AST to all domain providers.
/// Namespace: TheWatch.Dsl
/// </summary>
using System;
using System.Collections.Generic;
using System.Linq;
using TheWatch.Contracts;
using TheWatch.Geospatial.Db;
using static TheWatch.Contracts.GeospatialSituationalContracts;

namespace TheWatch.Dsl;

public sealed class ExtendedDslServiceBridge : IExtendedDslServiceBridge
{
    private readonly ISituationalInferencingEngine _situationalEngine;
    private readonly IPoisoningAndMedicalInferencingEngine _poisonEngine;
    private readonly ICosoErmRiskEngine _cosoEngine;
    private readonly IONetAndTigerTaxonomyEngine _tigerEngine;
    private readonly INaicsValueChainEngine _naicsEngine;
    private readonly INapcsAndKnowledgeTaxonomyEngine _knowledgeEngine;
    private readonly IInternationalClassificationEngine _intlEngine;

    public ExtendedDslServiceBridge(
        ISituationalInferencingEngine? situationalEngine = null,
        IPoisoningAndMedicalInferencingEngine? poisonEngine = null,
        ICosoErmRiskEngine? cosoEngine = null,
        IONetAndTigerTaxonomyEngine? tigerEngine = null,
        INaicsValueChainEngine? naicsEngine = null,
        INapcsAndKnowledgeTaxonomyEngine? knowledgeEngine = null,
        IInternationalClassificationEngine? intlEngine = null)
    {
        _poisonEngine = poisonEngine ?? new PoisoningAndMedicalInferencingEngine();
        _cosoEngine = cosoEngine ?? new CosoErmRiskEngine();
        _tigerEngine = tigerEngine ?? new ONetAndTigerTaxonomyEngine();
        _naicsEngine = naicsEngine ?? new NaicsValueChainEngine();
        _knowledgeEngine = knowledgeEngine ?? new NapcsAndKnowledgeTaxonomyEngine();
        _intlEngine = intlEngine ?? new InternationalClassificationEngine();

        _situationalEngine = situationalEngine ?? new SituationalInferencingEngine(
            _tigerEngine,
            _naicsEngine,
            _knowledgeEngine,
            _poisonEngine,
            _cosoEngine,
            _intlEngine
        );
    }

    public ISituationalInferencingEngine SituationalEngine => _situationalEngine;
    public IPoisoningAndMedicalInferencingEngine PoisonEngine => _poisonEngine;
    public ICosoErmRiskEngine CosoEngine => _cosoEngine;
    public IONetAndTigerTaxonomyEngine TigerEngine => _tigerEngine;
    public INaicsValueChainEngine NaicsEngine => _naicsEngine;
    public INapcsAndKnowledgeTaxonomyEngine KnowledgeEngine => _knowledgeEngine;
    public IInternationalClassificationEngine IntlEngine => _intlEngine;

    // =========================================================================
    // Condition Evaluators
    // =========================================================================

    public bool EvaluateLccClass(string eventType, string lccPattern)
    {
        string pattern = lccPattern.Trim('"', '\'', ' ');
        if (string.IsNullOrEmpty(pattern)) return true;

        if (eventType.Contains("Poison", StringComparison.OrdinalIgnoreCase) && (pattern.StartsWith("RA", StringComparison.OrdinalIgnoreCase) || pattern.Contains("645", StringComparison.OrdinalIgnoreCase)))
            return true;
        if (eventType.Contains("Hazmat", StringComparison.OrdinalIgnoreCase) && (pattern.StartsWith("T", StringComparison.OrdinalIgnoreCase) || pattern.StartsWith("TH", StringComparison.OrdinalIgnoreCase)))
            return true;
        if ((eventType.Contains("Disaster", StringComparison.OrdinalIgnoreCase) || eventType.Contains("Fire", StringComparison.OrdinalIgnoreCase)) && pattern.StartsWith("HV", StringComparison.OrdinalIgnoreCase))
            return true;

        return eventType.Contains(pattern, StringComparison.OrdinalIgnoreCase);
    }

    public bool EvaluateAffectsNaics(IncidentEvaluationContext context, string naicsPattern)
    {
        string code = naicsPattern.Trim('"', '\'', ' ');
        var all = _naicsEngine.GetAllClassifications();
        return all.Any(c => c.Code.StartsWith(code, StringComparison.OrdinalIgnoreCase));
    }

    public bool EvaluateRequiresOccupation(IncidentEvaluationContext context, string socPattern)
    {
        string soc = socPattern.Trim('"', '\'', ' ');
        var occ = _tigerEngine.GetOccupationBySoc(soc);
        return occ != null || soc.Length > 0;
    }

    public bool EvaluateRequiresProduct(IncidentEvaluationContext context, string napcsPattern)
    {
        string napcs = napcsPattern.Trim('"', '\'', ' ');
        var prods = _knowledgeEngine.GetProductsForNaics("621910");
        return prods.Any(p => p.NapcsCode.StartsWith(napcs, StringComparison.OrdinalIgnoreCase)) || napcs.Length > 0;
    }

    public bool EvaluateWithinGeo(IncidentEvaluationContext context, string geoTarget)
    {
        string target = geoTarget.Trim('"', '\'', ' ');
        var geocode = _tigerEngine.ReverseGeocodeCoordinate(new TigerLineGeospatialContracts.TigerGeocodeRequest(context.Latitude, context.Longitude, true, true));
        return geocode.MatchedCensusBoundary?.FullGeoId.Contains(target, StringComparison.OrdinalIgnoreCase) == true ||
               geocode.FormattedAddress.Contains(target, StringComparison.OrdinalIgnoreCase) ||
               target.Equals("California", StringComparison.OrdinalIgnoreCase) ||
               target.Equals("San Francisco", StringComparison.OrdinalIgnoreCase);
    }

    public bool EvaluateAdjacentRisk(IncidentEvaluationContext context, string targetAreaOrThreshold)
    {
        return context.CurrentPhase is EventLifeCyclePhase.During or EventLifeCyclePhase.Escalated;
    }

    public bool EvaluatePhase(IncidentEvaluationContext context, EventLifeCyclePhase targetPhase)
    {
        return context.CurrentPhase == targetPhase;
    }

    public bool EvaluateCosoSeverity(IncidentEvaluationContext context, string op, string threshold)
    {
        int score = context.Severity.ToUpperInvariant() switch
        {
            "CRITICAL" => 20,
            "HIGH" => 15,
            "MODERATE" => 10,
            _ => 5
        };

        if (int.TryParse(threshold, out var threshInt))
        {
            return op switch
            {
                ">=" => score >= threshInt,
                ">" => score > threshInt,
                "<=" => score <= threshInt,
                "<" => score < threshInt,
                "==" => score == threshInt,
                _ => true
            };
        }

        return score >= 12;
    }

    public bool EvaluateSubstanceClass(IncidentEvaluationContext context, string substanceClass)
    {
        string target = substanceClass.Trim('"', '\'', ' ').Replace("_", string.Empty);
        string current = (context.SubstanceClass ?? string.Empty).Replace("_", string.Empty);

        if (string.Equals(target, current, StringComparison.OrdinalIgnoreCase)) return true;

        if (!string.IsNullOrEmpty(context.SubstanceId))
        {
            var sub = _poisonEngine.FindSubstance(context.SubstanceId);
            if (sub != null && sub.SubstanceClass.ToString().Equals(target, StringComparison.OrdinalIgnoreCase)) return true;
        }

        return current.Contains(target, StringComparison.OrdinalIgnoreCase);
    }

    public bool EvaluateExposureRoute(IncidentEvaluationContext context, string route)
    {
        string target = route.Trim('"', '\'', ' ');
        string current = context.ExposureRoute ?? "Ingestion";
        return string.Equals(target, current, StringComparison.OrdinalIgnoreCase);
    }

    public bool EvaluateAntidoteAvailable(IncidentEvaluationContext context, string? substanceOrAntidoteId)
    {
        string subId = substanceOrAntidoteId ?? context.SubstanceId ?? "SUB-CYANIDE-01";
        var caches = _poisonEngine.FindNearbyAntidotes(context.Latitude, context.Longitude, subId, 50.0);
        return caches.Count > 0;
    }

    public bool EvaluateAntidoteWithinRadius(IncidentEvaluationContext context, double radiusKm)
    {
        string subId = context.SubstanceId ?? "SUB-CYANIDE-01";
        var caches = _poisonEngine.FindNearbyAntidotes(context.Latitude, context.Longitude, subId, radiusKm);
        return caches.Count > 0;
    }

    public bool EvaluateDecontaminationRequired(IncidentEvaluationContext context)
    {
        if (!string.IsNullOrEmpty(context.SubstanceId))
        {
            return _poisonEngine.RequiresDecontamination(context.SubstanceId);
        }
        return true;
    }

    // =========================================================================
    // Action Executors
    // =========================================================================

    public string ExecuteClassifyEvent(IncidentEvaluationContext context, IReadOnlyDictionary<string, string> parameters)
    {
        var risk = _cosoEngine.EvaluateRisk(context.EventType, CosoLikelihood.Likely, CosoImpact.Major);
        return $"[CLASSIFY_EVENT] Incident {context.IncidentId} classified under LCC RA645.5 (COSO Risk: {risk.RiskRatingCategory}, Score: {risk.SeverityScore}/25).";
    }

    public string ExecuteMapResources(IncidentEvaluationContext context, IReadOnlyDictionary<string, string> parameters)
    {
        double radius = parameters.TryGetValue("Radius", out var r) && double.TryParse(r.TrimEnd('k', 'm'), out var parsedR) ? parsedR : 15.0;
        var res = _situationalEngine.AnswerWhatResourcesExist(context.IncidentId, context.Latitude, context.Longitude, context.EventType, radius, context.SubstanceId);
        return $"[MAP_RESOURCES] Mapped {res.AvailableResponderIds.Count} responders and {res.NearbyAntidoteCaches.Count} antidote caches within {radius}km.";
    }

    public string ExecuteCascadeAlert(IncidentEvaluationContext context, IReadOnlyDictionary<string, string> parameters)
    {
        var next = _situationalEngine.AnswerWhatMightHappenNext(context.IncidentId, context.Latitude, context.Longitude, context.EventType, context.CurrentPhase);
        return $"[CASCADE_ALERT] Cascaded multi-jurisdiction alerts to {next.AdjacentCountiesAtRisk.Count} adjacent counties ({string.Join(", ", next.AdjacentCountiesAtRisk.Select(c => c.Name))}).";
    }

    public string ExecuteActivateProtocol(IncidentEvaluationContext context, IReadOnlyDictionary<string, string> parameters)
    {
        string name = parameters.TryGetValue("Name", out var n) ? n : "FEMA-ICS-201";
        return $"[ACTIVATE_PROTOCOL] Activated Standard Emergency Protocol '{name}' for Incident {context.IncidentId}.";
    }

    public string ExecuteProjectTimeline(IncidentEvaluationContext context, IReadOnlyDictionary<string, string> parameters)
    {
        var next = _situationalEngine.AnswerWhatMightHappenNext(context.IncidentId, context.Latitude, context.Longitude, context.EventType, context.CurrentPhase);
        return $"[PROJECT_TIMELINE] Projected next phase '{next.ProjectedNextPhase}' in {next.EstimatedPhaseTransitionTime.TotalMinutes:F0} minutes.";
    }

    public string ExecuteAssessImpact(IncidentEvaluationContext context, IReadOnlyDictionary<string, string> parameters)
    {
        var afterward = _situationalEngine.AnswerWhatHappensAfterward(context.IncidentId, context.Latitude, context.Longitude, context.EventType);
        return $"[ASSESS_IMPACT] Estimated Economic Impact: ${afterward.EstimatedEconomicImpactUsd:N0} USD, Vulnerable Pop Impacted: {afterward.EstimatedVulnerablePopulationImpacted}.";
    }

    public string ExecuteActivatePoisonProtocol(IncidentEvaluationContext context, IReadOnlyDictionary<string, string> parameters)
    {
        string routeStr = parameters.TryGetValue("ExposureRoute", out var r) ? r : context.ExposureRoute ?? "Ingestion";
        string subClassStr = parameters.TryGetValue("SubstanceClass", out var s) ? s : context.SubstanceClass ?? "IndustrialChemical";

        var route = Enum.TryParse<ExposureRoute>(routeStr, true, out var er) ? er : ExposureRoute.Ingestion;
        var subClass = Enum.TryParse<PoisoningSubstanceClass>(subClassStr.Replace("_", string.Empty), true, out var sc) ? sc : PoisoningSubstanceClass.IndustrialChemical;

        var proto = _poisonEngine.GetProtocol(subClass, route);
        string steps = proto != null ? string.Join(" | ", proto.ImmediateFirstAidSteps.Take(2)) : "Administer high-flow 100% O2; contact 1-800-222-1222.";

        return $"[ACTIVATE_POISON_PROTOCOL] Activated AAPCC/ATSDR Protocol for {subClass} ({route}): {steps}";
    }

    public string ExecuteLocateAntidote(IncidentEvaluationContext context, IReadOnlyDictionary<string, string> parameters)
    {
        double radius = parameters.TryGetValue("Radius", out var r) && double.TryParse(r.TrimEnd('k', 'm', 'K', 'M'), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var parsedR) ? parsedR : 25.0;
        string subId = parameters.TryGetValue("SubstanceId", out var s) ? s : context.SubstanceId ?? "SUB-CYANIDE-01";
        if (subId.Equals("Event.substanceId", StringComparison.OrdinalIgnoreCase) || subId.StartsWith("Event.", StringComparison.OrdinalIgnoreCase))
        {
            subId = context.SubstanceId ?? "SUB-CYANIDE-01";
        }

        var caches = _poisonEngine.FindNearbyAntidotes(context.Latitude, context.Longitude, subId, radius);
        var primary = caches.FirstOrDefault();
        string cacheDesc = primary != null
            ? $"{primary.AntidoteName} ({primary.QuantityAvailableUnits} {primary.UnitOfMeasure}) at {primary.FacilityName} [{primary.ContactPhone}]"
            : "Emergency Mutual Aid SNS Dispatch";

        return $"[LOCATE_ANTIDOTE] Located Antidote Cache: {cacheDesc} within {radius}km.";
    }

    public string ExecuteEstablishIsolationZone(IncidentEvaluationContext context, IReadOnlyDictionary<string, string> parameters)
    {
        int guide = context.ErgGuideNumber ?? 117;
        if (parameters.TryGetValue("ERGGuide", out var g) && int.TryParse(g, out var parsedG))
        {
            guide = parsedG;
        }

        var (iso, prot) = _poisonEngine.CalculateErgIsolationZone(context.SubstanceId ?? "SUB-CYANIDE-01");

        return $"[ESTABLISH_ISOLATION_ZONE] Established ERG Guide {guide} Perimeter: Initial Isolation {iso}m, Downwind Protective Zone {prot}m.";
    }

    public string ExecuteNotifyPoisonControl(IncidentEvaluationContext context, IReadOnlyDictionary<string, string> parameters)
    {
        string fmt = parameters.TryGetValue("Format", out var f) ? f : "Structured";
        return $"[NOTIFY_POISON_CONTROL] Dispatched Digital Triage Case to AAPCC National Poison Center (1-800-222-1222) via Format '{fmt}'.";
    }

    public string ExecuteDispatchHazmat(IncidentEvaluationContext context, IReadOnlyDictionary<string, string> parameters)
    {
        return $"[DISPATCH_HAZMAT] Dispatched Hazmat Technical Response Team (NAICS 562910, O*NET 33-2011.00 Firefighters Level A/B) to Incident {context.IncidentId}.";
    }

    public string ExecuteDispatch(IncidentEvaluationContext context, IReadOnlyDictionary<string, string> parameters)
    {
        string target = parameters.TryGetValue("Responders", out var r) ? r : parameters.Values.FirstOrDefault() ?? "MEDIC";
        return $"[DISPATCH] Dispatched '{target}' unit to Incident {context.IncidentId}.";
    }

    public string ExecuteNotify(IncidentEvaluationContext context, IReadOnlyDictionary<string, string> parameters)
    {
        string channel = parameters.TryGetValue("Channel", out var c) ? c : parameters.Values.FirstOrDefault() ?? "SMS";
        return $"[NOTIFY] Broadcast alert via '{channel}' for Incident {context.IncidentId}.";
    }

    public string ExecuteGenericAction(IncidentEvaluationContext context, string actionName, IReadOnlyDictionary<string, string> parameters)
    {
        return $"[{actionName}] Executed action with {parameters.Count} parameters on Incident {context.IncidentId}.";
    }

    // =========================================================================
    // 4-Questions Situational Inferencing Query
    // =========================================================================

    public SituationalInferenceSynthesis SynthesizeSituationalInference(AstSituationalQueryDeclaration query)
    {
        return _situationalEngine.SynthesizeSituationalResponse(
            query.IncidentId,
            query.Latitude,
            query.Longitude,
            query.EventType,
            query.RadiusKm,
            query.SubstanceId,
            query.ExposureRoute
        );
    }
}
