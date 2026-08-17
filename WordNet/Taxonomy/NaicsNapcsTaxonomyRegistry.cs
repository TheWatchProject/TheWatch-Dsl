// <copyright file="NaicsNapcsTaxonomyRegistry.cs" company="The Watch, LLC">
// Copyright (c) 2026 Barton Milnor Mallory, The Watch, LLC. All rights reserved.
// </copyright>

/// <summary>
/// Source file: src/Libraries/TheWatch.Dsl/WordNet/Taxonomy/NaicsNapcsTaxonomyRegistry.cs
/// Module: NAICS & NAPCS Multi-Mapping Knowledge Base & Semantic Classifier
/// Defines: class NaicsNapcsTaxonomyRegistry
/// Namespace: TheWatch.Dsl.WordNet.Taxonomy
/// </summary>

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using TheWatch.Abstractions.Taxonomy;

namespace TheWatch.Dsl.WordNet.Taxonomy;

/// <summary>
/// Authoritative Registry mapping WordNet 3.0 lexical categories, lemmas, and emergency events to multiple NAICS and NAPCS codes.
/// </summary>
public static class NaicsNapcsTaxonomyRegistry
{
    private static readonly Dictionary<string, (List<NaicsClassification> Naics, List<NapcsClassification> Napcs)> LemmaAndEventMappings = new(StringComparer.OrdinalIgnoreCase)
    {
        ["fire"] = (
            new List<NaicsClassification>
            {
                new("922160", "Fire Protection (Fire Suppression & Rescue)", "Public Administration", true, 2.5),
                new("236220", "Commercial and Institutional Building Construction", "Construction", false, 1.2)
            },
            new List<NapcsClassification>
            {
                new("6421111", "Structural fire suppression, ventilation, and perimeter attack", "Fire Protection"),
                new("6421112", "Heavy structural collapse rescue, shoring, and occupant extrication", "Fire Protection")
            }
        ),
        ["wildfire"] = (
            new List<NaicsClassification>
            {
                new("922160", "Fire Protection (Wildland Firefighting)", "Public Administration", true, 2.5),
                new("924120", "Administration of Conservation Programs (Forestry Fuel Management)", "Public Administration", true, 2.0)
            },
            new List<NapcsClassification>
            {
                new("6421111", "Wildland fire suppression, containment firebreaks, and retardant drops", "Fire Protection"),
                new("9221602", "Wildfire rate-of-spread atmospheric tracking and containment", "Public Safety")
            }
        ),
        ["shooting"] = (
            new List<NaicsClassification>
            {
                new("922120", "Police Protection (Tactical SWAT & Active Threat Intervention)", "Public Administration", true, 2.5),
                new("928110", "National Security and Counter-Terrorism Operations", "Public Administration", true, 2.5)
            },
            new List<NapcsClassification>
            {
                new("6411111", "Active shooter tactical intervention and counter-assault", "Law Enforcement"),
                new("6412112", "Hostage rescue, room clearing, and casualty extraction corridor ops", "Public Safety")
            }
        ),
        ["active shooter"] = (
            new List<NaicsClassification>
            {
                new("922120", "Police Protection (Tactical SWAT & Active Threat Intervention)", "Public Administration", true, 2.5),
                new("928110", "National Security and Counter-Terrorism Operations", "Public Administration", true, 2.5)
            },
            new List<NapcsClassification>
            {
                new("6411111", "Active shooter tactical intervention and counter-assault", "Law Enforcement"),
                new("6412112", "Hostage rescue, room clearing, and casualty extraction corridor ops", "Public Safety")
            }
        ),
        ["cardiac arrest"] = (
            new List<NaicsClassification>
            {
                new("621910", "Ambulance Services (Advanced Life Support)", "Health Care", true, 2.5),
                new("622110", "General Medical and Surgical Hospitals (Cardiac Intensive Care)", "Health Care", true, 2.5)
            },
            new List<NapcsClassification>
            {
                new("6431111", "Emergency cardiac life support, ALS resuscitation, and defibrillation", "Healthcare"),
                new("6431112", "Emergency department acute resuscitation and catheterization transfer", "Healthcare")
            }
        ),
        ["hazmat"] = (
            new List<NaicsClassification>
            {
                new("562211", "Hazardous Waste Treatment and Disposal", "Waste Management", true, 2.2),
                new("562910", "Remediation Services (Hazmat Containment)", "Waste Management", true, 2.0)
            },
            new List<NapcsClassification>
            {
                new("5629111", "Chemical neutralization, vapor suppression, and hazardous spill containment", "Environmental"),
                new("5413812", "Toxicological sample spectrometry and chemical identification testing", "Testing Laboratories")
            }
        ),
        ["evacuate"] = (
            new List<NaicsClassification>
            {
                new("485991", "Special Needs Transportation (Mass Evacuation Transit)", "Transportation", false, 1.8),
                new("922190", "Other Justice, Public Order, and Safety Activities", "Public Administration", true, 2.0)
            },
            new List<NapcsClassification>
            {
                new("4859911", "Mass population emergency evacuation transit and corridor transit", "Ground Transport"),
                new("6441115", "Emergency temporary congregate sheltering and refugee intake", "Disaster Services")
            }
        ),
        ["drone"] = (
            new List<NaicsClassification>
            {
                new("336411", "Aircraft and Unmanned Aerial Vehicle (UAV) Manufacturing", "Manufacturing", true, 2.5),
                new("488190", "Other Support Activities for Air Transportation (Autonomous Drone Ops)", "Transportation", true, 2.0)
            },
            new List<NapcsClassification>
            {
                new("3811111", "Unmanned aerial vehicles and tactical autonomous systems", "Defense Systems"),
                new("4881111", "Autonomous aerial surveillance and thermal reconnaissance services", "Aviation Services")
            }
        ),
        ["ambulance"] = (
            new List<NaicsClassification>
            {
                new("621910", "Ambulance Services (Ground and Air Emergency Transit)", "Health Care", true, 2.5),
                new("336212", "Truck Trailer and Emergency Vehicle Manufacturing", "Manufacturing", true, 2.0)
            },
            new List<NapcsClassification>
            {
                new("6432111", "Emergency ground/air ambulance patient evacuation transport", "Healthcare Transport"),
                new("3821111", "Emergency response vehicles and ambulances", "Transportation Equipment")
            }
        ),
        ["hospital"] = (
            new List<NaicsClassification>
            {
                new("622110", "General Medical and Surgical Hospitals (Trauma Centers)", "Health Care", true, 2.5),
                new("621493", "Freestanding Ambulatory Surgical and Emergency Centers", "Health Care", true, 2.2)
            },
            new List<NapcsClassification>
            {
                new("6431111", "Hospital inpatient trauma care and emergency treatment", "Healthcare"),
                new("6431112", "Emergency department acute surgical resuscitation services", "Healthcare")
            }
        )
    };

    private static readonly Dictionary<int, (List<NaicsClassification> Naics, List<NapcsClassification> Napcs)> CategoryMappings = new()
    {
        // 4: noun.act (Operations, interventions)
        [4] = (
            new List<NaicsClassification>
            {
                new("922190", "Other Justice, Public Order, and Safety Activities", "Public Administration", true, 2.0),
                new("541611", "Administrative Management and General Management Consulting", "Professional Services", false, 1.0)
            },
            new List<NapcsClassification>
            {
                new("6441111", "Disaster relief and emergency coordination services", "Emergency Management"),
                new("5416111", "Operational process and triage management services", "Management Services")
            }
        ),

        // 6: noun.artifact (Tactical equipment, drones, vehicles, radios, medical devices)
        [6] = (
            new List<NaicsClassification>
            {
                new("336411", "Aircraft and Unmanned Aerial Vehicle (UAV) Manufacturing", "Manufacturing", true, 2.5),
                new("334220", "Radio and Television Broadcasting and Wireless Communications Equipment", "Manufacturing", true, 2.2),
                new("336212", "Truck Trailer and Emergency Vehicle Manufacturing", "Manufacturing", true, 2.0),
                new("339112", "Surgical and Medical Instrument Manufacturing", "Manufacturing", true, 2.4),
                new("488190", "Other Support Activities for Air Transportation (Drone Fleet Support)", "Transportation", true, 1.8)
            },
            new List<NapcsClassification>
            {
                new("3811111", "Unmanned aerial vehicles and tactical autonomous systems", "Defense Systems"),
                new("3821111", "Emergency response vehicles and ambulances", "Transportation Equipment"),
                new("4821111", "Wireless communications and tactical radio hardware", "Telecommunications"),
                new("3831111", "Medical diagnostic and trauma resuscitation equipment", "Healthcare Products")
            }
        ),

        // 9: noun.cognition (AI decision engines, triage algorithms, situational reasoning)
        [9] = (
            new List<NaicsClassification>
            {
                new("541512", "Computer Systems Design and AI Architecture Services", "Professional Services", true, 2.2),
                new("541715", "Research and Development in Physical, Engineering, and Life Sciences", "Professional Services", true, 2.0)
            },
            new List<NapcsClassification>
            {
                new("5415111", "Custom emergency AI decision support and neural inference systems", "Software Services"),
                new("5417111", "Applied predictive algorithm and swarm intelligence research", "R&D Services")
            }
        ),

        // 11: noun.event (Wildfires, floods, active shooter incidents, storms)
        [11] = (
            new List<NaicsClassification>
            {
                new("922160", "Fire Protection and Wildland Firefighting Services", "Public Administration", true, 2.5),
                new("922120", "Police Protection and Active Threat Neutralization", "Public Administration", true, 2.5),
                new("928110", "National Security and Defense Readiness", "Public Administration", true, 2.5)
            },
            new List<NapcsClassification>
            {
                new("6421111", "Fire prevention, suppression, and wildland containment services", "Public Safety"),
                new("6411111", "Emergency law enforcement and public order incident response", "Public Safety"),
                new("6441111", "Catastrophic event and mass evacuation coordination services", "Disaster Response")
            }
        ),

        // 14: noun.group (Tactical squads, strike teams, search and rescue units)
        [14] = (
            new List<NaicsClassification>
            {
                new("922120", "Police Protection (Tactical SWAT & Special Units)", "Public Administration", true, 2.5),
                new("922160", "Fire Protection (Urban Search and Rescue Task Forces)", "Public Administration", true, 2.5),
                new("561612", "Security Guards and Patrol Services", "Administrative & Support", false, 1.3)
            },
            new List<NapcsClassification>
            {
                new("6412111", "Tactical unit specialized intervention and perimeter security", "Public Safety"),
                new("6441111", "Search, rescue, and technical extraction operations", "Public Safety")
            }
        ),

        // 15: noun.location (Geofence zones, helipads, hospitals, shelters)
        [15] = (
            new List<NaicsClassification>
            {
                new("622110", "General Medical and Surgical Hospitals (Trauma Centers)", "Health Care", true, 2.5),
                new("488119", "Other Airport Operations (Helipads and Heliports)", "Transportation", true, 1.8),
                new("721110", "Hotels and Emergency Congregate Sheltering", "Accommodation", true, 1.5),
                new("541370", "Surveying and Mapping (Geospatial GIS Infrastructure)", "Professional Services", true, 1.9)
            },
            new List<NapcsClassification>
            {
                new("6431111", "Hospital inpatient trauma care and emergency treatment", "Healthcare"),
                new("5413711", "Geospatial mapping, geofencing, and perimeter boundary demarcation", "Geospatial"),
                new("6441111", "Emergency temporary sheltering and refugee assembly facilities", "Disaster Services")
            }
        ),

        // 18: noun.person (First responders, paramedics, dispatchers, civilians, victims)
        [18] = (
            new List<NaicsClassification>
            {
                new("621910", "Ambulance Services (Paramedics & EMTs)", "Health Care", true, 2.5),
                new("922120", "Police Protection (Sworn Officers & Investigators)", "Public Administration", true, 2.5),
                new("922160", "Fire Protection (Firefighters & Rescue Technicians)", "Public Administration", true, 2.5),
                new("561621", "Security Systems Services (911 Emergency Dispatchers)", "Administrative & Support", true, 2.0)
            },
            new List<NapcsClassification>
            {
                new("6431111", "Emergency medical care, paramedic triage, and resuscitation", "Emergency Healthcare"),
                new("6411111", "Police emergency patrol, civil defense, and protective duties", "Law Enforcement"),
                new("6421111", "Fire rescue, hazmat extraction, and structural response", "Fire Protection"),
                new("6441111", "Emergency 911 public safety call routing and dispatch evaluation", "Telecommunications Dispatch")
            }
        ),

        // 27: noun.substance (Hazmat chemicals, toxic gases, decontamination materials, fuel)
        [27] = (
            new List<NaicsClassification>
            {
                new("562211", "Hazardous Waste Treatment and Disposal", "Waste Management", true, 2.2),
                new("325199", "All Other Basic Organic Chemical Manufacturing", "Manufacturing", true, 1.7),
                new("221310", "Water Supply and Irrigation Systems", "Utilities", true, 2.4)
            },
            new List<NapcsClassification>
            {
                new("5629111", "Hazardous chemical remediation, neutralizing, and decontamination", "Environmental"),
                new("2213111", "Potable water purification and emergency supply delivery", "Utilities")
            }
        ),

        // 32: verb.communication (Transmitting, broadcasting alerts, WEA messages, radio calls)
        [32] = (
            new List<NaicsClassification>
            {
                new("517112", "Wireless Telecommunications Carriers (FirstNet Network)", "Information", true, 2.5),
                new("515112", "Radio Stations and Emergency Broadcast Systems", "Information", true, 2.2)
            },
            new List<NapcsClassification>
            {
                new("4821111", "Wireless emergency alert transmission and mesh packet relay", "Telecommunications"),
                new("4822111", "Public safety radio network connectivity and dispatch signaling", "Telecommunications")
            }
        ),

        // 38: verb.motion (Dispatching, evacuating, patrolling, flying, deploying)
        [38] = (
            new List<NaicsClassification>
            {
                new("488190", "Other Support Activities for Air Transportation (Autonomous Flight)", "Transportation", true, 2.0),
                new("621910", "Ambulance Services (Patient Transit & Evacuation)", "Health Care", true, 2.5),
                new("484110", "General Freight Trucking (Logistics Supply Run)", "Transportation", true, 1.6)
            },
            new List<NapcsClassification>
            {
                new("3811111", "Autonomous tactical drone navigation, patrol, and transit", "Aviation Services"),
                new("6432111", "Emergency ground/air ambulance patient evacuation transport", "Healthcare Transport"),
                new("6441111", "Mass population evacuation routing and convoy escort", "Disaster Response")
            }
        ),

        // 39: verb.perception (Acoustic gunshot detection, FLIR thermal scan, radar ping)
        [39] = (
            new List<NaicsClassification>
            {
                new("541380", "Testing Laboratories (Acoustic Gunshot & Ballistic Forensics)", "Professional Services", true, 2.2),
                new("561621", "Security Systems Services (Surveillance & Sensor Monitoring)", "Administrative & Support", true, 2.2)
            },
            new List<NapcsClassification>
            {
                new("5413811", "Acoustic gunshot triangulation and audio spectrum forensic testing", "Testing Services"),
                new("5616211", "Thermal imaging, infrared perimeter scan, and radar telemetry analysis", "Surveillance Services")
            }
        ),

        // 43: verb.weather (Storming, raining, flooding, lightning)
        [43] = (
            new List<NaicsClassification>
            {
                new("541990", "All Other Professional, Scientific, and Technical Services (Meteorological Modeling)", "Professional Services", true, 1.8),
                new("922160", "Fire Protection (Wildland Fire Weather Forensics)", "Public Administration", true, 2.5)
            },
            new List<NapcsClassification>
            {
                new("5419911", "Meteorological storm tracking, flood plume prediction, and wind modeling", "Atmospheric Services"),
                new("6421111", "Wildfire rate-of-spread modeling and severe weather hazard alerts", "Public Safety")
            }
        )
    };

    private static readonly (List<NaicsClassification> Naics, List<NapcsClassification> Napcs) DefaultClassifications = (
        new List<NaicsClassification>
        {
            new("922190", "Other Justice, Public Order, and Safety Activities", "Public Administration", true, 1.5),
            new("541512", "Computer Systems Design Services", "Professional Services", true, 1.5)
        },
        new List<NapcsClassification>
        {
            new("6441111", "Emergency and disaster support service solutions", "Public Safety"),
            new("5415111", "Domain-specific software and ontology processing services", "Information Technology")
        }
    );

    /// <summary>
    /// Resolves all associated NAICS and NAPCS classifications for a given WordNet Lexicographer category and optional lemma.
    /// </summary>
    public static (IReadOnlyList<NaicsClassification> Naics, IReadOnlyList<NapcsClassification> Napcs) GetClassifications(int lexFileNum, string? lemma = null)
    {
        if (!string.IsNullOrWhiteSpace(lemma) && LemmaAndEventMappings.TryGetValue(lemma.Trim(), out var lemmaMapped))
        {
            return (lemmaMapped.Naics.AsReadOnly(), lemmaMapped.Napcs.AsReadOnly());
        }

        if (CategoryMappings.TryGetValue(lexFileNum, out var mapped))
        {
            return (mapped.Naics.AsReadOnly(), mapped.Napcs.AsReadOnly());
        }

        return (DefaultClassifications.Naics.AsReadOnly(), DefaultClassifications.Napcs.AsReadOnly());
    }
}
