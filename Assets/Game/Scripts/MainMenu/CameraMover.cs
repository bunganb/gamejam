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

        [Header("Director Reference (Optional)")]
        [Tooltip("Script PrototypeCameraDirector yang mengontrol goyangan/effects kamera (jika ada)")]
        [SerializeField] private PrototypeCameraDirector cameraDirector;

        private Transform currentTarget;
        private int currentIndex = -1;
        private int previousIndex = -1;
        private bool isMoving = false;

        private void Awake()
        {
            // Jika cameraTransform belum di-assign, cari Main Camera
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

            // Cari komponen PrototypeCameraDirector jika belum di-assign
            if (cameraDirector == null && cameraTransform != null)
            {
                cameraDirector = cameraTransform.GetComponent<PrototypeCameraDirector>();
            }
        }

        private void Start()
        {
            if (moveOnStart && targetPoints.Count > 0)
            {
                MoveToTargetIndex(startTargetIndex);
            }
        }

        private void Update()
        {
            if (!isMoving || currentTarget == null || cameraTransform == null) return;

            // 1. Gerakkan posisi dan rotasi kamera ke currentTarget
            Vector3 newPosition = Vector3.Lerp(cameraTransform.position, currentTarget.position, Time.deltaTime * moveSpeed);
            Quaternion newRotation = Quaternion.Lerp(cameraTransform.rotation, currentTarget.rotation, Time.deltaTime * rotateSpeed);

            cameraTransform.position = newPosition;
            cameraTransform.rotation = newRotation;

            // 2. Sync ke PrototypeCameraDirector agar efek goyang/groove tetap presisi
            if (cameraDirector != null)
            {
                cameraDirector.SetBaseline(cameraTransform.localPosition, cameraTransform.localRotation);
            }

            // 3. Hentikan gerakan jika sudah sampai di target
            if (Vector3.Distance(cameraTransform.position, currentTarget.position) < 0.005f &&
                Quaternion.Angle(cameraTransform.rotation, currentTarget.rotation) < 0.05f)
            {
                cameraTransform.position = currentTarget.position;
                cameraTransform.rotation = currentTarget.rotation;

                if (cameraDirector != null)
                {
                    cameraDirector.SetBaseline(cameraTransform.localPosition, cameraTransform.localRotation);
                }

                isMoving = false;
            }
        }

        /// <summary>
        /// Panggil fungsi ini dari Event OnClick Button dengan memasukkan nomor index (0, 1, 2, dst)
        /// </summary>
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

            // Simpan index sebelumnya untuk fungsi Kembali
            if (currentIndex != index)
            {
                previousIndex = currentIndex;
            }

            currentIndex = index;
            currentTarget = targetPoints[index];
            isMoving = true;
        }

        /// <summary>
        /// Panggil fungsi ini pada Tombol 'Back' / 'Kembali' untuk mereturn kamera ke lokasi target sebelumnya
        /// </summary>
        public void MoveToPreviousTarget()
        {
            MoveToTargetIndex(0);
        }
    }
}