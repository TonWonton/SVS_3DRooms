#nullable enable
using System;
using System.Reflection;
using BepInEx.Unity.IL2CPP;
using BepInEx;
using HarmonyLib;
using ILLGames.Unity.Animations;
using Character;

using Logging = SVS_3DRooms.ThreeDRoomsPlugin.Logging;


namespace SVS_3DRooms
{
	public static class PovXCompatibility
	{
		public const string POVX_GUID = "SVS_PovX";
		public const string POVX_CONTROLLER_FULL_TYPE_NAME = "SVS_PovX.Controller";

		//Harmony
		private static readonly Harmony _harmony = new Harmony(ThreeDRoomsPlugin.GUID + "PovXCompatibility");
		private static readonly HarmonyMethod _postSetChaControl = new HarmonyMethod(typeof(Hooks), nameof(Hooks.PostSetChaControl));
		private static readonly HarmonyMethod _postCameraPoVScene = new HarmonyMethod(typeof(Hooks), nameof(Hooks.PostCameraPoVScene));
		private static readonly HarmonyMethod _postNeckLookControllerVer2LateUpdate = new HarmonyMethod(typeof(Hooks), nameof(Hooks.NeckLookControllerVer2PostLateUpdate));

		//Patch targets
		private static Type? _povXControllerType;
		private static MethodInfo? _setChaControlPatchTarget;
		private static MethodInfo? _cameraPoVScenePatchTarget;
		private static MethodInfo? _neckLookControllerVer2LateUpdatePatchTarget;

		//Patches
		private static MethodInfo? _setChaControlPatch;
		private static MethodInfo? _cameraPoVScenePatch;
		private static MethodInfo? _neckLookControllerVer2LateUpdatePatch;

		//Current pov character
		private static Human? _currentPOVHuman;
		private static NeckLookControllerVer2? _currentPOVNeckLookControllerVer2;



		//Methods
		public static void Initialize()
		{
			ThreeDRoomsComponent.Started += OnThreeDRoomsComponentStarted;
			ThreeDRoomsComponent.Destroyed += OnThreeDRoomsComponentDestroyed;
		}



		//Event handling
		private static void OnThreeDRoomsComponentStarted()
		{
			if (IL2CPPChainloader.Instance.Plugins.TryGetValue(POVX_GUID, out PluginInfo? povXPluginInfo))
			{
				if (_setChaControlPatch == null && _cameraPoVScenePatch == null && _neckLookControllerVer2LateUpdatePatch == null)
				{
					Logging.Info("Trying to patch SVS_PovX compatibility");

					//Try get SVS_PovX.Controller type
					if (_povXControllerType == null)
					{
						System.Object? povXInstance = povXPluginInfo.Instance;
						if (povXInstance != null)
						{
							Assembly povXAssembly = povXInstance.GetType().Assembly;
							_povXControllerType = povXAssembly.GetType(POVX_CONTROLLER_FULL_TYPE_NAME);
						}
					}

					//Try get target patch methods
					if (_setChaControlPatchTarget == null) _setChaControlPatchTarget = _povXControllerType?.GetMethod("SetChaControl", AccessTools.all);
					if (_cameraPoVScenePatchTarget == null) _cameraPoVScenePatchTarget = _povXControllerType?.GetMethod("CameraPoVScene", AccessTools.all);
					if (_neckLookControllerVer2LateUpdatePatchTarget == null) _neckLookControllerVer2LateUpdatePatchTarget = typeof(NeckLookControllerVer2).GetMethod(nameof(NeckLookControllerVer2.LateUpdate));

					//If everything succeeded apply patches
					if (_setChaControlPatchTarget != null && _cameraPoVScenePatchTarget != null && _neckLookControllerVer2LateUpdatePatchTarget != null)
					{
						//Apply patches
						_setChaControlPatch = _harmony.Patch(_setChaControlPatchTarget, postfix: _postSetChaControl);
						_cameraPoVScenePatch = _harmony.Patch(_cameraPoVScenePatchTarget, postfix: _postCameraPoVScene);
						_neckLookControllerVer2LateUpdatePatch = _harmony.Patch(_neckLookControllerVer2LateUpdatePatchTarget, postfix: _postNeckLookControllerVer2LateUpdate);
						Logging.Info("SVS_PovX compatibility patch successful");
						return;
					}

					Logging.Warning("SVS_PovX compatibility patch failed");
				}
			}
		}

		private static void OnThreeDRoomsComponentDestroyed()
		{
			if (_setChaControlPatch != null || _cameraPoVScenePatch != null || _neckLookControllerVer2LateUpdatePatch != null)
			{
				Logging.Info("Unpatching SVS_PovX compatibility");
				_harmony.UnpatchSelf();
			}

			_setChaControlPatch = null;
			_cameraPoVScenePatch = null;
			_neckLookControllerVer2LateUpdatePatch = null;

			_currentPOVHuman = null;
			_currentPOVNeckLookControllerVer2 = null;
		}



		//Hooks
		public static class Hooks
		{
			//Hook into SVS_PovX to get current POV character
			public static void PostSetChaControl(Human? next)
			{
				if (next != null)
				{
					_currentPOVHuman = next;
					_currentPOVNeckLookControllerVer2 = next.face.neckLookCtrl;
				}
				else
				{
					_currentPOVHuman = null;
					_currentPOVNeckLookControllerVer2 = null;
				}
			}

			//Disable camera LateUpdate in ThreeDRoomsComponent and set after SVS_PovX changes instead
			public static void PostCameraPoVScene()
			{
				if (ThreeDRoomsPlugin.TryGetThreeDRoomsComponent(out ThreeDRoomsComponent? threeDRoomsComponent))
				{
					threeDRoomsComponent.SetPovXCompatibility();
					threeDRoomsComponent.UpdateCameraFOVPositionAndRotation();
				}
			}

			//Enable camera LateUpdate in ThreeDRoomsComponent and update FOV and position once
			public static void NeckLookControllerVer2PostLateUpdate(NeckLookControllerVer2 __instance, bool __runOriginal)
			{
				if (_currentPOVNeckLookControllerVer2 == null || (__instance == _currentPOVNeckLookControllerVer2 && __runOriginal))
				{
					if (ThreeDRoomsPlugin.TryGetThreeDRoomsComponent(out ThreeDRoomsComponent? threeDRoomsComponent) && threeDRoomsComponent.TryDisablePovXCompatibility())
					{
						threeDRoomsComponent.UpdateCameraFOVPositionAndRotation();
					}
				}
			}
		}
	}
}