

using UnityEngine;
using System.Collections;
using System;
using System.Collections.Generic;

public class ProceduralCapsule : MonoBehaviour
{
    private float _height = 0f;
    private float _radi = 0f;   
    private Vector3 _center;
    public bool Generate;

    public List <GameObject> joints;
    
#if UNITY_EDITOR
    [ContextMenu("Generate Procedural Capsule")]
    public void GenerateProceduralCapsule(GameObject go = null)
    {
        if (go == null) go = gameObject;
        for (int i = 0; i < go.transform.childCount; i++)
        {
            GameObject child = go.transform.GetChild(i).gameObject;
            AddToList(child);

            if (child.transform.childCount > 0)
                GenerateProceduralCapsule(child);
        }
        foreach (var child in joints)
        {
            GetCapsuleColliderValues(child);
            GenerateMesh(child.transform);

        }
    }
#endif

    private void Update()
    {
        if (Generate)
        {
            GenerateProceduralCapsule();
            Generate = false;
        }
    }


    public void GenerateMesh(Transform childTransform)
    {          
        GameObject go = GameObject.CreatePrimitive(PrimitiveType.Capsule);       
        DestroyImmediate(go.GetComponent<CapsuleCollider>());
        go.transform.parent = childTransform;
        go.transform.Rotate(Vector3.right, 90f);
        go.transform.localPosition = _center;
        go.transform.localScale = new Vector3(_height * 0.5f, _height * 0.5f, _height * 0.5f);        
    }

    private void AddToList(GameObject child)
    {
        CapsuleCollider _capsuleCollider = child.GetComponent<CapsuleCollider>();
        if (_capsuleCollider != null)
        {
            joints.Add(child);            
        }
    }

    private void GetCapsuleColliderValues(GameObject child)
    {
        CapsuleCollider _capsuleCollider = child.GetComponent<CapsuleCollider>();
        _height = _capsuleCollider.height;
        _radi = _capsuleCollider.radius;
        _center = new Vector3(_capsuleCollider.center.x, _capsuleCollider.center.y, _capsuleCollider.center.z);
    }

}
