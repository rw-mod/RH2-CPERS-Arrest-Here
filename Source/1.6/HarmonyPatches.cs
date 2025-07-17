using HarmonyLib;
using RimWorld;
using Verse;

namespace CapturedPersons;

[StaticConstructorOnStartup]
public static class HarmonyContainer
{
    static HarmonyContainer()
    {
        Harmony harmony = new Harmony("rw.mod.ArrestHere");
        harmony.PatchAll();
    }
}

[HarmonyPatch(typeof(JobGiver_PrisonerEscape), "ShouldStartEscaping")]
public static class ShouldStartEscapingPatch
{
    public static void Postfix(ref bool __result, Pawn pawn)
    {
        if (!__result) return;
        
        CompImprisonment comp = pawn.TryGetComp<CompImprisonment>();
        if (comp.LastTryingEscapeTick == 0)
        {
            comp.LastTryingEscapeTick = Find.TickManager.TicksGame;
            __result = false;
        }
        else if (Find.TickManager.TicksGame < comp.LastTryingEscapeTick + GenDate.TicksPerDay)
        {
            __result = false;
        }
        else
        {
            comp.LastTryingEscapeTick = 0;
        }
    }
}