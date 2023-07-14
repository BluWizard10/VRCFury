using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using VF.Builder;

namespace VF.VrcHooks {
    /**
     * This adds a hook before the VRCSDK creates its clone of the GameObject.
     * This allows us to do certain behaviours on the original object before all
     * prefab connections are lost.
     */
    [InitializeOnLoad]
    public class PreInstantiateHook {
        static PreInstantiateHook() {
            try {
                PatchPreuploadMethod("RunExportAndTestAvatarBlueprint");
                PatchPreuploadMethod("RunExportAndUploadAvatarBlueprint");
            } catch (Exception e) {
                Debug.LogError(new Exception("VRCFury prefab fix patch failed", e));
            }
        }

        private static void PatchPreuploadMethod(string fieldName) {
            var sdkBuilder = ReflectionUtils.GetTypeFromAnyAssembly("VRC.SDKBase.Editor.VRC_SdkBuilder");
            if (sdkBuilder == null) throw new Exception("Failed to find SdkBuilder");
            var runField = sdkBuilder.GetField(fieldName,
                BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
            if (runField == null) throw new Exception($"Failed to find {fieldName}");
            void Fix(GameObject obj) => VRCFPrefabFixer.Fix(new VFGameObject[] { obj });
            var runObj = runField.GetValue(null);
            if (runObj is Action<GameObject> run1) {
                runField.SetValue(null, Fix + run1);
            } else if (runObj is Func<GameObject, bool> run2) {
                runField.SetValue(null, (Func<GameObject, bool>)(obj => {
                    Fix(obj);
                    return run2(obj);
                }));
            } else {
                throw new Exception($"Invalid {fieldName}");
            }
        }
    }
}
