﻿// This file is part of the Prowl Game Engine
// Licensed under the MIT License. See the LICENSE file in the project root for details.

using System;

using Prowl.Runtime.Cloning;
using Prowl.Echo;

namespace Prowl.Runtime;

[Cloning.ManuallyCloned()]
public class Transform : ICloneExplicit
{
    #region Properties

    #region Position
    public Vector3 position
    {
        get
        {
            if (_isWorldPosDirty)
            {
                if (parent != null)
                    _cachedWorldPosition = parent.localToWorldMatrix.MultiplyPoint(m_LocalPosition);
                else
                    _cachedWorldPosition = m_LocalPosition;
                _isWorldPosDirty = false;
            }
            return MakeSafe(_cachedWorldPosition);
        }
        set
        {
            Vector3 newPosition = value;
            if (parent != null)
                newPosition = parent.InverseTransformPoint(newPosition);

            if (m_LocalPosition != newPosition)
            {
                m_LocalPosition = MakeSafe(newPosition);
                InvalidateTransformCache();
            }
        }
    }

    public Vector3 localPosition
    {
        get => MakeSafe(m_LocalPosition);
        set
        {
            if (m_LocalPosition != value)
            {
                m_LocalPosition = MakeSafe(value);
                InvalidateTransformCache();
            }
        }
    }
    #endregion

    #region Rotation
    public Quaternion rotation
    {
        get
        {
            if (_isWorldRotDirty)
            {
                Quaternion worldRot = m_LocalRotation;
                Transform p = parent;
                while (p != null)
                {
                    worldRot = p.m_LocalRotation * worldRot;
                    p = p.parent;
                }
                _cachedWorldRotation = worldRot;
                _isWorldRotDirty = false;
            }
            return MakeSafe(_cachedWorldRotation);
        }
        set
        {
            var newVale = Quaternion.identity;
            if (parent != null)
                newVale = MakeSafe(Quaternion.NormalizeSafe(Quaternion.Inverse(parent.rotation) * value));
            else
                newVale = MakeSafe(Quaternion.NormalizeSafe(value));
            if(localRotation != newVale)
            {
                localRotation = newVale;
                InvalidateTransformCache();
            }
        }
    }

    public Quaternion localRotation
    {
        get => MakeSafe(m_LocalRotation);
        set
        {

            if (m_LocalRotation != value)
            {
                m_LocalRotation = MakeSafe(value);
                InvalidateTransformCache();
            }
        }
    }

    public Vector3 eulerAngles
    {
        get => MakeSafe(rotation.eulerAngles);
        set
        {
            rotation = MakeSafe(Quaternion.Euler(value));
            InvalidateTransformCache();
        }
    }

    public Vector3 localEulerAngles
    {
        get => MakeSafe(m_LocalRotation.eulerAngles);
        set
        {
            m_LocalRotation.eulerAngles = MakeSafe(value);
            InvalidateTransformCache();
        }
    }
    #endregion

    #region Scale

    public Vector3 localScale
    {
        get => MakeSafe(m_LocalScale);
        set
        {
            if (m_LocalScale != value)
            {
                m_LocalScale = MakeSafe(value);
                InvalidateTransformCache();
            }
        }
    }

    public Vector3 lossyScale
    {
        get
        {
            if (_isWorldScaleDirty)
            {
                Vector3 scale = localScale;
                Transform p = parent;
                while (p != null)
                {
                    scale.Scale(p.localScale);
                    p = p.parent;
                }
                _cachedLossyScale = scale;
                _isWorldScaleDirty = false;
            }
            return MakeSafe(_cachedLossyScale);
        }
    }

    #endregion

    public Vector3 right { get => rotation * Vector3.right; }     // TODO: Setter
    public Vector3 up { get => rotation * Vector3.up; }           // TODO: Setter
    public Vector3 forward { get => rotation * Vector3.forward; } // TODO: Setter

    public Matrix4x4 worldToLocalMatrix => localToWorldMatrix.Invert();

    public Matrix4x4 localToWorldMatrix
    {
        get
        {
            if (_isLocalToWorldMatrixDirty)
            {
                Matrix4x4 t = Matrix4x4.TRS(m_LocalPosition, m_LocalRotation, m_LocalScale);
                _cachedLocalToWorldMatrix = parent != null ? parent.localToWorldMatrix * t : t;
                _isLocalToWorldMatrixDirty = false;
            }
            return _cachedLocalToWorldMatrix;
            //Matrix4x4 t = Matrix4x4.TRS(m_LocalPosition, m_LocalRotation, m_LocalScale);
            //return parent != null ? parent.localToWorldMatrix * t : t;
        }
    }

    public Transform parent
    {
        get => gameObject?.parent?.Transform;
        set => gameObject?.SetParent(value.gameObject, true);
    }

    // https://forum.unity.com/threads/transform-haschanged-would-be-better-if-replaced-by-a-version-number.700004/
    // Replacement for hasChanged
    public uint version
    {
        get => _version;
        set => _version = value;
    }

    public Transform root => parent == null ? this : parent.root;


    #endregion

    #region Fields

    [SerializeField] Vector3 m_LocalPosition;
    [SerializeField] Vector3 m_LocalScale = Vector3.one;
    [SerializeField] Quaternion m_LocalRotation = Quaternion.identity;

    [SerializeIgnore]
    uint _version = 1;

    [SerializeIgnore] private Vector3 _cachedWorldPosition;
    [SerializeIgnore] private Quaternion _cachedWorldRotation;
    [SerializeIgnore] private Vector3 _cachedLossyScale;
    [SerializeIgnore] private Matrix4x4 _cachedLocalToWorldMatrix;
    [SerializeIgnore] private bool _isLocalToWorldMatrixDirty = true;
    [SerializeIgnore] private bool _isWorldPosDirty = true;
    [SerializeIgnore] private bool _isWorldRotDirty = true;
    [SerializeIgnore] private bool _isWorldScaleDirty = true;

    public GameObject gameObject { get; internal set; }
    #endregion

    private void InvalidateTransformCache()
    {
        _isLocalToWorldMatrixDirty = true;
        _isWorldPosDirty = true;
        _isWorldRotDirty = true;
        _isWorldScaleDirty = true;
        _version++;

        // Invalidate children
        foreach (GameObject child in gameObject.children)
            child.Transform.InvalidateTransformCache();
    }

    public void SetLocalTransform(Vector3 position, Quaternion rotation, Vector3 scale)
    {
        m_LocalPosition = position;
        m_LocalRotation = rotation;
        m_LocalScale = scale;
        InvalidateTransformCache();
    }

    private double MakeSafe(double v) => double.IsNaN(v) ? 0 : v;
    private Vector3 MakeSafe(Vector3 v) => new Vector3(MakeSafe(v.x), MakeSafe(v.y), MakeSafe(v.z));
    private Quaternion MakeSafe(Quaternion v) => new Quaternion(MakeSafe(v.x), MakeSafe(v.y), MakeSafe(v.z), MakeSafe(v.w));

    public Transform? Find(string path)
    {
        ArgumentException.ThrowIfNullOrEmpty(path, nameof(path));

        var names = path.Split('/');
        var currentTransform = this;

        foreach (var name in names)
        {
            ArgumentException.ThrowIfNullOrEmpty(path, nameof(path));

            var childTransform = FindImmediateChild(currentTransform, name);
            if (childTransform == null)
                return null;

            currentTransform = childTransform;
        }

        return currentTransform;
    }

    private Transform? FindImmediateChild(Transform parent, string name)
    {
        foreach (var child in parent.gameObject.children)
            if (child.Name == name)
                return child.Transform;
        return null;
    }

    public Transform? DeepFind(string name)
    {
        if (name == null) return null;
        if (name == gameObject.Name) return this;
        foreach (var child in gameObject.children)
        {
            var t = child.Transform.DeepFind(name);
            if (t != null) return t;
        }
        return null;
    }

    public static string GetPath(Transform target, Transform root)
    {
        string path = target.gameObject.Name;
        while (target.parent != null)
        {
            target = target.parent;
            path = target.gameObject.Name + "/" + path;
            if (target == root)
                break;
        }
        return path;
    }

    public void Translate(Vector3 translation, Transform? relativeTo = null)
    {
        if (relativeTo != null)
            position += relativeTo.TransformDirection(translation);
        else
            position += translation;
    }

    public void Rotate(Vector3 eulerAngles, bool relativeToSelf = true)
    {
        Quaternion eulerRot = Quaternion.Euler(eulerAngles.x, eulerAngles.y, eulerAngles.z);
        if (relativeToSelf)
            localRotation = localRotation * eulerRot;
        else
            rotation = rotation * (Quaternion.Inverse(rotation) * eulerRot * rotation);
    }

    public void Rotate(Vector3 axis, double angle, bool relativeToSelf = true)
    {
        RotateAroundInternal(relativeToSelf ? TransformDirection(axis) : axis, angle * MathD.Deg2Rad);
    }

    public void RotateAround(Vector3 point, Vector3 axis, double angle)
    {
        Vector3 worldPos = position;
        Quaternion q = Quaternion.AngleAxis(angle, axis);
        Vector3 dif = worldPos - point;
        dif = q * dif;
        worldPos = point + dif;
        position = worldPos;
        RotateAroundInternal(axis, angle * MathD.Deg2Rad);
    }

    internal void RotateAroundInternal(Vector3 worldAxis, double rad)
    {
        Vector3 localAxis = InverseTransformDirection(worldAxis);
        if (localAxis.sqrMagnitude > double.Epsilon)
        {
            localAxis.Normalize();
            Quaternion q = Quaternion.AngleAxis(rad, localAxis);
            m_LocalRotation = Quaternion.NormalizeSafe(m_LocalRotation * q);
        }
    }

    public void LookAt(Vector3 worldPosition, Vector3 worldUp)
    {
        // Cheat using Matrix4x4.CreateLookAt
        Matrix4x4 m = Matrix4x4.CreateLookAt(position, worldPosition, worldUp);
        m_LocalRotation = Quaternion.NormalizeSafe(Quaternion.MatrixToQuaternion(m));
    }


    #region Transform

    public Vector3 TransformPoint(Vector3 inPosition) => Vector4.Transform(new Vector4(inPosition, 1.0), localToWorldMatrix).xyz;
    public Vector3 InverseTransformPoint(Vector3 inPosition) => Vector4.Transform(new Vector4(inPosition, 1.0), worldToLocalMatrix).xyz;
    public Quaternion InverseTransformRotation(Quaternion worldRotation) => Quaternion.Inverse(rotation) * worldRotation;

    public Vector3 TransformDirection(Vector3 inDirection) => rotation * inDirection;
    public Vector3 InverseTransformDirection(Vector3 inDirection) => Quaternion.Inverse(rotation) * inDirection;

    public Vector3 TransformVector(Vector3 inVector)
    {
        Vector3 worldVector = inVector;

        Transform cur = this;
        while (cur != null)
        {
            worldVector.Scale(cur.m_LocalScale);
            worldVector = cur.m_LocalRotation * worldVector;

            cur = cur.parent;
        }
        return worldVector;
    }
    public Vector3 InverseTransformVector(Vector3 inVector)
    {
        Vector3 newVector, localVector;
        if (parent != null)
            localVector = parent.InverseTransformVector(inVector);
        else
            localVector = inVector;

        newVector = Quaternion.Inverse(m_LocalRotation) * localVector;
        if (m_LocalScale != Vector3.one)
            newVector.Scale(InverseSafe(m_LocalScale));

        return newVector;
    }

    public Quaternion TransformRotation(Quaternion inRotation)
    {
        Quaternion worldRotation = inRotation;

        Transform cur = this;
        while (cur != null)
        {
            worldRotation = cur.m_LocalRotation * worldRotation;
            cur = cur.parent;
        }
        return worldRotation;
    }

    #endregion

    public Matrix4x4 GetWorldRotationAndScale()
    {
        Matrix4x4 ret = Matrix4x4.TRS(new Vector3(0, 0, 0), m_LocalRotation, m_LocalScale);
        if (parent != null)
        {
            Matrix4x4 parentTransform = parent.GetWorldRotationAndScale();
            ret = parentTransform * ret;
        }
        return ret;
    }

    static double InverseSafe(double f) => MathD.Abs(f) > double.Epsilon ? 1.0F / f : 0.0F;
    static Vector3 InverseSafe(Vector3 v) => new Vector3(InverseSafe(v.x), InverseSafe(v.y), InverseSafe(v.z));
    public void SetupCloneTargets(object target, ICloneTargetSetup setup)
    {
        setup.HandleObject(this, target);
    }
    public void CopyDataTo(object targetObj, ICloneOperation operation)
    {
        operation.HandleObject(this, targetObj);

        Transform target = targetObj as Transform;

        target.gameObject = gameObject;

        target.position = position;
        target.rotation = rotation;
        target.localScale = localScale;
    }
}
