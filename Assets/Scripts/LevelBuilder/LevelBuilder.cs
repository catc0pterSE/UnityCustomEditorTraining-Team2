using System;
using System.Collections.Generic;
using System.IO;
using Unity.VisualScripting;
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
    private const float _rotationSpeed = 2;

    private readonly Vector2 _iconDimensions = new Vector2(100, 100);

    private Vector2 _scrollPosition;
    private int _selectedElement;
    private List<GameObject> _catalog = new List<GameObject>();
    private bool _building;
    private int _selectedTabNumber = 0;
    private string[] _tabNames = { "Buildings", "Plants", "Props", "Rocks", "Skeletons", "ShipWreck" };
    private GameObject _createdObject;
    private GameObject _parent;
    private float _runningTime;

    [MenuItem("Level/Builder")]
    private static void ShowWindow()
    {
        GetWindow(typeof(LevelBuilder));
    }

    private void OnFocus()
    {
        SceneView.duringSceneGui -= OnSceneGUI;
        SceneView.duringSceneGui += OnSceneGUI;
    }

    private void OnGUI()
    {
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
        }

        EditorGUILayout.HelpBox("To rotate the object, use the Q and E buttons. Q counterclockwise and E clockwise",
            MessageType.Info);
    }

    private void DrawGrid(string assetPath)
    {
        RefreshCatalog(assetPath);
        _parent = (GameObject)EditorGUILayout.ObjectField("Parent", _parent, typeof(GameObject), true);
        EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);

        /*if (_createdObject != null)
        {
            EditorGUILayout.LabelField("Created Object Settings");
            Transform createdTransform = _createdObject.transform;
            createdTransform.position = EditorGUILayout.Vector3Field("Position", createdTransform.position);
            createdTransform.rotation =
                Quaternion.Euler(EditorGUILayout.Vector3Field("Rotation", createdTransform.rotation.eulerAngles));
            createdTransform.localScale = EditorGUILayout.Vector3Field("Scale", createdTransform.localScale);
        }*/

        EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
        _building = GUILayout.Toggle(_building, "Start building", "Button", GUILayout.Height(60));
        EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
        EditorGUILayout.BeginVertical(GUI.skin.window);
        _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);
        DrawCatalog(GetCatalogIcons());
        EditorGUILayout.EndScrollView();
        EditorGUILayout.EndVertical();
    }

    /*private void OnSceneGUI(SceneView sceneView)
    {
        if (_building)
        {
            if (Raycast(out Vector3 contactPoint))
            {
                DrawPounter(contactPoint, Color.red);

                if (CheckInput())
                {
                    CreateObject(contactPoint);
                }

                sceneView.Repaint();
            }
        }
    }*/

    private void OnSceneGUI(SceneView sceneView)
    {
        if (_building)
        {
            if (_createdObject == null)
                CreateObject();

            if (Raycast(out Vector3 contactPoint))
            {
                DrawPointer(contactPoint, Color.red);
                _createdObject.transform.position = contactPoint;

                if (CheckRotationInput(out Vector3 rotation))
                {
                    Quaternion quaternion = _createdObject.transform.rotation;
                    quaternion.eulerAngles = rotation;
                    _createdObject.transform.rotation = quaternion;
                }

                if (CheckPlacementInput())
                {
                    _building = false;
                    _createdObject = null;
                }

                sceneView.Repaint();
            }
        }
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

        if (Physics.Raycast(guiRay, out RaycastHit raycastHit, Single.PositiveInfinity,
                LayerMask.GetMask(LayerMask.LayerToName(_parent.layer))))
        {
            contactPoint = raycastHit.point;
            return true;
        }

        return false;
    }

    private void DrawPointer(Vector3 position, Color color)
    {
        Handles.color = color;
        Handles.DrawWireCube(position, Vector3.one);
    }

    private bool CheckPlacementInput()
    {
        HandleUtility.AddDefaultControl(0);

        return Event.current.type == EventType.MouseDown && Event.current.button == 0;
    }

    /*private void CreateObject(Vector3 position)
    {
        if (_selectedElement < _catalog.Count)
        {
            GameObject prefab = _catalog[_selectedElement];
            _createdObject = Instantiate(prefab);
            _createdObject.transform.position = position;
            _createdObject.transform.parent = _parent.transform;

            Undo.RegisterCreatedObjectUndo(_createdObject, "Create");
        }
    }*/

    private void CreateObject()
    {
        if (_selectedElement < _catalog.Count == false)
            return;

        GameObject prefab = _catalog[_selectedElement];
        _createdObject = Instantiate(prefab, _parent.transform, true);
        Undo.RegisterCreatedObjectUndo(_createdObject, "Create");
    }

    /*private void DrawCatalog(List<GUIContent> catalogIcons, int width, int height)
    {
        _selectedElement = GUILayout.SelectionGrid(_selectedElement, catalogIcons.ToArray(), 4, GUILayout.Width(width), GUILayout.Height(height));
    }*/

    private void DrawCatalog(List<GUIContent> catalogIcons)
    {
        int xIconCount = (int)position.width / (int)_iconDimensions.x;
        int yIconCount = catalogIcons.Count / xIconCount;
        float width = xIconCount * _iconDimensions.x;
        float height = yIconCount * _iconDimensions.y;

        _selectedElement = GUILayout.SelectionGrid(_selectedElement, catalogIcons.ToArray(), xIconCount,
            GUILayout.Width(width),
            GUILayout.Height(height));
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