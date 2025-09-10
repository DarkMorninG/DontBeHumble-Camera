using UnityEngine;

namespace DBH.Camera.dtos {
    public class CameraChangeDto {
        public CameraChangeDto(GameObject cameraGameObject, UnityEngine.Camera camera) {
            CameraGameObject = cameraGameObject;
            Camera = camera;
        }

        public GameObject CameraGameObject { get; }

        public UnityEngine.Camera Camera { get; }
    }
}