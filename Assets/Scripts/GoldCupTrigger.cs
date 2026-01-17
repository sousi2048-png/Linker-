using UnityEngine;

/// <summary>
/// Gold Cupに触れた際にゲームクリアを実行するスクリプト
/// </summary>
[RequireComponent(typeof(Collider))]
public class GoldCupTrigger : MonoBehaviour
{
    private GameManager gameManager;
    private bool hasTriggered = false; // 既にトリガー済みかどうか

    private void Start()
    {
        gameManager = GameManager.Instance;
        
        if (gameManager == null)
        {
            Debug.LogWarning($"GoldCupTrigger ({gameObject.name}): GameManagerが見つかりません。ゲームクリアが機能しない可能性があります。");
        }

        // Colliderが存在し、Triggerに設定されていることを確認
        Collider collider = GetComponent<Collider>();
        if (collider == null)
        {
            Debug.LogError($"GoldCupTrigger ({gameObject.name}): Colliderが見つかりません。");
        }
        else if (!collider.isTrigger)
        {
            Debug.LogWarning($"GoldCupTrigger ({gameObject.name}): ColliderがTriggerに設定されていません。Player検出が機能しない可能性があります。");
        }
    }

    /// <summary>
    /// PlayerがGold Cupに触れた時に呼ばれる
    /// </summary>
    private void OnTriggerEnter(Collider other)
    {
        // Playerタグを持つオブジェクトのみを対象とする
        if (other.CompareTag("Player"))
        {
            if (!hasTriggered && gameManager != null)
            {
                hasTriggered = true;
                Debug.Log($"GoldCupTrigger: PlayerがGold Cupに触れました。ゲームクリア！");
                gameManager.GameClear();
            }
        }
    }
}
