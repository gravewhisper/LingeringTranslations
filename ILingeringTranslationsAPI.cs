namespace LingeringTranslations;

public interface ILingeringTranslationsAPI
{
    /// <summary>
    /// Identifies a modded NomaiText so its translated entries can persist.
    /// Existing remembered translations are restored immediately.
    /// </summary>
    void RegisterTranslationSource(
        NomaiText nomaiText,
        string modUniqueName,
        string sourceId
    );
}