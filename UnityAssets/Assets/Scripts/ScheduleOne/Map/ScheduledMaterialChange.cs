namespace ScheduleOne.Map
{
	public class ScheduledMaterialChange : global::UnityEngine.MonoBehaviour
	{
		private enum EOnState
		{
			Undecided = 0,
			On = 1,
			Off = 2
		}

		public global::UnityEngine.MeshRenderer[] Renderers;

		public int MaterialIndex;

		[global::UnityEngine.Header("Settings")]
		public bool Enabled;

		public global::UnityEngine.Material OutsideTimeRangeMaterial;

		public global::UnityEngine.Material InsideTimeRangeMaterial;

		public int TimeRangeMin;

		public int TimeRangeMax;

		public int TimeRangeShift;

		public int TimeRangeRandomization;

		[global::UnityEngine.Range(0f, 1f)]
		public float TurnOnChance;

		private bool appliedInsideTimeRange;

		private global::ScheduleOne.Map.ScheduledMaterialChange.EOnState onState;

		private int randomShift;

		protected virtual void Start()
		{
		}

		protected virtual void Tick()
		{
		}

		private void SetMaterial(bool insideTimeRange)
		{
		}
	}
}
