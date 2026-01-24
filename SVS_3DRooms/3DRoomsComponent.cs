#nullable enable
using HarmonyLib;
using ILLGames.Unity.Component;
using Manager;
using SV;
using SV.H;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using Logging = SVS_3DRooms.ThreeDRoomsPlugin.Logging;
using Il2CppCollections = Il2CppSystem.Collections.Generic;
using Character;


namespace SVS_3DRooms
{
	public class ThreeDRoomsComponent : MonoBehaviour
	{
		public const int SIM_CAM_ORIG_STACK_INDEX = 1;

		/*VARIABLES*/
		//Instance
		public static ThreeDRoomsComponent? Instance { get; private set; }

		//Camera
		private Il2CppCollections.List<Camera> _cameraStackList = null!;

		//HScene
		private HActor[] _hActors = null!;
		private Camera _hSceneCamera = null!;
		private UniversalAdditionalCameraData _hSceneCameraData = null!;
		private Transform _hSceneCameraTransform = null!;

		//SimulationScene
		private SimulationScene _simulationScene = null!;
		private Camera _simulationSceneCamera = null!;
		private UniversalAdditionalCameraData _simulationSceneCameraData = null!;
		private Transform _simulationSceneCameraTransform = null!;

		//Saved values
		private Vector3 _simulationSceneCameraOriginalPosition;
		private Quaternion _simulationSceneCameraOriginalRotation;
		private float _simulationSceneCameraOriginalFOV = 50f;
		private int _simulationSceneCameraOriginalStackIndex = SIM_CAM_ORIG_STACK_INDEX;
		private bool _hSceneCameraOriginalClearDepth = true;
		private bool _simulationSceneCameraOriginalClearDepth = true;

		private Vector3 _simulationSceneCameraInitialPosition;
		private Quaternion _simulationSceneCameraInitialRotation;

		//BG blur and frame
		private GameObject _bgBlur = null!;
		private GameObject _bgFrameTop = null!;
		private GameObject _bgFrameBottom = null!;



		/*METHODS*/
		private void ShowSimulationSceneModels()
		{
			foreach (HActor hActor in _hActors)
			{
				if (hActor != null)
				{
					//Get character SimulationScene info
					int characterIndex = hActor.Actor.charasGameParam.Index;
					SV.Chara.AI simulationAI = _simulationScene.tempAIs[characterIndex];
					Human simulationHuman = simulationAI.chaCtrl;

					//Show character model and circle
					simulationHuman.visibleAll = true;
					simulationAI.objCircle.SetActive(true);
				}
			}
		}

		public void SetSimulationSceneModelsDisplay()
		{
			bool shouldShow = !ThreeDRoomsPlugin.enabled.Value;

			foreach (HActor hActor in _hActors)
			{
				if (hActor != null)
				{
					//Get character SimulationScene info
					int characterIndex = hActor.Actor.charasGameParam.Index;
					SV.Chara.AI simulationAI = _simulationScene.tempAIs[characterIndex];
					Human simulationHuman = simulationAI.chaCtrl;

					//Hide character model and circle
					simulationHuman.visibleAll = shouldShow;
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
				cameraStackList.Insert(_simulationSceneCameraOriginalStackIndex,  _simulationSceneCamera);
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



		/*UNITY METHODS*/
		//Set SimulationScene camera position and rotation
		private void LateUpdate()
		{
			if (ThreeDRoomsPlugin.enabled.Value)
			{
				Transform simulationSceneCameraTransform = _simulationSceneCameraTransform;
				Transform hSceneCameraTransform = _hSceneCameraTransform;
				simulationSceneCameraTransform.position = _simulationSceneCameraInitialPosition + hSceneCameraTransform.position;
				simulationSceneCameraTransform.rotation = _simulationSceneCameraInitialRotation * hSceneCameraTransform.rotation;
			}
		}

		//Setup
		private void Start()
		{
			//Set simulation scene models, BG display, and HScene camera postProcessingEffects
			SetSimulationSceneModelsDisplay();
			SetBGBlurAndFramesDisplay();

			//Get info and root position characterIndex
			int hActorsLength = _hActors.Length;
			int characterIndex;

			if (hActorsLength > 0)
			{
				if (hActorsLength > 1)
				{
					characterIndex = _hActors[1].Actor.charasGameParam.Index;
				}
				else
				{
					characterIndex = _hActors[0].Actor.charasGameParam.Index;
				}
			}
			else
			{
				Logging.Error("No HActors found in HScene.");
				Destroy(this);
				return;
			}

			//Save original position and FOV
			_simulationSceneCameraTransform.GetPositionAndRotation(out _simulationSceneCameraOriginalPosition, out _simulationSceneCameraOriginalRotation);
			_simulationSceneCameraOriginalFOV = _simulationSceneCamera.fieldOfView;

			//Save initial position
			_simulationSceneCameraInitialPosition = _simulationScene.tempAIs[characterIndex].transform.position;
			_simulationSceneCameraInitialRotation = new Quaternion(0f, 0f, 0f, 1f);

			//Update camera config
			UpdateCameraConfig();
		}

		private void OnDestroy()
		{
			if (Instance == this)
			{
				Instance = null;
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
			/// Update FOV post <c>HScene.CameraLoad()</c> since it might be changed after. 
			/// </summary>
			[HarmonyPostfix]
			[HarmonyPatch(typeof(HScene), nameof(HScene.CameraLoad))]
			public static void HScenePostCameraLoad()
			{
				if (ThreeDRoomsPlugin.TryGetThreeDRoomsComponent(out ThreeDRoomsComponent? threeDRoomsComponent) && ThreeDRoomsPlugin.enabled.Value)
				{
					threeDRoomsComponent._simulationSceneCamera.fieldOfView = threeDRoomsComponent._hSceneCamera.fieldOfView;
				}
			}

			/// <summary>
			/// Set references at the very start to avoid null reference exceptions.
			/// </summary>
			[HarmonyPostfix]
			[HarmonyPatch(typeof(HScene), nameof(HScene.Start))]
			public static void HScenePostStart(HScene __instance)
			{
				//Create Component and set _hScene
				ThreeDRoomsComponent threeDRoomsComponent = ThreeDRoomsPlugin.GetOrAddThreeDRoomsComponent();
				threeDRoomsComponent._hActors = __instance.Actors;
				threeDRoomsComponent._hSceneCamera = __instance._mainCamera;
				threeDRoomsComponent._hSceneCameraData = threeDRoomsComponent._hSceneCamera.GetUniversalAdditionalCameraData();
				threeDRoomsComponent._hSceneCameraTransform = threeDRoomsComponent._hSceneCamera.transform;
				threeDRoomsComponent._hSceneCameraOriginalClearDepth = threeDRoomsComponent._hSceneCameraData.m_ClearDepth;

				//Find SimulationScene
				var rootGameObjects = UnityEngine.SceneManagement.SceneManager.GetSceneByBuildIndex(4).GetRootGameObjects();
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
							if (threeDRoomsComponent._simulationSceneCamera == stackCamera)
							{
								int stackIndex = baseCameraStackList.IndexOf(stackCamera);
								threeDRoomsComponent._simulationSceneCameraOriginalStackIndex = (stackIndex < 0 || stackIndex >= baseCameraStackList.Count) ? SIM_CAM_ORIG_STACK_INDEX : stackIndex;
								break;
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
								threeDRoomsComponent._simulationSceneCameraOriginalStackIndex = (stackIndex < 0 || stackIndex >= baseCameraStackList.Count) ? SIM_CAM_ORIG_STACK_INDEX : stackIndex;
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