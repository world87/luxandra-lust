using RimWorld;
using rjw.Modules.Interactions;
using System.Collections.Generic;
using System.Linq;
using Verse;

namespace LuxandraLust
{
    public class IncidentWorker_LactoseCataclysm : IncidentWorker
    {
        protected override bool CanFireNowSub(IncidentParms parms)
        {
            if (!base.CanFireNowSub(parms)) return false;
            Map map = parms.target as Map;
            return map != null && map.mapPawns.FreeAdultColonistsSpawned.Any(p => !p.Dead && LuxandraUtilities.IsAdult(p));
        }

        protected override bool TryExecuteWorker(IncidentParms parms)
        {
            Map map = parms.target as Map;
            if (map == null) return false;

            // Cache Defs safely
            HediffDef resizeBreastsHediffDef = DefDatabase<HediffDef>.GetNamed("Luxandra_BustyCurse", false);
            if (resizeBreastsHediffDef == null)
            {
                Log.Warning("[Luxandra Debug] Def for Luxandra_BustyCurse not found in the database.");
                return false;
            }

            // Milk defs shenenigans
            HediffDef lactationHediff = HediffDefOf.Lactating;
            ThingDef milkFilthDef = DefDatabase<ThingDef>.GetNamed("Luxandra_FilthMilk", false) ?? ThingDefOf.Filth_Slime;
            ThingDef standardMilk = DefDatabase<ThingDef>.GetNamed("Milk", false);
            ThingDef milkDef = standardMilk;

            List<ThingDef> milkDefs = new List<ThingDef>();
            milkDefs.Add(standardMilk);

            // Equal milking milk
            ThingDef milkEM = DefDatabase<ThingDef>.GetNamed("EM_HumanMilk", false);
            if (milkEM != null)
            {
                milkDefs.Add(milkEM);
                milkDef = milkEM;
            }

            // Lactation expansion milk
            ThingDef milkLactation = DefDatabase<ThingDef>.GetNamed("SEX_BreastMilk", false);
            if (milkLactation != null)
            {
                milkDefs.Add(milkLactation);
                milkDef = milkLactation;
            }

            // ==========================================
            // PAWN BREASTS MANIPULATION
            // ==========================================
            foreach (Pawn pawn in map.mapPawns.AllHumanlikeSpawned)
            {
                if (pawn.Dead || !pawn.RaceProps.Humanlike) continue;

                bool hasBreasts = false;

                // Adult Males grow breasts
                if (pawn.gender == Gender.Male && LuxandraUtilities.IsAdult(pawn, false))
                {
                    HediffWithComps trackingHediff = HediffMaker.MakeHediff(resizeBreastsHediffDef, pawn, null) as HediffWithComps;
                    pawn.health.AddHediff(trackingHediff, null, null, null);
                    hasBreasts = true;
                }
                // Check females/others who already have breasts
                else if (pawn.GetBreasts().Any() && LuxandraUtilities.IsAdult(pawn, false))
                {
                    hasBreasts = true;
                }

                // Apply lactation
                if (hasBreasts && !pawn.health.hediffSet.HasHediff(lactationHediff))
                {
                    pawn.health.AddHediff(lactationHediff);
                }

                // Spawn Milk Filth under the pawn
                if (hasBreasts && milkFilthDef != null)
                {
                    int filthCount = Rand.RangeInclusive(8, 12);
                    for (int i = 0; i < filthCount; i++)
                    {
                        IntVec3 c = pawn.RandomAdjacentCell8Way();
                        if (c.InBounds(map) && c.Walkable(map))
                        {
                            FilthMaker.TryMakeFilth(pawn.Position, map, milkFilthDef);
                        }
                    }
                }

                // Max Out Nutrition and Sex Needs (Safe check)
                if (pawn.needs != null)
                {
                    Need foodNeed = pawn.needs.TryGetNeed(NeedDefOf.Food);
                    if (foodNeed != null) foodNeed.CurLevel = foodNeed.MaxLevel;

                    // Dynamically look for "Sex" need to prevent hard dependency crashes
                    Need sexNeed = LuxandraUtilities.GetSexNeed(pawn);
                    if (sexNeed != null) sexNeed.CurLevel = sexNeed.MaxLevel;
                }
            }

            // ==========================================
            // THE DAIRY MULTIPLICATION
            // ==========================================

            List<Thing> colonyMilks = map.listerThings.AllThings.Where(t =>
               t.def.category == ThingCategory.Item &&
               !t.Position.Fogged(map) &&
               milkDefs.Contains(t.def))
               .ToList();

            foreach (Thing milk in colonyMilks)
            {
                milk.stackCount = milk.stackCount * 2;
                if (milkDef != standardMilk)
                {
                    IntVec3 pos = milk.Position;
                    int count = milk.stackCount;
                    milk.Destroy();

                    Thing newHumanMilk = ThingMaker.MakeThing(milkDef);
                    newHumanMilk.stackCount = count;
                    GenPlace.TryPlaceThing(newHumanMilk, pos, map, ThingPlaceMode.Near);
                }
            }

            // ==========================================
            // COW MANHUNTER RAID
            // ==========================================
            IncidentDef manhunterDef = IncidentDefOf.ManhunterPack;
            PawnKindDef cowKind = PawnKindDef.Named("Cow");

            if (manhunterDef != null && cowKind != null)
            {
                IncidentParms customParms = StorytellerUtility.DefaultParmsNow(manhunterDef.category, map);
                customParms.pawnKind = cowKind;

                manhunterDef.Worker.TryExecute(customParms);
            }

            Find.LetterStack.ReceiveLetter(
                this.def.letterLabel,
                this.def.letterText,
                this.def.letterDef ?? LetterDefOf.ThreatBig
            );

            return true;
        }
    }
}