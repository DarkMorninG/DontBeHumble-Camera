using System;
using BetterCoroutine.AwaitRuntime;
using DBH.Camera.Controller;
using Unity.Cinemachine;
using UnityEngine;

namespace DBH.Camera.Addons {
    public interface ICameraController {
        GameObject CurrentActiveCameraObject { get; }
        CinemachineCamera CurrentActiveCamera { get; }
        CinemachineBrain CurrentActiveBrain { get; }
        event CameraController.CameraChange OnCameraChange;
        void ActivateCameraInArea(GameObject area, GameObject toFocusOn, Action onFinished = null);
        void ReturnToDefaultCamera();
        void FocusOn(GameObject targetToChangeTo);

        void SwitchToVirtualCamera(CinemachineCamera virtualCamera,
            GameObject toFocusOn,
            Action onFinished = null);

        void SwitchToVirtualCamera(CinemachineCamera cinemachineVirtualCamera, Action onFinished = null);
        void BlockCameraChange();
        void ResumeCameraChange();

        public void ShowLoadingScreen();

        public void HideLoadingScreen();

        public void ShowBlackScreenWithText(string textToDisplay);

        public void HideBlackScreenWithText();

        void SwitchCameraAndBlockUntil(Func<bool> toFulfill,
            CinemachineCamera cinemachineVirtualCamera,
            Action onFinished = null);

        void SwitchCameraAndBlockUntil(Func<bool> toFulfill,
            CinemachineCamera virtualCamera,
            GameObject toFocusOn,
            Action onFinished = null);

        void FocusOnTemp(GameObject targetToChangeTo);
        void ReleaseFocus();
        IAwaitRuntime ActivateBlur();
        IAwaitRuntime DeactivateBlur();
    }
}