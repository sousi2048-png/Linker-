using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// ゲーム全体の状態を管理するスクリプト
/// </summary>
public class GameManager : MonoBehaviour
{
    private static GameManager instance;
    public static GameManager Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindObjectOfType<GameManager>();
                if (instance == null)
                {
                    GameObject go = new GameObject("GameManager");
                    instance = go.AddComponent<GameManager>();
                }
            }
            return instance;
        }
    }

    private bool isGameCleared = false;
    private GameObject winCanvas;
    private Text winText;

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else if (instance != this)
        {
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// ゲームクリアを実行
    /// </summary>
    public void GameClear()
    {
        if (isGameCleared)
        {
            return; // 既にクリア済み
        }

        isGameCleared = true;
        Debug.Log("GameManager: ゲームクリア！");

        // 「You Win」UIを表示
        ShowWinUI();
    }

    /// <summary>
    /// 「You Win」UIを表示
    /// </summary>
    private void ShowWinUI()
    {
        // Canvasが既に存在する場合は破棄
        if (winCanvas != null)
        {
            Destroy(winCanvas);
        }

        // Canvasを作成
        winCanvas = new GameObject("WinCanvas");
        Canvas canvas = winCanvas.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 200; // メニューより上に表示

        // CanvasScalerを追加
        CanvasScaler scaler = winCanvas.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;

        // GraphicRaycasterを追加
        winCanvas.AddComponent<GraphicRaycaster>();

        // テキストオブジェクトを作成
        GameObject textObj = new GameObject("WinText");
        textObj.transform.SetParent(winCanvas.transform, false);

        RectTransform textRect = textObj.AddComponent<RectTransform>();
        textRect.anchorMin = new Vector2(0.5f, 0.5f);
        textRect.anchorMax = new Vector2(0.5f, 0.5f);
        textRect.pivot = new Vector2(0.5f, 0.5f);
        textRect.anchoredPosition = Vector2.zero;
        textRect.sizeDelta = new Vector2(800f, 200f);

        // Textコンポーネントを追加
        winText = textObj.AddComponent<Text>();
        winText.text = "You Win";
        // フォントを設定（デフォルトフォントを使用）
        winText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (winText.font == null)
        {
            // フォントが見つからない場合は、デフォルトフォントを作成
            winText.font = Font.CreateDynamicFontFromOSFont("Arial", 120);
        }
        winText.fontSize = 120;
        winText.color = Color.yellow;
        winText.alignment = TextAnchor.MiddleCenter;
        winText.fontStyle = FontStyle.Bold;

        // アウトライン効果を追加（見やすくするため）
        Outline outline = textObj.AddComponent<Outline>();
        outline.effectColor = Color.black;
        outline.effectDistance = new Vector2(4f, 4f);
    }

    /// <summary>
    /// ゲームクリア状態を取得
    /// </summary>
    public bool IsGameCleared()
    {
        return isGameCleared;
    }
}
