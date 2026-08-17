using System;
using System.Collections.Generic;
using System.Linq;

namespace TheWatch.Dsl;

public sealed record ResponderKsaProfile(
    string ResponderId,
    string FullName,
    string OnetSocCode,
    string OccupationTitle,
    string MilitaryCrosswalkCode,
    IReadOnlyList<string> CertifiedSkills,
    IReadOnlyList<string> KnowledgeAreas
);

public sealed record SkillMatchEvaluation(
    string ResponderId,
    string FullName,
    double MatchScore,
    IReadOnlyList<string> MatchedSkills,
    IReadOnlyList<string> MissingSkills,
    bool MeetsMandatoryRequirement
);

/// <summary>
/// O*NET SOC, CIP, and Military Crosswalk (KSA) Skills-Based Matching Engine for Responders.
/// </summary>
public sealed class SkillsAndKsaMatchingEngine
{
    private readonly List<ResponderKsaProfile> _profiles = new();

    public SkillsAndKsaMatchingEngine()
    {
        SeedProfiles();
    }

    public IReadOnlyList<ResponderKsaProfile> AllProfiles => _profiles.AsReadOnly();

    public IReadOnlyList<SkillMatchEvaluation> MatchResponders(
        IReadOnlyList<string> requiredSkills,
        IReadOnlyList<string> requiredKnowledge,
        double minimumMatchScore = 0.50)
    {
        var results = new List<SkillMatchEvaluation>();

        foreach (var p in _profiles)
        {
            var matchedSkills = p.CertifiedSkills.Intersect(requiredSkills, StringComparer.OrdinalIgnoreCase).ToList();
            var missingSkills = requiredSkills.Except(p.CertifiedSkills, StringComparer.OrdinalIgnoreCase).ToList();
            var matchedKnowledge = p.KnowledgeAreas.Intersect(requiredKnowledge, StringComparer.OrdinalIgnoreCase).ToList();

            double totalRequirements = (requiredSkills.Count + requiredKnowledge.Count);
            if (totalRequirements == 0) totalRequirements = 1;

            double score = (matchedSkills.Count * 1.5 + matchedKnowledge.Count) / (totalRequirements * 1.25);
            score = Math.Min(1.0, score);

            bool meetsMandatory = missingSkills.Count == 0;

            if (score >= minimumMatchScore)
            {
                results.Add(new SkillMatchEvaluation(
                    ResponderId: p.ResponderId,
                    FullName: p.FullName,
                    MatchScore: score,
                    MatchedSkills: matchedSkills,
                    MissingSkills: missingSkills,
                    MeetsMandatoryRequirement: meetsMandatory
                ));
            }
        }

        return results.OrderByDescending(r => r.MatchScore).ToList();
    }

    private void SeedProfiles()
    {
        _profiles.AddRange(new[]
        {
            new ResponderKsaProfile(
                ResponderId: "RESP-001",
                FullName: "Capt. Sarah Miller",
                OnetSocCode: "29-2042.00",
                OccupationTitle: "Paramedic (Critical Care)",
                MilitaryCrosswalkCode: "Army 68W (Combat Medic)",
                CertifiedSkills: new[] { "Advanced Cardiac Life Support", "Airway Management", "Triage Scoring", "Trauma Hemostasis" },
                KnowledgeAreas: new[] { "Emergency Medicine", "Pharmacology", "Disaster Life Support", "HIPAA" }
            ),
            new ResponderKsaProfile(
                ResponderId: "RESP-002",
                FullName: "Lt. James Rodriguez",
                OnetSocCode: "33-2011.00",
                OccupationTitle: "Firefighter (Hazmat Specialist)",
                MilitaryCrosswalkCode: "Air Force 3E7X1 (Fire Protection)",
                CertifiedSkills: new[] { "Structural Firefighting", "Hazmat Level-A Entry", "Vehicle Extrication", "Incident Command System" },
                KnowledgeAreas: new[] { "Hazardous Materials", "Building Construction", "Atmospheric Gas Detection" }
            ),
            new ResponderKsaProfile(
                ResponderId: "RESP-003",
                FullName: "Off. David Chen",
                OnetSocCode: "33-3051.00",
                OccupationTitle: "Police Patrol Officer",
                MilitaryCrosswalkCode: "Navy MA (Master-at-Arms)",
                CertifiedSkills: new[] { "Active Threat Neutralization", "Crisis De-escalation", "Scene Perimeter Control", "Evidence Preservation" },
                KnowledgeAreas: new[] { "Criminal Law", "NCIC Systems", "Emergency Vehicle Operation" }
            ),
            new ResponderKsaProfile(
                ResponderId: "RESP-004",
                FullName: "Alex Rivera",
                OnetSocCode: "17-3029.00",
                OccupationTitle: "Commercial UAS Pilot / Autonomous Drone Operator",
                MilitaryCrosswalkCode: "Army 15W (UAS Operator)",
                CertifiedSkills: new[] { "FAA Part 107 BVLOS Operations", "Thermal FLIR Search & Rescue", "Aerial Mesh Relay", "Geospatial Mapping" },
                KnowledgeAreas: new[] { "Aviation Regulations", "Airspace Coordination", "Drone Fleet Telemetry" }
            )
        });
    }
}
