#!/bin/bash

# 引数チェック
if [ $# -eq 0 ]; then
    echo "Usage: $0 <folder_path>"
    exit 1
fi

target_folder="$1"

# フォルダの存在確認
if [ ! -d "$target_folder" ]; then
    echo "Error: Folder '$target_folder' does not exist"
    exit 1
fi

cd "$target_folder"

# フォルダ内のすべてのファイルをループ
for file in *; do
    # ディレクトリはスキップ
    if [ -d "$file" ]; then
        continue
    fi

    # 正規表現でファイル名を分解
    # 1: 名前部分 (最初の"-"まで)
    # 2: 日付 (6桁)
    # 3: 番号 (3桁、後半部分から抽出)
    # 4: 拡張子 (.anim または .anim.meta)
    if [[ $file =~ ^([^-]+)-[^-]+-([0-9]{6})_.*_([0-9]{3})(_[^.]*)?(\.anim(\.meta)?)$ ]]; then
        prefix="${BASH_REMATCH[1]}"
        date_str="${BASH_REMATCH[2]}"
        number="${BASH_REMATCH[3]}"
        ext="${BASH_REMATCH[5]}"

        # 新しいファイル名を生成 (番号-名前-日付.拡張子)
        new_name="${number}-${prefix}-${date_str}${ext}"

        # 同名ファイルがなければリネーム実行
        if [ "$file" != "$new_name" ]; then
            echo "Renaming: $file -> $new_name"
            mv "$file" "$new_name"
        fi
    fi
done