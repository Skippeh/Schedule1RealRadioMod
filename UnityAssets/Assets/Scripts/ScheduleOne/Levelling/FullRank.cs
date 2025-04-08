namespace ScheduleOne.Levelling
{
	[global::System.Serializable]
	public struct FullRank
	{
		public global::ScheduleOne.Levelling.ERank Rank;

		[global::UnityEngine.Range(1f, 5f)]
		public int Tier;

		public FullRank(global::ScheduleOne.Levelling.ERank rank, int tier)
		{
			Rank = default(global::ScheduleOne.Levelling.ERank);
			Tier = 0;
		}

		public override string ToString()
		{
			return null;
		}

		public global::ScheduleOne.Levelling.FullRank NextRank()
		{
			return default(global::ScheduleOne.Levelling.FullRank);
		}

		public static string GetString(global::ScheduleOne.Levelling.FullRank rank)
		{
			return null;
		}

		public static bool operator >(global::ScheduleOne.Levelling.FullRank a, global::ScheduleOne.Levelling.FullRank b)
		{
			return false;
		}

		public static bool operator <(global::ScheduleOne.Levelling.FullRank a, global::ScheduleOne.Levelling.FullRank b)
		{
			return false;
		}

		public static bool operator <=(global::ScheduleOne.Levelling.FullRank a, global::ScheduleOne.Levelling.FullRank b)
		{
			return false;
		}

		public static bool operator >=(global::ScheduleOne.Levelling.FullRank a, global::ScheduleOne.Levelling.FullRank b)
		{
			return false;
		}

		public static bool operator ==(global::ScheduleOne.Levelling.FullRank a, global::ScheduleOne.Levelling.FullRank b)
		{
			return false;
		}

		public static bool operator !=(global::ScheduleOne.Levelling.FullRank a, global::ScheduleOne.Levelling.FullRank b)
		{
			return false;
		}

		public override bool Equals(object obj)
		{
			return false;
		}

		public override int GetHashCode()
		{
			return 0;
		}

		public int CompareTo(global::ScheduleOne.Levelling.FullRank other)
		{
			return 0;
		}
	}
}
