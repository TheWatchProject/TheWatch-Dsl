using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using TheWatch.Domain.Entities;

namespace TheWatch.Dsl;

/// <summary>
/// Domain-Specific Language (DSL) Tokens and AST Definitions for Emergency Response.
/// Syntax Example:
///   ON INCIDENT WHERE Priority == CRITICAL AND Distance <= 500m DISPATCH MEDIC NOTIFY POLICE
/// </summary>
public enum TokenType
{
    On,
    Incident,
    Where,
    And,
    Or,
    Dispatch,
    Notify,
    Identifier,
    StringLiteral,
    NumberLiteral,
    Equals,
    NotEquals,
    LessThan,
    LessThanOrEqual,
    GreaterThan,
    GreaterThanOrEqual,
    EndOfFile
}

public sealed record Token(TokenType Type, string Value, int Position);

public abstract record DslAstNode;

public sealed record DslRuleNode(
    string EventTarget,
    DslConditionNode Condition,
    IReadOnlyList<DslActionNode> Actions
) : DslAstNode;

public abstract record DslConditionNode : DslAstNode;

public sealed record DslBinaryConditionNode(
    string FieldName,
    string Operator,
    string TargetValue
) : DslConditionNode;

public sealed record DslCompoundConditionNode(
    DslConditionNode Left,
    string LogicalOp,
    DslConditionNode Right
) : DslConditionNode;

public sealed record DslActionNode(
    string ActionType,
    string TargetParameter
) : DslAstNode;

public sealed class DslParser
{
    private readonly List<Token> _tokens = new();
    private int _position = 0;

    public DslRuleNode Parse(string script)
    {
        var tokens = Tokenize(script);
        _tokens.Clear();
        _tokens.AddRange(tokens);
        _position = 0;

        Expect(TokenType.On);
        string eventTarget = Expect(TokenType.Identifier).Value;

        DslConditionNode condition = new DslBinaryConditionNode("True", "==", "True");
        if (Match(TokenType.Where))
        {
            condition = ParseCondition();
        }

        var actions = new List<DslActionNode>();
        while (!IsAtEnd())
        {
            if (Match(TokenType.Dispatch))
            {
                string target = Expect(TokenType.Identifier).Value;
                actions.Add(new DslActionNode("DISPATCH", target));
            }
            else if (Match(TokenType.Notify))
            {
                string target = Expect(TokenType.Identifier).Value;
                actions.Add(new DslActionNode("NOTIFY", target));
            }
            else
            {
                _position++;
            }
        }

        return new DslRuleNode(eventTarget, condition, actions);
    }

    private DslConditionNode ParseCondition()
    {
        string field = Expect(TokenType.Identifier).Value;
        string op = ExpectComparisonOperator();
        string value = ExpectLiteralOrIdentifier();

        return new DslBinaryConditionNode(field, op, value);
    }

    private string ExpectComparisonOperator()
    {
        var token = Peek();
        if (token.Type == TokenType.Equals ||
            token.Type == TokenType.LessThan ||
            token.Type == TokenType.LessThanOrEqual ||
            token.Type == TokenType.GreaterThan ||
            token.Type == TokenType.GreaterThanOrEqual ||
            token.Type == TokenType.NotEquals)
        {
            _position++;
            return token.Value;
        }
        throw new InvalidOperationException($"Expected comparison operator at position {token.Position}, got {token.Value}");
    }

    private string ExpectLiteralOrIdentifier()
    {
        var token = Peek();
        if (token.Type == TokenType.Identifier ||
            token.Type == TokenType.StringLiteral ||
            token.Type == TokenType.NumberLiteral)
        {
            _position++;
            return token.Value;
        }
        throw new InvalidOperationException($"Expected literal or identifier at position {token.Position}, got {token.Value}");
    }

    private Token Expect(TokenType expected)
    {
        var token = Peek();
        if (token.Type == expected || (expected == TokenType.Identifier && (token.Type == TokenType.Incident || token.Type == TokenType.Identifier)))
        {
            _position++;
            return token;
        }
        throw new InvalidOperationException($"Expected token {expected} at position {token.Position}, got {token.Type}");
    }

    private bool Match(TokenType type)
    {
        if (Peek().Type == type)
        {
            _position++;
            return true;
        }
        return false;
    }

    private Token Peek() => _position < _tokens.Count ? _tokens[_position] : new Token(TokenType.EndOfFile, "", -1);
    private bool IsAtEnd() => Peek().Type == TokenType.EndOfFile;

    private List<Token> Tokenize(string script)
    {
        var tokens = new List<Token>();
        var parts = script.Split(new[] { ' ', '\t', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);

        int pos = 0;
        foreach (var p in parts)
        {
            var upper = p.ToUpperInvariant();
            var type = upper switch
            {
                "ON" => TokenType.On,
                "INCIDENT" => TokenType.Incident,
                "WHERE" => TokenType.Where,
                "AND" => TokenType.And,
                "OR" => TokenType.Or,
                "DISPATCH" => TokenType.Dispatch,
                "NOTIFY" => TokenType.Notify,
                "==" => TokenType.Equals,
                "!=" => TokenType.NotEquals,
                "<=" => TokenType.LessThanOrEqual,
                ">=" => TokenType.GreaterThanOrEqual,
                "<" => TokenType.LessThan,
                ">" => TokenType.GreaterThan,
                _ => double.TryParse(p, out _) ? TokenType.NumberLiteral : TokenType.Identifier
            };

            tokens.Add(new Token(type, p, pos++));
        }

        tokens.Add(new Token(TokenType.EndOfFile, "", pos));
        return tokens;
    }
}

public sealed class DslExecutionEngine
{
    public sealed record DslExecutionResult(
        bool ConditionMatched,
        IReadOnlyList<string> ExecutedActionDescriptions
    );

    public DslExecutionResult Execute(DslRuleNode rule, Incident incident)
    {
        bool matches = EvaluateCondition(rule.Condition, incident);
        var actions = new List<string>();

        if (matches)
        {
            foreach (var action in rule.Actions)
            {
                actions.Add($"Executed Action '{action.ActionType}' with parameter '{action.TargetParameter}' on Incident {incident.Id}.");
            }
        }

        return new DslExecutionResult(matches, actions);
    }

    private bool EvaluateCondition(DslConditionNode condition, Incident incident)
    {
        if (condition is DslBinaryConditionNode binary)
        {
            if (binary.FieldName.Equals("Priority", StringComparison.OrdinalIgnoreCase))
            {
                return incident.Priority.ToString().Equals(binary.TargetValue, StringComparison.OrdinalIgnoreCase) ||
                       (binary.TargetValue == "CRITICAL" && incident.Priority == IncidentPriority.Critical);
            }
            if (binary.FieldName.Equals("Status", StringComparison.OrdinalIgnoreCase))
            {
                return incident.Status.ToString().Equals(binary.TargetValue, StringComparison.OrdinalIgnoreCase);
            }
            return true;
        }

        return true;
    }
}
