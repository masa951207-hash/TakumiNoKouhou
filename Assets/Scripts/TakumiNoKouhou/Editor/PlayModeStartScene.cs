using UnityEditor;
using UnityEditor.SceneManagement;

namespace TakumiNoKouhou.Editor
{
    /// <summary>
    /// ▶ ボタンを押したとき、常に Title シーン（scene 0）から再生を開始させる。
    /// どのシーンを開いた状態でも Title → StageSelect → Gameplay の順で動作する。
    /// </summary>
    [InitializeOnLoad]
    public static class PlayModeStartScene
    {
        private const string TitleScenePath = "Assets/Scenes/Title.unity";

        static PlayModeStartScene()
        {
            ApplyStartScene();
            EditorApplication.projectChanged += ApplyStartScene;
        }

        [MenuItem("匠の工法/▶ 再生開始シーンを Title に設定", priority = 1)]
        public static void ApplyStartScene()
        {
            var sceneAsset = AssetDatabase.LoadAssetAtPath<SceneAsset>(TitleScenePath);
            if (sceneAsset != null)
            {
                EditorSceneManager.playModeStartScene = sceneAsset;
            }
        }
    }
}
