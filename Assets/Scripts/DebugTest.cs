using UnityEngine;

/// <summary>
/// シーンにスクリプトが存在するか確認するためのテストスクリプト
/// </summary>
public class DebugTest : MonoBehaviour
{
    private void Start()
    {
        Debug.Log("=== DebugTest: Start()が呼ばれました ===");
        Debug.Log($"DebugTest: GameObject名 = {gameObject.name}");
        Debug.Log($"DebugTest: アクティブ = {gameObject.activeSelf}");
        Debug.Log($"DebugTest: コンポーネント有効 = {enabled}");
        
        // RedBoxControllerを検索
        RedBoxController redBoxController = FindObjectOfType<RedBoxController>();
        if (redBoxController != null)
        {
            Debug.Log($"DebugTest: RedBoxControllerが見つかりました。GameObject名 = {redBoxController.gameObject.name}, 有効 = {redBoxController.enabled}");
        }
        else
        {
            Debug.LogWarning("DebugTest: RedBoxControllerが見つかりませんでした。");
        }
        
        // MenuInputHandlerを検索
        MenuInputHandler menuInputHandler = FindObjectOfType<MenuInputHandler>();
        if (menuInputHandler != null)
        {
            Debug.Log($"DebugTest: MenuInputHandlerが見つかりました。GameObject名 = {menuInputHandler.gameObject.name}, 有効 = {menuInputHandler.enabled}");
        }
        else
        {
            Debug.LogWarning("DebugTest: MenuInputHandlerが見つかりませんでした。");
        }
        
        // MenuManagerを検索
        MenuManager menuManager = FindObjectOfType<MenuManager>();
        if (menuManager != null)
        {
            Debug.Log($"DebugTest: MenuManagerが見つかりました。GameObject名 = {menuManager.gameObject.name}, 有効 = {menuManager.enabled}");
        }
        else
        {
            Debug.LogWarning("DebugTest: MenuManagerが見つかりませんでした。");
        }
        
        // Playerを検索
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            Debug.Log($"DebugTest: Playerが見つかりました。GameObject名 = {player.name}");
            
            // ThirdPersonControllerを検索
            MonoBehaviour[] allMonoBehaviours = player.GetComponents<MonoBehaviour>();
            foreach (var mb in allMonoBehaviours)
            {
                if (mb.GetType().Name == "ThirdPersonController")
                {
                    Debug.Log($"DebugTest: ThirdPersonControllerが見つかりました。有効 = {mb.enabled}");
                    break;
                }
            }
        }
        else
        {
            Debug.LogWarning("DebugTest: Playerが見つかりませんでした。");
        }
        
        Debug.Log($"DebugTest: Time.timeScale = {Time.timeScale}");
        Debug.Log("=== DebugTest: Start()終了 ===");
    }
    
    private void Update()
    {
        if (Time.frameCount == 1)
        {
            Debug.Log("=== DebugTest: Update()が呼ばれました ===");
            Debug.Log($"DebugTest: Time.timeScale = {Time.timeScale}");
        }
    }
}



