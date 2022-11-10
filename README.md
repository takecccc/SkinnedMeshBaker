# SkinnedMeshBaker
SkinnedMeshRendererをMeshRendererに変換するUnityのエディタ拡張。

vketにてcc0で配布されていたものから、Clothコンポーネントが付いたオブジェクトを正常にBake出来ていなかった問題を修正しています。
https://winter2022.vket.com/docs/submission_tips_skinned_mesh_baker

# License
CC0

# How to Use
1. unitypackageをプロジェクトへインポート
2. poseを取った状態のオブジェクトをHierarchy上で右クリック BakeSkinnedMeshPoseを選択。
   (ClothコンポーネントやPhysBoneを動かしたい場合、再生状態で右クリック)
3. Assets/BakeResultフォルダにBakeされたオブジェクトが生成され、Hierarchyにも追加されます。
   (再生状態で生成した場合、Hierarchyに追加されたオブジェクトは停止すると消えてしまいますので、Assets/BakeResultフォルダのPrefabを使用します。)

# Note
* このツールはBake前のオブジェクト等を書き換えません。再Bakeする場合はBake前のオブジェクトを編集して再トライできます。
* テクスチャ・マテリアル・シェーダー等は元のメッシュの参照が引き継がれます。
