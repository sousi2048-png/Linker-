using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

[InitializeOnLoad]
public static class WallTunnelSetup
{
    static WallTunnelSetup()
    {
        // シーンが開かれたときに実行
        EditorSceneManager.sceneOpened += OnSceneOpened;
        
        // 既に開いているシーンにも適用
        EditorApplication.delayCall += SetupWallAndTunnel;
    }

    static void OnSceneOpened(UnityEngine.SceneManagement.Scene scene, OpenSceneMode mode)
    {
        // プレイモード中は実行しない
        if (!EditorApplication.isPlaying)
        {
            SetupWallAndTunnel();
        }
    }

    static void SetupWallAndTunnel()
    {
        // プレイモード中は実行しない
        if (EditorApplication.isPlaying)
        {
            return;
        }

        // 現在のシーン内のすべてのGameObjectを検索
#if UNITY_2023_1_OR_NEWER
        GameObject[] allObjects = Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None);
#else
        GameObject[] allObjects = Object.FindObjectsOfType<GameObject>();
#endif
        
        int wallCount = 0;
        int tunnelCount = 0;
        int redBoxCount = 0;
        int blueBoxCount = 0;
        int highJumpingCount = 0;
        int boxMeshCount = 0;
        int bigCount = 0;
        
        // Wall_Meshのマテリアルを取得（HighJumpingに適用するため）
        Material wallMaterial = null;
        GameObject firstWallMesh = null;
        foreach (GameObject obj in allObjects)
        {
            if (obj.name.StartsWith("Wall_Mesh"))
            {
                firstWallMesh = obj;
                Renderer wallRenderer = obj.GetComponent<Renderer>();
                if (wallRenderer != null && wallRenderer.sharedMaterial != null)
                {
                    wallMaterial = wallRenderer.sharedMaterial;
                    break;
                }
            }
        }

        foreach (GameObject obj in allObjects)
        {
            // Wall_Meshで始まるすべてのオブジェクト（Wall_Mesh、Wall_Mesh (1)など）を対象
            if (obj.name.StartsWith("Wall_Mesh"))
            {
                SetupStaticCollider(obj);
                wallCount++;
                
                // 最初のWall_Meshのマテリアルを保存（まだ取得していない場合）
                if (wallMaterial == null)
                {
                    Renderer wallRenderer = obj.GetComponent<Renderer>();
                    if (wallRenderer != null && wallRenderer.sharedMaterial != null)
                    {
                        wallMaterial = wallRenderer.sharedMaterial;
                    }
                }
            }
            // Tunnel_Meshで始まるすべてのオブジェクトを対象
            else if (obj.name.StartsWith("Tunnel_Mesh"))
            {
                SetupStaticCollider(obj);
                tunnelCount++;
            }
            // RedBoxで始まるすべてのオブジェクトを対象
            else if (obj.name.StartsWith("RedBox"))
            {
                SetupRedBox(obj);
                redBoxCount++;
            }
            // BlueBoxで始まるすべてのオブジェクトを対象
            else if (obj.name.StartsWith("BlueBox"))
            {
                SetupBlueBox(obj);
                blueBoxCount++;
            }
            // HighJumpingで始まるすべてのオブジェクトを対象
            else if (obj.name.StartsWith("HighJumping"))
            {
                SetupHighJumping(obj, wallMaterial);
                highJumpingCount++;
            }
            // Box_350x250x300_Meshで始まるすべてのオブジェクトを対象
            else if (obj.name.StartsWith("Box_350x250x300_Mesh"))
            {
                SetupStaticCollider(obj);
                boxMeshCount++;
            }
            // Bigで始まるすべてのオブジェクトを対象
            else if (obj.name.StartsWith("Big"))
            {
                SetupBig(obj, wallMaterial);
                bigCount++;
            }
            // Gold Cupで始まるすべてのオブジェクトを対象（元のマテリアルを復元）
            else if (obj.name.StartsWith("Gold Cup"))
            {
                RestoreGoldCupMaterial(obj);
            }
        }

        if (wallCount > 0 || tunnelCount > 0 || redBoxCount > 0 || blueBoxCount > 0 || highJumpingCount > 0 || boxMeshCount > 0 || bigCount > 0)
        {
            // シーンに変更をマーク（エディタモード中のみ）
            if (!EditorApplication.isPlaying)
            {
                EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            }
            Debug.Log($"オブジェクトの設定が完了しました。Wall_Mesh: {wallCount}個, Tunnel_Mesh: {tunnelCount}個, RedBox: {redBoxCount}個, BlueBox: {blueBoxCount}個, HighJumping: {highJumpingCount}個, Box_350x250x300_Mesh: {boxMeshCount}個, Big: {bigCount}個");
        }
        else
        {
            Debug.LogWarning("対象のオブジェクトが見つかりませんでした。シーンにこれらのオブジェクトが存在することを確認してください。");
        }
    }

    static void SetupStaticCollider(GameObject obj)
    {
        // Rigidbodyを追加（isKinematic = trueで不動にする）
        Rigidbody rb = obj.GetComponent<Rigidbody>();
        if (rb == null)
        {
            rb = obj.AddComponent<Rigidbody>();
        }
        rb.isKinematic = true;
        rb.useGravity = false;

        // Colliderを追加（すり抜けられないようにする）
        Collider collider = obj.GetComponent<Collider>();
        if (collider == null)
        {
            // MeshColliderを追加（メッシュの形状に合わせる）
            MeshFilter meshFilter = obj.GetComponent<MeshFilter>();
            if (meshFilter != null && meshFilter.sharedMesh != null)
            {
                MeshCollider meshCollider = obj.AddComponent<MeshCollider>();
                meshCollider.sharedMesh = meshFilter.sharedMesh;
                meshCollider.convex = false; // 複雑な形状の場合はfalse
            }
            else
            {
                // MeshFilterがない場合はBoxColliderを追加
                BoxCollider boxCollider = obj.AddComponent<BoxCollider>();
            }
        }

        // Staticフラグを設定（オプション：パフォーマンス向上のため）
        GameObjectUtility.SetStaticEditorFlags(obj, StaticEditorFlags.ContributeGI | StaticEditorFlags.OccluderStatic | StaticEditorFlags.BatchingStatic);
    }

    static void SetupRedBox(GameObject obj)
    {
        // Rigidbodyを追加（重力を有効にする）
        Rigidbody rb = obj.GetComponent<Rigidbody>();
        if (rb == null)
        {
            rb = obj.AddComponent<Rigidbody>();
        }
        rb.isKinematic = false; // 動的オブジェクト
        rb.useGravity = true; // 重力を有効
        rb.freezeRotation = true; // 回転を固定して転倒を防ぐ

        // 物理衝突用のColliderを追加（isTrigger = false）
        Collider physicsCollider = obj.GetComponent<Collider>();
        if (physicsCollider == null)
        {
            // MeshColliderを追加（メッシュの形状に合わせる）
            MeshFilter meshFilter = obj.GetComponent<MeshFilter>();
            if (meshFilter != null && meshFilter.sharedMesh != null)
            {
                MeshCollider meshCollider = obj.AddComponent<MeshCollider>();
                meshCollider.sharedMesh = meshFilter.sharedMesh;
                meshCollider.convex = true; // 動的オブジェクトの場合はconvexをtrueにする必要がある
            }
            else
            {
                // MeshFilterがない場合はBoxColliderを追加
                BoxCollider boxCollider = obj.AddComponent<BoxCollider>();
            }
            physicsCollider = obj.GetComponent<Collider>();
        }

        // 物理衝突用のColliderはTriggerにしない（床との衝突のため）
        if (physicsCollider != null)
        {
            physicsCollider.isTrigger = false;
        }

        // Player検出用のTrigger Colliderを追加（子オブジェクトとして）
        GameObject triggerObj = obj.transform.Find("TriggerCollider")?.gameObject;
        if (triggerObj == null)
        {
            triggerObj = new GameObject("TriggerCollider");
            triggerObj.transform.SetParent(obj.transform);
            triggerObj.transform.localPosition = Vector3.zero;
            triggerObj.transform.localRotation = Quaternion.identity;
            triggerObj.transform.localScale = Vector3.one;
        }

        // Trigger Colliderを追加（少し大きめに設定）
        Collider triggerCollider = triggerObj.GetComponent<Collider>();
        if (triggerCollider == null)
        {
            BoxCollider triggerBox = triggerObj.AddComponent<BoxCollider>();
            triggerBox.isTrigger = true;
            // 元のColliderのサイズより少し大きくする（1.2倍）
            if (physicsCollider is BoxCollider boxColl)
            {
                triggerBox.size = boxColl.size * 1.2f;
                triggerBox.center = boxColl.center;
            }
            else if (physicsCollider is MeshCollider meshColl)
            {
                // MeshColliderの場合はRendererのBoundsを使用
                Renderer renderer = obj.GetComponent<Renderer>();
                if (renderer != null)
                {
                    Bounds bounds = renderer.bounds;
                    // ローカル座標に変換
                    Vector3 localSize = obj.transform.InverseTransformVector(bounds.size);
                    triggerBox.size = localSize * 1.2f;
                    triggerBox.center = obj.transform.InverseTransformPoint(bounds.center);
                }
                else
                {
                    // Rendererがない場合はデフォルトサイズ
                    triggerBox.size = Vector3.one * 1.2f;
                }
            }
            else
            {
                // デフォルトサイズ
                triggerBox.size = Vector3.one * 1.2f;
            }
        }
        else
        {
            triggerCollider.isTrigger = true;
        }

        // RedBoxRegisterコンポーネントを追加（TriggerColliderにアタッチ）
        RedBoxRegister register = triggerObj.GetComponent<RedBoxRegister>();
        if (register == null)
        {
            register = triggerObj.AddComponent<RedBoxRegister>();
        }
        
        // RedBoxの色を赤色に設定
        SetObjectColor(obj, Color.red);
    }
    
    static void SetupBlueBox(GameObject obj)
    {
        // Rigidbodyを追加（重力を有効にする）
        Rigidbody rb = obj.GetComponent<Rigidbody>();
        if (rb == null)
        {
            rb = obj.AddComponent<Rigidbody>();
        }
        rb.isKinematic = false; // 動的オブジェクト
        rb.useGravity = true; // 重力を有効
        rb.freezeRotation = true; // 回転を固定して転倒を防ぐ

        // 物理衝突用のColliderを追加（isTrigger = false）
        Collider physicsCollider = obj.GetComponent<Collider>();
        if (physicsCollider == null)
        {
            // MeshColliderを追加（メッシュの形状に合わせる）
            MeshFilter meshFilter = obj.GetComponent<MeshFilter>();
            if (meshFilter != null && meshFilter.sharedMesh != null)
            {
                MeshCollider meshCollider = obj.AddComponent<MeshCollider>();
                meshCollider.sharedMesh = meshFilter.sharedMesh;
                meshCollider.convex = true; // 動的オブジェクトの場合はconvexをtrueにする必要がある
            }
            else
            {
                // MeshFilterがない場合はBoxColliderを追加
                BoxCollider boxCollider = obj.AddComponent<BoxCollider>();
            }
            physicsCollider = obj.GetComponent<Collider>();
        }

        // 物理衝突用のColliderはTriggerにしない（床との衝突のため）
        if (physicsCollider != null)
        {
            physicsCollider.isTrigger = false;
        }
        
        // BlueBoxの色を青色に設定
        SetObjectColor(obj, Color.blue);
    }
    
    static void SetupHighJumping(GameObject obj, Material wallMaterial)
    {
        // Rigidbodyを追加（重力を有効にする）
        Rigidbody rb = obj.GetComponent<Rigidbody>();
        if (rb == null)
        {
            rb = obj.AddComponent<Rigidbody>();
        }
        rb.isKinematic = false; // 動的オブジェクト
        rb.useGravity = true; // 重力を有効
        rb.freezeRotation = true; // 回転を固定して転倒を防ぐ

        // 物理衝突用のColliderを追加（isTrigger = false）
        Collider physicsCollider = obj.GetComponent<Collider>();
        if (physicsCollider == null)
        {
            // MeshColliderを追加（メッシュの形状に合わせる）
            MeshFilter meshFilter = obj.GetComponent<MeshFilter>();
            if (meshFilter != null && meshFilter.sharedMesh != null)
            {
                MeshCollider meshCollider = obj.AddComponent<MeshCollider>();
                meshCollider.sharedMesh = meshFilter.sharedMesh;
                meshCollider.convex = true; // 動的オブジェクトの場合はconvexをtrueにする必要がある
            }
            else
            {
                // MeshFilterがない場合はBoxColliderを追加
                BoxCollider boxCollider = obj.AddComponent<BoxCollider>();
            }
            physicsCollider = obj.GetComponent<Collider>();
        }

        // 物理衝突用のColliderはTriggerにしない（床との衝突のため）
        if (physicsCollider != null)
        {
            physicsCollider.isTrigger = false;
        }
        
        // HighJumpingのマテリアルをWall_Meshと同じにする
        SetObjectMaterial(obj, wallMaterial);
        
        // HighJumpingの色をグレーに設定（明るさを下げる）
        Color grayColor = new Color(0.5f, 0.5f, 0.5f, 1f); // グレー色（明るさ50%）
        SetObjectColor(obj, grayColor);
    }
    
    static void SetupBig(GameObject obj, Material wallMaterial)
    {
        // Rigidbodyを追加（isKinematic = trueで不動にする）
        Rigidbody rb = obj.GetComponent<Rigidbody>();
        if (rb == null)
        {
            rb = obj.AddComponent<Rigidbody>();
        }
        rb.isKinematic = true;
        rb.useGravity = false;

        // Colliderを追加（すり抜けられないようにする）
        Collider collider = obj.GetComponent<Collider>();
        if (collider == null)
        {
            // MeshColliderを追加（メッシュの形状に合わせる）
            MeshFilter meshFilter = obj.GetComponent<MeshFilter>();
            if (meshFilter != null && meshFilter.sharedMesh != null)
            {
                MeshCollider meshCollider = obj.AddComponent<MeshCollider>();
                meshCollider.sharedMesh = meshFilter.sharedMesh;
                meshCollider.convex = false; // 複雑な形状の場合はfalse
            }
            else
            {
                // MeshFilterがない場合はBoxColliderを追加
                BoxCollider boxCollider = obj.AddComponent<BoxCollider>();
            }
        }

        // Staticフラグを設定（オプション：パフォーマンス向上のため）
        GameObjectUtility.SetStaticEditorFlags(obj, StaticEditorFlags.ContributeGI | StaticEditorFlags.OccluderStatic | StaticEditorFlags.BatchingStatic);
        
        // BigのマテリアルをWall_Meshと同じにする
        SetObjectMaterial(obj, wallMaterial);
        
        // Bigの色をグレーに設定（HighJumpingと同じ色・質感）
        Color grayColor = new Color(0.5f, 0.5f, 0.5f, 1f); // グレー色（明るさ50%）
        SetObjectColor(obj, grayColor);
    }
    
    /// <summary>
    /// オブジェクトの色を設定する
    /// </summary>
    static void SetObjectColor(GameObject obj, Color color)
    {
        if (obj == null) return;
        
        // Rendererコンポーネントを取得
        Renderer renderer = obj.GetComponent<Renderer>();
        if (renderer == null)
        {
            renderer = obj.GetComponentInChildren<Renderer>();
        }
        
        if (renderer != null)
        {
            // Materialのインスタンスを作成（共有Materialを変更しないようにする）
            Material material = renderer.sharedMaterial;
            Material newMaterial = null;
            
            if (material != null)
            {
                // 既存のMaterialから新しいインスタンスを作成
                newMaterial = new Material(material);
            }
            else
            {
                // Materialがない場合はURPのLitシェーダーを使用
                Shader litShader = Shader.Find("Universal Render Pipeline/Lit");
                if (litShader == null)
                {
                    // URPが見つからない場合は標準のシェーダーを使用
                    litShader = Shader.Find("Standard");
                }
                if (litShader != null)
                {
                    newMaterial = new Material(litShader);
                }
            }
            
            if (newMaterial != null)
            {
                // 色を設定（複数の方法を試す）
                newMaterial.color = color;
                
                // URPの場合は_BaseColorプロパティも設定
                if (newMaterial.HasProperty("_BaseColor"))
                {
                    newMaterial.SetColor("_BaseColor", color);
                }
                
                // 標準シェーダーの場合は_Colorプロパティも設定
                if (newMaterial.HasProperty("_Color"))
                {
                    newMaterial.SetColor("_Color", color);
                }
                
                renderer.sharedMaterial = newMaterial;
            }
        }
    }
    
    /// <summary>
    /// オブジェクトのマテリアルを設定する（Wall_Meshと同じマテリアルを適用）
    /// </summary>
    static void SetObjectMaterial(GameObject obj, Material material)
    {
        if (obj == null) return;
        
        // Rendererコンポーネントを取得
        Renderer renderer = obj.GetComponent<Renderer>();
        if (renderer == null)
        {
            renderer = obj.GetComponentInChildren<Renderer>();
        }
        
        if (renderer != null)
        {
            if (material != null)
            {
                // Wall_Meshのマテリアルをそのまま使用
                renderer.sharedMaterial = material;
            }
            else
            {
                // Wall_Meshのマテリアルが見つからない場合、デフォルトのマテリアルを作成
                Shader litShader = Shader.Find("Universal Render Pipeline/Lit");
                if (litShader == null)
                {
                    litShader = Shader.Find("Standard");
                }
                if (litShader != null)
                {
                    Material newMaterial = new Material(litShader);
                    renderer.sharedMaterial = newMaterial;
                }
            }
        }
    }

    /// <summary>
    /// Gold Cupのマテリアルを、光源や影に影響されない白っぽい明るい黄色に置き換える
    /// </summary>
    static void RestoreGoldCupMaterial(GameObject obj)
    {
        if (obj == null) return;
        
        // Unlitシェーダーを取得（光源の影響を受けない）
        Shader unlitShader = Shader.Find("Universal Render Pipeline/Unlit");
        if (unlitShader == null)
        {
            // URPのUnlitが見つからない場合は、標準のUnlitシェーダーを使用
            unlitShader = Shader.Find("Unlit/Color");
            if (unlitShader == null)
            {
                unlitShader = Shader.Find("Unlit/Texture");
            }
        }
        
        if (unlitShader != null)
        {
            // 新しいマテリアルを作成
            Material newMaterial = new Material(unlitShader);
            
            // 白っぽい明るい黄色を設定
            Color lightYellow = new Color(1f, 0.95f, 0.7f, 1f); // 白っぽい明るい黄色
            
            // 色を設定
            if (newMaterial.HasProperty("_BaseColor"))
            {
                // URPのUnlitシェーダーの場合
                newMaterial.SetColor("_BaseColor", lightYellow);
            }
            else if (newMaterial.HasProperty("_Color"))
            {
                // 標準のUnlitシェーダーの場合
                newMaterial.SetColor("_Color", lightYellow);
            }
            else
            {
                // その他のプロパティを試す
                newMaterial.color = lightYellow;
            }
            
            // オブジェクト自体と子オブジェクトのRendererにマテリアルを適用
            Renderer[] renderers = obj.GetComponentsInChildren<Renderer>();
            foreach (Renderer renderer in renderers)
            {
                if (renderer != null)
                {
                    renderer.sharedMaterial = newMaterial;
                }
            }
        }
        else
        {
            Debug.LogWarning("Unlitシェーダーが見つかりませんでした。");
        }
    }

    [MenuItem("Tools/Setup Wall and Tunnel")]
    static void SetupManually()
    {
        // プレイモード中は実行できない
        if (EditorApplication.isPlaying)
        {
            EditorUtility.DisplayDialog("エラー", "この操作はプレイモード中には実行できません。", "OK");
            return;
        }
        SetupWallAndTunnel();
    }
}

