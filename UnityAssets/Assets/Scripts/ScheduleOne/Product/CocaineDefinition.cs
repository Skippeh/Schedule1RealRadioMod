namespace ScheduleOne.Product
{
	[global::System.Serializable]
	[global::UnityEngine.CreateAssetMenu(fileName = "CocaineDefinition", menuName = "ScriptableObjects/Item Definitions/CocaineDefinition", order = 1)]
	public class CocaineDefinition : global::ScheduleOne.Product.ProductDefinition
	{
		[global::UnityEngine.Header("Materials")]
		public global::UnityEngine.Material RockMaterial;

		public global::ScheduleOne.Product.CocaineAppearanceSettings AppearanceSettings { get; private set; }

		public override global::ScheduleOne.ItemFramework.ItemInstance GetDefaultInstance(int quantity = 1)
		{
			return null;
		}

		public void Initialize(global::System.Collections.Generic.List<global::ScheduleOne.Properties.Property> properties, global::System.Collections.Generic.List<global::ScheduleOne.Product.EDrugType> drugTypes, global::ScheduleOne.Product.CocaineAppearanceSettings _appearance)
		{
		}

		public override string GetSaveString()
		{
			return null;
		}

		public static global::ScheduleOne.Product.CocaineAppearanceSettings GetAppearanceSettings(global::System.Collections.Generic.List<global::ScheduleOne.Properties.Property> properties)
		{
			return null;
		}
	}
}
