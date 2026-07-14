using HarmonyLib;
using RimWorld;
using rjw;
using System;
using System.Linq;
using Verse;

namespace LuxandraLust
{
    [HarmonyPatch(typeof(SexUtility), nameof(SexUtility.Aftersex))]
    public static class Patch_SexUtility_Aftersex
    {
        [HarmonyPostfix]
        public static void Postfix(SexProps props)
        {
            try
            {
                // Since the disease can be rolled without Luxandra being the storyteller
                // run the check regardless
                HandleAphrodisiacFeverInfection(props);

                if (!LuxandraStorytellerCheck.IsActive())
                    return;

                // Validate that the properties packet isn't corrupted
                if (props == null || props.pawn == null)
                    return;

                Pawn actor = props.pawn;

                if (!actor.RaceProps.Humanlike || !actor.IsPlayerControlled)
                    return;

                // And a second check (Might not be necessary, TODO verify)
                bool isPlayerControlled = actor.Faction == Faction.OfPlayer ||    // Colonists & Mechs
                                          actor.IsPrisonerOfColony ||             // Prisoners
                                          actor.IsSlaveOfColony ||                // Slaves
                                          actor.IsQuestLodger();                  // Quest Guests / Refugees

                if (!isPlayerControlled)
                    return;

                if (LuxandraUtilities.DoesSexMatchLuxandraKink(props) == true)
                {
                    GameComponent_LuxandraLust.Instance?.RegisterSexAction(true);
                    if (LuxandraModSettings.enablePleasedNotification)
                    {
                        Messages.Message($"{actor.NameShortColored} has pleased Luxandra with their actions. She rewarded you with 1 Favor.", MessageTypeDefOf.PositiveEvent, false);
                    }
                    LuxandraDebugActions.DebugLogMessage($"The {props.sexType} done by {actor.NameShortColored} with {props.partner?.NameShortColored} matched Luxandra's current kink ({GameComponent_LuxandraLust.CurrentKink}).");
                }

                // Fapping won't count, sorry. Nor will touching each other. Get those dicks out already
                bool isMasturbation = props.sexType == xxx.rjwSextype.Masturbation || props.sexType == xxx.rjwSextype.MutualMasturbation;
                if (isMasturbation)
                {
                    LuxandraDebugActions.DebugLogMessage($"Masturbation detected for {actor.NameShortColored}: does not count as sex action.");
                    return;
                }

                // Will always register the sex action happening regardless of the type
                GameComponent_LuxandraLust.Instance?.RegisterSexAction();
                LuxandraDebugActions.DebugLogMessage($"Sex action detected for {actor.NameShortColored} with {props.partner?.NameShortColored}");

                // If necrophilia is detected I won't register the type. I don't care which hole you're using, it's moist all the same.
                bool isNecrophilia = props.partner != null && props.partner.Dead;
                if (isNecrophilia)
                {
                    GameComponent_LuxandraLust.Instance?.RegisterNecrophiliaSexAction();
                    LuxandraDebugActions.DebugLogMessage($"Necrophilia detected for {actor.NameShortColored} with {props.partner?.NameShortColored}");
                    return;
                }

                // Mechs aren't animals, sorry.
                bool isBestialityAct = props.partner != null && props.partner.RaceProps.Humanlike == false && !props.partner.IsColonyMech;
                if (isBestialityAct)
                {
                    GameComponent_LuxandraLust.Instance?.RegisterBestialitySexAction();
                    LuxandraDebugActions.DebugLogMessage($"Bestiality sex action detected for {actor.NameShortColored} with {props.partner?.NameShortColored}");
                    return;
                }

                // Only proper vaginal sex is pure... right... right? I mean, anal is fun too, but it's not pure. And oral is just gross.
                bool isImpureSex = props.sexType != xxx.rjwSextype.Vaginal;
                if (isImpureSex)
                {
                    GameComponent_LuxandraLust.Instance?.RegisterImpureSexAction();
                    LuxandraDebugActions.DebugLogMessage($"Impure sex action detected for {actor.NameShortColored} with {props.partner?.NameShortColored}");
                }

                // Shouldn't be counting animals and corpses. It's only rape if they can't actually say no.
                bool isRapeAct = props.isRape;
                if (isRapeAct)
                {
                    GameComponent_LuxandraLust.Instance?.RegisterRapeSexAction();
                    LuxandraDebugActions.DebugLogMessage($"Rape action detected involving {actor.NameShortColored} forcing themvelves on {props.partner?.NameShortColored}");
                }
            }
            catch (Exception ex)
            {
                Log.Error($"[Luxandra Debug] Critical exception tracking inside SexUtility.Aftersex Postfix: {ex}");
            }
        }

        public static void HandleAphrodisiacFeverInfection(SexProps props)
        {
            Pawn pawn = props.pawn;
            Pawn partner = props.partner;

            if (pawn == null || pawn.health == null) return;

            // Check if this specific pawn even has the fever
            var fever = pawn.health.hediffSet.hediffs
                .FirstOrDefault(h => h.def.defName == "Luxandra_AphrodisiacFever");

            if (fever == null || fever.Part == null)
            {
                // Pawn is not infected, check for the partner
                var partnerFever = partner.health.hediffSet.hediffs
                    .FirstOrDefault(h => h.def.defName == "Luxandra_AphrodisiacFever");

                // If neither pawn is infected do nothing
                if (partnerFever == null || partnerFever.Part == null)
                    return;
            }

            LuxandraDebugActions.DebugLogMessage($"Attempting to reduce severity of Aphrodisiac Fever for {props.pawn.NameShortColored} who had sex with with {props.partner?.NameShortColored}");

            // 2. Fluid Contact Validation
            bool hasFluidContact = props.sexType == xxx.rjwSextype.Vaginal ||
                                   props.sexType == xxx.rjwSextype.Anal ||
                                   props.sexType == xxx.rjwSextype.Oral ||
                                   props.sexType == xxx.rjwSextype.Cunnilingus ||
                                   props.sexType == xxx.rjwSextype.Fellatio;

            if (!hasFluidContact)
            {
                LuxandraDebugActions.DebugLogMessage($"Interaction did not involve fluid exchange. No reduction.");
                return;
            }
            ;

            // 2. Fetch the actual parts used from the resolved properties arrays safely
            string initiatorPartUsed = props.resolved?.InitiatorParts?[0]?.BodyPart?.def?.defName;
            string receiverPartUsed = props.resolved?.RecipientParts?[0]?.BodyPart?.def?.defName;

            // 3. Process the initiator (props.pawn)
            if (props.pawn != null)
            {
                TryReduceAphrodisiacFever(props.pawn, initiatorPartUsed);
            }

            // 4. Process the recipient/partner (props.partner)
            if (props.partner != null)
            {
                TryReduceAphrodisiacFever(props.partner, receiverPartUsed);
            }
        }

        private static void TryReduceAphrodisiacFever(Pawn pawn, string partUsed)
        {
            if (pawn.health == null || partUsed == null) return;

            // Find our custom fever hediff
            var partsWithFever = pawn.health.hediffSet.hediffs
                .Where(h => h.def.defName == "Luxandra_AphrodisiacFever");

            // If they don't have it, or it's on a part that was missing/destroyed, exit
            if (partsWithFever == null || !partsWithFever.Any()) return;

            // Cycle all the parts with fever
            foreach (var fever in partsWithFever)
            {
                try
                {
                    var feverPart = fever.Part.def.defName;

                    bool partWasUsed = false;
                    LuxandraDebugActions.DebugLogMessage($"Part with fever for {pawn.NameShortColored}: {feverPart}");
                    LuxandraDebugActions.DebugLogMessage($"Part used by {pawn.NameShortColored}: {partUsed}");

                    // Did the part they just used match where the fever is living?
                    if (feverPart == "Genitals" && partUsed == "Genitals")
                        partWasUsed = true;

                    if (feverPart == "Anus" && partUsed == "Anus")
                        partWasUsed = true;

                    if ((feverPart == "Mouth" || feverPart == "Stomach") && (partUsed == "Mouth" || partUsed == "Jaw"))
                        partWasUsed = true;

                    // Calculate the potential reduction
                    float reduction = 0f;

                    if (partWasUsed)
                    {
                        reduction = Rand.Range(0.05f, 0.10f);
                    }
                    else
                    {
                        reduction = 0.01f;
                    }

                    SkillDef sexSkillDef = DefDatabase<SkillDef>.GetNamedSilentFail("Sex");

                    // If the pawn is valid and has the skill, grab their level (0 - 20)
                    if (pawn != null && sexSkillDef != null && pawn.skills != null)
                    {
                        int skillLevel = pawn.skills.GetSkill(sexSkillDef).Level;

                        // A level 0 pawn gets a 1.0x modifier. A level 20 pawn gets a 4.0x modifier.
                        // Formula: 1.0 + (Level * 0.15) -> Maxes out at 4.0x at level 20.
                        float skillMultiplier = 1.0f + (skillLevel * 0.15f);
                        LuxandraDebugActions.DebugLogMessage($"{pawn.LabelShort}'s sex skill increases their healing by a factor of {skillMultiplier}.");
                        reduction = reduction * skillMultiplier;
                    }

                    // Reduce the fever
                    fever.Severity -= reduction;
                    string percentReduced = (reduction * 100f).ToString("F1");

                    LuxandraDebugActions.DebugLogMessage($"{pawn.LabelShort}'s Aphrodisiac Fever on their {fever.Part.def.label} cooled by {percentReduced}%.");

                    if (fever.Severity <= 0)
                    {
                        Messages.Message($"{pawn.LabelShort} has successfully quenched and cured their Aphrodisiac Fever!",
                            pawn, MessageTypeDefOf.PositiveEvent);
                    }
                }
                // Fallback in case something implodes
                catch
                {
                    {
                        LuxandraDebugActions.DebugLogMessage($"Error attempting to reduce {pawn.LabelShort}'s Aphrodisiac Fever on their {fever.Part.def.label}.");
                    }
                }
            }
        }
    }
}