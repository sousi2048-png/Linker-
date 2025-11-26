using UnityEngine;

/// <summary>
/// RedBoxに触れた際にメニューに登録するスクリプト
/// </summary>
[RequireComponent(typeof(Collider))]
public class RedBoxRegister : MonoBehaviour
{
    private MenuManager menuManager;
    private bool isRegistered = false; // 既に登録済みかどうか

    private void Start()
    {
        menuManager = FindObjectOfType<MenuManager>();
        
        if (menuManager == null)
        {
            Debug.LogWarning($"RedBoxRegister ({gameObject.name}): MenuManagerが見つかりません。メニューに登録できません。");
        }

        // Colliderが存在し、Triggerに設定されていることを確認
        Collider collider = GetComponent<Collider>();
        if (collider == null)
        {
            Debug.LogError($"RedBoxRegister ({gameObject.name}): Colliderが見つかりません。");
        }
        else if (!collider.isTrigger)
        {
            Debug.LogWarning($"RedBoxRegister ({gameObject.name}): ColliderがTriggerに設定されていません。Player検出が機能しない可能性があります。");
        }
    }

    /// <summary>
    /// PlayerがRedBoxに触れた時に呼ばれる
    /// </summary>
    private void OnTriggerEnter(Collider other)
    {
        // Playerタグを持つオブジェクトのみを対象とする
        if (other.CompareTag("Player"))
        {
            if (!isRegistered && menuManager != null)
            {
                RegisterToMenu();
            }
        }
    }

    /// <summary>
    /// メニューに登録する
    /// </summary>
    private void RegisterToMenu()
    {
        isRegistered = true;
        
        // 親オブジェクト（RedBox）の名前を取得
        GameObject redBox = transform.parent != null ? transform.parent.gameObject : gameObject;
        string redBoxName = redBox.name;
        
        Debug.Log($"RedBoxRegister: {redBoxName}をメニューに登録しました。");
        
        // MenuManagerにノードを追加（RedBoxは消さない）
        menuManager.AddNodeToMenu(redBoxName);
    }

    /// <summary>
    /// 登録状態をリセット（テスト用）
    /// </summary>
    public void ResetRegistration()
    {
        isRegistered = false;
    }
}

