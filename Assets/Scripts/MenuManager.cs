using UnityEngine;

public class MenuManager : MonoBehaviour
{
    [Header("Menu Settings")]
    private GameObject menuPanel;
    private GameObject menuCanvas;
    private GameObject nodeContainer; // ノードを配置するコンテナ
    private bool isMenuOpen = false;
    private System.Collections.Generic.List<string> collectedItems = new System.Collections.Generic.List<string>(); // 収集したアイテムのリスト
    private System.Collections.Generic.Dictionary<string, RectTransform> nodeTransforms = new System.Collections.Generic.Dictionary<string, RectTransform>(); // ノード名とRectTransformのマッピング
    private bool hasInitializedPlayerOperableMerge = false; // ゲーム開始時のPlayerとOperableの合体を初期化したかどうか

    // このコンポーネントは外部から呼び出されることを想定
    // Update()は使用しない（Player操作に干渉しないように）

    private void Start()
    {
        // ゲーム開始時に時間を正常に動かす（メニューが開いていない場合）
        Time.timeScale = 1f;
        
        // RedBoxControllerの存在を確認
        RedBoxController redBoxController = FindObjectOfType<RedBoxController>();
        if (redBoxController == null)
        {
            Debug.LogWarning("MenuManager: RedBoxControllerがシーンに見つかりません。Tools > Setup Menu Manager から追加してください。");
        }
        else
        {
            Debug.Log($"MenuManager: RedBoxControllerが見つかりました: {redBoxController.gameObject.name}");
        }
        
        // ゲーム開始時に「Player」ノードを追加
        if (!collectedItems.Contains("Player"))
        {
            collectedItems.Add("Player");
            Debug.Log("MenuManager: ゲーム開始時に「Player」ノードを追加しました。");
        }
        
        // ゲーム開始時に「Operable」ノードを追加
        if (!collectedItems.Contains("Operable"))
        {
            collectedItems.Add("Operable");
            Debug.Log("MenuManager: ゲーム開始時に「Operable」ノードを追加しました。");
        }
    }

    public void OpenMenu()
    {
        if (isMenuOpen)
        {
            return;
        }

        isMenuOpen = true;

        // メニューUIを作成（既に存在する場合は再作成しない）
        CreateMenuUI();
        if (menuCanvas != null)
        {
            menuCanvas.SetActive(true);
        }
        if (menuPanel != null)
        {
            menuPanel.SetActive(true);
        }
        
        // 既に収集したアイテムのノードを作成（既存のノードは保持）
        RefreshMenuNodes();
        
        Time.timeScale = 0f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void CloseMenu()
    {
        if (!isMenuOpen)
        {
            return;
        }

        isMenuOpen = false;

        // メニューUIを非表示にする（ノードの位置を保持するため破棄しない）
        if (menuCanvas != null)
        {
            menuCanvas.SetActive(false);
        }
        
        Time.timeScale = 1f;
        // カーソルロックは既存のシステム（StarterAssetsInputs）に任せる
    }

    public void ToggleMenu()
    {
        Debug.Log($"MenuManager: ToggleMenu()が呼ばれました。現在の状態: isMenuOpen = {isMenuOpen}");
        if (isMenuOpen)
        {
            CloseMenu();
        }
        else
        {
            OpenMenu();
        }
    }

    private void CreateMenuUI()
    {
        // メニューが開かれた時だけCanvasを作成（Player操作に干渉しないように）
        if (menuCanvas != null)
        {
            return; // 既に作成済み
        }

        // Canvasを作成
        menuCanvas = new GameObject("MenuCanvas");
        Canvas canvas = menuCanvas.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;

        // CanvasScalerを追加
        UnityEngine.UI.CanvasScaler scaler = menuCanvas.AddComponent<UnityEngine.UI.CanvasScaler>();
        scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;

        // GraphicRaycasterを追加（メニューが開いている時だけ有効）
        UnityEngine.UI.GraphicRaycaster raycaster = menuCanvas.AddComponent<UnityEngine.UI.GraphicRaycaster>();
        raycaster.enabled = true;

        // メニューパネルを作成（全画面の白いウィンドウ）
        menuPanel = new GameObject("MenuPanel");
        menuPanel.transform.SetParent(menuCanvas.transform, false);

        RectTransform panelRect = menuPanel.AddComponent<RectTransform>();
        panelRect.anchorMin = Vector2.zero;
        panelRect.anchorMax = Vector2.one;
        panelRect.sizeDelta = Vector2.zero;
        panelRect.anchoredPosition = Vector2.zero;

        // 白い背景を追加
        UnityEngine.UI.Image panelImage = menuPanel.AddComponent<UnityEngine.UI.Image>();
        panelImage.color = Color.white;

        // ノードコンテナを作成（スクロール可能なエリア）
        CreateNodeContainer();
    }

    private void CreateNodeContainer()
    {
        // ノードコンテナを作成
        nodeContainer = new GameObject("NodeContainer");
        nodeContainer.transform.SetParent(menuPanel.transform, false);

        RectTransform containerRect = nodeContainer.AddComponent<RectTransform>();
        containerRect.anchorMin = new Vector2(0.1f, 0.1f);
        containerRect.anchorMax = new Vector2(0.9f, 0.9f);
        containerRect.sizeDelta = Vector2.zero;
        containerRect.anchoredPosition = Vector2.zero;

        // スクロールビューを追加
        UnityEngine.UI.ScrollRect scrollRect = nodeContainer.AddComponent<UnityEngine.UI.ScrollRect>();
        scrollRect.horizontal = true;
        scrollRect.vertical = true;
        scrollRect.movementType = UnityEngine.UI.ScrollRect.MovementType.Elastic;

        // コンテンツエリアを作成
        GameObject content = new GameObject("Content");
        content.transform.SetParent(nodeContainer.transform, false);
        RectTransform contentRect = content.AddComponent<RectTransform>();
        contentRect.anchorMin = new Vector2(0f, 1f); // 上端にアンカー
        contentRect.anchorMax = new Vector2(1f, 1f); // 上端にアンカー
        contentRect.pivot = new Vector2(0.5f, 1f); // ピボットを上端に
        contentRect.sizeDelta = new Vector2(0f, 0f);
        contentRect.anchoredPosition = Vector2.zero;

        // Vertical Layout Groupを追加（ノードを縦に並べる）
        // ただし、手動配置のノードがある場合は無効化される
        UnityEngine.UI.VerticalLayoutGroup layoutGroup = content.AddComponent<UnityEngine.UI.VerticalLayoutGroup>();
        layoutGroup.spacing = 20f;
        layoutGroup.padding = new RectOffset(20, 20, 20, 20);
        layoutGroup.childControlHeight = false;
        layoutGroup.childControlWidth = false; // ノードの横幅を自由に設定できるようにする
        layoutGroup.childForceExpandWidth = false; // ノードの横幅を強制的に拡張しない
        layoutGroup.enabled = false; // 手動配置のため無効化

        // Content Size Fitterを追加（コンテンツのサイズに合わせる）
        UnityEngine.UI.ContentSizeFitter sizeFitter = content.AddComponent<UnityEngine.UI.ContentSizeFitter>();
        sizeFitter.verticalFit = UnityEngine.UI.ContentSizeFitter.FitMode.PreferredSize;

        scrollRect.content = contentRect;

        // 背景を追加（見やすくするため）
        UnityEngine.UI.Image containerImage = nodeContainer.AddComponent<UnityEngine.UI.Image>();
        containerImage.color = new Color(0.9f, 0.9f, 0.9f, 0.5f);
    }

    /// <summary>
    /// メニューにノードを追加する
    /// </summary>
    public void AddNodeToMenu(string itemName)
    {
        // 既に追加されている場合は追加しない
        if (collectedItems.Contains(itemName))
        {
            Debug.Log($"MenuManager: {itemName}は既にメニューに追加されています。");
            return;
        }

        collectedItems.Add(itemName);
        Debug.Log($"MenuManager: {itemName}を収集リストに追加しました。");

        // メニューが開いている場合はノードUIを作成
        if (isMenuOpen && nodeContainer != null)
        {
            CreateNodeUI(itemName);
        }
        // メニューが開いていない場合は、次回メニューを開いた時に表示される
    }

    /// <summary>
    /// メニューを開いた時に、収集したすべてのアイテムのノードを作成する
    /// </summary>
    private void RefreshMenuNodes()
    {
        if (nodeContainer == null || menuCanvas == null)
        {
            Debug.LogWarning("MenuManager: メニューUIが作成されていません。");
            return;
        }

        // コンテンツエリアを取得
        Transform content = nodeContainer.GetComponent<UnityEngine.UI.ScrollRect>().content;
        if (content == null)
        {
            Debug.LogWarning("MenuManager: コンテンツエリアが見つかりません。");
            return;
        }

        // 既存のノードを確認し、不足しているノードのみを作成（位置を保持するため）
        // まず既存のノードをnodeTransformsに登録
        for (int i = 0; i < content.childCount; i++)
        {
            Transform child = content.GetChild(i);
            if (child.name.StartsWith("Node_"))
            {
                string nodeName = child.name.Substring(5); // "Node_"を除去
                RectTransform nodeRect = child.GetComponent<RectTransform>();
                if (nodeRect != null)
                {
                    nodeTransforms[nodeName] = nodeRect;
                }
            }
        }

        // Playerを先に作成してからOperableを作成するように順序を保証
        System.Collections.Generic.List<string> orderedItems = new System.Collections.Generic.List<string>();
        if (collectedItems.Contains("Player"))
        {
            orderedItems.Add("Player");
        }
        foreach (string itemName in collectedItems)
        {
            if (itemName != "Player" && itemName != "Operable")
            {
                orderedItems.Add(itemName);
            }
        }
        if (collectedItems.Contains("Operable"))
        {
            orderedItems.Add("Operable");
        }

        foreach (string itemName in orderedItems)
        {
            // 既にノードが存在するか確認
            bool nodeExists = nodeTransforms.ContainsKey(itemName);

            // ノードが存在しない場合のみ作成
            if (!nodeExists)
            {
                CreateNodeUI(itemName);
            }
        }

        // PlayerとOperableノードが両方存在する場合、ゲーム開始時にのみ合体状態にする
        // （既に合体している場合は再合体しない）
        if (!hasInitializedPlayerOperableMerge && nodeTransforms.ContainsKey("Player") && nodeTransforms.ContainsKey("Operable"))
        {
            RectTransform playerNode = nodeTransforms["Player"];
            RectTransform operableNode = nodeTransforms["Operable"];
            
            if (playerNode != null && operableNode != null)
            {
                NodeMergeManager mergeManager = NodeMergeManager.Instance;
                if (mergeManager != null)
                {
                    // 既に合体しているかどうかを確認
                    bool alreadyMerged = false;
                    
                    // OperableがPlayerと合体しているか確認
                    if (mergeManager.IsMerged(operableNode))
                    {
                        var parent = mergeManager.GetParentNode(operableNode);
                        if (parent == playerNode)
                        {
                            alreadyMerged = true;
                        }
                    }
                    // PlayerがOperableと合体しているか確認（Operableが親の場合）
                    else if (mergeManager.IsParentNode(operableNode))
                    {
                        var children = mergeManager.GetChildNodes(operableNode);
                        if (children.Contains(playerNode))
                        {
                            alreadyMerged = true;
                        }
                    }
                    // Playerが親でOperableが子の場合
                    else if (mergeManager.IsParentNode(playerNode))
                    {
                        var children = mergeManager.GetChildNodes(playerNode);
                        if (children.Contains(operableNode))
                        {
                            alreadyMerged = true;
                        }
                    }
                    
                    if (!alreadyMerged)
                    {
                        // Player（青色）を親、Operable（黄色）を子として合体
                        mergeManager.MergeNodes(playerNode, operableNode);
                        Debug.Log("MenuManager: ゲーム開始時にPlayerとOperableノードを合体しました。");
                    }
                    
                    // 初期化フラグを立てる（一度だけ実行）
                    hasInitializedPlayerOperableMerge = true;
                }
            }
        }

        Debug.Log($"MenuManager: {collectedItems.Count}個のノードをメニューに表示しました。");
    }

    /// <summary>
    /// ノードのRectTransformの辞書を取得（外部からアクセス用）
    /// </summary>
    public System.Collections.Generic.Dictionary<string, RectTransform> GetNodeTransforms()
    {
        return nodeTransforms;
    }

    private void CreateNodeUI(string itemName)
    {
        if (nodeContainer == null || menuCanvas == null)
        {
            Debug.LogWarning("MenuManager: メニューUIが作成されていません。");
            return;
        }

        // コンテンツエリアを取得
        Transform content = nodeContainer.GetComponent<UnityEngine.UI.ScrollRect>().content;
        if (content == null)
        {
            Debug.LogWarning("MenuManager: コンテンツエリアが見つかりません。");
            return;
        }

        // ノードオブジェクトを作成
        GameObject nodeObj = new GameObject($"Node_{itemName}");
        nodeObj.transform.SetParent(content, false);

        RectTransform nodeRect = nodeObj.AddComponent<RectTransform>();
        // アンカーを左上に設定（サイズを自由に設定できるようにする）
        nodeRect.anchorMin = new Vector2(0f, 1f);
        nodeRect.anchorMax = new Vector2(0f, 1f);
        nodeRect.pivot = new Vector2(0f, 1f);
        nodeRect.sizeDelta = new Vector2(225f, 120f); // 横幅3倍(75→225)、縦幅1.5倍(80→120)
        
        // ノードのRectTransformを先に記録（位置設定で参照するため）
        nodeTransforms[itemName] = nodeRect;
        
        // ノードの初期位置を設定
        SetNodeInitialPosition(nodeRect, itemName);

        // ノードの背景（ノードリンク図のようなデザイン）
        UnityEngine.UI.Image nodeImage = nodeObj.AddComponent<UnityEngine.UI.Image>();
        // 「Operable」「HighJumping」「Big」の場合は黄色、それ以外は青色
        if (itemName == "Operable" || itemName == "HighJumping" || itemName.StartsWith("HighJumping") || 
            itemName == "Big" || itemName.StartsWith("Big"))
        {
            nodeImage.color = new Color(1f, 0.84f, 0f, 1f); // 黄色の背景
        }
        else
        {
            nodeImage.color = new Color(0.2f, 0.4f, 0.8f, 1f); // 青い背景
        }

        // ノードの枠線
        UnityEngine.UI.Outline outline = nodeObj.AddComponent<UnityEngine.UI.Outline>();
        outline.effectColor = Color.black;
        outline.effectDistance = new Vector2(2f, 2f);

        // ドラッグ可能にするコンポーネントを追加
        DraggableNode draggableNode = nodeObj.AddComponent<DraggableNode>();

        // テキストを追加（アイテム名）
        GameObject textObj = new GameObject("Text");
        textObj.transform.SetParent(nodeObj.transform, false);

        RectTransform textRect = textObj.AddComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.sizeDelta = Vector2.zero;
        textRect.anchoredPosition = Vector2.zero;

        UnityEngine.UI.Text text = textObj.AddComponent<UnityEngine.UI.Text>();
        text.text = itemName;
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.fontSize = 24;
        text.color = Color.white;
        text.alignment = TextAnchor.MiddleCenter;
        text.fontStyle = FontStyle.Bold;

        Debug.Log($"MenuManager: {itemName}のノードをメニューに追加しました。位置: {nodeRect.anchoredPosition}");
    }

    /// <summary>
    /// ノードの初期位置を設定
    /// </summary>
    private void SetNodeInitialPosition(RectTransform nodeRect, string itemName)
    {
        // ノードの間隔
        float nodeSpacing = 30f;
        
        if (itemName == "Player")
        {
            // Playerノードは左上に配置
            nodeRect.anchoredPosition = new Vector2(20f, -20f);
            Debug.Log($"SetNodeInitialPosition: Playerノードの位置を設定しました: {nodeRect.anchoredPosition}");
        }
        else if (itemName == "Operable")
        {
            // OperableノードはPlayerノードの右横に配置
            if (nodeTransforms.ContainsKey("Player"))
            {
                RectTransform playerNode = nodeTransforms["Player"];
                if (playerNode != null)
                {
                    // Playerノードの右端を計算（sizeDeltaを使用）
                    float playerWidth = playerNode.sizeDelta.x;
                    float playerRight = playerNode.anchoredPosition.x + playerWidth;
                    float playerY = playerNode.anchoredPosition.y;
                    nodeRect.anchoredPosition = new Vector2(playerRight + nodeSpacing, playerY);
                    Debug.Log($"SetNodeInitialPosition: Operableノードの位置を設定しました: {nodeRect.anchoredPosition} (Player位置: {playerNode.anchoredPosition}, Player幅: {playerWidth})");
                }
                else
                {
                    nodeRect.anchoredPosition = new Vector2(20f, -20f);
                    Debug.LogWarning("SetNodeInitialPosition: PlayerノードのRectTransformがnullです");
                }
            }
            else
            {
                // Playerノードが存在しない場合は左上に配置
                nodeRect.anchoredPosition = new Vector2(20f, -20f);
                Debug.LogWarning("SetNodeInitialPosition: Playerノードが見つかりません");
            }
        }
        else
        {
            // その他のノードは既存のノードの下に配置
            float maxY = -20f;
            foreach (var kvp in nodeTransforms)
            {
                if (kvp.Value != null)
                {
                    float nodeBottom = kvp.Value.anchoredPosition.y - kvp.Value.rect.height;
                    if (nodeBottom < maxY)
                    {
                        maxY = nodeBottom;
                    }
                }
            }
            nodeRect.anchoredPosition = new Vector2(20f, maxY - nodeSpacing);
        }
    }


    private void DestroyMenuUI()
    {
        // シーンが破棄される際にのみCanvasを破棄（通常は非表示にするだけ）
        if (menuCanvas != null)
        {
            Destroy(menuCanvas);
            menuCanvas = null;
            menuPanel = null;
            nodeContainer = null;
        }
    }

    private void OnDestroy()
    {
        // シーンが破棄される際に時間を元に戻す
        Time.timeScale = 1f;
        // UIも破棄
        DestroyMenuUI();
    }

    private void OnDisable()
    {
        // コンポーネントが無効化された時に時間を元に戻す
        if (isMenuOpen)
        {
            Time.timeScale = 1f;
            DestroyMenuUI();
            isMenuOpen = false;
        }
    }
}
