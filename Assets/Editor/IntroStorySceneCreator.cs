#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class IntroStorySceneCreator
{
    [MenuItem("Tools/Ascension/创建开场剧情场景")]
    public static void CreateIntroStoryScene()
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        GameObject root = new GameObject("IntroStoryRoot");
        var controller = root.AddComponent<IntroStoryController>();
        controller.battleSceneName = "SampleScene";

        const string path = "Assets/Scenes/IntroStoryScene.scene";
        EditorSceneManager.SaveScene(scene, path);
        AssetDatabase.Refresh();
        Selection.activeGameObject = root;
        Debug.Log("开场剧情场景已创建: Assets/Scenes/IntroStoryScene.scene");
    }
}
#endif
