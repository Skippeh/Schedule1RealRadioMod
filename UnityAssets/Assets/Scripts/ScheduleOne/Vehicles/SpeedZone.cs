namespace ScheduleOne.Vehicles
{
	[global::UnityEngine.RequireComponent(typeof(global::UnityEngine.BoxCollider))]
	public class SpeedZone : global::UnityEngine.MonoBehaviour
	{
		public static global::System.Collections.Generic.List<global::ScheduleOne.Vehicles.SpeedZone> speedZones;

		public global::UnityEngine.BoxCollider col;

		public float speed;

		public virtual void Awake()
		{
		}

		public static global::System.Collections.Generic.List<global::ScheduleOne.Vehicles.SpeedZone> GetSpeedZones(global::UnityEngine.Vector3 point)
		{
			return null;
		}

		private void OnDrawGizmos()
		{
		}

		private void OnDrawGizmosSelected()
		{
		}
	}
}
