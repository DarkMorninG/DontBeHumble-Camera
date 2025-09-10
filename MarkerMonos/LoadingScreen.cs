using DBH.Base;
using UnityEngine;

namespace DBH.Camera.MarkerMonos {
    public class LoadingScreen : DBHMono {
        private Canvas _canvas;

        private void Start() {
            Init();
        }

        private void Init() {
            if (_canvas == null) {
                _canvas = gameObject.GetComponent<Canvas>();
            }
        }

        public void FocusOnLoadingScreen(UnityEngine.Camera cameraToFocus) {
            gameObject.SetActive(true);
            Init();
            _canvas.renderMode = RenderMode.ScreenSpaceCamera;
            _canvas.worldCamera = cameraToFocus;
            _canvas.planeDistance = 0.2f;
        }

        public void UnFocusLoadingScreen() {
            _canvas.worldCamera = null;
            gameObject.SetActive(false);
        }
    }
}