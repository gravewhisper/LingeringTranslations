using UnityEngine;

namespace LingeringTranslations;

public class TranslationSource : MonoBehaviour
{
    public string ModUniqueName;
    public string SourceID;

    public string Key =>
        $"{ModUniqueName}:{SourceID.Replace('\\', '/')}";
}