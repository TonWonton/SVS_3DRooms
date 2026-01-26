#nullable enable
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using HarmonyLib;
using ILLGames.Unity.Animations;

using Logging = SVS_3DRooms.ThreeDRoomsPlugin.Logging;
using SV.H;
using Character;

namespace SVS_3DRooms
{
	public static class PovXCompatibility
	{
		//Variables
		private static readonly Harmony _harmony = new Harmony(ThreeDRoomsPlugin.GUID + "PovXCompatibility");
		private static readonly HarmonyMethod _postSetChaControl = new HarmonyMethod(typeof(Hooks), nameof(Hooks.PostSetChaControl));
		private static readonly HarmonyMethod _postCameraPoVScene = new HarmonyMethod(typeof(Hooks), nameof(Hooks.PostCameraPoVScene));
		private static readonly HarmonyMethod _postNeckLookControllerVer2LateUpdate = new HarmonyMethod(typeof(Hooks), nameof(Hooks.NeckLookControllerVer2PostLateUpdate));
		private static MethodInfo? _setChaControlPatchTarget;
		private static MethodInfo? _postCameraPoVScenePatchTarget;
		private static MethodInfo? _neckLookControllerVer2LateUpdatePatchTarget;
		private static MethodInfo? _setChaControlPatch;
		private static MethodInfo? _postCameraPoVScenePatch;
		private static MethodInfo? _neckLookControllerVer2LateUpdatePatch;
		private static Type? _povXControllerType;
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
			Logging.Info("Trying to patch SVS_PovX compatibility");
			if (ThreeDRoomsPlugin.TryGetThreeDRoomsComponent(out ThreeDRoomsComponent? threeDRoomsComponent) && _postCameraPoVScenePatch == null)
			{
				//Try get SVS_PovX.Controller type
				if (_povXControllerType == null) _povXControllerType = AccessTools.TypeByName("SVS_PovX.Controller");

				//Try get target patch methods
				if (_setChaControlPatchTarget == null) _setChaControlPatchTarget = _povXControllerType?.GetMethod("SetChaControl", AccessTools.all);
				if (_postCameraPoVScenePatchTarget == null) _postCameraPoVScenePatchTarget = _povXControllerType?.GetMethod("CameraPoVScene", AccessTools.all);
				if (_neckLookControllerVer2LateUpdatePatchTarget == null) _neckLookControllerVer2LateUpdatePatchTarget = typeof(NeckLookControllerVer2).GetMethod(nameof(NeckLookControllerVer2.LateUpdate));

				//If everything succeeded apply patches
				if (_setChaControlPatchTarget != null && _postCameraPoVScenePatchTarget != null && _neckLookControllerVer2LateUpdatePatchTarget != null)
				{
					//Apply patches
					_setChaControlPatch = _harmony.Patch(_setChaControlPatchTarget, postfix: _postSetChaControl);
					_postCameraPoVScenePatch = _harmony.Patch(_postCameraPoVScenePatchTarget, postfix: _postCameraPoVScene);
					_neckLookControllerVer2LateUpdatePatch = _harmony.Patch(_neckLookControllerVer2LateUpdatePatchTarget, postfix: _postNeckLookControllerVer2LateUpdate);
					Logging.Info("SVS_PovX compatibility patch successful");
				}
			}
		}

		private static void OnThreeDRoomsComponentDestroyed()
		{
			Logging.Info("Unpatching SVS_PovX compatibility");
			
			_harmony.UnpatchSelf();
			_setChaControlPatch = null;
			_postCameraPoVScenePatch = null;
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
					threeDRoomsComponent.UpdateSimulationSceneCameraFOV();
					threeDRoomsComponent.UpdateCameraPositionAndRotation();
				}
			}

			//Enable camera LateUpdate in ThreeDRoomsComponent and update FOV and position once
			public static void NeckLookControllerVer2PostLateUpdate(NeckLookControllerVer2 __instance, bool __runOriginal)
			{
				if (_currentPOVNeckLookControllerVer2 == null || (__instance == _currentPOVNeckLookControllerVer2 && __runOriginal))
				{
					if (ThreeDRoomsPlugin.TryGetThreeDRoomsComponent(out ThreeDRoomsComponent? threeDRoomsComponent) && threeDRoomsComponent.TryDisablePovXCompatibility())
					{
						threeDRoomsComponent.UpdateSimulationSceneCameraFOV();
						threeDRoomsComponent.UpdateCameraPositionAndRotation();
					}
				}
			}
		}
	}
}