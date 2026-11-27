using System;
using UnityEngine;

namespace RockAndHardPlace
{
    // Animate the tilemap hierarchy, rather than a clip with absolute scene coordinates.
    // The root travels; only the body squashes. Collision and turn changes belong to Mountain.
    public sealed class MountainEscape : MonoBehaviour
    {
        public const float WakeDuration = 0.45f;
        public const float CrouchDuration = 0.35f;
        public const float RiseDuration = 0.45f;
        public const float RunDuration = 3.3f;
        public const float RunStart = WakeDuration + CrouchDuration + RiseDuration;
        public const float Duration = RunStart + RunDuration;

        [SerializeField] private Transform body;
        [SerializeField] private Transform frontLeg;
        [SerializeField] private Transform backLeg;
        [SerializeField] private GameObject restingFace;
        [SerializeField] private GameObject awakeFace;
        [SerializeField, Min(15)] private float minimumTravel = 24;
        [SerializeField, Min(1)] private float strideLength = 3.4f;

        public bool IsRunning { get; private set; }
        public event Action<Vector3> Footstep;

        private Mountain mountain;
        private Vector3 origin;
        private Vector3 bodyOrigin;
        private Vector3 bodyScale;
        private Quaternion bodyRotation;
        private Vector3 frontOrigin;
        private Vector3 backOrigin;
        private float elapsed;
        private float travel;
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
            if (body != null)
            {
                bodyOrigin = body.localPosition;
                bodyScale = body.localScale;
                bodyRotation = body.localRotation;
            }
            if (frontLeg != null) frontOrigin = frontLeg.localPosition;
            if (backLeg != null) backOrigin = backLeg.localPosition;
            travel = minimumTravel;
            var camera = Camera.main;
            if (camera != null)
            {
                float right = camera.ViewportToWorldPoint(new Vector3(1, 0.5f,
                    Mathf.Abs(camera.transform.position.z - transform.position.z))).x;
                float halfWidth = GetComponentsInChildren<Renderer>().Length == 0 ? 5 :
                    GetBodyHalfWidth();
                Vector3 clearance = Vector3.right * Mathf.Max(0, right + halfWidth + 2 - transform.position.x);
                if (transform.parent != null) clearance = transform.parent.InverseTransformVector(clearance);
                travel = Mathf.Max(travel, Mathf.Abs(clearance.x));
            }
            elapsed = 0;
            lastStep = -1;
            if (restingFace != null) restingFace.SetActive(false);
            if (awakeFace != null) awakeFace.SetActive(true);
            IsRunning = true;
            Apply(Sample(0, travel, strideLength));
        }

        private float GetBodyHalfWidth()
        {
            float width = 0;
            foreach (var renderer in GetComponentsInChildren<Renderer>())
                width = Mathf.Max(width, Mathf.Abs(renderer.bounds.min.x - transform.position.x),
                    Mathf.Abs(renderer.bounds.max.x - transform.position.x));
            return width;
        }

        private void Update()
        {
            if (!IsRunning) return;
            elapsed = Mathf.Min(elapsed + Time.deltaTime, Duration);
            var pose = Sample(elapsed, travel, strideLength);
            Apply(pose);
            if (elapsed >= RunStart)
            {
                int step = Mathf.FloorToInt(pose.Stride / Mathf.PI);
                if (step != lastStep)
                {
                    lastStep = step;
                    var foot = step % 2 == 0 ? frontLeg : backLeg;
                    Footstep?.Invoke(foot != null ? foot.position - Vector3.up : transform.position);
                }
            }
            if (elapsed < Duration) return;
            IsRunning = false;
            mountain.OnLeaveFinished();
        }

        private void Apply(EscapePose pose)
        {
            transform.localPosition = origin + Vector3.right * pose.Distance;
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
        public static EscapePose Sample(float seconds, float distance, float strideLength = 3.4f)
        {
            float t = Mathf.Clamp(seconds, 0, Duration);
            var pose = new EscapePose { BodyScale = Vector3.one };
            if (t < WakeDuration)
            {
                pose.BodyOffset.x = Mathf.Sin(t * 45) * 0.055f * Mathf.Sin(t / WakeDuration * Mathf.PI);
                return pose;
            }
            if (t < WakeDuration + CrouchDuration)
            {
                float u = Mathf.SmoothStep(0, 1, (t - WakeDuration) / CrouchDuration);
                pose.BodyScale = new Vector3(1 + 0.12f * u, 1 - 0.16f * u, 1);
                return pose;
            }
            if (t < RunStart)
            {
                float u = Mathf.SmoothStep(0, 1, (t - WakeDuration - CrouchDuration) / RiseDuration);
                pose.BodyScale = Vector3.Lerp(new Vector3(1.12f, 0.84f, 1), Vector3.one, u);
                pose.BodyOffset.y = u;
                pose.Extension = u;
                pose.Lean = -3 * u;
                return pose;
            }
            float run = (t - RunStart) / RunDuration;
            pose.Distance = Mathf.Max(0, distance) * Mathf.Pow(run, 1.7f);
            pose.Stride = pose.Distance / Mathf.Max(1, strideLength) * Mathf.PI * 2;
            float blend = Mathf.SmoothStep(0, 1, (t - RunStart) / 0.2f);
            float bounce = Mathf.Abs(Mathf.Sin(pose.Stride));
            pose.BodyOffset.y = 1 + 0.18f * bounce * blend;
            pose.BodyScale = new Vector3(1 + 0.025f * bounce * blend, 1 - 0.035f * bounce * blend, 1);
            pose.Lean = -3 - blend * (2 + 1.5f * Mathf.Sin(pose.Stride));
            pose.Extension = 1;
            pose.FrontFoot = Foot(pose.Stride) * blend;
            pose.BackFoot = Foot(pose.Stride + Mathf.PI) * blend;
            return pose;
        }

        private static Vector3 Foot(float phase) =>
            new Vector3(0.7f * Mathf.Sin(phase), 0.55f * Mathf.Max(0, Mathf.Cos(phase)), 0);

        public struct EscapePose
        {
            public float Distance, Extension, Lean, Stride;
            public Vector3 BodyOffset, BodyScale, FrontFoot, BackFoot;
        }
    }
}
