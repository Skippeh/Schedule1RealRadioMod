namespace ScheduleOne.Law
{
	public class CurfewManager : global::ScheduleOne.DevUtilities.NetworkSingleton<global::ScheduleOne.Law.CurfewManager>
	{
		public const int WARNING_TIME = 2030;

		public const int CURFEW_START_TIME = 2100;

		public const int CURFEW_END_TIME = 500;

		[global::UnityEngine.Header("References")]
		public global::ScheduleOne.ObjectScripts.VMSBoard[] VMSBoards;

		public global::ScheduleOne.Audio.AudioSourceController CurfewWarningSound;

		public global::ScheduleOne.Audio.AudioSourceController CurfewAlarmSound;

		public global::UnityEngine.Events.UnityEvent onCurfewEnabled;

		public global::UnityEngine.Events.UnityEvent onCurfewDisabled;

		public global::UnityEngine.Events.UnityEvent onCurfewHint;

		public global::UnityEngine.Events.UnityEvent onCurfewWarning;

		private bool warningPlayed;

		private bool NetworkInitialize___EarlyScheduleOne_002ELaw_002ECurfewManagerAssembly_002DCSharp_002Edll_Excuted;

		private bool NetworkInitialize__LateScheduleOne_002ELaw_002ECurfewManagerAssembly_002DCSharp_002Edll_Excuted;

		public bool IsEnabled { get; protected set; }

		public bool IsCurrentlyActive { get; protected set; }

		public bool IsCurrentlyActiveWithTolerance => false;

		protected override void Start()
		{
		}

		public override void OnSpawnServer(global::FishNet.Connection.NetworkConnection connection)
		{
		}

		[global::FishNet.Object.ObserversRpc]
		[global::FishNet.Object.TargetRpc]
		public void Enable(global::FishNet.Connection.NetworkConnection conn)
		{
		}

		[global::FishNet.Object.ObserversRpc]
		public void Disable()
		{
		}

		private void MinPass()
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

		private void RpcWriter___Observers_Enable_328543758(global::FishNet.Connection.NetworkConnection conn)
		{
		}

		public void RpcLogic___Enable_328543758(global::FishNet.Connection.NetworkConnection conn)
		{
		}

		private void RpcReader___Observers_Enable_328543758(global::FishNet.Serializing.PooledReader PooledReader0, global::FishNet.Transporting.Channel channel)
		{
		}

		private void RpcWriter___Target_Enable_328543758(global::FishNet.Connection.NetworkConnection conn)
		{
		}

		private void RpcReader___Target_Enable_328543758(global::FishNet.Serializing.PooledReader PooledReader0, global::FishNet.Transporting.Channel channel)
		{
		}

		private void RpcWriter___Observers_Disable_2166136261()
		{
		}

		public void RpcLogic___Disable_2166136261()
		{
		}

		private void RpcReader___Observers_Disable_2166136261(global::FishNet.Serializing.PooledReader PooledReader0, global::FishNet.Transporting.Channel channel)
		{
		}

		public override void Awake()
		{
		}
	}
}
