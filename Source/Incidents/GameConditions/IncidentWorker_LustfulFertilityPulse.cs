using RimWorld;
using System.Collections.Generic;
using Verse;
using Verse.Sound;

namespace LuxandraLust
{
    public class IncidentWorker_LustfulFertilityPulse : IncidentWorker_MakeGameCondition
    {
        protected override bool CanFireNowSub(IncidentParms parms)
        {
            Map map = (Map)parms.target;

            if (!LuxandraEventCheck.IsEnabled(LuxandraIncidentDefOf.Luxandra_Inc_LustfulFertilityPulse.defName))
            {
                return false;
            }

            // Don't fire if the weather or condition is already active on this map
            if (map.gameConditionManager.ConditionIsActive(GameConditionDef.Named("Luxandra_WhiteRain")))
            {
                return false;
            }

            return true;
        }
    }

    public class GameCondition_FertilityPulse : GameCondition
    {
        private const int ScanInterval = 2500;

        public override void GameConditionTick()
        {
            base.GameConditionTick();

            if (Find.TickManager.TicksGame % ScanInterval == 0)
            {
                Map map = this.SingleMap;
                if (map != null)
                {
                    ManagePulseEffects(map);
                }
            }
        }

        public override void Init()
        {
            base.Init();

            // Play the low, echoing psychic hum across the map when it triggers
            if (SoundDefOf.PsychicSootheGlobal != null)
            {
                // Finds the primary map affected by this condition and plays it globally for the player
                Map map = this.SingleMap;
                if (map != null)
                {
                    SoundDefOf.PsychicSootheGlobal.PlayOneShotOnCamera(map);
                }
            }
        }

        private void ManagePulseEffects(Map map)
        {
            HediffDef childDef = HediffDef.Named("Luxandra_PulseChildConfusion");
            HediffDef maleDef = HediffDef.Named("Luxandra_PulseAdultMale");
            HediffDef femaleDef = HediffDef.Named("Luxandra_PulseAdultFemale");
            HediffDef pregnantDef = HediffDef.Named("Luxandra_PulsePregnantMaternal");

            foreach (Pawn pawn in map.mapPawns.AllHumanlikeSpawned)
            {
                if (pawn == null || pawn.Dead || !pawn.RaceProps.Humanlike) continue;

                // Ensure they have the correct hediff depending on age/gender
                if (!LuxandraUtilities.IsAdult(pawn))
                {
                    EnsureHediff(pawn, childDef);
                }
                else if (pawn.gender == Gender.Male)
                {
                    EnsureHediff(pawn, maleDef);
                    if (pawn.IsColonist || pawn.IsSlave || pawn.IsPrisoner) ClearFertilitySuppressants(pawn);
                }
                else if (pawn.gender == Gender.Female)
                {
                    if (LuxandraUtilities.IsPregnant(pawn))
                    {
                        EnsureHediff(pawn, pregnantDef);

                        // Remove the previous hediff if they get pregnant during the pulse
                        if (pawn.health.hediffSet.HasHediff(femaleDef))
                        {
                            Hediff oldHediff = pawn.health.hediffSet.GetFirstHediffOfDef(femaleDef);
                            pawn.health.RemoveHediff(oldHediff);
                        }
                    }
                    else
                    {

                        // Clean up the pregnant hediff if a pregnancy somehow ended while the pulse is active
                        if (pawn.health.hediffSet.HasHediff(pregnantDef))
                        {
                            Hediff oldPregnantHediff = pawn.health.hediffSet.GetFirstHediffOfDef(pregnantDef);
                            pawn.health.RemoveHediff(oldPregnantHediff);
                        }

                        EnsureHediff(pawn, femaleDef);
                        if (pawn.IsColonist || pawn.IsSlave || pawn.IsPrisoner) ClearFertilitySuppressants(pawn);

                        // If menstruation is loaded, replenish the ovary power and send the pawn in ovulation (possibly with multiple eggs)
                        if (LuxandraModChecks.IsMenstruationActive())
                        {
                            MenstruationIntegration.InduceOvulationAndRestoreOvaryPower(pawn);
                        }
                    }
                }
            }
        }

        private void EnsureHediff(Pawn pawn, HediffDef def)
        {
            if (!pawn.health.hediffSet.HasHediff(def))
            {
                pawn.health.AddHediff(def);
            }
        }

        private void ClearFertilitySuppressants(Pawn pawn)
        {
            List<Hediff> hediffs = pawn.health.hediffSet.hediffs;

            for (int i = hediffs.Count - 1; i >= 0; i--)
            {
                Hediff h = hediffs[i];
                if (h?.def == null) continue;

                // Skip surgeries, implants, or permanent injuries
                if (h is Hediff_AddedPart || h is Hediff_Implant || h.def.isBad) continue;

                // Hediffs that are tecnically implants but aren't classified as one
                if (h.def == DefDatabase<HediffDef>.GetNamed("ImpregnationBlocker", false) || // (RJW) Archotech pregnancy blocker
                    h.def == DefDatabase<HediffDef>.GetNamed("RJW_IUD", false)) // (RJW) IUD
                    continue;

                // Don't remove the biotech lactating hediff either as that one does tank fertility
                // but we don't want the babies to starve. Modded ones usually do not interfere
                if (ModsConfig.BiotechActive)
                {
                    if (HediffDefOf.Lactating != null && h.def == HediffDefOf.Lactating)
                        continue;
                }

                bool suppressesFertility = false;

                if (h.CurStage != null && EvaluateStageForContraception(h.CurStage))
                {
                    suppressesFertility = true;
                }
                //  Fallback text filter just in case
                else
                {
                    string name = h.def.defName.ToLower();
                    string label = h.def.label?.ToLower() ?? "";

                    if (name.Contains("contraceptive") || name.Contains("birthcontrol") ||
                        name.Contains("anti_fertility") || label.Contains("contraceptive"))
                    {
                        suppressesFertility = true;
                    }
                }

                // If caught, vaporize it from their health tracker
                if (suppressesFertility)
                {
                    pawn.health.RemoveHediff(h);
                    Messages.Message($"{pawn.LabelShort}'s contraceptive protection was faded away due to the fertility pulse!", pawn, MessageTypeDefOf.CautionInput, false);
                }
            }
        }

        private bool EvaluateStageForContraception(HediffStage stage)
        {
            if (stage == null) return false;
            // NOTE: I have to use a word check since RJW stuff targets RJW_Fertility and Biotech targets Fertility
            // and I want to catch both

            if (stage.capMods != null)
            {
                foreach (PawnCapacityModifier capMod in stage.capMods)
                {
                    if (capMod.capacity != null)
                    {
                        string capName = capMod.capacity.defName.ToLower();

                        // If the capacity is a fertility stat and it enforces a maximum cap under 50%
                        if (capName.Contains("fertility") && capMod.setMax < 0.5f)
                        {
                            return true;
                        }
                    }
                }
            }

            if (stage.statOffsets != null)
            {
                foreach (StatModifier modifier in stage.statOffsets)
                {
                    if (modifier.stat != null)
                    {
                        string statName = modifier.stat.defName.ToLower();
                        if (statName.Contains("fertility") && modifier.value <= -0.5f)
                        {
                            return true;
                        }
                    }
                }
            }

            if (stage.statFactors != null)
            {
                foreach (StatModifier factor in stage.statFactors)
                {
                    if (factor.stat != null)
                    {
                        string statName = factor.stat.defName.ToLower();
                        if (statName.Contains("fertility") && factor.value <= 0.5f)
                        {
                            return true;
                        }
                    }
                }
            }

            return false;
        }

        // Clean up everything when the condition ends
        public override void End()
        {
            Map map = this.SingleMap;
            if (map != null)
            {
                HediffDef childDef = HediffDef.Named("Luxandra_PulseChildConfusion");
                HediffDef maleDef = HediffDef.Named("Luxandra_PulseAdultMale");
                HediffDef femaleDef = HediffDef.Named("Luxandra_PulseAdultFemale");
                HediffDef pregnantDef = HediffDef.Named("Luxandra_PulsePregnantMaternal");

                // Using a traditional for loop or a cached collection is safer here 
                // since we are dynamically mutating the hediff lists inside the iteration
                var spawnedPawns = map.mapPawns.AllPawnsSpawned;
                for (int i = 0; i < spawnedPawns.Count; i++)
                {
                    Pawn pawn = spawnedPawns[i];
                    if (pawn == null || !pawn.RaceProps.Humanlike) continue;

                    // Bulk-removal tool to ensure all hediffs are removed
                    pawn.health.hediffSet.hediffs.RemoveAll(h =>
                        h.def == childDef || h.def == maleDef || h.def == femaleDef || h.def == pregnantDef
                    );
                }
            }
            base.End();
        }
    }

    public class ThoughtWorker_MalePulseOpinion : ThoughtWorker
    {
        protected override ThoughtState CurrentSocialStateInternal(Pawn pawn, Pawn otherPawn)
        {
            // Must be an adult male with the hediff
            if (pawn.gender != Gender.Male || pawn.health?.hediffSet?.HasHediff(DefDatabase<HediffDef>.GetNamed("Luxandra_PulseAdultMale")) == false)
                return false;

            // The person they are looking at must be female
            if (otherPawn.gender == Gender.Female)
            {
                return true; // Apply the opinion boost/shift towards her!
            }

            return false;
        }
    }

    // Females under the pulse viewing males
    public class ThoughtWorker_FemalePulseOpinion : ThoughtWorker
    {
        protected override ThoughtState CurrentSocialStateInternal(Pawn pawn, Pawn otherPawn)
        {
            if (pawn.gender != Gender.Female || pawn.health?.hediffSet?.HasHediff(DefDatabase<HediffDef>.GetNamed("Luxandra_PulseAdultFemale")) == false)
                return false;

            if (otherPawn.gender == Gender.Male)
            {
                return true;
            }

            return false;
        }
    }

    // Hediff management to remove it when the pawns leave the map
    public class HediffCompProperties_FertilityPulseCheck : HediffCompProperties
    {
        public GameConditionDef conditionDef;

        public HediffCompProperties_FertilityPulseCheck()
        {
            this.compClass = typeof(HediffComp_FertilityPulseCheck);
        }
    }

    public class HediffComp_FertilityPulseCheck : HediffComp
    {
        public HediffCompProperties_FertilityPulseCheck Props => (HediffCompProperties_FertilityPulseCheck)this.props;

        // Match the condition's scan interval for perfect alignment and maximum performance
        private const int CheckInterval = 2500;

        public override void CompPostTick(ref float severityAdjustment)
        {
            base.CompPostTick(ref severityAdjustment);

            if (Pawn.IsHashIntervalTick(CheckInterval))
            {
                Map currentMap = Pawn.Map;

                // If the pawn isn't on a map (e.g., traveling in a gravship), do nothing yet.
                if (currentMap == null) return;

                // If they are on a map, but the condition is NOT running on this map remove it
                if (Props.conditionDef != null && !currentMap.gameConditionManager.ConditionIsActive(Props.conditionDef))
                {
                    Pawn.health.RemoveHediff(this.parent);
                }
            }
        }
    }
}