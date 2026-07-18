using RimWorld;
using System.Collections.Generic;
using System.Linq;
using Verse;

namespace LuxandraLust
{
    public class IncidentWorker_TheMilkGame : IncidentWorker
    {
        protected override bool CanFireNowSub(IncidentParms parms)
        {
            if (!LuxandraEventCheck.IsEnabled(LuxandraIncidentDefOf.Luxandra_Inc_TheMilkGame.defName))
                return false;

            Map map = (Map)parms.target;
            if (map == null) return false;

            // Incident can fire only if there is at least one living, free female adult colonist
            return map.mapPawns.FreeAdultColonistsSpawned.Any(p => p.gender == Gender.Female && !p.Dead && LuxandraUtilities.IsAdult(p));
        }

        protected override bool TryExecuteWorker(IncidentParms parms)
        {
            Map map = (Map)parms.target;
            if (map == null) return false;

            // Full Stomachs & Spontaneous Lactation ---
            foreach (Pawn pawn in map.mapPawns.AllPawnsSpawned)
            {
                if (pawn.RaceProps.Humanlike && pawn.Faction == Faction.OfPlayer)
                {
                    // Satisfy hunger completely
                    if (pawn.needs?.food != null)
                    {
                        pawn.needs.food.CurLevel = pawn.needs.food.MaxLevel;
                    }

                    // "only adults" check for anything related to lactation
                    if (LuxandraUtilities.IsAdult(pawn))
                    {
                        // Biotech-specific lactation hook
                        if (ModsConfig.BiotechActive && pawn.gender == Gender.Female)
                        {
                            HediffDef lactationDef = HediffDefOf.Lactating;
                            if (lactationDef != null && !pawn.health.hediffSet.HasHediff(lactationDef))
                            {
                                pawn.health.AddHediff(lactationDef);
                            }
                        }
                    }
                }
            }

            // Crop-to-Milk Transmutation because it's very funny
            List<Thing> cropsInStorage = map.listerThings.AllThings.Where(t =>
                t.def.category == ThingCategory.Item &&
                !t.Position.Fogged(map) &&
                (t.def == ThingDef.Named("RawCorn") || t.def == ThingDef.Named("RawRice") || t.def == ThingDef.Named("RawPotatoes"))
            ).ToList();

            foreach (Thing cropStack in cropsInStorage)
            {
                int stackCount = cropStack.stackCount;
                int amountToConvert = Rand.RangeInclusive(stackCount / 3, stackCount * 2 / 3);

                if (amountToConvert > 0)
                {
                    IntVec3 position = cropStack.Position;

                    if (amountToConvert >= stackCount)
                    {
                        cropStack.Destroy(DestroyMode.Vanish);
                    }
                    else
                    {
                        cropStack.SplitOff(amountToConvert).Destroy(DestroyMode.Vanish);
                    }

                    ThingDef milkDef = DefDatabase<ThingDef>.GetNamed("Milk", false);

                    // Equal milking milk
                    ThingDef milkEM = DefDatabase<ThingDef>.GetNamed("EM_HumanMilk", false);
                    if (milkEM != null)
                        milkDef = milkEM;

                    // Lactation expansion milk
                    ThingDef milkLactation = DefDatabase<ThingDef>.GetNamed("SEX_BreastMilk", false);
                    if (milkLactation != null)
                        milkDef = milkLactation;

                    Thing milk = ThingMaker.MakeThing(milkDef);


                    milk.stackCount = amountToConvert;
                    GenSpawn.Spawn(milk, position, map);
                }
            }

            // Wolf Pack Ambush
            float threatPoints = StorytellerUtility.DefaultThreatPointsNow(map) * 0.85f;

            IncidentParms wolfParms = new IncidentParms
            {
                target = map,
                points = threatPoints,
                pawnKind = DefDatabase<PawnKindDef>.GetNamed("Warg")
            };

            IncidentDef manhunterIncident = IncidentDefOf.ManhunterPack;

            if (manhunterIncident.Worker.CanFireNow(wolfParms))
            {
                manhunterIncident.Worker.TryExecute(wolfParms);
            }

            // Send a letter to the player explaining the event7
            Find.LetterStack.ReceiveLetter(
                this.def.letterLabel,
                this.def.letterText,
                this.def.letterDef ?? LetterDefOf.NegativeEvent
            );

            return true;
        }
    }
}