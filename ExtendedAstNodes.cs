// <copyright file="ExtendedAstNodes.cs" company="The Watch, LLC">
// Copyright (c) 2026 Barton Milnor Mallory, The Watch, LLC. All rights reserved.
// </copyright>

/// <summary>
/// Source file: src/Libraries/TheWatch.Dsl/ExtendedAstNodes.cs
/// Module: Geospatial & Situational Inferencing AST Model
/// Defines: Complete AST hierarchy for 27 Node Types, 17 Relationships, 13 Conditions, and 11 Actions.
/// Namespace: TheWatch.Dsl
/// </summary>
using System;
using System.Collections.Generic;
using TheWatch.Contracts;
using static TheWatch.Contracts.GeospatialSituationalContracts;

namespace TheWatch.Dsl;

public abstract record AstNode;

// =============================================================================
// Top-Level Script AST
// =============================================================================
public sealed record ExtendedDslScriptNode(
    IReadOnlyList<AstNodeDeclaration> NodeDeclarations,
    IReadOnlyList<AstRelationshipDeclaration> RelationshipDeclarations,
    IReadOnlyList<AstRuleDeclaration> Rules,
    IReadOnlyList<AstSituationalQueryDeclaration> Queries
) : AstNode;

// =============================================================================
// Schema: 27 Node Declarations
// =============================================================================
public abstract record AstNodeDeclaration(
    string NodeCategory,
    string NodeType,
    string Identifier,
    IReadOnlyDictionary<string, string> Properties
) : AstNode;

// Geospatial (9)
public sealed record GeoStateNodeDeclaration(string Id, IReadOnlyDictionary<string, string> Props) : AstNodeDeclaration("Geospatial", "GeoState", Id, Props);
public sealed record GeoCountyNodeDeclaration(string Id, IReadOnlyDictionary<string, string> Props) : AstNodeDeclaration("Geospatial", "GeoCounty", Id, Props);
public sealed record GeoTractNodeDeclaration(string Id, IReadOnlyDictionary<string, string> Props) : AstNodeDeclaration("Geospatial", "GeoTract", Id, Props);
public sealed record GeoBlockGroupNodeDeclaration(string Id, IReadOnlyDictionary<string, string> Props) : AstNodeDeclaration("Geospatial", "GeoBlockGroup", Id, Props);
public sealed record GeoPlaceNodeDeclaration(string Id, IReadOnlyDictionary<string, string> Props) : AstNodeDeclaration("Geospatial", "GeoPlace", Id, Props);
public sealed record GeoRoadNodeDeclaration(string Id, IReadOnlyDictionary<string, string> Props) : AstNodeDeclaration("Geospatial", "GeoRoad", Id, Props);
public sealed record GeoWaterNodeDeclaration(string Id, IReadOnlyDictionary<string, string> Props) : AstNodeDeclaration("Geospatial", "GeoWater", Id, Props);
public sealed record GeoZctaNodeDeclaration(string Id, IReadOnlyDictionary<string, string> Props) : AstNodeDeclaration("Geospatial", "GeoZCTA", Id, Props);
public sealed record GeoAiannhNodeDeclaration(string Id, IReadOnlyDictionary<string, string> Props) : AstNodeDeclaration("Geospatial", "GeoAIANNH", Id, Props);

// Occupational (6)
public sealed record OccupationNodeDeclaration(string Id, IReadOnlyDictionary<string, string> Props) : AstNodeDeclaration("Occupational", "Occupation", Id, Props);
public sealed record SkillNodeDeclaration(string Id, IReadOnlyDictionary<string, string> Props) : AstNodeDeclaration("Occupational", "OccupationSkill", Id, Props);
public sealed record KnowledgeNodeDeclaration(string Id, IReadOnlyDictionary<string, string> Props) : AstNodeDeclaration("Occupational", "OccupationKnowledge", Id, Props);
public sealed record AbilityNodeDeclaration(string Id, IReadOnlyDictionary<string, string> Props) : AstNodeDeclaration("Occupational", "OccupationAbility", Id, Props);
public sealed record WorkActivityNodeDeclaration(string Id, IReadOnlyDictionary<string, string> Props) : AstNodeDeclaration("Occupational", "WorkActivity", Id, Props);
public sealed record DwaNodeDeclaration(string Id, IReadOnlyDictionary<string, string> Props) : AstNodeDeclaration("Occupational", "DetailedWorkActivity", Id, Props);

// Industry (5)
public sealed record NaicsSectorNodeDeclaration(string Id, IReadOnlyDictionary<string, string> Props) : AstNodeDeclaration("Industry", "NAICSSector", Id, Props);
public sealed record NaicsSubsectorNodeDeclaration(string Id, IReadOnlyDictionary<string, string> Props) : AstNodeDeclaration("Industry", "NAICSSubsector", Id, Props);
public sealed record NaicsIndustryGroupNodeDeclaration(string Id, IReadOnlyDictionary<string, string> Props) : AstNodeDeclaration("Industry", "NAICSIndustryGroup", Id, Props);
public sealed record NaicsIndustryNodeDeclaration(string Id, IReadOnlyDictionary<string, string> Props) : AstNodeDeclaration("Industry", "NAICSIndustry", Id, Props);
public sealed record NaicsNationalNodeDeclaration(string Id, IReadOnlyDictionary<string, string> Props) : AstNodeDeclaration("Industry", "NAICSNational", Id, Props);

// Product (5)
public sealed record NapcsSectionNodeDeclaration(string Id, IReadOnlyDictionary<string, string> Props) : AstNodeDeclaration("Product", "NAPCSSection", Id, Props);
public sealed record NapcsSubsectionNodeDeclaration(string Id, IReadOnlyDictionary<string, string> Props) : AstNodeDeclaration("Product", "NAPCSSubsection", Id, Props);
public sealed record NapcsGroupNodeDeclaration(string Id, IReadOnlyDictionary<string, string> Props) : AstNodeDeclaration("Product", "NAPCSGroup", Id, Props);
public sealed record NapcsSubgroupNodeDeclaration(string Id, IReadOnlyDictionary<string, string> Props) : AstNodeDeclaration("Product", "NAPCSSubgroup", Id, Props);
public sealed record NapcsProductNodeDeclaration(string Id, IReadOnlyDictionary<string, string> Props) : AstNodeDeclaration("Product", "NAPCSProduct", Id, Props);

// Classification (2)
public sealed record LccEventTypeNodeDeclaration(string Id, IReadOnlyDictionary<string, string> Props) : AstNodeDeclaration("Classification", "LCCEventType", Id, Props);
public sealed record CosoRiskNodeDeclaration(string Id, IReadOnlyDictionary<string, string> Props) : AstNodeDeclaration("Classification", "COSORiskAssessment", Id, Props);

// =============================================================================
// Schema: 17 Relationship Declarations
// =============================================================================
public sealed record AstRelationshipDeclaration(
    string RelationshipType,
    string SourceNodeId,
    string TargetNodeId,
    IReadOnlyDictionary<string, string> Properties
) : AstNode;

// =============================================================================
// Rules & Conditions (13 Conditions)
// =============================================================================
public sealed record AstRuleDeclaration(
    string RuleName,
    AstConditionNode? Condition,
    IReadOnlyList<AstActionStatement> Actions
) : AstNode;

public abstract record AstConditionNode : AstNode;

// Binary & Logical Conditions
public sealed record AstBinaryComparisonCondition(
    string LeftOperand,
    string Operator,
    string RightOperand
) : AstConditionNode;

public sealed record AstLogicalCondition(
    AstConditionNode Left,
    string LogicalOp, // "AND", "OR"
    AstConditionNode Right
) : AstConditionNode;

// General Situational Conditions (8)
public sealed record AstInLccClassCondition(string LccClassPattern) : AstConditionNode;
public sealed record AstAffectsNaicsCondition(string NaicsCodePattern) : AstConditionNode;
public sealed record AstRequiresOccupationCondition(string SocCodePattern) : AstConditionNode;
public sealed record AstRequiresProductCondition(string NapcsCodePattern) : AstConditionNode;
public sealed record AstWithinGeoCondition(string GeoIdOrName) : AstConditionNode;
public sealed record AstAdjacentRiskCondition(string TargetAreaOrThreshold) : AstConditionNode;
public sealed record AstPhaseIsCondition(EventLifeCyclePhase TargetPhase) : AstConditionNode;
public sealed record AstCosoSeverityCondition(string Operator, string SeverityThreshold) : AstConditionNode;

// Medical & Poisoning Conditions (5)
public sealed record AstSubstanceClassIsCondition(string SubstanceClass) : AstConditionNode;
public sealed record AstExposureRouteIsCondition(string ExposureRoute) : AstConditionNode;
public sealed record AstAntidoteAvailableCondition(string? SubstanceOrAntidoteId) : AstConditionNode;
public sealed record AstAntidoteWithinRadiusCondition(double RadiusKm) : AstConditionNode;
public sealed record AstDecontaminationRequiredCondition(bool Required = true) : AstConditionNode;

// =============================================================================
// Actions (11 Actions)
// =============================================================================
public abstract record AstActionStatement(
    string ActionName,
    IReadOnlyDictionary<string, string> Parameters
) : AstNode;

// General Actions (6)
public sealed record AstClassifyEventAction(IReadOnlyDictionary<string, string> Parameters) : AstActionStatement("CLASSIFY_EVENT", Parameters);
public sealed record AstMapResourcesAction(IReadOnlyDictionary<string, string> Parameters) : AstActionStatement("MAP_RESOURCES", Parameters);
public sealed record AstCascadeAlertAction(IReadOnlyDictionary<string, string> Parameters) : AstActionStatement("CASCADE_ALERT", Parameters);
public sealed record AstActivateProtocolAction(IReadOnlyDictionary<string, string> Parameters) : AstActionStatement("ACTIVATE_PROTOCOL", Parameters);
public sealed record AstProjectTimelineAction(IReadOnlyDictionary<string, string> Parameters) : AstActionStatement("PROJECT_TIMELINE", Parameters);
public sealed record AstAssessImpactAction(IReadOnlyDictionary<string, string> Parameters) : AstActionStatement("ASSESS_IMPACT", Parameters);

// Poisoning & Medical Actions (5)
public sealed record AstActivatePoisonProtocolAction(IReadOnlyDictionary<string, string> Parameters) : AstActionStatement("ACTIVATE_POISON_PROTOCOL", Parameters);
public sealed record AstLocateAntidoteAction(IReadOnlyDictionary<string, string> Parameters) : AstActionStatement("LOCATE_ANTIDOTE", Parameters);
public sealed record AstEstablishIsolationZoneAction(IReadOnlyDictionary<string, string> Parameters) : AstActionStatement("ESTABLISH_ISOLATION_ZONE", Parameters);
public sealed record AstNotifyPoisonControlAction(IReadOnlyDictionary<string, string> Parameters) : AstActionStatement("NOTIFY_POISON_CONTROL", Parameters);
public sealed record AstDispatchHazmatAction(IReadOnlyDictionary<string, string> Parameters) : AstActionStatement("DISPATCH_HAZMAT", Parameters);

// Standard Dispatch & Notification Actions
public sealed record AstDispatchAction(IReadOnlyDictionary<string, string> Parameters) : AstActionStatement("DISPATCH", Parameters);
public sealed record AstNotifyAction(IReadOnlyDictionary<string, string> Parameters) : AstActionStatement("NOTIFY", Parameters);
public sealed record AstGenericAction(string Name, IReadOnlyDictionary<string, string> Parameters) : AstActionStatement(Name, Parameters);

// =============================================================================
// 4-Questions Situational Query Declaration
// =============================================================================
public sealed record AstSituationalQueryDeclaration(
    string IncidentId,
    double Latitude,
    double Longitude,
    string EventType,
    double RadiusKm,
    string? SubstanceId,
    string? ExposureRoute
) : AstNode;
