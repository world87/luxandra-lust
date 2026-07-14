using RimWorld;
using rjw;
using rjw.Modules.Interactions;
using System.Collections.Generic;
using System.Linq;
using Verse;
using static LuxandraLust.GameComponent_LuxandraLust;

namespace LuxandraLust
{
    // Various utilities
    public static class LuxandraUtilities
    {
        /// <summary>
        /// Extracts all incident defs from a the sexual incident collections
        /// </summary>
        public static List<IncidentDef> ExtractIncidentsFromCollection(List<LuxandraIncidentDefs> incidentCollection)
        {
            LuxandraDebugActions.DebugLogMessage($"Extracting incidents from list with {incidentCollection.Count()} incidents.");

            if (incidentCollection == null || incidentCollection.Count() == 0)
                return new List<IncidentDef>();

            LuxandraDebugActions.DebugLogMessage($"Extracting incidents from list with {incidentCollection.Count()} incidents.");
            return incidentCollection.Select(i => i.IncidentDef).ToList();
        }

        /// <summary>
        ///  Gets the sex need of a pawn
        /// </summary>
        /// <param name="pawn"></param>
        public static Need_Sex GetSexNeed(Pawn pawn)
        {
            if (pawn == null || pawn.needs == null) return null;
            var sexNeed = pawn.needs.TryGetNeed<Need_Sex>();
            if (sexNeed != null)
            {
                return sexNeed;
            }
            return null;
        }

        /// <summary>
        /// Determines the average sex need of all the adult colonists and slaves in the colony
        /// </summary>
        public static float GetAverageColonySexNeed(Map map)
        {
            if (map == null) return 0.5f; // Safe fallback

            var eligiblePawns = map.mapPawns.AllPawnsSpawned.Where(p =>
                p.RaceProps != null && p.RaceProps.Humanlike && !p.Dead &&
                (p.IsColonist || p.IsSlave) &&
                IsAdult(p)
            );

            float totalSexNeed = 0f;
            int countWithNeed = 0;

            foreach (Pawn pawn in eligiblePawns)
            {
                var sexNeed = GetSexNeed(pawn);
                if (sexNeed != null)
                {
                    totalSexNeed += sexNeed.CurLevelPercentage;
                    countWithNeed++;
                }
            }

            // Divide by countwithneed rather than actual total of pawns to avoid pawns with no sex need (es, Androids)
            return countWithNeed > 0 ? (totalSexNeed / countWithNeed) : 0.5f;
        }

        /// <summary>
        /// Determines if there's at least 2 player controlled conscious adults on the map
        /// </summary>
        public static bool HasMultipleAdultColonists(Map map)
        {
            if (map == null) return false;

            int freeAdultCount = map.mapPawns.FreeColonistsSpawned.Count(pawn =>
                !pawn.Dead &&
                IsAdult(pawn)
            );

            int slaveAdultCount = map.mapPawns.SlavesOfColonySpawned.Count(pawn =>
                !pawn.Dead &&
                IsAdult(pawn)
            );

            return freeAdultCount + slaveAdultCount > 1;
        }

        /// <summary>
        /// Enlarges a sex part, within a certain limit
        /// </summary>
        public static bool EnlargeSexPart(Pawn pawn, List<RJWLewdablePart> sexParts)
        {
            if (pawn == null || pawn.Dead || sexParts.EnumerableNullOrEmpty()) return false;

            bool anyChanged = false;

            foreach (var part in sexParts)
            {
                if (part?.SexPart is Hediff_NaturalSexPart naturalPart)
                {
                    float currentSeverity = naturalPart.Severity;
                    float changeAmount = 0.25f;

                    // Calculates adjustments trying to not exceed the severity limits
                    float newSeverity = UnityEngine.Mathf.Min(currentSeverity + changeAmount, 5.0f);

                    if (newSeverity != currentSeverity)
                    {
                        naturalPart.Severity = newSeverity;

                        var comp = part.SexPart.GetPartComp();
                        comp?.SetSeverity(newSeverity, sync: false);

                        anyChanged = true;
                        LuxandraDebugActions.DebugLogMessage($"Increased {part.SexPart.Def.defName} size for {pawn.NameShortColored}.");
                    }
                }
            }

            return anyChanged;
        }

        /// <summary>
        /// Forces a RandomRape mental state on the specified pawn
        /// </summary>
        public static void ForceRapistBreak(Pawn pawn, string reasonString, bool tankSexNeed = false)
        {
            if (IsAdult(pawn) == false)
            {
                Log.Warning($"[Luxandra Debug] ForceRapistBreak aborted: {pawn.NameShortColored} is not adult.");
                return;
            }

            if (pawn?.mindState?.mentalStateHandler == null)
            {
                Log.Warning($"[Luxandra Debug] ForceRapistBreak aborted: {pawn.NameShortColored} or mentalStateHandler is null.");
                return;
            }

            MentalStateDef rjwBreakDef = DefDatabase<MentalStateDef>.GetNamed("RandomRape", errorOnFail: false);

            if (rjwBreakDef == null)
            {
                Log.Warning("[Luxandra Debug] Could not trigger Rape mental break: RJW 'RandomRape' MentalStateDef was not found in the game database.");
            }
            else
            {
                pawn.mindState.mentalStateHandler.TryStartMentalState(
                    rjwBreakDef,
                    reason: reasonString,
                    forced: true,
                    forceWake: true // Ensures the pawn snaps out of bed instantly to execute the event
                );

                // If enabled, set their sex need to 0 to ensure they are ready to act on the mental state
                if (tankSexNeed && pawn.needs != null)
                {
                    var sexNeed = GetSexNeed(pawn);
                    if (sexNeed != null)
                    {
                        sexNeed.CurLevel = 0f;
                    }
                }
            }
        }

        /// <summary>
        /// Causes a wet dream in the pawn and wakes them up
        /// </summary>
        public static void CauseWetDream(Pawn targetPawn, Map map)
        {
            // Locate and maximize their sex need
            Need sexNeed = LuxandraUtilities.GetSexNeed(targetPawn);
            if (sexNeed != null)
            {
                sexNeed.CurLevel = sexNeed.MaxLevel;
            }

            // Wake them up immediately!
            if (targetPawn.jobs?.curJob != null && targetPawn.Awake() == false)
            {
                targetPawn.jobs.EndCurrentJob(Verse.AI.JobCondition.InterruptForced);
            }

            // Spawn a bunch of filth around the bed/sleeping spot
            ThingDef filthDef = DefDatabase<ThingDef>.GetNamed("FilthCum", false)
                ?? ThingDefOf.Filth_Slime; // Safe vanilla fallback so the script never breaks

            int filthCount = Rand.RangeInclusive(4, 7);
            IntVec3 centerPos = targetPawn.Position;

            for (int i = 0; i < filthCount; i++)
            {
                // Radial radius of 1 means it spreads to the immediate tiles touching the bed
                if (CellFinder.TryFindRandomReachableNearbyCell(centerPos, map, 1, TraverseParms.For(targetPawn), null, null, out IntVec3 filthCell))
                {
                    int filthPerSpawn = 1;
                    if (LuxandraModSettings.allowFullCumStains) filthPerSpawn = 3;
                    FilthMaker.TryMakeFilth(filthCell, map, filthDef, filthPerSpawn, FilthSourceFlags.Pawn);
                }
            }
            // Add the moodlet
            if (targetPawn.needs?.mood?.thoughts?.memories != null)
            {
                ThoughtDef dreamThought = DefDatabase<ThoughtDef>.GetNamed("Luxandra_WetDreamMoodlet", false);
                if (dreamThought != null)
                {
                    targetPawn.needs.mood.thoughts.memories.TryGainMemory(dreamThought);
                }
            }

            // Add the hediff
            HediffDef dreamDebuff = DefDatabase<HediffDef>.GetNamed("Luxandra_DreamHangover", false);
            if (dreamDebuff != null)
            {
                targetPawn.health.AddHediff(dreamDebuff);
            }

            if (LuxandraModChecks.IsCumpilationActive())
            {
                LuxandraDebugActions.DebugLogMessage($"Cumpilation active: Attempting to make {targetPawn.Name} wet themselves.");
                float cumQuantity = Rand.Range(0.2f, 0.5f);
                CumpilationIntegration.CauseSelfCum(targetPawn, cumQuantity);
            }

            if (CurrentKink == StorytellerKink.Cum && targetPawn.GetPenises().Any())
            {
                Messages.Message($"Luxandra saw {targetPawn.NameShortColored} cum in their sleep. She rewards you with 1 Favor for the show", MessageTypeDefOf.NeutralEvent);
                GameComponent_LuxandraLust.Instance?.AddToFavorCounter(1);
            }
        }

        /// <summary>
        /// Verifies if the sex act that just happened matches the current kink
        /// from the storyteller. (Return nulls if the sex actor is not a player humanlike)
        /// </summary>
        public static bool? DoesSexMatchLuxandraKink(SexProps props)
        {
            if (props == null || props.pawn == null)
                return null;

            Pawn actor = props.pawn;

            // This is now done earlier in the postfix, leaving it here in case i need it
            //if (!actor.RaceProps.Humanlike)
            //    return null;

            //bool isPlayerControlled = actor.Faction == Faction.OfPlayer ||    // Colonists & Mechs
            //                          actor.IsPrisonerOfColony ||             // Prisoners
            //                          actor.IsSlaveOfColony ||                // Slaves
            //                          actor.IsQuestLodger();                  // Quest Guests / Refugees

            //if (!isPlayerControlled)
            //    return null;

            Pawn partner = props.partner;
            StorytellerKink currentKink = CurrentKink;

            string initiatorPartUsed = props.resolved?.InitiatorParts?[0]?.BodyPart?.def?.defName;
            string receiverPartUsed = props.resolved?.RecipientParts?[0]?.BodyPart?.def?.defName;

            LuxandraDebugActions.DebugLogMessage($"Parsing sex. Type: {props.sexType} - {actor.NameShortColored}'s {initiatorPartUsed} and {partner.NameShortColored}'s {receiverPartUsed}");

            switch (currentKink)
            {
                // No kink: always false
                case StorytellerKink.None:
                default:
                    return false;
                // Kink = Pregnancy: vaginal sex, and sex with pregnant women
                case StorytellerKink.Pregnancy:
                    if (props.sexType == xxx.rjwSextype.Vaginal)
                        return true;
                    if (IsPregnant(partner) || IsPregnant(actor))
                        return true;
                    break;
                // Kink = Anal: anal sex, fisting, and rimming
                case StorytellerKink.Anal:
                    if (props.sexType == xxx.rjwSextype.Anal || props.sexType == xxx.rjwSextype.Rimming)
                        return true;
                    if (props.sexType == xxx.rjwSextype.Fisting && (initiatorPartUsed == "Anus" || receiverPartUsed == "Anus"))
                        return true;
                    break;
                // Kink = Oral: blowjobs, cunnilingus, rimming
                case StorytellerKink.Oral:
                    if (props.sexType == xxx.rjwSextype.Oral || props.sexType == xxx.rjwSextype.Cunnilingus || props.sexType == xxx.rjwSextype.Rimming)
                        return true;
                    break;
                // Kink = Bestiality: one of the partecipants is an animal
                case StorytellerKink.Bestiality:
                    if (actor.IsAnimal || partner.IsAnimal)
                        return true;
                    break;
                // Kink = Rape: rape
                case StorytellerKink.Rape:
                    if (props.isRape)
                        return true;
                    break;
                // Kink = Masturbation: masturbation, mutual masturbation does not count
                case StorytellerKink.Masturbation:
                    if (props.sexType == xxx.rjwSextype.Masturbation)
                        return true;
                    break;
                // Kink = Necrophilia: the partner is dead
                case StorytellerKink.Necrophilia:
                    if (partner != null && partner.Dead)
                        return true;
                    break;
                // Kink = Gay: both partners are men
                case StorytellerKink.Gay:
                    if (actor.gender == Gender.Male && partner.gender == Gender.Male && props.sexType != xxx.rjwSextype.Masturbation)
                        return true;
                    break;
                // Kink = Lesbian: both partners are women
                case StorytellerKink.Lesbian:
                    if (actor.gender == Gender.Female && partner.gender == Gender.Female && props.sexType != xxx.rjwSextype.Masturbation)
                        return true;
                    break;
                // Kink = Cum: anything that involves a man and one of part used is a penis
                case StorytellerKink.Cum:
                    if ((actor.gender == Gender.Male && actor.GetPenises().Any() && initiatorPartUsed == "Genitals") || (partner.gender == Gender.Male && partner.GetPenises().Any() && receiverPartUsed == "Genitals"))
                        return true;
                    break;
                // Kink = Breasts: boobjobs and lesbian sex that is not fingering or cunnilingus
                case StorytellerKink.Breasts:
                    if ((actor.gender == Gender.Female && partner.gender == Gender.Female) && props.sexType != xxx.rjwSextype.Fingering && props.sexType != xxx.rjwSextype.Cunnilingus)
                        return true;
                    if (props.sexType == xxx.rjwSextype.Boobjob)
                        return true;
                    break;
                // Kink = Incest: blood relations
                case StorytellerKink.Incest:
                    var directRelations = actor.relations.DirectRelations.Where(r => r.def.familyByBloodRelation);
                    if (directRelations.Any() && directRelations.Any(r => r.otherPawn == partner))
                        return true;
                    break;
                // Kink = Implantation: sex involving female ovipositors
                case StorytellerKink.Implantation:
                    if (props.resolved?.InitiatorParts?[0]?.Family == LewdablePartFamily.FemaleOvipositor || props.resolved?.RecipientParts?[0]?.Family == LewdablePartFamily.FemaleOvipositor)
                        return true;
                    break;
                // Kink = Futa: chicks with dicks, and men with pussyes and/or breasts
                case StorytellerKink.Futa:
                    if (GenderHelper.GetSex(actor) == GenderHelper.Sex.Futa || GenderHelper.GetSex(actor) == GenderHelper.Sex.Trap || GenderHelper.GetSex(partner) == GenderHelper.Sex.Futa || GenderHelper.GetSex(partner) == GenderHelper.Sex.Trap)
                        return true;
                    break;
                // Kink = Mechanophilia: androids, mechs
                case StorytellerKink.Mechanophilia:
                    if ((actor.mechanitor?.OverseenPawns.Count > 0 && partner.IsColonyMech) || actor.RaceProps.IsMechanoid || partner.RaceProps.IsMechanoid || IsAndroid(actor) || IsAndroid(partner))
                        return true;
                    break;
                // Kink = Tentacles: Mimics from onahole, anomalies, pawns have tentacles from anomaly
                case StorytellerKink.Tentacles:
                    if (HasTentacles(actor) || HasTentacles(partner) || actor.IsEntity || partner.IsEntity || actor.def.defName == "Onahole_Mimic" || partner.def.defName == "Onahole_Mimic")
                        return true;
                    break;
            }

            return null;
        }

        /// <summary>
        /// Determines if the pawns are blood related
        /// </summary>
        public static bool ArePawnsBloodRelated(Pawn pA, Pawn pB)
        {
            return pA.relations.FamilyByBlood.Contains(pB);
        }

        /// <summary>
        /// Determines if the pawn is a living adult (or youth if RJW has the check enabled)
        /// </summary>
        public static bool IsAdult(Pawn pawn, bool countAndroids = true)
        {
            if (pawn == null)
                return false;

            // Dodge ghouls and similar things too as they break
            if (pawn.IsGhoul || pawn.IsEntity)
                return false;

            // Androids always count as adults
            if (countAndroids && IsAndroid(pawn))
                return true;

            // TODO: Alien race
            // This lifestage check fucks up sometimes, so i'm just throwing a hard 16+
            // regardless of every other setting.
            if (pawn.ageTracker.AgeBiologicalYears < 16)
                return false;

            var allowYouth = rjw.RJWSettings.AllowYouthSex;

            AgeCategory ageCategory = pawn.GetAgeCategory();

            if (ageCategory == AgeCategory.Adult)
                return true;
            else if (ageCategory == AgeCategory.Youth && allowYouth)
                return true;
            else
                return false;
        }

        /// <summary>
        /// Checks if the pawn is pregnant. Should catch modded pregnancies too
        /// </summary>
        public static bool IsPregnant(Pawn pawn)
        {
            if (pawn.health?.hediffSet == null) return false;

            foreach (Hediff h in pawn.health.hediffSet.hediffs)
            {
                if (h?.def == null) continue;

                var biotechPregnancy = DefDatabase<HediffDef>.GetNamed("PregnantHuman", false);
                var biotechLabor = DefDatabase<HediffDef>.GetNamed("PregnancyLabor", false);
                var biotechLaborPushing = DefDatabase<HediffDef>.GetNamed("PregnancyLaborPushing", false);

                var RJWPregnancy = DefDatabase<HediffDef>.GetNamed("RJW_pregnancy", false);
                var RJWBestialPregnancy = DefDatabase<HediffDef>.GetNamed("RJW_pregnancy_beast", false);
                var RJWMechPregnancy = DefDatabase<HediffDef>.GetNamed("RJW_pregnancy_mech", false);

                var elToroMechPregnancy = DefDatabase<HediffDef>.GetNamed("Hediff_MechCorePregnancy", false);

                var resourcePregnancy = DefDatabase<HediffDef>.GetNamed("PregnancyRJWMR", false);

                var graspbloomPregnancy = DefDatabase<HediffDef>.GetNamed("FA_GraspbloomPregnancy", false);
                var rapenantPregnancy = DefDatabase<HediffDef>.GetNamed("FA_RapenantPregnancy", false);

                HediffDef hediffDef = h.def;
                // Catches Vanilla, Biotech, RJW, and most common pregnancy mod variants
                if (hediffDef == biotechPregnancy || hediffDef == biotechLabor || hediffDef == biotechLaborPushing ||
                    hediffDef == RJWPregnancy || hediffDef == RJWBestialPregnancy || hediffDef == RJWMechPregnancy ||
                    hediffDef == elToroMechPregnancy || hediffDef == resourcePregnancy ||
                    hediffDef == graspbloomPregnancy || hediffDef == rapenantPregnancy)
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Checks if the pawn has the Masochist trait
        /// </summary>
        public static bool IsMasochist(Pawn pawn)
        {
            if (pawn == null) return false;

            TraitDef masochistTraitDef = DefDatabase<TraitDef>.GetNamed("Masochist", errorOnFail: false);
            if (masochistTraitDef != null && pawn.story?.traits != null)
            {
                Trait activeMasochistTrait = pawn.story.traits.allTraits
                    .FirstOrDefault(t => t.def == masochistTraitDef);

                if (activeMasochistTrait != null) return true;
            }

            return false;
        }

        /// <summary>
        /// Checks if it's an android
        /// </summary>
        public static bool IsAndroid(Pawn pawn)
        {
            // TODO: Other "android" types

            // If no biotech, this is false regardless
            if (!ModsConfig.BiotechActive)
                return false;

            GeneDef androidBodyGene = DefDatabase<GeneDef>.GetNamed("VREA_SyntheticBody", false);
            return pawn != null && !pawn.Dead && pawn.genes?.HasActiveGene(androidBodyGene) == true;
        }

        /// <summary>
        /// Determine if the pawn has tentacles so can be classified as tentacle-adjacent for kinks
        /// </summary>
        public static bool HasTentacles(Pawn pawn)
        {
            // TODO: Cases unrelated to anomaly
            if (pawn?.health?.hediffSet == null || !ModsConfig.AnomalyActive)
            {
                return false;
            }

            HediffDef tentacleDef = HediffDefOf.Tentacle;
            HediffDef fleshWhipDef = HediffDefOf.FleshWhip;

            // In case for some reason both defs aren't there
            if (tentacleDef == null && fleshWhipDef == null)
                return false;

            bool tentaclePresent = false;
            bool fleshWhipPresent = false;

            if (tentacleDef != null)
                tentaclePresent = pawn.health.hediffSet.HasHediff(tentacleDef);

            if (fleshWhipDef != null)
                fleshWhipPresent = pawn.health.hediffSet.HasHediff(fleshWhipDef);

            return tentaclePresent || fleshWhipPresent;
        }

        /// <summary>
        /// Counts how many living free colonists on the specified map possess a specific Trait.
        /// </summary>
        public static int CountColonistsWithTraitOnMap(Map map, TraitDef traitDef)
        {
            if (map == null || traitDef == null) return 0;

            int count = 0;
            var localColonists = map.mapPawns.FreeColonists;

            for (int i = 0; i < localColonists.Count; i++)
            {
                Pawn pawn = localColonists[i];
                if (pawn != null && !pawn.Dead && pawn.story?.traits?.HasTrait(traitDef) == true)
                {
                    count++;
                }
            }
            return count;
        }

        /// <summary>
        /// Counts how many living free colonists on the specified map possess a specific Gene.
        /// Returns 0 if the Biotech DLC is not active.
        /// </summary>
        public static int CountColonistsWithGeneOnMap(Map map, GeneDef geneDef)
        {
            if (!ModsConfig.BiotechActive || map == null || geneDef == null) return 0;

            int count = 0;
            var localColonists = map.mapPawns.FreeColonists;

            for (int i = 0; i < localColonists.Count; i++)
            {
                Pawn pawn = localColonists[i];
                if (pawn != null && !pawn.Dead && pawn.genes?.HasActiveGene(geneDef) == true)
                {
                    count++;
                }
            }
            return count;
        }

        /// <summary>
        /// Checks if the player's primary Ideology possesses a specific Precept.
        /// Returns false if the Ideology DLC is not active.
        /// </summary>
        public static bool PlayerFactionHasPrecept(PreceptDef preceptDef)
        {
            if (!ModsConfig.IdeologyActive || preceptDef == null) return false;

            Ideo playerIdeo = Faction.OfPlayer?.ideos?.PrimaryIdeo;
            if (playerIdeo == null) return false;

            var precepts = playerIdeo.PreceptsListForReading;
            for (int i = 0; i < precepts.Count; i++)
            {
                if (precepts[i].def == preceptDef)
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Checks if the player's primary Ideology possesses a specific Meme.
        /// Returns false if the Ideology DLC is not active.
        /// </summary>
        public static bool PlayerFactionHasMeme(MemeDef memeDef)
        {
            // Guard clause: If Ideology isn't active or def is null, bypass completely
            if (!ModsConfig.IdeologyActive || memeDef == null) return false;

            // Get the primary ideology of the player faction
            Ideo playerIdeo = Faction.OfPlayer?.ideos?.PrimaryIdeo;
            if (playerIdeo == null) return false;

            // Use RimWorld's built-in rapid list tracker method to check for the meme
            return playerIdeo.HasMeme(memeDef);
        }

        /// <summary>
        /// Determines the colony support for Bestiality
        /// </summary>
        public static DepravitySupportLevel DetermineBestialitySupport(Map map)
        {
            // Ideology - tecnically this would require sexperience-ideology, but those are going to
            // just be false if it's not loaded anyway. Manages to catch similar mods as well if someone
            // is degenerate enough
            if (ModsConfig.IdeologyActive)
            {
                MemeDef zoophileMeme = DefDatabase<MemeDef>.GetNamed("Zoophile", false);
                PreceptDef bestialityAccepted = DefDatabase<PreceptDef>.GetNamed("Bestiality_Acceptable", false);
                PreceptDef bestialityVenerated = DefDatabase<PreceptDef>.GetNamed("Bestiality_OnlyVenerated", false);
                PreceptDef bestialityBonded = DefDatabase<PreceptDef>.GetNamed("Bestiality_BondOnly", false);
                PreceptDef bestialityHonorable = DefDatabase<PreceptDef>.GetNamed("Bestiality_Honorable", false);

                // TODO: special cases for only venerated, only bonded, and the various bestial pregnancy precepts
                if (PlayerFactionHasPrecept(bestialityHonorable))
                {
                    return DepravitySupportLevel.Required;
                }
                else if (PlayerFactionHasMeme(zoophileMeme) ||
                   PlayerFactionHasPrecept(bestialityAccepted) ||
                   PlayerFactionHasPrecept(bestialityVenerated) ||
                   PlayerFactionHasPrecept(bestialityBonded) ||
                   PlayerFactionHasPrecept(bestialityHonorable))
                {
                    return DepravitySupportLevel.Accepted;
                }
            }

            var localColonists = map.mapPawns.FreeColonists;

            // Biotech - same as above but for RJW genes
            if (ModsConfig.BiotechActive)
            {
                GeneDef zoophileGene = DefDatabase<GeneDef>.GetNamed("rjw_genes_zoophile", false);
                int zoophileGeneColonists = CountColonistsWithGeneOnMap(map, zoophileGene);

                if (zoophileGeneColonists == localColonists.Count)
                    return DepravitySupportLevel.Required;
            }

            // Traits

            TraitDef zoophileTrait = DefDatabase<TraitDef>.GetNamed("Zoophile", false);
            int zoophileColonists = CountColonistsWithTraitOnMap(map, zoophileTrait);

            if (zoophileColonists == localColonists.Count)
                return DepravitySupportLevel.Required;

            return DepravitySupportLevel.Hated;
        }

        /// <summary>
        /// Determines the colony support for Rape
        /// </summary>
        public static DepravitySupportLevel DetermineRapistSupport(Map map)
        {
            if (ModsConfig.IdeologyActive)
            {
                MemeDef rapistMeme = DefDatabase<MemeDef>.GetNamed("Rapist", false);
                PreceptDef rapeAccepted = DefDatabase<PreceptDef>.GetNamed("Rape_Acceptable", false);
                PreceptDef rapeHonorable = DefDatabase<PreceptDef>.GetNamed("Rape_Honorable", false);

                if (PlayerFactionHasPrecept(rapeHonorable))
                {
                    return DepravitySupportLevel.Required;
                }
                else if (PlayerFactionHasMeme(rapistMeme) ||
                   PlayerFactionHasPrecept(rapeAccepted) ||
                   PlayerFactionHasPrecept(rapeHonorable))
                {
                    return DepravitySupportLevel.Required;
                }
            }

            var localColonists = map.mapPawns.FreeColonists;

            // Biotech - same as above but for RJW genes
            if (ModsConfig.BiotechActive)
            {
                GeneDef rapistGene = DefDatabase<GeneDef>.GetNamed("rjw_genes_rapist", false);
                int rapistGeneColonists = CountColonistsWithGeneOnMap(map, rapistGene);

                if (rapistGeneColonists == localColonists.Count)
                    return DepravitySupportLevel.Required;
            }

            // Traits

            TraitDef rapistTrait = DefDatabase<TraitDef>.GetNamed("Rapist", false);
            int rapistColonists = CountColonistsWithTraitOnMap(map, rapistTrait);

            if (rapistColonists == localColonists.Count)
                return DepravitySupportLevel.Required;

            return DepravitySupportLevel.Hated;
        }

        /// <summary>
        /// Determines the colony support for Brothel Colony's Repopulation
        /// </summary>
        public static DepravitySupportLevel DetermineRepopulationSupport(Map map)
        {
            // Ideology + Brothel Colony
            if (ModsConfig.IdeologyActive && LuxandraModChecks.IsBrothelColonyActive())
            {
                MemeDef repopulationMeme = DefDatabase<MemeDef>.GetNamed("CB_Repopulationist", false);
                PreceptDef repopulationKindness = DefDatabase<PreceptDef>.GetNamed("CB_Repopulation_Kindness", false);
                PreceptDef repopulationGreed = DefDatabase<PreceptDef>.GetNamed("CB_Repopulation_Greed", false);
                PreceptDef repopulationDuty = DefDatabase<PreceptDef>.GetNamed("CB_Repopulation_Duty", false);

                if (PlayerFactionHasPrecept(repopulationDuty))
                {
                    return DepravitySupportLevel.Required;
                }
                else if (PlayerFactionHasMeme(repopulationMeme) ||
                   PlayerFactionHasPrecept(repopulationKindness) ||
                   PlayerFactionHasPrecept(repopulationGreed) ||
                   PlayerFactionHasPrecept(repopulationDuty))
                {
                    return DepravitySupportLevel.Accepted;
                }
            }

            return DepravitySupportLevel.Hated;
        }

        /// <summary>
        /// Determines the colony support for incestuous relationships
        /// </summary>
        public static DepravitySupportLevel DetermineIncestSupport(Map map)
        {
            // Ideology
            if (ModsConfig.IdeologyActive)
            {
                PreceptDef incestRequired = DefDatabase<PreceptDef>.GetNamed("Incestuos_IncestOnly", false);
                PreceptDef incestFree = DefDatabase<PreceptDef>.GetNamed("Incestuos_Free", false);
                PreceptDef incestPassable = DefDatabase<PreceptDef>.GetNamed("Incestuos_Disapproved_CloseOnly", false);

                if (PlayerFactionHasPrecept(incestRequired))
                {
                    return DepravitySupportLevel.Required;
                }
                else if (PlayerFactionHasPrecept(incestFree) ||
                   PlayerFactionHasPrecept(incestPassable))
                {
                    return DepravitySupportLevel.Accepted;
                }
            }

            return DepravitySupportLevel.Hated;
        }
    }

    /// <summary>
    /// This class tracks the selected storyteller
    /// </summary>
    public static class LuxandraStorytellerCheck
    {
        /// <summary>
        /// Verifies that Luxandra is active
        /// </summary>
        public static bool IsActive()
        {
            return Find.Storyteller?.def.defName == "LuxandraLust";
        }
    }

    /// <summary>
    /// This class tracks if events are enabled
    /// </summary>
    public static class LuxandraEventCheck
    {
        /// <summary>
        /// Verifies if the event with the provided def is enabled in the settings
        /// </summary>
        public static bool IsEnabled(string defName)
        {
            try
            {
                // Small failsafe just in case
                if (defName == null || defName == "" || defName == " ") return false;

                // This should cover those weird edge cases where this is null
                if (LuxandraEventSettings.disabledEventNames == null || LuxandraEventSettings.disabledEventNames.Empty()) return true;

                return !LuxandraEventSettings.disabledEventNames.Contains(defName);
            }
            catch
            {
                Log.Warning($"[Luxandra Debug] Failed to check if {defName} is enabled. If you see this (expecially more than once) please send a log to the dev.");
                return false;
            }
        }

        /// <summary>
        /// Verifies which of the events with the provided def list are enabled in the settings
        /// </summary>
        public static List<string> EnabledIncidents(List<string> incidentDefs)
        {
            // Small failsafe just in case
            if (incidentDefs == null || incidentDefs.Count == 0) return new List<string>();

            return incidentDefs.Where(i => !LuxandraEventSettings.disabledEventNames.Contains(i)).ToList();
        }

        /// <summary>
        /// Verifies if ANY event is enabled at all of a given type.
        /// Returns true if at least one event is enabled, false if all events are disabled.
        /// </summary>
        public static bool IsAnyEventEnabled(LuxandraIncidentType eventType = LuxandraIncidentType.Any)
        {
            // Safety check in case it's still loading
            if (!LuxandraDefsCollections._isInitialized)
                return false;

            try
            {
                List<LuxandraIncidentDefs> incidentsToCheck = LuxandraDefsCollections.AllIncidents;

                switch (eventType)
                {
                    case LuxandraIncidentType.Positive:
                        incidentsToCheck = LuxandraDefsCollections.PositiveIncidents;
                        break;
                    case LuxandraIncidentType.Negative:
                        incidentsToCheck = LuxandraDefsCollections.NegativeIncidents;
                        break;
                    case LuxandraIncidentType.Neutral:
                        incidentsToCheck = LuxandraDefsCollections.NeutralIncidents;
                        break;
                    case LuxandraIncidentType.Quest:
                        incidentsToCheck = LuxandraDefsCollections.Quests;
                        break;
                    case LuxandraIncidentType.Raid:
                        incidentsToCheck = LuxandraDefsCollections.Raids;
                        break;
                    case LuxandraIncidentType.Any:
                    default:
                        incidentsToCheck = LuxandraDefsCollections.AllIncidents;
                        break;
                }

                return incidentsToCheck.Any(i => !LuxandraEventSettings.disabledEventNames.Contains(i.IncidentDef.defName));
            }
            catch
            {
                Log.Error("[Luxandra Debug] The cached incident collection failed to load. If you see this send a log to the dev.");
                Log.Error($"[Luxandra Debug] Additional note: incidents loaded at the error time = {LuxandraDefsCollections.AllIncidents.Count}");
                return false;
            }
        }
    }
}