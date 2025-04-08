namespace ScheduleOne.Combat
{
	public class CombatBehaviour : global::ScheduleOne.NPCs.Behaviour.Behaviour
	{
		[global::System.Runtime.CompilerServices.CompilerGenerated]
		private sealed class _003CSearchRoutine_003Ed__68 : global::System.Collections.Generic.IEnumerator<object>, global::System.Collections.IEnumerator, global::System.IDisposable
		{
			private int _003C_003E1__state;

			private object _003C_003E2__current;

			public global::ScheduleOne.Combat.CombatBehaviour _003C_003E4__this;

			object global::System.Collections.Generic.IEnumerator<object>.Current
			{
				[global::System.Diagnostics.DebuggerHidden]
				get
				{
					return null;
				}
			}

			object global::System.Collections.IEnumerator.Current
			{
				[global::System.Diagnostics.DebuggerHidden]
				get
				{
					return null;
				}
			}

			[global::System.Diagnostics.DebuggerHidden]
			public _003CSearchRoutine_003Ed__68(int _003C_003E1__state)
			{
			}

			[global::System.Diagnostics.DebuggerHidden]
			void global::System.IDisposable.Dispose()
			{
			}

			private bool MoveNext()
			{
				return false;
			}

			bool global::System.Collections.IEnumerator.MoveNext()
			{
				//ILSpy generated this explicit interface implementation from .override directive in MoveNext
				return this.MoveNext();
			}

			[global::System.Diagnostics.DebuggerHidden]
			void global::System.Collections.IEnumerator.Reset()
			{
			}
		}

		public const float EXTRA_VISIBILITY_TIME = 2.5f;

		public const float SEARCH_RADIUS_MIN = 25f;

		public const float SEARCH_RADIUS_MAX = 60f;

		public const float SEARCH_SPEED = 0.4f;

		public const float CONSECUTIVE_MISS_ACCURACY_BOOST = 0.1f;

		public const float REACHED_DESTINATION_DISTANCE = 2f;

		[global::UnityEngine.Header("General Setttings")]
		public float GiveUpRange;

		public float GiveUpTime;

		public int GiveUpAfterSuccessfulHits;

		public bool PlayAngryVO;

		[global::UnityEngine.Header("Movement settings")]
		[global::UnityEngine.Range(0f, 1f)]
		public float DefaultMovementSpeed;

		[global::UnityEngine.Header("Weapon settings")]
		public global::ScheduleOne.AvatarFramework.Equipping.AvatarWeapon DefaultWeapon;

		public global::ScheduleOne.AvatarFramework.Equipping.AvatarMeleeWeapon VirtualPunchWeapon;

		[global::UnityEngine.Header("Search settings")]
		public float DefaultSearchTime;

		protected bool overrideTargetDistance;

		protected float targetDistanceOverride;

		protected bool isTargetRecentlyVisible;

		protected bool isTargetImmediatelyVisible;

		protected float timeSinceLastSighting;

		protected float playerSightedDuration;

		protected global::UnityEngine.Vector3 lastKnownTargetPosition;

		protected global::ScheduleOne.AvatarFramework.Equipping.AvatarWeapon currentWeapon;

		protected int successfulHits;

		protected int consecutiveMissedShots;

		protected global::UnityEngine.Coroutine rangedWeaponRoutine;

		protected global::UnityEngine.Coroutine searchRoutine;

		protected global::UnityEngine.Vector3 currentSearchDestination;

		protected bool hasSearchDestination;

		private float nextAngryVO;

		private bool NetworkInitialize___EarlyScheduleOne_002ECombat_002ECombatBehaviourAssembly_002DCSharp_002Edll_Excuted;

		private bool NetworkInitialize__LateScheduleOne_002ECombat_002ECombatBehaviourAssembly_002DCSharp_002Edll_Excuted;

		public global::ScheduleOne.PlayerScripts.Player TargetPlayer { get; protected set; }

		public bool IsSearching { get; protected set; }

		public float TimeSinceTargetReacquired { get; protected set; }

		public override void Awake()
		{
		}

		public override void OnSpawnServer(global::FishNet.Connection.NetworkConnection connection)
		{
		}

		[global::FishNet.Object.ObserversRpc(RunLocally = true)]
		public virtual void SetTarget(global::FishNet.Connection.NetworkConnection conn, global::FishNet.Object.NetworkObject target)
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

		public override void Disable()
		{
		}

		protected virtual void StartCombat()
		{
		}

		protected void EndCombat()
		{
		}

		public override void BehaviourUpdate()
		{
		}

		protected virtual void FixedUpdate()
		{
		}

		protected void UpdateTimeout()
		{
		}

		protected virtual void UpdateLookAt()
		{
		}

		protected void SetMovementSpeed(float speed)
		{
		}

		[global::FishNet.Object.ObserversRpc(RunLocally = true)]
		protected virtual void SetWeapon(string weaponPath)
		{
		}

		[global::FishNet.Object.ObserversRpc(RunLocally = true)]
		protected void ClearWeapon()
		{
		}

		protected virtual bool ReadyToAttack(bool checkTarget = true)
		{
			return false;
		}

		[global::FishNet.Object.ObserversRpc(RunLocally = true)]
		protected virtual void Attack()
		{
		}

		protected void SucessfulHit()
		{
		}

		protected void CheckPlayerVisibility()
		{
		}

		public void MarkPlayerVisible()
		{
		}

		protected bool IsPlayerVisible()
		{
			return false;
		}

		private void ProcessVisionEvent(global::ScheduleOne.Vision.VisionEventReceipt visionEventReceipt)
		{
		}

		protected virtual float GetSearchTime()
		{
			return 0f;
		}

		private void StartSearching()
		{
		}

		private void StopSearching()
		{
		}

		[global::System.Runtime.CompilerServices.IteratorStateMachine(typeof(global::ScheduleOne.Combat.CombatBehaviour._003CSearchRoutine_003Ed__68))]
		private global::System.Collections.IEnumerator SearchRoutine()
		{
			return null;
		}

		private global::UnityEngine.Vector3 GetNextSearchLocation()
		{
			return default(global::UnityEngine.Vector3);
		}

		protected bool IsTargetValid()
		{
			return false;
		}

		private void RepositionToTargetRange(global::UnityEngine.Vector3 origin)
		{
		}

		private global::UnityEngine.Vector3 GetRandomReachablePointNear(global::UnityEngine.Vector3 point, float randomRadius, float minDistance = 0f)
		{
			return default(global::UnityEngine.Vector3);
		}

		protected float GetMinTargetDistance()
		{
			return 0f;
		}

		protected float GetMaxTargetDistance()
		{
			return 0f;
		}

		protected bool IsTargetInRange(global::UnityEngine.Vector3 origin = default(global::UnityEngine.Vector3))
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

		private void RpcWriter___Observers_SetTarget_1824087381(global::FishNet.Connection.NetworkConnection conn, global::FishNet.Object.NetworkObject target)
		{
		}

		public virtual void RpcLogic___SetTarget_1824087381(global::FishNet.Connection.NetworkConnection conn, global::FishNet.Object.NetworkObject target)
		{
		}

		private void RpcReader___Observers_SetTarget_1824087381(global::FishNet.Serializing.PooledReader PooledReader0, global::FishNet.Transporting.Channel channel)
		{
		}

		private void RpcWriter___Observers_SetWeapon_3615296227(string weaponPath)
		{
		}

		protected virtual void RpcLogic___SetWeapon_3615296227(string weaponPath)
		{
		}

		private void RpcReader___Observers_SetWeapon_3615296227(global::FishNet.Serializing.PooledReader PooledReader0, global::FishNet.Transporting.Channel channel)
		{
		}

		private void RpcWriter___Observers_ClearWeapon_2166136261()
		{
		}

		protected void RpcLogic___ClearWeapon_2166136261()
		{
		}

		private void RpcReader___Observers_ClearWeapon_2166136261(global::FishNet.Serializing.PooledReader PooledReader0, global::FishNet.Transporting.Channel channel)
		{
		}

		private void RpcWriter___Observers_Attack_2166136261()
		{
		}

		protected virtual void RpcLogic___Attack_2166136261()
		{
		}

		private void RpcReader___Observers_Attack_2166136261(global::FishNet.Serializing.PooledReader PooledReader0, global::FishNet.Transporting.Channel channel)
		{
		}

		protected virtual void Awake_UserLogic_ScheduleOne_002ECombat_002ECombatBehaviour_Assembly_002DCSharp_002Edll()
		{
		}
	}
}
