![logo](https://raw.githubusercontent.com/sh0ou/AssetReferLinker/main/Packages/jp.sh0uroom.assetlinker/UI/logo.png)
# AssetLinker
AssetLinkerは、特定のアセットのダウンロード先や利用規約のURL、ファイルパスなどの依存関係をJSONファイルとして保存し、ワンクリックで簡単に参照できるようにするツールです。

有償のアセットや再配布できないアセットを使用するプロジェクトで共同作業を行う場合などに役立ちます。

# 言語設定
![image](https://github.com/sh0ou/AssetReferLinker/assets/47475540/abba2866-65a6-4ff0-8950-6f4126034a62)

上メニューのWindow>AssetLinker>Settings から変更可能です。現時点では日本語、英語に対応しています。

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
