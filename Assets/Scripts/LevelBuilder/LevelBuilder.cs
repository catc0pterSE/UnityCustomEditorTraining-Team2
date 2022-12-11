using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

public class LevelBuilder : EditorWindow
{
    private const string _pathBuildings = "Assets/Editor Resources/Buildings";
    private const string _pathProps = "Assets/Editor Resources/Props";
    private const string _pathPlants = "Assets/Editor Resources/Plants";
    private const string _pathRocks = "Assets/Editor Resources/Rocks";
    private const string _pathSkeletons = "Assets/Editor Resources/Skeletons";
    private const string _pathShip = "Assets/Editor Resources/ShipWreck";
    private const string _pathVehicles = "Assets/Editor Resources/Vehicles";
    private const string _pathOther = "Assets/Editor Resources/Other";
    private const float _rotationSpeed = 2;
    private const float _scaleSpeed = 1.3f;
    private const string _propLayerName = "Prop";
    private const float _half = 0.5f;

    private readonly Vector2 _iconDimensions = new Vector2(100, 100);

    private Vector2 _scrollPosition;
    private int _selectedElement;
    private List<GameObject> _catalog = new List<GameObject>();
    private bool _building;
    private int _selectedTabNumber;

    private string[] _tabNames =
        { "Buildings", "Plants", "Props", "Rocks", "Skeletons", "ShipWreck", "Vehicles", "Other" };

    private GameObject _createdObject = null;
    private GameObject _parent;
    private LayerMask _layerMask;

    [MenuItem("Level/Builder")]
    private static void ShowWindow()
    {
        GetWindow(typeof(LevelBuilder));
    }

    private void OnEnable()
    {
        SceneView.duringSceneGui += OnSceneGUI;
    }

    private void OnDisable()
    {
        SceneView.duringSceneGui -= OnSceneGUI;
    }

    private void OnGUI()
    {
        _parent = (GameObject)EditorGUILayout.ObjectField("Parent", _parent, typeof(GameObject), true);

        if (_parent == null)
            return;

        _selectedTabNumber = GUILayout.Toolbar(_selectedTabNumber, _tabNames);

        switch (_selectedTabNumber)
        {
            case 0:
                DrawGrid(_pathBuildings);
                break;
            case 1:
                DrawGrid(_pathPlants);
                break;
            case 2:
                DrawGrid(_pathProps);
                break;
            case 3:
                DrawGrid(_pathRocks);
                break;
            case 4:
                DrawGrid(_pathSkeletons);
                break;
            case 5:
                DrawGrid(_pathShip);
                break;
            case 6:
                _building:
                DrawGrid(_pathVehicles);
                break;
            case 7:
                DrawGrid(_pathOther);
                break;
        }

        EditorGUILayout.HelpBox(
            "To rotate the object, use the Q and E buttons. Q counterclockwise and E clockwise\nTo upscale object use W, to downscale use S",
            MessageType.Info);
    }

    private void DrawGrid(string assetPath)
    {
        RefreshCatalog(assetPath);
        EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
        EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
        _building = GUILayout.Toggle(_building, "Start building", "Button", GUILayout.Height(60));
        EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
        EditorGUILayout.BeginVertical(GUI.skin.window);
        _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);
        DrawCatalog(GetCatalogIcons());
        EditorGUILayout.EndScrollView();
        EditorGUILayout.EndVertical();
    }

    private void OnSceneGUI(SceneView sceneView)
    {
        if (_building == false)
            return;

        sceneView.Focus();

        if (_createdObject == null)
        {
            CreateObject();
        }

        if (Raycast(out Vector3 contactPoint))
        {
            DrawPointer(Color.red);
            ManipulateCreatedObject(contactPoint);
            sceneView.Repaint();
        }
    }

    private void ManipulateCreatedObject(Vector3 contactPoint)
    {
        _createdObject.transform.position = contactPoint;

        if (CheckRotationInput(out Vector3 rotation))
        {
            Quaternion quaternion = _createdObject.transform.rotation;
            quaternion.eulerAngles = rotation;
            _createdObject.transform.rotation = quaternion;
        }

        if (CheckScaleInput(out Vector3 newScale))
        {
            _createdObject.transform.localScale = newScale;
        }

        if (CheckPlacementInput())
        {
            Bounds bounds = GetCreatedObjectBounds();

            if (Physics.OverlapBox
                (
                    bounds.center,
                    bounds.size * _half,
                    _createdObject.transform.rotation,
                    LayerMask.GetMask(_propLayerName)
                ).Length > 0)
                return;

            _createdObject.layer = LayerMask.NameToLayer(_propLayerName);
            _building = false;
            _createdObject = null;
        }
    }

    private bool CheckScaleInput(out Vector3 newScale)
    {
        newScale = _createdObject.transform.localScale;

        if (Event.current.type == EventType.KeyDown)
        {
            if (Event.current.keyCode == KeyCode.W)
            {
                newScale *= _scaleSpeed;
                return true;
            }

            if (Event.current.keyCode == KeyCode.S)
            {
                newScale /= _scaleSpeed;
                return true;
            }
        }

        return false;
    }

    private bool CheckRotationInput(out Vector3 rotation)
    {
        rotation = _createdObject.transform.rotation.eulerAngles;

        if (Event.current.type == EventType.KeyDown)
        {
            if (Event.current.keyCode == KeyCode.Q)
            {
                rotation.y -= _rotationSpeed;
                return true;
            }

            if (Event.current.keyCode == KeyCode.E)
            {
                rotation.y += _rotationSpeed;
                return true;
            }
        }

        return false;
    }

    private bool Raycast(out Vector3 contactPoint)
    {
        Ray guiRay = HandleUtility.GUIPointToWorldRay(Event.current.mousePosition);
        contactPoint = Vector3.zero;

        if (Physics.Raycast
            (
                guiRay,
                out RaycastHit raycastHit,
                Single.PositiveInfinity,
                LayerMask.GetMask(LayerMask.LayerToName(_parent.layer))
            ))
        {
            contactPoint = raycastHit.point;
            return true;
        }

        return false;
    }

    private void DrawPointer(Color color)
    {
        Bounds bounds = GetCreatedObjectBounds();
        Handles.color = color;
        Handles.DrawWireCube(bounds.center, bounds.size);
    }

    private Bounds GetCreatedObjectBounds()
    {
        if (_createdObject != null)
            return _createdObject.GetComponent<MeshRenderer>().bounds;

        return new Bounds(Vector3.zero, Vector3.zero);
    }

    private bool CheckPlacementInput()
    {
        HandleUtility.AddDefaultControl(0);
        return Event.current.type == EventType.MouseDown && Event.current.button == 0;
    }

    private void CreateObject()
    {
        if (_selectedElement < _catalog.Count == false)
            return;

        GameObject prefab = _catalog[_selectedElement];
        _createdObject = Instantiate(prefab, _parent.transform, true);
        Undo.RegisterCreatedObjectUndo(_createdObject, "Create");
    }

    private void DrawCatalog(List<GUIContent> catalogIcons)
    {
        int xIconCount = Mathf.Clamp((int)position.width / (int)_iconDimensions.x, min: 1, Int32.MaxValue);
        int yIconCount = Mathf.Clamp(catalogIcons.Count / xIconCount, min: 1, Int32.MaxValue);
        float width = xIconCount * _iconDimensions.x;
        float height = yIconCount * _iconDimensions.y;

        _selectedElement = GUILayout.SelectionGrid
        (
            _selectedElement,
            catalogIcons.ToArray(),
            xIconCount,
            GUILayout.Width(width),
            GUILayout.Height(height)
        );
    }

    private List<GUIContent> GetCatalogIcons()
    {
        List<GUIContent> catalogIcons = new List<GUIContent>();

        foreach (var element in _catalog)
        {
            Texture2D texture = AssetPreview.GetAssetPreview(element);
            catalogIcons.Add(new GUIContent(texture));
        }

        return catalogIcons;
    }

    private void RefreshCatalog(string path)
    {
        _catalog.Clear();
        Directory.CreateDirectory(path);
        string[] prefabFiles = Directory.GetFiles(path, "*.prefab");

        foreach (var prefabFile in prefabFiles)
            _catalog.Add(AssetDatabase.LoadAssetAtPath(prefabFile, typeof(GameObject)) as GameObject);
    }
}