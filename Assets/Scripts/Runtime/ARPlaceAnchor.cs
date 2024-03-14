using System;
using System.Collections.Generic;
using Unity.XR.CoreUtils;
using UnityEngine.XR.ARSubsystems;

namespace UnityEngine.XR.ARFoundation.Samples
{
    public class ARPlaceAnchor : MonoBehaviour
    {
        [SerializeField]
        [Tooltip("The enabled Anchor Manager in the scene.")]
        ARAnchorManager m_AnchorManager;

        [SerializeField]
        [Tooltip("The Scriptable Object Asset that contains the ARRaycastHit event.")]
        ARRaycastHitEventAsset m_RaycastHitEvent;

        List<ARAnchor> m_Anchors = new();

        public ARAnchorManager anchorManager
        {
            get => m_AnchorManager;
            set => m_AnchorManager = value;
        }

        public void RemoveAllAnchors()
        {
            foreach (var anchor in m_Anchors)
            {
                Destroy(anchor.gameObject);
            }
            m_Anchors.Clear();
        }

        // Runs when the reset option is called in the context menu in-editor, or when first created.
        void Reset()
        {
            if (m_AnchorManager == null)
                m_AnchorManager = FindAnyObjectByType<ARAnchorManager>();
        }

        void OnEnable()
        {
            if (m_AnchorManager == null)
                m_AnchorManager = FindAnyObjectByType<ARAnchorManager>();

            if ((m_AnchorManager ? m_AnchorManager.subsystem : null) == null)
            {
                enabled = false;
                Debug.LogWarning($"No XRAnchorSubsystem was found in {nameof(ARPlaceAnchor)}'s {nameof(m_AnchorManager)}, so this script will be disabled.", this);
                return;
            }

            if (m_RaycastHitEvent == null)
            {
                enabled = false;
                Debug.LogWarning($"{nameof(m_RaycastHitEvent)} field on {nameof(ARPlaceAnchor)} component of {name} is not assigned.", this);
                return;
            }

            m_RaycastHitEvent.eventRaised += OnPositionDecide;
        }

        void OnDisable()
        {
            if (m_RaycastHitEvent != null)
                m_RaycastHitEvent.eventRaised -= CreateAnchor;
        }

        /// <summary>
        /// Attempts to attach a new anchor to a hit `ARPlane` if supported.
        /// Otherwise, asynchronously creates a new anchor.
        /// </summary>
        async void CreateAnchor(object sender, ARRaycastHit hit)
        {
            if (m_AnchorManager.descriptor.supportsTrackableAttachments && hit.trackable is ARPlane plane)
            {
                var attachedAnchor = m_AnchorManager.AttachAnchor(plane, hit.pose);
                FinalizePlacedAnchor(attachedAnchor, $"Attached to plane {plane.trackableId}");
                return;
            }

            var result = await m_AnchorManager.TryAddAnchorAsync(hit.pose);
            if (result.TryGetResult(out var anchor))
                FinalizePlacedAnchor(anchor, $"Anchor (from {hit.hitType})");
        }

        public ARRaycastManager m_RaycastManager;
        private List<ARRaycastHit> s_Hits = new List<ARRaycastHit>();
        public GameObject anchorGameObject;

        private ARRaycastHit ScreenCenterHitPose()
        {
            // Raycast against planes and feature points
            const TrackableType trackableTypes =
                TrackableType.FeaturePoint |
                TrackableType.PlaneWithinPolygon;
            Vector2 position = new Vector2(Screen.width / 2, Screen.height / 2);

            // Perform the raycast
            if (m_RaycastManager.Raycast(position, s_Hits, trackableTypes))
            {

                // Raycast hits are sorted by distance, so the first one will be the closest hit.
                var hit = s_Hits[0];
                return hit;
            }
            else
            {
                ARRaycastHit hit = new ARRaycastHit();
                return hit;
            }
        }

        ARAnchor CreateAnchor(in ARRaycastHit hit)
        {
            Logger.Log($"hit {hit.hitType.ToString()} ");
            ARAnchor anchor = null;

            //if (m_Prefab != null)
            //{
            // Note: the anchor can be anywhere in the scene hierarchy
            // var gameObject = Instantiate(anchorGameObject, hit.pose.position, hit.pose.rotation);
            var gameObject = Instantiate(anchorGameObject, hit.pose.position, hit.pose.rotation);//+ new Vector3(0.1f,0.1f,0.1f)
                                                                                                                                     // Make sure the new GameObject has an ARAnchor component
            anchor = ComponentUtils.GetOrAddIf<ARAnchor>(gameObject, true);
            //}
            // else
            // {
            //     var gameObject = new GameObject("Anchor");
            //     gameObject.transform.SetPositionAndRotation(hit.pose.position, hit.pose.rotation);
            //     anchor = gameObject.AddComponent<ARAnchor>();
            // }

            //SetAnchorText(anchor, $"Anchor (from {hit.hitType})");

            return anchor;
        }


        private void OnPositionDecide(object sender, ARRaycastHit hit)
        {
            //ARRaycastHit hit = ScreenCenterHitPose();
            if (hit != null)
            {
                // Create a new anchor
                var anchor = CreateAnchor(hit);
                if (anchor != null)
                {
                    // Remember the anchor so we can remove it later.
                    m_Anchors.Add(anchor);
                }
                //controller.SetActive(true);
                //anchorGameObject.SetActive(false);
            }

        }

        void FinalizePlacedAnchor(ARAnchor anchor, string text)
        {
            var canvasTextManager = anchor.GetComponent<CanvasTextManager>();
            if (canvasTextManager != null)
            {
                canvasTextManager.text = text;
            }

            m_Anchors.Add(anchor);
        }
    }
}
