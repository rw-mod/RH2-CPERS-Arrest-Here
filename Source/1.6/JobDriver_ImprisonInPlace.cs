using RimWorld;
using System.Collections.Generic;
using Verse;
using Verse.AI;
using Verse.AI.Group;
using Verse.Sound;

namespace CapturedPersons;

public class JobDriver_ImprisonInPlace : JobDriver
{
	private const int ImprisonDurationTicks = 120;
	private Pawn Prisoner => (Pawn)job.GetTarget(TargetIndex.A).Thing;
	public override bool TryMakePreToilReservations(bool errorOnFailed)
	{
		return pawn.Reserve(Prisoner, job, 1, -1, null, errorOnFailed);
	}
	protected override IEnumerable<Toil> MakeNewToils()
	{
		this.FailOnDestroyedOrNull(TargetIndex.A);
		this.FailOnAggroMentalStateAndHostile(TargetIndex.A);
		yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.ClosestTouch)
			.FailOnDespawnedNullOrForbidden(TargetIndex.A)
			.FailOnSomeonePhysicallyInteracting(TargetIndex.A);
		
		Toil toil = new Toil
		{
			initAction = delegate
			{
				if (!job.def.makeTargetPrisoner) return;
				
				Pawn targetPawn = (Pawn)job.targetA.Thing;
				targetPawn.GetLord()?.Notify_PawnAttemptArrested(targetPawn);
				GenClamor.DoClamor(targetPawn, 10f, ClamorDefOf.Harm);
				if (!targetPawn.IsPrisoner)
				{
					QuestUtility.SendQuestTargetSignals(targetPawn.questTags, "Arrested", targetPawn.Named("SUBJECT"));
				}
				if (!targetPawn.CheckAcceptArrest(pawn))
				{
					pawn.jobs.EndCurrentJob(JobCondition.Incompletable);
				}
				else
				{
					Prisoner.jobs.StopAll();
					Prisoner.pather.StopDead();
					Prisoner.stances.stunner.StunFor(ImprisonDurationTicks, base.pawn);
				}
			}
		};
		yield return toil;
		
		Toil imprisonProgressToil = Toils_General.Wait(ImprisonDurationTicks);
		imprisonProgressToil.WithProgressBarToilDelay(TargetIndex.A, true);
		yield return imprisonProgressToil;
		Toils_General.Do(CheckMakeTakeGuest);
		
		Toil toil2 = new Toil
		{
			initAction = delegate
			{
				CheckMakeTakePrisoner();
				Prisoner.playerSettings ??= new Pawn_PlayerSettings(Prisoner);
			}
		};
		yield return toil2;
	}

	private void CheckMakeTakePrisoner()
	{
		if (!job.def.makeTargetPrisoner) return;
		
		if (Prisoner.guest.Released)
		{
			Prisoner.guest.Released = false;
			Prisoner.guest.SetNoInteraction();
			GenGuest.RemoveHealthyPrisonerReleasedThoughts(Prisoner);
		}
		
		if (!Prisoner.IsPrisonerOfColony)
		{
			Prisoner.guest.CapturedBy(Faction.OfPlayer, pawn);
		}
		
		CompImprisonment comp = pawn.TryGetComp<CompImprisonment>();
		comp.LastTryingEscapeTick = 0;
		CP_DefOf.CP_ArrestSound.PlayOneShot(new TargetInfo(Prisoner.Position, Prisoner.Map));
	}

	private void CheckMakeTakeGuest()
	{
		if (!job.def.makeTargetPrisoner && Prisoner.Faction != Faction.OfPlayer && Prisoner.HostFaction != Faction.OfPlayer && Prisoner.guest != null && !Prisoner.IsWildMan())
		{
			Prisoner.guest.SetGuestStatus(Faction.OfPlayer);
		}
	}
}