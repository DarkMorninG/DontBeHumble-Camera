using Unity.Cinemachine;

namespace DBH.Camera.Addons {
    public interface ICameraControllerAddOn {
        void ActualCameraChange(UnityEngine.Camera actualCamera);
        void VirtualCameraChange(CinemachineCamera virtualCamera);
    }
}