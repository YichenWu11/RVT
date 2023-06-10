using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
public class AllSettings : MonoBehaviour
{
    public Material material;
    
    [Range(0, 20000)] 
    public int basemapDistance = 1000;

    private Terrain[] _allChild;
    
    private void Start()
    {
        _allChild = GetComponentsInChildren<Terrain>();
        foreach (var terrain in _allChild)
        {
            terrain.materialTemplate = material;
            terrain.basemapDistance = basemapDistance;
        }
    }
}
