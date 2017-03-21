using UnityEditor;

public class CreateAssetBundles
{
    [MenuItem("Assets/Build AssetBundles")]
    static void BuildAllAssetBundles()
    {
        BuildPipeline.BuildAssetBundles("Assets/StreamingAssets/AssetBundlesPC", BuildAssetBundleOptions.None, BuildTarget.StandaloneWindows);
        BuildPipeline.BuildAssetBundles("Assets/StreamingAssets/AssetBundlesAndroid", BuildAssetBundleOptions.None, BuildTarget.Android);
        BuildPipeline.BuildAssetBundles("Assets/Photon Unity Netowrking/Resources/AssetBundlesHololens", BuildAssetBundleOptions.None, BuildTarget.WSAPlayer);
    }
}
