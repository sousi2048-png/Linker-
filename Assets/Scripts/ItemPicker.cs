using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

/// <summary>
/// マウスカーソルでオブジェクトを検出し、Eキーでメニューにノードを登録するスクリプト
/// </summary>
public class ItemPicker : MonoBehaviour
{
    [Header("Registration Settings")]
    [SerializeField] private float registrationDistance = 100f; // 登録できる距離（デフォルトを100に変更）
    [SerializeField] private LayerMask registrationLayer = -1; // 登録できるレイヤー（-1はすべて）
    
    private Camera mainCamera;
    private MenuManager menuManager;
    private GameObject lastDetectedObject = null;

    private void Start()
    {
        mainCamera = Camera.main;
        menuManager = FindObjectOfType<MenuManager>();
        
        if (mainCamera == null)
        {
            Debug.LogError("ItemPicker: Main Cameraが見つかりません。");
            enabled = false;
        }
        
        if (menuManager == null)
        {
            Debug.LogWarning("ItemPicker: MenuManagerが見つかりません。メニューにノードを登録できません。");
        }
    }

    private void Update()
    {
        // Raycastでマウス位置のオブジェクトを検出
        DetectObjectUnderMouse();
        
        // Eキーでアイテムを登録する（Interactアクション）
        CheckRegistrationInput();
    }

    private void DetectObjectUnderMouse()
    {
        // マウス位置を取得（Input System対応）
        Vector2 mousePosition;
#if ENABLE_INPUT_SYSTEM
        if (Mouse.current != null)
        {
            mousePosition = Mouse.current.position.ReadValue();
        }
        else
        {
            return; // マウスが利用できない
        }
#else
        mousePosition = Input.mousePosition;
#endif

        // マウス位置からRaycast
        Ray ray = mainCamera.ScreenPointToRay(mousePosition);
        RaycastHit hit;
        
        if (Physics.Raycast(ray, out hit, registrationDistance, registrationLayer))
        {
            GameObject hitObject = hit.collider.gameObject;
            
            // RedBoxのみを対象とする
            if (hitObject.name.StartsWith("RedBox"))
            {
                // 新しいオブジェクトを検出した場合
                if (hitObject != lastDetectedObject)
                {
                    lastDetectedObject = hitObject;
                    Debug.Log($"ItemPicker: RedBoxを検出しました: {hitObject.name}");
                }
            }
            else
            {
                if (lastDetectedObject != null)
                {
                    Debug.Log($"ItemPicker: RedBox以外のオブジェクトを検出しました: {hitObject.name}");
                }
                lastDetectedObject = null;
            }
        }
        else
        {
            // Raycastが何も当たらない場合（デバッグ用にコメントアウト、必要に応じて有効化）
            // Debug.Log("ItemPicker: Raycastが何も当たりませんでした。");
            lastDetectedObject = null;
        }
    }

    private void CheckRegistrationInput()
    {
        // Eキー（Interact）の入力検出
#if ENABLE_INPUT_SYSTEM
        if (Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame)
        {
            Debug.Log($"ItemPicker: Eキーが押されました。lastDetectedObject: {(lastDetectedObject != null ? lastDetectedObject.name : "null")}, menuManager: {(menuManager != null ? "存在" : "null")}");
            if (lastDetectedObject != null && menuManager != null)
            {
                RegisterItem(lastDetectedObject);
            }
            else
            {
                if (lastDetectedObject == null)
                {
                    Debug.LogWarning("ItemPicker: 検出されたオブジェクトがありません。RedBoxにマウスカーソルを合わせてください。");
                }
                if (menuManager == null)
                {
                    Debug.LogWarning("ItemPicker: MenuManagerが見つかりません。");
                }
            }
        }
#else
        if (Input.GetKeyDown(KeyCode.E))
        {
            Debug.Log($"ItemPicker: Eキーが押されました。lastDetectedObject: {(lastDetectedObject != null ? lastDetectedObject.name : "null")}, menuManager: {(menuManager != null ? "存在" : "null")}");
            if (lastDetectedObject != null && menuManager != null)
            {
                RegisterItem(lastDetectedObject);
            }
            else
            {
                if (lastDetectedObject == null)
                {
                    Debug.LogWarning("ItemPicker: 検出されたオブジェクトがありません。RedBoxにマウスカーソルを合わせてください。");
                }
                if (menuManager == null)
                {
                    Debug.LogWarning("ItemPicker: MenuManagerが見つかりません。");
                }
            }
        }
#endif
    }

    /// <summary>
    /// RedBoxをメニューに登録する（RedBoxは消えない）
    /// </summary>
    private void RegisterItem(GameObject item)
    {
        Debug.Log($"ItemPicker: {item.name}をメニューに登録しました。");
        
        // MenuManagerにノードを追加（RedBoxは消さない）
        menuManager.AddNodeToMenu(item.name);
    }

    /// <summary>
    /// 現在検出されているオブジェクトを取得
    /// </summary>
    public GameObject GetDetectedObject()
    {
        return lastDetectedObject;
    }
}

