using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(Player))]
public class PlayerEditor : Editor
{
     private SerializedProperty _name;
     private SerializedProperty _maxHealth;
     private SerializedProperty _health;
     private SerializedProperty _useRegeneration;
     private SerializedProperty _regenerationPerSecond;
     private SerializedProperty _damageResistance;
     private SerializedProperty _speed;
     private SerializedProperty _boosted;
     private SerializedProperty _boostSpeed;
     private SerializedProperty _friction;
     private SerializedProperty _weapon;
     private SerializedProperty _attackSpeed;

    private int _selectedTabNumber = 0;
    private string[] _tabNames = { "Health", "Movement", "Attack" };

    private void OnEnable()
    {
        _name = serializedObject.FindProperty("_name");
        _maxHealth = serializedObject.FindProperty("_maxHealth");
        _health = serializedObject.FindProperty("_health");
        _useRegeneration = serializedObject.FindProperty("_useRegeneration");
        _regenerationPerSecond = serializedObject.FindProperty("_regenerationPerSecond");
        _damageResistance = serializedObject.FindProperty("_damageResistance");
        _speed = serializedObject.FindProperty("_speed");
        _boosted = serializedObject.FindProperty("_boosted");
        _boostSpeed = serializedObject.FindProperty("_boostSpeed");
        _friction = serializedObject.FindProperty("_friction");
        _weapon = serializedObject.FindProperty("_weapon");
        _attackSpeed = serializedObject.FindProperty("_attackSpeed");
    }

    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        EditorGUILayout.PropertyField(_name);

        _selectedTabNumber = GUILayout.Toolbar(_selectedTabNumber, _tabNames);

        switch (_selectedTabNumber)
        {
            case 0:
                EditorGUILayout.LabelField("Health", _health.intValue.ToString());
                EditorGUILayout.PropertyField(_maxHealth);
                EditorGUILayout.PropertyField(_useRegeneration);

                if (_useRegeneration.boolValue)
                    EditorGUILayout.PropertyField(_regenerationPerSecond);

                EditorGUILayout.PropertyField(_damageResistance);
                break;
            case 1:
                EditorGUILayout.PropertyField(_speed);
                EditorGUILayout.PropertyField(_boosted);

                if(_boosted.boolValue)
                    EditorGUILayout.PropertyField(_boostSpeed);

                EditorGUILayout.PropertyField(_friction);
                break;
            case 2:
                EditorGUILayout.PropertyField(_attackSpeed);
                EditorGUILayout.PropertyField(_weapon);
                break;
        }

        serializedObject.ApplyModifiedProperties();
    }
}
