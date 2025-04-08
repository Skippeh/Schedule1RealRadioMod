namespace ScheduleOne.NPCs.Behaviour
{
	public class VehiclePursuitBehaviour : global::ScheduleOne.NPCs.Behaviour.Behaviour
	{
		public new const float MAX_CONSECUTIVE_PATHING_FAILURES = 5f;

		public const float EXTRA_VISIBILITY_TIME = 6f;

		public const float EXIT_VEHICLE_MAX_SPEED = 4f;

		public const float CLOSE_ENOUGH_THRESHOLD = 10f;

		public const float UPDATE_FREQUENCY = 0.2f;

		public const float STATIONARY_THRESHOLD = 1f;

		public const float TIME_STATIONARY_TO_EXIT = 3f;

		[global::UnityEngine.Header("Settings")]
		public global::UnityEngine.AnimationCurve RepathDistanceThresholdMap;

		public global::ScheduleOne.Vehicles.LandVehicle vehicle;

		private bool initialContactMade;

		private bool aggressiveDrivingEnabled;

		private bool isTargetVisible;

		private bool isTargetStrictlyVisible;

		private float playerSightedDuration;

		private float timeSinceLastSighting;

		private new int consecutivePathingFailures;

		private float timeStationary;

		private global::UnityEngine.Vector3 currentDriveTarget;

		private int targetChanges;

		private float timeSincePursuitStart;

		private bool beginAsSighted;

		private bool NetworkInitialize___EarlyScheduleOne_002ENPCs_002EBehaviour_002EVehiclePursuitBehaviourAssembly_002DCSharp_002Edll_Excuted;

		private bool NetworkInitialize__LateScheduleOne_002ENPCs_002EBehaviour_002EVehiclePursuitBehaviourAssembly_002DCSharp_002Edll_Excuted;

		public global::ScheduleOne.PlayerScripts.Player TargetPlayer { get; protected set; }

		private bool isDriving => false;

		private global::ScheduleOne.Vehicles.AI.VehicleAgent Agent => null;

		public override void Awake()
		{
		}

		private void OnDestroy()
		{
		}

		public void BeginAsSighted()
		{
		}

		protected override void Begin()
		{
		}

		protected override void Resume()
		{
		}

		protected override void Pause()
		{
		}

		protected override void End()
		{
		}

		public virtual void AssignTarget(global::ScheduleOne.PlayerScripts.Player target)
		{
		}

		private void StartPursuit()
		{
		}

		public override void BehaviourUpdate()
		{
		}

		public override void ActiveMinPass()
		{
		}

		protected virtual void FixedUpdate()
		{
		}

		private void UpdateDestination()
		{
		}

		private bool IsTargetValid()
		{
			return false;
		}

		private void CheckExitVehicle()
		{
		}

		private global::UnityEngine.Vector3 GetPlayerChasePoint()
		{
			return default(global::UnityEngine.Vector3);
		}

		private void SetAggressiveDriving(bool aggressive)
		{
		}

		private void DriveTo(global::UnityEngine.Vector3 location)
		{
		}

		private void NavigationCallback(global::ScheduleOne.Vehicles.AI.VehicleAgent.ENavigationResult status)
		{
		}

		private bool IsAsCloseAsPossible(global::UnityEngine.Vector3 pos, out global::UnityEngine.Vector3 closestPosition)
		{
			closestPosition = default(global::UnityEngine.Vector3);
			return false;
		}

		private bool IsPlayerVisible()
		{
			return false;
		}

		private void CheckPlayerVisibility()
		{
		}

		private void ProcessVisionEvent(global::ScheduleOne.Vision.VisionEventReceipt visionEventReceipt)
		{
		}

		private void ProcessThirdPartyVisionEvent(global::ScheduleOne.Vision.VisionEventReceipt visionEventReceipt)
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

		protected virtual void Awake_UserLogic_ScheduleOne_002ENPCs_002EBehaviour_002EVehiclePursuitBehaviour_Assembly_002DCSharp_002Edll()
		{
		}
	}
}
