// <copyright file="ExtendedDslLexerAndParser.cs" company="The Watch, LLC">
// Copyright (c) 2026 Barton Milnor Mallory, The Watch, LLC. All rights reserved.
// </copyright>

/// <summary>
/// Source file: src/Libraries/TheWatch.Dsl/ExtendedDslLexerAndParser.cs
/// Module: Geospatial & Situational Inferencing DSL Lexer and Recursive Descent Parser
/// Defines: Complete parser generating ExtendedDslScriptNode and AST structures.
/// Namespace: TheWatch.Dsl
/// </summary>
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using TheWatch.Contracts;
using static TheWatch.Contracts.GeospatialSituationalContracts;

namespace TheWatch.Dsl;

public enum ExtendedTokenType
{
    // Keywords
    Node,
    Relationship,
    Rule,
    When,
    Then,
    And,
    Or,
    QuerySituationalInference,

    // Condition Keywords
    InLccClass,
    AffectsNaics,
    RequiresOccupation,
    RequiresProduct,
    WithinGeo,
    AdjacentRisk,
    PhaseIs,
    CosoSeverity,
    SubstanceClassIs,
    ExposureRouteIs,
    AntidoteAvailable,
    AntidoteWithinRadius,
    DecontaminationRequired,

    // Action Keywords
    ClassifyEvent,
    MapResources,
    CascadeAlert,
    ActivateProtocol,
    ProjectTimeline,
    AssessImpact,
    ActivatePoisonProtocol,
    LocateAntidote,
    EstablishIsolationZone,
    NotifyPoisonControl,
    DispatchHazmat,
    Dispatch,
    Notify,

    // Node Types
    GeoState, GeoCounty, GeoTract, GeoBlockGroup, GeoPlace, GeoRoad, GeoWater, GeoZcta, GeoAiannh,
    Occupation, OccupationSkill, OccupationKnowledge, OccupationAbility, WorkActivity, DetailedWorkActivity,
    NaicsSector, NaicsSubsector, NaicsIndustryGroup, NaicsIndustry, NaicsNational,
    NapcsSection, NapcsSubsection, NapcsGroup, NapcsSubgroup, NapcsProduct,
    LccEventType, CosoRiskAssessment,

    // Relationship Types
    ContainsGeo, AdjacentTo, IntersectsRoad, WithinRadius, EvacuationRoute, FloodZoneOverlay,
    RequiresSkill, RequiresKnowledge, PerformsActivity, QualifiedFor, RelevantToEvent,
    AffectedBy, ProvidesResource, LocatedIn, EmploysOccupation,
    Mitigates, SuppliedBy, AvailableIn, AntidoteFor,

    // Literals & Identifiers
    Identifier,
    StringLiteral,
    NumberLiteral,
    DistanceLiteral,
    BooleanLiteral,

    // Operators & Delimiters
    Equals,
    NotEquals,
    LessThan,
    LessThanOrEqual,
    GreaterThan,
    GreaterThanOrEqual,
    Arrow, // ->
    Colon,
    Semicolon,
    OpenBrace,
    CloseBrace,
    OpenParen,
    CloseParen,
    Dot,
    Comma,

    EndOfFile
}

public sealed record ExtendedToken(ExtendedTokenType Type, string Value, int Position, int Line);

public sealed class ExtendedDslLexer
{
    public static List<ExtendedToken> Tokenize(string source)
    {
        var tokens = new List<ExtendedToken>();
        int i = 0;
        int line = 1;

        while (i < source.Length)
        {
            char c = source[i];

            // Whitespace
            if (char.IsWhiteSpace(c))
            {
                if (c == '\n') line++;
                i++;
                continue;
            }

            // Comments
            if (c == '/' && i + 1 < source.Length && source[i + 1] == '/')
            {
                while (i < source.Length && source[i] != '\n') i++;
                continue;
            }
            if (c == '/' && i + 1 < source.Length && source[i + 1] == '*')
            {
                i += 2;
                while (i + 1 < source.Length && !(source[i] == '*' && source[i + 1] == '/'))
                {
                    if (source[i] == '\n') line++;
                    i++;
                }
                i += 2;
                continue;
            }

            // String literals
            if (c == '"' || c == '\'')
            {
                char quote = c;
                int start = i++;
                var sb = new StringBuilder();
                while (i < source.Length && source[i] != quote)
                {
                    if (source[i] == '\\' && i + 1 < source.Length)
                    {
                        i++;
                        sb.Append(source[i]);
                    }
                    else
                    {
                        sb.Append(source[i]);
                    }
                    i++;
                }
                if (i < source.Length) i++; // consume closing quote
                tokens.Add(new ExtendedToken(ExtendedTokenType.StringLiteral, sb.ToString(), start, line));
                continue;
            }

            // Symbols & Multi-character operators
            if (c == '-' && i + 1 < source.Length && source[i + 1] == '>')
            {
                tokens.Add(new ExtendedToken(ExtendedTokenType.Arrow, "->", i, line));
                i += 2;
                continue;
            }
            if (c == '=' && i + 1 < source.Length && source[i + 1] == '=')
            {
                tokens.Add(new ExtendedToken(ExtendedTokenType.Equals, "==", i, line));
                i += 2;
                continue;
            }
            if (c == '!' && i + 1 < source.Length && source[i + 1] == '=')
            {
                tokens.Add(new ExtendedToken(ExtendedTokenType.NotEquals, "!=", i, line));
                i += 2;
                continue;
            }
            if (c == '<' && i + 1 < source.Length && source[i + 1] == '=')
            {
                tokens.Add(new ExtendedToken(ExtendedTokenType.LessThanOrEqual, "<=", i, line));
                i += 2;
                continue;
            }
            if (c == '>' && i + 1 < source.Length && source[i + 1] == '=')
            {
                tokens.Add(new ExtendedToken(ExtendedTokenType.GreaterThanOrEqual, ">=", i, line));
                i += 2;
                continue;
            }
            if (c == '<') { tokens.Add(new ExtendedToken(ExtendedTokenType.LessThan, "<", i++, line)); continue; }
            if (c == '>') { tokens.Add(new ExtendedToken(ExtendedTokenType.GreaterThan, ">", i++, line)); continue; }
            if (c == ':') { tokens.Add(new ExtendedToken(ExtendedTokenType.Colon, ":", i++, line)); continue; }
            if (c == ';') { tokens.Add(new ExtendedToken(ExtendedTokenType.Semicolon, ";", i++, line)); continue; }
            if (c == '{') { tokens.Add(new ExtendedToken(ExtendedTokenType.OpenBrace, "{", i++, line)); continue; }
            if (c == '}') { tokens.Add(new ExtendedToken(ExtendedTokenType.CloseBrace, "}", i++, line)); continue; }
            if (c == '(') { tokens.Add(new ExtendedToken(ExtendedTokenType.OpenParen, "(", i++, line)); continue; }
            if (c == ')') { tokens.Add(new ExtendedToken(ExtendedTokenType.CloseParen, ")", i++, line)); continue; }
            if (c == '.') { tokens.Add(new ExtendedToken(ExtendedTokenType.Dot, ".", i++, line)); continue; }
            if (c == ',') { tokens.Add(new ExtendedToken(ExtendedTokenType.Comma, ",", i++, line)); continue; }

            // Numbers & Distance Literals (e.g. 25km, 500m)
            if (char.IsDigit(c) || (c == '-' && i + 1 < source.Length && char.IsDigit(source[i + 1])))
            {
                int start = i;
                bool isNeg = c == '-';
                if (isNeg) i++;
                while (i < source.Length && (char.IsDigit(source[i]) || source[i] == '.')) i++;
                string numPart = source.Substring(start, i - start);

                // Check distance unit suffix
                int unitStart = i;
                while (i < source.Length && char.IsLetter(source[i])) i++;
                string unitPart = source.Substring(unitStart, i - unitStart);

                if (unitPart.Equals("km", StringComparison.OrdinalIgnoreCase) ||
                    unitPart.Equals("m", StringComparison.OrdinalIgnoreCase) ||
                    unitPart.Equals("mi", StringComparison.OrdinalIgnoreCase) ||
                    unitPart.Equals("ft", StringComparison.OrdinalIgnoreCase))
                {
                    tokens.Add(new ExtendedToken(ExtendedTokenType.DistanceLiteral, numPart + unitPart, start, line));
                }
                else
                {
                    i = unitStart; // backtrack unit if not distance
                    tokens.Add(new ExtendedToken(ExtendedTokenType.NumberLiteral, numPart, start, line));
                }
                continue;
            }

            // Identifiers & Keywords
            if (char.IsLetter(c) || c == '_')
            {
                int start = i;
                while (i < source.Length && (char.IsLetterOrDigit(source[i]) || source[i] == '_' || source[i] == '-' || source[i] == '.'))
                {
                    if (source[i] == '.' && (i + 1 < source.Length && !char.IsLetterOrDigit(source[i + 1]))) break;
                    i++;
                }
                string ident = source.Substring(start, i - start);
                ExtendedTokenType type = ResolveKeyword(ident);
                tokens.Add(new ExtendedToken(type, ident, start, line));
                continue;
            }

            i++; // skip unrecognized char
        }

        tokens.Add(new ExtendedToken(ExtendedTokenType.EndOfFile, string.Empty, i, line));
        return tokens;
    }

    private static ExtendedTokenType ResolveKeyword(string text)
    {
        return text.ToUpperInvariant() switch
        {
            "NODE" => ExtendedTokenType.Node,
            "RELATIONSHIP" => ExtendedTokenType.Relationship,
            "RULE" => ExtendedTokenType.Rule,
            "WHEN" => ExtendedTokenType.When,
            "THEN" => ExtendedTokenType.Then,
            "AND" => ExtendedTokenType.And,
            "OR" => ExtendedTokenType.Or,
            "QUERY_SITUATIONAL_INFERENCE" => ExtendedTokenType.QuerySituationalInference,

            // Condition Keywords
            "INLCCCLASS" => ExtendedTokenType.InLccClass,
            "AFFECTSNAICS" => ExtendedTokenType.AffectsNaics,
            "REQUIRESOCCUPATION" => ExtendedTokenType.RequiresOccupation,
            "REQUIRESPRODUCT" => ExtendedTokenType.RequiresProduct,
            "WITHINGEO" => ExtendedTokenType.WithinGeo,
            "ADJACENTRISK" => ExtendedTokenType.AdjacentRisk,
            "PHASEIS" => ExtendedTokenType.PhaseIs,
            "COSOSEVERITY" => ExtendedTokenType.CosoSeverity,
            "SUBSTANCECLASSIS" => ExtendedTokenType.SubstanceClassIs,
            "EXPOSUREROUTEIS" => ExtendedTokenType.ExposureRouteIs,
            "ANTIDOTEAVAILABLE" => ExtendedTokenType.AntidoteAvailable,
            "ANTIDOTEWITHINRADIUS" => ExtendedTokenType.AntidoteWithinRadius,
            "DECONTAMINATIONREQUIRED" => ExtendedTokenType.DecontaminationRequired,

            // Action Keywords
            "CLASSIFY_EVENT" => ExtendedTokenType.ClassifyEvent,
            "MAP_RESOURCES" => ExtendedTokenType.MapResources,
            "CASCADE_ALERT" => ExtendedTokenType.CascadeAlert,
            "ACTIVATE_PROTOCOL" => ExtendedTokenType.ActivateProtocol,
            "PROJECT_TIMELINE" => ExtendedTokenType.ProjectTimeline,
            "ASSESS_IMPACT" => ExtendedTokenType.AssessImpact,
            "ACTIVATE_POISON_PROTOCOL" => ExtendedTokenType.ActivatePoisonProtocol,
            "LOCATE_ANTIDOTE" => ExtendedTokenType.LocateAntidote,
            "ESTABLISH_ISOLATION_ZONE" => ExtendedTokenType.EstablishIsolationZone,
            "NOTIFY_POISON_CONTROL" => ExtendedTokenType.NotifyPoisonControl,
            "DISPATCH_HAZMAT" => ExtendedTokenType.DispatchHazmat,
            "DISPATCH" => ExtendedTokenType.Dispatch,
            "NOTIFY" => ExtendedTokenType.Notify,

            // Node Types
            "GEOSTATE" => ExtendedTokenType.GeoState,
            "GEOCOUNTY" => ExtendedTokenType.GeoCounty,
            "GEOTRACT" => ExtendedTokenType.GeoTract,
            "GEOBLOCKGROUP" => ExtendedTokenType.GeoBlockGroup,
            "GEOPLACE" => ExtendedTokenType.GeoPlace,
            "GEOROAD" => ExtendedTokenType.GeoRoad,
            "GEOWATER" => ExtendedTokenType.GeoWater,
            "GEOZCTA" => ExtendedTokenType.GeoZcta,
            "GEOAIANNH" => ExtendedTokenType.GeoAiannh,
            "OCCUPATION" => ExtendedTokenType.Occupation,
            "OCCUPATIONSKILL" => ExtendedTokenType.OccupationSkill,
            "OCCUPATIONKNOWLEDGE" => ExtendedTokenType.OccupationKnowledge,
            "OCCUPATIONABILITY" => ExtendedTokenType.OccupationAbility,
            "WORKACTIVITY" => ExtendedTokenType.WorkActivity,
            "DETAILEDWORKACTIVITY" => ExtendedTokenType.DetailedWorkActivity,
            "NAICSSECTOR" => ExtendedTokenType.NaicsSector,
            "NAICSSUBSECTOR" => ExtendedTokenType.NaicsSubsector,
            "NAICSINDUSTRYGROUP" => ExtendedTokenType.NaicsIndustryGroup,
            "NAICSINDUSTRY" => ExtendedTokenType.NaicsIndustry,
            "NAICSNATIONAL" => ExtendedTokenType.NaicsNational,
            "NAPCSSECTION" => ExtendedTokenType.NapcsSection,
            "NAPCSSUBSECTION" => ExtendedTokenType.NapcsSubsection,
            "NAPCSGROUP" => ExtendedTokenType.NapcsGroup,
            "NAPCSSUBGROUP" => ExtendedTokenType.NapcsSubgroup,
            "NAPCSPRODUCT" => ExtendedTokenType.NapcsProduct,
            "LCCEVENTTYPE" => ExtendedTokenType.LccEventType,
            "COSORISKASSESSMENT" => ExtendedTokenType.CosoRiskAssessment,

            // Relationship Types
            "CONTAINS_GEO" => ExtendedTokenType.ContainsGeo,
            "ADJACENT_TO" => ExtendedTokenType.AdjacentTo,
            "INTERSECTS_ROAD" => ExtendedTokenType.IntersectsRoad,
            "WITHIN_RADIUS" => ExtendedTokenType.WithinRadius,
            "EVACUATION_ROUTE" => ExtendedTokenType.EvacuationRoute,
            "FLOOD_ZONE_OVERLAY" => ExtendedTokenType.FloodZoneOverlay,
            "REQUIRES_SKILL" => ExtendedTokenType.RequiresSkill,
            "REQUIRES_KNOWLEDGE" => ExtendedTokenType.RequiresKnowledge,
            "PERFORMS_ACTIVITY" => ExtendedTokenType.PerformsActivity,
            "QUALIFIED_FOR" => ExtendedTokenType.QualifiedFor,
            "RELEVANT_TO_EVENT" => ExtendedTokenType.RelevantToEvent,
            "AFFECTED_BY" => ExtendedTokenType.AffectedBy,
            "PROVIDES_RESOURCE" => ExtendedTokenType.ProvidesResource,
            "LOCATED_IN" => ExtendedTokenType.LocatedIn,
            "EMPLOYS_OCCUPATION" => ExtendedTokenType.EmploysOccupation,
            "MITIGATES" => ExtendedTokenType.Mitigates,
            "SUPPLIED_BY" => ExtendedTokenType.SuppliedBy,
            "AVAILABLE_IN" => ExtendedTokenType.AvailableIn,
            "ANTIDOTE_FOR" => ExtendedTokenType.AntidoteFor,

            // Booleans
            "TRUE" => ExtendedTokenType.BooleanLiteral,
            "FALSE" => ExtendedTokenType.BooleanLiteral,

            _ => ExtendedTokenType.Identifier
        };
    }
}

public sealed class ExtendedDslParser
{
    private readonly List<ExtendedToken> _tokens;
    private int _current = 0;

    public ExtendedDslParser(List<ExtendedToken> tokens)
    {
        _tokens = tokens;
    }

    public static ExtendedDslScriptNode ParseScript(string scriptText)
    {
        var tokens = ExtendedDslLexer.Tokenize(scriptText);
        var parser = new ExtendedDslParser(tokens);
        return parser.Parse();
    }

    public ExtendedDslScriptNode Parse()
    {
        var nodes = new List<AstNodeDeclaration>();
        var rels = new List<AstRelationshipDeclaration>();
        var rules = new List<AstRuleDeclaration>();
        var queries = new List<AstSituationalQueryDeclaration>();

        while (!IsAtEnd())
        {
            if (Match(ExtendedTokenType.Node))
            {
                nodes.Add(ParseNodeDeclaration());
            }
            else if (Match(ExtendedTokenType.Relationship))
            {
                rels.Add(ParseRelationshipDeclaration());
            }
            else if (Match(ExtendedTokenType.Rule))
            {
                rules.Add(ParseRuleDeclaration());
            }
            else if (Match(ExtendedTokenType.QuerySituationalInference))
            {
                queries.Add(ParseSituationalQueryDeclaration());
            }
            else
            {
                _current++; // advance to recover
            }
        }

        return new ExtendedDslScriptNode(nodes, rels, rules, queries);
    }

    private AstNodeDeclaration ParseNodeDeclaration()
    {
        var typeToken = Advance();
        string nodeType = typeToken.Value;
        string id = Consume(ExtendedTokenType.Identifier, "Expected node identifier").Value;

        var props = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        if (Match(ExtendedTokenType.OpenBrace))
        {
            while (!Check(ExtendedTokenType.CloseBrace) && !IsAtEnd())
            {
                string key = Advance().Value;
                if (Match(ExtendedTokenType.Colon))
                {
                    string val = Advance().Value;
                    props[key] = val;
                }
                Match(ExtendedTokenType.Semicolon);
            }
            Consume(ExtendedTokenType.CloseBrace, "Expected '}' after node properties");
        }

        return CreateTypedNodeDeclaration(nodeType, id, props);
    }

    private AstNodeDeclaration CreateTypedNodeDeclaration(string nodeType, string id, Dictionary<string, string> props)
    {
        return nodeType.ToUpperInvariant() switch
        {
            "GEOSTATE" => new GeoStateNodeDeclaration(id, props),
            "GEOCOUNTY" => new GeoCountyNodeDeclaration(id, props),
            "GEOTRACT" => new GeoTractNodeDeclaration(id, props),
            "GEOBLOCKGROUP" => new GeoBlockGroupNodeDeclaration(id, props),
            "GEOPLACE" => new GeoPlaceNodeDeclaration(id, props),
            "GEOROAD" => new GeoRoadNodeDeclaration(id, props),
            "GEOWATER" => new GeoWaterNodeDeclaration(id, props),
            "GEOZCTA" => new GeoZctaNodeDeclaration(id, props),
            "GEOAIANNH" => new GeoAiannhNodeDeclaration(id, props),

            "OCCUPATION" => new OccupationNodeDeclaration(id, props),
            "OCCUPATIONSKILL" => new SkillNodeDeclaration(id, props),
            "OCCUPATIONKNOWLEDGE" => new KnowledgeNodeDeclaration(id, props),
            "OCCUPATIONABILITY" => new AbilityNodeDeclaration(id, props),
            "WORKACTIVITY" => new WorkActivityNodeDeclaration(id, props),
            "DETAILEDWORKACTIVITY" => new DwaNodeDeclaration(id, props),

            "NAICSSECTOR" => new NaicsSectorNodeDeclaration(id, props),
            "NAICSSUBSECTOR" => new NaicsSubsectorNodeDeclaration(id, props),
            "NAICSINDUSTRYGROUP" => new NaicsIndustryGroupNodeDeclaration(id, props),
            "NAICSINDUSTRY" => new NaicsIndustryNodeDeclaration(id, props),
            "NAICSNATIONAL" => new NaicsNationalNodeDeclaration(id, props),

            "NAPCSSECTION" => new NapcsSectionNodeDeclaration(id, props),
            "NAPCSSUBSECTION" => new NapcsSubsectionNodeDeclaration(id, props),
            "NAPCSGROUP" => new NapcsGroupNodeDeclaration(id, props),
            "NAPCSSUBGROUP" => new NapcsSubgroupNodeDeclaration(id, props),
            "NAPCSPRODUCT" => new NapcsProductNodeDeclaration(id, props),

            "LCCEVENTTYPE" => new LccEventTypeNodeDeclaration(id, props),
            "COSORISKASSESSMENT" => new CosoRiskNodeDeclaration(id, props),

            _ => new GeoCountyNodeDeclaration(id, props)
        };
    }

    private AstRelationshipDeclaration ParseRelationshipDeclaration()
    {
        string relType = Advance().Value;
        Consume(ExtendedTokenType.OpenParen, "Expected '(' after relationship type");
        string sourceId = Advance().Value;
        Consume(ExtendedTokenType.Arrow, "Expected '->' in relationship declaration");
        string targetId = Advance().Value;
        Consume(ExtendedTokenType.CloseParen, "Expected ')' after relationship endpoints");

        var props = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        if (Match(ExtendedTokenType.OpenBrace))
        {
            while (!Check(ExtendedTokenType.CloseBrace) && !IsAtEnd())
            {
                string key = Advance().Value;
                if (Match(ExtendedTokenType.Colon))
                {
                    string val = Advance().Value;
                    props[key] = val;
                }
                Match(ExtendedTokenType.Semicolon);
            }
            Consume(ExtendedTokenType.CloseBrace, "Expected '}' after relationship properties");
        }

        return new AstRelationshipDeclaration(relType, sourceId, targetId, props);
    }

    private AstRuleDeclaration ParseRuleDeclaration()
    {
        string ruleName = Advance().Value;
        Match(ExtendedTokenType.Colon);

        AstConditionNode? condition = null;
        if (Match(ExtendedTokenType.When))
        {
            condition = ParseConditionExpression();
        }

        var actions = new List<AstActionStatement>();
        while (Match(ExtendedTokenType.Then) && !IsAtEnd())
        {
            actions.Add(ParseActionStatement());
        }

        return new AstRuleDeclaration(ruleName, condition, actions);
    }

    private AstConditionNode ParseConditionExpression()
    {
        var left = ParseSingleCondition();

        while (Match(ExtendedTokenType.And) || Match(ExtendedTokenType.Or))
        {
            string op = Previous().Value.ToUpperInvariant();
            var right = ParseSingleCondition();
            left = new AstLogicalCondition(left, op, right);
        }

        return left;
    }

    private AstConditionNode ParseSingleCondition()
    {
        if (Match(ExtendedTokenType.InLccClass))
        {
            string pattern = Advance().Value;
            return new AstInLccClassCondition(pattern);
        }
        if (Match(ExtendedTokenType.AffectsNaics))
        {
            string pattern = Advance().Value;
            return new AstAffectsNaicsCondition(pattern);
        }
        if (Match(ExtendedTokenType.RequiresOccupation))
        {
            string pattern = Advance().Value;
            return new AstRequiresOccupationCondition(pattern);
        }
        if (Match(ExtendedTokenType.RequiresProduct))
        {
            string pattern = Advance().Value;
            return new AstRequiresProductCondition(pattern);
        }
        if (Match(ExtendedTokenType.WithinGeo))
        {
            string geo = Advance().Value;
            return new AstWithinGeoCondition(geo);
        }
        if (Match(ExtendedTokenType.AdjacentRisk))
        {
            string target = Check(ExtendedTokenType.Identifier) || Check(ExtendedTokenType.StringLiteral) || Check(ExtendedTokenType.NumberLiteral)
                ? Advance().Value
                : "True";
            return new AstAdjacentRiskCondition(target);
        }
        if (Match(ExtendedTokenType.PhaseIs))
        {
            string phaseName = Advance().Value;
            var phase = Enum.TryParse<EventLifeCyclePhase>(phaseName, true, out var p) ? p : EventLifeCyclePhase.During;
            return new AstPhaseIsCondition(phase);
        }
        if (Match(ExtendedTokenType.CosoSeverity))
        {
            string op = Advance().Value;
            string thresh = Advance().Value;
            return new AstCosoSeverityCondition(op, thresh);
        }

        // Poisoning Conditions
        if (Match(ExtendedTokenType.SubstanceClassIs))
        {
            string cls = Advance().Value;
            return new AstSubstanceClassIsCondition(cls);
        }
        if (Match(ExtendedTokenType.ExposureRouteIs))
        {
            string route = Advance().Value;
            return new AstExposureRouteIsCondition(route);
        }
        if (Match(ExtendedTokenType.AntidoteAvailable))
        {
            string? sub = Check(ExtendedTokenType.Identifier) || Check(ExtendedTokenType.StringLiteral)
                ? Advance().Value
                : null;
            return new AstAntidoteAvailableCondition(sub);
        }
        if (Match(ExtendedTokenType.AntidoteWithinRadius))
        {
            string dist = Advance().Value;
            double radiusKm = ParseDistanceKm(dist);
            return new AstAntidoteWithinRadiusCondition(radiusKm);
        }
        if (Match(ExtendedTokenType.DecontaminationRequired))
        {
            bool req = !Check(ExtendedTokenType.BooleanLiteral) || bool.Parse(Advance().Value);
            return new AstDecontaminationRequiredCondition(req);
        }

        // Parentheses
        if (Match(ExtendedTokenType.OpenParen))
        {
            var inner = ParseConditionExpression();
            Consume(ExtendedTokenType.CloseParen, "Expected ')' after condition");
            return inner;
        }

        // Binary comparison (e.g. Event.type == "PoisoningIngested")
        string leftExpr = Advance().Value;
        string comparisonOp = Advance().Value;
        string rightExpr = Advance().Value;

        return new AstBinaryComparisonCondition(leftExpr, comparisonOp, rightExpr);
    }

    private AstActionStatement ParseActionStatement()
    {
        var actionToken = Advance();
        string actionName = actionToken.Value;
        var parameters = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        while (!Check(ExtendedTokenType.Then) && !Check(ExtendedTokenType.Rule) && !Check(ExtendedTokenType.Node) && !Check(ExtendedTokenType.Relationship) && !IsAtEnd())
        {
            if (Check(ExtendedTokenType.Identifier) && PeekNext().Type == ExtendedTokenType.Colon)
            {
                string key = Advance().Value;
                Match(ExtendedTokenType.Colon);
                string val = Advance().Value;
                parameters[key] = val;
            }
            else if (Check(ExtendedTokenType.Identifier) || Check(ExtendedTokenType.StringLiteral))
            {
                string val = Advance().Value;
                parameters["Param" + parameters.Count] = val;
            }
            else
            {
                break;
            }
        }

        return actionName.ToUpperInvariant() switch
        {
            "CLASSIFY_EVENT" => new AstClassifyEventAction(parameters),
            "MAP_RESOURCES" => new AstMapResourcesAction(parameters),
            "CASCADE_ALERT" => new AstCascadeAlertAction(parameters),
            "ACTIVATE_PROTOCOL" => new AstActivateProtocolAction(parameters),
            "PROJECT_TIMELINE" => new AstProjectTimelineAction(parameters),
            "ASSESS_IMPACT" => new AstAssessImpactAction(parameters),

            "ACTIVATE_POISON_PROTOCOL" => new AstActivatePoisonProtocolAction(parameters),
            "LOCATE_ANTIDOTE" => new AstLocateAntidoteAction(parameters),
            "ESTABLISH_ISOLATION_ZONE" => new AstEstablishIsolationZoneAction(parameters),
            "NOTIFY_POISON_CONTROL" => new AstNotifyPoisonControlAction(parameters),
            "DISPATCH_HAZMAT" => new AstDispatchHazmatAction(parameters),

            "DISPATCH" => new AstDispatchAction(parameters),
            "NOTIFY" => new AstNotifyAction(parameters),
            _ => new AstGenericAction(actionName, parameters)
        };
    }

    private AstSituationalQueryDeclaration ParseSituationalQueryDeclaration()
    {
        Consume(ExtendedTokenType.OpenBrace, "Expected '{' after QUERY_SITUATIONAL_INFERENCE");

        string incidentId = "INC-AUTO";
        double lat = 0.0;
        double lon = 0.0;
        string eventType = "GeneralEmergency";
        double radiusKm = 10.0;
        string? substance = null;
        string? route = null;

        while (!Check(ExtendedTokenType.CloseBrace) && !IsAtEnd())
        {
            string key = Advance().Value.ToUpperInvariant();
            Match(ExtendedTokenType.Colon);
            string val = Advance().Value;
            Match(ExtendedTokenType.Semicolon);

            switch (key)
            {
                case "INCIDENT_ID": incidentId = val; break;
                case "LATITUDE": double.TryParse(val, NumberStyles.Float, CultureInfo.InvariantCulture, out lat); break;
                case "LONGITUDE": double.TryParse(val, NumberStyles.Float, CultureInfo.InvariantCulture, out lon); break;
                case "EVENT_TYPE": eventType = val; break;
                case "RADIUS": radiusKm = ParseDistanceKm(val); break;
                case "SUBSTANCE": substance = val; break;
                case "ROUTE": route = val; break;
            }
        }

        Consume(ExtendedTokenType.CloseBrace, "Expected '}' after query declaration");
        return new AstSituationalQueryDeclaration(incidentId, lat, lon, eventType, radiusKm, substance, route);
    }

    private static double ParseDistanceKm(string dist)
    {
        dist = dist.Trim().ToLowerInvariant();
        if (dist.EndsWith("km") && double.TryParse(dist.Substring(0, dist.Length - 2), NumberStyles.Float, CultureInfo.InvariantCulture, out var km)) return km;
        if (dist.EndsWith("m") && double.TryParse(dist.Substring(0, dist.Length - 1), NumberStyles.Float, CultureInfo.InvariantCulture, out var m)) return m / 1000.0;
        if (dist.EndsWith("mi") && double.TryParse(dist.Substring(0, dist.Length - 2), NumberStyles.Float, CultureInfo.InvariantCulture, out var mi)) return mi * 1.60934;
        if (dist.EndsWith("ft") && double.TryParse(dist.Substring(0, dist.Length - 2), NumberStyles.Float, CultureInfo.InvariantCulture, out var ft)) return ft * 0.0003048;
        if (double.TryParse(dist, NumberStyles.Float, CultureInfo.InvariantCulture, out var plain)) return plain;
        return 10.0;
    }

    private bool Match(ExtendedTokenType type)
    {
        if (Check(type))
        {
            Advance();
            return true;
        }
        return false;
    }

    private bool Check(ExtendedTokenType type) => !IsAtEnd() && Peek().Type == type;
    private ExtendedToken Advance() => !IsAtEnd() ? _tokens[_current++] : Previous();
    private ExtendedToken Peek() => _tokens[_current];
    private ExtendedToken PeekNext() => _current + 1 < _tokens.Count ? _tokens[_current + 1] : _tokens[^1];
    private ExtendedToken Previous() => _tokens[_current - 1];
    private bool IsAtEnd() => Peek().Type == ExtendedTokenType.EndOfFile;

    private ExtendedToken Consume(ExtendedTokenType type, string message)
    {
        if (Check(type)) return Advance();
        throw new InvalidOperationException($"Line {Peek().Line}: {message}. Got '{Peek().Value}' ({Peek().Type})");
    }
}
