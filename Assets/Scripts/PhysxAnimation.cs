using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class PhysxAnimation : MonoBehaviour
{
    public enum AnimationUpdateMode
    {
        Update = 0,
        FixedUpdate = 1,
        None = 2
    }

    public AnimationUpdateMode UpdateMode = PhysxAnimation.AnimationUpdateMode.Update;

    public Transform RootJoint = null;
    [Range(0, 1)]
    public float DampValue = 0.1f;
    [Range(0, 1)]
    public float StiffnessValue = 0.1f;

    // This value is very sensitive 
    public Vector3 ForceValue = Vector3.zero;

    private Vector3 _transformBias = Vector3.zero;
    private Vector3 _transformLastPos = Vector3.zero;

    [System.Serializable]
    public class Particle
    {
        public Transform Joint;
        public int ParentIndex;
        public float DampValue;
        public float StiffnessValue;

        // world space
        public Vector3 CurPos;
        public Vector3 LastPos;

        private Vector3 _origLocalPos;
        private Quaternion _origLocalRot;

        public Particle(Transform transform,
            int parentIndex,
            float damping,
            float stiffness)
        {
            Joint = transform;
            ParentIndex = parentIndex;

            CurPos = LastPos = transform.position;

            DampValue = damping;
            StiffnessValue = stiffness;

            _origLocalPos = transform.localPosition;
            _origLocalRot = transform.localRotation;
        }

        public void Reset()
        {
            Joint.localPosition = _origLocalPos;
            Joint.localRotation = _origLocalRot;
        }
    }

    Particle[] _particles = new Particle[0];

    void Start()
    {
        InitParticles();
    }

    void OnValidate()
    {
        DampValue = Mathf.Clamp01(DampValue);
        StiffnessValue = Mathf.Clamp01(StiffnessValue);

        if (Application.isEditor && Application.isPlaying)
        {
            ResetJoint();
            InitParticles();
        }
    }

    void FixedUpdate()
    {
        if (UpdateMode == AnimationUpdateMode.FixedUpdate)
        {
            ResetJoint();

            // http://www.cnblogs.com/miloyip/archive/2011/06/14/alice_madness_returns_hair.html
            if (RootJoint == null)
                return;

            UpdateParticle(Time.fixedDeltaTime);
        }
    }

    void LateUpdate()
    {
        if(UpdateMode == AnimationUpdateMode.Update)
        {
            ResetJoint();

            // http://www.cnblogs.com/miloyip/archive/2011/06/14/alice_madness_returns_hair.html
            if (RootJoint == null)
                return;

            UpdateParticle(Time.deltaTime);
        }
    }

    void InitParticles()
    {
        if (RootJoint == null)
            return;

        _transformLastPos = transform.position;
        _transformBias = Vector3.zero;

        Transform[] joints = RootJoint.GetComponentsInChildren<Transform>(includeInactive: false);
        _particles = new Particle[joints.Length];
        for (int i = 0; i < joints.Length; ++i)
            _particles[i] = new Particle(joints[i], i - 1, DampValue, StiffnessValue);
    }

    void ResetJoint()
    {
        for (int i = 0; i < _particles.Length; ++i)
            _particles[i].Reset();
    }

    void UpdateParticle(float deltaTime)
    {
        float t2 = deltaTime * deltaTime;
        _transformBias = transform.position - _transformLastPos;
        _transformLastPos = transform.position;

        for (int i = 0; i < _particles.Length; ++i)
        {
            Particle p = _particles[i];

            // 1. calculate verlet
            // Don't move root joint
            if (p.ParentIndex >= 0)
            {
                // Verlet
                Vector3 jointBias = p.CurPos - p.LastPos;
                Vector3 transformBias = _transformBias * p.StiffnessValue;
                p.LastPos = p.CurPos + transformBias;
                p.CurPos = p.CurPos + (1.0f - p.DampValue) * jointBias + ForceValue * t2 + transformBias;
            }
            else
            {
                p.LastPos = p.CurPos;
                p.CurPos = p.Joint.position;
            }

            // 2. calculate constraint
            if (i > 0)
            {
                Particle parentP = _particles[p.ParentIndex];

                float restLength = (parentP.Joint.position - p.Joint.position).magnitude;

                // keep shape
                if (p.StiffnessValue > 0)
                {
                    Matrix4x4 parentMat = parentP.Joint.localToWorldMatrix;
                    parentMat.SetColumn(3, parentP.CurPos);
                    Vector3 restPos = parentMat.MultiplyPoint3x4(p.Joint.localPosition);

                    Vector3 d = restPos - p.CurPos;
                    p.CurPos += d * p.StiffnessValue;

                    d = restPos - p.CurPos;
                    float len = d.magnitude;
                    float maxlen = restLength * (1 - p.StiffnessValue) * 2;
                    if (len > maxlen)
                        p.CurPos += d * ((len - maxlen) / len);
                }

                // keep length
                Vector3 dd = parentP.CurPos - p.CurPos;
                float leng = dd.magnitude;
                if (leng > 0)
                    p.CurPos += dd * ((leng - restLength) / leng);

                // 3. apply transform
                if (parentP.Joint.childCount <= 1)       // do not modify bone orientation if has more then one child
                {
                    Vector3 v = p.Joint.localPosition;
                    Vector3 v2 = p.CurPos - parentP.CurPos;

                    Quaternion rot = Quaternion.FromToRotation(parentP.Joint.TransformDirection(v), v2);
                    parentP.Joint.rotation = rot * parentP.Joint.rotation;
                }

                p.Joint.position = p.CurPos;
            }
        }
        _transformBias = Vector3.zero;
    }
}
