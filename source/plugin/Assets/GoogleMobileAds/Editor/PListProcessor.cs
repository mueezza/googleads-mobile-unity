#if UNITY_IPHONE || UNITY_IOS
using System;
using System.IO;

using UnityEditor;
using UnityEditor.Callbacks;
using UnityEditor.iOS.Xcode;

using GoogleMobileAds.Editor;

public static class PListProcessor
{
    private const string KEY_SK_ADNETWORK_ITEMS = "SKAdNetworkItems";

    private const string KEY_SK_ADNETWORK_IDENTIFIER = "SKAdNetworkIdentifier";

    [PostProcessBuild]
    public static void OnPostProcessBuild(BuildTarget buildTarget, string path)
    {
        string plistPath = Path.Combine(path, "Info.plist");
        PlistDocument plist = new PlistDocument();
        plist.ReadFromFile(plistPath);

        if (!GoogleMobileAdsSettings.Instance.IsAdManagerEnabled && !GoogleMobileAdsSettings.Instance.IsAdMobEnabled)
        {
            NotifyBuildFailure("Neither Ad Manager nor AdMob is enabled yet.");
        }

        if (GoogleMobileAdsSettings.Instance.IsAdManagerEnabled)
        {
            plist.root.SetBoolean("GADIsAdManagerApp", true);
        }

        if (GoogleMobileAdsSettings.Instance.IsAdMobEnabled)
        {
            string appId = GoogleMobileAdsSettings.Instance.AdMobIOSAppId;
            if (appId.Length == 0)
            {
                NotifyBuildFailure(
                    "iOS AdMob app ID is empty. Please enter a valid app ID to run ads properly.");
            }
            else
            {
                plist.root.SetString("GADApplicationIdentifier", appId);
            }
        }

        if (GoogleMobileAdsSettings.Instance.DelayAppMeasurementInit)
        {
            plist.root.SetBoolean("GADDelayAppMeasurementInit", true);
        }

        AddSkAdNetworkIdentifier(plist);

        File.WriteAllText(plistPath, plist.WriteToString());
    }

    private static PlistElementArray GetSkAdNetworkItemsArray(PlistDocument document)
    {
        PlistElementArray array;
        if (document.root.values.ContainsKey(KEY_SK_ADNETWORK_ITEMS))
        {
            try
            {
                PlistElement element;
                document.root.values.TryGetValue(KEY_SK_ADNETWORK_ITEMS, out element);
                array = element.AsArray();
            }
#pragma warning disable 0168
            catch (Exception e)
#pragma warning restore 0168
            {
                // The element is not an array type.
                array = null;
            }
        }
        else
        {
            array = document.root.CreateArray(KEY_SK_ADNETWORK_ITEMS);
        }
        return array;
    }

    private static void AddSkAdNetworkIdentifier(PlistDocument document)
    {
        PlistElementArray array = GetSkAdNetworkItemsArray(document);
        if (array != null)
        {
            PlistElementDict dict = array.AddDict();
            dict.SetString("SKAdNetworkIdentifier", "cstr6suwn9.skadnetwork");
        }
        else
        {
            ThrowBuildException("SKAdNetworkItems element already exists, but is not an array.");
        }
    }

    private static void NotifyBuildFailure(string message)
    {
        string prefix = "[GoogleMobileAds] ";

        bool openSettings = EditorUtility.DisplayDialog(
            "Google Mobile Ads", "Error: " + message, "Open Settings", "Close");
        if (openSettings)
        {
            GoogleMobileAdsSettingsEditor.OpenInspector();
        }

        ThrowBuildException(prefix + message);
    }

    private static void ThrowBuildException(string message)
    {
#if UNITY_2017_1_OR_NEWER
        throw new BuildPlayerWindow.BuildMethodException(message);
#else
        throw new OperationCanceledException(message);
#endif
    }
}

#endif
