#!/bin/bash

# 使用方法を表示する関数
show_usage() {
    echo "使用方法: $0 <対象フォルダ> <ファイル名パターン> <置き換え前のパス> <置き換え後のパス>"
    echo ""
    echo "例:"
    echo "  # 単一のパスを置き換え"
    echo "  $0 ./Assets '*.anim' 'camera' 'new_camera'"
    echo ""
    echo "  # 複数のパスから置き換え（OR条件、完全一致）"
    echo "  $0 ./Assets '*.anim' 'camera|camera_Bone' 'new_camera'"
    echo "  $0 ./Animations '*Vcam*.anim' 'old_path1|old_path2|old_path3' 'new_path'"
    echo ""
    echo "引数:"
    echo "  <対象フォルダ>           処理対象のフォルダパス"
    echo "  <ファイル名パターン>     処理するファイルのパターン (例: '*.anim', '*Vcam*.anim')"
    echo "  <置き換え前のパス>       マッチさせる既存のpath値 (複数の場合は'|'で区切る、完全一致)"
    echo "  <置き換え後のパス>       新しいpath値"
    echo ""
    echo "注意: 置き換え前のパスは完全一致で検索されます"
    exit 1
}

# 引数チェック
if [ $# -lt 4 ]; then
    show_usage
fi

TARGET_DIR="$1"
FILE_PATTERN="$2"
OLD_PATH="$3"
NEW_PATH="$4"

# フォルダの存在確認
if [ ! -d "$TARGET_DIR" ]; then
    echo "エラー: フォルダ '$TARGET_DIR' が見つかりません"
    exit 1
fi

# カウンター初期化
TOTAL_FILES=0
PROCESSED_FILES=0
SKIPPED_FILES=0

# スキップしたファイルのリスト
SKIPPED_LIST=()

echo "========================================"
echo "一括パス置き換え処理を開始します"
echo "========================================"
echo "対象フォルダ: $TARGET_DIR"
echo "ファイルパターン: $FILE_PATTERN"
echo "置き換え前: $OLD_PATH"
echo "置き換え後: $NEW_PATH"
echo "========================================"
echo ""

# OLD_PATHをエスケープしてOR条件の正規表現を構築
# パイプ区切りの各パスを個別にエスケープ
IFS='|' read -ra PATHS <<< "$OLD_PATH"
ESCAPED_PATHS=()
for path in "${PATHS[@]}"; do
    # 正規表現の特殊文字をエスケープ
    escaped=$(echo "$path" | sed 's/[.[\*^$()+?{|]/\\&/g')
    ESCAPED_PATHS+=("$escaped")
done

# エスケープされたパスを|で結合
REGEX_PATTERN=$(IFS='|'; echo "${ESCAPED_PATHS[*]}")

# ファイルを検索して処理
while IFS= read -r -d '' file; do
    TOTAL_FILES=$((TOTAL_FILES + 1))
    echo "処理中: $file"
    
    # 一時ファイル作成
    TEMP_FILE=$(mktemp)
    
    # 複数のpathフィールドを置き換え（完全一致、OR条件）
    sed -E "s/^([ ]*path: )($REGEX_PATTERN)$/\1$NEW_PATH/" "$file" > "$TEMP_FILE"
    
    # 置き換えが実行されたか確認
    if diff -q "$file" "$TEMP_FILE" > /dev/null; then
        echo "  → スキップ: '$OLD_PATH' にマッチするpathが見つかりませんでした"
        SKIPPED_FILES=$((SKIPPED_FILES + 1))
        SKIPPED_LIST+=("$file")
        rm "$TEMP_FILE"
    else
        mv "$TEMP_FILE" "$file"
        echo "  → 完了: pathを置き換えました"
        PROCESSED_FILES=$((PROCESSED_FILES + 1))
    fi
    echo ""
done < <(find "$TARGET_DIR" -type f -name "$FILE_PATTERN" -print0)

echo "========================================"
echo "処理結果"
echo "========================================"
echo "検出されたファイル数: $TOTAL_FILES"
echo "置き換え成功: $PROCESSED_FILES"
echo "スキップ: $SKIPPED_FILES"
echo "========================================"

# スキップしたファイルのリストを表示
if [ $SKIPPED_FILES -gt 0 ]; then
    echo ""
    echo "========================================"
    echo "スキップされたファイル一覧"
    echo "========================================"
    for skipped_file in "${SKIPPED_LIST[@]}"; do
        echo "  - $skipped_file"
    done
    echo "========================================"
fi

if [ $TOTAL_FILES -eq 0 ]; then
    echo ""
    echo "警告: パターン '$FILE_PATTERN' にマッチするファイルが見つかりませんでした"
    exit 1
fi