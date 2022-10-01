// https://github.com/azixMcAze/Unity-SerializableDictionary

using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;

[CustomPropertyDrawer(typeof(SerializableDictionaryBase), true)]
public class SerializableDictionaryPropertyDrawer : PropertyDrawer
{
    private const string KeysFieldName = "keys";
    private const string ValuesFieldName = "values";
    private const float IndentWidth = 15f;

    private static readonly GUIContent IconPlus = IconContent("Toolbar Plus", "Add entry");
    private static readonly GUIContent IconMinus = IconContent("Toolbar Minus", "Remove entry");

    private static readonly GUIContent WarningIconConflict =
        IconContent("console.warnicon.sml", "Conflicting key, this entry will be lost");

    private static readonly GUIContent WarningIconOther =
        IconContent("console.infoicon.sml", "Conflicting key");

    private static readonly GUIContent WarningIconNull =
        IconContent("console.warnicon.sml", "Null key, this entry will be lost");

    private static readonly GUIStyle ButtonStyle = GUIStyle.none;
    private static readonly GUIContent tempContent = new GUIContent();

    private class ConflictState
    {
        public object conflictKey;
        public object conflictValue;
        public int conflictIndex = -1;
        public int conflictOtherIndex = -1;
        public bool conflictKeyPropertyExpanded;
        public bool conflictValuePropertyExpanded;
        public float conflictLineHeight;
    }

    private readonly struct PropertyIdentity
    {
        public readonly UnityEngine.Object instance;
        public readonly string propertyPath;

        public PropertyIdentity(SerializedProperty property)
        {
            instance = property.serializedObject.targetObject;
            propertyPath = property.propertyPath;
        }
    }

    private static readonly Dictionary<PropertyIdentity, ConflictState> ConflictStateDict =
        new Dictionary<PropertyIdentity, ConflictState>();

    private enum Action
    {
        None,
        Add,
        Remove
    }

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        label = EditorGUI.BeginProperty(position, label, property);

        var buttonAction = Action.None;
        int buttonActionIndex = 0;

        var keyArrayProperty = property.FindPropertyRelative(KeysFieldName);
        var valueArrayProperty = property.FindPropertyRelative(ValuesFieldName);

        var conflictState = GetConflictState(property);

        if (conflictState.conflictIndex != -1)
        {
            keyArrayProperty.InsertArrayElementAtIndex(conflictState.conflictIndex);
            var keyProperty = keyArrayProperty.GetArrayElementAtIndex(conflictState.conflictIndex);
            SetPropertyValue(keyProperty, conflictState.conflictKey);
            keyProperty.isExpanded = conflictState.conflictKeyPropertyExpanded;

            if (valueArrayProperty != null)
            {
                valueArrayProperty.InsertArrayElementAtIndex(conflictState.conflictIndex);
                var valueProperty = valueArrayProperty.GetArrayElementAtIndex(conflictState.conflictIndex);
                SetPropertyValue(valueProperty, conflictState.conflictValue);
                valueProperty.isExpanded = conflictState.conflictValuePropertyExpanded;
            }
        }

        var buttonWidth = ButtonStyle.CalcSize(IconPlus).x;

        var labelPosition = position;
        labelPosition.height = EditorGUIUtility.singleLineHeight;
        if (property.isExpanded)
        {
            labelPosition.xMax -= ButtonStyle.CalcSize(IconPlus).x;
        }

        EditorGUI.PropertyField(labelPosition, property, label, false);
        // property.isExpanded = EditorGUI.Foldout(labelPosition, property.isExpanded, label);
        if (property.isExpanded)
        {
            var buttonPosition = position;
            buttonPosition.xMin = buttonPosition.xMax - buttonWidth;
            buttonPosition.height = EditorGUIUtility.singleLineHeight;
            EditorGUI.BeginDisabledGroup(conflictState.conflictIndex != -1);
            if (GUI.Button(buttonPosition, IconPlus, ButtonStyle))
            {
                buttonAction = Action.Add;
                buttonActionIndex = keyArrayProperty.arraySize;
            }

            EditorGUI.EndDisabledGroup();

            EditorGUI.indentLevel++;
            var linePosition = position;
            linePosition.y += EditorGUIUtility.singleLineHeight;
            linePosition.xMax -= buttonWidth;

            foreach (var entry in EnumerateEntries(keyArrayProperty, valueArrayProperty))
            {
                var keyProperty = entry.keyProperty;
                var valueProperty = entry.valueProperty;
                int i = entry.index;

                float lineHeight = DrawKeyValueLine(keyProperty, valueProperty, linePosition, i);

                buttonPosition = linePosition;
                buttonPosition.x = linePosition.xMax;
                buttonPosition.height = EditorGUIUtility.singleLineHeight;
                if (GUI.Button(buttonPosition, IconMinus, ButtonStyle))
                {
                    buttonAction = Action.Remove;
                    buttonActionIndex = i;
                }

                if (i == conflictState.conflictIndex && conflictState.conflictOtherIndex == -1)
                {
                    var iconPosition = linePosition;
                    iconPosition.size = ButtonStyle.CalcSize(WarningIconNull);
                    GUI.Label(iconPosition, WarningIconNull);
                }
                else if (i == conflictState.conflictIndex)
                {
                    var iconPosition = linePosition;
                    iconPosition.size = ButtonStyle.CalcSize(WarningIconConflict);
                    GUI.Label(iconPosition, WarningIconConflict);
                }
                else if (i == conflictState.conflictOtherIndex)
                {
                    var iconPosition = linePosition;
                    iconPosition.size = ButtonStyle.CalcSize(WarningIconOther);
                    GUI.Label(iconPosition, WarningIconOther);
                }

                linePosition.y += lineHeight;
            }

            EditorGUI.indentLevel--;
        }

        if (buttonAction == Action.Add)
        {
            keyArrayProperty.InsertArrayElementAtIndex(buttonActionIndex);
            if (valueArrayProperty != null)
            {
                valueArrayProperty.InsertArrayElementAtIndex(buttonActionIndex);
            }
        }
        else if (buttonAction == Action.Remove)
        {
            DeleteArrayElementAtIndex(keyArrayProperty, buttonActionIndex);
            if (valueArrayProperty != null)
            {
                DeleteArrayElementAtIndex(valueArrayProperty, buttonActionIndex);
            }
        }

        conflictState.conflictKey = null;
        conflictState.conflictValue = null;
        conflictState.conflictIndex = -1;
        conflictState.conflictOtherIndex = -1;
        conflictState.conflictLineHeight = 0f;
        conflictState.conflictKeyPropertyExpanded = false;
        conflictState.conflictValuePropertyExpanded = false;

        foreach (var entry1 in EnumerateEntries(keyArrayProperty, valueArrayProperty))
        {
            var keyProperty1 = entry1.keyProperty;
            int i = entry1.index;
            var keyProperty1Value = GetPropertyValue(keyProperty1);

            if (keyProperty1Value == null)
            {
                var valueProperty1 = entry1.valueProperty;
                SaveProperty(keyProperty1, valueProperty1, i, -1, conflictState);
                DeleteArrayElementAtIndex(keyArrayProperty, i);
                if (valueArrayProperty != null)
                {
                    DeleteArrayElementAtIndex(valueArrayProperty, i);
                }

                break;
            }

            foreach (var entry2 in EnumerateEntries(keyArrayProperty, valueArrayProperty, i + 1))
            {
                var keyProperty2 = entry2.keyProperty;
                int j = entry2.index;
                var keyProperty2Value = GetPropertyValue(keyProperty2);

                if (ComparePropertyValues(keyProperty1Value, keyProperty2Value))
                {
                    var valueProperty2 = entry2.valueProperty;
                    SaveProperty(keyProperty2, valueProperty2, j, i, conflictState);
                    DeleteArrayElementAtIndex(keyArrayProperty, j);
                    if (valueArrayProperty != null)
                    {
                        DeleteArrayElementAtIndex(valueArrayProperty, j);
                    }

                    goto breakLoops;
                }
            }
        }

        breakLoops:

        EditorGUI.EndProperty();
    }

    private static float DrawKeyValueLine(SerializedProperty keyProperty, SerializedProperty valueProperty,
        Rect linePosition, int index)
    {
        bool keyCanBeExpanded = CanPropertyBeExpanded(keyProperty);

        if (valueProperty != null)
        {
            bool valueCanBeExpanded = CanPropertyBeExpanded(valueProperty);

            if (!keyCanBeExpanded && valueCanBeExpanded)
            {
                return DrawKeyValueLineExpand(keyProperty, valueProperty, linePosition);
            }
            else
            {
                var keyLabel = keyCanBeExpanded ? ("Key " + index.ToString()) : "";
                var valueLabel = valueCanBeExpanded ? ("Value " + index.ToString()) : "";
                return DrawKeyValueLineSimple(keyProperty, valueProperty, keyLabel, valueLabel, linePosition);
            }
        }
        else
        {
            if (!keyCanBeExpanded)
            {
                return DrawKeyLine(keyProperty, linePosition, null);
            }
            else
            {
                var keyLabel = $"{ObjectNames.NicifyVariableName(keyProperty.type)} {index}";
                return DrawKeyLine(keyProperty, linePosition, keyLabel);
            }
        }
    }

    private static float DrawKeyValueLineSimple(SerializedProperty keyProperty, SerializedProperty valueProperty,
        string keyLabel, string valueLabel, Rect linePosition)
    {
        float labelWidth = EditorGUIUtility.labelWidth;
        float labelWidthRelative = labelWidth / linePosition.width;

        float keyPropertyHeight = EditorGUI.GetPropertyHeight(keyProperty);
        var keyPosition = linePosition;
        keyPosition.height = keyPropertyHeight;
        keyPosition.width = labelWidth - IndentWidth;
        EditorGUIUtility.labelWidth = keyPosition.width * labelWidthRelative;
        EditorGUI.PropertyField(keyPosition, keyProperty, TempContent(keyLabel), true);

        float valuePropertyHeight = EditorGUI.GetPropertyHeight(valueProperty);
        var valuePosition = linePosition;
        valuePosition.height = valuePropertyHeight;
        valuePosition.xMin += labelWidth;
        EditorGUIUtility.labelWidth = valuePosition.width * labelWidthRelative;
        EditorGUI.indentLevel--;
        EditorGUI.PropertyField(valuePosition, valueProperty, TempContent(valueLabel), true);
        EditorGUI.indentLevel++;

        EditorGUIUtility.labelWidth = labelWidth;

        return Mathf.Max(keyPropertyHeight, valuePropertyHeight);
    }

    private static float DrawKeyValueLineExpand(SerializedProperty keyProperty, SerializedProperty valueProperty,
        Rect linePosition)
    {
        float labelWidth = EditorGUIUtility.labelWidth;

        float keyPropertyHeight = EditorGUI.GetPropertyHeight(keyProperty);
        var keyPosition = linePosition;
        keyPosition.height = keyPropertyHeight;
        keyPosition.width = labelWidth - IndentWidth;
        EditorGUI.PropertyField(keyPosition, keyProperty, GUIContent.none, true);

        float valuePropertyHeight = EditorGUI.GetPropertyHeight(valueProperty);
        var valuePosition = linePosition;
        valuePosition.height = valuePropertyHeight;
        EditorGUI.PropertyField(valuePosition, valueProperty, GUIContent.none, true);

        EditorGUIUtility.labelWidth = labelWidth;

        return Mathf.Max(keyPropertyHeight, valuePropertyHeight);
    }

    private static float DrawKeyLine(SerializedProperty keyProperty, Rect linePosition, string keyLabel)
    {
        float keyPropertyHeight = EditorGUI.GetPropertyHeight(keyProperty);
        var keyPosition = linePosition;
        keyPosition.height = keyPropertyHeight;
        keyPosition.width = linePosition.width;

        var keyLabelContent = keyLabel != null ? TempContent(keyLabel) : GUIContent.none;
        EditorGUI.PropertyField(keyPosition, keyProperty, keyLabelContent, true);

        return keyPropertyHeight;
    }

    private static bool CanPropertyBeExpanded(SerializedProperty property)
    {
        switch (property.propertyType)
        {
            case SerializedPropertyType.Generic:
            case SerializedPropertyType.Vector4:
            case SerializedPropertyType.Quaternion:
                return true;
            default:
                return false;
        }
    }

    private static void SaveProperty(SerializedProperty keyProperty, SerializedProperty valueProperty, int index,
        int otherIndex, ConflictState conflictState)
    {
        conflictState.conflictKey = GetPropertyValue(keyProperty);
        conflictState.conflictValue = valueProperty != null ? GetPropertyValue(valueProperty) : null;
        float keyPropertyHeight = EditorGUI.GetPropertyHeight(keyProperty);
        float valuePropertyHeight = valueProperty != null ? EditorGUI.GetPropertyHeight(valueProperty) : 0f;
        float lineHeight = Mathf.Max(keyPropertyHeight, valuePropertyHeight);
        conflictState.conflictLineHeight = lineHeight;
        conflictState.conflictIndex = index;
        conflictState.conflictOtherIndex = otherIndex;
        conflictState.conflictKeyPropertyExpanded = keyProperty.isExpanded;
        conflictState.conflictValuePropertyExpanded = valueProperty?.isExpanded ?? false;
    }

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        float propertyHeight = EditorGUIUtility.singleLineHeight;

        if (property.isExpanded)
        {
            var keysProperty = property.FindPropertyRelative(KeysFieldName);
            var valuesProperty = property.FindPropertyRelative(ValuesFieldName);

            foreach (var entry in EnumerateEntries(keysProperty, valuesProperty))
            {
                var keyProperty = entry.keyProperty;
                var valueProperty = entry.valueProperty;
                float keyPropertyHeight = EditorGUI.GetPropertyHeight(keyProperty);
                float valuePropertyHeight = valueProperty != null ? EditorGUI.GetPropertyHeight(valueProperty) : 0f;
                float lineHeight = Mathf.Max(keyPropertyHeight, valuePropertyHeight);
                propertyHeight += lineHeight;
            }

            var conflictState = GetConflictState(property);

            if (conflictState.conflictIndex != -1)
            {
                propertyHeight += conflictState.conflictLineHeight;
            }
        }

        return propertyHeight;
    }

    private static ConflictState GetConflictState(SerializedProperty property)
    {
        var propId = new PropertyIdentity(property);
        if (!ConflictStateDict.TryGetValue(propId, out var conflictState))
        {
            conflictState = new ConflictState();
            ConflictStateDict.Add(propId, conflictState);
        }

        return conflictState;
    }

    private static readonly Dictionary<SerializedPropertyType, PropertyInfo> SerializedPropertyValueAccessorsDict;

    static SerializableDictionaryPropertyDrawer()
    {
        var serializedPropertyValueAccessorsNameDict = new Dictionary<SerializedPropertyType, string>()
        {
            {SerializedPropertyType.Integer, "intValue"},
            {SerializedPropertyType.Boolean, "boolValue"},
            {SerializedPropertyType.Float, "floatValue"},
            {SerializedPropertyType.String, "stringValue"},
            {SerializedPropertyType.Color, "colorValue"},
            {SerializedPropertyType.ObjectReference, "objectReferenceValue"},
            {SerializedPropertyType.LayerMask, "intValue"},
            {SerializedPropertyType.Enum, "intValue"},
            {SerializedPropertyType.Vector2, "vector2Value"},
            {SerializedPropertyType.Vector3, "vector3Value"},
            {SerializedPropertyType.Vector4, "vector4Value"},
            {SerializedPropertyType.Rect, "rectValue"},
            {SerializedPropertyType.ArraySize, "intValue"},
            {SerializedPropertyType.Character, "uintValue"},
            {SerializedPropertyType.AnimationCurve, "animationCurveValue"},
            {SerializedPropertyType.Bounds, "boundsValue"},
            {SerializedPropertyType.Gradient, "gradientValue"},
            {SerializedPropertyType.Quaternion, "quaternionValue"},
            {SerializedPropertyType.ExposedReference, "exposedReferenceValue"},
            {SerializedPropertyType.FixedBufferSize, "intValue"},
            {SerializedPropertyType.Vector2Int, "vector2IntValue"},
            {SerializedPropertyType.Vector3Int, "vector3IntValue"},
            {SerializedPropertyType.RectInt, "rectIntValue"},
            {SerializedPropertyType.BoundsInt, "boundsIntValue"},
            {SerializedPropertyType.ManagedReference, "managedReferenceValue"},
        };

        SerializedPropertyValueAccessorsDict = new Dictionary<SerializedPropertyType, PropertyInfo>();
        foreach (var kvp in serializedPropertyValueAccessorsNameDict)
        {
            var propertyInfo =
                typeof(SerializedProperty).GetProperty(kvp.Value, BindingFlags.Instance | BindingFlags.Public);
            SerializedPropertyValueAccessorsDict.Add(kvp.Key, propertyInfo);
        }
    }

    private static GUIContent IconContent(string name, string tooltip)
    {
        var builtinIcon = EditorGUIUtility.IconContent(name);
        return new GUIContent(builtinIcon.image, tooltip);
    }

    private static GUIContent TempContent(string text)
    {
        tempContent.text = text;
        return tempContent;
    }

    private static void DeleteArrayElementAtIndex(SerializedProperty arrayProperty, int index)
    {
        var property = arrayProperty.GetArrayElementAtIndex(index);
        if (property.propertyType == SerializedPropertyType.ObjectReference)
        {
            property.objectReferenceValue = null;
        }

        arrayProperty.DeleteArrayElementAtIndex(index);
    }

    private static object GetPropertyValue(SerializedProperty p)
    {
        if (SerializedPropertyValueAccessorsDict.TryGetValue(p.propertyType, out var propertyInfo))
        {
            return propertyInfo.GetValue(p, null);
        }
        else
        {
            if (p.isArray)
            {
                return GetPropertyValueArray(p);
            }
            else
            {
                return GetPropertyValueGeneric(p);
            }
        }
    }

    private static void SetPropertyValue(SerializedProperty p, object v)
    {
        if (SerializedPropertyValueAccessorsDict.TryGetValue(p.propertyType, out var propertyInfo))
        {
            propertyInfo.SetValue(p, v, null);
        }
        else
        {
            if (p.isArray)
            {
                SetPropertyValueArray(p, v);
            }
            else
            {
                SetPropertyValueGeneric(p, v);
            }
        }
    }

    private static object GetPropertyValueArray(SerializedProperty property)
    {
        var array = new object[property.arraySize];
        for (int i = 0; i < property.arraySize; i++)
        {
            var item = property.GetArrayElementAtIndex(i);
            array[i] = GetPropertyValue(item);
        }

        return array;
    }

    private static object GetPropertyValueGeneric(SerializedProperty property)
    {
        var dict = new Dictionary<string, object>();
        var iterator = property.Copy();
        if (iterator.Next(true))
        {
            var end = property.GetEndProperty();
            do
            {
                string name = iterator.name;
                var value = GetPropertyValue(iterator);
                dict.Add(name, value);
            } while (iterator.Next(false) && iterator.propertyPath != end.propertyPath);
        }

        return dict;
    }

    private static void SetPropertyValueArray(SerializedProperty property, object v)
    {
        var array = (object[])v;
        property.arraySize = array.Length;
        for (int i = 0; i < property.arraySize; i++)
        {
            var item = property.GetArrayElementAtIndex(i);
            SetPropertyValue(item, array[i]);
        }
    }

    private static void SetPropertyValueGeneric(SerializedProperty property, object v)
    {
        var dict = (Dictionary<string, object>)v;
        var iterator = property.Copy();
        if (iterator.Next(true))
        {
            var end = property.GetEndProperty();
            do
            {
                string name = iterator.name;
                SetPropertyValue(iterator, dict[name]);
            } while (iterator.Next(false) && iterator.propertyPath != end.propertyPath);
        }
    }

    private static bool ComparePropertyValues(object value1, object value2)
    {
        if (value1 is Dictionary<string, object> dict1 && value2 is Dictionary<string, object> dict2)
        {
            return CompareDictionaries(dict1, dict2);
        }
        else
        {
            return Equals(value1, value2);
        }
    }

    private static bool CompareDictionaries(IReadOnlyDictionary<string, object> dict1,
        IReadOnlyDictionary<string, object> dict2)
    {
        if (dict1.Count != dict2.Count)
        {
            return false;
        }

        foreach (var kvp1 in dict1)
        {
            var key1 = kvp1.Key;
            var value1 = kvp1.Value;

            if (!dict2.TryGetValue(key1, out var value2))
            {
                return false;
            }

            if (!ComparePropertyValues(value1, value2))
            {
                return false;
            }
        }

        return true;
    }

    private readonly struct EnumerationEntry
    {
        public readonly SerializedProperty keyProperty;
        public readonly SerializedProperty valueProperty;
        public readonly int index;

        public EnumerationEntry(SerializedProperty keyProperty, SerializedProperty valueProperty, int index)
        {
            this.keyProperty = keyProperty;
            this.valueProperty = valueProperty;
            this.index = index;
        }
    }

    private static IEnumerable<EnumerationEntry> EnumerateEntries(SerializedProperty keyArrayProperty,
        SerializedProperty valueArrayProperty, int startIndex = 0)
    {
        if (keyArrayProperty.arraySize > startIndex)
        {
            int index = startIndex;
            var keyProperty = keyArrayProperty.GetArrayElementAtIndex(startIndex);
            var valueProperty = valueArrayProperty?.GetArrayElementAtIndex(startIndex);
            var endProperty = keyArrayProperty.GetEndProperty();

            do
            {
                yield return new EnumerationEntry(keyProperty, valueProperty, index);
                index++;
            } while (keyProperty.Next(false)
                     && (valueProperty?.Next(false) ?? true)
                     && !SerializedProperty.EqualContents(keyProperty, endProperty));
        }
    }
}

[CustomPropertyDrawer(typeof(SerializableDictionaryBase.Storage), true)]
public class SerializableDictionaryStoragePropertyDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        property.Next(true);
        EditorGUI.PropertyField(position, property, label, true);
    }

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        property.Next(true);
        return EditorGUI.GetPropertyHeight(property);
    }
}
