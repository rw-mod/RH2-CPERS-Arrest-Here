using HarmonyLib;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;
using Verse.AI;

namespace CapturedPersons
{
    [StaticConstructorOnStartup]
    internal static class HarmonyContainer
    {
        public static Harmony harmony;
        static HarmonyContainer()
        {
            harmony = new Harmony("ChickenPlucker.CapturedPersonsPatches");
            harmony.PatchAll();
            foreach (var def in DefDatabase<ThingDef>.AllDefs.Where(x => x.race?.Humanlike ?? false))
            {
                def.comps.Add(new CompProperties_Imprisonment());
            }
        }
    }

    [HarmonyPatch(typeof(JobGiver_PrisonerEscape), "ShouldStartEscaping")]
    public static class ShouldStartEscapingPatch
    {
        public static void Postfix(ref bool __result, Pawn pawn)
        {
            if (__result)
            {
                var comp = pawn.TryGetComp<CompImprisonment>();
                if (comp.lastTryingEscapeTick == 0)
                {
                    comp.lastTryingEscapeTick = Find.TickManager.TicksGame;
                    __result = false;
                }
                else if (Find.TickManager.TicksGame < comp.lastTryingEscapeTick + GenDate.TicksPerDay)
                {
                    __result = false;
                }
                else
                {
                    comp.lastTryingEscapeTick = 0;
                }
            }
        }
    }


    [HarmonyPatch(typeof(FloatMenuMakerMap), "AddHumanlikeOrders")]
    public static class AddHumanlikeOrdersPatch
    {
        public static void Postfix(Vector3 clickPos, Pawn pawn, ref List<FloatMenuOption> opts)
        {
            if (pawn == null || clickPos == null)
                return;
            IntVec3 c = IntVec3.FromVector3(clickPos);
            if (pawn.health.capacities.CapableOf(PawnCapacityDefOf.Manipulation))
            {
                foreach (Thing thing in c.GetThingList(pawn.Map))
                {
                    if (thing is Pawn victim && pawn.CanReserveAndReach(victim, PathEndMode.OnCell, Danger.Deadly, 1, -1, null, true))
                    {
                        if (victim.RaceProps.Humanlike && victim.Downed ||
                            victim.CurJobDef == JobDefOf.Goto && victim.CurJob.exitMapOnArrival && victim.mindState.lastJobTag == JobTag.Escaping)
                        {
                            string label = "CP_ImprisonInPlace".Translate(victim.LabelCap);
                            if (victim.Faction != null && victim.Faction != Faction.OfPlayer && !victim.Faction.Hidden && !victim.Faction.HostileTo(Faction.OfPlayer) 
                                && !victim.IsPrisonerOfColony)
                            {
                                label += ": " + "AngersFaction".Translate().CapitalizeFirst();
                            }
                            Action action = delegate
                            {
                                Job job = JobMaker.MakeJob(CP_DefOf.CP_ImprisonInPlace, victim);
                                job.count = 1;
                                pawn.jobs.TryTakeOrderedJob(job);
                                PlayerKnowledgeDatabase.KnowledgeDemonstrated(ConceptDefOf.Capturing, KnowledgeAmount.Total);
                                if (victim.Faction != null && victim.Faction != Faction.OfPlayer && !victim.Faction.Hidden && !victim.Faction.HostileTo(Faction.OfPlayer) && !victim.IsPrisonerOfColony)
                                {
                                    Messages.Message("MessageCapturingWillAngerFaction".Translate(victim.Named("PAWN")).AdjustedFor(victim), victim, MessageTypeDefOf.CautionInput, historical: false);
                                }
                            };
                            opts.Add(FloatMenuUtility.DecoratePrioritizedTask(new FloatMenuOption(label, action, MenuOptionPriority.Default, null, victim), pawn, victim, "ReservedBy"));
                        }
                    }
                }
            }
        }
    }
}
