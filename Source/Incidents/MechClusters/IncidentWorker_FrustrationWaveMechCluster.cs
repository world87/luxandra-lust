using RimWorld;
using Verse;

namespace LuxandraLust
{
    public class IncidentWorker_FrustrationWaveMechCluster : IncidentWorker
    {
        protected override bool CanFireNowSub(IncidentParms parms)
        {
            if (!base.CanFireNowSub(parms)) return false;

            if (!LuxandraEventCheck.IsEnabled(LuxandraIncidentDefOf.Luxandra_Inc_FrustrationWaveMechCluster.defName))
                return false;

            Map map = (Map)parms.target;
            if (map == null || !map.IsPlayerHome) return false;

            GameConditionDef conditionDef = DefDatabase<GameConditionDef>.GetNamed("Luxandra_FrustrationWave", false);
            return conditionDef == null || !map.gameConditionManager.ConditionIsActive(conditionDef);
        }

        protected override bool TryExecuteWorker(IncidentParms parms)
        {
            Map map = (Map)parms.target;
            if (map == null) return false;

            ThingDef transmitterDef = DefDatabase<ThingDef>.GetNamed("Luxandra_FrustrationWaveTransmitter", false);
            if (transmitterDef == null) return false;

            // Generate the cluster sketch blueprint layout
            MechClusterSketch sketch = MechClusterGenerator.GenerateClusterSketch(parms.points, map, true);
            if (sketch == null || sketch.buildingsSketch == null) return false;

            SketchThing problemCauserTarget = null;

            for (int i = 0; i < sketch.buildingsSketch.Things.Count; i++)
            {
                SketchThing currentThing = sketch.buildingsSketch.Things[i];
                // Replace the transmitter with the Fertility Pulse one
                if (currentThing.def?.building?.buildingTags?.Contains("MechClusterProblemCauser") == true)
                {
                    problemCauserTarget = currentThing;
                    break;
                }
            }

            if (problemCauserTarget != null)
            {
                IntVec3 savedPos = problemCauserTarget.pos;
                Rot4 savedRot = problemCauserTarget.rot;

                // Remove the vanilla causer structure entirely from the sketch map
                sketch.buildingsSketch.Remove(problemCauserTarget);

                // Inject the new transmitter using the same parameters
                sketch.buildingsSketch.AddThing(transmitterDef, savedPos, savedRot);
            }
            else
            {
                // Fallback safe placement right at the center point of the layout grid cells
                sketch.buildingsSketch.AddThing(transmitterDef, IntVec3.Zero, Rot4.North);
            }

            // Send it scott
            IntVec3 dropCenter = MechClusterUtility.FindClusterPosition(map, sketch);
            if (dropCenter == IntVec3.Invalid) return false;

            MechClusterUtility.SpawnCluster(dropCenter, map, sketch);

            Find.LetterStack.ReceiveLetter(
                this.def.letterLabel,
                this.def.letterText,
                this.def.letterDef ?? LetterDefOf.NegativeEvent,
                new TargetInfo(dropCenter, map)
            );

            return true;
        }
    }
}