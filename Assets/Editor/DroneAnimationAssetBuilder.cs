#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using UnityEngine.UI;

namespace Gamejam2026.Editor
{
    [InitializeOnLoad]
    internal static class DroneAnimationAssetBuilder
    {
        private const string ControllerPath = "Assets/Image/Drone Anim/Drone Anim.controller";
        private const string ClipPath = "Assets/Image/Drone Anim/Drone anim.anim";
        private const string FramePathFormat = "Assets/Image/Drone Anim/dron_ani_{0}.png";
        private const string StateName = "Drone anim";

        static DroneAnimationAssetBuilder()
        {
            EditorApplication.delayCall += BuildIfNeeded;
        }

        [MenuItem("Tools/Gamejam2026/Rebuild Drone Animation")]
        private static void BuildIfNeeded()
        {
            AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(ClipPath);
            AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);

            if (clip == null || controller == null)
            {
                Debug.LogWarning("[DroneAnimationAssetBuilder] Drone animation clip or controller is missing.");
                return;
            }

            Sprite[] frames = LoadFrames();

            if (frames.Length == 0)
            {
                Debug.LogWarning("[DroneAnimationAssetBuilder] Drone animation frames are missing.");
                return;
            }

            bool changed = ConfigureClip(clip, frames);
            changed |= ConfigureController(controller, clip);

            if (!changed)
            {
                return;
            }

            EditorUtility.SetDirty(clip);
            EditorUtility.SetDirty(controller);
            AssetDatabase.SaveAssets();
            Debug.Log("[DroneAnimationAssetBuilder] Drone animation assets rebuilt.");
        }

        private static Sprite[] LoadFrames()
        {
            List<Sprite> frames = new List<Sprite>();

            for (int i = 1; i <= 5; i++)
            {
                string path = string.Format(FramePathFormat, i);
                Sprite sprite = LoadSprite(path);

                if (sprite != null)
                {
                    frames.Add(sprite);
                }
            }

            return frames.ToArray();
        }

        private static Sprite LoadSprite(string path)
        {
            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);

            if (sprite != null)
            {
                return sprite;
            }

            return AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().FirstOrDefault();
        }

        private static bool ConfigureClip(AnimationClip clip, Sprite[] frames)
        {
            EditorCurveBinding binding = new EditorCurveBinding
            {
                path = string.Empty,
                type = typeof(Image),
                propertyName = "m_Sprite"
            };

            ObjectReferenceKeyframe[] keyframes = new ObjectReferenceKeyframe[frames.Length + 1];
            float frameSeconds = 0.08f;

            for (int i = 0; i < frames.Length; i++)
            {
                keyframes[i] = new ObjectReferenceKeyframe
                {
                    time = i * frameSeconds,
                    value = frames[i]
                };
            }

            keyframes[keyframes.Length - 1] = new ObjectReferenceKeyframe
            {
                time = frames.Length * frameSeconds,
                value = frames[0]
            };

            ObjectReferenceKeyframe[] existing = AnimationUtility.GetObjectReferenceCurve(clip, binding);
            bool changed = existing == null || existing.Length != keyframes.Length;

            if (!changed)
            {
                for (int i = 0; i < existing.Length; i++)
                {
                    if (!Mathf.Approximately(existing[i].time, keyframes[i].time) || existing[i].value != keyframes[i].value)
                    {
                        changed = true;
                        break;
                    }
                }
            }

            if (changed)
            {
                AnimationUtility.SetObjectReferenceCurve(clip, binding, keyframes);
                clip.frameRate = 12f;
            }

            AnimationClipSettings settings = AnimationUtility.GetAnimationClipSettings(clip);

            if (!settings.loopTime)
            {
                settings.loopTime = true;
                AnimationUtility.SetAnimationClipSettings(clip, settings);
                changed = true;
            }

            return changed;
        }

        private static bool ConfigureController(AnimatorController controller, AnimationClip clip)
        {
            bool changed = false;

            if (controller.layers == null || controller.layers.Length == 0)
            {
                AnimatorStateMachine stateMachine = new AnimatorStateMachine
                {
                    name = "Base Layer"
                };

                AssetDatabase.AddObjectToAsset(stateMachine, controller);

                controller.layers = new[]
                {
                    new AnimatorControllerLayer
                    {
                        name = "Base Layer",
                        defaultWeight = 1f,
                        stateMachine = stateMachine
                    }
                };

                changed = true;
            }

            AnimatorStateMachine machine = controller.layers[0].stateMachine;
            AnimatorState state = machine.states
                .Select(child => child.state)
                .FirstOrDefault(candidate => candidate != null && candidate.name == StateName);

            if (state == null)
            {
                state = machine.AddState(StateName);
                changed = true;
            }

            if (state.motion != clip)
            {
                state.motion = clip;
                changed = true;
            }

            if (machine.defaultState != state)
            {
                machine.defaultState = state;
                changed = true;
            }

            return changed;
        }
    }
}
#endif
