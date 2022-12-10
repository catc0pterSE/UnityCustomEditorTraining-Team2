using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public class LevelBuilder : EditorWindow
{
    private const string _pathBuildings = "Assets/Editor Resources/Buildings";
    private const string _pathProps = "Assets/Editor Resources/Props";
    private const string _pathPlants = "Assets/Editor Resources/Plants";

    private Vector2 _scrollPosition;
    private int _selectedElement;
    private List<GameObject> _catalog = new List<GameObject>();
    private bool _building;
    private int _selectedTabNumber = 0;
    private string[] _tabNames = { "Buildings", "Plants", "Props" };
    private GameObject _createdObject;
    private GameObject _parent;


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

        switch (_selectedTabNumber) //TODO: scale grid size automatically
        {
            case 0:
                DrawAssetTab(_pathBuildings, 400, 1000);
                break;
            case 1:
                DrawAssetTab(_pathPlants, 400, 1000);
                break;
            case 2:
                DrawAssetTab(_pathProps, 400, 10000);
                break;
        }
    }

    private void DrawAssetTab(string assetPath, int width, int height)
    {
        RefreshCatalog(assetPath);
        _parent = (GameObject)EditorGUILayout.ObjectField("Parent", _parent, typeof(GameObject), true);
        EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
        if (_createdObject != null)
        {
            EditorGUILayout.LabelField("Created Object Settings");
            Transform createdTransform = _createdObject.transform;
            createdTransform.position = EditorGUILayout.Vector3Field("Position", createdTransform.position);
            createdTransform.rotation = Quaternion.Euler(EditorGUILayout.Vector3Field("Position", createdTransform.rotation.eulerAngles));
            createdTransform.localScale = EditorGUILayout.Vector3Field("Position", createdTransform.localScale);
        }
        EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
        _building = GUILayout.Toggle(_building, "Start building", "Button", GUILayout.Height(60));
        EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
        EditorGUILayout.BeginVertical(GUI.skin.window);
        _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);
        DrawCatalog(GetCatalogIcons(), width, height);
        EditorGUILayout.EndScrollView();
        EditorGUILayout.EndVertical();
    }
    
    private void OnSceneGUI(SceneView sceneView)
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
    }

    private bool Raycast(out Vector3 contactPoint)
    {
        Ray guiRay = HandleUtility.GUIPointToWorldRay(Event.current.mousePosition);
        contactPoint = Vector3.zero;

        if (Physics.Raycast(guiRay, out RaycastHit raycastHit))
        {
            contactPoint = raycastHit.point;
            return true;
        }

        return false;
    }

    private void DrawPounter(Vector3 position, Color color)
    {
        Handles.color = color;
        Handles.DrawWireCube(position, Vector3.one);
    }

    private bool CheckInput()
    {
        HandleUtility.AddDefaultControl(0);

        return Event.current.type == EventType.MouseDown && Event.current.button == 0;
    }

    private void CreateObject(Vector3 position)
    {
        if (_selectedElement < _catalog.Count)
        {
            GameObject prefab = _catalog[_selectedElement];
            _createdObject = Instantiate(prefab);
            _createdObject.transform.position = position;
            _createdObject.transform.parent = _parent.transform;

            Undo.RegisterCreatedObjectUndo(_createdObject, "Create");
        }
    }

    private void DrawCatalog(List<GUIContent> catalogIcons, int width, int height)
    {
        _selectedElement = GUILayout.SelectionGrid(_selectedElement, catalogIcons.ToArray(), 4, GUILayout.Width(width), GUILayout.Height(height));
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

        System.IO.Directory.CreateDirectory(path);
        string[] prefabFiles = System.IO.Directory.GetFiles(path, "*.prefab");
        foreach (var prefabFile in prefabFiles)
            _catalog.Add(AssetDatabase.LoadAssetAtPath(prefabFile, typeof(GameObject)) as GameObject);
    }
}