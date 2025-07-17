using Verse;

namespace CapturedPersons;

public class CompProperties_Imprisonment : CompProperties
{
    public CompProperties_Imprisonment()
    {
        compClass = typeof(CompImprisonment);
    }
}

public class CompImprisonment : ThingComp
{
    public int LastTryingEscapeTick;
    public override void PostExposeData()
    {
        base.PostExposeData();
        Scribe_Values.Look(ref LastTryingEscapeTick, "lastTryingEscapeTick");
    }
}

[StaticConstructorOnStartup]
public static class CompPropertiesPatch
{
    static CompPropertiesPatch()
    {
        foreach (ThingDef thingDef in DefDatabase<ThingDef>.AllDefs)
        {
            if (thingDef.race is { Humanlike: false }) continue;
            thingDef.comps.Add(new CompProperties_Imprisonment());
        }
    }
}