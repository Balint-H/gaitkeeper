using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MotionMatch
{
    public static class IKUtility 
    {
        public static GameObject CreateEffector(string name, Vector3 position, Quaternion rotation)
        {
            var effector = Resources.Load("Effector/Effector", typeof(GameObject)) as GameObject;
            return CreateEffectorFromGO(name, effector, position, rotation);
        }

        public static GameObject CreateBodyEffector(string name, Vector3 position, Quaternion rotation)
        {
            var prefab = Resources.Load("Effector/BodyEffector", typeof(GameObject)) as GameObject;
            var effector = CreateEffectorFromGO(name, prefab, position, rotation);
            effector.transform.localScale = new Vector3(0.05f, 0.05f, 0.05f);
            return effector;
        }

        public static GameObject CreateEffectorFromGO(string name, GameObject prefab, Vector3 position, Quaternion rotation)
        {
            var effector = Object.Instantiate(prefab);
            effector.name = name;
            effector.transform.position = position;
            effector.transform.rotation = rotation;
            effector.transform.localScale = Vector3.one * 0.15f;
            var meshRenderer = effector.GetComponent<MeshRenderer>();
            meshRenderer.material.color = Color.magenta;
            return effector;
        }

        static public Color FadeEffectorColorByWeight(Color original, float weight)
        {
            Color color = original * (0.2f + 0.8f * weight);
            color.a = (0.2f + 0.5f * weight);
            return color;
        }
    }
}
