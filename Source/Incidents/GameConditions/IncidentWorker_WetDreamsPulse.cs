using RimWorld;
using System.Collections.Generic;
using System.Linq;
using Verse;

namespace LuxandraLust
{
    public class IncidentWorker_WetDreamsPulse : IncidentWorker
    {
        protected override bool TryExecuteWorker(IncidentParms parms)
        {
            Map map = (Map)parms.target;
            if (map == null) return false;


            if (!LuxandraEventCheck.IsEnabled(LuxandraIncidentDefOf.Luxandra_Inc_WetDreamsPulse.defName))
            {
                return false;
            }

            GameConditionDef conditionDef = DefDatabase<GameConditionDef>.GetNamed("Luxandra_WetDreamsPulse", false);
            if (conditionDef == null) return false;

            // Roll duration between 1 and 3 full days (60,000 ticks per day)
            int durationTicks = Rand.RangeInclusive(1, 3) * 60000;

            GameCondition cond = GameConditionMaker.MakeCondition(conditionDef, durationTicks);
            map.gameConditionManager.RegisterCondition(cond);

            // Send a big bad warning letter so the player knows what nightmare is starting
            Find.LetterStack.ReceiveLetter(
                "Psychic Pleasure Surge",
                "An atmospheric phenomenon or localized psychic signature is hyper-activating biological drives across the region. Every few hours, dormant minds will feel severe involuntary overloads. Keep your brothels ready and your cleaners on standby.",
                LetterDefOf.NegativeEvent
            );

            return true;
        }
    }
    public class GameCondition_WetDreamsPulse : GameCondition
    {
        private int nextWetDreamPulseTick = -1;
        private const int PulseIntervalTicks = 7500; // 3 in-game hours

        public override void Init()
        {
            base.Init();
            // Schedule the very first pulse 3 hours after the event starts
            nextWetDreamPulseTick = Find.TickManager.TicksGame + PulseIntervalTicks;
        }

        public override void GameConditionTick()
        {
            base.GameConditionTick();

            // Check if it's time to trigger the wave
            if (Find.TickManager.TicksGame >= nextWetDreamPulseTick)
            {
                TriggerMapWidePulse();
                // Schedule the next pulse
                nextWetDreamPulseTick = Find.TickManager.TicksGame + PulseIntervalTicks;
            }
        }

        private void TriggerMapWidePulse()
        {
            // This condition can technically affect multiple maps if the player has colonies active
            foreach (Map map in AffectedMaps)
            {
                List<Pawn> affectedPawnsInThisPulse = new List<Pawn>();

                // Gather all colonists/slaves currently sleeping with low sex needs
                List<Pawn> candidates = map.mapPawns.AllHumanlikeSpawned
                    .Where(p => p.RaceProps.Humanlike
                                && !p.Dead
                                && p.CurJob != null
                                && p.jobs.curJob.def == JobDefOf.LayDown
                                && !p.Awake()
                                && LuxandraUtilities.IsAdult(p))
                    .Where(p =>
                    {
                        Need sexNeed = LuxandraUtilities.GetSexNeed(p);
                        return sexNeed != null && sexNeed.CurLevelPercentage < 0.50f;
                    })
                    .ToList();

                if (!candidates.Any()) continue;

                // Set up the filth definition safely
                ThingDef filthDef = DefDatabase<ThingDef>.GetNamed("FilthCum", false)
                    ?? ThingDefOf.Filth_Slime; // Safe vanilla fallback so the script never breaks

                foreach (Pawn pawn in candidates)
                {
                    LuxandraUtilities.CauseWetDream(pawn, map);

                    affectedPawnsInThisPulse.Add(pawn);
                }

                // Send alert text
                if (affectedPawnsInThisPulse.Count > 0)
                {
                    Messages.Message(
                        $"{affectedPawnsInThisPulse.Count} colonists woke up gasping from experiencing vivid dreams.",
                        MessageTypeDefOf.NeutralEvent,
                        false
                    );
                }
            }
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref nextWetDreamPulseTick, "nextWetDreamPulseTick", -1);
        }
    }
}