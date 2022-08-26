using System;
using System.Collections;
using System.Collections.Generic;
using Attributes;
using OpenMetaverse;
using SLUnity.Managers;
using SLUnity.Snapshot;
using UnityEngine;

public class SlRegionSceneSaverController : MonoBehaviour
{
    [SerializeField][ReadOnly]
    private float _lastObjectUpdate;
    [SerializeField][ReadOnly]
    private float _lastTerrainUpdate;

    [Min(1f)]
    [SerializeField] private float _waitTime;

    [ReadOnly]
    [SerializeField] private float _currentTime;
    [SerializeField] 
    private SLRegionSceneSaver _saver;

    [SerializeField] private bool _saved = false;

    private void Awake()
    {
        _saver = GetComponent<SLRegionSceneSaver>();
    }

    private void OnEnable()
    {
        SLManager.Instance.Client.Terrain.LandPatchReceived += TerrainOnLandPatchReceived;
        SLManager.Instance.Client.Objects.ObjectUpdate += ObjectsOnObjectUpdate;
    }


    private void OnDisable()
    {
        SLManager.Instance.Client.Terrain.LandPatchReceived -= TerrainOnLandPatchReceived;
        SLManager.Instance.Client.Objects.ObjectUpdate -= ObjectsOnObjectUpdate;
    }

    private void ObjectsOnObjectUpdate(object sender, PrimEventArgs e)
    {
        if(e.IsNew)
            _lastObjectUpdate = _currentTime;//Time.time;
    }
    private void TerrainOnLandPatchReceived(object sender, LandPatchReceivedEventArgs e)
    {
        _lastTerrainUpdate = _currentTime;//Time.time;
    }

    private void Update()
    {
        _currentTime = Time.time;
        if(_saved)
            return;
        
        var now = Time.time;
        var last = (_lastObjectUpdate < _lastTerrainUpdate) ? _lastTerrainUpdate : _lastObjectUpdate;
        if (last + _waitTime < now)
        {
            _saved = true;
            _saver.Save = true;
            
        }
    }
}

