using RimWorld;
using System;
using Verse;
using Verse.AI;

namespace CapturedPersons;

public class FloatMenuOptionProvider_ArrestHere : FloatMenuOptionProvider
{
	protected override bool Drafted => true;

	protected override bool Undrafted => true;

	protected override bool Multiselect => false;
	
	protected override bool AppliesInt(FloatMenuContext context)
	{
		return context.FirstSelectedPawn.health.capacities.CapableOf(PawnCapacityDefOf.Manipulation);
	}

	protected override FloatMenuOption GetSingleOptionFor(Pawn clickedPawn, FloatMenuContext context)
	{
		Pawn pawn = context.FirstSelectedPawn;
		bool escaping = clickedPawn.CurJobDef == JobDefOf.Goto && clickedPawn.CurJob.exitMapOnArrival && clickedPawn.mindState.lastJobTag == JobTag.Escaping;
		if (!clickedPawn.RaceProps.Humanlike || clickedPawn.DevelopmentalStage.Baby() || clickedPawn.IsPrisonerOfColony || !clickedPawn.Downed || escaping)
		{
			return null;
		}

		if (pawn.InSameExtraFaction(clickedPawn, ExtraFactionType.HomeFaction) || pawn.InSameExtraFaction(clickedPawn, ExtraFactionType.MiniFaction))
		{
			return null;
		}
		
		if (!pawn.CanReserveAndReach(clickedPawn, PathEndMode.OnCell, Danger.Deadly, 1, -1, null, true))
		{
			return null;
		}
		
		string label = "CP_ImprisonInPlace".Translate(clickedPawn.LabelCap);
		if (clickedPawn.Faction != null && clickedPawn.Faction != Faction.OfPlayer && !clickedPawn.Faction.Hidden && !clickedPawn.IsPrisonerOfColony && !clickedPawn.Faction.HostileTo(Faction.OfPlayer))
		{
			label += ": " + "AngersFaction".Translate().CapitalizeFirst();
		}
		
		Action action = delegate
		{
			Job job = JobMaker.MakeJob(CP_DefOf.CP_ImprisonInPlace, clickedPawn);
			job.count = 1;
			pawn.jobs.TryTakeOrderedJob(job);
			PlayerKnowledgeDatabase.KnowledgeDemonstrated(ConceptDefOf.Capturing, KnowledgeAmount.Total);
			if (clickedPawn.Faction != null && clickedPawn.Faction != Faction.OfPlayer && !clickedPawn.Faction.Hidden && !clickedPawn.Faction.HostileTo(Faction.OfPlayer) && !clickedPawn.IsPrisonerOfColony)
			{
				Messages.Message("MessageCapturingWillAngerFaction".Translate(clickedPawn.Named("PAWN")).AdjustedFor(clickedPawn), clickedPawn, MessageTypeDefOf.CautionInput, historical: false);
			}
		};
		
		return FloatMenuUtility.DecoratePrioritizedTask(new FloatMenuOption(label, action, MenuOptionPriority.Default, null, clickedPawn), context.FirstSelectedPawn, clickedPawn);
	}
}