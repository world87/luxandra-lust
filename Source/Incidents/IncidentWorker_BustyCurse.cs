using RimWorld;
using rjw;
using rjw.Modules.Interactions;
using System.Linq;
using Verse;
using static LuxandraLust.GameComponent_LuxandraLust;

namespace LuxandraLust
{
    public class IncidentWorker_BustyCurse : IncidentWorker
    {
        private static readonly string HediffDefName = "Luxandra_BustyCurse";

        protected override bool CanFireNowSub(IncidentParms parms)
        {
            if (!LuxandraEventCheck.IsEnabled(LuxandraIncidentDefOf.Luxandra_Inc_BustyCurse.defName))
                return false;

            Map map = (Map)parms.target;
            if (map == null) return false;

            return map.mapPawns.FreeAdultColonistsSpawned.Any(p =>
                p.gender == Gender.Male &&
                LuxandraUtilities.IsAdult(p) &&
                !p.Dead &&
                p.health != null &&
                !p.health.hediffSet.HasHediff(HediffDef.Named(HediffDefName))); // Prevent stacking the hediff
        }

        protected override bool TryExecuteWorker(IncidentParms parms)
        {
            Map map = (Map)parms.target;
            if (map == null) return false;


            HediffDef resizeHediffDef = DefDatabase<HediffDef>.GetNamed(HediffDefName, false);
            if (resizeHediffDef == null)
            {
                Log.Warning("[Luxandra Debug] Def for Luxandra_BustyCurse not found in the database.");
                return false;
            }

            // Find a male colonist
            Pawn targetPawn = map.mapPawns.FreeAdultColonistsSpawned
                .Where(p => p.gender == Gender.Male && LuxandraUtilities.IsAdult(p) && !p.Dead)
                .Where(p => p.health != null && !p.health.hediffSet.HasHediff(resizeHediffDef))
                .OrderBy(p => LuxandraUtilities.GetSexNeed(p).CurLevel)
                .FirstOrDefault();

            if (targetPawn == null) return false;

            // Apply the tracking Hediff to the pawn
            HediffWithComps trackingHediff = HediffMaker.MakeHediff(resizeHediffDef, targetPawn, null) as HediffWithComps;

            targetPawn.health.AddHediff(trackingHediff, null, null, null);

            var disappearComp = trackingHediff.TryGetComp<HediffComp_Disappears>();
            if (disappearComp != null)
            {
                ThoughtDef thought = DefDatabase<ThoughtDef>.GetNamed("Luxandra_BustyCurse", errorOnFail: false);

                // Give the thought
                targetPawn.needs.mood.thoughts.memories.TryGainMemory(thought);
            }

            var letterText = $"{targetPawn.Name} was heard expressing the desire to hold 'a giant pair of tits'. Luxandra must have heard them...";
            this.SendStandardLetter(this.def.letterLabel, letterText, this.def.letterDef, parms, targetPawn);
            if (CurrentKink == StorytellerKink.Breasts || CurrentKink == StorytellerKink.Futa)
            {
                Messages.Message($"Luxandra loves {targetPawn.LabelShort}'s new pair of breasts! She gifts you 1 Favor.", targetPawn, MessageTypeDefOf.NeutralEvent);
                GameComponent_LuxandraLust.Instance?.AddToFavorCounter(1);
            }

            return true;
        }
    }

    public class Hediff_BustyCurse : HediffWithComps
    {
        private const float ChangeAmount = 0.8f;
        private const float MaxSeverity = 5.0f;
        private const float MinSeverity = 0.01f;

        public override void PostAdd(DamageInfo? dinfo)
        {
            base.PostAdd(dinfo);
            LuxandraDebugActions.DebugLogMessage($"Busty Curse applied to {pawn.NameShortColored}.");

            var disappearComp = this.TryGetComp<HediffComp_Disappears>();
            if (disappearComp != null)
            {
                disappearComp.ticksToDisappear = 180000;
            }

            var sexParts = pawn.GetLewdParts();
            if (sexParts == null) return;

            var partsToModify = sexParts.Breasts;
            if (partsToModify.EnumerableNullOrEmpty()) return;

            // There should only be one, but just in case for whatever reason they have 4 nipples...
            for (int i = 0; i < partsToModify.Count; i++)
            {
                var part = partsToModify[i];
                if (part?.SexPart is Hediff_NaturalSexPart naturalPart)
                {
                    float currentSeverity = naturalPart.Severity;

                    // Calculates adjustments trying to not exceed the severity limits
                    float newSeverity = UnityEngine.Mathf.Min(currentSeverity + ChangeAmount, MaxSeverity);

                    if (newSeverity != currentSeverity)
                    {
                        naturalPart.Severity = newSeverity;

                        var comp = part.SexPart.GetPartComp();
                        comp?.SetSeverity(newSeverity, false);

                        LuxandraDebugActions.DebugLogMessage($"Increased {pawn.NameShortColored}'s {naturalPart.def} from {currentSeverity} to {newSeverity}.");
                    }
                }
            }
        }

        // RESTORE OLD SIZES ON EXPIRATION
        public override void PostRemoved()
        {
            base.PostRemoved();

            var sexParts = pawn.GetLewdParts();
            if (sexParts == null) return;

            // Target penises for males, breasts for females
            var partsToModify = sexParts.Breasts;
            if (partsToModify.EnumerableNullOrEmpty()) return;

            for (int i = 0; i < partsToModify.Count; i++)
            {
                var part = partsToModify[i];
                if (part?.SexPart is Hediff_NaturalSexPart naturalPart)
                {
                    float currentSeverity = naturalPart.Severity;
                    // Calculates adjustments trying to not exceed the severity limits
                    float newSeverity = UnityEngine.Mathf.Max(currentSeverity - ChangeAmount, MinSeverity);

                    if (newSeverity != currentSeverity)
                    {
                        naturalPart.Severity = newSeverity;

                        var comp = part.SexPart.GetPartComp();
                        comp?.SetSeverity(newSeverity, false);

                        LuxandraDebugActions.DebugLogMessage($"Restored {pawn.NameShortColored}'s {naturalPart.def} to {newSeverity}.");
                    }
                }
            }

            Messages.Message($"{pawn.LabelShort}'s breasts have reverted back to normal.", pawn, MessageTypeDefOf.NeutralEvent);
        }
    }
}