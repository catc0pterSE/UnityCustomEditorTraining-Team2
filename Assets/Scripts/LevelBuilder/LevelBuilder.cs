using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using UnityEditor;
using UnityEngine;

namespace LevelBuilder
{
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
        private const float _half = 0.5f;

        private readonly Vector2 _iconDimensions = new Vector2(100, 100);

        private Dictionary<string, List<GameObject>> _catalogs = new Dictionary<string, List<GameObject>>();

        private Dictionary<List<GameObject>, List<GUIContent>> _iconCatalogs =
            new Dictionary<List<GameObject>, List<GUIContent>>();

        private List<GameObject> _currentCatalog = new List<GameObject>();
        private Vector2 _scrollPosition;
        private int _selectedElement;
        private bool _building;
        private bool _collisionsAllowed;
        private bool _leapToSurface;
        private float _rotationSpeed = 2;
        private float _scaleSpeed = 1.3f;
        private LayerMask _propLayerMask;
        private string[] _layerNames;
        private float _verticalMovementSpeed = 20f;
        private int _selectedTabNumber;
        private GameObject _createdObject = null;
        private GameObject _parent;
        private float _lastYPosition;
        private string[] _tabNames =
            { "Buildings", "Plants", "Props", "Rocks", "Skeletons", "ShipWreck", "Vehicles", "Other" };

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
            
            if (_createdObject!=null)
                DestroyImmediate(_createdObject);

            _selectedTabNumber = GUILayout.Toolbar(_selectedTabNumber, _tabNames, GUILayout.Height(60));

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
                    DrawGrid(_pathVehicles);
                    break;
                case 7:
                    DrawGrid(_pathOther);
                    break;
            }

            EditorGUILayout.HelpBox(
                "To rotate the object in Y axis, use the Q and E buttons. Q counterclockwise and E clockwise" +
                "\nTo upscale object use W, to downscale use S" +
                "\nUse A to lean left, D to lean right, R to lean forward, F to lean backward" +
                "\nUse T to move object up, G to move object down",
                MessageType.Info);
        }

        private void DrawGrid(string assetPath)
        {
            RefreshCurrentCatalog(assetPath);
            _building = GUILayout.Toggle(_building, "Start building", "Button", GUILayout.Height(40));
            EditorGUILayout.BeginHorizontal();
            _collisionsAllowed =
                GUILayout.Toggle(_collisionsAllowed, "Collisions Allowed", "Button", GUILayout.Height(30));
            _leapToSurface = GUILayout.Toggle(_leapToSurface, "Leap to Surface", "Button", GUILayout.Height(30));
            EditorGUILayout.EndHorizontal();
            GUILayout.Label("RotationSpeed");
            _rotationSpeed = GUILayout.HorizontalSlider(_rotationSpeed, 0, 40, GUILayout.Height(20));
            GUILayout.Label("ScaleSpeed");
            _scaleSpeed = GUILayout.HorizontalSlider(_scaleSpeed, 0, 10, GUILayout.Height(20));
            GUILayout.Label("VerticalMovementSpeed");
            _verticalMovementSpeed = GUILayout.HorizontalSlider(_verticalMovementSpeed, 0, 20, GUILayout.Height(20));
            GUILayout.BeginHorizontal();
            GUILayout.Label("PropLayerMask", GUILayout.Width(150));
            _propLayerMask = EditorGUILayout.LayerField(_propLayerMask, GUILayout.Width(200));
            GUILayout.EndHorizontal();
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
            if (_leapToSurface==false)
            {
                _lastYPosition = GetYPosition(_lastYPosition);
                contactPoint.y = _lastYPosition;
                _createdObject.transform.position = contactPoint;
            }
            else
            {
                _createdObject.transform.position = contactPoint;
            }

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
                if (_collisionsAllowed == false)
                {
                    Bounds bounds = GetCreatedObjectBounds();
                
                    if (Physics.OverlapBox
                        (
                            bounds.center,
                            bounds.size * _half,
                            _createdObject.transform.rotation,
                            _propLayerMask
                        ).Length > 0)
                        return;
                }

                _createdObject.layer = _propLayerMask;
                _building = false;
                _createdObject = null;
            }
        }

        private bool CheckKeyboardInput()
        {
            return Event.current.type == EventType.KeyDown;
        }

        private float GetYPosition(float currentYPosition)
        {
            if (CheckKeyboardInput() == false)
                return currentYPosition;

            float newYPOsition = currentYPosition;

            if (Event.current.keyCode == KeyCode.T)
            {
                newYPOsition += _verticalMovementSpeed;
            }

            if (Event.current.keyCode == KeyCode.G)
            {
                newYPOsition -= _verticalMovementSpeed;
            }

            return newYPOsition;
        }

        private bool CheckScaleInput(out Vector3 newScale)
        {
            newScale = _createdObject.transform.localScale;

            if (CheckKeyboardInput() == false)
                return false;

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

            return false;
        }

        private bool CheckRotationInput(out Vector3 rotation)
        {
            rotation = _createdObject.transform.rotation.eulerAngles;

            if (CheckKeyboardInput() == false)
                return false;

            switch (Event.current.keyCode)
            {
                case KeyCode.Q:
                    rotation.y -= _rotationSpeed;
                    return true;
                case KeyCode.E:
                    rotation.y += _rotationSpeed;
                    return true;
                case KeyCode.A:
                    rotation.z -= _rotationSpeed;
                    return true;
                case KeyCode.D:
                    rotation.z += _rotationSpeed;
                    return true;
                case KeyCode.R:
                    rotation.x += _rotationSpeed;
                    return true;
                case KeyCode.F:
                    rotation.x -= _rotationSpeed;
                    return true;
            }

            return false;
        }

        private bool CheckPlacementInput()
        {
            HandleUtility.AddDefaultControl(0);
            return Event.current.type == EventType.MouseDown && Event.current.button == 0;
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

        private void CreateObject()
        {
            if (_selectedElement < _currentCatalog.Count == false)
                return;

            GameObject prefab = _currentCatalog[_selectedElement];
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
            if (_iconCatalogs.ContainsKey(_currentCatalog) == false)
            {
                List<GUIContent> catalogIcons = new List<GUIContent>();

                foreach (var element in _currentCatalog)
                {
                    Texture2D texture = null;

                    while (texture == null)
                    {
                        texture = AssetPreview.GetAssetPreview(element);
                    }

                    Texture2D cloneTexture = new Texture2D(texture.width, texture.height);
                    cloneTexture.SetPixels(texture.GetPixels());
                    cloneTexture.Apply();

                    catalogIcons.Add(new GUIContent(cloneTexture));
                }

                _iconCatalogs.Add(_currentCatalog, catalogIcons);
            }

            return _iconCatalogs[_currentCatalog];
        }

        private void RefreshCurrentCatalog(string path)
        {
            if (_catalogs.ContainsKey(path) == false)
            {
                Directory.CreateDirectory(path);

                string[] prefabFiles = Directory.GetFiles(path, "*.prefab");
                List<GameObject> catalog = new List<GameObject>();

                foreach (var prefabFile in prefabFiles)
                    catalog.Add(AssetDatabase.LoadAssetAtPath(prefabFile, typeof(GameObject)) as GameObject);

                _catalogs.Add(path, catalog);
            }

            if (_catalogs[path].Count != Directory.GetFiles(path, "*.prefab").Length)
                Close();

            _currentCatalog = _catalogs[path];
        }
    }
}