using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System.Collections.Generic;

/// <summary>
/// ノードをドラッグ可能にするコンポーネント
/// </summary>
[RequireComponent(typeof(RectTransform))]
public class DraggableNode : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IDropHandler, IPointerClickHandler
{
    private RectTransform rectTransform;
    private Canvas canvas;
    private CanvasGroup canvasGroup;
    public Transform originalParent; // OnEndDragで合体したノードから参照するためpublicに
    private int originalSiblingIndex;
    private VerticalLayoutGroup layoutGroup;
    private bool isDragging = false;
    private NodeMergeManager mergeManager;
    
    // ダブルクリック検出用
    private float lastClickTime = 0f;
    private const float doubleClickTime = 0.3f; // ダブルクリックと判定する時間（秒）

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        
        // Canvasを取得（親階層を遡って探す）
        canvas = GetComponentInParent<Canvas>();
        
        // CanvasGroupを追加（ドラッグ中に他のUI要素との相互作用を制御）
        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }

        // NodeMergeManagerを取得
        mergeManager = NodeMergeManager.Instance;
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        isDragging = true;
        originalParent = rectTransform.parent;
        originalSiblingIndex = rectTransform.GetSiblingIndex();
        
        // レイアウトグループを無効化（ドラッグ中は自動レイアウトを無効にする）
        layoutGroup = originalParent.GetComponent<VerticalLayoutGroup>();
        if (layoutGroup != null)
        {
            layoutGroup.enabled = false;
        }
        
        // ドラッグ中は他のUI要素との相互作用を無効化
        canvasGroup.alpha = 0.8f;
        canvasGroup.blocksRaycasts = false;
        
        // ノードをCanvasの最前面に移動（ドラッグ中に見やすくするため）
        // 青ノード（親ノード）が常に一番前に表示されるようにする
        if (mergeManager != null)
        {
            // このノードが親ノード（青ノード）の場合
            if (mergeManager.IsParentNode(rectTransform))
            {
                // まず子ノード（黄色ノード）を移動（後ろに配置）
                List<RectTransform> children = mergeManager.GetChildNodes(rectTransform);
                foreach (RectTransform child in children)
                {
                    if (child != null)
                    {
                        child.SetParent(canvas.transform);
                        child.SetAsLastSibling();
                    }
                }
                // 最後に親ノード（青ノード）を移動（最前面に配置）
                rectTransform.SetParent(canvas.transform);
                rectTransform.SetAsLastSibling();
            }
            // このノードが子ノード（黄色ノード）の場合
            else if (mergeManager.IsMerged(rectTransform))
            {
                RectTransform parentNode = mergeManager.GetParentNode(rectTransform);
                if (parentNode != null)
                {
                    // まず子ノード（黄色ノード）を移動（後ろに配置）
                    rectTransform.SetParent(canvas.transform);
                    rectTransform.SetAsLastSibling();
                    
                    // 他の子ノードも移動
                    List<RectTransform> siblings = mergeManager.GetChildNodes(parentNode);
                    foreach (RectTransform sibling in siblings)
                    {
                        if (sibling != null && sibling != rectTransform)
                        {
                            sibling.SetParent(canvas.transform);
                            sibling.SetAsLastSibling();
                        }
                    }
                    
                    // 最後に親ノード（青ノード）を移動（最前面に配置）
                    parentNode.SetParent(canvas.transform);
                    parentNode.SetAsLastSibling();
                }
            }
            else
            {
                // 合体していない場合は通常通り
                rectTransform.SetParent(canvas.transform);
                rectTransform.SetAsLastSibling();
            }
        }
        else
        {
            rectTransform.SetParent(canvas.transform);
            rectTransform.SetAsLastSibling();
        }
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (canvas == null)
        {
            return;
        }
        
        // マウスの位置に合わせてノードを移動（Canvas座標系）
        Vector2 canvasLocalPoint;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvas.transform as RectTransform,
            eventData.position,
            canvas.worldCamera,
            out canvasLocalPoint);
        
        rectTransform.localPosition = canvasLocalPoint;

        // 合体しているノードも一緒に移動（Canvas座標系で統一）
        if (mergeManager != null)
        {
            // このノードが親ノードの場合、子ノードも一緒に移動
            List<RectTransform> children = mergeManager.GetChildNodes(rectTransform);
            int childCount = children.Count;
            
            for (int i = 0; i < childCount; i++)
            {
                RectTransform child = children[i];
                if (child != null)
                {
                    // 複数の子ノードを円形に配置するためのオフセットを計算
                    float angleStep = 360f / Mathf.Max(childCount, 1);
                    float angle = angleStep * i * Mathf.Deg2Rad;
                    float radius = 40f;
                    
                    Vector2 offset = new Vector2(
                        Mathf.Cos(angle) * radius,
                        Mathf.Sin(angle) * radius
                    );
                    
                    Vector2 childPos = canvasLocalPoint + offset;
                    child.localPosition = childPos;
                }
            }

            // このノードが合体している子ノードの場合、親ノードも一緒に移動
            RectTransform parentNode = mergeManager.GetParentNode(rectTransform);
            if (parentNode != null)
            {
                // 複数の子ノードがある場合、現在の子ノードのインデックスを取得
                List<RectTransform> siblings = mergeManager.GetChildNodes(parentNode);
                int currentIndex = siblings.IndexOf(rectTransform);
                
                float angleStep = 360f / Mathf.Max(siblings.Count, 1);
                float angle = angleStep * currentIndex * Mathf.Deg2Rad;
                float radius = 40f;
                
                Vector2 offset = new Vector2(
                    Mathf.Cos(angle) * radius,
                    Mathf.Sin(angle) * radius
                );
                
                // 親ノードの位置を計算（子ノードの位置からオフセットを引く）
                Vector2 parentPos = canvasLocalPoint - offset;
                parentNode.localPosition = parentPos;
            }
        }
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        isDragging = false;
        
        // ドラッグ終了時の位置を取得（Canvas座標系）
        Vector2 localPoint;
        RectTransform canvasRect = canvas.transform as RectTransform;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvasRect,
            eventData.position,
            canvas.worldCamera,
            out localPoint);
        
        // 元の親に戻す
        rectTransform.SetParent(originalParent);
        
        // ローカル座標に変換（Content座標系）
        RectTransform parentRect = originalParent as RectTransform;
        Vector2 finalPosition;
        if (parentRect != null)
        {
            Vector2 parentLocalPoint;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                parentRect,
                eventData.position,
                canvas.worldCamera,
                out parentLocalPoint);
            rectTransform.localPosition = parentLocalPoint;
            finalPosition = parentLocalPoint;
        }
        else
        {
            finalPosition = rectTransform.localPosition;
        }

        // 合体しているノードも元の親に戻し、位置を更新
        if (mergeManager != null && parentRect != null)
        {
            // このノードが親ノード（青ノード）の場合
            if (mergeManager.IsParentNode(rectTransform))
            {
                // まず子ノード（黄色ノード）を元の親に戻し、位置を更新
                List<RectTransform> children = mergeManager.GetChildNodes(rectTransform);
                int childCount = children.Count;
                
                for (int i = 0; i < childCount; i++)
                {
                    RectTransform child = children[i];
                    if (child != null)
                    {
                        // 子ノードを元の親に戻す
                        child.SetParent(parentRect);
                        
                        // 複数の子ノードを円形に配置するためのオフセットを計算
                        float angleStep = 360f / Mathf.Max(childCount, 1);
                        float angle = angleStep * i * Mathf.Deg2Rad;
                        float radius = 40f;
                        
                        Vector2 offset = new Vector2(
                            Mathf.Cos(angle) * radius,
                            Mathf.Sin(angle) * radius
                        );
                        
                        // 子ノードの位置を更新（親ノードの位置 + オフセット）
                        // finalPositionは既にContent座標系なので、そのままオフセットを加算
                        Vector2 childPos = finalPosition + offset;
                        child.localPosition = childPos;
                    }
                }
                
                // 最後に親ノード（青ノード）を元の親に戻す（最前面に配置される）
                rectTransform.SetParent(parentRect);
                rectTransform.SetAsLastSibling();
            }
            // このノードが合体している子ノード（黄色ノード）の場合
            else if (mergeManager.IsMerged(rectTransform))
            {
                RectTransform parentNode = mergeManager.GetParentNode(rectTransform);
                if (parentNode != null)
                {
                    // 親ノードのDraggableNodeコンポーネントを取得してoriginalParentを取得
                    DraggableNode parentDraggable = parentNode.GetComponent<DraggableNode>();
                    Transform parentOriginalParent = null;
                    if (parentDraggable != null && parentDraggable.originalParent != null)
                    {
                        parentOriginalParent = parentDraggable.originalParent;
                    }
                    else
                    {
                        parentOriginalParent = parentRect;
                    }
                    
                    // まず子ノード（黄色ノード）を元の親に戻す
                    rectTransform.SetParent(parentRect);
                    
                    // 他の子ノードも元の親に戻す
                    List<RectTransform> siblings = mergeManager.GetChildNodes(parentNode);
                    int currentIndex = siblings.IndexOf(rectTransform);
                    
                    for (int i = 0; i < siblings.Count; i++)
                    {
                        RectTransform sibling = siblings[i];
                        if (sibling != null && sibling != rectTransform)
                        {
                            sibling.SetParent(parentRect);
                            
                            float angleStep = 360f / Mathf.Max(siblings.Count, 1);
                            float angle = angleStep * i * Mathf.Deg2Rad;
                            float radius = 40f;
                            
                            Vector2 offset = new Vector2(
                                Mathf.Cos(angle) * radius,
                                Mathf.Sin(angle) * radius
                            );
                            
                            Vector2 siblingPos = finalPosition + offset;
                            sibling.localPosition = siblingPos;
                        }
                    }
                    
                    // 最後に親ノード（青ノード）を元の親に戻す（最前面に配置される）
                    parentNode.SetParent(parentOriginalParent);
                    
                    float angleStep2 = 360f / Mathf.Max(siblings.Count, 1);
                    float angle2 = angleStep2 * currentIndex * Mathf.Deg2Rad;
                    float radius2 = 40f;
                    
                    Vector2 offset2 = new Vector2(
                        Mathf.Cos(angle2) * radius2,
                        Mathf.Sin(angle2) * radius2
                    );
                    
                    // 親ノードの位置を更新（子ノードの位置からオフセットを引く）
                    // finalPositionは既にContent座標系なので、そのままオフセットを減算
                    Vector2 parentPos = finalPosition - offset2;
                    
                    // 親ノードの位置を設定（親の親がContentなのでlocalPositionを使用）
                    parentNode.localPosition = parentPos;
                    parentNode.SetAsLastSibling();
                }
            }
        }
        
        // レイアウトグループは無効のまま（手動配置を保持するため）
        // 必要に応じて再有効化する場合は以下のコメントを外す
        // if (layoutGroup != null)
        // {
        //     layoutGroup.enabled = true;
        // }
        
        // UI要素との相互作用を再有効化
        canvasGroup.alpha = 1f;
        canvasGroup.blocksRaycasts = true;
    }

    /// <summary>
    /// ノードの上にドロップされたときに呼ばれる
    /// </summary>
    public void OnDrop(PointerEventData eventData)
    {
        // ドラッグ中のオブジェクトを取得
        GameObject draggedObject = eventData.pointerDrag;
        if (draggedObject == null)
        {
            return;
        }

        DraggableNode draggedNode = draggedObject.GetComponent<DraggableNode>();
        if (draggedNode == null || draggedNode == this)
        {
            return;
        }

        // ノードの色を取得
        NodeColor draggedColor = GetNodeColor(draggedNode.gameObject);
        NodeColor targetColor = GetNodeColor(gameObject);

        // 黄色と青色の組み合わせのみ合体可能
        if ((draggedColor == NodeColor.Yellow && targetColor == NodeColor.Blue) ||
            (draggedColor == NodeColor.Blue && targetColor == NodeColor.Yellow))
        {
            // 親ノード（青色）と子ノード（黄色）を決定
            RectTransform parentNode = (targetColor == NodeColor.Blue) ? rectTransform : draggedNode.rectTransform;
            RectTransform childNode = (targetColor == NodeColor.Blue) ? draggedNode.rectTransform : rectTransform;

            if (mergeManager != null)
            {
                // 黄色ノードが既に別の青ノードに合体している場合は、その合体を解除
                if (mergeManager.IsMerged(childNode))
                {
                    mergeManager.UnmergeNodes(childNode);
                }
                
                // 合体させる（青ノードに複数の黄色ノードを合体可能）
                mergeManager.MergeNodes(parentNode, childNode);
            }
        }
    }

    /// <summary>
    /// ノードの色の種類
    /// </summary>
    private enum NodeColor
    {
        Yellow, // 黄色
        Blue,   // 青色
        Other   // その他
    }

    /// <summary>
    /// ノードの色を取得
    /// </summary>
    private NodeColor GetNodeColor(GameObject nodeObj)
    {
        if (nodeObj == null)
        {
            return NodeColor.Other;
        }

        // Imageコンポーネントから色を取得
        // DraggableNodeはNode_XXXオブジェクトに直接アタッチされているので、
        // nodeObjから直接Imageを取得できる
        UnityEngine.UI.Image image = nodeObj.GetComponent<UnityEngine.UI.Image>();
        
        // 見つからない場合は、親オブジェクトからImageを取得
        if (image == null)
        {
            Transform parent = nodeObj.transform.parent;
            if (parent != null)
            {
                image = parent.GetComponent<UnityEngine.UI.Image>();
            }
        }

        if (image != null)
        {
            Color nodeColor = image.color;
            
            // 黄色かどうかを判定（RGB値で判定）
            // 黄色: Rが高く、Gが中程度、Bが低い (1f, 0.84f, 0f)
            if (nodeColor.r > 0.8f && nodeColor.g > 0.7f && nodeColor.b < 0.3f)
            {
                return NodeColor.Yellow;
            }
            // 青色かどうかを判定
            // 青色: Rが低く、Gが中程度、Bが高い (0.2f, 0.4f, 0.8f)
            else if (nodeColor.r < 0.4f && nodeColor.g > 0.3f && nodeColor.b > 0.6f)
            {
                return NodeColor.Blue;
            }
        }

        return NodeColor.Other;
    }

    /// <summary>
    /// ノード名を取得
    /// </summary>
    private string GetNodeName(GameObject nodeObj)
    {
        if (nodeObj != null)
        {
            string nodeName = nodeObj.name;
            if (nodeName.StartsWith("Node_"))
            {
                return nodeName.Substring(5);
            }
        }
        return "";
    }

    /// <summary>
    /// ノードがクリックされたときに呼ばれる
    /// </summary>
    public void OnPointerClick(PointerEventData eventData)
    {
        // ドラッグ中は無視
        if (isDragging)
        {
            return;
        }

        // ダブルクリック検出
        float currentTime = Time.time;
        if (currentTime - lastClickTime < doubleClickTime)
        {
            // ダブルクリックと判定
            OnDoubleClick();
            lastClickTime = 0f; // リセット
        }
        else
        {
            lastClickTime = currentTime;
        }
    }

    /// <summary>
    /// ダブルクリック時の処理
    /// </summary>
    private void OnDoubleClick()
    {
        if (mergeManager == null)
        {
            return;
        }

        // このノードが合体しているか確認
        bool isMerged = mergeManager.IsMerged(rectTransform);
        bool isParent = mergeManager.IsParentNode(rectTransform);

        if (isMerged || isParent)
        {
            // 合体を解除
            mergeManager.UnmergeNodes(rectTransform);
        }
    }
}

