using UnityEngine;

namespace DBH.Camera.MarkerMonos {
    public class TargetTracker {
        private Transform trackedTarget;
        private GameObject lookAtTarget;

        public Transform TrackedTarget => trackedTarget;

        public GameObject LookAtTarget => lookAtTarget;

        public TargetTracker(Transform trackedTarget, GameObject lookAtTarget) {
            this.trackedTarget = trackedTarget;
            this.lookAtTarget = lookAtTarget;
        }
    }
}