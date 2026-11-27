using System;
using UnityEngine;

namespace RockAndHardPlace
{
    // Animate the tilemap hierarchy, rather than a clip with absolute scene coordinates.
    // The root travels; only the body squashes. Collision and turn changes belong to Mountain.
    public sealed class MountainEscape : MonoBehaviour
    {
        public const float WakeDuration = 0.5f;
        public const float CrouchDuration = 0.45f;
        public const float RiseDuration = 1.35f;
        public const float RiseEnd = WakeDuration + CrouchDuration + RiseDuration;
        public const float RunStart = RiseEnd + 0.18f;
        public const float RunDuration = 1.1f;
        public const float Duration = RunStart + RunDuration;

        [SerializeField] private Transform body;
        [SerializeField] private Transform frontLeg;
        [SerializeField] private Transform backLeg;
        [SerializeField] private GameObject restingFace;
        [SerializeField] private GameObject awakeFace;
        [Tooltip("Offset from the mountain's feet to the distant bend in the river.")]
        [SerializeField] private Vector3 valleyOffset = new Vector3(2.05f, 4.78f, 10);

        public bool IsRunning { get; private set; }
        public event Action<Vector3> Footstep;

        private Mountain mountain;
        private Vector3 origin;
        private Vector3 originalScale;
        private Vector3 bodyOrigin;
        private Vector3 bodyScale;
        private Quaternion bodyRotation;
        private Vector3 frontOrigin;
        private Vector3 backOrigin;
        private float elapsed;
        private int lastStep;

        private void Awake()
        {
            mountain = GetComponent<Mountain>();
            // Prevent old scene clips from writing to the same root if this is added by hand.
            var legacy = GetComponent<Animator>();
            if (legacy != null) legacy.enabled = false;
        }

        public void Begin()
        {
            if (IsRunning) return;
            origin = transform.localPosition;
            originalScale = transform.localScale;
            if (body != null)
            {
                bodyOrigin = body.localPosition;
                bodyScale = body.localScale;
                bodyRotation = body.localRotation;
            }
            if (frontLeg != null) frontOrigin = frontLeg.localPosition;
            if (backLeg != null) backOrigin = backLeg.localPosition;
            elapsed = 0;
            lastStep = -1;
            if (restingFace != null) restingFace.SetActive(false);
            if (awakeFace != null) awakeFace.SetActive(true);
            IsRunning = true;
            GameAudio.Play(SoundCue.MountainWake);
            Apply(Sample(0, valleyOffset));
        }

        private void Update()
        {
            if (!IsRunning) return;
            elapsed = Mathf.Min(elapsed + Time.deltaTime, Duration);
            var pose = Sample(elapsed, valleyOffset);
            Apply(pose);
            if (elapsed >= RunStart)
            {
                int step = Mathf.FloorToInt((pose.Stride + Mathf.PI * 0.5f) / Mathf.PI);
                if (step != lastStep)
                {
                    lastStep = step;
                    var foot = step % 2 == 1 ? frontLeg : backLeg;
                    Footstep?.Invoke(foot != null ? foot.TransformPoint(Vector3.down) : transform.position);
                    GameAudio.Play(SoundCue.Footstep, Mathf.Lerp(0.1f, 0.8f, pose.FigureScale));
                }
            }
            if (elapsed < Duration) return;
            IsRunning = false;
            mountain.OnLeaveFinished();
        }

        private void Apply(EscapePose pose)
        {
            transform.localPosition = origin + pose.RootOffset;
            transform.localScale = originalScale * pose.FigureScale;
            if (body != null)
            {
                body.localPosition = bodyOrigin + pose.BodyOffset;
                body.localScale = Vector3.Scale(bodyScale, pose.BodyScale);
                body.localRotation = bodyRotation * Quaternion.Euler(0, 0, pose.Lean);
            }
            MoveLeg(frontLeg, frontOrigin, pose.FrontFoot, pose.Extension);
            MoveLeg(backLeg, backOrigin, pose.BackFoot, pose.Extension);
        }

        private static void MoveLeg(Transform leg, Vector3 rest, Vector3 foot, float extension)
        {
            if (leg == null) return;
            leg.gameObject.SetActive(extension > 0.01f);
            leg.localPosition = new Vector3(rest.x + foot.x, extension + foot.y, rest.z);
            leg.localScale = new Vector3(1, Mathf.Max(0.01f, extension), 1);
        }

        // Kept separate from Transform writes so continuity, travel, and timing can be checked.
        public static EscapePose Sample(float seconds, Vector3 valleyOffset)
        {
            float t = Mathf.Clamp(seconds, 0, Duration);
            var pose = new EscapePose { BodyScale = Vector3.one, FigureScale = 1 };
            if (t < WakeDuration)
            {
                pose.BodyOffset.x = Mathf.Sin(t * 36) * 0.075f * Mathf.Sin(t / WakeDuration * Mathf.PI);
                return pose;
            }
            if (t < WakeDuration + CrouchDuration)
            {
                float u = Mathf.SmoothStep(0, 1, (t - WakeDuration) / CrouchDuration);
                pose.BodyScale = new Vector3(1 + 0.12f * u, 1 - 0.16f * u, 1);
                return pose;
            }
            if (t < RiseEnd)
            {
                float u = Mathf.SmoothStep(0, 1, (t - WakeDuration - CrouchDuration) / RiseDuration);
                pose.BodyScale = Vector3.Lerp(new Vector3(1.12f, 0.84f, 1), Vector3.one, u);
                pose.BodyOffset.y = u;
                pose.Extension = u;
                pose.Lean = 1.5f * u;
                return pose;
            }

            pose.BodyOffset.y = 1;
            pose.Extension = 1;
            if (t < RunStart)
            {
                pose.Lean = Mathf.SmoothStep(1.5f, -5, (t - RiseEnd) / (RunStart - RiseEnd));
                return pose;
            }

            float run = Mathf.Clamp01((t - RunStart) / RunDuration);
            float depth = Mathf.Pow(run, 1.15f);
            // The camera is orthographic. Scale provides perspective as he follows the
            // river, and depth lets the distant ridges actually hide him at the end.
            pose.RootOffset = valleyOffset * depth;
            pose.RootOffset.x += 0.8f * Mathf.Sin(depth * Mathf.PI);
            pose.FigureScale = Mathf.Pow(0.025f, depth);
            pose.Stride = 4.6f * Mathf.PI * 2 * run;
            float blend = Mathf.SmoothStep(0, 1, run / 0.09f);
            float bounce = Mathf.Abs(Mathf.Sin(pose.Stride));
            pose.BodyOffset.y += 0.22f * bounce * blend;
            pose.Lean = -5 - blend * (4 + 2 * Mathf.Sin(pose.Stride));
            pose.FrontFoot = Foot(pose.Stride) * blend;
            pose.BackFoot = Foot(pose.Stride + Mathf.PI) * blend;
            return pose;
        }

        private static Vector3 Foot(float phase) =>
            new Vector3(0.7f * Mathf.Sin(phase), 0.55f * Mathf.Max(0, Mathf.Cos(phase)), 0);

        public struct EscapePose
        {
            public float FigureScale, Extension, Lean, Stride;
            public Vector3 RootOffset, BodyOffset, BodyScale, FrontFoot, BackFoot;
        }
    }
}
