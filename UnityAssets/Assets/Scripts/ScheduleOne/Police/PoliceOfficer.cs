namespace ScheduleOne.Police
{
	public class PoliceOfficer : global::ScheduleOne.NPCs.NPC
	{
		public const float DEACTIVATION_TIME = 1f;

		public const float INVESTIGATION_COOLDOWN = 60f;

		public const float INVESTIGATION_MAX_DISTANCE = 8f;

		public const float INVESTIGATION_MIN_VISIBILITY = 0.2f;

		public const float INVESTIGATION_CHECK_INTERVAL = 1f;

		public const float BODY_SEARCH_CHANCE_DEFAULT = 0.1f;

		public const float MIN_CHATTER_INTERVAL = 15f;

		public const float MAX_CHATTER_INTERVAL = 45f;

		public static global::System.Action<global::ScheduleOne.Vision.VisionEventReceipt> OnPoliceVisionEvent;

		public static global::System.Collections.Generic.List<global::ScheduleOne.Police.PoliceOfficer> Officers;

		public global::ScheduleOne.Vehicles.LandVehicle AssignedVehicle;

		[global::UnityEngine.Header("References")]
		public global::ScheduleOne.NPCs.Behaviour.PursuitBehaviour PursuitBehaviour;

		public global::ScheduleOne.NPCs.Behaviour.VehiclePursuitBehaviour VehiclePursuitBehaviour;

		public global::ScheduleOne.NPCs.Behaviour.BodySearchBehaviour BodySearchBehaviour;

		public global::ScheduleOne.NPCs.Behaviour.CheckpointBehaviour CheckpointBehaviour;

		public global::ScheduleOne.NPCs.Behaviour.FootPatrolBehaviour FootPatrolBehaviour;

		public global::ScheduleOne.FX.ProximityCircle ProxCircle;

		public global::ScheduleOne.NPCs.Behaviour.VehiclePatrolBehaviour VehiclePatrolBehaviour;

		public global::ScheduleOne.NPCs.Behaviour.SentryBehaviour SentryBehaviour;

		public global::ScheduleOne.VoiceOver.PoliceChatterVO ChatterVO;

		[global::UnityEngine.Header("Dialogue")]
		public global::ScheduleOne.Dialogue.DialogueContainer CheckpointDialogue;

		[global::UnityEngine.Header("Tools")]
		public global::ScheduleOne.AvatarFramework.Equipping.AvatarEquippable BatonPrefab;

		public global::ScheduleOne.AvatarFramework.Equipping.AvatarEquippable TaserPrefab;

		public global::ScheduleOne.AvatarFramework.Equipping.AvatarEquippable GunPrefab;

		[global::UnityEngine.Header("Settings")]
		public bool AutoDeactivate;

		public bool ChatterEnabled;

		[global::UnityEngine.Header("Behaviour Settings")]
		[global::UnityEngine.Range(0f, 1f)]
		public float Suspicion;

		[global::UnityEngine.Range(0f, 1f)]
		public float Leniency;

		[global::UnityEngine.Header("Body Search Settings")]
		[global::UnityEngine.Range(0f, 1f)]
		public float BodySearchChance;

		[global::UnityEngine.Range(1f, 10f)]
		public float BodySearchDuration;

		[global::UnityEngine.HideInInspector]
		public global::ScheduleOne.AvatarFramework.Customization.PoliceBelt belt;

		private float timeSinceReadyToPool;

		private float timeSinceOutOfSight;

		private float chatterCountDown;

		private global::ScheduleOne.Police.Investigation currentBodySearchInvestigation;

		public global::FishNet.Object.Synchronizing.SyncVar<global::FishNet.Object.NetworkObject> syncVar____003CTargetPlayerNOB_003Ek__BackingField;

		private bool NetworkInitialize___EarlyScheduleOne_002EPolice_002EPoliceOfficerAssembly_002DCSharp_002Edll_Excuted;

		private bool NetworkInitialize__LateScheduleOne_002EPolice_002EPoliceOfficerAssembly_002DCSharp_002Edll_Excuted;

		public global::FishNet.Object.NetworkObject TargetPlayerNOB
		{
			[global::System.Runtime.CompilerServices.CompilerGenerated]
			get
			{
				return null;
			}
			[global::System.Runtime.CompilerServices.CompilerGenerated]
			protected set
			{
			}
		}

		public global::FishNet.Object.NetworkObject SyncAccessor__003CTargetPlayerNOB_003Ek__BackingField
		{
			get
			{
				return null;
			}
			set
			{
			}
		}

		public override void Awake()
		{
		}

		protected override void Start()
		{
		}

		protected override void Update()
		{
		}

		protected void FixedUpdate()
		{
		}

		protected override void MinPass()
		{
		}

		private void CheckDeactivation()
		{
		}

		[global::FishNet.Object.ServerRpc(RunLocally = true, RequireOwnership = false)]
		public virtual void BeginFootPursuit_Networked(global::FishNet.Object.NetworkObject target, bool includeColleagues = true)
		{
		}

		[global::FishNet.Object.ObserversRpc(RunLocally = true)]
		private void BeginFootPursuitTest(string playerCode)
		{
		}

		[global::FishNet.Object.ServerRpc(RunLocally = true, RequireOwnership = false)]
		public virtual void BeginVehiclePursuit_Networked(global::FishNet.Object.NetworkObject target, global::FishNet.Object.NetworkObject vehicle, bool beginAsSighted)
		{
		}

		[global::FishNet.Object.ObserversRpc(RunLocally = true)]
		private void BeginVehiclePursuit(global::FishNet.Object.NetworkObject target, global::FishNet.Object.NetworkObject vehicle, bool beginAsSighted)
		{
		}

		public void BeginBodySearch_LocalPlayer()
		{
		}

		[global::FishNet.Object.ServerRpc(RunLocally = true, RequireOwnership = false)]
		public virtual void BeginBodySearch_Networked(global::FishNet.Object.NetworkObject target)
		{
		}

		[global::FishNet.Object.ObserversRpc(RunLocally = true)]
		private void BeginBodySearch(global::FishNet.Object.NetworkObject target)
		{
		}

		[global::FishNet.Object.ObserversRpc(RunLocally = true)]
		public virtual void AssignToCheckpoint(global::ScheduleOne.Law.CheckpointManager.ECheckpointLocation location)
		{
		}

		public void UnassignFromCheckpoint()
		{
		}

		public void StartFootPatrol(global::ScheduleOne.NPCs.Behaviour.PatrolGroup group, bool warpToStartPoint)
		{
		}

		public void StartVehiclePatrol(global::ScheduleOne.NPCs.Behaviour.VehiclePatrolRoute route, global::ScheduleOne.Vehicles.LandVehicle vehicle)
		{
		}

		public virtual void AssignToSentryLocation(global::ScheduleOne.Law.SentryLocation location)
		{
		}

		public void UnassignFromSentryLocation()
		{
		}

		public void Activate()
		{
		}

		public void Deactivate()
		{
		}

		protected override bool ShouldNoticeGeneralCrime(global::ScheduleOne.PlayerScripts.Player player)
		{
			return false;
		}

		public override bool ShouldSave()
		{
			return false;
		}

		public override string GetNameAddress()
		{
			return null;
		}

		private void UpdateChatter()
		{
		}

		private void ProcessVisionEvent(global::ScheduleOne.Vision.VisionEventReceipt visionEventReceipt)
		{
		}

		public virtual void UpdateBodySearch()
		{
		}

		private bool CanInvestigate()
		{
			return false;
		}

		private void UpdateExistingInvestigation()
		{
		}

		private void CheckNewInvestigation()
		{
		}

		private void StartBodySearchInvestigation(global::ScheduleOne.PlayerScripts.Player player)
		{
		}

		private void StopBodySearchInvestigation()
		{
		}

		public void ConductBodySearch(global::ScheduleOne.PlayerScripts.Player player)
		{
		}

		private bool CanInvestigatePlayer(global::ScheduleOne.PlayerScripts.Player player)
		{
			return false;
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

		private void RpcWriter___Server_BeginFootPursuit_Networked_419679943(global::FishNet.Object.NetworkObject target, bool includeColleagues = true)
		{
		}

		public virtual void RpcLogic___BeginFootPursuit_Networked_419679943(global::FishNet.Object.NetworkObject target, bool includeColleagues = true)
		{
		}

		private void RpcReader___Server_BeginFootPursuit_Networked_419679943(global::FishNet.Serializing.PooledReader PooledReader0, global::FishNet.Transporting.Channel channel, global::FishNet.Connection.NetworkConnection conn)
		{
		}

		private void RpcWriter___Observers_BeginFootPursuitTest_3615296227(string playerCode)
		{
		}

		private void RpcLogic___BeginFootPursuitTest_3615296227(string playerCode)
		{
		}

		private void RpcReader___Observers_BeginFootPursuitTest_3615296227(global::FishNet.Serializing.PooledReader PooledReader0, global::FishNet.Transporting.Channel channel)
		{
		}

		private void RpcWriter___Server_BeginVehiclePursuit_Networked_2261819652(global::FishNet.Object.NetworkObject target, global::FishNet.Object.NetworkObject vehicle, bool beginAsSighted)
		{
		}

		public virtual void RpcLogic___BeginVehiclePursuit_Networked_2261819652(global::FishNet.Object.NetworkObject target, global::FishNet.Object.NetworkObject vehicle, bool beginAsSighted)
		{
		}

		private void RpcReader___Server_BeginVehiclePursuit_Networked_2261819652(global::FishNet.Serializing.PooledReader PooledReader0, global::FishNet.Transporting.Channel channel, global::FishNet.Connection.NetworkConnection conn)
		{
		}

		private void RpcWriter___Observers_BeginVehiclePursuit_2261819652(global::FishNet.Object.NetworkObject target, global::FishNet.Object.NetworkObject vehicle, bool beginAsSighted)
		{
		}

		private void RpcLogic___BeginVehiclePursuit_2261819652(global::FishNet.Object.NetworkObject target, global::FishNet.Object.NetworkObject vehicle, bool beginAsSighted)
		{
		}

		private void RpcReader___Observers_BeginVehiclePursuit_2261819652(global::FishNet.Serializing.PooledReader PooledReader0, global::FishNet.Transporting.Channel channel)
		{
		}

		private void RpcWriter___Server_BeginBodySearch_Networked_3323014238(global::FishNet.Object.NetworkObject target)
		{
		}

		public virtual void RpcLogic___BeginBodySearch_Networked_3323014238(global::FishNet.Object.NetworkObject target)
		{
		}

		private void RpcReader___Server_BeginBodySearch_Networked_3323014238(global::FishNet.Serializing.PooledReader PooledReader0, global::FishNet.Transporting.Channel channel, global::FishNet.Connection.NetworkConnection conn)
		{
		}

		private void RpcWriter___Observers_BeginBodySearch_3323014238(global::FishNet.Object.NetworkObject target)
		{
		}

		private void RpcLogic___BeginBodySearch_3323014238(global::FishNet.Object.NetworkObject target)
		{
		}

		private void RpcReader___Observers_BeginBodySearch_3323014238(global::FishNet.Serializing.PooledReader PooledReader0, global::FishNet.Transporting.Channel channel)
		{
		}

		private void RpcWriter___Observers_AssignToCheckpoint_4087078542(global::ScheduleOne.Law.CheckpointManager.ECheckpointLocation location)
		{
		}

		public virtual void RpcLogic___AssignToCheckpoint_4087078542(global::ScheduleOne.Law.CheckpointManager.ECheckpointLocation location)
		{
		}

		private void RpcReader___Observers_AssignToCheckpoint_4087078542(global::FishNet.Serializing.PooledReader PooledReader0, global::FishNet.Transporting.Channel channel)
		{
		}

		public virtual bool ReadSyncVar___ScheduleOne_002EPolice_002EPoliceOfficer(global::FishNet.Serializing.PooledReader PooledReader0, uint UInt321, bool Boolean2)
		{
			return false;
		}

		protected virtual void Awake_UserLogic_ScheduleOne_002EPolice_002EPoliceOfficer_Assembly_002DCSharp_002Edll()
		{
		}
	}
}
