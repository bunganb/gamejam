using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace GameJam.Gameplay
{
    [DisallowMultipleComponent]
    public class CameraMover : MonoBehaviour
    {
        [Header("Camera Reference")]
        [SerializeField] private Transform cameraTransform;

        [Header("Target Points List")]
        [SerializeField] private List<Transform> targetPoints = new List<Transform>();

        [Header("Movement Settings")]
        [Tooltip("Waktu yang dibutuhkan untuk mencapai target (makin kecil makin cepat)")]
        [SerializeField] private float smoothTime = 0.3f;

        [Tooltip("Kecepatan gerakan rotasi kamera")]
        [SerializeField] private float rotateSpeed = 3f;

        [Tooltip("Jarak yang dianggap sudah sampai agar ekor Lerp tidak menahan hover terlalu lama")]
        [SerializeField, Min(0.001f)] private float arrivalDistance = 0.05f;

        [Tooltip("Selisih rotasi yang dianggap sudah sampai dalam derajat")]
        [SerializeField, Min(0.01f)] private float arrivalAngle = 0.5f;

        [Tooltip("Apakah kamera langsung bergerak ke target awal saat Start?")]
        [SerializeField] private bool moveOnStart = false;
        [SerializeField] private int startTargetIndex = 0;

        [Header("Director & Detector Reference (Optional)")]
        [SerializeField] private PrototypeCameraDirector cameraDirector;
        [SerializeField] private Raycast3DButtonDetector buttonDetector;

        private Transform currentTarget;
        private int currentIndex = -1;
        private int previousIndex = -1;
        private bool isMoving = false;

        private Vector3 currentBasePosition;
        private Quaternion currentBaseRotation;

        public bool IsMoving => isMoving;

        private void Awake()
        {
            if (cameraTransform == null)
            {
                cameraTransform = Camera.main != null ? Camera.main.transform : transform;
            }

            if (cameraDirector == null && cameraTransform != null)
                cameraDirector = cameraTransform.GetComponent<PrototypeCameraDirector>();

            if (buttonDetector == null && cameraTransform != null)
                buttonDetector = cameraTransform.GetComponent<Raycast3DButtonDetector>();
        }

        private void Start()
        {
            if (cameraTransform != null)
            {
                currentBasePosition = cameraTransform.position;
                currentBaseRotation = cameraTransform.rotation;
            }

            if (moveOnStart && targetPoints.Count > 0)
            {
                MoveToTargetIndex(startTargetIndex);
            }
        }

        private void Update()
        {
            if (currentTarget == null || cameraTransform == null) return;

            if (isMoving)
            {
                // 1. Pergerakan Posisi menggunakan SmoothDamp (Pengereman Halus)
                currentBasePosition = Vector3.SmoothDamp(
                    currentBasePosition, 
                    currentTarget.position, 
                    ref moveVelocity, 
                    smoothTime
                );

                // 2. Pergerakan Rotasi menggunakan Slerp
                currentBaseRotation = Quaternion.Slerp(
                    currentBaseRotation, 
                    currentTarget.rotation, 
                    Time.deltaTime * rotateSpeed
                );

                // 3. Terapkan ke Transform
                cameraTransform.position = currentBasePosition;
                cameraTransform.rotation = currentBaseRotation;

            // 3. Sync ke PrototypeCameraDirector
            if (cameraDirector != null)
            {
                cameraDirector.SetBaseline(cameraTransform.localPosition, cameraTransform.localRotation);
            }

            // 4. Sync rotasi acuan ke Raycast3DButtonDetector agar tidak memutar balik kamera
            if (buttonDetector != null)
            {
                buttonDetector.SetDefaultRotation(currentBaseRotation);
            }

            // 5. Hentikan gerakan jika sudah sampai di target
            if (Vector3.Distance(currentBasePosition, currentTarget.position) < arrivalDistance &&
                Quaternion.Angle(currentBaseRotation, currentTarget.rotation) < arrivalAngle)
            {
                currentBasePosition = currentTarget.position;
                currentBaseRotation = currentTarget.rotation;

                    cameraTransform.position = currentBasePosition;
                    cameraTransform.rotation = currentBaseRotation;

                    moveVelocity = Vector3.zero; // Reset kecepatan
                    isMoving = false;
                }
            }

            // 5. Tetap sync baseline setiap frame agar tidak kaget saat pergerakan berhenti
            SyncExternalComponents();
        }

        private void SyncExternalComponents()
        {
            if (cameraDirector != null)
            {
                cameraDirector.SetBaseline(cameraTransform.localPosition, cameraTransform.localRotation);
            }

                if (buttonDetector != null)
                {
                    buttonDetector.CompleteCameraTransition(currentBaseRotation);
                }

                isMoving = false;
            }
        }

        public void MoveToTargetIndex(int index)
        {
            if (targetPoints == null || index < 0 || index >= targetPoints.Count || targetPoints[index] == null)
            {
                Debug.LogWarning($"[CameraMover] Index {index} tidak valid!");
                return;
            }

            if (currentIndex != index)
            {
                previousIndex = currentIndex;
            }

            if (cameraTransform != null)
            {
                currentBasePosition = cameraTransform.position;
                currentBaseRotation = cameraTransform.rotation;
            }

            currentIndex = index;
            currentTarget = targetPoints[index];
            moveVelocity = Vector3.zero; // Reset kecepatan sebelum mulai gerak baru
            isMoving = true;

            // CameraMover owns the camera for the duration of this transition.
            // Hover focus must not write another rotation in LateUpdate until
            // this movement has reached its final pose.
            if (buttonDetector != null)
            {
                buttonDetector.BeginCameraTransition();
            }
        }

        public void MoveToPreviousTarget()
        {
            int targetIdx = (previousIndex >= 0 && previousIndex < targetPoints.Count) ? previousIndex : 0;
            MoveToTargetIndex(targetIdx);
        }
    }
}
