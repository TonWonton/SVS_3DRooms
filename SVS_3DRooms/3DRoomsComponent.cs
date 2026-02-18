#nullable enable
using System;
using System.Collections.Generic;
using HarmonyLib;
using BepInEx;
using BepInEx.Unity.IL2CPP;
using ILLGames.Unity.Component;
using Manager;
using SV;
using SV.H;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using Character;

using Logging = SVS_3DRooms.ThreeDRoomsPlugin.Logging;
using Il2CppCollections = Il2CppSystem.Collections.Generic;
using Il2CppArrays = Il2CppInterop.Runtime.InteropTypes.Arrays;


namespace SVS_3DRooms
{
	public class ThreeDRoomsComponent : MonoBehaviour
	{
		public const int HSCENE_CAM_ORIG_STACK_INDEX = 4;
		public const int SIM_CAM_ORIG_STACK_INDEX = 1;

		/*VARIABLES*/
		//Instance
		public static ThreeDRoomsComponent? Instance { get; private set; }
		private bool _isPovXCompatibility = false;

		//Camera
		private Il2CppCollections.List<Camera> _cameraStackList = null!;

		//HScene
		private HScene _hScene = null!;
		private Camera _hSceneCamera = null!;
		private UniversalAdditionalCameraData _hSceneCameraData = null!;
		private Transform _hSceneCameraTransform = null!;
		private int _hSceneCameraOriginalStackIndex = HSCENE_CAM_ORIG_STACK_INDEX;
		private bool _hSceneCameraOriginalClearDepth = true;

		//SimulationScene
		private SimulationScene _simulationScene = null!;
		private Camera _simulationSceneCamera = null!;
		private UniversalAdditionalCameraData _simulationSceneCameraData = null!;
		private Transform _simulationSceneCameraTransform = null!;
		private float _simulationSceneCameraOriginalFOV = 50f;
		private int _simulationSceneCameraOriginalStackIndex = SIM_CAM_ORIG_STACK_INDEX;
		private bool _simulationSceneCameraOriginalClearDepth = true;

		//Saved positions
		private Vector3 _simulationSceneCameraOriginalPosition;
		private Quaternion _simulationSceneCameraOriginalRotation;
		private Vector3 _simulationSceneCameraInitialPosition;
		private Quaternion _simulationSceneCameraInitialRotation;

		//BG blur and frame
		private GameObject _bgBlur = null!;
		private GameObject _bgFrameTop = null!;
		private GameObject _bgFrameBottom = null!;

		//Events
		public static event Action? Started;
		public static event Action? Destroyed;



		/*METHODS*/
		public void SetPovXCompatibility() { _isPovXCompatibility = true; }
		public bool TryDisablePovXCompatibility() { if (_isPovXCompatibility == true) { _isPovXCompatibility = false; return true; } else return false; }

		private void ShowSimulationSceneModels()
		{
			foreach (SV.Chara.AI simulationAI in _hScene.parameter.LowPolyAI)
			{
				if (simulationAI != null)
				{
					simulationAI.chaCtrl.visibleAll = true;
					simulationAI.objCircle.SetActive(true);
				}
			}
		}

		public void SetSimulationSceneModelsDisplay()
		{
			bool shouldShow = !ThreeDRoomsPlugin.enabled.Value;

			foreach (SV.Chara.AI simulationAI in _hScene.parameter.LowPolyAI)
			{
				if (simulationAI != null)
				{
					simulationAI.chaCtrl.visibleAll = shouldShow;
					simulationAI.objCircle.SetActive(shouldShow);
				}
			}
		}

		private void ShowBGBlurAndFrames()
		{
			_bgBlur.SetActive(true);
			_bgFrameTop.SetActive(true);
			_bgFrameBottom.SetActive(true);
		}

		public void SetBGBlurAndFramesDisplay()
		{
			bool shouldShow = !ThreeDRoomsPlugin.enabled.Value;

			_bgBlur.SetActive(shouldShow);
			_bgFrameTop.SetActive(shouldShow);
			_bgFrameBottom.SetActive(shouldShow);
		}

		public void ResetCamera()
		{
			//Restore SimulationScene camera view
			_simulationSceneCameraTransform.SetPositionAndRotation(_simulationSceneCameraOriginalPosition, _simulationSceneCameraOriginalRotation);

			//Restore camera stack order
			Il2CppCollections.List<Camera> cameraStackList = _cameraStackList;
			cameraStackList.Remove(_simulationSceneCamera);
			if (_simulationSceneCameraOriginalStackIndex >= 0 && _simulationSceneCameraOriginalStackIndex < cameraStackList.Count)
			{
				cameraStackList.Insert(_simulationSceneCameraOriginalStackIndex, _simulationSceneCamera);
			}
			else
			{
				cameraStackList.Add(_simulationSceneCamera);
			}

			//Restore FOV and clear depth flag
			_simulationSceneCamera.fieldOfView = _simulationSceneCameraOriginalFOV;
			_simulationSceneCameraData.m_ClearDepth = _simulationSceneCameraOriginalClearDepth;
			_hSceneCameraData.m_ClearDepth = _hSceneCameraOriginalClearDepth;
		}

		public void UpdateCameraConfig()
		{
			if (ThreeDRoomsPlugin.enabled.Value)
			{
				////Set camera clear depth
				//Il2CppCollections.List<Camera> cameraStackList = _cameraStackList;
				//int cameraStackListCount = cameraStackList.Count;

				//int simulationCameraStackIndex = cameraStackList.IndexOf(_simulationSceneCamera);
				//int hSceneCameraStackIndex = cameraStackList.IndexOf(_hSceneCamera);

				//if (simulationCameraStackIndex < 0 || simulationCameraStackIndex > cameraStackListCount) simulationCameraStackIndex = SIM_CAM_ORIG_STACK_INDEX;
				//if (hSceneCameraStackIndex < 0 || hSceneCameraStackIndex > cameraStackListCount) hSceneCameraStackIndex = HSCENE_CAM_ORIG_STACK_INDEX;

				//int start = Mathf.Min(simulationCameraStackIndex, hSceneCameraStackIndex);
				//int end = Mathf.Min(Mathf.Max(simulationCameraStackIndex, hSceneCameraStackIndex), cameraStackListCount);

				//for (int i = start + 1; i <= end; i++)
				//{
				//	if (i >= 0 && i < _cameraStackList.Count)
				//	{
				//		_cameraStackList[i].GetUniversalAdditionalCameraData().m_ClearDepth = false;
				//	}
				//}


				//Set camera stack order
				Il2CppCollections.List<Camera> cameraStackList = _cameraStackList;

				cameraStackList.Remove(_simulationSceneCamera);
				cameraStackList.Add(_simulationSceneCamera);

				cameraStackList.Remove(_hSceneCamera);
				cameraStackList.Add(_hSceneCamera);

				//Set FOV and clear depth flag
				_simulationSceneCamera.fieldOfView = _hSceneCamera.fieldOfView;
				_simulationSceneCameraData.m_ClearDepth = true;
				_hSceneCameraData.m_ClearDepth = false;
			}
			else
			{
				//Reset
				ResetCamera();
			}
		}

		public void UpdateCameraFOVPositionAndRotation()
		{
			if (ThreeDRoomsPlugin.enabled.Value)
			{
				_simulationSceneCamera.fieldOfView = _hSceneCamera.fieldOfView;

				_hSceneCameraTransform.GetPositionAndRotation(out Vector3 hSceneCameraPosition, out Quaternion hSceneCameraRotation);
				_simulationSceneCameraTransform.SetPositionAndRotation(
					_simulationSceneCameraInitialPosition + hSceneCameraPosition,
					_simulationSceneCameraInitialRotation * hSceneCameraRotation
				);
			}
		}



		/*UNITY METHODS*/
		//Set SimulationScene camera position and rotation
		private void LateUpdate()
		{
			//If enabled and PovX not active set camera position and rotation
			if (ThreeDRoomsPlugin.enabled.Value && _isPovXCompatibility == false)
			{
				_simulationSceneCamera.fieldOfView = _hSceneCamera.fieldOfView;

				_hSceneCameraTransform.GetPositionAndRotation(out Vector3 hSceneCameraPosition, out Quaternion hSceneCameraRotation);
				_simulationSceneCameraTransform.SetPositionAndRotation(
					_simulationSceneCameraInitialPosition + hSceneCameraPosition,
					_simulationSceneCameraInitialRotation * hSceneCameraRotation
				);
			}
		}

		//Setup
		private void Start()
		{
			//Save original position and FOV
			_simulationSceneCameraTransform.GetPositionAndRotation(out _simulationSceneCameraOriginalPosition, out _simulationSceneCameraOriginalRotation);
			_simulationSceneCameraOriginalFOV = _simulationSceneCamera.fieldOfView;
			Logging.Info($"Saved SimulationScene camera original values: Position = {_simulationSceneCameraOriginalPosition}, Rotation = {_simulationSceneCameraOriginalRotation}, FOV = {_simulationSceneCameraOriginalFOV}");

			//Save initial position
			_simulationSceneCameraInitialPosition = _hScene.parameter.Transform.position;
			_simulationSceneCameraInitialRotation = new Quaternion(0f, 0f, 0f, 1f);
			Logging.Info($"Saved SimulationScene camera initial values: Position = {_simulationSceneCameraInitialPosition}, Rotation = {_simulationSceneCameraInitialRotation}");

			//Set simulation scene models, BG display, and update camera config
			SetSimulationSceneModelsDisplay();
			SetBGBlurAndFramesDisplay();
			UpdateCameraConfig();

			//Invoke event
			Started?.Invoke();
			Logging.Info("ThreeDRoomsComponent setup finished");
		}

		private void OnDestroy()
		{
			if (Instance == this)
			{
				Instance = null;
				Destroyed?.Invoke();
			}
		}

		private void Awake()
		{
			if (Instance == null)
			{
				Instance = this;
			}
			else
			{
				Destroy(this);
			}
		}



		/*EVENT HANDLING*/
		private void OnHScenePreDispose()
		{
			ResetCamera();
			ShowSimulationSceneModels();
			ShowBGBlurAndFrames();
		}



		//Component specific hooks
		public static class Hooks
		{
			/// <summary>
			/// Set references at the very start to avoid null reference exceptions.
			/// </summary>
			[HarmonyPostfix]
			[HarmonyPatch(typeof(HScene), nameof(HScene.Start))]
			public static void HScenePostStart(HScene __instance)
			{
				Logging.Info("Trying to set up ThreeDRoomsComponent");
				
				//Create Component and set _hScene
				ThreeDRoomsComponent threeDRoomsComponent = ThreeDRoomsPlugin.GetOrAddThreeDRoomsComponent();
				threeDRoomsComponent._hScene = __instance;
				threeDRoomsComponent._hSceneCamera = __instance._mainCamera;
				threeDRoomsComponent._hSceneCameraData = threeDRoomsComponent._hSceneCamera.GetUniversalAdditionalCameraData();
				threeDRoomsComponent._hSceneCameraTransform = threeDRoomsComponent._hSceneCamera.transform;
				threeDRoomsComponent._hSceneCameraOriginalClearDepth = threeDRoomsComponent._hSceneCameraData.m_ClearDepth;

				//Find SimulationScene
				UnityEngine.SceneManagement.Scene simulationScene = UnityEngine.SceneManagement.SceneManager.GetSceneByName("Simulation");
				if (simulationScene.IsValid() == false && simulationScene.isLoaded == false)
				{
					Logging.Error("Simulation Scene not found, destroying ThreeDRoomsComponent");
					Destroy(threeDRoomsComponent);
					return;
				}

				//Find SimulationScene components
				var rootGameObjects = simulationScene.GetRootGameObjects();
				foreach (GameObject rootGameObject in rootGameObjects)
				{
					if (rootGameObject.name == "SimulationScene")
					{
						threeDRoomsComponent._simulationScene = rootGameObject.GetComponent<SimulationScene>();
						threeDRoomsComponent._simulationSceneCamera = threeDRoomsComponent._simulationScene.mainCamera;
						threeDRoomsComponent._simulationSceneCameraData = threeDRoomsComponent._simulationSceneCamera.GetUniversalAdditionalCameraData();
						threeDRoomsComponent._simulationSceneCameraTransform = threeDRoomsComponent._simulationSceneCamera.transform;
						threeDRoomsComponent._simulationSceneCameraOriginalClearDepth = threeDRoomsComponent._simulationSceneCameraData.m_ClearDepth;
						break;
					}
				}
				if (threeDRoomsComponent._simulationScene is null)
				{
					Logging.Error("SimulationScene component not found, destroying ThreeDRoomsComponent");
					Destroy(threeDRoomsComponent);
					return;
				}

				//Find BaseCamera
				Camera? baseCamera = null;
				foreach (Camera camera in Camera.allCameras)
				{
					if (camera.name == "BaseCamera")
					{
						baseCamera = camera;
						break;
					}
				}

				if (baseCamera != null)
				{
					//Get camera stack
					Il2CppCollections.List<Camera> baseCameraStackList = baseCamera.GetUniversalAdditionalCameraData().cameraStack;
					threeDRoomsComponent._cameraStackList = baseCameraStackList;

					//Get SimulationScene camera original stack index
					if (threeDRoomsComponent._simulationSceneCamera != null)
					{
						foreach (Camera stackCamera in baseCameraStackList)
						{
							if (stackCamera == threeDRoomsComponent._simulationSceneCamera)
							{
								int stackIndex = baseCameraStackList.IndexOf(stackCamera);
								threeDRoomsComponent._simulationSceneCameraOriginalStackIndex = (stackIndex >= 0 && stackIndex < baseCameraStackList.Count) ? stackIndex : SIM_CAM_ORIG_STACK_INDEX;
								break;
							}
							else if (stackCamera == threeDRoomsComponent._hSceneCamera)
							{
								int stackIndex = baseCameraStackList.IndexOf(stackCamera);
								threeDRoomsComponent._hSceneCameraOriginalStackIndex = (stackIndex >= 0 && stackIndex < baseCameraStackList.Count) ? stackIndex : HSCENE_CAM_ORIG_STACK_INDEX;
							}
						}
					}

					//If SimulationScene camera is null try to find by name
					else
					{
						foreach (Camera stackCamera in baseCameraStackList)
						{
							if (stackCamera.name == "Main Camera")
							{
								threeDRoomsComponent._simulationSceneCamera = stackCamera;
								threeDRoomsComponent._simulationSceneCameraData = threeDRoomsComponent._simulationSceneCamera.GetUniversalAdditionalCameraData();
								threeDRoomsComponent._simulationSceneCameraTransform = threeDRoomsComponent._simulationSceneCamera.transform;
								threeDRoomsComponent._simulationSceneCameraOriginalClearDepth = threeDRoomsComponent._simulationSceneCameraData.m_ClearDepth;

								int stackIndex = baseCameraStackList.IndexOf(stackCamera);
								threeDRoomsComponent._simulationSceneCameraOriginalStackIndex = (stackIndex >= 0 && stackIndex < baseCameraStackList.Count) ? stackIndex : SIM_CAM_ORIG_STACK_INDEX;
								break;
							}
						}
					}

					Transform transform = SingletonInitializer<Game>.Instance.transform.GetComponentInChildren<HighPolyBackGroundFrame>().animFrame.transform;
					threeDRoomsComponent._bgBlur = transform.Find("Panel").gameObject;
					threeDRoomsComponent._bgFrameBottom = transform.Find("DownFrame").gameObject;
					threeDRoomsComponent._bgFrameTop = transform.Find("UpFrame").gameObject;
				}
			}

			/// <summary>
			/// Perform cleanup pre <c>HScene.Dispose()</c> just in case.
			/// </summary>
			[HarmonyPrefix]
			[HarmonyPatch(typeof(HScene), nameof(HScene.Dispose))]
			public static void HScenePreDispose()
			{
				if (ThreeDRoomsPlugin.TryGetThreeDRoomsComponent(out ThreeDRoomsComponent? threeDRoomsComponent))
				{
					threeDRoomsComponent.OnHScenePreDispose();
				}
			}

			/// <summary>
			/// Destroy component post <c>HScene.Dispose()</c>.
			/// </summary>
			[HarmonyPostfix]
			[HarmonyPatch(typeof(HScene), nameof(HScene.Dispose))]
			public static void HScenePostDispose()
			{
				if (ThreeDRoomsPlugin.TryGetThreeDRoomsComponent(out ThreeDRoomsComponent? threeDRoomsComponent))
				{
					Destroy(threeDRoomsComponent);
				}
			}
		}
	}
}