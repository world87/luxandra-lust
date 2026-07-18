using RimWorld;
using Verse;
using Verse.Sound;

namespace LuxandraLust
{
    // ==========================================
    //       PLEASURE WAVE INCIDENT WORKER
    // ==========================================
    public class IncidentWorker_PleasureWave : IncidentWorker
    {
        protected override bool CanFireNowSub(IncidentParms parms)
        {
            if (!LuxandraEventCheck.IsEnabled(LuxandraIncidentDefOf.Luxandra_Inc_PleasureWave.defName))
                return false;

            if (!base.CanFireNowSub(parms)) return false;
            Map map = parms.target as Map;
            return map != null;
        }

        protected override bool TryExecuteWorker(IncidentParms parms)
        {
            Map map = parms.target as Map;
            if (map == null) return false;

            // Fetch the positive condition
            GameConditionDef conditionDef = DefDatabase<GameConditionDef>.GetNamed("Luxandra_PleasureWave", false);
            if (conditionDef == null) return false;

            // Apply condition (2 to 4 days)
            int durationTicks = Rand.RangeInclusive(120000, 240000);
            GameCondition condition = GameConditionMaker.MakeCondition(conditionDef, durationTicks);
            map.gameConditionManager.RegisterCondition(condition);

            // Play the positive global sound cue
            SoundDefOf.PsychicSootheGlobal.PlayOneShotOnCamera((Map)parms.target);

            Find.LetterStack.ReceiveLetter(
                this.def.letterLabel,
                this.def.letterText,
                this.def.letterDef ?? LetterDefOf.PositiveEvent
            );

            return true;
        }
    }

    // ==========================================
    //     FRUSTRATION DRONE INCIDENT WORKER
    // ==========================================
    public class IncidentWorker_FrustrationWave : IncidentWorker
    {
        protected override bool CanFireNowSub(IncidentParms parms)
        {
            if (!LuxandraEventCheck.IsEnabled(LuxandraIncidentDefOf.Luxandra_Inc_FrustrationWave.defName))
                return false;

            if (!base.CanFireNowSub(parms)) return false;
            Map map = parms.target as Map;
            return map != null;
        }

        protected override bool TryExecuteWorker(IncidentParms parms)
        {
            Map map = parms.target as Map;
            if (map == null) return false;

            // Fetch the negative condition
            GameConditionDef conditionDef = DefDatabase<GameConditionDef>.GetNamed("Luxandra_FrustrationWave");
            if (conditionDef == null) return false;

            // Apply condition (2 to 4 days)
            int durationTicks = Rand.RangeInclusive(120000, 240000);
            GameCondition condition = GameConditionMaker.MakeCondition(conditionDef, durationTicks);
            map.gameConditionManager.RegisterCondition(condition);

            // Play the negative global sound cue
            SoundDefOf.PsychicPulseGlobal.PlayOneShotOnCamera((Map)parms.target);

            Find.LetterStack.ReceiveLetter(
                this.def.letterLabel,
                this.def.letterText,
                this.def.letterDef ?? LetterDefOf.NegativeEvent
            );

            return true;
        }
    }

    // Game condition logic
    public abstract class GameCondition_SexSatisfactionBase : GameCondition
    {
        private const int ScanInterval = 2500;

        protected abstract float SatisfactionMultiplier { get; }
        protected abstract HediffDef HediffToApply { get; }

        public override void GameConditionTick()
        {
            base.GameConditionTick();

            if (Find.TickManager.TicksGame % ScanInterval == 0)
            {
                Map map = this.SingleMap;
                if (map != null && HediffToApply != null)
                {
                    ApplyPulse(map);
                }
            }
        }

        private void ApplyPulse(Map map)
        {
            foreach (Pawn pawn in map.mapPawns.AllHumanlikeSpawned)
            {
                if (pawn == null || pawn.Dead || !pawn.RaceProps.Humanlike) continue;

                Hediff hediff = pawn.health.hediffSet.GetFirstHediffOfDef(HediffToApply);
                if (hediff == null)
                {
                    hediff = pawn.health.AddHediff(HediffToApply);
                }

                // Pass the specific multiplier directly via severity
                hediff.Severity = SatisfactionMultiplier;
            }
        }

        public override void End()
        {
            Map map = this.SingleMap;
            if (map != null && HediffToApply != null)
            {
                foreach (Pawn pawn in map.mapPawns.AllHumanlikeSpawned)
                {
                    if (pawn == null) continue;

                    Hediff hediff = pawn.health.hediffSet.GetFirstHediffOfDef(HediffToApply);
                    if (hediff != null)
                    {
                        pawn.health.RemoveHediff(hediff);
                    }
                }
            }
            base.End();
        }
    }

    // --- Pleasure Wave ---
    public class GameCondition_PleasureWave : GameCondition_SexSatisfactionBase
    {
        protected override float SatisfactionMultiplier => 2.0f; // High Satisfaction
        protected override HediffDef HediffToApply => HediffDef.Named("Luxandra_SexSatisfactionShifted");
    }

    // --- Frustration Drone ---
    public class GameCondition_FrustrationWave : GameCondition_SexSatisfactionBase
    {
        protected override float SatisfactionMultiplier => 0.5f; // Low Satisfaction
        protected override HediffDef HediffToApply => HediffDef.Named("Luxandra_SexSatisfactionShifted");
    }

    // Hediff
    // TODO: Refactor this so i can use it for more stuff
    public class HediffCompProperties_RemoveWithoutCondition : HediffCompProperties
    {
        public HediffCompProperties_RemoveWithoutCondition()
        {
            this.compClass = typeof(HediffComp_RemoveWithoutCondition);
        }
    }

    public class HediffComp_RemoveWithoutCondition : HediffComp
    {
        public override bool CompShouldRemove
        {
            get
            {
                Map map = this.Pawn.Map;

                // If they aren't on a map (e.g. traveling in a caravan), strip the effect
                if (map == null) return true;

                // Check if either of our custom map conditions are active
                bool conditionIsActive = map.gameConditionManager.ConditionIsActive(GameConditionDef.Named("Luxandra_PleasureWave")) ||
                                         map.gameConditionManager.ConditionIsActive(GameConditionDef.Named("Luxandra_FrustrationWave"));

                return !conditionIsActive;
            }
        }
    }
}