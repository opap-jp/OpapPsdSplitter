OpapPsdSplitter
======================

PSDをトップレベルのレイヤグループで分割します。


使い方
-----------

<pre>
Usage: OpapPsdSplitter.exe [オプション]... [ファイル]...
PSDファイルをトップレベルのレイヤーグループ単位で分割します。
元のファイルには影響を与えません。

  -r     指定したディレクトリ及びサブディレクトリ内に存在する
         psdファイルを再帰的に検索して、すべて処理します。
  -o     OPAP-JP仕様のレイヤ構成において動画レイヤ（@マークで
         始まる名称のレイヤ）に含まれるサブレイヤグループを
         展開します。
  -e     トップレベルのレイヤグループが非表示状態になっている場合
         表示状態に変更する
  --outdir-prefix=PREFIX
         出力するフォルダ名の先頭に付けるプリフィクスを指定
         します。規定値は out- です。引用符は使用できません。
  --non-unicode-encoding=ENCODING
         非ユニコードのレイヤ名のエンコーディングを指定します。
         規定値は shift_jisです。引用符は使用できません。
</pre>


使い方の例
-----------

OpapPsdSplitter.exe -o -e aaaa.psd

out-aaaa.psdフォルダ内に、aaaa.psdをトップレベルのレイヤグループで分割した
psdファイルが出力されます。


OpapPsdSplitter.exe -o -e -r .

現在の作業ディレクトリ以下のpsdファイルを検索し、それぞれに対して分割処理を
行います。


いずれの場合でも、「--outdir-prefix=」オプションで指定された文字列または
「out-」から始まるディレクトリ内のpsdファイルは無視され、処理後に上書き
されます。


ライセンス
----------

本プログラムは、MITライセンスの下に利用が許諾されています。
詳細は、License.txtをご覧下さい。

Copyright (C) 2013 Butameron.


