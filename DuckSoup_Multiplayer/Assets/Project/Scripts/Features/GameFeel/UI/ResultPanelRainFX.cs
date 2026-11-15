using System;
using UnityEngine;

namespace DuckSoup.Features.GameFeel.UI
{
    [DisallowMultipleComponent]
    public sealed class ResultPanelRainFX : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private RectTransform targetPanel;
        [SerializeField] private ParticleSystem targetParticles;

        [Header("Placement")]
        [SerializeField, Min(0f)] private float widthPadding = 12f;
        [SerializeField, Min(0f)] private float spawnInset = 8f;
        [SerializeField, Min(1f)] private float spawnBandThickness = 6f;

        [Header("Motion")]
        [SerializeField, Min(0f)] private float fallSpeed = 220f;
        [SerializeField] private bool playOnEnable = false;

        private readonly Vector3[] corners = new Vector3[4];

        private void OnEnable()
        {
            if (playOnEnable)
            {
                Play();
            }
        }

        public void Play()
        {
            if (targetPanel == null || targetParticles == null)
            {
                Debug.LogWarning("[ResultPanelRainFX] Faltan referencias.", this);
                return;
            }

            ConfigureFromPanel();

            targetParticles.Clear(true);
            targetParticles.Play(true);
        }

        public void Stop()
        {
            if (targetParticles == null)
                return;

            targetParticles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }

        public void ConfigureFromPanel()
        {
            if (targetPanel == null || targetParticles == null)
                return;

            targetPanel.GetWorldCorners(corners);

            GetTopAndBottomEdges(
                corners,
                out Vector3 topA,
                out Vector3 topB,
                out Vector3 bottomA,
                out Vector3 bottomB);

            Vector3 topCenter = (topA + topB) * 0.5f;
            Vector3 bottomCenter = (bottomA + bottomB) * 0.5f;

            Vector3 topEdgeDirection = (topB - topA).normalized;
            if (topEdgeDirection.sqrMagnitude < 0.0001f)
                topEdgeDirection = Vector3.right;

            Vector3 fallDirection = (bottomCenter - topCenter).normalized;
            if (fallDirection.sqrMagnitude < 0.0001f)
                fallDirection = Vector3.down;

            float panelWidth = Vector3.Distance(topA, topB);
            float usableWidth = Mathf.Max(1f, panelWidth - widthPadding * 2f);

            Vector3 spawnPosition = topCenter + fallDirection * spawnInset;

            Transform particleTransform = targetParticles.transform;
            particleTransform.position = spawnPosition;
            particleTransform.right = topEdgeDirection;

            var main = targetParticles.main;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.startSpeed = 0f;

            var shape = targetParticles.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = new Vector3(usableWidth, spawnBandThickness, 0f);

            var velocity = targetParticles.velocityOverLifetime;
            velocity.enabled = true;
            velocity.space = ParticleSystemSimulationSpace.World;
            velocity.x = new ParticleSystem.MinMaxCurve(fallDirection.x * fallSpeed);
            velocity.y = new ParticleSystem.MinMaxCurve(fallDirection.y * fallSpeed);
            velocity.z = new ParticleSystem.MinMaxCurve(0f);
        }

        private static void GetTopAndBottomEdges(
            Vector3[] inputCorners,
            out Vector3 topA,
            out Vector3 topB,
            out Vector3 bottomA,
            out Vector3 bottomB)
        {
            Vector3[] sorted = new Vector3[inputCorners.Length];
            Array.Copy(inputCorners, sorted, inputCorners.Length);

            Array.Sort(sorted, (a, b) => b.y.CompareTo(a.y));

            Vector3 t1 = sorted[0];
            Vector3 t2 = sorted[1];
            Vector3 b1 = sorted[2];
            Vector3 b2 = sorted[3];

            if (t1.x <= t2.x)
            {
                topA = t1;
                topB = t2;
            }
            else
            {
                topA = t2;
                topB = t1;
            }

            if (b1.x <= b2.x)
            {
                bottomA = b1;
                bottomB = b2;
            }
            else
            {
                bottomA = b2;
                bottomB = b1;
            }
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            if (targetPanel == null)
                return;

            targetPanel.GetWorldCorners(corners);

            GetTopAndBottomEdges(
                corners,
                out Vector3 topA,
                out Vector3 topB,
                out Vector3 bottomA,
                out Vector3 bottomB);

            Vector3 topCenter = (topA + topB) * 0.5f;
            Vector3 bottomCenter = (bottomA + bottomB) * 0.5f;

            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(topA, topB);

            Gizmos.color = Color.yellow;
            Gizmos.DrawSphere(topCenter, 8f);

            Gizmos.color = Color.green;
            Gizmos.DrawLine(topCenter, bottomCenter);
        }
#endif
    }
}
