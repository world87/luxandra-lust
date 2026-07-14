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
            SoundDefOf.PsychicSootheGlobal.PlayOneShotOnCamera(null);

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
    public class IncidentWorker_FrustrationDrone : IncidentWorker
    {
        protected override bool CanFireNowSub(IncidentParms parms)
        {
            if (!base.CanFireNowSub(parms)) return false;
            Map map = parms.target as Map;
            return map != null;
        }

        protected override bool TryExecuteWorker(IncidentParms parms)
        {
            Map map = parms.target as Map;
            if (map == null) return false;

            // Fetch the negative condition
            GameConditionDef conditionDef = DefDatabase<GameConditionDef>.GetNamed("Luxandra_FrustrationDrone");
            if (conditionDef == null) return false;

            // Apply condition (2 to 4 days)
            int durationTicks = Rand.RangeInclusive(120000, 240000);
            GameCondition condition = GameConditionMaker.MakeCondition(conditionDef, durationTicks);
            map.gameConditionManager.RegisterCondition(condition);

            // Play the negative global sound cue
            SoundDefOf.PsychicPulseGlobal.PlayOneShotOnCamera(null);

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
    }

    // --- Pleasure Wave ---
    public class GameCondition_PleasureWave : GameCondition_SexSatisfactionBase
    {
        protected override float SatisfactionMultiplier => 2.0f; // High Satisfaction
        protected override HediffDef HediffToApply => HediffDef.Named("Luxandra_SexSatisfactionShifted");
    }

    // --- Frustration Drone ---
    public class GameCondition_FrustrationDrone : GameCondition_SexSatisfactionBase
    {
        protected override float SatisfactionMultiplier => 0.5f; // Low Satisfaction
        protected override HediffDef HediffToApply => HediffDef.Named("Luxandra_SexSatisfactionShifted");
    }
}