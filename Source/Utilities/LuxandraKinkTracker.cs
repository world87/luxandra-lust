using RimWorld;
using rjw;
using System.Collections.Generic;
using Verse;
using static LuxandraLust.GameComponent_LuxandraLust;

namespace LuxandraLust
{
    public class LuxandraKinkTracker : Alert
    {
        public LuxandraKinkTracker()
        {
            this.defaultLabel = "Luxandra's current kink: None";

            this.defaultPriority = AlertPriority.Medium;
        }

        public override AlertReport GetReport()
        {
            // If Luxandra is not active, immediately hide the alert from the sidebar
            if (!LuxandraStorytellerCheck.IsActive())
                return false;

            // Otherwise always display the alert
            return true;
        }
        public override string GetLabel()
        {
            string phaseName = CurrentKink.ToString();
            return $"Luxandra's current kink: {phaseName}";
        }

        public static void TriggerKinkShift(bool forceSpecific = false, StorytellerKink forcedType = StorytellerKink.None)
        {
            // Roll a new random phase
            RerollKink(forceSpecific, forcedType);

            // Update the monuments
            for (int m = 0; m < Find.Maps.Count; m++)
            {
                Map currentMap = Find.Maps[m];
                if (currentMap == null) continue;

                // Grab everything on this specific map that has a component
                var buildings = currentMap.listerThings.ThingsInGroup(ThingRequestGroup.BuildingArtificial);

                for (int i = 0; i < buildings.Count; i++)
                {
                    // Fast component check—if it's a regular wall, this returns null instantly
                    var comp = buildings[i].TryGetComp<Comp_LuxandraMonument>();
                    if (comp != null)
                    {
                        comp.Notify_KinkChanged();
                    }
                }
            }

            // Roll a random day counter between 1 and 7 days
            int randomDays = Rand.RangeInclusive(1, 7);

            // Convert days to internal ticks (1 day = 60,000 ticks)
            GameComponent_LuxandraCyclicEvents.Instance.ticksUntilKinkChange = randomDays * GenDate.TicksPerDay;

            if (LuxandraModSettings.enableLogging)
            {
                LuxandraDebugActions.DebugLogMessage($"Storyteller shifted phase to {CurrentKink}. Next shift scheduled in {randomDays} days ({GameComponent_LuxandraCyclicEvents.Instance.ticksUntilKinkChange} ticks).");
            }
        }

        public override TaggedString GetExplanation()
        {
            // Enum name
            string phaseName = $"Luxandra_KinkName_{CurrentKink}".Translate();
            string flavorTextKey = $"Luxandra_KinkFlavor_{CurrentKink}";

            if (!flavorTextKey.CanTranslate())
            {
                flavorTextKey = "Luxandra_KinkFlavor_None";
            }

            string flavorText = flavorTextKey.Translate();

            return "Luxandra_CurrentObsession".Translate(phaseName, flavorText);
        }

        /// <summary>
        /// Gets the kinks that are enabled based on the RJW settings and potential submods
        /// </summary>
        public static List<StorytellerKink> GetEnabledKinks()
        {
            List<StorytellerKink> validKinks = new List<StorytellerKink>();

            validKinks.Add(StorytellerKink.None);

            validKinks.Add(StorytellerKink.Pregnancy);
            validKinks.Add(StorytellerKink.Anal);
            validKinks.Add(StorytellerKink.Oral);
            validKinks.Add(StorytellerKink.Masturbation);
            validKinks.Add(StorytellerKink.Gay);
            validKinks.Add(StorytellerKink.Lesbian);
            validKinks.Add(StorytellerKink.Cum);
            validKinks.Add(StorytellerKink.Breasts);

            if (IsBestialityEnabled())
                validKinks.Add(StorytellerKink.Bestiality);

            if (IsRapeEnabled())
                validKinks.Add(StorytellerKink.Rape);

            if (IsNecrophiliaEnabled())
                validKinks.Add(StorytellerKink.Necrophilia);

            if (IsImplantationEnabled())
                validKinks.Add(StorytellerKink.Implantation);

            if (IsIncestEnabled())
                validKinks.Add(StorytellerKink.Incest);

            if (IsFutaEnabled())
                validKinks.Add(StorytellerKink.Futa);

            if (IsMechanophiliaEnabled())
                validKinks.Add(StorytellerKink.Mechanophilia);

            if (IsTentaclePornEnabled())
                validKinks.Add(StorytellerKink.Tentacles);

            return validKinks;
        }


        /// <summary>
        /// Rerolls Luxandra's current kink (optionally specify which to get)
        /// </summary>
        public static void RerollKink(bool forceSpecific = false, StorytellerKink forcedType = StorytellerKink.None)
        {
            if (forceSpecific)
                CurrentKink = forcedType;
            else
            {
                var validKinks = GetEnabledKinks();
                validKinks.Remove(CurrentKink);
                CurrentKink = validKinks.RandomElement(); //TODO: Weightings?
            }

            if (CurrentKink != StorytellerKink.None)
                Messages.Message($"Luxandra's whims have shifted: She is now into {CurrentKink}.", MessageTypeDefOf.CautionInput, false);
            else
                Messages.Message($"Luxandra's whims have shifted: She is not into anything specific at the moment.", MessageTypeDefOf.CautionInput, false);
        }

        /// <summary>
        /// Valid condition: RJW bestiality option enabled or ElToroMechanoids loaded
        /// </summary>
        public static bool IsBestialityEnabled()
        {
            if (RJWSettings.bestiality_enabled || LuxandraModChecks.IsElToroBestialityActive())
                return true;

            return false;
        }

        /// <summary>
        /// Valid condition: RJW rape option enabled
        /// </summary>
        public static bool IsRapeEnabled()
        {
            if (RJWSettings.rape_enabled)
                return true;

            return false;
        }

        /// <summary>
        /// Valid condition: RJW necrophilia option enabled
        /// </summary>
        public static bool IsNecrophiliaEnabled()
        {
            if (RJWSettings.necrophilia_enabled)
                return true;

            return false;
        }

        /// <summary>
        /// Valid condition: any RJW egg pregnancy option enabled or RJW Insects loaded
        /// </summary>
        public static bool IsImplantationEnabled()
        {
            if (RJWPregnancySettings.insect_pregnancy_enabled || RJWPregnancySettings.insect_anal_pregnancy_enabled ||
                RJWPregnancySettings.insect_oral_pregnancy_enabled || LuxandraModChecks.IsRJWInsectsActive())
                return true;

            return false;
        }

        /// <summary>
        /// Valid condition: option enabled or Sexperience-Ideology loaded
        /// </summary>
        public static bool IsIncestEnabled()
        {
            if (LuxandraModChecks.IsSexperienceIdeologyActive() || LuxandraModSettings.removeRomanceRestrictions)
                return true;

            return false;
        }

        /// <summary>
        /// Valid condition: any RJW futa spawning option enabled
        /// </summary>
        public static bool IsFutaEnabled()
        {
            if (RJWSettings.futa_natives_chance > 0 || RJWSettings.futa_nymph_chance > 0 || RJWSettings.futa_spacers_chance > 0 ||
                RJWSettings.FemaleFuta || RJWSettings.GenderlessAsFuta)
                return true;

            return false;
        }

        /// <summary>
        /// Valid condition: RJW option enabled or ElToroMechanoids loaded
        /// </summary>
        public static bool IsMechanophiliaEnabled()
        {
            if (LuxandraModChecks.IsElToroMechanoidsActive() || RJWSettings.mechanophilia_enabled)
                return true;

            return false;
        }

        public static bool IsTentaclePornEnabled()
        {
            if (ModsConfig.AnomalyActive || LuxandraModChecks.IsOnaholeActive())
                return true;

            return false;
        }
    }
}