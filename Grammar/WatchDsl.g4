grammar WatchDsl;

// =============================================================================
// TheWatch Domain-Specific Language (DSL) ANTLR4 Grammar
// Geospatial & Situational Inferencing Engine
// Copyright (c) 2026 Barton Milnor Mallory, The Watch, LLC. All rights reserved.
// =============================================================================

// Root Script
dslScript
    : (schemaDeclaration | ruleDeclaration | queryDeclaration)* EOF
    ;

// Schema Declarations (27 Nodes & 17 Relationships)
schemaDeclaration
    : nodeDeclaration
    | relationshipDeclaration
    ;

nodeDeclaration
    : NODE nodeType identifier '{' propertyAssignment* '}'
    ;

nodeType
    // Geospatial (9)
    : 'GeoState'
    | 'GeoCounty'
    | 'GeoTract'
    | 'GeoBlockGroup'
    | 'GeoPlace'
    | 'GeoRoad'
    | 'GeoWater'
    | 'GeoZCTA'
    | 'GeoAIANNH'
    // Occupational (6)
    | 'Occupation'
    | 'OccupationSkill'
    | 'OccupationKnowledge'
    | 'OccupationAbility'
    | 'WorkActivity'
    | 'DetailedWorkActivity'
    // Industry (5)
    | 'NAICSSector'
    | 'NAICSSubsector'
    | 'NAICSIndustryGroup'
    | 'NAICSIndustry'
    | 'NAICSNational'
    // Product (5)
    | 'NAPCSSection'
    | 'NAPCSSubsection'
    | 'NAPCSGroup'
    | 'NAPCSSubgroup'
    | 'NAPCSProduct'
    // Classification (2)
    | 'LCCEventType'
    | 'COSORiskAssessment'
    ;

relationshipDeclaration
    : RELATIONSHIP relationshipType '(' identifier '->' identifier ')' ('{' propertyAssignment* '}')?
    ;

relationshipType
    // Geospatial (6)
    : 'CONTAINS_GEO'
    | 'ADJACENT_TO'
    | 'INTERSECTS_ROAD'
    | 'WITHIN_RADIUS'
    | 'EVACUATION_ROUTE'
    | 'FLOOD_ZONE_OVERLAY'
    // Occupational (5)
    | 'REQUIRES_SKILL'
    | 'REQUIRES_KNOWLEDGE'
    | 'PERFORMS_ACTIVITY'
    | 'QUALIFIED_FOR'
    | 'RELEVANT_TO_EVENT'
    // Industry (4)
    | 'AFFECTED_BY'
    | 'PROVIDES_RESOURCE'
    | 'LOCATED_IN'
    | 'EMPLOYS_OCCUPATION'
    // Product (4)
    | 'MITIGATES'
    | 'SUPPLIED_BY'
    | 'AVAILABLE_IN'
    | 'ANTIDOTE_FOR'
    ;

propertyAssignment
    : identifier ':' expression ';'
    ;

// Emergency Situational & Reactive Rules
ruleDeclaration
    : RULE identifier ':' (WHEN conditionList)? (THEN actionStatement)+
    ;

conditionList
    : condition (('AND' | 'OR') condition)*
    ;

condition
    : binaryComparisonCondition
    | inLccClassCondition
    | affectsNaicsCondition
    | requiresOccupationCondition
    | requiresProductCondition
    | withinGeoCondition
    | adjacentRiskCondition
    | phaseIsCondition
    | cosoSeverityCondition
    | substanceClassIsCondition
    | exposureRouteIsCondition
    | antidoteAvailableCondition
    | antidoteWithinRadiusCondition
    | decontaminationRequiredCondition
    | '(' conditionList ')'
    ;

binaryComparisonCondition
    : expression comparisonOperator expression
    ;

inLccClassCondition
    : 'InLCCClass' stringLiteral
    ;

affectsNaicsCondition
    : 'AffectsNAICS' (stringLiteral | numberLiteral)
    ;

requiresOccupationCondition
    : 'RequiresOccupation' stringLiteral
    ;

requiresProductCondition
    : 'RequiresProduct' (stringLiteral | numberLiteral)
    ;

withinGeoCondition
    : 'WithinGeo' stringLiteral
    ;

adjacentRiskCondition
    : 'AdjacentRisk' (stringLiteral | numberLiteral)?
    ;

phaseIsCondition
    : 'PhaseIs' identifier
    ;

cosoSeverityCondition
    : 'COSOSeverity' comparisonOperator (identifier | numberLiteral)
    ;

// Medical / Poisoning Conditions (5)
substanceClassIsCondition
    : 'SubstanceClassIs' (stringLiteral | identifier)
    ;

exposureRouteIsCondition
    : 'ExposureRouteIs' (stringLiteral | identifier)
    ;

antidoteAvailableCondition
    : 'AntidoteAvailable' (stringLiteral | identifier)?
    ;

antidoteWithinRadiusCondition
    : 'AntidoteWithinRadius' distanceLiteral
    ;

decontaminationRequiredCondition
    : 'DecontaminationRequired' booleanLiteral?
    ;

// Action Statements (11 + standard CAD actions)
actionStatement
    : classifyEventAction
    | mapResourcesAction
    | cascadeAlertAction
    | activateProtocolAction
    | projectTimelineAction
    | assessImpactAction
    | activatePoisonProtocolAction
    | locateAntidoteAction
    | establishIsolationZoneAction
    | notifyPoisonControlAction
    | dispatchHazmatAction
    | dispatchAction
    | notifyAction
    | genericAction
    ;

classifyEventAction
    : 'CLASSIFY_EVENT' actionParameter*
    ;

mapResourcesAction
    : 'MAP_RESOURCES' actionParameter*
    ;

cascadeAlertAction
    : 'CASCADE_ALERT' actionParameter*
    ;

activateProtocolAction
    : 'ACTIVATE_PROTOCOL' actionParameter*
    ;

projectTimelineAction
    : 'PROJECT_TIMELINE' actionParameter*
    ;

assessImpactAction
    : 'ASSESS_IMPACT' actionParameter*
    ;

activatePoisonProtocolAction
    : 'ACTIVATE_POISON_PROTOCOL' actionParameter*
    ;

locateAntidoteAction
    : 'LOCATE_ANTIDOTE' actionParameter*
    ;

establishIsolationZoneAction
    : 'ESTABLISH_ISOLATION_ZONE' actionParameter*
    ;

notifyPoisonControlAction
    : 'NOTIFY_POISON_CONTROL' actionParameter*
    ;

dispatchHazmatAction
    : 'DISPATCH_HAZMAT' actionParameter*
    ;

dispatchAction
    : 'DISPATCH' actionParameter*
    ;

notifyAction
    : 'NOTIFY' actionParameter*
    ;

genericAction
    : identifier actionParameter*
    ;

actionParameter
    : identifier ':' expression
    ;

// Situational 4-Questions Query Declarations
queryDeclaration
    : 'QUERY_SITUATIONAL_INFERENCE' '{'
        ('INCIDENT_ID' ':' stringLiteral ';')?
        ('LATITUDE' ':' numberLiteral ';')?
        ('LONGITUDE' ':' numberLiteral ';')?
        ('EVENT_TYPE' ':' stringLiteral ';')?
        ('RADIUS' ':' distanceLiteral ';')?
        ('SUBSTANCE' ':' stringLiteral ';')?
        ('ROUTE' ':' identifier ';')?
    '}'
    ;

// Expressions & Literals
expression
    : primaryExpression
    ;

primaryExpression
    : qualifiedIdentifier
    | identifier
    | stringLiteral
    | numberLiteral
    | distanceLiteral
    | booleanLiteral
    ;

qualifiedIdentifier
    : identifier ('.' identifier)+
    ;

comparisonOperator
    : '==' | '!=' | '<=' | '>=' | '<' | '>'
    ;

distanceLiteral
    : NUMBER ('m' | 'km' | 'mi' | 'ft')
    ;

booleanLiteral
    : 'true' | 'false' | 'TRUE' | 'FALSE'
    ;

identifier
    : IDENTIFIER
    ;

stringLiteral
    : STRING
    ;

numberLiteral
    : NUMBER
    ;

// Lexer Rules
NODE            : 'NODE';
RELATIONSHIP    : 'RELATIONSHIP';
RULE            : 'RULE';
WHEN            : 'WHEN';
THEN            : 'THEN';
AND             : 'AND';
OR              : 'OR';

IDENTIFIER      : [a-zA-Z_][a-zA-Z0-9_]*;
STRING          : '"' (~["\\] | '\\' .)* '"';
NUMBER          : [0-9]+ ('.' [0-9]+)?;
WS              : [ \t\r\n]+ -> skip;
LINE_COMMENT    : '//' ~[\r\n]* -> skip;
BLOCK_COMMENT   : '/*' .*? '*/' -> skip;
