#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class MainMenuSceneCreator
{
    [MenuItem("Tools/Ascension/创建主菜单场景")]
    public static void CreateMainMenuScene()
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        GameObject root = new GameObject("MainMenuRoot");
        var controller = root.AddComponent<MainMenuController>();
        controller.startGameSceneName = "IntroStoryScene";

        const string path = "Assets/Scenes/MainMenu.scene";
        EditorSceneManager.SaveScene(scene, path);
        AssetDatabase.Refresh();
        Selection.activeGameObject = root;
        Debug.Log("主菜单场景已创建: Assets/Scenes/MainMenu.scene");
    }
}
#endif
