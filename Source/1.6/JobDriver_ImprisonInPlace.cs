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
using Verse.AI.Group;
using Verse.Sound;

namespace CapturedPersons
{
	public class JobDriver_ImprisonInPlace : JobDriver
	{
		public const int ImprisonDurationTicks = 120;
		protected Pawn Prisoner => (Pawn)job.GetTarget(TargetIndex.A).Thing;
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
			Toil toil = new Toil();
			toil.initAction = delegate
			{
				if (job.def.makeTargetPrisoner)
				{
					Pawn pawn = (Pawn)job.targetA.Thing;
					pawn.GetLord()?.Notify_PawnAttemptArrested(pawn);
					GenClamor.DoClamor(pawn, 10f, ClamorDefOf.Harm);
					if (!pawn.IsPrisoner)
					{
						QuestUtility.SendQuestTargetSignals(pawn.questTags, "Arrested", pawn.Named("SUBJECT"));
					}
					if (!pawn.CheckAcceptArrest(base.pawn))
					{
						base.pawn.jobs.EndCurrentJob(JobCondition.Incompletable);
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
			Toils_General.Do(CheckMakeTakeeGuest);
			Toil toil2 = new Toil();
			toil2.initAction = delegate
			{
				CheckMakeTakeePrisoner();
				if (Prisoner.playerSettings == null)
				{
					Prisoner.playerSettings = new Pawn_PlayerSettings(Prisoner);
				}
			};
			yield return toil2;
		}

		private void CheckMakeTakeePrisoner()
		{
			if (job.def.makeTargetPrisoner)
			{
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
				var comp = pawn.TryGetComp<CompImprisonment>();
				comp.lastTryingEscapeTick = 0;
				CP_DefOf.CP_ArrestSound.PlayOneShot(new TargetInfo(Prisoner.Position, Prisoner.Map));
			}
		}

		private void CheckMakeTakeeGuest()
		{
			if (!job.def.makeTargetPrisoner && Prisoner.Faction != Faction.OfPlayer && Prisoner.HostFaction != Faction.OfPlayer && Prisoner.guest != null && !Prisoner.IsWildMan())
			{
				Prisoner.guest.SetGuestStatus(Faction.OfPlayer);
			}
		}
	}
}