using System;
using System.Collections.Generic;
using System.Linq;
using BetterCoroutine;
using BetterCoroutine.AwaitRuntime;
using DBH.Attributes;
using DBH.Base;
using DBH.Camera.Addons;
using DBH.Camera.dtos;
using DBH.Camera.MarkerMonos;
using DG.Tweening;
using Sirenix.OdinInspector;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using Vault;
using Vault.BetterCoroutine;

namespace DBH.Camera.Controller {
    [Attributes.Controller]
    public class CameraController : DBHMono, ICameraController {
        public delegate void CameraChange(CameraChangeDto camera);

        private IAsyncRuntime _runningBlend;

        private readonly List<CinemachineCamera> _virtualCameras = new();
        private readonly List<CinemachineBrain> _cameraBrains = new();
        private CinemachineCamera _defaultCamera;
        private CinemachineCamera _currentActiveVirtualCamera;
        private CinemachineBrain _defaultCameraBrain;
        private CinemachineBrain _currentActiveBrain;

        [ShowInInspector]
        private List<ICameraControllerAddOn> _cameraControllerAddOns;

        public GameObject CurrentActiveCameraObject =>
            _currentActiveVirtualCamera != null ? _currentActiveBrain.gameObject : null;

        public CinemachineCamera CurrentActiveCamera => _currentActiveVirtualCamera;

        public CinemachineBrain CurrentActiveBrain => _currentActiveBrain;

        public event CameraChange OnCameraChange;

        private bool _initFinished;
        private bool _blockChange;

        private LoadingScreen _loadingScreen;
        private bool _loadingScreenEnabled;

        TransitionScreen _transitionScreen;
        bool _transitionScreenEnabled;

        private IAwaitRuntime runningBlur;

        private List<GameObject> _focusTargets = new();

        public void SwitchToVirtualCamera(CinemachineCamera virtualCamera,
            GameObject toFocusOn,
            Action onFinished = null) {
            _runningBlend = UnityAsyncRuntime.WaitUntil(() => _initFinished,
                () => {
                    virtualCamera.LookAt = toFocusOn.transform;

                    ChangeMainCameraTo(virtualCamera, onFinished);
                },
                false);
            if (!_blockChange) {
                _runningBlend.Start();
            }
        }


        public void ActivateCameraInArea(GameObject area, GameObject toFocusOn, Action onFinished = null) {
            var virtualCamera = area.GetComponentInChildrenForce<CinemachineCamera>();
            SwitchToVirtualCamera(virtualCamera, toFocusOn, onFinished);
        }

        public void SwitchToVirtualCamera(CinemachineCamera cinemachineVirtualCamera, Action onFinished = null) {
            if (_blockChange) {
                return;
            }

            _runningBlend = AsyncRuntime.WaitUntil(() => _initFinished,
                () => ChangeMainCameraTo(cinemachineVirtualCamera, onFinished));
        }

        public void SwitchCameraAndBlockUntil(Func<bool> toFulfill,
            CinemachineCamera virtualCamera,
            GameObject toFocusOn,
            Action onFinished = null) {
            BlockCameraChange();

            _runningBlend = AsyncRuntime.WaitUntil(() => _initFinished,
                () => {
                    virtualCamera.LookAt = toFocusOn.transform;
                    ChangeMainCameraTo(virtualCamera, onFinished);
                });

            AsyncRuntime.WaitUntil(toFulfill, ResumeCameraChange);
        }

        public void ShowLoadingScreen() {
            if (_loadingScreen == null) return;
            _loadingScreen.FocusOnLoadingScreen(_currentActiveBrain.OutputCamera);
            _loadingScreenEnabled = true;
        }

        public void HideLoadingScreen() {
            if (_loadingScreen == null) return;
            AsyncRuntime.WaitForSeconds(() => {
                    AsyncRuntime.WaitUntil(() => !_currentActiveBrain.IsBlending,
                        () => {
                            _loadingScreen.UnFocusLoadingScreen();
                            _loadingScreenEnabled = false;
                        });
                },
                .2f);
        }

        public void ShowBlackScreenWithText(string textToDisplay) {
            _transitionScreen.FocusOnTransitionScreen(_currentActiveBrain.OutputCamera, textToDisplay);
            _transitionScreenEnabled = true;
        }

        public void HideBlackScreenWithText() {
            AsyncRuntime.WaitForSeconds(() => {
                    AsyncRuntime.WaitUntil(() => !_currentActiveBrain.IsBlending,
                        () => {
                            _transitionScreen.UnFocusTransitionScreen();
                            _transitionScreenEnabled = false;
                        });
                },
                .2f);
        }

        public void SwitchCameraAndBlockUntil(Func<bool> toFulfill,
            CinemachineCamera cinemachineVirtualCamera,
            Action onFinished = null) {
            ChangeMainCameraTo(cinemachineVirtualCamera, onFinished);
            BlockCameraChange();

            AsyncRuntime.WaitUntil(toFulfill, ResumeCameraChange);
        }

        public void BlockCameraChange() {
            _blockChange = true;
            // _runningBlend?.Stop();
        }

        public void ResumeCameraChange() {
            _blockChange = false;
        }

        private void ChangeMainCameraTo(CinemachineCamera cinemachineVirtualCamera,
            Action onFinishedBlending = null) {
            if (_blockChange) return;
            if (cinemachineVirtualCamera == null) return;
            if (cinemachineVirtualCamera == _currentActiveVirtualCamera) return;
            Debug.Log("Changed camera to: " + cinemachineVirtualCamera.gameObject.name);

            //since change in virtual camera can be a change in active camera brain


            _virtualCameras.ForEach(virtualCamera => virtualCamera.Priority = 0);
            cinemachineVirtualCamera.Priority = 10;

            _currentActiveVirtualCamera = cinemachineVirtualCamera;

            IAwaitRuntime.WaitForEndOfFrame(() => {
                if (_currentActiveBrain.ActiveBlend == null) {
                    onFinishedBlending?.Invoke();
                    OnCameraChange?.Invoke(new CameraChangeDto(CurrentActiveCameraObject,
                        _currentActiveBrain.OutputCamera));
                    UpdateAddons(_currentActiveBrain.OutputCamera, _currentActiveVirtualCamera);
                    UpdateScreenSizeData();
                } else {
                    IAwaitRuntime.WaitUntil(() => _currentActiveBrain.ActiveBlend != null,
                        () => {
                            AsyncRuntime.WaitUntil(() => !_currentActiveBrain.IsBlending,
                                () => {
                                    onFinishedBlending?.Invoke();
                                    OnCameraChange?.Invoke(new CameraChangeDto(CurrentActiveCameraObject,
                                        _currentActiveBrain.OutputCamera));
                                    UpdateAddons(_currentActiveBrain.OutputCamera, _currentActiveVirtualCamera);
                                    UpdateScreenSizeData();
                                });
                        });
                }
            });
        }


        public void FocusOn(GameObject targetToChangeTo) {
            SmoothFocusChange(targetToChangeTo);
            BlockCameraChange();
        }

        public void FocusOnTemp(GameObject targetToChangeTo) {
            _focusTargets.Add(_currentActiveVirtualCamera.LookAt.gameObject);
            _currentActiveVirtualCamera.LookAt = targetToChangeTo.transform;
        }

        public void ReleaseFocus() {
            FocusOn(_focusTargets.LastItem());
            if (_focusTargets.Count > 1) {
                _focusTargets.RemoveLastItem();
            }
        }

        public void ReturnToDefaultCamera() {
            ChangeMainCameraTo(_defaultCamera);
        }

        public IAwaitRuntime ActivateBlur() {
            var volumeExtension = CurrentActiveCamera.GetComponent<CinemachineVolumeSettings>();
            var volumeExtensionProfile = volumeExtension.Profile;
            var depthOfField = volumeExtensionProfile.components.Find(component => component is DepthOfField) as DepthOfField;
            var activateBlur = IAwaitRuntime.EverySecondsDo(() => depthOfField.focalLength.value += 10, () => .01f, () => depthOfField.focalLength.value >= 190);
            runningBlur = activateBlur;
            return activateBlur;
        }

        public IAwaitRuntime DeactivateBlur() {
            if (runningBlur.IsRunning()) {
                runningBlur.Stop();
            }
            
            var volumeExtension = CurrentActiveCamera.GetComponent<CinemachineVolumeSettings>();
            var volumeExtensionProfile = volumeExtension.Profile;
            var depthOfField = volumeExtensionProfile.components.Find(component => component is DepthOfField) as DepthOfField;
            var deactivateBlur = IAwaitRuntime.EverySecondsDo(() => depthOfField.focalLength.value -= 10, () => .01f, () => depthOfField.focalLength.value <= 1);
            runningBlur = deactivateBlur;
            return deactivateBlur;
        }

        [AfterSceneUnLoad]
        void FindCameraBrain() {
            UpdateFoundCameras();
            _defaultCamera = FindFirstObjectByType<DefaultCamera>(FindObjectsInactive.Include).GetComponent<CinemachineCamera>();
            _defaultCameraBrain = FindFirstObjectByType<DefaultCameraBrain>(FindObjectsInactive.Include).GetComponent<CinemachineBrain>();

            _currentActiveBrain = _defaultCameraBrain.gameObject.activeInHierarchy
                ? _defaultCameraBrain
                : _cameraBrains.First(brain => brain.gameObject.activeInHierarchy);
        }

        [PostConstruct(0)]
        private void Init() {
            _cameraControllerAddOns = GetComponents<ICameraControllerAddOn>().ToList();
            _loadingScreen = GetComponentInChildren<LoadingScreen>(true);
            _transitionScreen = GetComponentInChildren<TransitionScreen>(true);

            UpdateFoundCameras();
            _defaultCamera = FindFirstObjectByType<DefaultCamera>(FindObjectsInactive.Include).GetComponent<CinemachineCamera>();
            _defaultCameraBrain = FindFirstObjectByType<DefaultCameraBrain>(FindObjectsInactive.Include).GetComponent<CinemachineBrain>();

            _currentActiveBrain = _defaultCameraBrain.gameObject.activeInHierarchy
                ? _defaultCameraBrain
                : _cameraBrains.First(brain => brain.gameObject.activeInHierarchy);


            var activeCameraInScene = _virtualCameras.OrderBy(virtualCamera => virtualCamera.Priority).First();
            if (_loadingScreenEnabled) {
                _loadingScreen.FocusOnLoadingScreen(_currentActiveBrain.OutputCamera);
            }

            if (activeCameraInScene != null && _currentActiveVirtualCamera == null) {
                ChangeMainCameraTo(activeCameraInScene);
            } else {
                ChangeMainCameraTo(_defaultCamera);
            }

            if (_currentActiveVirtualCamera == null) Debug.LogError("Missing Active Camera in Scene");
            
            _initFinished = true;
        }

        private void UpdateScreenSizeData() {
            var aspect = _currentActiveBrain.OutputCamera.aspect;
            var lens = LensSettings.FromCamera(_currentActiveBrain.OutputCamera);

            var size = new Vector3(
                aspect * lens.OrthographicSize * 2,
                lens.OrthographicSize * 2,
                lens.FarClipPlane - lens.NearClipPlane);
            
        }

        private void SmoothFocusChange(GameObject targetToChangeTo) {
            var currentTarget = _currentActiveVirtualCamera.LookAt;
            var targetSmoothObject = new GameObject("Changing Camera Focus") {
                transform = {
                    position = currentTarget.position
                }
            };
            _currentActiveVirtualCamera.LookAt = targetSmoothObject.transform;
            targetSmoothObject.transform.DOMove(targetToChangeTo.transform.position, .5f)
                .onComplete = () => {
                _currentActiveVirtualCamera.LookAt = targetToChangeTo.transform;
                Destroy(targetSmoothObject);
            };
        }

        private void UpdateAddons(UnityEngine.Camera currentActiveCamera, CinemachineCamera virtualCamera) {
            if (currentActiveCamera == null) return;
            foreach (var cameraControllerAddOn in _cameraControllerAddOns) {
                cameraControllerAddOn.ActualCameraChange(currentActiveCamera);
                cameraControllerAddOn.VirtualCameraChange(virtualCamera);
            }
        }

        // [AfterSceneUnLoad]
        private void UpdateOnSceneUnload() {
            Init();
            UpdateAddons(_currentActiveBrain.OutputCamera, _currentActiveVirtualCamera);
        }

        private void UpdateFoundCameras() {
            _virtualCameras.AddRange(FindObjectsByType<CinemachineCamera>(FindObjectsSortMode.None));
            _cameraBrains.AddRange(FindObjectsByType<CinemachineBrain>(FindObjectsInactive.Include, FindObjectsSortMode.None));
        }
    }
}