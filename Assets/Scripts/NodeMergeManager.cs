using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// ノードの合体状態を管理するコンポーネント
/// </summary>
public class NodeMergeManager : MonoBehaviour
{
    private static NodeMergeManager instance;
    public static NodeMergeManager Instance
    {
        get
        {
            if (instance == null)
            {
                GameObject obj = new GameObject("NodeMergeManager");
                instance = obj.AddComponent<NodeMergeManager>();
            }
            return instance;
        }
    }

    // 合体状態の管理：キーは子ノード（Operable）、値は親ノード（Player）
    private Dictionary<RectTransform, RectTransform> mergedNodes = new Dictionary<RectTransform, RectTransform>();
    
    // 合体時の相対位置オフセット（斜め後ろに配置）
    private Vector2 mergeOffset = new Vector2(30f, -30f);

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
        else if (instance != this)
        {
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// ノードを合体させる
    /// </summary>
    /// <param name="parentNode">親ノード（Player）</param>
    /// <param name="childNode">子ノード（Operable）</param>
    public void MergeNodes(RectTransform parentNode, RectTransform childNode)
    {
        // 既に同じ親ノードに合体している場合は何もしない
        if (mergedNodes.ContainsKey(childNode))
        {
            if (mergedNodes[childNode] == parentNode)
            {
                Debug.LogWarning("NodeMergeManager: このノードは既に同じ親ノードに合体しています。");
                return;
            }
            // 別の親ノードに合体している場合は、既存の合体を解除してから新しい親に合体
            mergedNodes.Remove(childNode);
        }

        mergedNodes[childNode] = parentNode;

        // 既存の子ノードの数を取得して、新しい子ノードの位置を決定
        List<RectTransform> existingChildren = GetChildNodes(parentNode);
        int childIndex = existingChildren.Count - 1; // 現在追加する子ノードのインデックス（既にリストに含まれているので-1）
        
        // 子ノードを親ノードの周りに配置（複数の子ノードがある場合は異なる位置に配置）
        RectTransform parentRect = parentNode;
        Vector2 parentPos = parentRect.anchoredPosition;
        
        // 複数の子ノードを円形に配置するためのオフセットを計算
        float angleStep = 360f / Mathf.Max(existingChildren.Count, 1); // 各子ノードの角度間隔
        float angle = angleStep * childIndex * Mathf.Deg2Rad; // 現在の子ノードの角度（ラジアン）
        float radius = 40f; // 親ノードからの距離
        
        Vector2 offset = new Vector2(
            Mathf.Cos(angle) * radius,
            Mathf.Sin(angle) * radius
        );
        
        childNode.anchoredPosition = parentPos + offset;
        
        // 青ノード（親ノード）が常に一番前に表示されるようにする
        // まず子ノード（黄色ノード）を後ろに配置
        if (childNode.parent != null)
        {
            childNode.SetAsFirstSibling();
        }
        
        // 最後に親ノード（青ノード）を最前面に配置
        if (parentNode.parent != null)
        {
            parentNode.SetAsLastSibling();
        }

        Debug.Log($"NodeMergeManager: {GetNodeName(parentNode)}と{GetNodeName(childNode)}を合体しました。（子ノード数: {existingChildren.Count}）");
    }

    /// <summary>
    /// ノードが合体しているか確認
    /// </summary>
    public bool IsMerged(RectTransform node)
    {
        return mergedNodes.ContainsKey(node);
    }

    /// <summary>
    /// ノードの親ノードを取得（合体している場合）
    /// </summary>
    public RectTransform GetParentNode(RectTransform childNode)
    {
        if (mergedNodes.ContainsKey(childNode))
        {
            return mergedNodes[childNode];
        }
        return null;
    }

    /// <summary>
    /// 親ノードに合体している子ノードのリストを取得
    /// </summary>
    public List<RectTransform> GetChildNodes(RectTransform parentNode)
    {
        List<RectTransform> children = new List<RectTransform>();
        foreach (var kvp in mergedNodes)
        {
            if (kvp.Value == parentNode)
            {
                children.Add(kvp.Key);
            }
        }
        return children;
    }

    /// <summary>
    /// 親ノードが移動したときに、合体している子ノードも一緒に移動させる
    /// </summary>
    public void UpdateMergedNodePosition(RectTransform parentNode, Vector2 newPosition)
    {
        List<RectTransform> children = GetChildNodes(parentNode);
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
                
                child.anchoredPosition = newPosition + offset;
            }
        }
    }

    /// <summary>
    /// 合体時のオフセットを取得
    /// </summary>
    public Vector2 GetMergeOffset()
    {
        return mergeOffset;
    }

    /// <summary>
    /// ノードが親ノードかどうかを確認（合体している子ノードを持っているか）
    /// </summary>
    public bool IsParentNode(RectTransform node)
    {
        return GetChildNodes(node).Count > 0;
    }

    /// <summary>
    /// 合体を解除する
    /// </summary>
    /// <param name="node">合体を解除するノード（親または子のどちらでも可）</param>
    public void UnmergeNodes(RectTransform node)
    {
        // このノードが子ノードの場合
        if (mergedNodes.ContainsKey(node))
        {
            RectTransform parentNode = mergedNodes[node];
            mergedNodes.Remove(node);
            Debug.Log($"NodeMergeManager: {GetNodeName(node)}と{GetNodeName(parentNode)}の合体を解除しました。");
        }
        // このノードが親ノードの場合、すべての子ノードとの合体を解除
        else
        {
            List<RectTransform> children = GetChildNodes(node);
            foreach (RectTransform child in children)
            {
                mergedNodes.Remove(child);
            }
            if (children.Count > 0)
            {
                Debug.Log($"NodeMergeManager: {GetNodeName(node)}と{children.Count}個の子ノードの合体を解除しました。");
            }
        }
    }

    /// <summary>
    /// ノード名を取得（デバッグ用）
    /// </summary>
    private string GetNodeName(RectTransform node)
    {
        if (node != null && node.gameObject != null)
        {
            string nodeName = node.gameObject.name;
            if (nodeName.StartsWith("Node_"))
            {
                return nodeName.Substring(5);
            }
            return nodeName;
        }
        return "Unknown";
    }
}

