![logo](https://raw.githubusercontent.com/sh0ou/AssetReferLinker/main/Packages/jp.sh0uroom.assetlinker/UI/logo.png)
# AssetLinker
- [日本語](https://github.com/sh0ou/AssetReferLinker/blob/main/README-JP.md)
- [English](https://github.com/sh0ou/AssetReferLinker/blob/main/README.md)

[![openupm](https://img.shields.io/npm/v/jp.sh0uroom.assetlinker?label=openupm&registry_uri=https://package.openupm.com)](https://openupm.com/packages/jp.sh0uroom.assetlinker/)

AssetLinkerは、特定のアセットのダウンロード先や利用規約のURL、ファイルパスなどの依存関係をJSONファイルとして保存し、ワンクリックで簡単に参照できるようにするツールです。

有償のアセットや再配布できないアセットを使用するプロジェクトで共同作業を行う場合などに役立ちます。

## 要件
- ✅️Unity 2022.3 以降
- ❓️Unity 2021.3
- ❌️Unity 2021.1 以前

# 言語設定
![image](https://github.com/sh0ou/AssetReferLinker/assets/47475540/abba2866-65a6-4ff0-8950-6f4126034a62)

上メニューのWindow>AssetLinker>Settings から変更可能です。現時点では日本語、英語に対応しています。<br/>
また、起動時に表示されるWelcomeウインドウの「Change Language」からも開くことができます。

# 使い方
## リンクの作成

UnityのProjectビューから、関連付けを行いたいアセットのルートフォルダ（一番上のフォルダ）を選択して右クリックします。

例えば、「Assets/AssetLinker/UI」といったフォルダがある場合、「Assets/AssetLinker」を開いた状態で右クリックします。

![image](https://github.com/sh0ou/AssetReferLinker/assets/47475540/5a7d74d2-872a-4abd-bb52-e04066cdcb89)

右クリックメニューが開いたら、下側にある「AssetLinker」を選択してください。

![image](https://github.com/sh0ou/AssetReferLinker/assets/47475540/f9f637de-7410-42ed-be61-ebf4cf9b5fdc)

選択しているアセットフォルダが正しいことを確認してください。<br/>
フォルダ名がそのままリンクファイル名になります。

アセットのダウンロード先（もしくは販売ページ）のURLを入力してください。
現時点では下記のURL検知に対応しています。
- AssetStore
- Github
- BOOTH
- Gumroad
- VketStore

これらに表示されていないURLもリンクは行えます。

![image](https://github.com/sh0ou/AssetReferLinker/assets/47475540/1b349e56-42b2-4dd5-a6e9-c592317e6243)

ダウンロードURLを入力すると詳細情報の入力画面が表示されます。
利用規約などのURL、アセットが無料かどうか、追跡対象とするファイルの選択などがここで行えます。

> [!WARNING]
> **追跡対象とするアセットは初期状態から変更しないでください！**
> 
> AssetLinkerは、アセットがそこにあるか、ないかのみを検証するツールです。<br/>
> アセットの状態はインポートしたものから一切変更を加えていないことを想定しています。
> 
> もしアセットの状態を保存して共有したい場合、アセットを利用してるGameObjectをPrefab化し、別のフォルダに保管するなどして、共有してください。
> 
> (UnityのPrefabファイルはモデルデータや画像データそのものは保存されず、値や状態のみが保存される仕様です)

「Link!」ボタンを押すとリンクが作成されます。<br/>
作成されたリンクファイルは /\[プロジェクトファイル名]/AssetLinker/ 内に保存されます。

## アセットの検証
![image](https://github.com/sh0ou/AssetReferLinker/assets/47475540/0ef9e170-52c8-466b-97e9-fb762a877921)

初期状態では、astlnkファイルが存在する場合に、そのアセットが存在するかどうかの検証が自動的に行われます。

アセットファイル、フォルダが存在しない場合、アセット確認ウインドウが自動的に表示されます。<br/>
ファイルパスの文字が赤いものは、プロジェクト内に存在しないアセットです。

> [!NOTE]
> ウインドウの自動表示はプロジェクト起動時に行われます。
> これは確認ウインドウの右下、もしくは設定画面から、次回から自動的に表示しないよう設定することもできます。

各アセットメニューには以下のボタンが存在します。<br/>
どのウインドウも、確認ウインドウでYesを選択した場合のみ実行されます。

- Download
  - ダウンロードページ、販売ページを開きます。
- License
  - 利用規約やライセンスが記載されたページを開きます。
- UnLink
  - AssetLinkerでの紐づけを解除します。該当する.astlnkファイルが削除されます。
 
#  不具合・要望
Githubのissue機能で受け付けています。<br/>
下記URLから投稿をお願いします。

[不具合報告](https://github.com/sh0ou/AssetReferLinker/issues/new?assignees=&labels=T%3A+Bug&projects=&template=-jp--%E4%B8%8D%E5%85%B7%E5%90%88%E5%A0%B1%E5%91%8A.md&title=)

[要望](https://github.com/sh0ou/AssetReferLinker/issues/new?assignees=&labels=P3%3A+Medium%2C+T%3A+Enhancement&projects=&template=-jp--%E8%A6%81%E6%9C%9B-%E6%8F%90%E6%A1%88.md&title=)

また、Pull Requestも歓迎しています。
