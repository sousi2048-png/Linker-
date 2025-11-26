using UnityEngine;
using System.Collections.Generic;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

/// <summary>
/// OperableノードとRedBoxノードが合体しているときに、WASDキーでRedBoxオブジェクトを動かすスクリプト
/// </summary>
public class RedBoxController : MonoBehaviour
{
    [Header("Movement Settings")]
    [Tooltip("RedBoxの移動速度")]
    public float moveSpeed = 5f;
    
    [Tooltip("RedBoxのジャンプ力")]
    public float jumpForce = 5f;
    
    [Tooltip("地面検出用のRaycast距離")]
    public float groundCheckDistance = 0.1f;

    private NodeMergeManager mergeManager;
    private MenuManager menuManager;
    
    // 操作可能なオブジェクトの管理
    private Dictionary<string, GameObject> controllableObjects = new Dictionary<string, GameObject>(); // ノード名 -> オブジェクト
    private Dictionary<string, Rigidbody> objectRigidbodies = new Dictionary<string, Rigidbody>(); // ノード名 -> Rigidbody
    private Dictionary<string, MonoBehaviour> objectThirdPersonControllers = new Dictionary<string, MonoBehaviour>(); // ノード名 -> ThirdPersonController
    
    // HighJumpingオブジェクトの管理（常にジャンプさせるため）
    private List<GameObject> highJumpingObjects = new List<GameObject>(); // HighJumpingオブジェクトのリスト
    
    private GameObject playerObject; // Playerオブジェクト
    private MonoBehaviour thirdPersonController; // ThirdPersonControllerコンポーネント
    private float originalMoveSpeed = 0f; // 元の移動速度を保存
    private float originalSprintSpeed = 0f; // 元のスプリント速度を保存
    private float originalJumpHeight = 0f; // 元のジャンプ力を保存
    private bool hasStoredOriginalValues = false; // 元の値を保存したかどうか
    
    // 元のスケールを保存（Big用）
    private Dictionary<string, Vector3> originalScales = new Dictionary<string, Vector3>();
    // CharacterControllerの元の値を保存（Big用）
    private Dictionary<string, float> originalCharacterHeights = new Dictionary<string, float>();
    private Dictionary<string, Vector3> originalCharacterCenters = new Dictionary<string, Vector3>();
    // ThirdPersonControllerの元の値を保存（Big用）
    private Dictionary<string, float> originalGroundedOffsets = new Dictionary<string, float>();
    private Dictionary<string, float> originalGroundedRadiuses = new Dictionary<string, float>();
    // 各オブジェクトの元のジャンプ高さを保存（合体解除時にリセットするため）
    private Dictionary<string, float> originalJumpHeights = new Dictionary<string, float>();
    
    // 現在操作可能なオブジェクト（1つのみ）
    private string currentControllableNodeName = null;

#if ENABLE_INPUT_SYSTEM
    private Keyboard keyboard;
#endif

    private void Start()
    {
        Debug.Log("RedBoxController: Start()が呼ばれました。");
        mergeManager = NodeMergeManager.Instance;
        menuManager = FindObjectOfType<MenuManager>();
        
        if (mergeManager == null)
        {
            Debug.LogWarning("RedBoxController: NodeMergeManager.Instanceが見つかりません。");
        }
        if (menuManager == null)
        {
            Debug.LogWarning("RedBoxController: MenuManagerが見つかりません。");
        }
        
        // HighJumpingオブジェクトを検出してリストに追加
        FindHighJumpingObjects();
        
        // Playerオブジェクトを取得（"Player"タグで検索）
        playerObject = GameObject.FindGameObjectWithTag("Player");
        if (playerObject == null)
        {
            // タグで見つからない場合は名前で検索
#if UNITY_2023_1_OR_NEWER
            GameObject[] allObjects = Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None);
#else
            GameObject[] allObjects = Object.FindObjectsOfType<GameObject>();
#endif
            foreach (GameObject obj in allObjects)
            {
                if (obj.name.Contains("Player") || obj.name.Contains("PlayerCapsule") || obj.name.Contains("PlayerArmature"))
                {
                    playerObject = obj;
                    break;
                }
            }
        }
        
        if (playerObject != null)
        {
            Debug.Log($"RedBoxController: Playerオブジェクトを見つけました: {playerObject.name}");
            // Playerの操作関連コンポーネントを取得
            var allMonoBehaviours = playerObject.GetComponents<MonoBehaviour>();
            Debug.Log($"RedBoxController: PlayerオブジェクトのMonoBehaviourコンポーネント数: {allMonoBehaviours.Length}");
            foreach (var mb in allMonoBehaviours)
            {
                string typeName = mb.GetType().Name;
                Debug.Log($"RedBoxController: コンポーネント名: {typeName}, フルネーム: {mb.GetType().FullName}");
                // ThirdPersonControllerクラスかどうかを確認
                if (typeName == "ThirdPersonController")
                {
                    thirdPersonController = mb;
                    Debug.Log($"RedBoxController: ThirdPersonControllerを見つけました！");
                    break;
                }
            }
            
            if (thirdPersonController == null)
            {
                Debug.LogWarning("RedBoxController: ThirdPersonControllerが見つかりませんでした。");
            }
            else
            {
                // Start()で元の値を保存（Update()が呼ばれる前に）
                StoreOriginalValues();
                
                // Playerを操作可能なオブジェクトとして登録
                controllableObjects["Player"] = playerObject;
                objectThirdPersonControllers["Player"] = thirdPersonController;
                
                // ゲーム開始時はPlayerを操作可能にする
                if (!thirdPersonController.enabled)
                {
                    thirdPersonController.enabled = true;
                    Debug.Log("RedBoxController: ゲーム開始時にPlayerのThirdPersonControllerを有効化しました。");
                }
            }
        }
        else
        {
            Debug.LogWarning("RedBoxController: Playerオブジェクトが見つかりませんでした。");
        }

#if ENABLE_INPUT_SYSTEM
        keyboard = Keyboard.current;
#endif
    }
    
    /// <summary>
    /// Playerの元の速度とジャンプ力を保存する
    /// </summary>
    private void StoreOriginalValues()
    {
        if (thirdPersonController == null)
        {
            return;
        }
        
        if (hasStoredOriginalValues)
        {
            return; // 既に保存済み
        }
        
        // フィールドを取得
        var moveSpeedField = thirdPersonController.GetType().GetField("MoveSpeed");
        var sprintSpeedField = thirdPersonController.GetType().GetField("SprintSpeed");
        var jumpHeightField = thirdPersonController.GetType().GetField("JumpHeight");
        
        // 元の値を保存
        if (moveSpeedField != null)
        {
            originalMoveSpeed = (float)moveSpeedField.GetValue(thirdPersonController);
            Debug.Log($"RedBoxController: 元のMoveSpeedを保存: {originalMoveSpeed}");
        }
        if (sprintSpeedField != null)
        {
            originalSprintSpeed = (float)sprintSpeedField.GetValue(thirdPersonController);
            Debug.Log($"RedBoxController: 元のSprintSpeedを保存: {originalSprintSpeed}");
        }
        if (jumpHeightField != null)
        {
            originalJumpHeight = (float)jumpHeightField.GetValue(thirdPersonController);
            Debug.Log($"RedBoxController: 元のJumpHeightを保存: {originalJumpHeight}");
        }
        hasStoredOriginalValues = true;
    }

    private void Update()
    {
        // 最初の数フレームだけログを出力（ログが多すぎるのを防ぐ）
        if (Time.frameCount <= 5)
        {
            Debug.Log($"RedBoxController: Update()が呼ばれました。フレーム: {Time.frameCount}, Time.timeScale: {Time.timeScale}");
            Debug.Log($"RedBoxController: playerObject={(playerObject != null ? playerObject.name : "null")}, thirdPersonController={(thirdPersonController != null ? thirdPersonController.GetType().Name : "null")}, enabled={(thirdPersonController != null ? thirdPersonController.enabled.ToString() : "N/A")}");
        }
        
        // 元の値を保存（まだ保存されていない場合）
        if (!hasStoredOriginalValues && thirdPersonController != null)
        {
            StoreOriginalValues();
        }
        
        // メニューが開かれているか確認
        bool isMenuOpen = false;
        if (menuManager != null)
        {
            var nodeTransforms = menuManager.GetNodeTransforms();
            isMenuOpen = (nodeTransforms != null && nodeTransforms.Count > 0);
        }
        
        // 合体状態を確認
        CheckMergeState();
        
        // HighJumpingオブジェクトを常にジャンプさせる（メニューの合体状態に関係なく）
        HandleHighJumpingObjects();
        
        // 操作可能なオブジェクトを制御
        if (!string.IsNullOrEmpty(currentControllableNodeName))
        {
            if (Time.frameCount <= 5)
            {
                Debug.Log($"RedBoxController: currentControllableNodeName={currentControllableNodeName}, EnableObjectControlを呼び出します。");
            }
            // Operableと合体している青ノードのオブジェクトを操作可能にする
            EnableObjectControl(currentControllableNodeName);
            
            // HighJumpingノードと合体している場合、JumpHeightを1.5倍にする（ThirdPersonControllerの場合）
            // Bigノードと合体している場合、JumpHeightを3倍にする（ThirdPersonControllerの場合）
            // 両方合体している場合、JumpHeightを6倍にする（ThirdPersonControllerの場合）
            if (objectThirdPersonControllers.ContainsKey(currentControllableNodeName))
            {
                GameObject obj = controllableObjects[currentControllableNodeName];
                MonoBehaviour tpc = objectThirdPersonControllers[currentControllableNodeName];
                if (obj != null && tpc != null)
                {
                    bool isMergedWithHighJumping = IsMergedWithHighJumping(currentControllableNodeName);
                    bool isMergedWithBig = IsMergedWithBig(currentControllableNodeName);
                    
                    var jumpHeightField = tpc.GetType().GetField("JumpHeight");
                    if (jumpHeightField != null)
                    {
                        float currentJumpHeight = (float)jumpHeightField.GetValue(tpc);
                        
                        // 元のジャンプ高さを保存（まだ保存されていない場合、または元の値が0の場合）
                        if (!originalJumpHeights.ContainsKey(currentControllableNodeName) || originalJumpHeights[currentControllableNodeName] == 0f)
                        {
                            // 現在の値が異常に大きい場合（既に倍率が適用されている場合）は、元の値として保存しない
                            // 通常のジャンプ高さの範囲（0.1～10.0）内の場合のみ保存
                            if (currentJumpHeight >= 0.1f && currentJumpHeight <= 10.0f)
                            {
                                originalJumpHeights[currentControllableNodeName] = currentJumpHeight;
                            }
                            else
                            {
                                // 異常に大きい値の場合は、デフォルト値（1.2f）を保存
                                originalJumpHeights[currentControllableNodeName] = 1.2f;
                            }
                        }
                        float originalJumpHeightForNode = originalJumpHeights[currentControllableNodeName];
                        
                        if (isMergedWithHighJumping && isMergedWithBig)
                        {
                            // 両方合体している場合、JumpHeightを6倍にする
                            if (originalJumpHeightForNode > 0f)
                            {
                                jumpHeightField.SetValue(tpc, originalJumpHeightForNode * 6f);
                            }
                        }
                        else if (isMergedWithHighJumping)
                        {
                            // HighJumpingのみ合体している場合、JumpHeightを1.5倍にする
                            if (originalJumpHeightForNode > 0f)
                            {
                                jumpHeightField.SetValue(tpc, originalJumpHeightForNode * 1.5f);
                            }
                        }
                        else if (isMergedWithBig)
                        {
                            // Bigのみ合体している場合、JumpHeightを3倍にする
                            if (originalJumpHeightForNode > 0f)
                            {
                                jumpHeightField.SetValue(tpc, originalJumpHeightForNode * 3f);
                            }
                        }
                        else
                        {
                            // 合体していない場合、元の値に戻す
                            if (originalJumpHeightForNode > 0f)
                            {
                                jumpHeightField.SetValue(tpc, originalJumpHeightForNode);
                            }
                        }
                    }
                }
            }
            
            // HighJumpingの場合は常にジャンプするように設定（ThirdPersonControllerの場合）
            // 毎フレーム確実にジャンプ入力を設定する
            if ((currentControllableNodeName == "HighJumping" || currentControllableNodeName.StartsWith("HighJumping")) 
                && objectThirdPersonControllers.ContainsKey(currentControllableNodeName))
            {
                GameObject obj = controllableObjects[currentControllableNodeName];
                if (obj != null)
                {
                    // StarterAssetsInputsコンポーネントを取得してjumpを常にtrueに設定
                    var starterAssetsInputs = obj.GetComponent<StarterAssets.StarterAssetsInputs>();
                    if (starterAssetsInputs != null)
                    {
                        starterAssetsInputs.jump = true; // 毎フレームtrueに設定して常にジャンプ
                    }
                    else
                    {
                        // StarterAssetsInputsが見つからない場合、リフレクションで_inputフィールドを取得
                        MonoBehaviour tpc = objectThirdPersonControllers[currentControllableNodeName];
                        if (tpc != null)
                        {
                            var inputField = tpc.GetType().GetField("_input", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                            if (inputField != null)
                            {
                                var inputObj = inputField.GetValue(tpc);
                                if (inputObj != null)
                                {
                                    var jumpField = inputObj.GetType().GetField("jump");
                                    if (jumpField != null)
                                    {
                                        jumpField.SetValue(inputObj, true); // 毎フレームtrueに設定して常にジャンプ
                                    }
                                }
                            }
                        }
                    }
                }
            }
            
            // 他の青ノードのオブジェクトを操作不可能にする
            DisableOtherObjects(currentControllableNodeName);
        }
        else
        {
            if (Time.frameCount <= 5)
            {
                Debug.Log($"RedBoxController: currentControllableNodeName=null, isMenuOpen={isMenuOpen}");
            }
            // メニューが開かれていない場合、デフォルトでPlayerを操作可能にする
            if (!isMenuOpen)
            {
                // Playerを操作可能にする
                if (controllableObjects.ContainsKey("Player"))
                {
                    if (Time.frameCount <= 5)
                    {
                        Debug.Log("RedBoxController: controllableObjectsにPlayerが見つかりました。EnableObjectControlを呼び出します。");
                    }
                    EnableObjectControl("Player");
                }
                else if (playerObject != null && thirdPersonController != null)
                {
                    // Playerオブジェクトが直接見つかっている場合、ThirdPersonControllerを有効化
                    if (!thirdPersonController.enabled)
                    {
                        thirdPersonController.enabled = true;
                        Debug.Log("RedBoxController: PlayerのThirdPersonControllerを有効化しました（メニュー未開時）。");
                    }
                    else if (Time.frameCount <= 5)
                    {
                        Debug.Log($"RedBoxController: PlayerのThirdPersonControllerは既に有効です。enabled={thirdPersonController.enabled}");
                    }
                }
                else
                {
                    if (Time.frameCount <= 5)
                    {
                        Debug.LogWarning($"RedBoxController: Playerが見つかりません。playerObject={(playerObject != null ? playerObject.name : "null")}, thirdPersonController={(thirdPersonController != null ? "found" : "null")}");
                    }
                }
            }
            else
            {
                // メニューが開かれているが、合体していない場合、すべてのオブジェクトを操作不可能にする
                DisableAllObjects();
            }
        }
    }

    /// <summary>
    /// OperableとRedBoxノードが合体しているか確認
    /// </summary>
    private bool CheckOperableRedBoxMerge()
    {
        if (mergeManager == null || menuManager == null)
        {
            return false;
        }

        var nodeTransforms = menuManager.GetNodeTransforms();
        if (nodeTransforms == null)
        {
            return false;
        }

        RectTransform operableNode = null;
        RectTransform redBoxNode = null;

        if (nodeTransforms.ContainsKey("Operable"))
        {
            operableNode = nodeTransforms["Operable"];
        }

        foreach (var kvp in nodeTransforms)
        {
            if (kvp.Key.StartsWith("RedBox"))
            {
                redBoxNode = kvp.Value;
                break;
            }
        }

        if (operableNode == null || redBoxNode == null)
        {
            return false;
        }

        // OperableとRedBoxが合体しているか確認
        bool areMerged = false;
        if (mergeManager.IsParentNode(operableNode))
        {
            var children = mergeManager.GetChildNodes(operableNode);
            areMerged = children.Contains(redBoxNode);
        }
        else if (mergeManager.IsParentNode(redBoxNode))
        {
            var children = mergeManager.GetChildNodes(redBoxNode);
            areMerged = children.Contains(operableNode);
        }
        else if (mergeManager.IsMerged(operableNode))
        {
            var parent = mergeManager.GetParentNode(operableNode);
            areMerged = (parent == redBoxNode);
        }
        else if (mergeManager.IsMerged(redBoxNode))
        {
            var parent = mergeManager.GetParentNode(redBoxNode);
            areMerged = (parent == operableNode);
        }

        return areMerged;
    }

    /// <summary>
    /// Playerを完全に操作不可能にする（ThirdPersonControllerを無効化）
    /// </summary>
    private void SetPlayerSpeedAndJumpToZero()
    {
        Debug.Log("RedBoxController: SetPlayerSpeedAndJumpToZero()が呼ばれました。");
        
        if (thirdPersonController == null && playerObject != null)
        {
            Debug.Log("RedBoxController: ThirdPersonControllerを再取得します。");
            // ThirdPersonControllerを再取得
            var allMonoBehaviours = playerObject.GetComponents<MonoBehaviour>();
            foreach (var mb in allMonoBehaviours)
            {
                if (mb.GetType().Name == "ThirdPersonController")
                {
                    thirdPersonController = mb;
                    Debug.Log($"RedBoxController: ThirdPersonControllerを見つけました: {mb.GetType().FullName}");
                    break;
                }
            }
        }
        
        if (thirdPersonController == null)
        {
            Debug.LogWarning("RedBoxController: ThirdPersonControllerが見つかりません。playerObject=" + (playerObject != null ? playerObject.name : "null"));
            return;
        }
        
        // 元の値を保存（まだ保存されていない場合）
        StoreOriginalValues();
        
        // ThirdPersonControllerコンポーネントを無効化（すべての動作を停止）
        if (thirdPersonController.enabled)
        {
            thirdPersonController.enabled = false;
            Debug.Log("RedBoxController: ThirdPersonControllerを無効化しました。");
        }
    }

    /// <summary>
    /// Playerを操作可能にする（ThirdPersonControllerを有効化）
    /// </summary>
    private void RestorePlayerSpeedAndJump()
    {
        Debug.Log("RedBoxController: RestorePlayerSpeedAndJump()が呼ばれました。");
        
        if (thirdPersonController == null && playerObject != null)
        {
            // ThirdPersonControllerを再取得
            var allMonoBehaviours = playerObject.GetComponents<MonoBehaviour>();
            foreach (var mb in allMonoBehaviours)
            {
                if (mb.GetType().Name == "ThirdPersonController")
                {
                    thirdPersonController = mb;
                    break;
                }
            }
        }
        
        if (thirdPersonController == null)
        {
            Debug.LogWarning("RedBoxController: RestorePlayerSpeedAndJump() - ThirdPersonControllerが見つかりません。");
            return;
        }
        
        if (!hasStoredOriginalValues)
        {
            Debug.LogWarning("RedBoxController: RestorePlayerSpeedAndJump() - 元の値が保存されていません。StoreOriginalValues()を呼び出します。");
            StoreOriginalValues();
        }
        
        // ThirdPersonControllerコンポーネントを有効化（すべての動作を復元）
        if (!thirdPersonController.enabled)
        {
            thirdPersonController.enabled = true;
            Debug.Log("RedBoxController: ThirdPersonControllerを有効化しました。");
        }
        
        // Move Speed、Sprint Speed、Jump Heightを元の値に戻す（念のため）
        var moveSpeedField = thirdPersonController.GetType().GetField("MoveSpeed");
        var sprintSpeedField = thirdPersonController.GetType().GetField("SprintSpeed");
        var jumpHeightField = thirdPersonController.GetType().GetField("JumpHeight");
        
        if (moveSpeedField != null && originalMoveSpeed > 0f)
        {
            moveSpeedField.SetValue(thirdPersonController, originalMoveSpeed);
            float currentValue = (float)moveSpeedField.GetValue(thirdPersonController);
            Debug.Log($"RedBoxController: MoveSpeedを元の値に戻しました。現在の値: {currentValue}, 元の値: {originalMoveSpeed}");
        }
        if (sprintSpeedField != null && originalSprintSpeed > 0f)
        {
            sprintSpeedField.SetValue(thirdPersonController, originalSprintSpeed);
            float currentValue = (float)sprintSpeedField.GetValue(thirdPersonController);
            Debug.Log($"RedBoxController: SprintSpeedを元の値に戻しました。現在の値: {currentValue}, 元の値: {originalSprintSpeed}");
        }
        if (jumpHeightField != null && originalJumpHeight > 0f)
        {
            jumpHeightField.SetValue(thirdPersonController, originalJumpHeight);
            float currentValue = (float)jumpHeightField.GetValue(thirdPersonController);
            Debug.Log($"RedBoxController: JumpHeightを元の値に戻しました。現在の値: {currentValue}, 元の値: {originalJumpHeight}");
        }
    }

    /// <summary>
    /// OperableとPlayerノードが合体しているか確認
    /// </summary>
    private bool CheckOperablePlayerMerge()
    {
        if (mergeManager == null || menuManager == null)
        {
            return false;
        }

        var nodeTransforms = menuManager.GetNodeTransforms();
        if (nodeTransforms == null || nodeTransforms.Count == 0)
        {
            // メニューがまだ開かれていない場合、デフォルトでtrueを返す（PlayerとOperableは初期状態で合体している）
            Debug.Log("RedBoxController: CheckOperablePlayerMerge() - メニューがまだ開かれていないため、デフォルトでtrueを返します。");
            return true;
        }

        RectTransform operableNode = null;
        RectTransform playerNode = null;

        if (nodeTransforms.ContainsKey("Operable"))
        {
            operableNode = nodeTransforms["Operable"];
        }
        if (nodeTransforms.ContainsKey("Player"))
        {
            playerNode = nodeTransforms["Player"];
        }

        if (operableNode == null || playerNode == null)
        {
            // ノードがまだ作成されていない場合、デフォルトでtrueを返す
            Debug.Log("RedBoxController: CheckOperablePlayerMerge() - ノードがまだ作成されていないため、デフォルトでtrueを返します。");
            return true;
        }

        // OperableとPlayerが合体しているか確認
        bool areMerged = false;

        if (mergeManager.IsParentNode(operableNode))
        {
            var children = mergeManager.GetChildNodes(operableNode);
            areMerged = children.Contains(playerNode);
        }
        else if (mergeManager.IsParentNode(playerNode))
        {
            var children = mergeManager.GetChildNodes(playerNode);
            areMerged = children.Contains(operableNode);
        }
        else if (mergeManager.IsMerged(operableNode))
        {
            var parent = mergeManager.GetParentNode(operableNode);
            areMerged = (parent == playerNode);
        }
        else if (mergeManager.IsMerged(playerNode))
        {
            var parent = mergeManager.GetParentNode(playerNode);
            areMerged = (parent == operableNode);
        }

        return areMerged;
    }

    /// <summary>
    /// 指定されたノードがHighJumpingノードと合体しているか確認
    /// </summary>
    private bool IsMergedWithHighJumping(string nodeName)
    {
        if (mergeManager == null || menuManager == null)
        {
            return false;
        }

        var nodeTransforms = menuManager.GetNodeTransforms();
        if (nodeTransforms == null || nodeTransforms.Count == 0)
        {
            return false;
        }

        RectTransform targetNode = null;
        RectTransform highJumpingNode = null;

        if (nodeTransforms.ContainsKey(nodeName))
        {
            targetNode = nodeTransforms[nodeName];
        }
        
        // HighJumpingノードを探す
        foreach (var kvp in nodeTransforms)
        {
            if (kvp.Key == "HighJumping" || kvp.Key.StartsWith("HighJumping"))
            {
                highJumpingNode = kvp.Value;
                break;
            }
        }

        if (targetNode == null || highJumpingNode == null)
        {
            return false;
        }

        // HighJumpingと合体しているか確認
        bool areMerged = false;

        if (mergeManager.IsParentNode(highJumpingNode))
        {
            var children = mergeManager.GetChildNodes(highJumpingNode);
            areMerged = children.Contains(targetNode);
        }
        else if (mergeManager.IsParentNode(targetNode))
        {
            var children = mergeManager.GetChildNodes(targetNode);
            areMerged = children.Contains(highJumpingNode);
        }
        else if (mergeManager.IsMerged(highJumpingNode))
        {
            var parent = mergeManager.GetParentNode(highJumpingNode);
            areMerged = (parent == targetNode);
        }
        else if (mergeManager.IsMerged(targetNode))
        {
            var parent = mergeManager.GetParentNode(targetNode);
            areMerged = (parent == highJumpingNode);
        }

        return areMerged;
    }

    /// <summary>
    /// 指定されたノードがBigノードと合体しているか確認
    /// </summary>
    private bool IsMergedWithBig(string nodeName)
    {
        if (mergeManager == null || menuManager == null)
        {
            return false;
        }

        var nodeTransforms = menuManager.GetNodeTransforms();
        if (nodeTransforms == null || nodeTransforms.Count == 0)
        {
            return false;
        }

        RectTransform targetNode = null;
        RectTransform bigNode = null;

        if (nodeTransforms.ContainsKey(nodeName))
        {
            targetNode = nodeTransforms[nodeName];
        }
        
        // Bigノードを探す
        foreach (var kvp in nodeTransforms)
        {
            if (kvp.Key == "Big" || kvp.Key.StartsWith("Big"))
            {
                bigNode = kvp.Value;
                break;
            }
        }

        if (targetNode == null || bigNode == null)
        {
            return false;
        }

        // Bigと合体しているか確認
        bool areMerged = false;

        if (mergeManager.IsParentNode(bigNode))
        {
            var children = mergeManager.GetChildNodes(bigNode);
            areMerged = children.Contains(targetNode);
        }
        else if (mergeManager.IsParentNode(targetNode))
        {
            var children = mergeManager.GetChildNodes(targetNode);
            areMerged = children.Contains(bigNode);
        }
        else if (mergeManager.IsMerged(bigNode))
        {
            var parent = mergeManager.GetParentNode(bigNode);
            areMerged = (parent == targetNode);
        }
        else if (mergeManager.IsMerged(targetNode))
        {
            var parent = mergeManager.GetParentNode(targetNode);
            areMerged = (parent == bigNode);
        }

        return areMerged;
    }

    /// <summary>
    /// Operableと合体している青ノードをすべて取得し、対応するオブジェクトを操作可能にする
    /// </summary>
    private void CheckMergeState()
    {
        // mergeManagerやmenuManagerがnullの場合、デフォルトでPlayerを操作可能にする
        if (mergeManager == null || menuManager == null)
        {
            currentControllableNodeName = "Player";
            // Playerオブジェクトが見つかっていない場合は探す
            if (!controllableObjects.ContainsKey("Player") && playerObject != null)
            {
                controllableObjects["Player"] = playerObject;
                if (thirdPersonController != null)
                {
                    objectThirdPersonControllers["Player"] = thirdPersonController;
                }
            }
            return;
        }

        var nodeTransforms = menuManager.GetNodeTransforms();
        
        // メニューがまだ開かれていない場合、デフォルトでPlayerとOperableが合体していると仮定
        if (nodeTransforms == null || nodeTransforms.Count == 0)
        {
            // ゲーム開始時はPlayerを操作可能にする
            currentControllableNodeName = "Player";
            // Playerオブジェクトが見つかっていない場合は探す
            if (!controllableObjects.ContainsKey("Player"))
            {
                FindObjectForNode("Player", null);
            }
            return;
        }

        RectTransform operableNode = null;
        if (nodeTransforms.ContainsKey("Operable"))
        {
            operableNode = nodeTransforms["Operable"];
        }

        if (operableNode == null)
        {
            // Operableノードが見つからない場合も、デフォルトでPlayerとOperableが合体していると仮定
            if (CheckOperablePlayerMerge())
            {
                currentControllableNodeName = "Player";
                if (!controllableObjects.ContainsKey("Player"))
                {
                    FindObjectForNode("Player", null);
                }
            }
            else
            {
                currentControllableNodeName = null;
            }
            return;
        }

        // Operableと合体している青ノードを探す
        List<string> mergedBlueNodeNames = new List<string>();
        
        // Operableが親の場合、子ノードの中から青ノードを探す
        if (mergeManager.IsParentNode(operableNode))
        {
            var children = mergeManager.GetChildNodes(operableNode);
            foreach (var childNode in children)
            {
                string nodeName = GetNodeName(childNode);
                if (IsBlueNode(childNode) && nodeName != null)
                {
                    mergedBlueNodeNames.Add(nodeName);
                }
            }
        }
        // Operableが子の場合、親ノードが青ノードか確認
        else if (mergeManager.IsMerged(operableNode))
        {
            var parentNode = mergeManager.GetParentNode(operableNode);
            string nodeName = GetNodeName(parentNode);
            if (IsBlueNode(parentNode) && nodeName != null)
            {
                mergedBlueNodeNames.Add(nodeName);
            }
        }

        // 合体している青ノードが1つだけの場合、そのオブジェクトを操作可能にする
        if (mergedBlueNodeNames.Count == 1)
        {
            string nodeName = mergedBlueNodeNames[0];
            currentControllableNodeName = nodeName;
            
            // オブジェクトが見つかっていない場合は探す
            if (!controllableObjects.ContainsKey(nodeName))
            {
                FindObjectForNode(nodeName, nodeTransforms[nodeName]);
            }
        }
        else
        {
            currentControllableNodeName = null;
        }
    }
    
    /// <summary>
    /// ノードが青ノードかどうかを判定
    /// </summary>
    private bool IsBlueNode(RectTransform nodeTransform)
    {
        if (nodeTransform == null) return false;
        
        UnityEngine.UI.Image image = nodeTransform.GetComponent<UnityEngine.UI.Image>();
        if (image == null)
        {
            image = nodeTransform.GetComponentInChildren<UnityEngine.UI.Image>();
        }
        
        if (image != null)
        {
            Color nodeColor = image.color;
            // 青色: Rが低く、Gが中程度、Bが高い
            if (nodeColor.r < 0.4f && nodeColor.g > 0.3f && nodeColor.b > 0.6f)
            {
                return true;
            }
        }
        
        return false;
    }
    
    /// <summary>
    /// ノード名を取得（"Node_"プレフィックスを除去）
    /// </summary>
    private string GetNodeName(RectTransform nodeTransform)
    {
        if (nodeTransform == null) return null;
        
        string nodeObjName = nodeTransform.gameObject.name;
        if (nodeObjName.StartsWith("Node_"))
        {
            return nodeObjName.Replace("Node_", "");
        }
        return nodeObjName;
    }

    /// <summary>
    /// ノード名から対応するオブジェクトを見つける
    /// </summary>
    private void FindObjectForNode(string nodeName, RectTransform nodeTransform)
    {
        if (string.IsNullOrEmpty(nodeName))
        {
            return;
        }

        GameObject foundObject = null;
        Rigidbody rb = null;
        MonoBehaviour tpc = null;

        // ノード名に基づいてオブジェクトを探す
        if (nodeName == "Player")
        {
            // Playerオブジェクトを探す
            foundObject = GameObject.FindGameObjectWithTag("Player");
            if (foundObject == null)
            {
#if UNITY_2023_1_OR_NEWER
                GameObject[] allObjects = Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None);
#else
                GameObject[] allObjects = Object.FindObjectsOfType<GameObject>();
#endif
                foreach (GameObject obj in allObjects)
                {
                    if (obj.name.Contains("Player") || obj.name.Contains("PlayerCapsule") || obj.name.Contains("PlayerArmature"))
                    {
                        foundObject = obj;
                        break;
                    }
                }
            }
            
            if (foundObject != null)
            {
                // ThirdPersonControllerを取得
                var allMonoBehaviours = foundObject.GetComponents<MonoBehaviour>();
                foreach (var mb in allMonoBehaviours)
                {
                    if (mb.GetType().Name == "ThirdPersonController")
                    {
                        tpc = mb;
                        break;
                    }
                }
            }
        }
        else if (nodeName.StartsWith("RedBox"))
        {
            // RedBoxオブジェクトを探す
            foundObject = GameObject.Find(nodeName);
            if (foundObject == null)
            {
#if UNITY_2023_1_OR_NEWER
                Rigidbody[] allRigidbodies = Object.FindObjectsByType<Rigidbody>(FindObjectsSortMode.None);
#else
                Rigidbody[] allRigidbodies = Object.FindObjectsOfType<Rigidbody>();
#endif
                foreach (Rigidbody rigidbody in allRigidbodies)
                {
                    if (rigidbody.gameObject.name.StartsWith("RedBox"))
                    {
                        foundObject = rigidbody.gameObject;
                        rb = rigidbody;
                        break;
                    }
                }
            }
            else
            {
                rb = foundObject.GetComponent<Rigidbody>();
            }
        }
        else
        {
            // その他のオブジェクトを名前で探す
            foundObject = GameObject.Find(nodeName);
            if (foundObject != null)
            {
                rb = foundObject.GetComponent<Rigidbody>();
            }
        }

        // 見つかったオブジェクトを保存
        if (foundObject != null)
        {
            controllableObjects[nodeName] = foundObject;
            if (rb != null)
            {
                objectRigidbodies[nodeName] = rb;
            }
            if (tpc != null)
            {
                objectThirdPersonControllers[nodeName] = tpc;
            }
        }
    }

    /// <summary>
    /// オブジェクトを操作可能にする
    /// </summary>
    private void EnableObjectControl(string nodeName)
    {
        if (string.IsNullOrEmpty(nodeName))
        {
            Debug.LogWarning("RedBoxController: EnableObjectControl - nodeNameがnullまたは空です。");
            return;
        }
        
        if (!controllableObjects.ContainsKey(nodeName))
        {
            Debug.LogWarning($"RedBoxController: EnableObjectControl - {nodeName}がcontrollableObjectsに見つかりません。");
            return;
        }

        GameObject obj = controllableObjects[nodeName];
        
        if (obj == null)
        {
            Debug.LogWarning($"RedBoxController: EnableObjectControl - {nodeName}のGameObjectがnullです。");
            return;
        }
        
        // Bigノードと合体している場合、スケールを5倍にする
        if (IsMergedWithBig(nodeName))
        {
            // 元のスケールを保存（まだ保存されていない場合）
            if (!originalScales.ContainsKey(nodeName))
            {
                originalScales[nodeName] = obj.transform.localScale;
                
                // スケール変更前のBoundsを取得
                Bounds originalBounds = GetObjectBounds(obj);
                float originalBottomY = originalBounds.min.y;
                float originalHeight = originalBounds.size.y;
                float originalCenterY = originalBounds.center.y;
                
                // CharacterControllerの場合は、heightとcenterも調整
                CharacterController characterController = obj.GetComponent<CharacterController>();
                MonoBehaviour tpc = null;
                if (objectThirdPersonControllers.ContainsKey(nodeName))
                {
                    tpc = objectThirdPersonControllers[nodeName];
                }
                
                if (characterController != null)
                {
                    // 元の値を保存
                    if (!originalCharacterHeights.ContainsKey(nodeName))
                    {
                        originalCharacterHeights[nodeName] = characterController.height;
                        originalCharacterCenters[nodeName] = characterController.center;
                    }
                    
                    float originalCharacterHeight = originalCharacterHeights[nodeName];
                    Vector3 originalCharacterCenter = originalCharacterCenters[nodeName];
                    
                    characterController.height = originalCharacterHeight * 5f;
                    characterController.center = new Vector3(
                        originalCharacterCenter.x,
                        originalCharacterCenter.y * 5f,
                        originalCharacterCenter.z
                    );
                }
                
                // ThirdPersonControllerの場合は、GroundedOffsetとGroundedRadiusも調整
                if (tpc != null)
                {
                    // 元の値を保存
                    if (!originalGroundedOffsets.ContainsKey(nodeName))
                    {
                        var groundedOffsetField = tpc.GetType().GetField("GroundedOffset", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
                        var groundedRadiusField = tpc.GetType().GetField("GroundedRadius", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
                        
                        if (groundedOffsetField != null)
                        {
                            originalGroundedOffsets[nodeName] = (float)groundedOffsetField.GetValue(tpc);
                        }
                        if (groundedRadiusField != null)
                        {
                            originalGroundedRadiuses[nodeName] = (float)groundedRadiusField.GetValue(tpc);
                        }
                    }
                    
                    // GroundedOffsetとGroundedRadiusを5倍に調整
                    var groundedOffsetField2 = tpc.GetType().GetField("GroundedOffset", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
                    var groundedRadiusField2 = tpc.GetType().GetField("GroundedRadius", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
                    
                    if (groundedOffsetField2 != null && originalGroundedOffsets.ContainsKey(nodeName))
                    {
                        groundedOffsetField2.SetValue(tpc, originalGroundedOffsets[nodeName] * 5f);
                    }
                    if (groundedRadiusField2 != null && originalGroundedRadiuses.ContainsKey(nodeName))
                    {
                        groundedRadiusField2.SetValue(tpc, originalGroundedRadiuses[nodeName] * 5f);
                    }
                }
                
                // スケールを5倍に設定
                Vector3 originalScale = originalScales[nodeName];
                obj.transform.localScale = originalScale * 5f;
                
                // スケール変更後のBoundsを計算
                // スケールを変更すると、中心は変わらず、サイズだけが変わる
                float newHeight = originalHeight * 5f;
                float heightIncrease = newHeight - originalHeight;
                
                // スケール変更後の新しい底部のY座標を計算
                // 中心が変わらないので、底部は中心から newHeight/2 下に移動
                float newBottomY = originalCenterY - (newHeight / 2f);
                
                // 地面を検出（Raycast）
                RaycastHit hit;
                Vector3 rayStart = new Vector3(obj.transform.position.x, newBottomY, obj.transform.position.z);
                float rayDistance = Mathf.Max(heightIncrease + 1f, 10f); // 十分な距離
                
                if (Physics.Raycast(rayStart, Vector3.down, out hit, rayDistance))
                {
                    float groundY = hit.point.y;
                    // 新しい底部が地面の上に来るように位置を調整
                    float requiredBottomY = groundY + 0.01f; // 少し余裕を持たせる
                    float offsetY = requiredBottomY - newBottomY;
                    
                    // 位置を調整（Y座標を上げる）
                    obj.transform.position = new Vector3(
                        obj.transform.position.x,
                        obj.transform.position.y + offsetY,
                        obj.transform.position.z
                    );
                }
                else
                {
                    // 地面が見つからない場合でも、高さの増加分の半分だけ上に移動（中心を基準に）
                    obj.transform.position = new Vector3(
                        obj.transform.position.x,
                        obj.transform.position.y + heightIncrease / 2f,
                        obj.transform.position.z
                    );
                }
            }
            else
            {
                // 既にスケールが設定されている場合は、スケールのみ更新（位置は既に調整済み）
                obj.transform.localScale = originalScales[nodeName] * 5f;
                
                // CharacterControllerの場合は、heightとcenterも更新
                CharacterController characterController = obj.GetComponent<CharacterController>();
                if (characterController != null)
                {
                    if (originalCharacterHeights.ContainsKey(nodeName))
                    {
                        float originalCharacterHeight = originalCharacterHeights[nodeName];
                        Vector3 originalCharacterCenter = originalCharacterCenters[nodeName];
                        characterController.height = originalCharacterHeight * 5f;
                        characterController.center = new Vector3(
                            originalCharacterCenter.x,
                            originalCharacterCenter.y * 5f,
                            originalCharacterCenter.z
                        );
                    }
                }
            }
        }
        else
        {
            // Bigと合体していない場合、元のスケールに戻す
            if (originalScales.ContainsKey(nodeName))
            {
                obj.transform.localScale = originalScales[nodeName];
                
                // CharacterControllerの場合は、heightとcenterも元に戻す
                CharacterController characterController = obj.GetComponent<CharacterController>();
                MonoBehaviour tpc = null;
                if (objectThirdPersonControllers.ContainsKey(nodeName))
                {
                    tpc = objectThirdPersonControllers[nodeName];
                }
                
                if (characterController != null)
                {
                    if (originalCharacterHeights.ContainsKey(nodeName))
                    {
                        characterController.height = originalCharacterHeights[nodeName];
                        characterController.center = originalCharacterCenters[nodeName];
                        originalCharacterHeights.Remove(nodeName);
                        originalCharacterCenters.Remove(nodeName);
                    }
                }
                
                // ThirdPersonControllerの場合は、GroundedOffsetとGroundedRadiusも元に戻す
                if (tpc != null)
                {
                    var groundedOffsetField = tpc.GetType().GetField("GroundedOffset", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
                    var groundedRadiusField = tpc.GetType().GetField("GroundedRadius", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
                    
                    if (groundedOffsetField != null && originalGroundedOffsets.ContainsKey(nodeName))
                    {
                        groundedOffsetField.SetValue(tpc, originalGroundedOffsets[nodeName]);
                        originalGroundedOffsets.Remove(nodeName);
                    }
                    if (groundedRadiusField != null && originalGroundedRadiuses.ContainsKey(nodeName))
                    {
                        groundedRadiusField.SetValue(tpc, originalGroundedRadiuses[nodeName]);
                        originalGroundedRadiuses.Remove(nodeName);
                    }
                }
                
                originalScales.Remove(nodeName);
            }
        }
        
        // Rigidbodyを持つオブジェクトの場合（RedBoxなど）
        if (objectRigidbodies.ContainsKey(nodeName))
        {
            // 移動とジャンプを処理
            MoveObjectWithRigidbody(nodeName);
        }
        // ThirdPersonControllerを持つオブジェクトの場合（Playerなど）
        else if (objectThirdPersonControllers.ContainsKey(nodeName))
        {
            MonoBehaviour tpc = objectThirdPersonControllers[nodeName];
            if (tpc != null)
            {
                if (!tpc.enabled)
                {
                    tpc.enabled = true;
                    Debug.Log($"RedBoxController: EnableObjectControl - {nodeName}のThirdPersonControllerを有効化しました。");
                }
                else if (Time.frameCount <= 5)
                {
                    Debug.Log($"RedBoxController: EnableObjectControl - {nodeName}のThirdPersonControllerは既に有効です。");
                }
                
                // HighJumpingノードと合体している場合、JumpHeightを1.5倍にする
                // Bigノードと合体している場合、JumpHeightを3倍にする
                // 両方合体している場合、JumpHeightを6倍にする
                bool isMergedWithHighJumping = IsMergedWithHighJumping(nodeName);
                bool isMergedWithBig = IsMergedWithBig(nodeName);
                
                var jumpHeightField = tpc.GetType().GetField("JumpHeight");
                if (jumpHeightField != null)
                {
                    float currentJumpHeight = (float)jumpHeightField.GetValue(tpc);
                    
                    // 元のジャンプ高さを保存（まだ保存されていない場合、または元の値が0の場合）
                    if (!originalJumpHeights.ContainsKey(nodeName) || originalJumpHeights[nodeName] == 0f)
                    {
                        // 現在の値が異常に大きい場合（既に倍率が適用されている場合）は、元の値として保存しない
                        // 通常のジャンプ高さの範囲（0.1～10.0）内の場合のみ保存
                        if (currentJumpHeight >= 0.1f && currentJumpHeight <= 10.0f)
                        {
                            originalJumpHeights[nodeName] = currentJumpHeight;
                        }
                        else
                        {
                            // 異常に大きい値の場合は、デフォルト値（1.2f）を保存
                            originalJumpHeights[nodeName] = 1.2f;
                        }
                    }
                    
                    float originalJumpHeightForNode = originalJumpHeights[nodeName];
                    
                    if (isMergedWithHighJumping && isMergedWithBig)
                    {
                        // 両方合体している場合、JumpHeightを6倍にする
                        if (originalJumpHeightForNode > 0f)
                        {
                            jumpHeightField.SetValue(tpc, originalJumpHeightForNode * 6f);
                        }
                    }
                    else if (isMergedWithHighJumping)
                    {
                        // HighJumpingのみ合体している場合、JumpHeightを1.5倍にする
                        if (originalJumpHeightForNode > 0f)
                        {
                            jumpHeightField.SetValue(tpc, originalJumpHeightForNode * 1.5f);
                        }
                    }
                    else if (isMergedWithBig)
                    {
                        // Bigのみ合体している場合、JumpHeightを3倍にする
                        if (originalJumpHeightForNode > 0f)
                        {
                            jumpHeightField.SetValue(tpc, originalJumpHeightForNode * 3f);
                        }
                    }
                    else
                    {
                        // 合体していない場合、元の値に戻す
                        if (originalJumpHeightForNode > 0f)
                        {
                            jumpHeightField.SetValue(tpc, originalJumpHeightForNode);
                        }
                    }
                }
                
                // HighJumpingの場合は常にジャンプするように設定
                if (nodeName == "HighJumping" || nodeName.StartsWith("HighJumping"))
                {
                    // StarterAssetsInputsコンポーネントを取得してjumpを常にtrueに設定
                    var starterAssetsInputs = obj.GetComponent<StarterAssets.StarterAssetsInputs>();
                    if (starterAssetsInputs != null)
                    {
                        starterAssetsInputs.jump = true;
                    }
                    else
                    {
                        // StarterAssetsInputsが見つからない場合、リフレクションで_inputフィールドを取得
                        var inputField = tpc.GetType().GetField("_input", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                        if (inputField != null)
                        {
                            var inputObj = inputField.GetValue(tpc);
                            if (inputObj != null)
                            {
                                var jumpField = inputObj.GetType().GetField("jump");
                                if (jumpField != null)
                                {
                                    jumpField.SetValue(inputObj, true);
                                }
                            }
                        }
                    }
                }
            }
            else
            {
                Debug.LogWarning($"RedBoxController: EnableObjectControl - {nodeName}のThirdPersonControllerがnullです。");
            }
        }
        else
        {
            Debug.LogWarning($"RedBoxController: EnableObjectControl - {nodeName}の操作コンポーネントが見つかりません。");
        }
    }
    
    /// <summary>
    /// 他のオブジェクトを操作不可能にする
    /// </summary>
    private void DisableOtherObjects(string exceptNodeName)
    {
        foreach (var kvp in controllableObjects)
        {
            if (kvp.Key == exceptNodeName)
            {
                continue;
            }
            
            DisableObject(kvp.Key);
        }
    }
    
    /// <summary>
    /// すべてのオブジェクトを操作不可能にする
    /// </summary>
    private void DisableAllObjects()
    {
        foreach (var kvp in controllableObjects)
        {
            DisableObject(kvp.Key);
        }
    }
    
    /// <summary>
    /// オブジェクトを操作不可能にする
    /// </summary>
    private void DisableObject(string nodeName)
    {
        if (string.IsNullOrEmpty(nodeName) || !controllableObjects.ContainsKey(nodeName))
        {
            return;
        }
        
        // Bigと合体していない場合、スケールを元に戻す
        if (!IsMergedWithBig(nodeName))
        {
            if (controllableObjects.ContainsKey(nodeName))
            {
                GameObject obj = controllableObjects[nodeName];
                if (obj != null && originalScales.ContainsKey(nodeName))
                {
                    obj.transform.localScale = originalScales[nodeName];
                    originalScales.Remove(nodeName);
                }
            }
        }

        // Rigidbodyを持つオブジェクトの場合
        if (objectRigidbodies.ContainsKey(nodeName))
        {
            Rigidbody rb = objectRigidbodies[nodeName];
            if (rb != null)
            {
                // XZ方向の速度を0にする（Y方向は重力のため保持）
#if UNITY_6000_0_OR_NEWER
                Vector3 currentVelocity = rb.linearVelocity;
                rb.linearVelocity = new Vector3(0f, currentVelocity.y, 0f);
#else
                Vector3 currentVelocity = rb.velocity;
                rb.velocity = new Vector3(0f, currentVelocity.y, 0f);
#endif
            }
        }
        // ThirdPersonControllerを持つオブジェクトの場合
        else if (objectThirdPersonControllers.ContainsKey(nodeName))
        {
            MonoBehaviour tpc = objectThirdPersonControllers[nodeName];
            if (tpc != null)
            {
                // 合体が解除された場合、ジャンプ高さを元の値に戻す
                if (!IsMergedWithHighJumping(nodeName) && !IsMergedWithBig(nodeName))
                {
                    var jumpHeightField = tpc.GetType().GetField("JumpHeight");
                    if (jumpHeightField != null && originalJumpHeights.ContainsKey(nodeName))
                    {
                        float originalJumpHeightForNode = originalJumpHeights[nodeName];
                        if (originalJumpHeightForNode > 0f)
                        {
                            jumpHeightField.SetValue(tpc, originalJumpHeightForNode);
                        }
                    }
                }
                
                if (tpc.enabled)
                {
                    tpc.enabled = false;
                }
            }
        }
    }
    
    /// <summary>
    /// Rigidbodyを持つオブジェクトをWASD + Spaceで操作する
    /// </summary>
    private void MoveObjectWithRigidbody(string nodeName)
    {
        if (string.IsNullOrEmpty(nodeName) || !controllableObjects.ContainsKey(nodeName) || !objectRigidbodies.ContainsKey(nodeName))
        {
            return;
        }

        GameObject obj = controllableObjects[nodeName];
        Rigidbody rb = objectRigidbodies[nodeName];
        
        if (obj == null || rb == null)
        {
            return;
        }

#if ENABLE_INPUT_SYSTEM
        if (keyboard == null)
        {
            return;
        }

        Vector3 moveDirection = Vector3.zero;

        // WASDキーの入力を取得
        if (keyboard.wKey.isPressed)
        {
            moveDirection += Vector3.forward;
        }
        if (keyboard.sKey.isPressed)
        {
            moveDirection += Vector3.back;
        }
        if (keyboard.aKey.isPressed)
        {
            moveDirection += Vector3.left;
        }
        if (keyboard.dKey.isPressed)
        {
            moveDirection += Vector3.right;
        }

        // 地面に接触しているか確認（Raycast）
        // BlueBoxの場合は下面の全域をチェックするため、複数のRaycastを実行
        bool isGrounded = false;
        RaycastHit hit;
        
        if (nodeName.StartsWith("BlueBox"))
        {
            // BlueBoxのBoundsを取得
            Bounds bounds = GetObjectBounds(obj);
            Vector3 bottomCenter = new Vector3(bounds.center.x, bounds.min.y, bounds.center.z);
            
            // BlueBoxの下面付近に他のオブジェクトがあるか確認（OverlapBoxを使用）
            // 下面から少し下方向にOverlapBoxを配置して、他のオブジェクトとの接触を検出
            Vector3 boxSize = new Vector3(bounds.size.x * 0.9f, 0.15f, bounds.size.z * 0.9f); // 少し小さめのサイズで検出
            Vector3 boxCenter = bottomCenter - new Vector3(0, 0.05f, 0); // 下面から少し下
            
            // OverlapBoxで他のオブジェクトとの接触を検出
            Collider[] overlappingColliders = Physics.OverlapBox(
                boxCenter,
                boxSize / 2f,
                Quaternion.identity
            );
            
            // BlueBox自身を除外して、他のオブジェクトとの接触を確認
            foreach (Collider col in overlappingColliders)
            {
                if (col != null && col.gameObject != obj)
                {
                    // 接触しているオブジェクトがBlueBoxの下側にあるか確認
                    Bounds colBounds = col.bounds;
                    if (colBounds.max.y < obj.transform.position.y)
                    {
                        isGrounded = true;
                        break;
                    }
                }
            }
            
            // OverlapBoxで検出できない場合、Raycastも試す（地面との接触を確認）
            if (!isGrounded)
            {
                float checkDistance = groundCheckDistance + 0.3f; // 距離を長くして、他のオブジェクトの上に乗っている場合も検出
                int checkPoints = 5;
                float stepX = bounds.size.x / (checkPoints - 1);
                float stepZ = bounds.size.z / (checkPoints - 1);
                int groundedCount = 0;
                
                for (int i = 0; i < checkPoints; i++)
                {
                    for (int j = 0; j < checkPoints; j++)
                    {
                        float offsetX = (i - (checkPoints - 1) / 2f) * stepX;
                        float offsetZ = (j - (checkPoints - 1) / 2f) * stepZ;
                        Vector3 checkPoint = bottomCenter + new Vector3(offsetX, 0, offsetZ);
                        
                        if (Physics.Raycast(checkPoint, Vector3.down, out hit, checkDistance))
                        {
                            // 自分自身以外のオブジェクトと接触している場合
                            if (hit.collider != null && hit.collider.gameObject != obj)
                            {
                                groundedCount++;
                            }
                        }
                    }
                }
                
                // 1点以上が接地していれば接地と判定（他のオブジェクトの上に乗っている場合）
                isGrounded = groundedCount > 0;
            }
        }
        else
        {
            // 通常のオブジェクトは中心から1点のみチェック
            Vector3 rayStart = obj.transform.position;
            float rayDistance = groundCheckDistance + 0.1f;
            
            if (Physics.Raycast(rayStart, Vector3.down, out hit, rayDistance))
            {
                isGrounded = true;
            }
        }
        
        // HighJumpingの場合は常にジャンプし続ける
        bool shouldJump = false;
        if (nodeName == "HighJumping" || nodeName.StartsWith("HighJumping"))
        {
            // HighJumpingの場合は地面に接触しているときに常にジャンプ
            // より確実にするため、Y方向の速度が低い場合（着地している）にジャンプ
#if UNITY_6000_0_OR_NEWER
            float currentYVelocity = rb.linearVelocity.y;
#else
            float currentYVelocity = rb.velocity.y;
#endif
            // 地面に接触していて、Y方向の速度が低い場合（着地しているか、下降中）にジャンプ
            shouldJump = isGrounded && currentYVelocity <= 0.1f;
        }
        else
        {
            // 通常の場合はSpaceキーが押された時のみジャンプ（地面に接触している必要がある）
            shouldJump = keyboard.spaceKey.wasPressedThisFrame && isGrounded;
        }
        
        // ジャンプ処理
        if (shouldJump)
        {
            // HighJumpingノードと合体している場合、ジャンプ力を1.5倍にする
            // Bigノードと合体している場合、ジャンプ力を3倍にする
            // 両方合体している場合、ジャンプ力を6倍にする
            float actualJumpForce = jumpForce;
            bool isMergedWithHighJumping = IsMergedWithHighJumping(nodeName);
            bool isMergedWithBig = IsMergedWithBig(nodeName);
            
            if (isMergedWithHighJumping && isMergedWithBig)
            {
                actualJumpForce = jumpForce * 6f;
            }
            else if (isMergedWithHighJumping)
            {
                actualJumpForce = jumpForce * 1.5f;
            }
            else if (isMergedWithBig)
            {
                actualJumpForce = jumpForce * 3f;
            }
            
#if UNITY_6000_0_OR_NEWER
            Vector3 currentVelocity = rb.linearVelocity;
            currentVelocity.y = actualJumpForce;
            rb.linearVelocity = currentVelocity;
#else
            Vector3 currentVelocity = rb.velocity;
            currentVelocity.y = actualJumpForce;
            rb.velocity = currentVelocity;
#endif
        }
        
        // 移動方向を正規化
        if (moveDirection.magnitude > 0.1f)
        {
            moveDirection.Normalize();
            
            // カメラの向きを考慮して移動方向を変換
            Camera mainCamera = Camera.main;
            if (mainCamera != null)
            {
                Vector3 cameraForward = mainCamera.transform.forward;
                Vector3 cameraRight = mainCamera.transform.right;
                
                cameraForward.y = 0f;
                cameraRight.y = 0f;
                cameraForward.Normalize();
                cameraRight.Normalize();
                
                Vector3 worldMoveDirection = cameraForward * moveDirection.z + cameraRight * moveDirection.x;
                
#if UNITY_6000_0_OR_NEWER
                Vector3 currentVelocity = rb.linearVelocity;
                rb.linearVelocity = new Vector3(worldMoveDirection.x * moveSpeed, currentVelocity.y, worldMoveDirection.z * moveSpeed);
#else
                Vector3 currentVelocity = rb.velocity;
                rb.velocity = new Vector3(worldMoveDirection.x * moveSpeed, currentVelocity.y, worldMoveDirection.z * moveSpeed);
#endif
            }
            else
            {
#if UNITY_6000_0_OR_NEWER
                Vector3 currentVelocity = rb.linearVelocity;
                rb.linearVelocity = new Vector3(moveDirection.x * moveSpeed, currentVelocity.y, moveDirection.z * moveSpeed);
#else
                Vector3 currentVelocity = rb.velocity;
                rb.velocity = new Vector3(moveDirection.x * moveSpeed, currentVelocity.y, moveDirection.z * moveSpeed);
#endif
            }
        }
        else
        {
#if UNITY_6000_0_OR_NEWER
            Vector3 currentVelocity = rb.linearVelocity;
            rb.linearVelocity = new Vector3(0f, currentVelocity.y, 0f);
#else
            Vector3 currentVelocity = rb.velocity;
            rb.velocity = new Vector3(0f, currentVelocity.y, 0f);
#endif
        }
#else
        // Input Systemが無効な場合は従来のInputクラスを使用
        Vector3 moveDirection = Vector3.zero;

        if (Input.GetKey(KeyCode.W))
        {
            moveDirection += Vector3.forward;
        }
        if (Input.GetKey(KeyCode.S))
        {
            moveDirection += Vector3.back;
        }
        if (Input.GetKey(KeyCode.A))
        {
            moveDirection += Vector3.left;
        }
        if (Input.GetKey(KeyCode.D))
        {
            moveDirection += Vector3.right;
        }
        
        bool isGrounded = false;
        RaycastHit hit;
        
        if (nodeName.StartsWith("BlueBox"))
        {
            // BlueBoxのBoundsを取得
            Bounds bounds = GetObjectBounds(obj);
            Vector3 bottomCenter = new Vector3(bounds.center.x, bounds.min.y, bounds.center.z);
            
            // BlueBoxの下面付近に他のオブジェクトがあるか確認（OverlapBoxを使用）
            // 下面から少し下方向にOverlapBoxを配置して、他のオブジェクトとの接触を検出
            Vector3 boxSize = new Vector3(bounds.size.x * 0.9f, 0.15f, bounds.size.z * 0.9f); // 少し小さめのサイズで検出
            Vector3 boxCenter = bottomCenter - new Vector3(0, 0.05f, 0); // 下面から少し下
            
            // OverlapBoxで他のオブジェクトとの接触を検出
            Collider[] overlappingColliders = Physics.OverlapBox(
                boxCenter,
                boxSize / 2f,
                Quaternion.identity
            );
            
            // BlueBox自身を除外して、他のオブジェクトとの接触を確認
            foreach (Collider col in overlappingColliders)
            {
                if (col != null && col.gameObject != obj)
                {
                    // 接触しているオブジェクトがBlueBoxの下側にあるか確認
                    Bounds colBounds = col.bounds;
                    if (colBounds.max.y < obj.transform.position.y)
                    {
                        isGrounded = true;
                        break;
                    }
                }
            }
            
            // OverlapBoxで検出できない場合、Raycastも試す（地面との接触を確認）
            if (!isGrounded)
            {
                float checkDistance = groundCheckDistance + 0.3f; // 距離を長くして、他のオブジェクトの上に乗っている場合も検出
                int checkPoints = 5;
                float stepX = bounds.size.x / (checkPoints - 1);
                float stepZ = bounds.size.z / (checkPoints - 1);
                int groundedCount = 0;
                
                for (int i = 0; i < checkPoints; i++)
                {
                    for (int j = 0; j < checkPoints; j++)
                    {
                        float offsetX = (i - (checkPoints - 1) / 2f) * stepX;
                        float offsetZ = (j - (checkPoints - 1) / 2f) * stepZ;
                        Vector3 checkPoint = bottomCenter + new Vector3(offsetX, 0, offsetZ);
                        
                        if (Physics.Raycast(checkPoint, Vector3.down, out hit, checkDistance))
                        {
                            // 自分自身以外のオブジェクトと接触している場合
                            if (hit.collider != null && hit.collider.gameObject != obj)
                            {
                                groundedCount++;
                            }
                        }
                    }
                }
                
                // 1点以上が接地していれば接地と判定（他のオブジェクトの上に乗っている場合）
                isGrounded = groundedCount > 0;
            }
        }
        else
        {
            // 通常のオブジェクトは中心から1点のみチェック
            Vector3 rayStart = obj.transform.position;
            float rayDistance = groundCheckDistance + 0.1f;
            
            if (Physics.Raycast(rayStart, Vector3.down, out hit, rayDistance))
            {
                isGrounded = true;
            }
        }
        
        // HighJumpingの場合は常にジャンプし続ける
        bool shouldJump = false;
        if (nodeName == "HighJumping" || nodeName.StartsWith("HighJumping"))
        {
            // HighJumpingの場合は地面に接触しているときに常にジャンプ
            // より確実にするため、Y方向の速度が低い場合（着地している）にジャンプ
            float currentYVelocity = rb.velocity.y;
            // 地面に接触していて、Y方向の速度が低い場合（着地しているか、下降中）にジャンプ
            shouldJump = isGrounded && currentYVelocity <= 0.1f;
        }
        else
        {
            // 通常の場合はSpaceキーが押された時のみジャンプ
            shouldJump = Input.GetKeyDown(KeyCode.Space) && isGrounded;
        }
        
        if (shouldJump)
        {
            Vector3 currentVelocity = rb.velocity;
            currentVelocity.y = jumpForce;
            rb.velocity = currentVelocity;
        }

        if (moveDirection.magnitude > 0.1f)
        {
            moveDirection.Normalize();
            
            Camera mainCamera = Camera.main;
            if (mainCamera != null)
            {
                Vector3 cameraForward = mainCamera.transform.forward;
                Vector3 cameraRight = mainCamera.transform.right;
                
                cameraForward.y = 0f;
                cameraRight.y = 0f;
                cameraForward.Normalize();
                cameraRight.Normalize();
                
                Vector3 worldMoveDirection = cameraForward * moveDirection.z + cameraRight * moveDirection.x;
                
                Vector3 currentVelocity = rb.velocity;
                rb.velocity = new Vector3(worldMoveDirection.x * moveSpeed, currentVelocity.y, worldMoveDirection.z * moveSpeed);
            }
            else
            {
                Vector3 currentVelocity = rb.velocity;
                rb.velocity = new Vector3(moveDirection.x * moveSpeed, currentVelocity.y, moveDirection.z * moveSpeed);
            }
        }
        else
        {
            Vector3 currentVelocity = rb.velocity;
            rb.velocity = new Vector3(0f, currentVelocity.y, 0f);
        }
#endif
    }
    
    /// <summary>
    /// シーン内のHighJumpingオブジェクトを検出してリストに追加
    /// </summary>
    private void FindHighJumpingObjects()
    {
        highJumpingObjects.Clear();
        
#if UNITY_2023_1_OR_NEWER
        GameObject[] allObjects = Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None);
#else
        GameObject[] allObjects = Object.FindObjectsOfType<GameObject>();
#endif
        
        foreach (GameObject obj in allObjects)
        {
            if (obj.name == "HighJumping" || obj.name.StartsWith("HighJumping"))
            {
                highJumpingObjects.Add(obj);
            }
        }
    }
    
    /// <summary>
    /// HighJumpingオブジェクトを常にジャンプさせる（メニューの合体状態に関係なく）
    /// </summary>
    private void HandleHighJumpingObjects()
    {
        // 定期的にHighJumpingオブジェクトを再検索（新しく追加された場合に備えて）
        if (Time.frameCount % 300 == 0) // 5秒ごと（60fpsの場合）
        {
            FindHighJumpingObjects();
        }
        
        foreach (GameObject highJumpingObj in highJumpingObjects)
        {
            if (highJumpingObj == null)
            {
                continue;
            }
            
            // Rigidbodyを持つHighJumpingオブジェクトの場合
            Rigidbody rb = highJumpingObj.GetComponent<Rigidbody>();
            if (rb != null)
            {
                // 地面に接触しているか確認（Raycast）
                bool isGrounded = false;
                RaycastHit hit;
                Vector3 rayStart = highJumpingObj.transform.position;
                float rayDistance = groundCheckDistance + 0.1f;
                
                if (Physics.Raycast(rayStart, Vector3.down, out hit, rayDistance))
                {
                    isGrounded = true;
                }
                
                // 地面に接触していて、Y方向の速度が低い場合（着地しているか、下降中）にジャンプ
                // HighJumpingのジャンプ高さを2倍にする
                float highJumpForce = jumpForce * 2f;
#if UNITY_6000_0_OR_NEWER
                float currentYVelocity = rb.linearVelocity.y;
                if (isGrounded && currentYVelocity <= 0.1f)
                {
                    Vector3 currentVelocity = rb.linearVelocity;
                    currentVelocity.y = highJumpForce;
                    rb.linearVelocity = currentVelocity;
                }
#else
                float currentYVelocity = rb.velocity.y;
                if (isGrounded && currentYVelocity <= 0.1f)
                {
                    Vector3 currentVelocity = rb.velocity;
                    currentVelocity.y = highJumpForce;
                    rb.velocity = currentVelocity;
                }
#endif
            }
            // ThirdPersonControllerを持つHighJumpingオブジェクトの場合
            else
            {
                // ThirdPersonControllerを検索
                MonoBehaviour[] allMonoBehaviours = highJumpingObj.GetComponents<MonoBehaviour>();
                MonoBehaviour tpc = null;
                foreach (var mb in allMonoBehaviours)
                {
                    if (mb.GetType().Name == "ThirdPersonController")
                    {
                        tpc = mb;
                        break;
                    }
                }
                
                if (tpc != null)
                {
                    
                    
                    // StarterAssetsInputsコンポーネントを取得してjumpを常にtrueに設定
                    var starterAssetsInputs = highJumpingObj.GetComponent<StarterAssets.StarterAssetsInputs>();
                    if (starterAssetsInputs != null)
                    {
                        starterAssetsInputs.jump = true; // 毎フレームtrueに設定して常にジャンプ
                    }
                    else
                    {
                        // StarterAssetsInputsが見つからない場合、リフレクションで_inputフィールドを取得
                        var inputField = tpc.GetType().GetField("_input", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                        if (inputField != null)
                        {
                            var inputObj = inputField.GetValue(tpc);
                            if (inputObj != null)
                            {
                                var jumpField = inputObj.GetType().GetField("jump");
                                if (jumpField != null)
                                {
                                    jumpField.SetValue(inputObj, true); // 毎フレームtrueに設定して常にジャンプ
                                }
                            }
                        }
                    }
                }
            }
        }
    }
    
    /// <summary>
    /// オブジェクトのBoundsを取得する（ColliderまたはRendererから）
    /// </summary>
    private Bounds GetObjectBounds(GameObject obj)
    {
        Bounds bounds = new Bounds();
        bool hasBounds = false;
        
        // ColliderからBoundsを取得
        Collider collider = obj.GetComponent<Collider>();
        if (collider != null)
        {
            bounds = collider.bounds;
            hasBounds = true;
        }
        else
        {
            // Colliderがない場合はRendererから取得
            Renderer renderer = obj.GetComponent<Renderer>();
            if (renderer == null)
            {
                renderer = obj.GetComponentInChildren<Renderer>();
            }
            if (renderer != null)
            {
                bounds = renderer.bounds;
                hasBounds = true;
            }
        }
        
        // Boundsが取得できなかった場合、デフォルトのBoundsを返す
        if (!hasBounds)
        {
            bounds = new Bounds(obj.transform.position, Vector3.one);
        }
        
        return bounds;
    }

}

