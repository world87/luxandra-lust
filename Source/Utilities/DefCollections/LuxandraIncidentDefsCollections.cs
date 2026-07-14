using RimWorld;
using System.Collections.Generic;
using System.Linq;
using Verse;

namespace LuxandraLust
{
    /// <summary>
    /// Defines the type of incident, for categorization and filtering purposes.
    /// </summary>
    public enum LuxandraIncidentType
    {
        Any = 0, // used as default value
        Positive,
        Negative,
        Neutral,
        Quest,
        Raid
    }

    // This file contains mostly the quick references to the defs, as well as the categorization of every
    // event compatible with the mod.

    [DefOf]
    public static class LuxandraIncidentDefOf
    {
        // Auras, pulses and similar stuff
        public static IncidentDef Luxandra_Inc_HornyRushFemale;
        public static IncidentDef Luxandra_Inc_HornyRushMale;
        public static IncidentDef Luxandra_Inc_WhiteRain;

        public static IncidentDef Luxandra_Inc_LustfulFertilityPulse;
        public static IncidentDef Luxandra_Inc_FertilityPulseSite;
        public static IncidentDef Luxandra_Inc_FertilityPulseMechCluster;

        public static IncidentDef Luxandra_Inc_WetDreamsPulse;
        public static IncidentDef Luxandra_Inc_WetDreamsPulseSite;
        public static IncidentDef Luxandra_Inc_WetDreamsPulseMechCluster;

        public static IncidentDef Luxandra_Inc_PleasureWave;
        public static IncidentDef Luxandra_Inc_FrustrationWave;
        public static IncidentDef Luxandra_Inc_FrustrationWaveMechCluster;

        public static IncidentDef Luxandra_Inc_RapistBreak;

        // Raids
        public static IncidentDef Luxandra_Inc_HornyTribalRaid;
        public static IncidentDef Luxandra_Inc_DeviantHordeRaid;
        public static IncidentDef Luxandra_Inc_DeviantHungerSwarm;
        public static IncidentDef Luxandra_Inc_AmazonStealthAmbush;
        public static IncidentDef Luxandra_Inc_AmazonBloodTrial;
        public static IncidentDef Luxandra_Inc_StarvingAmazons;
        [MayRequireRoyalty]
        public static IncidentDef Luxandra_Inc_InquisitionPurgeSappers;

        public static IncidentDef Luxandra_Inc_TheMilkGame;

        // Disease, illnesses
        public static IncidentDef Luxandra_Inc_AphrodisiacFever;
        public static IncidentDef Luxandra_Inc_IntimateInfestation;

        // Body part messing
        public static IncidentDef Luxandra_Inc_MaleExpansion;
        public static IncidentDef Luxandra_Inc_FemaleExpansion;
        public static IncidentDef Luxandra_Inc_MaleReduction;
        public static IncidentDef Luxandra_Inc_FemaleReduction;
        [MayRequireRoyalty]
        public static IncidentDef Luxandra_Inc_RoyalLuxury;
        [MayRequireRoyalty]
        public static IncidentDef Luxandra_Inc_RoyalDepravity;
        [MayRequireIdeology]
        public static IncidentDef Luxandra_Inc_IdeoLeaderBlessing;
        [MayRequireIdeology]
        public static IncidentDef Luxandra_Inc_IdeoLeaderDepravity;

        // Boons
        public static IncidentDef Luxandra_Inc_LustfulSupplies;
        public static IncidentDef Luxandra_Inc_ThrallPodCrash;
        public static IncidentDef Luxandra_Inc_BustyCurse;

        // Quests incidents
        public static IncidentDef Luxandra_Inc_BreakPrisonersContractQuest;

        // Other random stuff
        public static IncidentDef Luxandra_Inc_ForbiddenLove;
        public static IncidentDef Luxandra_Inc_DivineConception;
        public static IncidentDef Luxandra_Inc_ForbiddenConception;
        public static IncidentDef Luxandra_Inc_WetDreamIncident;
        public static IncidentDef Luxandra_Inc_StripQuake;
        public static IncidentDef Luxandra_Inc_CumMeteor;

        static LuxandraIncidentDefOf()
        {
            DefOfHelper.EnsureInitializedInCtor(typeof(LuxandraIncidentDefOf));
        }
    }

    public static class LuxandraDefsCollections
    {
        public static bool _isInitialized = false;

        // Cached list of all incidents managed by the mod
        // For usage in the rest of the mod
        private static readonly List<LuxandraIncidentDefs> _allIncidents = new List<LuxandraIncidentDefs>();
        private static List<LuxandraIncidentDefs> _positiveIncidents = new List<LuxandraIncidentDefs>();
        private static List<LuxandraIncidentDefs> _negativeIncidents = new List<LuxandraIncidentDefs>();
        private static List<LuxandraIncidentDefs> _negativeIncidentsNoRaids = new List<LuxandraIncidentDefs>();
        private static List<LuxandraIncidentDefs> _neutralIncidents = new List<LuxandraIncidentDefs>();
        private static List<LuxandraIncidentDefs> _neutralIncidentsNoQuests = new List<LuxandraIncidentDefs>();
        private static List<LuxandraIncidentDefs> _quests = new List<LuxandraIncidentDefs>();
        private static List<LuxandraIncidentDefs> _raids = new List<LuxandraIncidentDefs>();

        /// <summary>
        /// All incidents available. Safe-checked to ensure data is loaded.
        /// </summary>
        public static List<LuxandraIncidentDefs> AllIncidents
        {
            get
            {
                if (!_isInitialized)
                {
                    Log.Error("[Luxandra Lust] Notice: A system tried to read the incident database before initialization finished. If the game is still booting up, DO NOT PANIC—this will resolve itself automatically once loading completes. Please share this full log with the dev.");
                }
                return _allIncidents;
            }
        }

        /// <summary>
        /// Incidents that provide benefits to the player
        /// </summary>
        public static List<LuxandraIncidentDefs> PositiveIncidents => _positiveIncidents;

        /// <summary>
        /// Incidents that harm or challenge the player (including raids)
        /// </summary>
        public static List<LuxandraIncidentDefs> NegativeIncidents => _negativeIncidents;

        /// <summary>
        /// Incidents that harm or challenge the player (excluding raids)
        /// </summary>
        public static List<LuxandraIncidentDefs> NegativeIncidentsNoRaids => _negativeIncidentsNoRaids;

        /// <summary>
        /// Incidents that are not strictly good nor bad (including quests)
        /// </summary>
        public static List<LuxandraIncidentDefs> NeutralIncidents => _neutralIncidents;

        /// <summary>
        /// Incidents that are not strictly good nor bad (excluding quests)
        /// </summary>
        public static List<LuxandraIncidentDefs> NeutralIncidentsNoQuests => _neutralIncidentsNoQuests;

        /// <summary>
        /// Quests
        /// </summary>
        public static List<LuxandraIncidentDefs> Quests => _quests;

        /// <summary>
        /// Incidents that cause raids
        /// </summary>
        public static List<LuxandraIncidentDefs> Raids => _raids;

        #region Debug stuff
        /// <summary>
        /// Reset the event pool and recalculate it
        /// </summary>
        public static void ReinitializeIncidentPool()
        {
            // Flip the flag so systems know we are in a mutating state
            _isInitialized = false;

            // Clear the master list contents (handles the readonly constraint safely)
            _allIncidents.Clear();

            // Wipe out the cached sub-lists by allocating empty lists
            _positiveIncidents = new List<LuxandraIncidentDefs>();
            _negativeIncidents = new List<LuxandraIncidentDefs>();
            _negativeIncidentsNoRaids = new List<LuxandraIncidentDefs>();
            _neutralIncidents = new List<LuxandraIncidentDefs>();
            _neutralIncidentsNoQuests = new List<LuxandraIncidentDefs>();
            _quests = new List<LuxandraIncidentDefs>();
            _raids = new List<LuxandraIncidentDefs>();

            InizializeLuxandraIncidents();
            Log.Message($"[Luxandra Debug] Manual cache invalidation triggered. Active incident pools have been rebuilt based on current kink preferences.");
            PrintLuxandraIncidentTotals();

            Messages.Message("Luxandra incident pools successfully recalculated!", MessageTypeDefOf.TaskCompletion, false);
        }

        /// <summary>
        /// Prints the amount of available incident per type
        /// </summary>
        public static void PrintLuxandraIncidentTotals()
        {
            Log.Message($"[Luxandra Lust] found {AllIncidents.Count} lustful events.");

            LuxandraDebugActions.DebugLogMessage($"Positive incidents: {PositiveIncidents.Count}");
            LuxandraDebugActions.DebugLogMessage($"Neutral incidents: {NeutralIncidents.Count} (including {Quests.Count} quests)");
            LuxandraDebugActions.DebugLogMessage($"Negative incidents: {NegativeIncidents.Count} (including {Raids.Count} raids)");
            LuxandraDebugActions.DebugLogMessage($"TOTAL: {AllIncidents.Count} incidents available.");
        }
        #endregion

        public static void InizializeLuxandraIncidents()
        {
            // Prevent double-initialization just in case
            if (_isInitialized) return;

            var temporaryList = new List<LuxandraIncidentDefs>();

            #region Luxandra's base events
            temporaryList.Add(new LuxandraIncidentDefs(
                incidentDef: LuxandraIncidentDefOf.Luxandra_Inc_HornyRushFemale,
                incidentType: LuxandraIncidentType.Positive,
                description: "Luxandra_Inc_HornyRushFemale_Desc".Translate(),
                pointBaseCost: 15
            ));

            temporaryList.Add(new LuxandraIncidentDefs(
                incidentDef: LuxandraIncidentDefOf.Luxandra_Inc_HornyRushMale,
                incidentType: LuxandraIncidentType.Positive,
                description: "Luxandra_Inc_HornyRushMale_Desc".Translate(),
                pointBaseCost: 15
            ));

            temporaryList.Add(new LuxandraIncidentDefs(
                incidentDef: LuxandraIncidentDefOf.Luxandra_Inc_WhiteRain,
                incidentType: LuxandraIncidentType.Negative,
                description: "Luxandra_Inc_WhiteRain_Desc".Translate(),
                pointBaseCost: 40,
                kinks: new[] { StorytellerKink.Cum, StorytellerKink.Rape }
            ));

            temporaryList.Add(new LuxandraIncidentDefs(
                incidentDef: LuxandraIncidentDefOf.Luxandra_Inc_LustfulFertilityPulse,
                incidentType: LuxandraIncidentType.Neutral,
                description: "Luxandra_Inc_LustfulFertilityPulse_Desc".Translate(),
                pointBaseCost: 55,
                kinks: new[] { StorytellerKink.Pregnancy }
            ));

            temporaryList.Add(new LuxandraIncidentDefs(
                incidentDef: LuxandraIncidentDefOf.Luxandra_Inc_FertilityPulseSite,
                incidentType: LuxandraIncidentType.Negative,
                description: "Luxandra_Inc_FertilityPulseSite_Desc".Translate(),
                kinks: new[] { StorytellerKink.Pregnancy }
            ));

            temporaryList.Add(new LuxandraIncidentDefs(
                incidentDef: LuxandraIncidentDefOf.Luxandra_Inc_FertilityPulseMechCluster,
                incidentType: LuxandraIncidentType.Raid,
                description: "Luxandra_Inc_FertilityPulseMechCluster_Desc".Translate(),
                pointBaseCost: 45,
                kinks: new[] { StorytellerKink.Pregnancy }
            ));

            temporaryList.Add(new LuxandraIncidentDefs(
                incidentDef: LuxandraIncidentDefOf.Luxandra_Inc_WetDreamsPulse,
                incidentType: LuxandraIncidentType.Neutral,
                description: "Luxandra_Inc_WetDreamsPulse_Desc".Translate(),
                pointBaseCost: 55,
                kinks: new[] { StorytellerKink.Cum }
            ));

            temporaryList.Add(new LuxandraIncidentDefs(
                incidentDef: LuxandraIncidentDefOf.Luxandra_Inc_WetDreamsPulseSite,
                incidentType: LuxandraIncidentType.Negative,
                description: "Luxandra_Inc_WetDreamsPulseSite_Desc".Translate(),
                kinks: new[] { StorytellerKink.Cum }
            ));

            temporaryList.Add(new LuxandraIncidentDefs(
                incidentDef: LuxandraIncidentDefOf.Luxandra_Inc_WetDreamsPulseMechCluster,
                incidentType: LuxandraIncidentType.Raid,
                description: "Luxandra_Inc_WetDreamsPulseMechCluster_Desc".Translate(),
                pointBaseCost: 55,
                kinks: new[] { StorytellerKink.Cum }
            ));

            temporaryList.Add(new LuxandraIncidentDefs(
                incidentDef: LuxandraIncidentDefOf.Luxandra_Inc_PleasureWave,
                incidentType: LuxandraIncidentType.Positive,
                description: "Luxandra_Inc_PleasureWave_Desc".Translate(),
                pointBaseCost: 20
            ));

            temporaryList.Add(new LuxandraIncidentDefs(
                incidentDef: LuxandraIncidentDefOf.Luxandra_Inc_FrustrationWave,
                incidentType: LuxandraIncidentType.Negative,
                description: "Luxandra_Inc_FrustrationWave_Desc".Translate(),
                pointBaseCost: 20
            ));

            temporaryList.Add(new LuxandraIncidentDefs(
                incidentDef: LuxandraIncidentDefOf.Luxandra_Inc_FrustrationWaveMechCluster,
                incidentType: LuxandraIncidentType.Raid,
                description: "Luxandra_Inc_FrustrationWaveMechCluster_Desc".Translate(),
                pointBaseCost: 55
            ));

            temporaryList.Add(new LuxandraIncidentDefs(
                incidentDef: LuxandraIncidentDefOf.Luxandra_Inc_RapistBreak,
                incidentType: LuxandraIncidentType.Negative,
                description: "Luxandra_Inc_RapistBreak_Desc".Translate(),
                kinks: new[] { StorytellerKink.Rape }
            ));

            temporaryList.Add(new LuxandraIncidentDefs(
                incidentDef: LuxandraIncidentDefOf.Luxandra_Inc_HornyTribalRaid,
                incidentType: LuxandraIncidentType.Raid,
                description: "Luxandra_Inc_HornyTribalRaid_Desc".Translate(),
                pointBaseCost: 35,
                kinks: new[] { StorytellerKink.Rape }
             ));

            temporaryList.Add(new LuxandraIncidentDefs(
                incidentDef: LuxandraIncidentDefOf.Luxandra_Inc_DeviantHordeRaid,
                incidentType: LuxandraIncidentType.Raid,
                description: "Luxandra_Inc_DeviantHordeRaid_Desc".Translate(),
                pointBaseCost: 50,
                kinks: new[] { StorytellerKink.Cum, StorytellerKink.Rape }
            ));

            temporaryList.Add(new LuxandraIncidentDefs(
                incidentDef: LuxandraIncidentDefOf.Luxandra_Inc_DeviantHungerSwarm,
                incidentType: LuxandraIncidentType.Raid,
                description: "Luxandra_Inc_DeviantHungerSwarm_Desc".Translate(),
                pointBaseCost: 35
            ));

            temporaryList.Add(new LuxandraIncidentDefs(
                incidentDef: LuxandraIncidentDefOf.Luxandra_Inc_AmazonStealthAmbush,
                incidentType: LuxandraIncidentType.Raid,
                description: "Luxandra_Inc_AmazonStealthAmbush_Desc".Translate()
            ));

            temporaryList.Add(new LuxandraIncidentDefs(
                incidentDef: LuxandraIncidentDefOf.Luxandra_Inc_AmazonBloodTrial,
                incidentType: LuxandraIncidentType.Raid,
                description: "Luxandra_Inc_AmazonBloodTrial_Desc".Translate(),
                pointBaseCost: 35
            ));

            temporaryList.Add(new LuxandraIncidentDefs(
                incidentDef: LuxandraIncidentDefOf.Luxandra_Inc_StarvingAmazons,
                incidentType: LuxandraIncidentType.Raid,
                description: "Luxandra_Inc_StarvingAmazons_Desc".Translate(),
                pointBaseCost: 40
            ));

            temporaryList.Add(new LuxandraIncidentDefs(
                incidentDef: LuxandraIncidentDefOf.Luxandra_Inc_TheMilkGame,
                incidentType: LuxandraIncidentType.Neutral,
                description: "Luxandra_Inc_TheMilkGame_Desc".Translate(),
                pointBaseCost: 40
            ));

            temporaryList.Add(new LuxandraIncidentDefs(
                incidentDef: LuxandraIncidentDefOf.Luxandra_Inc_AphrodisiacFever,
                incidentType: LuxandraIncidentType.Negative,
                description: "Luxandra_Inc_AphrodisiacFever_Desc".Translate(),
                pointBaseCost: 15
            ));

            temporaryList.Add(new LuxandraIncidentDefs(
                incidentDef: LuxandraIncidentDefOf.Luxandra_Inc_MaleExpansion,
                incidentType: LuxandraIncidentType.Positive,
                description: "Luxandra_Inc_MaleExpansion_Desc".Translate(),
                pointBaseCost: 25
            ));

            temporaryList.Add(new LuxandraIncidentDefs(
                incidentDef: LuxandraIncidentDefOf.Luxandra_Inc_FemaleExpansion,
                incidentType: LuxandraIncidentType.Positive,
                description: "Luxandra_Inc_FemaleExpansion_Desc".Translate(),
                pointBaseCost: 25,
                kinks: new[] { StorytellerKink.Breasts }
            ));

            temporaryList.Add(new LuxandraIncidentDefs(
                incidentDef: LuxandraIncidentDefOf.Luxandra_Inc_MaleReduction,
                incidentType: LuxandraIncidentType.Negative,
                description: "Luxandra_Inc_MaleReduction_Desc".Translate(),
                pointBaseCost: 15
            ));

            temporaryList.Add(new LuxandraIncidentDefs(
                incidentDef: LuxandraIncidentDefOf.Luxandra_Inc_FemaleReduction,
                incidentType: LuxandraIncidentType.Negative,
                description: "Luxandra_Inc_FemaleReduction_Desc".Translate(),
                pointBaseCost: 15,
                kinks: new[] { StorytellerKink.Breasts }
            ));

            temporaryList.Add(new LuxandraIncidentDefs(
                incidentDef: LuxandraIncidentDefOf.Luxandra_Inc_LustfulSupplies,
                incidentType: LuxandraIncidentType.Positive,
                description: "Luxandra_Inc_LustfulSupplies_Desc".Translate(),
                pointBaseCost: 55
            ));

            temporaryList.Add(new LuxandraIncidentDefs(
                incidentDef: LuxandraIncidentDefOf.Luxandra_Inc_ThrallPodCrash,
                incidentType: LuxandraIncidentType.Positive,
                description: "Luxandra_Inc_ThrallPodCrash_Desc".Translate()
            ));

            temporaryList.Add(new LuxandraIncidentDefs(
                incidentDef: LuxandraIncidentDefOf.Luxandra_Inc_BustyCurse,
                incidentType: LuxandraIncidentType.Positive,
                description: "Luxandra_Inc_BustyCurse_Desc".Translate(),
                pointBaseCost: 15,
                kinks: new[] { StorytellerKink.Breasts, StorytellerKink.Futa }
            ));

            temporaryList.Add(new LuxandraIncidentDefs(
                incidentDef: LuxandraIncidentDefOf.Luxandra_Inc_BreakPrisonersContractQuest,
                incidentType: LuxandraIncidentType.Quest,
                description: "Luxandra_Inc_BreakPrisonersContractQuest_Desc".Translate(),
                pointBaseCost: 40,
                kinks: new[] { StorytellerKink.Rape }
            ));

            temporaryList.Add(new LuxandraIncidentDefs(
                incidentDef: LuxandraIncidentDefOf.Luxandra_Inc_ForbiddenLove,
                incidentType: LuxandraIncidentType.Neutral,
                description: "Luxandra_Inc_ForbiddenLove_Desc".Translate(),
                pointBaseCost: 50,
                kinks: new[] { StorytellerKink.Incest, StorytellerKink.Pregnancy }
            ));

            temporaryList.Add(new LuxandraIncidentDefs(
                incidentDef: LuxandraIncidentDefOf.Luxandra_Inc_DivineConception,
                incidentType: LuxandraIncidentType.Positive,
                description: "Luxandra_Inc_DivineConception_Desc".Translate(),
                pointBaseCost: 75,
                kinks: new[] { StorytellerKink.Pregnancy }
            ));

            temporaryList.Add(new LuxandraIncidentDefs(
                incidentDef: LuxandraIncidentDefOf.Luxandra_Inc_ForbiddenConception,
                incidentType: LuxandraIncidentType.Neutral,
                description: "Luxandra_Inc_ForbiddenConception_Desc".Translate(),
                pointBaseCost: 75,
                kinks: new[] { StorytellerKink.Pregnancy, StorytellerKink.Bestiality }
            ));

            temporaryList.Add(new LuxandraIncidentDefs(
                incidentDef: LuxandraIncidentDefOf.Luxandra_Inc_WetDreamIncident,
                incidentType: LuxandraIncidentType.Neutral,
                description: "Luxandra_Inc_WetDreamIncident_Desc".Translate(),
                pointBaseCost: 5,
                kinks: new[] { StorytellerKink.Cum }
            ));

            temporaryList.Add(new LuxandraIncidentDefs(
                incidentDef: LuxandraIncidentDefOf.Luxandra_Inc_StripQuake,
                incidentType: LuxandraIncidentType.Neutral,
                description: "Luxandra_Inc_StripQuake_Desc".Translate(),
                pointBaseCost: 100
            ));

            temporaryList.Add(new LuxandraIncidentDefs(
                incidentDef: LuxandraIncidentDefOf.Luxandra_Inc_CumMeteor,
                incidentType: LuxandraIncidentType.Negative,
                description: "Luxandra_Inc_CumMeteor_Desc".Translate(),
                pointBaseCost: 20,
                kinks: new[] { StorytellerKink.Cum }
            ));
            #endregion

            #region Royalty
            if (ModsConfig.RoyaltyActive)
            {
                temporaryList.Add(new LuxandraIncidentDefs(
                    incidentDef: LuxandraIncidentDefOf.Luxandra_Inc_RoyalLuxury,
                    incidentType: LuxandraIncidentType.Positive,
                    description: "Luxandra_Inc_RoyalLuxury_Desc".Translate(),
                    requiresMod: true,
                    modRequired: "Royalty",
                    pointBaseCost: 35
                ));

                temporaryList.Add(new LuxandraIncidentDefs(
                    incidentDef: LuxandraIncidentDefOf.Luxandra_Inc_RoyalDepravity,
                    incidentType: LuxandraIncidentType.Negative,
                    description: "Luxandra_Inc_RoyalDepravity_Desc".Translate(),
                    requiresMod: true,
                    modRequired: "Royalty",
                    kinks: new[] { StorytellerKink.Rape }
                ));

                temporaryList.Add(new LuxandraIncidentDefs(
                    incidentDef: LuxandraIncidentDefOf.Luxandra_Inc_InquisitionPurgeSappers,
                    incidentType: LuxandraIncidentType.Raid,
                    description: "Luxandra_Inc_InquisitionPurgeSappers_Desc".Translate(),
                    requiresMod: true,
                    modRequired: "Royalty"
                ));
            }
            #endregion

            #region Ideology
            if (ModsConfig.IdeologyActive)
            {
                temporaryList.Add(new LuxandraIncidentDefs(
                    incidentDef: LuxandraIncidentDefOf.Luxandra_Inc_IdeoLeaderBlessing,
                    incidentType: LuxandraIncidentType.Positive,
                    description: "Luxandra_Inc_IdeoLeaderBlessing_Desc".Translate(),
                    requiresMod: true,
                    modRequired: "Ideology",
                    pointBaseCost: 35
                ));

                temporaryList.Add(new LuxandraIncidentDefs(
                    incidentDef: LuxandraIncidentDefOf.Luxandra_Inc_IdeoLeaderDepravity,
                    incidentType: LuxandraIncidentType.Negative,
                    description: "Luxandra_Inc_IdeoLeaderDepravity_Desc".Translate(),
                    requiresMod: true,
                    modRequired: "Ideology",
                    kinks: new[] { StorytellerKink.Rape }
                ));
            }
            #endregion

            #region RimJobWorld
            IncidentDef nymphJoins = DefDatabase<IncidentDef>.GetNamed("NymphJoins", false);
            temporaryList.Add(new LuxandraIncidentDefs(
                incidentDef: nymphJoins,
                incidentType: LuxandraIncidentType.Positive,
                requiresMod: true,
                modRequired: "RimJobWorld",
                pointBaseCost: 35
            ));

            IncidentDef nymphVisitor = DefDatabase<IncidentDef>.GetNamed("NymphVisitor", false);
            temporaryList.Add(new LuxandraIncidentDefs(
                incidentDef: nymphVisitor,
                incidentType: LuxandraIncidentType.Neutral,
                requiresMod: true,
                modRequired: "RimJobWorld"
            ));
            #endregion

            #region Brothel Colony Quests
            if (LuxandraModChecks.IsBrothelColonyQuestsActive())
            {
                IncidentDef brothelQuest = DefDatabase<IncidentDef>.GetNamed("RJWBCQ_GiveQuest_BrothelCustomer", false);
                temporaryList.Add(new LuxandraIncidentDefs(
                   incidentDef: brothelQuest,
                   incidentType: LuxandraIncidentType.Quest,
                   requiresMod: true,
                   modRequired: "Brothel Colony Quests",
                   pointBaseCost: 45
               ));

                IncidentDef brothelQuestBig = DefDatabase<IncidentDef>.GetNamed("RJWBCQ_GiveQuest_BrothelCustomer_Big", false);
                temporaryList.Add(new LuxandraIncidentDefs(
                   incidentDef: brothelQuestBig,
                   incidentType: LuxandraIncidentType.Quest,
                   requiresMod: true,
                   modRequired: "Brothel Colony Quests",
                   pointBaseCost: 55
               ));

                IncidentDef brothelQuestExtreme = DefDatabase<IncidentDef>.GetNamed("RJWBCQ_GiveQuest_BrothelCustomer_Extreme", false);
                temporaryList.Add(new LuxandraIncidentDefs(
                   incidentDef: brothelQuestExtreme,
                   incidentType: LuxandraIncidentType.Quest,
                   requiresMod: true,
                   modRequired: "Brothel Colony Quests",
                   pointBaseCost: 65
               ));
            }
            #endregion

            #region RJW Events
            if (LuxandraModChecks.IsRJWEventsActive())
            {
                IncidentDef psychicArouse = DefDatabase<IncidentDef>.GetNamed("PsychicArouse", false);
                temporaryList.Add(new LuxandraIncidentDefs(
                   incidentDef: psychicArouse,
                   incidentType: LuxandraIncidentType.Negative,
                   requiresMod: true,
                   modRequired: "RJW Events",
                   pointBaseCost: 25
               ));
            }
            #endregion

            #region RJW Genes
            if (LuxandraModChecks.IsRJWGenesActive())
            {
                IncidentDef psychicArouse = DefDatabase<IncidentDef>.GetNamed("SuccubusDreamVisit", false);
                temporaryList.Add(new LuxandraIncidentDefs(
                   incidentDef: psychicArouse,
                   incidentType: LuxandraIncidentType.Neutral,
                   requiresMod: true,
                   modRequired: "RJW Genes"
               ));
            }
            #endregion

            #region RJW Insects
            if (LuxandraModChecks.IsRJWInsectsActive())
            {
                temporaryList.Add(new LuxandraIncidentDefs(
                    incidentDef: LuxandraIncidentDefOf.Luxandra_Inc_IntimateInfestation,
                    incidentType: LuxandraIncidentType.Negative,
                    description: "Luxandra_Inc_IntimateInfestation_Desc".Translate(),
                    requiresMod: true,
                    pointBaseCost: 35,
                    kinks: new[] { StorytellerKink.Implantation, StorytellerKink.Bestiality }
                ));
            }
            #endregion

            #region Unleashed Bastards
            if (LuxandraModChecks.IsUnleashedBastardsActive())
            {
                IncidentDef unleashedBastardsRaid = DefDatabase<IncidentDef>.GetNamed("Luxandra_Inc_UnleashedBastardsRaid", false);
                temporaryList.Add(new LuxandraIncidentDefs(
                    incidentDef: unleashedBastardsRaid,
                    incidentType: LuxandraIncidentType.Raid,
                    description: "Luxandra_Inc_UnleashedBastardsRaid_Desc".Translate(),
                    requiresMod: true,
                    kinks: new[] { StorytellerKink.Rape }
                ));
            }
            #endregion

            #region Anomaly
            if (ModsConfig.AnomalyActive)
            {
                #region Forbidden Anomalies
                if (LuxandraModChecks.IsForbiddenAnomaliesActive())
                {
                    IncidentDef rapenant = DefDatabase<IncidentDef>.GetNamed("FA_RapenantIncident", false);
                    temporaryList.Add(new LuxandraIncidentDefs(
                       incidentDef: rapenant,
                       incidentType: LuxandraIncidentType.Negative,
                       requiresMod: true,
                       modRequired: "Forbidden Anomalies"
                    ));

                    IncidentDef graspBloom = DefDatabase<IncidentDef>.GetNamed("FA_GraspbloomSpawn", false);
                    temporaryList.Add(new LuxandraIncidentDefs(
                       incidentDef: graspBloom,
                       incidentType: LuxandraIncidentType.Negative,
                       requiresMod: true,
                       modRequired: "Forbidden Anomalies",
                       pointBaseCost: 35
                    ));
                }
                #endregion
            }
            #endregion

            LuxandraDebugActions.DebugLogMessage($"{temporaryList.Count} parsed. Checking kinks...");
            var incidentWithMissingDefs = temporaryList.Where(i => i.IncidentDef == null || i.IncidentDef.defName == "");
            if (incidentWithMissingDefs.Any())
            {
                Log.Warning($"[Luxandra Debug] Warning: {incidentWithMissingDefs.Count()} events had missing defs. If this shown in your game, please contact the dev. He probably messed up.");
                temporaryList = temporaryList.Where(i => i.IncidentDef != null && i.IncidentDef.defName != "").ToList();
            }

            int incidentsDisabledCauseKinks = 0;
            if (LuxandraModSettings.enableLogging)
            {
                var enabledKinks = LuxandraKinkTracker.GetEnabledKinks();
                var listOfEnabledKinks = string.Join(" - ", enabledKinks);

                LuxandraDebugActions.DebugLogMessage($"{enabledKinks.Count} kinks enabled: {listOfEnabledKinks}");
            }

            foreach (var incident in temporaryList)
            {
                bool shouldBeEnabled = AreRelatedKinksEnabled(incident);
                if (shouldBeEnabled)
                {
                    _allIncidents.Add(incident);
                }
                else
                {
                    LuxandraDebugActions.DebugLogMessage($"Disabling {incident.IncidentDef.defName} since the necessary kink(s) are not enabled");
                    incidentsDisabledCauseKinks++;
                }
            }

            if (incidentsDisabledCauseKinks > 0)
                Log.Message($"Disabled {incidentsDisabledCauseKinks} events due to detected kink conditions not found");

            // Populate the remaining caches
            _positiveIncidents = _allIncidents.Where(i => i.IncidentType == LuxandraIncidentType.Positive).ToList();
            _negativeIncidents = _allIncidents.Where(i => i.IncidentType == LuxandraIncidentType.Negative || i.IncidentType == LuxandraIncidentType.Raid).ToList();
            _negativeIncidentsNoRaids = _allIncidents.Where(i => i.IncidentType == LuxandraIncidentType.Negative).ToList();
            _neutralIncidents = _allIncidents.Where(i => i.IncidentType == LuxandraIncidentType.Neutral || i.IncidentType == LuxandraIncidentType.Quest).ToList();
            _neutralIncidentsNoQuests = _allIncidents.Where(i => i.IncidentType == LuxandraIncidentType.Neutral).ToList();
            _quests = _allIncidents.Where(i => i.IncidentType == LuxandraIncidentType.Quest).ToList();
            _raids = _allIncidents.Where(i => i.IncidentType == LuxandraIncidentType.Raid).ToList();

            // Initialization successful
            _isInitialized = true;
        }

        public static bool AreRelatedKinksEnabled(LuxandraIncidentDefs incident)
        {
            var tags = incident.AssociatedKinks;

            if (tags.Contains(StorytellerKink.Bestiality) && !LuxandraKinkTracker.IsBestialityEnabled())
                return false;

            if (tags.Contains(StorytellerKink.Rape) && !LuxandraKinkTracker.IsRapeEnabled())
                return false;

            if (tags.Contains(StorytellerKink.Necrophilia) && !LuxandraKinkTracker.IsNecrophiliaEnabled())
                return false;

            if (tags.Contains(StorytellerKink.Implantation) && !LuxandraKinkTracker.IsImplantationEnabled())
                return false;

            if (tags.Contains(StorytellerKink.Incest) && !LuxandraKinkTracker.IsIncestEnabled())
                return false;

            if (tags.Contains(StorytellerKink.Futa) && !LuxandraKinkTracker.IsFutaEnabled())
                return false;

            if (tags.Contains(StorytellerKink.Mechanophilia) && !LuxandraKinkTracker.IsMechanophiliaEnabled())
                return false;

            if (tags.Contains(StorytellerKink.Tentacles) && !LuxandraKinkTracker.IsTentaclePornEnabled())
                return false;

            return true;
        }
    }

    public class LuxandraIncidentDefs
    {
        /// <summary>
        /// The actual def
        /// </summary>
        public IncidentDef IncidentDef { get; set; }

        /// <summary>
        /// Type of incident
        /// </summary>
        public LuxandraIncidentType IncidentType { get; set; }

        /// <summary>
        /// Short description of the event
        /// </summary>
        public string ShortDescription { get; set; }

        /// <summary>
        /// Base cost to be bought from event selectors (null = can't be bought)
        /// </summary>
        public decimal? PointBaseCost { get; set; }

        /// <summary>
        /// Does this require another mod to work?
        /// </summary>
        public bool RequiresMod { get; set; }

        /// <summary>
        /// Mod the event is from
        /// </summary>
        public string ModRequired { get; set; }

        /// <summary>
        /// What kinks are associated with the event
        /// </summary>
        public StorytellerKink[] AssociatedKinks { get; }

        public LuxandraIncidentDefs(IncidentDef incidentDef, LuxandraIncidentType incidentType, string description = "", decimal? pointBaseCost = null, bool requiresMod = false, string modRequired = "", StorytellerKink[] kinks = null)
        {
            this.IncidentDef = incidentDef;
            this.IncidentType = incidentType;
            this.ShortDescription = description;
            this.PointBaseCost = pointBaseCost;
            this.RequiresMod = requiresMod;
            this.ModRequired = modRequired;
            this.AssociatedKinks = kinks ?? new StorytellerKink[0];
        }
    }
}
