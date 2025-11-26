using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

/// <summary>
/// Escapeキーを監視してMenuManagerを制御するスクリプト
/// MenuManagerコンポーネント自体は無効化しておき、このスクリプトから制御する
/// </summary>
public class MenuInputHandler : MonoBehaviour
{
    private MenuManager menuManager;
    private bool wasEscapePressed = false;

    private void Start()
    {
        // MenuManagerを検索
        menuManager = FindObjectOfType<MenuManager>();
        
        if (menuManager == null)
        {
            Debug.LogWarning("MenuInputHandler: MenuManagerが見つかりません。MenuManagerをシーンに追加してください。");
            enabled = false; // このコンポーネントを無効化
        }
        else
        {
            Debug.Log("MenuInputHandler: MenuManagerを見つけました。Escapeキーでメニューを開閉できます。");
        }
    }

    private void Update()
    {
        // MenuManagerが見つからない場合は何もしない
        if (menuManager == null)
        {
            return;
        }

        // Escapeキーでメニューの開閉（Input Systemを使用）
#if ENABLE_INPUT_SYSTEM
        if (Keyboard.current != null)
        {
            bool isEscapePressed = Keyboard.current.escapeKey.isPressed;
            if (isEscapePressed && !wasEscapePressed)
            {
                Debug.Log("MenuInputHandler: Escapeキーが押されました。");
                menuManager.ToggleMenu();
            }
            wasEscapePressed = isEscapePressed;
        }
#else
        // 旧Input Manager用（Input Systemが無効な場合）
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Debug.Log("MenuInputHandler: Escapeキーが押されました。");
            menuManager.ToggleMenu();
        }
#endif
    }
}

