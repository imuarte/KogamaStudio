#define _SILENCE_EXPERIMENTAL_FILESYSTEM_DEPRECATION_WARNING
#include "stdafx.h"
#include "UILocale.h"
#include <unordered_map>
#include <string>
#include <vector>
#include <fstream>
#include <experimental/filesystem>
#include "json.hpp"

using json = nlohmann::json;
namespace fs = std::experimental::filesystem;

namespace Locale {

    static std::unordered_map<std::string, std::string> s_translations;
    static std::string s_currentLang = "en";
    static std::vector<std::string> s_langList;

    static std::string GetLangDir() {
        char buf[MAX_PATH];
        ExpandEnvironmentStringsA("%LOCALAPPDATA%\\KogamaStudio\\Lang", buf, MAX_PATH);
        return buf;
    }

    void SetLanguage(const char* langCode) {
        if (!langCode) langCode = "en";
        s_currentLang = langCode;
        s_translations.clear();
        if (s_currentLang.empty() || s_currentLang == "en")
            return;

        std::string path = GetLangDir() + "\\" + s_currentLang + ".json";
        if (fs::exists(path)) {
            try {
                std::ifstream f(path);
                json j = json::parse(f);
                for (auto it = j.begin(); it != j.end(); ++it) {
                    if (it.value().is_string())
                        s_translations[it.key()] = it.value().get<std::string>();
                }
            } catch (...) {}
        }
    }

    const char* T(const char* key) {
        if (!key) return "";
        auto it = s_translations.find(key);
        if (it != s_translations.end())
            return it->second.c_str();
        return key;
    }

    const char* GetCurrentLanguage() {
        return s_currentLang.c_str();
    }

    void RefreshLanguageList() {
        s_langList.clear();
        s_langList.push_back("en");
        std::string dir = GetLangDir();
        if (!fs::exists(dir)) return;
        try {
            for (auto& entry : fs::directory_iterator(dir)) {
                if (entry.path().extension() == ".json")
                    s_langList.push_back(entry.path().stem().string());
            }
        } catch (...) {}
    }

    int GetLanguageCount() {
        if (s_langList.empty()) RefreshLanguageList();
        return (int)s_langList.size();
    }

    const char* GetLanguageName(int index) {
        if (s_langList.empty()) RefreshLanguageList();
        if (index < 0 || index >= (int)s_langList.size()) return "";
        return s_langList[index].c_str();
    }
}
