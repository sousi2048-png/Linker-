using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 再生時に「Press Esc for Menu」の看板をワールドに生成します。
/// シーンにこのコンポーネントを持つGameObjectを1つ置くだけでOKです。
/// </summary>
[DisallowMultipleComponent]
public class MenuSignSpawner : MonoBehaviour
{
    [SerializeField] private string signText = "Press Esc for Menu";
    [SerializeField] private Vector3 localOffsetFromPlayer = new Vector3(0f, 1.4f, 4f);
    [SerializeField] private Vector3 boardScale = new Vector3(3.2f, 1.2f, 0.12f);

    [Header("Start Hint (UI)")]
    [SerializeField] private bool showStartHint = true;
    [SerializeField] private float startHintSeconds = 5f;
    [SerializeField] private int startHintSortingOrder = 90; // MenuManager(100)より下にして、メニューが出たら隠れる

    private static bool s_spawned;
    private static bool s_hintShown;

    // Domain Reloadが無効でも毎回再生で出るように、staticをリセット
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        s_spawned = false;
        s_hintShown = false;
    }

    private void Start()
    {
        if (s_spawned)
        {
            return;
        }
        s_spawned = true;

        if (showStartHint && !s_hintShown)
        {
            s_hintShown = true;
            StartCoroutine(ShowStartHint());
        }

        Transform target = null;
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            target = player.transform;
        }
        else if (Camera.main != null)
        {
            target = Camera.main.transform;
        }

        Vector3 signPos;
        Vector3 lookAt;

        if (target != null)
        {
            signPos = target.TransformPoint(localOffsetFromPlayer);
            lookAt = target.position + Vector3.up * 1.2f;
        }
        else
        {
            signPos = new Vector3(0f, 1.6f, 3f);
            lookAt = Vector3.zero;
        }

        GameObject signRoot = new GameObject("MenuSign");
        signRoot.transform.position = signPos;
        if (target != null)
        {
            signRoot.transform.LookAt(lookAt);
        }

        // 板
        GameObject board = GameObject.CreatePrimitive(PrimitiveType.Cube);
        board.name = "Board";
        board.transform.SetParent(signRoot.transform, false);
        board.transform.localScale = boardScale;
        board.transform.localPosition = Vector3.zero;

        var boardRenderer = board.GetComponent<Renderer>();
        if (boardRenderer != null)
        {
            // 実行時に生成したMaterialを使う（既存アセットに依存しない）
            var mat = new Material(Shader.Find("Standard"));
            mat.color = new Color(1f, 1f, 1f, 0.98f);
            boardRenderer.material = mat;
        }

        // 文字（3D Text）
        GameObject textObj = new GameObject("Text");
        textObj.transform.SetParent(signRoot.transform, false);
        textObj.transform.localPosition = new Vector3(0f, 0f, (boardScale.z * 0.5f) + 0.01f);
        textObj.transform.localRotation = Quaternion.identity;

        TextMesh textMesh = textObj.AddComponent<TextMesh>();
        textMesh.text = signText;
        textMesh.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        textMesh.fontSize = 90;
        textMesh.characterSize = 0.04f;
        textMesh.anchor = TextAnchor.MiddleCenter;
        textMesh.alignment = TextAlignment.Center;
        textMesh.color = new Color(0.1f, 0.1f, 0.1f, 1f);

        // 文字が少し見切れにくいようにスケール調整
        textObj.transform.localScale = new Vector3(1f, 1f, 1f);
    }

    private System.Collections.IEnumerator ShowStartHint()
    {
        float seconds = Mathf.Max(0.01f, startHintSeconds);

        GameObject hintCanvas = new GameObject("StartHintCanvas");
        Canvas canvas = hintCanvas.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = startHintSortingOrder;

        CanvasScaler scaler = hintCanvas.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;

        hintCanvas.AddComponent<GraphicRaycaster>();

        GameObject textObj = new GameObject("StartHintText");
        textObj.transform.SetParent(hintCanvas.transform, false);

        RectTransform textRect = textObj.AddComponent<RectTransform>();
        textRect.anchorMin = new Vector2(0.5f, 0.5f);
        textRect.anchorMax = new Vector2(0.5f, 0.5f);
        textRect.pivot = new Vector2(0.5f, 0.5f);
        textRect.anchoredPosition = Vector2.zero;
        textRect.sizeDelta = new Vector2(1200f, 160f);

        Text uiText = textObj.AddComponent<Text>();
        uiText.text = signText;
        uiText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (uiText.font == null)
        {
            uiText.font = Font.CreateDynamicFontFromOSFont("Arial", 80);
        }
        uiText.fontSize = 80;
        uiText.color = Color.white;
        uiText.alignment = TextAnchor.MiddleCenter;
        uiText.fontStyle = FontStyle.Bold;

        Outline outline = textObj.AddComponent<Outline>();
        outline.effectColor = new Color(0f, 0f, 0f, 0.85f);
        outline.effectDistance = new Vector2(3f, -3f);

        // Time.timeScaleの影響を受けない「実時間」で5秒表示
        yield return new WaitForSecondsRealtime(seconds);
        if (hintCanvas != null)
        {
            Destroy(hintCanvas);
        }
    }
}

