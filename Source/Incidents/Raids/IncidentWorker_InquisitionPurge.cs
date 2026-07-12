using RimWorld;
using System.Collections.Generic;
using Verse;
using Verse.AI.Group;

namespace LuxandraLust
{
    public class IncidentWorker_InquisitionPurge : IncidentWorker
    {
        protected override bool CanFireNowSub(IncidentParms parms)
        {
            if (!base.CanFireNowSub(parms)) return false;

            if (!ModsConfig.RoyaltyActive || !LuxandraEventCheck.IsEnabled(LuxandraIncidentDefOf.Luxandra_Inc_InquisitionPurgeSappers.defName))
            {
                return false;
            }

            Map map = (Map)parms.target;

            Faction crusaderFaction = Find.FactionManager.FirstFactionOfDef(LuxandraFactionDefOf.Luxandra_PuritanCrusaders);
            if (crusaderFaction == null || !crusaderFaction.HostileTo(Faction.OfPlayer)) return false;

            return true;
        }

        protected override bool TryExecuteWorker(IncidentParms parms)
        {
            Map map = (Map)parms.target;
            Faction crusaderFaction = Find.FactionManager.FirstFactionOfDef(LuxandraFactionDefOf.Luxandra_PuritanCrusaders);

            if (crusaderFaction == null || !crusaderFaction.HostileTo(Faction.OfPlayer))
            {
                Log.Warning("[Luxandra Debug] Attempted to spawn a Puritan Crusader Inquisition raid, but the faction is null or not hostile to the player.");
                return false;
            }

            // Grab the custom pawn kinds
            PawnKindDef cleanserKind = DefDatabase<PawnKindDef>.GetNamed("Luxandra_Crusader_Cleanser", false);
            PawnKindDef inquisitorKind = DefDatabase<PawnKindDef>.GetNamed("Luxandra_Crusader_Inquisitor", false);
            PawnKindDef whiteCapeKind = DefDatabase<PawnKindDef>.GetNamed("Luxandra_Crusader_WhiteCape", false);
            PawnKindDef conscriptKind = DefDatabase<PawnKindDef>.GetNamed("Luxandra_Crusader_Conscript", false);

            // Pick a valid edge cell to enter from
            if (!CellFinder.TryFindRandomEdgeCellWith(c => c.Walkable(map) && !c.Fogged(map), map, CellFinder.EdgeRoadChance_Neutral, out IntVec3 spawnCell))
            {
                return false;
            }

            List<Pawn> raidSquad = new List<Pawn>();
            float pointsRemaining = parms.points;

            // Put a minimum so at least 1 enemy always spawns
            if (pointsRemaining <= 35f)
            {
                pointsRemaining = 36f;
            }

            // Keep drawing from your custom unit roster until raid budget points are spent
            while (pointsRemaining > 35f) // 35 is the lowest CP (Conscript)
            {
                PawnKindDef chosenKind = whiteCapeKind; // Default budget filler

                // Randomly weigh weights for the composition shape
                float rand = Rand.Value;
                if (rand < 0.40f)
                {
                    chosenKind = cleanserKind; // Heavy emphasis on the fire starters
                }
                else if (rand < 0.65f && ModsConfig.AnomalyActive)
                {
                    chosenKind = inquisitorKind; // Anomaly check protection!
                }
                else if (rand < 0.85f)
                {
                    chosenKind = conscriptKind; // Cowardly back-row support
                }

                // If points don't allow the roll, fallback to a single cleanser
                if (pointsRemaining < chosenKind.combatPower)
                {
                    chosenKind = cleanserKind;
                }

                PawnGenerationRequest request = new PawnGenerationRequest(
                    chosenKind,
                    crusaderFaction,
                    PawnGenerationContext.NonPlayer,
                    mustBeCapableOfViolence: true
                );

                Pawn inquisitorUnit = PawnGenerator.GeneratePawn(request);
                GenSpawn.Spawn(inquisitorUnit, spawnCell, map);

                // Use your verified Lord isolation fixer
                Lord backgroundLord = inquisitorUnit.GetLord();
                backgroundLord?.Notify_PawnLost(inquisitorUnit, PawnLostCondition.ForcedToJoinOtherLord);

                raidSquad.Add(inquisitorUnit);
                pointsRemaining -= chosenKind.combatPower;
            }

            if (raidSquad.Count == 0) return false;

            // THE INCENDIARY SAPPER AI JUMP: Assign them to use Sapper logic
            // canAsSappers: true forces the AI engine to evaluate grenades/weapons to breach walls directly
            LordJob_AssaultColony sapperJob = new LordJob_AssaultColony(
                crusaderFaction,
                canKidnap: false,
                canPickUpOpportunisticWeapons: true,
                canSteal: false,
                sappers: true
            );

            LordMaker.MakeNewLord(crusaderFaction, sapperJob, map, raidSquad);

            // Send custom dramatic event title
            Find.LetterStack.ReceiveLetter(
                "The Cleansing Inquisition",
                "A fanatical purge squad from the Puritan Crusaders has arrived!\n\nThey haven't come to conquer or negotiate. Armed with incendiary charges, they are planning of burning and blasting directly through your walls to leave your colony a smoking ruin.",
                LetterDefOf.ThreatBig,
                raidSquad[0]
            );

            return true;
        }
    }
}