﻿using UnityEngine;

namespace Leap.Unity.HandsModule {

    [System.Serializable]
    public class BoundHand {
        public BoundFinger[] fingers = new BoundFinger[5];
        public BoundBone wrist = new BoundBone();
        public BoundBone elbow = new BoundBone();
    }

    [System.Serializable]
    public class BoundFinger {
        public BoundBone[] boundBones = new BoundBone[4];
    }

    [System.Serializable]
    public class BoundBone {
        public Transform boundTransform;
        public TransformStore startTransform = new TransformStore();
        public TransformStore offset = new TransformStore();
    }

    [System.Serializable]
    public class TransformStore {
        public Vector3 position = Vector3.zero;
        public Vector3 rotation = Vector3.zero;
    }

    public enum BoundTypes {
        THUMB_METACARPAL,
        THUMB_PROXIMAL,
        THUMB_INTERMEDIATE,
        THUMB_DISTAL,

        INDEX_METACARPAL,
        INDEX_PROXIMAL,
        INDEX_INTERMEDIATE,
        INDEX_DISTAL,

        MIDDLE_METACARPAL,
        MIDDLE_PROXIMAL,
        MIDDLE_INTERMEDIATE,
        MIDDLE_DISTAL,

        RING_METACARPAL,
        RING_PROXIMAL,
        RING_INTERMEDIATE,
        RING_DISTAL,

        PINKY_METACARPAL,
        PINKY_PROXIMAL,
        PINKY_INTERMEDIATE,
        PINKY_DISTAL,

        WRIST,
        ELBOW,
    }
}