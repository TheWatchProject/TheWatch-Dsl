// <copyright file="WordNetDatabaseLoader.cs" company="The Watch, LLC">
// Copyright (c) 2026 Barton Milnor Mallory, The Watch, LLC. All rights reserved.
// </copyright>

/// <summary>
/// Source file: src/Libraries/TheWatch.Dsl/WordNet/Engine/WordNetDatabaseLoader.cs
/// Module: WordNet 3.0 Lexical Database Loader & In-Memory Semantic Indexer
/// Defines: class WordNetDatabaseLoader, record WordNetSynsetRecord
/// Namespace: TheWatch.Dsl.WordNet.Engine
/// </summary>

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using TheWatch.Dsl.WordNet.Categories;

namespace TheWatch.Dsl.WordNet.Engine;

public sealed record WordNetSynsetRecord(
    string SynsetId,
    int LexFileNum,
    WordNetPos Pos,
    IReadOnlyList<string> Words,
    IReadOnlyList<string> Hypernyms,
    IReadOnlyList<string> Hyponyms,
    string Gloss);

public sealed class WordNetDatabaseLoader
{
    private readonly ILogger<WordNetDatabaseLoader> _logger;
    private readonly ConcurrentDictionary<string, WordNetSynsetRecord> _synsetsById = new(StringComparer.OrdinalIgnoreCase);
    private readonly ConcurrentDictionary<string, List<WordNetSynsetRecord>> _synsetsByLemma = new(StringComparer.OrdinalIgnoreCase);
    private readonly ConcurrentDictionary<int, List<WordNetSynsetRecord>> _synsetsByLexCategory = new();

    public int TotalSynsetsCount => _synsetsById.Count;
    public int TotalLemmasCount => _synsetsByLemma.Count;

    public WordNetDatabaseLoader(ILogger<WordNetDatabaseLoader>? logger = null)
    {
        _logger = logger ?? NullLogger<WordNetDatabaseLoader>.Instance;
        LoadBuiltinEmergencyBaseline();
    }

    public async Task<int> LoadFromDirectoryAsync(string wordNetDictDirectory, CancellationToken cancellationToken = default)
    {
        if (!Directory.Exists(wordNetDictDirectory))
        {
            _logger.LogWarning("WordNet dictionary directory not found at '{Path}'. Using embedded baseline.", wordNetDictDirectory);
            return _synsetsById.Count;
        }

        var dataFiles = new[]
        {
            (Path.Combine(wordNetDictDirectory, "data.noun"), WordNetPos.Noun),
            (Path.Combine(wordNetDictDirectory, "data.verb"), WordNetPos.Verb),
            (Path.Combine(wordNetDictDirectory, "data.adj"), WordNetPos.Adjective),
            (Path.Combine(wordNetDictDirectory, "data.adv"), WordNetPos.Adverb)
        };

        int loadedCount = 0;

        foreach (var (filePath, pos) in dataFiles)
        {
            if (!File.Exists(filePath)) continue;

            using var reader = new StreamReader(filePath);
            string? line;
            while ((line = await reader.ReadLineAsync(cancellationToken)) != null)
            {
                if (string.IsNullOrWhiteSpace(line) || line.StartsWith("  ")) continue; // Skip header license lines

                var record = ParseDataLine(line, pos);
                if (record != null)
                {
                    IndexSynset(record);
                    loadedCount++;
                }
            }
        }

        _logger.LogInformation("Successfully ingested {Count} synsets from WordNet 3.0 dictionary at '{Path}'",
            loadedCount, wordNetDictDirectory);

        return _synsetsById.Count;
    }

    public WordNetSynsetRecord? GetSynsetById(string synsetId)
    {
        _synsetsById.TryGetValue(synsetId, out var record);
        return record;
    }

    public IReadOnlyList<WordNetSynsetRecord> LookupByLemma(string lemma)
    {
        if (_synsetsByLemma.TryGetValue(lemma, out var list))
        {
            return list;
        }
        return Array.Empty<WordNetSynsetRecord>();
    }

    public IReadOnlyList<WordNetSynsetRecord> GetByCategory(int lexFileNumber)
    {
        if (_synsetsByLexCategory.TryGetValue(lexFileNumber, out var list))
        {
            return list;
        }
        return Array.Empty<WordNetSynsetRecord>();
    }

    private void IndexSynset(WordNetSynsetRecord record)
    {
        _synsetsById[record.SynsetId] = record;

        foreach (var word in record.Words)
        {
            _synsetsByLemma.AddOrUpdate(
                word,
                _ => new List<WordNetSynsetRecord> { record },
                (_, list) => { lock (list) { list.Add(record); } return list; });
        }

        _synsetsByLexCategory.AddOrUpdate(
            record.LexFileNum,
            _ => new List<WordNetSynsetRecord> { record },
            (_, list) => { lock (list) { list.Add(record); } return list; });
    }

    private static WordNetSynsetRecord? ParseDataLine(string line, WordNetPos pos)
    {
        try
        {
            var parts = line.Split('|', 2);
            var header = parts[0].Trim();
            var gloss = parts.Length > 1 ? parts[1].Trim() : string.Empty;

            var tokens = header.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (tokens.Length < 4) return null;

            var synsetOffset = tokens[0];
            var lexFileNum = int.Parse(tokens[1]);
            var wordCountHex = tokens[3];
            int wordCount = Convert.ToInt32(wordCountHex, 16);

            var words = new List<string>();
            int idx = 4;
            for (int i = 0; i < wordCount && idx < tokens.Length; i++)
            {
                words.Add(tokens[idx].Replace("_", " ").ToLowerInvariant());
                idx += 2; // skip lex_id
            }

            var hypernyms = new List<string>();
            var hyponyms = new List<string>();

            if (idx < tokens.Length)
            {
                if (int.TryParse(tokens[idx], out int ptrCount))
                {
                    idx++;
                    for (int p = 0; p < ptrCount && idx + 3 < tokens.Length; p++)
                    {
                        var ptrSymbol = tokens[idx];
                        var targetOffset = tokens[idx + 1];
                        var targetPos = tokens[idx + 2];

                        if (ptrSymbol == "@") // Hypernym pointer
                        {
                            hypernyms.Add($"{targetOffset}-{targetPos}");
                        }
                        else if (ptrSymbol == "~") // Hyponym pointer
                        {
                            hyponyms.Add($"{targetOffset}-{targetPos}");
                        }

                        idx += 4;
                    }
                }
            }

            var posCode = pos switch
            {
                WordNetPos.Noun => "n",
                WordNetPos.Verb => "v",
                WordNetPos.Adjective => "a",
                WordNetPos.Adverb => "r",
                _ => "s"
            };

            return new WordNetSynsetRecord(
                SynsetId: $"{synsetOffset}-{posCode}",
                LexFileNum: lexFileNum,
                Pos: pos,
                Words: words,
                Hypernyms: hypernyms,
                Hyponyms: hyponyms,
                Gloss: gloss);
        }
        catch
        {
            return null;
        }
    }

    private void LoadBuiltinEmergencyBaseline()
    {
        var baseline = new List<WordNetSynsetRecord>
        {
            // Artifacts
            new("03247012-n", 6, WordNetPos.Noun, new[] { "drone", "uav" }, new[] { "02688465-n" }, Array.Empty<string>(), "an autonomous or remotely piloted aircraft used for aerial surveillance and tactical payload delivery"),
            new("04050720-n", 6, WordNetPos.Noun, new[] { "radio", "transceiver" }, new[] { "03700549-n" }, Array.Empty<string>(), "electronic equipment used for wireless communication over VHF/UHF tactical bands"),
            new("02741512-n", 6, WordNetPos.Noun, new[] { "ambulance", "medic vehicle" }, new[] { "04524313-n" }, Array.Empty<string>(), "a vehicle that takes people to and from hospitals"),

            // Persons
            new("10512830-n", 18, WordNetPos.Noun, new[] { "responder", "first responder", "paramedic" }, new[] { "10000784-n" }, Array.Empty<string>(), "a person trained in emergency medical and disaster response operations"),
            new("10023405-n", 18, WordNetPos.Noun, new[] { "dispatcher", "operator" }, new[] { "10000784-n" }, Array.Empty<string>(), "a public safety coordinator who directs emergency vehicles and personnel"),
            new("10414457-n", 18, WordNetPos.Noun, new[] { "patient", "victim" }, new[] { "10000784-n" }, Array.Empty<string>(), "a person who is undergoing medical treatment or distress"),

            // Locations
            new("08658087-n", 15, WordNetPos.Noun, new[] { "hospital", "trauma center" }, new[] { "08524735-n" }, Array.Empty<string>(), "a health facility where patients receive medical treatment"),
            new("08654876-n", 15, WordNetPos.Noun, new[] { "shelter", "evacuation center" }, new[] { "08524735-n" }, Array.Empty<string>(), "a protective zone providing safety during emergencies"),

            // Events
            new("07312236-n", 11, WordNetPos.Noun, new[] { "wildfire", "bushfire" }, new[] { "07309999-n" }, Array.Empty<string>(), "an uncontrolled fire in an area of combustible vegetation"),
            new("07315589-n", 11, WordNetPos.Noun, new[] { "flood", "inundation" }, new[] { "07309999-n" }, Array.Empty<string>(), "an overflow of a large amount of water beyond its normal limits"),

            // Motion Verbs
            new("01955835-v", 38, WordNetPos.Verb, new[] { "evacuate", "clear out" }, new[] { "01835496-v" }, Array.Empty<string>(), "move out of an unsafe location into safety"),
            new("01956789-v", 38, WordNetPos.Verb, new[] { "dispatch", "deploy" }, new[] { "01835496-v" }, Array.Empty<string>(), "send away toward a designated emergency destination"),
            new("01948834-v", 38, WordNetPos.Verb, new[] { "patrol", "reconnoiter" }, new[] { "01835496-v" }, Array.Empty<string>(), "maintain the security of an area by periodic inspection"),

            // Communication Verbs
            new("00938567-v", 32, WordNetPos.Verb, new[] { "broadcast", "transmit" }, new[] { "00932802-v" }, Array.Empty<string>(), "disseminate urgent safety information over wireless networks"),

            // Perception Verbs
            new("02106789-v", 39, WordNetPos.Verb, new[] { "detect", "discover" }, new[] { "02105423-v" }, Array.Empty<string>(), "discover the presence of a hazard or acoustic gunshot anomaly")
        };

        foreach (var syn in baseline)
        {
            IndexSynset(syn);
        }
    }
}
