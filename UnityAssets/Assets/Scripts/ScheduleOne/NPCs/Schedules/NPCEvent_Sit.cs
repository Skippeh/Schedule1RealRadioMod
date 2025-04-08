namespace ScheduleOne.NPCs.Schedules
{
	public class NPCEvent_Sit : global::ScheduleOne.NPCs.Schedules.NPCEvent
	{
		public const float DESTINATION_THRESHOLD = 1.5f;

		public global::ScheduleOne.AvatarFramework.Animation.AvatarSeatSet SeatSet;

		public bool WarpIfSkipped;

		private bool seated;

		private global::ScheduleOne.AvatarFramework.Animation.AvatarSeat targetSeat;

		public global::UnityEngine.Events.UnityEvent onSeated;

		public global::UnityEngine.Events.UnityEvent onStandUp;

		private bool NetworkInitialize___EarlyScheduleOne_002ENPCs_002ESchedules_002ENPCEvent_SitAssembly_002DCSharp_002Edll_Excuted;

		private bool NetworkInitialize__LateScheduleOne_002ENPCs_002ESchedules_002ENPCEvent_SitAssembly_002DCSharp_002Edll_Excuted;

		public new string ActionName => null;

		public override string GetName()
		{
			return null;
		}

		public override void Started()
		{
		}

		public override void OnSpawnServer(global::FishNet.Connection.NetworkConnection connection)
		{
		}

		public override void LateStarted()
		{
		}

		public override void ActiveMinPassed()
		{
		}

		public override void JumpTo()
		{
		}

		public override void End()
		{
		}

		public override void Interrupt()
		{
		}

		public override void Resume()
		{
		}

		public override void Skipped()
		{
		}

		private bool IsAtDestination()
		{
			return false;
		}

		protected override void WalkCallback(global::ScheduleOne.NPCs.NPCMovement.WalkResult result)
		{
		}

		[global::FishNet.Object.ObserversRpc(RunLocally = true)]
		[global::FishNet.Object.TargetRpc]
		protected virtual void StartAction(global::FishNet.Connection.NetworkConnection conn, int seatIndex)
		{
		}

		[global::FishNet.Object.ObserversRpc(RunLocally = true)]
		protected virtual void EndAction()
		{
		}

		public override void NetworkInitialize___Early()
		{
		}

		public override void NetworkInitialize__Late()
		{
		}

		public override void NetworkInitializeIfDisabled()
		{
		}

		private void RpcWriter___Observers_StartAction_2681120339(global::FishNet.Connection.NetworkConnection conn, int seatIndex)
		{
		}

		protected virtual void RpcLogic___StartAction_2681120339(global::FishNet.Connection.NetworkConnection conn, int seatIndex)
		{
		}

		private void RpcReader___Observers_StartAction_2681120339(global::FishNet.Serializing.PooledReader PooledReader0, global::FishNet.Transporting.Channel channel)
		{
		}

		private void RpcWriter___Target_StartAction_2681120339(global::FishNet.Connection.NetworkConnection conn, int seatIndex)
		{
		}

		private void RpcReader___Target_StartAction_2681120339(global::FishNet.Serializing.PooledReader PooledReader0, global::FishNet.Transporting.Channel channel)
		{
		}

		private void RpcWriter___Observers_EndAction_2166136261()
		{
		}

		protected virtual void RpcLogic___EndAction_2166136261()
		{
		}

		private void RpcReader___Observers_EndAction_2166136261(global::FishNet.Serializing.PooledReader PooledReader0, global::FishNet.Transporting.Channel channel)
		{
		}

		public override void Awake()
		{
		}
	}
}
