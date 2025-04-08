namespace ScheduleOne.Dialogue
{
	public class DialogueController_ArmsDealer : global::ScheduleOne.Dialogue.DialogueController
	{
		[global::System.Serializable]
		public class WeaponOption
		{
			public string Name;

			public float Price;

			public bool IsAvailable;

			public string NotAvailableReason;

			public global::ScheduleOne.ItemFramework.StorableItemDefinition Item;
		}

		public global::System.Collections.Generic.List<global::ScheduleOne.Dialogue.DialogueController_ArmsDealer.WeaponOption> MeleeWeapons;

		public global::System.Collections.Generic.List<global::ScheduleOne.Dialogue.DialogueController_ArmsDealer.WeaponOption> RangedWeapons;

		public global::System.Collections.Generic.List<global::ScheduleOne.Dialogue.DialogueController_ArmsDealer.WeaponOption> Ammo;

		private global::System.Collections.Generic.List<global::ScheduleOne.Dialogue.DialogueController_ArmsDealer.WeaponOption> allWeapons;

		private global::ScheduleOne.Dialogue.DialogueController_ArmsDealer.WeaponOption chosenWeapon;

		private void Awake()
		{
		}

		public override void ChoiceCallback(string choiceLabel)
		{
		}

		public override void ModifyChoiceList(string dialogueLabel, ref global::System.Collections.Generic.List<global::ScheduleOne.Dialogue.DialogueChoiceData> existingChoices)
		{
		}

		private global::System.Collections.Generic.List<global::ScheduleOne.Dialogue.DialogueChoiceData> GetWeaponChoices(global::System.Collections.Generic.List<global::ScheduleOne.Dialogue.DialogueController_ArmsDealer.WeaponOption> options)
		{
			return null;
		}

		public override bool CheckChoice(string choiceLabel, out string invalidReason)
		{
			invalidReason = null;
			return false;
		}

		public override string ModifyDialogueText(string dialogueLabel, string dialogueText)
		{
			return null;
		}
	}
}
