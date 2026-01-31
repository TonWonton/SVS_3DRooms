#nullable enable
using System;
using System.Diagnostics.CodeAnalysis;
using BepInEx;
using BepInEx.Unity.IL2CPP;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;

using LogLevel = BepInEx.Logging.LogLevel;


namespace SVS_3DRooms
{
	[BepInProcess(PROCESS_NAME)]
	[BepInPlugin(GUID, PLUGIN_NAME, VERSION)]
	public partial class ThreeDRoomsPlugin : BasePlugin
	{
		#region PLUGIN_INFO

		/*PLUGIN INFO*/
		public const string PLUGIN_NAME = "SVS_3DRooms";
		public const string COPYRIGHT = "";
		public const string COMPANY = "https://github.com/TonWonton/SVS_3DRooms";

		public const string PROCESS_NAME = "SamabakeScramble";
		public const string GUID = "SVS_3DRooms";
		public const string VERSION = "1.1.2";

		#endregion



		/*VARIABLES*/
		//Instance
		public static ThreeDRoomsPlugin Instance { get; private set; } = null!;
		private static ManualLogSource _log = null!;



		/*METHODS*/
		public static bool TryGetThreeDRoomsComponent([MaybeNullWhen(false)] out ThreeDRoomsComponent threeDRoomsComponent)
		{
			threeDRoomsComponent = ThreeDRoomsComponent.Instance;
			return threeDRoomsComponent != null;
		}

		public static ThreeDRoomsComponent GetOrAddThreeDRoomsComponent()
		{
			ThreeDRoomsComponent? threeDRoomsComponent = ThreeDRoomsComponent.Instance;

			if (threeDRoomsComponent != null)
			{
				return threeDRoomsComponent;
			}
			else
			{
				return Instance.AddComponent<ThreeDRoomsComponent>();
			}
		}



		/*EVENT HANDLING*/
		private static void OnEnabledChanged(object? sender, EventArgs args)
		{
			if (TryGetThreeDRoomsComponent(out ThreeDRoomsComponent? threeDRoomsComponent))
			{
				//Update displays
				threeDRoomsComponent.SetBGBlurAndFramesDisplay();
				threeDRoomsComponent.SetSimulationSceneModelsDisplay();

				//Update camera config
				threeDRoomsComponent.UpdateCameraConfig();
			}
		}



		/*CONFIG*/
		public const string CATEGORY_3DROOMS = "3DRooms";
		public static ConfigEntry<bool> enabled = null!;


		/*PLUGIN LOAD*/
		public override void Load()
		{
			//Instance
			Instance = this;
			_log = Log;

			//Config
			enabled = Config.Bind(CATEGORY_3DROOMS, "Enabled", true);
			enabled.SettingChanged += OnEnabledChanged;

			//Compatibility
			PovXCompatibility.Initialize();

			//Create hooks
			Harmony.CreateAndPatchAll(typeof(ThreeDRoomsComponent.Hooks), GUID);
			Logging.Info("Loaded");
		}



		//Logging
		public static class Logging
		{
			public static void Log(LogLevel level, string message)
			{
				_log.Log(level, message);
			}

			public static void Fatal(string message)
			{
				_log.LogFatal(message);
			}

			public static void Error(string message)
			{
				_log.LogError(message);
			}

			public static void Warning(string message)
			{
				_log.LogWarning(message);
			}

			public static void Message(string message)
			{
				_log.LogMessage(message);
			}

			public static void Info(string message)
			{
				_log.LogInfo(message);
			}

			public static void Debug(string message)
			{
				_log.LogDebug(message);
			}
		}
	}
}