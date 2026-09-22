using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace GameJam.Gameplay
{
    [DisallowMultipleComponent]
    public class CameraMover : MonoBehaviour
    {
        [Header("Camera Reference")]
        [Tooltip("Drag Object Main Camera / Camera di sini")]
        [SerializeField] private Transform cameraTransform;

        [Header("Target Points List")]
        [Tooltip("Daftar Empty GameObject target tujuan camera (Index 0, 1, 2, dst)")]
        [SerializeField] private List<Transform> targetPoints = new List<Transform>();

        [Header("Movement Settings")]
        [Tooltip("Kecepatan gerakan posisi kamera")]
        [SerializeField] private float moveSpeed = 3f;

        [Tooltip("Kecepatan gerakan rotasi kamera")]
        [SerializeField] private float rotateSpeed = 3f;

        [Tooltip("Apakah kamera langsung bergerak ke target awal saat Start?")]
        [SerializeField] private bool moveOnStart = false;

        [Tooltip("Index target awal jika Move On Start diaktifkan")]
        [SerializeField] private int startTargetIndex = 0;

        [Header("Director & Detector Reference (Optional)")]
        [Tooltip("Script PrototypeCameraDirector yang mengontrol goyangan/effects kamera (jika ada)")]
        [SerializeField] private PrototypeCameraDirector cameraDirector;

        [Tooltip("Script Raycast3DButtonDetector yang mengontrol hover kamera (jika ada)")]
        [SerializeField] private Raycast3DButtonDetector buttonDetector;

        private Transform currentTarget;
        private int currentIndex = -1;
        private int previousIndex = -1;
        private bool isMoving = false;

        // Variabel penampung posisi & rotasi murni (mencegah feedback loop dengan Director & Detector)
        private Vector3 currentBasePosition;
        private Quaternion currentBaseRotation;

        private void Awake()
        {
            if (cameraTransform == null)
            {
                if (Camera.main != null)
                {
                    cameraTransform = Camera.main.transform;
                }
                else
                {
                    cameraTransform = transform;
                }
            }

            if (cameraDirector == null && cameraTransform != null)
            {
                cameraDirector = cameraTransform.GetComponent<PrototypeCameraDirector>();
            }

            if (buttonDetector == null && cameraTransform != null)
            {
                buttonDetector = cameraTransform.GetComponent<Raycast3DButtonDetector>();
            }
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
            if (!isMoving || currentTarget == null || cameraTransform == null) return;

            // 1. Lerp posisi dan rotasi MURNI (tanpa mengambil nilai dari cameraTransform yang sudah terkontaminasi sway/hover)
            currentBasePosition = Vector3.Lerp(currentBasePosition, currentTarget.position, Time.deltaTime * moveSpeed);
            currentBaseRotation = Quaternion.Lerp(currentBaseRotation, currentTarget.rotation, Time.deltaTime * rotateSpeed);

            // 2. Terapkan nilai murni ke cameraTransform
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
            if (Vector3.Distance(currentBasePosition, currentTarget.position) < 0.005f &&
                Quaternion.Angle(currentBaseRotation, currentTarget.rotation) < 0.05f)
            {
                currentBasePosition = currentTarget.position;
                currentBaseRotation = currentTarget.rotation;

                cameraTransform.position = currentBasePosition;
                cameraTransform.rotation = currentBaseRotation;

                if (cameraDirector != null)
                {
                    cameraDirector.SetBaseline(cameraTransform.localPosition, cameraTransform.localRotation);
                }

                if (buttonDetector != null)
                {
                    buttonDetector.SetDefaultRotation(currentBaseRotation);
                }

                isMoving = false;
            }
        }

        public void MoveToTargetIndex(int index)
        {
            if (targetPoints == null || index < 0 || index >= targetPoints.Count)
            {
                Debug.LogWarning($"[CameraMover] Index {index} di luar jangkauan! Total target: {targetPoints?.Count ?? 0}");
                return;
            }

            if (targetPoints[index] == null)
            {
                Debug.LogWarning($"[CameraMover] Target pada index {index} belum dimasukkan (NULL)!");
                return;
            }

            if (currentIndex != index)
            {
                previousIndex = currentIndex;
            }

            // Inisialisasi posisi & rotasi acuan awal saat gerakan dimulai
            if (cameraTransform != null)
            {
                currentBasePosition = cameraTransform.position;
                currentBaseRotation = cameraTransform.rotation;
            }

            currentIndex = index;
            currentTarget = targetPoints[index];
            isMoving = true;
        }

        public void MoveToPreviousTarget()
        {
            if (previousIndex >= 0 && previousIndex < targetPoints.Count)
            {
                MoveToTargetIndex(previousIndex);
            }
            else
            {
                MoveToTargetIndex(0);
            }
        }
    }
}