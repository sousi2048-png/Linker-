# Linker - ノードマージパズルゲーム

## 概要

**Linker**は、ノードをマージしてオブジェクトを操作する3Dパズルアクションゲームです。プレイヤーは様々なオブジェクトを発見し、メニューシステムでノードを組み合わせることで、新しい能力を獲得し、ゴールのGold Cupを目指します。

## ゲームスクリーンショット

![ゲーム画面](images/スクリーンショット%202026-01-17%20223138.png)

![メニュー画面](images/スクリーンショット%202026-01-17%20223232.png)

## ゲームの目的

ゲーム内に配置された**Gold Cup (Low Poly)**に触れることでゲームクリアとなります。画面中央に「You Win」が表示され、ゲームが終了します。

## 操作方法

### 基本操作

- **移動**: WASDキー / 左スティック
- **視点操作**: マウス / 右スティック
- **ジャンプ**: スペースキー / Aボタン
- **ダッシュ**: Shiftキー / 左トリガー
- **メニュー開閉**: Escapeキー

### メニュー操作

- **ノードのドラッグ**: マウスでノードをドラッグして移動
- **ノードの合体**: ノードを別のノードの上にドロップして合体
- **ノードの分離**: 合体したノードをダブルクリックして分離

## ゲームシステム

### ノードシステム

ゲーム内には様々なノードが存在し、それぞれ異なる機能を持っています：

- **Player**: プレイヤーキャラクター。常に操作可能
- **Operable**: 操作可能なオブジェクトの基本ノード
- **RedBox**: プレイヤーが触れるとメニューに登録される赤い箱
- **BlueBox**: 青色の箱
- **HighJumping**: 常にジャンプし続けるオブジェクト
- **Big**: 大きなサイズのオブジェクト

### ノードマージシステム

メニュー内でノードをドラッグ&ドロップすることで、ノードを合体させることができます：

- **Player + Operable**: プレイヤーが操作可能な状態になる
- **Operable + RedBox**: RedBoxオブジェクトを操作可能になる
- **Operable + HighJumping**: HighJumpingオブジェクトを操作可能になる
- **Operable + Big**: Bigオブジェクトを操作可能になる

ノードを合体させると、対応するオブジェクトが操作可能になり、WASDキーで移動できるようになります。

### メニューシステム

- **Escapeキー**でメニューを開閉
- メニュー内では、収集したノードが表示されます
- ノードをドラッグして移動・合体が可能
- 合体したノードはダブルクリックで分離できます

### ゲームクリア条件

**Gold Cup (Low Poly)**に触れると、ゲームクリアとなります。画面中央に黄色の「You Win」テキストが表示されます。

## セットアップ方法

### 必要な環境

- Unity 2021.3以降
- Universal Render Pipeline (URP)

### セットアップ手順

1. プロジェクトをUnityエディタで開く
2. シーンを開く（`Assets/Scenes/Playground.unity`など）
3. エディタメニューから「**Tools > Setup Wall and Tunnel**」を実行
   - これにより、Wall、Tunnel、RedBox、BlueBox、HighJumping、Big、Gold Cupなどのオブジェクトが自動設定されます
4. エディタメニューから「**Tools > Setup Menu Manager**」を実行
   - メニューシステムに必要なコンポーネントが追加されます
5. プレイモードで実行してテスト

### 手動設定（必要な場合）

Gold CupがPrefabインスタンスの場合、手動で設定が必要な場合があります：

1. Hierarchyで「**Gold Cup (Low Poly)**」を選択
2. 子オブジェクトとして「**TriggerCollider**」を作成
3. 「**TriggerCollider**」に以下を追加：
   - **Box Collider**（Is Triggerにチェック、Size: 3×3×3）
   - **Gold Cup Trigger**スクリプト

## プロジェクト構造

```
Assets/
├── Scripts/                    # ゲームロジックスクリプト
│   ├── GameManager.cs         # ゲーム全体の状態管理
│   ├── GoldCupTrigger.cs      # Gold Cup接触検出
│   ├── MenuManager.cs         # メニューシステム
│   ├── NodeMergeManager.cs    # ノードマージ管理
│   ├── RedBoxController.cs    # RedBox操作制御
│   ├── RedBoxRegister.cs      # RedBox登録システム
│   └── DraggableNode.cs       # ノードドラッグ機能
├── TutorialInfo/
│   └── Scripts/Editor/
│       └── WallTunnelSetup.cs # エディタ自動設定スクリプト
├── StarterAssets/             # サードパーティアセット
└── TrophyCups/                # トロフィーカップアセット
```

## 主な機能

### 自動設定システム

`WallTunnelSetup.cs`がエディタスクリプトとして動作し、シーンを開いたときに自動的に以下を設定します：

- Wall_Mesh: 静的コライダーの設定
- Tunnel_Mesh: 静的コライダーの設定
- RedBox: 物理設定とTrigger Colliderの追加
- BlueBox: 物理設定
- HighJumping: 物理設定とマテリアル設定
- Big: 静的コライダーとマテリアル設定
- Gold Cup: マテリアル設定とゲームクリア用Trigger Collider

### ノードマージシステム

ノードを合体させることで、対応するオブジェクトを操作可能にします。合体したノードは、メニュー内で視覚的に接続されて表示されます。

### ゲームクリアシステム

Gold Cupに触れると、`GameManager`がゲームクリア状態を管理し、画面中央に「You Win」UIを表示します。

## 技術スタック

- **Unity**: 2021.3以降
- **Universal Render Pipeline (URP)**: レンダリングパイプライン
- **Unity Input System**: 入力管理
- **C#**: スクリプト言語

## ライセンス

このプロジェクトは教育目的で作成されています。

## 開発者向け情報

### デバッグ機能

- Consoleウィンドウで各種ログを確認できます
- `DebugTest.cs`でデバッグ機能を追加可能

### カスタマイズ

各スクリプトのInspectorで設定可能なパラメータ：

- **RedBoxController**: 移動速度、ジャンプ力など
- **MenuManager**: メニューUIの設定
- **GameManager**: ゲームクリア時のUI設定

## トラブルシューティング

### Gold Cupに触れてもゲームクリアにならない場合

1. 「**TriggerCollider**」が正しく設定されているか確認
2. 「**TriggerCollider**」のBox Colliderが「**Is Trigger**」にチェックされているか確認
3. 「**Gold Cup Trigger**」コンポーネントが追加されているか確認
4. プレイヤーオブジェクトに「**Player**」タグが設定されているか確認

### メニューが開かない場合

1. 「**Tools > Setup Menu Manager**」を実行
2. `MenuManager`と`MenuInputHandler`がシーンに存在するか確認

### ノードが合体しない場合

1. `NodeMergeManager`がシーンに存在するか確認
2. ノードを正しくドラッグ&ドロップしているか確認

## 今後の拡張予定

- 追加のノードタイプ
- より複雑なパズル要素
- サウンドエフェクト
- パーティクルエフェクト

---

**楽しんでプレイしてください！**
