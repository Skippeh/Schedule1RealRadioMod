namespace ScheduleOne.NPCs.Behaviour
{
	public class ConsumeProductBehaviour : global::ScheduleOne.NPCs.Behaviour.Behaviour
	{
		public global::ScheduleOne.AvatarFramework.Equipping.AvatarEquippable JointPrefab;

		public global::ScheduleOne.AvatarFramework.Equipping.AvatarEquippable PipePrefab;

		private global::ScheduleOne.Product.ProductItemInstance product;

		private global::UnityEngine.Coroutine consumeRoutine;

		public global::ScheduleOne.Audio.AudioSourceController WeedConsumeSound;

		public global::ScheduleOne.Audio.AudioSourceController MethConsumeSound;

		public global::ScheduleOne.Audio.AudioSourceController SnortSound;

		public global::UnityEngine.ParticleSystem SmokeExhaleParticles;

		[global::UnityEngine.Header("Debug")]
		public global::ScheduleOne.Product.ProductDefinition TestProduct;

		public global::UnityEngine.Events.UnityEvent onConsumeDone;

		private bool NetworkInitialize___EarlyScheduleOne_002ENPCs_002EBehaviour_002EConsumeProductBehaviourAssembly_002DCSharp_002Edll_Excuted;

		private bool NetworkInitialize__LateScheduleOne_002ENPCs_002EBehaviour_002EConsumeProductBehaviourAssembly_002DCSharp_002Edll_Excuted;

		public global::ScheduleOne.Product.ProductItemInstance ConsumedProduct { get; private set; }

		protected virtual void Start()
		{
		}

		[global::FishNet.Object.ServerRpc(RequireOwnership = false, RunLocally = true)]
		public void SendProduct(global::ScheduleOne.Product.ProductItemInstance _product)
		{
		}

		[global::FishNet.Object.ObserversRpc(RunLocally = true)]
		public void SetProduct(global::ScheduleOne.Product.ProductItemInstance _product)
		{
		}

		[global::FishNet.Object.ObserversRpc(RunLocally = true)]
		public void ClearEffects()
		{
		}

		protected override void Begin()
		{
		}

		protected override void Resume()
		{
		}

		private void TryConsume()
		{
		}

		public override void Disable()
		{
		}

		protected override void End()
		{
		}

		private void ConsumeWeed()
		{
		}

		private void ConsumeMeth()
		{
		}

		private void ConsumeCocaine()
		{
		}

		[global::FishNet.Object.ObserversRpc]
		private void ApplyEffects()
		{
		}

		private void Clear()
		{
		}

		private void DayPass(int minsSlept)
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

		private void RpcWriter___Server_SendProduct_2622925554(global::ScheduleOne.Product.ProductItemInstance _product)
		{
		}

		public void RpcLogic___SendProduct_2622925554(global::ScheduleOne.Product.ProductItemInstance _product)
		{
		}

		private void RpcReader___Server_SendProduct_2622925554(global::FishNet.Serializing.PooledReader PooledReader0, global::FishNet.Transporting.Channel channel, global::FishNet.Connection.NetworkConnection conn)
		{
		}

		private void RpcWriter___Observers_SetProduct_2622925554(global::ScheduleOne.Product.ProductItemInstance _product)
		{
		}

		public void RpcLogic___SetProduct_2622925554(global::ScheduleOne.Product.ProductItemInstance _product)
		{
		}

		private void RpcReader___Observers_SetProduct_2622925554(global::FishNet.Serializing.PooledReader PooledReader0, global::FishNet.Transporting.Channel channel)
		{
		}

		private void RpcWriter___Observers_ClearEffects_2166136261()
		{
		}

		public void RpcLogic___ClearEffects_2166136261()
		{
		}

		private void RpcReader___Observers_ClearEffects_2166136261(global::FishNet.Serializing.PooledReader PooledReader0, global::FishNet.Transporting.Channel channel)
		{
		}

		private void RpcWriter___Observers_ApplyEffects_2166136261()
		{
		}

		private void RpcLogic___ApplyEffects_2166136261()
		{
		}

		private void RpcReader___Observers_ApplyEffects_2166136261(global::FishNet.Serializing.PooledReader PooledReader0, global::FishNet.Transporting.Channel channel)
		{
		}

		public override void Awake()
		{
		}
	}
}
