using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

// 自動追加を無効化（手動追加のみ）
public static class MenuManagerSetup
{
    // 自動追加を無効化しました
    // メニューから手動で追加してください

    static void SetupMenuManager()
    {
        // プレイモード中は実行しない
        if (EditorApplication.isPlaying)
        {
            return;
        }

        // シーン内にMenuManagerが既に存在するか確認
        MenuManager existingManager = Object.FindObjectOfType<MenuManager>();
        if (existingManager != null)
        {
            Debug.Log("MenuManagerは既にシーンに存在します。");
            // MenuInputHandlerも確認
            MenuInputHandler existingHandler = Object.FindObjectOfType<MenuInputHandler>();
            if (existingHandler == null)
            {
                GameObject handlerObjForExisting = new GameObject("MenuInputHandler");
                handlerObjForExisting.AddComponent<MenuInputHandler>();
                EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
                Debug.Log("MenuInputHandlerを追加しました。");
            }
            else
            {
                Debug.Log("MenuInputHandlerは既にシーンに存在します。");
            }
            
            // RedBoxControllerも確認
            RedBoxController existingController = Object.FindObjectOfType<RedBoxController>();
            if (existingController == null)
            {
                GameObject controllerObjForExisting = new GameObject("RedBoxController");
                controllerObjForExisting.AddComponent<RedBoxController>();
                EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
                Debug.Log("RedBoxControllerを追加しました。");
            }
            else
            {
                Debug.Log("RedBoxControllerは既にシーンに存在します。");
            }
            
            // ItemPickerは不要になりました（RedBoxRegisterを使用）
            return;
        }

        // MenuManagerを追加するGameObjectを作成
        GameObject menuManagerObj = new GameObject("MenuManager");
        MenuManager menuManager = menuManagerObj.AddComponent<MenuManager>();
        menuManagerObj.SetActive(true); // GameObjectは有効化

        // MenuInputHandlerを追加するGameObjectを作成
        GameObject handlerObjForNew = new GameObject("MenuInputHandler");
        handlerObjForNew.AddComponent<MenuInputHandler>();

        // RedBoxControllerを追加するGameObjectを作成
        GameObject controllerObj = new GameObject("RedBoxController");
        controllerObj.AddComponent<RedBoxController>();

        // ItemPickerは不要になりました（RedBoxRegisterを使用）

        // シーンに変更をマーク
        if (!EditorApplication.isPlaying)
        {
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        }

        Debug.Log("MenuManager、MenuInputHandler、RedBoxControllerをシーンに追加しました。Escapeキーでメニューを開閉できます。");
    }

    [MenuItem("Tools/Setup Menu Manager", false, 1)]
    static void SetupManually()
    {
        // プレイモード中は実行できない
        if (EditorApplication.isPlaying)
        {
            EditorUtility.DisplayDialog("エラー", "この操作はプレイモード中には実行できません。", "OK");
            return;
        }
        SetupMenuManager();
    }

    // メニューが見つからない場合の代替方法：GameObjectメニューから追加
    [MenuItem("GameObject/UI/Menu Manager", false, 10)]
    static void CreateMenuManagerFromGameObject()
    {
        // プレイモード中は実行できない
        if (EditorApplication.isPlaying)
        {
            EditorUtility.DisplayDialog("エラー", "この操作はプレイモード中には実行できません。", "OK");
            return;
        }
        SetupMenuManager();
    }
}

