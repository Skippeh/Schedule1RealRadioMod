namespace VLB
{
	public class MinMaxRangeAttribute : global::System.Attribute
	{
		public float minValue { get; private set; }

		public float maxValue { get; private set; }

		public MinMaxRangeAttribute(float min, float max)
		{
		}
	}
}
