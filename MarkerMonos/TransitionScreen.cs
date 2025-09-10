using DBH.Base;
using UnityEngine;

namespace DBH.Camera.MarkerMonos {
    public class TransitionScreen : DBHMono {
        [SerializeField]
        GameObject transitionText;

        Canvas _canvas;


        public override void OnStart() {
            Init();
        }

        void Init() {
            if (_canvas == null) {
                _canvas = gameObject.GetComponent<Canvas>();
            }
        }

        public void FocusOnTransitionScreen(UnityEngine.Camera cameraToFocus, string toDisplay) {
            gameObject.SetActive(true);
            Init();
            _canvas.renderMode = RenderMode.ScreenSpaceCamera;
            _canvas.worldCamera = cameraToFocus;
            _canvas.planeDistance = 0.2f;
        }

        public void UnFocusTransitionScreen() {
            _canvas.worldCamera = null;
            gameObject.SetActive(false);
        }
    }
}