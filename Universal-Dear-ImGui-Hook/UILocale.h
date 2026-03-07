#pragma once
// No STL includes here — only primitive types to avoid PCH conflicts with <windows.h>

namespace Locale {
    void SetLanguage(const char* langCode);
    const char* T(const char* key);
    const char* GetCurrentLanguage();
    // Language list enumeration
    int GetLanguageCount();
    const char* GetLanguageName(int index);
    void RefreshLanguageList();
}

// Short global alias usable in every UI file
inline const char* T(const char* key) { return Locale::T(key); }
