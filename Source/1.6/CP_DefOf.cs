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
    [DefOf]
    public static class CP_DefOf
    {
        public static JobDef CP_ImprisonInPlace;
        public static SoundDef CP_ArrestSound;
    }
}
