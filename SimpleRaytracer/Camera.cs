﻿using System.Numerics;

namespace SimpleRaytracer
{
    public class Camera : Transform
    {
        public float NearClipPlane { get; set; }
        public float FieldOfView { get; set; }
        public float Aspect { get; set; }
        public float PlaneHeight => NearClipPlane * (float)Math.Tan(FieldOfView * 0.5f * (Math.PI / 180)) * 2;
        public float PlaneWidth => PlaneHeight * Aspect;
        public Vector3 BottomLeft => new(-PlaneWidth / 2, -PlaneHeight / 2, NearClipPlane);

        public Camera()
        {
        }

        public Camera(Vector3 position, float nearClipPlane, float fieldOfView, float aspect) : base(position)
        {
            NearClipPlane = nearClipPlane;
            FieldOfView = fieldOfView;
            Aspect = aspect;
        }

        public Camera(Vector3 position, Quaternion rotation, float nearClipPlane, float fieldOfView, float aspect) : base(position, rotation)
        {
            NearClipPlane = nearClipPlane;
            FieldOfView = fieldOfView;
            Aspect = aspect;
        }
    }
}
