using RimWorld;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace LuxandraLust
{
    public class LuxandraEventMod : Mod
    {
        public LuxandraEventMod(ModContentPack content) : base(content)
        {
            // Forces RimWorld to read the XML file and populate the static variables at boot
            GetSettings<LuxandraEventSettings>();
        }

        public override string SettingsCategory()
        {
            return "Luxandra Lust - Events";
        }

        public override void DoSettingsWindowContents(Rect inRect)
        {
            // Forward directly to the static drawing method
            LuxandraEventSettings.DoWindowContents(inRect);
        }
    }

    public class LuxandraEventSettings : ModSettings
    {
        public static List<string> disabledEventNames;

        private static Vector2 scrollPosition = Vector2.zero;

        public static void DoWindowContents(Rect inRect)
        {
            // --- SECTION 1: HEADER ---
            Listing_Standard headerListing = new Listing_Standard();
            headerListing.Begin(inRect);
            Rect labelRect = headerListing.Label("Luxandra_UI_ToggleEvents_Label".Translate());
            TooltipHandler.TipRegion(labelRect, "Luxandra_UI_ToggleEvents_Tooltip".Translate());
            headerListing.Gap(10f);
            headerListing.End();

            // --- SECTION 2: THE SCROLL VIEW CONTAINER ---
            Rect outRect = new Rect(inRect.x, inRect.y + 40f, inRect.width, inRect.height - 140f);
            float totalViewHeight = (LuxandraDefsCollections.AllIncidents.Count * 26f) + 10f;
            Rect viewRect = new Rect(0f, 0f, outRect.width - 16f, totalViewHeight);

            // Begin the scroll area window context
            Widgets.BeginScrollView(outRect, ref scrollPosition, viewRect);
            Listing_Standard scrollListing = new Listing_Standard();
            scrollListing.Begin(viewRect);

            if (disabledEventNames == null || !LuxandraDefsCollections._isInitialized || !LuxandraDefsCollections.AllIncidents.Any())
            {
                scrollListing.Label("Luxandra_UI_DatabaseLoading_Label".Translate());
            }
            else
            {
                foreach (var eventWrapper in LuxandraDefsCollections.AllIncidents)
                {
                    if (eventWrapper.IncidentDef == null) continue;

                    string defName = eventWrapper.IncidentDef.defName;
                    string modSource = eventWrapper.ModRequired != "" ? $"({eventWrapper.ModRequired})" : "";
                    string label = eventWrapper.IncidentDef.label.CapitalizeFirst();

                    bool isEnabled = disabledEventNames == null || !disabledEventNames.Contains(defName);
                    bool previousState = isEnabled;

                    string modDescription = $"{label} {modSource}";
                    // If Devmode is on, show the def too
                    if (Prefs.DevMode)
                    {
                        modDescription = modDescription + $" (def: {defName})";
                    }

                    Rect rowRect = scrollListing.GetRect(26f);

                    // Tiny box on the left for the checkmark icon
                    Rect checkRect = new Rect(rowRect.x, rowRect.y, 24f, 24f);

                    // Carve out the remaining space to the right of the checkmark for the text label
                    Rect textRect = new Rect(rowRect.x + 30f, rowRect.y, rowRect.width - 30f, 24f);

                    // Draw the actual clickable check/cross widget on the left -- Using RJW one cause it's appropriate lol
                    //Widgets.Checkbox(checkRect.position, ref isEnabled, 24f);
                    rjw.MainTab.DesignatorCheckbox.Checkbox(checkRect.position, ref isEnabled, 24f);

                    // Draw the text label immediately next to it
                    Widgets.Label(textRect, $"{modDescription}");

                    // Add a tool-tip mouseover description box across the whole row area
                    if (Mouse.IsOver(rowRect) && !eventWrapper.ShortDescription.NullOrEmpty())
                    {
                        var eventTooltip = eventWrapper.ShortDescription + DisplayRelatedKinks(eventWrapper);
                        TooltipHandler.TipRegion(rowRect, eventTooltip);
                    }

                    if (isEnabled != previousState)
                    {
                        if (isEnabled)
                            disabledEventNames.Remove(defName);
                        else
                            disabledEventNames.Add(defName);
                    }
                    scrollListing.Gap(2f);
                }
            }

            scrollListing.End();
            Widgets.EndScrollView();

            Rect resetRect = new Rect(inRect.x, inRect.height - 40f, 140f, 30f);
            if (Widgets.ButtonText(resetRect, "Reset Defaults"))
            {
                disabledEventNames.Clear();
                SoundDefOf.Tick_High.PlayOneShotOnCamera();
            }
        }

        public override void ExposeData()
        {
            base.ExposeData();

            Scribe_Collections.Look(ref disabledEventNames, "disabledEventNames", LookMode.Value);

            // Safety check: ensure it isn't null on a brand-new save/game boot
            if (disabledEventNames == null)
            {
                disabledEventNames = new List<string>();
            }
        }

        private static string DisplayRelatedKinks(LuxandraIncidentDefs incident)
        {
            if (incident.AssociatedKinks.Any())
            {
                var kinkStrings = incident.AssociatedKinks.Select(k => k.ToString());

                string kinks = "\n\nAssociated Kinks: " + string.Join(" - ", kinkStrings);

                return kinks;
            }

            return "";
        }
    }
}