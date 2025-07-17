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
    public class CompProperties_Imprisonment : CompProperties
    {
        public CompProperties_Imprisonment()
        {
            this.compClass = typeof(CompImprisonment);
        }
    }

    public class CompImprisonment : ThingComp
    {
        public int lastTryingEscapeTick;
        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look(ref lastTryingEscapeTick, "lastTryingEscapeTick");
        }
    }
}
