using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using System;

var scene = EditorSceneManager.OpenScene("Assets/Scenes/Game.unity");

const string spriteDir = "Assets/Sprites/Characters/FortuneTellerTemp";
const string animDir = "Assets/Animations/Characters/FortuneTellerTemp";
const string controllerPath = "Assets/Animations/Characters/FortuneTellerTemp/FortuneTellerPortrait.controller";

Func<string, string, Sprite> createDogSprite = (assetPath, assetName) =>
{
    AssetDatabase.DeleteAsset(assetPath);

    var texture = new Texture2D(256, 256, TextureFormat.RGBA32, false);
    texture.name = assetName + "_Texture";
    texture.filterMode = FilterMode.Point;

    var clear = new Color32(0, 0, 0, 0);
    var fur = new Color32(214, 170, 108, 255);
    var ear = new Color32(126, 84, 50, 255);
    var muzzle = new Color32(244, 226, 190, 255);
    var nose = new Color32(43, 33, 33, 255);
    var tongue = new Color32(214, 92, 118, 255);
    var blush = new Color32(234, 142, 162, 255);
    var white = new Color32(255, 255, 255, 255);
    var brow = new Color32(87, 55, 38, 255);

    var pixels = new Color32[256 * 256];
    for (var i = 0; i < pixels.Length; i++)
    {
        pixels[i] = clear;
    }

    Action<int, int, Color32> setPixel = (x, y, color) =>
    {
        if (x < 0 || x >= 256 || y < 0 || y >= 256)
        {
            return;
        }

        pixels[y * 256 + x] = color;
    };

    Action<int, int, int, Color32> fillCircle = (cx, cy, radius, color) =>
    {
        var sqr = radius * radius;
        for (var y = cy - radius; y <= cy + radius; y++)
        {
            for (var x = cx - radius; x <= cx + radius; x++)
            {
                var dx = x - cx;
                var dy = y - cy;
                if ((dx * dx) + (dy * dy) <= sqr)
                {
                    setPixel(x, y, color);
                }
            }
        }
    };

    Action<int, int, int, int, Color32> fillEllipse = (cx, cy, rx, ry, color) =>
    {
        var rx2 = rx * rx;
        var ry2 = ry * ry;
        for (var y = cy - ry; y <= cy + ry; y++)
        {
            for (var x = cx - rx; x <= cx + rx; x++)
            {
                var dx = x - cx;
                var dy = y - cy;
                if ((dx * dx * ry2) + (dy * dy * rx2) <= rx2 * ry2)
                {
                    setPixel(x, y, color);
                }
            }
        }
    };

    Action<int, int, int, int, Color32> fillRect = (xMin, yMin, xMax, yMax, color) =>
    {
        for (var y = yMin; y <= yMax; y++)
        {
            for (var x = xMin; x <= xMax; x++)
            {
                setPixel(x, y, color);
            }
        }
    };

    Action<int, int, int, bool, Color32> fillEar = (cx, cy, size, left, color) =>
    {
        for (var row = 0; row < size; row++)
        {
            var width = (row * 22) / size + 8;
            for (var col = 0; col < width; col++)
            {
                var x = left ? cx - col : cx + col;
                var y = cy - row;
                setPixel(x, y, color);
            }
        }
    };

    fillEar(84, 210, 72, true, ear);
    fillEar(172, 210, 72, false, ear);
    fillCircle(128, 134, 82, fur);
    fillEllipse(128, 98, 58, 46, fur);
    fillEllipse(128, 90, 44, 34, muzzle);
    fillEllipse(128, 86, 18, 11, nose);
    fillEllipse(94, 108, 16, 12, blush);
    fillEllipse(162, 108, 16, 12, blush);

    if (assetName.Contains("Joy"))
    {
        fillRect(80, 136, 108, 142, brow);
        fillRect(148, 136, 176, 142, brow);
        fillEllipse(128, 52, 28, 18, tongue);
        fillEllipse(128, 64, 36, 18, nose);
    }
    else if (assetName.Contains("Angry"))
    {
        for (var i = 0; i < 20; i++)
        {
            fillRect(77 + i, 138 - (i / 3), 88 + i, 141 - (i / 3), brow);
            fillRect(168 - i, 138 - (i / 3), 179 - i, 141 - (i / 3), brow);
        }

        fillRect(88, 128, 104, 134, white);
        fillRect(152, 128, 168, 134, white);
        fillRect(92, 130, 100, 134, nose);
        fillRect(156, 130, 164, 134, nose);
        fillRect(110, 56, 146, 60, nose);
    }
    else
    {
        fillEllipse(92, 132, 12, 8, nose);
        fillEllipse(164, 132, 12, 8, nose);
        fillRect(114, 58, 142, 62, nose);
        fillRect(112, 54, 118, 58, nose);
        fillRect(138, 54, 144, 58, nose);
    }

    texture.SetPixels32(pixels);
    texture.Apply();

    AssetDatabase.CreateAsset(texture, assetPath);
    var sprite = Sprite.Create(texture, new Rect(0f, 0f, 256f, 256f), new Vector2(0.5f, 0.15f), 100f);
    sprite.name = assetName + "_Sprite";
    AssetDatabase.AddObjectToAsset(sprite, texture);
    EditorUtility.SetDirty(texture);
    return sprite;
};

var normalSprite = createDogSprite(spriteDir + "/FortuneTellerDog_Normal.asset", "FortuneTellerDog_Normal");
var joySprite = createDogSprite(spriteDir + "/FortuneTellerDog_Joy.asset", "FortuneTellerDog_Joy");
var angrySprite = createDogSprite(spriteDir + "/FortuneTellerDog_Angry.asset", "FortuneTellerDog_Angry");

AssetDatabase.DeleteAsset(animDir + "/FortuneTellerDog_Normal.anim");
AssetDatabase.DeleteAsset(animDir + "/FortuneTellerDog_Joy.anim");
AssetDatabase.DeleteAsset(animDir + "/FortuneTellerDog_Angry.anim");
AssetDatabase.DeleteAsset(controllerPath);

Func<string, Sprite, AnimationClip> createClip = (clipPath, sprite) =>
{
    var clip = new AnimationClip();
    var binding = EditorCurveBinding.PPtrCurve(string.Empty, typeof(SpriteRenderer), "m_Sprite");
    var keys = new ObjectReferenceKeyframe[1];
    keys[0] = new ObjectReferenceKeyframe { time = 0f, value = sprite };
    AnimationUtility.SetObjectReferenceCurve(clip, binding, keys);
    AssetDatabase.CreateAsset(clip, clipPath);
    return clip;
};

var normalClip = createClip(animDir + "/FortuneTellerDog_Normal.anim", normalSprite);
var joyClip = createClip(animDir + "/FortuneTellerDog_Joy.anim", joySprite);
var angryClip = createClip(animDir + "/FortuneTellerDog_Angry.anim", angrySprite);

var controller = AnimatorController.CreateAnimatorControllerAtPath(controllerPath);
var stateMachine = controller.layers[0].stateMachine;
for (var i = stateMachine.states.Length - 1; i >= 0; i--)
{
    stateMachine.RemoveState(stateMachine.states[i].state);
}

var normalState = stateMachine.AddState("Normal");
normalState.motion = normalClip;
var joyState = stateMachine.AddState("Joy");
joyState.motion = joyClip;
var angryState = stateMachine.AddState("Angry");
angryState.motion = angryClip;
stateMachine.defaultState = normalState;

var root = GameObject.Find("CharacterPortraitsRoot");
if (root == null)
{
    root = new GameObject("CharacterPortraitsRoot");
}

var portraitGo = GameObject.Find("FortuneTellerPortrait");
if (portraitGo == null)
{
    portraitGo = new GameObject("FortuneTellerPortrait");
    portraitGo.transform.SetParent(root.transform, false);
}

portraitGo.transform.position = new Vector3(4.25f, -1.15f, 0f);
portraitGo.transform.localScale = new Vector3(2.1f, 2.1f, 1f);

var spriteRenderer = portraitGo.GetComponent<SpriteRenderer>();
if (spriteRenderer == null)
{
    spriteRenderer = portraitGo.AddComponent<SpriteRenderer>();
}
spriteRenderer.sprite = normalSprite;
spriteRenderer.sortingOrder = 10;

var animator = portraitGo.GetComponent<Animator>();
if (animator == null)
{
    animator = portraitGo.AddComponent<Animator>();
}
animator.runtimeAnimatorController = controller;

var portrait = portraitGo.GetComponent<U1W.Game.StoryCharacterPortrait>();
if (portrait == null)
{
    portrait = portraitGo.AddComponent<U1W.Game.StoryCharacterPortrait>();
}

var portraitSo = new SerializedObject(portrait);
portraitSo.FindProperty("characterId").stringValue = "fortune_teller";
portraitSo.FindProperty("spriteRenderer").objectReferenceValue = spriteRenderer;
portraitSo.FindProperty("animator").objectReferenceValue = animator;
portraitSo.FindProperty("defaultExpressionId").stringValue = U1W.Game.StoryCharacterExpressions.Normal;
portraitSo.FindProperty("playDefaultOnEnable").boolValue = true;
portraitSo.FindProperty("hideWhenExpressionMissing").boolValue = false;
var expressionsProp = portraitSo.FindProperty("expressions");
expressionsProp.arraySize = 3;
expressionsProp.GetArrayElementAtIndex(0).FindPropertyRelative("expressionId").stringValue = U1W.Game.StoryCharacterExpressions.Normal;
expressionsProp.GetArrayElementAtIndex(0).FindPropertyRelative("animatorStateName").stringValue = "Normal";
expressionsProp.GetArrayElementAtIndex(1).FindPropertyRelative("expressionId").stringValue = U1W.Game.StoryCharacterExpressions.Joy;
expressionsProp.GetArrayElementAtIndex(1).FindPropertyRelative("animatorStateName").stringValue = "Joy";
expressionsProp.GetArrayElementAtIndex(2).FindPropertyRelative("expressionId").stringValue = U1W.Game.StoryCharacterExpressions.Angry;
expressionsProp.GetArrayElementAtIndex(2).FindPropertyRelative("animatorStateName").stringValue = "Angry";
portraitSo.ApplyModifiedPropertiesWithoutUndo();

var conversationManager = GameObject.Find("ConversationPartManager").GetComponent<U1W.Game.ConversationPartManager>();
var conversationSo = new SerializedObject(conversationManager);
var portraitsProp = conversationSo.FindProperty("characterPortraits");
var exists = false;
for (var i = 0; i < portraitsProp.arraySize; i++)
{
    if (portraitsProp.GetArrayElementAtIndex(i).objectReferenceValue == portrait)
    {
        exists = true;
        break;
    }
}
if (!exists)
{
    var index = portraitsProp.arraySize;
    portraitsProp.arraySize++;
    portraitsProp.GetArrayElementAtIndex(index).objectReferenceValue = portrait;
}
conversationSo.ApplyModifiedPropertiesWithoutUndo();

EditorUtility.SetDirty(portraitGo);
EditorUtility.SetDirty(conversationManager);
EditorSceneManager.MarkSceneDirty(scene);
AssetDatabase.SaveAssets();
EditorSceneManager.SaveScene(scene);

return "Created temporary fortune teller dog portrait assets, placed GameObject, and wired ConversationPartManager.";
