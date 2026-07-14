using RimWorld;
using rjw;
using System.Collections.Generic;
using System.Linq;
using Verse;
using static LuxandraLust.GameComponent_LuxandraLust;

namespace LuxandraLust
{
    public class IncidentWorker_WhiteRain : IncidentWorker
    {
        protected override bool CanFireNowSub(IncidentParms parms)
        {
            Map map = (Map)parms.target;

            if (!LuxandraEventCheck.IsEnabled(LuxandraIncidentDefOf.Luxandra_Inc_WhiteRain.defName))
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

        protected override bool TryExecuteWorker(IncidentParms parms)
        {
            Map map = (Map)parms.target;

            GameConditionDef conditionDef = GameConditionDef.Named("Luxandra_WhiteRain");
            WeatherDef weatherDef = WeatherDef.Named("Luxandra_Rain_White");

            // Random duration for the storm (2 to 5 days)
            int duration = Rand.Range(120000, 300000);

            GameCondition gameCondition = GameConditionMaker.MakeCondition(conditionDef, duration);
            map.gameConditionManager.RegisterCondition(gameCondition);

            map.weatherManager.TransitionTo(weatherDef);

            if (this.def.letterDef != null)
            {
                string letterText = string.Format(this.def.letterText, map.Parent.Label).CapitalizeFirst();
                Find.LetterStack.ReceiveLetter(this.def.letterLabel, letterText, this.def.letterDef);
            }

            return true;
        }
    }

    public class GameCondition_WhiteRain : GameCondition_ForceWeather
    {
        private const int TickBuildupInterval = 300;
        private List<Pawn> tempPawnList = new List<Pawn>();

        public override WeatherDef ForcedWeather()
        {
            return WeatherDef.Named("Luxandra_Rain_White");
        }

        public override void GameConditionTick()
        {
            base.GameConditionTick();

            // --- Apply Hediff Buildup (Every 5s) ---
            if (Find.TickManager.TicksGame % TickBuildupInterval == 0)
            {
                Map map = this.SingleMap;
                if (map != null)
                {
                    ApplyBuildupToMap(map);
                }
            }
        }
        public override void End()
        {
            base.End();

            // Clear out the permanent weather by forcing the map to pick a new, normal vanilla weather pattern
            if (this.SingleMap != null)
            {
                this.SingleMap.weatherDecider.StartNextWeather();
            }
        }

        private void ApplyBuildupToMap(Map map)
        {
            // --- Map-wide Random Rain Drop Spawning ---
            ThingDef filthDef = DefDatabase<ThingDef>.GetNamedSilentFail("Luxandra_FilthCumRain");

            if (LuxandraModSettings.allowFullCumStains && filthDef != null)
            {
                int dropsToSpawn = Rand.Range(20, 30);

                for (int d = 0; d < dropsToSpawn; d++)
                {
                    IntVec3 randomCell = CellFinder.RandomCell(map);

                    // Only drop if it's outdoors, inside map bounds, and not on a solid wall/deep water
                    if (!randomCell.Roofed(map) && randomCell.WalkableByAny(map))
                    {
                        int cumToSpawn = Rand.Range(1, 3);

                        // Use RimWorld's native optimized filth spawner utility
                        FilthMaker.TryMakeFilth(randomCell, map, filthDef, cumToSpawn);
                    }
                }
            }

            // --- Pawn Buildup Logic ---
            tempPawnList.Clear();
            tempPawnList.AddRange(map.mapPawns.AllPawnsSpawned);

            HediffDef buildupDef = HediffDef.Named("Luxandra_WhiteRainBuildup");

            // Skip this step for non-humans and non-colonyanimals if there's too many animals on the map to save on performance
            bool skipAnimals = tempPawnList.Where(p => !p.IsHumanLike() && !p.IsColonyAnimal).Count() > 50;

            for (int i = tempPawnList.Count - 1; i >= 0; i--)
            {
                Pawn pawn = tempPawnList[i];

                // Check if they are outdoors and alive
                if (pawn != null && !pawn.Dead && pawn.Spawned && !pawn.Position.Roofed(map))
                {
                    bool skipCurrentPawn = !pawn.IsHumanLike() && !pawn.IsColonyMech && !pawn.IsColonyAnimal && skipAnimals;

                    // Drop a mess right under their feet as they walk in the rain!
                    if (!skipCurrentPawn && filthDef != null && pawn.Position.InBounds(map) && pawn.Position.WalkableByAny(map))
                    {
                        FilthMaker.TryMakeFilth(pawn.Position, map, filthDef, count: 1);
                    }

                    // Only tick the hediff on humanlikes
                    if (pawn.RaceProps.Humanlike)
                    {
                        // Get the current severity BEFORE adjusting it (default to 0f if they don't have it yet)
                        Hediff existingHediff = pawn.health.hediffSet.GetFirstHediffOfDef(buildupDef);
                        float severityBefore = existingHediff != null ? existingHediff.Severity : 0f;

                        HealthUtility.AdjustSeverity(pawn, buildupDef, 0.015f);

                        // If cumpilation is active: strat drenching the pawn in cum;
                        if (LuxandraModChecks.IsCumpilationActive())
                            CumpilationIntegration.DrenchInCumFromNothing(pawn, 0.1f);

                        // Get the new severity AFTER the adjustment
                        Hediff activeHediff = pawn.health.hediffSet.GetFirstHediffOfDef(buildupDef);
                        if (activeHediff != null)
                        {
                            float severityAfter = activeHediff.Severity;

                            // Check if the tick pushed them past the specific 25%, 50%, or 75% thresholds
                            bool crossedMilestone = severityBefore < 0.50f && severityAfter >= 0.50f;

                            if (crossedMilestone)
                            {
                                if (CurrentKink == StorytellerKink.Cum)
                                {
                                    Messages.Message($"Luxandra saw {pawn.NameShortColored} being drenched in cum and is enjoying it. She rewards you with 1 Favor.", MessageTypeDefOf.NeutralEvent);
                                    GameComponent_LuxandraLust.Instance?.AddToFavorCounter(1);
                                }
                            }

                            // Check for the 100% mental break snap
                            if (severityAfter >= 1.0f)
                            {
                                TriggerMentalBreak(pawn, activeHediff);
                            }
                        }
                    }
                }
            }

            tempPawnList.Clear();
        }

        private void TriggerMentalBreak(Pawn pawn, Hediff hediff)
        {
            // Only adults should actually trigger the primary target break
            if (LuxandraUtilities.IsAdult(pawn))
            {
                MentalStateDef rapistBreak = DefDatabase<MentalStateDef>.GetNamed("RandomRape", false);
                if (rapistBreak != null && pawn.mindState.mentalStateHandler.TryStartMentalState(rapistBreak, null, true))
                {
                    if (CurrentKink == StorytellerKink.Cum || CurrentKink == StorytellerKink.Rape)
                    {
                        Messages.Message($"Luxandra saw {pawn.NameShortColored} snap after being drenched in cum and loved it. She rewards your bravery...or insanity... with 10 Favor.", MessageTypeDefOf.NeutralEvent);
                        GameComponent_LuxandraLust.Instance?.AddToFavorCounter(10);
                    }

                    Messages.Message(pawn.LabelCap + " has snapped from exposure to the white rain!", pawn, MessageTypeDefOf.NegativeEvent);
                    pawn.health.RemoveHediff(hediff); // Clear it so it stops checking
                }
            }
            // Kids go hide in their room instead
            else
            {
                if (pawn.mindState.mentalStateHandler.TryStartMentalState(MentalStateDefOf.Wander_OwnRoom, null, true))
                {
                    Messages.Message(pawn.LabelCap + " is grossed out by the white rain and has locked themselves in their bedroom.", pawn, MessageTypeDefOf.NegativeEvent);
                    pawn.health.RemoveHediff(hediff);
                }
            }
        }
    }

    // Keeping this around to avoid XML issues
    public class Hediff_WhiteRainBuildup : HediffWithComps
    {
    }
}