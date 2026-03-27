# AGENTS.md

## 目皁E
こ�Eプロジェクト�E、ゲームジャム向けの 2D ゲームを短期間で立ち上げるため�E Unity チE��プレートです、E 
現時点ではチE�Eマ未定�Eため、ジャンルを固定せず、既存�E補助アセチE��を使って最短でプロトタイプを絁E�Eことを優先します、E
## 作業開始前ルール

- 本作�Eを始める前に、忁E�� `AGENTS.md` を更新する
- 仕様変更、方針変更、使ぁE��助アセチE��の変更があれ�E、その着手前に `AGENTS.md` へ反映する
- こ�E更新を飛�Eして作業を始めそうなら、�Eに `AGENTS.md` の更新をリマインドすめE
## 2026-03-17 作業メモ

- `Assets/Scenes/Game.unity` に大まかな進行制御用の `SequenceManager` を�E置し、会話パ�Eトと操作パートを往復する仮実裁E��追加する、E- 進行制御は `UniTask` を使った状態�E移で構�Eし、新規実裁E��は `Coroutine` を使わなぁE��E- シーン冁E��照はコード探索で補完せず、`SequenceManager` から忁E��な UI と制御対象めE`SerializeField` で明示参�Eする、E- `SequenceManager` は進行状態�E刁E��替えだけを拁E��し、会話パ�Eトと操作パート�E表示・入力征E��は別 Manager に刁E��する、E- `Game` シーンには `ConversationPartManager` と `OperationPartManager` を個別配置し、`SequenceManager` から `SerializeField` 参�Eで接続する、E
## 2026-03-18 作業メモ

- シーン刁E��替え時の暗転は、既存�Eフェードイン・フェードアウト時間に加えて「�E面が黒になってぁE��保持時間」を個別持E��できるようにする、E- 暗転保持時間の持E���E `SceneTransitionManager` の API で受け取り、各呼び出し�Eは `SerializeField` で遷移ごとの秒数を設定する、E- 暗転保持時間はロード完亁E��ではなく、フェードアウト完亁E��後かつシーンロード開始前に挟�E仕様で扱ってよい、E- シーン刁E��替え開始と同時に、`SceneTransitionManager` の全画面オーバ�Eレイ冁E��透�Eな Raycast 遮断パネルを有効化し、フェード中から遷移完亁E��で画面操作を無効化する、E- シーン遷移用の入力�E断は `SceneTransitionManager` の既存オーバ�Eレイ配下で完結させ、別シーンの UI 探索めE��別画面ごとの追加パネル生�Eは行わなぁE��E- 占ぁE��の立ち絵は本アセチE��確定まで仮グラフィチE��で進め、犬の顔モチ�Eフをシーン上に配置してレイアウトと表惁E�Eり替え導線を先に確認できる状態にする、E- 仮グラフィチE��生�EめE��ーン配置は Unity Editor 上で完結させ、忁E��な参�Eは既存どおり `SerializeField` で接続する、E- Game シーンに表示するキャラクター立ち絵は UI `Image` 直差しではなく、シーン上�E `SpriteRenderer` と `Animator` を持つ専用コンポ�Eネントで管琁E��る、E- `StoryAsset` からの表惁E��更は Sprite 参�E直持E��ではなく「キャラクター ID + 表惁EID」で持E��し、立ち絵コンポ�Eネント�Eで Animator の状態�Eり替えへ解決する、E- `StoryAsset` と立ち絵側で扱ぁE��ャラクター ID / 表惁EID は、�E力ミス防止のため string 直書きではなぁEEnum を使って統一する、E- 立ち絵の表惁E��義は `Normal`、`Joy`、`Angry` などの識別子と Animator State 名�E対応をシリアライズチE�Eタとして保持し、シーン探索で補完しなぁE��E- 既孁E`StoryAsset` チE�Eタは頁E��新フォーマットへ移行し、会話サンプルではメチE��ージ間に `ChangeExpression` スチE��プを挿入して表惁E���Eの使ぁE��を残す、E- `uLoopMCP` は従来どおり自動スタートアチE�Eを有効に保つ、E- `com.coplaydev.unity-mcp` は自動スタートアチE�Eを無効にし、忁E��時だけ手動で接続する、E- Title シーンのオプションボタン初回押下で `OptionsPanel` が開かなぁE���E合を修正する、E- 原因ぁE`OptionUI` 初期化頁E��パネル活性化�E競合にある場合�E、シーン探索を増やさず `SerializeField` 構�Eのまま制御頁E��けを見直す、E- 会話パ�Eト�Eセリフ表示は DOTween Pro の `DOText` で一斁E��ずつ表示する、E- セリフ表示中のクリチE��めEEnter は次送りではなく、そのセリフ�E全斁E��時表示として扱ぁE��E- セリフ�E斁E�E表示完亁E��、次の入力を受け付ける状態では会話ウィンドウ右下に `▼` を表示してクリチE��征E��を�E示する、E- チE��スト表示は Unity 標準�E Localization パッケージを使ぁE��針に刁E��替える、E- 会話パ�Eト、操作パート、タイトル画面の表示斁E��は斁E���E直書きではなぁEString Table を参照する、E- 動的に刁E��替わる斁E��は `LocalizedString` めE`SerializeField` で保持し、表示允EUI への反映めEUnity Localization の仕絁E��に寁E��る、E- Localization 関連アセチE��は `Assets/Localize` 配下に雁E��E��、`Localization Settings.asset` と Locale、String Table をこの配下で管琁E��る、E- 会話再生は `LocalizedString[]` の直列指定かめE`StoryAsset` 持E��へ刁E��替える、E- `StoryAsset` は `ScriptableObject` として作�Eし、E つのアセチE��冁E��斁E��表示、キャラクター表惁E��更、文章間ウェイトを頁E��に構�Eできるようにする、E- 会話 UI 側は `StoryAsset` の吁E��チE��プを頁E��再生するだけに留め、表示斁E��表惁E��刁E�E実データは `StoryAsset` に寁E��る、E- 表惁E��更対象もシーン探索ではなぁE`SerializeField` 参�Eで受け取り、`StoryAsset` 側では刁E��替え�Eの Sprite を�E示参�Eする、E- ゲームシーケンスにチャプター概念を追加し、`SequenceManager` が現在チャプターを保持して進行する、E- 吁E��ャプターは開始会話、�E功会話、失敗�E岐会話めE`SerializeField` 参�Eで持てる構�Eにする、E- クリア条件判定�E操作パート完亁E��に評価し、失敗時は判定結果に応じた会話を挟んで同一チャプターの操作パートへ戻す、E- 成功時�E次チャプターへ進み、最終チャプター成功後�Eみ完亁E��示へ遷移する、E- Title シーンから Game シーンへ開始チャプターを渡す際は、永綁EGameManager めEPlayerPrefs ではなく、次回ロード時に 1 回だけ消費する静的な遷移コンチE��ストで chapterId を渡す、E- 最終チャプター成功後�E `SequenceManager` でエンチE��ング用 `StoryAsset` を�E生し、その完亁E��に `SceneTransitionManager` 経由で `Title` シーンへ戻す、E- エンチE��ング導線でもシーン探索は使わず、`SequenceManager` の `SerializeField` でエンチE��ング `StoryAsset` と戻り�Eシーン名を管琁E��る、E- `SequenceManager` の Legacy Fallback は廁E��し、`chapters` が未設定なら互換動作に落とさず設定エラーとして停止する、E- 操作パートでは出来事カーチEUI を常時表示し、クリチE��で表裏を刁E��替え、ドラチE��で並び頁E��入れ替えられるようにする、E- カード�E並び替え�E `HorizontalLayoutGroup` 任せ�E瞬間移動ではなく、DOTween を使ったスライド移動で差し替え感が�Eかる見た目にする、E- カード詳細の補助説明�Eカード本体と別 UI に出し、�Eウスオーバ�E中だけ�EチE�EアチE�E表示する、E- チャプターごとのカード�E容、�E期表裏、正解の頁E��と表裏状態、�Eバ�E説明文はチE�Eタとして保持し、`OperationPartManager` へ `SerializeField` 参�Eで渡す、E- カード�E入力�E琁E��並び替え、表示更新、正解判定�E `OperationPartManager` に抱え込まず、操作パート専用の別クラスへ刁E��出して責務�E離する、E- カードドラチE��直後�E通常クリチE��ぁE1 回無効になる不�E合�E、ドラチE��中だけクリチE��抑止し、ドラチE��終亁E��に抑止状態を確実に解除する形で修正する、E- 操作パート�E吁E�E来事カード�E、カード単位で「表裏�E刁E��替え可否」と「頁E��の入れ替え可否」を個別設定できるようにする、E- 操作不可の頁E��はコード�Eで探索補完せず、`OperationChapterAsset` のカード定義チE�Eタにフラグとして保持する、E- `canReorder` ぁE`false` のカード�E他カード�E移動でも�E置スロチE��を動かさず、`canReorder` ぁE`true` のカードだけを可動スロチE��冁E��並べ替える、E- 操作パートには、現在チャプターの開始時点のカード並びと表裏状態へ戻すリセチE��ボタンを追加する、E- リセチE��処琁E�E `OperationChapterAsset` の定義頁E�� `startsFlipped` を�E期状態として扱ぁE��操作パート専用のカード制御クラス冁E��復允E��る、E- 操作パート失敗時の再挑戦ではカード�E置と表裏状態を維持し、�E期�E置への自動リセチE��はリセチE��ボタン押下時のみ行う、E
## 2026-03-19 作業メモ

- `Game` シーンでは会話パ�Eトと操作パート�E刁E��替えに合わせて、`Character` 配下�E立ち絵ルートを位置アニメーションさせる、E- 操作パート開始時は机に視線が寁E��よう、立ち絵ルートを上方向へ移動する演�Eを�Eれる、E- 会話パ�Eト開始時は、立ち絵ルートを会話用のチE��ォルト位置へ戻すアニメーションを�Eれる、E- こ�E刁E��替え演�Eは `SequenceManager` の `SerializeField` 参�Eで制御し、シーン探索で対象を補完しなぁE��E- 立ち絵ルート�E移動�Eが現在地と同一の場合�E、不要な Tween を作らずアニメーションをスキチE�Eする、E- 会話ログ機�Eを追加し、`Game` シーン右上�Eログボタンから会話履歴パネルを常時開けるようにする、E- 会話ログは `ConversationPartManager` 配下�E専用コンポ�Eネントで最大 100 件まで保持し、`StoryAsset` の `ShowMessage` スチE��プ確定時に追記する、E- ログパネルの UI は `Game` シーン冁E��明示配置し、�Eタン、パネル、スクロール表示先�E `SerializeField` 参�Eで接続する、E- ログ一覧はスクロール可能な縦並び UI で表示し、会話進行中でも開閉できるが、ログ操作�Eためにシーン探索めE��皁EUI 生�Eは行わなぁE��E- 会話ログ修正では、E��閉制御コンポ�Eネント�E身は常時有効のまま残し、E��表示対象は冁E��ウィンドウだけに限定して再オープン不�Eを避ける、E- 会話ログのスクロール領域には縦スクロールバ�Eを追加し、`ScrollRect` と `SerializeField` 参�Eで明示接続する、E- `Title` シーンの `OptionsPanel` に、E��量スライダー、`Japanese` / `English` の言語�Eり替え、クレジチE��表記を追加する、E- オプション頁E��の表示斁E��とクレジチE��本斁E�E `Assets/Localize` 配下�E String Table で管琁E��、`OptionUI` の `SerializeField` 参�Eから反映する、E- 音量設定と言語設定�Eタイトル画面で変更後すぐ反映されるよぁE��し、�E起動後も保持する、E- タイトル画面の言語�Eり替え�E 2 ボタン固定ではなく、E つのボタンを押すたびに `Japanese` / `English` が交互に刁E��替わる仕様にする、E
## 2026-03-21 作業メモ

- チャプター開始演�Eの再生開始時に、`SETable` の `ChapterStart` めE1 回だけ�E生する、E- `OperationPanel` の出来事カード表示では、各カード�E間に `▶` を見せて時系列方向が刁E��るよぁE��する、E- 時系列記号はカーチEprefab に含めず、`OperationPanel` 上�E別 prefab めE`SerializeField` 参�Eで配置してカード間へ並べる、E- 時系列記号の位置更新は操作パート�Eカード制御クラス冁E��行い、カード移動や裏返しとは独立した表示として扱ぁE��E- 時系列記号が表示されなぁE���E合�E確認では、`timelineMarkerPrefab` のシリアライズ参�E、生成条件、Unity Console のログを優先して刁E��刁E��る、E- 操作パート�Eカードを通常操作でめくったとき�E `SETable` の `FlipCard` を�E生する、E- 初期配置リセチE��に伴ぁE��裏復允E��は既存�Eめくりアニメーションは維持しつつ、`FlipCard` は再生しなぁE��E- 操作パート�Eカード並び替えで実際に位置入れ替えが発生したとき�E `SETable` の `ReplaceCard` を�E生する、E- 初期配置リセチE��に伴ぁE��ード�E配置では `ReplaceCard` を�E生しなぁE��E- `StoryAsset` のスチE��プ編雁E�E `enum + 1クラス` 構�Eを維持し、`CustomPropertyDrawer` で `StepType` ごとに忁E��な頁E��だけを Inspector 表示する、E- `StoryAsset` のサウンド演�EスチE��プに `StopBGM` を追加し、BGM 停止は即停止ではなくフェードアウトで行う、E- `StopBGM` スチE��プ�Eフェード秒数は `StoryAsset` 側のシリアライズ頁E��で持E��し、会話再生時に `AudioManager` へ明示皁E��渡す、E- 会話パ�Eト�E斁E��送り中だけ、`SETable` の `Conversation` キーを使ったタイプ音 SE を鳴らす、E- タイプ音 SE は `ConversationPartManager` の斁E��表示進行に追従して再生し、会話斁E�E全斁E��示後や会話外では鳴らさなぁE��E- 斁E��送り SE の呼び出し�E `ConversationPartManager` 冁E��完結させ、追加のシーン探索めE��皁E��ブジェクト生成�E行わなぁE��E- `Opening_Story` 冁E�E `TitleCall` で設定しぁE`PlaySe` が鳴らなぁE��は、`StoryAsset` のチE�Eタ冁E��、`ConversationPartManager` の刁E��、`SETable` 登録状況、Unity ログを優先確認して原因を特定する、E- `Game` シーンの吁E��ャプター開始時に、画面全体を暗くした上で中央へ黒帯を表示し、`CHAPTER 1` のような章開始タイトルを短ぁE��ニメーションで見せる演�Eを追加する、E- こ�E章開始演�Eは `SequenceManager` から専用コンポ�Eネントを `SerializeField` 参�Eして呼び出し、シーン探索で対象 UI を補完しなぁE��E- 章開始演�Eのオーバ�Eレイ UI とチE��スト�E `Game` シーン冁E��明示配置し、表示斁E��はチャプター定義チE�Eタ側で章ごとに設定できるようにする、E- `ChapterIntroOverlay` の開閉ではコンポ�Eネント�E身を非活性化せず、オーバ�Eレイ表示の見た目と入力�E断だけを刁E��替えて再生不�Eを避ける、E
- 操作パート�E出来事カード背景は `Assets/Sprites/UI` 配下�E表面・裏面スプライトを `OperationCardView` の `SerializeField` 参�Eで刁E��替える、E- カード�E表裏�Eり替え演�Eは DOTween で横方向に畳んでから開く「めくり」アニメーションにし、見た目の差し替え�Eアニメーション中点で行う、E- カード背景スプライト�E参�Eは Prefab 側で保持し、実行時に `Resources` めE��ーン探索で補完しなぁE��E- 出来事カード�E斁E��色は表面・裏面で個別持E��できるようにし、`OperationCardView` の `SerializeField` で事実文と解釈文それぞれの前後色を持つ、E- `SequenceManager` にチャプター開始前の全体オープニング `StoryAsset` を追加し、ゲーム開始時に 1 回だけ�E生してから吁E��ャプターへ入る構�Eにする、E- オープニング中のタイトル表示は `StoryAsset` のスチE��プとして扱ぁE��表示対象の Sprite と表示允EUI は `SerializeField` 参�Eで明示接続する、E- タイトル Sprite の生�Eと `Game` シーンへの配置・参�E配線�E Unity Editor 上で行い、コード�Eでシーン探索して補完しなぁE��E- オープニングのタイトル Sprite 表示中は会話メチE��ージウィンドウを非表示にし、タイトルだけを画面に見せる、E- タイトル Sprite は表示後に即消しせず、次の進行へ移る際にフェードアウトしてから非表示にする、E- タイトル Sprite 表示中はクリチE��めEEnter によるスキチE�Eを受け付けず、`StoryAsset` 側の `waitSeconds` 経過後に自動でフェードアウトさせる、E- `ShowMessage` スチE��プ�E `waitForAdvance == false` かつ `waitSeconds > 0` の場合、�E力征E��ではなく指定秒数ぶん�E動征E��してから次へ進める、E- `ShowMessage` スチE��プで `waitForAdvance == false` の場合�E、`AdvanceIndicatorText` を表示せず自動進行中であることを維持する、E- `ShowMessage` スチE��プで `waitForAdvance == false` の場合�E、�E動征E��へ入る直前にめE`AdvanceIndicatorText` を�E示皁E��非表示化する、E- `ShowMessage` 以外�E `StoryAsset` スチE��プへ入る際は、各刁E���E処琁E��に `AdvanceIndicatorText` を�E示皁E��非表示化する、E- オープニングのタイトル Sprite はフェードアウト完亁E��、次のメチE��ージへ移る前に 1 秒征E��を挟�E、E- `Wait` スチE��プには会話メチE��ージウィンドウの表示有無を�Eり替えるオプションを追加し、タイトル後�E征E��ではウィンドウを表示しなぁE��E- `StoryAsset` の `Wait` スチE��プ中は `AdvanceIndicatorText` を表示せず、征E��完亁E��で次送り征E��表示を�EさなぁE��E- Title 画面のチャプター選択�Eタンから開始した場合�Eみ、`SequenceManager` の全体オープニング `openingStory` はスキチE�Eして選択チャプターへ直接入る、E- `New Game` 開始では従来どおり全体オープニングを�E生し、E��始経路ごとの差刁E�E静的な遷移コンチE��ストで 1 回だけ消費する、E- 操作パートで初期配置へ戻す際、カード�E表裏状態が開始時点から変わってぁE��カードだけ既存�E裏返しアニメーションを�E生してから初期状態へ戻す、E- サウンド管琁E��は `AudioManager` が参照する `BGM` / `SE` の登録チE�EブルめE`ScriptableObject` として刁E��し、実アセチE��は `Assets/ScriptableObjects/Sounds` 配下で管琁E��る、E- 音源参照は斁E���EパスめE`Resources` を使わず、各チE�Eブルのシリアライズ済みエントリで保持する、E- `StoryAsset` には会話演�EスチE��プ�E一種として `BGM` 再生と `SE` ワンショチE��再生を追加し、会話再生中のタイミング制御をデータ側で行えるよぁE��する、E- `StoryAsset` のサウンドスチE��プ�E `AudioManager` の登録キー斁E���Eを参照し、E��量やループ指定もスチE��プ�Eのシリアライズ頁E��で持つ、E
## 2026-03-22 作業メモ

- `Title` シーン入場時に `TitleManager` から `AudioManager` を通して `Key` の BGM を�E動�E生する、E- タイトル BGM のキー、E��量、ループ設定�E `TitleManager` の `SerializeField` で管琁E��、シーン探索めE��字�Eパス補完�E増やさなぁE��E- すでに同じ BGM が�E生中ならタイトル再�E場時に不要な再�E生を行わなぁE��E- カード操作シーンのマウスオーバ�E補助チE��スト表示は当面オミットし、�E利用しやすいよう関連処琁E�E削除せずコメントアウトで停止する、E
## 現在使える前提

- Unity 6 `6000.3.2f1`
- URP 2D 設定済み
- Input System 設定済み
- TextMesh Pro 導�E済み
- DOTween / DOTween Pro 導�E済み
- UniTask 導�E済み
- `Assets/Scenes/SampleScene.unity` がビルド対象

## 現在のフォルダ方釁E
- `Assets/Scenes`: 実�Eレイ用シーン
- `Assets/Scripts`: ゲーム固有スクリプト
- `Assets/Prefabs/Characters`: プレイヤーめENPC
- `Assets/Prefabs/Enemies`: 敵
- `Assets/Prefabs/Environment`: 地形、E��害物、背景要素
- `Assets/Prefabs/Systems`: ゲーム管琁E��カメラ、スポナー、UI 以外�E共通機�E
- `Assets/Prefabs/UI`: HUD、メニュー、リザルチE- `Assets/ScriptableObjects/Databases`: バランス値めE��義チE�Eタ
- `Assets/ScriptableObjects/Stories`: 会話、演�E、E��行用の `StoryAsset` チE�Eタ
- `Assets/Sprites`, `Assets/Animations`, `Assets/Materials`, `Assets/Audio` or `Assets/Sounds`: 素材格紁E
## 入力�E前提

`Assets/InputSystem_Actions.inputactions` には、すでに以下�E汎用アクションがあるため、テーマ決定前はこれを流用する、E
- `Move`
- `Attack`
- `Interact`
- `Jump`
- `Sprint`
- `Crouch`
- `Previous`
- `Next`
- `UI` 系の標準操佁E
チE�Eマ決定後も、最初�E試作では入力アセチE��を増やしすぎなぁE��E 
不足が�Eるまでは既存アクション名に寁E��る、E
## ゲームジャム向け実裁E��釁E
- 最初�E半日で、E ループ遊べる状態」を作る
- チE�Eマ解釈より�Eに、操作して気持ちぁE��コア体験を 1 つ決める
- スチE�Eジ数より、失敗と再挑戦のチE��ポを優先すめE- シスチE��は使ぁE��せる粒度で作るが、E��剰な抽象化�EしなぁE- ScriptableObject は敵設定、アイチE��設定、スチE�Eジ設定など再利用効果が高い箁E��だけに使ぁE- 演�Eは DOTween で素早く付けめE- 非同期ロードや演�E征E��は UniTask を使って褁E��なコルーチンを増やしすぎなぁE
## チE�Eマ決定後に最初に決めること

1. プレイヤーぁE10 秒以冁E��琁E��できる目皁E2. 主要アクションめE1 つから 2 つ
3. 敵また�E障害物の基本ルール
4. 1 プレイの長ぁE5. リトライ導緁E
こ�E 5 点が曖昧なまま作り始めなぁE��E 
送E��ここが決まれ�E、アートや演�Eは後追ぁE��よい、E
## 最小実裁E�E優先頁E
1. プレイヤー操佁E2. カメラ
3. 勝敗条件
4. 1 種類�E障害物また�E敵
5. タイトルまた�E開始導緁E6. リザルトまた�E再開導緁E7. SE
8. 画面演�E
9. BGM

## 避けること

- 初日から褁E��シーン構�Eに庁E��ること
- 汎用フレームワーク化に時間を使ぁE��と
- 入力、セーブ、設定画面を最初から作り込むこと
- 未使用フォルダを増やし続けること
- `Audio` と `Sounds` を用途未整琁E�Eまま併用すること

## アセチE��参�Eと生�Eルール

- `Resources` フォルダは新規作�EしなぁE- `Resources.Load`、`Resources.LoadAsync` など `Resources` 経由の実行時ロード�E使わなぁE- 実行時に `new GameObject` めE`AddComponent` を絁E��合わせて GameObject をその場生�EしなぁE- 実行時に生�Eが忁E��なオブジェクト�E、忁E�� Prefab 化して Inspector から `SerializeField` で参�Eを受け取めE- 実行時生�Eは、Inspector で割り当てぁEPrefab めE`Instantiate` して行う
- 斁E���Eパスベ�EスでアセチE��を引く実裁E��り、Prefab 参�EめEScriptableObject 参�Eを優先すめE- `GameObject.Find`、`Transform.Find`、`FindFirstObjectByType`、`FindAnyObjectByType` などの探索参�Eは新規実裁E��使わなぁE- オブジェクト参照は Inspector からの `SerializeField` 割り当てを第一候補にし、シーン冁EUI めE��琁E��ブジェクトもできるだけ�E示参�Eで接続すめE- 参�E不足をコード�E探索で補完しなぁE��忁E��な参�Eがあるなら、Prefab めEScene にシリアライズして持たせる
- 例外が忁E��なら、�Eに `AGENTS.md` に琁E��と許容篁E��を追記してから着手すめE
## 運用メモ

- 音素材�E置き場は早めに `Assets/Audio` ぁE`Assets/Sounds` のどちらかへ統一する
- `SampleScene` は早めにゲーム名に沿った名称へ変更してよい
- ビルド対象シーンは常に最小限に保つ
- 追加アセチE��を�Eれる前に「今回のゲームに忁E��か」を確認すめE
## こ�EチE��プレートで向いてぁE��も�E

- 1 画面完結�Eアクション
- 軽ぁE��索めE��雁E- ウェーブ制の生存系
- 物琁E�Eースの小規模ギミック
- スコアアタチE��型�E反復プレイ

## こ�EチE��プレートで後回しにしたぁE��の

- 大規模な会話シスチE��
- 重い成長要素
- 褁E��なインベントリ
- 庁E��ワールド�E移
- ネットワーク要素

## 推琁E��ーム実裁E��イチE
### ゲーム概要E
本作�E、証拠カード�E解釈を裏返すことで事件の矛盾を解消し、真相に近づぁE��ぁE��推琁E��ームである、E
プレイヤーは探偵として事件賁E��を調査し、提示された�E来事を時系列で整琁E��ながら、それぞれ�E出来事に対する解釈を刁E��替えてぁE��、E重要なのは、カードに書かれてぁE��冁E��は事実そのも�Eであり、変化するのはそ�E事実�E意味づけだけである、とぁE��点である、E
コア体験�E以下�E 2 つで構�Eする、E
- 同じ事象でも、見方によって意味が送E��する
- 出来事を正しい時系列と正しい解釈で並べたとき、事件全体が琁E��できる

### コアゲームルーチE
プレイヤーは以下を繰り返す、E
1. 出来事カードを確認すめE2. 吁E��ード�E解釈を表裏で刁E��替える
3. カードを時系列頁E��並べめE4. 現在の並びと解釈�E絁E��合わせから矛盾が解消されてぁE��かを確認すめE5. 条件を満たすと次の推琁E��階や物語進行が解放されめE
新しい証拠を大量に追加することよりも、既にある証拠の意味を読み替えることを主目皁E��する、E
### コアギミック: 解釈反転

吁E�E来事カード�E以下�E惁E��を持つ、E
- 変化しなぁE��実テキスチE- 表の解釁E- 裏�E解釁E
カードに書かれた事実�E体�E固定であり、�Eレイヤーはカードを裏返すことで別の解釈を選択できる、E
表の解釁E
警察�E推琁E��一般皁E��表面皁E��つ犯罪皁E��見える解釈。�E期状態で表示される想定、E
裏�E解釁E
プレイヤーが見抜く別視点の意味。行動の意図めE��場を反転させる解釈で、真相に近づく鍵となる、E
反転対象は特に行動の解釈反転を重視する、E
- 犯行か救助ぁE- 送E��か追跡ぁE- 襲撁E��警護ぁE- 証拠探しか証拠隠しか

行動自体ではなく、その行動の意図を反転させる、E
### 出来事カード仕槁E
吁E��ード�E最低限以下を持つこと、E
- `id`
- `factText`
- `frontInterpretation`
- `backInterpretation`
- `isFlipped`
- `correctTimelineOrder`
- `tags`

想定侁E

- 事宁E 被害老E�E後をつけてぁE��
  表: 襲ぁE��会をぁE��がってぁE��
  裁E 被害老E��警護してぁE��
- 事宁E 被害老E�E家に忍�E込んだ
  表: 犯行�Eために侵入した
  裁E 被害老E��助けるために侵入した
- 事宁E 被害老E�E机をあさってぁE��
  表: 物色して証拠を探してぁE��
  裁E 危険な証拠を隠してぁE��
- 事宁E 現場から走り去っぁE  表: 犯人として送E��した
  裁E 真犯人を追ぁE��けた

### もう一つのコアギミック: 時系列�E構�E

プレイヤーは、�E来事カードを正しい頁E��に並べることで、事件当夜に何が起きたかを再構�Eする、E
吁E��拠は単体では意味が曖昧であり、以下�E 2 つが揃ったときにのみ真相が見える、E
- 正しい時系刁E- 正しい解釁E
単なるカード反転パズルではなく、E��E��と意味の両方を整える推琁E��ズルとして設計すること、E
進行判定では以下を確認できるようにする、E
- カード頁E��正しいぁE- 吁E��ード�E表裏状態が正しいぁE- 忁E��に応じて、特定カード同士の因果関係が成立してぁE��ぁE
### 事件構造

ゲーム開始時点では、警察�E推琁E��提示されてぁE��、E
- 警察�E全ての証拠を表面皁E��解釈してぁE��
- そ�Eため、ある人物を犯人だと見なしてぁE��
- プレイヤーはそ�E推琁E�E矛盾を、証拠の読み替えによって暴ぁE��ぁE��

プレイヤーの役割は以下である、E
- 新証拠を大量に発見することではなぁE- 既存証拠の意味を読み替えること
- 時系列を整琁E��ること
- 矛盾のなぁE��貫した物語を再構築すること

### スト�Eリー上�E前提

容疑老E��されてぁE��人物は、真実を知ってぁE��が、それを警察に話せなぁE��情がある、E
- 彼が真相を語ると、犯罪絁E��に裏�Eり老E��見なされめE- そ�E結果、彼が守ろぁE��してぁE��人物に危険が及ぶ

そ�Eため彼は沈黙してぁE��。警察から見ると、その沈黙と不審な行動が犯人らしく見える、Eしかし�Eレイヤーが行動の意味を頁E��に解き�EかしてぁE��ことで、彼の行動が実�E被害老E��守るためのも�Eだったと刁E��る、E
### プレイヤー体験として重視すること

こ�Eゲームの面白さ�E、事実を増やすことではなく、同じ事実�E見え方が反転することにある、E
- 最初�E明らかに犯人に見えめE- だが、カードを裏返すと印象が変わめE- さらに時系列を正しく並べると、矛盾が消えて一本の筋になめE- 最終的に、犯人に見えた人物が実�E守ろぁE��してぁE��と琁E��できる

特に重要なのは、すべてを裏返せば正解にならなぁE��計にすること、Eカードごとに、表が正しい場合も裏が正しい場合もありぁE��構�Eを望ましいも�Eとする、E
### 実裁E��釁E
最低限、以下�E機�Eを実裁E��ること、E
- 出来事カード�E一覧表示
- カードごとの表裏�Eり替ぁE- カード�E並び替ぁE- 現在の並び頁E��解釈状態�E保孁E- 正解判宁E- 正解時�Eスト�Eリー進衁E
カード�E容めE��解条件は、できるだけデータとして定義すること。ロジチE��に直書きせず、章追加めE��件追加がしめE��ぁE��造にする、E
推奨チE�Eタ:

- 事件チE�Eタ
- カードデータ
- 判定条件チE�Eタ
- 会話、演�EチE�Eタ

今後追加しやすい構造を前提とする、E
- 褁E��事件
- 褁E��章構�E
- 新しい解釈パターン
- ダミ�E選択肢
- 中間演�E
- 一部カード�Eみ追加条件付きで反転可能

### UI/UX 上�E注愁E
- カード�E事実テキスト�E常に不変であると刁E��る見せ方にする
- 反転によって変わる�Eは注釈、意味、推琁E��ベルであると明確にする
- 表裏�Eり替え�E直感的で気持ちよい演�Eにする
- 並び替え後に、時系列が変わった実感を持てるよぁE��する
- 解釈変更によって矛盾が減ったことが�EかるフィードバチE��を用意すめE
### NG 事頁E
以下�E避けること、E
- カード�E事実文そ�Eも�Eが変わる実裁E- 全カードを裏返すだけで正解になる単純設訁E- 時系列要素が形だけで、E��E��が推琁E��影響しなぁE���E
- 表解釈と裏解釈�E差が弱く、反転の驚きがなぁE��ザイン
- プレイヤーが証拠を増やすゲームだと誤解する UI

### こ�Eゲームの一斁E��紁E
証拠カード�E解釈を裏返し、時系列を再構�Eすることで、犯人に見えた人物の行動が実�E救助だったと見抜ぁE��ぁE��推琁E��ーム、E
## 合言葁E
チE�Eマに合わせて庁E��る�Eではなく、まず遊べる芯を作り、既存アセチE��で短く強ぁE��験に寁E��る、E
## Async Rule

- Unity Editor の確認、E��層取得、スクリーンショチE��、ログ確認、コンパイル、テスト、PlayMode 操作など、Codex からの Unity 操作�E今征E`uloop` を優先して使ぁE- Unity 用の MCP / 自動操作手段を選べる場合も、まぁE`uloop` のスキルと CLI を確認し、特別な琁E��がなぁE��めE`uloop` を第一候補にする

- 非同期�E琁E�E忁E�� UniTask を使ぁE- `Coroutine`、`IEnumerator`、`StartCoroutine` は新規実裁E��使わなぁE- フレーム征E��、時間征E��、`AsyncOperation` の征E��も UniTask で統一する
## 2026-03-22 調査メモ

- `Title` シーン起動時にタイトル BGM が鳴らなぁE��は、`TitleManager` のシリアライズ値、`AudioManager` 配置、Unity ログを優先確認して原因を�Eり�Eける、E- 原因が�E期化頁E��シーン冁E��照にある場合�E、既存�E `SerializeField` / Prefab 構�Eを崩さず最小修正で対応する、E- `Title` シーンの `DeveloperImage` には URL 遷移用ボタンを追加し、押下時は `TitleManager` 経由で `Application.OpenURL` を呼ぶ、E- 遷移允EURL とボタン参�Eは `TitleManager` の `SerializeField` で明示保持し、`Title` シーン上で配線する、E- `Title` シーンのオプション表示時にローカライズ反映前�E初期チE��ストが一瞬見える不�E合�E、`OptionUI` 側で表示前に忁E��な斁E��更新を完亁E��せてからパネルを開く形で修正する、E- 修正ではシーン探索を増やさず、既存�E `SerializeField` 参�Eと UniTask ベ�Eスの初期化頁E��御だけで対応する、E- `Title` シーンのオプション初回クリチE��で開かなぁE���E合�E、`OptionUI` を載せたルートを常時有効のまま維持し、背景と冁E��ウィンドウだけを `SerializeField` 参�Eで開閉する構�Eへ見直して対応する、E- `Title` シーンのオプション再オープン時に `UniTask` 冁Eawait 例外が出る不�E合�E、`OptionUI` の初期化征E��タスクを�E利用可能な形で保持し、E 回目以降�E表示でも同じ�E期化状態を安�Eに参�Eできるよう修正する、E
## 2026-03-24 作業メモ

- 操作パート�Eカード説明補助は、カード追従�EチE�EアチE�Eを使わず、`Game` シーン冁E��常設の説明テキストエリアを追加してマウスオーバ�E中のカード説明を表示する、E- 説明テキストエリアの表示更新とクリアは `OperationPartManager` / 操作パート専用カード制御クラスの `SerializeField` 参�E冁E��完結させ、シーン探索めE��皁EUI 生�Eで補完しなぁE��E- 操作パート�E説明テキストエリアは、未ホバー時�E初期メチE��ージだけ文字色を薄くし、カード説明表示時�E通常色へ戻す、E- 操作パートでカード反転アニメーション中にポインターイベントが一瞬外れても、同一カード上�Eホバー説明�E維持し、不要に初期メチE��ージへ戻さなぁE��E
## 2026-03-25 作業メモ

- 操作パート失敗時のスト�Eリー刁E���E、従来の `judgementId` だけでなく、カードごとの表裏状態と現在の並び頁E��条件として評価できるようにする、E- 失敗�E岐条件はチャプターごとにリストで `SerializeField` 設定できる構�Eにし、褁E��条件を同時に満たす場合�E配�Eの先頭要素を優先して採用する、E- 条件評価に忁E��な現在のカード状態�E操作パート専用カード制御クラスから取得し、`SequenceManager` へ結果として渡す。シーン探索めE��皁E��参�E補完�E追加しなぁE��E- 条件に使ぁE��ード識別子�E `OperationChapterAsset` のカード定義 `id` を基準にし、�E岐条件側でも同ぁE`id` めE`SerializeField` で明示持E��する、E
## 2026-03-25 SpaceスキチE�Eメモ

- 会話パ�Eトでは Space キー長押しで高速スキチE�Eを受け付ける、E
- 高速スキチE�Eは ConversationPartManager 冁E��処琁E��、文字送り中は即時�E斁E��示、その後�E短ぁE��隔で次送りを�E動発火する、E
- スキチE�E対象は会話メチE��ージと会話用 Wait スチE��プに限定し、タイトル Sprite 表示の非スキチE�E仕様�E維持する、E

- ShowFactCard が機�EしなぁE��能性を確認する際は、ConversationPartManager の StoryStepType.ShowFactCard 刁E��、ConversationFactCardOverlay の参�E状態、Unity Console ログを優先して刁E��刁E��る、E

## 2026-03-26 ��ƃ���

- ����p�[�g�̐��딻��ɁA�J�[�h�̕\����Ԃ𖳎����ď��Ԃ����Ő�������ł���`���v�^�[�P�ʃI�v�V������ǉ�����B
- ���̔���I�v�V������ OperationChapterAsset �̃V���A���C�Y���ڂŕێ����A�V�[���T������s���⊮�͍s��Ȃ��B
- Chapter2Operation �ł͂��̃I�v�V������L�������A���������ԂȂ�\����ԂɊ֌W�Ȃ� successJudgementId ��Ԃ��B
## 2026-03-27 ��ƃ���
- Ending_Story �� Localization ���ڑ��̂܂� messageFallback �x�[�X�ŉ�b�e�L�X�g��ێ����A�G���f�B���O�{�҂ƃG�s���[�O���ʂ� StoryAsset �� ShowMessage step �ŏ��Ԃɕ\������B
- �G���f�B���O���� �`�Ó]�`�A���G�s���[�O�A�`�G���h�^�C�g���` ����p UI �ǉ��ł͂Ȃ���b�e�L�X�g�Ƃ��� Ending_Story �Ɋ܂߂ĕ\������B

- StoryAsset �� ShowEndSprite �X�e�b�v��ǉ����A������ ShowTitleSprite �Ɠ��� UI �\���o�H�Ƒҋ@�E�t�F�[�h�A�E�g�����ōĐ��ł���悤�ɂ���B
- ShowEndSprite �̕\���f�[�^�� StoryAsset �̃V���A���C�Y���ڂŕێ����A���s���ɃV�[���T���� Resources �Q�Ƃŕ⊮���Ȃ��B
## 2026-03-27 ����J�[�hLock�\������

- ����p�[�g�̃J�[�h�ŕ\�����삪�����ȍ��ڂ́AOperationCard prefab ���� LockImage ��\�����đ���s�𖾎�����B
- LockImage �̕\������� OperationCardView �� SerializeField �Q�Ƃōs���A�V�[���T������s���⊮�͍s��Ȃ��B

## 2026-03-27 ����s�J�[�h���s�t�B�[�h�o�b�N����

- canFlip �� alse �̃J�[�h���N���b�N�����Ƃ��́AOperationCardView ��Ōy���V�F�C�N���o���Đ����đ��쎸�s�����o�I�ɓ`����B
- ���s�t�B�[�h�o�b�N�͊����J�[�h View ���Ŋ��������A�V�[���T����ǉ��I�u�W�F�N�g�����͍s��Ȃ��B

## 2026-03-27 ���s�V�F�C�N�ē��C������

- ����s�J�[�h�̎��s�V�F�C�N�́ATween �ē����ł��J�[�h�̃��C�A�E�g��ʒu�֕K���߂�悤�ɂ���B
- �V�F�C�N�̊�ʒu�� OperationCardView ���ŕێ����A�N���b�N�A�łő��Έʒu������Ȃ��悤�ɂ���B

## 2026-03-27 Opening_Story 追記メモ

- `Opening_Story` への斁E��追加は、既孁E`messageFallback` を上書きせず、未登録の斁E��だけを追加する、E- タイトル表示位置など既存スチE��プ�E構�Eは維持し、忁E��な追記だけで会話頁E��整える、E

## 2026-03-27 Opening_Story 事件説明追記メモ

- `Opening_Story` の事件説明�E、既存文言を削除せず、追加持E��された説明文と `ShowFactCard` / `StackFactCard` を頁E��に追記する、E
- `ShowFactCard` のカーチEID は `OperationChapterAsset` の定義に合わせ、存在しなぁEID を増やさなぁE��E

## 2026-03-27 Opening_Story 末尾追記メモ

- `Opening_Story` の追記依頼は、既存スチE��プを編雁E��ぁE`steps` 配�Eの末尾へ追加する、E
- 斁E��めE��ード表示コマンド�E、その時点でユーザーが渡した冁E��だけを反映する、E

## 2026-03-27 Opening_Story 末尾追訁Eメモ

- `Opening_Story` の後続追記も既存スチE��プを編雁E��ず、`steps` 配�E末尾へ積み増す、E
- カード表示コマンド�E ID は実在定義を優先し、�E力�E綴り違ぁE�E既存アセチE��に合わせて補正する、E


## 2026-03-27 C1_SuccessStory �ǋL����

- C1_SuccessStory �ɂ͐������̉�b�⋭�Ƃ��āA�̎^�̓����Z���t�ƒǉ������e�L�X�g�� StoryAsset �� steps �����֒ǋL����B
- �ǋL���̊���ω��� ChangeExpression �X�e�b�v�ŋ��݁A�\��؂�ւ��������� Enum / SerializeField �\���̂܂܈����B

## 2026-03-27 C2_OpeningStory �ǋL����

- C2_OpeningStory �ɂ͓��������̕⋭���AShowFactCard / StackFactCard / ClearFactCards ���܂ސi�s�X�e�b�v�� steps �����֒ǋL����B
- ShowFactCard �̃J�[�h ID �͊��� Chapter2Operation ��`�ɍ��킹�A����` ID ��V�݂��������f�[�^�Q�Ƃō\������B

## 2026-03-27 C2_SuccessStory �ǋL����

- C2_SuccessStory �ɂ͐������o�̕⋭�Ƃ��āA�Z���̎^���Ɨv�񕶂� StoryAsset �� steps �����֒ǋL����B

## 2026-03-27 C3_OpeningStory �ǋL����

- C3_OpeningStory �ɂ͏͓����̒Z���� StoryAsset �� steps �֒ǋL���A�����̉�b�A�Z�b�g�\����������ǉ�����B

## 2026-03-27 C3_SuccessStory �ǋL����

- C3_SuccessStory �ɂ͐������̒ǉ���b�� StoryAsset �� steps �����֒ǋL���A�����̏͊������o��⋭����B

## 2026-03-27 Ending_Story �ǋL����

- �G���f�B���O�p StoryAsset �ɂ͖{�Ҍ����ƃG�s���[�O���� steps �����֒ǋL���A�����̉�b�A�Z�b�g�\����������ǉ�����B## 2026-03-27 StoryAsset ���O�Q�ƃ���

- StoryAsset �̉�b���ł� {name_0} �̂悤�ȃv���[�X�z���_���g���A���O�{�̂͐�p 
ameTable ScriptableObject �����������B
- 
ameTable �͊e�G���g�����Ƃɕ\�����ƐF�������A��b�\������ <color> �^�O�t��������֓W�J����B
- ���� A B C D �𒼐ڏ����Ă������ StoryAsset �́A���� {name_0} 
ame_1 
ame_2 
ame_3 �Q�Ƃ֒u��������B
