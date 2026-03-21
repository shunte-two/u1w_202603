using System.Collections.Generic;
using U1W.Game;
using UnityEditor;
using UnityEngine;

namespace Project.Editor
{
    [CustomPropertyDrawer(typeof(StoryStep))]
    internal sealed class StoryStepDrawer : PropertyDrawer
    {
        private const float VerticalSpacing = 2f;

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);

            SerializedProperty stepTypeProperty = property.FindPropertyRelative("stepType");
            Rect foldoutRect = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);
            property.isExpanded = EditorGUI.Foldout(
                foldoutRect,
                property.isExpanded,
                BuildLabel(label, stepTypeProperty),
                true);

            if (property.isExpanded)
            {
                EditorGUI.indentLevel++;

                float currentY = foldoutRect.yMax + VerticalSpacing;
                currentY = DrawProperty(position, property, "stepType", currentY);

                foreach (string fieldName in EnumerateVisibleFieldNames(property, stepTypeProperty))
                {
                    currentY = DrawProperty(position, property, fieldName, currentY);
                }

                EditorGUI.indentLevel--;
            }

            EditorGUI.EndProperty();
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            float height = EditorGUIUtility.singleLineHeight;
            if (!property.isExpanded)
            {
                return height;
            }

            SerializedProperty stepTypeProperty = property.FindPropertyRelative("stepType");
            height += VerticalSpacing + GetChildPropertyHeight(property, "stepType");

            foreach (string fieldName in EnumerateVisibleFieldNames(property, stepTypeProperty))
            {
                height += VerticalSpacing + GetChildPropertyHeight(property, fieldName);
            }

            return height;
        }

        private static GUIContent BuildLabel(GUIContent label, SerializedProperty stepTypeProperty)
        {
            string labelText = label.text;
            if (stepTypeProperty == null)
            {
                return new GUIContent(labelText);
            }

            string typeName = stepTypeProperty.enumDisplayNames[stepTypeProperty.enumValueIndex];
            return new GUIContent($"{labelText} ({typeName})");
        }

        private static float DrawProperty(
            Rect totalRect,
            SerializedProperty rootProperty,
            string childPropertyName,
            float currentY)
        {
            SerializedProperty childProperty = rootProperty.FindPropertyRelative(childPropertyName);
            if (childProperty == null)
            {
                return currentY;
            }

            float height = EditorGUI.GetPropertyHeight(childProperty, true);
            Rect rect = new Rect(totalRect.x, currentY, totalRect.width, height);
            EditorGUI.PropertyField(rect, childProperty, true);
            return rect.yMax + VerticalSpacing;
        }

        private static float GetChildPropertyHeight(SerializedProperty rootProperty, string childPropertyName)
        {
            SerializedProperty childProperty = rootProperty.FindPropertyRelative(childPropertyName);
            return childProperty != null
                ? EditorGUI.GetPropertyHeight(childProperty, true)
                : 0f;
        }

        private static IEnumerable<string> EnumerateVisibleFieldNames(
            SerializedProperty rootProperty,
            SerializedProperty stepTypeProperty)
        {
            StoryStepType stepType = stepTypeProperty != null
                ? (StoryStepType)stepTypeProperty.enumValueIndex
                : StoryStepType.ShowMessage;

            switch (stepType)
            {
                case StoryStepType.ShowMessage:
                    yield return "message";
                    yield return "messageFallback";
                    yield return "waitForAdvance";
                    SerializedProperty waitForAdvanceProperty = rootProperty.FindPropertyRelative("waitForAdvance");
                    if (waitForAdvanceProperty != null && !waitForAdvanceProperty.boolValue)
                    {
                        yield return "waitSeconds";
                    }

                    break;

                case StoryStepType.ChangeExpression:
                    yield return "characterId";
                    yield return "expressionId";
                    yield return "hideCharacterWhenExpressionMissing";
                    break;

                case StoryStepType.Wait:
                    yield return "waitSeconds";
                    yield return "showConversationWindowDuringWait";
                    break;

                case StoryStepType.ShowTitleSprite:
                    yield return "titleSprite";
                    yield return "waitSeconds";
                    break;

                case StoryStepType.PlayBgm:
                    yield return "audioKey";
                    yield return "audioVolume";
                    yield return "loopBgm";
                    break;

                case StoryStepType.StopBgm:
                    yield return "audioFadeSeconds";
                    break;

                case StoryStepType.PlaySe:
                    yield return "audioKey";
                    yield return "audioVolume";
                    break;
            }
        }
    }
}
