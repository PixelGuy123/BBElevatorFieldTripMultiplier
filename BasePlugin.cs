using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;

namespace BBElevatorFieldTripMultiplier.Plugin
{
	[BepInPlugin(ModInfo.GUID, ModInfo.Name, ModInfo.Version)]
	public class BasePlugin : BaseUnityPlugin
	{
		public ConfigEntry<int> fieldTripAmount;
		public ConfigEntry<int> elevatorMultiplier;
		void Awake()
		{
			Harmony harmony = new(ModInfo.GUID);
			harmony.PatchAll();

			i = this;

			fieldTripAmount = Config.Bind("Settings", "FieldTrip Amount", 2);
			if (fieldTripAmount.Value <= 0)
				fieldTripAmount.BoxedValue = 1;

			elevatorMultiplier = Config.Bind("Settings", "Elevator Multiplier", 2);
			if (elevatorMultiplier.Value <= 0)
				elevatorMultiplier.BoxedValue = 1;
		}

		public static BasePlugin i;

	}


	internal static class ModInfo
	{
		internal const string GUID = "pixelguy.pixelmodding.baldiplus.bbelvtripmultiplier";
		internal const string Name = "BB+ Elevators & Field Trips Multiplier";
		internal const string Version = "1.0.0";
	}
}
