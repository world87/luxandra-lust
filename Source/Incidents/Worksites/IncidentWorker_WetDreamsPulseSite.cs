using RimWorld;
using RimWorld.Planet;
using System.Collections.Generic;
using Verse;

namespace LuxandraLust
{
    public class IncidentWorker_WetDreamsPulseSite : IncidentWorker
    {
        protected override bool CanFireNowSub(IncidentParms parms)
        {
            if (!base.CanFireNowSub(parms)) return false;

            if (!LuxandraEventCheck.IsEnabled(LuxandraIncidentDefOf.Luxandra_Inc_WetDreamsPulseSite.defName))
            {
                return false;
            }

            Map map = (Map)parms.target;
            if (map == null || !map.IsPlayerHome) return false;

            PlanetTile playerPlanetTile = new PlanetTile(map.Tile);

            // Tightened distance check for the radar finder sub-routine
            return TileFinder.TryFindNewSiteTile(out _, playerPlanetTile, 3, 8, false, null, 0.5f, true, TileFinderMode.Near);
        }

        protected override bool TryExecuteWorker(IncidentParms parms)
        {
            Map map = (Map)parms.target;
            if (map == null || !map.IsPlayerHome) return false;

            PlanetTile playerPlanetTile = new PlanetTile(map.Tile);

            if (!TileFinder.TryFindNewSiteTile(out PlanetTile targetTile, playerPlanetTile, 3, 8, false, null, 0.5f, true, TileFinderMode.Near))
            {
                return false;
            }

            var sitePartList = new List<SitePartDef>();

            SitePartDef pulseBuildingGen = DefDatabase<SitePartDef>.GetNamed("Luxandra_WetDreamsPulseSitePart", false);
            if (pulseBuildingGen == null) return false;
            sitePartList.Add(pulseBuildingGen);

            // Roll a random threat
            int threatType = Rand.RangeInclusive(1, 3);
            switch (threatType)
            {
                case 1:
                    SitePartDef manhuntersGen = DefDatabase<SitePartDef>.GetNamed("Manhunters", false);
                    LuxandraDebugActions.DebugLogMessage($"Threat added to the pulse site: {manhuntersGen.defName}.");
                    sitePartList.Add(manhuntersGen);
                    break;
                case 2:
                    SitePartDef sleepingMechsGen = DefDatabase<SitePartDef>.GetNamed("SleepingMechanoids", false);
                    LuxandraDebugActions.DebugLogMessage($"Threat added to the pulse site: {sleepingMechsGen.defName}.");
                    sitePartList.Add(sleepingMechsGen);
                    break;
                case 3:
                    SitePartDef turretsGen = DefDatabase<SitePartDef>.GetNamed("Turrets", false);
                    LuxandraDebugActions.DebugLogMessage($"Threat added to the pulse site: {turretsGen.defName}.");
                    sitePartList.Add(turretsGen);
                    break;
                default:
                    LuxandraDebugActions.DebugLogMessage($"Something broke and no threat was rolled for the pulse site.");
                    break;
            }


            Site site = SiteMaker.MakeSite(sitePartList, targetTile, parms.faction);
            if (site == null) return false;

            int durationDays = Rand.RangeInclusive(15, 30);
            TimeoutComp timeoutComp = site.GetComponent<TimeoutComp>();
            if (timeoutComp != null)
            {
                timeoutComp.StartTimeout(durationDays * 60000);
            }

            Find.WorldObjects.Add(site);

            string letterLabel = "Wet Dreams Inducer Activated";
            string letterText = $"Long-range sensors have detected an active mechanical transmitter nearby. It has begun broadcasting an intense psychic disturbance, frequently waking up pawns in their sleep.\n\nIt will continue to plague your colony until you send a caravan to destroy it.\nAccording to energy signatures, its internal power matrix will deplete and shut down naturally in {durationDays} days if left alone.\n\n" +
                $"There may be something or someone defending it...";

            Find.LetterStack.ReceiveLetter(letterLabel, letterText, LetterDefOf.NegativeEvent, site);

            return true;
        }
    }
}